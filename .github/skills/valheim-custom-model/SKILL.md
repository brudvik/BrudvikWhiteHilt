---
name: valheim-custom-model
description: 'Add a free 3D model (glTF/.glb from Sketchfab, Poly Haven, Quaternius, etc.) to the BrudvikWhiteHilt Valheim mod. Use when: adding a new model, mesh, texture or asset bundle; adding an animated creature or monster from a rigged model (Blender + Unity animator); replacing the look of a cloned vanilla item, pickable, plant, food or piece; searching for free models; checking model licenses; rebuilding whitehilt_foraging. Covers license check, glb conversion, Unity 6 asset bundle build, runtime mesh swap, icons, dedicated-server safety and in-game testing.'
argument-hint: 'Model file or what the model should be used for'
---

# Custom 3D models in BrudvikWhiteHilt

Proven in game on 2026-09-27 with the Chanterelle. The approach: clone a vanilla prefab with Jotunn and keep all its
components, colliders and vanilla material. Hide its meshes, show our mesh in their place and swap in our texture.
No custom shaders or prefabs are shipped. The bundle only contains meshes and textures.

## When to use
- A new item, pickable or piece needs a look that no vanilla prefab has.
- If a vanilla model with a tint (`VisualHelper.Tint`) or a hue shift (`VisualHelper.Recolor`) is good enough, use that instead. It needs no asset work.

## Tools (AssetSource/Tools, run with the repo's .venv python; usage at the top of each file)
- `find_models.py "<term>" [...]`: Sketchfab search limited to CC0/CC BY and a face budget, numbered contact sheet in `%TEMP%\wh_models\sheet.png`; `--uids <uid> ...` sheets chosen models with descriptions; `--animated` only rigged, animated ones.
- `glb_info.py <file.glb>`: author/license/source, every part with its material and bounds as the model stands, materials, image sizes. `--profile <axis>` slices it; `--sides out.png --right x,y,z --up x,y,z` draws both sides with flat material colours (front vs back).
- `glb_edit.py <file.glb> --colour <material>=r,g,b --drop <node> --keep <node> --max-texture 1024 [--out]`: fix a download before converting (in place without `--out`).
- `vanilla_prefab.py <prefab> [--materials] [--all-bundles]`: a vanilla prefab's hierarchy with local position/rotation/scale, mesh bounds, colliders, materials (shader, textures, colours); `--hash <name>` gives Valheim's stable hash.
- `AssetSource/Preview/compose_preview.py`, `weapon_fit.py`, `frame_axes.py`: see below.

## 1. Find and check the model
- Search with `find_models.py` (or the Sketchfab API directly: `https://api.sketchfab.com/v3/search?type=models&q=<term>&downloadable=true&license=cc0`). Other sources are Poly Haven (`https://api.polyhaven.com/assets?type=models`), Quaternius and Kenney. The last two are CC0 and stylized.
- **Licenses**: CC0 is fine. CC BY is fine with credit in the README `## Credits`. Do **not** use NC (NonCommercial) or unclear licenses like Sketchfab "Free Standard".
- **Budget**: at most about 5 000 faces, texture ≤ 1024 px. It gets downscaled to 512. Photogrammetry models with 40k+ faces or 8K textures are too heavy.
- **Shape**: any number of mesh parts. [convert_glb.py](../../../AssetSource/convert_glb.py) merges them into one mesh. Parts that share one base colour texture keep it as-is (PNG or JPEG). Parts with **different textures** or with **only a base colour** (no texture) are packed side by side into one PNG atlas, and their UVs are moved to match. Tiling UVs (outside 0..1) are wrapped per triangle into the first repeat, and the few repeats a triangle still spans are baked into its tile (the Waste Well needed this). Atlases get 512 px per tile in Unity, at most 4096 wide.
- **Emission**: a model with one texture and an `emissiveTexture` also gets `<name>_emission` in the bundle. Put it on `_EmissionMap` (enable `_EMISSION`), optionally recoloured with `VisualHelper.RecolorTexture`; the Valkyrie Stone does this.
- Sketchfab downloads need a login, so the user downloads the `.glb` themselves.
- Read the license from the file itself before using it (`glb_info.py` prints it). It sits in `asset.extras`: author, license, source.
- **Look at the thumbnail before recommending a model.** Names and tags lie: the first "Porcini mushroom" (CC BY, 210 faces) turned out to be an auto-generated orange blob. `find_models.py` draws the contact sheet; view it.

