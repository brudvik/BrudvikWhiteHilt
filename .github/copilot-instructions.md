# Copilot Instructions for BrudvikWhiteHilt

Guidelines for AI assistants (GitHub Copilot and others) working on the BrudvikWhiteHilt Valheim mod. The mod is open
source and read by modders who want to learn from it, so the code must stay readable and explain itself.

Read [docs/architecture.md](../docs/architecture.md) first: it describes how the source is put together (start-up,
the item and piece pattern, assets, multiplayer, Harmony, settings, texts, performance and tests). This file holds the
rules; the guide holds the reasons.

## Project Overview

- **Language**: C# 10 on .NET Framework 4.8
- **Framework**: BepInEx 5 plugin with Harmony patches
- **Library**: Jötunn (prefabs, items, pieces, config sync, GUI)
- **Tests**: xUnit in `BrudvikWhiteHilt.Tests`, for what runs without the game
- **Layout**: one folder per feature (`Items/`, `Pieces/`, `Navigation/`, `Chests/`...), patches in `Patches/` in
  folders that mirror the features; namespaces follow the folders

## Code Style & Documentation

`.editorconfig` holds the formatting: CRLF line endings, four spaces, lines up to 120 columns.

### Comments

- Every public and protected type and member has an XML doc comment (`<summary>`, and `<param>`/`<returns>` where
  they apply). The build with `-p:GenerateDocumentationFile=true` must give no CS1591/CS1573/CS1572/CS1574 warnings.
- Overrides use `/// <inheritdoc/>`; the base class explains the pattern once.
- Private methods of some length get a `//` comment above them saying **what they do and why**: which machine runs
  them (owner, server or every client), why work is spread over frames, why a vanilla prefab is cloned, what keeps a
  failure from breaking the mod. Do not restate the code; explain what the reader cannot see in it.
- Write plain English in short sentences.

### Class Structure

Follow this order in classes:
1. Constants
2. Private fields
3. Properties (abstract first, then virtual, then regular)
4. Constructor
5. Public methods
6. Protected methods
7. Private methods

### Naming Conventions

- **Classes**: PascalCase (e.g., `WhiteHiltSword`)
- **Methods**: PascalCase (e.g., `AddItem()`)
- **Properties**: PascalCase (e.g., `BaseName`)
- **Private fields**: camelCase (e.g., `instance`)
- **Constants**: PascalCase (e.g., `MaxDamage`)
- **ZDO keys and RPC names**: `"whitehilt_..."`, hashed with `GetStableHashCode()` once in a static field

## Item Creation Pattern

Adding an item or piece means adding a class: the plugin finds every class implementing `IWhiteHiltCustomItem` or
`IWhiteHiltCustomPiece` by reflection when it starts. A base class per kind does the work:

```
Items/
├── Weapons/
│   ├── WhiteHiltWeaponBase.cs      # Base class
│   └── WhiteHiltSword/
│       └── WhiteHiltSword.cs       # Concrete implementation
```

### Required Properties for Items

Each item class must implement:
- `BaseName` - Internal item identifier (the prefab name, also the translation key)
- `FullName` - English name, for the log and the config
- `Description` - Item tooltip description
- `CopyFrom` - Base game item to clone
- `Requirements` - Crafting recipe
- `Enabled` - Whether the item is active

### Looks

A method that gives a clone its own look (`TryApplyVisual`) returns at once when `VisualHelper.IsHeadless` (a
dedicated server draws nothing), catches every exception and keeps the vanilla look, so a missing model never removes
an item from players' inventories.

## Texts

- English is registered in code with `Translations.AddEnglish(key, text)`, next to the feature that uses it.
- Every English text needs a Norwegian one in `BrudvikWhiteHilt/Translations/Norwegian.json`; the test
  `EveryEnglishTextHasANorwegianOne` fails otherwise.
- Game objects refer to texts by token: `Translations.Token(key)`.

## Multiplayer

- Shared or saved state lives in the object's ZDO, not in fields.
- Only the owner of a ZDO simulates the object and writes to it; begin such code with an `IsOwner()` check.
- Server-only work (scanning the world) runs in a service on the `Game` object and sends results with `ZRoutedRpc`.
- The server checks everything a client sends before it uses it.
- Things that go on while nobody is near use the world clock (`ZNet.instance.GetTime()`), stored in the ZDO.

