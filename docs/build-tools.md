# 🏗️ Build camera & toolbar

[← Back to the README](../README.MD)

## 🏗️ BUILD CAMERA & TOOLBAR

While you hold a build tool (hammer, hoe, cultivator, or a build tool from another mod such as Clutter), a toolbar shows on the left and a key hint bottom-left. **Hold Left Alt** to get the mouse cursor and click the buttons; hovering a button explains it, and clicking the toolbar's title folds it down to its status lines. Every button also has a hotkey. On Linux with KDE Plasma, Alt + mouse may be taken by the window manager for moving windows; change the `Cursor` key in the config, or set the window-action modifier to Meta in KDE's settings. The status lines show the piece's heading, tilt and roll, its height above the ground and its distance from the last piece you placed. Red, green and blue lines on the piece show its own axes.

| Tool | Key | What it does |
|------|-----|--------------|
| Build camera | B | A free camera that flies within 12 m of you or anywhere inside a crafting station's build range, also below ground to reach awkward spots. You stand still; pieces are aimed and placed from the camera, up to 50 m away. W A S D fly, Space up, Ctrl down, Shift faster. At the edge of the allowed area, rings show where it ends. Your Wisp light follows the camera |
| Fly to aim | J | Flies the camera to 3 m in front of what you aim at (and switches it on) |
| Orbit | hold Z | Circles the camera around the piece or the point you aim at; W / S go closer or further |
| Camera speed | Num+ / Num- | Faster or slower flying |
| Rotation step | Y | 22.5°, 15°, 5° or 1° per turn of the mouse wheel, also used for tilt and roll |
| Tilt | Num8 / Num2, or Ctrl + wheel | Tilt the piece forward or back |
| Roll | Num4 / Num6, or Alt + wheel | Roll the piece left or right |
| Quick angles | Num7 / Num9 | Set the tilt or the roll to 45°, 90° or back to 0° |
| Flip | Num5 | Turn the piece upside down |
| Reset | Num0 | Straighten the piece and clear the nudge |
| Snap | N | Snapping to other pieces on and off |
| Snap point | vanilla key | Next snap point on the held piece |
| Copy | I | Pick the piece under the crosshair with its heading, tilt and roll (Shift + middle mouse does the same) |
| Nudge | Arrows, PgUp / PgDn | Move the piece 5 cm along its own axes, the one closest to where you look |
| Grid | H | Keep the piece on a 0.5 m grid when it is not snapped |
| Stamp | K | Place a copy of the last piece right next to it, on the side you look towards; press again for a row, look up for a stack. Not for ground, water or terrain pieces |
| Undo / Redo | Ctrl + Z / Ctrl + Y | Take back the last piece you placed within 30 s, with everything it cost, and put it back again |
| Repair mode | Ctrl + R | Switches the hammer to its repair and back to the piece you held. One click repairs every damaged piece in a circle around where you aim, from the ground to the roof; the mouse wheel sets the size (1 to 30 m, or one piece as in vanilla). A ring shows the area and damaged pieces glow red to yellow by how worn they are. Each piece costs a quarter of a normal repair's stamina and tool wear; warded pieces and pieces whose crafting station is out of reach are skipped |
| Light | L | Camera light on and off |

The `BuildTools` config section sets how far the camera may fly (`CameraPlayerRange`, `CameraStationRange`), the placing reach, the undo time, whether the camera picks up items near it (`AutoPickup`, off by default), and area repair (`RepairArea`, `RepairMaxRadius`, `RepairAreaCost`); these are synced from the server. Speed, mouse, light, nudge step, grid size, repair radius, the axes and whether the toolbar and hint show are your own, and every key can be changed in `BuildTools.Keys`.

### Media: photos and films

While the build camera is on:

| Key | What it does |
|-----|--------------|
| P | Take a photo without HUD, toolbar, ghost or other build helpers. It is saved in the game's screenshot folder as `whitehilt_<date>_<time>.png`, at 2× the screen resolution (`PhotoSize`: 1, 2 or 4) |
| O | Photo view: only the world is shown while you fly, and the mouse wheel zooms |
| U | Media panel on the right. The cursor is free; hold the right mouse button to look around |

The media panel also hides your own character, and sets the time of day and the weather. Only you see this, and everything goes back to normal when the camera is switched off.

**Films** are camera paths played in real time, for recording with your own recorder (OBS, Xbox Game Bar with Win+Alt+R, ShadowPlay). Fly to a spot and press *Add here*; each point gets a small picture. Points can be updated, visited, moved up and down and deleted, and each has its time to the next point, a pause and a smooth or even transition. The camera glides along a curve through the points. A film can have a title card with title, name and date, and fade in from and out to black. *Preview* runs the path quickly with the panel shown; *Play* counts down 3-2-1 so you can start recording, then plays without HUD. Esc stops. Films are saved per world in `BepInEx/config/BrudvikWhiteHilt/media/`. The keys are in `BuildTools.Media`.

