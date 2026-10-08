# 🕰️ Clock, party list, sound & floating items

[← Back to the README](../README.MD)

## 🕰️ CLOCK

The time of day in 24 hours at the upper left above the weapon/tool hotbar, e.g. **Day 42 · 14:37**, with an icon for the weather (sun, moon, clouds, rain, storm, snow, mist or ash). The hotbar moves down only as far as needed to leave room for the clock, and returns to its original position when the clock is hidden. Sunrise is 06:00 and night falls at 18:00, as the game's own day and night. Two in-game hours before dark a warning shows and the clock turns orange until night falls. The clock is hidden in the inventory, in build mode, on the large map, in menus and with the HUD.

The `Clock` section sets whether it shows (`Enabled`), the day number (`ShowDay`), the weather icon (`ShowWeather`), rounding (`RoundMinutes`, 1 = every minute), the warning (`DuskWarningHours`, 0 = off), `FontSize` and `OffsetY`; `Clock.Keys` → `ToggleClock` binds a key to show and hide it. A server can turn the clock off for everyone with `AllowClock`.

The [horizontal HUD compass](navigation.md#horizontal-hud-compass) and boss bars remain at the top centre and do not move the clock. `OffsetY` controls the clock's distance from the top edge independently of the compass.

---

## 🛡️ PARTY LIST

The other players in a compact list at the upper left, under the hotbar and the arrow count shown under a bow, nearest first. Each row has the player's portrait (the one taken for the [map](navigation.md), or their initial on a disc), their name, how far away they are and up to four status effects, burning, poison and frost first. Under that come health and stamina as bars with numbers, `184/250`, and eitr for players who have it. A row flashes when its player takes a hard hit, the name turns red and the bar pulses under a quarter of the health, and a dead player's row fades until they respawn. The distance is shown only when the player is near or shares their position on the map. The list is hidden in the inventory, in build mode, on the large map, in menus and with the HUD.

Each player sends their own health, stamina and eitr to the others, at most twice a second and only when something changed, so the list works however far apart you are.

The `Party` section sets whether it shows (`Enabled`), `ShowStamina`, `ShowEitr`, `ShowDistance`, `ShowEffects`, `ShowSelf` (off), `MaxPlayers` (8) and `Scale`; `Party.Keys` → `ToggleParty` binds a key to show and hide it. A server can turn it off for everyone with `AllowParty`, and then nothing is shared.

---

## 🔊 INDOOR SOUND

Wind, rain, sea and thunder are quieter and muffled under a roof, as heard through walls, so a house is a quiet place to cook and craft in. How much follows how well you are covered: a little under an open roof, most in a closed house (the game's own shelter), and some under the tent of the White Hilt Ship. Under a turf, slate, reed or straw roof (see [Roofs](roofs.md)) it is quieter still (`[Roofs] QuietRoofBonus`). Only the weather's sound changes; fires, cooking, crafting and doors sound as before. Dungeons are left alone.

The `Sound` section sets it: `IndoorSound` (on/off), `IndoorWindVolume` (0.25) and `IndoorRainVolume` (0.3) inside a closed house, `IndoorMuffle` with `MuffleCutoffHz` (1200, lower is more muffled), `FadeSeconds` (1.5) and `ShipTentCounts`.

---

## 🌊 FLOATING ITEMS

Everything dropped floats in water instead of sinking, so a Serpent killed at sea leaves its trophy and meat bobbing on the surface, and nothing is lost when a fight at sea goes overboard. Items that already lay on the sea floor come up when their area loads. The server sets it in the `FloatingItems` section: `Enabled` (on by default) and `SinkingItems`, a comma-separated list of prefab names that still sink (e.g. `Stone,Flint`).
