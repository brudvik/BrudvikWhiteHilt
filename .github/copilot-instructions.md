# Copilot Instructions for BrudvikWhiteHilt

This document provides guidelines for AI assistants (GitHub Copilot, etc.) working on the BrudvikWhiteHilt Valheim mod project.

## Project Overview

BrudvikWhiteHilt is a Valheim mod built with:
- **Language**: C# (.NET Framework)
- **Framework**: BepInEx plugin system
- **Dependencies**: Jotunn (Valheim modding library)
- **IDE**: Visual Studio / VS Code

## Code Style & Documentation

### XML Documentation Comments

All public and protected members MUST have XML documentation comments:

```csharp
/// <summary>
/// Brief description of the class/method/property.
/// </summary>
/// <param name="paramName">Description of the parameter.</param>
/// <returns>Description of the return value.</returns>
```

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

## Item Creation Pattern

All custom items follow a base class pattern:

```
Items/
├── Weapons/
│   ├── WhiteHiltWeaponBase.cs      # Base class
│   └── WhiteHiltSword/
│       └── WhiteHiltSword.cs       # Concrete implementation
```

### Required Properties for Items

Each item class must implement:
- `BaseName` - Internal item identifier
- `FullName` - Display name shown to players
- `Description` - Item tooltip description
- `CopyFrom` - Base game item to clone
- `Requirements` - Crafting recipe
- `Enabled` - Whether the item is active

## Game Balance Guidelines

### Material Tier Restrictions

**IMPORTANT**: All crafting requirements must use materials available up to and including the **Swamp biome**:

**Allowed Materials (Meadows → Swamp)**:
- Basic: Wood, Stone, Resin, Leather Scraps, Deer Hide
- Black Forest: Bronze, Copper, Tin, Core Wood, Fine Wood, Troll Hide
- Swamp: Iron, Chain, Withered Bone, Ancient Bark (ElderBark), Guck, Root, Surtling Core

**NOT Allowed (Mountains and beyond)**:
- Mountains: Silver, Obsidian, Freeze Gland, Wolf materials
- Plains: Black Metal, Linen Thread, Needle, Padded armor materials
- Mistlands: Black Marble, Eitr, Yggdrasil Wood, Carapace
- Ashlands: Flametal, Askvin materials

**Exception – Stone Pot food and foraging meads**: Recipes cooked in the Stone Pot (`Items/Food/`) and the meads brewed from White Hilt forageables (`Items/Meads/`) may use food ingredients from any biome up to and including the **Mountains** (e.g. Wolf Meat, Onion, and the White Hilt Mountains forageables). The ingredients gate them naturally, so these recipes stay at `ProgressionTier.Start` and must not get tier materials such as Bronze or Silver. Gift potions may use the White Hilt forageables up to the Mountains too (Skadi uses Crowberries and Roseroot). Gear, tools and pieces still follow the Swamp rule.

**Exception – gear biome upgrades and the White Hilt Cape**: White Hilt armor (uniforms included), weapons and shields upgrade past quality 4 with one level per later biome, paid with that biome's material (`[Gear.Armor]`, `[Gear.Weapons]` and `[Gear.Shields] Upgrade*`, see `Items/GearUpgrades.cs`; shields also take a Lindorm Scale per level, staffs stay at quality 4). The White Hilt Cape needs a Deathsquito trophy (Plains) and has `ProgressionTier.Plains`, since its feather fall comes from the Mistlands feather cape. The crafting recipes themselves still follow the Swamp rule.

### CopyFrom Item References

Use Swamp-tier or earlier base items:
- Weapons: `SwordIron`, `MaceIron`, `AtgeirIron`, `BowHuntsman`, `SpearElderbark`
- Shields: `ShieldBanded`, `ShieldIronTower`
- Armor: Iron-tier armor pieces

Exception: the White Hilt Crossbow copies `CrossbowArbalest`, the only vanilla crossbow, so it shoots bolts and reloads. Its recipe still follows the Swamp rule.

## Version Management

### Version Number Format

