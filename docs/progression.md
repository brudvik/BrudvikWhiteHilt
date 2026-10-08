# ⚙️ Progression (config)

[← Back to the README](../README.MD)

## 🛠️ SETTINGS WINDOW

Open the inventory (Tab) and click **Settings** in the small White Hilt panel in the bottom left corner, or press **F7** anywhere (`Settings.Keys` → `OpenSettings`). The window shows every White Hilt setting, in English or Norwegian, grouped by feature, with a search field.

Both lists support mouse-wheel scrolling at the same speed as the portal list.

- **My settings**: your own choices, such as keys, labels, the clock and sound. Always editable.
- **Server and world**: the rules. Editable in a local game and by server admins (`adminlist.txt`). Other players see the values the server uses, read-only.
- Changes are collected until you press **Save**; **Close** or Esc throws them away. Changed settings are marked in orange, ↺ puts a setting back to its default, and hovering a setting shows what it does and its default.
- When an admin saves server settings, they are sent to the server, which saves them in its own config file and passes them on to everyone online at once. Most take effect right away; settings marked *needs restart* apply after a restart.
- The console command `whitehilt_config_missing` lists settings without a translation in the current language.

## ⚙️ PROGRESSION (CONFIG)

Configured in `BepInEx/config/com.jotunn.BrudvikWhiteHilt.cfg`, or in the settings window above. The rules below are admin-only and synced from the server; each feature page lists its own settings and says which ones are each player's own. Every value in the mod's config defaults to the mod's built-in behaviour.

| Setting | Values | Description |
|---------|--------|-------------|
| `[General] Mode` | `Linear` (default), `Full` | Linear: White Hilt gear unlocks biome by biome. Full: every recipe is available from the start. |
| `[General] ShowUnlockMessages` | `true`/`false` | Show a message listing the newly available items when a tier unlocks. |
| `[Tiers] <ItemId>` | `Default`, `Start`, `BlackForest`, `Swamp`, `Mountain`, `Plains`, `Mistlands`, `Ashlands`, `Never` | Per-item tier override (linear mode). `Never` disables the recipe in both modes. Also works for the Stone Pot and its food. |
| `[Content] <ItemId>` | `true` (default)/`false` | Off: the item or piece can no longer be crafted or built. Existing copies are kept. |
| `[Recipes] <ItemId>` | empty (default) or a list | Empty: the built-in recipe. Otherwise `Prefab:Amount` or `Prefab:Amount:AmountPerLevel`, comma separated, e.g. `Iron:10:5, FineWood:4`. Unknown prefabs are skipped with a warning. Pieces keep at most as many requirements as the build menu shows. |
| `[Foraging.<Name>] Spawn` | `true`/`false` | Let the plant grow in new zones, and once in old land (`[OldLand]`). |
| `[Foraging.<Name>] GroupsPerZoneMin` / `GroupsPerZoneMax` | 0–20 | Fewest and most groups in each zone (64 × 64 m) where the ground suits the plant; the generator keeps trying spots until they are placed. |
| `[Foraging.<Name>] ExtraDropChance` | 0–1 | Chance that picking the matching vanilla plant also gives the ingredient. 0 turns it off. |
| `[Foraging.Crowberries] CreatureDropChance` | 0–1 | Chance that a Wolf drops 1–2 Crowberries. |
| `[Food.<Name>] Health`, `Stamina`, `DurationMinutes`, `Regen` | numbers | Values of each Stone Pot dish. Changes also apply to food already in inventories. |
| `[OldLand] Enabled` | `true` (default)/`false` | When the server starts, land generated before a spider nest, slate outcrop or forageable came gets its share, once per world and kind, by the same rules as new land. |
| `[OldLand] BuildingDistance` | 0–200 (default 50) | Metres anything placed in old land keeps from anything built. |

`[Content]` and `[Recipes]` use the same `<ItemId>` keys as `[Tiers]`.

Foraging and food settings take effect without a restart.

**Linear** is how a new player starts: a new config file holds `Linear`, while a config file that already has a mode keeps it, so an update never changes a running world. In linear mode, a tier unlocks the first time you obtain its key material. Recipes in that tier also cost some of that material, unless they already require it.

| Tier | Unlocked by | Extra cost | Default items |
|------|-------------|------------|---------------|
| Start | – | – | Hammer, Axe, Pickaxe, Hoe, Cultivator, Root Dowser, Stone Pot, all Stone Pot food, Lingonberry Mead, Roseroot Mead, Cranberry Mead, Sweet Gale Ale, Crowberry Wine, Rampart Stairs, Feeding Trough, Tether Post, Grooming Comb, Dog House, Dog Bed, Dog Bowl, Dog Water Bowl, Dog's Grave, White Hilt banners, Wood beam 4m, Wood pole 4m, Compost Bin, Net Winch, Shore Net, Waste Well |
| Black Forest | Bronze | Bronze ×5 | Sword, Palisade Rampart, Rampart Corner, Rampart Bend, Gatehouse, all watchtowers, Cheval de Frise, Cartographer's Desk, Navigator's Table, Pathfinder's Amulet, White Hilt Portal, White Hilt Rune Circle, Surt's Brazier, White Hilt Banner Cape, Paint Bench, Paint Brush, Paint Pot, Repair Anvil · Ratatoskr, Tyr, Freyr, Idunn |
| Swamp | Iron | Iron ×5 | Ship, all ship upgrades, Chain Bench, Rune Forge, Rune Post, all runes, Home Stone, Valkyrie Stone, Portal Astrolabe, Harbour Anchor, Mooring Post, Munin's Perch, Pathfinder's Ruby Amulet, Stone Dowser, Bow, Crossbow, Knife, Mace, Atgeir, Spear, Battleaxe, Sledge, Buckler, Tower Shield, all armor and uniforms, Arrows, Bolts, Belt Pouch, the wood iron beams and poles · Fenrir, Skadi, Njord |
| Mountain | Silver | Silver ×5 | Staff of Fire, Staff of Ice, Megingjord · Freya, Odin, Thor |
| Plains | Black Metal | Black Metal ×5 | Staff of Lightning, White Hilt Cape · Sleipnir, Baldur, Hel |
| Mistlands | Eitr | Eitr ×3 | Necromancer's Staff · Loki |
| Ashlands | Flametal | Flametal ×3 | Surt |

The Gifts of **Hugin** (every skill to 100), **Munin** (every recipe) and **Brokkr** (+25 to every skill) are not available in linear mode: there you learn and find things yourself. A `[Tiers]` setting can still give them a tier.

The Herb Tray, the Smoke Oven and the dog's gear (whistle, collars, treats and the like) have no tier. The biome upgrades of White Hilt gear are gated by their own materials, see [Upgrades through the biomes](equipment.md#-upgrades-through-the-biomes).

Recipes are hidden, never removed, so switching mode never deletes items you already own.
