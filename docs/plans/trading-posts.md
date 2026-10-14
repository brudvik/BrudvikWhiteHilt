# ⚓ Plan: trading posts by the sea

Plan for **trading posts**: small harbours along the coast, each with a jetty, a stall and a trader of its own. You
sail there, tie up, walk up the jetty and buy what the land around you has to offer, so you do not have to search
for it for hours. You sell goods from another land there for coins, and every post buys fish. It builds on the
jetties and quays ([harbour.md](../harbour.md)), the Decor Hammer's models ([decor.md](../decor.md)), the portraits
(a hidden copy of the player body, `Navigation/Portraits/PortraitCapture.cs`), the treasure maps (server-picked
places, a map bought from Hildir, [treasure.md](../treasure.md)) and the Valuables chest
([chests.md](../chests.md)).

The work is done in **three rounds agreed with the user before any code is written**, then built phase by phase:

1. **The model** of the trading post with its jetty: pictures until the user is happy (section 6, round A).
2. **The trader**: the player body in recoloured clothes, one colour scheme per biome (round B).
3. **Coding** (phases 1–7) only once both are agreed.

Written for an AI agent that carries it out phase by phase without the conversation behind it. Read all of it once
before you start.

## 1. What the player gets

- **Trading posts along the coast.** 3–4 in each biome that meets the sea, placed once per world by the server: a
  long jetty out to water deep enough for a longship, with mooring posts, cargo and a flag at its head; a gate with a
  sign where it meets a wide landing; two stalls under awnings (the land's goods and the fish), stock, a harbour crane;
  the trader's small laft house behind, and a fire with benches. Lanterns light it at night, so it is found from the
  sea.
- **The trader.** A man or woman in the player's own body, in clothes dyed in the colours of the biome (the stall's
  awning and the tent share them). He greets you, talks now and then and waves you goodbye.
- **Buying.** Each post sells the **materials of its own biome**: what is grown, mined, hunted or found there, never a
  boss drop or a key material of a later tier. Prices are high enough that gathering it yourself stays the cheap way.
- **Selling.** Each post buys the goods of one **linked biome** (another biome than its own, see section 4) and
  **fish from every biome**. Prices are a fraction of what the post asks when it sells the same thing.
- **A map from Hildir.** Hildir sells a **Trading Post Chart** for each biome. Reading it puts pins on your map for
  every trading post of that biome. A biome's chart is on her list once the world has reached that biome.
- **A safe harbour.** A dome like the one over Haldor's camp covers the post, its jetty and a ship tied up there:
  no monster comes inside, so you trade in peace.
- **Escorts start here.** The Escort Contract is sold by the traders at the posts instead of by Haldor. You buy it,
  start it at the post, and the trading ship comes in to the post on the far side of the jetty from your own ship,
  or at least some way from it, and waits. The escort only really begins when you go aboard your own ship: then the
  merchant casts off, its destination is set and the Nail Ships are on their way.
- **Coins mean something.** In linear progression coins no longer become unlimited in the chests (section 3). Coins
  are earned by trade, escorts, raids and treasure, and spent on goods.

## 2. Decisions already made

