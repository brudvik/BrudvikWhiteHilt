"""The log house: laft walls, corners and gables, a dry-laid stone foundation, plank floors in more sizes and doors.

build_defenses.py adds log_house_pieces() to the layout it writes; run that script, not this one. The mod builds them in
Pieces/LogHouse.

Laft is the Norwegian way of building with logs lying on top of each other, notched where the walls meet so the ends
cross and stick out past the corner (the laftehode). The logs of two walls that meet lie half a log apart in height, so
the walls come in two kinds:
  plain   courses centred at 0.25, 0.75, 1.25 and 1.75 m, a whole log lying on the floor
  offset  courses centred at 0, 0.5, 1.0 and 1.5 m: the lowest log is sunk half into what the wall stands on, as the
          half log a real house starts its end walls with, and the log at 2 m is the next piece's lowest
A house is built with the plain wall along one pair of opposite sides and the offset wall along the other. Courses are
0.5 m apart, so every height on the game's 1 m grid starts a course of the same kind: walls, low walls and gables stack.

Every wall lies along x, centred on the origin, from y 0 up, with its centre line (where the snap points are) at z 0.
The doors are 2 x 2 m like the vanilla door and snap in its place. The log ends turn a little about their own axis
(seeded, so every player sees the same) so the texture does not repeat from course to course.
Vanilla mesh facts used below:
  wood_pole_log    core wood log along y, 2.22 m long, radius 0.26 (x) by 0.28 (z); wood_pole_log_4 is 4.44 m long
  wood_floor       2 x 2 m of planks, top at y 0.08, 0.22 thick; wood_floor_1x1 is 1 x 1 m
  wood_beam        along x, 2 m long, 0.4 m square
  dragon_head      the longship's carved head, 0.58 m long along -x, its neck at x -4.48
  rock             fire pit stone, about 1.14 x 0.69 x 0.9 m at scale 1
"""

import math
import random

from build_defenses import box, part, r3, views
from build_defence_extras import plank
from build_gate_controls import IRON_TINT

COURSE = 0.5  # height from one log to the next in a wall
LOG_RADIUS = 0.27  # up and down: the logs overlap 4 cm, as they would sit in their notches
LOG_DEPTH = 0.26  # across the wall
HEAD = 0.4  # how far a laftehode sticks out past the corner
HEAD_INTO_WALL = 0.3  # how far a corner's log ends reach along the wall, under the wall's own logs
HEAD_SWELL = 1.05  # the heads are a little thicker than the wall logs, so the two never draw in the same place
WALL_DEPTH = 0.56  # the wall's collider, a little more than the logs
LOG_SHADES = (0.86, 0.93, 1.0)  # a few shades, not a random one per log: every shade is a material, i.e. a draw call
STONE_SHADES = (0.62, 0.7, 0.78)
DOOR_PLANKS_TINT = [0.82, 0.72, 0.62]
TAR_DOOR_TINT = [0.52, 0.42, 0.34]
BRONZE_TINT = [1.05, 0.72, 0.38]


def log_x(x0, x1, y, rng, z=0.0, swell=1.0, **extra):
    """A laft log from x0 to x1 at height y, turned about its own axis by a seeded amount."""
    length = x1 - x0
    mesh, mesh_length = ("wood_pole_log_4", 4.44) if length > 2.6 else ("wood_pole_log", 2.22)
    shade = rng.choice(LOG_SHADES)
    # (spin, yaw, 90): laid along x by the roll, then spun about the log's own axis.
    return part(mesh, ((x0 + x1) / 2, y, z), (rng.uniform(0, 360), 0, 90),
                (LOG_RADIUS * swell / 0.26, length / mesh_length, LOG_DEPTH * swell / 0.28),
                tint=[shade, shade * 0.98, shade * 0.95], **extra)


def log_z(z0, z1, y, rng, x=0.0, swell=1.0, **extra):
    """A laft log from z0 to z1, i.e. log_x turned to run along z."""
    data = log_x(-z1, -z0, y, rng, swell=swell, **extra)
    data["position"] = r3((x, y, -data["position"][0]))
    data["rotation"] = r3((data["rotation"][0], 90, 90))
    return data


def courses(offset, height):
    """Centre heights of the logs of a wall of the given kind, from the floor up to (not including) its top."""
    first = 0.0 if offset else COURSE / 2
    count = int(round((height - first) / COURSE + 0.4999))
    return [first + i * COURSE for i in range(count) if first + i * COURSE < height - 0.01]


