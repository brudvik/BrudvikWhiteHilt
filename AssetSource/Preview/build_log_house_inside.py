"""More for the log house: log walls with windows, stairs and a ladder, railings, a floor hatch and a cellar wall.

build_defenses.py adds inside_pieces() to the layout it writes; run that script, not this one. The mod builds them in
Pieces/LogHouse. The measures and helpers of the log walls are in build_log_house.py.

Moving parts follow the hidden vanilla door the piece is cloned from (SwingDriver in the mod): the shutters always swing
out, whichever side the door is opened from, and the hatch lid always lifts.
Walking surfaces are box ramps at most 30 degrees steep; the vanilla stair is 26.6.
"""

import math
import random

from build_defenses import box, ladder_parts, part, r3, ramp, views
from build_defence_extras import plank
from build_gate_controls import IRON_TINT
from build_log_house import (TAR_DOOR_TINT, WALL_DEPTH, WALL_VIEWS, courses, foundation, log_x, piece_entry)
from build_stone_defenses import spans_with_hole

FRAME_TINT = [0.9, 0.85, 0.8]
WINDOW_CUT = {False: [1.25, 1.75], True: [1.0, 1.5]}  # the logs taken out for a window, by kind (offset or not)
FRAME = 0.07  # thickness of a window frame's boards
SHUTTER = 0.05  # thickness of a shutter


def opening_heights(offset, cut):
    """The bottom and top of an opening where the logs centred at the heights in cut are taken out: the opening runs
    between the tops and bottoms of the logs left above and below it, which the frame covers."""
    # The next piece's lowest log closes the top: at 2 m on an offset wall, at 2.25 m on a plain one.
    kept = [y for y in courses(offset, 2.0) if y not in cut] + [2.0 + (0 if offset else 0.25)]
    below = max(y for y in kept if y < min(cut))
    above = min(y for y in kept if y > max(cut))
    return below + 0.25, above - 0.25


def wall_with_hole(offset, half_width, cut, seed):
    """A 2 m log wall whose logs at the heights in cut stop at the opening, x -half_width..half_width, with a board
    frame round the opening. Returns the parts, the colliders, the snap points and the opening (bottom, top)."""
    rng = random.Random(seed)
    parts = []
    for y in courses(offset, 2.0):
        if y in cut:
            parts += [log_x(-1, -half_width, y, rng), log_x(half_width, 1, y, rng)]
        else:
            parts.append(log_x(-1, 1, y, rng))
    bottom, top = opening_heights(offset, cut)
    depth = WALL_DEPTH + 0.02
    for x in (-half_width - FRAME / 2, half_width + FRAME / 2):
        parts.append(plank((x, bottom - FRAME, 0), (x, top + FRAME, 0), depth, FRAME, tint=FRAME_TINT))
    for y in (bottom - FRAME / 2, top + FRAME / 2):
        parts.append(plank((-half_width - FRAME, y, 0), (half_width + FRAME, y, 0), depth, FRAME, tint=FRAME_TINT))
    colliders = spans_with_hole(-1, 1, 0, 2, -WALL_DEPTH / 2, WALL_DEPTH / 2, (-half_width, half_width, bottom, top))
    snaps = [(x, y, 0) for x in (-1, 1) for y in (0, 2)]
    return parts, colliders, snaps, (bottom, top)


def shutter(group, x0, x1, bottom, top, z, rng):
    """One shutter of vertical boards with two iron bands, closing the opening from outside (+z)."""
    parts = []
    boards = 3
    for i in range(boards):
        a = x0 + (x1 - x0) * i / boards
        b = x0 + (x1 - x0) * (i + 1) / boards
        shade = rng.choice((0.9, 1.0))
        parts.append(plank(((a + b) / 2, bottom, z), ((a + b) / 2, top, z), SHUTTER, (b - a) * 0.96,
                           tint=[c * shade for c in TAR_DOOR_TINT], group=group))
    for y in (bottom + 0.12, top - 0.12):
        parts.append(plank((x0 + 0.02, y, z + SHUTTER), (x1 - 0.02, y, z + SHUTTER), 0.03, 0.08, tint=IRON_TINT, texture="metalwall",
                           group=group, detail=True))
    collider = box(((x0 + x1) / 2, (bottom + top) / 2, z), (abs(x1 - x0), top - bottom, SHUTTER + 0.02), group=group)
    return parts, collider