| Decision | Choice | Why |
|---|---|---|
| Where a post stands | At the coast: the land end of the jetty on the bank, the jetty out over water at least 2.5 m deep at its head | You sail there and tie up; the user's wish |
| How it is built | Out of the mod's own pieces and models: Jetty, Jetty Head, Mooring Post, Harbour Crane, the laft house pieces, the Decor Hammer's Market Stall, crates, barrels, sacks, tables, lanterns, the fish rack, the fire pit and benches, plus a pennant made for it | Reuses pieces players already know; the user's wish |
| The trader's house | A small laft house (the `lafthus` of the log house pieces, 6 × 4 m) on the land behind the stalls, its door facing the jetty down a path between the stalls; not a tent | The user's wish (round A) |
| The jetty | 16 m out from the landing plus the head; the place is only chosen where the water at the head is deep enough for a longship (section 6, phase 3). Where the bed lies deeper than the jetty pieces' 3 m piles, extra piles go down to it | The user's wish (round A); the piles must not hang in the water |
| Mooring | Two of our own Mooring Posts along the jetty, where a ship lies alongside | The user's wish (round A) |
| What it is in the world | A **location the server places once per world** (like the treasures), not a Jotunn location from world generation, so it also comes to worlds made before the mod | Worlds already played get posts too |
| Protection | Each post gets a copy of the `ForceField` of Haldor's location (`Vendor_BlackForest`): the dome mesh and its child `NoMonsterArea` (an `EffectArea` of the no-monsters type with a sphere collider), centred on the landing and scaled so it covers the house, the landing, the whole jetty and a ship alongside (about 26 m radius; Haldor's is scaled 25). Cloned from the vanilla prefab at start-up, not rebuilt | The user's wish: no attacks while trading; the same look and rule players know from the vanilla traders |
| Escorts | The Escort Contract moves from Haldor to the trading posts (section 4a); Haldor keeps selling it only while `[TradingPosts] Enabled` is off | The user's wish: more reasons to visit a post |
| Who may change it | Its pieces cannot be removed with the hammer, burnt or broken; the trader cannot be hurt | A post must not vanish under a careless player |
| The trader | Not a `Character`: a player body (`Player` prefab's visual and `VisEquipment`, without `Player`) with the vanilla `Trader` component for the store window | Monsters leave him alone, like Haldor; the store window is the game's own |
| His look | Stored in his ZDO (body, hair, beard, skin and hair colour, the hashes of his clothes), so every client draws the same man | Synced by the game's own `VisEquipment` |
| The traders' looks | One look per biome, as agreed in round B (`AssetSource/Preview/build_traders.py`, `TRADERS`): body, hair, beard, hair colour, tunic cut, cape and colours. Every post of a biome has the same trader | Agreed with the user (round B) |
| Biome colours | One colour scheme per biome for the awning, the tent and the trader's clothes, made with `VisualHelper.Recolor`; the clothes are NPC-only item prefabs (never crafted, never sold) so their hashes sync | Robust: no per-instance material work |
| Linked biomes and goods | Section 4; to be agreed with the user before phase 4 | Balance |
| Unlimited coins | A new setting, off by default in linear progression (section 3) | The user's wish: coins should be worth working for |
| Unlimited goods | A setting `[TradingPosts] BuyUnlimitedGoods`, **on by default**: the posts pay for an item even when it is unlimited in the world's chests. Off: they pay nothing for it ("the market is full of it") | The user's wish: otherwise coins would be hard to earn; a server that wants the stricter economy turns it off |
| Biome rule | A post sells only its own biome's materials and earlier ones, and its trader appears with the world: posts of a biome open once the world has reached that biome (same keys as the tiers) | [Biome rule](../../.github/copilot-instructions.md) |
| Release | Changelog under `## Unreleased`; new pieces, items and RPCs make it a MINOR bump | Copilot instructions |

## 3. Coins are no longer unlimited by default

Today the Treasure chest (`ChestCategory.Treasure`: coins, gems and everything with a trade value) unlocks like
every other chest in `ChestMode.Linear`: store a full stack and the item is unlimited for the whole world. A full
stack of coins makes money meaningless.

- New admin-only setting `[Chests] UnlimitedValuables` with the values `Auto` (default), `On` and `Off`.
  - `Auto`: off when `[Chests] Mode` is `Linear`, on in `Full` and `Discovered`.
  - `Off`: coins and the other valuables never become unlimited in any mode; full stacks of them stay ordinary items.
  - `On`: as today.
- Switching to `Off` removes the unlimited stacks of valuables from the chests, the way switching to a stricter mode
  removes what the new mode does not supply (`ChestModule`, the restock). Real coins stored in a chest stay.
- The chest hover, the Gathering panel and the requirement bars must not count valuables as unlockable while it is off.
- The posts have their own setting for unlimited goods, `[TradingPosts] BuyUnlimitedGoods` (on by default, section 2).
- Norwegian label and text in `Norwegian.json`; `cfg_keys.py --check` must pass; [chests.md](../chests.md) gets the
  setting.
- Verified: `ItemSorter` puts every item with a trade value (`m_value > 0`) in `ChestCategory.Treasure`, so coins,
  amber, rubies and the rest are there. (The plan first said `Valuables`; there is no such category.)

This is phase 1, and it can ship before the posts.

## 4. Goods and prices (draft, to be agreed before phase 4)

Each post sells the gatherable materials of its own biome. It buys the goods of one linked biome and fish. The
links pair a biome with one you reach by sea from it, so a trading voyage has a point:

| Post in | Sells (examples, the full list comes from the biome lists the Gathering panel already has) | Buys from |
|---|---|---|
| Meadows | Wood, Stone, Flint, Resin, Leather Scraps, Deer Hide, Raspberries, Honey, Feathers | Black Forest goods |
| Black Forest | Core Wood, Fine Wood, Copper Ore, Tin Ore, Troll Hide, Blueberries, Carrot Seeds, Thistle | Swamp goods |
| Swamp | Iron Scrap, Ancient Bark, Guck, Root, Bloodbag, Turnip Seeds, Entrails, Chain | Mountain goods |
| Mountain | Silver Ore, Obsidian, Freeze Gland, Wolf Pelt, Onion Seeds, Crystal | Plains goods |
| Plains | Black Metal Scrap, Flax, Barley, Linen Thread, Cloudberries, Lox Meat, Lox Pelt, Tar | Mistlands goods |
| Mistlands | Black Marble, Yggdrasil Wood, Carapace, Sap, Royal Jelly | Ashlands goods |
| Ashlands | Flametal Ore, Ashwood, Grausten, Charcoal Resin, Vineberry Seeds | Meadows goods (homecoming) |
| Every post | | All fish, raw and cooked |

- A post sells only after the world has reached its biome (its tier's key is set). An unreached biome's posts stand
  shut: the trader says he has nothing for you yet.
- Prices are in a table in code with a settings override `[TradingPosts] Prices` (`Prefab:Buy:Sell, ...`), as
  `[Recipes]` does it. Starting point: what Haldor asks for things of the same tier, and a post pays 1/4 of what it
  asks.
- Stock: unlimited by default. A setting can limit each item to a number per day.
- Fish: every post buys every fish, and pays more for fish from a later biome and for fish of higher quality.
- **Agree with the user**: the links, the full lists, the prices.

## 4a. Escorts from the trading posts

Today (see [escorts.md](escorts.md)) Haldor sells the Escort Contract, and using it aboard your own ship at open sea
calls the trading ship up beside you at once. With the trading posts:

- **Who sells it.** Every trading post's trader, once the sea battles are open to the world (the same key as today),
  for the same price. Haldor stops selling it (section 2).
- **Where it is used.** At a trading post: using the contract inside a post's dome asks the server for an escort
  from that post. Using it anywhere else gives a message saying where to go; the contract is kept, as for every refusal.
- **Where the merchant appears.** Beside the post's jetty, on the side away from your own ship: the server takes the
  side of the jetty opposite to the nearest ship the player owns or last steered within 60 m, or either side if there
  is none, and places the merchant 15–20 m off the jetty, inside the dome (26 m radius), bow to the open sea. It never appears
  within 20 m of another ship.
- **Waiting.** The merchant lies there with its sail furled (a new state, `Waiting`, in its ZDO). No route, no waves,
  no abandon timer yet. If nobody who bought it goes aboard a ship within `[SeaBattles.Escorts] WaitSeconds`
  (default 600), it sails off and the contract is given back.
- **The start.** The escort really begins when the buyer, or a player in their party, goes aboard their own ship
  (any ship that is not a trading or Nail Ship) within `StartRange` (default 150 m) of the merchant: the merchant's
  owner sets the state to `Sailing`, the server picks the destination, the route is marked on the map, the waves are
  timed and the abandon rule applies from then on, as today.
- **Where it sails.** Preferably to **another trading post** 1200–2000 m away over deep water, so an escort ends in a
  harbour; where none lies in that range, to an open-sea destination as today. **Agree with the user before phase 7.**
- **Console.** `whitehilt_seabattle escort` keeps working anywhere (admins, for testing) and starts sailing at once.
- **Code.** `EscortService.Request` checks the dome instead of open sea; `Approve` gets the post and the player's
  ship and computes the waiting place; `TradeShip` gets the waiting state and the boarding check (owner-only, a few
  times a second); `EscortContract` and `TradeShipRegistry` stay. The escort plan's section 11 says what the code
  really does today; read it first.

## 5. Names, keys and texts

| What | Name |
|---|---|
| Settings section | `[TradingPosts]`, Norwegian label `Handelssteder` |
| Pieces of the post | the existing jetty and decor prefabs, plus `piece_whitehilt_handelstelt` (the tent) |
| The trader prefab | `WhiteHilt_Trader` (one prefab; biome and look in the ZDO) |
| The NPC clothes | `WhiteHilt_TraderTunic_<Biome>`, `WhiteHilt_TraderCape_<Biome>`, ... (not craftable, not sold) |
| The charts | `WhiteHilt_TradeChart_<Biome>`, sold by Hildir |
| ZDO keys | `whitehilt_trader_biome`, `whitehilt_trader_look`, `whitehilt_post_id`, hashed once in static fields |
| Server state | the list of posts (id, biome, position, yaw) in the world's save, like the treasures |
| Console (admin) | `whitehilt_tradepost list`, `whitehilt_tradepost here [biome]` (builds one where you stand, for testing), `whitehilt_tradepost remove` |

Every English text has a Norwegian one in `Translations/Norwegian.json`.

## 6. Rounds and phases

### Round A: the model of the post (pictures only, no game code)

- A preview script `AssetSource/Preview/build_trading_post.py` puts the post together out of the jetty pieces and the
  decor models, at their sizes in the game, with water and a bank, and renders it with `render_preview.ps1`.
- The parts and their positions in it are the ones the mod will use (phase 3 reads the same layout), so what the user
  agrees to is what is built.
- Round after round with the user until it is agreed. Record the agreed layout under section 10.

### Round B: the trader (pictures only)

- The player body (`uni_Player` / `uni_PlayerFemale`, already exported for the uniform pictures) in vanilla clothes
  recoloured per biome, behind the counter. One picture per biome.
- Agree on: body, hair and beard per post (fixed, or random per post from a list), the clothes, the seven colour
  schemes.

### Phase 1: unlimited valuables (section 3)

### Phase 2: the post as a structure

- A `TradingPostLayout` in code with the agreed parts (prefab, position, yaw), generated from the preview script's
  layout file so the two stay the same.
- `TradingPostBuilder` spawns the parts at a position and yaw on the server, marks them as part of a post (ZDO key) and
  makes them indestructible and not removable (patch on `Player.RemovePiece` / `WearNTear.Damage` for pieces with the
  key: **verify** which checks vanilla offers first).
- The pennant on the flag pole as a model of its own, in the biome colours (the tent was replaced by the laft house
  in round A).
- The protective dome: the `ForceField` child of `Vendor_BlackForest` cloned once at start-up and added to each post
  (section 2). **Verify** that a cloned `EffectArea` registers itself (it does in `Awake`) and that Haldor's camp keeps
  its own.
- `whitehilt_tradepost here` builds one where you stand.

### Phase 3: placing the posts in the world

- `TradingPostService` on the server: once per world (and again when a new biome is reached, if needed) it looks
  along the coasts of each biome for places where the bank is low and flat and water 2.5 m deep lies within 20 m,
  away from player buildings (`[OldLand] BuildingDistance`) and from each other (at least 1 km), and picks 3–4 per
  biome (`[TradingPosts] PerBiome`). It uses `WorldGenerator` heights, so unloaded land can be searched, like the
  treasures.
- The jetty runs from the bank out to sea; a place is only taken when the water is at least 2.5 m deep under the
  jetty head and stays clear of rocks 6 m to the side, so a longship can lie alongside. The jetty's length may grow
  by a section or two where the bed falls slowly.
- A post is built when its zone first loads; the terrain under the bank is levelled with a `TerrainModifier`, and
  extra piles go from the jetty's piles down to the bed where it is deeper than 3 m.
- **Verify**: how the treasure service picks places and stores them, and reuse it.

### Phase 4: the trader

- `TraderRegistry` makes `WhiteHilt_Trader`: a copy of the `Player` prefab's visual with `VisEquipment`, `ZNetView`
  (persistent), the vanilla `Trader` component, a collider for hovering, without `Player`, `Rigidbody` or physics.
- `TraderLook`: writes body, hair, beard, colours and clothes into the ZDO when the post is built; `VisEquipment`
  draws them on every client.
- Animations from the player's own animator: idle, the `emote_*` triggers for greeting, talking and goodbye (patch
  the `Trader` component's animator triggers, which are made for Haldor's animator: **verify** their names), and
  `crafting` / sitting where the layout asks for it.
- The NPC clothes per biome (section 2).

### Phase 5: the store

- The post's goods (section 4) on its `Trader`, filtered by the world's progression.
- Selling: the vanilla store sells, so buying goods from the player needs our own small sell list in `StoreGui`
  (patch) or a panel like the Quartermaster's. **Decide in this phase**, with the user, after looking at what
  `StoreGui` offers.
- With `[TradingPosts] BuyUnlimitedGoods` off, items that are unlimited in the world are bought for 0, with a line
  saying so.

### Phase 6: Hildir's charts

- One chart per biome, sold by Hildir once the world has reached the biome (`TreasureRegistry` puts maps on her list
  the same way). Reading it asks the server for the posts of that biome and adds pins (one icon, the biome in the
  name). Pins are your own, so the school's map writing keeps them.

### Phase 7: escorts from the posts (section 4a)

- The contract moves from Haldor to the posts; the waiting state, the waiting place on the far side of the jetty,
  the start on boarding, the return of the contract when nobody comes, and the destination at another post.
- Update [escorts.md](escorts.md)'s log and [sea-battles.md](../sea-battles.md#escorts).

### Phase 8: docs, pictures, release

- A page `docs/trading-posts.md` with the pictures from rounds A and B, a line in `docs/README.md` and the README, the
  changelog, the setting in `chests.md`.

## 7. Multiplayer and performance notes

- The server picks places, builds posts and answers chart requests. Clients only draw.
- A post is about 40 pieces; built once, then ordinary saved pieces. The trader is one player body with cloth: fine
  for 3–4 in a biome, never more than a few in view at once.
- A dedicated server draws nothing: every look is behind `VisualHelper.IsHeadless`.

## 8. Checks after every phase

- `dotnet build`, the tests, `cfg_keys.py --check`, `EveryEnglishTextHasANorwegianOne`.

## 9. What cannot be verified without the game

- How the post looks in real water and light, and whether a longship can tie up at the head.
- The trader's animations and his clothes on the body.
- Placement on real coasts in several seeds.

## 10. Log

- 2026-10-14: plan written. Round A started with the first draft of the model.
- 2026-10-14, round A draft 1 (`AssetSource/Preview/build_trading_post.py`, picture
  `BrudvikWhiteHiltUnity/Preview/out/tradingpost.png`): a landing of six jetty sections (8 × 6 m) on the bank's edge, a
  12 m jetty straight out from its middle with a jetty head; the market stall at the land end facing the jetty, sacks
  and a barrel beside it; crates and barrels to the left; a fish rack, fish barrels and a table to the right; lantern
  posts at the jetty's foot and head; a striped A-frame tent, a handcart and logs on the land behind. Waiting for the
  user's comments.
- 2026-10-14, the user on draft 1: make more of it so it feels like arriving at a trading place; a small building
  instead of the tent; a longer jetty, and placement must make sure the water is deep enough; use our own Mooring
  Post; posts should pay for unlimited goods by default (a setting).
- 2026-10-14, round A draft 2: a landing of nine jetty sections (12 × 6 m), a 16 m jetty with its head and extra piles
  to the bed (water about 2.6 m deep at the head), a gate with a sign and lamps at the jetty's foot, two stalls (goods
  left, fish right) with a path between them to the house door, stock on the left, the harbour crane on the right
  front corner, cargo and two Mooring Posts along the jetty, a flag and a lamp at the head, the laft house on the land
  behind, a fire with benches to the right. Waiting for the user's comments.