### Groups, blueprints, lines and areas

These work with any build tool, with or without the build camera. The right mouse button leaves a mode.

| Key | What it does |
|-----|--------------|
| Ctrl + A | Selection mode: click pieces to add or remove them, or Shift+click two corners to select everything in a box (PgUp / PgDn change its height). Selected pieces glow blue. Leaving selection mode (Ctrl + A again, right-click or putting the hammer away) clears the selection |
| Ctrl + C | Copy the selection and start placing the copy |
| Ctrl + X | Move the selection: it is torn down and put up again where you place it, for free. Sign texts and portal names follow; chests and stands must be empty |
| Ctrl + V | Place the last copy again |
| Delete | Tear down the selection, giving back what the hammer would |
| Ctrl + S | Save the selection as a blueprint |
| Ctrl + O | Blueprint panel: search, cost against what you have (chests count), place, rename, delete |
| Ctrl + L | Line and area with the held piece: click the start, then the end. Walls, fences and beams go in a row (a line carries on from where it ended; low pieces such as fences follow the ground), floors fill the area. Press again to switch between line, area and off |
| Ctrl + D | Clear the selection |

While placing a group, a see-through preview shows where everything goes: blue when it can be placed, red when something is missing (the hint says what). The mouse wheel turns it, and the arrows and PgUp / PgDn shift it. A group is placed all or nothing, from the bottom up, with the usual rules: known recipes, crafting stations, wards and no-build zones. Ctrl + Z takes back a whole paste, move, line, area or tear-down.

Blueprints are saved in `BepInEx/config/BrudvikWhiteHilt/blueprints/` in PlanBuild's `.blueprint` format, shared by all worlds. PlanBuild `.blueprint` and BuildShare `.vbuild` files put in that folder can be used too; pieces from mods you do not have are skipped. Terrain changes are not part of blueprints. The server can switch group building off and limit the number of pieces per group (`BuildTools.Groups`, no limit by default).

### White Hilt hoe and cultivator

The **White Hilt Hoe** and **White Hilt Cultivator** get their own tools. While you hold one, a *Terrain* or *Field* panel shows next to the build toolbar (hold Left Alt to click it). Areas are picked by clicking two corners; after the first corner the arrow keys set a fixed size. The right mouse button steps back.

| Tool | Key | What it does |
|------|-----|--------------|
| Road (hoe) | Ctrl + R | The map opens: click points, double-click or close the map to finish. The road (4 m wide by default; the up/down arrow keys change the width while the map is open, 1 to 12 m; stone, dirt or no surface) is built as you walk along it, since only land near you is loaded. It follows the ground with the bumps smoothed out, or keeps an even slope between the points. It stops at water, pays stone as it goes and pauses when you run out. Pause or cancel it on the panel; it is saved with the world |
| Level (hoe) | Ctrl + F | Pick an area, then the height (PgUp / PgDn in 0.25 m steps, Shift+click to take it from where you aim, or the height reference). The preview shows how much is dug and filled and the cost. The edges slope down to the ground around |
| Ramp (hoe) | Ctrl + G | Click the bottom and the top: an even ramp, as wide as set on the panel (arrow up/down), with length, rise and slope. The slope can be locked to 10, 15, 20 or 25 %. The next ramp starts where the last ended |
| Paint (hoe) | Ctrl + P | Stone, dirt or grass over an area without changing its height |
| Reset (hoe) | Ctrl + T | Put an area back to the ground as the world made it |
| Height reference (hoe) | Ctrl + M | The panel shows the height where you aim and the difference from the reference |
| Big brush (hoe) | Ctrl + mouse wheel | The hoe's own level, raise, smooth and paths work over 1 to 8 m, costing more for the larger area. A circle shows the size |
| Grid (cultivator) | Ctrl + G | Plant rows x columns at once (arrows), as far apart as the plant needs to grow (PgUp / PgDn for more). Green places can be planted, red ones cannot (wrong biome, no room, not cultivated). The ground is cultivated first if *Cultivate under* is on |
| Refill (cultivator) | Ctrl + R | Plant the empty places of the last grid again |
| Cultivate area (cultivator) | Ctrl + F | Cultivate an area |
| Harvest area (cultivator) | Ctrl + H | Pick every ripe crop in an area |
| Growth (cultivator) | panel | Labels over nearby plants: time left in green, or why a plant is not growing in red. Picked berry bushes, mushrooms and flowers show when they grow back in blue |

Ctrl + Z takes back terrain changes for 60 seconds, with the stone. On a server the change is taken back where the terrain is yours to change. Levelling, lowering, dirt and cultivating are free; paved stone and raised ground cost stone as with the vanilla hoe for the same area and height. The game keeps ground within 8 m of where it started; the tools warn when a change would go past that. The `BuildTools.Terrain` section lets the server switch the tools off, change the cost, allow or forbid harvesting areas, and limit the size of one change (2500 m² by default).
