# ⛈️ Great storms

[← Back to the README](../README.MD)

## ⛈️ GREAT STORMS

Now and then a great storm comes. No clothes, food or mead keep it out: out in it you tire, freeze, go half blind
and in the end die. You are warned first, in time to get home, find shelter or build one. The Meadows never have a
storm, but for Ragnarok.

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

The [Gift of Odin and the Gift of Freya](potions.md) wane with the meter: half their healing and stamina from 20%, a
quarter from 40%, and nothing once you freeze at 60%.

### The eye of the storm

A storm comes in two waves with a still eye between them, somewhere near the middle of it:

- *The wind dies: the eye of the storm. It returns in 1:00, worse than before!* For 60 seconds (`EyeSeconds`) the
  rain and snow stop and the wind drops to what it was while the storm gathered. Out in the open your meter holds where
  it is; it falls only under a roof by a fire, in a cave or a dungeon. Nothing hurts you, no lightning strikes and no
  tree falls. The clock counts down to the storm's return. Make for shelter.
- *The storm returns, fiercer!* The last wave drives the meter on half as fast again (`LastWaveFury`), and lightning,
  thunder and falling trees come that much more often.

The eye adds its time to the storm. A storm shorter than 2 minutes has no eye.

### Ragnarok

Once a boss has fallen, now and then the storm is **Ragnarok**, the worst of all. The first storm after a new boss
falls is always Ragnarok, and any other is one time in twenty (`RagnarokChance`).

- It is foretold and warned of twice as early: *Ragnarok! The worst storm of all is coming, over the Meadows too. Find
  shelter within 6:00!* The clock and the forecast name it.
- It rages half as long again and drives the meter half as hard again (`RagnarokFury`), on top of the waves: its last
  wave is more than twice an ordinary first one.
- It reaches the Meadows, with the forest's storm, and lightning strikes in every storm, the sandstorm, the blizzard
  and the firestorm too. The view closes in further.
- Those who were out in it find twice as much after, and some of it rare: thunderstones, rubies, amber pearls, silver
  necklaces, surtling cores and now and then Ymir flesh.

Admins can call it with `whitehilt_storm ragnarok`.

### The storm remembers you

Each storm you live through out in it, with the meter past 40%, the storm remembers: *The storm remembers you. It will
come harder for you next time.* For every storm it remembers your meter climbs 5% faster (`GrudgeStep`), up to ten
storms and 50%. Each storm you sit out, the meter never past 20%, it forgets you a little. In return it leaves you one
more find for every two storms it remembers. The exposure's tooltip shows how much faster it is for you, and the
grudge stays with your character. Turn it off with `Grudge`.

### Visions in the storm

Deep in the storm you hear and see things. Out in it with the meter past 40%, every 14 to 35 seconds (twice as often
at the top of the meter, more often in Ragnarok), something comes:

- footsteps crunching right behind you, that stop;
- whispers at your ear;
- a voice calling from the fog: *Over here...*, *Help me... I cannot see...*, *You will not make it home.*;
- once you freeze, a dark figure standing off to one side of where you look, 24 to 34 m away, facing you. It is gone
  when you look straight at it or come close.

None of it is real, none of it can hurt you, and no one else hears or sees it. It is your own setting (`Visions`). The
voice and sounds are made with ElevenLabs.

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

### The Storm Bar (stormbom)

A heavy beam resting in the forks of two posts, built at the workbench from 8 fine wood, 4 round logs and 6 bronze
nails, to stand inside by the door. Where the comfort is 10 or more (`StormBarComfort`), counted as for resting where
the bar stands, put it across while a storm is coming or raging: every door and window within 20 m (`StormBarRadius`)
slams shut and is barred. Gates, and doors that need a key, are left alone.

- No one opens a barred door or window, inside or out, nor can the household; no gust tears them open.
- Whoever tries pounds on the door instead: everyone near hears the blows, and those within the bar's reach are told
  who stands outside. Someone inside must go to the bar and lift it.
- The bar lifts by itself when the storm has passed, so no one is barred out for good.

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

### Wind, hail and the lost course

- **Fires go out.** A fire with no roof over it (a campfire, a hearth, a brazier, a torch on a wall) goes out in a
  raging storm and must be fed again. A torch in your hand blows out in the open, in the lee and among trees: it goes
  back in the bag. Not in the firestorm. A krypinn needs its fire under the roof.
- **Doors tear open.** Gusts tear shut doors open near you, now and then, through the game's own door so everyone sees
  it. While a door within 8 m of you stands open the storm comes in: the room shelters you no better than the lee of a
  wall, and its fire does not help, until the door is shut. A locked door holds; a self-closing door shuts again.