def window_wall(offset, seed):
    """A 2 m log wall with a window 1 m wide, closed by two shutters on the outside (+z) that swing out. The window lies
    round the eyes of a player standing inside (1.85 m tall, the eyes at about 1.6 m): from 1 to 2 m in the plain wall,
    from 0.77 to 1.73 m in the offset wall, as high as the logs let it."""
    rng = random.Random(seed + "_shutters")
    cut = WINDOW_CUT[offset]
    parts, colliders, snaps, (bottom, top) = wall_with_hole(offset, 0.5, cut, seed)
    z = WALL_DEPTH / 2 + 0.04
    left, left_collider = shutter("leaf_left", -0.5, 0.0, bottom, top, z, rng)
    right, right_collider = shutter("leaf_right", 0.0, 0.5, bottom, top, z, rng)
    parts += left + right
    colliders += [left_collider, right_collider]
    groups = [{"name": "leaf_left", "pivot": [-0.5, 0, z]}, {"name": "leaf_right", "pivot": [0.5, 0, z]}]
    return parts, colliders, snaps, groups


def glugg_wall(offset, seed):
    """A 2 m log wall with a glugg: a small open window, one log high and 0.6 m wide, as in the oldest houses, high up
    where the light comes in and the smoke goes out (1.5 to 2 m, or 1.27 to 1.73 m in the offset wall)."""
    cut = [1.75] if not offset else [1.5]
    parts, colliders, snaps, _ = wall_with_hole(offset, 0.3, cut, seed)
    return parts, colliders, snaps


def narrow_stair():
    """A stair 1 m wide rising 2 m over 4 m, the vanilla stair's slope, from +z (bottom) to -z (top), so one reaches a
    loft: two stringers and eight treads."""
    parts = []
    for x in (-0.47, 0.47):
        parts.append(plank((x, -0.05, 2.0), (x, 1.95, -2.0), 0.06, 0.3))
    for i in range(8):
        y = 0.25 * (i + 1)
        z = 2.0 - 0.5 * (i + 1) + 0.25
        parts.append(plank((-0.5, y - 0.03, z), (0.5, y - 0.03, z), 0.5, 0.06))
    colliders = [ramp((0, 0, 2.0), (0, 2.0, -2.0), 1.0)]
    snaps = [(x, 0, 2.0) for x in (-0.5, 0.5)] + [(x, 2.0, -2.0) for x in (-0.5, 0.5)]
    return parts, colliders, snaps


SPIRAL_RADIUS = 1.2
SPIRAL_TURN = 270.0  # degrees for one storey of 2 m: a full turn would leave too little headroom under the treads above
SPIRAL_STEPS = 9


def spiral_point(angle, radius, y):
    a = math.radians(angle)
    return (radius * math.sin(a), y, radius * math.cos(a))


def spiral_stair(seed):
    """A spiral stair round a post, rising 2 m in three quarters of a turn: you go up facing +z and come off facing -x.
    Snap another on top for the next storey; SpiralStairPatches turns it a quarter back."""
    rng = random.Random(seed)
    parts = [part("wood_pole_log", (0, 1.1, 0), (0, rng.uniform(0, 360), 0), (0.14 / 0.26, 2.2 / 2.22, 0.14 / 0.28))]
    rise = 2.0 / SPIRAL_STEPS
    step = SPIRAL_TURN / SPIRAL_STEPS
    for i in range(SPIRAL_STEPS - 1):
        angle = step * (i + 0.5)
        y = rise * (i + 1)
        inner, outer = spiral_point(angle, 0.12, y - 0.03), spiral_point(angle, SPIRAL_RADIUS, y - 0.03)
        parts.append(plank(inner, outer, 0.45, 0.06))
        # A baluster at the outer end of every tread, and the handrail from one to the next.
        top = spiral_point(angle, SPIRAL_RADIUS - 0.06, y + 0.9)
        parts.append(plank(spiral_point(angle, SPIRAL_RADIUS - 0.06, y), top, 0.05, 0.05, detail=True))
        if i > 0:
            previous = spiral_point(angle - step, SPIRAL_RADIUS - 0.06, y - rise + 0.9)
            parts.append(plank(previous, top, 0.07, 0.06))
    colliders = []
    middle = 0.7
    for i in range(SPIRAL_STEPS):
        low = spiral_point(step * i, middle, rise * i)
        high = spiral_point(step * (i + 1), middle, rise * (i + 1))
        colliders.append(ramp(low, high, 1.1))
    colliders.append(box((0, 1.1, 0), (0.28, 2.2, 0.28)))
    snaps = [(0, 0, 0), (0, 2.0, 0)]
    return parts, colliders, snaps


