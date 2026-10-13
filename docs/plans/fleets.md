# ⚓ Plan: Nail Ship fleets and the draugr jarl

Implementation plan for *Fleets* under *More for the sea battles* in [Ideas for new content](../ideas.md). It builds on
the sea battles (0.128.0) and damage at sea (0.129.x); their plans and logs are [sea-battles.md](sea-battles.md) and
[sea-damage-and-weapons.md](sea-damage-and-weapons.md). Written for an AI agent that carries it out phase by phase
without the conversation behind it. Read all of it once before you start, and read section 11 of both earlier plans
for what the existing code really does.

## 1. What the player gets

- **Fleets.** Once Moder is slain, a Nail Ship sometimes comes with company: two ships, and after Yagluth up to three.
  They sail up in a wedge behind a **flagship**, break formation when the fight starts, and come at you from both sides.
- **The flagship.** A Nail Ship with a blood-red sail, a stronger hull, a bigger crew with more stars, and always a
  ballista on its bow. Its hold carries more loot and always a Raid Chart.
- **The draugr jarl.** The flagship is led by a jarl: a huge undead viking with a fur cloak and a great axe, a creature
  of his own with his own model, animations and glowing eyes. He fights from the deck and boards with his men.
- **Morale.** When the jarl falls or the flagship is taken or sunk, the other ships of the fleet turn and run.
- **Saga.** Breaking a fleet (the flagship taken or sunk) is a deed of its own, and slaying the jarl is a listed
  creature kill.
- **Console.** `whitehilt_seabattle fleet [2|3]` sends a whole fleet (admin).

## 2. Decisions already made

| Decision | Choice | Why |
|---|---|---|
| Jarl model | `d:\3dmodels\northern_outsider.glb`, "Northern Outsider" by Splodeman, CC BY 4.0, https://sketchfab.com/3d-models/northern-outsider-296600f89eab458f99f9fd8105aebed5 | Chosen by the user |
| Jarl look | Undead: the skin and beard greyed toward the draugr (in code, `VisualHelper.Recolor` style, or in the creature pipeline), two glowing eyes | He leads a crew of the dead |
| Jarl creature | A sixth entry in `WildMonsters` / `WildMonsterRegistry` (clone of a vanilla humanoid like the other wild monsters), but **no natural spawns**: he only appears on a flagship | Reuses the tested creature path (corpse, weapons, sounds, stars, settings section) |
| Jarl sounds | The vanilla `Draugr_Elite` sounds (the creature keeps or copies them); no new sound files | Fits, and no asset work |
| Flagship | Its own prefab `WhiteHilt_NailFlagship`, a clone of `WhiteHilt_NailShip` made in `NailShipRegistry`, with its own sail colour and hull health | Clean per-prefab look and stats; no per-instance material work |
| Fleet identity | Every ship of a fleet stores the flagship's `ZDOID` and its rank (0 = flagship, 1 and 2 = escorts) | Survives ownership changes; escorts can read the flagship's ZDO even when it is unloaded |
| Fleet size | Chosen by the server per encounter from a setting of key → size and chance; one request still raises one fleet | Same server-decides rule as the rest |
| Formation | Escorts hold slots in a wedge behind the flagship until it closes in, then fight on their own sides | Readable and cheap |
| Biome | Fleets and the jarl are encounters, not recipes; loot stays Swamp tier or earlier, as for every Nail Ship | Biome rule |
| Release | Changelog under `## Unreleased`; the spawn RPC changes, so the release is a MINOR bump | Copilot instructions |

## 3. Rules for the implementer

Section 3 of [sea-battles.md](sea-battles.md) applies in full (settings with Norwegian labels and `cfg_keys.py
--check`, class-level `[HarmonyPatch]`, `ZNetScene.instance.Destroy`, owner-only ZDO writes, headless guards with
try/catch, no file edits from the terminal, verify vanilla names, read `%TEMP%\wh_tame\full\*.cs`, build and test
after each phase, no commit/push/deploy, XML docs, deviations in the log of this file). The asset rules are in
[.github/skills/valheim-custom-model/SKILL.md](../../.github/skills/valheim-custom-model/SKILL.md): read it, especially
*Animated creatures* and *Pitfalls*. Also:

- Inside `BrudvikWhiteHilt.Patches.*` namespaces write `global::BrudvikWhiteHilt.SeaBattles.X`.
- Pure logic in static classes with plain values, tested in `BrudvikWhiteHilt.Tests` (no `Ship`/`Player` types there).
- Never commit large source files you did not need: the glb in `AssetSource/Creatures/` must be shrunk first (below).

## 4. Settings

New section `SeaBattles.Fleets` (admin only, Norwegian labels):

| Key | Default | Range | Meaning |
|---|---|---|---|
| `Enabled` | true | | Nail Ships can come as fleets |
| `FleetTiers` | `defeated_dragon=2:35; defeated_goblinking=3:50` | | `key=size:chance%`; the last tier whose key is set wins; no tier = always one ship |
| `FormationSpacing` | 30 | 10–80 | Metres between ships in the wedge |
| `FormationBreak` | 80 | 20–300 | The escorts leave the wedge when the flagship is this close to its target, or no longer approaches |
| `EscortSpread` | 1.6 | 1–3 | An escort on the same side as the flagship keeps this many times the broadside distance |
| `FlagshipHullHealth` | 2000 | 100–20000 | Health of the flagship |
| `FlagshipExtraCrew` | 2 | 0–10 | Extra crew on the flagship besides the jarl |
| `FlagshipCrewLevel` | 2 | 1–6 | Least level (stars + 1) of the flagship's crew |
| `JarlLevel` | 3 | 1–6 | Level (stars + 1) of the jarl |
| `FlagshipLootRolls` | 4 | 0–20 | Extra draws for the flagship's hold |
| `FlagshipRaidChart` | true | | The flagship's hold always carries a Raid Chart |
| `FleeWhenFlagshipFalls` | true | | Escorts flee when the flagship is a prize or sunk |
| `FleeWhenJarlFalls` | true | | Escorts flee when the jarl is slain |

The jarl's own section comes with the wild-monster pattern (`Monsters.DraugrJarl`: health, damage, scale, sounds...;
copy what the other wild monsters have, drop the spawn keys or set them so he never spawns naturally) — look at
`Monsters/WildMonsterSettings.cs`. Saga: `RenownFleet` (40) in `[Saga]`; add the jarl to the saga's listed creatures if
the saga keeps such a list (`SagaSettings.IsListed`).

## 5. Names, keys and texts

