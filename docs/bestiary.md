# 📖 Black Bestiary & Material Counters

[← Back to the README](../README.MD)

The **Black Bestiary** (**Svartboka** in Norwegian) is an open book on the same kind of stand as the guestbook. Build it with the hammer under **Furniture**, using only Meadows materials. It is a field guide, not a visitor log, and reading it does not require killing a beast first.

Pages unlock only when **the reader discovers a biome where that beast lives**, independently of boss kills. A character who has only explored Meadows sees **A warning / Et varsel**: a hint of dangers beyond the forest, mountains and waves, without monster names, illustrations or weaknesses. Exploration belongs to the character, not the stand: different players reading the same book see their own discovered pages. Existing exploration is recognized from the game's stored biome names; new discoveries are also saved with stable biome identities, independent of language. The selected beast stays selected when new pages become available.

| Discovered biome | Pages unlocked |
|---|---|
| Meadows | Warning only |
| Black Forest | Black Troll |
| Swamp | Black Abomination |
| Mountains | Black Stone Golem, Black Dragon |
| Plains | Black Fuling Berserker, Black Dragon if not already known |
| Mistlands | Black Seeker Soldier |
| Ashlands | Black Morgen, Black Bonemaw |
| Ocean | Black Serpent |

The Dragon has one page even after visiting both Mountains and Plains. Discovering a biome reveals knowledge, not an early encounter: the beasts retain their existing guardian-boss spawn requirements.

Each page shows an indistinct **ink sketch on textured paper**, its biome and guardian boss, dangerous attacks, its unique material counter, where to gather the materials and how to craft the counter. The illustration suggests the creature's shape through soft outlines and sparse hatching rather than showing a full-colour trophy: fine details stay hidden until the encounter. Trophy and inventory icons are unchanged. The recipe is read from the game's registered recipe, so server recipe overrides, station level, output and disabled/locked status are reflected in the book. Long pages scroll. The book closes on Escape, death, destruction, or moving beyond the configured reading distance.

| Piece | Station | Requirements |
|---|---|---|
| **Black Bestiary** (`piece_whitehilt_svartbok`) | Hammer, near Workbench | Wood x6, Leather Scraps x3, Feathers x1, Coal x2 |

## Material Weaknesses

These are **specific alchemical preparations**, not new universal damage-type weaknesses. A plain frost arrow still benefits from the Black Dragon's existing frost weakness, but only a **Rime Arrow made with crowberries** earns its material bonus. Likewise, ordinary poison, spirit oils and runes do not count as the corresponding special preparations.

The default material bonus is an extra damage component equal to **50% of the incoming attack's combat damage before resistance and armour**. For Stonebreaker, pickaxe damage counts too; chop damage never counts. The extra component uses Valheim's creature-only damage channel, with its own resistance and armour handling, so elemental immunity does not erase the material preparation and it is **not true damage**. Existing resistance profiles, health and attacks are not changed. No bonus applies to other black beasts, ordinary creatures, players or Kraken.

The marker follows the fired projectile rather than looking at the attacker's currently held weapon when it lands. It is consumed once on the target owner before vanilla status-effect processing. All participating clients and the server need this version.

## Special Arrows

All are made at the **Workbench, level 1**. Each batch makes **20** by default and uses **Wood x8, Feathers x2**, plus the materials below. These are arrows for bows, not crossbow bolts. Base damage is before bow damage, skill, buffs or target resistances.

| Arrow | Matching black beast | Additional materials | Base damage |
|---|---|---|---|
| **Troll Arrow / Trollpil** | Black Troll | Flint x2, Lingonberries x3 | 27 pierce |
| **Ember Arrow / Glødepil** | Black Abomination | Peat x2, Resin x3 | 11 pierce, 22 fire |
| **Rime Arrow / Rimpil** | Black Dragon | Crowberries x3, Freeze Gland x1 | 26 pierce, 52 frost |
| **Seid Arrow / Seidpil** | Black Morgen | Juniper Berries x3, Silver x1 | 26 pierce, 20 spirit |
| **Bog Venom Arrow / Myrgiftpil** | Black Serpent | Poison Gland x1, Sweet Gale x3 | 26 pierce, 26 poison |
| **Storm Arrow / Stormpil** | Black Bonemaw | Rosehips x3, Kraken Ink x1 | 32 pierce, 30 lightning |