def log_wall(length, height, offset, seed):
    """A straight laft wall along x, centred on the origin."""
    rng = random.Random(seed)
    parts = [log_x(-length / 2, length / 2, y, rng) for y in courses(offset, height)]
    colliders = [box((0, height / 2, 0), (length, height, WALL_DEPTH))]
    snaps = []
    for x in (-length / 2, length / 2):
        snaps += [(x, 0, 0), (x, height, 0)]
    if length > 2:
        snaps += [(0, 0, 0), (0, height, 0)]
    return parts, colliders, snaps


def log_corner(mirrored, seed):
    """A laft corner at the origin, 2 m high: the plain wall's log ends run along x and the offset wall's along z, each
    reaching HEAD past the corner. The walls run from the corner towards +x and +z; mirrored swaps the kinds, for the
    two corners of a house where the plain wall runs along z."""
    rng = random.Random(seed)
    parts = []
    for offset, along_x in ((False, not mirrored), (True, mirrored)):
        for y in courses(offset, 2.0):
            if along_x:
                parts.append(log_x(-HEAD, HEAD_INTO_WALL, y, rng, swell=HEAD_SWELL))
            else:
                parts.append(log_z(-HEAD, HEAD_INTO_WALL, y, rng, swell=HEAD_SWELL))
    reach = HEAD + HEAD_INTO_WALL
    middle = (HEAD_INTO_WALL - HEAD) / 2
    colliders = [box((middle, 1.0, 0), (reach, 2.0, WALL_DEPTH)), box((0, 1.0, middle), (WALL_DEPTH, 2.0, reach))]
    snaps = [(0, 0, 0), (0, 2.0, 0)]
    return parts, colliders, snaps


def log_joint(seed):
    """Where an inner wall meets an outer one (krysslaft): the inner wall's log ends run along z through the outer wall
    and stick out HEAD on its outside. The outer wall runs along x through the origin; the inner wall comes from +z."""
    rng = random.Random(seed)
    parts = [log_z(-HEAD - WALL_DEPTH / 2, HEAD_INTO_WALL, y, rng, swell=HEAD_SWELL) for y in courses(True, 2.0)]
    reach = HEAD + WALL_DEPTH / 2 + HEAD_INTO_WALL
    colliders = [box((0, 1.0, HEAD_INTO_WALL - reach / 2), (WALL_DEPTH, 2.0, reach))]
    snaps = [(0, 0, 0), (0, 2.0, 0)]
    return parts, colliders, snaps


def log_gable(rise, seed):
    """Half a gable of offset logs, 2 m along x: its top runs from y 0 at x -1 (the eave, at the corner) to y rise at
    x +1, the slope of the vanilla 26 or 45 degree roof. Every log ends where the slope meets its top, a barge board
    covers the stepped ends, and the lowest log reaches HEAD past the corner like the corner's logs below it."""
    rng = random.Random(seed)

    def slope_x(y):
        return 2 * y / rise - 1

    parts, colliders = [], []
    for y in courses(True, rise):
        x0 = -1 - HEAD if y == 0 else slope_x(min(y + LOG_RADIUS * 0.75, rise))
        if x0 >= 0.95:
            continue
        parts.append(log_x(x0, 1, y, rng, swell=HEAD_SWELL if y == 0 else 1.0))
        bottom = max(0.0, y - COURSE / 2)
        top = min(rise, y + COURSE / 2)
        start = max(-1.0, slope_x(top)) if y > 0 else -1.0
        colliders.append(box(((start + 1) / 2, (bottom + top) / 2, 0), (1 - start, top - bottom, WALL_DEPTH)))
    # Barge boards on both faces along the slope, and a rafter on top between them that the roof lies on.
    for z in (WALL_DEPTH / 2 + 0.02, -WALL_DEPTH / 2 - 0.02):
        parts.append(plank((-1.05, -0.05, z), (1.0, rise - 0.05, z), 0.06, 0.32))
    parts.append(plank((-1.0, -0.06, 0), (1.0, rise - 0.06, 0), WALL_DEPTH, 0.2))
    snaps = [(-1, 0, 0), (1, 0, 0), (1, rise, 0)]
    return parts, colliders, snaps


