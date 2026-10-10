"""Stone to build with, in plain stone, black marble and grausten: arched doorways and windows, a great arch, a stair,
cornices, columns, a balustrade and gables. Valheim gives black marble and grausten walls and floors, but players miss
the arches, frames, stairs and finishing pieces that would make a building of them.

build_defenses.py adds stone_building_pieces() to the layout it writes; run that script, not this one. The mod builds
them in Pieces/Stonework (StoneBuildingPieces.cs). Each shape is laid out once and built in each stone with the stone
defences' material() and block(): the black marble and grausten pieces end in _marmor and _grausten.

Every piece stands from y 0 with its face to +z, its walls centred on z 0 and 0.6 m thick, like the vanilla stone
walls. Arches are rings of wedge stones (voussoirs) with a keystone; the walling round them is laid in strips that
follow the curve, so the opening is round and walkable.
"""

import math
import random

from build_defenses import box, part, r3, ramp, views
from build_stone_defenses import STONEBOX, block, masonry, material, shade, span, spans_with_hole

DEPTH = 0.6
STONES = ("stein", "marmor", "grausten")
COLUMN_TEXTURE = {"stein": "stone", "marmor": None, "grausten": "Grausten_d"}


def suffix(stone):
    return "" if stone == "stein" else "_" + stone


def arch_ring(cx, spring, radius, thickness, depth, rng, count=9, z=0.0):
    """A semicircular ring of wedge stones over (cx, spring), the inner radius given, the middle one a keystone."""
    parts = []
    for i in range(count):
        a = math.pi * (i + 0.5) / count
        r = radius + thickness / 2
        chord = 2 * r * math.sin(math.pi / count / 2) * 1.04
        key = i == count // 2
        size = (chord * (1.15 if key else 1.0), thickness * (1.2 if key else 1.0), depth * (1.04 if key else 1.0))
        position = (cx + r * math.cos(a), spring + r * math.sin(a), z)
        parts.append(part(STONEBOX, position, (0, 0, math.degrees(a) - 90), size, tint=shade(rng, 0.93, 1.0),
                          **({"texture": TEX()} if TEX() else {})))
    return parts


def TEX():
    from build_stone_defenses import STONE
    return STONE["texture"]


def walling_round_arch(x0, x1, top, cx, spring, outer, depth, rng):
    """Walling from x0 to x1 up to top over a round opening: strips beside and above the ring, following its curve."""
    parts = []
    strips = 10
    for i in range(strips):
        a = cx - outer + 2 * outer * i / strips
        b = cx - outer + 2 * outer * (i + 1) / strips
        middle = (a + b) / 2
        bottom = spring + math.sqrt(max(0.0, outer * outer - (middle - cx) ** 2))
        if top - bottom > 0.02:
            parts.append(block(a, b, bottom, top, -depth / 2, depth / 2, tint=shade(rng)))
    # Beside the ring, from the springing up, where the wall is wider than the arch.
    for a, b in ((x0, cx - outer), (cx + outer, x1)):
        if b - a > 0.01:
            parts.append(block(a, b, spring, top, -depth / 2, depth / 2, tint=shade(rng)))
    return parts


def arch_colliders(cx, spring, radius, top, depth):
    """Boxes over a round opening, in strips, so the opening is free under the ring."""
    boxes = []
    strips = 8
    for i in range(strips):
        a = cx - radius + 2 * radius * i / strips
        b = cx - radius + 2 * radius * (i + 1) / strips
        bottom = spring + math.sqrt(max(0.0, radius * radius - max((a - cx) ** 2, (b - cx) ** 2)))
        boxes.append(span(a, b, bottom, top, -depth / 2, depth / 2))
    return boxes


def arched_doorway(seed):
    """A wall 2 m wide and 3 m high with a round-arched doorway 1.4 m wide, springing at 2 m: 2.7 m to the crown."""
    rng = random.Random(seed)
    half, spring, radius, ring, top = 0.7, 2.0, 0.7, 0.24, 3.0
    parts = masonry(-1, -half, 0, spring, -DEPTH / 2, DEPTH / 2, rng)
    parts += masonry(half, 1, 0, spring, -DEPTH / 2, DEPTH / 2, rng)
    parts += arch_ring(0, spring, radius, ring, DEPTH + 0.04, rng)
    parts += walling_round_arch(-1, 1, top, 0, spring, radius + ring, DEPTH, rng)
    # The springers: a step out at the foot of the arch.
    for x in (-half - 0.08, half + 0.08):
        parts.append(block(x - 0.12, x + 0.12, spring - 0.12, spring, -DEPTH / 2 - 0.04, DEPTH / 2 + 0.04, tint=shade(rng, 0.93, 1.0)))
    colliders = [span(-1, -half, 0, top, -DEPTH / 2, DEPTH / 2), span(half, 1, 0, top, -DEPTH / 2, DEPTH / 2)]
    colliders += arch_colliders(0, spring, radius, top, DEPTH)
    snaps = [(x, y, 0) for x in (-1, 1) for y in (0, 2, 3)]
    return parts, colliders, snaps


