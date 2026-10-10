# 🕰️ HUD, clock, party list, sound & floating items

[← Back to the README](../README.MD)

## 🧭 WHITE HILT HUD

The game's own HUD has grown corner by corner: health and food at the lower left, stamina in the middle, status effects under the map, and White Hilt's clock, party list and compass added where there was room. Counted up there are about 25 elements in 9 places and 3 styles, and no place for what other mods add:

![Everything drawn on the HUD before: vanilla, White Hilt's and other mods' elements spread over nine places](images/hud_before.png)

The White Hilt HUD gathers everything about yourself in one console at the bottom centre, so the eye goes to one place, and leaves the middle of the screen to the world:

![Concept drawing of the White Hilt HUD: compass with clock and waypoint at the top, the console at the bottom with food, bars, hotbar and status effects](images/hud_console.png)

*A concept drawing; in the game the icons are the game's own.*

- **The hotbar** in the middle, with the arrows, bolts, bait or staff casts over the slot that uses them.
- **Health, stamina and eitr** over the hotbar as bars as wide as it, health the widest, each with its number, `212 / 260`; the health number turns red under a quarter. Eitr shows for those who have it, adrenaline while there is some.
- **The food** to the left: each icon on a dark disc with a ring round it that empties as the food runs out, going from green through yellow to red, the time left under it, flashing when it can be eaten again. **The guardian power** to the left of the food.
- **Status effects** to the right, four to a row, each with a ring that empties as its time runs out and its time under it: the game's, White Hilt's and other mods' alike. When there are more than fit, the last place says how many are left out.
- **A context strip** over the bars when there is something to say, up to three lines:
  - **Warnings**, for a while, instead of a message in the middle of the screen: night falling (orange) and, aboard a ship with a Navigator's Table, a storm coming (violet).
  - **A raid or other event** while it lasts, in red, instead of the game's event bar.
  - **The piece you are about to build** and what it costs, each amount in the game's own colour for it: red while you have too little, and White Hilt's colour when [nearby chests](chests.md) make up the rest. The game's build panel at the bottom is hidden.
  - **The ship** while you steer it (or ride one sailing its route): speed, heading, depth, wind, held course and time to arrival, instead of under the wind indicator.

