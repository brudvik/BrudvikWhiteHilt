# 🏠 Plan: the Husvette, the book of the hall and Homecraft

Plan for **comfort over a whole room**. Today Valheim counts comfort from the pieces within **10 m of where the player
stands** (`SE_Rested.c_ComfortRadius`, read from the game's code), one per comfort group, and resting also needs a fire's
warmth and shelter. So everything that gives comfort must be crowded round the spot by the fire, and a large, well
furnished hall gives no more than a corner of it. The user chose three things that work together (2026-10-14):

1. **The Husvette** (*house spirit*): a carved figure you build in the house; comfort pieces within its reach count for
   everyone under the roof within it.
2. **The book of the hall** (*kunnskap om interiør*): read once, it widens your own comfort reach.
3. **Homecraft** (*Husflid*): a skill that widens it further as it grows, up to a cap.

Everything is configurable. Written for an AI agent that carries it out phase by phase without the conversation behind
it. Read all of it once before you start.

## 1. What the player gets

- **The Husvette.** A carved wooden gardvord, a small old man with a red pointed cap and a grey beard, on a stump, with
  a bowl of porridge before him, as folk tradition says the house spirit must be given. Built with the hammer under
  Furniture. Looking at it shows its reach as a ring on the ground, like the Quartermaster's Table.
  - Every **comfort piece within 30 m** of the Husvette (`[Comfort] HusvetteRadius`) counts for a player who stands
    **under a roof within the same 30 m**, as if it stood beside them.
  - Optionally (`HusvetteWarmth`, on), a burning fire anywhere within the Husvette's reach is warm enough to rest by,
    so you need not sit at the hearth to be rested.
- **The book of the hall.** A book sold by Hildir (and the trading posts' traders). Read it once: your own comfort
  reach grows by **10 m** (`BookBonus`), from 10 to 20 m, wherever you are. It is remembered for your character; the
  book is used up.
- **Homecraft (Husflid).** A new skill in the skills dialog, raised by resting (`Rested`), by sitting in a room with
  comfort, and by building comfort pieces. It widens your own reach by up to `SkillBonus` (default 20 m at level 100),
  but your own reach is never more than **the Husvette's radius plus 10 m** (`PersonalMax` = 40 m by default). Its
  milestones show in the skill book; the School Bench can teach it like any skill.
- **Counting.** Comfort is counted from the union of the pieces within your own reach and those within the reach of
  every Husvette whose room you stand in. One per comfort group still, as in the game, so a big room gives variety, not
  endless comfort.

### Reach, worked through with the defaults

| You have | Your own reach | Under a Husvette's roof |
|---|---|---|
| Nothing | 10 m (vanilla) | the Husvette's 30 m around it, plus your 10 m |
| The book | 20 m | 30 m around it, plus your 20 m |
| The book and Homecraft 50 | 30 m | 30 m around it, plus your 30 m |
| The book and Homecraft 100 | 40 m (the cap) | 30 m around it, plus your 40 m |

**To confirm with the user before phase 2:** that the book and Homecraft widen the *player's own* reach (as above),
and not the Husvette's. The other reading (the Husvette's 30 m grows to 40 m with the book) would leave Homecraft
nothing to add once the book is read.

## 2. Decisions already made

| Decision | Choice | Why |
|---|---|---|
| The three parts | Husvette + book + skill, all configurable | The user's choice |
| Husvette radius | 30 m (`HusvetteRadius`) | The user's choice |
| Book | +10 m (`BookBonus`) | The user's choice |
| Skill cap | Own reach at most the Husvette radius + 10 m (`PersonalMax`, 40 m) | The user's choice |
| Look | A carved, painted gardvord with a red cap on a stump and a bowl of porridge (preview `AssetSource/Preview/build_husvette.py`) | Norse folk tradition; the first draft is waiting for the user's comments |
| How comfort is widened | A Harmony patch on `SE_Rested.GetNearbyComfortPieces` (the game's own list): the pieces within the player's reach and within each Husvette whose room the player stands in are added, then the game counts them its own way | One small patch; the game's grouping rule stays |
| "Room" | Under a roof (`Player.InShelter()`) and within the Husvette's radius; no wall tracing | Simple and robust; walls are what players build anyway |
| Biome | Black Forest: Fine Wood, Wood, a Troll Hide strip? (to agree), `ProgressionTier.BlackForest`; the book's price at Hildir 300 coins; the skill from the start | Biome rule; the book is knowledge, not a material |

## 3. Settings (`[Comfort]`, admin-only)

| Setting | Default | What it does |
|---|---|---|
| `Enabled` | true | All three parts |
| `HusvetteRadius` | 30 | Reach of a Husvette, in metres |
| `HusvetteWarmth` | true | A burning fire within a Husvette's reach is warm enough to rest by |
| `BookBonus` | 10 | Metres the book of the hall adds to the player's own reach |
| `BookPrice` | 300 | The book's price at Hildir and the trading posts |
| `SkillBonus` | 20 | Metres Homecraft adds at level 100 (in proportion below) |
| `PersonalMax` | 40 | Most the player's own reach can be, in metres (the Husvette's radius + 10 by default) |

## 4. Phases

### Round A: the Husvette's look (pictures only)
Draft 1 is `BrudvikWhiteHiltUnity/Preview/out/husvette.png` from `build_husvette.py`. Rounds with the user until agreed.

### Phase 1: the comfort count
- `ComfortReach` (pure, tested): the player's own reach from the book and the skill, capped; whether a point is in a
  Husvette's room.
- The patch on `SE_Rested.GetNearbyComfortPieces` and, with `HusvetteWarmth`, on the warmth check (`Player`'s fire
  test, via `EffectArea` or the near-fire timer: **verify** which is simplest).
- Settings and Norwegian texts.

### Phase 2: the Husvette piece
- A piece built from the agreed meshes (the turned shapes made in code, as the pennant is), the range ring on hover
  (reuse the Quartermaster's ring code), registered like the other pieces.

### Phase 3: the book
- `WhiteHilt_HallBook` sold by Hildir and the trading posts' traders; using it sets a player unique key and is used up;
  the hover text says when it is already known.

### Phase 4: Homecraft
- A custom skill like Foraging and Husbandry (`Mastery/`): experience from resting, comfort level and comfort pieces
  built; milestones in the skill book at 25/50/75/100 showing the reach; the School Bench teaches it.

### Phase 5: docs, pictures, release
- A section in `docs/base.md` (or a page of its own), the changelog, the README card.

## 5. Log

- 2026-10-14: plan written; the user chose the Husvette (30 m), the book (+10 m) and Homecraft (cap Husvette + 10 m).
  First draft of the Husvette's look rendered.
- 2026-10-14: the user agreed to the reading of the reach (book and Homecraft widen the player's own), the look and
  the recipe (Fine Wood ×4, Wood ×2, Troll Hide ×1, Black Forest).
- 2026-10-14, phases 1 to 5 done (not yet tried in the game):
  - `ComfortReach.Personal` (pure, `ComfortReachTests`): 10 + book + skill share, capped by `PersonalMax`, never below 10.
  - `ComfortService.NearbyComfortPieces` replaces `SE_Rested.GetNearbyComfortPieces` (prefix): the pieces within the
    local player's own reach (other points get the game's 10 m) and within every built Husvette whose reach the point
    is in; the game sorts and counts one per group. The game only asks for a player under a roof.
  - `Husvette` (component): list of loaded ones (ghosts excluded by `IsBuilt`), the workbench's area marker as its
    ring (hover, and placement ghost via `Player.UpdatePlacementGhost`), and the warmth: for the local player under a
    roof within reach, a burning `Fireplace` within reach (listed every 5 s) calls `Player.OnNearFire`, as a fire's
    own warmth does.
  - `HusvettePiece` (DefensePieceBase, layout `husvette` from `build_husvette.py`: the stump is `wood_pole_log`), the
    figure and porridge made in code by `HusvetteModel` from the same profiles as the preview, one mesh with both sides
    and a palette texture.
  - `WhiteHilt_HallBook` on Hildir's list and every trading post's, `BookPrice`; reading it sets the unique key
    `whitehilt_hallbook` and uses it up. Homecraft: a Jotunn skill with the chair's icon, raised every 10 s of
    `Resting` and per comfort point of a piece placed.
  - To check in the game: the comfort number in the Rested message with a Husvette across a large hall, the warmth
    from a fire across the room, the figure's look and colours, the ring.
