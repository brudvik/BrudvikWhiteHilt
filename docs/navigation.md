# 🧭 Navigation

[← Back to the README](../README.MD)

## 🧭 NAVIGATION

A new skill, **Exploration**, rises with every patch of map you uncover for the first time. On its own it does nothing; it makes the navigation gear better. From level 50, map that others have shared with you through a map table is shown just like map you uncovered yourself, without the see-through layer (hiding shared map data brings the layer back). The gear is made at the **Cartographer's Desk**, which works as an extension of the map table and can only be used within 5 m of one.

From Exploration 25 the **Lookout** (O) opens up the map around you and shows sea monsters and ships; see [Skills & milestones](skills.md).

<img src="images/cartographers_desk.png" alt="Cartographer's Desk" title="Cartographer's Desk" height="140"> <img src="images/navigators_table.png" alt="Navigator's Table" title="Navigator's Table" height="140"> <img src="images/pathfinders_amulet.png" alt="Pathfinder's Amulet" title="Pathfinder's Amulet" height="140">

| Item | Description | Crafting Station | Requirements |
|------|-------------|------------------|--------------|
| **Cartographer's Desk** | A writing desk with sea charts, a sextant and map scrolls; one scroll per ten levels of your Exploration skill | Hammer (Workbench), within 5 m of a map table | Fine Wood ×10, Bronze ×2, Deer Hide ×2, Resin ×4 |
| **Navigator's Table** | Use it on the helm of a karve, longship, drakkar or White Hilt Ship to set it up on deck; on the White Hilt Ship the mast takes it too, like the ship upgrades (Shift + Use on the helm takes it back). Everyone aboard uncovers the map further: 140 m at level 0, up to 300 m at level 100 (vanilla is 100 m). Use on the table opens the map for route markers at Exploration 30 (up to 5, shown to everyone aboard, with an arrow on the minimap toward the next one); at Exploration 50 "Take me there" lets the ship sail the route on its own, past every marker in turn. It sets off once the one who chose it sits down (within 30 seconds by default, or it is called off), rows at the slowest speed while anyone aboard stands, and takes the sail down to half above 45 knots. **Explorer mode** (also Exploration 50) sails past the markers as close to land as it safely can, following the coast, and never with full sail near land | Cartographer's Desk | Fine Wood ×6, Bronze ×3, Leather Scraps ×4 |
| **Pathfinder's Amulet** | A valknut pendant worn as a trinket, with one gem per twenty levels of Exploration. Uncovers the map further (120 m, up to 200 m). New land fills its adrenaline; when it is full, **Raven Sight** uncovers 500 m around you | Cartographer's Desk | Bronze ×3, Silver Necklace ×1, Ruby ×1 |

The table and the amulet do not add up; the wider one counts.

## 🪨 Stone Dowser

The rocks a **Mysterious Rock** is made from (Rock + Coal) lie in Black Forest clearings around a big boulder, 22 in each. The **Stone Dowser** helps you find the next clearing, the way the Wishbone finds silver.

| Item | Description | Crafting Station | Requirements |
|------|-------------|------------------|--------------|
| **Stone Dowser** | A Wishbone in grey stone, worn in an accessory slot. Every 30 seconds it finds the nearest clearing within 3000 m that still has rocks, marks it on the map with the number left, and tells you the direction and distance when it changes. Within 40 m of a rock it pings like the Wishbone, faster the closer you come, at a lower pitch so the two can be told apart | Stonecutter | Stone ×20, Iron ×2, Greydwarf Eye ×5 |

- The server knows every clearing in the world, also those nobody has been near; those count as full.
- In a clearing someone has visited, only the rocks not yet picked count. Clearings with none left are skipped.
- Taking the dowser off removes the pin.

## 🖼️ Player portraits on the map

Other players are shown on the map as a portrait of their Viking on a see-through black disc, with the name under it in a soft shadow, instead of the red figure.

- Your portrait is taken in the main menu when the character is shown: bare head (no helmet), hair and beard, in neutral light. A new one is only taken when the look changes (body, hair, beard or colours).
- When you join, the others get the portrait once (about 10 KB). They keep it on disk, so the next time only a short fingerprint is sent. Nothing extra is needed on the server.
- A player without a portrait gets the first letter of the name on a coloured disc.
- Names are always shown on the large map; on the minimap only if you turn it on.
- Portraits are stored in `BepInEx/config/WhiteHilt/portraits/`. Delete `own_<id>.bin` to have yours taken again.

