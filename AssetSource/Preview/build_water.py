"""Bridges, jetties and quays: building pieces for the water's edge, which snap end to end.

build_defenses.py adds water_pieces() to the layout it writes; run that script, not this one. The mod builds them in
Pieces/Water.

Every piece has its walking surface at y 0 where it meets the land or the next piece, so it is placed at the height of
the bank or of the deck before it; what is under it (piles, piers, the quay's face) reaches down to y -3, into the water
and the ground. Bridges and jetties run along x; a quay and a jetty face the water at +z.
"""

import math
import random

from build_borgund import pole
from build_defenses import box, part, r3, ramp, views
from build_defence_extras import plank
from build_farm import GREY_POLE
from build_gate_controls import IRON_TINT, ROPE_TINT
from build_log_house import TAR_DOOR_TINT
from build_stone_building import arch_ring
from build_stone_defenses import block, masonry, shade, span

DECK = [0.78, 0.7, 0.6]  # weathered deck boards
PILE = [0.5, 0.42, 0.34]  # tarred piles, dark where the water reaches
BOTTOM = -3.0


def deck(x0, x1, z0, z1, rng, y=0.0):
    """Deck boards across x0..x1 (laid along z), on two stringers under them."""
    parts = []
    count = max(2, round((x1 - x0) / 0.25))
    for i in range(count):
        a = x0 + (x1 - x0) * i / count
        b = x0 + (x1 - x0) * (i + 1) / count
        shade_ = rng.choice((0.88, 0.94, 1.0))
        parts.append(plank(((a + b) / 2, y - 0.04, z0), ((a + b) / 2, y - 0.04, z1), (b - a) * 0.94, 0.08, tint=[c * shade_ for c in DECK]))
    for z in (z0 + 0.25, z1 - 0.25):
        parts.append(plank((x0, y - 0.2, z), (x1, y - 0.2, z), 0.2, 0.24, tint=PILE))
    return parts


def piles(points, rng, top=-0.1):
    """Round piles from top down to BOTTOM at each (x, z), with a cross brace between each pair along x."""
    parts = []
    for x, z in points:
        parts.append(pole((x, BOTTOM, z), (x, top, z), 0.14 * rng.choice((0.9, 1.0)), tint=PILE))
    return parts


def jetty(length, seed):
    """A jetty section length along x and 2 m wide (z -1..1), its deck at y 0 on piles reaching 3 m down."""
    rng = random.Random(seed)
    half = length / 2
    parts = deck(-half, half, -1, 1, rng)
    xs = [-half + 0.2 + (length - 0.4) * i / max(1, round(length / 2)) for i in range(round(length / 2) + 1)]
    parts += piles([(x, z) for x in xs for z in (-0.85, 0.85)], rng)
    for x in xs:
        parts.append(pole((x, -1.8, -0.85), (x, -0.4, 0.85), 0.05, tint=PILE, detail=True))
        parts.append(plank((x, -0.35, -1.05), (x, -0.35, 1.05), 0.18, 0.16, tint=PILE))
    colliders = [box((0, -0.06, 0), (length, 0.14, 2.0))]
    colliders += [box((x, (BOTTOM - 0.1) / 2, z), (0.3, -BOTTOM, 0.3)) for x in xs for z in (-0.85, 0.85)]
    snaps = [(x, 0, z) for x in (-half, half) for z in (-1, 1)] + [(x, 0, 0) for x in (-half, half)]
    return parts, colliders, snaps


