"""Pieces after Borgund stave church (about 1180, Lærdal): ridge dragons, gable crosses, ridge crests, a ridge turret,
shingled stave walls, St Andrew's crosses, arcade arches, nave columns with masks, scissor trusses, a svalgang porch, a
free-standing bell tower and consecration crosses.

build_defenses.py adds borgund_pieces() to the layout it writes; run that script, not this one. The mod builds them in
Pieces/LogHouse.

What Borgund has, and how it is made here from vanilla meshes:
  dragon heads on the gables   the longship's dragon head on a tall curved neck of boards with a crest along its back
  crosses on the gables        a cross of boards with round ends on a short post
  carved ridge crests          a band of crossed laths between two rails, with round knobs along the top
  ridge turret                 a boarded shaft, three tiers of dark shingles, an arcaded belfry, dragon heads on the
                               lowest tier and a spire with a cross
  scale shingles on the walls  the vanilla dark shingle roof stood upright: tilted 63.4 degrees its slope is vertical
  St Andrew's crosses          crossed boards with a round sun in the middle and round lobes down the arms, the
                               carved leaves of the originals
  arcade and columns           round arches of short boards; round columns with a base, a cushion capital and a mask
                               (the skeleton trophy's skull, in wood)
  scissor trusses              crossed rafters under a 45 degree roof
  svalgang porches             a small steep gable over a gap in the svalgang, with lattice and a cross
  bell tower (stopul)          a stave tower with a shingled middle, an open belfry with the bell (it rings like the
                               alarm bell) and a pyramid roof
  consecration crosses         a painted cross in a ring on the wall
"""

import math
import random

from build_defenses import box, part, r3, views
from build_defence_extras import plank
from build_gate_controls import IRON_TINT
from build_log_house import WALL_VIEWS, piece_entry
from build_stave import EAVE, GALLERY, ROOF_SCALE, STAVE_DEPTH, STAVE_TINT, TIMBER_TINT, boards, gallery_side, stave_wall

CARVED_TINT = [0.66, 0.52, 0.38]  # carved work, tarred lighter than the walls
HEAD_TINT = [0.3, 0.22, 0.16]  # the dragon head's own texture is lighter than the boards
PAINT_RED = [0.62, 0.2, 0.14]
PAINT_OCHRE = [0.85, 0.62, 0.28]
SHINGLE_TILT = math.degrees(math.atan(2.0))  # 63.43: the dark shingles rise 1 m over 2 m, so this stands them upright
SHINGLE_SCALE = 0.697  # scales the upright shingles to cover 2 m of wall, their overhang included
SHINGLE_BACK = 0.161  # how far the scaled shingles' back lies in front of their origin


def euler_matrix(rotation):
    """Unity's rotation matrix for Euler angles in degrees: z first, then x, then y."""
    x, y, z = (math.radians(a) for a in rotation)
    rx = [[1, 0, 0], [0, math.cos(x), -math.sin(x)], [0, math.sin(x), math.cos(x)]]
    ry = [[math.cos(y), 0, math.sin(y)], [0, 1, 0], [-math.sin(y), 0, math.cos(y)]]
    rz = [[math.cos(z), -math.sin(z), 0], [math.sin(z), math.cos(z), 0], [0, 0, 1]]

    def mul(a, b):
        return [[sum(a[i][k] * b[k][j] for k in range(3)) for j in range(3)] for i in range(3)]

    return mul(ry, mul(rx, rz))


def place(mesh, anchor_local, anchor_world, rotation, scale, **extra):
    """A mesh turned and scaled so that its point anchor_local (in the mesh's own space) lands on anchor_world."""
    m = euler_matrix(rotation)
    scaled = [a * s for a, s in zip(anchor_local, scale)]
    turned = [sum(m[i][k] * scaled[k] for k in range(3)) for i in range(3)]
    return part(mesh, [w - t for w, t in zip(anchor_world, turned)], rotation, scale, **extra)


def dragon_head(neck, direction_yaw, lift, size, **extra):
    """The longship's dragon head and the top of its neck, the foot of that neck at neck, looking out along
    direction_yaw (0 is +z) and lift degrees up."""
    # The head looks along -x; the foot of its own neck is at (-4.5, 0.86, 0). Yaw 90 turns -x to +z, a negative roll
    # lifts it.
    return place("dragon_head", (-4.5, 0.86, 0), neck, (0, 90 + direction_yaw, -lift), (size, size, size), **extra)