def foundation(length, seed, height=1.0, depth=0.7):
    """A dry-laid foundation wall (grunnmur) along x, from 0.6 m under the ground up to height: courses of big field
    stones, each set off from the one below, round a dark core of stone that fills the gaps. What is under the ground
    shows where the ground falls away."""
    rng = random.Random(seed)
    bottom = -0.6
    rows = round((height - bottom) / 0.4)
    course = (height - bottom) / rows
    parts = [part("stonebox", (0, (bottom + height) / 2 - 0.12, 0), scale=(length - 0.3, height - bottom - 0.25, depth * 0.55),
                  tint=[0.32, 0.31, 0.3])]
    for row in range(rows):
        y = bottom + row * course
        x = -length / 2
        first = rng.uniform(0.25, 0.4) if row % 2 else None
        while x < length / 2 - 0.05:
            stone = first or rng.uniform(0.5, 0.75)
            first = None
            if length / 2 - x - stone < 0.3:
                stone = length / 2 - x
            size = (stone * 1.12 / 1.14, course * 1.45 / 0.69, depth * rng.uniform(0.98, 1.06) / 0.9)
            shade = rng.choice(STONE_SHADES)
            parts.append(part("rock", (x + stone / 2, y + 0.1 * size[1], rng.uniform(-0.02, 0.02)),
                              (rng.uniform(-3, 3), rng.choice((0, 180)) + rng.uniform(-4, 4), rng.uniform(-3, 3)),
                              size, tint=[shade, shade, shade * 0.96]))
            x += stone
    colliders = [box((0, (height + bottom) / 2, 0), (length, height - bottom, depth))]
    snaps = []
    for x in (-length / 2, length / 2):
        snaps += [(x, 0, 0), (x, height, 0)]
    if length > 2:
        snaps += [(0, height, 0)]
    return parts, colliders, snaps


def cornerstone(seed, height=1.0):
    """A big squared stone for the corner of a foundation, or to stand alone under a floor beam."""
    rng = random.Random(seed)
    parts = []
    y = -0.6
    while y < height - 0.05:
        course = min(rng.uniform(0.4, 0.55), height - y)
        size = (0.85 / 1.14 * rng.uniform(1.0, 1.08), course * 1.12 / 0.69, 0.85 / 0.9 * rng.uniform(1.0, 1.08))
        shade = rng.choice(STONE_SHADES)
        parts.append(part("rock", (0, y + 0.14 * size[1], 0), (rng.uniform(-3, 3), rng.uniform(0, 360), rng.uniform(-3, 3)), size,
                          tint=[shade, shade, shade * 0.96]))
        y += course
    colliders = [box((0, (height - 0.6) / 2, 0), (0.8, height + 0.6, 0.8))]
    snaps = [(0, 0, 0), (0, height, 0)]
    return parts, colliders, snaps


