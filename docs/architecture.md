# 🧱 How the mod is built

A guide for modders who read the source to learn. The [feature pages](README.md) say what the mod does. This page
explains how the source is put together and why, so you know where to look and which patterns repeat. Each
section ends with files that are worth reading next. Every longer method in them has a comment saying what it does
and why.

Back to the [documentation](README.md) · [Main page](../README.MD)

## 🗺️ The source at a glance

| Folder | What lives there |
|--------|------------------|
| `BrudvikWhiteHilt.cs` | The plugin: start-up order, discovery of items and pieces, config refresh |
| `Items/`, `Pieces/` | One class per item or piece, grouped by theme (weapons, meads, ships, roofs, defences...) |
| `Patches/` | Every Harmony patch, in folders that mirror the features they serve |
| `Helpers/` | Shared tools: translations, the asset bundle, changing a cloned prefab's look |
| `Progression/`, `Settings/` | The config file, the in-game settings window, progression tiers and recipe overrides |
| `Chests/` | The restocking chests: a module of its own that started as a separate mod |
| `Navigation/`, `Difficulty/`, `Kraken/`, `Monsters/`, `Companions/`, `Treasure/`... | Larger features with their own services, components and settings |
| `Translations/Norwegian.json` | Every text in Norwegian; English is registered in code next to where it is used |
| `../AssetSource/` | Python and Unity scripts that build the asset bundle and the defence layout |
| `../BrudvikWhiteHilt.Tests/` | Tests for everything that runs without the game |

Namespaces follow the folders, so `Pieces/Ships/Skidbladnir/SkidbladnirShip.cs` is `BrudvikWhiteHilt.Pieces.Ships.Skidbladnir`.

## 🚀 Start-up

`BrudvikWhiteHilt.Awake` runs once when BepInEx loads the plugin. The game has no prefabs yet at that point, so it
only does what must come first:

1. **It binds every setting.** Each feature has a `*Settings.Initialize()` that binds its config entries. They must
   exist before Jotunn syncs the config from a server, so they are bound before anything else.
2. **It registers texts.** English texts are added in code (`Translations.AddEnglish`), and the Norwegian file is
   loaded from the embedded resources. Valheim reads its languages early, so this must happen in `Awake`.
3. **It discovers the items and pieces.** `DiscoverCustomEntries` uses reflection to find every class that implements
   `IWhiteHiltCustomItem` or `IWhiteHiltCustomPiece` (and every `ForageableBase`), creates one of each and keeps the
   enabled ones. Items and pieces are registered for progression here, so their config entries also exist before
   the sync.
4. **It subscribes to Jotunn's events.** The real work waits for them:
   - `PrefabManager.OnVanillaPrefabsAvailable` fires once, when the vanilla prefabs can be cloned. Items and pieces
     are added here. This has to happen before the main menu copies ObjectDB, or the character preview drops
     modded gear.
   - `PrefabManager.OnPrefabsRegistered` fires for every new `ZNetScene` (every world you join). It is used for
     changes to vanilla prefabs that must be made again each time, such as adding drops to vanilla creatures.
5. **It applies the Harmony patches** with `harmony.PatchAll()`, which finds every `[HarmonyPatch]` class in the
   assembly.

Larger features add their own `MonoBehaviour` services to the game object. A small patch on `Game.Start` adds them,
so they live exactly as long as a world session (see `Patches/Navigation/DiscoveryPatches.cs`).

> Read next: `BrudvikWhiteHilt.cs`, `Items/IWhiteHiltCustomItem.cs`, `Patches/Navigation/DiscoveryPatches.cs`.

## 🗡️ Adding an item means adding a class

Every White Hilt item and piece is one small class that describes it. A base class per kind of thing does the work,
so the knife is only this:

```csharp
public class WhiteHiltKnife : WhiteHiltWeaponBase
{
    public WhiteHiltKnife(ItemManager instance) : base(instance) { }

    protected override string BaseName => "WhiteHiltKnife";       // prefab name, also the translation key
    protected override string FullName => "White Hilt Knife";      // English name, for the log and config
    protected override string Description => "The Indestructible Knife of Dyrnwyn";
    protected override string CopyFrom => "KnifeChitin";           // the vanilla prefab it is cloned from
    protected override string ModelName => "whknife";              // its model in the asset bundle
    public override bool Enabled => true;
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Iron", Amount = 10, Recover = false },
        new() { Item = "LeatherScraps", Amount = 5, Recover = false },
        new() { Item = "KnifeFlint", Amount = 1, Recover = false }
    };
}
```