def pole(p0, p1, radius, **extra):
    """A smooth round pole of core wood from p0 to p1."""
    d = [b - a for a, b in zip(p0, p1)]
    length = math.sqrt(sum(v * v for v in d))
    tilt = math.degrees(math.acos(max(-1.0, min(1.0, d[1] / length))))
    yaw = math.degrees(math.atan2(d[0], d[2]))
    middle = [(a + b) / 2 for a, b in zip(p0, p1)]
    return part("wood_pole_log", middle, (tilt, yaw, 0), (radius / 0.26, length / 2.22, radius / 0.28), **extra)


def disc(centre, radius, thickness, facing="z", **extra):
    """A round disc, a thin slice of a log: facing is the axis it faces along."""
    rotation = {"z": (90, 0, 0), "x": (0, 0, 90), "y": (0, 0, 0)}[facing]
    return part("wood_pole_log", centre, rotation, (radius / 0.26, thickness / 2.22, radius / 0.28), **extra)


def shingles(x, y, z, width=1.0, height=1.0, **extra):
    """Upright dark scale shingles, width wide and height high, their back against z and their foot at y. A metre square
    keeps the shingles near the size of Borgund's, half the vanilla roof's."""
    k = height / 2
    return part("darkwood_roof", (x, y + 1.055 * k, z - SHINGLE_BACK * k), (SHINGLE_TILT, 0, 0), (width / 2, SHINGLE_SCALE * k, SHINGLE_SCALE * k),
                **extra)


def shingled_wall(height, seed):
    """A stave wall whose outside (+z) is clad in scale shingles, as Borgund's walls and gables are."""
    parts, colliders, snaps = stave_wall(2, height, seed)
    face = STAVE_DEPTH / 2 + 0.01
    for y in range(int(height)):
        for x in (-0.5, 0.5):
            parts.append(shingles(x, y, face))
    colliders = [box((0, height / 2, 0.08), (2, height, STAVE_DEPTH + 0.16))]
    return parts, colliders, snaps


def ridge_dragon(seed):
    """A dragon on the end of a ridge, as on Borgund's gables: a neck of boards rising from the ridge and curving out
    (+z, past the gable) to the head, with a crest of small boards along its back. Its foot sits on the ridge at the
    origin."""
    rng = random.Random(seed)
    parts = [part("stonebox", (0, 0.05, -0.2), scale=(0.16, 0.12, 0.7), tint=[0.3, 0.24, 0.18])]
    # The neck: a curve from the ridge up and out, as a chain of boards.
    points = []
    for i in range(9):
        t = i / 8
        points.append((0, 0.1 + 1.2 * math.sin(t * math.pi / 2), -0.45 + 0.6 * (1 - math.cos(t * math.pi / 2)) + 0.2 * t))
    for a, b in zip(points, points[1:]):
        parts.append(pole(a, b, 0.1 - 0.03 * (a[1] / 1.3), tint=HEAD_TINT))
    for a, b in zip(points[1:-1], points[2:]):
        middle = [(p + q) / 2 for p, q in zip(a, b)]
        parts.append(plank((0, middle[1] + 0.05, middle[2] - 0.1), (0, middle[1] + 0.22, middle[2] - 0.2), 0.03, 0.06, tint=HEAD_TINT, detail=True))
    top = points[-1]
    parts.append(dragon_head((0, top[1] - 0.03, top[2]), 0, 15, 1.3, tint=HEAD_TINT))
    colliders = [box((0, 0.8, 0), (0.2, 1.6, 0.6))]
    snaps = [(0, 0, 0)]
    return parts, colliders, snaps


def gable_cross(seed):
    """A cross on the top of a gable: a short post and a cross of boards with a round knob at each end."""
    parts = [plank((0, 0, 0), (0, 1.35, 0), 0.1, 0.1, tint=CARVED_TINT)]
    parts.append(plank((-0.32, 0.95, 0), (0.32, 0.95, 0), 0.08, 0.09, tint=CARVED_TINT))
    for centre in ((0, 1.38, 0), (-0.34, 0.95, 0), (0.34, 0.95, 0)):
        parts.append(disc(centre, 0.06, 0.08, tint=CARVED_TINT))
    parts.append(part("stonebox", (0, 0.04, 0), scale=(0.22, 0.08, 0.22), tint=[0.3, 0.24, 0.18]))
    colliders = [box((0, 0.7, 0), (0.7, 1.4, 0.12))]
    return parts, colliders, [(0, 0, 0)]


