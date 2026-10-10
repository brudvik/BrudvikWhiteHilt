"""Stave walls and the svalgang, doors 3 m high, and more windows for the log house.

build_defenses.py adds stave_pieces() to the layout it writes; run that script, not this one. The mod builds them in
Pieces/LogHouse.

A stave wall (stavvegg) is tarred boards standing on end between a sill (sville) and a wall plate (stavlegje), as on the
stave churches and the oldest halls; round corner staves stand where two walls meet. The svalgang is the covered
gallery round a stave church: a low board wall, an arcade of small posts and arches, and a lean-to roof against the
church's wall, here of the vanilla dark shingles.

The doors are 3 m high, like the vanilla wood gate (2 x 3 m, its leaf 1.68 x 3 m). The vanilla door's leaf is 1.39 x
1.88 m, a hand over the player's 1.85 m capsule, which is why players build gates instead; these doors leave a metre
over the head, except the stave church portal, whose round-headed door is 2.4 m high like the old portals.
Vanilla mesh facts used below:
  darkwood_roof          2 x 2 m of dark shingles rising 1 m towards -z, its low edge at y 0, z +1
  darkwood_roof_ocorner  the outer corner: its apex at (-1, 1, -1), the other three corners low
"""

import math
import random

from build_defenses import box, part, r3, ramp, views
from build_defence_extras import plank
from build_gate_controls import IRON_TINT
from build_log_house import (BRONZE_TINT, TAR_DOOR_TINT, WALL_DEPTH, WALL_VIEWS, door_frame, door_leaf, log_wall, piece_entry)
from build_log_house_inside import WINDOW_CUT, shutter, wall_with_hole
from build_stone_defenses import span, spans_with_hole

STAVE_TINT = [0.46, 0.36, 0.28]  # tarred boards
TIMBER_TINT = [0.62, 0.5, 0.4]  # sill, plate and posts, tarred less
STAVE_DEPTH = 0.3  # the wall's collider; the boards are 0.08 thick between a sill and plate 0.3 deep
SILL = 0.24
PLATE = 0.2
BOARD = 0.25


def boards(x0, x1, y0, y1, rng, gaps=(), z=0.0):
    """Vertical tarred boards from x0 to x1, standing from y0 to y1, every other one set a little out so the wall has
    some relief. gaps are (x0, x1, y0, y1) openings the boards stop at."""
    parts = []
    count = max(1, round((x1 - x0) / BOARD))
    for i in range(count):
        a = x0 + (x1 - x0) * i / count
        b = x0 + (x1 - x0) * (i + 1) / count
        middle = (a + b) / 2
        spans_y = [(y0, y1)]
        for gx0, gx1, gy0, gy1 in gaps:
            if gx0 < middle < gx1:
                spans_y = [(lo, hi) for lo, hi in ((y0, gy0), (gy1, y1)) if hi - lo > 0.02]
        shade = rng.choice((0.9, 1.0))
        for lo, hi in spans_y:
            parts.append(part("wood_floor_1x1", (middle, (lo + hi) / 2, z + (0.015 if i % 2 else 0)), (90, 0, 0),
                              ((b - a) * 0.98, 0.08 / 0.22, hi - lo), tint=[c * shade for c in STAVE_TINT]))
    return parts


def frame_timbers(length, height):
    """The sill and the wall plate along a wall."""
    return [plank((-length / 2, SILL / 2, 0), (length / 2, SILL / 2, 0), STAVE_DEPTH, SILL, tint=TIMBER_TINT),
            plank((-length / 2, height - PLATE / 2, 0), (length / 2, height - PLATE / 2, 0), STAVE_DEPTH, PLATE, tint=TIMBER_TINT)]


def stave_wall(length, height, seed, gaps=()):
    rng = random.Random(seed)
    parts = frame_timbers(length, height) + boards(-length / 2, length / 2, SILL, height - PLATE, rng, gaps)
    if gaps:
        colliders = spans_with_hole(-length / 2, length / 2, 0, height, -STAVE_DEPTH / 2, STAVE_DEPTH / 2, gaps[0])
    else:
        colliders = [box((0, height / 2, 0), (length, height, STAVE_DEPTH))]
    snaps = [(x, y, 0) for x in (-length / 2, length / 2) for y in (0, height)]
    if length > 2:
        snaps += [(0, 0, 0), (0, height, 0)]
    if height > 2:
        snaps += [(x, 2.0, 0) for x in (-length / 2, length / 2)]
    return parts, colliders, snaps