## Game Balance Guidelines

### The biome rule

**IMPORTANT**: Content belongs to the biome where it is found or first makes sense: a forageable to the biome it grows in, a dish, mead, potion, rune, dye or piece to the latest biome among its ingredients. A recipe may use materials from **its own biome and every earlier biome**, never from a later one, and its strength (food stats, effect, damage, `ProgressionTier`) fits that biome.

Biome order and their typical materials:
- Meadows: Wood, Stone, Flint, Resin, Leather Scraps, Deer Hide, Raspberries, Honey
- Black Forest: Bronze, Copper, Tin, Core Wood, Fine Wood, Troll Hide, Blueberries, Carrot, Thistle
- Swamp: Iron, Chain, Withered Bone, Ancient Bark (ElderBark), Guck, Root, Surtling Core, Turnip, Entrails, Bloodbag
- Mountains: Silver, Obsidian, Freeze Gland, Wolf Pelt and Meat, Onion, Crystal, Slate, Soapstone
- Plains: Black Metal, Linen Thread, Needle, Barley, Flax, Cloudberries, Lox Meat and Pelt, Tar
- Mistlands: Black Marble, Eitr, Yggdrasil Wood, Carapace, Sap
- Ashlands: Flametal, Askvin materials

Ocean materials (Octopus, Kraken Ink and Tentacle) count as Swamp, since the Kraken rises once Bonemass is slain.

- Things gated by their own ingredients (Stone Pot dishes in `Items/Food/`, meads in `Items/Meads/`, roofs in `Pieces/Roofs/`) stay at `ProgressionTier.Start`, or are not tied to tiers at all, and must not get extra tier materials such as Bronze or Silver.
- The White Hilt forageables of a biome (`Items/Foraging/`) count as that biome's materials.

**Exception – White Hilt gear**: the crafting recipes of White Hilt armor (uniforms included), weapons, shields and tools still use materials up to and including the **Swamp**, since the gear grows with the player instead. It upgrades past quality 4 with one level per later biome, paid with that biome's material (`[Gear.Armor]`, `[Gear.Weapons]` and `[Gear.Shields] Upgrade*`, see `Items/GearUpgrades.cs`; shields also take a Lindorm Scale per level, staffs stay at quality 4). The White Hilt Cape needs a Deathsquito trophy (Plains) and has `ProgressionTier.Plains`, since its feather fall comes from the Mistlands feather cape.

### CopyFrom Item References

Use Swamp-tier or earlier base items:
- Weapons: `SwordIron`, `MaceIron`, `AtgeirIron`, `BowHuntsman`, `SpearElderbark`
- Shields: `ShieldBanded`, `ShieldIronTower`
- Armor: Iron-tier armor pieces

Exception: the White Hilt Crossbow copies `CrossbowArbalest`, the only vanilla crossbow, so it shoots bolts and reloads. Its recipe still follows the Swamp rule.

## Version Management

The version is in three places, which must agree:

- `BrudvikWhiteHilt/BrudvikWhiteHilt.cs`: `PluginVersion = "X.Y.Z"`
- `BrudvikWhiteHilt/Package/manifest.json`: `"version_number": "X.Y.Z"`
- `BrudvikWhiteHilt/Properties/AssemblyInfo.cs` (Latin-1 encoded): `AssemblyVersion` and `AssemblyFileVersion` as
  `"X.Y.Z.0"`

### When to Increment Versions

- **MAJOR**: Breaking changes, major feature overhauls
- **MINOR**: New features, new items added
- **PATCH**: Bug fixes, balance adjustments

Changes go under `## Unreleased` in `CHANGELOG.md` as they are made; a release renames that heading to
`## vX.Y.Z - YYYY-MM-DD` and bumps the three version numbers.

## README.MD Maintenance

`README.MD` is the GitHub front page. Its sections, in this order:

1. Header (logo, title, tagline, badges, links, banner) and a short introduction, which also says the mod is written
   to be learned from
2. New in the latest version (short, with links to the feature pages)
3. Installation
4. Features: feature cards in tables, one per page in `docs/` (picture, title and a one-line description)
5. Documentation
6. For modders (the guide, the comments, the tests)
7. Known issues
8. Building from source (folded)
9. Credits (folded)

