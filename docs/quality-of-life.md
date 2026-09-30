# 🕰️ Clock, sound & floating items

[← Back to the README](../README.MD)

## 🕰️ CLOCK

The time of day in 24 hours at the top of the screen, e.g. **Day 42 · 14:37**, with an icon for the weather (sun, moon, clouds, rain, storm, snow, mist or ash). Sunrise is 06:00 and night falls at 18:00, as the game's own day and night. Two in-game hours before dark a warning shows and the clock turns orange until night falls. The clock is hidden in the inventory, in build mode, on the large map, in menus and with the HUD; during a boss fight it moves below the boss's health bar.

The `Clock` section sets whether it shows (`Enabled`), the day number (`ShowDay`), the weather icon (`ShowWeather`), rounding (`RoundMinutes`, 1 = every minute), the warning (`DuskWarningHours`, 0 = off), `FontSize` and `OffsetY`; `Clock.Keys` → `ToggleClock` binds a key to show and hide it. A server can turn the clock off for everyone with `AllowClock`.

---

## 🔊 INDOOR SOUND

Wind, rain, sea and thunder are quieter and muffled under a roof, as heard through walls, so a house is a quiet place to cook and craft in. How much follows how well you are covered: a little under an open roof, most in a closed house (the game's own shelter), and some under the tent of the White Hilt Ship. Only the weather's sound changes; fires, cooking, crafting and doors sound as before. Dungeons are left alone.

The `Sound` section sets it: `IndoorSound` (on/off), `IndoorWindVolume` (0.25) and `IndoorRainVolume` (0.3) inside a closed house, `IndoorMuffle` with `MuffleCutoffHz` (1200, lower is more muffled), `FadeSeconds` (1.5) and `ShipTentCounts`.

---

## 🌊 FLOATING ITEMS

Everything dropped floats in water instead of sinking, so a Serpent killed at sea leaves its trophy and meat bobbing on the surface, and nothing is lost when a fight at sea goes overboard. Items that already lay on the sea floor come up when their area loads. The server sets it in the `FloatingItems` section: `Enabled` (on by default) and `SinkingItems`, a comma-separated list of prefab names that still sink (e.g. `Stone,Flint`).
