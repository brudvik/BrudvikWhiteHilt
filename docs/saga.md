# 📜 Saga

[← Back to the README](../README.MD)

## 📜 SAGA

Every character keeps a saga of the deeds it has been part of. Press **F8** to read it. The newest deeds come first, each with the day it happened.

| Deed | When | Renown |
|------|------|--------|
| **A boss fell** | A boss dies within 50 m of you | 10 |
| **A black beast was slain** | A black beast dies within 50 m of you | 3 |
| **A great creature was slain** | The Kraken, a Lindorm or a Giant Spider dies within 50 m of you | 5 |
| **A treasure was dug up** | A treasure chest comes up within 50 m of you | 3 |
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
| `WitnessRange` | 50 | How near a slain foe or a dug-up treasure you must be, in metres |
| `RenownBoss`, `RenownBeast`, `RenownMonster` | 10, 3, 5 | Renown for a boss, a black beast and a listed creature |
| `RenownTreasure`, `RenownBiome`, `RenownSkill` | 3, 2, 1 | Renown for a treasure, a new biome and a skill milestone |
| `RenownPerRank` | 20 | Renown for each rank |
| `MaxRank` | 10 | Highest rank |
| `CarryPerRank`, `StaminaPerRank` | 10, 3 | Carry weight and maximum stamina per rank |
| `Creatures` | WhiteHilt_Kraken, WhiteHilt_Lindorm, WhiteHilt_GiantSpider | Prefab names of creatures whose death is a deed, besides bosses and black beasts |

Section `[Saga.Keys]` (your own): `OpenSaga` (F8).
