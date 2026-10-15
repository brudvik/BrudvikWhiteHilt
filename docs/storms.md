# ⛈️ Great storms

[← Back to the README](../README.MD)

## ⛈️ GREAT STORMS

Now and then a great storm comes. No clothes, food or mead keep it out: out in it you tire, freeze, go half blind
and in the end die. You are warned first, in time to get home, find shelter or build one. The Meadows never have a
storm.

### The warning
- The sky darkens and the wind picks up, distant thunder rolls and a horn sounds far away.
- *A great storm is coming! Find shelter within 3:00* shows in the middle of the screen, and the clock counts down
  with a storm icon.
- After 3 minutes (`WarningMinutes`) the storm breaks and rages for 3 to 6 minutes. The clock shows the time left.
- About one storm comes every two and a half game days (`StormsPerDay` 0.4), none in the first 5 days of a world and
  never two within 25 minutes of each other or during a blood moon.

### Foretold

The storm is decided 5 minutes (`ForecastMinutes`) before the warning. In that time only the
[weather forecast](navigation.md#weather-forecast) knows: near a map table or a Cartographer's Desk its first row reads
*Great storm in 8:00* with the storm icon, and aboard a ship with a Navigator's Table everyone is told *The Navigator's
Table foretells a great storm in 8:00* and the ship's bell rings. The bell rings again at the warning. While the storm
rages the forecast shows the time left.

### Out in it

An exposure meter, shown as a status effect with its percentage, climbs while you are out in the storm:

| Exposure | What happens |
|----------|--------------|
| 20% | Stamina comes back at half the pace |
| 40% | Stamina comes back slowly (a quarter) |
| 60% | You freeze (or burn), the view closes in from the edges and the fog thickens; stamina hardly comes back |
| 80% | You lose health, faster and faster until you die |

In the open, the meter is full after about 4 minutes (`MinutesToFull`). A storm of the worst biomes is faster.

### The storm of each biome

| Biome | Storm | Pace | Also |
|-------|-------|------|------|
| Black Forest, Swamp | Thunder, lashing rain and wind | 1× | Close trees give a little shelter |
| Sea | Rain, fog, cold and wind at full strength | 1.25× | |
| Plains | Sandstorm | 1× | You do not freeze or lose health, but you can hardly see from the start and you tire |
| Mountain | Blizzard | 1.25× | You move at 60% speed in the open |
| Mistlands | Dense, bitter cold fog | 1.5× | |
| Ashlands | Firestorm | 1.5× | You catch fire, and lose health already from 50%. Only a stone roof shelters: wood burns |
| Deep North | Snow, snowstorm and howling wind | 2× | You move at half speed in the open |

### Shelter

| Where | Exposure |
|-------|----------|
| A dungeon, crypt, cave or mine, or under a trading post's dome | Falls |
| Under a roof by a burning fire | Falls; from full to nothing in 1.5 minutes (`MinutesToRecover`) |
| Under a roof without a fire | Holds |
| In the lee of a rock, a cliff or a wall, upwind | Climbs at half the pace |
| Among close trees in the forest storm | Climbs at three quarters of the pace |
| In the open | Climbs |

A *krypinn* needs the two things the game already counts as shelter and warmth: a roof and walls enough to give the
**Sheltered** status, and a fire close by. A roofed ship counts like any roofed shelter. When the storm passes the meter
falls everywhere.

### The Storm Lean-to (vindskjul)

A lean-to of logs on two posts, open to the front, its roof sloping to the ground behind. It costs 12 wood and is built
with the hammer anywhere, without a workbench, so it can go up in the minutes of the warning. Under its front half you
are under a roof; light a campfire in front of the opening and the meter falls. Turn the closed back to the wind. It is
wood, so it does not shelter you in the Ashlands' firestorm.

### Lightning

In the storms that thunder (the forest storm, the sea and the Mistlands) lightning strikes near every player about every
30 seconds (`LightningSeconds`):
- Most often it hits a tree 12 to 45 m away, and the tree comes down.
- Else it hits the ground. Within 3 m it hurts.
- Now and then (15%, `PlayerStrikeChance`) it seeks out a player in the open: twice as often with metal in hand (iron,
  bronze, silver, black metal, flametal), half as much again on a ship. *Your hair stands on end. Move!* and a crackle
  warn you 1.5 seconds before; whoever has moved 3 m away by then is not hit.
- A direct hit does lightning damage of 40% of your maximum health (`LightningDamage`), less with lightning resistance,
  sets you alight briefly and throws you back.
- Each strike is a jagged bolt from the clouds with a burst of light, a flash of the screen and its own crack and boom.

### Falling trees

In every storm but the sandstorm and the firestorm, gusts fell a tree 10 to 35 m from a player about every 40 seconds
(`TreeFallSeconds`). It creaks, shakes and cracks first, then falls with the wind. Mind the trunk; the timber is yours.
Trees inside a base (near a workbench or fire) are never felled, and each tree is felled by the player closest to it
only, so friends standing together do not fell twice as many.

### Weathering (Herdet)

A new skill. It rises with every second out in a raging storm (in the open, in the lee or among trees) and by a good
step for every storm you live through outside with the meter past 40%. It slows the meter: at level 100 it climbs 30%
more slowly (`SkillReduction`). It does not change shelter or recovery.

### What the storm leaves behind

When a storm passes, everyone who was out in it finds 4 things (`FindsCount`) lying about 5 to 22 m around them, from
the storm's biome:

| Storm | Finds |
|-------|-------|
| Black Forest, Swamp | Resin, amber, feathers, now and then an amber pearl or a thunderstone |
| Sea | Amber pearls, amber, chitin, serpent scales (on the nearest dry land, or on deck) |
| Plains | Coins, amber, rubies, now and then a silver necklace the sand laid bare |
| Mountain, Deep North | Crystals, frost glands, obsidian |
| Mistlands | Soft tissue, black marble, carapace, now and then a wisp |
| Ashlands | Sulfur stone, grausten, coal, now and then a molten core |

Lightning leaves a **thunderstone** where it struck one time in seven.

### The Husvette and your dog

The [Husvette](base.md#the-husvette-speaks) calls you in when a storm is coming and you are within 50 m of the house
(*A storm is coming! Inside with you, and light the fire!*), and once you are inside while it rages, he is glad of the
walls (*Listen to it howl out there. Good thing we have walls.*). Your [dog](dog.md) whines and looks toward home at the
warning, and whines now and then while it is out in the storm.

### Heard, not only seen

Every storm has a sound of its own: rain and wind in the forest, waves and a creaking hull at sea, hissing sand, a
howling blizzard, moaning fog and roaring fire. Thunder cracks overhead with a flash of lightning in the forest storm,
at sea and in the Mistlands. Under a roof the storm is heard through the walls; in a cave hardly at all. The sounds are
made with ElevenLabs (`AssetSource/Audio/audio.json`).

### Commands
- `whitehilt_storm` or `whitehilt_storm status` shows the storm, the time left, and your cover and exposure.
- `whitehilt_storm warn` (admin) warns of a storm now; `start` lets one break at once; `stop` ends it.

### Settings

Section `Storms`. Everything but `Volume` is admin-only and synced from the server.

| Setting | Default | What it does |
|---------|---------|--------------|
| `Enabled` | true | Storms come at all |
| `StormsPerDay` | 0.4 | Storms per game day on average (a day is about 30 minutes) |
| `GraceDays` | 5 | World days at the start without storms |
| `MinutesBetween` | 25 | Minutes of calm after a storm before the next can come |
| `WarningMinutes` | 3 | Minutes of warning before a storm breaks |
| `MinMinutes`, `MaxMinutes` | 3, 6 | How long a storm rages |
| `Biomes` | all but the Meadows | Biomes that have storms, comma separated |
| `MinutesToFull` | 4 | Minutes in the open from no exposure to full, at pace 1× |
| `MinutesToRecover` | 1.5 | Minutes in shelter from full exposure to none |
| `DamageMultiplier` | 1 | Multiplier of the health lost |
| `CanKill` | true | Off, a storm leaves you at 1 health |
| `VisionStrength` | 1 | How hard the view closes in, 0 to 1 |
| `LeeShelters` | true | Rocks, cliffs, walls and close trees slow the meter |
| `NotWithBloodMoon` | true | No storm during a blood moon |
| `Lightning` | true | Lightning strikes in the forest storm, at sea and in the Mistlands |
| `LightningSeconds` | 30 | Seconds between strikes near each player on average |
| `PlayerStrikeChance` | 15 | Chance in percent that a strike seeks out a player in the open |
| `LightningDamage` | 0.4 | Lightning damage of a direct hit as a share of maximum health |
| `FallingTrees` | true | Gusts fell trees near players |
| `TreeFallSeconds` | 40 | Seconds between felled trees near each player on average |
| `ForecastMinutes` | 5 | Minutes before the warning that the forecast shows the storm; 0 not foretold |
| `Finds` | true | A storm leaves finds around those who were out in it |
| `FindsCount` | 4 | Finds around each player |
| `SkillReduction` | 0.3 | How much more slowly the meter climbs at Weathering 100 |
| `Volume` | 1 | Volume of the storm's wind and thunder (your own setting) |

### How it works

The server decides when a storm comes (5 minutes before the warning, so the forecast can show it) and sends the phase and when it ends to every client, in the world's time, so
everyone counts down alike. Each client works out its own player's exposure from the biome they are in and their
shelter: `Player.InInterior`, `Player.InShelter` (or under a lean-to's roof) and the game's near-fire timer, rays upwind for the lee, the trees
within 8 m, and the material of the piece overhead in the Ashlands. The health is lost as a hit that no armour or
resistance changes.

The storm's weather is a copy of the game's nearest one (the thunderstorm, the snowstorm, the Ashlands' storm or a clear
Plains sky for the sandstorm) with thicker fog, harder wind and colours of its own, added to the game's weathers. The
game is told to use it after its own forced weathers, so a dungeon, a raid or a boss still has its own, and the sky
blends into and out of the storm as into any other weather. The warning has a darker copy without rain or snow.

Lightning and falling trees are worked out on each client around its own player. A tree is felled with one hit as hard
as it is sound, from the direction of the wind or the strike, so the game drops it as it would for a woodcutter and
every player sees it fall.
