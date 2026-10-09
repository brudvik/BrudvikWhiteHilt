# 🪵 Log house

Build a house the Norwegian way: *laft*, core wood logs laid on top of each other and notched where the walls meet, so the log ends cross and stick out past the corners. Set it on a dry-laid stone foundation, lay the floors in large pieces, hang a plank door, a dragon portal or barn doors in the wall, put in shuttered windows, go up by a stair, a spiral stair or a ladder, and down through a hatch to the cellar. Everything is built with the ordinary Hammer near a Workbench.

<img src="images/log_lafthus.png" alt="A small log house on a stone foundation" title="A log house of 6 × 4 m: stone foundation and cornerstones, log walls, corners and gables, a plank door, and the vanilla 45° roof" height="260">

## 🪵 LOG WALLS

<img src="images/log_laftvegg.png" alt="Log Wall" title="Log Wall" height="140"> <img src="images/log_laftvegg_forskutt.png" alt="Offset Log Wall" title="Offset Log Wall" height="140"> <img src="images/log_laftehjorne.png" alt="Log Corner" title="Log Corner" height="140"> <img src="images/log_laftekryss.png" alt="Log Wall Joint" title="Log Wall Joint" height="140">

In a log house the logs of two walls that meet lie half a log apart in height, so that each log rests in the notch of the one across it. The walls therefore come in two kinds:

- The **Log Wall** starts with a whole log on the floor: its logs lie at 0.25, 0.75, 1.25 and 1.75 m.
- The **Offset Log Wall** lies half a log higher. Its lowest log is sunk half into the floor or foundation, like the half log a real house starts its end walls with, and its top log is the lowest log of whatever stands on it, a wall or a gable.

Build two opposite walls of the house with the Log Wall and the other two with the Offset Log Wall. Logs are 0.5 m apart, so walls, low walls and gables of the same kind stack on each other at every metre and keep the rhythm. Walls snap end to end and on top of each other like the vanilla walls, with their snap points on the centre line of the logs.

The **Log Corner** puts the crossing log ends (*laftehoder*) on a corner: set it on the corner, where the centre lines of the two walls meet. Turn it until its logs follow the walls. At two of the four corners of a house they will not, and the **Log Corner, Mirrored** fits there. A corner piece is 2 m high, so stack two for a two-storey house. Where an inner wall meets an outer one, the **Log Wall Joint** lets the inner wall's logs run through and stick out on the outside, as in *krysslaft*; build the inner wall with the Offset Log Wall.

| Piece | Description | Crafting Station | Requirements |
|-------|-------------|------------------|--------------|
| **Log Wall** (`piece_whitehilt_laftvegg`) | 2 m long, 2 m high | Hammer (Workbench) | Core Wood ×4 |
| **Log Wall 1 m** (`piece_whitehilt_laftvegg_1m`) | 1 m long, to close a gap | Hammer (Workbench) | Core Wood ×2 |
| **Log Wall 4 m** (`piece_whitehilt_laftvegg_4m`) | Whole 4 m logs, without a joint in the middle | Hammer (Workbench) | Core Wood ×8 |
| **Offset Log Wall** (`piece_whitehilt_laftvegg_forskutt`) | 2 m long, 2 m high, logs half a log higher | Hammer (Workbench) | Core Wood ×4 |
| **Offset Log Wall 1 m** (`piece_whitehilt_laftvegg_forskutt_1m`) | 1 m long | Hammer (Workbench) | Core Wood ×2 |
| **Offset Log Wall 4 m** (`piece_whitehilt_laftvegg_forskutt_4m`) | Whole 4 m logs | Hammer (Workbench) | Core Wood ×8 |
| **Low Offset Log Wall** (`piece_whitehilt_laftvegg_forskutt_lav`) | 2 m long, 1 m high, for under a 26° gable | Hammer (Workbench) | Core Wood ×2 |
| **Log Corner** (`piece_whitehilt_laftehjorne`) | The crossing log ends of a corner, 2 m high | Hammer (Workbench) | Core Wood ×3 |
| **Log Corner, Mirrored** (`piece_whitehilt_laftehjorne_speilet`) | The same, the other way round | Hammer (Workbench) | Core Wood ×3 |
| **Log Wall Joint** (`piece_whitehilt_laftekryss`) | An inner wall's log ends through the outer wall | Hammer (Workbench) | Core Wood ×2 |

