# 🧭 Navigation

[← Back to the README](../README.MD)

## 🧭 NAVIGATION

A new skill, **Exploration**, rises with every patch of map you uncover for the first time. On its own it does nothing; it makes the navigation gear better. From level 50, map that others have shared with you through a map table is shown just like map you uncovered yourself, without the see-through layer (hiding shared map data brings the layer back). The gear is made at the **Cartographer's Desk**, which works as an extension of the map table and can only be used within 5 m of one.

<img src="images/cartographers_desk.png" alt="Cartographer's Desk" title="Cartographer's Desk" height="140"> <img src="images/navigators_table.png" alt="Navigator's Table" title="Navigator's Table" height="140"> <img src="images/pathfinders_amulet.png" alt="Pathfinder's Amulet" title="Pathfinder's Amulet" height="140">

| Item | Description | Crafting Station | Requirements |
|------|-------------|------------------|--------------|
| **Cartographer's Desk** | A writing desk with sea charts, a sextant and map scrolls; one scroll per ten levels of your Exploration skill | Hammer (Workbench), within 5 m of a map table | Fine Wood ×10, Bronze ×2, Deer Hide ×2, Resin ×4 |
| **Navigator's Table** | Use it on the helm of a karve, longship, drakkar or White Hilt Ship to set it up on deck; on the White Hilt Ship the mast takes it too, like the ship upgrades (Shift + Use on the helm takes it back). Everyone aboard uncovers the map further: 140 m at level 0, up to 300 m at level 100 (vanilla is 100 m). Use on the table opens the map for route markers at Exploration 30 (up to 5, shown to everyone aboard, with an arrow on the minimap toward the next one); at Exploration 50 "Take me there" lets the ship sail the route on its own | Cartographer's Desk | Fine Wood ×6, Bronze ×3, Leather Scraps ×4 |
| **Pathfinder's Amulet** | A valknut pendant worn as a trinket, with one gem per twenty levels of Exploration. Uncovers the map further (120 m, up to 200 m). New land fills its adrenaline; when it is full, **Raven Sight** uncovers 500 m around you | Cartographer's Desk | Bronze ×3, Silver Necklace ×1, Ruby ×1 |

The table and the amulet do not add up; the wider one counts.

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