def ladder():
    """A ladder 2.3 m long up to a loft at 2 m: use it at the foot to climb onto the loft behind it (-z), and on the loft
    with the alternate key to climb down."""
    parts = ladder_parts(0, 0, 0, 2.3)
    colliders = [box((x, 1.15, 0), (0.12, 2.3, 0.12)) for x in (-0.25, 0.25)]
    ladders = [{"center": [0, 1.15, 0], "size": [0.7, 2.3, 0.15], "stops": [0, 0.05, 0.6, 0, 2.1, -0.6]}]
    snaps = [(0, 0, 0), (0, 2.0, 0)]
    return parts, colliders, ladders, snaps


def railing(length):
    """A railing of hewn posts, a top and a bottom rail and square balusters, 1 m high, along x."""
    parts = []
    for x in (-length / 2 + 0.05, length / 2 - 0.05):
        parts.append(plank((x, 0, 0), (x, 1.05, 0), 0.1, 0.1))
    for y, size in ((1.0, 0.1), (0.12, 0.07)):
        parts.append(plank((-length / 2, y, 0), (length / 2, y, 0), size, size * 0.8))
    count = int(round(length / 0.2))
    for i in range(1, count):
        x = -length / 2 + length * i / count
        parts.append(plank((x, 0.15, 0), (x, 0.96, 0), 0.04, 0.04, detail=True))
    colliders = [box((0, 0.55, 0), (length, 1.1, 0.12))]
    snaps = [(x, 0, 0) for x in (-length / 2, length / 2)] + [(x, 1.0, 0) for x in (-length / 2, length / 2)]
    return parts, colliders, snaps


def stair_railing():
    """A railing for a stair of the vanilla slope, 2 m along it and rising 1 m: from x -1 (bottom) to +1 (top)."""
    parts = []
    for x, y in ((-0.95, 0.025), (0.95, 0.975)):
        parts.append(plank((x, y, 0), (x, y + 1.05, 0), 0.1, 0.1))
    parts.append(plank((-1.0, 1.0, 0), (1.0, 2.0, 0), 0.1, 0.08))
    parts.append(plank((-1.0, 0.12, 0), (1.0, 1.12, 0), 0.07, 0.06))
    for i in range(1, 10):
        x = -1 + 0.2 * i
        y = (x + 1) / 2
        parts.append(plank((x, y + 0.15, 0), (x, y + 0.96, 0), 0.04, 0.04, detail=True))
    colliders = [box((0, 1.05, 0), (2.24, 1.0, 0.12), (0, 0, 26.565))]
    snaps = [(-1.0, 0, 0), (1.0, 1.0, 0)]
    return parts, colliders, snaps


