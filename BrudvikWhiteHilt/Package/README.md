<!-- Generated from README.MD by build_thunderstore_readme.ps1 when the package is built. Edit README.MD instead. -->

![White Hilt](https://raw.githubusercontent.com/brudvik/BrudvikWhiteHilt/master/BrudvikWhiteHilt/Package/icon.png)

# Brudvik White Hilt

**Viking life, deepened.**
Ships and sea routes, stonework and turf roofs, foraging and feasts, forts, beasts and a hundred small comforts for Valheim.

[![Latest release](https://img.shields.io/github/v/release/brudvik/BrudvikWhiteHilt?style=flat-square&color=d4a72c&label=release)](https://github.com/brudvik/BrudvikWhiteHilt/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/brudvik/BrudvikWhiteHilt/total?style=flat-square&color=4a6b7c)](https://github.com/brudvik/BrudvikWhiteHilt/releases)
![Valheim mod](https://img.shields.io/badge/Valheim-mod-3a5568?style=flat-square)
![BepInEx 5.4](https://img.shields.io/badge/BepInEx-5.4-6e4a2e?style=flat-square)
![Jötunn 2.30](https://img.shields.io/badge/J%C3%B6tunn-2.30-6e4a2e?style=flat-square)
![Everyone needs the mod in multiplayer](https://img.shields.io/badge/multiplayer-everyone%20needs%20it-555?style=flat-square)
[![License](https://img.shields.io/github/license/brudvik/BrudvikWhiteHilt?style=flat-square&color=555)](https://github.com/brudvik/BrudvikWhiteHilt/blob/master/LICENSE)

[Install](#-installation) ·
[Features](#-features) ·
[Documentation](https://github.com/brudvik/BrudvikWhiteHilt/blob/master/docs/README.md) ·
[For modders](#-for-modders) ·
[Changelog](https://github.com/brudvik/BrudvikWhiteHilt/blob/master/CHANGELOG.md) ·
[Releases](https://github.com/brudvik/BrudvikWhiteHilt/releases) ·
[Report an issue](https://github.com/brudvik/BrudvikWhiteHilt/issues)

![The palisade fort, the White Hilt uniform, Skidbladnir, the Shipwright's Bench and the White Hilt Portal](https://raw.githubusercontent.com/brudvik/BrudvikWhiteHilt/master/docs/images/readme/banner.png)

White Hilt grew out of many playthroughs where the best part of Valheim was building a home and defending it. It adds what a Viking farm, a ship and a long winter would need, in the game's own look, and it explains itself: every piece has a page with its recipes and settings, and everything can be switched off or tuned in an in-game settings window.

It is also open source and written to be read. The code explains what it does and why, and a [guide to how the mod is built](https://github.com/brudvik/BrudvikWhiteHilt/blob/master/docs/architecture.md) shows the patterns behind it, so modders can learn from it and borrow freely.

Everything is named after **Dyrnwyn**, the white-hilted sword of Welsh legend that blazed with fire when drawn by one who was worthy.

## ✨ New in 0.97.0

- **[Decor Hammer](https://github.com/brudvik/BrudvikWhiteHilt/blob/master/docs/decor.md)**: a hammer of its own with 174 decorations under eight tabs. Bushes, ferns, flowers and young trees that sway in the wind and bend when you walk through them; stones, stumps and logs; barrels, baskets, bowls and food; tools, fences and firewood; tables, stools and shelves; hanging cloth and banners; candles, lanterns and fires without fuel; runestones, graves and wrecked ships. Each shows up once you know its materials.

### Also new in 0.95.0

- **[Necromancer's Staff](https://github.com/brudvik/BrudvikWhiteHilt/blob/master/docs/equipment.md#waking-the-fallen)**: a skull burning green on the White Hilt scepter. It raises skeletons, and at a fallen friend's grave it wakes them there with their gear and lost skills, at a heavy price in your own life.
- **[Find an item's chest](https://github.com/brudvik/BrudvikWhiteHilt/blob/master/docs/chests.md)**: point at an item in your inventory and the chests it belongs in light up nearby; its tooltip names the chest.
- **Wall drawers and ship workshops** in Valheim's own wood and iron, and Skidbladnir's lower room stays dry in waves.

See the [changelog](https://github.com/brudvik/BrudvikWhiteHilt/blob/master/CHANGELOG.md) for everything else.

## 📥 Installation

**With a mod manager** (r2modman or Thunderstore Mod Manager): install *BrudvikWhiteHilt*; BepInEx and Jötunn come along.

**By hand:**

1. Install [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/) and [Jötunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/).
2. Download `BrudvikWhiteHilt.dll.zip` from the [latest release](https://github.com/brudvik/BrudvikWhiteHilt/releases/latest).
3. Copy `plugins/BrudvikWhiteHilt.dll` into `BepInEx/plugins` in your Valheim folder, and start the game.

On a server, the server and every player need the mod. The server's settings are synced to everyone.

## 🧭 Features

Every feature has its own page in the [documentation](https://github.com/brudvik/BrudvikWhiteHilt/blob/master/docs/README.md) with all items, recipes and settings.

### 🏠 Home & building

#### [Around the base](https://github.com/brudvik/BrudvikWhiteHilt/blob/master/docs/base.md)

<img src="https://raw.githubusercontent.com/brudvik/BrudvikWhiteHilt/master/docs/images/waste_well.png" alt="Around the base" height="120">

Fires without fuel, crafting from nearby chests, a quartermaster's table, a guestbook, a waste well and self-closing doors.

#### [Defences](https://github.com/brudvik/BrudvikWhiteHilt/blob/master/docs/defences.md)

<img src="https://raw.githubusercontent.com/brudvik/BrudvikWhiteHilt/master/docs/images/fort_overview.png" alt="Defences" height="120">

Palisade and stone forts with ramparts, gatehouses and towers, moats and drawbridges.

#### [Stonework](https://github.com/brudvik/BrudvikWhiteHilt/blob/master/docs/stonework.md)

<img src="https://raw.githubusercontent.com/brudvik/BrudvikWhiteHilt/master/docs/images/memorial_stone.png" alt="Stonework" height="120">

Memorial stones, soapstone lamps, hnefatafl, ship settings, slate floors and dry stone walls.

#### [Roofs](https://github.com/brudvik/BrudvikWhiteHilt/blob/master/docs/roofs.md)

<img src="https://raw.githubusercontent.com/brudvik/BrudvikWhiteHilt/master/docs/images/roof_turf.png" alt="Roofs" height="120">

Turf, reed, straw, tarred shingles and slate in every shape, with smoke holes and dragon gables.

#### [Painting](https://github.com/brudvik/BrudvikWhiteHilt/blob/master/docs/painting.md)

<img src="https://raw.githubusercontent.com/brudvik/BrudvikWhiteHilt/master/docs/images/paint_bench.png" alt="Painting" height="120">

Paint or stain any piece in a colour mixed from dyes, and dye banners, sails and capes.

#### [Beams, poles & banners](https://github.com/brudvik/BrudvikWhiteHilt/blob/master/docs/building-pieces.md)

<img src="https://raw.githubusercontent.com/brudvik/BrudvikWhiteHilt/master/docs/images/readme/tile_banners.png" alt="Beams, poles & banners" height="120">

Longer and angled beams and poles, iron grates in more sizes and White Hilt banners.

#### [Build camera & toolbar](https://github.com/brudvik/BrudvikWhiteHilt/blob/master/docs/build-tools.md)

<img src="https://raw.githubusercontent.com/brudvik/BrudvikWhiteHilt/master/docs/images/readme/tile_build_tools.png" alt="Build camera & toolbar" height="120">

A free build camera, precise rotation, undo, area repair, blueprints and terrain tools.

#### [Decor Hammer](https://github.com/brudvik/BrudvikWhiteHilt/blob/master/docs/decor.md)

<img src="https://raw.githubusercontent.com/brudvik/BrudvikWhiteHilt/master/docs/images/decor_hearth.png" alt="Decor Hammer" height="120">

174 decorations: plants that sway in the wind, stones, kitchen and workshop things, furniture, cloth, lights and Norse pieces.

### ⛵ Sea & travel

#### [Ships](https://github.com/brudvik/BrudvikWhiteHilt/blob/master/docs/ships.md)

<img src="https://raw.githubusercontent.com/brudvik/BrudvikWhiteHilt/master/docs/images/skidbladnir.png" alt="Ships" height="120">

The White Hilt Ship and Skidbladnir, ship upgrades, sailing help, mooring and the Shipwright's Bench.

#### [Navigation](https://github.com/brudvik/BrudvikWhiteHilt/blob/master/docs/navigation.md)

<img src="https://raw.githubusercontent.com/brudvik/BrudvikWhiteHilt/master/docs/images/navigators_table.png" alt="Navigation" height="120">

Route sailing, weather forecasts, pathfinder amulets, a compass and discoveries on the map.

#### [Portals & travel](https://github.com/brudvik/BrudvikWhiteHilt/blob/master/docs/portals.md)

<img src="https://raw.githubusercontent.com/brudvik/BrudvikWhiteHilt/master/docs/images/white_hilt_portal.png" alt="Portals & travel" height="120">

Runes that let portals carry metal, a portal network with a travel map, and the Home and Valkyrie Stones.

#### [Treasure maps](https://github.com/brudvik/BrudvikWhiteHilt/blob/master/docs/treasure.md)

<img src="https://raw.githubusercontent.com/brudvik/BrudvikWhiteHilt/master/docs/images/treasure_chest.png" alt="Treasure maps" height="120">

Buy a map from Hildir, match its scrap of land to your own map and dig up the chest.

### 🍄 Farm & food

#### [Foraging & food](https://github.com/brudvik/BrudvikWhiteHilt/blob/master/docs/foraging.md)

<img src="https://raw.githubusercontent.com/brudvik/BrudvikWhiteHilt/master/docs/images/stone_pot.png" alt="Foraging & food" height="120">

Wild herbs, berries and lichens, the Stone Pot and its extensions, meads, ales and cured food.

#### [Planting](https://github.com/brudvik/BrudvikWhiteHilt/blob/master/docs/planting.md)

<img src="https://raw.githubusercontent.com/brudvik/BrudvikWhiteHilt/master/docs/images/readme/tile_planting.png" alt="Planting" height="120">

Plant berry bushes, mushrooms, flowers, debris and saplings with the cultivator.

#### [Animal husbandry](https://github.com/brudvik/BrudvikWhiteHilt/blob/master/docs/husbandry.md)

<img src="https://raw.githubusercontent.com/brudvik/BrudvikWhiteHilt/master/docs/images/feeding_trough.png" alt="Animal husbandry" height="120">

The Feeding Trough, favourite foods, the Tether Post, the Grooming Comb and a husbandry skill.

#### [Fishing nets](https://github.com/brudvik/BrudvikWhiteHilt/blob/master/docs/fishing.md)

<img src="https://raw.githubusercontent.com/brudvik/BrudvikWhiteHilt/master/docs/images/net_winch.png" alt="Fishing nets" height="120">

A net winch with a fish barrel on the shore and shore nets that fill it with fish.

#### [Dog](https://github.com/brudvik/BrudvikWhiteHilt/blob/master/docs/dog.md)

<img src="https://raw.githubusercontent.com/brudvik/BrudvikWhiteHilt/master/docs/images/dog_house.png" alt="Dog" height="120">

Raise a puppy from the Bog Witch into a companion that follows, fights, learns tricks and grows old.

### ⚔️ Gear & crafting

#### [White Hilt gear](https://github.com/brudvik/BrudvikWhiteHilt/blob/master/docs/equipment.md)

<img src="https://raw.githubusercontent.com/brudvik/BrudvikWhiteHilt/master/docs/images/white_hilt_sword.png" alt="White Hilt gear" height="120">

Indestructible weapons, shields, armour and tools, upgraded biome by biome.

#### [Potions](https://github.com/brudvik/BrudvikWhiteHilt/blob/master/docs/potions.md)

<img src="https://raw.githubusercontent.com/brudvik/BrudvikWhiteHilt/master/docs/images/readme/potions.png" alt="Potions" height="120">

Eighteen meads named after the Norse gods, from endless stamina to permanent skills.

#### [Smithing](https://github.com/brudvik/BrudvikWhiteHilt/blob/master/docs/smithing.md)

<img src="https://raw.githubusercontent.com/brudvik/BrudvikWhiteHilt/master/docs/images/repair_anvil.png" alt="Smithing" height="120">

The Repair Anvil, the Chain Bench, rune etching, whetstones and weapon oils.

#### [Restocking chests](https://github.com/brudvik/BrudvikWhiteHilt/blob/master/docs/chests.md)

<img src="https://raw.githubusercontent.com/brudvik/BrudvikWhiteHilt/master/docs/images/collection_post.png" alt="Restocking chests" height="120">

Fourteen chests that keep themselves stocked, wall drawers and a Collection Post that sorts.

### 🐉 Beasts & challenge

#### [Lindorm, spiders & dragons](https://github.com/brudvik/BrudvikWhiteHilt/blob/master/docs/monsters.md)

<img src="https://raw.githubusercontent.com/brudvik/BrudvikWhiteHilt/master/docs/images/giantspider.png" alt="Lindorm, spiders & dragons" height="120">

A great worm in the forest at night, giant spiders round their nests and fire-breathing dragons.

#### [Kraken & octopus](https://github.com/brudvik/BrudvikWhiteHilt/blob/master/docs/kraken.md)

<img src="https://raw.githubusercontent.com/brudvik/BrudvikWhiteHilt/master/docs/images/kraken.png" alt="Kraken & octopus" height="120">

Octopuses in the deep, and a brutal Kraken that grips and lifts ships.

#### [Difficulty & blood moon](https://github.com/brudvik/BrudvikWhiteHilt/blob/master/docs/difficulty.md)

<img src="https://raw.githubusercontent.com/brudvik/BrudvikWhiteHilt/master/docs/images/readme/tile_difficulty.png" alt="Difficulty & blood moon" height="120">

A world that grows harder as you progress: up to 5 stars, black beasts and the rare blood moon.

#### [Black Bestiary](https://github.com/brudvik/BrudvikWhiteHilt/blob/master/docs/bestiary.md)

<img src="https://raw.githubusercontent.com/brudvik/BrudvikWhiteHilt/master/docs/images/readme/tile_bestiary.png" alt="Black Bestiary" height="120">

A field guide to the nine black beasts, with the arrows and weapon treatments that exploit their weaknesses.

### 🎒 Quality of life

#### [Backpack](https://github.com/brudvik/BrudvikWhiteHilt/blob/master/docs/backpack.md)

<img src="https://raw.githubusercontent.com/brudvik/BrudvikWhiteHilt/master/docs/images/readme/tile_backpack.png" alt="Backpack" height="120">

An extra row, two hotbars, and slots for equipment, food, potions, ammo and coins.

#### [Production timers](https://github.com/brudvik/BrudvikWhiteHilt/blob/master/docs/production.md)

<img src="https://raw.githubusercontent.com/brudvik/BrudvikWhiteHilt/master/docs/images/readme/tile_production.png" alt="Production timers" height="120">

Time left, fuel and why it stopped on every smelter, kiln, fermenter, oven and hive.

#### [Clock, sound & floating items](https://github.com/brudvik/BrudvikWhiteHilt/blob/master/docs/quality-of-life.md)

<img src="https://raw.githubusercontent.com/brudvik/BrudvikWhiteHilt/master/docs/images/readme/tile_clock.png" alt="Clock, sound & floating items" height="120">

A clock with the weather, muffled weather indoors and dropped items that float.

#### [Skills & milestones](https://github.com/brudvik/BrudvikWhiteHilt/blob/master/docs/skills.md)

<img src="https://raw.githubusercontent.com/brudvik/BrudvikWhiteHilt/master/docs/images/readme/tile_skills.png" alt="Skills & milestones" height="120">

Milestones from level 25 to 100, a new Foraging skill, starred food and crops and the Compost Bin.

#### [Saga](https://github.com/brudvik/BrudvikWhiteHilt/blob/master/docs/saga.md)

<img src="https://raw.githubusercontent.com/brudvik/BrudvikWhiteHilt/master/docs/images/readme/tile_saga.png" alt="Saga" height="120">

Each character's saga of its deeds, with renown that gives carry weight and stamina.

#### [Settings & progression](https://github.com/brudvik/BrudvikWhiteHilt/blob/master/docs/progression.md)

<img src="https://raw.githubusercontent.com/brudvik/BrudvikWhiteHilt/master/docs/images/readme/tile_settings.png" alt="Settings & progression" height="120">

An in-game settings window (F7), linear or full progression, and per-item switches.

## 📚 Documentation

The [documentation](https://github.com/brudvik/BrudvikWhiteHilt/blob/master/docs/README.md) has a page for every feature: what it does, how to build or craft it, and every setting.

## 🧱 For modders

White Hilt is a large, working Valheim mod under the [MIT No Attribution](https://github.com/brudvik/BrudvikWhiteHilt/blob/master/LICENSE) licence, kept readable on purpose. If you want to learn how a mod like this is put together, you are welcome to read it, copy from it and build on it.

- **[How the mod is built](https://github.com/brudvik/BrudvikWhiteHilt/blob/master/docs/architecture.md)** walks through the source: start-up, cloning vanilla items and pieces, assets and data-driven building pieces, multiplayer (ZDOs, owners, RPCs, server checks), Harmony patches, settings, translations, performance and tests. It ends with which file to read to learn what.
- **Every longer method** has a comment saying what it does and why, and every public type and member is documented, so the reasons behind the code are next to it.
- **Tests** in `BrudvikWhiteHilt.Tests` show how to test the parts of a mod that run without the game.

Questions about the code are welcome on the [issues page](https://github.com/brudvik/BrudvikWhiteHilt/issues). How to build it yourself is under *Building from source* below.

## 🐞 Known issues

No known issues at the moment. Please report anything odd on the [issues page](https://github.com/brudvik/BrudvikWhiteHilt/issues).

## 🛠️ Building from source

The mod does not compile out of the box; read up on [Valheim mod development](https://github.com/Valheim-Modding/JotunnModStub) first. Jötunn's build files find Valheim through Steam and BepInEx in Valheim's folder; if yours is elsewhere, set `BEPINEX_PATH` in a git-ignored `Environment.props` next to the solution. [How the mod is built](https://github.com/brudvik/BrudvikWhiteHilt/blob/master/docs/architecture.md) explains how the source is put together.

The `Assets` folder is not part of the public source. The potion icons are bought from [Graphicriver](https://graphicriver.net/item/rpg-potion-icons/24972053), and the licence only allows shipping them in the pre-built mod. The chest icons (`Assets/strg_*.png`) come from [Fantasy Strategy Skills](https://graphicriver.net/item/fantasy-strategy-skills/35481040) under the same terms; without them the chests use the vanilla chest icon. To make a similar mod you would need your own licence.

The 3D models are built into `BrudvikWhiteHilt/Assets/whitehilt_foraging` by `AssetSource/build_foraging_bundle.ps1`. It needs Python and Unity 6000.0.75f1 (the same version as Valheim), and creates the git-ignored Unity project `BrudvikWhiteHiltUnity` on first run. A `<model>.crop.json` next to a `.glb` in `AssetSource/Models` keeps only part of a model, for files that hold several objects in one mesh. Short `.wav` sounds in `AssetSource/Sounds` are added to the same bundle. Animated creatures (`AssetSource/Creatures/<name>.glb` with a `<name>.creature.json`) also need Blender: it turns each rigged model into an FBX, and Unity builds a prefab with an animator for it. `AssetSource/Preview/render_creatures.ps1` renders every animation of them without starting the game.

The defences, the navigation pieces and the stonework are laid out by `AssetSource/Preview/build_defenses.py`, which writes `defenses.json`; the mod embeds that file and builds the pieces from Valheim's own meshes and the bundle's models at start-up. `AssetSource/Preview/render_preview.ps1` renders preview images of the same layout without starting the game (it extracts the vanilla meshes into the git-ignored Unity project; they are never committed).

`BrudvikWhiteHilt.Tests` holds tests for what runs without the game: reading and writing blueprints, pack lists and other saved data, what is sent over the network, pure logic such as compass bearings and map geometry, and checks that every English text has a Norwegian one and every piece is in `defenses.json`. They load the built mod, so build it first (the solution does), then run `dotnet test BrudvikWhiteHilt.Tests`.

The Thunderstore package gets a plain markdown version of this README (`BrudvikWhiteHilt/Package/README.md`), written by `build_thunderstore_readme.ps1` on every Release build; edit this file, not that one.

## 🙏 Credits

- Skidbladnir model: ["Sailing Ship"](https://sketchfab.com/3d-models/sailing-ship-258e45faec8e406a826960a6d0277d0a) by [3ddans](https://sketchfab.com/3ddans), licensed under [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/). Central deck opened and rebuilt, lower floor, stairs and mast lookout added, sails separated, converted to metre-scale OBJ and made double-sided where needed for Valheim.
- Chanterelle model: ["Chanterelle"](https://sketchfab.com/3d-models/chanterelle-136f5f6bac124b8bb7738945f12243b5) by [Zacxophone](https://sketchfab.com/Zacxophone), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Converted to OBJ, rescaled and made double-sided for Valheim.
- Porcini model: ["Boletus Mushroom"](https://sketchfab.com/3d-models/boletus-mushroom-1b9dde383cb84944b3ffc17d4cb29ebc) by [Jonny Crabb](https://sketchfab.com/JonnyCrabb), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Merged into one mesh, converted to OBJ, rescaled and made double-sided for Valheim.
- Stone Pot model: ["Stone Bowl"](https://sketchfab.com/3d-models/stone-bowl-1469a70e3ea54ff1ab1d442749d94b48) by [rickmaolly](https://sketchfab.com/rickmaolly), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Converted to OBJ, rescaled and made double-sided for Valheim.
- Herb Tray model: ["Health Pack: Mortar and Pestle Herb Crafting Set"](https://sketchfab.com/3d-models/health-pack-mortar-and-pestle-herb-crafting-set-98c25d51e2d84fc48153adf1df7ed520) by [Michael Neocleous](https://sketchfab.com/mikegneo), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Merged into one mesh, converted to OBJ, rescaled and made double-sided for Valheim.
- Chain Bench vise: ["Medieval_Blacksmith_Vise"](https://sketchfab.com/3d-models/medieval-blacksmith-vise-27f9f65e020f4f88b65b42a510f5a433) by [GetDeadEntertainment](https://sketchfab.com/GetDeadEntertainment), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Merged into one mesh with a combined texture, converted to OBJ, rescaled and made double-sided for Valheim.
- Chain Bench chains: ["Hanging wall chains"](https://sketchfab.com/3d-models/hanging-wall-chains-97bc5d1699c94b619d0666f6eab79ea0) by [Karyu](https://sketchfab.com/karyu.lucca), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Converted to OBJ, rescaled and made double-sided for Valheim.
- Rune Forge model: ["Medieval Workbench"](https://sketchfab.com/3d-models/medieval-workbench-b9e0b742add340f28f3194d1dc022d26) by [Catsnap0006](https://sketchfab.com/Catsnap0006), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Merged into one mesh, converted to OBJ, rescaled and made double-sided for Valheim.
- Rune model: ["Rune Ring"](https://sketchfab.com/3d-models/rune-ring-266ccf4f356c45a89fe7e8d344590446) by [Christopher Turner](https://sketchfab.com/TUR17002508), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Converted to OBJ, rescaled, made double-sided and recoloured per metal for Valheim.
- Rune Post plank: ["Wooden hook rack"](https://sketchfab.com/3d-models/wooden-hook-rack-b7928bccdd1344b79c0b67e7a9878016) by [Sousinho](https://sketchfab.com/sousinho), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Converted to OBJ, rescaled and made double-sided for Valheim.
- Ship figurehead: ["Dragon Winestopper"](https://sketchfab.com/3d-models/dragon-winestopper-fb547b2a57804d92a66df5da25abef11) by [PEDBRA](https://sketchfab.com/pedbra), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Converted to OBJ, rescaled, turned to face forward and made double-sided for Valheim.
- Valkyrie Stone model: ["Rune Stone"](https://sketchfab.com/3d-models/rune-stone-065bcefe36344914ba244c35b95610b6) by [Jadon_TheArtist](https://sketchfab.com/Jadon_TheArtist), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Converted to OBJ, rescaled, made double-sided and its glow recoloured to gold for Valheim.
- Waste Well model: ["Stone Well"](https://sketchfab.com/3d-models/stone-well-1498f53c9df54a289439f09ca89fc7d5) by [Andrew Jepson](https://sketchfab.com/ajepson), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Merged into one mesh with a combined texture (tiling baked in), converted to OBJ, rescaled and made double-sided for Valheim.
- Repair Anvil model: ["Asset02 Medieval Anvil"](https://sketchfab.com/3d-models/asset02-medieval-anvil-f6123f83e46345ccac727e35d91392ee) by [Margot D.](https://sketchfab.com/Winterll), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Merged into one mesh, converted to OBJ, rescaled and made double-sided for Valheim.
- Portal Astrolabe model: ["Armillary Amethyst"](https://sketchfab.com/3d-models/armillary-amethyst-8d95bfd75bbe4f8491d3e8407e33a06a) by [keishamikaele](https://sketchfab.com/keishamikaele), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Merged into one mesh, converted to OBJ, rescaled and made double-sided for Valheim.
- Harbour Anchor model: ["Medieval Anchor (Free)"](https://sketchfab.com/3d-models/medieval-anchor-free-5896ac54d63e4b84bd32e0b232619dfd) by [wolfgar74](https://sketchfab.com/wolfgar74), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Merged into one mesh with a combined texture, converted to OBJ, rescaled and made double-sided for Valheim.
- Munin's Perch post: ["nordic totem"](https://sketchfab.com/3d-models/nordic-totem-5ea68a916d0d42e494c67307510294c6) by [nofaced3d](https://sketchfab.com/nofaced3d), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Texture reduced to 1024 px, converted to OBJ, rescaled and made double-sided for Valheim.
- Munin's Perch raven: ["Ghost Raven"](https://sketchfab.com/3d-models/ghost-raven-0a5014dc066548f7b746ccfdbb4f2d9a) by [Alex Sanches](https://sketchfab.com/ASanches), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Branch cut away, textures reduced to 1024 px, eyes recoloured gold, converted to OBJ, rescaled and made double-sided for Valheim.
- Smoke Oven model: ["Furnace"](https://sketchfab.com/3d-models/furnace-64f344213c084424ba438875e62bd452) by [Tronin Dmitry](https://sketchfab.com/kosmotron), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Merged into one mesh with a combined texture, converted to OBJ, rescaled and made double-sided for Valheim.
- Cartographer's Desk model: ["Medieval Writing Desk"](https://sketchfab.com/3d-models/medieval-writing-desk-0982348984ad4126a90ace26be1d7300) by [Dmitriy Korotkov](https://sketchfab.com/ArtDmitriyK), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Converted to OBJ, rescaled and made double-sided for Valheim.
- Sextant model: ["Sextant"](https://sketchfab.com/3d-models/sextant-78852ce5d4264f33897b462e31069ddd) by [Asylum Nox](https://sketchfab.com/peter.pottiez), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Converted to OBJ, rescaled and made double-sided for Valheim.
- Map scroll model: ["Old / Ancient Scroll"](https://sketchfab.com/3d-models/old-ancient-scroll-73e9333251c7490786f99e67beb41d6e) by [Kigha](https://sketchfab.com/Kigha), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Converted to OBJ, rescaled and made double-sided for Valheim.
- Sea chart model: ["Pirate Map"](https://sketchfab.com/3d-models/pirate-map-ba468f31212e4d79b69dd09a509c0fd3) by [3000volt](https://sketchfab.com/3000volt), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Converted to OBJ, rescaled and made double-sided for Valheim.
- Pathfinder's Amulet pendant: ["viking ornaments amulets 5"](https://sketchfab.com/3d-models/viking-ornaments-amulets-5-81611cb361aa40daa733049acaa545c4) by [leoxx300](https://sketchfab.com/leoxx300), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). One pendant cut out of the set, converted to OBJ, rescaled and made double-sided for Valheim.
- White Hilt Portal model: ["Simple Stone portal"](https://sketchfab.com/3d-models/simple-stone-portal-e005e778460047d4a297a5c628b2cc8f) by [Lucia Criscuolo](https://sketchfab.com/Neruth), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Merged into one mesh with a combined texture, converted to OBJ, rescaled and made double-sided for Valheim.
- White Hilt Rune Circle model: ["Runic circle - remake of Frozen Throne"](https://sketchfab.com/3d-models/runic-circle-remake-of-frozen-throne-e28a908060eb4ab08fed2bea1aab92e5) by [Dawid](https://sketchfab.com/Dawid.Gerula), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Converted to OBJ, rescaled and made double-sided for Valheim.
- Surt's Brazier model: ["Primitive Brazier (Free)"](https://sketchfab.com/3d-models/primitive-brazier-free-3b155a4948b042ffb69f1d8a4aead250) by [wolfgar74](https://sketchfab.com/wolfgar74), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Converted to OBJ, rescaled and made double-sided for Valheim.
- Home Stone model: ["Rune in stone"](https://sketchfab.com/3d-models/rune-in-stone-1608283699bd414195319a41445d8055) by [Mardukblake](https://sketchfab.com/Mardukblake), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Converted to OBJ, rescaled and made double-sided for Valheim.
- Feeding Trough model: ["Wooden Trough With Stone Stand. Lowpoly"](https://sketchfab.com/3d-models/wooden-trough-with-stone-stand-lowpoly-673baf17c3bb43e8affd610eaf31cc8a) by [Hakan Unlu](https://sketchfab.com/hakan3d), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Merged into one mesh, converted to OBJ, rescaled and made double-sided for Valheim.
- Compost Bin model: ["Compost Bin SM SG"](https://sketchfab.com/3d-models/compost-bin-sm-sg-daaa0514f23a4ca18e955ea576671ef0) by [Pants85](https://sketchfab.com/Pants85), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Textures reduced to 1024 px, converted to OBJ, rescaled and made double-sided for Valheim.
- Net Winch winch: ["Sail ship winch"](https://sketchfab.com/3d-models/sail-ship-winch-77ce957ad88a4ce9a888a25ea429480c) by [Max Wittig](https://sketchfab.com/WittigMax), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Merged into one mesh, converted to OBJ, rescaled and made double-sided for Valheim.
- Net Winch barrel: ["Photobash Fish Barrel"](https://sketchfab.com/3d-models/photobash-fish-barrel-0260f702429144aea3d85cb7664e295d) by [Lakin](https://sketchfab.com/lakinrt), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Textures reduced to 1024 px, converted to OBJ, rescaled and made double-sided for Valheim.
- Grooming Comb model: ["CC0 - Hair Comb 6"](https://sketchfab.com/3d-models/cc0-hair-comb-6-0bde272bb07b4e1a99fd2f981926e187) by [plaggy](https://sketchfab.com/plaggy), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Converted to OBJ, rescaled and made double-sided for Valheim.
- Tether Post model: ["Wooden Post"](https://sketchfab.com/3d-models/wooden-post-4991b5d72f534a339e02c10848ee322b) by [PionX](https://sketchfab.com/PionX), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Converted to OBJ, rescaled and made double-sided for Valheim. Its chain is the Chain Bench's.
- Dog House model: ["Dog House Free"](https://sketchfab.com/3d-models/dog-house-free-fc9e3897b3564f36be62748aaf46adb5) by [donnichols](https://sketchfab.com/donnichols), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Textures downscaled, converted to OBJ, rescaled and made double-sided for Valheim.
- Trophy Altar model: ["Stone Altar"](https://sketchfab.com/3d-models/stone-altar-3d4f2edb18e4424cabb41b60aa160cb5) by [TheoClarke](https://sketchfab.com/TheoClarke), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Textures reduced to 1024 px, converted to OBJ, rescaled and made double-sided for Valheim.
- Dog Bed model: ["Wicker_Basket"](https://sketchfab.com/3d-models/wicker-basket-c8e8dc18b73946daaa4d51f81783ceab) by [National Heritage Administration](https://sketchfab.com/NHA_Asset), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Lid cut away, converted to OBJ, rescaled and made double-sided for Valheim.
- Dog Bowl model: ["Bark_bowl_package"](https://sketchfab.com/3d-models/bark-bowl-package-81641c44d91f40de85bbe36c5a346223) by [GetDeadEntertainment](https://sketchfab.com/GetDeadEntertainment), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). The low bowl taken out of the set, textures downscaled, converted to OBJ, rescaled and made double-sided for Valheim.
- Dog Whistle model: ["Wajia Flute"](https://sketchfab.com/3d-models/wajia-flute-e6d1c436d3bc41f99f49b5dc2c821b2f) by [3D Vault](https://sketchfab.com/3DVault), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Textures downscaled, converted to OBJ, rescaled and made double-sided for Valheim.
- Leather Dog Collar model: ["Leather Collar"](https://sketchfab.com/3d-models/leather-collar-de6857a0957b4047bff8c031877a255e) by [AnyRPG](https://sketchfab.com/anyrpg), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Converted to OBJ, rescaled and made double-sided for Valheim.
- Iron Dog Collar model: ["Spiked collar"](https://sketchfab.com/3d-models/spiked-collar-15d2eb02ee8d43f0b40f807d2d0f33a0) by [strakacher21](https://sketchfab.com/strakacher21), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Converted to OBJ, rescaled, made double-sided and coloured as iron for Valheim.
- Dog bark: ["Barking 1.wav"](https://freesound.org/people/Mrthenoronha/sounds/420450/) by [Mrthenoronha](https://freesound.org/people/Mrthenoronha/), released under [CC0](https://creativecommons.org/publicdomain/zero/1.0/).
- Dog whine: ["Dog Whine 5.wav"](https://freesound.org/people/esperri/sounds/118970/) by [esperri](https://freesound.org/people/esperri/), released under [CC0](https://creativecommons.org/publicdomain/zero/1.0/).
- Happy dog bark: ["Dog bark2.wav"](https://freesound.org/people/MisterTood/sounds/9032/) by [MisterTood](https://freesound.org/people/MisterTood/), released under [CC0](https://creativecommons.org/publicdomain/zero/1.0/).
- Dog snoring: ["Dog Sleeping_1.wav"](https://freesound.org/people/Ddustin99/sounds/462926/) by [Ddustin99](https://freesound.org/people/Ddustin99/), released under [CC0](https://creativecommons.org/publicdomain/zero/1.0/).
- Map compass: ["Seadogs Compass"](https://polyhaven.com/a/seadogs_compass) by Benny Weimer, [Poly Haven](https://polyhaven.com), released under [CC0](https://creativecommons.org/publicdomain/zero/1.0/). Dial and needle rendered from above, printed letters painted out and the needle's north end painted red.
- Treasure chest model: ["Treasure Chest"](https://polyhaven.com/a/treasure_chest) by Rico Cilliers, [Poly Haven](https://polyhaven.com), released under [CC0](https://creativecommons.org/publicdomain/zero/1.0/). Reduced to 5000 faces, converted to OBJ, rescaled and made double-sided for Valheim.
- Roof textures: ["Leafy Grass"](https://polyhaven.com/a/leafy_grass), ["Roof Slates 02"](https://polyhaven.com/a/roof_slates_02), ["Thatch Roof Angled"](https://polyhaven.com/a/thatch_roof_angled), ["Reed Roof 04"](https://polyhaven.com/a/reed_roof_04), ["Dark Wooden Planks"](https://polyhaven.com/a/dark_wooden_planks) and ["Brown Mud Leaves 01"](https://polyhaven.com/a/brown_mud_leaves_01) from [Poly Haven](https://polyhaven.com), released under [CC0](https://creativecommons.org/publicdomain/zero/1.0/). Regraded, the ridge band cut off the reed roof, and the shingles, scale shingles and turf edge drawn from the planks, mud and grass.
- Ship's bell: ["Striking a bell 15cm large"](https://commons.wikimedia.org/wiki/File:Striking_a_bell_15cm_large.ogg) by stephan (pdsounds.org), released into the public domain. Two strikes cut out, faded and normalized.
- White Hilt Sword model: ["Decorated Viking King Sword"](https://sketchfab.com/3d-models/decorated-viking-king-sword-401726ac11db416e91535caf2e81f865) by [Asylum Nox](https://sketchfab.com/peter.pottiez), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Textures downscaled, converted to OBJ, rescaled, made double-sided and the hilt painted white for Valheim.
- White Hilt Bow model: ["Bow of the Pack Hunter"](https://sketchfab.com/3d-models/bow-of-the-pack-hunter-8e27516a119941218def8076850800ec) by [Asylum Nox](https://sketchfab.com/peter.pottiez), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Textures downscaled, converted to OBJ, rescaled, made double-sided, the grip painted white and bent at runtime for Valheim.
- White Hilt Buckler model: ["Worn Round Shield"](https://sketchfab.com/3d-models/worn-round-shield-d848db788bc041ddab8a51c7836979d2) by [iedalton](https://sketchfab.com/iedalton), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Merged into one mesh with a combined texture, textures downscaled, converted to OBJ, rescaled, made double-sided and the boards painted white for Valheim.
- White Hilt Battleaxe model: ["Nordic Axe - Cloudcleaver"](https://sketchfab.com/3d-models/nordic-axe-cloudcleaver-10f5b39b05d54d4597a7d00996da3af8) by [Peter Nox](https://sketchfab.com/Peter.Nox), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). One of the two axes taken out of the file, textures downscaled, converted to OBJ, rescaled, made double-sided and the grip painted white for Valheim.
- White Hilt Mace model: ["Brass Viking Mace"](https://sketchfab.com/3d-models/brass-viking-mace-a0c1ee6b023f4cfdacba70d40960bfba) by [Asylum Nox](https://sketchfab.com/peter.pottiez), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Textures downscaled, converted to OBJ, rescaled, made double-sided and the grip painted white for Valheim.
- White Hilt Sledge model: ["Viking Warhammer"](https://sketchfab.com/3d-models/viking-warhammer-717b9fe5bb494fd48242db2d44c8d06c) by [Peter Nox](https://sketchfab.com/Peter.Nox), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Converted to OBJ, rescaled, made double-sided and the grip painted white for Valheim.
- White Hilt Spear model: ["Winterbite – Spear of the Frozen North"](https://sketchfab.com/3d-models/winterbite-spear-of-the-frozen-north-97e562017446407ea4e26ba30db69805) by [Peter Nox](https://sketchfab.com/Peter.Nox), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Textures downscaled, converted to OBJ, rescaled, made double-sided and the grip painted white for Valheim.
- White Hilt Staff model: the dark scepter from ["Weapon Set"](https://sketchfab.com/3d-models/weapon-set-480128be3ab744d4ba15de7c7b55e3bd) by [Asylum Nox](https://sketchfab.com/peter.pottiez), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Taken out of the set, textures downscaled, converted to OBJ, rescaled, made double-sided and the grip painted white for Valheim.
- White Hilt Atgeir model: the polearm from ["Weapon Set"](https://sketchfab.com/3d-models/weapon-set-480128be3ab744d4ba15de7c7b55e3bd) by [Asylum Nox](https://sketchfab.com/peter.pottiez), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Taken out of the set, textures downscaled, converted to OBJ, rescaled, made double-sided and the shaft painted white for Valheim.
- White Hilt Knife model: ["Seax Sword"](https://sketchfab.com/3d-models/seax-sword-06c33a65b47e409b84c607b1e79c63a1) by [iedalton](https://sketchfab.com/iedalton), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Merged into one mesh with a combined texture, textures downscaled, converted to OBJ, rescaled, made double-sided and the grip painted white for Valheim.
- White Hilt Crossbow model: ["LowPoly Crossbow Asset"](https://sketchfab.com/3d-models/lowpoly-crossbow-asset-bf49c17143ba418fa9abcf2724ae8e0b) by [iedalton](https://sketchfab.com/iedalton), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Merged into one mesh with a combined texture, textures downscaled, converted to OBJ, rescaled, made double-sided, the stock painted white and a drawn string added for Valheim.
- White Hilt Tower Shield model: ["Medieval Kite Shield"](https://sketchfab.com/3d-models/medieval-kite-shield-ef454e3700a7462eb27af245e685529a) by [iedalton](https://sketchfab.com/iedalton), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Merged into one mesh with a combined texture, textures downscaled, converted to OBJ, rescaled, made double-sided and the boards painted white for Valheim.
- White Hilt Paint Brush model: ["Paint Brush"](https://sketchfab.com/3d-models/paint-brush-5e68e46fe7c146e298e81a31f6454ac8) by [KOH4RU](https://sketchfab.com/KOH4RU), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Merged into one mesh, texture downscaled, converted to OBJ, rescaled, made double-sided and the handle painted white for Valheim.
- Paint Bench table and the Decor Hammer's trestle table: ["Medieval Table (Free)"](https://sketchfab.com/3d-models/medieval-table-free-6d4f897c019f4a55aeda23cda6e6fb57) by [wolfgar74](https://sketchfab.com/wolfgar74), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Texture downscaled, converted to OBJ, rescaled and made double-sided for Valheim.
- Paint Bench palette: ["Color palette"](https://sketchfab.com/3d-models/color-palette-82e2019ab9754e208dadc9f0a23e7161) by [Alberto](https://sketchfab.com/onzoalberto), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Merged into one mesh, the paint blobs and board coloured, converted to OBJ, rescaled and made double-sided for Valheim.
- Paint Pot and Paint Bench bucket: ["Stylized Low Poly Wooden Bucket | Game Ready"](https://sketchfab.com/3d-models/stylized-low-poly-wooden-bucket-game-ready-b55447c9da0e462b95dbf9dfbe97f061) by [Null__](https://sketchfab.com/Nullqw), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Merged into one mesh with a combined texture, the water coloured as paint, converted to OBJ, rescaled and made double-sided for Valheim.
- Paint Bench brushes: ["Brush in a cup"](https://sketchfab.com/3d-models/brush-in-a-cup-3518d3fbb5634c3eac5d210ed1c4b558) by [Spiketus](https://sketchfab.com/Spiketus), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Merged into one mesh with a combined texture, the saucer removed, converted to OBJ, rescaled and made double-sided for Valheim.
- Kraken model: ["Lurker - Rigged and Animated"](https://sketchfab.com/3d-models/28b3e1a216904de7ad212368fb9d8f59) by [Greeble3d](https://sketchfab.com/Greeble3d), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Two parts with broken skinning removed, glowing eyes added, merged into one mesh, converted to FBX with four of its animations and rescaled for Valheim.
- Octopus model: ["Octopus"](https://sketchfab.com/3d-models/f9c0186d5ac54bcfada2b6113de40ede) by [rkuhlf](https://sketchfab.com/rkuhlf), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Merged into one mesh, converted to FBX with its animations and rescaled for Valheim.
- Lindorm model: ["Worm Monster"](https://sketchfab.com/3d-models/5563066315694125b741901681d387c5) by [CR!STALLL](https://sketchfab.com/CR1STALLL), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Merged into one mesh, converted to FBX with six of its animations and rescaled for Valheim.
- Giant Spider model: ["Wolf Spider (Rigged) - (Rabidosa rabida)"](https://sketchfab.com/3d-models/6392e4cfb64d407182fdad2cea9e0abe) by [Dreaming In Alternation 27](https://sketchfab.com/DreamingInAlternation27), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Textures downscaled, converted to FBX with two of its animations, idle, bite, hit and death animations added, and rescaled for Valheim.
- Desert Dragon model: ["Red Dragon"](https://sketchfab.com/3d-models/red-dragon-d53fe00255334386a0fd1f4ac2858cab) by [absol](https://sketchfab.com/absol_cg), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Textures downscaled, converted to FBX with two of its animations, a fire-breathing animation added, recoloured in game and rescaled for Valheim.
- README icons: [Noto Emoji](https://github.com/googlefonts/noto-emoji) by Google, licensed under the [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0), set on round badges.

- Decor Hammer models from [Poly Haven](https://polyhaven.com), released under [CC0](https://creativecommons.org/publicdomain/zero/1.0/): ["Fern 02"](https://polyhaven.com/a/fern_02) by Rob Tuytel and Rico Cilliers, ["Nettle Plant"](https://polyhaven.com/a/nettle_plant) by Rob Tuytel and Rico Cilliers, ["Shrub 02"](https://polyhaven.com/a/shrub_02) by Rico Cilliers, ["Shrub 03"](https://polyhaven.com/a/shrub_03) by Rico Cilliers, ["Shrub Sorrel 01"](https://polyhaven.com/a/shrub_sorrel_01) by Rico Cilliers, ["Dandelion 01"](https://polyhaven.com/a/dandelion_01) by Rob Tuytel and Rico Cilliers, ["Celandine 01"](https://polyhaven.com/a/celandine_01) by Rob Tuytel and Rico Cilliers, ["Periwinkle Plant"](https://polyhaven.com/a/periwinkle_plant) by Amal Kumar, ["Weed Plant 02"](https://polyhaven.com/a/weed_plant_02) by Rob Tuytel and Rico Cilliers, ["Grass Medium 01"](https://polyhaven.com/a/grass_medium_01) by Rob Tuytel and Rico Cilliers, ["Grass Medium 02"](https://polyhaven.com/a/grass_medium_02) by Rico Cilliers, ["Flower Ursinia"](https://polyhaven.com/a/flower_ursinia) by Jenelle van Heerden and Rico Cilliers, ["Flower Empodium"](https://polyhaven.com/a/flower_empodium) by Jenelle van Heerden and Rico Cilliers, ["Flower Heliophila"](https://polyhaven.com/a/flower_heliophila) by Jenelle van Heerden, ["Flower Gazania"](https://polyhaven.com/a/flower_gazania) by James Ray Cock and Jenelle van Heerden, ["Moss 01"](https://polyhaven.com/a/moss_01) by Rob Tuytel, ["Tree Stump 01"](https://polyhaven.com/a/tree_stump_01) by Rob Tuytel, ["Tree Stump 02"](https://polyhaven.com/a/tree_stump_02) by Rob Tuytel, ["Dead Tree Trunk"](https://polyhaven.com/a/dead_tree_trunk) by Rob Tuytel, ["Dead Tree Trunk 02"](https://polyhaven.com/a/dead_tree_trunk_02) by Jenelle van Heerden and Rico Cilliers, ["Dry Branches Medium 01"](https://polyhaven.com/a/dry_branches_medium_01) by Rico Cilliers, ["Pine Roots"](https://polyhaven.com/a/pine_roots) by Rob Tuytel, ["Rock Moss Set 01"](https://polyhaven.com/a/rock_moss_set_01) by Kless Gyzen, ["Rock Moss Set 02"](https://polyhaven.com/a/rock_moss_set_02) by Kless Gyzen, ["Boulder 01"](https://polyhaven.com/a/boulder_01) by Rico Cilliers, ["Stone 01"](https://polyhaven.com/a/stone_01) by Dario Barresi and Rico Cilliers, ["Wine Barrel 01"](https://polyhaven.com/a/wine_barrel_01) by James Ray Cock, ["Wooden Barrels 01"](https://polyhaven.com/a/wooden_barrels_01) by James Ray Cock, ["Wooden Crate 01"](https://polyhaven.com/a/wooden_crate_01) by James Ray Cock, ["Wooden Crate 02"](https://polyhaven.com/a/wooden_crate_02) by James Ray Cock and Jurita Burger, ["Wooden Bucket 01"](https://polyhaven.com/a/wooden_bucket_01) by James Ray Cock, ["Wooden Bucket 02"](https://polyhaven.com/a/wooden_bucket_02) by James Ray Cock, ["Wicker Basket 01"](https://polyhaven.com/a/wicker_basket_01) by Kuutti Siitonen, ["Wicker Basket 02"](https://polyhaven.com/a/wicker_basket_02) by Kuutti Siitonen, ["Wooden Bowl 01"](https://polyhaven.com/a/wooden_bowl_01) by Oliver Harries, ["Wooden Bowl 02"](https://polyhaven.com/a/wooden_bowl_02) by Kuutti Siitonen, ["Carved Wooden Plate"](https://polyhaven.com/a/carved_wooden_plate) by Jan Martens, ["Wooden Spoon"](https://polyhaven.com/a/wooden_spoon) by Ronnie Barter, ["Wooden Cutting Board"](https://polyhaven.com/a/wooden_cutting_board) by Kuutti Siitonen, ["Ceramic Pot"](https://polyhaven.com/a/ceramic_pot) by Aron Łyczek, ["Planter Pot Clay"](https://polyhaven.com/a/planter_pot_clay) by Amal Kumar, ["CheeseBox_01"](https://polyhaven.com/a/CheeseBox_01) by Gabriel Radić, ["Yellow Onion"](https://polyhaven.com/a/yellow_onion) by Kuutti Siitonen, ["Food Apple 01"](https://polyhaven.com/a/food_apple_01) by Oliver Harries, ["Hatchet"](https://polyhaven.com/a/hatchet) by James Ray Cock and Ulan Cabanilla, ["Wooden Axe 03"](https://polyhaven.com/a/wooden_axe_03) by Ulan Cabanilla, ["Wooden Ladder 02"](https://polyhaven.com/a/wooden_ladder_02) by JN_3DPRINTINGNERD, ["Spinning Wheel 01"](https://polyhaven.com/a/spinning_wheel_01) by Sofia Pahaoja, ["Hand Plane No4"](https://polyhaven.com/a/hand_plane_no4) by Satyaki Mandal, ["Cross Pein Hammer"](https://polyhaven.com/a/cross_pein_hammer) by Tics, ["Rusted Spade 01"](https://polyhaven.com/a/rusted_spade_01) by Blemonade, ["Folding Wooden Stool"](https://polyhaven.com/a/folding_wooden_stool) by Ulan Cabanilla, ["Wooden Stool 01"](https://polyhaven.com/a/wooden_stool_01) by Kuutti Siitonen, ["Wooden Stool 02"](https://polyhaven.com/a/wooden_stool_02) by Kuutti Siitonen, ["Round Wooden Table 02"](https://polyhaven.com/a/round_wooden_table_02) by Ulan Cabanilla, ["Small Wooden Table 01"](https://polyhaven.com/a/small_wooden_table_01) by Ulan Cabanilla, ["Wooden Table 01"](https://polyhaven.com/a/WoodenTable_01) by Ethan Place, ["Wooden Table 02"](https://polyhaven.com/a/WoodenTable_02) by Fran Calvente, ["Bull Head"](https://polyhaven.com/a/bull_head) by Tina, ["Horse Head"](https://polyhaven.com/a/horse_head) by Tina, ["Wooden Candlestick"](https://polyhaven.com/a/wooden_candlestick) by Josh Dean, ["Wooden Lantern 01"](https://polyhaven.com/a/wooden_lantern_01) by James Ray Cock, ["Stone Fire Pit"](https://polyhaven.com/a/stone_fire_pit) by Sebastian Platen. Reduced to a few thousand faces, textures downscaled, converted to OBJ, rescaled and made double-sided for Valheim.
- Decor Hammer farm props: ["Norse Farmstead Props"](https://sketchfab.com/3d-models/norse-farmstead-props-5b86930e11804164b5c03d0d66e5b389) by [Vecarus](https://sketchfab.com/Vecarus), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Split into separate props, textures downscaled, converted to OBJ, rescaled and made double-sided for Valheim.
- Decor Hammer fish drying rack: ["Drying Fish Low Poly"](https://sketchfab.com/3d-models/drying-fish-low-poly-0466aebea7c1460fb4a87fb10f0181ff) by [RacKrab](https://sketchfab.com/RacKrab), licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/). Converted to OBJ, rescaled and made double-sided for Valheim.

Made for Valheim by Kjell Arne Brudvik · [github.com/brudvik/BrudvikWhiteHilt](https://github.com/brudvik/BrudvikWhiteHilt)