Prefabs: `WhiteHilt_NailFlagship`, `WhiteHilt_DraugrJarl` (and its corpse, trophy if the wild-monster pattern makes
one: `WhiteHilt_TrophyDraugrJarl`).

ZDO keys on a fleet ship (hash once, in `RaiderKeys`):

| Key | Type | Meaning |
|---|---|---|
| `whitehilt_raider_fleet` | ZDOID | The flagship's id; None for a lone ship |
| `whitehilt_raider_rank` | int | 0 flagship, 1–2 escort slot |
| `whitehilt_raider_jarl` | bool | On a creature: it is the jarl |
| `whitehilt_raider_jarlfallen` | bool | On the flagship: its jarl has been slain |
| `whitehilt_raider_broken` | bool | On the flagship: the fleet is broken (saga deed given) |

English texts, with Norwegian:

| Key | English | Norwegian |
|---|---|---|
| `enemy_whitehilt_draugrjarl` | Draugr Jarl | Draugrjarl |
| `whitehilt_nailflagship` | Nail Flagship | Naglflaggskip |
| `msg_whitehilt_fleet_sighted` | A fleet on the horizon! The Nail Ships come in force. | En flåte i horisonten! Naglskipene kommer i flokk. |
| `msg_whitehilt_jarl_falls` | The jarl has fallen! | Jarlen har falt! |
| `msg_whitehilt_fleet_breaks` | The fleet breaks and runs! | Flåten brytes og flykter! |
| `whitehilt_saga_fleet` | A Nail fleet was broken | En naglflåte ble knust |

Trophy name/description if one is made: "Draugr Jarl Trophy" / "The rotting head of a jarl who sailed with Naglfar." —
Norwegian "Draugrjarl-trofé" / "Det råtnende hodet til en jarl som seilte med Naglfar."

## 6. Phases

Each phase ends with the checks in section 9.

### Phase A: the jarl's model and animations (assets only)

1. Inspect `d:\3dmodels\northern_outsider.glb` (32 MB, 39 832 triangles, three 4096 px PNGs, one material
   `Material_15`, a skin of 72 joints, animations `idle` (2.7 s) and `walk` (4.67 s)) with
   `.venv\Scripts\python.exe AssetSource\Tools\glb_info.py` and
   `.venv\Scripts\python.exe AssetSource\Tools\preview_animations.py <glb> --drop ... --focus 0,0,110 --size 260`.
   Known so far: `Object_15` and `Object_17` have broken skinning (posed bounds tens of kilometres away) and
   `Object_9` (4 196 tris, most likely the axe) poses about 6–11 m below the body. With those three dropped, idle and
   walk render correctly (body height about 1.8 m in Blender units ×0.01). Find out what `Object_9` is; if it is the axe,
   try to keep it (parent it to the right-hand bone in the Blender step, or fix its skinning); if that fails, drop it and
   log it — the jarl then fights bare-handed in the animation while the game's weapon does the damage.
2. Shrink the source: textures to 1024 px (`AssetSource\Tools\glb_edit.py <glb> --max-texture 1024 --out
   AssetSource\Creatures\draugrjarl.glb`), and keep the triangle count under about 10 000 in the bundle (the body
   `Object_13` has 23 762): use the creature pipeline's decimation if `export_creature.py` supports it (the Nidhogg
   Spawn used 0.55; read the script and `AssetSource/Creatures/nidhoggspawn.creature.json`), else decimate in the glb.
   The committed `draugrjarl.glb` should be a few MB at most.
3. Write `AssetSource/Creatures/draugrjarl.creature.json` following `yggdrasilwarden.creature.json` and
   `nidhoggspawn.creature.json`: `source` credit line, `drop` the broken parts, `clips` idle and walk from the glb, and
   `generate` the missing ones from bone rotations (find the bone names first; rotate test bones and render to learn
   the axes, as the skill describes):
   - `swing`: a two-handed overhead axe chop (wind-up 0–0.4, strike about 0.5, recover), hit at the strike;
   - `sweep`: a sideways swing, as a second attack;
   - `hit`: a short stagger;
   - `die`: falling backwards or to the knees and over, ending on the ground.
   `controller`: idle, move = walk (`move_speed` from the walk's stride, about 1.4), run = walk faster (`run_speed`
   about 3.5), stagger = hit, attacks `jarl_swing` (hit ≈ 0.5) and `jarl_sweep`. `front_bone` / `front_origin_bone`
   so he faces +z. `eyes`: two small glowing spheres at the eyes in the idle pose (use `--focus` on the head to place
   them; the Kraken and Lindorm creature.json files show the format), a cold blue-green like the draugr.
4. Build: `powershell -ExecutionPolicy Bypass -File AssetSource\build_foraging_bundle.ps1` (Blender + Unity
   6000.0.75f1). Check the log for the clip curve counts and idle bounds, then render
   `AssetSource\Preview\render_creatures.ps1 -Creature draugrjarl` and look at every state in `draugrjarl_anim.png`:
   he stands upright, faces the camera at the front, the swing reads as a swing, the death lies on the ground. Iterate
   on the generated keys until it does. Mind the memory note: Blender frame numbers do not map linearly to Unity clip
   time; check in the Unity render.
5. The undead look: either recolour the albedo in the pipeline (desaturate the skin toward grey-green, darken the
   beard and cloth a little), or leave the texture and let phase 2 recolour it in code. Choose one, say which in the
   log. The doc picture comes from the same render (`draugrjarl.png`); copy it to `docs/images/draugrjarl.png` if the
   other monster pictures were made that way (check `docs/images/` and `AssetSource/Preview/showcase.json`).
6. Credits: add the model to `README.MD` `## Credits` in the same format as the other Sketchfab creatures.
7. If the rebuilt bundle changes the decor bundle only in bytes, restore that from git (see the earlier plan logs).

### Phase 1: settings, texts and pure logic

1. `SeaBattles/Fleets/FleetSettings.cs` (section 4), `Initialize()` from `Awake` next to the other sea battle
   settings. `RenownFleet` in `SagaSettings`, `SagaKind.Fleet` in `SagaLog` with its `RPC_Deed` case.
