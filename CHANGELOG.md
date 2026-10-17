# Changelog

All notable changes to BrudvikWhiteHilt. Newest version first.

## Unreleased

### Added
- **[Forge of Potential](docs/equipment.md#-forge-of-potential)**: a fair roll at the game's refinement forge instead
  of one fixed by the character's name, with every failed try on the same item and level raising the next one's
  chance. Crafting, the item's own skill and all skills together raise the chance, and lower the break chance. A
  failed try costs only the idol by default; it can also lose a level, break the item, or follow the game. Hold Shift
  to offer up to three more idols for a better chance. The trading posts sell their biome's idols, and at the forge
  three idols make one of the next tier, and a Battle idol becomes a Protection idol and back. All of it is in the
  `Refinement` settings.
- **[Octagonal stairwells](docs/building-pieces.md#-wood-floor-triangles--octagons)**: both octagonal wood floors
  with an opening in the middle, for the next storey of a tower. The 2 m one has an eight-sided opening 2.6 m across
  that the Spiral Stair stands in, its foot and head snapping to the centres of the floors; the 4 m one has a square
  opening 4 × 4 m on the vanilla floor's grid, to lay vanilla floors in and leave out what a stair, a hatch or a
  ladder needs.

### Changed
- The octagonal wood floors also snap at their centre, where a spiral stair's foot stands.

### Fixed
- The **Spiral Stair** stacks: aimed anywhere at the top treads of one, the next goes on top, turned to go on where
  it ends. The game only snapped it when aimed at the point inside the post, so stairs put on top stood askew. Each
  stair now has a top tread at 2 m to step off, which was missing where no floor was, and its handrail stops you
  walking off the outer edge, but at the top tread, where you step off to the side.
- The portal list's search finds what it should: the list goes back to its top with every new search, where the
  rows found could lie out of sight above it, and the words may come in any order and without accents. The
  scrollbar beside the list shows, and the mouse wheel scrolls three rows per notch, not a fraction of one.
- Mistlands plants no longer grow inside or under the cliffs and arches. When land explored before a plant came was
  filled, only the positions of the rocks were known, not how far they reach. Plants that already lie under rock are
  removed when they load, and old land is filled once more with the new rules, for all forageables.
- Draugr harbours are no longer made inside rocks, and their hoard goes onto the real ground near biome borders.
- A buried treasure no longer settles under an overhang or inside a large rock.

## v0.147.0 - 2026-10-17

### Added
- **[Wood floor triangles & octagons](docs/building-pieces.md#-wood-floor-triangles--octagons)**: the vanilla wood
  floor sawn to shape, every cut plank and joist with a closed end: Triangle 2×2 and 1×1, a 2×1 floor with a 45°
  end (and mirrored), and octagons with 2 m and 4 m sides that a vanilla wall stands on.
- **[Wood walls 4 m & window](docs/building-pieces.md#-wood-walls-4-m--window)**: Wood Wall 4×2 and 4×4, and a
  2×2 wall with a window sawn into it and the vanilla window's shutter.
- **[Lattices & braces](docs/building-pieces.md#lattices--braces)**: Wood Lattice 2×2 and 2×1, and braces at 45°
  (0.5, 1 and 2 m) for the corner under a beam.
- **[Wide stair, landing & bargeboards](docs/building-pieces.md#wide-stair-landing--bargeboards)**: the Wide Wood
  Stair (4 m), a Wood Stair with a 2×2 m landing, and plain bargeboards at 26°, 45° and 67° that follow the vanilla roof pieces down to the eave.

### Changed
- The White Hilt settings in the inventory are now a small round gear button in the bottom left corner, with a
  short line beside it showing the key (F7). The old, taller panel lay over big chests and an inventory widened to
  eight rows.

## v0.146.0 - 2026-10-17

### Added
- **[Mistlands foraging](docs/foraging.md#growing-ingredients)**: six new things to gather, each with its own generated
  model, growing in new land and (once) in land generated before:
  - **Hazelnuts** from a coppiced hazel in the clearings; Hares sometimes drop them.
  - **Wood Sorrel** in the damp shade; a Magecap sometimes gives it too.
  - **Chaga** on dead birch stumps, and **Lungwort Lichen** on fallen logs.
  - **Sweet Cicely**, a tall anise-scented herb.
  - **Vivianite**, grey-blue nodules in the moss: a blue dye for the Paint Bench. Hazelnuts, Chaga and Lungwort
    Lichen are dyes too.
- **New Mistlands food and drink**:
  - **Hazelnut Bread** (Stone Pot level 4): 58 health, 58 stamina, 35 min.
  - **Cured Hare Sausage** (Raw Hare Sausage on the Drying Rack, 2 days): 46 health, 70 stamina, 60 min, dodging
    uses 25% less stamina.
  - **Cured Mist Stag Ham** (Seasoned Mist Stag Ham on the Drying Rack, 3 days): 78 health, 34 stamina, 60 min,
    attacks use 15% less stamina. `[Curing] HareSausageDays` and `MistStagHamDays` set the days.
  - **Sweet Cicely Mead**: sneaking uses 50% less stamina (`SneakStaminaReduction`).
  - **Chaga Tea**: pierce damage taken is a quarter less.

## v0.145.0 - 2026-10-17

### Added
- **[Ashlands foraging](docs/foraging.md#growing-ingredients)**: six new things to gather, each with its own generated
  model, growing in new land and (once) in land generated before:
  - **Lyme Grain** from lyme grass on the low black sand along the shore.
  - **Sea Buckthorn**, a thorny silver shrub with orange berries; Asksvin sometimes drop them.
  - **Wild Thyme** on the cooled lava; a Smoke Puff sometimes gives it too.
  - **Tinder Fungus** on burnt stumps. Used from the hotbar on a wood fire it burns like three pieces of wood
    (`[Foraging.TinderFungus] FuelValue`), and the Charcoal Kiln turns it into Coal.
  - **Hot Spring Salt**, white crusts on the ash, for curing meat.
  - **Red Ochre**, a deep red dye for the Paint Bench; Wild Thyme and Sea Buckthorn are dyes too.
- **New Ashlands food and drink**:
  - **Hot Spring Bread** (Stone Pot level 4): 45 health, 92 stamina, 35 min.
  - **Cured Asksvin Sausage** (Raw Asksvin Sausage on the Drying Rack, 2 days): 72 health, 58 stamina, 60 min,
    +60 carry weight.
  - **Dried Bonemaw** (Salted Bonemaw on the Drying Rack, 3 days): 88 health, 38 stamina, 60 min, health regenerates
    25% faster. `[Curing] AsksvinSausageDays` and `BonemawDays` set the days.
  - **Sea Buckthorn Mead**: stamina regenerates 30% faster (`StaminaRegenBonus`).
  - **Wild Thyme Tea**: fire resistance.

## v0.144.0 - 2026-10-17

### Added
- **[A wilder storm](docs/storms.md#wind-hail-and-the-lost-course)**:
  - Fires with no roof over them go out in a raging storm, and a torch in the hand blows out in the open
    (`FiresGoOut`). Not in the firestorm.
  - Gusts tear doors open near players; while a door close by stands open the storm comes in, and the room shelters
    no better than the lee of a wall until it is shut (`DoorsBlowOpen`).
  - Gusts push players in the open with the wind and now and then knock them off their feet (`Gusts`).
  - Hail showers in the forest storm, at sea and in the blizzard hurt those in the open or the lee, softened by armour,
    and break young crops with no roof over them (`Hail`).
  - The Kraken rises at sea in a raging storm by day or night, in any weather, four times as often, and in Ragnarok
    at least every other minute (`KrakenFactor`).
  - Lost at sea: the compass, the minimap and your marker on the map are gone in a storm at sea unless a Navigator's
    Table is aboard (`LostAtSea`).
- **[The build camera with a pickaxe](docs/build-tools.md#-build-camera--toolbar)**: hold any pickaxe and the build
  camera stays on; the blow lands where the camera aims, up to 4 m from it (`CameraMining`, `CameraMiningReach`).
  Break a rock while building, or dig the bottom under water from the shore.
- **[Even ground by the tile](docs/build-tools.md#white-hilt-hoe-and-cultivator)**: the White Hilt Hoe's areas can be
  given a size on the panel (1 x 40, 4 x 4, 8 x 8, 16 x 16, 40 x 40 or typed in), turned with the wheel or the way the
  camera looks, and levelled tile by tile with K, each starting at the height of the last. PgUp / PgDn change the
  height by 1 m with Shift and 0.1 m with Ctrl.

### Fixed
- **Other players no longer hang in the air** for a moment before they drop into place. The game stops sending
  positions while a player's connection is busy, and carries on the last movement in a straight line meanwhile, so a
  player who jumped kept rising on the other screens. White Hilt now sends its own data so that positions come first:
  - The portal map sent every portal again every two seconds while a ship was loaded, since a ship bobs on the waves.
    Ships now go in a small list of their own, sent again only when a ship has moved 8 m.
  - Discoveries are sent whole only once; after that a player gets only what changed, such as berries ripening.
  - Large lists and shared blueprints go out in small pieces, and only while the connection has room, taking at most
    64 KB/s; before, they went in 192 KB pieces that held back positions for seconds.
  See [the analysis](docs/plans/network-stalls.md).

## v0.143.0 - 2026-10-16

### Added
- **[The silence before the dark](docs/darkness.md#the-silence-before)**: while the lights flicker below ground the
  place's sounds, the wind and the music fall away, and it is almost silent in the black.
- **[Doors in the dark](docs/darkness.md#doors-in-the-dark)**: as the black falls a door close by slams shut or swings
  open, and now and then again while it lasts; everyone hears it.
- **The light shrinks**: the longer a place has been in use, the less far a torch or a circlet reaches in the dark,
  down to a step or two.
- **A voice down the passage**: the voice from the storm calls from the dark below ground, with six new lines.

### Changed
- **Every Norwegian voice is new**: each speaker has a Norwegian voice of its own, a native Eastern Norwegian speaker,
  and every Norwegian line is spoken again with ElevenLabs' newest model, which knows Norwegian. Before they sounded
  Danish.

### Fixed
- **[The shadows](docs/darkness.md#something-is-down-there) come at last**: the shadow failed to load (its eyes asked
  for a shader the game does not have), so it never came. It also gave up for the whole darkness when the way behind
  was a wall; it now tries the sides and ahead.
- **[Fear of the dark](docs/darkness.md#fear-of-the-dark)** holds while the dark comes and goes instead of falling,
  and fills in 40 seconds (`FearSeconds`, was 75), so a long darkness brings the shadows. A server that already has
  the setting keeps its value.
- The glowing eyes in the dark and the figure in the storm failed in the same way and are now seen.

## v0.142.0 - 2026-10-16

### Added
- **[Creeping darkness](docs/darkness.md)**: below ground (crypts, caves, mines, troll caves and every other dungeon)
  the light now and then flickers, fades and goes almost black for a while: after a calm of 3 minutes, every 4 minutes
  and then more and more often and for longer the longer the place is in use, the same for everyone in it. A torch in
  hand, a circlet, a fire you built or a friend's light holds it off. Without one, fear grows: slower stamina, a
  pounding heart, failing legs and in the end lost health. From half fear a shadow comes that hunts only those without
  a light and is easy to hurt in the light. Torches burn down faster and gutter in the black, circlets flicker, and
  eyes, scraping, breathing and whispers come out of the dark. New sounds made with ElevenLabs; admins can call it with
  `whitehilt_darkness start`.
- **[The eye of the storm](docs/storms.md#the-eye-of-the-storm)**: a storm comes in two waves with a still eye
  between them. In the eye the wind dies, the meter holds out in the open and falls only in real shelter, and nothing
  strikes; the clock counts down to its return. The last wave drives the meter on half as fast again, with more
  lightning, thunder and falling trees.
- **[Ragnarok](docs/storms.md#ragnarok)**: once a boss has fallen, the first storm after a new boss falls, and now and
  then another, is Ragnarok: foretold and warned of twice as early, half as long and half as hard again, over the
  Meadows too, with lightning in every storm, and twice the finds after, some of them rare. Admins can call it with
  `whitehilt_storm ragnarok`.
- **[The storm remembers you](docs/storms.md#the-storm-remembers-you)**: each storm you live through out in it makes
  the meter climb 5% faster for you the next time, up to 50%, and leaves you more finds; each storm you sit out in
  shelter it forgets you a little.
- **[Visions in the storm](docs/storms.md#visions-in-the-storm)**: deep in the storm you hear footsteps behind you,
  whispers and a voice calling from the fog, and once you freeze a dark figure stands at the edge of your view and is
  gone when you look. A new voice and sounds made with ElevenLabs; your own setting, `Visions`.

## v0.141.2 - 2026-10-16

### Changed
- **[Odin and Freya wane in a great storm](docs/potions.md#out-in-a-great-storm)**: out in the storm, the Gift of
  Odin's healing and all of the Gift of Freya give half from 20% exposure, a quarter from 40% and nothing once you
  freeze at 60%. Before, the two potions together all but cancelled the storm.

### Fixed
- **Lag and stuck objects after a fire storm** in multiplayer: the flames on the player were removed in a way that
  left a dead network object behind, and the game then failed to unload anything around you, every frame.
- **[What the storm leaves behind](docs/storms.md#what-the-storm-leaves-behind)** is left again: the finds failed to
  drop when the storm passed.
- **The mod's monsters and the White Dragon are heard by every player**: their alert, hit, death and idle sounds
  reached only the player whose game ran the creature.
- The Root and Stone Dowser's pings, the ship's bell and the White Dragon's roar no longer make network objects other
  players' games cannot build.

## v0.141.1 - 2026-10-16

### Fixed
- **Heavy lag near the great beasts in multiplayer**: the heavy footsteps of the Black Beasts and the trading posts'
  terrors (and the dog's sounds) made a network object at every step that no other player's game could build, so
  every client warned of a "Missing prefab hash" thousands of times a second. The sounds are now local, as the game's
  own footsteps are.
- An error when a removed object was unloaded no longer stops the game cleaning up the objects around you.

## v0.141.0 - 2026-10-16

### Added
- **[Supply and demand](docs/trading-posts.md#the-store)** at the trading posts: a post pays less for what it has been
  sold lately, 10% a stack down to 40%, and the price comes back over the days. Each post keeps its own market, shared
  by all players, and the trader tells you when it has too much. While a great storm rages and for a day after, the
  posts pay 25% more.
- **[The merchant at your jetty](docs/trading-posts.md#the-merchant-at-your-jetty)**: now and then a trading ship moors
  beside a Jetty Head you built and stays a game day with goods of a land the world has opened. Trade at its helm.
- **The traders and the storm**: they call you in under the dome when a storm is coming, aloud, and the dome keeps the
  storm out. New voice lines made with ElevenLabs.

### Fixed
- **A waiting merchant no longer vanishes** when its caller goes to fetch their ship: the trading ship is kept in the
  world, and the server ends what nobody comes back to (the contract goes back for a merchant that waited in vain).
- **Nail Ships on a server**: they appeared 180 m away, beyond the area a server keeps alive (Valheim 1.0 agrees the
  shorter simulation distance of the server and the player), and the game removed them at once: the escort's attack was
  announced but no ship came. They now come closer where needed, down to 40 m, in escorts and natural encounters alike.
- **[A merchant that runs aground](docs/sea-battles.md#escorts)** gives up after three new routes from the same place: the
  ship is taken away, you get the contract back and can try again at once.
- Reading a bounty notice or a Mine Chart from the inventory no longer locks the map and the inventory open.
- A trading ship without its skipper says why in the log.

## v0.140.0 - 2026-10-15

### Added
- **[Great storms](docs/storms.md)**: now and then a storm comes, with three minutes' warning: the sky darkens, thunder
  rolls far off and the clock counts down. Out in it, whatever you wear or have eaten, your stamina fails, you freeze,
  the view closes in and in the end the storm kills you. Each biome has its own: thunder and rain in the Black Forest
  and the Swamp, a cold storm at sea, a blinding sandstorm on the Plains, a blizzard on the Mountain you can hardly
  move in, bitter fog in the Mistlands, a firestorm in the Ashlands that sets you alight, and the howling Deep North.
  Never in the Meadows. Shelter under a roof by a fire, or in a cave or dungeon; the lee of a rock or a wall and close
  trees slow it down. In the Ashlands only a stone roof shelters.
- **[The Storm Lean-to](docs/storms.md#the-storm-lean-to-vindskjul)**: a vindskjul of logs for 12 wood, built anywhere
  without a workbench. Under it you are under a roof; a fire in front of the opening keeps you alive.
- **[Lightning](docs/storms.md#lightning)** strikes in the forest storm, at sea and in the Mistlands: a tree close by,
  which falls, the ground, or you out in the open, more often with metal in hand or on a ship. A crackle warns you to
  move.
- **[Falling trees](docs/storms.md#falling-trees)**: gusts fell trees around you with the wind, never inside a base.
- **Storm sounds** made with ElevenLabs: rain, sea, sand, blizzard, fog and fire storms, thunder that cracks overhead
  with a flash of lightning, lightning strikes, creaking and cracking trees, and the warning's distant horn.
- **[Foretold](docs/storms.md#foretold)**: the weather forecast shows a great storm 5 minutes before the warning, and
  aboard a ship with a Navigator's Table everyone is told and the bell rings.
- **[Weathering](docs/storms.md#weathering-herdet)**, a new skill (Herdet): it rises out in the storms and slows the
  exposure meter, by 30% at level 100.
- **[What the storm leaves behind](docs/storms.md#what-the-storm-leaves-behind)**: when it passes, amber and resin,
  pearls, coins and rubies, crystals and frost glands lie about near those who were out in it; lightning leaves a
  thunderstone now and then.
- **[The Husvette and your dog](docs/storms.md#the-husvette-and-your-dog)** feel it coming: the Husvette calls you in
  aloud and is glad of the walls while it rages; your dog whines and looks toward home.
- `whitehilt_storm` shows the storm and your exposure; admins can `warn`, `start` and `stop` one.

## v0.139.0 - 2026-10-15

### Added
- **[Grapnels](docs/sea-battles.md#grapnels)**: a Nail Ship's crew throws a grapnel at your rail while it lies
  alongside, and the rope hauls the two ships together. Cut it with Use at your own rail. Any grapnel rope can now be
  cut at either end.
- **[The ram](docs/sea-battles.md#the-ram)**: from Moder on, some Nail Ships carry an iron ram on the bow and charge a
  ship that lies still or sails slowly. Keep way on.
- **[Fire ships](docs/sea-battles.md#fire-ships)**: from Yagluth on, an attack may bring a burning fire ship with no
  crew that sails straight at you, sets your deck alight when it strikes and then goes down. Sink it or outrun it.
- **[The jarl's horn](docs/sea-battles.md#the-jarls-horn)**: a flagship whose jarl still lives after three minutes of
  fighting blows its horn, and one more Nail Ship joins the fleet.
- **Nail Ships after the Queen and Fader**: new crews (draugr elites with a fuling brute, then charred warriors,
  marksmen and warlocks, all at least one star), a stronger hull for every boss slain, a hold with the goods of the
  latest biome, fleets that come more often, and from Yagluth on two warriors leap aboard at a time. Old config files
  that still hold the earlier crew and fleet lists get the new ones.
- **The sighting tells you where**: *Sails on the horizon, off the starboard bow!*, and the place is pinned on your
  map for a while.
- **Night, storms and fog** make an attack more likely (×1.5 each).
- **The crew mends its ship**: every ten seconds a Nail Ship's crew puts out a fire on its deck or patches a hole.

### Changed
- **[Nail Ships](docs/sea-battles.md#the-fight) fight smarter**: they come up level with a moving ship instead of
  trailing it, match its speed alongside, and take the side with fewer defenders. A ship that is kept from you by land
  or shallows gives up after a while instead of lying stranded under your ballista.
- `whitehilt_seabattle summon` and `fleet` take `fire` and `ram`, and `status` shows a ship's hull and whether it is a
  fire ship or carries a ram.
- `HullHealth` and `FlagshipHullHealth` now apply to new Nail Ships at once, without a restart.
- **The game and the server start faster**: the mod's settings file was written again for every one of its some
  3600 settings while the mod started (about half a minute on a fast machine, more on a server); it is now written
  once. The log also shows how long each step of the mod's start takes (`Start-up: Awake took ...`).
- **[The chests](docs/chests.md) keep their world progress in White Hilt's own folder**,
  `BepInEx/config/BrudvikWhiteHilt/chests/`. On the first start the mod moves it there from the old
  `BepInEx/config/BrudvikStackedChest/` folder, which it then removes when nothing else is left in it.
- The server and every player need this version: the chests' world progress has a new network message, and the sea
  battles' network messages have changed.

### Fixed
- **A Nail Ship never caught a ship that ran from it once it had begun to board**: it went on closing in at half sail,
  however far away you were. It now goes back to the chase at full sail, and its time alongside before boarding starts
  over.
- In multiplayer, a Nail Ship started its 40 seconds alongside over again whenever another player's machine took it
  over. The time is now kept with the ship.
- A Nail Ship that found no clear way stopped dead and stayed there; it now turns about or backs off.
- The raiders' taunt *Run, little merchant, run!* is only shouted at a merchant's ship.
- **[Trading posts](docs/trading-posts.md)**: a post whose trader had gone missing stayed without one. The post now
  puts out a new trader when none is there.
- The beam across the front of the trader's porch hung 1.5 m over the floor, so nobody could walk in to the door. It
  is gone; the posts are built again to the new plan.
- **The gathering panel's "closest to unlocking"** could show amounts from chests that no longer held them, and after
  many restarts stop taking new chests into account: the server told the chests apart by a number the game gives
  anew every time a world loads. Each chest now has an id of its own that lasts; the old record is dropped and fills
  again as the chests are used.

### Removed
- **The chest census** (`whitehilt_chest_census`) and the count the server took at every start. It was made for the
  move from BrudvikStackedChest, long done, and its warnings that chests were "gone" were false: the game numbers its
  objects anew every time a world loads, and the census told the chests apart by that number. No chest was lost.
- **What was left of BrudvikStackedChest**: White Hilt no longer waits for the old mod when it is installed, and no
  longer takes over its config file. Coming from the old mod, set `[Chests] Mode` by hand before the first start
  (see [Moving over](docs/chests.md#moving-over-from-brudvikstackedchest)).

## v0.138.1 - 2026-10-15

### Changed
- **Valheim 1.0.20**: the mod is built for it and needs it.
- **[Trophies and stars](docs/monsters.md#trophies-and-stars)**: the trophies of the mod's own monsters now drop as
  the game's own do since 1.0.20: each star raises the chance (twice at one star, four times at two, the loot
  multiplier above that), one trophy at a time. A starred monster of the mod is no longer harder to get a trophy from
  than a vanilla one.

## v0.138.0 - 2026-10-15

### Added
- **The White Hilt, the Husvette and Munin speak**: the White Hilt's own voice says its unlocks, the blood moon and
  the land growing restless (new messages at a quarter, a half and three quarters of the pressure); the
  [Husvette](docs/base.md#the-husvette-speaks) welcomes you home, murmurs as you rest, grumbles at a cold hearth;
  [Munin](docs/navigation.md) caws and tells of new places on the map. English and Norwegian, made with ElevenLabs.
- **[Fight music](docs/difficulty.md#heard-not-only-seen)**: a fight with one of the mod's great beasts, near a Nail
  Ship's crew or under a blood moon has music of its own in place of the game's combat music, and a blood moon has
  war drums and wolves at night.
- **More sounds**: digging up treasure, the Rune Forge, runes hung and awakened, rune etching, a sound for each god's
  gift and the meads, and the Black Bestiary, the School Bench and the saga.
- **[Monster sounds](docs/monsters.md#sounds)**: every monster of the mod has new sounds made with ElevenLabs, three takes of
  each so they do not repeat: the Giant Spider, Lindorm, Kraken, Greenland Shark, Desert Dragon, Mist Stag, Nidhogg
  Spawn, Yggdrasil Warden and Garm Hound, and now also the Draugr Jarl, the White Dragon, the nine Black Beasts and the
  eight plagues. The big ones walk heavily, dragons beat their wings, the Lindorm and the Kraken sound as they come,
  a dread sounds when a Black Beast or a plague comes, spider nests rustle, and the Nail Ships' crews shout battle
  cries and taunts.
- **[The traders speak](docs/trading-posts.md#voices-and-sound)**: every line of the eight traders, Ulf the Hunter
  and the trading ship's skipper is recorded, each with a voice of their own, in English and Norwegian (by the game's
  language). The skipper shouts when the ship casts off, when the Nail Ships come and in port. The posts have the sound
  of the sea and gulls and their own music under the dome, the plagues roar, and bounties, tracks and charts have their
  sounds. Made with ElevenLabs; the volumes are in `[Sound]`.
- **[Plagues](docs/trading-posts.md#plagues)**: now and then a named monster of the biome (Bristleback, Gnawtooth,
  Mire Mother, Whiteclaw, Skullsplitter, Clickjaw, Ashfang, Icemaw) terrorises a trading post and comes down to its
  yard at night. The trader hands out a free Bounty Notice with a search circle on the map, pays well for the
  monster's head (800 coins in the Meadows, 8000 in the Deep North) and gives half off the post's garb. While a post
  is plagued its prices are 20 % worse.
- **[The traders' garb](docs/trading-posts.md#the-traders-garb)**: every biome's traders have a set of four pieces of
  clothing in their colours (hood, tunic, trousers, cape), each sold at one of the biome's posts only. Light armour, a
  small bonus of the biome's kind per piece, a set bonus for all four (the Swamp's keeps you dry, the Mountains' keeps
  the cold out, and so on), and 3 % better prices per piece at the biome's posts, 15 % with the whole set.
- **[The Mine Chart](docs/trading-posts.md#the-mine-chart)**: the trader at the Mistlands posts sells a chart (250
  coins, `MineChartPrice`) that marks the nearest infested mine you have not found yet and shows it on the map.

### Changed
- **The sounds are a file of their own**, `whitehilt_audio`, next to `whitehilt_foraging` and `whitehilt_decor`. A mod
  manager installs it by itself; by hand, copy all four files in `plugins/`.
- **[The hunter at the trading posts](docs/trading-posts.md#the-hunter)** now sells the Elaking's trophy in the Deep
  North, at a great beast's price.

### Fixed
- **Icons**: rendered icons fill their square. The maps, charts and many small items and pieces came out small in a
  corner of their icon, because the icon renderer measured the hidden vanilla model too and framed the model from its
  base instead of its centre.

## v0.137.0 - 2026-10-15

### Added
- **[The hunter at the trading posts](docs/trading-posts.md#the-hunter)**: now and then Ulf the Hunter sits on a chair
  behind a post's fish stall, a woodsman in troll leather with a bow on his back. When you come to a post there is a
  20 % chance he is there (`HunterChance`), and the answer holds for a game day. He sells one of each trophy of the
  post's biome, White Hilt's own monsters included (never a boss's), behind the same boss as the post's goods, at the
  biome's worth times 15 (`TrophyWorth`), three times that for the great beasts.
- **[A skipper at the trading ship's helm](docs/sea-battles.md#the-trading-ship)**: the merchant you escort no longer
  sails empty. A skipper dressed like the posts' traders holds the helm, each ship its own, the same for every player.

### Changed
- **[The trading posts look lived in](docs/trading-posts.md#the-posts)**: the trader's house is now 8 × 4 m with its
  gable and door towards the jetty under a roof carried out over a porch, windows in both long walls, a plank floor over
  all of it and furnished inside (bed, chest, shelves, a table with the trader's map and accounts, a bench, lamps).
  Behind the landing lies a fenced yard with a gate: a fire with a grate and benches, a wood yard, stone paths with
  lanterns. The ground under the house, the yard and the paths is painted earth and stone, so no grass grows through
  floors and decks. Flowers, shrubs and birches grow along the edges in the green biomes; shells, a shore rock, a fish
  rack and old piles lie by the water. Posts already in the world are built again to the new plan.
- New posts are only placed where the land 17 m behind the landing is flat enough for the yard.

### Fixed
- The bench beside a trading post's fire faced away from it.
- The small laft house of the log house pieces (the documentation's example) left a strip of its floor out.

## v0.136.1 - 2026-10-15

### Fixed
- **[The trading ship finds its way to port](docs/sea-battles.md#escorts)**: a merchant bought at a trading post used to
  steer straight for the port and ran aground when islands lay between. The server now plans a sea route over the
  world's ground, as the Navigator's Table does, and the merchant follows it turning point by turning point, easing the
  sail in the sharp turns. If it cannot go on it plans a new route from where it lies.

### Changed
- **The escort pays by the route**: the pay for a voyage between trading posts follows the length of the planned sea
  route, not the straight line over the islands. A port in shallows is moved to the nearest deep water, and the distance
  shown on the ship is the way left along its route.
- The contract is refused, and kept, when no sea route leads to the nearest other trading post.

## v0.136.0 - 2026-10-14

### Added
- **[Sort recipes by what they give](docs/base.md#-crafting-panel)**: a sort button above every recipe list switches
  between Name, Health, Stamina, Eitr, Total, Best for me and Strength. Best for me weighs a food's health, stamina and
  eitr by your profile: Balanced, Warrior, Archer, Mage or your own weights. Strength sorts weapons and ammunition by
  damage, armour by armour and shields by block.
- **What each recipe gives, on its line**: a food's health, stamina and eitr in the game's colours and its minutes, a
  potion's or mead's effect and time (a mead base shows the mead it ferments into), and gear's damage, armour or block
  at the level the recipe makes. New settings in `[CraftingPanel]`: `SortBy`, `ShowStats`, `FoodProfile`,
  `HealthWeight`, `StaminaWeight` and `EitrWeight`.

## v0.135.0 - 2026-10-14

### Added
- **[Trading posts](docs/trading-posts.md) sell White Hilt's own items**: everything the mod adds that can be crafted
  (food, meads, potions, ammunition, gear and the rest) is shared out among the posts of the biome of its tier, each
  piece at one post only, priced from its recipe and sold as many at once as one craft makes. New kinds for what a post
  is known for: provisions, ammunition and wares.
- **Trading posts in the Swamp, the Mountains and the Ashlands**: the Swamp and the Mountains never meet the sea, so
  their posts stand on the shore below them (swamp or mountain within 400 m inland); the Ashlands' broken coast is
  searched every 20 m and allows rougher land behind the landing. Worlds searched before are searched once more, keeping
  their posts.
- **Light at the posts**: lanterns along the jetty, a candle stand at the trader's stall and a soft light over the
  trader, so the trader is seen at night.

### Changed
- **Posts pay more**: from 60 % of their price at Trading 0 to 90 % at 100 (was 25 % to 40 %); settings left at the old
  defaults are moved to the new ones.
- **A wider jetty and a larger dome**: the jetty is 4 m wide, so the cargo on it no longer blocks the way, and the dome
  is a third larger (34.7 m), covering ships on both sides. Posts already standing are built again where they stand.

### Fixed
- **A store showed one item priced 123456**: an item the mod adds to a trader's list had no tooltip, which stopped the
  store window after its first line. Affected the trading posts and the mod's items at Haldor and Hildir.

## v0.134.0 - 2026-10-14

### Added
- **[Comfort in the whole room](docs/base.md#-comfort-in-the-whole-room)**: the **Husvette**, a carved house spirit with
  a red cap and his porridge, makes every comfort piece within 30 m count for everyone under its roof, and lets a fire
  anywhere in the room keep them warm enough to rest. The **Book of the Hall** (Hildir and the trading posts) widens your
  own comfort reach from 10 to 20 m, and the new **Homecraft** skill, raised by resting and building comfort, widens it
  further, up to 40 m. Settings in `[Comfort]`.
- **[Trading](docs/trading-posts.md#the-trading-skill)**, a new skill raised by trading at the posts and by escorts: the
  posts pay you from 25 % up to 40 % of their price and sell up to 10 % cheaper. Every post now also sells **gear of its
  biome**, found from the game's recipes and shared out so each piece is sold at one post only; it opens at Trading 20,
  40, 60 and 80, and what you have not opened shows greyed out. A post's trader and its pin on your map say what it is
  known for. Settings in `[TradingPosts]`.

### Changed
- **[Workbench extensions](docs/build-tools.md#workbench-extensions) fit where they fit**: an extension is no longer
  refused because another extension, of any station, stands near its centre; only one that really overlaps another is
  (`[StationExtensions] Spacing`). Extensions may stand up to 10 m from their station (5 m in the game, `Reach`), for
  placing and for the station's level, and the station's reach shows as a ring while you place one (`ShowReachRing`).

## v0.133.0 - 2026-10-14

### Added
- **A Deep North tier**: Bloodgold unlocks it in linear progression (recipes in it take 3 Bloodgold extra), and White
  Hilt armor, weapons and shields upgrade one level further, to quality 9, for 10 Bloodgold: armor +6 (48 per piece),
  capes +2, weapons +65% of their base damage (the sword about 176 slash), shields +40% of their base block. They stay a
  little below the Protector armor, the Nord Sword and the Nord shields. `[Tiers]` takes `DeepNorth`; new settings
  `UpgradeDeepNorth`, `DeepNorthArmor`, `DeepNorthDamage` and `DeepNorthBlock` in `[Gear.Armor]`, `[Gear.Weapons]` and
  `[Gear.Shields]`.
- **Trading posts, first part** ([plan](docs/plans/trading-posts.md)): a harbour of jetties with two market stalls, the
  trader's laft house, a harbour crane, mooring posts, a gate and a flag, under a dome like Haldor's that keeps
  monsters out. Its pieces cannot be broken or removed, and nothing can be built inside its dome.
- **Trading posts along the coasts**: once per world, also in worlds made before, the server places up to 4 posts in
  each biome that meets the sea, the Deep North included, where the bank is low, the land behind is flat and the water at the jetty head is deep
  enough for a longship, at least 1000 m apart and away from what players built. The land behind the landing is
  levelled and trees and rocks on the post are cleared.
- **The traders**: every post has its trader behind the goods stall, a man or woman in the biome's colours (Asbjørn in
  the Meadows, Gunnar in the Black Forest, Ragnhild in the Swamp, Hallvard in the Mountains, Sigrid on the Plains, Ylva
  in the Mistlands, Brand in the Ashlands, Frøydis in the Deep North). They turn to you, wave, greet you, talk now and
  then and wave you goodbye; the stalls' awnings and the flag take the same colours.
- **The store**: a post sells the materials of its own biome once the world has slain the boss before it (the
  Meadows' at once), so you need not search for hours: wood, ore, hides, berries, seeds and the rest, dearer for later
  biomes, ore and hides. It buys the goods of the next biome over (the Deep North's buy the Meadows') and every fish,
  for a quarter of its own price; fish from later waters and with stars fetch more. The sell button shows which
  stack it sells and for how much, and the » button beside it steps to the next stack the post buys. Settings `BuyShare`, `Prices` and `BuyUnlimitedGoods` in `[TradingPosts]`.
- **Trading Post Charts from Hildir**: one for each biome, on sale once the world has reached it (150 coins,
  `ChartPrice`). Read it and every trading post on that biome's coasts gets a pin on your map. The chart is kept, so you
  can read it again or give it to a friend.

### Changed
- The server and every player need this version: the trading posts and the escorts from them add new network messages.
- **Escorts start at the trading posts**: the traders there sell the Escort Contract instead of Haldor. Use it at a
  post and the trading ship comes in beside the jetty, on the side away from your ship; it sets out for the nearest
  other trading post when you go aboard your own ship, and pays 400 coins per kilometre of the voyage for a full cargo
  (`PayPerKilometre`). If you do not come aboard within 10 minutes it sails off and you get the contract back
  (`WaitSeconds`, `StartRange`). With the trading posts off, Haldor sells it and escorts start at sea as before. Admins: `whitehilt_tradepost here` (build one where you stand,
  the jetty running out the way you look), `remove`, `list` and `place` (search the coasts again). Settings `[TradingPosts] Enabled`, `PerBiome`, `MinDistance` and `BuildingDistance`.
- **[Coins are worth something again](docs/chests.md#coins-and-valuables)**: in Linear mode coins, gems and everything else the Treasure Chest holds no longer become unlimited, so coins are earned by trade, escorts, raids and treasure. A new setting, `UnlimitedValuables` in `[Chests]`, decides it: `Auto` (default: not in Linear, yes in Full and Discovered), `On` or `Off`. **In a Linear world, the chests remove their unlimited stacks of coins and valuables the first time they are opened after the update**; set `UnlimitedValuables` to `On` first to keep them.

## v0.132.0 - 2026-10-14

### Added
- **[Escorts](docs/sea-battles.md#escorts)**: Haldor sells an **Escort Contract** (300 coins, once Bonemass is slain). Use it aboard your ship at open sea and a **trading ship** sails up beside you, bound for a port 1200 to 2000 m away over deep water, marked on your map. It has a blue and cream sail and barrels and crates on deck; it sails itself, a little slower than a longship, and its helm and hold are shut.
- **Nail Ships come for the merchant, not for you**: two waves on the way (a fleet once fleets are open to the world), and no natural Nail Ship encounter within 400 m of it. Every living raider who leaps onto its deck carries the cargo off, 0.5% a second each, so sink or take the Nail Ships, kill the raiders and [patch the holes](docs/sea-battles.md#patching-and-bailing).
- **The pay**: when the merchant reaches port, every player within 150 m gets coins by the cargo left (600 for a full cargo), with a 25% chance each of a Raid Chart. It fails if the ship sinks, the cargo is gone, or nobody stays within 300 m of it for two minutes. Settings in `[SeaBattles.Escorts]`.
- **A new [saga](docs/saga.md) deed**, *A trading ship was seen safely to port* (40 renown, `RenownEscort`).
- **`whitehilt_seabattle escort` and `escort wave`**, console commands for admins: call a trading ship for your ship without a contract, and send its next wave at once. `status` now lists the trading ships loaded.

### Changed
- **[No outrunning the Nail Ships at first](docs/sea-battles.md#no-outrunning-them-at-first)**: when Nail Ships attack, the hunted ship is held at 85% of their speed (never below 4 m/s) for 60 seconds, so they catch even a White Hilt ship. After that you may run, unless living raiders stand on your deck. Settings `PursuitEnabled`, `PursuitShockSeconds`, `PursuitSpeedFactor` and `PursuitFloorSpeed` in `[SeaBattles]`.
- The server and every player need this version: escorts add new network messages.
- "Escorts" is gone from the ideas page.

### Fixed
- **Nail Ships no longer wreck themselves**: a crew member's blows (such as a Fuling Brute's swing), a Nail ballista's bolts and collisions between ships of the same fleet no longer damage a Nail Ship. Players' attacks hurt them as before.
- The four pursuit settings in `[SeaBattles]` (`PursuitEnabled`, `PursuitShockSeconds`, `PursuitSpeedFactor`, `PursuitFloorSpeed`) had no Norwegian names in the settings window; they have now.

## v0.131.0 - 2026-10-13

### Fixed
- **Players cut off from the server**: Steam refuses a message over 512 KB, and the game then tries that message again and again, so nothing else reaches the player: they see no world around them and are dropped, and so is everyone who joins after. The server's lists of discoveries, map areas and portals, which grow as a world is explored, now go out in pieces; any message that is still too large is dropped and logged (with what it was) instead of blocking the connection; and the School Bench keeps its library well under the limit.
- **Grapnel Bolt**: its recipe now asks for Bog Iron as intended; the Bog Iron was left out, so it cost only Iron and Deer Hide.

## v0.130.0 - 2026-10-13

### Added
- **[Nail Ship fleets](docs/sea-battles.md#fleets)**: after Moder (35% of the encounters) and Yagluth (50%), a Nail Ship may come with company: two ships, or three after Yagluth. They sail up in a wedge behind a **flagship**, break formation when the flagship is within 80 m, and come at your ship from both sides. Settings in `[SeaBattles.Fleets]`.
- **[The Nail Flagship](docs/sea-battles.md#the-flagship)**: a Nail Ship with a blood-red sail, a stronger hull (2000 health), a bigger crew with more stars, the ballista on its bow always, a fuller hold and always a Raid Chart.
- **[The Draugr Jarl](docs/monsters.md#draugr-jarl)**: a huge undead viking with a fur cloak, a great axe and glowing eyes, a creature of his own with his own model and animations. He leads the flagship's crew, leaps across last, and always drops his trophy. He grows with the world: two stars after Moder, up to five after Fader (`[SeaBattles.Fleets] JarlLevels`). He never spawns in the wild. Settings in `[Monsters.DraugrJarl]`.
- **[Morale](docs/sea-battles.md#morale)**: when the jarl is slain or the flagship is taken or sunk, the other ships of the fleet turn and run (`FleeWhenJarlFalls`, `FleeWhenFlagshipFalls`).
- **A new [saga](docs/saga.md) deed**, *A Nail fleet was broken* (40 renown, `RenownFleet`), when a flagship is taken or sunk. The Draugr Jarl is a listed creature, so slaying him is a deed too; an existing config file needs `WhiteHilt_DraugrJarl` added to `[Saga] Creatures` by hand.
- **`whitehilt_seabattle fleet [2|3]`**, a console command for admins: sends a whole fleet after your ship. `status` now shows whether each Nail Ship sails alone, as a flagship or as an escort.

### Changed
- The server and every player need this version: the messages that raise and announce a Nail Ship changed to carry the fleet size.
- The Monsters, Saga and Sea battles pages describe the fleets and the jarl, and "Fleets" is gone from the ideas page.

## v0.129.1 - 2026-10-13

### Fixed
- Bailing water or stamping out a deck fire without enough stamina now flashes the stamina bar and says *You are too tired*; before, nothing happened.
- The [ballista](docs/ships.md#the-ship-ballista)'s chosen ammunition is kept with your character, so it is still chosen after you log out.
- Everyone near a [ram](docs/ships.md#the-ram-prow) feels the camera shake, not only the player whose game controls the ship.

## v0.129.0 - 2026-10-13

### Added
- **[Holes, flooding, patching and bailing](docs/sea-battles.md#damage-at-sea)**: in a sea battle a heavy hit on a hull (at least 60 damage) knocks a hole at the waterline, and a ram always does. Holes let water in: the ship lies lower, sails slower and, unless it is indestructible, breaks up when full. The White Hilt ships flood and slow down but never sink. Look at a hole and press Use to patch it (2 wood and 1 resin), press Shift + Use aboard to bail (costs stamina). The water in the hull shows at the helm. Settings in `[SeaBattles.Damage]`.
- **[Deck fires and Pitch Pots](docs/sea-battles.md#fire-on-deck)**: a Pitch Pot (Workbench: Pine Tar, Resin, Leather Scraps) that lands on a deck sets it alight. The fire burns players and the hull for 30 seconds, half that in rain, and is stamped out with Use. See [The Pitch Pot](docs/sea-battles.md#the-pitch-pot).
- **[The Nail Ship fights back](docs/sea-battles.md#the-nail-ship-fights-back)**: its crew throw pitch pots at your deck, and once Moder is slain it carries a ballista of its own that shoots Ship Bolts at your hull. Settings in `[SeaBattles.Weapons]`.
- **[Chain Shot](docs/ships.md#the-ship-ballista)**: ammunition for the Ship Ballista that tears the sail of the ship it hits, so it sails at half force for 45 seconds.
- **[Grapnel Bolt](docs/ships.md#the-ship-ballista)**: ammunition for the Ship Ballista that binds the ship it hits to yours with a rope and pulls the two together, so you can board. A bound Nail Ship cannot flee. Look at the rope on your rail and press Use to cut it.
- **Ammunition choice**: while manning the ballista, the secondary attack switches between the Ship Bolts, Chain Shots and Grapnel Bolts you carry. The crosshair shows which and how many. New keys in `[Ships.Ballista]`.
- **[Ram Prow](docs/ships.md#the-ram-prow)**: a new ship upgrade for the White Hilt Ship and Skidbladnir (Shipwright's Bench: Iron, Bog Iron, Fine Wood, Chain). Striking a ship or a creature bow-first hurts it in proportion to your speed, and always knocks a hole in a ship. Settings in `[Ships.Ram]`.
- **`whitehilt_seabattle hole [n]`**, a console command (needs cheats): knocks holes in the ship you stand on, to try flooding and patching.

### Changed
- The secondary attack no longer makes you let go of the ballista: it switches ammunition.
- The server and every player need this version: holes, fires and ropes add new network messages.

## v0.128.0 - 2026-10-13

### Added
- **[Nail Ships](docs/sea-battles.md)**: after Bonemass is slain, a dark longship crewed by draugr may hunt your ship on the open sea (deep ocean with no land near, at most one every 45 minutes in the world). A horn sounds, it lies alongside while its archers shoot, then closes in so its warriors leap aboard. It flees when its crew or hull is nearly gone. The crew grows with the world's progress (fulings after Yagluth) and with the players aboard. Settings in `[SeaBattles]`.
- **Prize ships**: when the whole crew is dead the Nail Ship is yours: open its hold, take the helm and sail it. Its nails do not hold, so it sinks after 10 minutes and the hold drops as a cargo crate.
- **Boarding leap**: standing on any ship, look at another ship within 10 m and press Use to leap aboard.
- **[Raid Charts and draugr harbours](docs/sea-battles.md#raids)**: a Nail Ship's hold may carry a Raid Chart. Using it marks a draugr harbour on a coast you have explored: a moored Nail Ship, guards on the shore and a hoard chest that only opens once the guards are dead. Settings in `[SeaBattles.Raids]`.
- **Two [saga](docs/saga.md) deeds**: a Nail Ship taken or sunk (25 renown) and a draugr harbour sacked (40), with the settings `RenownSeaBattle` and `RenownRaid`.
- **`whitehilt_seabattle [status|summon|raid]`**, a console command: everyone can see the conditions for a Nail Ship; admins can send one after their ship or make a harbour near them.
- **Ship Ballista** (a new ship upgrade for the White Hilt Ship and Skidbladnir) and **Ship Bolts**. The ballista stands on the forecastle: look at it and press Use to man it, aim with the camera and shoot with the attack button. The bolts are made at the Shipwright's Bench and are not turret bolts. Everything is a setting in `[Ships.Ballista]`, including the firing arc, reload time and damage. See [Ships](docs/ships.md#the-ship-ballista).

### Changed
- The Harbour Anchor's map and the Lookout treat a Nail Ship as nobody's ship: it never shows on the anchor's map, and the Lookout shows it as a sea monster.
- The server and every player need this version: the Nail Ships and the raids add new network messages.

## v0.127.0 - 2026-10-12

### Added
- **Five wild monsters**, each with its own animated model and sounds: the **Mist Stag** (a pale stag that charges and stomps), the **Nidhogg Spawn** (a dark dragon's brood with a poisonous bite, which roars once when it sees you; at night in the Mistlands), the **Yggdrasil Warden** (a tree that stands dormant like a dead tree until you come within 8 m, very weak to fire), the **Garm Hound** (fire-biting packs of 2-3 in the Ashlands at night) and the **Greenland Shark** (a slow shark in the open sea, once Bonemass is slain, that also bites boats). Every number is a setting in `[Monsters.<Name>]`.
- **Loot and what it makes:** Mist Stag Meat roasts on the iron cooking station into **Roast Mist Stag**; Shark Meat ferments into **Hákarl**; Brood Scales make the **Brood Scale Charm** (armor and poison resistance, an accessory); Living Heartwood makes the **Heartwood Draught** (health regenerates twice as fast); Garm Fangs make **Garm Fang Arrows**. Each monster also drops a trophy. The settings are in `[Monsters.Loot]` and `[Meads.HeartwoodDraught]`.

### Changed
- The Monsters page is now called "Monsters" and describes the new beasts; "More beasts" is gone from the ideas page.

## v0.126.0 - 2026-10-12

### Added
- **The School Bench shares map pins.** Writing also writes your own pins (the marks you set and the places you found, such as Vegvisir boss stones and Hildir's quests; never deaths, beds or pings), and reading gives you the pins you do not have, in biomes you have been to. Writing again replaces your own pins. Three new `[School]` settings: `TeachPins`, `MaxPins` and `PinMergeDistance`.

### Changed
- The bench's counts and messages list map pins. Everyone, and the server, needs this version: older versions cannot read a bench with pins.

## v0.125.0 - 2026-10-12

### Added
- **Mistlands and Ashlands foraging.** Eight new plants: Dewberries, Lady's Mantle, Beard Lichen and Mistletoe in the Mistlands, and Fireweed, Ash Morel, Fire Moss and Rowanberries in the Ashlands. Each has its own generated model, grows in new land and (once) in land generated before, and has an extra drop from a vanilla plant or a beast.
- **Eight Stone Pot dishes** from them, cooked at level 4 (the Soapstone Cauldron): Hare and Dewberry Pot, Dewberry Cake, Lady's Mantle Broth, Lichen Bread, Asksvin Stew with Fireweed, Rowanberry Jelly, Ash Morel Broth and Rowan-Smoked Volture. Lichen Bread and the smoked Volture have a buff.
- **Four brews:** Mistletoe Mead (eitr regenerates faster), Dewberry Wine (less noise), Rowan Brew (lightning resistance) and Fireweed Ale (cheaper running, faster movement).
- **Two roof coverings** in every shape and pitch: a black marble roof (Mistlands) and a grausten roof (Ashlands), which the ash rain wears only down to a tenth of its health.
- **Eitr on food.** Stone Pot dishes can give eitr; the two eitr dishes have an `Eitr` setting.
- Ashlands plants keep off lava, and old land gets them too.

### Changed
- The slate roof's description no longer calls it the strongest roof (the grausten roof is stronger).
- The `QuietRoofBonus` setting text lists black marble and grausten.

## v0.124.1 - 2026-10-12

### Changed
- **For modders:** the four largest classes (`DogRegistry`, `DogCompanion`, `HudConsole` and `QuartermasterPanel`) are split into partial files by concern, and the release scripts are now in `tools/publishing/` (bump the version, build the package, publish it on GitHub). A new page, [Ideas for new content](docs/ideas.md), lists what could come next.

### Fixed
- **Norwegian texts for the settings that had none.** The party list, the Necromancer's Staff, the crafting panel's search and sorting, the hit and death log, the chest search range, the build smoke and the Desert Dragon's scales showed their English names and descriptions in the settings window, with no Norwegian section for the party list and the staff.

## v0.124.0 - 2026-10-11

### Added
- **`whitehilt_perf`**, a console command that measures the mod: `start` times all of its own code and counts this machine's network traffic per message, `report [rows]` shows the slowest methods (ms per second, calls, average and worst) and the busiest messages (bytes and messages per second, out and in), and `stop` ends it and removes every patch it added. The report also goes to the log. See [How the mod is built](docs/architecture.md#-performance-habits).
- **Performance reports on a dedicated server**: `[Diagnostics] PerfLogMinutes` (off by default) measures all the time and writes the same report to the log every so many minutes, as a server has no console. The log also notes how long the mod took to start, and which items, pieces and plants were slowest to make. See [Settings](docs/progression.md#graphics-and-diagnostics).

### Changed
- **The models, textures and sounds are a file next to the DLL now**, `whitehilt_foraging`, like the Decor Hammer's `whitehilt_decor`. The DLL shrinks from 55 to 11 MB, and the game no longer keeps two extra copies of the 44 MB bundle in memory while it starts: it reads from the file what it needs. A mod manager installs it by itself; by hand, copy all three files in `plugins/` (see [Installation](README.MD#-installation)).
- **The mod's own models are hidden in the distance**, as Valheim's own are: wild plants, props, decorations and pieces with a White Hilt model were drawn, with their shadows, as far as the game draws anything. They now disappear when smaller than 1.5% of the screen's height (`[Graphics] ModelCullSize`, 0 to never hide them).
- **Less network traffic.** The party list sends health in whole numbers and without the player's name (the game's player list has it), about 26 bytes instead of 50, and stamina or eitr alone, which change all the time while running, at most every 1.5 seconds instead of twice a second. The server's lists of portals, map areas and Munin's finds are gzipped when they are larger than 1 KB. **The server and every player need this version**, as the messages changed.
- **Big bases load with less work**: building pieces no longer each get a ship-furnishing part unless they stand on Skidbladnir, and paint and dye no longer register their own message on every piece; one message each now serves all pieces.
- Creatures' AI no longer looks for dog, Kraken, Lindorm or Dread parts on every creature each frame while none of them are about.
- Models, textures and sounds are looked up by name in a list made once, instead of listing the whole bundle for every one, which makes the start a little faster.

### Fixed
- The map's exploration overview no longer redraws itself every frame while it is open but still locked.

## v0.123.0 - 2026-10-11

### Added
- **The White Dragon has new powers** ([Dragon Horn](docs/monsters.md#the-dragon-horn-a-white-dragon-at-your-side), all in `[DragonHorn]`):
  - **An entrance**: it arrives with a roar and dives at once at the foe nearest to you.
  - **Frost breath**: it breathes fire and frost by turns (`Breath`, `FrostDamage`), the frost from Moder's own ice shards. Frost slows what it hits and bites on foes that shrug off fire; its blast then bursts like the Deep North's frost meteor.
  - **Dives**: every 20 seconds it swoops low and fast at its foe and slams into the ground beside it, 600 damage before stars to every enemy within 10 m (`Dive*`).
  - **Terror roar**: every 30 seconds while foes are near, every foe within 20 m staggers and moves 60% slower for 4 seconds (`Roar*`).
  - **It guards you**: whatever hits you is its foe for the next 10 seconds (`GuardSeconds`).
  - **Dragon's Blessing**: within 30 m of your dragon you take a quarter less physical damage, and your health and stamina come back half as fast again (`Blessing*`).
  - **It grows with the world**: each of Yagluth, the Queen and Fader slain gives a dragon called afterwards 25% more health and damage (`PowerKeys`, `PowerPerKey`).
  - **A cold aura**: a pale light on its body and frost mist from its mouth (`Aura`).

### Changed
- **[The White Dragon](docs/monsters.md#the-dragon-horn-a-white-dragon-at-your-side) hits hard.** Its breath is 12 flames of 25 fire every 5 seconds (was 8 flames of 8 every 10), it has 1500 health before stars (was 600), and **each breath ends in a blast** where it first strikes: a burst like one of Yagluth's meteors that shakes the ground, deals 450 damage before stars to every enemy within 7 m, half its breath's element and half blunt, and throws them back (`BlastDamage`, `BlastRadius`, `BlastPush` in `[DragonHorn]`). It is half as big again as a Desert Dragon (`Size`) and roars twice as loud, heard twice as far (`RoarLoudness`). Players, tamed animals and buildings are still spared. It now **fights bosses** at your side too (`FightsBosses`, was off). Unchanged old defaults move to the new ones once.

### Fixed
- **Ship holds count again when building and crafting.** A ship or cart hold you had once asked for, and then came to own another way, such as by boarding the ship, never counted as yours again, so its materials were left out. It now counts as soon as it is yours. Blueprints, roads and moats no longer try to take a ship or cart over to pay from its hold; its owner sends what is needed instead, as the game passes a ship to whoever is aboard.

## v0.122.0 - 2026-10-11

### Added
- **Select a whole building at once.** In the group tools' selection mode, a double-click on a piece selects it and every piece joined to it, so a blueprint of a large house is Ctrl + A, a double-click and Ctrl + S instead of clicking every piece ([build tools](docs/build-tools.md#groups-blueprints-lines-and-areas), `JoinGap` and `MaxJoined` in `[BuildTools.Groups]`).
- **The build line shows what you have.** In the White Hilt HUD, each cost of the piece you are about to build is followed by how many you have, counting the chests you may use, or *unlimited* when a chest nearby keeps it so.

### Fixed
- **Blueprints, roads and moats are built straight from the chests.** Placing a blueprint, a copy, a line or an area first moved everything it cost from the chests into your inventory, so a large build left you overencumbered, and what did not fit fell to the ground and was then missing. Chests you hold are now paid from directly, and the chests other players hold are handed over to you instead of sending their contents. Roads, the other terrain tools and the stakes of a staked ditch pay the same way. Crafting and single pieces no longer move materials into your inventory from chests you already hold either.
- **A road goes on after it has waited.** A road that paused for want of stone, or while it waited for the chests, skipped the stretch it was on when you went on, and paused again at once. It now waits for the chests without pausing, and builds the stretch it could not pay for when you go on.
- **The round minimap shows the map again.** Only the player, the pins and the areas were drawn on it, not the land; the map's zoom and position now reach the round map too.

## v0.121.0 - 2026-10-11

### Added
- **[The Dragon Horn](docs/monsters.md#the-dragon-horn-a-white-dragon-at-your-side)**, sold by the Bog Witch once Moder is slain. Blow it and a five-star White Dragon flies in from behind you and fights at your side for 3 game hours, then flies off out of sight. The horn then rests 24 game hours. The dragon's breath is weaker than a Desert Dragon's, it leaves no ground fire, never burns buildings, leaves bosses to you and drops nothing (`[DragonHorn]`). Its call is a real Viking horn, sounded as four horns at once in a long hall, which rings on after the call.

### Changed
- **The Desert Dragon keeps flying while it breathes fire.** Its wings froze in a glide for the whole breath; they now keep beating while it lowers its head and opens its jaws. The Black Dragon, which is made from it, flaps too.
- **The Desert Dragon roars.** Its idle, alert, attack, injury and death sounds are deep roars and growls made from CC0 recordings, slowed down and deepened, with the beat of its wings in its idle sound. The Black Dragon has them too.

## v0.120.0 - 2026-10-11

### Added
- **[The School Bench](docs/school.md)**, the map table for what you know: a long table set with open scrolls, wax tablets, open books, inkhorns and rune staves, with a bench in front. *Write what I know* writes your skill levels, the materials and crafting stations you know and the runestone texts you have read in it; *Read* teaches you what others wrote, and with it their recipes and pieces. Sitting on the bench studies the skill you are furthest behind in, up to half the best level written (`[School]`). A material is only taught if you have been to a biome where it is found, so nobody skips the progression by reading. Black Forest tier.
- **[Shared blueprints](docs/build-tools.md#shared-blueprints).** The blueprint panel has a *Shared* tab with the blueprints everyone has shared in this world, kept on the server: *Share* one of yours, *Fetch* a copy of someone else's, and *Unshare* your own (only the one who shared it can). By default this is done at a School Bench (`[BuildTools.SharedBlueprints]`). The server checks every blueprint's size, pieces and the sharer's quota.
- **[Darkwood doors, walls and gables](docs/building-pieces.md#darkwood-doors-walls--gables)**, cut from the vanilla Darkwood Gate: the **Darkwood Door** 3 m and 4 m high (the gate's leaf with the arch straightened into three panels, its hinges and ring, on its post under a lintel), **Darkwood Walls** 2 m wide and 1 to 4 m high whose rails line up with the doors, and **Darkwood Gable Walls** for the 26°, 45° and 67° roofs. Plains tier.
- **The Darkwood Double Gate**: both leaves of the darkwood gate under one arch as one piece, 4 m wide, so the two halves need not be placed against each other. Both leaves open together.

### Fixed
- **The White Hilt Axe and Pickaxe work in every biome.** They kept the tool tier of the iron axe and pickaxe they are cloned from, so in the Mistlands the game said the wood and stone were too hard, and you needed the black metal ones. They now have the highest tier of the game's axes and pickaxes, as the [White Hilt tools](docs/equipment.md#-tools) are meant to last the whole game.
- **Looted gear goes back on.** After logging out and in again without quitting the game, the build tools' axis lines threw an error every frame, which stopped the [equipment slots](docs/backpack.md) and food slots from doing their work. Armor taken back from your grave lay in its slots without being worn, and you had to take it out and put it on again. It is now worn as soon as it is back in its slot.
- **The Gifts of the gods say what they do.** Odin, Freya, Loki, Hugin and Munin showed only their own name as their effect, and most others a vague line such as "Enhanced crafting abilities". Every [potion](docs/potions.md)'s tooltip now names what the game does not list by itself, with the numbers from the server's settings: Fenrir's attack speed, stamina and life steal, Thor's chopping, mining and stamina, Kvasir's skill gain, Ullr's bow damage, Hel's trigger, Heimdall's and the Völva's reach, and so on. Hugin's and Munin's descriptions say what they give, and Loki's lost a stray word.
- **An upgraded item no longer drops out of the upgrade list.** Right after an upgrade paid partly from chests, the list was rebuilt counting only the chests just taken from, so a next level the other chests could pay for looked unaffordable and vanished from a list filtered to Craftable (or sank to the bottom with `CraftableFirst`) until you switched tabs. The list now counts every chest around you again.
- **The world unloads again after building.** Leaving out the puff of smoke when a piece is built removed some puffs (such as a wood pole's) the wrong way, as they are shared with the other players. Once you walked away, the game threw an error every frame and stopped unloading the world behind you. The puffs are now removed the way the game removes them.

## v0.119.2 - 2026-10-11

### Fixed
- **The Mast Wisp clears the Mistlands mist again.** The mist keeps clear of a sphere round a wisplight's force field, 15 m across its reach; at the top of the mast, 15 m up on the White Hilt Ship and 21 m on Skidbladnir, that sphere hardly reached the deck and none of the water, so the mist stayed. The light still sits at the top of the mast, but the part that keeps the mist away is held at the ship, 3 m over its waterline under the mast, so the mist clears round the whole hull as a wisplight clears it round you.

### Changed
- **The Mast Wisp reaches further with Exploration.** It keeps the mist away and thins the fog 15 m round the ship at Exploration 0, growing evenly to 30 m at 100 (`MastWispReach` and the new `MastWispReachMax` in `[Ships]`). Each player sees it by their own level.

## v0.119.1 - 2026-10-11

### Changed
- **Empty food places stay.** The White Hilt HUD keeps three places for food whether you have eaten or not, so the console keeps its shape: an empty place is a dark disc in a red ring with a red cross, and with nothing eaten at all the red rings pulse slowly. Empty places show even with the quiet screen.

## v0.119.0 - 2026-10-11

### Added
- **A round minimap.** In the White Hilt HUD the minimap is round, in a dark compass ring with N, E, S and W. Your pins, the bosses you know of, your death and the waypoint that lie beyond what the map shows sit on the ring in the direction they lie, and the biome's name stands under it. `Minimap` in `[HUD]` (`Round` or `Vanilla` for the game's square map), `MinimapRingMarkers` and `MinimapRingOpacity`.

### Fixed
- **The White Hilt HUD stays at the bottom on a ship.** At the helm the console rose to the middle of the screen, as it measured the game's ship controls by their container, which spans far more of the screen than the rudder and sail icons in it. It no longer moves at all: it stays at the bottom on a ship as on land, where the rudder and wind indicator at the lower right leave it room.
- **The strip over the console sits clear of it.** The line for the ship, the piece you build, the creature you fight and warnings lay a few pixels into the panel behind the console; it now stands just above the panel, with rounded corners like it (`PanelRadius`).
- **The weather icon is inside the band.** The clock placed its weather icon half outside its own box, so it sat on the left edge of the band behind the top. It now sits within it, on a dark disc of its own, and the markers on the compass (pins, bosses, your death) sit on dark discs too, so they read against a bright sky.

## v0.118.0 - 2026-10-11

### Added
- **More in the White Hilt HUD.** Worn gear you have on shows among the status effects under a fifth of its durability, flashing red when broken (`ShowWear`, `WearWarning`), and your load from 85 % of what you can carry, red when too much (`ShowWeight`, `WeightWarning`). The strip over the console names the creature you fight with its stars, health and what it is weak to, resists and is immune to (`ShowTarget`), and the stations near you that have stopped, need you or are done (`ShowProduction`, `ProductionRange`).
- **Where a hit came from.** A soft red arc at the edge of the screen shows the side a hit came from, also from behind, turning with the camera as it fades; with either HUD (`DamageDirection`).
- **A panel behind the console.** A half dark rectangle with rounded corners lies behind the White Hilt HUD's bars, food, hotbar and status effects, so they read against snow and a bright sky; it fades with the quiet screen. `PanelOpacity` in `[HUD]` (0.45; 0 for none).
- **A band behind the top.** The clock, the compass and the waypoint sit on one rounded, half dark band, as the console does, so they read against a bright sky (`TopBackground` = `Band`); `Fade` darkens the top of the screen instead, fading out downwards over `TopFadeHeight`, and `None` leaves it clear. `TopOpacity` sets how dark, and `PanelRadius` the corners of both the band and the console's panel.
- **Ready-made HUD layouts.** `Preset` in `[HUD]`: Compact (smaller, with the quiet screen), Standard and Large (larger text and icons and the key hints always shown, for a TV or a large screen) set the console's, compass's, clock's and party list's sizes at once; Custom changes nothing.

### Fixed
- **The Ridge Dragon, Ridge Turret, Dragon Portal and Stave Church Portal load again.** They use the longship's carved dragon head, which the piece builder could not find in the game, so the four pieces were missing from the hammer with an error in the log.
- **No more "Missing prefab hash" in the log** after travelling through a White Hilt portal: its arrival and departure sounds carried a network object that no other client could place, hundreds of warnings a session.
- **No more "Particle Velocity curves must all be in the same mode"** from the portal's rising sparks and dust, nor "LiberationSans SDF Font Asset was not found" from the craft amount, the requirement counts, the chest panels and the discovery badges.

## v0.117.0 - 2026-10-11

### Added
- **Darkwood Roof Cross 26°, 45° and 67°.** Valheim has the Wood Roof Cross for each roof pitch but none in darkwood: these are the same crosses of two carved darkwood beams, snapping and carrying as the wooden ones, for Wood ×2 and Tar ×1, after the darkwood beams in the hammer.
- **Darkwood Beam 1 m and Darkwood Pole 1 m**, as the wooden beam and pole have, at half the cost. With the crosses, darkwood now has every beam and roof the wooden family has. Valheim's own Darkwood Beam 67° gets the name it lacks in the game's translations.

### Changed
- **Laft walls close at the joints.** The logs of the laft walls, corners, gables and the stream mill were scaled so their rounded ends met where two walls meet, leaving a notch between every pair of logs. The full log now runs from end to end of the wall, and the rounded ends lie inside the next wall's logs, so a row of walls reads as one wall. Snap points are unchanged.
- **Fewer Desert Dragons, more from each.** A dragon comes over the Plains about a third as often (`SpawnChance` 5 instead of 10, every 450 s instead of 300), drops its trophy at 35 % instead of 10 % and 4 to 6 scales instead of 2 to 3, set with the new `ScalesMin` and `ScalesMax` in `[Desert Dragon]`. Existing configs move to the new spawn and trophy defaults once.
- **Key hints beside the console.** In the White Hilt HUD the game's key hints for fighting, building, the inventory and fishing no longer lie over the hotbar and the status effects when you take out a bow or a tool: they stand at the right of the console, and the build tools' hint at its left, each made smaller to fit, or over the console on a narrow screen. They show for a few seconds after you take out a tool or weapon and then fade (`KeyHints` = `Brief` in `[HUD]`, with `KeyHintSeconds`); `Always` keeps them and `Hidden` shows them only in the inventory and while the `ShowAll` key is held.
- **What the icons are.** The last place of a full status tray goes through the effects left out in turn, with how many they are in its corner, instead of only "+3". In the inventory, and while the `ShowAll` key is held, the status effects are a list with each one's name and time.
- **Rings that warn.** The rings of meads and other timed effects, like the food's, turn yellow when less than half is left and red under a fifth, and a red ring pulses.

## v0.116.0 - 2026-10-10

### Added
- **The White Hilt HUD.** Health, stamina, eitr and adrenaline, the food, the guardian power, the hotbar and the status effects are gathered in one console at the bottom centre: the hotbar in the middle with the bars and their numbers over it, the food and the guardian power to its left and the status effects, other mods' among them, to its right. The clock and the waypoint arrow sit at the two ends of the compass, the party list at the left and the arrow count over the bow. The console rises above the ship controls when they are under it. `Style` in `[HUD]` switches back to the game's own HUD (`Vanilla`); `Scale`, `BottomMargin`, `StatusColumns` and `MaxStatusEffects` set the rest.
- **A context strip over the console**, up to three lines over the bars in the White Hilt HUD. While you steer a ship, or ride one sailing its route, its speed, heading, depth, wind, course and time to arrival. In build mode, the piece and what it costs, each amount red while too little and in White Hilt's colour when nearby chests make up the rest, instead of the game's build panel. A raid or event while it lasts, instead of the event bar. And for a while the warnings that were messages in the middle of the screen: night falling and a storm coming.
- **Countdown rings.** Food and status effects in the console sit on dark discs with a ring that empties as their time runs out; the food's ring goes from green to red.
- **Companions in the party list.** Your dog and the tamed animals following you are listed under the players, nearest first, with name, distance and health (`ShowCompanions`, `MaxCompanions` in `[Party]`), with either HUD.
- **Keys for the HUD.** `[HUD.Keys]` `ToggleStyle` switches between the White Hilt HUD and the game's own; `ShowAll` shows the whole console while held, with `Calm`. Both unbound at first.
- **A quiet screen.** With `Calm` in `[HUD]` each part of the console fades while it has nothing to tell: a full bar, food not yet half gone, status effects that have not changed for a while, the hotbar between uses. `CalmSeconds` and `CalmOpacity` set how long they stay and how much is left.
- **Room for other mods.** Their status effects are in the console as they are; other things they want shown go in through `WhiteHiltHud`, as a line in the context strip or an icon among the status effects. Elements they put on the HUD where the console is are lifted into a band over it, so they do not cover the bars or the hotbar.

## v0.115.3 - 2026-10-10

### Added
- **Switch accessories off where they lie.** Right-click a Megingjord, Wisplight, Wishbone or other accessory in one of the five accessory slots to switch it off: it stays in its slot, faded, and gives nothing until you right-click it again. It stays off across sessions; dragged out of the row it is on again when next worn. Taking an accessory off is now a drag out of its slot.
- **No puff of smoke when you build.** Building a piece no longer shows the game's cloud of dust and smoke, only the hammer's knock is heard. `PlaceSmoke` in `[BuildTools]` brings it back; it is your own setting, off by default.

## v0.115.2 - 2026-10-10

### Fixed
- **Friezes go onto the wall's face.** A carved frieze or post board snapped to a wall sat inside it, as Valheim's walls snap on their centre line and the frieze on its back. Snapped to a wall or a post it now moves out onto the face it faces, whatever the wall's thickness.
- **Log corners turn to the wall.** A log corner snapped to the end of a log wall turns itself so its logs follow that wall; the plain or the mirrored corner decides which way the other wall goes.
- **The brass candleholders have lost the flames Poly Haven modelled on their candles**, which burned as solid shapes inside the real flames. The candelabrum and the three-armed and plain candlesticks keep their size, and their flames stand on the wicks.

## v0.115.1 - 2026-10-10

### Fixed
- **Spiral stairs stack.** A spiral stair turns three quarters of a turn per storey, so the next one up must start a quarter back, but Valheim's snapping never turns a piece and stacked stairs came out with their treads crossing. A spiral stair snapped onto another (or under it) now turns itself to go on where the other ends, so a stairwell climbs as many storeys as you like.

## v0.115.0 - 2026-10-10

### Changed
- **Decor Hammer sizes after Valheim's own.** 84 decorations stood at their real size and looked lost beside the game's things, whose tankard is 0.26 m high and onion 0.27 m: tableware and food are now about twice life size, tools and weapons about one and a half, candles and lamps in between. The Carved Stand (once the Low Table, 0.3 m wide), the Low Stool, the Anvil, the Wall Plank and Moss were also too small.
- **Flames where the candles are.** A decoration can place a flame on each candle: the Brass Candelabrum burns seven, the Three-Armed Candlestick (once the Double Candlestick) three and the Chandelier five at the ends of its arms instead of one in its bowl. The wall candle, wall lamp and sconce burn on the candle instead of between it and the wall, the wooden lantern behind its glass instead of under its roof, and the cooking grate's fire burns under the grate instead of round the pot.
- **The Svalgang is 2 m deep** instead of 1.8 m, a vanilla floor's width: its outer edge and its corner (now 2 × 2 m) snap to Valheim's 2 m grid, so floors, posts and railings along it line up with the rest of the building. Its roof meets the wall 3.25 m up.

## v0.114.0 - 2026-10-10

### Added
- **[Carved friezes and post boards](docs/carvings.md)** in four patterns (plait, cable, ring chain and Urnes loops), their strands passing over and under each other between raised rims, and a **Carved Portal** 2 × 3 m with Urnes loops up its posts and a ring chain over the door.
- **Carvings for the [Decor Hammer](docs/decor.md)**, from CC BY models: a carved dragon head on a curving neck, the bow and the stern of a Viking ship rising into spiral stems as on the Oseberg ship, and a scan of the Asferg runestone from Jutland, in a small and a large size.

### Changed
- Models in the mod's own asset bundle can carry a normal map (`<name>_normal`), for carved relief that keeps its edges.

## v0.113.0 - 2026-10-10

### Added
- **[Bridges, jetties & quays](docs/harbour.md)**, building pieces for the water's edge that snap end to end:
  - Jetties of 4 and 2 m on tarred piles reaching 3 m down, and a jetty head with bollards and a stair down to the boats.
  - A stone quay with a cope, a timber fender, an iron ring and a bollard, its outer corner, and steps down along its face.
  - A log bridge of 4 and 8 m with a log railing, a rope bridge of 8 m that sags in the middle, and a stone bridge of 8 m over an arch 6 m across.

## v0.112.0 - 2026-10-10

### Added
- **[Arches, columns and finishing stone](docs/stonework.md#arches-columns-and-finishing-stone)**, each in stone, black marble and grausten (30 pieces), built at the Stonecutter and hard like the stone defences: an arched doorway 2.7 m to its crown, an arched window at eye height, a great arch 4 × 4 m, a wide stair, a cornice and its corner, a column, a balustrade and gables for the 26° and 45° roofs. The arches are rings of wedge stones with a keystone, and their openings are round and free to walk through.

## v0.111.0 - 2026-10-10

### Added
- **[The farm](docs/farm.md)**:
  - The skigard, the Norwegian round-pole fence, in 4 and 2 m: sections run on into each other without a seam, and the last one slopes down to the ground. A farm gate and a double gate 4 m wide for a cart, which shut by themselves like every door.
  - A wattle fence, a wattle-and-daub wall in 2 and 1 m (the Paint Bench can whitewash it) and a bare wattle wall.
  - A well sweep: use it and it dips its bucket straight down into the well.
  - A stream mill: a little mill house on stone feet with a horizontal wheel under its floor. It grinds barley into flour like the windmill, at full speed whatever the wind, and its wheel turns while it grinds.
- Swinging parts can turn less than the door that drives them, and a part can hang from another and follow it without turning, as the well's bucket does from the sweep.

## v0.110.0 - 2026-10-10

### Added
- **Doors 3 m high** (in [the log house](docs/log-house.md#-doors)), so you walk through upright: your character is 1.85 m tall, and the vanilla door's leaf 1.88 m. A tall plank door, a double door opening from the middle, hall doors 4 m wide with iron straps, and a stave church portal with a round-headed door and dragon heads. They snap in place of a wall and the metre over it, like the vanilla gate; a new low log wall fills over them.
- **[Stave walls](docs/log-house.md#-stave-walls-and-the-svalgang)**: tarred boards on end between a sill and a wall plate, as on the stave churches, 1, 2 and 4 m long, 4 m high for a hall and 1 m high over a door; round corner staves; gables for the 26° and 45° roofs; a stave wall with a shuttered window and one with a round glugg high up.
- **The svalgang**: the covered gallery round a stave church, with a low wall, an arcade of small posts and arches and a lean-to of dark shingles, and its outer corner.
- **[After Borgund stave church](docs/log-house.md#-after-borgund-stave-church)**: a ridge dragon on a tall curved neck, a ridge turret with three tiers, a belfry and a spire, carved ridge crests and gable crosses; stave walls clad in scale shingles; nave columns with a capital and masks, arcade arches, St Andrew's crosses with a sun and leaves, and scissor trusses; a svalgang porch with lattice in its gable; a free-standing stave bell tower whose bell rings like the alarm bell, also by itself when a raid comes; and a painted consecration cross.
- The Stave Church Portal has half-columns with a base and a capital, as on Borgund's west portal.
- **More windows**: log walls with a narrow window and one shutter, and log walls with a window behind iron bars, in both kinds of wall.

### Changed
- **Windows at eye height**: the log walls' windows lie round a player's eyes, from 1 to 2 m in the plain wall and 0.77 to 1.73 m in the offset wall, instead of from 0.5 m; the gluggs sit high up, where the light comes in.

## v0.109.0 - 2026-10-10

### Added
- **Windows, stairs and a cellar for [the log house](docs/log-house.md)**:
  - Log walls, plain and offset, with a window 1 m wide closed by two shutters. The shutters always swing out against the wall, whichever side they are opened from, and close like other windows when rain, a storm, night or a raid comes.
  - Log walls, plain and offset, with a *glugg*: a small framed opening one log high, as in the oldest houses.
  - A narrow stair 1 m wide that reaches the next storey in one piece, and a spiral stair round a post that rises 2 m in three quarters of a turn.
  - A loft ladder, railings of 2 and 1 m and a railing for the slope of the stairs.
  - A floor hatch whose lid lifts, with a ladder 2 m down to the cellar, and a dry-laid cellar wall 2 m high.

## v0.108.0 - 2026-10-10

### Added
- **[The log house](docs/log-house.md)**: build the Norwegian way, with *laft*.
  - Log walls of core wood in 1, 2 and 4 m, in two kinds whose logs lie half a log apart, so the logs of two walls that meet cross at the corner as in a real log house. Also a low wall for under a 26° gable.
  - Log corners whose log ends (*laftehoder*) stick out past the corner, a mirrored corner for the other two corners of a house, and a joint where an inner wall's logs run through the outer wall.
  - Log gables for the 26° and 45° roofs, with barge boards.
  - A dry-laid stone foundation in 2 and 4 m, 1 m high and reaching into the ground, and a cornerstone.
  - Plank floors of 4 × 4, 4 × 2 and 2 × 1 m, and a 4 × 4 m floor on joists for a loft.
  - A plank door, a dragon portal and barn doors wide enough for a cart, which snap in place of a wall like the vanilla door and close by themselves like every door.

## v0.107.0 - 2026-10-09

### Added
- **A recipe list you can find your way in**, at every station, the game's own included (`[CraftingPanel]`): a search field above the list finds recipes by name or by what they are made of; a **Craftable** button lists only what you have the materials for, counting the chests around you (`OnlyCraftable`); tabs for weapons, armour and gear, tools, ammunition, food and meads, and materials, read from each recipe's item so other mods' recipes are sorted too; and **favourites**: right-click a recipe to give it a gold ★ and keep it at the top, saved on the character. `RecipeTools` turns the search and tabs off.

### Changed
- The recipe list is sorted by name, favourites first and upgrades with the highest level first (`SortByName`, on). `CraftableFirst` puts what you can craft now first, as the game did; with `SortByName` off the game's order is kept.

## v0.106.0 - 2026-10-08

### Added
- **A party list under the hotbar** (`[Party]`, on by default): the other players, nearest first, each in a compact row with their portrait (or their initial), name, distance and up to four status effects (burning, poison and frost first), and under that health, stamina and eitr as bars with numbers, `184/250`. A row flashes when its player takes a hard hit, the name turns red and the bar pulses under a quarter of the health, and a dead player's row fades until they respawn. It sits below the arrow count under the hotbar and is hidden in the inventory, in build mode, on the large map and in menus. Each player sends their own status at most twice a second and only when it changes, so the list works at any distance; the distance is shown only for players nearby or sharing their position on the map. `ShowStamina`, `ShowEitr`, `ShowDistance`, `ShowEffects`, `ShowSelf`, `MaxPlayers`, `Scale` and a toggle key set it; a server can turn it off with `AllowParty`. Every player needs this version to be shown.
- **A death recap in the log** (`[Difficulty] LogDeathRecap`, on): when you die, a `[DeathRecap]` lists the last twelve hits however small, burning and poison included, the effects you had, and health lost between hits without a hit, such as a maximum that fell when food or Gift of Odin ran out.

### Changed
- The `[BigHit]` line shows your health with its maximum, before and after the hit.

## v0.105.1 - 2026-10-08

### Added
- **A log line for hard hits**: when you lose more than 100 health in one hit (`[Difficulty] LogHitsAbove`, each player's own; 0 turns it off), a `[BigHit]` line in the BepInEx log tells who struck, at which level and stars, the mod's and the game's damage factors (players nearby, the world's combat setting), your armour and the damage before and after it. Fire, poison and spirit come over time and are shown as they arrived.

## v0.105.0 - 2026-10-08

### Added
- **71 new decorations for the Decor Hammer**:
  - Kitchen: a cask rack with tapped casks, brass pots, pans and goblets, clay jars, a mortar and pestle, an iron pot, trivet, ladle, rolling pin, knife, cups, plates and spice pots.
  - Home: rose-painted furniture in the old Scandinavian way (bench, chairs, stool, cupboards, side table, shelf and long table), a carved high seat, bed, cupboard and chest of drawers, a worn bookshelf, a round hall table and an iron-bound chest.
  - Workshop and jetty: axes, a broom, pier decking, pier posts and mooring posts.
  - Lights: brass candlesticks and a candelabrum, a tallow candle and a candle sconce.
  - Weapon wall: a kite shield, an ornate dagger, mace and war hammer, an estoc and a bronze lion's head.
  - Wilds: shore rocks, roots, loose bark, a low thicket and young firs and pines.

### Changed
- The Rune Etching Table, the Repair Anvil, the Oil Cauldron, the Net Winch and the Valkyrie Stone take what they cost from nearby chests too, like crafting, instead of only from your inventory.
- The Paint Bench shows each dye of a mix and the resin with its icon and how many you have, or ∞ when a chest in range never runs out of it.

### Fixed
- **`BurningFlames` hardly toned anything down**: the big flames of a burning creature use a shader that takes its colours from its own gradient and adds them to the screen, ignoring the fading, and even half of them overlapped into solid white. Those flames now become fewer and smaller with the setting (at 0.5 a quarter as many, half as large), so a monster shot with fire arrows can be seen through its flames. The blue, green and undead kinds of burning are toned down too.
- **Forageables were far rarer than meant**, worst of all Bog Iron and Peat in the Swamp. A zone got at most one or two tries at a random spot, and a try that missed the biome, height or slope placed nothing, so a whole Swamp could hold only a handful. Every forageable now gets one or more groups in each zone with suitable ground (most one or two; Chanterelle, Porcini and Reed up to three; Ergot, Henbane, Wild Hops and Wolf Lichen one), and Bog Iron also grows on the wet mud of the banks. Land explored before is filled once more when the server starts, only in zones that have none of the plant. Slate outcrops come up in as many Mountain zones as `SlateOutcropPerZone` says. The setting `SpawnPerZone` is replaced by `GroupsPerZoneMin` and `GroupsPerZoneMax`.
- The Paint Bench and the build and terrain tools sometimes said you lacked materials you had in nearby chests until you opened one. In multiplayer they counted only the chests already handed over to you. Blueprints, copy and paste, lines and areas of pieces, planting grids, undo, the terrain tools, the hoe's big brush, moat stakes, dyeing banners and sails and the Paint Bench now count every chest in range and fetch from the others when you use them, as crafting and building do, and go on by themselves once the items have arrived.

## v0.104.1 - 2026-10-08

### Fixed
- The map compass disappeared after leaving a world and joining another, and filled the log with an error every frame.
- A Glow Rune's glow was faint at first, and from quality 5 the game's own upgrade glow shone through it in a colour of its own. The etched glow is now stronger and takes the place of the upgrade glow.
- Etched glows and flames went out when the weapon or shield was put on the back. They now stay on.
- A Flame Rune's colour did not reach the White Hilt Sword's own fire, nor the flames it gives other gear, which kept their usual orange.

## v0.104.0 - 2026-10-07

### Added
- **Attack styles**: the White Hilt melee weapons no longer swing the same way every time. Each has a signature style and one or two others that suit its grip (a Gladius stabs and lunges, a Falchion chops, a War Axe hews from above), and every new combo draws one of them. Damage and stamina per second stay the weapon's own in every style. The tooltip lists the styles. The server chooses Varied, Signature or Vanilla for everyone, and each player can choose for themselves. See [Attack styles & animations](docs/attack-styles.md).
- **The Rune Sword's own cuts**: a rising overhead strike, a lunging thrust and a leaping whirl, new animations made for the mod and seen by every player.

## v0.103.0 - 2026-10-07

### Added
- **Decorations in more sizes**: barrels, crates, baskets, pots, sacks and chests, stones, stumps and logs, bushes and trees, runestones and the dolmen can also be built small (0.6 times) and large (1.5 times), and the cart wheel twice the size. A copy costs about half, one and a half or twice as much. 199 more pieces in all.

### Fixed
- **Decor candles and lanterns burned with campfire-sized flames.** Their flames were shrunk only where they rose from, not in size; now they are truly small: a few centimetres on a candle, a little more in a lantern, half a campfire in a fire pit or forge.
- **Nothing could be set on a decoration**, not even a pot on a decor table: decorations told Valheim they carry nothing, and it refuses to place anything on such a piece. Solid decorations (tables, barrels, crates, shelves, chests, stones) now carry what is set on them; plants and cloth still do not. A decoration also no longer wears down without support, as was meant.
- **Decor seats**: you sat about half a metre too high, on top of a chair's back, since the sitting animation lifts the body above the seat point; it now sits on the seat as on Valheim's own chairs. The Wooden Chair no longer has you facing into its back.
- The decor stools, the Long Bench and the Wooden Chair are a little bigger, near Valheim's own (stools 0.55 m, the chair 1.2 m, the Low Stool 0.3 m).

## v0.102.0 - 2026-10-07

### Added
- **Twelve more White Hilt weapons and shields**, each with its own model, from materials only (no vanilla weapon in the recipe, so none hides until one has been carried):
  - Swamp: **Viking Sword** (the last blow of a run strikes three times as hard), **Hand Axe** (more chop than slash), **Round Shield**.
  - Mountains: **Falchion** (sweeps half again as wide), **Bardiche** (a slashing pole-axe).
  - Plains: **Gladius** (slash and pierce, backstab bonus, cheaper blows), **War Axe** (two-handed), **Pike** (half again the reach), **Recurve Bow** (drawn in 60% of the time), **Hird Shield** (half again the parry bonus).
  - Mistlands: **Pike Axe** (its gilded head glows), **Bec de Corbin** (breaks armour).
- The **White Hilt Trident** has a model of its own and flies as itself when thrown.
- **Flame Rune**: smithed at the Rune Forge, coloured at the Paint Bench and etched at the Rune Etching Table like the Glow Rune. The White Hilt Sword's Dyrnwyn fire and the staffs' flames take its colour, a blue flame for instance; other White Hilt gear catches fire along its head or blade.
- At the Rune Etching Table, with White Hilt gear in hand, **Shift + Use** takes the glow off and **Alt + Use** puts the flame out (own flames included) or brings it back as it was, both free.

### Changed
- White Hilt gear above quality 4 no longer glows brighter and changes colour with every level: the vanilla upgrade sparkles stay as at quality 5. An etched glow is a little softer.
- The Paint Bench's colour wheel has a button for each rune: **Glow Rune** and **Flame Rune**.
- The colour wheel says what each button did, in the window above the buttons and with the colour used: the message used to show in the middle of the screen, hidden behind the window.
- The Glow Rune and the Flame Rune are runestones of their own instead of rings like the etching runes, so they are told apart at a glance. Their carved sign glows in the rune's colour, also on the ground.

## v0.101.1 - 2026-10-06

### Fixed
- **Decorations looked see-through and shiny.** Wood and stone decorations took their material from the snow cap of the vanilla piece they copy (Valheim's snow shader) instead of its wood or stone. They now get the piece's own material and layer, so they are solid, weather and take light like the buildings around them. The wall drawers and the ship workshops had the same fault and are fixed too.
- Glass and water that a model made see-through, such as the water in the Quench Tub, vanished in the game; they are now drawn solid.
- The decor page shows the Textiles gallery too.

## v0.101.0 - 2026-10-06

### Added
- **White Hilt Crystal Axe**: the White Hilt Battleaxe made one-handed, so a shield goes with it. It swings like the Iron Axe, has a lilac head with the Crystal Battleaxe's glow, and deals +20 spirit damage like it (`[Gear.Weapons] CrystalAxeSpiritDamage`). Forge level 2: Iron ×20, Crystal ×10, Iron Axe ×1; it unlocks with the Mountains.
- **Four new White Hilt weapons for the Swamp**, each with its own model and white grip:
  - **Bearded Axe**: a one-handed axe whose beard hooks shields, staggering a target with a shield 1.5 times as much.
  - **Seax**: a knife that cuts deeper than the White Hilt Knife and makes its target **bleed** (4 damage per second for 6 seconds, +1 per quality level; `[Gear.Weapons] Bleed*`).
  - **Javelin**: a light spear made for throwing: faster, harder and cheaper throws, a weaker stab.
  - **Throwing Axe**: hews like the Iron Axe and is thrown like a spear, spinning end over end.
  - The javelin and the throwing axe fly as themselves instead of as a vanilla spear.
- **Six White Hilt weapons for later biomes**, with that biome's materials, strength and unlock:
  - Mountains: **Dane Axe** (two-handed, a quarter more reach), **Flail** (30% of blows cannot be blocked), **War Hammer** (blunt and pierce, mines ore like an iron pickaxe) and **Ice Sword** (frost that slows, a blade that glows icy blue).
  - Plains: **Morning Star** (breaks armour: the target takes 15% more damage for 8 seconds) and **Halberd** (25% more damage against large creatures).
  - Mistlands: **Claymore** (two-handed sword), **Scythe** (slash and spirit, heals you for 5% of the damage), **Crystal Staff** (a third stronger than the Staff of Ice, glowing cyan) and **Wand** (one-handed fire staff for half the eitr, with a shield).
  - Ashlands: **Rune Sword** (runes that glow) and **Trident** (lightning, 30% more damage against foes in the water and sea creatures; Splitnir's look for now).
- **Glow**: a **Glow Rune** from the Rune Forge, coloured at the Paint Bench (**Colour Glow Rune** in the colour wheel) and etched at the Rune Etching Table, makes any White Hilt weapon or shield glow in that colour, bound or not. The blade or head glows and sheds a soft light, and everyone sees it. A black rune puts the glow out.

## v0.100.0 - 2026-10-06

### Fixed
- **Crafting and building from chests said there was not enough in multiplayer**, even with unlimited chests, when another player held the chests. The cause was older than the chest crafting itself: the patch that prepares every chest for multiplayer was never applied, because Harmony skips a patch class without its own `[HarmonyPatch]`. So no chest could be asked for, or answer a request for its items. This also brings to life what was built on it: the Collection Post and the Quartermaster's Table now get chests handed over from other players instead of using only the ones you hold, the Collection Post shows its hover text and pauses with Alt + E, and items players drop are left alone by it also after the area has been reloaded (unless `PlayerDrops` is on). A test now fails if a patch class would be skipped.
- A chest whose holder does not answer within 4 seconds is now asked to be handed over instead and taken from then, and the log says who held it.

## v0.99.1 - 2026-10-06

### Fixed
- **Crafting and building from chests no longer keeps you waiting in multiplayer.** A player whose chests another player held was told to try again for ever. This was first put down to two players handing the chests to each other; the real cause, found in 0.100.0, was that the chests were never handed over at all, because Harmony skipped the patch that prepares them. The chests now stay where they are: the player who holds a chest takes out what you need and sends it to you. Meanwhile you see *Fetching from the chests...*, and the crafting or placing then goes on by itself, with no second click. If a chest does not answer within 4 seconds (`[ChestCrafting] HandoffTimeout`), it goes on with what has arrived, and a piece is never placed half paid for.

## v0.99.0 - 2026-10-06

### Added
- **Skidbladnir can be switched on and off** (`[Ships.Skidbladnir] Enabled`), and is **off by default** until it has been tried more in multiplayer. Off, Skidbladnir, the White Hilt Ship Hammer and the ship workshops cannot be built or crafted, and nothing new can be built aboard; what is already made is kept.

### Changed
- The flames on anything burning are toned down to half, so a monster set on fire by White Hilt arrows can still be seen (`[Gear.Ammunition] BurningFlames`, each player's own; 1 as in the game, 0 none).
- Getting ready to build or craft asks only for the chests that hold what the selected piece, recipe or station needs, no more than make up what is missing, and leaves a chest just handed to another player with them for 20 seconds (`[ChestCrafting] HandoffKeepSeconds`). The Collection Post leaves such chests be too.
- A stack a chest keeps full without limit counts as enough for any amount, and is taken from again as it refills, so ten dishes at once work like one.
- Crafting that lacks materials now says *Not enough in your inventory and the chests nearby* instead of nothing.

### Fixed
- Building and crafting from chests no longer waits for ever on a chest that cannot be handed over: chests someone has open, ship and cart holds, and chests left marked as open by a player who logged out (now freed). After 4 seconds of waiting (`[ChestCrafting] HandoffTimeout`) it goes on with the chests at hand, and the log says which chests it waited for.
- Slate, soapstone, birch bark, straw and turf belong in a chest again (Stone and Material Chests) and show in the gathering overview where they drop: slate and soapstone in the Mountains, birch bark where birches grow, straw in the Plains and turf in the `TurfBiomes`. Before, the chests took them for unobtainable. Forageables that also drop from vanilla plants are listed in those plants' biomes too.
- The broken slate outcrop is registered with the game, so every player sees it and it stays after a reload.

## v0.98.0 - 2026-10-06

### Added
- **87 more decorations for the Decor Hammer**, 261 in all: real trees that sway in the wind (fir, pine, birch, autumn birch, oak, maple, ash, apple, cherry and plum) with shrubs and saplings; dead trees, fallen logs and rock outcrops; a smithy with anvil, stone forge, smith's bench, quench tub, bellows, tongs and a tool wall; a market stall, handcarts, a cart wheel, pottery, sacks and crates; tables, chairs, stools and shelves; a chandelier, oil lamps and candlesticks; stocks, a quintain, skulls, swords and shields. Forges, cooking grates and lamps burn without fuel. Models from eight Sketchfab packs (CC BY), credited in the README.

## v0.97.0 - 2026-10-06

### Added
- **Decor Hammer**: a build tool of its own with 174 decorations under eight tabs: Garden, Wilds, Hearth, Workshop, Home, Textiles, Lights and Norse. Bushes, ferns, flowers, young trees and mushrooms sway in the wind and bend when you walk through them; stones, stumps and logs; barrels, baskets, bowls and food; tools, fences, firewood and a trader's wagon; tables, stools and chairs to sit on; hanging cloth, banners and rugs; candles, lanterns and fires that need no fuel; runestones, graves, a dolmen, fuling totems and wrecked ships. Many are Valheim's own props and plants that could not be built before; the rest are models from Poly Haven and Sketchfab. A decoration shows up once you know its materials, needs no support and gives its materials back when removed. See [Decor Hammer](docs/decor.md).

## v0.96.0 - 2026-10-06

### Changed
- **New players start in linear progression.** A new config file now holds `Linear` for both `[General] Mode` (White Hilt gear unlocks biome by biome) and the chests' `Mode` (an item becomes unlimited once a chest holds a full stack of it). A config file that already holds a mode keeps it, so existing worlds and servers play on as before. If you play in Full and delete or replace your config file, set both back to `Full` before opening any chest, or the chests remove what is not unlocked.

## v0.95.2 - 2026-10-06

### Fixed
- **Climbing down a ladder no longer hurts**: going down from Skidbladnir's mast and lookout, or a tower's upper floors, counted as a fall from the height you left.

## v0.95.1 - 2026-10-05

### Fixed
- **Wall drawers and the ship workbench, forge and stonecutter** showed broad black and white bands on their tops and fronts, which looked like tilted boards with the tools floating over them: the light pine wood was not found in the game, and the old colour palette was stretched over them instead. The pine is now looked for among everything the game has loaded, and the plank wood in a light tone stands in should it still be missing.
- **Skidbladnir stops when everyone has left it**, as the other ships do, instead of sliding on.
- A build refused for its requirements writes to the log which requirement is missing, as the game shows one message for all of them.

## v0.95.0 - 2026-10-05

### Added
- **White Hilt Necromancer's Staff** (Mistlands, Galdr Table): a skull on the dark scepter with a green flame, raising skeletons like the Dead Raiser. Held at a fallen friend's grave within 10 minutes of the death, it offers to wake them there; if they agree you stand still while a green flame rises from the grave, pay half your health, all your stamina and 60 eitr with a while of Death's Price, and they are brought to the grave with their gear and the skills they lost.
- **Find the chest for an item**: with the inventory open, point at an item in your own inventory and the chests and wall drawers it belongs in light up nearby (within 30 m, setting `FindRange`), while its tooltip names the chest.

### Changed
- **Wall drawers and the ship workbench, forge and stonecutter** have Valheim's own wood, iron and stone instead of flat white and black: light pine tops and fronts, plank frames and dark iron fittings, like the other chests and workbenches.
- The Staff of Lightning burns white-blue instead of green; green now belongs to the Necromancer's Staff.

### Fixed
- **Skidbladnir's lower room stays dry in waves**: the sea no longer shows inside it, neither from within the room nor looking down the stairs from the deck.
- **Building on Skidbladnir**: pieces set on the ship's walls, high up, or on other pieces now stay fixed to the ship. Before, they could be left standing still in the world while the ship rocked under them, so they seemed to bob with the waves.
- **Memorial Stone**: the inscription lies on the stone's face and follows its curve, instead of hanging in front of it; the letters are smaller, wrap to fit the face and look cut into the stone.
- **Gathering progress** could leave out a biome you had been in, such as the Plains: the game keeps the biomes you found as translated sector names, which may differ from the biome's own (world variants, another language), and the panel looked for the biome's name in them. It now knows every biome you enter by its identity, as the bestiary does, and matches the names saved before against this world's sectors as well. A biome still missing appears the next time you are in it.
- **Workbenches and building said materials were missing** while they lay in the chests around, even in unlimited chests, most often for other players on a server: only chests handed over to you counted, and other players seldom hold them. Every chest you may use now counts for what is shown; the chests are handed over when something is crafted or built, and if one is still on its way you are told to try again in a moment instead of the materials being called missing.

## v0.94.1 - 2026-10-05

### Changed
- **Faster start**: the defence layout the mod reads at start is a quarter of its former size (the black marble and grausten pieces are written as what differs from the stone ones), and item and piece icons are kept on disk between starts, made anew only when the mod or the game changes.
- **Stone defences are lighter to draw**: thin stones (string courses, coping, corbels) are plain boxes in place of the full vanilla block, about half the triangles for walls and stairs and a fifth fewer for towers and the gatehouse, looking the same.
- **Quartermaster's Table**: packing a list reads the chests once per item rather than once per stack, and the store and bag reuse their slots instead of building them anew at every change.

## v0.94.0 - 2026-10-05

### Added
- **Oil Cauldron**: an iron cauldron on a tipping frame over a hearth, for the walk over a gate. Fill it with Resin or Tar for boiling pitch, which must heat a while, or with Stone; tip it and it pours down in front of the wall, burning or striking whatever stands below.
- **Alarm Bell**: ring it and every gate, drawbridge, portcullis and door around it shuts, and every player near hears it and gets a mark on the map. It rings by itself when a guestbook nearby notes a raid.
- **Harbour Crane**: opens the Quartermaster's store with the ship alongside chosen, to load it from the chests or unload it into them. The crane swings out over the ship, luffs to reach the hold and lowers its hook into it, high ship or not.
- **Unload button** at the Quartermaster's Table and the crane: puts everything in the chosen cart or ship hold back into the right chests.
- **Stone defences in black marble and grausten**: every stone piece also in black marble for the Mistlands and grausten for the Ashlands, costing half as much of that stone, with 1.5 and 2 times the health; grausten takes only a quarter of blunt damage and has iron spikes on its merlons.

### Changed
- **Stone towers and the gatehouse are lighter to draw**: about a quarter to a third fewer triangles (arrow slits as plain beams, taller corner stones, wider spaced corbels), looking the same.
- A Windlass House no one is near no longer looks for gates.

## v0.93.0 - 2026-10-05

### Added
- **Gate Rope**: a rope on a post that opens and closes the gate, drawbridge or portcullis nearby when pulled; Shift + use chooses which. Put up one for each to work them from inside the walls.
- **Windlass House**: a small stone house with the gate machinery: a great wheel for the drawbridge, a crank for the portcullis and a lever for the gate, and Shift + use to shut everything at once. The wheel, crank and lever turn as things open and close.
- **Gate Horn**: blow it outside the walls and the gate opens for you, raising the portcullis, or closes behind you. Teach it a gate's call at a Gate Rope or Windlass House and it calls only that gate. Only players who may open the gate are answered.

### Changed
- **The stone gatehouse's portcullis works**: it comes down in front of the gate leaves and blocks the gateway, worked with the new gate controls.

## v0.92.0 - 2026-10-05

### Added
- **Stone defences**: the palisade fort in stone, built near a Stonecutter. A stone rampart (with or without a buttress), a corner bastion, a 45 degree corner, stairs, a gatehouse with a portcullis and round-fronted towers, three stone towers (the largest under a slate roof), dragon's teeth and a stone drawbridge. The wall walk is 3 m up; the fronts have plinths, corbelled parapets, merlons, buttresses and corner stones, with moss on the ledges. Stone has about 2.5 times the palisade's health, and trolls' blunt blows do half damage. See [Defences](docs/defences.md#-stone-defences).

### Fixed
- **Building and crafting from chests is safe in multiplayer**: two players taking from the same chest at once could overwrite each other, so items were lost or doubled. A chest now counts once it is handed over with its newest contents, as for the Collection Post and the Quartermaster's Table. The chests are asked for when you take out the hammer, open the crafting panel or look at a smelter, fire or cooking station, so they are ready when you click. Alone or as the host nothing changes.
- **Signs in blueprints keep their text**: a comma in a sign's text came back as a full stop, and a backslash followed by an n (as in *C:\new*) became a line break when the blueprint was read back.

## v0.91.0 - 2026-10-05

### Added
- **Quartermaster's Table**: a counter with a ledger and a tally board that opens the store of every chest, cart and ship hold within 30 m of it, as one list with filters per chest and a search. Click an item to take a stack: unlimited items refill, and of items players stored themselves the last one always stays, so the store is never emptied. Stacks in your bag can be put back into the right chests from the same window, and a second page packs what a blueprint or the piece selected in the hammer needs. It needs no workbench, so it can stand alone at an outpost. Built with the Hammer from Wood ×20, Stone ×8, Leather Scraps ×4 and Flint ×4. See [Around the base](docs/base.md).
  - **Load a cart or ship**: take or pack straight into a cart or ship hold within the table's range instead of your bag.
  - **Pack lists**: your own kits, such as an outpost kit, built by Shift + clicking items in the store or saved from a piece or blueprint, packed with one click at any table.
  - **Stock warnings**: right click an item to watch it. When it runs below its limit the table glows red and its hover text says what is low, for everyone who uses it.

### Changed
- **Chests are handed over before they are changed for every container, not only White Hilt chests**: the Collection Post's handoff now also covers ordinary chests, carts and ship holds, and one handoff serves a whole ship, so the White Hilt Ship's hold and sea chest share it. The Quartermaster's Table uses it for every take, pack and put-back.

## v0.90.0 - 2026-10-04

### Added
- **Memorial Stone**: a 2.5 m raised stone, a bauta, built near a Stonecutter from Stone ×20. Use it to carve an inscription of your own on its face. See the new [Stonework](docs/stonework.md) page.
- **Soapstone Lamp**: a small soapstone bowl with a wick in resin, built near a Stonecutter. A soft light for tables and shelves that burns resin three times as long as a torch.
- **Hnefatafl Board**: the Norse board game set up on an 11 × 11 board, with dark and light stones and an amber king. Furniture for the table that gives +1 comfort, built near a Stonecutter.
- **Ship Setting and Stone Ring**: 22 raised stones in the outline of a 12 m ship, tallest at the stems, and a ring of ten low stones for a fire place or thing site. Each is one stone piece built near a Stonecutter.
- **Slate floors, steps and paths**: thin slate slabs in the stone of the slate roof, as a 2 × 2 m and a 1 × 1 m floor, broad steps rising 1 m over 2 m, and loose flagstones for a path. They snap like the wooden floor and stair but are stone. Built near a Stonecutter from Slate.
- **Dry stone walls**: field stones stacked without mortar, as a 2 m and a 1 m wall, a corner, and a low field wall. They snap end to end, built near a Stonecutter from Stone.
- **Soapstone Cauldron**: a fourth Stone Pot extension, carved out of soapstone near a Stonecutter. With the Herb Tray and the Smoke Oven it raises the pot to level 4, for three new Plains dishes: Fish Soup, Juniper Lox Pot and Cloudberry Porridge.

### Changed
- **The Whetstone is made at the Stonecutter** instead of the Workbench, where stone is worked.

## v0.89.8 - 2026-10-04

### Added
- **Shipwright's Bench**: a workbench of its own for everything that goes on a ship, with an anchor leaning on it, an anchor chain, a coil of rope, a rushlight, a fishing net and a bucket of tar. It needs no roof or walls and does not wear in the rain, so it can stand on the jetty. Built with the Hammer near a Workbench from Fine Wood ×10, Pine Tar ×2, Bronze ×2 and Leather Scraps ×4.

### Changed
- **Ship upgrades and the Ship Hammer are made at the Shipwright's Bench**, and the Harbour Anchor and the Mooring Post are built near it, instead of at the Workbench.
- **Ship upgrades use foraged materials**: bog iron for the ironwork, pine tar to seal, sphagnum moss to caulk, peat for the brazier, and cattail, reed and birch bark for the fishing net. See [Ships](docs/ships.md) for each recipe.

## v0.89.7 - 2026-10-04

### Fixed
- **The chart table's route buttons are no longer covered**: when the map is opened from a ship's chart table to plan a route, the Munin's memory and overview panels under the map are hidden, so the route panel's buttons can be clicked. They come back on the ordinary map.

## v0.89.6 - 2026-10-04

### Added
- **Gathering progress shows where an item goes**: under each item stands the chest it belongs in, larger, with that chest's sign and in its glow colour, and every chest when it belongs in several. Click an item and those chests and wall drawers within 100 m pulse with light and sparkle for 20 seconds, so you can find the right one.

## v0.89.5 - 2026-10-04

### Added
- **Quick buttons in Munin's memory**: **Show all** and **Hide all** switch every kind on the map on or off at once, and **Hide unlimited** keeps everything you have unlimited in chests off the map, also what becomes unlimited later, so the map shows only what you still need. Icons hidden that way fade and keep their own choice for when the switch is turned off.

## v0.89.4 - 2026-10-04

### Changed
- **Bog Iron and Peat are easier to see**: bog iron lumps are about twice as large and peat stacks about 2.5 times as tall, so they show above the Swamp's grass. New bog iron grows at most ankle-deep in water, where the murky water no longer hides it; lumps already placed deeper stay.
- **Small plants stand out more**: Sphagnum Moss is twice as tall, Iceland Moss and Madder about 1.6 times, so they no longer vanish in the Swamp's and Plains' grass or among mountain stones. New Reed grows at most 0.5 m deep in water instead of 0.8 m, so more than its tip shows.

### Fixed
- **Bog Iron, Peat and other finds in old land lie on the ground**: land generated before they came was filled using the height of the biome at each spot, not the terrain the game actually builds, which blends neighbouring biomes. Near biome borders, above all at the edges of the Swamp, they lay buried and could only be found by digging. Old land is now filled at the real ground height, and plants, slate outcrops and spider nests placed by earlier versions move onto the ground when their area loads.
- **Discovery markers point at something to pick**: a marker showed the middle of all finds of its kind in the square, which could lie between two groups with nothing there. It now sits on the find nearest that middle, an unpicked one when the square has any.

## v0.89.3 - 2026-10-04

### Added
- **The Collection Post shows what it does**: when it moves items, its light flares and a sparkle rises over the basket and over each chest that received something, for every player nearby. The hover text shows how many chests it uses and what it sorted last. While stacks in the basket fit in no chest, the light turns red and breathes, and the hover text counts those stacks.

### Fixed
- **Basket items that fit nowhere no longer block the rest**: with 20 or more such stacks first in the basket, the stacks behind them were never sorted. The basket is now tried a batch at a time in turn.
- **One chest changing hands no longer stops the whole basket**: a chest last used by another player is requested together with the others, and the remaining stacks keep moving meanwhile. Items wait for their category chest instead of falling through to the Everlasting Chest.
- **Items held in several chests are spread correctly**: chests that absorb an item without limit are preferred, a full chest passes the rest on to the next matching chest or wall drawer, and collection rounds with many posts and chests are lighter.

## v0.89.2 - 2026-10-04

### Changed
- **Skidbladnir's tent ends before the forecastle stair**: it reached over the foot of the stair, so you could not get past it to the bow. It keeps the longship's width and height but is shorter, between the boarding ladder's gap and the stair.

### Fixed
- **Furnishings on Skidbladnir can be used**: workbenches, ship workshops, chests and wall drawers built on the ship only showed the ship's own text, and Use went to the ship.
- **Ship workshops stand on the floor**: aiming at a wall or post hung the Ship Workbench, Forge or Stonecutter in mid-air there. They now drop onto the floor or deck beneath.

## v0.89.1 - 2026-10-04

### Changed
- **Skidbladnir's tent and cargo barrels at full size**: the tent was shrunk to a third and stood low at the port rail. It now has the longship's size, spans the whole waist from the boarding ladder's gap towards the forecastle stair and is high enough to walk under. The cargo barrels and crates are more than twice as large and stand along the port rail beneath it.

### Fixed
- **Building on Skidbladnir lines up with the ship**: placed pieces follow the deck's heading and tilt instead of the world's, so they stand level on the deck even when the ship rocks or lies at an angle. Rotation steps, the grid, nudging and copying follow the ship too.
- **Crafting stations can be placed on Skidbladnir**: workbenches, forges, stonecutters, the Ship Workbench and other pieces that may not stand in water were always shown as invalid placement, since the build ray then looked past the ship to the water below.

## v0.89.0 - 2026-10-04

### Added
- **Drop items in the Collection Post**: its basket now holds 8 × 4 slots. Put items in and close it, and the post sorts them into nearby White Hilt chests by the same rules as loose drops: category chests first, then the Everlasting Chest, and items a chest already holds without limit are absorbed. Items that fit nowhere stay in the basket. **Use** opens the basket; pausing moved to **Shift + Use**.

### Changed
- **The Collection Post stands on its own**: it no longer needs a workbench nearby and is no longer a workbench extension, so a workbench it stood next to loses that station level. The `StationDistance` setting is removed.
- **Collection prefers chests that already hold an item**: among chests of the same kind, the one already holding the item gets it before the nearest one.

## v0.88.2 - 2026-10-04

### Changed
- **Wall drawers have their own build icons**: the chest's category icon with the drawer in front, so they are easy to tell from the chests in the Hammer menu.
- **Wall drawers sit closer to the wall**: they are shallower (0.22 m instead of 0.3 m), and while placing they turn square to the wall and move flat against it, so no corner sticks out when your aim is a little off.

## v0.88.1 - 2026-10-04

### Changed
- **Skidbladnir camera below deck**: the camera comes in to 2 m and stays inside the hull while you are in the lower room, so you see it at once instead of looking down on the deck from outside (`[Ships.Skidbladnir] LowerDeckCameraDistance`, each player's own, 0 keeps the vanilla camera).
- **Smoother ride for passengers**: on a White Hilt Ship or Skidbladnir someone else sails, your client now steers the ship by velocity toward the helmsman's extrapolated position and heading instead of vanilla's small jumps toward each network update. In an offline simulation with 50–150 ms latency, the largest per-step jolt fell to about a third and walking passengers no longer slide on deck. Not yet tested in multiplayer (`[Ships] PassengerSmoothing`, `PassengerSmoothSeconds`, `PassengerSnapDistance`, each player's own).

### Fixed
- **Skidbladnir upgrades were invisible**: only the deck brazier and the mast wisp showed. The lantern, barrels and tent stayed hidden with the longship's switched-off trader dressing, and the anchor, sea chest and deck portal lost their models when the longship's hull was hidden. All nine now show; the rail shields no longer shrink the barrels, the tent can be stood on, and the fishing net hangs along the hull instead of across it.
- **No storage on Skidbladnir**: the cargo hatch and the sea chest had their colliders switched off, so they could not be opened. The brazier blocks players again too.
- **Ship Stonecutter missing from the Ship Hammer**: it failed to load because the vanilla stonecutter has no extension connection point.
- **Ship Hammer pieces floated above the lower deck**: the build ray started from the camera outside the hull and hit the deck above. With the camera inside the room, workshops land on the floor.
- **Drift Anchor did not hold**: an anchored White Hilt Ship or Skidbladnir now also has its drift and turn stopped after every physics step, besides the rigidbody constraints. The anchor is still weighed when someone takes the helm (`AutoAnchor`).
- **Furnishings below Skidbladnir's deck weathered in the rain**: vanilla's roof check ignores ships, and furnishings aboard were also made to take rain wear even when their vanilla piece does not (crafting stations). The decks now count as a roof for pieces below them, and each piece keeps its own rule.
- **Every building piece in the world ran a per-frame ship check**: it now only runs on furnishings attached to Skidbladnir.

## v0.88.0 - 2026-10-04

### Added
- **Skidbladnir details**: a rudder on the sternpost that turns with the helm. The White Hilt logo on the main course, readable from both sides. With the Ship Lantern upgrade, two stern lanterns and three lamps in the lower room light and switch together with the deck lantern. A railing around the stair opening replaces the handrails that rose out of the deck, and plank lining along the lower room's sides puts wall drawers on a visible wall. The hull and new woodwork are about a third brighter, so the ship no longer looks almost black at night. Its wake, bow waves, splashes and wake sounds now follow Skidbladnir's waterline instead of the longship's.
- **Skidbladnir seats**: the longship's stools and holdfasts were switched off together with its hull. Now five stools stand on the quarterdeck, by the helm and on the forecastle, with holdfasts at the main mast and the bow, so you can sit or hold on instead of being thrown off.

### Fixed
- **Skidbladnir looked black and see-through**: the ship export mirrored the model without reversing triangle winding, so the hull, decks, stairs and mast ladder were rendered inside out (only their far, unlit inner faces showed, and the main deck and the stairs down appeared to be missing). Faces now point outward and the whole structure is two-sided, so the hull is also solid from inside the lower room.
- **Skidbladnir boarding ladder did nothing**: its lower stop was above a swimmer's feet, so Use from the water only lifted you to the surface. The ladder now hangs outside the hull bulge amidships at a gap in the port rail, and Use from the water climbs straight to the deck. Live boarding still needs an in-game check.
- **Skidbladnir walking surfaces now follow the model**: the old hand-placed stern and bow decks were flat boxes up to a metre away from the visible decks, the stairs ran into the quarterdeck and an invisible bow deck, and the lower room had no end walls. The quarterdeck, the sloping poop deck and the forecastle now have colliders measured from the model. New stairs lead up to the quarterdeck, the poop deck and the forecastle, and the stair down to the lower deck now lies in the open waist. Ramps under every stair keep them climbable. Walls follow the balustrades, and new railings line the waist and the open deck edges. Bulkheads close the lower room, and its floor reaches the hull. New woodwork uses the hull's own wood texture instead of a flat light brown. Furnishings already placed aboard keep their saved positions, so move any that now stand in the new stairs.
- **Skidbladnir fittings placed on the decks they belong to**: the portal and Navigator's Table on the quarterdeck, the sea chest on the poop deck, tilted to the slope. The cargo hatch and barrels are rearranged around the new stairs, and the brazier no longer stands inside the tent. The lantern hangs at head height, the helm sits on the deck, and the drift anchor hangs at the bow instead of in the air beside the hull.

### Removed
- **Skidbladnir's loose sail sheets**, which stuck out three metres from the hull and ended in mid-air once the sails were furled, and the longship's water mask, which did not fit the new hull.

## v0.87.0 - 2026-10-04

### Added
- **Iron grate panels** in 2×1 m, 4×4 m and 4×1 m sizes (width × height), built as single hammer pieces from unscaled vanilla grate sections. Preserve vanilla bar thickness, wear/destruction models, strength and building category; add edge snap points every metre, fitted placement collision, icons and Norwegian text. Cost Iron ×2, ×8 and ×4 respectively, with normal per-piece content/recipe configuration. Original grates are unchanged. Live snapping, placement, refunds and multiplayer require in-game testing.

## v0.86.2 - 2026-10-04

### Fixed
- **Root Dowser pulse interruptions**: preserve time toward the next pulse when switching between available roots, preventing repeated nearest-target changes from postponing every signal. Pulses continue to speed up on approach and slow down on retreat using the existing settings. Offline target-switching and approach/retreat checks pass; the reported in-game audio behavior still requires verification.

## v0.86.1 - 2026-10-04

### Fixed
- **Bog Iron and Peat ground clearance**: enable configurable 0.1 m clearance, lifting their visuals and pick colliders together and fitting a pick target to the visible model. Existing occurrences update when loaded again; saved positions, spawning, yields and regrowth are unchanged. This reduces burial by small terrain differences but does not compensate for large terrain raising or make underwater Bog Iron visible above water. Live terrain visibility still requires in-game testing.

## v0.86.0 - 2026-10-04

### Added
- **Wall drawers** for all fourteen restocking chest categories, usable on player-built wooden and stone walls both ashore and aboard Skidbladnir. Independent prefabs keep the original inventory dimensions, category icons, hover/progress UI and restocking behavior. Original chest inventories are unchanged. New models, Norwegian text and feature documentation included. Live crafting, wall placement and multiplayer persistence still require backup-world testing.

## v0.85.0 - 2026-10-04

### Added
- **Ship workshops**: an everlasting White Hilt Ship Hammer with a separate build menu for a compact Workbench, Forge and Stonecutter, restricted to stationary Skidbladnir. Preserves vanilla recipes, extensions, shelter rules and building costs; uses small original Blender tables/tools and the existing Repair Anvil model for the forge.

## v0.84.0 - 2026-10-04

### Added
- **Skidbladnir**: Freyr's indestructible sailing home (`WhiteHiltSkidbladnir`), with an empty lower deck for player-built furnishings and stations, stairs, boarding ladder and a mast-top lookout. Supports the existing nine ship upgrades, Navigator's Table, routes and its own deck-portal arrival point. Persistent ship-local furniture attachments preserve independent inventories and survive parent-ship unloading; dismantling requires first removing furnishings. Horizontal speed is capped at at most half the White Hilt Ship's calculated ideal full-sail speed. Adds server-synced building, waterline, speed, furniture-position update and sail-animation settings with Norwegian text. Model by 3ddans, CC BY 4.0, with a reproducible separate ship export. Live sailing, stations, shelter, reloading and multiplayer still require validation in a backup world; update server and clients together.
- Compilation, bundle export, Norwegian config coverage and 314 isolated Unity production-code/geometry checks passed. These checks use controlled network stand-ins and do not replace live multiplayer testing.

## v0.83.1 - 2026-10-04

### Fixed
- **Startup Harmony patch failure**: remove the Beast Counter treatment prefix's invalid result parameter from the void `Humanoid.UseItem` method. Incompatible treatments still show the wrong-weapon message and skip normal item use; compatible treatments retain normal consumption.
- **Ship water-mask material error**: check for `_MainTex` before reading ship material textures, preserving the vanilla water mask without logging a missing-property error during recolouring or upgrade preparation.

## v0.83.0 - 2026-10-04

### Added
- **Root Dowser (Rotsøker)**: craft at Workbench level 2 from Bone Fragments ×10, Wood ×5, Resin ×5 and Greydwarf Eye ×2. Worn in an accessory slot, it finds the nearest loaded, unpicked Madder Root or Roseroot plant within 30 m with green, higher-pitched Wishbone-style pulses that speed up nearby. Within 3 m the nearest plant receives a faint local green light. Picked plants stop signalling until they regrow; loose items and random bonus roots are ignored. Adds configurable target items, search distance, scan and pulse intervals, sound pitch and glow settings with Norwegian text. Normal content/recipe settings apply; no extra bronze or Bonemass requirement. In-game lighting, pulse appearance and simultaneous accessories still require testing.

## v0.82.0 - 2026-10-04

### Added
- **Kraken tent sweep**: one existing tentacle periodically reaches horizontally through the side opening beneath an installed White Hilt Ship tent, aiming at a sheltered sailor. Collision checks try the targeted height and slightly lower/higher openings, skipping blocked paths. A two-second warning precedes the visible three-second reach and withdrawal. Players still in the swept lane receive light blunt damage and a strong shove toward the opposite opening; moving away avoids the hit. Ship invulnerability is unchanged. Only the tentacle's network owner delivers hits, with saved per-player hit stamps preventing duplicates after ownership changes. Adds server-synced timing, radius, damage, knockback and range settings with Norwegian text. Compilation and 32 isolated production-component checks passed; visual clearance, actual knockback and multiplayer require in-game testing. Update server and clients together.

## v0.81.0 - 2026-10-04

### Added
- **Ship lantern switch and Kraken blackout**: interact with the installed lantern to light it or put it out. The choice is saved on the ship and synchronized through its owner; untouched lanterns retain automatic night lighting. A lit lantern flickers before Kraken's tentacles appear, then goes out and cannot be relit during the encounter. After death or retreat it stays off until manually relit. Only the target ship within the configured range is affected, independently of ship holding/lifting. Adds server-synced warning, flicker and range settings with Norwegian text. Compilation and 30 isolated production-method encounter/lantern checks passed; the interaction target, warning visuals and multiplayer still need in-game testing. Update server and clients together.

## v0.80.6 - 2026-10-04

### Changed
- **Softer, wider ship lantern**: reduce White Hilt Ship lantern intensity from 2.5 to 1 times the vanilla lamp, and increase its range from 2 to 3 times vanilla, reaching farther around both sides without the former intense deck lighting. Existing default values migrate once; other custom values are preserved. Settings remain local and require a restart. Brazier, mast wisp and portal lighting are unchanged; the final balance on the white hull and water requires in-game testing.

## v0.80.5 - 2026-10-04

### Changed
- **Taller Caraway and low-flower clearance**: lengthen Caraway's main stem and increase its base height from 0.54 to 0.74 m without stretching its flower heads. Caraway, Yarrow, Bog Bean and Henbane now have a configurable 0.1 m ground clearance, lifting their visuals and pick colliders together above the saved terrain point. Their pick targets follow the visible model. Mountain plants retain their existing 0.2 m default; other plants, dropped item fitting, saved positions, yields and regrowth are unchanged. Existing plants update when loaded again. Actual terrain and grass visibility still require in-game testing.

## v0.80.4 - 2026-10-04

### Changed
- **Taller Yarrow**: lengthen the three flowering stems and increase the growing plant's base height from 0.46 to 0.72 m so the white flower heads stand higher above Plains grass. Leaves and flower heads retain their source dimensions rather than being stretched. Existing plants use the new look when loaded again; saved positions, yield, regrowth and recipes are unchanged.

## v0.80.3 - 2026-10-04

### Fixed
- **White Hilt Ship helm ownership**: prefer the helmsman's client for manual sailing physics when both cargo containers are idle. Container open/stack requests reserve ownership, open inventories block handoff, and both inventories plus current motion are saved before transferring back. The new owner restores received speed and rudder before vanilla physics updates them. Vanilla ships, autopilot ownership, sail force and speed are unchanged. Includes server-synced ownership settings and optional local ownership/frame/ZDO-revision diagnostics. Compilation and 44 isolated production-method checks passed; the reported multiplayer lag was not reproduced and in-game smoothness and inventory handoff still require testing. Server and clients should update together.

### Changed
- **Quieter, lighter sounding**: depth and obstacle scans run once per second by default instead of five times per second. Persistent shallow-water danger gives a reminder every 60 seconds rather than repeating on the eight-second cooldown. Five seconds of clear water at warning speed rearm a new encounter; short gaps or stopping do not. The existing minimum warning cooldown remains configurable. New scan, reminder and clear-water intervals have Norwegian settings text and ship documentation.

## v0.80.2 - 2026-10-04

### Changed
- **Bestiary illustrations**: replace the full-colour trophy pictures in Svartboka with indistinct monochrome ink sketches on softly worn, textured paper. Coarse, smoothed silhouettes and sparse hatching hide fine details, leaving the real appearance for the encounter. Trophy and inventory icons remain unchanged. Sketches are generated once per source icon while reading and released with the panel; no asset bundle rebuild is needed. Compilation and eight isolated production-pixel checks passed; actual monster sketches and HUD scaling still require in-game testing.

## v0.80.1 - 2026-10-04

### Fixed
- **Dropped forageable size**: Rock Lichen and the other procedural forageable items now fit the longest side of the original item rather than its height, preventing flat or wide models from becoming oversized when dropped. Hidden equip models remain excluded, as in the earlier Chanterelle fix. Growing plants keep their existing sizes.

## v0.80.0 - 2026-10-04

### Added
- **Black Bestiary / Svartboka**: a buildable book on the guestbook stand, with up to nine browsable, illustrated pages. Each describes a black beast's dangers, biome, guardian, distinct material counter, gathering places and actual registered crafting recipe, including server overrides. Available under Furniture from Meadows ingredients, without requiring a trophy or kill first. Pages unlock per reader only after discovering a biome where the beast lives; Meadows-only characters see a foreboding warning without monster spoilers. New discoveries are saved with stable biome identities, independent of language; discovering Mountains and Plains does not duplicate the Dragon's page.
- **Material weaknesses**: six special arrows (lingonberry Troll, peat Ember, crowberry Rime, juniper Seid, sweet-gale Bog Venom and rosehip/kraken-ink Storm), and three temporary weapon treatments (rock-lichen Stonebreaker for pickaxes, henbane Berserker for blunt weapons and woad Carapace Whetstone for slashing weapons). Each grants a default 50% extra material-damage component only against its matching black beast. Existing resistances, ordinary creatures and Kraken are unchanged. The bonus follows the serialized attack, is resolved once on the target owner, and respects armour rather than using true damage.
- **Counter settings**: server-synced per-counter bonus, output, arrow pierce/element damage and treatment attack count. Treatments last 30 eligible attacks by default (misses count), apply only to the weapon held when used, and replace the previous material treatment. Mining/woodcutting also use the corresponding treatment. Recipes and content toggles use the existing progression settings; all default recipes are ingredient-gated, with no extra tier materials. Server and clients must update together. In-game combat, book layout, icons and multiplayer still require testing.

## v0.79.1 - 2026-10-04

### Changed
- **Odin near black beasts**: within 40 m of a living black beast, retain 80% of Odin's instant healing, passive healing and regeneration bonus (+1.6 HP/s, 1.8x regeneration, 80%-of-maximum-health heal on drink at default settings). Maximum health, fall protection, duration and Freya remain unchanged. Includes horn-called beasts and beasts at sea; excludes ordinary starred creatures. Multiple beasts do not stack, and only the strongest healing attenuation applies near Kraken. Full strength returns outside range or when the beast dies. Range and retained share are server-configurable in `[Difficulty.Beasts]`.

## v0.79.0 - 2026-10-04

### Added
- **Brutal Kraken encounter**: six tentacles, periodic owner-controlled hull lifts with a three-second warning, and a synchronized enraged phase below half health. Enrage increases body and tentacle damage by 50%, speeds up the body's animation and shortens subsequent lift cycles without cutting warnings short. Lift height, acceleration, timing and enrage are server-configurable.
- **Potion pressure near Kraken**: within 80 m of a living, non-retreating Kraken, Odin retains 60% of its health/fall-protection bonus and 25% of its healing bonuses; Freya retains 50% of its stamina benefits, blending action costs toward normal and preventing stamina generation while attenuated. Both retain useful protection, keep their duration, and automatically return to full strength outside the fight. Range and retained shares are configurable; other potions are unchanged.

### Changed
- **Kraken strength**: body health 4000 -> 8000, tentacle health 500 -> 900, slam 90 -> 140, lash 45 -> 70 and ship-damage share 30% -> 50%; attack interval is now three seconds. Previous default settings migrate once; custom values are preserved.
- **Kraken presence**: darker wine-black hide, narrow slanted red eyes, more of the head above water, deeper calls audible up to 180 m, periodic ambient calls and warning/enrage roars. Runtime appearance is reused by the Unity preview tool. The existing model and attribution are retained.

### Fixed
- Includes the dedicated-server horn correction from 0.78.1. The user confirmed Kraken spawning in game after the correction. Server and clients must update together.
- Compilation, 71 isolated production Kraken/potion checks (including 1001 lift-height samples and a gravity-enabled lift simulation) and rendered idle/attack/death previews passed. Final boat handling, sound mix and combat balance still require an in-game retest.

## v0.78.1 - 2026-10-04

### Fixed
- **Horn summons on dedicated servers**: validate the caller and nearby encounters using network data instead of requiring live player and ship objects on the server. The server authorizes the caller's client to spawn the encounter, reserves the pending call and starts the world-wide cooldown only after a successful confirmation. Rejections, authorizations and spawn results are logged with a `Horn:` prefix. Server and clients must update together. Compilation and 43 isolated production-service checks passed; the user confirmed Kraken spawning in game.

## v0.78.0 - 2026-10-04

### Added
- **Horn of the Deep**: a forge-crafted celebration-horn clone with black trim, a synchronized mouth-and-arm blowing pose and an original deep sustained WAV tone. Equip it and attack to blow; movement, swimming, combat actions, attaching or putting it away cancels the call. A completed call asks the server to summon Kraken beside a ship in deep Ocean, or an unlocked black beast belonging to the land biome. Boss requirements, disabled creatures, nearby encounters and a configurable world-wide horn cooldown are respected; weather and dark-hour rolls are bypassed. Meadows and Deep North have no horn beast. Horn-called Kraken stays by day but keeps its retreat timer; horn-called land beasts stay by day while players are within the encounter range. Compilation, 31 production-server checks, 20 production arm-geometry checks, trim coverage and packed-audio checks passed; multiplayer animation and sound still require in-game testing.

## v0.77.5 - 2026-10-04

### Fixed
- **Portal list scrollbar and footer**: the scrollbar now sits outside the masked scroll view and its row canvas, with explicit right-edge anchors and a centred handle. Rows follow the reserved viewport width. The panel's lower edge is raised to leave room for the Munin's memory and Uncovered headers below the map. Compilation passed; thumb visibility, dragging and scaled layout still require in-game testing.

## v0.77.4 - 2026-10-04

### Fixed
- **Clock HUD placement**: day, time and weather now sit at the upper left above the weapon/tool hotbar instead of below the compass among status effects. The hotbar reserves space while the clock is visible and returns to its original position when hidden. Compass and boss bars no longer push the clock down; the ruby waypoint arrow no longer reserves space for the clock. Compilation passed; HUD scaling and appearance still require in-game testing.

## v0.77.3 - 2026-10-03

### Fixed
- **Settings window scrolling**: mouse-wheel sensitivity increases from 35 to 132 UI pixels per unit of wheel input in both settings lists.
- **Portal list and map input**: the map ignores input while the pointer is over the open portal panel, so scrolling the list no longer zooms the map behind it. Map input outside the panel is unchanged. Compilation passed; pointer routing and scrolling feel still require in-game testing.

## v0.77.2 - 2026-10-03

### Fixed
- **Portal list scrolling**: a wider, centred gold scrollbar handle on a darker track is easier to see and drag, with reserved space beside the portal rows. Mouse-wheel scrolling increases from 35 to 132 UI pixels per unit of wheel input, about three rows. The settings window is unchanged. Compilation passed; appearance and scrolling feel still require in-game testing.

## v0.77.1 - 2026-10-03

### Fixed
- **Valkyrie Stone repeat deaths**: a new local death clears the previous trip marker before respawn saves the player, so dying again at exactly the same coordinates no longer blocks travel to the new death point. Trip cost and the once-per-death setting are unchanged.

## v0.77.0 - 2026-10-03

### Added
- **Dragon ground fire**: Desert Dragons and Black Dragons leave temporary flame patches when their breath hits dry terrain. Defaults: 6 seconds, 1.5 m radius and 10 fire damage per second before resistance; damage inherits the projectile's star and black beast scaling. Nearby impacts merge, at most 3 new patches per breath and 6 active per dragon. Overlapping patches apply only the strongest damage on each target's network owner. No spreading, terrain changes, water ignition, dynamic lights or particle collision; buildings remain safe unless `BurnsBuildings` is enabled. Seven admin-synced ground-fire settings and Norwegian setting texts control the feature. Server and clients must update together. Compilation and isolated behavior checks passed; particle appearance, multiplayer ownership and performance still require in-game testing.

### Changed
- **Stronger dragons**: base health rises from 800 to 1200 and fire damage per breath flame from 15 to 20. Flame count and breath cooldown remain unchanged. Previous defaults migrate once; other configured values are preserved. Black Dragons retain their existing five stars and beast bonuses, giving 8100 health and 60 fire per flame with default difficulty settings, plus 30 ground fire damage per second before resistance.

## v0.76.1 - 2026-10-03

### Fixed
- **Monster display names**: Desert Dragons, Lindorms, Giant Spiders, Krakens and Kraken Tentacles now show their localized names instead of internal `WhiteHilt_` prefab identifiers. Restore `Character.m_name` after Jotunn's cloning constructor overwrites the configured name. Internal prefab identifiers, saved creatures and spawn commands remain unchanged; black beasts and dogs already restore their names and are unaffected.

## v0.76.0 - 2026-10-03

### Added
- **Kraken crew damage scaling**: the Kraken and its tentacles deal 15% more solo damage to players and 3% more solo ship damage per additional player aboard when the encounter starts. Five aboard means +60% crew damage and +12% ship damage, on top of vanilla scaling. Crew size is saved on the Kraken and copied to its tentacles so leaving the ship does not reduce the bonus; admin summons use the same rule, and older encounters default to solo damage. Admin-synced `[Kraken] CrewDamagePerExtraPlayer` and `ShipDamagePerExtraPlayer` control the bonuses, with Norwegian setting texts; 0 disables each bonus. Existing zero-damage settings still block damage. Health, tentacle count, attack chance and cooldown are unchanged. Compilation and 12 isolated damage checks passed; multiplayer in-game testing remains required.

## v0.75.1 - 2026-10-03

### Changed
- **More dangerous Kraken encounters for large crews**: doubled the default `ChancePerExtraPlayer` from 4 to 8 percentage points. Natural attack chance is now 8% per minute solo, 16% with two aboard, 24% with three and 40% with five, capped at 100%. All existing conditions and the world-wide cooldown remain unchanged. Existing configured values are preserved; set `[Kraken] ChancePerExtraPlayer` to 8 to adopt the new balance on an already configured server.

## v0.75.0 - 2026-10-03

### Added
- **Kraken crew bonus**: each additional player aboard the same ship adds a configurable 4 percentage points to the natural attack chance per minute (8% solo, 12% with two, 16% with three), capped at 100%. Existing weather, night, biome, depth, progression and world-wide cooldown requirements remain unchanged. Admin-synced `[Kraken] ChancePerExtraPlayer` controls the bonus; 0 disables it. `ChancePerMinute = 0` still disables natural attacks. The diagnostic console command now shows crew size and effective chance. Server and clients must update together for the changed request RPC. Validated with compilation and isolated chance checks; multiplayer in-game testing remains required.

## v0.74.1 - 2026-10-03

### Fixed
- **Self-closing doors after portal travel**: unattended timer-managed doors now save their closed state when their owner leaves the active area, before the scene unloads and ownership is released. This prevents a quick portal departure from leaving a door open with no running close timer. Leaving the active area on foot has the same safeguard; ordinary delays still apply inside it. Hold-open, nearby players and tamed animals, disabled settings, excluded pieces and key doors are respected. Validated with isolated behavior checks and compilation; in-game portal and multiplayer testing remains required.

## v0.74.0 - 2026-10-03

### Added
- **HUD compass**: a subdued horizontal tape above the day, clock and weather follows the camera's exact heading every frame after camera effects. Fixed centre indicator, eight direction labels, three-digit degrees and small/medium/large ticks; angular positioning wraps correctly across north. Defaults to 700 by 52 UI units and a 120-degree window, with fading edges and a faint optional background. Local `[HUD.Compass]` settings control dimensions, intervals, labels, placement, scale and opacity.
- **Compass markers**: own unchecked saved map pins, known boss locations and own unchecked death markers use their existing map icons and the same angular projection. Off-screen markers are hidden, not clamped. Category switches, icon size and membership refresh are configurable; icons are pooled. `HudCompassMarkers.SetMarker` / `RemoveMarker` allow explicit custom world markers without scanning or revealing unknown locations. Portals, boats and other players are not automatically added in this version.

### Changed
- **Top HUD layout**: while the compass is visible, boss bars sit below it, the clock remains below the boss bars, and the existing ruby waypoint arrow stays below the visible clock. Original layout returns when the compass is disabled or hidden. Compass width and horizontal position are constrained to HUD space. The round map compasses and the ruby amulet's requirements are unchanged. Hidden with the HUD, death, inventory, menus and large map; visible while building.

## v0.73.1 - 2026-10-03

### Fixed
- **Player portraits**: your own portrait and gold heading ring now draw in front of other players on both maps when markers overlap, including after switching maps or new player portraits appearing.

## v0.73.0 - 2026-10-03

### Added
- **Munin's memory**: a gold infinity badge at the top-left of item icons shows which items are unlimited under the world's current chest rules. Warm, light backgrounds highlight items and locations found in the player's current biome, including kinds found in several biomes. Hover text includes both statuses. Location illustrations never count as unlimited items; ordering, discovered-only lists, filter rims, checkmarks and map selections are unchanged. Status follows biome and chest changes while the panel is open.

## v0.72.4 - 2026-10-03

### Fixed
- **Mountain forageables**: lift plants and their existing pick colliders 0.2 m above their saved terrain point, and add a pick target matching the visible model with a minimum height of 0.4 m. Addresses low moss and lichen disappearing into slopes and the mismatch between custom models and vanilla pick colliders. Server-synced `GroundClearance` and `MinimumPickHeight` in each Mountain plant's foraging section apply when plants next load, including existing plants; saved positions, yields and regrowth are unchanged. In-game snow and steep-slope testing is still required.

## v0.72.3 - 2026-10-03

### Changed
- **Lindorm**: boss progression sets its minimum strength when it emerges: 0 stars after Eikthyr, then 1 through 5 after the Elder, Bonemass, Moder, Yagluth and the Queen. Server-configurable `ProgressionStars` ignores ordinary biome star limits; encounters also receive the existing difficulty pressure bonuses at spawn. Their strength stays fixed during the fight.
- **Gift of Surt**: halves fire damage by default instead of granting fire and frost immunity. Still an Ashlands potion; `FireModifier` configures its fire protection.
- **Gift of Eir**: heals 25% and cleanses once when drunk, then grants 1.5x health regeneration for 2 minutes. No continuous cleansing; it cannot be drunk again while its effect is active. Bog Bean Bitter is unchanged.
- **Gift of Freya**: duration reduced from 20 to 10 minutes, matching Odin; stamina effects are unchanged.
- **Gift of Fenrir**: attack speed bonus reduced from 50% to 20%, and life steal from 15% to 5%.
- **Desert Dragon**: default health increased from 500 to 800, also strengthening its Black Dragon cousin. No new attack in this balance pass.
- Previous default values for Eir, Freya, Fenrir and dragon health migrate once; custom values are preserved. Buff combinations, giant spiders and the Kraken are unchanged.

## v0.72.2 - 2026-10-03

### Changed
- **Lindorm**: default size increased from 1.3 to 1.6 (about 6.4 m long), and bite damage from 55 to 75 pierce. Configurable visual growth of 15% per star up to two stars; higher stars also use the difficulty size settings. Previous default config values migrate once; custom values are preserved.
- **Monster sounds**: original growls and rasps for the Lindorm, deep watery calls and a separate tentacle lash for the Kraken, and growls, roars and fiery breath for Desert Dragons. Each has idle, alert, attack, injury and death sounds, controlled by its server-synced `Sounds` setting. Black Dragons inherit the dragon sounds. Visual effects and corpses remain unchanged, and inherited attack calls are removed when custom sounds are enabled.

## v0.72.1 - 2026-10-03

### Changed
- **Giant spiders**: default size increased by 50% (about 2.4 m across), with configurable visual growth of 15% per star up to two stars. Higher stars retain the difficulty system's extra growth. Bite damage increased from 18 pierce / 15 poison to 30 pierce / 20 poison. Previous default config values migrate once; custom values are preserved.
- **Giant spiders**: original clicks, rasps and hisses for idle, alert, bite, injury and death, switchable with `[Giant Spider] Sounds`.

### Fixed
- **Giant spiders**: face forwards instead of walking abdomen-first. The model import measures the fangs relative to the abdomen rather than the offset model origin, and centres the body horizontally.

## v0.72.0 - 2026-10-03

### Added
- **Collection Post**: a carved workbench extension with a wicker basket gathers loose items into matching White Hilt category chests, with the Everlasting Chest as a fallback. Collection and chest radii are independently server-configurable (80 metres each by default, 10–200 metres). Use the post to pause/resume it; placement shows the collection radius. Full chests leave items on the ground, partial deposits keep their remainder, and starred/custom-data items retain their data. Player drops are excluded by default, with new drops marked across world reloads. Only loaded areas and accessible, closed chests are used. Server settings control interval, batch/search limits, connection distance, player drops and ownership retries. Reuses the existing carved post and wicker basket models.

## v0.71.0 - 2026-10-03

### Added
- **Dog**: use a Dog House while your grown dog follows you to make it the dog's new home, wherever the house stands. The old house can stay. The hover text on a Dog House shows whether your dog lives there.

## v0.70.0 - 2026-10-03

### Added
- **Desert Dragons**: once Moder is slain, sand-coloured dragons about 8 m across fly over the Plains by day and night. They circle above you and breathe a stream of fire that leaves the mouth about as wide as the mouth and widens to 3 m towards the ground; it sets you burning. They are immune to fire, weak to frost and fall out of the sky when slain. They drop Desert Dragon Scales, sometimes a Surtling Core and rarely their trophy. Their fire leaves buildings alone unless `[Desert Dragon] BurnsBuildings` is on. Every number is in the `[Desert Dragon]` section. Model: "Red Dragon" by absol (CC BY 4.0).
- **Dragonscale Broth**: a Plains dish from the Stone Pot (Desert Dragon Scale, Onion, Barley) whose buff halves fire damage.
- **Dragonfire Arrow**: fire arrows tipped with Desert Dragon scale, 30 pierce and 60 fire, 20 per craft at the workbench.
- **Black Dragon**: a new black beast of the dark hour and the blood moon. It is a black, 5 star Desert Dragon that comes in the Mountains and the Plains once Moder is slain. Its trophy can be bound at the Binding Stone (+10%, rune strength 15%).

### Changed
- **Beasts**: a biome can now have more than one black beast; when it does, one of them comes at random. `whitehilt_beast <biome>` also picks at random. Flying beasts are 10% slower in the air too.
- A black beast cloned from one of the mod's own monsters leaves a black corpse.

## v0.69.4 - 2026-10-03

### Changed
- **White Hilt Rune Circle**: the ground portal's stone base, runes and collision shape are one third as tall, with the same diameter. The stone surface is now 4 cm above the ground instead of 12 cm. Standing and ship portals are unchanged.

## v0.69.3 - 2026-10-03

### Fixed
- **Munin's memory**: icons remain in their original colours on opaque light-grey discs even when switched off. Coloured rims and checkmarks show active filters, grey rims show inactive filters, and rims brighten on hover. Panel size and filtering are unchanged.

## v0.69.2 - 2026-10-03

### Fixed
- **Self-closing doors**: automatic closing sets the closed state directly instead of depending on the owner's animator being ready for interaction. Repeated closing cannot reopen a door, and doors with inverted open/closed states are handled correctly.
- **Drawbridges**: the vanilla drawbridge follows the gate closing settings. The White Hilt drawbridge keeps its prefab reference rotations when loaded, and follows gate changes without depending on the owner's animator; closing raises it instead of lowering it.

## v0.69.1 - 2026-10-03

### Fixed
- **Player portraits**: a player who died showed up with only the first letter of their name on the map for the rest of the session. While they waited to respawn they looked as if they had left, so their portrait was forgotten, and they never sent it again.

## v0.69.0 - 2026-10-03

### Added
- **Guestbook**: a lectern with an open book that writes down who came by, what was built and torn down nearby and by whom, and raids with the creatures in them and how long they lasted. Anyone can read it. Built at the workbench from Wood ×6, Leather Scraps ×3 and Feathers ×1. New section `[Guestbook]`. See [Around the base](docs/base.md#-guestbook)

## v0.68.0 - 2026-10-03

### Added
- **Chest search** (F9): type part of an item's name, and every chest, cart and ship hold within 100 m that you may open and that holds it glows and gets a map pin, with a list of how many there are and in how many chests. The marks stay for 60 seconds after the search closes (`[Storage]`)

## v0.67.0 - 2026-10-03

### Added
- **Saga** (F8): every character keeps a saga of its deeds, each with its day: bosses, black beasts and great creatures slain near it, treasures dug up near it, first steps into a biome and skills reaching 25, 50, 75 and 100. Deeds give renown; every 20 renown is a rank, up to 10, and each rank gives +10 carry weight and +3 maximum stamina (`[Saga]`). See [Saga](docs/saga.md)

## v0.66.0 - 2026-10-03

### Added
- **Treasure hunts**: Hildir also sells a **Treasure Hunt Map** (1500 coins). The chest under its cross holds a little loot and the next map of the hunt, whose treasure lies within 1200 m of where it is read. The last of the 3 maps leads to a chest with 2 black beast trophies and 6 draws from the loot list (`[Treasure] HuntSteps`, `HuntPrice`, `HuntStepMaxDistance`, `HuntStepLootRolls`, `HuntFinalLootRolls`, `HuntFinalTrophies`)

## v0.65.0 - 2026-10-03

### Added
- **Loom** (Hammer, Workbench): an upright loom with stone weights that weaves **Linen Cloth** from Linen Thread
- **Dyeing with a Paint Pot**: used from the hotbar while looking at a banner, it dyes only the banner's cloth; looking at a ship, it dyes the sail (with Linen Cloth); standing at a loom, it dyes the cape you wear, which everyone sees. Elsewhere the pot loads the brush as before (`[Textiles]`)

## v0.64.0 - 2026-10-03

### Added
- **Drying Rack** (Hammer, Workbench): a rack of poles where meat and fish hang in the wind for game days until they are cured. It needs no fire and never burns anything; the production timers show the time left
- **Cured Ham** from a **Seasoned Ham** (Stone Pot: Raw Meat, Wild Garlic, Thistle) after 3 days on the rack: 46 health and 24 stamina for 60 minutes, frost damage taken a quarter less
- **Cured Sausage** from a **Raw Sausage** (Stone Pot: Raw Meat, Deer Meat, Thistle) after 2 days: 28 health and 46 stamina for 60 minutes, +40 carry weight
- **Stockfish** from a Coral Cod after 2 days: 30 health and 50 stamina for 75 minutes, stamina regenerates 20% faster
- **Rakfisk**: a **Rakfisk Tub** (Stone Pot: Pike, Honey) ferments into 4 in the fermenter: 40 health and 34 stamina for 50 minutes, poison damage taken halved (`[Curing]`, `[Food.<Name>]`)

## v0.63.0 - 2026-10-03

### Added
- **Moats** (White Hilt hoe, Ctrl + H): click a wall to dig a moat round every wall connected to it, or click points on the ground for a line or a ring of your own. Three ditches: a dry **V-ditch**, a **wet moat** with water you can swim in, and a **staked ditch** with sharp stakes along its bottom. The dug earth becomes a bank outside or inside the ditch, or is carried away, and ground is left as a causeway in front of gates. It is dug as you walk along it, like a road; digging is free. Creatures down in a ditch move at half speed and slide down its sides for a while before they climb out (`[BuildTools.Moats]`)
- **Drawbridge** (Hammer, Workbench): a 4 × 9 m deck over a moat that is lowered when opened and raised when closed, and follows the nearest gate (`[Defences] DrawbridgeLinkRange`)

### Changed
- *Reset* with the White Hilt hoe also takes away a moat dug there

## v0.62.0 - 2026-10-03

### Added
- Lone giant spiders come out in the Black Forest at night, away from nests (`[Giant Spider] NightSpawnChance`, `NightSpawnSeconds`, `NightSpawnMax`)
- **Old land is filled in**: spider nests, slate outcrops and forageables only grew in land generated after they came, so worlds explored earlier had none. Once per world and kind, when the server starts, that land now gets its share by the same rules and chances as new land, at least 50 m from anything built. Kinds added later are filled in the same way (`[OldLand] Enabled`, `BuildingDistance`)

### Fixed
- Spider nests were far rarer than `NestChancePerZone`: the generator tried only one spot per zone and gave up when it was steep, open or blocked. It now tries up to 50

## v0.61.1 - 2026-10-03

### Fixed
- White Hilt rune circles that were Portal Stations stations could not be removed ("requires a workbench" next to a workbench), and would have dropped nothing: their workbench and building costs were never resolved
- The Binding Stone and the Valkyrie Stone broke apart on stone floors: as stone they need ten times the support of wood, and their centre of mass lay on the floor's surface, so the support was reckoned sideways and ran out. It now sits in the middle of the stone, and rain no longer wears them

## v0.61.0 - 2026-10-03

### Added
- **Thunder Rune** and **Rowan's Ward**: the first rune for a shield, of iron, silver and crystal. Etched with Fine Wood and Rosehips into a bound White Hilt shield, a foe within 10 m whose blow you block is struck by lightning worth half the shield's base block power (`[Gear.Infusions] WardCost`, `WardShare`)
- **Rushlight** (Hammer, Workbench): a cattail rush dipped in fat, held slanted in an iron rush nip on a stump, with its own model. A small, dimmer light that burns Cattails instead of Resin, each twice as long

### Changed
- The Rune Etching Table etches a shield rune into the bound White Hilt shield on your arm

## v0.60.0 - 2026-10-03

### Added
- **Rosehips**: dog roses on the Plains whose red hips hide when picked and grow back, or from cloudberry bushes picked there (30%). An orange-red dye (`[Foraging.Rosehips]`)
- **Yarrow**: feathery leaves and flat white flower heads on the Plains, or from wild flax picked there (20%). A wound herb and an ale herb (`[Foraging.Yarrow]`)
- **Caraway**: slender stems with umbrellas of white flowers on the Plains, or from wild barley picked there (20%). The northern spice (`[Foraging.Caraway]`)
- **Woad**: blue-green leaves under yellow flowers on the Plains, or from wild flax picked there (15%). The deep blue dye; Rock Lichen now dyes the true purple of its lye bath (`[Foraging.Woad]`)
- **Madder Root**: a scrambling plant with whorls of narrow leaves on the Plains, or from cloudberry bushes picked there (15%). Its roots are the red dye (`[Foraging.MadderRoot]`)
- **Henbane**: a sticky weed with pale, purple-veined flowers by the Plains fields, or from wild barley picked there (10%). For seers and berserkers (`[Foraging.Henbane]`)
- **Ergot**: patches of wild barley gone dark with blight on the Plains, or from wild barley picked there (15%). Its black horns bring dread (`[Foraging.Ergot]`)
- **Hop Cones**: wild hop vines twined up old stakes on the Plains; the cones hide when picked and grow back. Cloudberry bushes picked there give them 10% of the time (`[Foraging.HopCones]`)
- **Lox Milk**: crouch and use a tame lox to milk it once a day for 2 Lox Milk, one more from a groomed lox. Raises Animal Husbandry (`[Husbandry] LoxMilking`, `MilkPerLox`, `MilkDays`)
- **Rosehip Soup** (Stone Pot level 2): Rosehips, Honey and Barley Flour, 32 health and 64 stamina for 40 minutes (`[Food.RosehipSoup]`)
- **Caraway Lox Stew** (Stone Pot level 2): Lox Meat, Caraway, Onion and Turnip, 64 health and 36 stamina for 40 minutes (`[Food.CarawayLoxStew]`)
- **Skyr with Cloudberries** (Stone Pot level 2): Lox Milk, Cloudberries and Honey, 48 health and 48 stamina for 45 minutes; health regenerates 25% faster for the first half (`[Food.SkyrwithCloudberries]`)
- **Smoked Grouper with Yarrow** (Stone Pot level 3): Grouper, Yarrow and Juniper Berries, 66 health and 34 stamina for 45 minutes; blocking uses 25% less stamina for the first half (`[Food.SmokedGrouperwithYarrow]`)
- **Yarrow Gruit** (Cauldron, then Fermenter): Yarrow, Barley and Honey; below half health, health regenerates 2.5 times as fast for 10 minutes (`[Meads.YarrowGruit]`)
- **Hop Ale** (Cauldron, then Fermenter): Hop Cones, Barley and Honey; for 30 minutes Rested lasts 50% longer, also a Rested you already have (`[Meads.HopAle]`)
- **Henbane Beer** (Cauldron, then Fermenter): Henbane, Barley and Honey; berserkergang for 5 minutes: 30% more blunt, slash and pierce damage and no stagger, but half your armor, and you lose a fifth of your health when it ends (never fatal) (`[Meads.HenbaneBeer]`)
- **Gift of the Völva**: the seeress's draught, brewed from Henbane, Ergot and Rock Lichen. For 10 minutes the 40 nearest things to pick within 50 m, and every unopened chest the world placed there, show on the map (`[Potions.GiftOfVolva]`)
- **Obsidian Rune** and **Dread**: a rune of iron and obsidian, smithed at the Rune Forge for etching. Etched with Ergot into a bound White Hilt weapon, a quarter of its hits send the foe running in terror for 4 seconds; never bosses (`[Gear.Infusions] DreadCost`, `DreadChance`, `DreadSeconds`)
- **Mire Rune** and **Mire's Hold**: a rune of iron, copper and tar. Etched with Peat and Tar into a bound White Hilt weapon, every hit tars the target as a tar pit does, which slows it (`[Gear.Infusions] MireCost`)
- **Blood Rune** and **Berserker's Rage**: a rune of iron and bloodbags. Etched with Henbane into a bound White Hilt weapon, it hits harder the more health you have lost: up to 60% more damage near death (`[Gear.Infusions] BerserkerCost`, `BerserkerMaxBonus`)

## v0.59.0 - 2026-10-03

### Added
- **Juniper Berries**: prickly juniper shrubs on the Mountain slopes, or from wild onions picked there (20%). Their blue berries hide when picked and grow back. A dye, and an ingredient against the dead (`[Foraging.JuniperBerries]`)
- **Angelica**: a tall herb with green flower globes in the Mountains, or from wild onions picked there (20%). Vikings grew it and chewed the stalks for strength (`[Foraging.Angelica]`)
- **Iceland Moss**: curly brown lichen on the Mountain heath, or from wild onions picked there (15%). Boiled into porridge (`[Foraging.IcelandMoss]`)
- **Wolf Lichen**: bright yellow tufts on dead branches in the Mountains; the branch stays and the lichen grows back. Wolves drop it 10% of the time. A sulphur-yellow dye and a bane for beasts (`[Foraging.WolfLichen]`)
- **Mountain Sorrel**: round sour leaves and red seed spikes on damp Mountain ledges, or from wild onions picked there (25%) (`[Foraging.MountainSorrel]`)
- **Rock Lichen**: grey crusts on Mountain stones; the stone stays and the crust grows back. Stone Golems drop it half the time. The purple dye the paint bench lacked (`[Foraging.RockLichen]`)
- **Moss Porridge** (Stone Pot level 2): Iceland Moss, Crowberries and Honey, 30 health and 56 stamina for 35 minutes (`[Food.MossPorridge]`)
- **Juniper-Smoked Wolf Ham** (Stone Pot level 3): Wolf Meat, Juniper Berries and Onion, 58 health and 30 stamina for 40 minutes; for the first 20 minutes frost damage is halved (`[Food.Juniper-SmokedWolfHam]`)
- **Candied Angelica** (Stone Pot level 2): Angelica and Honey, a light snack of 20 health and 38 stamina that lasts 50 minutes (`[Food.CandiedAngelica]`)
- **Mountain Sorrel Salad** (Stone Pot level 2): Mountain Sorrel, Onion and Wild Garlic, 28 health and 48 stamina for 35 minutes with faster healing (`[Food.MountainSorrelSalad]`)
- **Juniper Sahti** (Cauldron, then Fermenter): Juniper Berries, Honey and Crowberries; spirit damage taken is halved for 10 minutes (`[Meads.JuniperSahti]`)
- **Gift of Kvasir**: the mead of poetry, brewed from Meadowsweet, Honey and Angelica. Every skill rises 50% faster for 20 minutes (`[Potions.GiftOfKvasir]`)
- **Gift of Ullr**: the hunter god's gift, brewed from Juniper Berries, Angelica and Feathers. Bows hit 25% harder, you move 15% faster and are harder to notice for 20 minutes (`[Potions.GiftOfUllr]`)
- **Gift of Heimdall**: the watchman's gift, brewed from Angelica, Crowberries and Crystal. Every foe within 60 m shows on the map for 10 minutes (`[Potions.GiftOfHeimdall]`)
- **Bone Rune** and **Seid Smoke**: a rune of iron, bone and silver, smithed at the Rune Forge and only etched, never hung on a post. Etched with Juniper Berries into a bound White Hilt weapon it adds spirit damage, the bane of draugr, skeletons and ghosts (`[Gear.Infusions] SeidCost`)
- **Fang Rune** and **Wolfsbane**: a rune of iron, wolf fangs and silver. Etched with Wolf Lichen it adds poison damage that is three times as strong against beasts: wolves, fenrings, bears, boars, deer, lox, hares and asksvin (`[Gear.Infusions] WolfsbaneCost`, `WolfsbaneBeastMultiplier`, `WolfsbaneBeasts`)
- **Whetstone**: a bar of mountain slate made at the Workbench. Use it from the inventory and your weapon deals 15% more slash and pierce damage for the next 30 hits (`[Coatings.Whetstone]` `Hits`, `Strength`)
- **Juniper Oil** (Cauldron): Juniper Berries and Resin; for 30 hits a fifth of the weapon's physical damage is added as spirit damage (`[Coatings.JuniperOil]`)
- **Wolf Lichen Oil** (Cauldron): Wolf Lichen and Entrails; for 30 hits a fifth of the weapon's physical damage is added as poison damage (`[Coatings.WolfLichenOil]`)

## v0.58.0 - 2026-10-03

### Added
- **Sphagnum Moss**: red and green cushions of bog moss on the wet ground of the Swamp, or from red mushrooms picked there (25%). A dye and a healer's ingredient (`[Foraging.SphagnumMoss]`)
- **Bog Bean**: three-lobed leaves and fringed white flowers at the edge of the Swamp water, or from wild turnips picked there (30%). Bitter, for fever draughts (`[Foraging.BogBean]`)
- **Labrador Tea**: a low evergreen shrub on the Swamp hummocks with domes of white flowers, or from thistles picked there (20%). Its sharp smell keeps biting insects away (`[Foraging.LabradorTea]`)
- **Cattail**: tall bog grass with brown velvet heads at the water's edge in the Swamp, or from wild turnips picked there (20%). Its roots give flour for Swamp dishes (`[Foraging.Cattail]`)
- **Meadowsweet**: tall red stems with frothy cream flowers on the damp edges of the Swamp, or from wild turnips picked there (20%). It sweetens mead and dulls pain (`[Foraging.Meadowsweet]`)
- **Bog Iron**: rust-brown lumps in the mud and shallow water of the Swamp. The Smelter turns each lump into Iron, the Iron Rune lets it through portals, and it gives a rust dye (`[Foraging.BogIron]`)
- **Peat**: stacks of cut peat on the drier banks of the Swamp. Use a brick from the hotbar on a wood fire (campfire, hearth, bonfire) and it burns like two pieces of wood (`FuelValue`); the Charcoal Kiln turns it into Coal (`[Foraging.Peat]`)
- **Bog Fish Stew** (Stone Pot): Trollfish, Cattail and Wild Garlic, 50 health and 30 stamina for 32 minutes (`[Food.BogFishStew]`)
- **Cattail Porridge** (Stone Pot): Cattail, Cranberries and Honey, 22 health and 52 stamina for 32 minutes (`[Food.CattailPorridge]`)
- **Meadowsweet Mead** (Cauldron, then Fermenter): Honey, Meadowsweet and Cranberries; 15% more armor and 25% less stagger for 10 minutes (`[Meads.MeadowsweetMead]`)
- **Bog Bean Bitter** (Cauldron, then Fermenter): Bog Bean, Sweet Gale and Honey; ends poison, burning, frost, shock, tar and smoke at once and keeps them off for 3 minutes (`[Meads.BogBeanBitter]`)
- **Labrador Tea Brew** (Cauldron, then Fermenter): Labrador Tea, Sweet Gale and Honey; leeches, deathsquitoes and ticks do not notice you for 10 minutes (`[Meads.LabradorTeaBrew]` `IgnoredBy`)
- **Gift of Eir**: brewed from Sphagnum Moss, Bog Bean and Honey. Heals half your health at once, ends poison, fire and frost and keeps them off, and doubles health regeneration for 10 minutes (`[Potions.GiftOfEir]`)

## v0.57.2 - 2026-10-03

### Fixed
- **Roofs**: the White Hilt roofs show where you aim and are placed there. They were put down twice as far from the middle of the world as where you aimed, so neither the ghost nor the placed roof could be seen
- **Dragon gable**: the dragon heads are carved wood instead of pink
- **Cultivator grid**: plants in a grid are now spaced so every one can grow. The spacing counts the size of the sapling and of the grown crop, not only the grow radius, and a place is red when the plant would stop a plant next to it from growing (or the other way round)

## v0.57.1 - 2026-10-02

### Fixed
- **Rune Post**: a rune ring now shows on the post as soon as it is hung (or disappears when taken), instead of only after you left the area and came back. An error when the post was loaded stopped it from updating its look

## v0.57.0 - 2026-10-02

### Added
- **Roofs**: six Viking roof coverings, each at 26°, 45° and 67° as roof, ridge, inner corner, outer corner and smoke hole: **turf roof** (sod over birch bark with a turf log along the eave; does not burn), **reed thatch**, **straw thatch**, **shingle roof** and **scale shingle roof** (tarred pine shingles with ridge boards) and **slate roof** (the strongest; does not burn). They snap like the vanilla thatch and sit right after it in the build menu. The overhang past the eave only shows on the lowest row. Wind and rain are muffled more under turf, slate, reed and straw. New sections `[Roofs]` and `[Roofs.<Covering>]`. See [Roofs](docs/roofs.md)
- **Smoke holes (ljore)** with a hatch you open and close; it closes by itself when rain starts and opens when it stops, unless a fire burns below
- **Dragon gable**: crossed barge boards with carved dragon heads for the ends of a ridge, made with Lindorm Scales (`[Roofs.Gable]`)
- **Roseroot on turf roofs**: plant roseroot on a turf roof and pick it again and again (`[Roofs.Garden]`)
- **Roof materials**: Birch Bark from felled birches and split birch logs, Turf from digging grassland with a pickaxe, Reed (a new Swamp forageable at the water's edge), Pine Tar from the new **Tar Kiln**, Slate and Soapstone from new slate outcrops and from any Mountain rock, and Straw from every barley and flax harvest (`[Roofs.Materials]`, `[Foraging.Reed]`)
- **Soapstone Hearth**: a hearth of soapstone that burns twice as long on its wood and gives 3 comfort

## v0.56.1 - 2026-10-02

### Fixed
- **Benches and stations with their own model** break into pieces of their own model when they are destroyed or removed, instead of into the pieces of the vanilla workbench, table, forge part or mortar they were made from. The Paint Bench, Rune Forge, Trophy Altar, Rune Etching Table, Binding Stone, Chain Bench, Herb Tray, Smoke Oven, Rune Post, Tether Post, Munin's Perch, Portal Astrolabe, Valkyrie Stone and Harbour Anchor split into 5 to 8 chunks that burst apart; things standing on them, such as the pots on the Paint Bench, fall off whole

## v0.56.0 - 2026-10-02

### Added
- **Trophy Altar** copies ordinary trophies too: one trophy and one Hard Antler from Eikthyr give a full stack. It covers every trophy of the game itself except the boss trophies, which still take a Swamp Key, and deer trophies, which are left out because they summon Eikthyr. The White Hilt monsters' trophies, the black beast trophies and other mods' trophies are not copied. New settings `OrdinaryTrophies`, `AntlersPerCraft`, `OrdinaryTrophiesMade` and `ExcludedTrophies` in `[TrophyAltar]`. See [Base](docs/base.md)

## v0.55.0 - 2026-10-02

### Added
- **Portal effects**: travelling by portal, Home Stone or Valkyrie Stone now looks like something. You rise, spin and stretch thin while a ring of glowing runes turns under you and sparks stream off your body, then vanish in a flash. At the far side the ring pulses while your world loads, and you appear in a flash of sparks and dust and settle to the ground. Others nearby see all of it. Your own camera swings out, the view widens and the screen flashes white behind a ring of runes; the loading screen waits for the effects. The colour follows the strongest rune at the portal you leave from, a dog travelling along gets a small flash, and leaving and arriving have their own sounds. New section `[Portals.Effects]`, each player's own. See [Portals & travel](docs/portals.md)

## v0.54.2 - 2026-10-02

### Fixed
- **Portals**: a player who travels through a portal no longer stays behind as a frozen copy for players near the portal, repeating their attacks and emotes. This is a bug in the game itself: the server only told the others that the player had left when the old spot was already outside their area. The mod must be on the server for the fix to work

## v0.54.1 - 2026-10-02

### Fixed
- **Chests (Linear)**: fish of level 2 and up can be made unlimited. Each level is unlocked on its own by storing a full stack of it, and the chests then keep a stack of that level too. Before, a fish level above 1 could never be unlocked once level 1 was, and its tooltip kept asking for "0 more". Level 1 progress now only counts level 1 fish. In Full and Discovered mode every level of a fish is unlimited once stored, like other ordinary items

## v0.54.0 - 2026-10-02

### Changed
- **Binding Stone** has its own model: a broad runestone on a footing stone, bound with an iron band and a ring, with a serpent band of blood-red runes along its edge and a valknut round a Surtling Core. The runes and the core glow. It is solid stone to hit, and its collider fits the stone instead of the forge cooler it was copied from
- **Rune Etching Table** has its own model: a carver's table with a stone slab half covered in runes, a chisel, a mallet, rune rings and a bowl of red ochre, instead of the Galdr table's rune table

## v0.53.0 - 2026-10-02

### Added
- **Treasure maps** from Hildir (750 coins, once The Elder is slain): each map buries a new treasure in land you have explored and shows a scrap of that land with a cross on it, drawn like an old chart with shores, height lines, forests, landmarks, a north arrow and a scale. Parts are faded and the edges torn, so you match it against your own map. A cairn with a stick and a red rag marks the spot; three pickaxe blows dig up a chest with a black beast trophy (of a beast whose boss is slain) and a few things from the mod. Help on the map follows `HintLevel` (Easy, Normal, Hard), and on Normal the Exploration skill fades less and adds landmark names and a dotted path. Carrying the map, you are told when the ground nearby looks dug up and dust rises from the heap. New section `[Treasure]`. See [Treasure maps](docs/treasure.md)
- Own models for the treasure map, the heap of dug earth and the cairn, and the CC0 Treasure Chest from Poly Haven

### Fixed
- Buying a puppy from the Bog Witch no longer throws an error after the purchase (the trade entry had no buy effects)

## v0.52.0 - 2026-10-02

### Added
- **Binding Stone** (Rune Forge extension: Stone ×20, Chain ×2, Surtling Core ×1): use a black beast trophy on it to bind it to the White Hilt weapon in your hand, or to the shield. One trophy per item, used up, and a new one replaces the old. The bonus follows the beast: +5% damage or block power with the Black Troll up to +20% with the Black Morgen and Black Bonemaw
- **Rune Etching Table** (Rune Forge extension, the Galdr table's rune table: Fine Wood ×10, Iron ×4, Resin ×6): use a rune on it to etch it into a trophy-bound White Hilt weapon, with materials the mod adds: Dyrnwyn's Flame (Flametal Rune, Surtling Cores), Frost (Silver Rune, Crowberries), Venom (Bronze Rune, Poison Glands), Storm (Black Metal Rune, Kraken Ink), Spider's Web (Iron Rune, Spider Silk) and Grip of the Deep (Gold Rune, Kraken Ink and Tentacle). Its strength follows the bound trophy: 10% to 25% of the weapon's base damage
- Binding and runes are kept on the item and shown in its tooltip. New sections `[Gear.Binding]` and `[Gear.Infusions]`. See [Smithing](docs/smithing.md#-binding-and-rune-etching)

### Changed
- Black beast trophies are never supplied by the restocking chests, carts or ship holds, also in Full mode

## v0.51.0 - 2026-10-02

### Added
- **Compass on the map**: a brass compass in the bottom-right corner of the minimap and the top-left corner of the large map. The letters stay put (the map is north up) and the needle points where you look, with the bearing in degrees under it. The letters follow the game's language (N, Ø, S, V in Norwegian). New section `[Map.Compass]`. See [Navigation](docs/navigation.md#-compass-on-the-map)

### Changed
- **Heading ring on map portraits**: the small arrow at the bottom of your portrait is replaced by a ring around it with a point that slides round the edge as you turn. Nearby players get a white ring pointing where they face; further away the ring has no point. New setting `[Map] HeadingMarker`
- **Munin's memory** and **Uncovered** sit side by side under the bottom-left corner of the large map, lined up with its left edge, and open upwards over the map. Uncovered no longer covers the vanilla buttons at the bottom right
- The **Swamp Key** can be kept in the four extra accessory slots; it still opens crypt doors from there
- Crafting, building and fuelling never take items from the accessory slots, so a key or amulet kept there is not used up by a recipe (e.g. the Trophy Altar's Swamp Key)
- In linear mode the **Gifts of Hugin, Munin and Brokkr** are no longer available: there you learn skills and find recipes yourself. A `[Tiers]` setting can still give them a tier

## v0.50.0 - 2026-10-02

### Added
- **Trophy Altar**: a small stone altar with a miniature Eikthyr. One boss trophy and one Swamp Key give a full stack (20) of that trophy, so every extra stack costs another kill of The Elder. It copies the seven vanilla boss trophies, and a trophy only shows up once you have picked it up. Built at the workbench from Stone ×10, Fine Wood ×4, Iron ×2 and Ancient Bark ×2. New section `[TrophyAltar]`. See [Around the base](docs/base.md#-trophy-altar)

## v0.49.0 - 2026-10-02

### Added
- **White Hilt weapons and shields grow with the world**, like the armor: quality 5 with Silver (Mountain), 6 with Black Metal (Plains), 7 with Carapace (Mistlands) and 8 with Flametal (Ashlands), Iron ×5 per level up to quality 4. A weapon gains 10%, 30%, 35% and 30% of its base damage at those levels (the White Hilt Sword goes from 79 slash at quality 4 to 137 at quality 8), a shield 20% and 40% of its base block power, always a little below the best vanilla gear of that biome. The staffs stay at quality 4. New sections `[Gear.Weapons] Upgrade*`, `*Damage` and `[Gear.Shields]`. See [White Hilt gear](docs/equipment.md#-upgrades-through-the-biomes)
- Every shield upgrade also takes a **Lindorm Scale**, the first use of the Lindorm's loot

### Changed
- The Lindorm and giant spider loot is now used: **Gift of Loki** is brewed with Spider Silk (Loki made the first fishing net) instead of Raspberries, **Gift of Hel** with Poison Glands instead of Stone, and **Shore Nets** are mended with 2 Spider Silk, or 4 Leather Scraps without silk (`[Fishing.Net] MendWith` replaces `MendItem` and `MendAmount`)
- **Gift of Idunn** is brewed with Lingonberries and Honey, and **Gift of Skadi** with Crowberries and Roseroot, instead of wood, stone and raspberries
- The map table range is one setting for the Cartographer's Desk, Portal Astrolabe, Harbour Anchor and Munin's Perch: `[Navigation] MapTableRange` (was `[Portals] MapTableExtensionRange`, and the desk always used 5 m). The value is moved over
- Config sections renamed to match the feature pages: `[Ranching]` is now `[Husbandry]` and `[Defenses]` is now `[Defences]`. The values are moved over
- The console command `bsc_progress` is now `whitehilt_chest_progress`
- Player portraits are kept in `BepInEx/config/BrudvikWhiteHilt/portraits/` with the blueprints and films; existing ones are moved there from `BepInEx/config/WhiteHilt/`
- Documentation: everything the Exploration skill gives is in one table, the four map table pieces are listed together, and the linear tier table lists every item again

### Fixed
- O no longer uses the Lookout while the build camera is on, where it switches the photo view

### Removed
- **Gift of Mimir**: its map reveal overlapped the Exploration skill, the Lookout and the Navigator's Table. Gifts of Mimir and their mead bases in inventories and chests disappear, and its settings are removed from the config file

## v0.48.0 - 2026-10-02

### Added
- **White Hilt armor grows with the world**: the armor and the uniforms upgrade past quality 4, one level per biome after the Swamp: quality 5 with Silver (Mountain), 6 with Black Metal (Plains), 7 with Carapace (Mistlands) and 8 with Flametal (Ashlands). A helmet, chest or leg piece goes 26 / 30 / 36 / 42, a cape gains 2 per level, always a little below the best vanilla armor of that biome. From quality 5 the station level of quality 4 is enough. New settings `[Gear.Armor] Upgrade*`, `*Armor` and `CapeArmorPerBiome`. See [White Hilt gear](docs/equipment.md#-upgrades-through-the-biomes)

### Changed
- White Hilt armor was too strong for the Swamp (a full set at quality 4 had 92 armor, more than the Mountain wolf set). A piece now has 18 armor at quality 1 and 24 at quality 4 (was 27), and a full set 80 at quality 4: good enough for the Mountains. `ArmorPerLevelBonus` is now 0 (reset once in existing config files)
- Upgrading White Hilt armor now costs Iron ×5 per level up to quality 4 (it was free); the recipe's own materials are only paid when crafting
- `MovementBonus` only takes away the iron armor's slowdown: the helmet and capes no longer make you 5% faster each
- The White Hilt Cape needs a Deathsquito Trophy instead of 3 Wraith Trophies, and unlocks with the Plains tier in linear mode

## v0.47.1 - 2026-10-02

### Fixed
- The White Hilt Uniform Cape no longer gives feather fall; it keeps the White Hilt Cape's armor

## v0.47.0 - 2026-10-02

### Added
- **Exploration overview** on the large map from Exploration 25: the share and area of each biome you have uncovered (shared map too from Exploration 50), the whole world, and what Munin's Perch has found. New section `Map.Overview`. See [Navigation](docs/navigation.md#-exploration-overview)

## v0.46.0 - 2026-10-02

### Added
- **Mooring Post** (Hammer, Workbench: Fine Wood ×4, Iron ×1, Leather Scraps ×4): a thick post with a coil of rope. Use it to moor the nearest ship within 20 m; a rope runs to the ship, which lies still where it is (rocking on the waves) and cannot sail until it is cast off at the post. Works on every ship, one ship per post, and holds after logging out. New setting `Ships` → `MooringRange`. See [Ships](docs/ships.md)

## v0.45.0 - 2026-10-02

### Added
- **Weather forecast** on the large map within 10 m of a map table or the Cartographer's Desk, and aboard a ship with a Navigator's Table: the weather and wind now and in the coming periods of about 11 minutes, worked out the way the game draws them. 1 period ahead at Exploration 0, up to 4 at 100. **Storm warning** with the ship's bell aboard a ship with a Navigator's Table, 3 minutes before thunder or a snowstorm. New section `Navigation.Forecast`. See [Navigation](docs/navigation.md#️-weather-forecast)

## v0.44.0 - 2026-10-02

### Added
- **Man overboard**: fall into the water from a ship moving at 2 m/s or more, and everyone aboard is told, with the ship's bell, a pin on the map and an arrow on the minimap toward you; you get a pin and an arrow toward the ship. A ship sailing its route or holding its course stops. From the deck within 25 m, **E** throws a **lifeline** that pulls the one in the water back aboard. New settings `Ships` → `ManOverboard`, `Overboard*`, `Lifeline*`. See [Ships](docs/ships.md)

## v0.43.0 - 2026-10-02

### Added
- **Sounding line and shoal warning** on every ship: the read-out under the wind indicator shows the depth of the water under the ship, rocks under water included. While you steer, or ride a ship that sails its route, the water ahead is sounded (5 seconds of sailing, 15 to 60 m past the bow); shallower than 3.5 m or rocks in the way turns the depth red, shows a warning and rings the new **ship's bell**. Quiet below 2 m/s and at most once every 8 seconds. Each player's own settings `Ships` → `ShowDepth`, `ShoalWarning`, `ShoalBell`, `Shoal*`. See [Ships](docs/ships.md)

## v0.42.0 - 2026-10-02

### Added
- **Pathfinder's Ruby Amulet**: from Exploration 50 (new milestone **Ruby Pathfinder**), the Cartographer's Desk sets a large ruby in the middle of the Pathfinder's Amulet (Pathfinder's Amulet ×1, Ruby ×3, Iron ×2). It does all the Pathfinder does, and while you wear it, Shift + click on the large map sets a target, on one of your pins if you click one. An arrow at the top of the screen points the way with the name and distance, a red ring marks the target on both maps, and an arrow on the minimap's edge points to it while it is beyond the minimap. Keep moving away from it and the ruby tells you that you are going the wrong way; reach it and the target is removed. The target is your own, saved per character and world. One already made keeps working if Exploration drops below 50. New sections `Gear.WhiteHiltPathfinderRuby` (rules from the server, arrow settings your own) and `[Skills.Exploration] RubyPathfinderLevel`. See [Navigation](docs/navigation.md#-pathfinders-ruby-amulet-a-target-on-the-map)

## v0.41.0 - 2026-10-02

### Added
- **Munin's Perch**, a new map table extension: a carved post with a raven on top (Workbench: Fine Wood ×8, Iron ×2, Feathers ×6). While it stands within 5 m of a map table, the caves, settlements, wild berries and plants, resources and landmarks uncovered on that table's map can be shown on everyone's map, each with its in-game icon. Nothing shows until you pick it in the new **Munin's memory** panel at the left of the large map, where every kind that has been found is listed by group. Plants and deposits are counted per 64 m square. The markers need Exploration 20 on the large map and 50 on the minimap. Silver veins are hidden by default, and treasure is never shown. All numbers in the new section `Map.Discoveries`. See [Navigation](docs/navigation.md#-munins-perch-discoveries-on-the-map)

## v0.40.0 - 2026-10-01

### Added
- **Self-closing doors**: doors, gates and windows built by players close on their own a few seconds after the last one went through (doors 5 s, gates 10 s, windows off by default), never while a player or tamed animal is within 3 m. Shift + E holds a door open. Windows close when rain or night begins, and everything closes when enemies on the hunt come within 20 m. Key doors, dungeon doors and doors that cannot be closed are left alone. All numbers in the new server section `Doors`. See [Around the base](docs/base.md)

## v0.39.0 - 2026-10-01

### Added
- **Camera sweep when a ship sets off on its route**: with "Take me there" or explorer mode, the camera of everyone sitting aboard swings out around the ship, stops for a moment in front of the sail and comes round to behind you again, with the HUD hidden. Looking calmly around does not disturb it; a quick swing of the mouse, standing up or opening a menu brings the camera back at once. Each player's own settings `Ships` → `RouteCameraSweep*`. See [Ships](docs/ships.md)

## v0.38.0 - 2026-10-01

### Added
- **Black White Hilt uniforms** with gold trim, the White Hilt badge on the left breast and the logo on the back of a gold-edged cape: Uniform Tunic and Trousers, Officer's Jerkin and Breeches, and the Uniform Cape. They are indestructible and have the same armor as the White Hilt Chestplate, Greaves and Cape. Made at the Workbench (level 2) from Deer Hide, Leather Scraps, Coal and Coins (Troll Hide for the cape). The officer's pieces copy the Deep North medium armor, which vanilla cannot craft yet, so a game update may change their look. See [White Hilt gear](docs/equipment.md#black-uniforms)

### Fixed
- The logo on the White Hilt Banner Cape sat about 5 cm to one side, and its edge could show on the cape's tail at the hips. It is now centred on the back and a little smaller (0.46 m), like on the new Uniform Cape

## v0.37.0 - 2026-10-01

### Added
- **Fishing nets**: build a Net Winch with a fish barrel on the shore and set out Shore Nets (10 m) on the water within 25 m of it. Each net catches a fish every 6 minutes where the water is deep enough, also for up to 2 hours while nobody is near, and the fish end up in the barrel. Bait in the barrel makes the nets catch faster, the nets must be mended with Leather Scraps now and then (Shift + E on the winch), and whoever opens the barrel gets Fishing experience. All numbers in the new `Fishing.Net` section. See [Fishing nets](docs/fishing.md)

## v0.36.0 - 2026-10-01

### Added
- **Ship camera zoom**: at the helm the camera zooms 2 m further out than before, and everyone aboard a ship, standing on deck or sitting, can now zoom out just as far as the one at the helm. Settings `Ships` → `CameraExtraZoom` and `CameraZoomAllAboard`. See [Ships](docs/ships.md)

## v0.35.0 - 2026-10-01

### Added
- **What you have** in the crafting panel and the build menu: a small box on each requirement's icon shows how many you have in your inventory and the chests around you, or a gold ∞ when a restocking chest nearby keeps the item unlimited. In Linear chest mode, a thin gold bar shows how close the item is to becoming unlimited, and the tooltip says how many more are missing. See [Crafting panel](docs/base.md#-crafting-panel)
- **Craft several at once**: arrows next to the Craft button (or the mouse wheel over the number) choose how many to make, e.g. 4 axes. Settings in the `CraftingPanel` section, `MaxCraftAmount` (20) set by the server

### Fixed
- In the build menu, the small chest icon could stay on the crafting station slot after looking at another piece

## v0.34.1 - 2026-10-01

### Fixed
- The map areas (bases, outposts, fields, pastures and wards) were drawn over portal and ship pins and the player marker. They are now drawn under them, and their colours are softer

## v0.34.0 - 2026-10-01

### Added
- **Unlimited cargo**: carts and ship holds, including the White Hilt Ship's sea chest, keep the stacks of unlimited items full, like the Everlasting Chest. Bring Wood in the ship when Wood is unlimited, and it never runs out while you build from the hold. Cargo never unlocks items or adds items you did not bring. Setting `Chests` → `UnlimitedCargo`. See [Restocking chests](docs/chests.md#carts-and-ships)

### Fixed
- Removing a chest dropped its unlimited stacks again (e.g. a full stack of Wood from the Wood Chest). Taking the unlimited stacks out made the chest refill them at once, and the refills were dropped. A chest that is being removed is no longer refilled

## v0.33.0 - 2026-10-01

### Changed
- The **White Hilt Rune Circle** lies on a round slab of dark, glassy stone that frames it and lifts it a little off the ground. The rounded rim slopes down to the ground, so you walk straight onto it, and glows a faint blue that grows a little stronger as you come near. Rune circles already built get the stone too

## v0.32.1 - 2026-10-01

### Changed
- Player portraits on the map are drawn on top of everything else (area outlines and names, markers, other pins), so other players can always be seen
- You are shown with your own portrait too, with your direction arrow at the bottom of it instead of the plain arrow

### Fixed
- Portraits were framed too high, showing mostly the top of the head. The head is now found in the picture and centred, with a little of the shoulders below. Every portrait is taken again the next time the character is shown in the main menu
- The helmet was still in the portrait, because it was only removed at the end of the frame the picture was taken in

## v0.32.0 - 2026-10-01

### Added
- **Stone Dowser**, cut at the stonecutter (Stone ×20, Iron ×2, Greydwarf Eye ×5) and worn in an accessory slot. It leads to the nearest clearing that still has rocks for the Mysterious Rock: a pin on the map with the number of rocks left, and the direction and distance when it changes. Near a rock it pings like the Wishbone, at a lower pitch. Settings under `Gear.WhiteHiltStoneDowser`. See [Navigation](docs/navigation.md)

## v0.31.0 - 2026-10-01

### Added
- **Explorer mode** at the Navigator's Table, next to "Take me there": the ship sails past the markers as close to land as it safely can, following the coast instead of the fastest way. Near land it never sails with full sail, only half sail, or rowing while anyone stands, so you can stand and shoot at monsters along the way. Settings under `Ships`: `RouteExploreLevel`, `RouteExploreCoastCells`, `RouteExploreOpenWaterCost`, `RouteExploreNearLand`

### Changed
- "Take me there" takes the sail down to half when the ship goes faster than 45 knots, and sets it full again once it is well below. Change it under `Ships` → `RouteMaxSpeed` (0 never reefs)
- "Take me there" waits until the player who chose it sits down (on a bench, the deck or in a bed) before the ship sets off. If they have not sat down within 30 seconds (`Ships` → `RouteSitSeconds`), the route is called off and has to be started again
- While anyone aboard stands, the ship sailing its route rows at the slowest speed, also when the captain stands up again

### Fixed
- "Take me there" with several markers sailed straight for the last one when the way there was open water. It now sails past every marker in turn

## v0.30.0 - 2026-10-01

### Added
- **F7** opens and closes the White Hilt settings window anywhere, not only from the inventory. Change it in the window under `Settings.Keys` → `OpenSettings`

### Changed
- The settings cog is replaced by a small White Hilt panel in the bottom left corner of the inventory, right of the health and food bars: a short explanation, the shortcut and a **Settings** button like the Craft button. The cog was easy to miss and floated in the middle of the screen
- The settings window shows the description of a setting in a dark box that fills the space down to the buttons

## v0.29.2 - 2026-10-01

### Fixed
- The main menu logo covered the Merch Store button and its version text ran into the game's version label. It now sits in the top right corner, with the version on one line

## v0.29.1 - 2026-10-01

### Fixed
- The chest census on a dedicated server counted 0 chests: it ran before the server had loaded the world. It now runs right after the world is loaded, and an empty count is never used as the baseline for the next comparison

## v0.29.0 - 2026-10-01

### Added
- The restocking chests of BrudvikStackedChest are now part of White Hilt: 14 chests in the Hammer's Chests category with Full, Linear and Discovered modes, Learn all, the gathering panel and `bsc_progress`. See [Restocking chests](docs/chests.md)
- Chests placed with BrudvikStackedChest keep their contents: same prefab names, size, saved data and server progress folder. On the first start the old config file is taken over (above all `Mode`) into the new `[Chests]` section
- While BrudvikStackedChest is still installed, White Hilt leaves the chests to it
- Chest census: at every world start the server counts every chest's contents from the saved world, writes it to `BepInEx/config/BrudvikStackedChest/census/` and logs anything that decreased since the last count. Also `whitehilt_chest_census`
- Chest settings in the settings window, in English and Norwegian

### Fixed
- Chests no longer swallow, refill, merge or delete items with skill stars or their own data (such as a dog's remains); such items now always count as stored by the player

## v0.28.0 - 2026-10-01

### Changed
- White Hilt gear and Gift of Odin no longer make you practically immortal; they now give a small boost. Gift of Freya and the White Hilt Ship are unchanged
- Armor: no more +999 armor on every White Hilt item (the Megingjord counted too). Each armor piece gets +4 armor and +1 extra per quality level (was +10). Still indestructible, weightless and 5% faster per piece
- White Hilt Helmet keeps the Flametal look but has the Iron Helmet's stats
- Weapons: +10% damage (was +50%), and +2 per quality level only on the damage types the weapon already deals (was +10 on plain, fire, pierce and slash). No more free +10 plain/fire/pierce damage. Still indestructible
- White Hilt Sword has the Iron Sword's stats instead of Dyrnwyn's, but keeps Dyrnwyn's flaming hits and trail and deals +5 fire damage, setting the target briefly alight
- Gift of Odin: full heal on drink, +50 max HP, +2 HP/s, 2x health regeneration and half fall damage for 10 minutes (was 500 max HP, 20 HP every frame and 21x regeneration for 20 minutes)
- Gift of Idunn: 1.25x health and 1.5x stamina/eitr regeneration for 20 minutes (was 5x and +1 HP/s for 40 minutes)
- Config: the changed settings are reset once to their new defaults; `[Gear.Indestructible] ArmorBonus`, `[Gear.Weapons] BonusDamage` and Gift of Odin's `MaxHealth` and `HealPerFrame` are removed from the file. New: `[Gear.Armor] ArmorBonus`, `[Gear.Weapons] SwordFireDamage`, Gift of Odin `BonusMaxHealth` and `HealPerSecond`

## v0.27.0 - 2026-10-01

### Added
- In-game settings window: open the inventory and click the cog in the bottom left corner. Every White Hilt setting, grouped by feature, searchable, in English and Norwegian
- Two tabs: your own settings, and the server and world rules. Admins (and everyone in a local game) can change the rules; other players see the server's values read-only
- Changes are saved with the Save button; server settings are then sent to the server, which saves them and passes them on to everyone online at once. Settings that need a restart are marked
- Console command `whitehilt_config_missing` lists settings without a translation

### Changed
- Many config changes at once are applied together, once per frame

## v0.26.0 - 2026-10-01

### Added
- Everything can be configured, and the defaults are the values the mod used before, so nothing changes until you edit the config
- `[Content]`: switch any White Hilt item or piece off. It can no longer be crafted or built; copies players already have are kept
- `[Recipes]`: replace the recipe of any White Hilt item or piece, e.g. `Iron:10:5, FineWood:4`. Empty keeps the built-in recipe
- Gear: the White Hilt bonuses of weapons, armour, tools, ammunition, runes, belt pouch, Home Stone, Navigator's Table, Pathfinder's Amulet and the indestructible armour and weight (`Gear.*`), and every mead's duration and effect (`Meads.*`)
- Gift potions: duration and every effect strength per potion (`Potions.GiftOf*`)
- Ranching: switches for husbandry bonuses, favourite foods, trough production and grooming, plus all its numbers, the favourite food list and what each animal produces (`Ranching`)
- Dog: `Enabled` (the Bog Witch stops selling puppies), litters, bond, tricks, digging, healing, guard and sniff numbers
- Ships: switches for course holding, pushing, the fishing net, tent shelter, the ship portal, sea routes and the route autopilot, and the fishing net, auto anchor and Mast Wisp numbers
- Navigation: switches for the bigger explore radius and the shared map reveal, and their levels, bonuses and the Raven Sight radius (`Navigation`)
- Skills: the level of every milestone and the strength of its effect (`Skills.<Skill>`), and the star chances
- Defences health, Chain Bench output, compost, rune post and Valkyrie Stone rules, paint pot uses, build tool reaches, regrow time, pick amount and group size per forageable, Kraken loot, Lindorm and Giant Spider extras
- Branding: `MainMenuLogo` switch; Backpack: `ShieldFollowsWeapon` and `CoinsToCoinSlots` switches

### Fixed
- Descriptions and tooltips that name a configurable number (trough, tether post, dog bowl, compost bin, Navigator's Table, Pathfinder's Amulet, Exploration skill, Portal Astrolabe and Harbour Anchor, Valkyrie Stone, dog bandage and trick lessons, Gift of Thor, Crowberry Wine, Roseroot Mead, Sweet Gale Ale) now show the configured value, in English and Norwegian
- Texts with a number in them (skill book, ore echo, lookout, catch log, junk filter, map area counts, compost bin) showed a raw `{0}` instead of the number

## v0.25.1 - 2026-10-01

### Fixed
- Dogs no longer breed like vanilla wolves: two players' dogs near each other could get wolf cubs, faster than wolves do. Litters with real puppies work as before

## v0.25.0 - 2026-10-01

### Added
- Production timers on smelters, kilns, spinning wheels, windmills, the eitr refinery, fermenters, cooking stations, ovens, beehives, sap collectors, fires, eggs and breeding tame animals: time left, fuel for the queue, when food burns, and why a station has stopped, in the hover text
- Labels over working stations near you, an overview of the stations around you (K), and messages when a station finishes, fills up, stops or is about to burn the food. Each kind can be turned off in the `Production` config

## v0.24.0 - 2026-10-01

### Added
- Skill milestones at levels 25, 50, 75 and 100 for Foraging, Cooking, Woodcutting, Pickaxes, Blocking, Farming, Fishing, Animal Husbandry and Exploration, plus bonuses that grow with every level. The tooltip of each skill in the skills dialog shows what it gives at your level and its milestones
- Foraging, a new skill that rises with wild picks: extra yield, stars, best picking times and sweep picking
- Stars on food, wild picks, crops and seeds: starred food gives more and lasts longer, starred ingredients give starred dishes, starred seeds give better crops. Items with different stars do not stack
- Cooking: faster cooking stations near a skilled cook, food that does not burn near a watchful cook, and a junk filter (Shift + E on an item on the ground)
- Woodcutting: aimed falls, replanting, domino felling, old growth, clean splits and bird's nests
- Pickaxes: clean strikes, an ore echo on the map, rich veins, extra ore and finds
- Blocking: more health, less damage, cheaper blocking, Riposte, Shield Wall, Last Stand and Iron Guard
- Farming: green thumb, giant crops, and the Compost Bin (a slatted wooden bin, model by Pants85), which turns waste into compost that makes crops nearby grow faster
- Fishing: a fight on the line, snags, legendary fish, double catches and a catch log
- Animal Husbandry: twins and stronger young
- Exploration: the Lookout (O) opens up the map around you and shows sea monsters and ships
- `DeathLossMultiplier` scales the skill loss on death

## v0.23.0 - 2026-10-01

### Added
- Built areas on the map: everything players have built is outlined and filled in the builder's colour, with a gold border for a base (bed and fire), blue for an outpost (workbench or portal) and white for other buildings. Fields of planted crops are drawn green, pastures with tamed animals brown with the number of animals, and wards as a dashed ring showing their reach. Zoomed in, the large map shows names; a sign whose text starts with `#` names the area. Only uncovered map is drawn; the server decides whether players see each other's buildings and how many pieces, plants or animals make an area

## v0.22.0 - 2026-10-01

### Added
- Player portraits on the map: other players are shown as a portrait of their Viking on a see-through black disc with the name under it, instead of the red figure. The portrait is taken once in the main menu (bare head, neutral light), shared once per look and cached by the other players; players without one get their first letter. `whitehilt_portrait test` shows a test pin with your own portrait

## v0.21.0 - 2026-10-01

### Added
- Lindorm: a great worm that breaks out of the ground near players on foot in the Black Forest and the Swamp at night, once Eikthyr is slain; it burrows back down when it loses you or at dawn, and drops Lindorm Scale, Entrails and sometimes the Lindorm Trophy. `whitehilt_lindorm` shows the conditions and `whitehilt_lindorm summon` calls it (admins)
- Giant spiders: poisonous spiders whose bite webs you (slowed for a few seconds), spawned by spider nests in newly generated Black Forest land; they drop Spider Silk, Poison Gland and sometimes the Giant Spider Trophy, and a destroyed nest drops Spider Silk
- Asset pipeline: `export_creature.py` can key new animations for a model that lacks them (`generate` in the creature.json), and creature controllers can blend walk and run and play a stagger clip

## v0.20.0 - 2026-10-01

### Added
- Kraken: on a calm, foggy night at sea the Kraken rises beside your ship, raises tentacles around it and holds it fast until it is slain; it drops Kraken Tentacle, Kraken Ink and the Kraken Trophy. Damage to the crew and to the ship can each be scaled or turned off in the config; `whitehilt_kraken` shows the conditions and `whitehilt_kraken summon` calls it (admins)
- Octopus: an animated octopus swims in the ocean and can be caught with a fishing rod
- Stone Pot dishes Octopus Stew and Kraken Feast; Kraken Ink is a black dye at the Paint Bench
- Animated creatures in the asset pipeline: rigged glTF models are converted with Blender and built into Unity prefabs with an animator (`AssetSource/Creatures`, `export_creature.py`, `render_creatures.ps1`)

## v0.19.1 - 2026-09-30

### Fixed
- Pathfinder's Amulet: the character locked up when Raven Sight ended (NullReferenceException in StatusEffect.RemoveStartEffects every frame); the raven effects are no longer start effects

## v0.19.0 - 2026-09-30

### Added
- Painting: the Paint Bench with a colour wheel that mixes paint pots from dyes in your inventory and nearby chests, and the everlasting White Hilt Paint Brush that paints, stains, cleans and picks colours on building pieces, one piece or a whole radius at a time

## v0.18.0 - 2026-09-30

### Added
- Beams and poles: Wood beam 4m, Wood pole 4m, Wood iron beam 4m and Wood iron pole 4m, Wood iron beam and pole 1m, and Wood iron beams at 26° and 45°, each with its own icon and right after its original in the hammer

## v0.17.0 - 2026-09-30

### Added
- "Take me there": everyone aboard sees the speed in knots, heading, wind and the time left to arrival (minutes:seconds) while the ship sails the route

### Changed
- "Take me there" steers around rocks and shallows it meets: it sweeps the water ahead as wide as the hull and as deep as the keel, also for rocks under water, turns up to 75° off course and slows to half sail. It stops only when no way around is found
- Routes keep further from land: each straight leg is checked on the sea floor every 4 m and 9 m to each side, and the ship turns 20 m before a turning point instead of 40 m

## v0.16.0 - 2026-09-30

### Added
- Hotbar: under a bow, crossbow or fishing rod the arrows, bolts or bait it will use are shown with how many are left, red at 20 or fewer (`ShowHotbarAmmo`, `LowAmmoWarning`). Under a staff, how many casts your current eitr allows (`ShowHotbarCasts`)

## v0.15.3 - 2026-09-30

### Added
- Road planning: the up/down arrow keys change the road width on the map (1 to 12 m), so it no longer needs the cursor

### Fixed
- Build tools: holding Left Alt showed the cursor but it would not move on Linux. The game locked and unlocked it every frame; it now stays unlocked while the key is held

## v0.15.2 - 2026-09-30

### Fixed
- Group tools: turning selection mode off left the selected pieces glowing blue (and still selected). Leaving selection mode now clears the selection

## v0.15.1 - 2026-09-30

### Fixed
- The inventory threw an error every frame when it held a White Hilt Buckler or Tower Shield painted in a colour other than the first: the new icon now covers every paint variant

## v0.15.0 - 2026-09-30

### Added
- The Brudvik White Hilt logo: in the main menu with the mod version, and on a black loading screen when entering a world, teleporting and respawning (`Branding` → `LoadingScreenLogo`)
- White Hilt Banners in white and black, in the normal size and half again as big
- White Hilt Banner Cape: a white troll hide cape with the logo on the back
- The logo is the Thunderstore icon and heads this README

### Changed
- White Hilt Ship: the sail is white with gold stripes along the edges and the logo in the middle, and the shields along the rail carry the logo, all turned upright

## v0.14.0 - 2026-09-30

### Added
- Planting: the cultivator plants vanilla berry bushes, mushrooms, flowers, debris (branch, stone, flint) and decorative flora (small trees, bushes, shrubs, vines, ferns), unlocked by having had one of each ingredient; growth and yields stay vanilla. Replaces PlantEverything (without its picked models)
- Saplings: ancient (swamp tree) and autumn birch, plus Yggdrasil and ashwood that are off by default
- Cultivator removes berry bushes, mushrooms, flowers and debris, picking them first
- Growth markers of the White Hilt cultivator also show when picked berry bushes, mushrooms and flowers grow back

### Changed
- Group tools no longer select wild berry bushes, mushrooms, flowers or debris

## v0.13.0 - 2026-09-30

### Added
- White Hilt Sword, Bow, Buckler and Tower Shield: new models in the White Hilt style, with a white hilt, a white bow grip and white-painted shield boards, and new icons
- White Hilt Battleaxe, Mace, Sledge, Spear, Atgeir, Knife and Crossbow: new models in the same style, with white grips, a white atgeir shaft, a white knife grip and a white crossbow stock
- White Hilt Bow: the limbs bend and the string is pulled back to the drawing hand, and it snaps forward when the arrow is loosed
- White Hilt Staffs of Fire, Ice and Lightning: a dark scepter with a white grip wrap, with a red, blue or green flame on its head while held
- Exploration 50: map that others have shared through a map table is shown like your own, without the see-through layer

### Changed
- White Hilt Crossbow: now a real crossbow (based on the Arbalest) that shoots bolts and reloads, instead of a bow; the White Hilt Bolts can finally be used

## v0.12.0 - 2026-09-30

### Added
- White Hilt Ship: the sail force and the constant tailwind can be set in the config (`Ships` → `SailForce`, `AlwaysTailwind`); the defaults are unchanged
- Build tools: how many steps undo remembers and how fast terrain jobs run can be set in the config (`BuildTools` → `UndoSteps`, `BuildTools.Terrain` → `UndoSteps`, `EditsPerFrame`); the defaults are unchanged

### Changed
- White Hilt Ship: "Take me there" weighs the anchor, and lowering the anchor stops the route
- White Hilt Ship: "Push the ship" is not offered while the ship lies at anchor
- White Hilt Ship: the fishing net only raises the Fishing skill when it catches something

### Fixed
- White Hilt Ship: the sea chest drops its items when the ship is taken down, like the cargo hold

### Removed
- Unused code for indestructible ships and pieces
- An unused field on every Gift effect

## v0.11.0 - 2026-09-30

### Added
- Dog: the Bog Witch sells puppies in three colours. A puppy grows up over ten days while it has food, a Dog House and a Dog Bed under a roof. A grown dog follows you and fights for you
- Dog House, Dog Bed, Dog Bowl, Dog Water Bowl and Dog's Grave, known once you buy your first puppy
- Dog's Gravestone at the stonecutter: the dog's name and age are chiselled into its grave
- Naming the puppy, sitting, portal travel, bond levels, guard dog, sniffing for forage, cuddling, collars, Bone Broth, the Dog Whistle and map pins for the dog and its house
- The dog sleeps in its bed at night, beside your bed when you sleep near its home, and shelters in its house in the rain, in a lying pose made for the wolf skeleton
- The dog drinks from its water bowl, digs up small finds near home, greys around the muzzle with age and in the end dies of old age, leaving a puppy behind
- Litters, tricks taught with Dog Treats, mood, shaking off water, freezing without a Dog Coat, limping and swamp poison with the Dog Bandage, yawning, scratching, stretching and belly rubs
- Good Memories at the dog's grave, and its ghost at dusk
- A neglected dog runs away, a starving one dies, and a dead dog leaves remains for its grave
- Console command `whitehilt_rest` to check the resting pose
- Navigator's Table route: at Exploration 30, Use on the table opens the map to set up to 5 route markers. They belong to the ship, so everyone aboard sees them on the map, and an arrow on the minimap points to the next one with the distance. A marker is ticked off when the ship comes within 60 m
- "Take me there" at Exploration 50: the ship plots a course through deep water around land and sails it on its own, under sail only, slowing for turns and near the end. It steers around rocks and shallows ahead (also rocks under water) and stops when someone takes the helm, everyone leaves, no way around is found or it arrives. While it sails, everyone aboard sees speed, heading, wind and the time left to arrival under the wind indicator. Markers on land are moved to the nearest deep water. Docks and other building in the water are not known to the route (`RouteMarkersLevel`, `RouteSailLevel`)

### Changed
- White Hilt Ship: the deck brazier stands on the port side just in front of the cargo crates, out of the walkway
- White Hilt Ship: the drift anchor hangs closer to the hull and follows its slope
- Dog: stands closer to the water bowl when drinking
- Backpack: three coin slots next to the shield and ammo
- Backpack: a potion slot shows what the potion restores (or its regeneration bonus or effect) instead of 0, and the total counts only food
- Dog: a playful puppy's hop is a smooth arc of the body instead of the wolf's jump
- Portal travel map: the list reaches down to the buttons, and "Take me home" between Travel and Close goes straight to your home portal

## v0.10.0 - 2026-09-30

### Added
- Dynamic difficulty: players online, days, biomes visited and White Hilt gear worn add up to a pressure. The pressure raises star chances and gives creatures more health and damage
- Creatures with up to 5 stars. How many stars can appear depends on the biomes visited and has a limit per biome. Stars above 2 add less strength and loot than vanilla stars, and the creatures grow
- Beasts of the dark hour: black 5 star creatures that come between 00:00 and 02:00 in bad weather. There is one per biome and two at sea, and each drops its own trophy
- Blood moon: a rare red night when beasts come all night and stars are more common
- Console commands `whitehilt_difficulty`, `whitehilt_bloodmoon` and `whitehilt_beast`

## v0.9.0 - 2026-09-30

### Added
- Repair mode for the hammer (Ctrl + R or the toolbar): repairs every damaged piece in an adjustable circle (mouse wheel, up to 30 m) with one click, shows the area and makes damaged pieces glow (`RepairArea`, `RepairMaxRadius`, `RepairAreaCost`)

## v0.8.0 - 2026-09-29

### Added
- Backpack: one more inventory row (8 × 5), on top of rows bought from a trader (`ExtraRows`)
- Travel bar and build bar: two hotbars, switched with 9 or automatically when a build tool is taken out and put away
- Equipment slots for helmet, chest, legs, cape and trinket in a panel next to the inventory; worn items no longer take room in the grid
- Three food slots and two potion slots, used with Left Alt + 1 to 5
- Five accessory slots: belts, Wisplight, Wishbone and the like are worn at once, each kind once; the extra slots also hold the Home Stone and the Pathfinder's Amulet
- Megingjord upgrades at the forge up to quality 4, +50 carry weight per level
- Home Stone return trip: used again within 2 minutes of going home, it takes you back to where you were
- Belt Pouch: used once, it gives the character one more inventory row for good
- Deck Brazier ship upgrade: a fire under the tent that burns without fuel, warms the crew and counts for resting
- Sea Chest ship upgrade: a 4 × 2 chest by the helm besides the cargo hold
- Ship Portal ship upgrade: a rune circle on deck that makes the ship a White Hilt portal destination, following it wherever it sails
- Clock: the time of day in 24 hours at the top of the screen, with day number, a weather icon and a warning before night falls
- Shield and ammo slots under the accessories: the shield comes and goes with one-handed weapons, and the ammo there is used first
- Indoor sound: wind, rain, sea and thunder are quieter and muffled under a roof, by how well you are covered
- Floating items: everything dropped floats in water, e.g. a Serpent's trophy at sea (`FloatingItems`)
- Waste Well: throw rubbish in and it is gone once closed; it can also collect items lying on the ground nearby
- The Mast Wisp also thins ordinary fog (misty weather) for those near the ship, not only the Mistlands mist
- Repair Anvil: repairs everything you wear at once, free by default (`RepairAnvil`)
- Hold course (H at the helm): every ship keeps its heading when you let go of the helm, and stops before shallow water
- Speed in knots, heading and wind direction under the wind indicator while steering
- Push a stranded ship off the shore with E
- The drift anchor drops by itself when the last person leaves the ship, and is weighed when someone takes the helm

### Changed
- The Navigator's Table and the Deck Brazier block players like the deck crates; the table has its own hover text, and Shift + Use on it takes it back
- The White Hilt Ship's tent can be stood on (with a walkway along the steep ridge) and still walked under
- Portal and ship markers on the map are smaller, grow and shrink with the zoom, and the player and ship markers are always drawn above them
- The Fishing Net's catches follow the Fishing skill (up to twice as often, often two fish, and it raises the skill), bring up seaweed and now and then an amber pearl, and its interval can be set
- The Pathfinder's Amulet costs a Silver Necklace instead of two Amber
- The Navigator's Table can also be set up at the White Hilt Ship's mast, where the other upgrades go, and is listed there
- The Ship Lantern shines 2.5 times as bright and reaches twice as far (configurable)

## v0.7.0 - 2026-09-28

### Added
- Cranberry Mead (poison resistance), Sweet Gale Ale (+75 carry weight) and Crowberry Wine (faster health regeneration), brewed from White Hilt forageables
- Smoke Oven: a clay oven, a second Stone Pot extension that raises the pot to level 3
- Smoked Fish and Smoked Wolf Jerky, level 3 dishes that also give a small buff
- Fishing Net ship upgrade: catches fish of the local waters into the cargo hold while the ship sails
- Drift Anchor ship upgrade: lowered at the mast, it holds the ship where it is
- Defences: Palisade Rampart with walkway, Rampart Corner, Rampart Bend (45°), Rampart Stairs, a Gatehouse with a gate that opens, small, medium and large Watchtowers with roofs and ladders, and a Cheval de Frise. All walkways join, so you can walk round the whole fort
- Exploration skill, raised by uncovering new map
- Cartographer's Desk, a map table extension for making navigation gear
- Navigator's Table: set up on a ship's deck, it widens what the crew uncovers on the map, up to 300 m
- Pathfinder's Amulet: a trinket that widens what you uncover, up to 200 m, and fills its adrenaline from new land for a 500 m Raven Sight
- White Hilt Portal and White Hilt Rune Circle: one portal network with a travel map (searchable, sortable list next to the map), private portals and a home portal. Portal Stations stations carry on as rune circles
- Home Stone: takes you to your home portal, then rests for 5 minutes (configurable)
- Surt's Brazier: a workbench extension; while one stands, fires, torches, ovens and hot tubs burn without fuel and stay lit in rain (configurable, smelters optional)
- Crafting from chests: crafting, building, fuel, ore and cooking use what lies in nearby chests, carts and ship holds, with amber counts and chests that open when taken from
- Build camera and build toolbar for every build tool: a free camera within building reach (also below ground) with fly-to, orbit and speed keys, rotation step, tilt and roll (also on the mouse wheel), quick 45°/90° angles, flip, snap on/off, copy with rotation, nudge, grid, stamp, undo/redo, axes on the piece, height and distance read-outs, tooltips and a foldable toolbar, all with hotkeys
- Media mode in the build camera: photos without HUD, photo view with zoom, hidden character, local time of day and weather, and camera-path films with title card and fades for recording
- Group building: select pieces (one by one or in a box), copy, move, tear down and undo them as one; blueprints (PlanBuild .blueprint and BuildShare .vbuild are read too) with cost list; line and area tool for rows of walls and fences and fields of floor
- White Hilt hoe tools: roads planned on the map and built as you walk, levelling areas to a height, ramps with an even or locked slope, painting and resetting areas, a height meter and a big brush, all undoable. White Hilt cultivator tools: grid planting with auto-cultivating, refilling, cultivating and harvesting areas, and growth labels
- Animal husbandry: Feeding Trough that hungry animals eat from, favourite foods, the Animal Husbandry skill, Tether Post, Grooming Comb, and produce that content animals leave in the trough

### Changed
- The Smoke Oven's fire and smoke use a shared helper, also used by Surt's Brazier

## v0.6.0 - 2026-09-28

### Added
- Portal runes: a Rune Forge (built next to a forge), six iron runes (bronze, iron, silver, black metal, flametal, gold) and a Rune Post. Runes on a post near the portal you travel from let it carry their metals; all six on one post let it carry everything
- Portal runes also work on the stations of the Portal Stations mod
- Valkyrie Stone: for one Surtling Core it carries you to where you last fell, once per death
- Portal Astrolabe: a map table extension that shows every portal (also mod portals and Portal Stations) on everyone's map, with its runes on hover
- Harbour Anchor: a map table extension that shows every ship on everyone's map, following them as they sail
- White Hilt Ship upgrades, used on the mast: Ship Lantern (light at night), Cargo Barrels (hold 8 × 4), Ship Tent (shelter and dry) and Mast Wisp (clears the mist)

### Changed
- The White Hilt Ship has its own look: dragon figurehead, white and gold sail with a white-hilted sword, whitewashed hull with gold fittings and shields along the rail

## v0.5.0 - 2026-09-28

### Added
- Chain Bench: a forge extension with a smith's vise and hanging chains. While it stands next to the forge, the forge makes Chains from Iron ×2 and Coal ×1

## v0.4.0 - 2026-09-28

### Added
- Cranberries and Sweet Gale in the Swamp. In existing worlds, red Mushrooms in the Swamp sometimes give Cranberries and Thistles there sometimes give Sweet Gale
- Sweet Gale Sausages and Cranberry Soup, Swamp dishes between the Black Forest and Mountain food
- Herb Tray: a Stone Pot extension with a mortar and herbs that raises the pot to level 2
- Lingonberry Mead (frost resistance) and Roseroot Mead (faster stamina regeneration), brewed in the Cauldron and fermented like vanilla meads

### Changed
- The Stone Pot has its own model: a lidded stone pot hanging from the tripod, instead of a shrunken vanilla cauldron
- Mountain Stew and Roseroot Broth now need Stone Pot level 2, so a Herb Tray next to the pot

## v0.3.0 - 2026-09-27

### Added
- Porcini and Lingonberries in the Black Forest, Crowberries and Roseroot in the Mountains
- Porcini Stew, Lingonberry Soup, Mountain Stew and Roseroot Broth, cooked in the Stone Pot
- Wolves sometimes drop Crowberries, and Blueberry bushes sometimes give Lingonberries
- Config for every forageable (spawning, spawn density, extra-drop chance) and every Stone Pot dish (health, stamina, duration, regen), admin-only and synced from the server
- Norwegian translations for the whole mod: items, the Stone Pot, the ship, potions and their messages, and the tier unlock messages

### Changed
- Extra drops from vanilla plants now depend on the biome: a Mushroom gives a Chanterelle in the Meadows and a Porcini in the Black Forest
- If a custom model fails to load, the item keeps the vanilla look instead of disappearing

### Fixed
- White Hilt items are now registered at the main menu instead of on the first world load, so equipped White Hilt gear no longer disappears from the character preview

## v0.2.0 - 2026-09-27

### Added
- Chanterelle and Wild Garlic, pickable in the Meadows forests, and as a 30% extra drop from vanilla Mushroom and Dandelion
- Stone Pot: a cooking station built from Stone, Flint and Wood
- Chanterelle Stew (30 health, 22 stamina) and Wild Garlic Soup (18 health, 32 stamina), cooked in the Stone Pot

## v0.1.0 - 2026-09-26

### Added
- Config file with `Full` and `Linear` progression modes, synced from the server (admin-only)
- Linear mode: White Hilt gear unlocks biome by biome when you first obtain Bronze, Iron, Silver, Black Metal, Eitr or Flametal, and recipes cost some of that material
- Per-item tier overrides, including `Never` to disable a recipe
- Unlock message when a new tier opens, listing the White Hilt items that became available
- Gift of Fenrir: faster attacks and real life steal (replaces the constant regeneration)
- Gift of Thor: double chopping and mining damage
- Gift of Mimir: marks nearby creatures on the minimap
- Gift of Odin: the 500 max HP now actually holds while the effect is active

### Fixed
- Gift of Surt now requires the obtainable Ashlands Flametal instead of the legacy Ancient Flametal
- Potion requirements in the README now match the actual recipes
- Gift of Odin increased fall damage instead of reducing it; it now halves fall damage
- Gift of Skadi never removed Freezing/Cold (wrong status effect hash)
- Gift of Munin showed one popup per material; materials are now learned silently
- Healing potions no longer spam floating heal numbers every frame
- Potion tooltips and README effects now describe what the potions actually do

## v0.0.5 - 2026-09-26

### Changed
- Compiled against Valheim 1.0.x (Unity 6000.0) and Jotunn 2.30.2 (was 2.27.1)
- Thunderstore manifest now depends on Jotunn 2.30.2 and BepInExPack 5.4.2350

### Fixed
- Removed a stale Unity 2022 editor reference from the project file

## v0.0.4 - 2026-01-03

### Changed
- **BREAKING**: All weapon crafting requirements now use Swamp-tier materials or earlier (Iron, Ancient Bark, Guck, etc.)
- **BREAKING**: Armor (Chestplate, Greaves, Shield) now use Swamp-tier materials and CopyFrom references
- **BREAKING**: Ammunition (Arrows, Bolts) now use Swamp-tier materials and CopyFrom references
- Removed dependencies on Plains/Mistlands/Ashlands materials (Black Metal, Linen Thread, Eitr, Yggdrasil Wood, Flametal, etc.)
- Staves now craftable at Forge instead of Galdr Table
- Updated CopyFrom references to use Swamp-tier base items
- Fixed Battleaxe CopyFrom to use correct prefab name (Battleaxe instead of BattleaxeIron)
- Enabled all weapons and armor pieces

### Fixed
- Game balance: All weapons are now obtainable before reaching Mountains biome

## v0.0.3

- Added 14 new potions: Gift of Sleipnir, Ratatoskr, Njord, Surt, Skadi, Baldur, Thor, Idunn, Brokkr, Tyr, Fenrir, Freyr, Hel, and Mimir.
- Added 12 new weapons: Battleaxe, Spear, Mace, Atgeir, Knife, Sledge, Crossbow, Staff of Fire, Staff of Ice, Staff of Lightning, Tower Shield, and Buckler.
- Added 2 new armor pieces: Chestplate and Greaves.
- Added Megingjord accessory with +450 carry weight.
- Added ammunition: White Hilt Arrows and Bolts (200 per craft, enhanced damage).
- Fixed EpicLoot compatibility issue with potions (changed base class to SE_Stats).
- Refactored WearNTear configuration to shared helper class.
- Added pickaxe damage immunity to indestructible items.
- Various code quality improvements.

## v0.0.2

- Added potions Gift of Hugin and Munin.

## v0.0.1

- Initial release.