Nothing else needs to be registered. Reflection finds the class, and `WhiteHiltWeaponBase.Add` does the rest:

1. It clones the vanilla prefab with Jotunn (`new CustomItem(name, copyFrom, config)`), which gives the item the
   animations, sounds, attacks and network setup of an item the game already knows how to run.
2. It copies stats from another item if asked (`StatsFrom`).
3. It applies the config and swaps in the White Hilt model.
4. It hands the item to Jotunn, which adds it to ObjectDB and its recipe to the crafting station.

The base classes are `WhiteHiltWeaponBase`, `WhiteHiltArmorBase`, `WhiteHiltAccessoryBase`, `PotionBase`,
`WhiteHiltMeadBase`, the tool, ammunition and ship bases, and `ForageableBase`. Each of them explains its pattern in
its class comment, so the many subclasses only use `<inheritdoc/>`.

**Why clone rather than build from scratch?** A Valheim item or piece is a prefab with many components that must fit
together: `ItemDrop`, `ZNetView`, `WearNTear`, `Piece`, colliders, effects and animation hooks. A clone of something
close starts out working, so you only change what differs.

> Read next: `Items/Weapons/WhiteHiltWeaponBase.cs`, `Items/Foraging/ForageableBase.cs`, `Pieces/IWhiteHiltCustomPiece.cs`.

## 🎨 Changing how a clone looks

Almost every item and piece has a `TryApplyVisual` (or `ApplyLook`) method, and they all follow the same rules:

- **On a dedicated server, skip all of it.** `VisualHelper.IsHeadless` is true when there is no graphics device, so
  the server spends no time on meshes, textures and icons it never shows.
- **A failure never removes the thing.** The method catches every exception, logs a warning and keeps the vanilla
  look. A missing model must not take an item out of players' inventories.
- **Lighting and weather come from vanilla materials.** A model from the bundle gets a copy of a vanilla material
  (the wood pole, the workbench top...) with its own texture, so it is lit and weathered like everything else.
- **Icons are rendered from the finished prefab** (`VisualHelper.RenderIcon`). For items with variants, the icon
  is repeated once per variant, because `GetIcon` indexes the icon array by the saved variant.
- **A broken piece breaks into its own look.** `PieceFragments.Apply` makes the debris use the new model, not the
  pieces of the vanilla prefab it was cloned from.
- **Wear states point at the new look.** Pieces with `WearNTear` switch between new, worn and broken looks, so either
  all three get the new model (`RuneForge`, `PaintBench`) or all three point at the same object (`SlatePieces`).

> Read next: `Helpers/VisualHelper.cs`, `Pieces/Smithing/RuneForge/RuneForge.cs`, `Helpers/PieceFragments.cs`.

## 📦 Assets and data files

The 3D models, textures and sounds are in one asset bundle, `Assets/whitehilt_foraging`, which is embedded in the
DLL and loaded by `ForagingAssets`. The bundle is built outside the game: `AssetSource/build_foraging_bundle.ps1`
takes `.glb` models and `.wav` sounds and builds them with Unity, the same version as Valheim. The
[README](../README.MD) has the details under *Building from source*.

The defences, stonework and navigation pieces are **data, not code**. `AssetSource/Preview/build_defenses.py` lays
them out from Valheim's own meshes and writes `defenses.json`. The mod embeds that file, and `DefensePieceBase`
builds a game piece from each entry. `DefenseModelBuilder` combines the meshes per material, so a wall made of fifty
vanilla parts is drawn with a few draw calls. To add a shape, you change the Python script; the tests in
`LayoutTests` check that the file still matches what the code expects.

The Decor Hammer goes one step further. Each of its decorations is one line in `AssetSource/Decor/decor.json`, which
the mod embeds, and `DecorPieceFactory` builds a piece from it. A decoration with a vanilla look copies only the visible
parts of a vanilla prefab, so a decorative bush keeps Valheim's wind but cannot be chopped. One with a model takes it
from a second bundle, `whitehilt_decor`, which `prepare_decor.py` and Blender fill from Poly Haven and downloaded
models. That bundle is a file next to the DLL rather than embedded, so Unity reads it from disk instead of the mod
keeping a copy in memory.

> Read next: `Pieces/Defenses/DefenseLayout.cs`, `Pieces/Defenses/DefenseModelBuilder.cs`, `Helpers/ForagingAssets.cs`,
> `Decor/DecorPieceFactory.cs` with `AssetSource/Decor/README.md`.