Use Semantic Versioning: `MAJOR.MINOR.PATCH.BUILD`
- Located in: `BrudvikWhiteHilt/Properties/AssemblyInfo.cs`

```csharp
[assembly: AssemblyVersion("X.Y.Z.0")]
[assembly: AssemblyFileVersion("X.Y.Z.0")]
```

### When to Increment Versions

- **MAJOR**: Breaking changes, major feature overhauls
- **MINOR**: New features, new items added
- **PATCH**: Bug fixes, balance adjustments
- **BUILD**: Always 0 (reserved)

### Automatic Version Update Process

When making changes:
1. Determine version increment type based on changes
2. Update `AssemblyVersion` and `AssemblyFileVersion` in `AssemblyInfo.cs`
3. Add an entry to `CHANGELOG.md`

## README.MD Maintenance

### Structure

The README contains these sections (maintain this order):
1. Overview
2. Installation
3. Features (only a title, a one-line description and a link per feature page in `docs/`)
4. Compilation
5. Changelog (only a link to `CHANGELOG.md`; never write entries in the README)
6. Credits
7. Known issues

The README is also the Thunderstore page, so links from it use absolute GitHub URLs (`https://github.com/brudvik/BrudvikWhiteHilt/blob/master/...`); relative links break there.

## Feature Pages (`docs/`)

Every feature group has its own page in `docs/`, e.g. `docs/dog.md`, `docs/ships.md`, `docs/equipment.md`. The page holds everything about it: tables, recipes, keys, config settings and console commands. Each page starts with `# <emoji> <Title>` and a link back to `../README.MD`; its sections use `##` and `###`.

- Changing a feature: update its page in `docs/`, and its one-line description in the README if the summary no longer fits.
- New feature group: create `docs/<name>.md` and add a `### <emoji> [Title](absolute URL)` entry with a one-line description to the README's Features list.
- A README feature entry may show one picture under its heading: `<img src="https://raw.githubusercontent.com/brudvik/BrudvikWhiteHilt/master/docs/images/<image>.png" alt="Name" height="120">`.
- Links between pages are relative (`[White Hilt gear](equipment.md)`).
- Pictures live in `docs/images/` and are rendered from the models with `python AssetSource\Preview\render_showcase.py` (list in `AssetSource/Preview/showcase.json`). New models get an entry there and an `<img ... height="140">` in the image row above their table.

## CHANGELOG.md Maintenance

The changelog lives in `CHANGELOG.md` in the repository root, and every change that bumps the version MUST get an entry there. `publish.ps1` ships it in the Thunderstore package next to the README.

Add entries at the TOP, right under the intro line, newest version first:

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

## File Organization

```
BrudvikWhiteHilt/
├── Items/
│   ├── Weapons/           # All weapon items
│   ├── Armors/            # All armor pieces
│   ├── Tools/             # Hammer, Axe, Pickaxe, etc.
│   ├── Potions/           # Mead bases and effects
│   ├── Ammunition/        # Arrows and Bolts
│   ├── Accessories/       # Belts, rings, etc.
│   └── Indestructible/    # Base indestructible classes
├── Helpers/               # Utility classes
├── Extensions/            # Extension methods
├── Patches/               # Harmony patches
├── Pieces/                # Buildable pieces
└── Properties/            # Assembly info
```

## Pre-Commit Checklist

Before committing changes:

1. ✅ All new classes have XML documentation
2. ✅ Crafting requirements use Swamp-tier or earlier materials
3. ✅ `Enabled` property is set appropriately
4. ✅ Version number incremented in `AssemblyInfo.cs`
5. ✅ Changelog updated in `CHANGELOG.md`
6. ✅ Feature pages in `docs/` updated (and the README summary if needed)
7. ✅ Code compiles without errors
8. ✅ Naming follows conventions

## Common Jotunn Patterns

### Adding Items

```csharp
ItemConfig itemConfig = new()
{
    Name = "Display Name",
    Description = "Item description",
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

- Test items in-game after changes
- Verify crafting recipes work at the correct station
- Check item stats and damage values
- Ensure indestructible property works