## 2. Build the bundle
1. Copy the file to `AssetSource/Models/<name>.glb`. The lower-case file name becomes the mesh name `<name>` and the texture name `<name>_albedo` (`.png` or `.jpg`, whatever the model contains).
2. Run `powershell -ExecutionPolicy Bypass -File AssetSource\build_foraging_bundle.ps1`. It needs Python and Unity **6000.0.75f1**, the same version as Valheim; check with `UnityPlayer.dll` FileVersion in the game folder.
   - It converts every `.glb` to OBJ + PNG. The mesh is baked to world space, stands at the origin with height 1, and is made double-sided because Valheim shaders cull back faces.
   - It creates the git-ignored Unity project `BrudvikWhiteHiltUnity` on first run, then builds with `-executeMethod BuildForagingBundle.Build`.
   - It copies the result to `BrudvikWhiteHilt/Assets/whitehilt_foraging`, which is embedded in the DLL.
3. Check the `[WhiteHilt] Mesh '<name>' ... bounds` line. Y must be the height (Extents.y = 0.5). The full log is `%TEMP%\whitehilt_unity_build.log`.
4. To check the orientation before Unity, run `python AssetSource\preview_obj.py <file.obj> <out.png>` on a converted OBJ and view the PNG. It shows front and side silhouettes. A model lying on its side shows up there at once.
5. **Composite preview (do this before testing in game)**: for a model with props on it (a bench with tools, a table with a bucket), write `AssetSource/Preview/<name>.preview.json` with the same numbers as the code (`base` fitted like `ReplaceMesh(size:)` or by `height`, `props` placed like `AddMesh`: base position in base-model units, height, yaw) and run `python AssetSource\Preview\compose_preview.py AssetSource\Preview\<name>.preview.json`. It renders textured views with Unity into `BrudvikWhiteHiltUnity/Preview/out/<name>.png`; view it. Example: `paintbench.preview.json`, matching `PaintBench.props`. It caught a palette rendered all orange and a leftover saucer before anyone started the game.
   - Held items use `AssetSource\Preview\weapon_fit.py` instead (see below).