def stave_window(seed):
    """A 2 m stave wall with a window 1 m wide from 1 m up to the wall plate, round a player's eyes, closed by two
    shutters on the outside."""
    hole = (-0.5, 0.5, 1.0, 1.78)
    parts, colliders, snaps = stave_wall(2.0, 2.0, seed, gaps=(hole,))
    depth = STAVE_DEPTH + 0.02
    for x in (-0.535, 0.535):
        parts.append(plank((x, 0.93, 0), (x, 1.8, 0), depth, 0.07, tint=TIMBER_TINT))
    for y in (0.965,):
        parts.append(plank((-0.57, y, 0), (0.57, y, 0), depth, 0.07, tint=TIMBER_TINT))
    rng = random.Random(seed + "_shutters")
    z = STAVE_DEPTH / 2 + 0.04
    left, left_collider = shutter("leaf_left", -0.5, 0.0, 1.0, 1.78, z, rng)
    right, right_collider = shutter("leaf_right", 0.0, 0.5, 1.0, 1.78, z, rng)
    groups = [{"name": "leaf_left", "pivot": [-0.5, 0, z]}, {"name": "leaf_right", "pivot": [0.5, 0, z]}]
    return parts + left + right, colliders + [left_collider, right_collider], snaps, groups


def stave_glugg(seed):
    """A 2 m stave wall with a small round window high up, as on the stave churches: a ring of short boards round an
    opening 0.4 m across."""
    centre, radius = 1.55, 0.2
    hole = (-radius, radius, centre - radius, centre + radius)
    parts, colliders, snaps = stave_wall(2.0, 2.0, seed, gaps=(hole,))
    # The boards stop at a square hole; a solid ring of eight boards turns it round, on both faces.
    for z in (STAVE_DEPTH / 2 + 0.01, -STAVE_DEPTH / 2 - 0.01):
        for i in range(8):
            a0, a1 = math.radians(45 * i), math.radians(45 * (i + 1))
            p0 = (radius * 1.15 * math.cos(a0), centre + radius * 1.15 * math.sin(a0), z)
            p1 = (radius * 1.15 * math.cos(a1), centre + radius * 1.15 * math.sin(a1), z)
            parts.append(plank(p0, p1, 0.04, 0.12, tint=TIMBER_TINT))
        for cx, cy in ((-1, -1), (1, -1), (1, 1), (-1, 1)):
            parts.append(part("stonebox", (cx * radius * 0.85, centre + cy * radius * 0.85, z), (0, 0, 45), (0.16, 0.16, 0.03), tint=[0.18, 0.15, 0.13]))
    return parts, colliders, snaps


def corner_stave(height, seed):
    """A round corner stave where two stave walls meet, with a cap."""
    rng = random.Random(seed)
    parts = [part("wood_pole_log" if height <= 2.2 else "wood_pole_log_4", (0, height / 2, 0), (0, rng.uniform(0, 360), 0),
                  (0.24 / 0.26, height / (2.22 if height <= 2.2 else 4.44), 0.24 / 0.28), tint=TIMBER_TINT)]
    parts.append(part("stonebox", (0, height + 0.04, 0), (0, 45, 0), (0.4, 0.08, 0.4), tint=[0.3, 0.25, 0.2]))
    colliders = [box((0, height / 2, 0), (0.46, height, 0.46))]
    snaps = [(0, 0, 0), (0, height, 0)] + ([(0, 2.0, 0)] if height > 2 else [])
    return parts, colliders, snaps