- 2026-10-14: the user agreed to round A draft 2. Round B started.
- 2026-10-14, round B draft 1 (`AssetSource/Preview/build_traders.py`, pictures `Preview/out/traders.png` and
  `tradingpost_blackforest.png`): one trader per biome in the player body (bind pose in the pictures), a recoloured
  Hildir tunic (body paint plus skirt mesh), troll leather trousers, a troll hide or Deep North cape on four of them,
  and the character creator's hair and beards tinted. Colours (cloth / trim): Meadows green / straw, Black Forest
  spruce / bronze (troll cape), Swamp olive / bone (woman, Deep North cape), Mountain ice blue / white (white troll
  cape), Plains barley gold / red ochre (woman), Mistlands violet / eitr blue (woman, dark Deep North cape),
  Ashlands ember red / orange (charcoal troll cape). The stall's awning takes the cloth colour. `export_vanilla.py`
  now also exports skinned meshes (hair, beards, worn clothes) in their bind pose. Waiting for the user's comments.
- 2026-10-14: the hair and beards sat too high in the round B pictures. The Player prefab's `Visual` is scaled to 0.95;
  the body and clothes were exported with it, hair and beards from their own prefabs without it. The preview now
  scales them by 0.95 (`VISUAL_SCALE`). In the game they hang under `Visual`, so phase 4 needs nothing for this.