2. All texts of section 5 (English in code, Norwegian in `Norwegian.json`), config labels for every key.
3. `SeaBattles/Fleets/FleetRules.cs`, pure, tested in `BrudvikWhiteHilt.Tests/FleetTests.cs`:
   - `ParseTiers(string)` → list of (key, size 1–3, chance 0–100), tolerant like `CrewTable.Parse`;
     `PickSize(tiers, Func<string,bool> hasKey, float roll)` → 1 when no tier applies or the roll fails.
   - `SlotPoint(Vector2 flagship, float heading, int rank, float spacing)` → wedge: rank 1 back-left, rank 2
     back-right (about 0.7 × spacing back and to the side).
   - `EscortSail(Vector2 self, Vector2 slot, float heading, float spacing)` → `RaiderSail` and course: Full when
     behind and far, Half when near, Stop when ahead of the slot; within 10 m steer the flagship's heading.
   - `FlagshipWaits(IReadOnlyList<float> escortSlotErrors, float spacing)` → true while any escort is more than one
     spacing out of its slot (the flagship then sails Half instead of Full while approaching).
   - `BreaksFormation(RaiderState flagshipState, float flagshipDistance, float breakDistance)`.
   - `EscortSide(int rank, int flagshipSide)` and `EscortDistance(int rank, int flagshipSide, float broadside, float
     spread)`: rank 1 takes the other side, rank 2 the same side at `spread ×` the distance.
   - `MoraleBreaks(bool flagshipGone, RaiderState flagshipState, bool jarlFallen, bool fleeOnFlagship, bool fleeOnJarl)`.

### Phase 2: the jarl, the flagship and the fleet

1. **The jarl creature**: add `DraugrJarl` to `WildMonsters` (read how each entry sets `Id`, `BaseCreature`,
   `TemplateCreature`, `SoundPrefix`, weapons, health, scale) with a vanilla humanoid base suited to a big melee
   biped (verify with `vanilla_prefab.py`; `Draugr_Elite` is the natural choice and keeps draugr sounds and faction).
   No natural spawn (no `SpawnSystem` entry, or one that is always disabled). Two attacks on the triggers from phase A
   (an overhead chop with high damage and stagger, a sweep with a wider arc). Faction the same as the crew so they do
   not fight each other. Read `WildMonsterRegistry` fully first; keep its patterns (corpse, `AddCreatureSounds`,
   trophy via `MonsterRegistry.AddItem` on a vanilla trophy recolour, settings section). If phase A did not recolour
   the texture, recolour the jarl's material copy here (headless guard).
2. **The flagship prefab** in `NailShipRegistry`: `WhiteHilt_NailFlagship` cloned from the Nail Ship after it is made
   (same event; keep the order), with `FlagshipHullHealth`, a blood-red sail (recolour only the sail renderer's
   material copy; find how `NailShipLook` reaches the sail), and the same components. Make every place that tests for
   "a Nail Ship" by prefab name accept both: add `NailShipRegistry.IsNailShip(int prefabHash)` / `(string name)` and use
   it in `SeaBattleService.TargetShip`, `RaidService` (raid ships stay plain Nail Ships), and anything else found with a
   grep for `ShipName`. Components (`RaiderShip`, `NailShipWeapons`, ...) come with the clone.
3. **Spawning a fleet**: `SeaBattleService.RPC_Request` picks the size with `FleetRules.PickSize` (only when
   `FleetSettings.Enabled`) and sends it with the spawn order (add an `int size` parameter to the spawn RPC; it is a
   protocol change). Admin: `whitehilt_seabattle fleet [2|3]` through a new routed RPC (`WhiteHiltSeaBattleFleet`,
   admin-checked like `summon`), falling back to the first fleet tier's crew. `RaiderShip.SpawnFleet(target,
   crewBonus, tier, size)`: the flagship (or a plain Nail Ship when size 1) at the spawn point as now, escorts at
   `FleetRules.SlotPoint` around it in deep water (`SeaBattleService.DeepWater`; skip an escort with none), each with
   the fleet and rank keys. The sighted message for a fleet is `msg_whitehilt_fleet_sighted`.
4. **Crew**: the flagship's crew is the tier's crew plus `FlagshipExtraCrew`, every member at least
   `FlagshipCrewLevel` (`Character.SetLevel` after spawning, outside the spawn context, as `LindormService` does), and
   the jarl on the post nearest the mast (or the aft deck), level `JarlLevel`, marked `whitehilt_raider_jarl`. He holds
   his post like the crew (`RaiderCrew`), and is a melee crew member for boarding (give him the last leap turn so his
   men go first).
5. **Formation and tactics** in `RaiderShip.Sail` (owner tick): an escort whose flagship is loaded and has not broken
   formation (`FleetRules.BreaksFormation`) steers `FleetRules.EscortSail` toward its slot instead of
   `RaiderTactics`; after the break it uses `RaiderTactics` with `FleetRules.EscortSide`/`EscortDistance` in its tuning.
   The flagship sails Half while `FleetRules.FlagshipWaits` (it reads its loaded escorts through the static registry by
   fleet id). An escort whose flagship is unloaded or gone acts alone.
6. **Morale**: the flagship's owner sets `whitehilt_raider_jarlfallen` when the marked jarl is dead (count it with the
   crew; a jarl that was spawned and is no longer alive), messages `msg_whitehilt_jarl_falls`; escorts read the
   flagship's ZDO (`ZDOMan.instance.GetZDO(fleetId)`, works when unloaded) every tick and switch to Flee with
   `msg_whitehilt_fleet_breaks` when `FleetRules.MoraleBreaks`. A grappled escort still never flees (existing rule).
7. **Loot and saga**: the flagship's hold gets `FlagshipLootRolls` extra draws and, with `FlagshipRaidChart`, a Raid
   Chart for sure. When the flagship becomes a prize or its hull is destroyed, its owner sets
   `whitehilt_raider_broken` once and announces `SagaKind.Fleet` (in addition to the ship's own deed).
8. **Weapons**: the flagship always carries its ballista when `[SeaBattles.Weapons] NailBallista` is on, regardless
   of `NailBallistaKey` (`NailShipWeapons`).
9. Console status (`whitehilt_seabattle status`) lists fleet id and rank per loaded Nail Ship.

### Phase 3: documentation

1. `docs/sea-battles.md`: a *Fleets* section (when they come, the wedge, the flagship, the jarl, morale, the saga
   deed), the jarl picture if phase A made one, a `[SeaBattles.Fleets]` settings table, the jarl's settings section,
   `fleet [2|3]` in the console table. Every number checked against the code.
2. `docs/monsters.md`: a short entry for the Draugr Jarl (only on Nail flagships, link to the sea battles page) if
   the wild monsters are listed there.
3. `docs/saga.md`: the fleet deed and `RenownFleet`.
4. `docs/ideas.md`: remove the *Fleets* bullet under *Bigger enemies*.
5. `README.MD`: the Sea battles card line if needed; `## Credits` (phase A). Do not touch "New in …".
6. `CHANGELOG.md` under `## Unreleased` → `### Added` (fleets, flagship, jarl, morale, saga deed, console command), and
   a line under `### Changed` that the server and every player need the new version.