def stave_gable(rise, seed):
    """Half a stave gable 2 m along x: boards from the plate up to the roof's slope, rising from y 0 at x -1 to rise at
    x +1, with barge boards and a rafter like the log gables."""
    rng = random.Random(seed)
    parts, colliders = [], []
    count = 8
    for i in range(count):
        a, b = -1 + 2 * i / count, -1 + 2 * (i + 1) / count
        top = rise * (b + 1) / 2
        shade = rng.choice((0.9, 1.0))
        parts.append(part("wood_floor_1x1", ((a + b) / 2, top / 2, 0.015 if i % 2 else 0), (90, 0, 0),
                          ((b - a) * 0.98, 0.08 / 0.22, top), tint=[c * shade for c in STAVE_TINT]))
        colliders.append(box(((a + b) / 2, top / 2, 0), (b - a, top, STAVE_DEPTH)))
    for z in (STAVE_DEPTH / 2 + 0.02, -STAVE_DEPTH / 2 - 0.02):
        parts.append(plank((-1.05, -0.05, z), (1.0, rise - 0.05, z), 0.05, 0.28, tint=TIMBER_TINT))
    parts.append(plank((-1.0, -0.06, 0), (1.0, rise - 0.06, 0), STAVE_DEPTH, 0.18, tint=TIMBER_TINT))
    snaps = [(-1, 0, 0), (1, 0, 0), (1, rise, 0)]
    return parts, colliders, snaps


# The svalgang stands on the +z side of a wall along x through the origin, reaching GALLERY out from its centre line.
GALLERY = 1.8
EAVE = 2.25
ROOF_SCALE = GALLERY / 2  # the dark shingles rise 1 m over 2 m; scaled, the gallery's roof keeps that slope


def gallery_side(x0, x1, rng, z=GALLERY - 0.1, along_z=False):
    """The outer side of the gallery from x0 to x1: a board wall to 1 m with a cap, posts on it and arches between them
    under a plate at the eave. along_z builds it along z at x = z instead (for the corner)."""
    parts = []

    def at(x, y, depth):
        return (depth, y, x) if along_z else (x, y, depth)

    count = max(1, round((x1 - x0) / BOARD))
    for i in range(count):
        a = x0 + (x1 - x0) * i / count
        b = x0 + (x1 - x0) * (i + 1) / count
        shade = rng.choice((0.9, 1.0))
        rotation = (90, 90, 0) if along_z else (90, 0, 0)
        parts.append(part("wood_floor_1x1", at((a + b) / 2, 0.55, z), rotation, ((b - a) * 0.98, 0.07 / 0.22, 0.9),
                          tint=[c * shade for c in STAVE_TINT]))
    parts.append(plank(at(x0, 0.12, z), at(x1, 0.12, z), 0.22, 0.2, tint=TIMBER_TINT))
    parts.append(plank(at(x0, 1.03, z), at(x1, 1.03, z), 0.2, 0.07, tint=TIMBER_TINT))
    parts.append(plank(at(x0, EAVE - 0.12, z), at(x1, EAVE - 0.12, z), 0.18, 0.16, tint=TIMBER_TINT))
    posts = [x0 + (x1 - x0) * (i + 0.5) / round((x1 - x0) / 0.5) for i in range(round((x1 - x0) / 0.5))]
    for x in posts:
        parts.append(plank(at(x, 1.06, z), at(x, EAVE - 0.2, z), 0.1, 0.1, tint=TIMBER_TINT))
    # Round-headed arches between the posts: four short boards each, from post to post under the plate.
    spring = EAVE - 0.5
    for a, b in zip(posts, posts[1:]):
        middle, half = (a + b) / 2, (b - a) / 2
        points = [(middle - half * math.cos(t), spring + 0.3 * math.sin(t)) for t in (0, math.pi / 4, math.pi / 2, 3 * math.pi / 4, math.pi)]
        for (pa, ya), (pb, yb) in zip(points, points[1:]):
            parts.append(plank(at(pa, ya, z), at(pb, yb, z), 0.06, 0.05, tint=TIMBER_TINT, detail=True))
    return parts, posts


