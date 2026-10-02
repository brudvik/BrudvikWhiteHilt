# 📦 Restocking chests

[← Back to the README](../README.MD)

The chests from the former **BrudvikStackedChest** mod are now part of White Hilt. Chests placed with the old mod keep their place and their contents, see [Moving over from BrudvikStackedChest](#moving-over-from-brudvikstackedchest).

## 📦 CHESTS

Fourteen chests that fill themselves with their kind of items and refill them to a full stack as soon as you take some out, use them from the chest, or build or craft from them with [nearby chests](base.md). Each chest has its own colour, icon and glow.

### Building a chest

All chests are built with the **Hammer**, in the **Chests** category.

| Resource | Amount | Recoverable |
|----------|--------|-------------|
| Wood     | 10     | Yes         |

### Available chests

The contents of every chest are worked out when a world loads. Every item in the game is sorted by its type and by where it comes from, including White Hilt items and items from other mods.

| Chest | Colour | Contents |
|-------|--------|----------|
| Wood Chest | Black | Everything that drops from trees and logs (wood, resin, bark), plus Coal |
| Stone Chest | Grey | Everything mined from rocks and deposits (stone, flint, obsidian, crystal, sulfur), plus what is crafted from stone only |
| Metal Chest | Dark red | Ores, scrap and bars, plus what is crafted from metal only (nails) and Chain |
| Food Chest | Brown | All food, fish, raw and uncooked ingredients, and anything used in a food recipe (10 rows) |
| Material Chest | Dark blue | All remaining crafting materials, including Surtling Core, Ectoplasm, Flax, casts and moulds |
| Animal Chest | Yellow | Materials dropped by creatures (hides, bones, scales, feathers, leather scraps…) |
| Seed Chest | Green | Seeds, cones and nuts that are planted but not used as ingredients |
| Trophy Chest | Teal | All trophies |
| Treasure Chest | Gold | Items with a trade value (Coins, Amber, Ruby…), gemstones, keys, eggs and boss rewards |
| Tools Chest | Purple | Pickaxes, building and farming tools, fishing rods and bait, torches, lanterns, saddles and other gear |
| Armor Chest | Brown | All helmets, chest pieces, legs, capes and trinkets |
| Weapon Chest | Red | All weapons, shields, arrows, bolts and bombs (10 rows) |
| Potion Chest | Pink | All meads and mead bases |
| Everlasting Chest | Dark grey | Empty: put in your own items and they are restocked |

Items that cannot be obtained in normal play (creature attacks, test items, unused variants) are left out. `DumpItemLists` writes every chest's list to the BepInEx log.

### Restocking

- Every unlimited item takes one full stack. When one recipe or build piece needs more than a stack (more than 50 wood, say), the chest keeps enough full stacks for it.
- Putting an item into a chest that already holds it without limit (ctrl-click, drag and drop or **Place stacks**) takes it out of your inventory instead of making another stack. **Place stacks** is a quick way to empty your pockets.
- **Take all** only takes what you stored yourself and leaves the unlimited stacks.
- Chests keep their contents sorted from the top left: unlimited items first, then what you stored yourself, each by type and name (`SortContents`).
- Items with skill stars (see [Skills & milestones](skills.md)) or their own data, such as a dog's remains with its name, are always yours: they are never refilled, merged, swallowed or deleted.

### Removing a chest

- The unlimited stacks are deleted, not dropped, so a chest can be moved without duplicating anything.
- Items you stored yourself are dropped like from any other chest. In Full mode ordinary stackable items always count as unlimited and are deleted too; gear that a player crafted or upgraded and items with stars or their own data are dropped.

> **Warning**: in Full mode, ordinary stackable items stored in the Everlasting Chest are lost when the chest is removed.

### Carts and ships

Carts and ship holds keep unlimited items full like the Everlasting Chest, so what you bring along never runs out. Put Wood in the Longship when Wood is unlimited, and the stack stays full while you build from the hold at an outpost with [nearby chests](base.md). This covers every cart and ship, including the White Hilt Ship's sea chest (`UnlimitedCargo`).

- Any amount of an unlimited item is filled up to a full stack. Extra stacks of it are removed, which frees the slots.
- Only items that stack are refilled. Which items are unlimited follows `Mode`, as in the Everlasting Chest. In Full mode, that is every ordinary stackable item.
- Cargo never unlocks items in Linear mode and never adds items you did not bring. It is not sorted and does not grow.
- Putting in more of an unlimited item, **Take all** and destroying the cart or ship work as in the chests: the item is swallowed, the unlimited stacks stay behind, and they are deleted instead of dropped.
- Items with skill stars or their own data are never refilled or deleted.

### Chest modes

`Mode` decides how generous the chests are:

| Mode | Behaviour |
|------|-----------|
| Full | Every item is always available |
| Linear | A chest works like a normal chest until it holds a full stack of an item (`UnlockStacks`). From then on the item is unlimited in every chest of that type in the world |
| Discovered | An item is unlimited as soon as any player in the world has discovered it |

In Linear and Discovered:

- Unlocked and discovered items are shared by everyone in the world and saved by the server in `BepInEx/config/BrudvikStackedChest/`.
- Items that do not stack, like weapons and armor, are never duplicated; those chests work as normal chests.
- The Everlasting Chest makes any stackable item unlimited once it holds a full stack (Linear) or once it is discovered (Discovered).

Switching to a less generous mode removes the unlimited items the new mode no longer supplies, for example everything that is not unlocked when going from Full to Linear.

### Seeing your progress

- Unlimited stacks show a gold **∞** instead of their amount.
- In Linear mode, items that can still be unlocked get a blue bar under the slot that fills up as you store more.
- The item tooltip says whether the item is unlimited, how many more you need to store, or why it is stored normally.
- The chest title and hover text show how many of the chest's items are unlimited, and the hover text lists the three items closest to being unlocked.
- Everyone on the server is told when a player makes an item unlimited.
- A chest glows brighter the more of its items are unlimited, turns gold when all of them are, and sparkles when it refills or unlocks something.
- **On the front of a chest** the icon turns grey when it is empty, with a bar for the used slots and, in Linear and Discovered, a gold bar for the unlimited items (`ShowIndicators`).
- **Looking at a chest** shows its contents as item icons below the crosshair (`ShowHoverPanel`).

### Learning items

- Items in an open chest that you have not learned yet are marked with a yellow **!**.
- **Learn all** next to Place stacks teaches you every item in the chest at once, as if you had picked them up, so their recipes and build pieces unlock (`LearnAll`, `LearnTrophies`).

### Gathering progress

The player screen (Tab) has an extra button next to Skills, Compendium, Trophies and Achievements. It lists what can be gathered in the biome you are in: what trees, rocks, plants and creatures drop and what is found in its dungeons. In Linear mode a bar shows how much the closest chest in the world holds and how much is missing; in Discovered mode whether the item is discovered. The arrows browse the other biomes you have visited.

## ⚙️ Settings

All in the `Chests` section of the White Hilt settings. `Display` settings are each player's own; the rest is the server's.

| Section | Setting | Description |
|---------|---------|-------------|
| Chests | Mode | Full, Linear or Discovered (default Full) |
| Chests | UnlockStacks | Linear: full stacks a chest must hold to unlock an item (1–10, default 1) |
| Chests | SortContents | Keep the contents sorted from the top left (default on) |
| Chests | LearnAll | Show the Learn all button (default on) |
| Chests | LearnTrophies | Let Learn all learn trophies too (default on) |
| Chests | UnlimitedCargo | Carts and ship holds keep unlimited items full (default on) |
| Chests | ShowIndicators | Icon and bars on the front of chests (default on, per player) |
| Chests | ShowHoverPanel | Contents as icons when looking at a chest (default on, per player) |
| Chests | DumpItemLists | Write every chest's item list to the log (per player) |
| Chests.&lt;Chest&gt; | Include | Comma-separated prefab names always placed in this chest |
| Chests.&lt;Chest&gt; | Exclude | Comma-separated prefab names never placed in this chest |

## 💬 Console commands

| Command | Description |
|---------|-------------|
| `whitehilt_chest_progress` | Lists how many items of each chest are unlimited (was `bsc_progress`) |
| `whitehilt_chest_census` | Counts the contents of every chest and compares them with the previous count (server or host) |

## 🔁 Moving over from BrudvikStackedChest

The chests keep their old prefab names, size and saved data, and the server keeps its progress files in `BepInEx/config/BrudvikStackedChest/`, so placed chests and their contents carry over.

1. Back up the world (`worlds_local/<world>.db` and `.fwl`, on the server too).
2. Install this version of White Hilt on the server and every client **while BrudvikStackedChest is still installed**. As long as the old mod is loaded, White Hilt leaves the chests to it and only counts them: at every world start the server writes a census to `BepInEx/config/BrudvikStackedChest/census/`.
3. Remove BrudvikStackedChest everywhere, but **keep** `BepInEx/config/com.jotunn.BrudvikStackedChest.cfg` and the `BepInEx/config/BrudvikStackedChest/` folder. On the first start White Hilt takes over the old settings, above all `Mode`, and logs each value it took over. A wrong mode would make the chests remove the stacks they no longer supply.
4. Start the server. The census at the start compares the chests with the previous count and logs any chest that is gone or any item that decreased. Visit some chests and restart once more to check again; taking items out also counts as a decrease.