## 3. Use it in code
```csharp
if (!VisualHelper.IsHeadless)
{
    VisualHelper.ReplaceMesh(visualRoot, ForagingAssets.LoadMesh("<name>"), ForagingAssets.LoadTexture("<name>_albedo"));
}
Sprite icon = VisualHelper.RenderIcon(item.ItemPrefab); // null on a server or failure: keep the vanilla icon
```
- `visualRoot`: the item prefab, or `Pickable.m_hideWhenPicked` for a pickable, so the model hides when picked.
- `ReplaceMesh` fits the new mesh to the **height** of the vanilla meshes and puts it on the same base. So pick a vanilla prefab of similar size and shape to clone, e.g. `Mushroom` / `Pickable_Mushroom`.
- For a model with very different proportions that hangs, pass `hang: true`: it fits the **width** and keeps the **top** in place. The Stone Pot uses it on `piece_cauldron`'s `new/cauldron (1)`, so only the hanging pot changes and the tripod, chain and fire effects stay vanilla.
- For a flat model that should not be stretched, pass `size:` (longest side in local units); the Herb Tray does this and refits the piece's BoxColliders to the returned model.
- To combine two models, `ReplaceMesh` returns the new model and `VisualHelper.AddMesh(model, mesh, texture, basePosition, height)` adds a second one in its mesh units (the model is 1 high). The Chain Bench hangs chains from its vise this way.
- To build a piece from scratch, `VisualHelper.HideRenderers(prefab)` hides every vanilla look (new, worn, broken) and `VisualHelper.CreateModel(parent, mesh, texture, templateRenderer, position, rotation, scale)` adds meshes that reuse a vanilla material. Vanilla meshes can be reused too: the Rune Post copies `wood_pole2/New` for its posts. For a glow, prefer an emission map (only the lit parts bright, e.g. carved runes) pulsed per instance with a `MaterialPropertyBlock`, plus a small point light faded by `EffectFade`. Vanilla effects are sized for their own piece: `portal_wood/_target_found_red` on the Rune Post was a huge, far too intense flame swirl. For a piece that swaps looks as it takes damage (e.g. `piece_workbench`), call `ReplaceMesh` on each of `New`, `Worn` and `Broken`.
- `VisualHelper.RecolorTexture(texture, pixel => ...)` returns a recoloured copy of any texture, e.g. one ring texture in six metal colours.
- Item prefabs contain an **inactive** held/equip copy of the model (`Mushroom/equipoffset/pie (1)` sits 50 m below the item). `ReplaceMesh` only measures renderers whose parents are all `activeSelf` up to the root. `activeInHierarchy` does not work, because Jotunn's prefab container is disabled. Without this the dropped item became about 50 m tall.
- To see a vanilla prefab's real hierarchy, run `AssetSource\Tools\vanilla_prefab.py <prefab>` (UnityPy on `valheim_Data/StreamingAssets`: transforms, `activeSelf`, scales, mesh bounds, materials). Verify with it before guessing.
- Examples: [ForageableBase.cs](../../../BrudvikWhiteHilt/Items/Foraging/ForageableBase.cs), [Chanterelle.cs](../../../BrudvikWhiteHilt/Items/Foraging/Chanterelle/Chanterelle.cs), [VisualHelper.cs](../../../BrudvikWhiteHilt/Helpers/VisualHelper.cs), [ForagingAssets.cs](../../../BrudvikWhiteHilt/Helpers/ForagingAssets.cs).
- Add new `.cs` files to `BrudvikWhiteHilt.csproj` (old-style project, no globbing).

### Weapons and shields (held items)
- The hand is the origin of the item's `attach` child: `VisEquipment.AttachItem` puts the attach copy on the hand joint with an identity transform, so only the children's transforms count.
- Put `<name>.weapon.json` next to the glb: the mesh is then written in attach space in metres (no height-1 placement) and `WhiteHiltWeaponBase.ModelName` swaps it in with `VisualHelper.ReplaceWeaponMesh` (reuses the vanilla model object, so its collider and LOD entry stay). Fields: `axes` (glb direction for Unity X, Y, Z; must have determinant -1, the converter checks), `grip` (glb point for the hand), `length`+`measure` or `scale`, optional `offset`.
- Tilted vanilla models (bows, shields): `python AssetSource\Preview\frame_axes.py <prefab> <child> <glb for local X> <Y> <Z>` prints `axes` that line up with the vanilla child's frame, and where the hand is in its mesh space. Shields: the hand is on the back, so that tells front from back (ShieldBanded's front is local -z, ShieldIronTower's +z).
- Check every fit with `python AssetSource\Preview\weapon_fit.py AssetSource\Models\<name>.glb <vanilla prefab>` (red vanilla, blue new, cross = hand, green = sword trail) before building.
- Theme: `<name>.paint.json` recolours texture areas (`material` name or a `min`/`max` box in the final mesh space) keeping the shading; the White Hilt look is ivory `[0.92, 0.9, 0.84]` on hilts, grips and shield boards.
- A mesh the mod deforms at runtime (the bow flex) must be in `BuildForagingBundle.CombinedModels` so it stays readable.
- `split` in weapon.json writes parts (by material) as their own OBJ with the same texture, optionally bent into a V (`pull`, `span`). The crossbow uses it for its string, straight and drawn back; `WhiteHiltCrossbow.OnModelApplied` puts them on the Unloaded/Loaded objects that the vanilla `WeaponLoadState` toggles.
- Vanilla MeshColliders under attach are replaced by a BoxCollider (`ReplaceWeaponMesh`); a multi-material renderer (Battleaxe) gets one material.

