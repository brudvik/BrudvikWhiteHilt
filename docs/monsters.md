# 🕷️ Monsters

[← Back to the README](../README.MD)

<img src="images/lindorm.png" alt="Lindorm" title="Lindorm" height="140"> <img src="images/giantspider.png" alt="Giant Spider" title="Giant Spider" height="140"> <img src="images/desertdragon.png" alt="Desert Dragon" title="Desert Dragon" height="140"> <img src="images/miststag.png" alt="Mist Stag" title="Mist Stag" height="140"> <img src="images/nidhoggspawn.png" alt="Nidhogg Spawn" title="Nidhogg Spawn" height="140"> <img src="images/yggdrasilwarden.png" alt="Yggdrasil Warden" title="Yggdrasil Warden" height="140"> <img src="images/garmhound.png" alt="Garm Hound" title="Garm Hound" height="140"> <img src="images/greenlandshark.png" alt="Greenland Shark" title="Greenland Shark" height="140"> <img src="images/draugrjarl.png" alt="Draugr Jarl" title="Draugr Jarl" height="140">

New monsters haunt the land: the Lindorm that breaks out of the ground at night, giant spiders that nest among the trees, and Desert Dragons that breathe fire over the Plains. Five more beasts walk the Mistlands, the Ashlands and the open sea: the Mist Stag, the Nidhogg Spawn, the Yggdrasil Warden, the Garm Hound and the Greenland Shark (see [The wild beasts](#the-wild-beasts)). Once Moder is slain, a horn from the Bog Witch calls a White Dragon to fight at your side. A sixth beast never roams: the Draugr Jarl leads the crew of a Nail Ship flagship (see [Draugr Jarl](#draugr-jarl)).

Creature labels use localized names: Lindorm, Giant Spider and Desert Dragon (Lindorm, Kjempeedderkopp and Ørkendrage in Norwegian), and likewise for the wild beasts, not their internal `WhiteHilt_` prefab identifiers.

## The Lindorm

A great worm that lies in wait under the forest floor.

The Lindorm is about **6.4 m long** at the default size and bites for **75 pierce damage**. One and two stars add 15% and 30% visual size; higher stars also use the difficulty size settings. It has its own low growls and rasps for idle, alert, bite, injury and death.

It only comes when everything lines up:

- you are **on foot** in the **Black Forest** or the **Swamp** (not on a ship, in the water, indoors or near your base),
- it is **night**,
- **Eikthyr** is slain.

Then, once a minute, there is a 3% chance it **breaks out of the ground** 9–15 m from you, in a burst of earth and stone. Each player can meet it at most once every 30 minutes.

- It cannot be hurt and does not move while it is still breaking out.
- It **burrows back down**, without loot, when it has lost you for about 25 seconds, or soon after dawn.
- When slain it writhes, falls still and sinks into the ground.

Its strength is set when it emerges, according to the world's defeated bosses, not the victim's equipment. It starts at 0 stars after Eikthyr, then gains a minimum of 1, 2, 3, 4 and 5 stars after the Elder, Bonemass, Moder, Yagluth and the Queen. This ignores ordinary biome star limits, so new players in an advanced world should be wary. Natural encounters and `whitehilt_lindorm summon` use the same progression.

| Latest defeated boss | Stars | Health | Pierce damage |
|---|---|---|---|
| Eikthyr | 0 | 700 | 75 |
| The Elder | 1 | 1400 | 112.5 |
| Bonemass | 2 | 2100 | 150 |
| Moder | 3 | 2450 | 168.75 |
| Yagluth | 4 | 2800 | 187.5 |
| The Queen | 5 | 3150 | 206.25 |

These values use the default difficulty star formulas, before pressure bonuses (up to +20% health and +10% damage). The chosen stars and pressure bonuses stay fixed for that encounter. Empty `ProgressionStars` disables boss-based stars; disabling difficulty stars disables the pressure bonus, not the configured boss-based stars.

## Giant spiders

Giant spiders live around **spider nests** in the Black Forest: a pale, web-covered mound with a ring of eggs. A nest keeps up to 3 spiders near it and sends out a new one every 20 seconds or so while someone is close. **Destroy the nest** to stop them; it drops Spider Silk.

Their bite is **poisonous** and **webs** you: you move at half speed for 3 seconds. They are afraid of fire, avoid water and are weak to blunt and fire damage.

A normal spider is about **2.4 m across** (50% larger than before). One and two stars add 15% and 30% visual size, respectively; higher stars also use the [Difficulty](difficulty.md) size settings. They have their own dry clicks, rasps and hisses for idle, alert, bite, injury and death.

Nests appear in about 15% of the Black Forest's zones as the land is generated. Land generated before the spiders came gets its nests once, when the server starts: the same zones win the same roll, and the nest goes on a free, flat forest spot at least 50 m from anything built (`[OldLand]`, see [Progression](progression.md)).

At night, a **lone spider** may also come out anywhere in the forest, nest or no nest.

## Desert Dragons

Once **Moder** is slain, sand-coloured dragons about 8 m from wingtip to wingtip take to the skies over the **Plains**, by day and by night. A Desert Dragon never lands: it circles 5–12 m above the ground, swoops down and **breathes a stream of fire** at you from up to 25 m away. The fire leaves its mouth about as wide as the mouth and widens to 3 m on its way down, so step aside rather than back. It sets you burning, so fire resistance helps.

- Breath hitting dry terrain leaves **ground flames for 6 seconds**, with a **1.5 m radius** and **10 fire damage per second** before resistance. Nearby impacts merge; at most 3 new patches per breath and 6 active per dragon. Overlapping fields apply only their strongest damage, not their sum. Fire does not spread, change the terrain or ignite water. Damage is checked once per second and scales with stars and black beast bonuses like the breath.
- Its fire only hurts players and creatures; buildings are safe unless `BurnsBuildings` is on.
- It is immune to fire and spirit damage, weak to frost, resists poison and ignores chop and pickaxe damage. Bring a bow.
- When slain it tumbles out of the sky and lies where it fell for a while.
- It keeps beating its wings while it breathes fire, lowering its head and opening its jaws into the stream.
- It has deep roars and growls for idle, alert, attack, injury and death, three of each so it does not repeat itself, and you hear the beat of its wings while it flies.

In the dark hour and under a blood moon, its black cousin, the **Black Dragon**, may come for you in the Mountains and the Plains, like the other black beasts (see [Difficulty](difficulty.md)).

### The Dragon Horn: a White Dragon at your side

Once **Moder** is slain, the **Bog Witch** sells a **Dragon Horn** for 3000 coins. Blow it (attack with it in your hand and stand still for 3 seconds) and a pale **White Dragon** with five stars appears out of sight behind you, flies in and fights at your side. It follows you, burns what attacks you and keeps to you while you move on. After **3 game hours** (about 4 real minutes) it turns and flies off, and vanishes once no player is near enough to see it go. It also flies off if you get more than 100 m away from it or are gone for 10 seconds.

The horn then rests **24 game hours** (one game day, 30 real minutes; sleeping passes the hours too). Blowing it before then tells you how many hours are left. One dragon at a time.

Its call is a real Viking horn, sounded as four horns at once (one a little out of tune with it, one an octave below, one a fifth above that joins after half a second) in a long hall that rings on after the call. It is made from a CC0 recording by `AssetSource/Tools/make_dragon_horn_sound.py`.

What it brings to a fight:

- **An entrance**: when it reaches you it roars, the ground bursts under it, and it dives at once at the foe nearest to you.
- **Fire and frost by turns** (`Breath`): **12 flames of 25 fire**, or 12 shards of 25 frost from Moder's own ice, at most every 5 seconds before stars, against the Desert Dragon's 12 flames of 20 every 8 seconds. Its five stars raise that to about 69 per flame. Frost slows what it hits and bites on foes that shrug off fire.
- **A blast where each breath first strikes**: it bursts like one of Yagluth's meteors, or the Deep North's frost meteor for a frost breath, shaking the ground. Every enemy within 7 m takes **450 damage** before stars (about 1240 with five stars), half the breath's element and half blunt, so even fire-proof foes feel it, falling to half at the edge, and is thrown back.
- **A dive** every 20 seconds: it swoops low and fast at its foe and slams into the ground beside it, **600 damage** before stars (about 1650 with five stars) to every enemy within 10 m, which are thrown hard.
- **A terror roar** every 30 seconds while foes are near: every foe within 20 m staggers and moves 60% slower for 4 seconds (*Dragon Terror*).
- **It guards you**: whatever hits you becomes its foe for the next 10 seconds, and it dives at that one first.
- **Its blessing**: while you are within 30 m of your dragon, *Dragon's Blessing* makes you take a quarter less blunt, slash and pierce damage, and your health and stamina come back half as fast again.
- **It grows with the world**: each of Yagluth, the Queen and Fader slain gives a dragon called after it 25% more health and damage.
- A cold light shines from its body, and frost mist drifts from its mouth.
- It is **half as big again as a Desert Dragon**, about 12 m from wingtip to wingtip, and its roar is **twice as loud** and heard twice as far.
- It has **1500 health** before stars (6750 with five stars by default).
- It leaves **no ground fire** and never burns buildings, whatever `BurnsBuildings` says. Its blasts, dives and roar spare buildings too.
- It **fights bosses at your side** too; with `FightsBosses` off it leaves them to you, and they leave it alone.
- It drops nothing when it dies, and lies white where it fell.
- Nothing it does ever hurts players or tamed animals.

With default settings, a 0/1/2-star Desert Dragon has **1200/2400/3600 health**, **20/30/40 fire damage per breath flame** and **10/15/20 ground fire damage per second** before resistance and any pressure bonuses. A naturally summoned **Black Dragon** has five stars and the existing beast bonuses: **8100 health**, **60 fire damage per breath flame** and **30 ground fire damage per second**, with damage capped by the difficulty settings. Changing difficulty settings changes these values; a plain devcommand spawn does not apply the natural beast encounter bonuses.

| | Health | Attack | Weak to |
|---|---|---|---|
| **Lindorm** | 700 | Bite, 75 pierce | Fire |
| **Giant Spider** | 120 | Bite, 30 pierce + 20 poison, webs you | Blunt, fire |
| **Spider Nest** | 300 | — | — |
| **Desert Dragon** | 1200 | Fire breath, 12 flames of 20 fire; ground flames, 10 fire/sec | Frost |

The Lindorm shrugs off chop and pickaxe damage, resists pierce and poison and is immune to spirit damage. Spiders ignore chop and pickaxe damage and are immune to poison.

## The wild beasts

Five more monsters, each with its own animated model, own sounds and own loot. They are cloned from vanilla creatures, so they use the normal star system and the [Difficulty](difficulty.md) settings. Each has its own section `[Monsters.<Name>]` (see Config below), and all of them can be switched off or made rarer there. The Draugr Jarl is the exception: he never spawns in the wild.

| | Where | When | Health | Attacks | Speed (walk / run) |
|---|---|---|---|---|---|
| **Mist Stag** | Mistlands | Day and night | 900 | Charge, 80 pierce (hard knock-back); stomp, 100 blunt | 3 / 9 |
| **Nidhogg Spawn** | Mistlands | Night only | 1800 | Bite, 95 slash + 30 poison; crushing blow, 150 blunt | 2.5 / 8 |
| **Yggdrasil Warden** | Mistlands | Day and night | 3000 | Slam, 160 blunt, long reach | 1.2 / 2.5 |
| **Garm Hound** | Ashlands | Night only, in packs of 2–3 | 500 | Bite, 60 slash + 45 fire | 4 / 10 |
| **Greenland Shark** | Open sea, after **Bonemass** | Day and night | 450 | Bite, 60 pierce | swims at 6 |
| **Draugr Jarl** | The deck of a Nail flagship only | Never in the wild | 600 per level | Chop, 70 slash; sweep, 45 slash | 1.4 / 2.5 |

### Mist Stag

One of the stags that gnaw at Yggdrasil: pale as mist, with glowing eyes and huge antlers, about **2.6 m** to the antler tips. It grazes until you come close (it notices you from about 14 m), then **charges**, rearing up to stomp if you stay near. Spirit damage hurts it most; pierce and frost damage barely scratch it. It drops Mist Stag Meat, which you roast on the iron cooking station into **Roast Mist Stag**.

### Nidhogg Spawn

A dark scaled brood of the dragon that gnaws Yggdrasil's roots: **about 5 m long**, with red eyes. It hunts at night. The first time it sees you it **roars**: everyone near sees it, hears the alert and the beast stands still for about two seconds before it comes for you. Its bite poisons, and its heavy blow throws you back. Slash and pierce barely hurt it, poison does nothing, and fire hurts it most. Its drop, Brood Scale, becomes a **Brood Scale Charm** (see Loot).

### Yggdrasil Warden

A tree grown from a fallen branch of Yggdrasil, about **4 m tall**, with glowing runes. It **stands dormant like a dead tree** until a player comes within **8 m** (`WakeRange`), or until something hurts it. Then it wakes, and slams down on whatever is in reach. It is slow and very tough, resists pierce and frost, is immune to poison, and is **very weak to fire**. It drops Living Heartwood.

### Garm Hound

Whelps of Garm, the hound of Hel: charcoal-black, with yellow eyes, about 1.5 m at the head and **2.6 m long**. They hunt in **packs of 2–3** at night in the Ashlands and circle their prey before they bite. Their bite is slash and **fire**, so it sets you burning. They are immune to fire and weak to frost and spirit. They drop Garm Fangs.

### Greenland Shark

A slow, grey, dead-eyed shark about **5 m long**, in the open sea, once **Bonemass** is slain (`RequiredKey`, the ocean counts as Swamp). It hunts swimmers and bites boats like the serpents do. It is immune to frost and weak to lightning. Its meat, put in a fermenter, turns into **Hákarl**.

### Draugr Jarl

A huge undead viking with a fur cloak, glowing blue-green eyes and a great axe, about **2.3 m tall**, who sailed with Naglfar. He **never spawns in the wild**: he only appears as the leader of the crew of a Nail Ship **flagship**, where he keeps a post near the mast, leaps across last of all and does not flee (see [Fleets](sea-battles.md#fleets) and [The draugr jarl](sea-battles.md#the-draugr-jarl)). He is of the undead faction like his crew, and he has his own raspy war cries and heavy steps.

- **Attacks:** an overhead **chop** of 70 slash damage (reach 3 m, every 3 seconds, a hard knock-back) and a wide **sweep** of 45 slash damage (reach 3.2 m, a 140° arc, every 5 seconds).
- **Strength:** 600 health and the damage above are **per level**, and his level grows with the world (`[SeaBattles.Fleets] JarlLevels`): level 3 (two stars) after Moder, 4 after Yagluth, 5 after the Queen and 6 after Fader. That is about **1800 / 2400 / 3000 / 3600 health**, and his attacks hit 2, 2.5, 3 and 3.5 times the numbers above, before pressure bonuses (see [Difficulty](difficulty.md)). Each star makes him 8% bigger up to two stars, so about 2.7 m from level 3; higher stars also use the difficulty size settings.
- **Resistances:** resists pierce, is weak to fire and spirit, immune to poison, and ignores chop and pickaxe damage.
- **Loot:** only his trophy, the **Draugr Jarl Trophy**, always (100%). His body sinks after 10 seconds.
- Slaying him is a [saga](saga.md) deed (a listed creature).

Spawn them with the console: `spawn WhiteHilt_MistStag`, `WhiteHilt_NidhoggSpawn`, `WhiteHilt_YggdrasilWarden`, `WhiteHilt_GarmHound`, `WhiteHilt_GreenlandShark` and `WhiteHilt_DraugrJarl` (with `devcommands`).

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
| **Desert Dragon Scale** ×4–6 | Desert Dragon | Always | Dragonscale Broth, Dragonfire Arrows (below) |
| **Surtling Core** | Desert Dragon | 50% | Vanilla recipes |
| **Desert Dragon Trophy** | Desert Dragon | 35% | Your wall |
| **Mist Stag Meat** ×2–3 | Mist Stag | Always | Roast Mist Stag (below) |
| **Mist Stag Trophy** | Mist Stag | 10% | Your wall |
| **Brood Scale** ×2–3 | Nidhogg Spawn | Always | Brood Scale Charm (below) |
| **Nidhogg Spawn Trophy** | Nidhogg Spawn | 10% | Your wall |
| **Living Heartwood** ×1–2 | Yggdrasil Warden | Always | Heartwood Draught (below) |
| **Yggdrasil Warden Trophy** | Yggdrasil Warden | 10% | Your wall |
| **Garm Fang** ×1–2 | Garm Hound | Always | Garm Fang Arrow (below) |
| **Garm Hound Trophy** | Garm Hound | 10% | Your wall |
| **Shark Meat** ×2–4 | Greenland Shark | Always | Hákarl (below) |
| **Greenland Shark Trophy** | Greenland Shark | 10% | Your wall |
| **Draugr Jarl Trophy** | Draugr Jarl | Always | Your wall |

### Made from Desert Dragon Scales

| Item | Description | Crafting Station | Requirements |
|------|-------------|------------------|--------------|
| **Dragonscale Broth** | Plains: 50 health, 30 stamina, 40 min. Buff: fire damage taken is halved | Stone Pot level 2 ([Foraging](foraging.md)) | Desert Dragon Scale ×1, Onion ×2, Barley ×2 |
| **Dragonfire Arrow** ×20 | Fire arrows with 30 pierce and 60 fire damage (the vanilla Fire Arrow has 11 and 22). Set in `[Gear.WhiteHiltDragonfireArrow]` `Pierce` and `Fire` | Workbench level 2 | Wood ×8, Feathers ×2, Desert Dragon Scale ×1 |

### Made from the wild beasts' loot

| Item | Description | Where | Requirements |
|------|-------------|-------|--------------|
| **Roast Mist Stag** | Mistlands: 66 health, 22 stamina, 25 min, regen 5 ([Foraging](foraging.md)) | Iron cooking station, 30 s (`RoastSeconds`) | Mist Stag Meat ×1 |
| **Hákarl** | Swamp: 46 health, 40 stamina, 60 min, regen 3 ([Foraging](foraging.md)) | Fermenter, **2** per Shark Meat (`HakarlYield`) | Shark Meat ×1 |
| **Brood Scale Charm** | An accessory: **+8 armor** and **poison Resistant** while worn (`CharmArmor`, `CharmPoison`; [White Hilt gear](equipment.md)) | Galdr Table level 1 | Brood Scale ×4, Black Marble ×2, Eitr ×2 |
| **Garm Fang Arrow** ×20 | Charred arrows with 82 pierce (as much as the vanilla Charred Arrow) and 25 fire damage (`ArrowPierce`, `ArrowFire`) | Workbench level 2 | Blackwood ×8, Garm Fang ×2, Feathers ×2 |
| **Heartwood Draught** | Mistlands mead: health regenerates 100% faster for 10 min (`HealthRegenMultiplier` 2; [Foraging & food](foraging.md)) | Cauldron (base), then the fermenter, 6 per base | Honey ×10, Living Heartwood ×2, Sap ×5 |

## Config

Sections `[Monsters.MistStag]`, `[Monsters.NidhoggSpawn]`, `[Monsters.YggdrasilWarden]`, `[Monsters.GarmHound]` and `[Monsters.GreenlandShark]` (admin only, synced from the server). They share the same keys:

| Setting | Default | What it does |
|---|---|---|
| `Enabled` | true | The monster spawns |
| `Health` | 900 / 1800 / 3000 / 500 / 450 | Health (after a restart) |
| `Damage` | 80 / 95 / 160 / 60 / 60 | Damage of the first attack: the stag's charge, the Spawn's and the Garm Hound's bite, the Warden's slam, the shark's bite (after a restart) |
| `SecondDamage` | 100 / 150 | Blunt damage of the second attack, for the stag (stomp) and the Spawn (crushing blow) (after a restart) |
| `ExtraDamage` | 30 / 45 | Poison (Spawn) or fire (Garm Hound) added to the bite (after a restart) |
| `Scale` | 1 | Size (1 = the sizes above; after a restart) |
| `StarScale` | 0.15 | Visual growth per star up to two stars (+15% / +30%; after a restart) |
| `SpawnChance` | 15 / 10 / 6 / 15 / 8 | Percent chance per spawn check; 0 = none |
| `SpawnSeconds` | 300 / 400 / 600 / 250 / 300 | Seconds between two spawn checks |
| `MaxSpawned` | 1 / 1 / 1 / 3 / 1 | Most around a player |
| `GroupMin` / `GroupMax` | 2 / 3 | Garm Hound only: pack size |
| `TrophyChance` | 10 | Percent chance of its trophy (after a restart) |
| `Sounds` | true | Own sounds for idle, alert, attack, injury and death (after a restart) |
| `WakeRange` | 8 | Yggdrasil Warden only: metres at which a player wakes it (after a restart) |
| `RequiredKey` | defeated_bonemass | Greenland Shark only: global key needed first; empty for none |

Section `[Monsters.DraugrJarl]` (admin only, synced from the server) has no `Enabled`, spawn or `RequiredKey` settings, as he never spawns in the wild:

| Setting | Default | What it does |
|---|---|---|
| `Health` | 600 | Health per level (after a restart) |
| `Damage` | 70 | Slash damage of the overhead chop, per level (after a restart) |
| `SecondDamage` | 45 | Slash damage of the sideways sweep, per level (after a restart) |
| `Scale` | 1 | Size (1 = about 2.3 m tall; after a restart) |
| `StarScale` | 0.08 | Visual growth per star up to two stars (+8% / +16%; after a restart) |
| `TrophyChance` | 100 | Percent chance of his trophy (after a restart) |

His level, and the crew around him, are set in `[SeaBattles.Fleets]`, see [Sea battles & raids](sea-battles.md#config).

Section `[Monsters.Loot]`:

| Setting | Default | What it does |
|---|---|---|
| `RoastSeconds` | 30 | Seconds Mist Stag Meat takes on the iron cooking station (after a restart) |
| `HakarlYield` | 2 | Hákarl from one Shark Meat (after a restart) |
| `CharmArmor` | 8 | Armor of the Brood Scale Charm |
| `CharmPoison` | Resistant | How the charm changes poison damage taken |
| `ArrowPierce` / `ArrowFire` | 82 / 25 | Pierce and fire damage of a Garm Fang Arrow |

Section `[Meads.HeartwoodDraught]` has `DurationMinutes` and `HealthRegenMultiplier` (2), as the other meads do.

Section `[Lindorm]` (admin only, synced from the server):

| Setting | Default | What it does |
|---|---|---|
| `Enabled` | true | The Lindorm can come at all |
| `RequiredKey` | defeated_eikthyr | Global key needed first; empty for none |
| `Biomes` | BlackForest, Swamp | Biomes where it lurks |
| `NightOnly` | true | Only at night |
| `ChancePerMinute` | 3 | Percent per minute while every condition holds |
| `CooldownMinutes` | 30 | Real minutes before it can come for the same player again |
| `Health` / `Damage` | 700 / 75 | Health, and pierce damage of its bite |
| `ProgressionStars` | defeated_gdking:1, defeated_bonemass:2, defeated_dragon:3, defeated_goblinking:4, defeated_queen:5 | Minimum stars after each global boss key, 0–5; highest unlocked value wins. Empty: no boss scaling. Applies to new encounters |
| `Scale` | 1.6 | Size (1 = about 4 m long; default about 6.4 m; after a restart) |
| `StarScale` | 0.15 | Visual growth per star up to two stars (+15% / +30%; after a restart) |
| `Sounds` | true | Own growls and rasps (after a restart) |
| `GiveUpSeconds` | 25 | Seconds without prey in sight before it burrows away |
| `TrophyChance` | 15 | Percent chance of its trophy (after a restart) |

On upgrade to 0.72.2, the previous Lindorm defaults (`Scale` 1.3, `Damage` 55) move to the new defaults once. Other configured values are preserved.

Section `[Giant Spider]`:

| Setting | Default | What it does |
|---|---|---|
| `Enabled` | true | Nests and lone spiders in the Black Forest |
| `Health` | 120 | Health of a spider |
| `Damage` / `Poison` | 30 / 20 | Pierce and poison damage of its bite |
| `WebSeconds` | 3 | Seconds a bite slows you; 0 = no slow |
| `Scale` | 1.5 | Size (1 = about 1.6 m across; default about 2.4 m; after a restart) |
| `StarScale` | 0.15 | Visual growth per star up to two stars (+15% / +30%; after a restart) |
| `Sounds` | true | Own spider clicks, rasps and hisses (after a restart) |
| `NestChancePerZone` | 0.15 | Chance of a nest in each Black Forest zone |
| `NestHealth` | 300 | Health of a nest |
| `NestMaxNear` | 3 | Spiders a nest keeps around it |
| `TrophyChance` | 10 | Percent chance of a spider's trophy (after a restart) |
| `NestSpawnSeconds` | 20 | Seconds between two spiders from a nest (after a restart) |
| `NestLevelUpChance` | 10 | Percent chance a spider from a nest gets a star (after a restart) |
| `NightSpawnChance` | 20 | Percent chance per spawn check of a lone spider at night; 0 = none |
| `NightSpawnSeconds` | 240 | Seconds between two spawn checks for lone spiders |
| `NightSpawnMax` | 1 | Lone spiders around a player at most |

On upgrade to 0.72.1, the previous spider defaults (`Scale` 1, `Damage` 18, `Poison` 15) move to the new defaults once. Other configured values are preserved.

Section `[Desert Dragon]`:

| Setting | Default | What it does |
|---|---|---|
| `Enabled` | true | Desert Dragons over the Plains |
| `RequiredKey` | defeated_dragon | Global key needed first; empty for none |
| `SpawnChance` | 5 | Percent chance per spawn check; 0 = none |
| `SpawnSeconds` | 450 | Seconds between two spawn checks |
| `SpawnMax` | 1 | Desert Dragons around a player at most |
| `Health` | 1200 | Health (after a restart) |
| `FireDamage` / `Flames` | 20 / 12 | Fire damage of each flame, and flames in one breath (after a restart) |
| `BreathSeconds` | 8 | Seconds between two breaths at most (after a restart) |
| `BreathRange` | 25 | Metres it breathes fire from (after a restart) |
| `BreathWidth` | 3 | Metres wide the fire gets halfway through its range, about at the ground (after a restart) |
| `FlySpeed` | 11 | Metres per second when it chases you (after a restart) |
| `FlyHeightMin` / `FlyHeightMax` | 5 / 12 | Metres above the ground it flies at (after a restart) |
| `Scale` | 1 | Size (1 = about 8 m from wingtip to wingtip; after a restart) |
| `Sounds` | true | Deep roars and growls, also on Black Dragons (after a restart) |
| `TrophyChance` | 35 | Percent chance of its trophy (after a restart) |
| `ScalesMin` / `ScalesMax` | 4 / 6 | Desert Dragon Scales it drops (after a restart) |
| `BurnsBuildings` | false | Its fire also damages the buildings it hits |
| `GroundFire` | true | Dry terrain impacts leave short-lived flames; no spreading or terrain changes |
| `GroundDamage` | 10 | Base fire damage per second before resistance, scaled like the final breath projectile; overlapping fields do not stack |
| `GroundSeconds` | 6 | Seconds a patch lasts; nearby impacts refresh it |
| `GroundRadius` | 1.5 | Metres around each patch; nearby impacts merge |
| `GroundTickSeconds` | 1 | Seconds between damage checks; damage per check scales with the interval |
| `GroundPerBreath` | 3 | Maximum new patches per breath |
| `GroundMax` | 6 | Maximum active patches per dragon, including earlier breaths |

On upgrade to 0.77.0, previous defaults (`Health` 800, `FireDamage` 15) move to 1200 and 20 once. The older health default 500 is also migrated. Other configured values are preserved. New creature prefabs need a restart; update both server and clients for ground fire. Its particle appearance, multiplayer ownership and performance still need in-game testing.

On upgrade to 0.117.0, `SpawnChance`, `SpawnSeconds` and `TrophyChance` move to the new defaults (5, 450 and 35) once: about a third as many dragons, and their trophy more than three times as often. Other configured values are preserved.

Section `[DragonHorn]`:

| Setting | Default | What it does |
|---|---|---|
| `Enabled` | true | The Bog Witch sells the Dragon Horn, and it calls a White Dragon |
| `Price` | 3000 | Coins it costs |
| `RequiredKey` | defeated_dragon | Global key needed before it is sold and before it calls (Moder); empty for none |
| `CooldownHours` | 24 | Game hours the horn rests after a call |
| `StayHours` | 3 | Game hours the dragon fights at your side |
| `BlowSeconds` | 3 | Seconds of blowing before the dragon is called |
| `SoundRange` | 150 | Metres the horn is heard |
| `Stars` | 5 | Stars of the dragon |
| `Health` | 1500 | Health before stars (after a restart) |
| `FireDamage` / `Flames` | 25 / 12 | Fire damage of each flame before stars, and flames in one breath (after a restart) |
| `BreathSeconds` | 5 | Seconds between two breaths at most (after a restart) |
| `BlastDamage` | 450 | Damage of the blast where each breath first strikes, half the breath's element and half blunt, before stars; half at the edge; 0 = no blast |
| `BlastRadius` | 7 | Metres the blast reaches |
| `BlastPush` | 80 | How hard the blast throws enemies back |
| `Size` | 1.5 | Size as a multiple of a Desert Dragon's (after a restart) |
| `RoarLoudness` | 2 | How much louder it roars than a Desert Dragon, and how many times further it is heard; 1 = as loud (after a restart) |
| `Breath` | Both | Fire, Frost, or Both by turns (after a restart) |
| `FrostDamage` | 25 | Frost damage of each shard of the frost breath before stars (after a restart) |
| `DiveSeconds` | 20 | Seconds between two dives |
| `DiveDamage` | 600 | Damage of the slam at the end of a dive, half the breath's element and half blunt, before stars; half at the edge; 0 = no dives |
| `DiveRadius` / `DivePush` | 10 / 150 | Metres the slam reaches, and how hard it throws |
| `DiveSpeed` | 1.8 | How many times faster than its normal flight it dives |
| `DiveRange` | 40 | Metres away a foe may be for it to dive at it |
| `RoarSeconds` | 30 | Seconds between two terror roars while foes are near; 0 = none (it still roars as it arrives) |
| `RoarRadius` | 20 | Metres around it the roar staggers and slows foes |
| `RoarSlow` / `RoarSlowSeconds` | 0.6 / 4 | How much slower the roar leaves foes, and for how long |
| `GuardSeconds` | 10 | Seconds it goes after whatever hit you; 0 = it picks its own foes |
| `BlessingRange` | 30 | You are blessed within this many metres of it; 0 = no blessing |
| `BlessingResistance` | SlightlyResistant | How you take blunt, slash and pierce damage while blessed |
| `BlessingRegen` | 1.5 | Health and stamina regeneration while blessed |
| `PowerKeys` | defeated_goblinking, defeated_queen, defeated_fader | Global keys that each make a dragon called afterwards stronger |
| `PowerPerKey` | 0.25 | Extra health and damage for each of them |
| `Aura` | true | A cold light and frost mist around it (after a restart) |
| `FightsBosses` | true | It also fights bosses; off, it leaves them to you |
| `ArriveDistance` | 80 | Metres behind you it appears before it flies in |
| `LeaveDistance` | 100 | Get further from it than this and it flies off |
| `VanishDistance` | 90 | On its way off, it vanishes once no player is this close |
| `FlightSeconds` | 30 | Longest time it takes to fly in or off |

On upgrade to 0.123.0, the previous defaults (`Health` 600, `FireDamage` 8, `Flames` 8, `BreathSeconds` 10, `FightsBosses` false) move to the new ones once. Other configured values are preserved.

## Console commands

- `whitehilt_lindorm` shows every condition where you are, and whether it holds.
- `whitehilt_lindorm summon` (admins) lets the Lindorm break out near you now.
- With devcommands: `spawn WhiteHilt_GiantSpider`, `spawn WhiteHilt_SpiderNest`, `spawn WhiteHilt_Lindorm` and `spawn WhiteHilt_DesertDragon`.

## Sounds

Every monster of the mod has its own sounds, made with [ElevenLabs](https://elevenlabs.io): idle, alert, attack, injury and death, three takes of each that the game picks between, so the same roar is not heard twice in a row. That goes for the monsters above, the Draugr Jarl, the White Dragon, the [Black Beasts](difficulty.md) (darker, with an eerie echo, so you know one is near) and the [plagues](trading-posts.md#plagues).

- The big ones walk heavily: the Yggdrasil Warden's steps creak like timber, and the Draugr Jarl, the Black Troll, the Black Stone Golem, the Black Abomination, the Mire Mother, Gnawtooth and Icemaw thud.
- The dragons beat their wings while they fly.
- The Lindorm's ground rumbles and bursts as it comes up, the sea surges as the Kraken rises, and a dread sounds when a Black Beast or a plague comes.
- A spider nest rustles and skitters, a Nidhogg Spawn breathes and hisses as it lurks, and a Yggdrasil Warden creaks and breathes as it stands like a tree.

The monsters' `Sounds` settings switch their own sounds off (after a restart). The loops follow `AmbienceVolume` and the rest `EffectsVolume` in `[Sound]`.