## 🌐 Multiplayer

Valheim is peer-to-peer with a server in the middle. Most bugs in multiplayer mods come from forgetting who decides
what. The mod follows the game's own rules:

**The ZDO is the state.** Every networked object has a `ZDO`, a bag of values that the game syncs to everyone near
it. Anything that must survive a reload, or be the same for every player, goes in there under a key made with
`"whitehilt_..." .GetStableHashCode()`. Fields on a component are only a local copy or a cache.

**The owner decides.** Each ZDO has one owner, usually the client of the nearest player. Only the owner simulates
the object and writes its ZDO. That is why so many methods begin with `if (!nview.IsOwner()) return;`, and why their
comments say *"On the owner:"*. Every machine may read the ZDO to show the object, for example to animate a
portcullis, tip a cauldron or pose a dog.

**Two kinds of RPC.**
- `ZNetView.Register` and `InvokeRPC` talk to *one object*, usually its owner. For example, `ShipChartTable`
  asks the ship's owner to set up the chart table. When two players do it at once, the owner settles it, and the
  loser gets their table back.
- `ZRoutedRpc` talks to *machines*, usually the server. Server services such as `DiscoveryService`,
  `MapAreaService`, `TreasureService` and `DifficultyService` scan the world (only the server sees all of it), and
  then send each player what they may see.

**The server checks what clients send.** A client could be modified, so the server checks every request: the horn
call (`SummoningHornService.RPC_Call`), stock reports from chests (`WorldProgress.OnServerReceive`) and portraits
from other players (`PortraitNetwork.RPC_Data`, which compares the data with the hash that was announced).

**Ownership can be handed over.** A chest that two players change at once loses items unless one of them owns it
first. `ContainerHandoff` asks the owner to save the chest, send its newest data and pass ownership on, and the
change waits until that has arrived. Ships do the same at the helm (`ShipAssist.UpdateHelmOwnership`): the
helmsman owns the physics, so steering does not lag.

**Or the owner does the change.** Handing a chest over goes wrong when two players want the same chests at the same
moment: each hands its chests to the other. Crafting and building from chests therefore ask the owner to take the
items out and send them (`ChestWithdrawal`): the request goes to the chest's network object, which routes it to the
owner; the owner checks access, takes the items, saves the chest and replies to the asker with the items in a
`ZPackage`. The click waits for the replies and is then done again by itself.

**World time, not frame time.** Things that go on while nobody is near use the world clock
(`ZNet.instance.GetTime()`) and store the last tick in the ZDO. Examples are compost, fishing nets, a dog's hunger
and the Kraken's lifts. When the area loads again, the owner catches up on the time that passed, with a cap.

> Read next: `Chests/ContainerHandoff.cs`, `Crafting/ChestWithdrawal.cs`, `Navigation/Discoveries/DiscoveryService.cs`,
> `Pieces/Ships/ShipAssist.cs`, `Pieces/Fishing/NetWinchComponent.cs`,
> `Pieces/Ships/WhiteHiltShip/ShipPassengerSync.cs`.

## 🪝 Harmony patches

All patches live in `Patches/`, in folders named after the features they serve. They follow a few habits:

- **The patches stay small.** A patch finds out whether it concerns the mod and then calls into the feature's own
  code. The logic does not live in the patch.
- **The chests use events.** The patches in `Patches/Chests` raise C# events (`ContainerPatch.ContainerChangedPatched`
  and so on), and `ChestModule.Initialize` subscribes to them. This keeps the module free of Harmony and lets
  several handlers react to one game method.
- **A postfix is the default.** A prefix that returns `false` replaces the vanilla method, so it is only used where
  that is the point, for example `PeatFuelPatch` (peat in a wood fire). Even then it hands everything that is not
  its case back to vanilla by returning `true`.
- **Harmony's naming.** `__instance` is the patched object, `__result` the return value, and `__state` carries a
  value from the prefix to the postfix (`ForagingDropPatch` uses it to know that a pick really happened).
- **Every patch class carries `[HarmonyPatch]`**, even when each method names its own target. `PatchAll` skips a
  class without it and says nothing; `PatchTests` fails if one slips through.

> Read next: `Patches/Chests/ContainerPatch.cs` with `Chests/ChestModule.cs`, `Patches/Foraging/PeatFuelPatch.cs`,
> `Patches/Foraging/ForagingDropPatch.cs`.