def arched_window(seed):
    """A wall 2 m wide and 2 m high with a window 0.8 m wide at eye height: a sill at 1 m and a round head from 1.5 m
    to its crown at 1.9 m."""
    rng = random.Random(seed)
    half, sill, spring, ring, top = 0.4, 1.0, 1.5, 0.16, 2.0
    parts = masonry(-1, 1, 0, sill, -DEPTH / 2, DEPTH / 2, rng)
    parts += masonry(-1, -half, sill, top, -DEPTH / 2, DEPTH / 2, rng)
    parts += masonry(half, 1, sill, top, -DEPTH / 2, DEPTH / 2, rng)
    parts += arch_ring(0, spring, half, ring * 0.6, DEPTH + 0.02, rng, count=7)
    parts += walling_round_arch(-half, half, top, 0, spring, half + ring * 0.6, DEPTH, rng)
    parts.append(block(-half - 0.12, half + 0.12, sill - 0.08, sill, -DEPTH / 2 - 0.08, DEPTH / 2 + 0.04, tint=shade(rng, 0.93, 1.0)))
    hole = (-half, half, sill, spring)
    colliders = spans_with_hole(-1, 1, 0, spring, -DEPTH / 2, DEPTH / 2, hole)
    colliders += [span(-1, -half, spring, top, -DEPTH / 2, DEPTH / 2), span(half, 1, spring, top, -DEPTH / 2, DEPTH / 2)]
    colliders += arch_colliders(0, spring, half, top, DEPTH)
    snaps = [(x, y, 0) for x in (-1, 1) for y in (0, 2)]
    return parts, colliders, snaps


def great_arch(seed):
    """A great arch 4 m wide and 4 m high: an opening 3 m wide springing at 2 m, for a hall, a gateway or a bridge."""
    rng = random.Random(seed)
    half, spring, ring, top = 1.5, 2.0, 0.32, 4.0
    parts = masonry(-2, -half, 0, spring, -DEPTH / 2, DEPTH / 2, rng)
    parts += masonry(half, 2, 0, spring, -DEPTH / 2, DEPTH / 2, rng)
    parts += arch_ring(0, spring, half, ring, DEPTH + 0.06, rng, count=13)
    parts += walling_round_arch(-2, 2, top, 0, spring, half + ring, DEPTH, rng)
    for x in (-half - 0.1, half + 0.1):
        parts.append(block(x - 0.15, x + 0.15, spring - 0.15, spring, -DEPTH / 2 - 0.05, DEPTH / 2 + 0.05, tint=shade(rng, 0.93, 1.0)))
    colliders = [span(-2, -half, 0, top, -DEPTH / 2, DEPTH / 2), span(half, 2, 0, top, -DEPTH / 2, DEPTH / 2)]
    colliders += arch_colliders(0, spring, half, top, DEPTH)
    snaps = [(x, y, 0) for x in (-2, 0, 2) for y in (0, 2, 4) if not (x == 0 and y < 4)]
    return parts, colliders, snaps


def stair(seed):
    """A stone stair 2 m wide rising 2 m over 4 m, the vanilla stair's slope: eight solid steps from +z up to -z."""
    rng = random.Random(seed)
    parts = []
    for i in range(8):
        y1 = 0.25 * (i + 1)
        z0, z1 = 2.0 - 0.5 * (i + 1), 2.0 - 0.5 * i
        parts.append(block(-1, 1, 0, y1 - 0.06, z0, z1, mesh="stone_wall_2x1", tint=shade(rng)))
        parts.append(block(-1.02, 1.02, y1 - 0.08, y1, z0 - 0.04, z1, tint=shade(rng, 0.93, 1.0)))
    colliders = [ramp((0, 0, 2.0), (0, 2.0, -2.0), 2.0), box((0, 0.5, -0.5), (2.0, 1.0, 3.0))]
    snaps = [(x, 0, 2.0) for x in (-1, 1)] + [(x, 2.0, -2.0) for x in (-1, 1)]
    return parts, colliders, snaps