def jetty_head(seed):
    """The end of a jetty, 2 x 2 m: a deck with a bollard post on each outer corner and a stair down the end (+x)
    to 1.5 m below the deck, for climbing up from a boat."""
    rng = random.Random(seed)
    parts = deck(-1, 1, -1, 1, rng)
    parts += piles([(x, z) for x in (-0.8, 0.8) for z in (-0.85, 0.85)], rng)
    for z in (-0.85, 0.85):
        parts.append(pole((0.85, -0.2, z), (0.85, 0.75, z), 0.13, tint=PILE))
        parts.append(pole((0.85, 0.55, z - 0.15), (0.85, 0.55, z + 0.15), 0.03, tint=ROPE_TINT, detail=True))
    # The stair down beside the end, along -z at x 1.
    for i in range(6):
        y = -0.25 * (i + 1)
        parts.append(plank((1.15, y, -0.35), (1.15, y, 0.35), 0.28, 0.06, tint=DECK))
    for z in (-0.4, 0.4):
        parts.append(plank((1.05, 0.0, z), (1.3, -1.6, z), 0.06, 0.18, tint=PILE))
    colliders = [box((0, -0.06, 0), (2.0, 0.14, 2.0)), ramp((1.45, -1.5, 0), (1.05, 0.0, 0), 0.7)]
    colliders += [box((0.85, 0.35, z), (0.28, 0.8, 0.28)) for z in (-0.85, 0.85)]
    snaps = [(-1, 0, z) for z in (-1, 0, 1)] + [(x, 0, z) for x in (-1, 1) for z in (-1, 1)]
    return parts, colliders, snaps


QUAY_DEPTH = 2.0


def quay(seed, corner=False):
    """A stone quay 4 m along x, its top at y 0 and its face to the water at +z, reaching down to y -3: coursed
    masonry, a cope of long stones, a timber fender, an iron ring and a stone bollard. The corner turns it round to
    face +x too, the quay running along -x from it."""
    rng = random.Random(seed)
    parts = []
    face = 0.0
    if corner:
        parts += masonry(-2, 0, BOTTOM, -0.25, -QUAY_DEPTH, face, rng, damp=-1.0)
        parts += masonry(0, QUAY_DEPTH, BOTTOM, -0.25, -QUAY_DEPTH, face, rng, damp=-1.0)
        parts.append(block(-2, 0.25, -0.25, 0, -QUAY_DEPTH, face + 0.15, tint=shade(rng, 0.93, 1.0)))
        parts.append(block(0, QUAY_DEPTH + 0.15, -0.25, 0, -QUAY_DEPTH, face + 0.15, tint=shade(rng, 0.93, 1.0)))
        parts.append(block(-0.4, 0.4, 0, 0.7, -0.4, 0.4, tint=shade(rng, 0.93, 1.0)))
        parts.append(block(-0.3, 0.3, 0.7, 0.85, -0.3, 0.3, tint=shade(rng, 0.93, 1.0)))
        colliders = [span(-2, QUAY_DEPTH, BOTTOM, 0, -QUAY_DEPTH, face), span(-0.4, 0.4, 0, 0.85, -0.4, 0.4)]
        snaps = [(-2, 0, face), (-2, 0, -QUAY_DEPTH), (QUAY_DEPTH, 0, face), (QUAY_DEPTH, 0, -QUAY_DEPTH)]
        return parts, colliders, snaps
    parts += masonry(-2, 2, BOTTOM, -0.25, -QUAY_DEPTH, face, rng, damp=-1.0)
    for x0 in (-2, -1, 0, 1):
        parts.append(block(x0, x0 + 1, -0.25, 0, -QUAY_DEPTH, face + 0.15, tint=shade(rng, 0.93, 1.0)))
    # The fender: two timbers along the face, on short uprights.
    for y in (-0.6, -1.6):
        parts.append(plank((-2, y, face + 0.12), (2, y, face + 0.12), 0.16, 0.2, tint=TAR_DOOR_TINT))
    for x in (-1.5, 0, 1.5):
        parts.append(plank((x, -1.9, face + 0.06), (x, -0.4, face + 0.06), 0.12, 0.18, tint=TAR_DOOR_TINT))
    # An iron ring in the face, and a bollard on the cope.
    for i in range(10):
        a0, a1 = 2 * math.pi * i / 10, 2 * math.pi * (i + 1) / 10
        parts.append(plank((-1.0 + 0.14 * math.sin(a0), -0.45 + 0.14 * math.cos(a0), face + 0.06),
                           (-1.0 + 0.14 * math.sin(a1), -0.45 + 0.14 * math.cos(a1), face + 0.06), 0.03, 0.03, tint=IRON_TINT,
                           texture="metalwall", detail=True))
    parts.append(pole((1.0, 0.0, -0.4), (1.0, 0.55, -0.4), 0.16, tint=[0.55, 0.55, 0.55]))
    colliders = [span(-2, 2, BOTTOM, 0, -QUAY_DEPTH, face), box((1.0, 0.28, -0.4), (0.34, 0.56, 0.34))]
    snaps = [(x, 0, z) for x in (-2, 2) for z in (face, -QUAY_DEPTH)] + [(0, 0, face)]
    return parts, colliders, snaps