- 2026-10-14: the user agreed to round B as drawn (one look per biome, the colours and clothes in `TRADERS`). Coding
  starts with phase 1.
- 2026-10-14, phase 1 done: `UnlimitedValuablesMode` (Auto/On/Off) and `[Chests] UnlimitedValuables`; `ChestSupply`
  holds back the Treasure chest's items (`IsWithheld`) in every supply check, unlock and count;
  `ContainerExtension.RemoveAfterModeChange` stores the rules as mode + 100 when valuables are held back and removes
  the stacks no longer supplied. Norwegian texts, `docs/chests.md`, changelog. Build, `cfg_keys.py --check` and the 853
  tests pass. Not yet tried in the game.
- 2026-10-14, the user: the trading area must have the same kind of protective dome as the vanilla traders, so
  nobody is attacked while trading; the escort job moves from Haldor to the posts, the trading ship appears on the
  far side of the post from your own ship (or some way from it), and the escort only really starts when you go aboard
  your own ship after buying and starting it. Added to sections 1, 2 and 4a, phases 2 and 7.
- 2026-10-14, phase 2 done (not yet tried in the game):
  - `build_trading_post.py` now also writes `AssetSource/Preview/tradingpost.json` (embedded as
    `BrudvikWhiteHilt.TradingPost.json`): 96 parts in post coordinates (middle of the landing at deck height, +z to the
    sea), the laft house unfolded into its pieces and `wood_roof_45`, decor as `piece_whitehilt_decor_<id>`, parts on
    the land marked `ground`; the trader's place, the dome (centre z 5, radius 26), 27 pile places, the ground to level
    (phase 3) and the jetty head.
  - Post-only pieces in `build_water.py` / `defenses.json`: `handelsport` (gate with sign), `flaggstang` (flag pole; the
    pennant is a mesh made in code, `TradingPostPennant`) and `handelspaele` (6 m pile). In C# they are
    `TradingPostPieceBase` subclasses implementing the new `IUnlistedPiece`: not on the hammer
    (`PieceConfig.Enabled = false`) and not registered with progression, so they get no config entries.
  - `TradingPostBuilder` (server) creates the marker `WhiteHilt_TradingPost` and every part as network data only, each
    with `whitehilt_post` = the post's id, long piles stacked down to the bed where it lies deeper than the jetty's own
    piles; `Remove` destroys every object of a post.
  - `TradingPostSite` on the marker attaches the dome: `TradingPostDome` copies `ForceField` (with its `NoMonsterArea`)
    out of `Vendor_BlackForest` once and scales it to the radius; renderers off on a dedicated server.
  - `TradingPostPatches`: a post's pieces skip `WearNTear.RPC_Damage`, cannot be removed (`WearNTear.CanBeRemoved`,
    `Piece.CanBeRemoved`), need no support and do not wear or burn.
  - `whitehilt_tradepost here|remove|list` (admins, checked on the server), `[TradingPosts] Enabled`.
  - Open for phase 3: levelling the ground (`level`), and keeping players from building inside a post's dome.