| Setting (section `Map`) | Default | Description |
|-------------------------|---------|-------------|
| `PlayerPortraits` | true | Show portraits, and take and share your own |
| `ShowNamesOnMinimap` | false | Show names under the portraits on the minimap too |

| Console command | Description |
|-----------------|-------------|
| `whitehilt_portrait` | Shows which players' portraits are known |
| `whitehilt_portrait test` | Adds or removes a pin with your own portrait 20 m east of you |

## 🏰 Built areas, fields, pastures and wards

The map shows where people have built. The server looks through the whole world every half minute and groups what it finds into areas of 16 m squares; squares up to two apart belong to the same area, so a path or garden between houses does not split a base.

| Area | What counts | Drawn as |
|------|-------------|----------|
| **Base** | Player-built pieces with a bed and a fire | Filled in the builder's colour, gold border |
| **Outpost** | Player-built pieces with a workbench (or other crafting station) or a portal | Builder's colour, blue border |
| **Building** | Other player-built pieces | Builder's colour, white border |
| **Field** | Planted crops, growing or ripe (ripe ones only beside a farm, since seeds also grow wild) | Green |
| **Pasture** | Tamed animals | Brown, with the number of animals |
| **Ward** | Every ward | Dashed ring showing its reach: green when on, grey when off |

- Each builder has their own colour, the same for everyone. The area takes the colour of whoever built most of it.
- Only areas where you have uncovered the map are drawn.
- Zoom in on the large map to see the names. A sign in the area whose text starts with `#` names it (`#Brudvik`); otherwise it shows the kind and the builder.
- Ships and carts are left out; the Harbour Anchor shows ships.

| Setting (section `Map.Areas`) | Default | Description |
|-------------------------------|---------|-------------|
| `AllowAreas` (server) | true | Find areas and wards at all |
| `ShowOthers` (server) | true | Players see everyone's buildings; off: only their own, and the fields and pastures beside them |
| `MinPieces` (server) | 5 | Fewest pieces for a built area (a lone campfire is left out) |
| `MinPlants` (server) | 4 | Fewest crops for a field |
| `MinAnimals` (server) | 2 | Fewest tamed animals for a pasture |
| `ShowBuildings` / `ShowFields` / `ShowPastures` / `ShowWards` | true | What you draw on your own map |
| `ShowLabels` | true | Names on the large map when zoomed in |

## Config

All admin only, synced from the server.

| Setting | Default | What it does |
|---|---|---|
| `[Navigation] ExploreRadiusBonus` | true | The Navigator's Table and the Pathfinder's Amulet widen the uncovered circle |
| `[Navigation] TableBonus` | 2 | Extra radius with the table at Exploration 100, as a share of the vanilla 100 m (2 = 300 m) |
| `[Navigation] AmuletBonus` | 1 | Extra radius with the amulet at Exploration 100 (1 = 200 m) |
| `[Navigation] BonusAtLevelZero` | 0.2 | Share of the extra radius already given at Exploration 0 |
| `[Navigation] SkillPerSquareMetre` | 0.0005 | Exploration experience per square metre of new map |
| `[Navigation] SharedMapReveal` | true | Shared map is drawn like your own from `SharedMapRevealLevel` |
| `[Navigation] SharedMapRevealLevel` | 50 | Exploration level for that |
| `[Navigation] RavenSightRadius` | 500 | Metres that Raven Sight uncovers |
| `[Gear.WhiteHiltChartTable] Weight` | 10 | Weight of the Navigator's Table item |
| `[Gear.WhiteHiltPathfinder] MaxAdrenaline` | 50 | Adrenaline needed for Raven Sight |
| `[Gear.WhiteHiltPathfinder] AdrenalinePerSquareMetre` | 0.0001 | Adrenaline per square metre of new map |
| `[Gear.WhiteHiltStoneDowser] SearchRadius` | 3000 | Metres the Stone Dowser looks for a clearing with rocks left |
| `[Gear.WhiteHiltStoneDowser] RefreshSeconds` | 30 | Seconds between each search |
| `[Gear.WhiteHiltStoneDowser] PingRange` | 40 | Metres within which it pings toward the nearest rock |
| `[Gear.WhiteHiltStoneDowser] ShowPin` | true | Mark the clearing on the map; off: only direction and distance |
| `[Gear.WhiteHiltStoneDowser] Items` | StoneRock | Items whose pickables it counts and pings toward |
| `[Gear.WhiteHiltStoneDowser] Locations` | BigRockClearing | Locations it leads to |