The [clock](#-clock) sits at the left end of the [HUD compass](navigation.md#horizontal-hud-compass) and the [waypoint arrow](navigation.md) at the right end; without the compass they go back to their own places. The [party list](#-party-list), with your dog and tamed animals under it, stands at the left, a third of the way down. When the ship controls are under the console, it rises above them.

Other mods that put something of their own on the HUD where the console is get it lifted into a band over the console, stacked if there are several, so nothing covers the bars or the hotbar. Their status effects need nothing; they are in the console with the rest.

### A quiet screen

With `Calm` on, each part of the console shows only while it has something to tell, and fades when it has not: a bar while it is not full, the food while some is half gone or can be eaten again, the status effects for a while after one comes or goes (or while one flashes), and the hotbar and guardian power after a hotbar key, a change of weapon, a drawn bow or a hit. When you rest at full health with full bellies, only the compass is left. Everything shows while the inventory is open, or while you hold the `ShowAll` key.

![Concept drawing of the quiet screen in four situations: resting, combat, sailing and building](images/hud_calm.png)

*A concept drawing of the idea; in the game the bars stay over the hotbar rather than round the crosshair.*

### Settings

The `HUD` section, each player's own:

| Setting | Default | |
|---|---|---|
| `Style` | `WhiteHilt` | `Vanilla` gives the game's own HUD back, with White Hilt's additions where they always were |
| `Scale` | 1 | Size of the bars, food, status effects and context strip; the hotbar keeps the game's size |
| `BottomMargin` | 16 | Pixels from the bottom of the screen to the hotbar |
| `StatusColumns` | 4 | Status effects per row |
| `MaxStatusEffects` | 8 | Most status effects shown; the last place counts the rest |
| `Calm` | off | The quiet screen |
| `CalmSeconds` | 4 | Seconds a part stays after it last changed (status effects three times as long) |
| `CalmOpacity` | 0 | How much of a faded part still shows |

`HUD.Keys` → `ToggleStyle` binds a key to switch between the White Hilt HUD and the game's own, and `ShowAll` one that shows the whole console while held, with `Calm`. Both are unbound at first.

With the White Hilt HUD the label beside the hotbar that tells the travel bar from the build bar is left out, as the food is there; the message when the bar switches still shows. The game's own bars, food and status list are hidden by size, not switched off, so the game and other mods go on updating them, and they come back as they were with `Vanilla`.

### For mod authors

Status effects need nothing: every effect the game shows on its HUD is in the console. For anything else, `BrudvikWhiteHilt.HudLayout.WhiteHiltHud` takes a context line for the strip or an indicator among the status effects. Both are read every frame while the White Hilt HUD shows; return null for nothing.

```csharp
// A line in the strip over the console; higher priority first, at most three lines. The ship read-out is 100.
WhiteHiltHud.RegisterContext("mymod.fishing", 50, () => fishing ? $"Line {depth:0} m · {bites} bites" : null);

// An icon among the status effects, after them.
WhiteHiltHud.RegisterIndicator("mymod.quiver", 0, () => new HudIndicator { Icon = quiverIcon, Text = arrows.ToString() });

WhiteHiltHud.Unregister("mymod.fishing");
```

`WhiteHiltHud.Active` says whether the player sees the White Hilt HUD. A function that throws is logged and removed, so it cannot break the HUD.

---

## 🕰️ CLOCK

The time of day in 24 hours at the upper left above the weapon/tool hotbar, e.g. **Day 42 · 14:37**, with an icon for the weather (sun, moon, clouds, rain, storm, snow, mist or ash). The hotbar moves down only as far as needed to leave room for the clock, and returns to its original position when the clock is hidden. Sunrise is 06:00 and night falls at 18:00, as the game's own day and night. Two in-game hours before dark a warning shows and the clock turns orange until night falls. The clock is hidden in the inventory, in build mode, on the large map, in menus and with the HUD.

The `Clock` section sets whether it shows (`Enabled`), the day number (`ShowDay`), the weather icon (`ShowWeather`), rounding (`RoundMinutes`, 1 = every minute), the warning (`DuskWarningHours`, 0 = off), `FontSize` and `OffsetY`; `Clock.Keys` → `ToggleClock` binds a key to show and hide it. A server can turn the clock off for everyone with `AllowClock`.

With the [White Hilt HUD](#-white-hilt-hud) the clock sits at the left end of the compass instead, and the hotbar is at the bottom. The [horizontal HUD compass](navigation.md#horizontal-hud-compass) and boss bars remain at the top centre and do not move the clock. `OffsetY` controls the clock's distance from the top edge independently of the compass.

---

## 🛡️ PARTY LIST

The other players in a compact list at the upper left, under the hotbar and the arrow count shown under a bow (with the [White Hilt HUD](#-white-hilt-hud), at the left a third of the way down), nearest first. Each row has the player's portrait (the one taken for the [map](navigation.md), or their initial on a disc), their name, how far away they are and up to four status effects, burning, poison and frost first. Under that come health and stamina as bars with numbers, `184/250`, and eitr for players who have it. A row flashes when its player takes a hard hit, the name turns red and the bar pulses under a quarter of the health, and a dead player's row fades until they respawn. The distance is shown only when the player is near or shares their position on the map. The list is hidden in the inventory, in build mode, on the large map, in menus and with the HUD.

Under the players come your companions: your own [dog](dog.md) and every tamed animal following you, nearest first, with name, distance and health; the name turns red under a quarter. They show while they are loaded, near enough for the game to have them.

Each player sends their own health, stamina and eitr to the others, at most twice a second and only when something changed, so the list works however far apart you are.

The `Party` section sets whether it shows (`Enabled`), `ShowStamina`, `ShowEitr`, `ShowDistance`, `ShowEffects`, `ShowSelf` (off), `MaxPlayers` (8), `ShowCompanions` (on), `MaxCompanions` (6) and `Scale`; `Party.Keys` → `ToggleParty` binds a key to show and hide it. A server can turn it off for everyone with `AllowParty`, and then nothing is shared.

---

## 🔊 INDOOR SOUND

Wind, rain, sea and thunder are quieter and muffled under a roof, as heard through walls, so a house is a quiet place to cook and craft in. How much follows how well you are covered: a little under an open roof, most in a closed house (the game's own shelter), and some under the tent of the White Hilt Ship. Under a turf, slate, reed or straw roof (see [Roofs](roofs.md)) it is quieter still (`[Roofs] QuietRoofBonus`). Only the weather's sound changes; fires, cooking, crafting and doors sound as before. Dungeons are left alone.

The `Sound` section sets it: `IndoorSound` (on/off), `IndoorWindVolume` (0.25) and `IndoorRainVolume` (0.3) inside a closed house, `IndoorMuffle` with `MuffleCutoffHz` (1200, lower is more muffled), `FadeSeconds` (1.5) and `ShipTentCounts`.

---

## 🌊 FLOATING ITEMS

Everything dropped floats in water instead of sinking, so a Serpent killed at sea leaves its trophy and meat bobbing on the surface, and nothing is lost when a fight at sea goes overboard. Items that already lay on the sea floor come up when their area loads. The server sets it in the `FloatingItems` section: `Enabled` (on by default) and `SinkingItems`, a comma-separated list of prefab names that still sink (e.g. `Stone,Flint`).