def plank_floor(width, depth):
    """A plank floor of width x depth metres, tiled from the vanilla 2 x 2 m floor (1 x 1 m where a side is odd), so the
    boards keep their size; its top is at y 0.08 like the vanilla floor."""
    parts = []
    tile = 2 if width % 2 == 0 and depth % 2 == 0 else 1
    mesh = "wood_floor" if tile == 2 else "wood_floor_1x1"
    for i in range(width // tile):
        for j in range(depth // tile):
            parts.append(part(mesh, (-width / 2 + tile * (i + 0.5), 0, -depth / 2 + tile * (j + 0.5))))
    colliders = [box((0, -0.03, 0), (width, 0.22, depth))]
    snaps = [(x, 0, z) for x in (-width / 2, width / 2) for z in (-depth / 2, depth / 2)]
    snaps += [(-width / 2 + i, 0, z) for i in range(1, width) for z in (-depth / 2, depth / 2)]
    snaps += [(x, 0, -depth / 2 + j) for j in range(1, depth) for x in (-width / 2, width / 2)]
    return parts, colliders, snaps


def joisted_floor(width, depth):
    """A plank floor laid on hewn joists 1 m apart, for a loft or over a cellar: the joists show from below."""
    parts, colliders, snaps = plank_floor(width, depth)
    for i in range(width + 1):
        x = -width / 2 + i if 0 < i < width else (-width / 2 + 0.12 if i == 0 else width / 2 - 0.12)
        parts.append(plank((x, -0.27, -depth / 2 + 0.02), (x, -0.27, depth / 2 - 0.02), 0.22, 0.3))
    colliders = [box((0, -0.15, 0), (width, 0.46, depth))]
    return parts, colliders, snaps


# The doors: 2 x 2 m, centred on x, from y 0 to 2, closed in the wall's centre line. The leaf hangs on its hinge at
# x +0.75 and turns with the vanilla door the piece is cloned from, as the gatehouse leaves do (GateLeafDriver).
LEAF_WIDTH = 1.5
LEAF_HEIGHT = 1.98
LEAF_THICKNESS = 0.1


def door_leaf(group, x0, x1, rng, tint, braces="z", hinge_side=1):
    """A board door from x0 to x1: vertical boards, battens on the inside (-z) and iron strap hinges and a ring on the
    outside (+z). hinge_side is +1 when it hangs on its x1 edge."""
    parts = []
    width = x1 - x0
    boards = max(3, round(width / 0.25))
    for i in range(boards):
        bx0 = x0 + width * i / boards
        bx1 = x0 + width * (i + 1) / boards
        shade = rng.choice((0.9, 1.0))
        parts.append(part("wood_floor_1x1", ((bx0 + bx1) / 2, 0.03 + LEAF_HEIGHT / 2, 0), (90, 0, 0),
                          ((bx1 - bx0) * 0.97, LEAF_THICKNESS / 0.22, LEAF_HEIGHT), tint=[tint[0] * shade, tint[1] * shade, tint[2] * shade],
                          group=group))
    inside = -LEAF_THICKNESS / 2 - 0.03
    for y in (0.3, 1.7):
        parts.append(plank((x0 + 0.05, y, inside), (x1 - 0.05, y, inside), 0.06, 0.16, group=group, tint=tint))
    if braces == "z":
        lean = 1 if hinge_side > 0 else -1
        low, high = (x1 - 0.12, x0 + 0.12) if lean > 0 else (x0 + 0.12, x1 - 0.12)
        parts.append(plank((low, 0.4, inside), (high, 1.6, inside), 0.06, 0.14, group=group, tint=tint))
    outside = LEAF_THICKNESS / 2 + 0.015
    hinge = x1 if hinge_side > 0 else x0
    for y in (0.35, 1.65):
        reach = min(width * 0.75, 1.0)
        a, b = (hinge - reach, hinge + 0.02) if hinge_side > 0 else (hinge - 0.02, hinge + reach)
        parts.append(plank((a, y, outside), (b, y, outside), 0.03, 0.07, group=group, tint=IRON_TINT, texture="metalwall", detail=True))
    handle = x0 + 0.15 if hinge_side > 0 else x1 - 0.15
    parts.append(part("Bell", (handle, 1.0, outside + 0.02), (90, 0, 0), (0.09, 0.02, 0.09), tint=IRON_TINT, group=group, detail=True))
    collider = box(((x0 + x1) / 2, 0.03 + LEAF_HEIGHT / 2, 0), (width, LEAF_HEIGHT, LEAF_THICKNESS + 0.06), group=group)
    return parts, collider


def door_frame(rng, post=0.25):
    """Two hewn posts either side of the 1.5 m opening and a threshold, the wall's depth deep."""
    parts = []
    for x in (-1 + post / 2, 1 - post / 2):
        parts.append(plank((x, 0, 0), (x, 2.0, 0), WALL_DEPTH, post, tint=[0.9, 0.85, 0.8]))
    parts.append(plank((-1 + post, 0.02, 0), (1 - post, 0.02, 0), WALL_DEPTH, 0.05, tint=[0.8, 0.75, 0.7]))
    colliders = [box((x, 1.0, 0), (post, 2.0, WALL_DEPTH)) for x in (-1 + post / 2, 1 - post / 2)]
    return parts, colliders


def door_snaps():
    return [(-1, 0, 0), (1, 0, 0), (-1, 2, 0), (1, 2, 0)]


def plank_door():
    """A board door of tarred planks on iron strap hinges, between two hewn posts."""
    rng = random.Random("plankedor")
    parts, colliders = door_frame(rng)
    leaf, leaf_collider = door_leaf("leaf", -LEAF_WIDTH / 2, LEAF_WIDTH / 2, rng, TAR_DOOR_TINT)
    parts += leaf
    colliders.append(leaf_collider)
    groups = [{"name": "leaf", "pivot": [LEAF_WIDTH / 2, 0, 0]}]
    return parts, colliders, door_snaps(), groups


def dragon_portal():
    """A door in a portal of broad boards, as on the old storehouses and stave churches: the posts faced with boards on
    both sides, two dragon heads looking out from their tops and a bronze ring on the leaf. There is no lintel board, so
    the leaf turns freely; the wall above is the lintel."""
    rng = random.Random("dorportal")
    parts, colliders = door_frame(rng, post=0.25)
    face = WALL_DEPTH / 2 + 0.04
    for z in (face, -face):
        for x in (-0.82, 0.82):
            parts.append(plank((x, 0, z), (x, 2.0, z), 0.08, 0.36, tint=[0.95, 0.82, 0.62]))
    # Dragon heads at the ends of the lintel, looking out and away from the door, on the outside only.
    for side in (-1, 1):
        # The head points along -x from its neck at x -4.48; turned so it points out along side * x.
        yaw = 0 if side < 0 else 180
        neck = (side * 0.92, 1.86, face + 0.06)
        offset = -4.48 * 0.55
        parts.append(part("dragon_head", (neck[0] + side * offset, neck[1] - 1.2 * 0.55, neck[2]), (0, yaw, 0), (0.55, 0.55, 0.55),
                          tint=[0.62, 0.5, 0.38]))
    leaf, leaf_collider = door_leaf("leaf", -LEAF_WIDTH / 2, LEAF_WIDTH / 2, rng, [0.78, 0.6, 0.42])
    parts += leaf
    parts.append(part("Bell", (-LEAF_WIDTH / 2 + 0.2, 1.05, LEAF_THICKNESS / 2 + 0.05), (90, 0, 0), (0.16, 0.03, 0.16), tint=BRONZE_TINT,
                      group="leaf", detail=True))
    colliders.append(leaf_collider)
    groups = [{"name": "leaf", "pivot": [LEAF_WIDTH / 2, 0, 0]}]
    return parts, colliders, door_snaps(), groups


def barn_doors():
    """Double barn doors, 4 m wide and 2 m high, for a cart: two leaves hung on the outer posts, opening outwards like
    the gatehouse's."""
    rng = random.Random("lavedor")
    post = 0.3
    parts = []
    for x in (-2 + post / 2, 2 - post / 2):
        parts.append(plank((x, 0, 0), (x, 2.0, 0), WALL_DEPTH, post, tint=[0.9, 0.85, 0.8]))
    colliders = [box((x, 1.0, 0), (post, 2.0, WALL_DEPTH)) for x in (-2 + post / 2, 2 - post / 2)]
    half = 2 - post
    left, left_collider = door_leaf("leaf_left", -half, 0, rng, TAR_DOOR_TINT, hinge_side=-1)
    right, right_collider = door_leaf("leaf_right", 0, half, rng, TAR_DOOR_TINT, hinge_side=1)
    parts += left + right
    colliders += [left_collider, right_collider]
    groups = [{"name": "leaf_left", "pivot": [-half, 0, 0]}, {"name": "leaf_right", "pivot": [half, 0, 0]}]
    snaps = [(x, y, 0) for x in (-2, 0, 2) for y in (0, 2)]
    return parts, colliders, snaps, groups


def piece_entry(name, base, parts, colliders, snaps, **extra):
    data = {"name": name, "base": base, "parts": parts, "colliders": colliders, "snapPoints": sum((r3(p) for p in snaps), [])}
    data.update({key: value for key, value in extra.items() if value})
    return data


WALL_VIEWS = views(("outside", 160, 12), ("end", 90, 10), ("angle", -140, 25))


def log_house_pieces():
    pieces = []
    for kind, offset in (("", False), ("_forskutt", True)):
        for length, suffix in ((2, ""), (1, "_1m"), (4, "_4m")):
            name = f"laftvegg{kind}{suffix}"
            pieces.append(piece_entry(name, "wood_pole_log", *log_wall(length, 2.0, offset, name), views=WALL_VIEWS))
    pieces.append(piece_entry("laftvegg_forskutt_lav", "wood_pole_log", *log_wall(2, 1.0, True, "laftvegg_forskutt_lav"), views=WALL_VIEWS))
    corner_views = views(("outside", -135, 15), ("inside", 45, 25), ("top", -135, 65))
    pieces.append(piece_entry("laftehjorne", "wood_pole_log", *log_corner(False, "laftehjorne"), views=corner_views))
    pieces.append(piece_entry("laftehjorne_speilet", "wood_pole_log", *log_corner(True, "laftehjorne_speilet"), views=corner_views))
    pieces.append(piece_entry("laftekryss", "wood_pole_log", *log_joint("laftekryss"), views=views(("outside", 160, 15), ("side", 90, 15), ("top", 160, 65))))
    pieces.append(piece_entry("laftgavl_26", "wood_pole_log", *log_gable(1.0, "laftgavl_26"), views=WALL_VIEWS))
    pieces.append(piece_entry("laftgavl_45", "wood_pole_log", *log_gable(2.0, "laftgavl_45"), views=WALL_VIEWS))
    pieces.append(piece_entry("grunnmur", "stone_wall_2x1", *foundation(2.0, "grunnmur"), views=WALL_VIEWS))
    pieces.append(piece_entry("grunnmur_4m", "stone_wall_2x1", *foundation(4.0, "grunnmur_4m"), views=WALL_VIEWS))
    pieces.append(piece_entry("hjornestein", "stone_wall_1x1", *cornerstone("hjornestein"), views=views(("front", 160, 15), ("top", 160, 60))))
    floor_views = views(("above", 160, 40), ("below", 160, -25), ("top", 0, 85))
    for width, depth in ((4, 4), (4, 2), (2, 1)):
        pieces.append(piece_entry(f"plankegulv_{width}x{depth}", "wood_floor", *plank_floor(width, depth), views=floor_views))
    pieces.append(piece_entry("bjelkelag_4x4", "wood_floor", *joisted_floor(4, 4), views=floor_views))
    door_views = views(("outside", 180, 10), ("inside", 20, 15), ("angle", 140, 25))
    for name, build, base in (("plankedor", plank_door, "wood_door"), ("dorportal", dragon_portal, "wood_door"), ("lavedor", barn_doors, "wood_gate")):
        parts, colliders, snaps, groups = build()
        pieces.append(piece_entry(name, base, parts, colliders, snaps, groups=groups, keep=["door"], views=door_views))
    pieces.append(overview())
    return pieces


def overview():
    """A small laft house from the pieces, for the documentation: 6 x 4 m on a foundation, door in the long wall."""
    def at(name, x, z, yaw=0, y=0.0):
        data = {"piece": name, "position": r3((x, y, z))}
        if yaw:
            data["rotation"] = [0, yaw, 0]
        return data

    base = 1.0
    parts = []
    # Foundation along the four sides, cornerstones at the corners.
    for x in (-2, 0, 2):
        parts += [at("grunnmur", x, 2), at("grunnmur", x, -2)]
    for z in (-1, 1):
        parts += [at("grunnmur", -3, z, 90), at("grunnmur", 3, z, 90)]
    for x, z in ((-3, -2), (3, -2), (-3, 2), (3, 2)):
        parts.append(at("hjornestein", x, z))
    # Long walls (plain, along x) with the door in the front one; end walls offset, along z.
    parts += [at("laftvegg", -2, 2, y=base), at("plankedor", 0, 2, y=base), at("laftvegg", 2, 2, y=base)]
    parts += [at("laftvegg", -2, -2, y=base), at("laftvegg", 0, -2, y=base), at("laftvegg", 2, -2, y=base)]
    for z in (-1, 1):
        parts += [at("laftvegg_forskutt", -3, z, 90, base), at("laftvegg_forskutt", 3, z, 90, base)]
    # Corners: heads stick out away from the house. The plain kind runs along x, so the corners where the walls run
    # towards +x and +z (and the opposite one) take the plain corner, the other two the mirrored one.
    parts += [at("laftehjorne", -3, -2, 180, base), at("laftehjorne", 3, 2, 0, base)]
    parts += [at("laftehjorne_speilet", 3, -2, 90, base), at("laftehjorne_speilet", -3, 2, -90, base)]
    # Gables over the end walls, rising to the ridge along x 0.
    top = base + 2
    for x in (-3, 3):
        parts += [at("laftgavl_45", x, 1, 90, top), at("laftgavl_45", x, -1, -90, top)]
    parts.append(at("plankegulv_4x4", -1, 0, 0, base + 0.02))
    parts.append(at("plankegulv_4x2", 1, 0, 90, base + 0.02))
    # The vanilla 45 degree roof on the gables, reaching 1 m past them at both ends.
    for x in (-3, -1, 1, 3):
        parts.append(part("wood_roof_45", (x, top + 1, 1)))
        parts.append(part("wood_roof_45", (x, top + 1, -1), (0, 180, 0)))
    return {"name": "lafthus", "parts": parts, "views": views(("front", 160, 15), ("corner", -140, 25), ("top", 160, 60))}
