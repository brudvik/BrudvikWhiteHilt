"""The gate controls: the Gate Rope and the Windlass House, laid out for defenses.json.

build_defenses.py adds gate_control_pieces() to the layout it writes; run that script, not this one.
Pieces/Defenses/GateControl builds them in the game. Both work the gatehouses, drawbridges and portcullises near them,
of wood or stone. The moving parts lie in groups the mod turns: "pull" on the rope, and "wheel", "crank" and "lever"
in the windlass house, whose colliders in those groups are what the player uses.
"""

import math
import random

from build_defenses import box, log, part, post, r3, views
from build_stone_defenses import COURSE_TINT, SLATE, block, masonry, masonry_along_z, moss

ROPE_TINT = [0.8, 0.66, 0.45]
IRON_TINT = [0.55, 0.55, 0.58]
WHEEL_PIVOT = (-0.4, 1.15, -0.25)
WHEEL_RADIUS = 0.75
CRANK_PIVOT = (0.8, 0.9, -0.6)
LEVER_PIVOT = (0.55, 0.32, 0.65)


def coil(cx, cz, y, radius, turns, seed, rope=0.026):
    """A coil of rope lying round a point."""
    rng = random.Random(seed)
    parts = []
    segments = 22
    for turn in range(turns):
        level = y + turn * 0.045
        r = radius - turn * 0.01
        offset = rng.uniform(0, 1)
        for i in range(segments):
            a0 = 2 * math.pi * (i - 0.25 + offset) / segments
            a1 = 2 * math.pi * (i + 1.25 + offset) / segments
            parts.append(log((cx + math.sin(a0) * r, level, cz + math.cos(a0) * r), (cx + math.sin(a1) * r, level, cz + math.cos(a1) * r),
                             rope, tint=ROPE_TINT, detail=True))
    return parts


def gate_rope():
    """A post with a cleat and a coil of rope. The rope runs up over a block at the top and hangs down the front to a
    wooden toggle, in the group "pull", which the mod pulls down when the rope is used."""
    top = 1.9
    parts = [post(0, 0, -0.3, top, 0.09)]
    # A horn cleat on the front, with the rope made fast round it.
    parts.append(log((-0.16, 1.05, 0.1), (0.16, 1.05, 0.1), 0.035))
    parts.append(log((0, 1.0, 0.04), (0, 1.05, 0.1), 0.03))
    for k in range(3):
        a = 0.7 + k * 0.5
        parts.append(log((-0.13, 1.05 + 0.02 * k, 0.13), (0.13, 1.07 - 0.02 * k, 0.07), 0.018, tint=ROPE_TINT))
    # The block at the top and the rope over it.
    parts.append(block(-0.07, 0.07, top - 0.05, top + 0.13, -0.02, 0.16, tint=[0.6, 0.48, 0.35]))
    parts.append(log((0, 1.07, 0.11), (0, top + 0.02, 0.15), 0.015, tint=ROPE_TINT))
    parts.append(log((0, top + 0.13, 0.0), (0, top + 0.13, -0.6), 0.015, tint=ROPE_TINT))
    # The hanging end with its toggle, which moves.
    parts.append(log((0.04, top, 0.17), (0.04, 1.35, 0.17), 0.015, tint=ROPE_TINT, group="pull"))
    parts.append(log((-0.07, 1.3, 0.17), (0.15, 1.3, 0.17), 0.028, group="pull"))
    parts += coil(0, 0, 0.0, 0.2, 3, "porttau")
    colliders = [box((0, 0.8, 0), (0.3, 2.2, 0.3)), box((0.04, 1.55, 0.17), (0.3, 0.6, 0.15), group="pull")]
    groups = [{"name": "pull", "pivot": [0.04, top, 0.17]}]
    return parts, colliders, [(0, 0, 0)], groups


