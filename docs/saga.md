# 📜 Saga

[← Back to the README](../README.MD)

## 📜 SAGA

Every character keeps a saga of the deeds it has been part of. Press **F8** to read it. The newest deeds come first, each with the day it happened.

| Deed | When | Renown |
|------|------|--------|
| **A boss fell** | A boss dies within 50 m of you | 10 |
| **A black beast was slain** | A black beast dies within 50 m of you | 3 |
| **A great creature was slain** | The Kraken, a Lindorm, a Giant Spider or a [Draugr Jarl](sea-battles.md#the-draugr-jarl) dies within 50 m of you | 5 |
| **A treasure was dug up** | A treasure chest comes up within 50 m of you | 3 |
| **A Nail Ship was taken** | A [Nail Ship](sea-battles.md)'s crew is dead, or its hull sinks, within 50 m of you | 25 |
| **A Nail fleet was broken** | The flagship of a [fleet](sea-battles.md#fleets) is taken or sunk within 50 m of you. It comes besides *A Nail Ship was taken* for the flagship itself | 40 |
| **A draugr harbour was sacked** | The hoard of a [draugr harbour](sea-battles.md#raids) is opened with no guard alive, within 50 m of you | 40 |
| **First steps into a biome** | You set foot in a biome for the first time (not the Meadows) | 2 |
| **A skill milestone** | A skill reaches 25, 50, 75 or 100 | 1 |

Everyone near a deed has it in their saga, so a party that fights together shares it.

## Renown and rank

Every 20 renown is a rank, up to rank 10. Each rank gives **+10 carry weight** and **+3 maximum stamina** for good. The window shows your rank, your renown, how much the next rank needs, and what your rank gives. A new rank is announced.

The saga and the renown are kept on the character, like its skills.

## Config

Section `[Saga]` (admin only, synced from the server):

| Setting | Default | What it does |
|---|---|---|
| `Enabled` | true | Keep the saga; off also switches the rewards off |
| `WitnessRange` | 50 | How near a slain foe, a dug-up treasure, a taken Nail Ship, a broken fleet or a sacked harbour you must be, in metres |
| `RenownBoss`, `RenownBeast`, `RenownMonster` | 10, 3, 5 | Renown for a boss, a black beast and a listed creature |
| `RenownTreasure`, `RenownBiome`, `RenownSkill` | 3, 2, 1 | Renown for a treasure, a new biome and a skill milestone |
| `RenownSeaBattle`, `RenownRaid`, `RenownFleet` | 25, 40, 40 | Renown for a Nail Ship taken or sunk, a draugr harbour sacked and a Nail fleet broken, see [Sea battles & raids](sea-battles.md) |
| `RenownPerRank` | 20 | Renown for each rank |
| `MaxRank` | 10 | Highest rank |
| `CarryPerRank`, `StaminaPerRank` | 10, 3 | Carry weight and maximum stamina per rank |
| `Creatures` | WhiteHilt_Kraken, WhiteHilt_Lindorm, WhiteHilt_GiantSpider, WhiteHilt_DraugrJarl | Prefab names of creatures whose death is a deed, besides bosses and black beasts. A config file made before the jarl came keeps its old list: add `WhiteHilt_DraugrJarl` to it by hand |

Section `[Saga.Keys]` (your own): `OpenSaga` (F8).