Links in `README.MD` are relative. The Thunderstore README (`BrudvikWhiteHilt/Package/README.md`) is generated from it
by `build_thunderstore_readme.ps1` on every Release build, which turns the cards into plain markdown and makes every
link absolute; never edit the package README by hand. The changelog is only linked from the README; entries go in
`CHANGELOG.md`.

## Feature Pages (`docs/`)

Every feature group has its own page in `docs/`, e.g. `docs/dog.md`, `docs/ships.md`, `docs/equipment.md`. The page holds everything about it: tables, recipes, keys, config settings and console commands. Each page starts with `# <emoji> <Title>` and a link back to `../README.MD`; its sections use `##` and `###`.

- Changing a feature: update its page in `docs/`, and its card in the README if the summary no longer fits.
- New feature group: create `docs/<name>.md`, list it in `docs/README.md`, and add a card to the README's Features.
- Links between pages are relative (`[White Hilt gear](equipment.md)`).
- Pictures live in `docs/images/` and are rendered from the models with `python AssetSource\Preview\render_showcase.py` (list in `AssetSource/Preview/showcase.json`). New models get an entry there and an `<img ... height="140">` in the image row above their table.
- `docs/architecture.md` is the guide for modders; update it when a pattern it describes changes.

## CHANGELOG.md Maintenance

The changelog lives in `CHANGELOG.md` in the repository root, and every change players notice MUST get an entry there. `publish.ps1` ships it in the Thunderstore package next to the README.

Entries go at the TOP, newest version first:

```markdown
## vX.Y.Z - YYYY-MM-DD

### Added
- New feature or item descriptions

### Changed
- Modifications to existing features

### Fixed
- Bug fixes and corrections

### Removed
- Removed features (if any)
```

Leave out empty subsections.

### Feature Tables

When adding new items, update the appropriate table in the feature's page in `docs/`:

```markdown
| **Item Name** | Description | Crafting Station | Requirements |
```

## Pre-Commit Checklist

Before committing changes:

1. ✅ All new public types and members have XML documentation, and longer private methods a what-and-why comment
2. ✅ Crafting requirements follow the biome rule (White Hilt gear recipes: Swamp or earlier)
3. ✅ `Enabled` property is set appropriately
4. ✅ Every new English text has a Norwegian one
5. ✅ Changelog updated in `CHANGELOG.md`
6. ✅ Feature pages in `docs/` updated (and the README card if needed)
7. ✅ The solution builds without warnings, and `dotnet test BrudvikWhiteHilt.Tests` passes
8. ✅ Naming follows conventions

## Common Jotunn Patterns

### Adding Items

```csharp
ItemConfig itemConfig = new()
{
    Name = Translations.Token(Translations.ItemKey(BaseName)),
    Description = Translations.Token($"{Translations.ItemKey(BaseName)}_description"),
    CraftingStation = CraftingStations.Forge,
    MinStationLevel = 2,
    Requirements = new RequirementConfig[]
    {
        new() { Item = "Iron", Amount = 10, Recover = false }
    }
};
```

### Crafting Stations

- `CraftingStations.Forge` - Forge
- `CraftingStations.Workbench` - Workbench
- `CraftingStations.Cauldron` - Cauldron
- `"piece_magetable"` - Galdr Table (Mistlands)

### Settings

- `WhiteHiltConfig.BindAdminOnly` for settings the server decides (synced to every client), `BindLocal` for settings
  each player sets (keys, HUD positions).
- Anything that caches a setting has an `ApplyConfig`, called from the plugin's `RefreshConfig`.

## Error Handling

Always wrap item registration in try-catch:

```csharp
try
{
    // Item creation code
    Jotunn.Logger.LogInfo($"{FullName} added!");
}
catch (Exception ex)
{
    Jotunn.Logger.LogError($"{FullName} failed to load!");
    Jotunn.Logger.LogError(ex);
}
```

## Testing Notes

- Logic that runs without the game gets an xUnit test in `BrudvikWhiteHilt.Tests` (they load the built mod, so build
  first). Where code needs the running game, move the logic into a small method that takes plain values.
- Test items in-game after changes
- Verify crafting recipes work at the correct station
- Check item stats and damage values
- Ensure indestructible property works
- Try multiplayer changes with two clients: owner and non-owner behave differently
