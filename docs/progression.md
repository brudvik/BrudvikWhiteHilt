# ⚙️ Progression (config)

[← Back to the README](../README.MD)

## 🛠️ SETTINGS WINDOW

Open the inventory (Tab) and click the cog in the bottom left corner. The window shows every White Hilt setting, in English or Norwegian, grouped by feature, with a search field.

- **My settings**: your own choices, such as keys, labels, the clock and sound. Always editable.
- **Server and world**: the rules. Editable in a local game and by server admins (`adminlist.txt`). Other players see the values the server uses, read-only.
- Changes are collected until you press **Save**; **Close** or Esc throws them away. Changed settings are marked in orange, ↺ puts a setting back to its default, and hovering a setting shows what it does and its default.
- When an admin saves server settings, they are sent to the server, which saves them in its own config file and passes them on to everyone online at once. Most take effect right away; settings marked *needs restart* apply after a restart.
- The console command `whitehilt_config_missing` lists settings without a translation in the current language.

## ⚙️ PROGRESSION (CONFIG)

Configured in `BepInEx/config/com.jotunn.BrudvikWhiteHilt.cfg`. All settings are admin-only and synced from the server. Every value in the mod's config defaults to the mod's built-in behaviour.

| Setting | Values | Description |
|---------|--------|-------------|
| `[General] Mode` | `Full` (default), `Linear` | Full: every recipe is available as before. Linear: White Hilt gear unlocks biome by biome. |
| `[General] ShowUnlockMessages` | `true`/`false` | Show a message listing the newly available items when a tier unlocks. |
| `[Tiers] <ItemId>` | `Default`, `Start`, `BlackForest`, `Swamp`, `Mountain`, `Plains`, `Mistlands`, `Ashlands`, `Never` | Per-item tier override (linear mode). `Never` disables the recipe in both modes. Also works for the Stone Pot and its food. |
| `[Content] <ItemId>` | `true` (default)/`false` | Off: the item or piece can no longer be crafted or built. Existing copies are kept. |
| `[Recipes] <ItemId>` | empty (default) or a list | Empty: the built-in recipe. Otherwise `Prefab:Amount` or `Prefab:Amount:AmountPerLevel`, comma separated, e.g. `Iron:10:5, FineWood:4`. Unknown prefabs are skipped with a warning. Pieces keep at most as many requirements as the build menu shows. |
| `[Foraging.<Name>] Spawn` | `true`/`false` | Let the plant grow in zones generated from now on. Existing zones are not changed. |
| `[Foraging.<Name>] SpawnPerZone` | 0–20 | Maximum groups per zone (64 × 64 m). Values below 1 are a chance to place one group. |
| `[Foraging.<Name>] ExtraDropChance` | 0–1 | Chance that picking the matching vanilla plant also gives the ingredient. 0 turns it off. |
| `[Foraging.Crowberries] CreatureDropChance` | 0–1 | Chance that a Wolf drops 1–2 Crowberries. |
| `[Food.<Name>] Health`, `Stamina`, `DurationMinutes`, `Regen` | numbers | Values of each Stone Pot dish. Changes also apply to food already in inventories. |

`[Content]` and `[Recipes]` use the same `<ItemId>` keys as `[Tiers]`.

Foraging and food settings take effect without a restart.

In **Linear** mode, a tier unlocks the first time you obtain its key material. Recipes in that tier also cost some of that material, unless they already require it.

| Tier | Unlocked by | Extra cost | Default items |
|------|-------------|------------|---------------|
| Start | – | – | Hammer, Axe, Pickaxe, Hoe, Cultivator, Stone Pot, all Stone Pot food, Lingonberry Mead, Roseroot Mead, Cranberry Mead, Sweet Gale Ale, Crowberry Wine, Rampart Stairs, Feeding Trough, Tether Post, Grooming Comb, Dog House, Dog Bed, Dog Bowl, Dog's Grave |
| Black Forest | Bronze | Bronze ×5 | Sword, Palisade Rampart, Rampart Corner, Rampart Bend, Gatehouse, all watchtowers, Cheval de Frise, Cartographer's Desk, Navigator's Table, Pathfinder's Amulet, White Hilt Portal, White Hilt Rune Circle, Surt's Brazier · Ratatoskr, Tyr, Brokkr, Freyr, Idunn |
| Swamp | Iron | Iron ×5 | Ship, all ship upgrades, Chain Bench, Rune Forge, Rune Post, all runes, Home Stone, Bow, Crossbow, Knife, Mace, Atgeir, Spear, Battleaxe, Sledge, Buckler, Tower Shield, all armor, Arrows, Bolts, Belt Pouch · Fenrir, Skadi, Njord |
| Mountain | Silver | Silver ×5 | Staff of Fire, Staff of Ice, Megingjord · Freya, Odin, Thor |
| Plains | Black Metal | Black Metal ×5 | Staff of Lightning · Sleipnir, Hugin, Baldur, Hel |
| Mistlands | Eitr | Eitr ×3 | Loki, Munin, Mimir |
| Ashlands | Flametal | Flametal ×3 | Surt |

Recipes are hidden, never removed, so switching mode never deletes items you already own.
