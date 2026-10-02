# 🕷️ Lindorm & giant spiders

[← Back to the README](../README.MD)

<img src="images/lindorm.png" alt="Lindorm" title="Lindorm" height="140"> <img src="images/giantspider.png" alt="Giant Spider" title="Giant Spider" height="140">

Two new monsters haunt the forest and the swamp: the Lindorm that breaks out of the ground at night, and giant spiders that nest among the trees.

## The Lindorm

A great worm that lies in wait under the forest floor. It only comes when everything lines up:

- you are **on foot** in the **Black Forest** or the **Swamp** (not on a ship, in the water, indoors or near your base),
- it is **night**,
- **Eikthyr** is slain.

Then, once a minute, there is a 3% chance it **breaks out of the ground** 9–15 m from you, in a burst of earth and stone. Each player can meet it at most once every 30 minutes.

- It cannot be hurt and does not move while it is still breaking out.
- It **burrows back down**, without loot, when it has lost you for about 25 seconds, or soon after dawn.
- When slain it writhes, falls still and sinks into the ground.

## Giant spiders

Giant spiders live around **spider nests** in the Black Forest: a pale, web-covered mound with a ring of eggs. A nest keeps up to 3 spiders near it and sends out a new one every 20 seconds or so while someone is close. **Destroy the nest** to stop them; it drops Spider Silk.

Their bite is **poisonous** and **webs** you: you move at half speed for 3 seconds. They are afraid of fire, avoid water and are weak to blunt and fire damage.

Nests only appear in **newly generated** Black Forest land, in about 15% of its zones. Land you have already explored keeps what it had.

| | Health | Attack | Weak to |
|---|---|---|---|
| **Lindorm** | 700 | Bite, 55 pierce | Fire |
| **Giant Spider** | 120 | Bite, 18 pierce + 15 poison, webs you | Blunt, fire |
| **Spider Nest** | 300 | — | — |

The Lindorm shrugs off chop and pickaxe damage, resists pierce and poison and is immune to spirit damage. Spiders ignore chop and pickaxe damage and are immune to poison.

## Loot

| Item | From | Chance | Used for |
|------|------|--------|----------|
| **Lindorm Scale** ×2–4 | Lindorm | Always | Every upgrade of a White Hilt shield ([White Hilt gear](equipment.md#-upgrades-through-the-biomes)) |
| **Entrails** ×1–3 | Lindorm | Always | Vanilla recipes |
| **Lindorm Trophy** | Lindorm | 15% | Your wall |
| **Spider Silk** ×1–2 | Giant Spider | Always | Gift of Loki ([Potions](potions.md)), mending Shore Nets ([Fishing nets](fishing.md)), the Spider's Web rune ([Smithing](smithing.md#-binding-and-rune-etching)) |
| **Poison Gland** | Giant Spider | 50% | Gift of Hel ([Potions](potions.md)), the Venom rune ([Smithing](smithing.md#-binding-and-rune-etching)) |
| **Giant Spider Trophy** | Giant Spider | 10% | Your wall |
| **Spider Silk** ×3–5 | Spider Nest | Always | As above |

## Config

Section `[Lindorm]` (admin only, synced from the server):

| Setting | Default | What it does |
|---|---|---|
| `Enabled` | true | The Lindorm can come at all |
| `RequiredKey` | defeated_eikthyr | Global key needed first; empty for none |
| `Biomes` | BlackForest, Swamp | Biomes where it lurks |
| `NightOnly` | true | Only at night |
| `ChancePerMinute` | 3 | Percent per minute while every condition holds |
| `CooldownMinutes` | 30 | Real minutes before it can come for the same player again |
| `Health` / `Damage` | 700 / 55 | Health, and pierce damage of its bite |
| `Scale` | 1.3 | Size (1 = about 4 m long; after a restart) |
| `GiveUpSeconds` | 25 | Seconds without prey in sight before it burrows away |
| `TrophyChance` | 15 | Percent chance of its trophy (after a restart) |

Section `[Giant Spider]`:

| Setting | Default | What it does |
|---|---|---|
| `Enabled` | true | New nests appear in new Black Forest land |
| `Health` | 120 | Health of a spider |
| `Damage` / `Poison` | 18 / 15 | Pierce and poison damage of its bite |
| `WebSeconds` | 3 | Seconds a bite slows you; 0 = no slow |
| `Scale` | 1 | Size (1 = about 1.6 m across; after a restart) |
| `NestChancePerZone` | 0.15 | Chance of a nest in each new Black Forest zone |
| `NestHealth` | 300 | Health of a nest |
| `NestMaxNear` | 3 | Spiders a nest keeps around it |
| `TrophyChance` | 10 | Percent chance of a spider's trophy (after a restart) |
| `NestSpawnSeconds` | 20 | Seconds between two spiders from a nest (after a restart) |
| `NestLevelUpChance` | 10 | Percent chance a spider from a nest gets a star (after a restart) |

## Console commands

- `whitehilt_lindorm` shows every condition where you are, and whether it holds.
- `whitehilt_lindorm summon` (admins) lets the Lindorm break out near you now.
- With devcommands: `spawn WhiteHilt_GiantSpider`, `spawn WhiteHilt_SpiderNest` and `spawn WhiteHilt_Lindorm`.