- 2026-10-14, phase 3 done (not yet tried in the game):
  - `TradingPostSites` (pure, tested in `TradingPostSitesTests`): a place suits when the bank under the landing is
    0.3–2.5 m above the sea, the land behind (nine samples where the house, fire and cart stand) is no more than 2 m
    above the deck and within 2.5 m of itself, nothing along the jetty rises within 0.6 m of the deck, and the water is
    2.5 m deep at the head and 6 m beyond it and 2 m deep 6 m either side of the head. The deck is 1 m above the sea or
    0.1 m above the bank. Sixteen directions are tried; deeper water at the head, flatter land and deeper water further
    out score best. `Pick` takes them in the world seed's shuffled order, at least `MinDistance` apart.
  - `TradingPostPlacer` (on the server, after the world's locations are generated, once per world: global key
    `whitehilt_tradingposts_placed`): a 40 m grid over the world within 10 km (not within 300 m of the centre), from
    `WorldGenerator` heights, so unvisited land counts; clear of player buildings (`BuildingDistance`) and of locations
    (their exterior radius plus the dome's); `PerBiome` posts for Meadows to Ashlands (not the Deep North), counting
    the posts already standing. Spread over frames (4 ms). `whitehilt_tradepost place` runs it again.
  - The marker is now also a `Location` with `m_noBuild` over the dome's radius (Haldor's rule, which the mod's own
    building tools already respect), and carries two round `TerrainModifier`s from the layout's `level` that level the
    land behind the landing to the bank; things on the land stand at that height.
  - `TradingPostSite` clears trees, logs, rocks, bushes and pickables inside the post (not what players built,
    creatures, containers or dropped items) on the marker's owner, 1, 5 and 15 s after it loads, since a zone generated
    as a player arrives gets its vegetation a moment later.
  - The marker stores the post's biome (`whitehilt_post_biome`).
  - To check in the game: how many places suit per biome on real coasts (the log lists them; if too few, let the
    jetty grow by a section where the bed falls slowly), that the terrain modifiers level the land, that no vegetation
    is left on the landing.