## 7. Multiplayer and performance notes

- The server decides the fleet size; the client that owns the hunted ship spawns every ship of the fleet, so the
  fleet is complete and placed in one go.
- Each ship's owner runs its own AI; escorts only read the flagship's ZDO. Formation never writes another ship's ZDO.
- Jarl death and fleet break are written by the flagship's owner only.
- The fleet lookups go through the existing static registry of loaded Nail Ships; nothing scans the scene per frame.

## 8. Asset notes (from the first look, 2026-10-13)

- `glb_info`: Object_7 (2 406 tris), Object_9 (4 196), Object_11 (1 884), Object_13 (23 762, the body), Object_15
  (5 180, broken), Object_17 (368, broken), Object_19 (1 280), Object_21 (756); all on `Material_15`; three 4096 px
  PNGs (13.6, 4.0 and 11.8 MB: base colour, normal and probably metal/roughness); skin of 72 joints.
- `preview_animations` with `--drop Object_15,Object_17,Object_9 --focus 0,0,110 --size 260` shows a bald, bearded
  viking with a fur cloak over one shoulder, bare arms with bracers, standing upright in idle and stepping in walk. The
  side view looked as if lying, so check the facing in Unity, not from that view.

## 9. Checks after every phase

```powershell
$msbuild = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" | Select-Object -First 1
& $msbuild BrudvikWhiteHilt\BrudvikWhiteHilt.csproj "/t:BeforeBuild;CoreBuild" "/p:SolutionDir=$PWD\" /p:GenerateDocumentationFile=true /v:minimal
dotnet test BrudvikWhiteHilt.Tests
.venv\Scripts\python.exe AssetSource\Tools\cfg_keys.py --check
```

No new warnings, all tests pass, 0 missing config labels. Phase A: the bundle builds and the creature render looks
right. Run terminal commands one at a time.

## 10. Reference

- Creature pipeline: `AssetSource/Tools/export_creature.py` (Blender), `BrudvikWhiteHiltUnity/Assets/Editor/BuildCreatures.cs`
  (Unity), `AssetSource/Preview/render_creatures.ps1`, examples in `AssetSource/Creatures/*.creature.json`.
- Wild monsters: `Monsters/WildMonsters.cs`, `WildMonsterRegistry.cs`, `WildMonsterSettings.cs`, `WildMonster.cs`.
- Sea battles: `SeaBattles/RaiderShip.cs` (spawn, crew, AI tick, prize), `RaiderTactics.cs`, `RaiderCrew.cs`,
  `NailShipRegistry.cs`, `NailShipLook.cs`, `SeaBattleService.cs`, `SeaBattleCommands.cs`, `Weapons/NailShipWeapons.cs`.
- Levels: level = stars + 1; `Character.SetLevel` after spawn, see `Monsters/LindormService.cs`.

## 11. Log

### Phase A: the jarl's model and animations (2026-10-13)

**Done.** `AssetSource/Creatures/draugrjarl.glb` (5.7 MB, textures 1024 px), `AssetSource/Creatures/draugrjarl.creature.json`,
`whitehilt_foraging` rebuilt with `build_foraging_bundle.ps1` (64.7 → 66.9 MB), `docs/images/draugrjarl.png` (copy of the
Unity 3/4 render, like the other monsters), the model in `README.MD` `## Credits`. The decor bundle changed only in bytes
and was restored with `git checkout`. No C# touched.

**What the model really is** (section 8 was close but not right):
- `Object_15` is the lower legs and boots ("Feet", weights LLeg/RLeg/foot/toe), `Object_17` is the axe (368 tris, one
  bone `Axe Rig_059`), `Object_9` is the belt/skirt and the bag (weights Pelvis, Bag, thighs, one collar strap), not the axe.
- 15 and 17 were not broken skinning but vertices stored point-reflected (negative scale) and 900 times too large for the
  shared skin. A throwaway script multiplied their POSITIONs by -1/900, negated normals, kept winding (the reflection
  already turns it outward; checked numerically) and the committed glb has that fix baked in, so no pipeline step is needed.
  Boots and axe now land exactly (feet y 0..47 cm, axe on the Axe Rig bone) and the glb is correct in any viewer.
- `Object_9` was not fixable with a uniform scale or reflection (its axes are permuted and non-uniformly scaled); it is
  **dropped** in the creature.json (`drop`), so he has no belt or skirt. It is still in the glb (about 100 KB) so the
  fix can be tried again.

**Decisions**
- Axe: **kept, and held in the right hand in every clip.** In the source the axe bone is rigid to `RPalm_033` (verified: the
  relation is constant over the whole walk), the walk carries it on the shoulder. The idle's own axe pose is 80 cm from his
  hand, so the source idle is dropped and every clip holds the axe in the walk's grip. One-handed (the left hand is free
  and swings against it); no IK.
- Decimation: **0.24** (`decimate`), 9 124 triangles in total in the bundle (body 5 702 faces, boots 1 242, cloak 452 +
  307 + 577, eyeballs 180, glowing eyes 576 are not decimated). One joined mesh, 6 535 vertices, 72 bones.
- Undead look: **done in the pipeline** (`albedo_recolour` in the json: saturation 0.4, tint 0.78/0.96/0.86, value 0.68 on
  the albedo; alpha kept for the fur). Phase 2 needs no recolour code. If it is too dark or green in game, change those
  three numbers and rebuild.
- Eyes: two glowing spheres, radius 1.0, cold blue-green `[0.25, 0.95, 0.8]`, emission 4, skinned to `Head_026`
  (at x ±4.0, y -6.7, z 159.3 cm in the Blender pose). Material `eye` in the prefab (emission colour 1.0/3.8/3.2).

**Generic pipeline changes (`AssetSource/Tools/export_creature.py`, all optional keys, documented in its docstring)**
- `generate[].base.borrow`: copies the local pose of bones (by prefix) from another take and frame before the keys apply.
  Used to put the right arm of the walk's shoulder carry under the standing idle body.
- `generate[].hold` `{bone, to, from:{take,frame}, shift, turn}`: keys a prop bone on every frame so it stays fixed to a
  hand bone in the relation it has in a take. Used for the axe. Not supported with `follow`.
- `albedo_recolour` `{saturation, tint, value}` on every albedo.
- `keep_clips` now removes the unwanted actions before renaming, so a generated take can be renamed to a name an unused
  original take still has (`carry_idle` → `idle`). No change for the other creatures.
- Regression: Blender exports of giantspider, yggdrasilwarden, kraken and nidhoggspawn with the old (HEAD) and new
  exporter give the same actions, frame ranges and pose signatures; the Unity render of yggdrasilwarden is byte-identical
  to `docs/images/yggdrasilwarden.png`. (`docs/images/giantspider.png` and `kraken.png` were already out of date before.)

