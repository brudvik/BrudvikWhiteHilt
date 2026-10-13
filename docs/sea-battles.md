# ⚓ Sea battles & raids

[← Back to the README](../README.MD)

Out on the open sea, a dark longship may come for your ship. It is a **Nail Ship** from Naglfar's fleet, crewed by draugr. You can outrun it, sink it or kill its crew and take it as a prize. Its hold may carry a **Raid Chart** that leads to a draugr harbour on a coast you know.

Nothing here needs a new model: the Nail Ship is the vanilla longship with a dark hull, a dim sail and bone-white shields, and the crew are creatures from the game. The fight is built for the [Ship Ballista](ships.md#the-ship-ballista), but a bow and a good crew will do.

## The Nail Ship

### When and where it comes

A Nail Ship only comes when everything lines up:

- **Nail Ships are on** (`[SeaBattles] Enabled`) and the world has reached the key `RequiredKey` (**Bonemass** is slain, by default).
- You are on a ship at sea: the **ocean** biome, at least **20 m** of water under you (`MinDepth`) and **no land within 100 m** (`MinShoreDistance`).
- You are aboard a ship at sea. The ship can be any ship: a raft, a Karve, a longship, a Drakkar or a White Hilt ship. A Nail Ship is never sent after another Nail Ship.
- No other Nail Ship is hunting and no [Kraken](kraken.md) is about.
- The world's **cooldown** is over: at most one Nail Ship every **45 real minutes** (`CooldownMinutes`) in the whole world. The server forgets the cooldown when it restarts.
- The crew list has a tier for the world. With `RequiredKey` empty and no tier's key set yet, the first tier sails (see [Crew](#the-crew)).

Once a minute, the client that owns the ship asks the server, which checks all of this again and rolls the dice once per ship: **4% per minute** with one player aboard, **2 more percentage points** for each extra player aboard (`ChancePerMinute`, `ChancePerExtraPlayer`; 0 means none come by themselves). The cooldown starts only when a Nail Ship is sent, and the order to raise it goes to whichever machine owns your ship.

When one comes, a **horn** sounds near your ship and everyone aboard sees *Sails on the horizon! A Nail Ship comes for you.* The Nail Ship appears **180 m** away (`SpawnDistance`) in the quarter ahead of you, in deep water, facing your ship. It has no helmsman and no player aboard: its crew sails it.

A [Lookout](navigation.md#what-exploration-gives) shows it as a sea monster. It never shows on a [Harbour Anchor](ships.md)'s map, it cannot be pushed, moored or taken apart with the Hammer, and the Kraken does not rise beside it.

### The crew

The crew depends on how far the world has come. Each tier belongs to a global key, and the last key that is set wins (`CrewTiers`):

| After | Crew |
|---|---|
| Bonemass | 3 Draugr and 2 Draugr Archers |
| Moder | 3 Draugr, 2 Draugr Archers and 1 Draugr Elite |
| Yagluth | 3 Fulings, 2 Fuling Archers and 1 Fuling Brute |

Each extra player aboard your ship adds **1 more warrior** (`CrewPerExtraPlayer`). Players counted are those within 60 m of your ship. A creature with *Ranged* or *Archer* in its prefab name is an archer; the others fight in melee. The crew are ordinary creatures of their kind, with stars rolled as for any other spawn, and they hunt players.

The Nail Ship has **1200 health** (`HullHealth`). It takes pierce damage in full, so arrows and ballista bolts hurt it, is weak to fire and shrugs off chop, pickaxe, poison and spirit damage. It is not indestructible.

## The fight

1. **Approach.** It sails at full sail with the wind straight behind it (`Tailwind`), aiming ahead of where you are going. It steers round shallows and rocks, and stops if it finds no clear way.
2. **Broadside.** Within about 38 m (1.5 × `BroadsideDistance`) it turns and lies alongside at half sail, **25 m** off one side of your ship (chosen at random). Its archers shoot, and its warriors wait on its deck. It sails faster again if you pull more than 50 m away.
3. **Boarding.** After **40 seconds** (`BroadsideSeconds`, 0 means never) it closes in until the gap is about 4 m. Then, every **4 seconds** (`BoardIntervalSeconds`), the next of its warriors leaps across onto your deck, one at a time and in number order, once he is within **6 m** (`BoardDistance`) of it. Once he is aboard he is an ordinary creature on your ship. A ship with only archers left never boards.
4. **Flee.** When only **25%** of its crew is left (`FleeCrewShare`) or **20%** of its hull (`FleeHullShare`), it turns and runs at full sail for good, and everyone within 100 m is told *The Nail Ship turns and runs.* A fleeing ship gives no prize unless you catch it and kill the crew.

While nobody is on a Nail Ship's deck, its crew keep to their posts. Archers shoot from where they stand and warriors wait for their turn. If you leap aboard, the warriors fight you, but one who walks off the deck is put back.

The helm of a Nail Ship stays shut (*The dead still hold the helm.*) and so does its hold (*The crew guards the hold.*) while its crew lives. You cannot use the hold for crafting either.

A Nail Ship **gives up** when no player is within **300 m** (`DespawnRange`) for **60 seconds** (`DespawnSeconds`), or when the ship it hunts is gone for as long. Then it and its crew sail out of sight. A player standing on it keeps it.

### Boarding leap

On the deck of any ship, **look at another ship within 10 m** (`BoardingLeapRange`) and press **Use**: *Leap aboard* shows under the crosshair. You land on the deck point nearest to where you looked and keep the other ship's speed, so you do not slide off. There is a one-second wait between leaps.

It works between any two ships, also to help a friend, and onto a Nail Ship. Use goes to the helm, the hold or a door first if you look at one of them. The leap works while `[SeaBattles] Enabled` is on.

## The prize

When the last of the crew is dead, the Nail Ship is yours:

- It stops, and everyone within 100 m sees *The crew is dead. The Nail Ship is yours, but its nails will not hold for long.*
- The helm and the hold open. Take the helm and sail it, or empty the hold.
- The hull shows *Prize: sinks in N min*. After **10 minutes** (`PrizeMinutes`, world time) the nails give way and it sinks. The hold drops as a **cargo crate**.
- A prize at sea is not kept in the world. If everyone sails far away, it is gone with its hold.

The hold holds **4 draws** from a loot list (`Loot`, `LootRolls`): by default coins, iron, chain, withered bone, amber, amber pearls, rubies, Ship Bolts and a draugr trophy. By chance (**35%**, `RaidChartChance`) it also holds a **Raid Chart**.

If you sink the hull instead, the deed counts too and the hold drops as a cargo crate, but the crew goes down with the ship and leaves no loot.

## The Ship Ballista

The [Ship Ballista](ships.md#the-ship-ballista) is a ship upgrade for the White Hilt Ship and Skidbladnir. Man it, aim with the camera and shoot **Ship Bolts** with the attack button. The bolts hurt a Nail Ship's hull and crew, and also serpents and the Kraken. The recipe, the bolt and the settings are on the [Ships](ships.md) page.

## Raids

A **Raid Chart** has no recipe and no trader: it is found in a Nail Ship's hold. Use it from your inventory to mark a **draugr harbour** on a coast.

### The chart

- The harbour lies **400 to 1500 m** from you (`MinDistance`, `MaxDistance`), on land you have **explored**, near water. The biomes are the ones in `[Treasure] AllowedBiomes`: the Meadows to the Plains by default.
- The server checks the place. It must be dry, away from other harbours (`[Treasure] MinTreasureDistance`, 150 m), from anything players have built (`[Treasure] MinBaseDistance`, 60 m), from other places and from trees and rocks at the chest. There must be **deep water** for a ship within 80 m of it (`ShipSearchRadius`).
- The harbour is marked on your map as a **Draugr harbour** pin and you see *A draugr harbour is marked on your map.* If you wear the Pathfinder's Ruby Amulet, its target arrow points the way.
- The chart is used up only when a harbour was made. With no known coast it says *No coast on this chart is known to you.* With `MaxActive` harbours waiting (6 by default; a sacked one does not count) it says *Every draugr harbour is manned already. Sack one first.* In both cases you keep the chart.

### The harbour

- **A moored Nail Ship** lies in the water off the shore, along the coast, crewed as at sea and with a hold of its own. It does not sail. Kill its crew and it becomes a prize like any other: you can sail it away, and it sinks after the prize minutes.
- **Guards** stand in a ring round the hoard, **12 m** from it (`GuardRadius`): by default 4 Draugr, 2 Draugr Archers and 1 Draugr Elite (`Guards`). A guard that strays more than **8 m** from its post (`GuardHoldRadius`) turns back, unless a player stands within that distance of the post.
- **The hoard** is a chest with 8 draws from its own loot list (`Loot`, `LootRolls` in `[SeaBattles.Raids]`: the ship's list plus a Gift of Odin). It stays shut while a guard lives near it (*The guards still live.*). It cannot be broken open or taken apart.
- The harbour is made in the saved world, so it is there when you arrive, also on a dedicated server where nobody was near, and it stays after you leave or the world restarts.

When the hoard is opened with no guard alive, the harbour is **sacked**. It is a [saga](saga.md) deed, once. The chest goes away once it is empty. The ship and what is left of its crew stay.

## Saga deeds

Both are deeds in the [Saga](saga.md), shared by everyone within 50 m (`[Saga] WitnessRange`):

| Deed | When | Renown |
|---|---|---|
| **A Nail Ship was taken** | Its crew is dead, or its hull sinks | 25 (`RenownSeaBattle`) |
| **A draugr harbour was sacked** | Its hoard is opened with no guard alive | 40 (`RenownRaid`) |

## Multiplayer

- **The server decides** when a Nail Ship comes and makes the draugr harbours. It checks every request, so a modified client cannot ask for more. The server and every player need this version of the mod.
- Everyone sees the same ship, the same crew and the same horn. The ship's owner, usually the nearest player, steers it. If that player leaves, the next one carries on.
- The crew are owned apart from the ship, so a crew member shot by one player and a ship steered by another still work together.
- The aim of a ballista is kept on the gunner, so nobody has to own the ship to shoot.
- The prize timer uses world time, so it runs also while nobody is near.

## Console

`whitehilt_seabattle [status|summon|raid]`

| Command | Who | What it does |
|---|---|---|
| `status` | Everyone | Shows the conditions for a Nail Ship where you are (on a ship, owner, players aboard, chance, ocean, depth, nearest land, key) and the Nail Ships loaded here |
| `summon` | Admins | Sends a Nail Ship after the ship you are aboard, with no dice, no cooldown, no key and no ocean test. It needs deep water in the quarter ahead; the machine that owns your ship raises it. With no crew key set in the world, the first crew tier is used. It starts the cooldown |
| `raid` | Admins | Makes a draugr harbour on the coast nearest you, 50 m to `MaxDistance` away. You need not have explored it, and `MaxActive` still counts. The console says where it is and a pin is set |

The server checks who may call `summon` and `raid`: the host and the admins of a dedicated server. Others get *Only admins can do that.* Both need `[SeaBattles] Enabled`.

## Config

All admin only, synced from the server. Settings in section `[SeaBattles]`:

| Setting | Default | What it does |
|---|---|---|
| `Enabled` | true | Nail Ships can come, Raid Charts and the boarding leap work. Off: no new Nail Ships and no charts |
| `RequiredKey` | defeated_bonemass | Global key needed before a Nail Ship comes; empty for none (then the first crew tier sails until a later tier's key is set) |
| `ChancePerMinute` | 4 | Chance in percent per minute for a ship at sea while every condition holds; 0 to 100. 0 stops the natural encounters |
| `ChancePerExtraPlayer` | 2 | Extra percentage points per extra player aboard; 0 to 100 |
| `CooldownMinutes` | 45 | Real minutes before the next Nail Ship anywhere in the world; 0 to 1440 |
| `MinDepth` | 20 | Least depth of water under your ship, in metres; 5 to 200 |
| `MinShoreDistance` | 100 | No land within this many metres of your ship; 0 to 500 (0 accepts any shore) |
| `SpawnDistance` | 180 | Metres from your ship where the Nail Ship appears; 80 to 400 |
| `CrewTiers` | `defeated_bonemass=Draugr:3,Draugr_Ranged:2; defeated_dragon=Draugr:3,Draugr_Ranged:2,Draugr_Elite:1; defeated_goblinking=Goblin:3,GoblinArcher:2,GoblinBrute:1` | The crew per global key: `key=Prefab:count,Prefab:count; next key=...`. The last key that is set wins. A name with *Ranged* or *Archer* is an archer. Unknown prefabs are skipped with a warning |
| `CrewPerExtraPlayer` | 1 | Extra warriors per extra player aboard; 0 to 5 |
| `HullHealth` | 1200 | Health of a Nail Ship's hull; 100 to 20000. Applies after a restart |
| `SailForceFactor` | 1 | Multiplier on the longship's sail force; 0.2 to 5. Applies after a restart |
| `Tailwind` | true | Nail Ships under sail always have the wind behind them |
| `BroadsideDistance` | 25 | Metres it keeps alongside while its archers shoot; 10 to 60 |
| `BroadsideSeconds` | 40 | Seconds alongside before it closes in to board; 0 to 300. 0: it never boards |
| `BoardDistance` | 6 | Metres within which its warriors leap across; 3 to 15 |
| `BoardIntervalSeconds` | 4 | Seconds between two warriors leaping; 1 to 30 |
| `FleeCrewShare` | 0.25 | It flees when this share of its crew is left; 0 to 1 |
| `FleeHullShare` | 0.2 | It flees when this share of its hull health is left; 0 to 1 |
| `DespawnRange` | 300 | Metres. With no player this close it sinks out of sight; 100 to 1000 |
| `DespawnSeconds` | 60 | Seconds without a player in range before it goes; 10 to 600 |
| `PrizeMinutes` | 10 | Minutes a Nail Ship without a crew floats before it sinks; 1 to 60 |
| `Loot` | `Coins:40-120:10, Iron:3-8:6, Chain:1-2:4, WitheredBone:4-8:6, Amber:1-2:4, AmberPearl:1-2:3, Ruby:1-2:2, WhiteHiltShipBolt:5-10:6, TrophyDraugr:1-1:2` | The hold's loot list, `Item:min-max:weight` separated by commas, as in `[Treasure] Loot` |
| `LootRolls` | 4 | Draws from the loot list; 0 to 20 |
| `RaidChartChance` | 0.35 | Chance that the hold carries a Raid Chart; 0 to 1 |
| `BoardingLeapRange` | 10 | Metres you can leap from deck to deck; 3 to 25 |

Settings in section `[SeaBattles.Raids]`:

| Setting | Default | What it does |
|---|---|---|
| `MinDistance` | 400 | Least distance from the one who uses a chart to the harbour, in metres; 100 to 3000 |
| `MaxDistance` | 1500 | Farthest distance, in metres; 200 to 5000 |
| `Guards` | `Draugr:4,Draugr_Ranged:2,Draugr_Elite:1` | The shore guards, `Prefab:count` separated by commas |
| `GuardRadius` | 12 | Metres round the hoard the guards stand; 4 to 40 |
| `GuardHoldRadius` | 8 | Metres a guard walks from its post before it turns back; 2 to 40 |
| `ShipSearchRadius` | 80 | Metres from the hoard to look for deep water for the moored Nail Ship; 20 to 200 |
| `Loot` | the ship's list plus `GiftOfOdin:1-1:2` | The hoard's loot list |
| `LootRolls` | 8 | Draws from the hoard's loot list; 0 to 30 |
| `MaxActive` | 6 | Most harbours in the world that have not been sacked; 1 to 50 |

The harbours' spacing, their clearance from buildings and their biomes come from `[Treasure]` (`MinTreasureDistance`, `MinBaseDistance`, `AllowedBiomes`), see [Treasure maps](treasure.md). The two deeds' renown is `RenownSeaBattle` and `RenownRaid` in `[Saga]`. The ballista and the bolts have their own section, `[Ships.Ballista]`, on the [Ships](ships.md) page.
