# 🔥 Around the base

[← Back to the README](../README.MD)

## 🔥 ETERNAL FIRE

**Surt's Brazier** is a workbench extension: an iron brazier holding an ember of Surt. While one burns anywhere in the world, every campfire, hearth, torch, sconce, brazier, jack-o-turnip, hot tub and stone oven burns without fuel and stays lit in rain and wind. Hovering over them shows *Eternal fire*. When the last brazier is torn down, the fires are full and burn down as usual.

The config (`EternalFire` section) decides when it applies: `Progression` (default) needs the brazier in linear progression and is always on in full progression, `Always` needs no brazier, and `Off` switches it off. Fires, ovens and smelters can each be switched on or off, and single prefabs excluded. Smelters and blast furnaces are **off** by default, so coal stays part of smelting; they take coal from nearby chests instead (see below).

<img src="images/surts_brazier.png" alt="Surt's Brazier" title="Surt's Brazier" height="140">

| Item | Description | Crafting Station | Requirements |
|------|-------------|------------------|--------------|
| **Surt's Brazier** | Workbench extension; eternal fire across the world | Hammer (next to a Workbench) | Stone ×10, Bronze ×4, Surtling Core ×3, Resin ×5 |

---

## 📦 CRAFTING FROM CHESTS

What lies in the chests, carts and ship holds within 30 m counts as your own when you **craft**, **build**, **fuel** or **smelt**, and **cook**. Requirements that are partly in chests show their amount in amber, with a small chest on the icon; the tooltip shows how many are in your inventory and how many in chests. A chest that something is taken from opens its lid and glows briefly.

- Adding fuel or ore to a smelter, kiln, fire or oven takes one from the chests when your inventory has none. Hold **Shift** to fill it up in one go, first from your inventory, then from the chests.
- Using a cooking station without raw food in your inventory puts raw food from the chests on it; with Shift, on every free slot.
- Only chests you may open are used: private chests of others and chests inside someone else's ward are skipped, and so are chests another player has open.
- **Left Alt + O** switches it off and on for you.

The `ChestCrafting` config section sets the range (`Range` for crafting, fuel and cooking, `BuildRange` for building, both 30 m), whether to leave one of each item in a chest, which of the four uses are on, and comma-separated lists of containers and items never to take from.

---

## 🧾 CRAFTING PANEL

The crafting panel and the build menu show what you have, not only what a recipe costs.

- **What you have**: a small dark box on the left of each requirement's icon shows how many you have, in your inventory and in the chests you may use around you (white when it is enough, red when it is not). Large amounts are shortened, e.g. `1.2k`.
- **∞**: shown in gold when a restocking chest, cart or ship hold within reach keeps the item unlimited, so it never runs out here.
- **On the way to unlimited**: in Linear chest mode, a thin gold bar along the bottom of the icon fills up as the best chest in the world gets closer to unlocking the item.
- **Tooltip**: shows the split (*You have 14: 6 in your inventory + 8 in chests*) and either *Unlimited from a chest nearby*, *Unlimited in the Wood Chest, but none is nearby*, or *Unlimited after 12 more (38/50 in the best chest)*.
- **Craft several at once**: arrows on the left of the Craft button choose how many to make, e.g. 4 axes. The mouse wheel over the number works too. The requirements show the cost for all of them, and the button reads *Craft x 4*. It starts at 1 for every recipe and is not used for upgrades. With 1 chosen, Shift + Craft still makes five like in vanilla.

| Setting (`CraftingPanel`) | Default | Description |
|---|---|---|
| `ShowAvailable` | on | The box with what you have (each player) |
| `ShowInBuildMenu` | on | The same in the build menu (each player) |
| `ShowUnlockProgress` | on | The gold bar towards unlimited (each player) |
| `AmountSelector` | on | The arrows next to the Craft button (each player) |
| `MaxCraftAmount` | 20 | The most that can be crafted at once (server) |

---

## 🗑️ WASTE WELL

<img src="images/waste_well.png" alt="Waste Well" title="Waste Well" height="140">

| **Piece** | Description | Crafting Station | Requirements |
|---|---|---|---|
| **Waste Well** | A stone well for rubbish. Open it (E) and throw things in: 5 seconds after it is closed they are gone, so opening it again before then gets them back. Shift + Use makes the well collect items that have lain on the ground within 8 m for 30 seconds or more; it never takes hatching eggs or items placed as decorations. Chest crafting never takes from it | Workbench | Stone ×20, Wood ×6 |

The server sets it in the `WasteWell` section: `DisposeDelaySeconds` (5), `CollectRadius` (8), `MinItemAgeSeconds` (30) and `KeepItems`, prefab names never collected from the ground.