def quay_steps(seed):
    """Stone steps down the face of a quay, 4 m along x: they run down along the face from y 0 at x -2 to y -2 at x +2,
    on a wall 0.8 m out from it."""
    rng = random.Random(seed)
    parts = []
    for i in range(8):
        x0, x1 = -2 + 0.5 * i, -2 + 0.5 * (i + 1)
        top = -0.25 * (i + 1)
        parts.append(block(x0, x1, BOTTOM, top, 0, 0.8, tint=shade(rng)))
    colliders = [ramp((-2.0, 0.0, 0.4), (2.0, -2.0, 0.4), 0.8), span(-2, 2, BOTTOM, -2.1, 0, 0.8)]
    snaps = [(-2, 0, 0), (2, 0, 0), (-2, 0, 0.8)]
    return parts, colliders, snaps


def log_bridge(length, seed):
    """A bridge of logs across a stream, length along x and 2 m wide: two log stringers resting on the banks at the
    ends, deck boards across them and a log railing on posts."""
    rng = random.Random(seed)
    half = length / 2
    parts = []
    for z in (-0.65, 0.65):
        parts.append(pole((-half - 0.3, -0.35, z), (half + 0.3, -0.35, z), 0.24, tint=PILE))
    parts += deck(-half, half, -1, 1, rng)[:-2]
    posts = [-half + 0.15 + (length - 0.3) * i / max(1, round(length / 2)) for i in range(round(length / 2) + 1)]
    for z in (-0.95, 0.95):
        for x in posts:
            parts.append(pole((x, -0.4, z), (x, 1.05, z), 0.07, tint=GREY_POLE))
        parts.append(pole((-half, 1.0, z), (half, 1.0, z), 0.06, tint=GREY_POLE))
        parts.append(pole((-half, 0.5, z), (half, 0.5, z), 0.045, tint=GREY_POLE, detail=True))
    colliders = [box((0, -0.06, 0), (length, 0.14, 2.0))] + [box((0, 0.55, z), (length, 1.1, 0.12)) for z in (-0.95, 0.95)]
    snaps = [(x, 0, z) for x in (-half, half) for z in (-1, 0, 1)]
    return parts, colliders, snaps


ARCH_SPAN = 3.0  # half the opening under the stone bridge
HUMP = 0.8  # how far the deck rises in the middle