**Facts for the code phase**
- Prefab: **`draugrjarl_visual`** (`CreatureVisual.Attach(parent, "draugrjarl", template, pos, rot, scale)`). Root is
  empty, the model with the Animator is its first child; one `SkinnedMeshRenderer`, two material slots:
  `draugrjarl_material15` (albedo + normal, **cutout** for the fur cards, `_ALPHATEST_ON`: use a template material with
  `_Cutoff`, the Warden's template) and `draugrjarl_eye` (emission). Textures `draugrjarl_material15_albedo/_normal`.
- Size: prefab units are centimetres: **182.3 units tall** (hair knot included; idle bounds 119 × 183 × 108 with the axe).
  Scale for about 2.3 m in game: **0.0126** (2.2 m = 0.0121, 2.4 m = 0.0132). The model's origin is at the feet,
  centred on the pelvis, facing +z (checked in the Unity render, front view faces the camera).
- Animator parameters as for every creature (`forward_speed` etc.). Movement blend tree on `forward_speed`: idle at 0,
  walk at **1.0**, run (= the same walk clip) at **2.5**. At 2.5 m/s the feet will slide; set `Animator.speed` or keep
  the creature's run speed near 1.5 if that looks bad. The foot speed in the walk is 0.77 m/s at model size, 0.97 m/s at
  scale 0.0126, hence 1.0.
- States: `Movement`, `stagger` (clip `hit`, 0.58 s, via the `stagger` trigger), attack states named like their triggers
  and tagged `attack`: **`jarl_swing`** (clip `swing`, overhead chop, 1.08 s, `OnAttackTrigger` at normalised **0.5** =
  0.54 s, the axe is at its lowest in front) and **`jarl_sweep`** (clip `sweep`, horizontal slash from his right across the
  front to his left, 1.17 s, event at normalised **0.42** = 0.49 s, axe across the front). Both have `hasExitTime` back to
  Movement. `die` (1.25 s, no transition, play with `Animator.Play("die")`; he falls on his back with the axe in his hand
  and ends lying on the ground, head toward -z, body about 1.8 m long, back sunk a few cm). `idle` 2.0 s loop, `walk`
  1.875 s loop.
- Bones (Blender/Unity names, spaces kept): head `Head_026`, right hand/axe hand `RPalm_033`, left `LPalm_09`, axe
  `Axe Rig_059` (child of `Axe Dummy_058`), spine `SpineLower_03`/`SpineUpper_04`, pelvis `Pelvis_02`, root `_rootJoint`. A
  weapon effect or trail would go on `Axe Rig_059` (the blade end is about 100 cm along its length at model size).
- `always_animate` is on, so hit events fire when he is off screen.
- The creature is bigger than the hit boxes of a normal humanoid: 2.3 m tall with a 2.4 m long axe reach in the
  animation; give the weapon the damage and range, the animation only has to look right.

**Verification.** Blender renders of every clip (front, side, from above for the sweep) while tuning; the whole bundle
built (every creature "binds 730 of 730 curves" for the jarl; the others unchanged); `render_creatures.ps1 -Creature
draugrjarl` viewed: upright, faces the camera, the swing reads as wind-up behind the head, chop to low in front, recover;
the sweep goes out to his right and across the front; the hit leans back; the death lands on the ground. The eyes are
placed on the eye sockets (checked in a Blender close-up); their glow is not visible in the Standard-shader preview.

**Deviations / risks**
- Two small generic features added to the exporter (`borrow`, `hold`, `albedo_recolour`, the `keep_clips` order) beyond
  the three files named; logged above and regression-checked.
- No belt/skirt, and the left hand does not grip the axe. The look in the game's lighting and creature shader is not
  seen yet; the albedo may need another round of the three recolour numbers.
- The run uses the walk clip (no faster clip), so at run speed the feet slide a little.
- The jarl's die pose keeps the axe glued to the hand and pointing slanted up from the ground.
- Triangles 9 124 (limit about 10 000); bundle +2.2 MB.

### Phase 1: settings, texts and pure logic (2026-10-13)

**Done.** New: `SeaBattles/Fleets/FleetSettings.cs` (section `SeaBattles.Fleets`, the 13 keys of section 4, `Initialize()`
called in `Awake` right after `WeaponSettings.Initialize()`), `SeaBattles/Fleets/FleetRules.cs` (pure: `FleetTier`,
`ParseTiers`, `PickSize`, `SlotPoint`, `EscortSail`, `FlagshipWaits`, `BreaksFormation`, `EscortSide`, `EscortDistance`,
`MoraleBreaks`), `BrudvikWhiteHilt.Tests/FleetTests.cs` (64 tests). Changed: `Saga/SagaSettings.cs` (`RenownFleet` 40,
`whitehilt_saga_fleet`, `WhiteHilt_DraugrJarl` added to the default `Creatures` list), `Saga/SagaLog.cs` (`SagaKind.Fleet`
and its `RPC_Deed` case), `BrudvikWhiteHilt.cs`, `Translations/Norwegian.json`. Nothing else in the game code is touched;
no ZDO keys, prefabs, patches or RPCs yet.

**Where the texts are registered**
- `FleetSettings.AddTranslations`: `whitehilt_nailflagship`, `msg_whitehilt_fleet_sighted`, `msg_whitehilt_jarl_falls`,
  `msg_whitehilt_fleet_breaks`.
- `SagaSettings.Initialize`: `whitehilt_saga_fleet` ("A Nail fleet was broken"), next to the other saga lines (deviation
  from "one place"; the saga page lists them there).
- **`enemy_whitehilt_draugrjarl` is not registered in code.** `WildMonsterRegistry.AddTexts` registers every creature
  name itself as `Translations.AddEnglish(monster.NameKey, monster.EnglishName)` with `NameKey = enemy_whitehilt_{Id}`, so
  phase 2 gets the English name from the `WildMonster` entry (`Id = "draugrjarl"`, `EnglishName = "Draugr Jarl"`). The
  Norwegian text `enemy_whitehilt_draugrjarl` ("Draugrjarl") is already in `Norwegian.json`, next to the other wild
  monsters. The trophy texts are phase 2's (`AddItemText`, with Norwegian).
- Norwegian: all four fleet texts, the saga line, `whitehilt_cfgsec_seabattles_fleets`, a label and description for every
  key (`whitehilt_cfg_seabattles_fleets_<key>` and `_desc`) and `whitehilt_cfg_saga_renownfleet`. `cfg_keys.py --check`:
  0 missing.

**Rules in `FleetRules` that the plan left open** (all in the XML docs and tests)
- `ParseTiers` format `key=size:chance%`; the chance may be left out (100) and the `%` is optional; a part with a
  non-numeric size or chance, a missing key or extra fields is skipped; size is clamped to 1–3, chance to 0–100.
  `PickSize(tiers, hasKey, roll)` takes the roll as 0 up to (not including) 1 and succeeds when `roll * 100 < chance`; the
  last tier whose key is set decides, and when it fails its roll the result is 1, an earlier tier is not tried.
- Headings are degrees from north, positions (x, z), as in `RaiderTactics`; forward is (sin h, cos h), starboard (cos h,
  -sin h). `SlotPoint`: rank 1 back and to port, rank 2 (and higher) back and to starboard, 0.7 × spacing each way; rank 0
  is the flagship's own position.
- `EscortSail` returns a `RaiderOrder` with state `Approach`: within 10 m of the slot the flagship's heading at Half; ahead
  of the slot along the heading Stop (heading kept); otherwise the course to the slot, Full when farther than `spacing`,
  else Half.
- `BreaksFormation`: true for every flagship state except Approach, or when `flagshipDistance <= breakDistance`.
- `EscortSide(rank, flagshipSide)`: rank 1 takes the other side, any other rank the flagship's (side 0 counts as
  starboard). `EscortDistance` returns `broadside × max(1, spread)` for rank 1+ on the flagship's side, else `broadside`.