Log pieces have 200 health per core wood they cost, so a 2 m wall has 800, and burn like other wood.

## 🔺 GABLES

<img src="images/log_laftgavl_26.png" alt="Log Gable 26°" title="Log Gable 26°" height="140"> <img src="images/log_laftgavl_45.png" alt="Log Gable 45°" title="Log Gable 45°" height="140">

A gable is half the end of the house, 2 m wide: offset logs cut to the slope of the vanilla 26° or 45° roof, with barge boards over their ends and a rafter on top for the roof to lie on. Its lowest log reaches past the corner like the corner's logs under it. Set it on top of an end wall (an Offset Log Wall) and turn it so it rises towards the ridge; the other half of the end wall takes another, turned the other way.

For a house wider than 4 m, the inner part of the end wall rises higher: under a 45° gable put a 2 m Offset Log Wall, under a 26° gable a Low Offset Log Wall, and the gable on top of that.

| Piece | Description | Crafting Station | Requirements |
|-------|-------------|------------------|--------------|
| **Log Gable 26°** (`piece_whitehilt_laftgavl_26`) | 2 m wide, rising 1 m | Hammer (Workbench) | Core Wood ×2 |
| **Log Gable 45°** (`piece_whitehilt_laftgavl_45`) | 2 m wide, rising 2 m | Hammer (Workbench) | Core Wood ×3 |

Every [White Hilt roof](roofs.md) has the same slopes as the vanilla roofs, so turf, slate and shingles fit the gables too.

## 🪨 FOUNDATION

<img src="images/log_grunnmur.png" alt="Stone Foundation" title="Stone Foundation" height="140"> <img src="images/log_hjornestein.png" alt="Cornerstone" title="Cornerstone" height="140">

A *grunnmur* of big field stones laid dry keeps the logs off the damp ground and the house level on a slope. It stands 1 m high and reaches 0.6 m into the ground, so it does not hang in the air where the ground falls away. Log walls, floors and the vanilla pieces snap to its top. It is stone in the building rules: it holds what stands on it like the ground does, does not wear in the rain and sounds like stone when struck.

The **Cornerstone** goes under each corner, where the log ends stick out past the foundation walls, or alone as a pillar under a floor.

| Piece | Description | Crafting Station | Requirements |
|-------|-------------|------------------|--------------|
| **Stone Foundation** (`piece_whitehilt_grunnmur`) | 2 m long, 1 m high | Hammer (Workbench) | Stone ×12 |
| **Stone Foundation 4 m** (`piece_whitehilt_grunnmur_4m`) | 4 m long, 1 m high | Hammer (Workbench) | Stone ×24 |
| **Cornerstone** (`piece_whitehilt_hjornestein`) | A stone pillar 0.8 m square, 1 m high | Hammer (Workbench) | Stone ×8 |

## 🟫 FLOORS

<img src="images/log_plankegulv_4x4.png" alt="Plank Floor 4×4" title="Plank Floor 4×4" height="140"> <img src="images/log_bjelkelag_4x4.png" alt="Joisted Floor 4×4" title="Joisted Floor 4×4" height="140">

Plank floors in more sizes, laid from the vanilla floor's boards, so they look the same and the boards keep their size. They cost what the vanilla floors they cover cost. The **Joisted Floor** lies on hewn joists 1 m apart, for a loft: from below you see the joists, not the underside of the boards.

| Piece | Description | Crafting Station | Requirements |
|-------|-------------|------------------|--------------|
| **Plank Floor 4×4** (`piece_whitehilt_plankegulv_4x4`) | 4 × 4 m | Hammer (Workbench) | Wood ×8 |
| **Plank Floor 4×2** (`piece_whitehilt_plankegulv_4x2`) | 4 × 2 m | Hammer (Workbench) | Wood ×4 |
| **Plank Floor 2×1** (`piece_whitehilt_plankegulv_2x1`) | 2 × 1 m, for a strip along a wall | Hammer (Workbench) | Wood ×1 |
| **Joisted Floor 4×4** (`piece_whitehilt_bjelkelag_4x4`) | 4 × 4 m on joists | Hammer (Workbench) | Wood ×12 |