def stone_bridge(seed):
    """A stone bridge 8 m long and 2.4 m wide over an arch 6 m across: the deck humps 0.8 m in the middle, on spandrel
    walls over the arch, with low parapets and abutments reaching down into the banks."""
    rng = random.Random(seed)
    parts = []
    width = 2.4
    spring = -2.6
    # The arch: a ring of wedge stones on each face and a barrel under it, through the bridge's width.
    for z in (-width / 2 + 0.3, width / 2 - 0.3):
        parts += arch_ring(0, spring, ARCH_SPAN, 0.45, 0.6, rng, count=15, z=z)
    for i in range(14):
        a0, a1 = math.pi * i / 14, math.pi * (i + 1) / 14
        p0 = (ARCH_SPAN * math.cos(a0), spring + ARCH_SPAN * math.sin(a0))
        p1 = (ARCH_SPAN * math.cos(a1), spring + ARCH_SPAN * math.sin(a1))
        mid = ((p0[0] + p1[0]) / 2, (p0[1] + p1[1]) / 2)
        parts.append(part("stonebox", (mid[0], mid[1] + 0.12, 0), (0, 0, math.degrees((a0 + a1) / 2) - 90),
                          (math.hypot(p1[0] - p0[0], p1[1] - p0[1]) * 1.04, 0.25, width - 1.2), tint=shade(rng)))

    def deck_y(x):
        return HUMP * math.cos(math.pi * x / 8) ** 2 if abs(x) <= 4 else 0.0

    # Spandrels: the walling between the arch and the deck, in strips that follow both curves.
    for z0, z1 in ((-width / 2, -width / 2 + 0.6), (width / 2 - 0.6, width / 2)):
        for i in range(16):
            a, b = -4 + 0.5 * i, -4 + 0.5 * (i + 1)
            middle = (a + b) / 2
            under = spring + math.sqrt(max(0.0, (ARCH_SPAN + 0.45) ** 2 - middle * middle)) if abs(middle) < ARCH_SPAN + 0.45 else BOTTOM
            top = deck_y(middle) - 0.25
            if top - under > 0.05:
                parts.append(block(a, b, under, top, z0, z1, tint=shade(rng)))
    # The deck: slabs following the hump, and parapets with a cope.
    for i in range(16):
        a, b = -4 + 0.5 * i, -4 + 0.5 * (i + 1)
        y0, y1 = deck_y(a), deck_y(b)
        angle = math.degrees(math.atan2(y1 - y0, b - a))
        middle = ((a + b) / 2, (y0 + y1) / 2)
        parts.append(part("stonebox", (middle[0], middle[1] - 0.12, 0), (0, 0, angle), (0.52, 0.24, width - 1.0), tint=shade(rng, 0.93, 1.0)))
        for z in (-width / 2 + 0.2, width / 2 - 0.2):
            parts.append(part("stonebox", (middle[0], middle[1] + 0.3, z), (0, 0, angle), (0.52, 0.6, 0.4), tint=shade(rng)))
            parts.append(part("stonebox", (middle[0], middle[1] + 0.64, z), (0, 0, angle), (0.53, 0.1, 0.48), tint=shade(rng, 0.93, 1.0)))
    colliders = []
    for i in range(8):
        a, b = -4 + i, -3 + i
        colliders.append(ramp((a, deck_y(a), 0), (b, deck_y(b), 0), width - 0.8, 0.3))
        for z in (-width / 2 + 0.2, width / 2 - 0.2):
            colliders.append(ramp((a, deck_y(a) + 0.7, z), (b, deck_y(b) + 0.7, z), 0.4, 0.7))
    colliders += [span(-4, -ARCH_SPAN, BOTTOM, -0.25, -width / 2, width / 2), span(ARCH_SPAN, 4, BOTTOM, -0.25, -width / 2, width / 2)]
    snaps = [(x, 0, z) for x in (-4, 4) for z in (-width / 2, 0, width / 2)]
    return parts, colliders, snaps


def suspension_bridge(seed):
    """A rope bridge 8 m long: two posts at each end, plank treads on ropes that sag 0.5 m in the middle, and rope
    handrails."""
    rng = random.Random(seed)
    parts = []
    sag = 0.5
    width = 1.2

    def y_at(x):
        return -sag * (1 - (x / 4) ** 2)

    for x in (-4, 4):
        for z in (-width / 2 - 0.1, width / 2 + 0.1):
            parts.append(pole((x, -1.0, z), (x, 1.2, z), 0.1, tint=GREY_POLE))
    steps = 16
    for i in range(steps):
        a, b = -4 + 8 * i / steps, -4 + 8 * (i + 1) / steps
        middle = (a + b) / 2
        parts.append(plank((middle, y_at(middle) - 0.03, -width / 2), (middle, y_at(middle) - 0.03, width / 2), 0.36, 0.05,
                           tint=[c * rng.choice((0.85, 1.0)) for c in DECK]))
        for z in (-width / 2 + 0.05, width / 2 - 0.05):
            parts.append(pole((a, y_at(a) - 0.07, z), (b, y_at(b) - 0.07, z), 0.025, tint=ROPE_TINT))
        for z in (-width / 2 - 0.1, width / 2 + 0.1):
            parts.append(pole((a, y_at(a) + 1.0, z), (b, y_at(b) + 1.0, z), 0.025, tint=ROPE_TINT))
            if i % 2 == 0:
                parts.append(pole((a, y_at(a) - 0.05, z), (a, y_at(a) + 1.0, z), 0.012, tint=ROPE_TINT, detail=True))
    colliders = []
    for i in range(8):
        a, b = -4 + i, -3 + i
        colliders.append(ramp((a, y_at(a), 0), (b, y_at(b), 0), width, 0.12))
        for z in (-width / 2 - 0.1, width / 2 + 0.1):
            colliders.append(ramp((a, y_at(a) + 0.55, z), (b, y_at(b) + 0.55, z), 0.08, 1.0))
    snaps = [(x, 0, z) for x in (-4, 4) for z in (-width / 2, 0, width / 2)]
    return parts, colliders, snaps