- `MoraleBreaks`: the flagship counts as fallen when it is gone or in state Prize (not when it flees); the grapple rule stays
  in `RaiderTactics`.

**Checks.** Build: no new warnings (the CS0419/CS1591 ones in `NearbyContainers`, `RecipeCategories` and
`DefenseModelBuilder` were there before). `dotnet test BrudvikWhiteHilt.Tests`: 700 passed, 0 failed (64 of them the
fleet tests). `cfg_keys.py --check`: 0 missing.

**What phase 2 must know**
- Read the settings with `FleetRules.ParseTiers(FleetSettings.FleetTiers.Value)` (parse where it is used; nothing caches
  it). `FleetSettings.FlagshipHullHealth` applies at registration like `HullHealth`.
- The ZDO keys of section 5 (`whitehilt_raider_fleet`, `_rank`, `_jarl`, `_jarlfallen`, `_broken`) are not in `RaiderKeys`
  yet: add them there. `fleet` is a ZDOID (a `const string` stored under two hashes, like `Target` and `Ship`), the rest
  hashed ints and bools.
- The jarl: `WildMonsters` entry with `Id = "draugrjarl"`, `PrefabName = "WhiteHilt_DraugrJarl"` (the saga's default list
  and the plan already use it), `EnglishName = "Draugr Jarl"`; do not add an `AddEnglish` for the name. The saga's
  `Creatures` default only changes new config files; an existing file keeps its old list.
- `SagaLog.Announce(SagaKind.Fleet, "$whitehilt_nailflagship", position)` is the call for the broken-fleet deed; the line
  shows as "A Nail fleet was broken" (it takes no name argument).
- Norwegian for the trophy ("Draugrjarl-trofé" and its description) and for the jarl's own config section
  (follow how the other wild monsters have theirs) are still to add.

### Phase 2: the jarl, the flagship and the fleet (2026-10-13)

**Done** (items 1-9 of the phase; not seen in the game yet). Changed: `Monsters/WildMonster.cs`, `WildMonsters.cs`,
`WildMonsterRegistry.cs`, `WildMonsterSettings.cs`, `SeaBattles/RaiderKeys.cs`, `RaiderCrew.cs`, `RaiderShip.cs`,
`RaiderTactics.cs` (`RaiderTuning.WithBroadside`), `NailShipRegistry.cs`, `NailShipLook.cs`, `SeaBattleService.cs`,
`SeaBattleCommands.cs`, `Fleets/FleetRules.cs`, `Weapons/WeaponRules.cs`, `Weapons/NailShipWeapons.cs`,
`Mastery/MapPings.cs` (the lookout pin names a flagship by its own name), `Translations/Norwegian.json` (trophy),
`BrudvikWhiteHilt.Tests/FleetTests.cs` (57 new tests). No new settings keys of the fleet section (the 13 of phase 1 are
all used now); the jarl has the keys of `[Monsters.DraugrJarl]` below.