def gallery(seed):
    """A 2 m section of svalgang along a wall: deck, outer side, rafters and a lean-to of dark shingles that meets the
    wall at 3.15 m, so it wants a wall at least that high (the tall stave wall)."""
    rng = random.Random(seed)
    parts = [part("wood_floor", (0, 0, 0.15 + (GALLERY - 0.15) / 2), scale=(1, 1, (GALLERY - 0.15) / 2), tint=[0.75, 0.68, 0.6])]
    side, posts = gallery_side(-1, 1, rng)
    parts += side
    for x in (-0.75, -0.25, 0.25, 0.75):
        parts.append(plank((x, EAVE + 0.02, GALLERY + 0.15), (x, EAVE + ROOF_SCALE + 0.02, 0.0), 0.08, 0.12, tint=TIMBER_TINT))
    parts.append(part("darkwood_roof", (0, EAVE + 0.1, GALLERY - ROOF_SCALE), scale=(1, ROOF_SCALE, ROOF_SCALE)))
    colliders = [box((0, -0.03, 0.15 + (GALLERY - 0.15) / 2), (2.0, 0.22, GALLERY - 0.15)),
                 box((0, 0.55, GALLERY - 0.1), (2.0, 1.1, 0.22)),
                 ramp((0, EAVE + 0.1, GALLERY), (0, EAVE + 0.1 + ROOF_SCALE, 0.0), 2.0, 0.15)]
    colliders += [box((x, (1.06 + EAVE) / 2, GALLERY - 0.1), (0.12, EAVE - 1.06, 0.12)) for x in posts]
    snaps = [(x, 0, z) for x in (-1, 1) for z in (0, GALLERY)]
    return parts, colliders, snaps


def gallery_corner(seed):
    """The outer corner of the svalgang, where the galleries along two walls meet: the walls run along -x and -z from
    the origin, the galleries on their +z and +x sides, and this fills the square between them under a hipped corner of
    dark shingles."""
    rng = random.Random(seed)
    middle = 0.15 + (GALLERY - 0.15) / 2
    parts = [part("wood_floor", (middle, 0, middle), scale=((GALLERY - 0.15) / 2, 1, (GALLERY - 0.15) / 2), tint=[0.75, 0.68, 0.6])]
    side_z, posts_z = gallery_side(0, GALLERY, rng)
    side_x, posts_x = gallery_side(0, GALLERY - 0.2, rng, along_z=True)
    parts += side_z + side_x
    parts.append(plank((GALLERY - 0.1, 0, GALLERY - 0.1), (GALLERY - 0.1, EAVE, GALLERY - 0.1), 0.16, 0.16, tint=TIMBER_TINT))
    parts.append(part("darkwood_roof_ocorner", (ROOF_SCALE, EAVE + 0.1, ROOF_SCALE), scale=(ROOF_SCALE,) * 3))
    colliders = [box((middle, -0.03, middle), (GALLERY - 0.15, 0.22, GALLERY - 0.15)),
                 box((GALLERY / 2, 0.55, GALLERY - 0.1), (GALLERY, 1.1, 0.22)),
                 box((GALLERY - 0.1, 0.55, GALLERY / 2), (0.22, 1.1, GALLERY))]
    colliders += [box((x, (1.06 + EAVE) / 2, GALLERY - 0.1), (0.12, EAVE - 1.06, 0.12)) for x in posts_z]
    colliders += [box((GALLERY - 0.1, (1.06 + EAVE) / 2, z), (0.12, EAVE - 1.06, 0.12)) for z in posts_x]
    snaps = [(0, 0, 0), (0, 0, GALLERY), (GALLERY, 0, 0), (GALLERY, 0, GALLERY)]
    return parts, colliders, snaps


# Doors 3 m high, from y 0 to 3, closed in the wall's centre line.
TALL = 3.0


def tall_plank_door():
    """A board door 1.5 m wide and 3 m high between hewn posts: a metre over the head, like the vanilla gate."""
    rng = random.Random("hoy_plankedor")
    parts, colliders = door_frame(rng, height=TALL)
    leaf, collider = door_leaf("leaf", -0.75, 0.75, rng, TAR_DOOR_TINT, height=TALL - 0.02)
    groups = [{"name": "leaf", "pivot": [0.75, 0, 0]}]
    return parts + leaf, colliders + [collider], tall_snaps(2), groups