- **Gusts.** Out in the open, hard gusts push you along with the wind, and one in five knocks you off your feet (more in
  Ragnarok). Not at the helm or seated.
- **Hail.** In the forest storm, at sea and in the blizzard, a hail shower comes every minute or so and beats down for
  8 to 15 seconds: blunt hits of 1.5% of your health a second in the open or in the lee, which a helmet and armour
  soften. Trees, a roof or a cave keep it off. When it starts, young crops with no roof over them may break.
- **The Kraken wakes.** At sea in a raging storm the Kraken rises by day or night, in fog or not and in any wind, four
  times as often (`KrakenFactor`); in Ragnarok ten times as often, and at least every other minute. Its cooldown holds.
- **Lost at sea.** At sea in a raging storm the storm hides sun and stars: the compass and the minimap are gone, the map
  shows neither you nor your ship and opens somewhere else. A [Navigator's Table](ships.md) aboard keeps the course.

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
- `whitehilt_storm` or `whitehilt_storm status` shows the storm, its wave, the time left, and your cover, exposure,
  fury and grudge.
- `whitehilt_storm warn` (admin) warns of a storm now; `start` lets one break at once; `ragnarok` lets Ragnarok break
  at once; `stop` ends it.

### Settings

Section `Storms`. Everything but `Volume` and `Visions` is admin-only and synced from the server.

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
| `FiresGoOut` | true | Fires with no roof over them and torches in the open go out |
| `DoorsBlowOpen` | true | Gusts tear doors open, and an open door lets the storm in |
| `StormBar` | true | The Storm Bar can be built, to bar a house's doors and windows in a storm |
| `StormBarComfort` | 10 | The comfort a Storm Bar needs where it stands before it can be put across |
| `StormBarRadius` | 20 | Metres from a Storm Bar within which it bars doors and windows |
| `Gusts` | true | Gusts push players in the open and now and then knock them down |
| `Hail` | true | Hail showers in the forest storm, at sea and in the blizzard |
| `KrakenFactor` | 4 | How many times more likely the Kraken rises at sea in a storm; 1 changes nothing |
| `LostAtSea` | true | No compass, minimap or marker at sea in a storm without a Navigator's Table |
| `ForecastMinutes` | 5 | Minutes before the warning that the forecast shows the storm; 0 not foretold |
| `Finds` | true | A storm leaves finds around those who were out in it |
| `FindsCount` | 4 | Finds around each player |
| `SkillReduction` | 0.3 | How much more slowly the meter climbs at Weathering 100 |
| `Volume` | 1 | Volume of the storm's wind and thunder (your own setting) |
| `EyeSeconds` | 60 | Seconds of the still eye in the middle of a storm; 0 no eye |
| `LastWaveFury` | 1.5 | How much faster the meter climbs in the last wave, after the eye |
| `Ragnarok` | true | Ragnarok comes once a boss has fallen |
| `RagnarokChance` | 5 | Chance in percent that another storm is Ragnarok |
| `RagnarokFury` | 1.5 | How much faster the meter climbs in Ragnarok, on top of the wave |
| `Grudge` | true | The storm remembers who braves it |
| `GrudgeStep` | 0.05 | How much faster the meter climbs per storm remembered, up to 10 |
| `Visions` | true | Footsteps, whispers, a voice and a figure deep in the storm (your own setting) |

### How it works

The server decides when a storm comes (5 minutes before the warning, so the forecast can show it), whether it is
Ragnarok and when its eye opens and closes, and sends the phase and when it ends to every client, in the world's time,
so everyone counts down alike. It counts the fallen bosses (their global keys) while calm; a world just loaded starts
from the count it has. Each client works out its own player's exposure from the biome they are in and their
shelter: `Player.InInterior`, `Player.InShelter` (or under a lean-to's roof) and the game's near-fire timer, rays upwind for the lee, the trees
within 8 m, and the material of the piece overhead in the Ashlands. The health is lost as a hit that no armour or
resistance changes.

The storm's weather is a copy of the game's nearest one (the thunderstorm, the snowstorm, the Ashlands' storm or a clear
Plains sky for the sandstorm) with thicker fog, harder wind and colours of its own, added to the game's weathers. The
game is told to use it after its own forced weathers, so a dungeon, a raid or a boss still has its own, and the sky
blends into and out of the storm as into any other weather. The warning has a darker copy without rain or snow.

The visions and the grudge are the local player's own: the grudge is kept in the character's custom data, and the
figure is a copy of the player's body in one flat dark colour, with no network object, collider or physics.

Lightning and falling trees are worked out on each client around its own player. A tree is felled with one hit as hard
as it is sound, from the direction of the wind or the strike, so the game drops it as it would for a woodcutter and
every player sees it fall.