def water_pieces():
    side = views(("side", 160, 15), ("end", 90, 10), ("angle", -140, 30))
    pieces = [
        {"name": "brygge", "base": "wood_floor", **dict(zip(("parts", "colliders", "snapPoints"), _snaps(jetty(4, "brygge")))), "views": side},
        {"name": "brygge_2m", "base": "wood_floor", **dict(zip(("parts", "colliders", "snapPoints"), _snaps(jetty(2, "brygge_2m")))), "views": side},
        {"name": "bryggehode", "base": "wood_floor", **dict(zip(("parts", "colliders", "snapPoints"), _snaps(jetty_head("bryggehode")))), "views": side},
        {"name": "kai", "base": "stone_wall_2x1", **dict(zip(("parts", "colliders", "snapPoints"), _snaps(quay("kai")))), "views": side},
        {"name": "kai_hjorne", "base": "stone_wall_2x1", **dict(zip(("parts", "colliders", "snapPoints"), _snaps(quay("kai_hjorne", corner=True)))), "views": side},
        {"name": "kaitrapp", "base": "stone_wall_2x1", **dict(zip(("parts", "colliders", "snapPoints"), _snaps(quay_steps("kaitrapp")))), "views": side},
        {"name": "tommerbru", "base": "wood_floor", **dict(zip(("parts", "colliders", "snapPoints"), _snaps(log_bridge(4, "tommerbru")))), "views": side},
        {"name": "tommerbru_8m", "base": "wood_floor", **dict(zip(("parts", "colliders", "snapPoints"), _snaps(log_bridge(8, "tommerbru_8m")))), "views": side},
        {"name": "steinbru", "base": "stone_wall_2x1", **dict(zip(("parts", "colliders", "snapPoints"), _snaps(stone_bridge("steinbru")))), "views": side},
        {"name": "hengebru", "base": "wood_floor", **dict(zip(("parts", "colliders", "snapPoints"), _snaps(suspension_bridge("hengebru")))), "views": side},
    ]
    pieces.append(overview())
    return pieces


def _snaps(result):
    parts, colliders, snaps = result
    return parts, colliders, sum((r3(p) for p in snaps), [])


def overview():
    """A harbour for the documentation: a stone quay with steps, a jetty out from it, and the three bridges."""
    def at(name, x, z, yaw=0, y=0.0):
        data = {"piece": name, "position": r3((x, y, z))}
        if yaw:
            data["rotation"] = [0, yaw, 0]
        return data

    parts = [at("kai", x, 0) for x in (-6, -2, 2)] + [at("kai_hjorne", 4, 0)]
    parts += [at("kaitrapp", -6, 0.0, 0)]
    parts += [at("brygge", 1, 3, 90), at("brygge", 1, 7, 90), at("bryggehode", 1, 10, -90)]
    parts += [at("steinbru", -12, 4, 90), at("tommerbru_8m", 10, 6), at("hengebru", 10, 12)]
    return {"name": "havn", "parts": parts, "views": views(("harbour", 160, 25), ("angle", -140, 30), ("top", 160, 65))}