## Animated creatures (rigged glTF, first used for the Kraken and the octopus in 0.20.0)
- Find: `find_models.py "<term>" --animated` (animation count per model). Check the download: `glb_info.py` lists skins and animations; `preview_animations.py <file.glb>` renders every animation with Blender and prints each part's posed bounds. A part hundreds of metres away has broken skinning: drop it (the Lurker's eyes and upper beak were).
- Source: `AssetSource/Creatures/<name>.glb` + `<name>.creature.json` (`drop`; `pose` + `eyes` = glowing spheres skinned to a bone, positions in Blender world space at that pose; `clips` = actions to keep with their new names and loop flags; `front_bone` turns the model to face +z; `controller`: `idle`, optional `move` + `move_speed` (1D blend on forward_speed, m/s), optional `run` + `run_speed`, optional `stagger` (clip on the stagger trigger), `attacks` [{trigger, clip, hit = normalised time of the OnAttackTrigger event}], `always_animate`). Missing clips (idle, attack, death) can be keyed in Blender with `generate`: a base pose (take + frame) plus local bone rotations per key frame, bones matched by name prefix; find the axes first by rotating test bones and rendering (the giant spider's idle, bite, hit and die are made this way). Needs Blender (found under Program Files).
- Build: `build_foraging_bundle.ps1` runs `Tools/export_creature.py` in Blender (one joined mesh, textures as `<name>_<material>_albedo/_normal.png`, `<name>.materials.json`, FBX), then Unity's `BuildCreatures.cs` makes `<name>_visual.prefab` (model under an empty root, Animator with Valheim's parameter names, attack states tagged `attack`, other clips as states for `Animator.Play`, Standard materials). The log prints how many curves of each clip bind and the idle bounds. `-UnityOnly` rebuilds without converting again.
- Check: `AssetSource\Preview\render_creatures.ps1` renders `<name>_anim.png` (every state, front = +z faces the camera) and a transparent 3/4 `<name>.png` for the docs. Unity does not update skinning between renders in batch mode, so the preview bakes each pose with `BakeMesh`.
- Use: `CreatureVisual.Attach(parent, "<name>", CreatureVisual.TemplateOf(vanillaCreature), position, rotation, scale)` moves the textures onto copies of a vanilla creature material (skipped on a dedicated server). On a cloned creature: empty `Visual`, attach, add `CharacterAnimEvent` on the Animator object (Jotunn's `CustomCreature` needs both), and give the weapon `m_attackAnimation` = the trigger with `m_attackChainLevels = 1` and `m_attackRandomAnimations = 0` (else a number is appended). See `Kraken/KrakenRegistry.cs`.
- Vanilla values: `vanilla_prefab.py <prefab> --values` prints component fields (an item's shared data and attack) and animator states/clips; `vanilla_environments.py` prints the weather table (fog, wind, weights per biome).

## Pitfalls (all hit once already)
- **Parts with only a base colour and the same colour share one atlas tile**, so a `paint.json` rule per material paints them all (the palette's seven paint blobs, all grey 0.8 in the glb, came out one colour). Set distinct colours in the glb with `glb_edit.py --colour <material>=r,g,b` instead. A part you do not want at all is dropped with `glb_edit.py --drop <node>`. That is cleaner than a crop box when the part touches the rest.
- **Unity mirrors x when it imports OBJ.** A part you measure at +x in the converted `.obj` is at -x in the game. Negate x before using a measured position in code (y and z are unchanged). The Chain Bench chains first ended up on the vise instead of the stump because of this.
- A flat model (a curtain of chains, a plank) seen **edge-on** looks like a thin line. Check which axis is thin in the `[WhiteHilt] Mesh` bounds line and rotate it (`AddMesh(..., rotation)`) so its broad side faces the player.
- **Never use Jotunn `AssetUtils.LoadAssetBundleFromResources`**. It disposes the stream, Unity reads bundle data lazily, and the game crashes with "ManagedStream object must be readable". `ForagingAssets` uses `AssetBundle.LoadFromMemory`.
- Meshes are **sub-assets** of the imported model, so use `LoadAllAssets<Mesh>()`, not `LoadAsset<Mesh>(name)`. OBJ mesh names come from the `g` line, not `o`. Otherwise Unity calls the mesh `default`.
- **Dedicated servers** (Linux, no GPU) must skip `ReplaceMesh`, `Tint`, `Recolor` and `RenderIcon`. Guard with `VisualHelper.IsHeadless`. Items, pickables and recipes must still be registered on the server.
- **Timing**: pickables and vegetation must be created in `PrefabManager.OnVanillaPrefabsAvailable` (main menu). `PrefabManager.AddPrefab` and `ZoneManager.AddCustomVegetation` do not register into a running `ZNetScene` or `ZoneSystem`. Edits to vanilla prefabs, such as creature drops, go in `OnPrefabsRegistered` and must be idempotent.
- Vegetation only spawns in **newly generated** zones. For existing worlds, add an extra drop: `ForageableBase.ExtraDropFrom` + `ExtraDropBiome` (biome-aware, via `Patches/Foraging/ForagingDropPatch.cs`), or `CreatureDropFrom` for a vanilla creature.
- A failing `ApplyVisual` is caught by `ForageableBase.TryApplyVisual` and the item keeps its vanilla look. Never let a visual error stop an item from registering, or players lose it from their inventories.
- A cloned crafting station needs a unique `CraftingStation.m_name`. Recipes match stations by name.
- `Tint` multiplies colours, so it can only darken a channel. To change one colour, e.g. blue berries to red, use `VisualHelper.RecolorHue(root, hueMin, hueMax, newHue, saturationScale, valueScale)`. It only touches clearly coloured pixels in the hue range, so leaves and stems stay green or brown.
- Materials that use `KHR_materials_pbrSpecularGlossiness` keep their texture in `diffuseTexture`. The converter handles both that and `baseColorTexture`.

## 4. Test and ship
1. Build Debug: `msbuild BrudvikWhiteHilt.sln /t:Build /p:Configuration=Debug`. It deploys to the r2modman profile set in the git-ignored `Environment.props` (`MOD_DEPLOYPATH`). **Close Valheim first**: a running game locks the DLL, so the copy fails silently and you end up testing the old build. Compare file hashes if in doubt.
2. In game with devcommands: `spawn <PrefabName>` for the item, `spawn Pickable_<PrefabName>` for the plant. Check size, that it stands on the ground, the texture and swaying. Also check that the BepInEx log shows "`<Name>` added!" on both client and server.
3. Follow the checklist in `.github/copilot-instructions.md`: the feature's page in `docs/`, `CHANGELOG.md`, `## Credits` for CC BY, and the version in `AssemblyInfo.cs`, the `PluginVersion` constant and `Package/manifest.json`.
4. Picture for the docs: add an entry to `AssetSource/Preview/showcase.json` (`models` for bundle models, `piece` for a `defenses.json` piece, `compose` for a `.preview.json` spec; optional `view` yaw/pitch), run `python AssetSource\Preview\render_showcase.py <image>` and put `<img src="images/<image>.png" alt="Name" title="Name" height="140">` in the image row above the item table of its `docs/` page. It renders on a transparent background, crops and scales to at most 480 px. Parts only added in code (vanilla pelts, fire, recolours) are not in the picture.
