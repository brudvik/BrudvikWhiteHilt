# Changelog

All notable changes to BrudvikWhiteHilt. Newest version first.

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
