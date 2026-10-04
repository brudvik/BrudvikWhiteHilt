# Changelog

All notable changes to BrudvikWhiteHilt. Newest version first.

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
