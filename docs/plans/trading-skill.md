# 🪙 Plan: the Trading skill and each post's own gear

Plan for **Trading** (*Handel*), agreed with the user on 2026-10-14. It builds on the trading posts
([trading-posts.md](trading-posts.md)). Written for an AI agent that carries it out without the conversation behind it.

## 1. What the player gets

- **A Trading skill.** It rises with every trade at a trading post, buying and selling (1 point per 5 coins,
  `ExperiencePerCoins`), and with the pay of an escort.
- **Better paid.** A post pays a share of its own price that grows with the skill: 60 % at level 0 (`BuyShare`) to
  90 % at 100 (`MaxBuyShare`) (25 % to 40 % before v0.135.0).
- **A discount.** What a post sells costs less as the skill grows: up to 10 % off at 100 (`BuyDiscount`).
- **Each post's own gear.** Every biome has a pool of gear: the weapons, armour, capes, shields, bows and tools whose
  recipes use that biome's materials and no later biome's (the biome rule, found from the game's own recipes, so new
  items from game updates come along; White Hilt gear is left out). The pool is shared out among the biome's posts so
  that **every item is sold at one post only**: to have them all you travel. Sold at quality 1 for the recipe's
  materials at the posts' prices times `GearPriceFactor` (1.5), less the discount.
- **Unlocked by the skill.** The gear opens in four steps by price, at Trading **20, 40, 60 and 80**
  (`UnlockLevels`), the cheapest first. Locked gear shows in the store, greyed out, with the level it needs; it cannot
  be bought. The biome's boss key applies as for the other goods.
- **Known for.** A post is known for what most of its gear is (weapons, bows, armour, shields or tools): the trader's
  hover text says so, and once you have traded there the post's pin on your map does too.

## 2. Decisions

| Decision | Choice |
|---|---|
| Gear pools | Found from the recipes at start, by the latest biome among the materials (`TradingPostGoods` knows the raw ones, a table the refined ones) |
| Sharing out | A post's place is its rank among the biome's posts by id; the server, which knows every marker, tells a reader on request (RPC, nothing written to the network data). Item *j* of the biome's pool, by price, goes to place *j* mod count |
| Locked gear | Shown greyed out with *(Trading n)*, not buyable (`StoreGui.CanAfford` says no) |
| Prices per player | The trader's goods are made again for the local player each time the store opens, with the player's discount |
| Skill icon | The coins' |

## 3. Settings (`[TradingPosts]`)

`MaxBuyShare` (0.4), `BuyDiscount` (0.1), `UnlockLevels` (`20,40,60,80`), `GearPriceFactor` (1.5),
`ExperiencePerCoins` (5).

## 4. Log

- 2026-10-14: plan written and agreed (locked gear greyed out, the discount, all defaults, both extras).
- 2026-10-14, done (not yet tried in the game):
  - `TradingGear` (pure, `TradingGearTests`): a material's biome and worth (the posts' goods, and a table of refined
    metals, trophies and creature parts), a recipe's biome (its latest material; unknown material: not sold) and price,
    the sharing out, the unlock levels, share, discount, experience and specialty.
  - `ExperiencePerCoins` is 5, not 50: the game's skill curve needs about 20 000 points to 100, so 50 coins a point
    would need a million coins; at 5, Trading 20 takes about 1 800 coins and 80 about 57 000.
  - `TradingPostGear` finds the pools from `ObjectDB.m_recipes` (one-handed, two-handed, bows and crossbows, shields,
    helmets, chests, legs, capes, tools; not White Hilt's, not DLC). `TradingPostService.RequestSlot` asks the server
    for a post's place when its trader loads.
  - `TradingPostStore.BuildItems` makes the store again for the local player when it opens (`StoreGui.Show` prefix):
    their discount, the post's gear, the locked pieces in a list `ShowLocked` greys out (`FillList` postfix), and
    `CanAfford` says no to them. Trading rises in `Trader.OnBought`, the post's sell, and the escort's pay.
  - The trader's hover says *Known for …*; opening the store renames the nearest trading post pin (or adds one).
  - To check in the game: the gear lists and prices at each post, the greyed entries, the pin, the skill's icon.