def double_door():
    """Two narrow leaves 0.8 m wide and 3 m high, opening from the middle, between posts 0.2 m wide."""
    rng = random.Random("dobbeldor")
    parts, colliders = door_frame(rng, post=0.2, height=TALL)
    left, left_collider = door_leaf("leaf_left", -0.8, 0, rng, TAR_DOOR_TINT, hinge_side=-1, height=TALL - 0.02)
    right, right_collider = door_leaf("leaf_right", 0, 0.8, rng, TAR_DOOR_TINT, hinge_side=1, height=TALL - 0.02)
    groups = [{"name": "leaf_left", "pivot": [-0.8, 0, 0]}, {"name": "leaf_right", "pivot": [0.8, 0, 0]}]
    return parts + left + right, colliders + [left_collider, right_collider], tall_snaps(2), groups


def hall_doors():
    """The hall's great doors: two leaves 1.7 m wide and 3 m high on hewn posts, with long iron straps."""
    rng = random.Random("haldor")
    post = 0.3
    parts = [plank((x, 0, 0), (x, TALL, 0), WALL_DEPTH, post, tint=[0.9, 0.85, 0.8]) for x in (-2 + post / 2, 2 - post / 2)]
    colliders = [box((x, TALL / 2, 0), (post, TALL, WALL_DEPTH)) for x in (-2 + post / 2, 2 - post / 2)]
    half = 2 - post
    left, left_collider = door_leaf("leaf_left", -half, 0, rng, TAR_DOOR_TINT, hinge_side=-1, height=TALL - 0.02)
    right, right_collider = door_leaf("leaf_right", 0, half, rng, TAR_DOOR_TINT, hinge_side=1, height=TALL - 0.02)
    # A third strap across the middle of each leaf.
    for group, a, b in (("leaf_left", -half + 0.02, -0.4), ("leaf_right", 0.4, half - 0.02)):
        parts.append(plank((a, 1.5, 0.065), (b, 1.5, 0.065), 0.03, 0.07, group=group, tint=IRON_TINT, texture="metalwall", detail=True))
    groups = [{"name": "leaf_left", "pivot": [-half, 0, 0]}, {"name": "leaf_right", "pivot": [half, 0, 0]}]
    return parts + left + right, colliders + [left_collider, right_collider], tall_snaps(4), groups


def stave_church_portal():
    """A portal as on the stave churches: broad boards up both sides of a narrow door with a round head, dragon heads
    looking out at the top and a bronze ring. The door is 1.2 m wide and 2.4 m high; the round head over it is fixed."""
    rng = random.Random("stavkirkeportal")
    half, spring = 0.6, 2.4
    parts = []
    for x in (-1 + 0.2, 1 - 0.2):
        parts.append(plank((x, 0, 0), (x, TALL, 0), STAVE_DEPTH + 0.06, 0.4, tint=TIMBER_TINT))
    # The head: boards over the spring line, with a ring of short boards round the arch on both faces.
    parts += boards(-half, half, spring, TALL, rng)
    for z in (STAVE_DEPTH / 2 + 0.04, -STAVE_DEPTH / 2 - 0.04):
        points = [(-half * math.cos(t), spring + half * math.sin(t) * 0.9) for t in [math.pi * i / 6 for i in range(7)]]
        for (xa, ya), (xb, yb) in zip(points, points[1:]):
            parts.append(plank((xa, ya, z), (xb, yb, z), 0.06, 0.12, tint=BRONZE_TINT if z > 0 else TIMBER_TINT))
        for x in (-0.8, 0.8):
            parts.append(plank((x, 0, z), (x, TALL, z), 0.06, 0.32, tint=[0.7, 0.56, 0.42]))
    for side in (-1, 1):
        neck = (side * 0.85, TALL - 0.12, STAVE_DEPTH / 2 + 0.1)
        offset = -4.48 * 0.55
        parts.append(part("dragon_head", (neck[0] + side * offset, neck[1] - 1.2 * 0.55, neck[2]), (0, 0 if side < 0 else 180, 0),
                          (0.55, 0.55, 0.55), tint=[0.62, 0.5, 0.38]))
    # Half-columns either side of the door on the outside, with a round base and a capital, as on Borgund's west
    # portal.
    for x in (-half - 0.06, half + 0.06):
        z = STAVE_DEPTH / 2 + 0.09
        parts.append(part("wood_pole_log", (x, 1.25, z), scale=(0.06 / 0.26, 2.1 / 2.22, 0.06 / 0.28), tint=[0.78, 0.62, 0.44]))
        for y, radius in ((0.12, 0.09), (0.24, 0.075), (2.3, 0.08), (2.4, 0.1)):
            parts.append(part("wood_pole_log", (x, y, z), scale=(radius / 0.26, 0.08 / 2.22, radius / 0.28), tint=[0.78, 0.62, 0.44], detail=True))
    leaf, collider = door_leaf("leaf", -half, half, rng, [0.7, 0.52, 0.36], height=spring - 0.02)
    leaf.append(part("Bell", (-half + 0.2, 1.1, 0.1), (90, 0, 0), (0.16, 0.03, 0.16), tint=BRONZE_TINT, group="leaf", detail=True))
    colliders = [box((x, TALL / 2, 0), (0.4, TALL, STAVE_DEPTH)) for x in (-0.8, 0.8)]
    colliders.append(box((0, (spring + TALL) / 2, 0), (2 * half, TALL - spring, STAVE_DEPTH)))
    groups = [{"name": "leaf", "pivot": [half, 0, 0]}]
    return parts + leaf, colliders + [collider], tall_snaps(2), groups