def floor_hatch():
    """A 2 x 2 m plank floor with a hatch in one quarter (x 0..1, z -1..0) and a ladder 2 m down under it. The lid is
    hung on its far edge (z -1) and lifts with the hidden vanilla door; the ladder takes you from the cellar up onto the
    floor beside the hatch, and with the alternate key from there down."""
    rng = random.Random("lem")
    parts = []
    for x, z in ((-0.5, -0.5), (-0.5, 0.5), (0.5, 0.5)):
        parts.append(part("wood_floor_1x1", (x, 0, z)))
    colliders = [box((-0.5, -0.03, 0), (1.0, 0.22, 2.0)), box((0.5, -0.03, 0.5), (1.0, 0.22, 1.0))]
    # The lid: boards across the hinge, two battens underneath and an iron ring.
    for i in range(4):
        x = 0.06 + 0.22 * i + 0.11
        shade = rng.choice((0.9, 1.0))
        parts.append(plank((x, 0.05, -0.98), (x, 0.05, -0.02), 0.21, 0.06, tint=[c * shade for c in TAR_DOOR_TINT], group="lid"))
    for z in (-0.8, -0.2):
        parts.append(plank((0.08, -0.01, z), (0.92, -0.01, z), 0.08, 0.05, tint=TAR_DOOR_TINT, group="lid"))
    parts.append(part("Bell", (0.5, 0.09, -0.2), (0, 0, 0), (0.08, 0.015, 0.08), tint=IRON_TINT, group="lid", detail=True))
    for x in (0.25, 0.75):
        parts.append(plank((x, 0.085, -1.0), (x, 0.085, -0.75), 0.06, 0.015, tint=IRON_TINT, texture="metalwall", group="lid", detail=True))
    colliders.append(box((0.5, 0.05, -0.5), (0.96, 0.08, 0.96), group="lid"))
    # The ladder against the hatch's far side, from the cellar floor 2 m down.
    parts += ladder_parts(0.5, -0.88, -2.0, 0.0)
    ladders = [{"center": [0.5, -1.0, -0.88], "size": [0.7, 2.0, 0.15], "stops": [0.5, -1.95, -0.3, -0.5, 0.1, -0.5]}]
    groups = [{"name": "lid", "pivot": [0.5, 0.05, -1.0]}]
    snaps = [(x, 0, z) for x in (-1, 0, 1) for z in (-1, 0, 1) if (x, z) != (0, 0)]
    return parts, colliders, snaps, groups, ladders


def cellar_wall():
    """A cellar wall of dry-laid stones, 2 m long and 2 m high, for the walls of a dug-out cellar."""
    return foundation(2.0, "kjellermur", height=2.0)


def inside_pieces():
    pieces = []
    for kind, offset in (("", False), ("_forskutt", True)):
        parts, colliders, snaps, groups = window_wall(offset, f"laftvegg{kind}_vindu")
        pieces.append(piece_entry(f"laftvegg{kind}_vindu", "wood_door", parts, colliders, snaps, groups=groups, keep=["door"],
                                  views=views(("outside", 180, 10), ("inside", 0, 10), ("angle", 140, 25))))
        pieces.append(piece_entry(f"laftvegg{kind}_glugg", "wood_pole_log", *glugg_wall(offset, f"laftvegg{kind}_glugg"), views=WALL_VIEWS))
    stair_views = views(("side", 90, 10), ("front", 180, 25), ("angle", 140, 30))
    pieces.append(piece_entry("smal_trapp", "wood_stair", *narrow_stair(), views=stair_views))
    pieces.append(piece_entry("vindeltrapp", "wood_stair", *spiral_stair("vindeltrapp"), views=views(("side", 180, 15), ("angle", 120, 30), ("top", 180, 80))))
    parts, colliders, ladders, snaps = ladder()
    pieces.append(piece_entry("stige", "wood_pole2", parts, colliders, snaps, ladders=ladders, views=views(("front", 180, 10), ("side", 90, 10))))
    pieces.append(piece_entry("rekkverk", "wood_pole2", *railing(2.0), views=WALL_VIEWS))
    pieces.append(piece_entry("rekkverk_1m", "wood_pole2", *railing(1.0), views=WALL_VIEWS))
    pieces.append(piece_entry("trapperekkverk", "wood_pole2", *stair_railing(), views=WALL_VIEWS))
    parts, colliders, snaps, groups, ladders = floor_hatch()
    pieces.append(piece_entry("lem", "wood_door", parts, colliders, snaps, groups=groups, ladders=ladders, keep=["door"],
                              views=views(("above", 160, 40), ("below", 160, -20), ("top", 0, 85))))
    pieces.append(piece_entry("kjellermur", "stone_wall_2x1", *cellar_wall(), views=WALL_VIEWS))
    return pieces

