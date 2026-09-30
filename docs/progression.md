# ⚙️ Progression (config)

[← Back to the README](../README.MD)

## ⚙️ PROGRESSION (CONFIG)

Configured in `BepInEx/config/com.jotunn.BrudvikWhiteHilt.cfg`. All settings are admin-only and synced from the server.

| Setting | Values | Description |
|---------|--------|-------------|
| `[General] Mode` | `Full` (default), `Linear` | Full: every recipe is available as before. Linear: White Hilt gear unlocks biome by biome. |
| `[General] ShowUnlockMessages` | `true`/`false` | Show a message listing the newly available items when a tier unlocks. |
| `[Tiers] <ItemId>` | `Default`, `Start`, `BlackForest`, `Swamp`, `Mountain`, `Plains`, `Mistlands`, `Ashlands`, `Never` | Per-item tier override (linear mode). `Never` disables the recipe in both modes. Also works for the Stone Pot and its food. |
| `[Foraging.<Name>] Spawn` | `true`/`false` | Let the plant grow in zones generated from now on. Existing zones are not changed. |
| `[Foraging.<Name>] SpawnPerZone` | 0–20 | Maximum groups per zone (64 × 64 m). Values below 1 are a chance to place one group. |
| `[Foraging.<Name>] ExtraDropChance` | 0–1 | Chance that picking the matching vanilla plant also gives the ingredient. 0 turns it off. |
| `[Foraging.Crowberries] CreatureDropChance` | 0–1 | Chance that a Wolf drops 1–2 Crowberries. |
| `[Food.<Name>] Health`, `Stamina`, `DurationMinutes`, `Regen` | numbers | Values of each Stone Pot dish. Changes also apply to food already in inventories. |

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