def cornice(seed, corner=False):
    """A cornice for the top of a wall, 2 m along x: three courses stepping out over the face (+z), 0.4 m high. The
    corner turns it round an outer corner, the walls running along -x and -z from the origin."""
    rng = random.Random(seed)
    parts = []
    steps = [(0.0, 0.14, 0.08), (0.14, 0.26, 0.17), (0.26, 0.4, 0.26)]
    for y0, y1, out in steps:
        tint = shade(rng, 0.93, 1.0)
        if corner:
            parts.append(block(-1, out + DEPTH / 2, y0, y1, -DEPTH / 2, DEPTH / 2 + out, tint=tint))
            parts.append(block(-DEPTH / 2, DEPTH / 2 + out, y0, y1, -1, DEPTH / 2, tint=tint))
        else:
            parts.append(block(-1, 1, y0, y1, -DEPTH / 2, DEPTH / 2 + out, tint=tint))
    if corner:
        colliders = [span(-1, DEPTH / 2 + 0.26, 0, 0.4, -DEPTH / 2, DEPTH / 2 + 0.26), span(-DEPTH / 2, DEPTH / 2 + 0.26, 0, 0.4, -1, DEPTH / 2)]
        snaps = [(0, 0, 0), (-1, 0, 0), (0, 0, -1), (0, 0.4, 0)]
    else:
        colliders = [span(-1, 1, 0, 0.4, -DEPTH / 2, DEPTH / 2 + 0.26)]
        snaps = [(-1, 0, 0), (1, 0, 0), (-1, 0.4, 0), (1, 0.4, 0)]
    return parts, colliders, snaps


def column(stone, seed):
    """A column 3 m high: a square base, a shaft of the black marble column stone (in this stone's texture) and a
    capital stepping out under a square abacus."""
    rng = random.Random(seed)
    texture = COLUMN_TEXTURE[stone]
    extra = {"texture": texture} if texture else {}
    parts = [block(-0.42, 0.42, 0, 0.2, -0.42, 0.42, tint=shade(rng, 0.93, 1.0)), block(-0.34, 0.34, 0.2, 0.34, -0.34, 0.34, tint=shade(rng, 0.93, 1.0))]
    for i in range(5):
        y0 = 0.34 + i * 0.42
        parts.append(part("blackmarble_column_1", (0, y0 + 0.21, 0), (0, 45 * (i % 2), 0), (0.5, 0.42, 0.5), tint=shade(rng), **extra))
    parts.append(block(-0.3, 0.3, 2.44, 2.62, -0.3, 0.3, tint=shade(rng, 0.93, 1.0)))
    parts.append(block(-0.4, 0.4, 2.62, 2.8, -0.4, 0.4, tint=shade(rng, 0.93, 1.0)))
    parts.append(block(-0.5, 0.5, 2.8, 3.0, -0.5, 0.5, tint=shade(rng, 0.93, 1.0)))
    colliders = [span(-0.42, 0.42, 0, 3.0, -0.42, 0.42)]
    return parts, colliders, [(0, 0, 0), (0, 3.0, 0)]


def balustrade(seed):
    """A balustrade 2 m long and 1 m high: a plinth, stone balusters and a coping rail, for a terrace, a gallery or a
    bridge."""
    rng = random.Random(seed)
    parts = [block(-1, 1, 0, 0.18, -0.2, 0.2, tint=shade(rng)), block(-1.02, 1.02, 0.86, 1.0, -0.22, 0.22, tint=shade(rng, 0.93, 1.0))]
    for x in (-0.95, 0.95):
        parts.append(block(x - 0.08, x + 0.08, 0.18, 0.86, -0.16, 0.16, tint=shade(rng)))
    for i in range(7):
        x = -0.72 + 0.24 * i
        parts.append(block(x - 0.05, x + 0.05, 0.18, 0.3, -0.07, 0.07, tint=shade(rng, 0.93, 1.0)))
        parts.append(block(x - 0.035, x + 0.035, 0.3, 0.74, -0.05, 0.05, tint=shade(rng, 0.93, 1.0)))
        parts.append(block(x - 0.05, x + 0.05, 0.74, 0.86, -0.07, 0.07, tint=shade(rng, 0.93, 1.0)))
    colliders = [span(-1, 1, 0, 1.0, -0.2, 0.2)]
    return parts, colliders, [(-1, 0, 0), (1, 0, 0), (-1, 1.0, 0), (1, 1.0, 0)]