Prefab names: `WhiteHiltTrollArrow`, `WhiteHiltEmberArrow`, `WhiteHiltRimeArrow`, `WhiteHiltSeidArrow`, `WhiteHiltBogVenomArrow`, `WhiteHiltStormArrow`.

## Weapon Treatments

Made at the **Workbench, level 1**, one per craft. Hold a compatible weapon and use the preparation from the inventory or hotbar. Only that item is treated: switching to another weapon neither transfers the effect nor uses a charge. Switching back resumes it. A new material treatment replaces the previous one. Treatments do not persist through logout and are not permanent item upgrades.

Each lasts **30 eligible attacks**, including misses. Stonebreaker mining swings also use charges; Carapace woodcutting swings also use charges. No charge or marker is added when the attack already carries another explicit attack status effect. Normal damage remains unchanged against nonmatching targets. Existing oils and runes are separate systems.

| Treatment | Matching black beast | Compatible weapon | Requirements |
|---|---|---|---|
| **Stonebreaker Coating / Steinbryterbelegg** | Black Stone Golem | Pickaxe | Rock Lichen x3, Slate x2, Resin x2 |
| **Berserker Coating / Berserkerbelegg** | Black Fuling Berserker | Club, mace or sledge | Henbane x3, Resin x2, Coal x1 |
| **Carapace Whetstone / Panserbryner** | Black Seeker Soldier | Sword or axe | Woad x3, Slate x2, Resin x2 |

Prefab names: `WhiteHiltStonebreakerCoating`, `WhiteHiltBerserkerCoating`, `WhiteHiltCarapaceWhetstone`.

Gathering locations are described in the book and on [Foraging](foraging.md). Slate is described on [Roofs](roofs.md); Poison Glands on [Monsters](monsters.md); Kraken Ink on [Kraken](kraken.md). Every default recipe uses ingredients from its own biome or earlier. These recipes stay at `ProgressionTier.Start`: ingredients gate them without added tier costs.

## Server Settings

Admin-only, synced. `[Bestiary]`:

| Setting | Default | Meaning |
|---|---|---|
| `BookHealth` | 300 | Book and stand health |
| `ReadingDistance` | 8 | Metres before the open book closes |
| `RefreshSeconds` | 1 | Interval for refreshing open-page recipes and settings |

Each `[Bestiary.<Counter>]`, e.g. `[Bestiary.RimeArrow]`:

| Setting | Default | Meaning |
|---|---|---|
| `Bonus` | 0.5 | Extra material damage share only against the matching beast; 0 disables the bonus |
| `Yield` | 20 arrows / 1 treatment | Items made per craft |
| `Pierce` | table above | Arrow pierce damage |
| `Element` | table above | Arrow fire/frost/spirit/poison/lightning damage; unused for Troll Arrows |
| `Attacks` | 30 | Treatment attack count, including misses |

The existing `[Content]` and `[Recipes]` settings control each item and `piece_whitehilt_svartbok`. Recipe overrides are visible in the book; gathering directions describe the default special ingredients. See [Difficulty](difficulty.md) for encounters and [Smithing](smithing.md) for permanent trophy binding, runes and ordinary oils.

## Testing

Build and offline behavior checks do not replace in-game testing. Verify all nine counters against their matching and nonmatching beasts, shot-then-weapon-switch behavior, treatment charges and weapon restrictions, sketch readability, paging and scrolling at different HUD scales, Escape/input release and dedicated-server ownership. No asset bundle rebuild is needed: existing vanilla arrows, oil-flask/whetstone models and guestbook stand are reused; trophy icons are processed into cached paper sketches only for the book.