## 🚪 DOORS

<img src="images/log_plankedor.png" alt="Plank Door" title="Plank Door" height="140"> <img src="images/log_dorportal.png" alt="Dragon Portal" title="Dragon Portal" height="140"> <img src="images/log_lavedor.png" alt="Barn Doors" title="Barn Doors" height="140">

The doors snap in place of a wall like the vanilla door: the Plank Door and the Dragon Portal in place of a 2 m wall, the Barn Doors in place of a 4 m wall. They fit log walls and vanilla walls alike. Open and close them with **Use**; they are [self-closing doors](base.md#-self-closing-doors) like every other door, so Shift + Use holds one open and they shut by themselves when a raid comes.

| Piece | Description | Crafting Station | Requirements |
|-------|-------------|------------------|--------------|
| **Plank Door** (`piece_whitehilt_plankedor`) | A door of tarred boards on strap hinges between two hewn posts, 2 × 2 m | Hammer (Workbench) | Wood ×6, Resin ×2 |
| **Dragon Portal** (`piece_whitehilt_dorportal`) | A door in a portal of broad boards with two dragon heads and a bronze ring, 2 × 2 m | Hammer (Workbench) | Fine Wood ×8, Wood ×4, Bronze ×1 |
| **Barn Doors** (`piece_whitehilt_lavedor`) | Two tarred board doors, 4 × 2 m, wide enough for a cart | Hammer (Workbench) | Wood ×16, Resin ×4 |

## 🪟 WINDOWS

<img src="images/log_laftvegg_vindu.png" alt="Log Wall with Window" title="Log Wall with Window" height="140"> <img src="images/log_laftvegg_glugg.png" alt="Log Wall with Glugg" title="Log Wall with Glugg" height="140">

The windows are log walls with an opening framed in boards, in both kinds of wall, and take the place of a 2 m wall. The **Log Wall with Window** has a window 1 m wide closed by two shutters on the outside. **Use** opens them: they always swing out against the wall, whichever side you open them from. They are windows to the [self-closing doors](base.md#-self-closing-doors), so they close when rain, a storm or night begins (and can be opened again while it lasts), and at once when a raid comes. The **Log Wall with Glugg** has a *glugg*, a small opening one log high and 0.6 m wide, as in the oldest houses, which let in light and let out smoke.

The window lies where the logs are taken out, so it sits a little higher in the offset wall than in the plain one.

| Piece | Description | Crafting Station | Requirements |
|-------|-------------|------------------|--------------|
| **Log Wall with Window** (`piece_whitehilt_laftvegg_vindu`) | Window 1 m wide with two shutters | Hammer (Workbench) | Core Wood ×4, Wood ×2 |
| **Offset Log Wall with Window** (`piece_whitehilt_laftvegg_forskutt_vindu`) | The same in the offset wall | Hammer (Workbench) | Core Wood ×4, Wood ×2 |
| **Log Wall with Glugg** (`piece_whitehilt_laftvegg_glugg`) | A small open window, one log high | Hammer (Workbench) | Core Wood ×4 |
| **Offset Log Wall with Glugg** (`piece_whitehilt_laftvegg_forskutt_glugg`) | The same in the offset wall | Hammer (Workbench) | Core Wood ×4 |

## 🪜 STAIRS, LADDERS AND RAILINGS

<img src="images/log_smal_trapp.png" alt="Narrow Stair" title="Narrow Stair" height="140"> <img src="images/log_vindeltrapp.png" alt="Spiral Stair" title="Spiral Stair" height="140"> <img src="images/log_stige.png" alt="Loft Ladder" title="Loft Ladder" height="140"> <img src="images/log_rekkverk.png" alt="Railing" title="Railing" height="140"> <img src="images/log_trapperekkverk.png" alt="Stair Railing" title="Stair Railing" height="140">

The **Narrow Stair** is 1 m wide and rises 2 m over 4 m, as steep as the vanilla stair, so it reaches a loft or the next storey in one piece. The **Spiral Stair** winds round a post with a handrail, rising 2 m in three quarters of a turn, 2.4 m across: you go up facing one way and come off a quarter turn to the left. Stack another on top, turned a quarter back, for the next storey. Cut the hole in the floor above yourself, by leaving out a floor piece.

The **Loft Ladder** stands against the edge of a loft 2 m up. **Use** it at its foot to climb onto the loft behind it; on the loft, the alternate key (Shift by default) and **Use** take you down.

The **Railing** is 1 m high, for the edge of a loft or a stair well, and the **Stair Railing** follows the slope of the vanilla stair and the Narrow Stair (two go along one Narrow Stair).

| Piece | Description | Crafting Station | Requirements |
|-------|-------------|------------------|--------------|
| **Narrow Stair** (`piece_whitehilt_smal_trapp`) | 1 m wide, rising 2 m over 4 m | Hammer (Workbench) | Wood ×6 |
| **Spiral Stair** (`piece_whitehilt_vindeltrapp`) | Rising 2 m in three quarters of a turn | Hammer (Workbench) | Wood ×14 |
| **Loft Ladder** (`piece_whitehilt_stige`) | 2.3 m long, up to a loft 2 m up | Hammer (Workbench) | Wood ×4 |
| **Railing** (`piece_whitehilt_rekkverk`) | 2 m long, 1 m high | Hammer (Workbench) | Wood ×3 |
| **Railing 1 m** (`piece_whitehilt_rekkverk_1m`) | 1 m long | Hammer (Workbench) | Wood ×2 |
| **Stair Railing** (`piece_whitehilt_trapperekkverk`) | 2 m along a stair, rising 1 m | Hammer (Workbench) | Wood ×3 |

## 🧱 CELLAR

<img src="images/log_lem.png" alt="Floor Hatch" title="Floor Hatch" height="140"> <img src="images/log_kjellermur.png" alt="Cellar Wall" title="Cellar Wall" height="140">

Dig out a cellar under the house with the pickaxe, line it with the **Cellar Wall**, and lay a **Floor Hatch** in the floor over it. The hatch is a 2 × 2 m floor with a lid in one corner and a ladder 2 m down under it. **Use** the lid to lift it; it is a door to the self-closing doors and falls shut a while after you went through. From the cellar, **Use** the ladder to climb up onto the floor beside the hatch; from there, the alternate key and **Use** take you down.

| Piece | Description | Crafting Station | Requirements |
|-------|-------------|------------------|--------------|
| **Floor Hatch** (`piece_whitehilt_lem`) | A 2 × 2 m floor with a hatch and a ladder 2 m down | Hammer (Workbench) | Wood ×8 |
| **Cellar Wall** (`piece_whitehilt_kjellermur`) | Field stones laid dry, 2 m long and 2 m high | Hammer (Workbench) | Stone ×20 |

## 🏡 BUILDING A HOUSE, STEP BY STEP

1. Lay the **Stone Foundation** round the house and a **Cornerstone** at each corner.
2. Put **Log Walls** along the two long sides and **Offset Log Walls** along the two ends, on top of the foundation. Leave a 2 m gap for the door.
3. Set a **Log Corner** on each corner, turning it until its logs follow the walls; where no turn fits, use the **Log Corner, Mirrored**.
4. Put the **Plank Door** in the gap, swap a wall or two for a **Log Wall with Window**, and lay the **floor** inside, on the foundation.
5. On top of the end walls, set two **Log Gables** each, rising to the ridge, and put the roof on.

## Config

The log house pieces are switched on and off and priced in `[Content]` and `[Recipes]` like the other White Hilt pieces. With linear progression, the log walls, corners, gables, windows and the Dragon Portal come with the Black Forest (core wood needs the bronze axe); the foundation, floors, doors, stairs, ladder, railings, hatch and cellar wall are there from the start.