def ridge_crest(seed):
    """2 m of carved ridge crest along x: crossed laths between a low rail and a top rail, knobs along the top."""
    parts = [plank((-1, 0.04, 0), (1, 0.04, 0), 0.08, 0.08, tint=CARVED_TINT), plank((-1, 0.34, 0), (1, 0.34, 0), 0.05, 0.05, tint=CARVED_TINT)]
    for i in range(8):
        x0 = -1 + 0.25 * i
        parts.append(plank((x0, 0.06, 0), (x0 + 0.25, 0.32, 0), 0.03, 0.04, tint=CARVED_TINT, detail=True))
        parts.append(plank((x0 + 0.25, 0.06, 0), (x0, 0.32, 0), 0.03, 0.04, tint=CARVED_TINT, detail=True))
        parts.append(disc((x0 + 0.125, 0.42, 0), 0.05, 0.04, tint=CARVED_TINT, detail=True))
    colliders = [box((0, 0.2, 0), (2, 0.4, 0.1))]
    return parts, colliders, [(-1, 0, 0), (1, 0, 0)]


def pyramid(y, half, rise, **extra):
    """A pyramid roof of four dark shingle corners round the y axis, from y up rise over half."""
    parts = []
    s = half / 2
    for yaw, sx, sz in ((0, 1, 1), (90, 1, -1), (180, -1, -1), (270, -1, 1)):
        parts.append(part("darkwood_roof_ocorner", (sx * s, y, sz * s), (0, yaw, 0), (s, rise, s), **extra))
    return parts


def ridge_turret(seed):
    """A ridge turret (takrytter) as on Borgund, set on the ridge at the origin: a boarded shaft, a skirt of shingles,
    an open belfry with round arches, a second tier, a tall spire and a cross. Dragon heads look out from the corners
    of the skirt. 7.5 m high."""
    rng = random.Random(seed)
    parts = []
    w = 0.6  # half the shaft's width
    for side in range(4):
        yaw = 90 * side
        for p in boards(-w, w, 0, 1.4, rng, z=w):
            parts.append(rotate_y(p, yaw))
    parts += pyramid(1.3, 1.1, 0.7)
    # The belfry: corner posts, round arches between them, a plate on top.
    for sx in (-1, 1):
        for sz in (-1, 1):
            parts.append(plank((sx * w, 1.6, sz * w), (sx * w, 2.8, sz * w), 0.1, 0.1, tint=TIMBER_TINT))
    for side in range(4):
        yaw = 90 * side
        arch = [(-w + w * (1 - math.cos(t)), 2.35 + 0.3 * math.sin(t)) for t in (0, math.pi / 4, math.pi / 2, 3 * math.pi / 4, math.pi)]
        for (xa, ya), (xb, yb) in zip(arch, arch[1:]):
            parts.append(rotate_y(plank((xa, ya, w), (xb, yb, w), 0.05, 0.05, tint=TIMBER_TINT, detail=True), yaw))
        parts.append(rotate_y(plank((-w, 2.82, w), (w, 2.82, w), 0.12, 0.08, tint=TIMBER_TINT), yaw))
        parts.append(rotate_y(plank((-w, 1.62, w), (w, 1.62, w), 0.1, 0.06, tint=TIMBER_TINT), yaw))
    parts += pyramid(2.85, 0.9, 0.5)
    for p in boards(-0.35, 0.35, 3.1, 3.6, rng, z=0.35):
        parts += [rotate_y(p, 90 * side) for side in range(4)]
    parts += pyramid(3.55, 0.55, 3.6)
    parts.append(plank((0, 7.0, 0), (0, 7.55, 0), 0.04, 0.04, tint=IRON_TINT, texture="metalwall"))
    parts.append(plank((-0.12, 7.4, 0), (0.12, 7.4, 0), 0.03, 0.04, tint=IRON_TINT, texture="metalwall"))
    for side in range(4):
        corner = math.radians(45 + 90 * side)
        reach = 1.1 * math.sqrt(2) * 0.9
        parts.append(dragon_head((reach * math.sin(corner) * 0.75, 1.4, reach * math.cos(corner) * 0.75), 45 + 90 * side, 25, 0.6,
                                 tint=HEAD_TINT, detail=True))
    colliders = [box((0, 1.4, 0), (1.3, 2.8, 1.3)), box((0, 4.5, 0), (0.8, 3.0, 0.8))]
    return parts, colliders, [(0, 0, 0)]


