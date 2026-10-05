"""The stone defences: the stone counterparts of the palisade modules, laid out for defenses.json.

build_defenses.py adds stone_pieces() to the layout it writes; run that script, not this one. Pieces/Defenses/
StoneDefensePieces.cs builds them in the game.

Every module keeps the footprint and snap points of its wooden counterpart: the outside faces +z and straight walls
are 4 m long. Stone stands taller than a palisade, so the wall walk is at WALK, 3 m, a metre above the palisade's.

Weathering keeps them from looking new: the lowest courses are damp and darker, moss grows on every ledge (plinth,
string course, coping, merlon tops) and in patches low on the faces.

What keeps the fronts from being flat: a stepped plinth with rubble at the foot, a string course, a parapet that
overhangs on corbels, merlons with arrow slits, buttresses, corner quoins that stand proud of the face, round
corner turrets and D-shaped gate towers.

Vanilla mesh facts used below (from export_vanilla.py):
  stone_wall_1x1  1 x 1 x 1 m block centred on its origin (with 5 cm lips)
  stone_wall_2x1  2 x 1 x 1 m, stone_wall_4x2  4 x 2 x 1.3 m, both centred
  stone_arch      2 x 1 x 1.1 m block with an arch cut out underneath, centred
  iron_grate      2 m wide, 3 m tall (y -1..2), 12 cm thick
"""

import contextlib
import json
import math
import pathlib
import random

from build_defenses import (box, drawbridge, floor, floor_box, gate_leaf, ladder_parts, part, piece, post, r3, ramp, roof,
                            rotate, torch, views)

WALK = 3.0  # height of every stone wall walk, so they all join
SLATE = "roof_slate_albedo"  # the White Hilt slate roof texture

# Nominal size of each block mesh, which the scale is worked out from.
BLOCKS = {"stone_wall_1x1": (1.0, 1.0, 1.0), "stone_wall_2x1": (2.0, 1.0, 1.0), "stone_wall_4x2": (4.0, 2.0, 1.3),
          "stonebox": (1.0, 1.0, 1.0)}
# A block thinner than this (string courses, coping, corbels, roof edges) is drawn as STONEBOX: a plain 12-triangle box
# in the stone's texture, where the vanilla block has some 430 triangles of bevels nobody sees on a 10 cm ledge. The mod
# builds it in VanillaMeshLibrary; write_stonebox() writes the same box for the preview.
THIN = 0.3
STONEBOX = "stonebox"
# The part of the vanilla stone texture the block's flat front shows, which every face of the box shows too.
STONEBOX_UV = (0.21, 0.03, 0.345, 0.17)
SLIT_TINT = [0.06, 0.06, 0.07]
QUOIN_TINT = [1.08, 1.04, 0.98]
COURSE_TINT = [1.05, 1.02, 0.96]
DAMP_TINT = [0.72, 0.75, 0.68]  # the lowest courses, darkened by the wet ground
MOSS_TINTS = [[0.36, 0.5, 0.22], [0.45, 0.56, 0.27]]

# The stones the defences can be built of. Each changes the blocks' texture and the weathering: what darkens the
# lowest courses, what grows on the ledges, the rubble at the foot, and for grausten iron spikes on the merlons.
MATERIALS = {
    "stein": {"texture": None, "damp": DAMP_TINT, "moss": MOSS_TINTS, "rubble": [0.7, 0.7, 0.68], "spikes": False, "shade": 1.0},
    "marmor": {"texture": "marble_d", "damp": [0.78, 0.84, 0.8], "moss": [[0.3, 0.46, 0.32], [0.4, 0.54, 0.38]],
               "rubble": [0.48, 0.5, 0.5], "spikes": False, "shade": 1.0},
    "grausten": {"texture": "Grausten_d", "damp": [0.62, 0.56, 0.52], "moss": [[0.16, 0.15, 0.15], [0.28, 0.25, 0.23]],
                 "rubble": [0.32, 0.3, 0.29], "spikes": True, "shade": 0.62},
}
STONE = dict(MATERIALS["stein"])


@contextlib.contextmanager
def material(name):
    """Builds what is laid out inside the block of the given stone."""
    saved = dict(STONE)
    STONE.clear()
    STONE.update(MATERIALS[name])
    try:
        yield
    finally:
        STONE.clear()
        STONE.update(saved)
PARAPET = 1.0  # breast height of the parapet above a walk
MERLON = 0.7  # merlon height above the parapet


SHADES = [0.8, 0.87, 0.93, 1.0]


def shade(rng, low=0.82, high=1.0):
    """One of a few stone shades within the range: every distinct tint is a material of its own in the game."""
    choices = [s for s in SHADES if low - 0.03 <= s <= high + 0.03] or [min(SHADES, key=lambda s: abs(s - (low + high) / 2))]
    s = rng.choice(choices)
    if STONE["shade"] == 1.0:
        return [s, s, round(s * 0.98, 3)]
    # A darker stone, a little warm, as grausten is.
    s *= STONE["shade"]
    return [round(s, 3), round(s * 0.97, 3), round(s * 0.93, 3)]


