# 🐙 Kraken & octopus

[← Back to the README](../README.MD)

<img src="images/kraken.png" alt="Kraken" title="Kraken" height="140"> <img src="images/octopus.png" alt="Octopus" title="Octopus" height="140">

The sea is no longer empty. Octopuses swim in the deep, and on a still, foggy night the Kraken may rise beside your ship.

## Octopus

Octopuses swim below the surface of the ocean, jetting along mantle first in short pulses. Catch them with a fishing rod and ocean bait, like the fish they share the water with. Cook them into **Octopus Stew** in the Stone Pot (see [Foraging & food](foraging.md)).

## The Kraken

The Kraken only comes when everything lines up:

- you sail a ship on the **ocean**, over at least 25 m of water,
- it is **night**,
- the weather is **misty** (fog),
- the wind is **calm** (at most 0.35),
- **Bonemass** is slain.

Then, once a minute, there is an 8% chance it rises with one player aboard. Each additional player on the same ship adds 8 percentage points: 16% with two players, 24% with three, and 40% with five, capped at 100%. Players on other ships do not contribute to that ship's chance. All the conditions above still apply. It comes at most once every 90 minutes in the whole world.

### The fight

1. The sea shakes and **the Kraken rises** beside the ship, its glowing eyes just above the water.
2. **Tentacles** break the surface in a ring around the ship and lash at everyone aboard.
3. The Kraken **holds the ship fast**: it barely moves and rocks now and then.
4. **Kill the Kraken** to free the ship. Its tentacles die with it, and the dead Kraken sinks into the deep.
5. It gives up after 5 minutes, at dawn, or when no one is near, and sinks back with its tentacles, without loot.

| | Health | Attack |
|---|---|---|
| **Kraken** | 4000 | Slam, 90 blunt, reaches about 13 m |
| **Kraken Tentacle** | 500 | Lash, 45 blunt, reaches about 12 m |

Both shrug off chop and pickaxe damage and poison, resist fire and frost, and are weak to lightning.

Large crews face harder blows. When the Kraken rises, it remembers how many players are aboard its target ship. Both the body and tentacles deal an extra **15% of solo damage to players** and **3% of solo ship damage** per additional crew member. These bonuses multiply the configured damage shares and stack with vanilla multiplayer and world-difficulty scaling; the attack values above are base values, before armor and resistances.

| Players aboard at spawn | Extra damage to players | Extra damage to the ship |
|---|---|---|
| 1 | None | None |
| 2 | +15% | +3% |
| 3 | +30% | +6% |
| 5 | +60% | +12% |
| 10 | +135% | +27% |

The crew bonus stays fixed for that encounter, even if players jump overboard, die or join later. It is saved with the Kraken and copied to its tentacles, including for admin-summoned encounters. Older Krakens without a saved crew count retain solo damage. Health and tentacle count are unchanged; vanilla effective-health scaling still applies. Set either extra-player damage setting to 0 to disable that bonus. A damage share of 0 still prevents that kind of damage entirely.

The Kraken has its own deep, watery calls for idle, alert, slam, injury and death. Its tentacles share the calls and use a separate wet lash sound when striking. Set `[Kraken] Sounds` to false to keep the previous vanilla sounds.

### Loot

| Item | Description | Used for |
|------|-------------|----------|
| **Kraken Tentacle** ×4–6 | A slab of tentacle | Kraken Feast, the Grip of the Deep rune ([Smithing](smithing.md#-binding-and-rune-etching)) |
| **Kraken Ink** ×3–5 | Thick black ink | Kraken Feast, a black dye at the Paint Bench ([Painting](painting.md)), the Storm and Grip of the Deep runes ([Smithing](smithing.md#-binding-and-rune-etching)) |
| **Kraken Trophy** | Proof you lived | Your wall |
| **Chitin** ×6–10 | Hard shell | Vanilla recipes |

## Config

Section `[Kraken]` (admin only, synced from the server):

| Setting | Default | What it does |
|---|---|---|
| `Enabled` | true | The Kraken can come at all |
| `RequiredKey` | defeated_bonemass | Global key needed first; empty for none |
| `FogWeathers` | Misty | Weathers that count as fog |
| `MaxWind` | 0.35 | Strongest wind that counts as calm |
| `NightOnly` | true | Only at night |
| `ChancePerMinute` | 8 | Base percent per minute for one player while every condition holds; 0 disables natural attacks |
| `ChancePerExtraPlayer` | 8 | Extra percentage points per additional player on the same ship, capped at 100% total; 0 disables the crew bonus |
| `CooldownMinutes` | 90 | Real minutes between Krakens, world-wide |
| `MinDepth` | 25 | Least depth of water under the ship |
| `Tentacles` | 4 | Tentacles around the ship (0–8) |
| `BodyHealth` / `TentacleHealth` | 4000 / 500 | Health |
| `BodyDamage` / `TentacleDamage` | 90 / 45 | Blunt damage per blow |
| `CrewDamagePercent` | 100 | Share of the damage the crew takes; 0 = never hurt |
| `ShipDamagePercent` | 30 | Share of the damage the ship takes; 0 = never hurt |
| `CrewDamagePerExtraPlayer` | 15 | Extra percent of solo damage to players per additional crew member at spawn, on top of vanilla scaling; 0 disables the bonus |
| `ShipDamagePerExtraPlayer` | 3 | Extra percent of solo ship damage per additional crew member at spawn; 0 disables the bonus |
| `HoldShip` | true | The Kraken holds the ship fast |
| `RetreatMinutes` | 5 | Minutes before it gives up |
| `Scale` | 1.5 | Size of the Kraken (after a restart) |
| `Sounds` | true | Own watery calls and tentacle lash (after a restart) |
| `LootMultiplier` | 1 | Multiplier on the meat, ink and chitin it drops; the trophy stays one (after a restart) |

Section `[Octopus]`: `Enabled`, `MaxSpawned` (2) and `SpawnChance` (20%).

## Console commands

- `whitehilt_kraken` shows every condition where you are, whether it holds, the number of players aboard and the resulting chance per minute before conditions and cooldown are applied.
- `whitehilt_kraken summon` (admins) raises the Kraken beside your ship now.