def tall_snaps(width):
    return [(x, y, 0) for x in (-width / 2, width / 2) for y in (0, 2.0, TALL)]


def narrow_window(offset, seed):
    """A 2 m log wall with a narrow window 0.6 m wide closed by one shutter that swings out: three logs high in the plain
    wall (0.52 to 2 m), two in the offset wall (0.77 to 1.73 m), so it never sits under the eyes."""
    cut = [0.75, 1.25, 1.75] if not offset else [1.0, 1.5]
    parts, colliders, snaps, (bottom, top) = wall_with_hole(offset, 0.3, cut, seed)
    rng = random.Random(seed + "_shutter")
    z = WALL_DEPTH / 2 + 0.04
    leaf, collider = shutter("leaf_right", -0.3, 0.3, bottom, top, z, rng)
    groups = [{"name": "leaf_right", "pivot": [0.3, 0, z]}]
    return parts + leaf, colliders + [collider], snaps, groups


def barred_window(offset, seed):
    """A 2 m log wall with a window 1 m wide behind iron bars, for a storehouse or a smithy."""
    parts, colliders, snaps, (bottom, top) = wall_with_hole(offset, 0.5, WINDOW_CUT[offset], seed)
    for i in range(4):
        x = -0.375 + 0.25 * i
        parts.append(plank((x, bottom, 0), (x, top, 0), 0.035, 0.035, tint=IRON_TINT, texture="metalwall"))
    parts.append(plank((-0.5, (bottom + top) / 2, 0), (0.5, (bottom + top) / 2, 0), 0.035, 0.05, tint=IRON_TINT, texture="metalwall"))
    colliders.append(span(-0.5, 0.5, bottom, top, -0.03, 0.03))
    return parts, colliders, snaps