def rotate_y(data, yaw):
    """Turns a part about the piece's y axis (as build_defenses.rotate)."""
    from build_defenses import rotate
    return rotate(data, yaw)


def andrew_cross(seed):
    """A St Andrew's cross between two columns, 2 m wide and 1 m high, as over Borgund's arcade: two boards crossing,
    a round sun where they meet and round lobes down both sides of the arms, between a top and a bottom rail."""
    parts = [plank((-1, 0.05, 0), (1, 0.05, 0), 0.12, 0.1, tint=TIMBER_TINT), plank((-1, 0.95, 0), (1, 0.95, 0), 0.12, 0.1, tint=TIMBER_TINT)]
    for a, b in (((-0.95, 0.1), (0.95, 0.9)), ((-0.95, 0.9), (0.95, 0.1))):
        parts.append(plank((a[0], a[1], 0), (b[0], b[1], 0), 0.07, 0.11, tint=CARVED_TINT))
        dx, dy = b[0] - a[0], b[1] - a[1]
        length = math.hypot(dx, dy)
        nx, ny = -dy / length, dx / length
        for i in range(1, 8):
            if i == 4:
                continue
            t = i / 8
            side = 1 if i % 2 else -1
            centre = (a[0] + dx * t + side * nx * 0.07, a[1] + dy * t + side * ny * 0.07, 0)
            parts.append(disc(centre, 0.055, 0.06, tint=CARVED_TINT, detail=True))
    parts.append(disc((0, 0.5, 0), 0.17, 0.1, tint=[0.78, 0.62, 0.42]))
    parts.append(disc((0, 0.5, 0), 0.11, 0.12, tint=CARVED_TINT))
    colliders = [box((0, 0.5, 0), (2, 1, 0.12))]
    return parts, colliders, [(x, y, 0) for x in (-1, 1) for y in (0, 1)]


def arcade_arch(seed):
    """A round arch 2 m wide and 1 m high between two columns, as in Borgund's arcade: a ring of short boards with
    boards filling the spandrels, under a plate."""
    parts = [plank((-1, 0.95, 0), (1, 0.95, 0), 0.14, 0.1, tint=TIMBER_TINT)]
    ring = [(-0.9 * math.cos(t), 0.05 + 0.82 * math.sin(t)) for t in [math.pi * i / 10 for i in range(11)]]
    for (xa, ya), (xb, yb) in zip(ring, ring[1:]):
        parts.append(plank((xa, ya, 0), (xb, yb, 0), 0.1, 0.12, tint=CARVED_TINT))
    for side in (-1, 1):
        for i in range(3):
            x = side * (0.95 - 0.1 * i)
            top = 0.9
            bottom = 0.05 + 0.82 * math.sin(math.acos(min(1.0, abs(x) / 0.9))) if abs(x) < 0.9 else 0.05
            parts.append(plank((x, bottom, 0), (x, top, 0), 0.05, 0.1, tint=STAVE_TINT))
    colliders = [box((x, 0.5, 0), (0.25, 1.0, 0.14)) for x in (-0.88, 0.88)] + [box((0, 0.93, 0), (2, 0.14, 0.14))]
    return parts, colliders, [(x, y, 0) for x in (-1, 1) for y in (0, 1)]