def gable(rise, seed):
    """Half a stone gable 2 m along x, from y 0 at x -1 to rise at x +1, the slope of the vanilla 26 or 45 degree roof:
    courses that step in under a coping laid along the slope."""
    rng = random.Random(seed)
    parts = []
    courses = 4 if rise <= 1 else 6
    height = rise / courses
    for i in range(courses):
        y0, y1 = i * height, (i + 1) * height
        x0 = -1 + 2 * y1 / rise
        if 1 - x0 > 0.05:
            parts.append(block(x0 - 0.05, 1, y0, y1, -DEPTH / 2, DEPTH / 2, tint=shade(rng)))
    length = math.hypot(2, rise)
    angle = math.degrees(math.atan2(rise, 2))
    texture = TEX()
    parts.append(part(STONEBOX, (0, rise / 2 - 0.02, 0), (0, 0, angle), (length + 0.1, 0.16, DEPTH + 0.12), tint=shade(rng, 0.93, 1.0),
                      **({"texture": texture} if texture else {})))
    colliders = []
    for i in range(courses):
        y0, y1 = i * height, (i + 1) * height
        x0 = -1 + 2 * y0 / rise
        colliders.append(span(x0, 1, y0, y1, -DEPTH / 2, DEPTH / 2))
    return parts, colliders, [(-1, 0, 0), (1, 0, 0), (1, rise, 0)]


SHAPES = [
    ("buegang", lambda stone, seed: arched_doorway(seed), views(("front", 180, 12), ("angle", 140, 20))),
    ("buevindu", lambda stone, seed: arched_window(seed), views(("front", 180, 12), ("angle", 140, 20))),
    ("storbue", lambda stone, seed: great_arch(seed), views(("front", 180, 12), ("angle", 140, 20))),
    ("steintrinn", lambda stone, seed: stair(seed), views(("side", 90, 10), ("angle", 140, 25))),
    ("gesims", lambda stone, seed: cornice(seed), views(("front", 160, 20), ("end", 90, 10))),
    ("gesims_hjorne", lambda stone, seed: cornice(seed, corner=True), views(("outside", 135, 25), ("top", 135, 70))),
    ("steinsoyle", lambda stone, seed: column(stone, seed), views(("front", 160, 10))),
    ("brystning", lambda stone, seed: balustrade(seed), views(("front", 160, 15), ("end", 90, 10))),
    ("steingavl_26", lambda stone, seed: gable(1.0, seed), views(("front", 160, 12), ("angle", -140, 20))),
    ("steingavl_45", lambda stone, seed: gable(2.0, seed), views(("front", 160, 12), ("angle", -140, 20))),
]


def stone_building_pieces():
    pieces = []
    for stone in STONES:
        with material(stone):
            for name, build, piece_views in SHAPES:
                parts, colliders, snaps = build(stone, name)
                pieces.append({"name": name + suffix(stone), "base": "stone_wall_2x1", "parts": parts, "colliders": colliders,
                               "snapPoints": sum((r3(p) for p in snaps), []), "views": piece_views})
    pieces.append(overview())
    return pieces


def overview():
    """A front in each stone for the documentation: columns and a great arch, an arched doorway and windows, a cornice
    and a balustrade."""
    def at(name, x, z, yaw=0, y=0.0):
        data = {"piece": name, "position": r3((x, y, z))}
        if yaw:
            data["rotation"] = [0, yaw, 0]
        return data

    parts = []
    for i, stone in enumerate(STONES):
        s = suffix(stone)
        z = -6 * i
        parts += [at("storbue" + s, 0, z), at("buegang" + s, -3, z), at("buegang" + s, 3, z)]
        parts += [at("buevindu" + s, -3, z, 0, 3.0), at("buevindu" + s, 3, z, 0, 3.0)]
        parts += [at("gesims" + s, x, z, 0, 4.0) for x in (-3, -1, 1, 3)]
        parts += [at("brystning" + s, x, z + 2.5) for x in (-3, 3)]
        parts += [at("steinsoyle" + s, x, z + 1.2) for x in (-2, 2)]
    return {"name": "steinbygg", "parts": parts, "views": views(("front", 160, 15), ("angle", -140, 25))}