def wheel(rng):
    """The great windlass wheel facing the open front: a rim of twelve pieces, six spokes and handles round the rim, on
    an axle running back to the rear wall."""
    px, py, pz = WHEEL_PIVOT
    parts = []
    sides = 12
    for i in range(sides):
        a0, a1 = 2 * math.pi * i / sides, 2 * math.pi * (i + 1.08) / sides
        parts.append(log((px + math.sin(a0) * WHEEL_RADIUS, py + math.cos(a0) * WHEEL_RADIUS, pz),
                         (px + math.sin(a1) * WHEEL_RADIUS, py + math.cos(a1) * WHEEL_RADIUS, pz), 0.05, group="wheel"))
        if i % 2 == 0:
            a = 2 * math.pi * i / sides
            parts.append(log((px, py, pz), (px + math.sin(a) * WHEEL_RADIUS, py + math.cos(a) * WHEEL_RADIUS, pz), 0.035, group="wheel"))
            parts.append(log((px + math.sin(a) * (WHEEL_RADIUS + 0.02), py + math.cos(a) * (WHEEL_RADIUS + 0.02), pz + 0.02),
                             (px + math.sin(a) * (WHEEL_RADIUS + 0.2), py + math.cos(a) * (WHEEL_RADIUS + 0.2), pz + 0.02), 0.025, group="wheel"))
    # The drum the chain winds on, running back to the rear wall.
    parts.append(log((px, py, pz - 0.5), (px, py, pz + 0.05), 0.14, group="wheel"))
    parts.append(log((px, py, pz - 0.55), (px, py, pz + 0.12), 0.05, tint=IRON_TINT))
    return parts


def crank():
    """A smaller drum on two posts with an iron crank handle, for the portcullis."""
    px, py, pz = CRANK_PIVOT
    parts = [log((px - 0.35, py, pz), (px + 0.3, py, pz), 0.11, group="crank")]
    parts.append(log((px + 0.3, py, pz), (px + 0.38, py, pz), 0.03, tint=IRON_TINT, group="crank"))
    parts.append(log((px + 0.38, py, pz), (px + 0.38, py + 0.32, pz), 0.025, tint=IRON_TINT, group="crank"))
    parts.append(log((px + 0.38, py + 0.32, pz), (px + 0.55, py + 0.32, pz), 0.03, group="crank"))
    for x in (px - 0.4, px + 0.27):
        parts.append(post(x, pz, 0.15, py + 0.05, 0.06))
    return parts


def lever():
    """A long lever in a stone socket, for the gate leaves."""
    px, py, pz = LEVER_PIVOT
    parts = [block(px - 0.18, px + 0.18, 0.15, py, pz - 0.18, pz + 0.18, tint=COURSE_TINT)]
    parts.append(log((px, py - 0.05, pz), (px, py + 1.15, pz), 0.045, group="lever"))
    parts.append(log((px, py + 1.05, pz), (px, py + 1.2, pz), 0.06, tint=[0.5, 0.36, 0.24], group="lever"))
    return parts