def nave_column(seed):
    """A round column 4 m high as in Borgund's nave: a square foot, a round base, the shaft, a cushion capital and a
    carved mask looking out from it on two sides."""
    rng = random.Random(seed)
    parts = [part("stonebox", (0, 0.08, 0), scale=(0.5, 0.16, 0.5), tint=[0.36, 0.3, 0.24])]
    parts.append(disc((0, 0.22, 0), 0.25, 0.12, facing="y", tint=CARVED_TINT))
    parts.append(part("wood_pole_log_4", (0, 2.0, 0), (0, rng.uniform(0, 360), 0), (0.19 / 0.26, 3.5 / 4.44, 0.19 / 0.28), tint=CARVED_TINT))
    for y, radius in ((3.62, 0.22), (3.74, 0.26), (3.86, 0.3)):
        parts.append(disc((0, y, 0), radius, 0.12, facing="y", tint=CARVED_TINT))
    parts.append(part("stonebox", (0, 3.96, 0), scale=(0.62, 0.08, 0.62), tint=[0.36, 0.3, 0.24]))
    for yaw in (0, 180):
        parts.append(rotate_y(part("trophy_skull", (0, 3.45, 0.24), (0, 0, 0), (1.3, 1.3, 1.3), tint=[0.62, 0.48, 0.34], detail=True), yaw))
    colliders = [box((0, 2.0, 0), (0.42, 4.0, 0.42))]
    return parts, colliders, [(0, 0, 0), (0, 2.0, 0), (0, 4.0, 0)]


def scissor_truss(seed):
    """A scissor truss under a 45 degree roof 4 m wide, from wall top to wall top across x: two rafters to the ridge,
    two braces crossing between them, and a tie at the foot."""
    parts = [plank((-2, 0.05, 0), (0, 2.05, 0), 0.14, 0.18, tint=TIMBER_TINT), plank((2, 0.05, 0), (0, 2.05, 0), 0.14, 0.18, tint=TIMBER_TINT)]
    parts.append(plank((-1.7, 0.25, 0), (1.0, 1.25, 0), 0.1, 0.14, tint=TIMBER_TINT))
    parts.append(plank((1.7, 0.25, 0), (-1.0, 1.25, 0), 0.1, 0.14, tint=TIMBER_TINT))
    parts.append(plank((-1.9, 0.08, 0), (1.9, 0.08, 0), 0.12, 0.14, tint=TIMBER_TINT))
    parts.append(plank((-0.55, 1.5, 0), (0.55, 1.5, 0), 0.08, 0.1, tint=TIMBER_TINT))
    colliders = [box((0, 0.08, 0), (3.9, 0.16, 0.14))]
    return parts, colliders, [(-2, 0, 0), (2, 0, 0), (0, 2, 0)]


def gallery_porch(seed):
    """A 2 m section of svalgang with an opening 1.2 m wide in its outer side and a steep little gable over it, with
    lattice in the gable and a cross on the top, as Borgund's svalgang has at its doors."""
    rng = random.Random(seed)
    z = GALLERY - 0.1
    parts = [part("wood_floor", (0, 0, 0.15 + (GALLERY - 0.15) / 2), scale=(1, 1, (GALLERY - 0.15) / 2), tint=[0.75, 0.68, 0.6])]
    for x0, x1 in ((-1, -0.6), (0.6, 1)):
        side, _ = gallery_side(x0, x1, rng)
        parts += side
    for x in (-0.6, 0.6):
        parts.append(plank((x, 0, z), (x, EAVE, z), 0.14, 0.14, tint=TIMBER_TINT))
    parts.append(plank((-0.6, 2.05, z), (0.6, 2.05, z), 0.14, 0.12, tint=TIMBER_TINT))
    # The main lean-to behind, as on the plain section.
    parts.append(part("darkwood_roof", (0, EAVE + 0.1, GALLERY - ROOF_SCALE), scale=(1, ROOF_SCALE, ROOF_SCALE)))
    # The porch gable: two steep halves of dark shingles meeting over x 0, from the eave out past the side.
    ridge = EAVE + 1.0
    for side in (-1, 1):
        parts.append(part("darkwood_roof", (side * 0.45, EAVE - 0.05, GALLERY - 0.05), (0, 90 * side, 0), (0.6, 1.05, 0.45)))
    # The gable's front: a triangle of lattice between barge boards.
    for side in (-1, 1):
        parts.append(plank((side * 0.95, EAVE - 0.05, GALLERY + 0.25), (0, ridge, GALLERY + 0.25), 0.05, 0.16, tint=CARVED_TINT))
    for i in range(-3, 4):
        x = 0.22 * i
        height = (ridge - EAVE) * (1 - abs(x) / 0.9)
        if height > 0.1:
            parts.append(plank((x - 0.12, EAVE, GALLERY + 0.22), (x + 0.12, EAVE + height * 0.9, GALLERY + 0.22), 0.03, 0.04, tint=TIMBER_TINT, detail=True))
            parts.append(plank((x + 0.12, EAVE, GALLERY + 0.22), (x - 0.12, EAVE + height * 0.9, GALLERY + 0.22), 0.03, 0.04, tint=TIMBER_TINT, detail=True))
    parts.append(plank((-0.95, EAVE, GALLERY + 0.22), (0.95, EAVE, GALLERY + 0.22), 0.06, 0.1, tint=TIMBER_TINT))
    cross, _, _ = gable_cross(seed)
    parts += [dict(p, position=r3((p["position"][0], p["position"][1] + ridge, p["position"][2] + GALLERY + 0.25))) for p in cross]
    colliders = [box((0, -0.03, 0.15 + (GALLERY - 0.15) / 2), (2.0, 0.22, GALLERY - 0.15))]
    colliders += [box((x, 0.55, z), (0.4, 1.1, 0.22)) for x in (-0.8, 0.8)]
    colliders += [box((x, EAVE / 2, z), (0.16, EAVE, 0.16)) for x in (-0.6, 0.6)]
    snaps = [(x, 0, zz) for x in (-1, 1) for zz in (0, GALLERY)]
    return parts, colliders, snaps