def block(x0, x1, y0, y1, z0, z1, mesh="stone_wall_1x1", tint=None, **extra):
    """One block mesh stretched over a box, in the stone being built of."""
    if min(x1 - x0, y1 - y0, z1 - z0) < THIN:
        mesh = STONEBOX
    nx, ny, nz = BLOCKS[mesh]
    if STONE["texture"] and tint != SLIT_TINT:
        extra.setdefault("texture", STONE["texture"])
    centre = ((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2)
    return part(mesh, centre, scale=((x1 - x0) / nx, (y1 - y0) / ny, (z1 - z0) / nz), tint=tint, **extra)


def write_stonebox(folder):
    """Writes STONEBOX as a preview mesh: a unit box centred on its origin, every face showing STONEBOX_UV."""
    faces = [  # normal, two in-face axes
        ((0, 1, 0), (1, 0, 0), (0, 0, 1)), ((0, -1, 0), (1, 0, 0), (0, 0, -1)),
        ((1, 0, 0), (0, 0, -1), (0, 1, 0)), ((-1, 0, 0), (0, 0, 1), (0, 1, 0)),
        ((0, 0, 1), (1, 0, 0), (0, 1, 0)), ((0, 0, -1), (-1, 0, 0), (0, 1, 0)),
    ]
    u0, v0, u1, v1 = STONEBOX_UV
    vertices, normals, uvs, triangles = [], [], [], []
    for normal, u, v in faces:
        start = len(vertices) // 3
        for su, sv in ((-1, -1), (1, -1), (1, 1), (-1, 1)):
            vertices += [0.5 * (normal[k] + su * u[k] + sv * v[k]) for k in range(3)]
            normals += normal
            uvs += [u0 if su < 0 else u1, v0 if sv < 0 else v1]
        # Unity winds front faces clockwise seen from outside: the corners already run so when u x v is the normal.
        cross = (u[1] * v[2] - u[2] * v[1], u[2] * v[0] - u[0] * v[2], u[0] * v[1] - u[1] * v[0])
        if cross == normal:
            triangles += [start, start + 1, start + 2, start, start + 2, start + 3]
        else:
            triangles += [start, start + 2, start + 1, start, start + 3, start + 2]
    folder = pathlib.Path(folder)
    folder.mkdir(parents=True, exist_ok=True)
    mesh = {"name": STONEBOX, "parts": [{"texture": "stone", "vertices": vertices, "normals": normals, "uvs": uvs, "triangles": triangles}]}
    (folder / f"{STONEBOX}.json").write_text(json.dumps(mesh), encoding="utf-8")


def masonry(x0, x1, y0, y1, z0, z1, rng, damp=None):
    """A box of coursed masonry: tiled with the block mesh that fits, so the stones keep their own size. Courses below
    the damp line (by default 0.7 m) take the damp tint now and then."""
    w, h = x1 - x0, y1 - y0
    if w <= 0.01 or h <= 0.01:
        return []
    if w >= 1.5:
        mesh, (nx, ny, _) = "stone_wall_2x1", BLOCKS["stone_wall_2x1"]
    else:
        mesh, (nx, ny, _) = "stone_wall_1x1", BLOCKS["stone_wall_1x1"]
    cols, rows = max(1, round(w / nx)), max(1, round(h / ny))
    parts = []
    for i in range(cols):
        for j in range(rows):
            bottom = y0 + h * j / rows
            wet = bottom < (0.7 if damp is None else damp) and rng.random() < 0.5
            parts.append(block(x0 + w * i / cols, x0 + w * (i + 1) / cols, bottom, y0 + h * (j + 1) / rows, z0, z1, mesh,
                               tint=STONE["damp"] if wet else shade(rng)))
    return parts


def masonry_with_hole(x0, x1, y0, y1, z0, z1, hole, rng):
    """Masonry with a rectangular opening (hx0, hx1, hy0, hy1) through it."""
    hx0, hx1, hy0, hy1 = hole
    return (masonry(x0, hx0, y0, y1, z0, z1, rng) + masonry(hx1, x1, y0, y1, z0, z1, rng)
            + masonry(hx0, hx1, y0, hy0, z0, z1, rng) + masonry(hx0, hx1, hy1, y1, z0, z1, rng))


def span(x0, x1, y0, y1, z0, z1):
    """A box collider from its bounds."""
    return box(((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2), (x1 - x0, y1 - y0, z1 - z0))


def spans_with_hole(x0, x1, y0, y1, z0, z1, hole):
    """Box colliders of a wall with a rectangular opening (hx0, hx1, hy0, hy1) through it, so it can be walked through."""
    hx0, hx1, hy0, hy1 = hole
    boxes = [(x0, hx0, y0, y1), (hx1, x1, y0, y1), (hx0, hx1, y0, hy0), (hx0, hx1, hy1, y1)]
    return [span(a, b, c, d, z0, z1) for a, b, c, d in boxes if b - a > 0.01 and d - c > 0.01]


def moved(items, yaw, dx=0.0, dz=0.0):
    """Turns parts or boxes about the origin and moves them."""
    result = []
    for item in items:
        turned = rotate(item, yaw)
        key = "position" if "position" in turned else "center"
        turned[key] = r3((turned[key][0] + dx, turned[key][1], turned[key][2] + dz))
        result.append(turned)
    return result


def floors_with_hatch(x0, x1, z0, z1, hatch, level, mesh="wood_floor"):
    """Floor planks and colliders over a rectangle, leaving a hatch (hx0, hx1, hz0, hz1) open in one corner."""
    hx0, hx1, hz0, hz1 = hatch
    rects = [(x0, x1, z0, hz0), (x0, x1, hz1, z1), (x0, hx0, hz0, hz1), (hx1, x1, hz0, hz1)]
    rects = [r for r in rects if r[1] - r[0] > 0.01 and r[3] - r[2] > 0.01]
    return [floor(a, b, c, d, level, mesh) for a, b, c, d in rects], [floor_box(a, b, c, d, level) for a, b, c, d in rects]


def ring_colliders(cx, cz, radius, y0, y1, thickness, sides=8, skip=()):
    """Box colliders following a polygonal ring."""
    edge = 2 * radius * math.tan(math.pi / sides)
    boxes = []
    for i in range(sides):
        if i not in skip:
            boxes += moved([box((0, (y0 + y1) / 2, radius - thickness / 2), (edge + 0.05, y1 - y0, thickness))],
                           math.degrees(2 * math.pi * i / sides), cx, cz)
    return boxes


def masonry_along_z(x0, x1, y0, y1, z0, z1, rng, hole=None):
    """Masonry for a wall running along z (seen from the side), with its stones laid along it, and an optional opening
    (hz0, hz1, hy0, hy1)."""
    if hole is None:
        local = masonry(-z1, -z0, y0, y1, x0, x1, rng)
    else:
        hz0, hz1, hy0, hy1 = hole
        local = masonry_with_hole(-z1, -z0, y0, y1, x0, x1, (-hz1, -hz0, hy0, hy1), rng)
    return moved(local, 90)


def moss(x0, x1, y, z0, z1, rng, count):
    """Moss tufts lying on a ledge at height y between x0..x1 and z0..z1."""
    parts = []
    for _ in range(count):
        width = rng.uniform(0.3, 0.7)
        parts.append(part("rock", (rng.uniform(x0 + width / 2, max(x0 + width / 2, x1 - width / 2)), y - 0.01,
                                   rng.uniform(z0, z1)), (0, rng.uniform(0, 360), 0),
                          (width, rng.uniform(0.12, 0.22), min(z1 - z0, width) * 0.9 + 0.08), tint=rng.choice(STONE["moss"]), detail=True))
    return parts


def face_moss(x0, x1, y0, y1, z, rng, count):
    """Thin patches of moss on a face looking towards +z, between y0 and y1."""
    parts = []
    for _ in range(count):
        width, height = rng.uniform(0.3, 0.8), rng.uniform(0.2, 0.5)
        parts.append(part("rock", (rng.uniform(x0 + width / 2, max(x0 + width / 2, x1 - width / 2)), rng.uniform(y0, y1 - height), z + 0.02),
                          (0, rng.uniform(-10, 10), rng.uniform(-20, 20)), (width, height / 0.7, 0.05), tint=rng.choice(STONE["moss"]), detail=True))
    return parts


def slit(x, y, z, height=0.55):
    """An arrow slit: a dark sliver just proud of a face that looks towards +z. Being nearly black, it is a plain beam,
    a tenth of the triangles of a stone block."""
    return part("wood_beam", (x, y, z + 0.01), scale=(0.05, height / 0.4, 0.05), tint=SLIT_TINT, detail=True)


def rubble(x0, x1, z, rng, count):
    """Field stones heaped against the foot of a wall."""
    parts = []
    for _ in range(count):
        size = rng.uniform(0.45, 0.8)
        parts.append(part("rock", (rng.uniform(x0, x1), -0.1, z + rng.uniform(0.05, 0.35)), (0, rng.uniform(0, 360), 0),
                          (size, size * 0.7, size), tint=STONE["rubble"], detail=True))
    return parts


def crenels(x0, x1, y, z0, z1, rng, merlons):
    """Merlons on a parapet top at y, centred on the given x positions, each with an arrow slit."""
    parts = []
    for x in merlons:
        a, b = max(x0, x - 0.38), min(x1, x + 0.38)
        parts.append(block(a, b, y, y + MERLON, z0, z1, tint=shade(rng, 0.9, 1.05)))
        if b - a > 0.6:
            parts.append(slit(x, y + 0.3, z1, 0.4))
        if STONE["spikes"]:
            # Their own random tilt, so the rest of the wall comes out as in plain stone.
            tilt = random.Random(f"spike{x:.2f}{y:.2f}{z0:.2f}")
            for dx in (-0.2, 0.2):
                parts.append(part("stake", (x + dx, y + MERLON - 0.05, (z0 + z1) / 2), (tilt.uniform(-6, 6), 0, tilt.uniform(-6, 6)),
                                  (0.25, 0.14, 0.25), tint=[0.22, 0.2, 0.2], detail=True))
    # A coping stone along the parapet top, with moss in the embrasures and on some merlons.
    parts.append(block(x0, x1, y - 0.06, y + 0.04, z0 - 0.03, z1 + 0.03, tint=COURSE_TINT, detail=True))
    parts += moss(x0, x1, y + 0.04, z0, z1, rng, max(1, round((x1 - x0) / 1.5)))
    for x in merlons:
        if rng.random() < 0.4:
            parts += moss(x - 0.3, x + 0.3, y + MERLON, z0, z1, rng, 1)
    return parts


def curtain(x0, x1, rng, buttress=True, merlons=None):
    """A stretch of curtain wall along x, facing +z: a thick wall with the walk on top at WALK, a parapet overhanging
    on corbels, merlons, a string course, a stepped plinth and rubble at the foot."""
    front, back = 0.35, -1.8
    parts = masonry(x0, x1, -0.3, 0.5, back - 0.1, front + 0.25, rng)  # plinth step
    parts += masonry(x0, x1, 0.5, WALK, back, front, rng)
    parts.append(block(x0, x1, WALK - 0.45, WALK - 0.3, front - 0.05, front + 0.08, "stone_wall_2x1", tint=COURSE_TINT, detail=True))
    # The parapet stands 15 cm proud of the face, carried by a row of corbels.
    p0, p1 = -0.1, front + 0.15
    parts += masonry(x0, x1, WALK, WALK + PARAPET, p0, p1, rng)
    x = x0 + 0.25
    while x < x1 - 0.1:
        parts.append(block(x - 0.15, x + 0.15, WALK - 0.28, WALK, front - 0.1, p1, tint=shade(rng, 0.85, 0.95), detail=True))
        x += 0.5
    if merlons is None:
        count = max(1, round((x1 - x0) * 3 / 4))
        merlons = [x0 + (x1 - x0) * (k + 0.5) / count for k in range(count)]
    parts += crenels(x0, x1, WALK + PARAPET, p0, p1, rng, merlons)
    if buttress:
        mid = (x0 + x1) / 2
        for y0, y1, depth, half in ((-0.3, 0.9, 0.75, 0.5), (0.9, 1.7, 0.5, 0.45), (1.7, 2.3, 0.3, 0.4)):
            parts += masonry(mid - half, mid + half, y0, y1, front, front + depth, rng)
            parts.append(block(mid - half - 0.02, mid + half + 0.02, y1 - 0.04, y1 + 0.05, front, front + depth + 0.04,
                               tint=COURSE_TINT, detail=True))
    parts += moss(x0, x1, 0.5, front, front + 0.25, rng, max(2, round((x1 - x0) * 0.8)))
    parts += moss(x0, x1, WALK - 0.3, front, front + 0.08, rng, max(1, round((x1 - x0) / 2)))
    parts += face_moss(x0, x1, 0.5, 1.6, front, rng, max(1, round((x1 - x0) / 2)))
    parts += rubble(x0, x1, front + 0.25, rng, max(2, round(x1 - x0)))
    return parts


def curtain_colliders(x0, x1):
    return [box(((x0 + x1) / 2, (WALK - 0.3) / 2, -0.725), (x1 - x0, WALK + 0.3, 2.3)),
            box(((x0 + x1) / 2, WALK + (PARAPET + MERLON) / 2, 0.2), (x1 - x0, PARAPET + MERLON, 0.6))]


def stone_wall(buttress=True):
    rng = random.Random("steinmur" if buttress else "steinmur_slett")
    parts = curtain(-2.0, 2.0, rng, buttress=buttress)
    colliders = curtain_colliders(-2.0, 2.0) + ([box((0, 1.0, 0.7), (1.0, 2.6, 0.75))] if buttress else [])
    snaps = [(-2, 0, 0), (2, 0, 0), (-2, WALK, 0), (2, WALK, 0), (-1, WALK, -1.8), (0, WALK, -1.8), (1, WALK, -1.8)]
    return parts, colliders, snaps


def ring(cx, cz, radius, y0, y1, thickness, rng, sides=8, start=0.0, skip=()):
    """A polygonal ring of masonry, for round towers and turrets."""
    parts = []
    edge = 2 * radius * math.tan(math.pi / sides)
    for i in range(sides):
        if i in skip:
            continue
        a = start + 2 * math.pi * i / sides
        local = masonry(-edge / 2 - 0.02, edge / 2 + 0.02, y0, y1, radius - thickness, radius, rng)
        yaw = math.degrees(a)
        for p in local:
            moved = rotate(p, yaw)
            moved["position"] = r3((moved["position"][0] + cx, moved["position"][1], moved["position"][2] + cz))
            parts.append(moved)
    return parts


def turret_top(cx, cz, radius, y, rng, sides=8, start=0.0, skip=()):
    """Corbelled parapet and merlons on top of a round tower: every other face carries a merlon. Faces in skip are
    left open. Face i looks towards (sin a, cos a) for a = 2 pi i / sides: 0 is +z, 2 is +x, 4 is -z, 6 is -x."""
    parts = []
    edge = 2 * (radius + 0.2) * math.tan(math.pi / sides)
    for i in range(sides):
        if i in skip:
            continue
        a = start + 2 * math.pi * i / sides
        local = [block(-edge / 2 - 0.02, edge / 2 + 0.02, y - 0.35, y, radius - 0.2, radius + 0.15, tint=shade(rng, 0.85, 0.95))]
        local += masonry(-edge / 2 - 0.02, edge / 2 + 0.02, y, y + PARAPET, radius - 0.2, radius + 0.2, rng)
        if i % 2 == 0:
            local.append(block(-edge / 2 + 0.05, edge / 2 - 0.05, y + PARAPET, y + PARAPET + MERLON, radius - 0.2, radius + 0.2,
                               tint=shade(rng, 0.9, 1.05)))
            local.append(slit(0, y + PARAPET + 0.3, radius + 0.2, 0.4))
        local.append(block(-edge / 2, edge / 2, y + PARAPET - 0.06, y + PARAPET + 0.04, radius - 0.23, radius + 0.23,
                           tint=COURSE_TINT, detail=True))
        for p in local:
            moved = rotate(p, math.degrees(a))
            moved["position"] = r3((moved["position"][0] + cx, moved["position"][1], moved["position"][2] + cz))
            parts.append(moved)
    return parts


def corner90():
    """Two walls meeting at a right angle, with a round bastion standing out over the outside corner. Its top is level
    with the wall walk, behind a parapet of its own."""
    rng = random.Random("steinhjorne")
    parts = curtain(0.0, 2.0, rng, buttress=False)
    parts += [rotate(p, -90) for p in curtain(-2.0, 0.0, rng, buttress=False)]
    radius = 1.25
    parts += ring(0, 0, radius + 0.25, -0.3, 0.5, radius + 0.25, rng)
    parts += ring(0, 0, radius, 0.5, WALK, radius, rng)
    parts += turret_top(0, 0, radius, WALK, rng, skip=(2, 3, 4))
    parts.append(slit(-0.62, 1.4, 1.1))
    parts += rubble(-1.5, 0.5, 1.2, rng, 3)
    colliders = curtain_colliders(0.0, 2.0) + [rotate(c, -90) for c in curtain_colliders(-2.0, 0.0)]
    colliders += [box((0, (WALK - 0.3) / 2, 0), (2.4, WALK + 0.3, 2.4)), rotate(box((0, (WALK - 0.3) / 2, 0), (2.4, WALK + 0.3, 2.4)), 45)]
    colliders += ring_colliders(0, 0, radius + 0.2, WALK, WALK + PARAPET + MERLON, 0.4, skip=(2, 3, 4))
    snaps = [(0, 0, 0), (2, 0, 0), (0, 0, -2), (2, WALK, 0), (0, WALK, -2)]
    return parts, colliders, snaps


def corner45():
    """Two walls meeting at 45 degrees, with an angle buttress over the joint."""
    rng = random.Random("steinhjorne45")
    parts = curtain(0.0, 2.0, rng, buttress=False)
    parts += [rotate(p, -45) for p in curtain(-2.0, 0.0, rng, buttress=False)]
    buttress = masonry(-0.55, 0.55, -0.3, 1.2, -0.2, 0.9, rng) + masonry(-0.45, 0.45, 1.2, 2.3, -0.2, 0.7, rng)
    buttress.append(block(-0.48, 0.48, 2.28, 2.4, -0.2, 0.75, tint=COURSE_TINT, detail=True))
    parts += [rotate(p, -22.5) for p in buttress]
    colliders = curtain_colliders(0.0, 2.0) + [rotate(c, -45) for c in curtain_colliders(-2.0, 0.0)]
    colliders.append(rotate(box((0, 1.0, 0.35), (1.1, 2.6, 1.1)), -22.5))
    snaps = [(0, 0, 0), (2, 0, 0), (-1.414, 0, -1.414), (2, WALK, 0), (-1.414, WALK, -1.414)]
    return parts, colliders, snaps


def stairs():
    """A flight of solid stone steps up the back of the wall, 2 m wide, with a cheek wall and coping on one side."""
    rng = random.Random("steintrapp")
    parts = []
    steps = 12
    length = 0.5 * steps
    for i in range(steps):
        z1 = -length + 0.5 * (i + 1)
        top = WALK * (i + 1) / steps
        parts += masonry(-1.0, 1.0, 0.0, top, z1 - 0.5, z1, rng)
    # The cheek wall follows the slope as a stepped parapet.
    for i in range(steps):
        z1 = -length + 0.5 * (i + 1)
        top = WALK * (i + 1) / steps
        parts.append(block(-1.25, -1.0, top, top + 0.7, z1 - 0.5, z1, tint=shade(rng)))
        parts.append(block(-1.28, -0.97, top + 0.7, top + 0.78, z1 - 0.52, z1 + 0.02, tint=COURSE_TINT, detail=True))
    for i in rng.sample(range(steps), 4):
        z1 = -length + 0.5 * (i + 1)
        parts += moss(0.4, 1.0, WALK * (i + 1) / steps + 0.01, z1 - 0.45, z1 - 0.05, rng, 1)
    colliders = [ramp((0, 0, -length), (0, WALK, 0), 2.0), box((-1.125, WALK / 2 + 0.35, -length / 2), (0.25, WALK + 0.7, length))]
    snaps = [(0, WALK, 0), (-1, 0, -length), (1, 0, -length)]
    return parts, colliders, snaps


def quoins(h, y0, y1, rng):
    """Dressed corner stones at the four corners, standing a little proud of both faces, long and short in turn. They
    are a metre high, so a tall tower does not need many, and are left out at a distance."""
    parts = []
    course = 1.0
    y = y0
    i = 0
    while y < y1 - 0.05:
        top = min(y1, y + course)
        for sx in (-1, 1):
            for sz in (-1, 1):
                long_x = (i + (sx * sz > 0)) % 2 == 0
                lx, lz = (0.62, 0.42) if long_x else (0.42, 0.62)
                x0, x1 = sorted((sx * (h + 0.04), sx * (h + 0.04 - lx)))
                z0, z1 = sorted((sz * (h + 0.04), sz * (h + 0.04 - lz)))
                parts.append(block(x0, x1, y, top, z0, z1, tint=QUOIN_TINT, detail=True))
        y = top
        i += 1
    return parts


def stone_tower(n, levels, seed, roofed):
    """A square stone tower n metres across with floors at the given heights: a plinth, quoins, arrow slits, a door
    at the foot of the back, doorways to the wall walks on both sides near the back, a ladder up the front through a
    hatch in each floor, and a corbelled parapet on top. Returns parts, colliders, ladders and snap points."""
    rng = random.Random(seed)
    h = n / 2
    t = 0.3 if n <= 2 else 0.4
    inner = h - t
    top = levels[-1]
    hatch = 0.6 if n <= 2 else 0.75
    door = 0.7 if n <= 2 else 1.0
    parts = masonry(-h - 0.3, h + 0.3, -0.3, 0.4, -h - 0.3, h + 0.3, rng)
    parts.append(block(-h - 0.32, h + 0.32, 0.38, 0.46, -h - 0.32, h + 0.32, tint=COURSE_TINT, detail=True))
    colliders = [span(-h - 0.3, h + 0.3, -0.3, 0.4, -h - 0.3, h + 0.3)]
    # Side doorways sit near the back, clear of the hatch at the front; in each face's own frame (x along the face).
    side_door = {90: (inner - 0.02 - door, inner - 0.02), 270: (-inner + 0.02, -inner + 0.02 + door)}
    for yaw in (0, 90, 180, 270):
        if yaw == 180:
            hole = (-door / 2, door / 2, 0.4, 2.3)
        elif yaw in side_door:
            hole = (side_door[yaw][0], side_door[yaw][1], WALK, WALK + 2.1)
        else:
            hole = None
        if hole:
            face = masonry_with_hole(-h, h, 0.4, top, h - t, h, hole, rng)
            face.append(block(hole[0] - 0.05, hole[1] + 0.05, hole[3], hole[3] + 0.2, h - t - 0.02, h + 0.03, tint=COURSE_TINT))
            face_colliders = spans_with_hole(-h, h, 0.4, top, h - t, h, hole)
        else:
            face = masonry(-h, h, 0.4, top, h - t, h, rng)
            face_colliders = [span(-h, h, 0.4, top, h - t, h)]
        for level in levels:
            face.append(slit(0 if yaw in (0, 180) else (h - 0.6 if n > 2 else 0), level + 1.1, h))
        face.append(slit(0 if yaw != 180 else h * 0.55, 1.2, h))
        parts += moved(face, yaw)
        colliders += moved(face_colliders, yaw)
    parts += quoins(h, 0.4, top, rng)
    # Corbels, the overhanging parapet and its merlons.
    for yaw in (0, 90, 180, 270):
        side = []
        x = -h + 0.25
        while x < h - 0.1:
            side.append(block(x - 0.14, x + 0.14, top - 0.4, top, h - 0.05, h + 0.25, tint=shade(rng, 0.85, 0.95), detail=True))
            x += 0.7
        side += masonry(-h - 0.25, h + 0.25, top, top + PARAPET, h - 0.15, h + 0.25, rng)
        count = {2: 2, 3: 2, 4: 3}[n]
        spots = [-h - 0.25 + (2 * h + 0.5) * (k + 0.5) / count for k in range(count)]
        side += crenels(-h - 0.25, h + 0.25, top + PARAPET, h - 0.15, h + 0.25, rng, spots)
        parts += moved(side, yaw)
        colliders += moved([span(-h - 0.25, h + 0.25, top, top + PARAPET + MERLON, h - 0.15, h + 0.25)], yaw)
    # Floors with the hatch in the front right corner, and the ladder against the front wall.
    hole = (inner - hatch, inner, inner - hatch, inner)
    for level in levels:
        floor_parts, floor_colliders = floors_with_hatch(-inner, inner, -inner, inner, hole, level,
                                                         "wood_floor_1x1" if n <= 2 else "wood_floor")
        parts += floor_parts
        colliders += floor_colliders
    ladder_x, ladder_z = inner - hatch / 2, inner - 0.12
    parts += ladder_parts(ladder_x, ladder_z, 0.4, top + 0.05)
    stops = [r3((ladder_x, level + 0.05, inner - hatch - 0.35)) for level in [0.4] + levels]
    ladders = [{"center": r3((ladder_x, (top + 0.45) / 2, ladder_z)), "size": r3((0.7, top - 0.35, 0.15)), "stops": sum(stops, [])}]
    if roofed:
        corner = h - 0.2
        roof_base = top + 2.4
        for sx in (-1, 1):
            for sz in (-1, 1):
                parts.append(post(sx * corner, sz * corner, top, roof_base, 0.16))
        parts += [dict(p, texture=SLATE, tint=[0.62, 0.66, 0.74]) if p.get("mesh") == "wood_roof_ocorner_45" else p
                  for p in roof(0, 0, roof_base, h + 0.6, rng)]
        # A thin lid under the roof shelters the top floor from rain.
        colliders.append(box((0, roof_base + 0.05, 0), (2 * (h + 0.9), 0.1, 2 * (h + 0.9))))
    for yaw in (0, 90, 180, 270):
        ledge = moss(-h - 0.3, h + 0.3, 0.46, h, h + 0.3, rng, n) + face_moss(-h + 0.5, h - 0.5, 0.5, 1.8, h, rng, 1)
        parts += moved(ledge, yaw)
    parts += rubble(-h, h, h + 0.3, rng, n)
    parts.append(torch((h - 0.7, WALK + 1.0, h + 0.05)))
    snaps = []
    for side in (-1, 1):
        for q in (-h + 0.2, 0, h - 0.2):
            snaps += [r3((side * h, 0, q)), r3((q, 0, side * h))]
    return parts, colliders, ladders, snaps


GATE_WALK = 4.4  # the walk over the gateway
PORTCULLIS_DROP = 2.75  # how far the portcullis comes down from raised to the ground (Defenses/GateControl uses the same)
GATE_TOWER = 6.6  # the top floor of the gate towers


def gate_tower(side, rng):
    """One of the gatehouse towers, x 2.3..4.3 on the given side, D-shaped: square at the back, round at the front.
    Solid up to the wall walk; above it hollow, entered from the wall walk through a doorway in its outer side, with a
    ladder up to the walk over the gate (through a doorway in its inner side) and on to the top."""
    s = side
    cx = s * 3.3
    outer, inner = cx + s * 1.0, cx - s * 1.0
    x0, x1 = sorted((outer, inner))
    t = 0.4
    door = (-1.0, 0.0)  # z range of both doorways
    parts = masonry(x0, x1, -0.3, WALK, -1.8, 0.6, rng)
    parts += ring(cx, 0.6, 1.3, -0.3, 0.5, 1.3, rng, skip=(3, 4, 5))
    parts += ring(cx, 0.6, 1.0, 0.5, WALK, 1.0, rng, skip=(3, 4, 5))
    colliders = [span(x0, x1, -0.3, WALK, -1.8, 0.6), span(cx - 0.9, cx + 0.9, -0.3, WALK, 0.6, 1.6)]
    # The hollow upper part: back wall, two side walls with doorways, and the round front.
    parts += masonry(x0, x1, WALK, GATE_TOWER, -1.8, -1.8 + t, rng)
    colliders.append(span(x0, x1, WALK, GATE_TOWER, -1.8, -1.8 + t))
    for x, y in ((outer, WALK), (inner, GATE_WALK)):
        wx0, wx1 = sorted((x, x - s * t if x == outer else x + s * t))
        parts += masonry_along_z(wx0, wx1, WALK, GATE_TOWER, -1.8 + t, 0.6, rng, (door[0], door[1], y, y + 2.1))
        colliders += moved(spans_with_hole(-0.6, 1.8 - t, WALK, GATE_TOWER, wx0, wx1, (-door[1], -door[0], y, y + 2.1)), 90)
        parts.append(block(wx0 - 0.02, wx1 + 0.02, y + 2.1, y + 2.3, door[0] - 0.05, door[1] + 0.05, tint=COURSE_TINT))
    parts += ring(cx, 0.6, 1.0, WALK, GATE_TOWER, t, rng, skip=(2, 3, 4, 5, 6))
    colliders += ring_colliders(cx, 0.6, 1.0, WALK, GATE_TOWER, t, skip=(2, 3, 4, 5, 6))
    parts += turret_top(cx, 0.6, 1.0, GATE_TOWER, rng, skip=(3, 4, 5))
    colliders += ring_colliders(cx, 0.6, 1.2, GATE_TOWER, GATE_TOWER + PARAPET + MERLON, 0.4, skip=(3, 4, 5))
    # The back half of the top, square, has plain parapets on its three sides.
    parts += masonry(x0, x1, GATE_TOWER, GATE_TOWER + PARAPET, -1.8, -1.45, rng)
    parts += masonry_along_z(x0, x0 + 0.35, GATE_TOWER, GATE_TOWER + PARAPET, -1.45, 0.3, rng)
    parts += masonry_along_z(x1 - 0.35, x1, GATE_TOWER, GATE_TOWER + PARAPET, -1.45, 0.3, rng)
    colliders += [span(x0, x1, GATE_TOWER, GATE_TOWER + PARAPET, -1.8, -1.45),
                  span(x0, x0 + 0.35, GATE_TOWER, GATE_TOWER + PARAPET, -1.45, 0.3),
                  span(x1 - 0.35, x1, GATE_TOWER, GATE_TOWER + PARAPET, -1.45, 0.3)]
    # Floors with a hatch at the back, and the ladder against the back wall.
    hatch = (cx - 0.4, cx + 0.4, -1.8 + t, -0.6)
    for level in (GATE_WALK, GATE_TOWER):
        floor_parts, floor_colliders = floors_with_hatch(x0 + t, x1 - t, -1.8 + t, 1.4, hatch, level)
        parts += floor_parts
        colliders += floor_colliders
    ladder_z = -1.8 + t + 0.12
    parts += ladder_parts(cx, ladder_z, WALK, GATE_TOWER + 0.05)
    stops = [r3((cx, level + 0.05, -0.2)) for level in (WALK, GATE_WALK, GATE_TOWER)]
    ladders = [{"center": r3((cx, (WALK + GATE_TOWER + 0.05) / 2, ladder_z)), "size": r3((0.7, GATE_TOWER - WALK + 0.05, 0.15)),
                "stops": sum(stops, [])}]
    for y in (1.3, 3.6, 5.4):
        parts.append(slit(cx, y, 1.62))
    parts += moss(cx - 1.0, cx + 1.0, 0.5, 1.6, 1.85, rng, 2)
    parts += face_moss(cx - 0.4, cx + 0.4, 0.6, 2.0, 1.6, rng, 1)
    return parts, colliders, ladders


def gatehouse():
    """A stone gatehouse 8.6 m wide: an arched gateway with the wooden gate leaves of the palisade gate, a raised
    portcullis, machicolations over the gate, and two D-shaped towers standing out in front."""
    rng = random.Random("steinport")
    parts = []
    # Piers and the mass over the gateway; the gateway is 4 m wide and 3 m high.
    parts += masonry(-2.3, -2.0, -0.3, GATE_WALK, -1.8, 0.6, rng) + masonry(2.0, 2.3, -0.3, GATE_WALK, -1.8, 0.6, rng)
    parts += masonry(-2.3, 2.3, 3.0, GATE_WALK, -1.8, 0.6, rng)
    parts.append(part("stone_arch", (0, 3.25, 0.62), scale=(2.15, 0.55, 0.12), tint=COURSE_TINT))
    parts += gate_leaf(-2.0, "leaf_left", rng) + gate_leaf(0.0, "leaf_right", rng)
    # The portcullis, two iron grates side by side, hangs raised in a slot through the mass over the gateway: only its
    # spikes show under the arch, and its top stands in a chamber behind the parapet. The mod lowers the group
    # "portcullis" by PORTCULLIS_DROP to the ground, its collider with it.
    for x in (-1.0, 1.0):
        parts.append(part("iron_grate", (x, PORTCULLIS_DROP + 0.9, 0.4), scale=(1.0, 0.9, 1.0), tint=[0.6, 0.6, 0.62], group="portcullis"))
    # Machicolations: the parapet over the gate stands out on long corbels, with gaps to drop things through.
    for x in (-1.75, -1.05, -0.35, 0.35, 1.05, 1.75):
        parts.append(block(x - 0.15, x + 0.15, GATE_WALK - 0.5, GATE_WALK, 0.6, 1.1, tint=shade(rng, 0.85, 0.95)))
    parts += masonry(-2.3, 2.3, GATE_WALK, GATE_WALK + PARAPET, 0.6, 1.1, rng)
    parts += crenels(-2.3, 2.3, GATE_WALK + PARAPET, 0.6, 1.1, rng, [-1.6, 0.0, 1.6])
    parts.append(floor(-2.3, 2.3, -1.8, 0.2, GATE_WALK, "wood_floor"))
    parts += [torch((x, 2.2, 0.62)) for x in (-2.15, 2.15)]
    parts.append(part("trophy_deer", (0, 3.75, 0.7), scale=(1.1, 1.1, 1.1), detail=True))
    colliders = [box((1.0, 1.5, 0.05), (2.0, 3.0, 0.5), group="leaf_right"),
                 box((-1.0, 1.5, 0.05), (2.0, 3.0, 0.5), group="leaf_left"),
                 span(-2.3, -2.0, -0.3, GATE_WALK, -1.8, 0.6), span(2.0, 2.3, -0.3, GATE_WALK, -1.8, 0.6),
                 span(-2.3, 2.3, 3.0, GATE_WALK, -1.8, 0.6), span(-2.3, 2.3, GATE_WALK, GATE_WALK + PARAPET + MERLON, 0.6, 1.1),
                 box((0, PORTCULLIS_DROP + 1.35, 0.4), (4.0, 2.7, 0.2), group="portcullis")]
    ladders = []
    for side in (1, -1):
        tower_parts, tower_colliders, tower_ladders = gate_tower(side, rng)
        parts += tower_parts
        colliders += tower_colliders
        ladders += tower_ladders
    parts += rubble(-4.3, -2.6, 1.7, rng, 3) + rubble(2.6, 4.3, 1.7, rng, 3)
    snaps = [(-4.3, 0, 0), (4.3, 0, 0), (-4.3, WALK, 0), (4.3, WALK, 0)]
    groups = [{"name": "leaf_right", "pivot": [2.0, 0, 0.05]}, {"name": "leaf_left", "pivot": [-2.0, 0, 0.05], "mirror": True},
              {"name": "portcullis", "pivot": [0, 0, 0.4]}]
    return parts, colliders, snaps, groups, ladders


def dragons_teeth():
    """Dragon's teeth: square stone posts with a pointed cap, in two staggered rows, the stone answer to the cheval de
    frise. The cap is a block stood on its corner, so its top is a sharp three-sided point."""
    rng = random.Random("draketenner")
    parts = []
    for x, z in ((-1.8, -0.35), (-0.6, -0.35), (0.6, -0.35), (1.8, -0.35), (-1.2, 0.4), (0.0, 0.4), (1.2, 0.4)):
        x += rng.uniform(-0.05, 0.05)
        width = rng.uniform(0.5, 0.6)
        top = rng.uniform(0.75, 0.95) * (1.15 if z > 0 else 1.0)
        yaw = rng.uniform(0, 90)
        post_parts = [block(-width / 2, width / 2, -0.15, top, -width / 2, width / 2, tint=shade(rng, 0.8, 0.93))]
        post_parts.append(part("stone_wall_1x1", (0, top + 0.05, 0), (35.264, 0, 45), (width * 1.05,) * 3, tint=shade(rng, 0.87, 1.0)))
        for p in post_parts:
            moved = rotate(p, yaw)
            moved["position"] = r3((moved["position"][0] + x, moved["position"][1], moved["position"][2] + z))
            parts.append(moved)
        size = width * 1.6
        parts.append(part("rock", (x, -0.05, z), (0, rng.uniform(0, 360), 0), (size, size * 0.35, size), tint=STONE["damp"], detail=True))
        parts += moss(x - 0.35, x + 0.35, 0.12, z - 0.3, z + 0.3, rng, 1)
    colliders = [box((0, 0.7, 0), (4.4, 1.4, 1.6))]
    snaps = [(-2.2, 0, 0), (2.2, 0, 0)]
    return parts, colliders, snaps


def stone_drawbridge():
    """The drawbridge deck of the palisade, hinged between two stone piers joined by an arch."""
    rng = random.Random("steinvindebro")
    parts, colliders, snaps, groups = drawbridge()
    parts = [p for p in parts if p.get("group") == "deck"]
    colliders = [c for c in colliders if c.get("group") == "deck"]
    for x in (-2.5, 2.5):
        parts += masonry(x - 0.4, x + 0.4, -0.3, 0.4, -0.6, 0.3, rng)
        parts += masonry(x - 0.32, x + 0.32, 0.4, 3.9, -0.5, 0.2, rng)
        parts += moss(x - 0.4, x + 0.4, 0.4, 0.2, 0.3, rng, 1) + face_moss(x - 0.25, x + 0.25, 0.5, 1.5, 0.2, rng, 1)
        colliders.append(box((x, 1.8, -0.15), (0.8, 4.2, 0.9)))
    for x in (-2.15, -1.45, -0.75, 0.0, 0.75, 1.45, 2.15):
        parts.append(block(x - 0.12, x + 0.12, 3.6, 3.9, -0.45, 0.3, tint=shade(rng, 0.85, 0.93), detail=True))
    parts += masonry(-2.85, 2.85, 3.9, 4.5, -0.5, 0.3, rng)
    parts += crenels(-2.85, 2.85, 4.5, -0.5, 0.3, rng, [-2.3, -0.75, 0.75, 2.3])
    for x in (-1.9, 1.9):
        parts.append(part("chains", (x, 2.55, 0.25), (0, 90, 0), (1.0, 1.35, 1.0), detail=True))
    colliders.append(box((0, 4.55, -0.1), (5.7, 1.3, 0.8)))
    return parts, colliders, snaps, groups


def defence(name, parts, colliders, snaps, base="stone_wall_4x2", **extra):
    data = {"name": name, "base": base, "parts": parts, "colliders": colliders, "snapPoints": sum((r3(p) for p in snaps), [])}
    data.update({key: value for key, value in extra.items() if value})
    return data


def stone_pieces(stone="stein"):
    """Every stone defence of one stone, and for plain stone a fort put together from them for the preview. Pieces of
    another stone than plain stone have its name after theirs, e.g. steinmur_marmor."""
    with material(stone):
        return _stone_pieces("" if stone == "stein" else "_" + stone, stone == "stein")


def _stone_pieces(suffix, overview):
    def defence_(name, *args, **extra):
        return defence(name + suffix, *args, **extra)

    pieces = [defence_("steinmur", *stone_wall(), views=views(("outside", 160, 12), ("inside", -20, 25), ("side", 90, 8)))]
    pieces.append(defence_("steinmur_slett", *stone_wall(buttress=False), views=views(("outside", 160, 12), ("inside", -20, 25), ("side", 90, 8))))
    pieces.append(defence_("steinhjorne", *corner90(), views=views(("outside", 135, 15), ("inside", -45, 25), ("top", -45, 60))))
    pieces.append(defence_("steinhjorne45", *corner45(), views=views(("outside", 160, 15), ("inside", -20, 30), ("top", -20, 65))))
    gate_parts, gate_colliders, gate_snaps, gate_groups, gate_ladders = gatehouse()
    pieces.append(defence_("steinport", gate_parts, gate_colliders, gate_snaps, base="wood_gate", groups=gate_groups, ladders=gate_ladders,
                          keep=["door"], views=views(("outside", 170, 12), ("angle", 135, 20), ("inside", -20, 25))))
    pieces.append(defence_("steintrapp", *stairs(), views=views(("side", 90, 10), ("front", 0, 20), ("angle", -40, 30))))
    for name, n, levels, roofed in (("steintarn_liten", 2, [WALK, 5.6], False), ("steintarn", 3, [WALK, 5.6], False),
                                    ("steintarn_stor", 4, [WALK, 5.6, 8.2], True)):
        parts, colliders, ladders, snaps = stone_tower(n, levels, name, roofed)
        pieces.append(defence_(name, parts, colliders, snaps, ladders=ladders,
                              views=views(("outside", 150, 12), ("back", -30, 20), ("top", 150, 55))))
    pieces.append(defence_("draketenner", *dragons_teeth(), base="piece_sharpstakes", keep=["HIT AREA"], hitArea=box((0, 0.7, 0), (4.6, 1.4, 1.9)),
                          views=views(("front", 170, 15), ("side", 90, 10), ("angle", -40, 30))))
    bridge_parts, bridge_colliders, bridge_snaps, bridge_groups = stone_drawbridge()
    pieces.append(defence_("steinvindebro", bridge_parts, bridge_colliders, bridge_snaps, base="wood_gate", groups=bridge_groups, keep=["door"],
                          views=views(("outside", 160, 20), ("side", 90, 10), ("top", 180, 60))))
    if not overview:
        return pieces
    pieces.append({"name": "steinborg", "parts": [
        piece("steinport"),
        piece("steinmur", (-6.3, 0, 0)),
        piece("steinmur_slett", (-10.3, 0, 0)),
        piece("steinmur", (6.3, 0, 0)),
        piece("steintarn_liten", (9.3, 0, -0.8)),
        piece("steinmur_slett", (12.3, 0, 0)),
        piece("steinhjorne45", (14.3, 0, 0)),
        piece("steinmur", (15.71, 0, -1.41), (0, 45, 0)),
        piece("steintarn_stor", (-14.3, 0, -1.0)),
        piece("steinhjorne", (-12.3, 0, -2.0), (0, -90, 0)),
        piece("steintarn", (-9.0, 0, -8.0)),
        piece("steintrapp", (-6.3, 0, -1.8)),
        piece("draketenner", (-4, 0, 5)),
        piece("draketenner", (5, 0, 5.5), (0, 15, 0)),
    ], "views": views(("outside", 200, 18), ("inside", 20, 30), ("top", 0, 70))})
    return pieces