def windlass_house():
    """A small open-fronted stone house with a slate roof, holding the gate machinery: the great wheel for the
    drawbridge, the crank for the portcullis, the lever for the gate leaves, and their chains up through the roof."""
    rng = random.Random("vindehus")
    w, d, h = 1.6, 1.3, 2.4
    parts = masonry(-w - 0.15, w + 0.15, -0.2, 0.15, -d - 0.15, d + 0.15, rng)
    parts += masonry(-w, w, 0.15, h, -d, -d + 0.4, rng)
    for x0, x1 in ((-w, -w + 0.4), (w - 0.4, w)):
        parts += masonry(x0, x1, 0.15, h, -d + 0.4, d, rng)
    # A lintel beam over the open front, and a gable roof of slate slabs with a stone gable at the back.
    parts.append(block(-w - 0.05, w + 0.05, h - 0.1, h + 0.05, d - 0.25, d + 0.05, tint=[0.55, 0.42, 0.3]))
    run, rise = d + 0.35, 0.9
    slope = math.degrees(math.atan2(rise, run))
    length = math.hypot(run, rise)
    for side in (1, -1):
        parts.append(part("wood_floor", (0, h + rise / 2 - 0.02, side * run / 2), (side * slope, 0, 0), ((2 * w + 0.7) / 2, 0.8, length / 2),
                          texture=SLATE, tint=[0.62, 0.66, 0.74]))
    parts.append(log((-w - 0.35, h + rise + 0.03, 0), (w + 0.35, h + rise + 0.03, 0), 0.07))
    # Stone gables close the ends of the roof over the side walls.
    for x0, x1 in ((-w, -w + 0.4), (w - 0.4, w)):
        for y0, y1, half in ((h, h + 0.3, d * 0.85), (h + 0.3, h + 0.55, d * 0.5), (h + 0.55, h + 0.8, d * 0.18)):
            parts += masonry_along_z(x0, x1, y0, y1, -half, half, rng)
    parts.append(log((-w, h + 0.05, d - 0.1), (w, h + 0.05, d - 0.1), 0.08))
    parts += wheel(rng) + crank() + lever()
    parts.append(post(WHEEL_PIVOT[0], WHEEL_PIVOT[2] + 0.2, 0.15, WHEEL_PIVOT[1] - 0.08, 0.07))
    # Chains from the drums up through the roof, towards the gate.
    for x, y, z in ((WHEEL_PIVOT[0], WHEEL_PIVOT[1] + 0.14, WHEEL_PIVOT[2] - 0.3), (CRANK_PIVOT[0], CRANK_PIVOT[1] + 0.11, CRANK_PIVOT[2])):
        parts.append(part("chains", (x, y, z), (0, 90, 0), (1.0, h - y + 0.1, 1.0)))
    parts.append(part("chains", (LEVER_PIVOT[0], LEVER_PIVOT[1] + 1.2, LEVER_PIVOT[2]), (0, 90, 0), (1.0, h - LEVER_PIVOT[1] - 1.1, 1.0)))
    parts += moss(-w - 0.15, w + 0.15, 0.15, d, d + 0.15, rng, 2)
    parts.append(part("piece_walltorch", (w - 0.4, 1.7, 0.0), (0, 180, 0), detail=True))
    colliders = [box((0, -0.025, 0), (2 * w + 0.3, 0.35, 2 * d + 0.3)), box((0, h / 2, -d + 0.2), (2 * w, h, 0.4))]
    colliders += [box((x, h / 2, 0.2), (0.4, h, 2 * d - 0.4)) for x in (-w + 0.2, w - 0.2)]
    colliders.append(box((0, h + 0.5, 0), (2 * w + 0.7, 0.1, 2 * d + 0.7)))
    colliders.append(box(WHEEL_PIVOT, (2 * WHEEL_RADIUS + 0.3, 2 * WHEEL_RADIUS + 0.3, 0.3), group="wheel"))
    colliders.append(box((CRANK_PIVOT[0] + 0.1, CRANK_PIVOT[1] + 0.1, CRANK_PIVOT[2]), (0.9, 0.5, 0.35), group="crank"))
    colliders.append(box((LEVER_PIVOT[0], LEVER_PIVOT[1] + 0.6, LEVER_PIVOT[2]), (0.2, 1.2, 0.2), group="lever"))
    groups = [{"name": "wheel", "pivot": list(WHEEL_PIVOT)}, {"name": "crank", "pivot": list(CRANK_PIVOT)},
              {"name": "lever", "pivot": list(LEVER_PIVOT)}]
    snaps = [(-w, 0, d), (w, 0, d), (-w, 0, -d), (w, 0, -d)]
    return parts, colliders, snaps, groups


def gate_control_pieces():
    """The Gate Rope and the Windlass House."""
    rope_parts, rope_colliders, rope_snaps, rope_groups = gate_rope()
    house_parts, house_colliders, house_snaps, house_groups = windlass_house()
    return [
        {"name": "porttau", "base": "wood_pole2", "parts": rope_parts, "colliders": rope_colliders,
         "snapPoints": sum((r3(p) for p in rope_snaps), []), "groups": rope_groups,
         "views": views(("front", 180, 10), ("side", 90, 10), ("angle", -140, 20))},
        {"name": "vindehus", "base": "stone_wall_4x2", "parts": house_parts, "colliders": house_colliders,
         "snapPoints": sum((r3(p) for p in house_snaps), []), "groups": house_groups,
         "views": views(("front", 180, 6), ("angle", 150, 15), ("back", -30, 20))},
    ]