def bell_tower(seed):
    """A free-standing bell tower (stopul) as south of Borgund church, 3 x 3 m and 9 m high: a boarded foot, a shingled
    middle, an open belfry with the bell and a pyramid roof with a cross. The bell swings in the group "bell" and rings
    like the alarm bell."""
    rng = random.Random(seed)
    parts = []
    h = 1.5
    for side in range(4):
        yaw = 90 * side
        for p in boards(-h, h, 0, 3.0, rng, z=h):
            parts.append(rotate_y(p, yaw))
        parts.append(rotate_y(plank((-h - 0.1, 0.12, h), (h + 0.1, 0.12, h), 0.24, 0.24, tint=TIMBER_TINT), yaw))
        for x in (-1, 0, 1):
            for y in (3.0, 4.0):
                parts.append(rotate_y(shingles(x * 1.0, y, h + 0.01), yaw))
        parts.append(rotate_y(plank((-h, 5.0, h), (h, 5.0, h), 0.14, 0.12, tint=TIMBER_TINT), yaw))
        parts.append(rotate_y(plank((-h, 6.9, h), (h, 6.9, h), 0.16, 0.14, tint=TIMBER_TINT), yaw))
        for x0 in (-h, 0.0):
            arch = [(x0 + 0.75 - 0.75 * math.cos(t), 6.0 + 0.6 * math.sin(t)) for t in (0, math.pi / 4, math.pi / 2, 3 * math.pi / 4, math.pi)]
            for (xa, ya), (xb, yb) in zip(arch, arch[1:]):
                parts.append(rotate_y(plank((xa, ya, h), (xb, yb, h), 0.06, 0.07, tint=TIMBER_TINT, detail=True), yaw))
        parts.append(rotate_y(plank((0, 5.05, h), (0, 6.85, h), 0.12, 0.12, tint=TIMBER_TINT), yaw))
        parts.append(rotate_y(plank((-h, 5.4, h), (h, 5.4, h), 0.06, 0.08, tint=TIMBER_TINT), yaw))
    for sx in (-1, 1):
        for sz in (-1, 1):
            parts.append(plank((sx * h, 0, sz * h), (sx * h, 6.95, sz * h), 0.18, 0.18, tint=TIMBER_TINT))
    parts += pyramid(6.95, 2.0, 2.6)
    cross, _, _ = gable_cross(seed)
    parts += [dict(p, position=r3((p["position"][0], p["position"][1] + 9.4, p["position"][2]))) for p in cross]
    # The bell under a beam across the belfry.
    beam_y = 6.7
    parts.append(plank((-h, beam_y, 0), (h, beam_y, 0), 0.16, 0.16, tint=TIMBER_TINT))
    bell_scale = 0.75
    crown = beam_y - 0.14
    bell = [part("Bell", (0, crown - 1.19 * bell_scale, 0), scale=(bell_scale,) * 3)]
    bell.append(plank((0, beam_y - 0.08, 0), (0, crown, 0), 0.06, 0.06, tint=IRON_TINT, texture="metalwall"))
    parts += [dict(p, group="bell") for p in bell]
    colliders = [box((0, 2.5, 0), (3.1, 5.0, 3.1)), box((0, 6.95, 0), (3.1, 0.2, 3.1))]
    colliders += [box((sx * h, 6.0, sz * h), (0.2, 2.0, 0.2)) for sx in (-1, 1) for sz in (-1, 1)]
    groups = [{"name": "bell", "pivot": [0, beam_y, 0]}]
    return parts, colliders, [(0, 0, 0)], groups


