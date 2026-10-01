# ⏳ Production timers

[← Back to the README](../README.MD)

Every station that works on its own tells how long it has left, whether its fuel lasts, and why it has stopped. Times are real time (`45s`, `3m 05s`, `2h 10m`) and follow the White Hilt bonuses: faster cooking near a skilled cook, shorter pregnancies near a skilled keeper.

## Stations

| Station | Shows |
|---|---|
| **Smelter, blast furnace, charcoal kiln, spinning wheel, windmill, eitr refinery** | Next item, the whole queue, whether the fuel lasts the queue and how much more is needed. Stopped: no fuel, no roof, smoke blocked, no wind (windmill times follow the wind) |
| **Fermenter** | Time until ready; that the timer starts over while it is uncovered |
| **Cooking station, iron cooking station, stone oven** | Next item and how many are cooking, how many are ready, when the food burns (red), that a watchful cook keeps it from burning, how long the oven's wood lasts. Stopped: no fire or no fuel |
| **Beehive** | Next honey and when it is full. Stopped: wrong biome, too much cover |
| **Sap collector** | Next sap and when it is full. Stopped: not connected, root drained |
| **Fires, braziers, torches** (built by players) | How long the fuel lasts; out or wet. Eternal fires show nothing |
| **Egg** | Time until it hatches. Stopped: in a stack, needs warmth, needs a roof |
| **Tame animal** | Time until it gives birth; otherwise love points and what holds it back: hungry, too many animals near, no partner. Not the dog |

## Hover text

Look at a station: the timer lines are added under the vanilla text.

## Labels

Small labels float over the working stations within 15 m: time left in green, ready or full in blue, stopped in red. Food about to burn turns the label red. Fires only get a label when they have less than 5 minutes of fuel left or have gone out.

## Overview

**K** opens and closes a list of the stations within 60 m at the right side of the screen, with their state and distance: stopped ones first, then the ones that need you soon, then ready ones, then the working ones, soonest done first. It does not take the mouse, so you can walk around with it open. It does not open in build mode.

## Messages

A message at the top left when a station within 40 m finishes, fills up, gives birth, or stops while it was working, and once when food is about to burn (20 seconds before). A station is only watched once you are near it, so walking up to one sends nothing.

## Config

Section `Production`, each player's own:

| Setting | Default | |
|---|---|---|
| `ShowHover` | on | Timers in the hover text |
| `ShowLabels` | on | Labels over the stations |
| `LabelRange` | 15 | Metres |
| `OverviewRange` | 60 | Metres |
| `KeyOverview` | K | Opens and closes the overview |
| `Notify` | on | Messages |
| `NotifyRange` | 40 | Metres |

Section `Production.Stations` turns each kind on and off everywhere: `Smelters`, `Fermenters`, `Cooking`, `Beehives`, `SapCollectors`, `Fires`, `Eggs`, `Animals`.