**The jarl** (`WildMonsters.DraugrJarl`)
- `Id draugrjarl`, `PrefabName WhiteHilt_DraugrJarl`, `Faction Undead` (the crew's), `ModelScale 0.0126` (about 2.3 m),
  capsule 0.5 m radius and 2.3 m tall, walk 1.4 and run 2.5 m/s (the model's blend points are 1.0 and 2.5).
- Cloned from the **Greyling** like the other wild monsters, not from `Draugr_Elite` (deviation from the plan text): the
  Greyling is the tested path (`PrepareClone` strips the base creature's own parts anyway). `TemplateCreature` is
  `Draugr_Elite`, which gives the material shader (`Custom/Creature`, has `_Cutoff` 0.591 and `_Cull` 0, so the fur
  cards work; `CreatureVisual.Convert` copies the model's cutoff) and, through `SetUpHumanoid`, the Draugr Elite's hit,
  death, alert and idle effects. `SoundPrefix` is null: no bundle sounds, and no `Sounds` setting. Death plays `die` via
  the generic `MonsterCorpse` (`Animator.Play("die")`); corpse sinks after 10 s and goes after 16 s.
- Health 600 per level (a level 3 jarl has 1800, as the game multiplies by the level). Attacks, both slash, damage is
  multiplied by the game's level factor (x2 at level 3): **Chop** (`jarl_swing`) 70, range 3.0, height 2.2, angle 70,
  every 3 s, force 160; **Sweep** (`jarl_sweep`) 45, range 3.2, height 1.6, angle 140, every 5 s, force 80. Modifiers:
  pierce resistant, fire weak, spirit weak, poison immune, chop and pickaxe ignored.
- `ConfigureJarl`: view 30, hear 30, alert 20, `m_enableHuntPlayer`, no low-health flee, no flee when the target cannot be
  reached (he holds the deck).
- **No natural spawns**: new `WildMonster.Spawns = false` gives no `SpawnConfig`, and `WildMonsterSettings` leaves out
  `Enabled`, `SpawnChance`, `SpawnSeconds` and `MaxSpawned`; `WildMonsterRegistry` only stores spawn data that exists.
  New `WildMonster.StarScale` (default 0.15, the jarl 0.08, so a level 3 jarl is 16% bigger, about 2.7 m).
- Trophy `WhiteHilt_TrophyDraugrJarl`: a recoloured `TrophyDraugrElite`, 100% chance, the only drop. Texts in
  `AddTexts`, Norwegian added. The name `enemy_whitehilt_draugrjarl` comes from the entry as before.
- Config section `[Monsters.DraugrJarl]` (all Norwegian labels already exist, shared with the other monsters): `Health`
  600, `Damage` 70 (chop), `SecondDamage` 45 (sweep), `Scale` 1, `StarScale` 0.08, `TrophyChance` 100.

**The flagship**: `NailShipRegistry.FlagshipName = WhiteHilt_NailFlagship`, made in `AddFlagship` (own try) with
`CreateClonedPrefab(name, GameObject)` from the finished Nail Ship, so it shares the hull textures and has the same
components. Differences: hull health `FlagshipHullHealth`, piece name `$whitehilt_nailflagship`, a blood-red sail
(`NailShipLook.ApplyFlagship` recolours the Nail Ship's already recoloured sail texture with hue 0.99, saturation 0.8,
value x1.9; headless: nothing). `NailShipRegistry.IsNailShip(int | string)` and `IsFlagship(GameObject)`; used in
`SeaBattleService.TargetShip`. Every other test goes through `RaiderShip.Is/Of` (both prefabs carry the component), and
`RaidService` keeps making plain Nail Ships. `RaiderShip.IsFlagship`, `NameToken`, `FleetId`, `Rank` are public.

**Fleet flow**
- Server: `RPC_Request` calls `PickFleetSize()` (`FleetRules.PickSize` over `ParseTiers(FleetTiers)`, 1 when
  `FleetSettings.Enabled` is off) and sends it in the spawn RPC. **Protocol changes**: `WhiteHiltSeaBattleSpawn` is
  now `(ZDOID, int tier, int bonus, int size)` and `WhiteHiltSeaBattleSighted` `(ZDOID, int size)`; new routed RPC
  `WhiteHiltSeaBattleFleet (ZDOID, int size)`, admin-checked through the shared `SendForAdmin` (size held to 2-3; a
  world with no crew key gets the first tier, as `summon`). `whitehilt_seabattle fleet [2|3]` (default 2) calls it.
- Client: `RaiderShip.SpawnFleet(target, bonus, tier, size)` (`Spawn` is `SpawnFleet(..., 1)`): the flagship (or a plain
  ship at size 1) at the spawn point, escorts at `FleetRules.SlotPoint` behind it, each only if `DeepWater` (6 m) there; the
  flagship stores `Fleet` = its own id and `Rank` 0, an escort the flagship's id and rank 1/2. If the flagship prefab is
  missing it falls back to one plain ship. The sighted text is `msg_whitehilt_fleet_sighted` when the lead is a flagship.
- Crew (`SpawnCrew`): the flagship gets `FleetRules.CrewBonus` (+`FlagshipExtraCrew`), every member `SetLevel` to at least
  `FlagshipCrewLevel` after the spawn (outside the spawn context), and the jarl last: on the melee post nearest the mast
  (`FleetRules.NearestPost`, the mast from `ship.m_mastObject`, else the middle), level `JarlLevel`, ZDO `Jarl` = true on
  him **and on the flagship** (the plan only had it on the creature), and the highest melee index, so he is the last to
  leap. A flagship without the jarl prefab sails without him (logged).
- Tactics (`Sail` + `FleetOrder`): an escort reads the flagship through `ZDOMan.GetZDO` (state, `JarlFallen`, `Broken`,
  `Side`). Morale (`FleetRules.MoraleBreaks`, flagship counted as fallen when `Broken`, a Prize, or its data missing for
  5 s, `FleetRules.FlagshipGone`) feeds the tactics as state Flee, so a grappled escort still never runs; the message is
  `msg_whitehilt_fleet_breaks` instead of `raider_flees`. After the formation breaks (`formationBroken`, a field, never
  undone) or when the flagship is not loaded, the escort uses `RaiderTactics` with `EscortSide` and the broadside
  distance from `EscortDistance` (`RaiderTuning.WithBroadside`). In formation (flagship loaded here, state Approach, order
  Approach) it sails `FleetRules.EscortSail` for its slot. The flagship sails Half (`FleetRules.FlagshipOrder`) while a
  loaded escort that still approaches is more than one spacing from its slot, and only while it is farther than
  `FormationBreak` from its target (the same rule that releases the escorts, so an escort another machine owns can not
  hold it back after the break).
- Jarl and loot: the flagship's owner sets `JarlFallen` when a flagship that carries a jarl has seen him alive and no living
  jarl is loaded (`FleetRules.JarlHasFallen`; a machine that never saw him can not tell), and says
  `msg_whitehilt_jarl_falls`. The hold gets `FlagshipLootRolls` more draws and a sure chart with `FlagshipRaidChart`
  (`FleetRules.HoldRolls`, `ChartInHold`). `BreakFleet` sets `Broken` once and announces `SagaKind.Fleet`
  (`$whitehilt_nailflagship`) when the flagship becomes a prize or its hull is destroyed, besides the ship's own deed.
- Weapons: `WeaponRules.BallistaOn(enabled, flagship, keyReached)`: a flagship always carries the ballista when
  `NailBallista` is on.
- Status: each loaded ship line shows `alone`, or `fleet <id>, flagship` / `escort <rank>`.

**Facts checked**: `vanilla_prefab Draugr_Elite --values --materials` (faction 3 = Undead, health 200, drops
`TrophyDraugrElite`, `Draugr_elite_mat` `Custom/Creature` with `_Cutoff`/`_Cull`); Jotunn 2.30.2 xml (`GetPrefab` finds
custom prefabs first, `CreateClonedPrefab(string, GameObject)` exists); `%TEMP%\wh_tame\full\MonsterAI.cs` (`m_enableHuntPlayer`,
`m_fleeIfLowHealth`, `m_fleeIfHurtWhenTargetCantBeReached`); `MonsterCorpse` plays `die`.

**Checks.** Build (`/p:GenerateDocumentationFile=true`): no new warnings (only the old CS0419/CS1591 in `NearbyContainers`,
`RecipeCategories`, `DefenseModelBuilder`). `dotnet test BrudvikWhiteHilt.Tests`: 757 passed, 0 failed (57 new: the ship
rules, `NailShipRegistry.IsNailShip`, the raider key hashes, the jarl's data). `cfg_keys.py --check`: 0 missing.
A test can not read the jarl's `Faction` (the enum lives in `Character`, which .NET Framework can not load).

**Deviations / risks**
- Greyling base instead of `Draugr_Elite` (see above); `Spawns`/`StarScale` added to `WildMonster`; the `Jarl` key also on
  the flagship; the spawn and sighted RPCs changed signature (server and clients need the same version).
- Escorts in formation need the flagship **loaded on their owner's machine**; otherwise they fight alone. The morale
  read works unloaded but needs the flagship's data on that machine, hence the 5 s grace before "gone".
- Both escorts say the fleet-break message, so a player may see it twice (the second replaces the first).
- An escort with no deep water at its slot is left out, so a fleet can be one ship smaller than the server chose.
- Level 3 jarl: 1800 health and x2 damage (140 chop) is a guess at the right strength for the Moder tier; tune with
  `[Monsters.DraugrJarl]` `Health`/`Damage`/`SecondDamage` and `[SeaBattles.Fleets] JarlLevel`.
- Untested in the game: the red sail, the jarl's material (fur cutout, eyes), his fit on the deck (2.3 m, collider), the
  attack reach, the sound of the Draugr Elite effects on a different animation, formation sailing, morale.

**In-game checklist**
1. Start the game with the mod: no errors for `Nail Flagship`, `Draugr Jarl`, `Draugr Jarl Trophy` in the log;
   `[Monsters.DraugrJarl]` has no spawn keys; `[SeaBattles.Fleets]` shows the 13 keys.
2. `devcommands`, then at sea on a ship: `whitehilt_seabattle fleet` (2 ships) and `fleet 3`: the horn, the message
   "A fleet on the horizon!", a flagship with a red sail and two plain Nail Ships behind it in a wedge (30 m apart);
   `status` shows `fleet`, `flagship` and `escort 1/2`.
3. The flagship's crew: more members than a plain ship, stars 1+ (level 2), a jarl at the mast (about 2.3 m tall, fur
   cloak, glowing eyes, undead colour), with the ballista on the bow even with `NailBallistaKey` unset; walk the jarl through
   idle, walk, chop, sweep, hit and death (`spawn WhiteHilt_DraugrJarl` for a close look, also his trophy drop).
4. The escorts hold the wedge while the flagship approaches (it slows when they lag), break at 80 m and lie alongside on
   the other side (rank 1) and farther out (rank 2).
5. Board: the jarl leaps last; kill the jarl: "The jarl has fallen!", the escorts turn and run with "The fleet breaks and runs!".
6. Take the flagship (kill the crew): the saga shows "A Nail fleet was broken" once, plus the ship's own deed; the hold has
   more loot and a Raid Chart; the escorts run. Sink a flagship with fire: the same deed once.
7. Two players: the owner of the flagship and of an escort differ; the escorts still follow, run on morale, and do not flee
   while a grapnel binds them. A dedicated server: the fleet raises and fights, with no errors about visuals.
8. `FleetTiers` empty or `Enabled` off: only lone ships come; `fleet` replies that fleets are off.

### Phase 3: documentation (2026-10-13)

**Done.** Only documents changed; no code, no version numbers, `Package/README.md` and `Package/CHANGELOG.md` untouched.
- `docs/sea-battles.md`: a *Fleets* section (when fleets come, the wedge, the flagship, the draugr jarl with
  `images/draugrjarl.png`, morale), a `[SeaBattles.Fleets]` settings table, the fleet deed in *Saga deeds*, fleet bullets
  in *Multiplayer*, `fleet [2|3]` and the fleet part of `status` in the console table, and a line in the ballista
  paragraph that a flagship always carries one. The intro no longer says nothing needs a new model.
- `docs/monsters.md`: a *Draugr Jarl* entry (picture in the top row, a row in the wild beasts table, trophy in the loot
  table, the `spawn` line) and a `[Monsters.DraugrJarl]` table with only the keys he really has (no `Enabled`, spawn,
  `Sounds` or `RequiredKey`).
- `docs/saga.md`: the fleet deed, `RenownFleet`, the jarl in the listed creatures and the new `Creatures` default (with
  the note that an old config file keeps its old list).
- `docs/ideas.md`: the *Fleets* bullet removed. `README.MD`: the Sea battles card line. `docs/README.md`: the index line
  for the page (not asked for, same wording). `CHANGELOG.md`: a new `## Unreleased` with `### Added` and `### Changed`.
- `## Credits` already had the Northern Outsider line from phase A.

**Numbers checked against the code** (not the plan): the 13 `[SeaBattles.Fleets]` keys, defaults and ranges
(`FleetSettings.cs`); the wedge's 0.7 x spacing, 10 m reach, spacing "far" limit and the break rule (`FleetRules.cs`,
`RaiderShip.FleetOrder`); the escort side and 1.6 x distance (`EscortSide`, `EscortDistance`); the jarl's stats,
attacks, modifiers, 100% trophy and corpse times (`WildMonsters.DraugrJarl`); the settings he has (`WildMonsterSettings`);
the 5 s "flagship gone" grace; the 100 m message range; `fleet` clamped to 2-3 and defaulting to 2 (`SeaBattleCommands`,
`SeaBattleService.RPC_Fleet`); `RenownFleet` 40 and the new `Creatures` default (`SagaSettings`). Level 3 = two stars =
1800 health and x2 damage follows the default difficulty formulas in `docs/difficulty.md`, and the Lindorm table there.

**Things in the code that looked inconsistent (reported, not changed)**
- The plan's checklist item 8 says an empty `FleetTiers` makes `fleet` reply that fleets are off; the code only does so
  when `[SeaBattles.Fleets] Enabled` is off (an empty `FleetTiers` only affects natural encounters).
- `docs/saga.md` names the listed-creature deed *A great creature was slain*, but the game's text is `{0} was slain`
  (`whitehilt_saga_monster`), so the saga shows *Draugr Jarl was slain*.
- A flagship that flees because its own crew or hull is low does not break the morale (only a prize, a sunk or gone
  flagship, or a fallen jarl does): the escorts leave the wedge and fight on. Documented as it is.
- The flagship also carries its ballista before `NailBallistaKey` (Moder) when an admin sends `fleet` early, since the
  flagship ignores the key.
- The jarl is level 3 even in the Yagluth tier, where he leads Fulings and is still a draugr.
- **Review fix (after phase 3, user request):** `JarlLevel` (int 3) is replaced by `JarlLevels`
  (`defeated_dragon=3; defeated_goblinking=4; defeated_queen=5; defeated_fader=6`, last set key wins, first entry when none,
  held to `CreatureStars.MaxLevel`), read through `FleetRules.PickLevel` and tested in `JarlLevelTests`. Docs and changelog
  follow. Build exit 0, 766 tests passed, `cfg_keys.py --check` 0 missing.