def consecration_cross(seed):
    """A consecration cross painted on the wall, as on Borgund's south wall: a red cross in an ochre ring, 0.5 m
    across, on a thin board that hangs on a wall facing +z."""
    parts = [disc((0, 0, 0.02), 0.26, 0.03, tint=[0.55, 0.45, 0.34])]
    for i in range(12):
        a0, a1 = 2 * math.pi * i / 12, 2 * math.pi * (i + 1) / 12
        parts.append(plank((0.21 * math.cos(a0), 0.21 * math.sin(a0), 0.04), (0.21 * math.cos(a1), 0.21 * math.sin(a1), 0.04), 0.01, 0.04, tint=PAINT_OCHRE))
    parts.append(plank((0, -0.17, 0.045), (0, 0.17, 0.045), 0.01, 0.06, tint=PAINT_RED))
    parts.append(plank((-0.17, 0, 0.045), (0.17, 0, 0.045), 0.01, 0.06, tint=PAINT_RED))
    colliders = [box((0, 0, 0.02), (0.52, 0.52, 0.05))]
    return parts, colliders, [(0, 0, 0)]


def borgund_pieces():
    near = views(("front", 160, 15), ("side", 90, 10), ("angle", -130, 30))
    pieces = [
        piece_entry("sponvegg", "wood_wall_roof", *shingled_wall(2.0, "sponvegg"), views=WALL_VIEWS),
        piece_entry("sponvegg_hoy", "wood_wall_roof", *shingled_wall(4.0, "sponvegg_hoy"), views=WALL_VIEWS),
        piece_entry("monedrage", "wood_pole2", *ridge_dragon("monedrage"), views=views(("side", 90, 10), ("front", 180, 15), ("angle", 130, 25))),
        piece_entry("gavlkors", "wood_pole2", *gable_cross("gavlkors"), views=near),
        piece_entry("monekam", "wood_pole2", *ridge_crest("monekam"), views=near),
        piece_entry("takrytter", "wood_pole2", *ridge_turret("takrytter"), views=views(("front", 160, 10), ("angle", -130, 20))),
        piece_entry("andreaskors", "wood_pole2", *andrew_cross("andreaskors"), views=near),
        piece_entry("arkadebue", "wood_pole2", *arcade_arch("arkadebue"), views=near),
        piece_entry("skipsstav", "wood_pole2", *nave_column("skipsstav"), views=views(("front", 160, 10), ("capital", 160, 10))),
        piece_entry("saksesperre", "wood_pole2", *scissor_truss("saksesperre"), views=near),
        piece_entry("svalgang_inngang", "wood_floor", *gallery_porch("svalgang_inngang"), views=views(("outside", 160, 15), ("end", 90, 10), ("angle", 130, 30))),
        piece_entry("vigselskors", "wood_pole2", *consecration_cross("vigselskors"), views=near),
    ]
    parts, colliders, snaps, groups = bell_tower("stopul")
    pieces.append(piece_entry("stopul", "wood_pole2", parts, colliders, snaps, groups=groups, views=views(("front", 160, 10), ("angle", -130, 20))))
    pieces.append(overview())
    return pieces


def overview():
    """The stave hall of build_stave.py dressed after Borgund: shingled gable walls, a ridge turret, ridge dragons and
    crests, gable crosses, a svalgang porch at the door, and the bell tower beside it."""
    def at(name, x, z, yaw=0, y=0.0):
        data = {"piece": name, "position": r3((x, y, z))}
        if yaw:
            data["rotation"] = [0, yaw, 0]
        return data

    parts = [at("stavhall", 0, 0)]
    parts += [at("takrytter", 0, 0, 0, 5.0)]
    parts += [at("monedrage", -3.6, 0, -90, 5.0), at("monedrage", 3.6, 0, 90, 5.0)]
    parts += [at("monekam", x, 0, 0, 5.0) for x in (-2, 2)]
    parts += [at("stopul", 8, 4, 0)]
    return {"name": "borgund", "parts": parts, "views": views(("front", 150, 12), ("corner", -140, 20), ("top", 150, 55))}