def stave_pieces():
    pieces = []
    for name, length, height in (("stavvegg", 2, 2.0), ("stavvegg_1m", 1, 2.0), ("stavvegg_4m", 4, 2.0), ("stavvegg_hoy", 2, 4.0), ("stavvegg_lav", 2, 1.0)):
        pieces.append(piece_entry(name, "wood_wall_roof", *stave_wall(length, height, name), views=WALL_VIEWS))
    pieces.append(piece_entry("hjornestav", "wood_pole2", *corner_stave(2.0, "hjornestav"), views=views(("front", 160, 15))))
    pieces.append(piece_entry("hjornestav_4m", "wood_pole2", *corner_stave(4.0, "hjornestav_4m"), views=views(("front", 160, 15))))
    pieces.append(piece_entry("stavgavl_26", "wood_wall_roof", *stave_gable(1.0, "stavgavl_26"), views=WALL_VIEWS))
    pieces.append(piece_entry("stavgavl_45", "wood_wall_roof", *stave_gable(2.0, "stavgavl_45"), views=WALL_VIEWS))
    window_views = views(("outside", 180, 10), ("inside", 0, 10), ("angle", 140, 25))
    parts, colliders, snaps, groups = stave_window("stavvegg_vindu")
    pieces.append(piece_entry("stavvegg_vindu", "wood_door", parts, colliders, snaps, groups=groups, keep=["door"], views=window_views))
    pieces.append(piece_entry("stavvegg_glugg", "wood_wall_roof", *stave_glugg("stavvegg_glugg"), views=WALL_VIEWS))
    gallery_views = views(("outside", 160, 15), ("end", 90, 10), ("angle", 130, 30))
    pieces.append(piece_entry("svalgang", "wood_floor", *gallery("svalgang"), views=gallery_views))
    pieces.append(piece_entry("svalgang_hjorne", "wood_floor", *gallery_corner("svalgang_hjorne"), views=gallery_views))
    door_views = views(("outside", 180, 10), ("inside", 20, 15), ("angle", 140, 25))
    for name, build, base in (("hoy_plankedor", tall_plank_door, "wood_door"), ("dobbeldor", double_door, "wood_gate"),
                              ("haldor", hall_doors, "wood_gate"), ("stavkirkeportal", stave_church_portal, "wood_door")):
        parts, colliders, snaps, groups = build()
        pieces.append(piece_entry(name, base, parts, colliders, snaps, groups=groups, keep=["door"], views=door_views))
    pieces.append(piece_entry("laftvegg_lav", "wood_pole_log", *log_wall(2, 1.0, False, "laftvegg_lav"), views=WALL_VIEWS))
    for kind, offset in (("", False), ("_forskutt", True)):
        parts, colliders, snaps, groups = narrow_window(offset, f"laftvegg{kind}_smal_vindu")
        pieces.append(piece_entry(f"laftvegg{kind}_smal_vindu", "wood_door", parts, colliders, snaps, groups=groups, keep=["door"], views=window_views))
        pieces.append(piece_entry(f"laftvegg{kind}_sprosser", "wood_pole_log", *barred_window(offset, f"laftvegg{kind}_sprosser"), views=window_views))
    pieces.append(overview())
    return pieces


def overview():
    """A small stave hall for the documentation: tall stave walls with corner staves, the svalgang along the front and
    one end, a stave church portal and the gables under a roof of dark shingles."""
    def at(name, x, z, yaw=0, y=0.0):
        data = {"piece": name, "position": r3((x, y, z))}
        if yaw:
            data["rotation"] = [0, yaw, 0]
        return data

    parts = []
    # A hall 6 x 4 m: front wall along x at z 2, back at z -2, ends along z at x -3 and 3.
    parts += [at("stavvegg_hoy", -2, 2), at("stavvegg_hoy", 2, 2), at("stavkirkeportal", 0, 2), at("stavvegg_lav", 0, 2, 0, 3.0)]
    parts += [at("stavvegg_hoy", x, -2) for x in (-2, 0, 2)]
    parts += [at("stavvegg_hoy", x, z, 90) for x in (-3, 3) for z in (-1, 1)]
    parts += [at("hjornestav_4m", x, z) for x in (-3, 3) for z in (-2, 2)]
    for x in (-3, 3):
        parts += [at("stavgavl_26", x, 1, 90, 4.0), at("stavgavl_26", x, -1, -90, 4.0)]
    # The dark shingles at 26 degrees, from the eaves at 4 m to the ridge at 5 m, 1 m past the gables.
    for x in (-3, -1, 1, 3):
        parts.append(part("darkwood_roof", (x, 4.0, 1)))
        parts.append(part("darkwood_roof", (x, 4.0, -1), (0, 180, 0)))
    # The svalgang along the front and round the right-hand corner.
    parts += [at("svalgang", x, 2) for x in (-2, 0, 2)]
    parts.append(at("svalgang_hjorne", 3, 2, 0))
    parts += [at("svalgang", 3, z, 90) for z in (1, -1)]
    return {"name": "stavhall", "parts": parts, "views": views(("front", 150, 15), ("corner", -140, 25), ("top", 150, 60))}