## ⚙️ Settings

- `WhiteHiltConfig.BindAdminOnly` binds a setting the server decides. Jotunn syncs it to every client, and only an
  admin can change it. `BindLocal` binds one each player sets for themselves, such as keys and HUD positions.
- **Refresh on the next frame.** Saving the settings window fires one event per setting. The plugin only sets a
  flag, and `RefreshConfig` runs once on the next frame. Everything that caches a setting has an `ApplyConfig` that
  is called from there.
- **Renaming a setting does not lose the player's value.** `WhiteHiltConfig` moves values from old names to new ones
  (`MoveRenamedEntries`) and removes settings that no longer exist, so the config file does not fill up with dead
  entries.

> Read next: `Progression/WhiteHiltConfig.cs`, `Progression/ProgressionManager.cs`.

## 🌍 Texts

- English is registered in code, near the feature, with `Translations.AddEnglish(key, text)`, usually in the
  feature's `AddTranslations`.
- Norwegian is in `Translations/Norwegian.json`, embedded in the DLL.
- Game objects refer to a text by its token, `Translations.Token(key)` (`$key`), which Valheim looks up in the
  player's language.
- The test `EveryEnglishTextHasANorwegianOne` fails when a new English text has no Norwegian one, so a translation
  cannot be forgotten.
- Numbers in texts that follow a setting use `Translations.AddDynamic`, so the text changes when the setting does.

## ⏱️ Performance habits

Valheim runs everything on one thread, and so does the mod. These habits recur:

- **Work on a timer, not every frame.** Many `Update` methods start with `if (Time.time < nextCheck) return;`. Where
  many objects share the same check, they get a random first delay, so a large roof or a pack of dogs does not do
  all its work in the same frame (`RoofEave`, `DogCompanion`).
- **Spread large scans over frames.** The server services scan a few sectors of the world per frame
  (`ContinueScan`). The sea route finder and the old-land filler are coroutines that yield when their time budget
  for the frame runs out.
- **Cache and rebuild only on change.** Materials, textures and lookups by prefab are made once and kept. UI is
  rebuilt only when the data behind it has changed (for example `ShipRoute.Read` compares `DataRevision`).
- **Draw less.** Meshes are combined per material (`DefenseModelBuilder`), and a bone animation is skipped while its
  model is off screen (`DogExpression`).

## 🧪 Tests

`BrudvikWhiteHilt.Tests` holds xUnit tests for what runs without the game:

- reading and writing saved data, such as blueprints, pack lists and keys;
- what is sent over the network, such as map areas, the difficulty state and portraits;
- the pure logic, such as compass bearings, paint colours and treasure map geometry;
- checks of the data files against the code: every text translated, and every laid-out piece in `defenses.json`.

The tests load the built mod (and see its `internal` types through `InternalsVisibleTo`), so build the solution
first and then run `dotnet test BrudvikWhiteHilt.Tests`. Code that needs a running world (`ObjectDB`, `ZNetScene`,
`WorldGenerator`) cannot be tested this way. Where it is worth it, the logic is moved out into a small method that
takes plain values, so it can be tested.

## 🧭 Where to start reading

| If you want to learn... | Read |
|-------------------------|------|
| How a mod item is cloned and registered | `Items/Weapons/WhiteHiltKnife/WhiteHiltKnife.cs`, then `WhiteHiltWeaponBase.cs` |
| How a piece with its own model is made | `Pieces/Smithing/RuneForge/RuneForge.cs` |
| How state is shared in multiplayer | `Pieces/Farming/CompostBinComponent.cs` (small), then `Pieces/Fishing/NetWinchComponent.cs` |
| How a server service scans the world | `Navigation/Areas/MapAreaService.cs` |
| How ownership is handed over safely | `Chests/ContainerHandoff.cs` |
| How a custom creature is made | `Monsters/MonsterRegistry.cs`, then `Kraken/KrakenRegistry.cs` |
| How UI is built with Jotunn's GUIManager | `Navigation/Overview/OverviewPanel.cs` (small), then `Pieces/Portals/WhiteHiltPortal/PortalTravelPanel.cs` |
| How meshes are made in code | `Pieces/Roofs/RoofMeshBuilder.cs`, `Pieces/Fishing/FishingNetComponent.cs` (the rope) |
| How a ship is smoothed for passengers | `Pieces/Ships/WhiteHiltShip/ShipPassengerSync.cs` |
