"""Generates defenses.json: the palisade defence pieces, put together from vanilla Valheim meshes.

Run: python build_defenses.py  (then render_preview.ps1 for images; the mod embeds the same file)

Everything is built in metres, y up, and every wall faces its outside towards +z. The random variation (stake height,
thickness, lean, rocks) is seeded, so the same layout comes out every time and every player sees the same.
Besides the parts, each piece lists what the game needs: the vanilla piece it is cloned from, box colliders,
snap points, ladders and moving groups (gate leaves). Parts marked detail are dropped at a distance.
Vanilla mesh facts used below:
  stake          base at y 0, 3.4 m tall, 0.44 m thick
  stake_support  log along z, 2.1 m long, radius 0.11
  wood_beam      along x, 2 m long, 0.4 m square; wood_beam_1 is 1 m long
  wood_floor     2 x 2 m, top at y 0.08; wood_floor_1x1 is 1 x 1 m
  wood_stair     2 m wide, rises 1 m over 2 m, high end at -z
  wood_roof_ocorner_45  2 x 2 m footprint around its origin, apex 2 m up over (-1, -1)
  piece_walltorch  sticks out along +x
"""

import json
import math
import pathlib
import random

OUT = pathlib.Path(__file__).with_name("defenses.json")
WALK = 2.0  # height of every walkway top, so they all join
SHIELD_TINTS = [None, [0.95, 0.55, 0.5], [0.55, 0.65, 0.95]]


def r3(values):
    return [round(v, 3) for v in values]


def part(mesh, position, rotation=(0, 0, 0), scale=(1, 1, 1), **extra):
    data = {"mesh": mesh, "position": r3(position)}
    if any(rotation):
        data["rotation"] = r3(rotation)
    if tuple(scale) != (1, 1, 1):
        data["scale"] = r3(scale)
    data.update({key: value for key, value in extra.items() if value is not None and value is not False})
    return data


def piece(name, position=(0, 0, 0), rotation=(0, 0, 0)):
    data = {"piece": name, "position": r3(position)}
    if any(rotation):
        data["rotation"] = r3(rotation)
    return data


def box(center, size, rotation=(0, 0, 0), group=None):
    data = {"center": r3(center), "size": r3(size)}
    if any(rotation):
        data["rotation"] = r3(rotation)
    if group:
        data["group"] = group
    return data


def ramp(low, high, width, thickness=0.2):
    """Box collider whose top face runs from low to high (both points on the walking surface)."""
    d = [b - a for a, b in zip(low, high)]
    length = math.sqrt(sum(v * v for v in d))
    yaw = math.degrees(math.atan2(d[0], d[2]))
    pitch = -math.degrees(math.asin(d[1] / length))
    horizontal = math.hypot(d[0], d[2])
    normal = (-d[0] / horizontal * d[1] / length, horizontal / length, -d[2] / horizontal * d[1] / length)
    center = [(a + b) / 2 - n * thickness / 2 for a, b, n in zip(low, high, normal)]
    return box(center, (width, thickness, length), (pitch, yaw, 0))


def log(p0, p1, radius, **extra):
    """A round log from p0 to p1."""
    d = [b - a for a, b in zip(p0, p1)]
    length = math.sqrt(sum(v * v for v in d))
    yaw = math.degrees(math.atan2(d[0], d[2]))
    pitch = -math.degrees(math.asin(d[1] / length))
    middle = [(a + b) / 2 for a, b in zip(p0, p1)]
    return part("stake_support", middle, (pitch, yaw, 0), (radius / 0.11, radius / 0.11, length / 2.1), **extra)


def beam(p0, p1, size, height=None, mesh="wood_beam", **extra):
    """A square beam between two points at the same height."""
    dx, dz = p1[0] - p0[0], p1[2] - p0[2]
    length = math.hypot(dx, dz)
    yaw = math.degrees(math.atan2(-dz, dx))
    middle = [(a + b) / 2 for a, b in zip(p0, p1)]
    return part(mesh, middle, (0, yaw, 0), (length / (2 if mesh == "wood_beam" else 1), (height or size) / 0.4, size / 0.4), **extra)


def post(x, z, y0, y1, radius):
    return part("wood_pole_log", (x, (y0 + y1) / 2, z), scale=(radius / 0.26, (y1 - y0) / 2.22, radius / 0.26))


def stake(x, y, z, height, thickness, rng, lean=2.0, **extra):
    rotation = (rng.uniform(-lean, lean), rng.uniform(0, 360), rng.uniform(-lean, lean))
    return part("stake", (x, y, z), rotation, (thickness / 0.44, height / 3.4, thickness / 0.44), **extra)


def stake_row(x0, x1, z, y, height, thickness, spacing, rng, height_var=0.1, lean=2.0, **extra):
    """Stakes from x0 to x1: uneven heights, every other one a little thinner."""
    count = max(1, round((x1 - x0) / spacing) + 1)
    step = (x1 - x0) / max(1, count - 1)
    parts = []
    for i in range(count):
        thick = thickness * (0.82 if i % 2 else 1.0) * rng.uniform(0.94, 1.06)
        tall = height * rng.uniform(1 - height_var, 1 + height_var * 0.5)
        parts.append(stake(x0 + i * step + rng.uniform(-0.02, 0.02), y, z + rng.uniform(-0.03, 0.03), tall, thick, rng, lean, **extra))
    return parts


def floor(x0, x1, z0, z1, top, mesh="wood_floor"):
    size = 2.0 if mesh == "wood_floor" else 1.0
    return part(mesh, ((x0 + x1) / 2, top - 0.08, (z0 + z1) / 2), scale=((x1 - x0) / size, 1, (z1 - z0) / size))


def floor_box(x0, x1, z0, z1, top):
    return box(((x0 + x1) / 2, top - 0.08, (z0 + z1) / 2), (x1 - x0, 0.16, z1 - z0))


def rotate(data, yaw):
    """Turns a part or box around the piece's y axis. Adding to the Euler yaw is exact for Unity's order (z, x, y)."""
    if yaw == 0:
        return data
    key = "position" if "position" in data else "center"
    x, y, z = data[key]
    a = math.radians(yaw)
    rotated = dict(data)
    rotated[key] = r3((x * math.cos(a) + z * math.sin(a), y, -x * math.sin(a) + z * math.cos(a)))
    rotation = list(data.get("rotation", [0, 0, 0]))
    rotation[1] += yaw
    rotated["rotation"] = r3(rotation)
    return rotated


def palisade_segment(seed):
    """2 m of palisade, x -1..1: stakes, back supports, lashing bands, base spikes, rocks and an earth bank."""
    rng = random.Random(seed)
    parts = stake_row(-0.8, 0.8, 0, -0.1, 3.45, 0.42, 0.4, rng)
    for y in (1.0, 2.3):
        parts.append(log((-1.02, y, -0.3), (1.02, y, -0.3), 0.12))
    for y in (1.25, 2.45):
        sag = rng.uniform(-0.04, 0.04)
        parts.append(log((-1.02, y + sag, 0.25), (1.02, y - sag, 0.25), 0.065, detail=True))
    for x in (-0.75, -0.25, 0.25, 0.75):
        rotation = (rng.uniform(40, 52), rng.uniform(-15, 15), 0)
        length = rng.uniform(1.6, 1.9)
        parts.append(part("stake", (x + rng.uniform(-0.08, 0.08), -0.2, 0.35), rotation, (0.45, length / 3.4, 0.45), detail=True))
    for _ in range(3):
        size = rng.uniform(0.35, 0.6)
        parts.append(part("rock", (rng.uniform(-0.9, 0.9), -0.08, rng.uniform(0.55, 0.95)), (0, rng.uniform(0, 360), 0),
                          (size, size * 0.8, size), detail=True))
    parts.append(part("rock", (rng.uniform(-0.1, 0.1), -0.12, 0.75), (0, rng.uniform(-10, 10), 0), (1.9, 0.45, 1.4), tint=[0.55, 0.45, 0.35]))
    return parts


def walkway(x0, x1):
    """Walkway behind a straight wall from x0 to x1: planks 1.4 m deep on beams, posts and knee braces."""
    parts = [floor(x0, x1, -1.8, -0.4, WALK)]
    for z in (-0.5, -1.7):
        parts.append(beam((x0, WALK - 0.26, z), (x1, WALK - 0.26, z), 0.28))
    posts = [x0 + 0.1, (x0 + x1) / 2, x1 - 0.1] if x1 - x0 > 3 else [x0 + 0.1, x1 - 0.1]
    for x in posts:
        parts.append(post(x, -1.7, -0.3, WALK - 0.1, 0.17))
    for a, b in zip(posts, posts[1:]):
        parts.append(log((a, 0.5, -1.7), (b, 0.5, -1.7), 0.16))
        parts.append(log((a, WALK - 0.9, -1.7), (a + 0.6, WALK - 0.3, -1.7), 0.09, detail=True))
        parts.append(log((b, WALK - 0.9, -1.7), (b - 0.6, WALK - 0.3, -1.7), 0.09, detail=True))
    return parts


def corner_post(rng):
    return stake(0, -0.1, 0, 3.6, 0.6, rng, lean=0.5)


def shields(positions, rng):
    return [part("shield_wood", p, (90, 0, rng.uniform(-8, 8)), tint=SHIELD_TINTS[i % len(SHIELD_TINTS)], detail=True)
            for i, p in enumerate(positions)]


def torch(position):
    """A wall torch sticking out towards +z."""
    return part("piece_walltorch", position, (0, -90, 0), detail=True)


def roof(cx, cz, base, half, rng):
    """Pyramid roof of four vanilla outer-corner pieces, with hip logs over the seams and a spire."""
    s = half / 2
    parts = []
    for yaw, sx, sz in ((0, 1, 1), (90, 1, -1), (180, -1, -1), (270, -1, 1)):
        parts.append(part("wood_roof_ocorner_45", (cx + sx * s, base, cz + sz * s), (0, yaw, 0), (s, s, s)))
    apex = (cx, base + half + 0.05, cz)
    for sx, sz in ((1, 1), (1, -1), (-1, -1), (-1, 1)):
        parts.append(log((cx + sx * half, base + 0.05, cz + sz * half), apex, 0.09))
    parts.append(stake(cx, base + half - 0.25, cz, 1.3, 0.2, rng, lean=0))
    return parts


def ladder_parts(x, z, y0, y1):
    parts = [log((x - 0.25, y0, z), (x - 0.25, y1, z), 0.06), log((x + 0.25, y0, z), (x + 0.25, y1, z), 0.06)]
    y = y0 + 0.4
    while y < y1 - 0.1:
        parts.append(beam((x - 0.25, y, z), (x + 0.25, y, z), 0.07, mesh="wood_beam_1", detail=True))
        y += 0.45
    return parts


def tower(n, levels, seed):
    """Square tower of n x n metres with floors at the given heights, an overhanging top with a parapet and a roof."""
    rng = random.Random(seed)
    half = n / 2
    corner = half - 0.1
    radius = {2: 0.2, 3: 0.26, 4: 0.31}[n]
    top = levels[-1]
    overhang = 0.45
    edge = half + overhang
    roof_base = top + 2.5
    parts, colliders = [], []
    for sx in (-1, 1):
        for sz in (-1, 1):
            parts.append(post(sx * corner, sz * corner, -0.1, roof_base, radius))
            colliders.append(box((sx * corner, roof_base / 2, sz * corner), (radius * 2, roof_base, radius * 2)))
    tiles = [i - half + 0.5 for i in range(n)]
    hatch = (tiles[-1], tiles[0])
    for level in levels:
        for z in (-corner, corner):
            parts.append(beam((-corner, level - 0.25, z), (corner, level - 0.25, z), 0.3))
        for x in (-corner, corner):
            parts.append(beam((x, level - 0.25, -corner), (x, level - 0.25, corner), 0.3))
        for x in tiles:
            for z in tiles:
                if (x, z) != hatch:
                    parts.append(floor(x - 0.5, x + 0.5, z - 0.5, z + 0.5, level, "wood_floor_1x1"))
        # Everything but the hatch in the back right corner.
        colliders.append(floor_box(-half, half, -half + 1, half, level))
        colliders.append(floor_box(-half, half - 1, -half, -half + 1, level))
    # The top floor reaches past the posts on every side, carried by brackets.
    for x0, x1, z0, z1 in ((-edge, edge, half, edge), (-edge, edge, -edge, -half), (half, edge, -half, half), (-edge, -half, -half, half)):
        parts.append(floor(x0, x1, z0, z1, top))
        colliders.append(floor_box(x0, x1, z0, z1, top))
    for sx in (-1, 1):
        for sz in (-1, 1):
            parts.append(log((sx * corner, top - 1.0, sz * corner), (sx * (edge - 0.1), top - 0.2, sz * corner), 0.08))
            parts.append(log((sx * corner, top - 1.0, sz * corner), (sx * corner, top - 0.2, sz * (edge - 0.1)), 0.08))
    e = edge - 0.12
    for yaw in (0, 90, 180, 270):
        rows = stake_row(-e, e, e, top - 0.05, 1.35, 0.3, 0.33, rng, height_var=0.08, lean=1.5)
        parts += [rotate(p, yaw) for p in rows]
        count = max(1, n - 1)
        spots = [((i + 0.5) / count * 2 - 1) * (e - 0.4) for i in range(count)]
        parts += [rotate(p, yaw) for p in shields([(x, top + 0.75, e + 0.2) for x in spots], rng)]
        colliders.append(rotate(box((0, top + 0.65, e), (2 * e + 0.3, 1.3, 0.3)), yaw))
    if len(levels) > 2:
        middle = levels[1]
        m = half + 0.02
        for yaw in (0, 90, 180, 270):
            parts += [rotate(p, yaw) for p in stake_row(-m, m, m, middle - 0.05, 1.1, 0.28, 0.33, rng, height_var=0.08, lean=1.5)]
            colliders.append(rotate(box((0, middle + 0.5, m), (2 * m + 0.3, 1.1, 0.3)), yaw))
    for yaw in (0, 90):
        brace = [log((-corner, 0.2, half + 0.1), (corner, levels[0] - 0.45, half + 0.1), 0.1),
                 log((corner, 0.2, half + 0.1), (-corner, levels[0] - 0.45, half + 0.1), 0.1)]
        parts += [rotate(p, yaw) for p in brace]
    parts += [beam((corner, roof_base - 0.1, z), (-corner, roof_base - 0.1, z), 0.26) for z in (-corner, corner)]
    parts += [beam((x, roof_base - 0.1, -corner), (x, roof_base - 0.1, corner), 0.26) for x in (-corner, corner)]
    parts += roof(0, 0, roof_base, edge + 0.3, rng)
    # A thin lid under the roof shelters the top floor from rain.
    colliders.append(box((0, roof_base + 0.05, 0), (2 * (edge + 0.3), 0.1, 2 * (edge + 0.3))))
    ladder_z = hatch[1] + 0.3
    parts += ladder_parts(hatch[0], ladder_z, 0, top + 0.05)
    parts.append(torch((corner, levels[0] + 1.0, half + 0.28)))
    # Each stop stands beside the hatch, towards the middle of the tower.
    stops = [r3((hatch[0] - 0.9, level + 0.05, hatch[1] + 0.9)) for level in [0.0] + levels]
    ladders = [{"center": r3((hatch[0], (top + 0.05) / 2, ladder_z)), "size": r3((0.7, top + 0.05, 0.15)), "stops": sum(stops, [])}]
    snaps = []
    for s in (-1, 1):
        for t in (-half + 0.2, 0, half - 0.2):
            snaps += [r3((s * half, 0, t)), r3((t, 0, s * half))]
    return parts, colliders, ladders, snaps


def wall():
    parts = [piece("pal_a", (-1, 0, 0)), piece("pal_b", (1, 0, 0))] + walkway(-2, 2)
    colliders = [box((0, 1.6, 0), (4.0, 3.3, 0.55)), floor_box(-2, 2, -1.8, -0.4, WALK)]
    snaps = [(-2, 0, 0), (2, 0, 0), (-2, WALK, 0), (2, WALK, 0), (-1, WALK, -1.8), (0, WALK, -1.8), (1, WALK, -1.8)]
    return parts, colliders, snaps


def corner90():
    rng = random.Random("skansehjorne")
    parts = [piece("pal_c", (1, 0, 0)), piece("pal_d", (0, 0, -1), (0, -90, 0)), corner_post(rng)]
    parts.append(floor(0.3, 2.0, -2.0, -0.3, WALK))
    for z in (-0.5, -1.9):
        parts.append(beam((0.3, WALK - 0.26, z), (2.0, WALK - 0.26, z), 0.28))
    parts.append(post(1.9, -1.9, -0.3, WALK - 0.1, 0.17))
    parts.append(log((1.9, WALK - 0.9, -1.9), (1.3, WALK - 0.3, -1.9), 0.09, detail=True))
    parts.append(log((1.9, WALK - 0.9, -1.9), (1.9, WALK - 0.3, -1.3), 0.09, detail=True))
    colliders = [box((1, 1.6, 0), (2, 3.3, 0.55)), box((0, 1.6, -1), (0.55, 3.3, 2)), floor_box(0.3, 2.0, -2.0, -0.3, WALK)]
    snaps = [(0, 0, 0), (2, 0, 0), (0, 0, -2), (2, WALK, 0), (0, WALK, -2)]
    return parts, colliders, snaps


def corner45():
    rng = random.Random("skansehjorne45")
    parts = [piece("pal_c", (1, 0, 0)), piece("pal_d", (-0.707, 0, -0.707), (0, -45, 0)), corner_post(rng)]
    parts.append(floor(0, 2, -1.8, -0.4, WALK))
    parts.append(part("wood_floor", (0.071, WALK - 0.08, -1.485), (0, -45, 0), (1, 1, 0.7)))
    parts.append(part("wood_floor_1x1", (0.38, WALK - 0.075, -0.92), (0, -22.5, 0), (1.3, 1, 1.3)))
    parts.append(beam((0, WALK - 0.26, -1.7), (2, WALK - 0.26, -1.7), 0.28))
    parts.append(part("wood_beam", (0.49, WALK - 0.26, -1.91), (0, -45, 0), (1, 0.7, 0.7)))
    for x, z in ((1.9, -1.7), (0.69, -1.66), (-0.21, -2.62)):
        parts.append(post(x, z, -0.3, WALK - 0.1, 0.17))
    colliders = [box((1, 1.6, 0), (2, 3.3, 0.55)), box((-0.707, 1.6, -0.707), (2, 3.3, 0.55), (0, -45, 0)),
                 floor_box(0, 2, -1.8, -0.4, WALK),
                 box((0.071, WALK - 0.08, -1.485), (2, 0.16, 1.4), (0, -45, 0)),
                 box((0.38, WALK - 0.08, -0.92), (1.3, 0.16, 1.3), (0, -22.5, 0))]
    snaps = [(0, 0, 0), (2, 0, 0), (-1.414, 0, -1.414), (2, WALK, 0), (-1.414, WALK, -1.414)]
    return parts, colliders, snaps


def gate_leaf(x0, group, rng):
    """One 2 m gate leaf of stakes, with iron bands and bolts outside and cross planks and a brace inside."""
    parts = stake_row(x0 + 0.2, x0 + 1.8, 0.05, 0.05, 3.0, 0.4, 0.4, rng, height_var=0.05, lean=0.8, group=group)
    for y in (0.7, 2.3):
        parts.append(beam((x0 + 0.05, y, 0.27), (x0 + 1.95, y, 0.27), 0.05, height=0.13, texture="metalwall", group=group))
        for i in range(5):
            parts.append(part("wood_pole", (x0 + 0.2 + i * 0.4, y, 0.31), (90, 0, 0), (0.22, 0.05, 0.22), texture="metalwall",
                              group=group, detail=True))
        parts.append(beam((x0 + 0.05, y, -0.2), (x0 + 1.95, y, -0.2), 0.16, height=0.2, group=group))
    parts.append(log((x0 + 0.2, 0.75, -0.28), (x0 + 1.8, 2.25, -0.28), 0.1, group=group))
    return parts


def flank_tower(side, rng):
    """One of the gatehouse towers, x 2.3..4.3 on the given side: a stair from the walkway up to the gate walk."""
    s = side
    lo, hi = sorted((s * 2.3, s * 4.3))
    inner_lo, inner_hi = sorted((s * 2.3, s * 3.3))
    parts = stake_row(s * 2.45, s * 4.15, 0.05, -0.05, 4.9, 0.42, 0.34, rng, height_var=0.06, lean=1.0)
    for x in (2.4, 4.2):
        parts.append(post(s * x, -1.8, -0.1, 6.3, 0.2))
        parts.append(post(s * x, -0.3, 3.5, 6.3, 0.18))
    parts.append(floor(lo, hi, -1.8, 0.2, WALK))
    parts.append(beam((lo, WALK - 0.26, -1.8), (hi, WALK - 0.26, -1.8), 0.28))
    parts.append(floor(inner_lo, inner_hi, -1.8, 0.2, 3.5))
    parts.append(beam((inner_lo, 3.24, -1.8), (inner_hi, 3.24, -1.8), 0.28))
    parts.append(part("wood_stair", (s * 3.8, WALK, -0.8), (0, 180, 0), (0.5, 1.5, 1)))
    parts.append(log((s * 2.4, 0.5, -1.8), (s * 4.2, 0.5, -1.8), 0.15))
    parts += shields([(s * x, 4.1, 0.35) for x in (2.65, 3.3, 3.95)], rng)
    for x in (2.4, 4.2):
        parts.append(beam((s * x, 6.2, -1.8), (s * x, 6.2, 0.0), 0.22))
    parts += roof(s * 3.3, -0.9, 6.25, 1.35, rng)
    colliders = [box((s * 3.3, 2.45, 0.05), (2.1, 4.9, 0.5)),
                 floor_box(lo, hi, -1.8, 0.2, WALK),
                 floor_box(inner_lo, inner_hi, -1.8, 0.2, 3.5),
                 ramp((s * 3.8, WALK, -1.8), (s * 3.8, 3.5, 0.2), 1.0),
                 box((s * 3.3, 6.3, -0.9), (2.7, 0.1, 2.7))]
    return parts, colliders


def gatehouse():
    rng = random.Random("skanseport")
    parts = [post(-2.2, 0, -0.2, 4.6, 0.23), post(2.2, 0, -0.2, 4.6, 0.23)]
    parts.append(log((-2.35, 3.3, 0), (2.35, 3.3, 0), 0.22))
    parts += gate_leaf(-2.0, "leaf_left", rng) + gate_leaf(0.0, "leaf_right", rng)
    parts += stake_row(-2.05, 2.05, 0, 3.35, 1.45, 0.34, 0.34, rng, height_var=0.18, lean=3.0)
    parts.append(part("trophy_deer", (0, 3.2, 0.3), scale=(1.1, 1.1, 1.1), detail=True))
    parts += [part("trophy_skull", (x, 4.6, 0), scale=(1.6, 1.6, 1.6), detail=True) for x in (-2.2, 2.2)]
    parts += [torch((x, 2.2, 0.35)) for x in (-2.2, 2.2)]
    parts += [floor(-2.2, 2.2, -1.8, -0.4, 3.5), beam((-2.2, 3.24, -1.7), (2.2, 3.24, -1.7), 0.28)]
    parts += [post(x, -1.7, -0.1, 3.4, 0.17) for x in (-2.2, 2.2)]
    colliders = [box((1.0, 1.5, 0.05), (2.0, 3.0, 0.5), group="leaf_right"),
                 box((-1.0, 1.5, 0.05), (2.0, 3.0, 0.5), group="leaf_left"),
                 box((-2.2, 2.2, 0), (0.5, 4.8, 0.5)), box((2.2, 2.2, 0), (0.5, 4.8, 0.5)),
                 box((0, 4.05, 0), (4.6, 1.9, 0.5)),
                 floor_box(-2.2, 2.2, -1.8, -0.4, 3.5)]
    for side in (1, -1):
        flank_parts, flank_colliders = flank_tower(side, rng)
        parts += flank_parts
        colliders += flank_colliders
    snaps = [(-4.3, 0, 0), (4.3, 0, 0), (-4.3, WALK, 0), (4.3, WALK, 0)]
    groups = [{"name": "leaf_right", "pivot": [2.0, 0, 0.05]}, {"name": "leaf_left", "pivot": [-2.0, 0, 0.05], "mirror": True}]
    return parts, colliders, snaps, groups


def stairs():
    parts = [part("wood_stair", (0, 1, -1), (0, 180, 0)), part("wood_stair", (0, 0, -3), (0, 180, 0))]
    parts += [post(x, -0.2, -0.3, WALK - 0.1, 0.17) for x in (-0.9, 0.9)]
    parts += [post(x, -2.0, -0.3, 0.95, 0.17) for x in (-0.9, 0.9)]
    for x in (-1.0, 1.0):
        parts.append(post(x, -3.9, 0, 1.0, 0.1))
        parts.append(log((x, 1.0, -3.9), (x, WALK + 1.0, -0.1), 0.07))
    colliders = [ramp((0, 0, -4), (0, 1, -2), 2.0), ramp((0, 1, -2), (0, WALK, 0), 2.0)]
    snaps = [(0, WALK, 0), (-1, 0, -4), (1, 0, -4)]
    return parts, colliders, snaps


def cheval_de_frise():
    rng = random.Random("spansk_rytter")
    parts = [log((-2.2, 0.7, 0), (2.2, 0.7, 0), 0.16)]
    for i in range(4):
        x = -1.6 + i * 1.066 + rng.uniform(-0.05, 0.05)
        for tilt, z in ((45, -0.66), (-45, 0.66)):
            rotation = (tilt + rng.uniform(-4, 4), rng.uniform(-6, 6), 0)
            parts.append(part("stake", (x, 0.04, z), rotation, (0.5, 0.55 * rng.uniform(0.92, 1.05), 0.5)))
    colliders = [box((0, 0.7, 0), (4.4, 1.4, 1.3))]
    snaps = [(-2.2, 0, 0), (2.2, 0, 0)]
    return parts, colliders, snaps


def views(*items):
    return [{"label": label, "yaw": yaw, "pitch": pitch} for label, yaw, pitch in items]


BRIDGE_LENGTH = 9.0
BRIDGE_TOP = 0.3


def drawbridge():
    """A drawbridge 4 m wide, hinged at z = 0 and reaching BRIDGE_LENGTH out along +z over a moat.

    The deck lies in the group "deck", which the mod turns up round the hinge when the bridge is closed. Two posts with a
    crossbar stand at the hinge.
    """
    rng = random.Random("vindebro")
    top, length = BRIDGE_TOP, BRIDGE_LENGTH
    parts = []
    z = 0.0
    while z < length - 0.01:
        z1 = min(length, z + 2.0)
        parts.append(dict(floor(-2.0, 2.0, z, z1, top), group="deck"))
        z = z1
    for x in (-1.75, 1.75):
        parts.append(beam((x, top - 0.28, 0.1), (x, top - 0.28, length - 0.1), 0.3, group="deck"))
    for z in (0.4, length / 2, length - 0.4):
        parts.append(beam((-2.0, top - 0.5, z), (2.0, top - 0.5, z), 0.22, group="deck"))
    for z in (1.5, length - 1.5):
        parts.append(beam((-2.0, top + 0.02, z), (2.0, top + 0.02, z), 0.05, height=0.12, texture="metalwall", group="deck", detail=True))
    for x in (-1.9, 1.9):
        parts.append(part("wood_pole", (x, top + 0.05, length - 0.35), (90, 0, 0), (0.5, 0.08, 0.5), texture="metalwall", group="deck", detail=True))
    for x in (-2.45, 2.45):
        parts.append(post(x, -0.15, -0.4, 4.4 + rng.uniform(-0.05, 0.05), 0.22))
    parts.append(log((-2.65, 4.2, -0.15), (2.65, 4.2, -0.15), 0.18))
    colliders = [box((0, top - 0.25, length / 2), (4.0, 0.5, length), group="deck"),
                 box((-2.45, 2.0, -0.15), (0.45, 4.8, 0.45)), box((2.45, 2.0, -0.15), (0.45, 4.8, 0.45))]
    snaps = [(-2.0, 0, 0), (2.0, 0, 0)]
    groups = [{"name": "deck", "pivot": [0, top, 0]}]
    return parts, colliders, snaps, groups


# Map scrolls grow with the Exploration skill: one plus one per ten levels. Each lies in its own group, scroll_1..scroll_11.
SCROLLS = 11
SCROLL_SCALE = 0.055  # the scroll mesh is 8 long for a height of 1, so about 0.44 m long and 6 cm thick


def scroll(index, position, yaw=0.0, pitch=0.0, roll=0.0):
    """A map scroll lying with its centre line at position; the mesh's origin is at its bottom."""
    x, y, z = position
    return part("mapscroll", (x, y - SCROLL_SCALE / 2, z), (pitch, yaw, roll), (SCROLL_SCALE,) * 3, group=f"scroll_{index}", detail=True)


def chart_table():
    """The navigator's table: a small plank table with a sea chart, a sextant and the map scrolls on and under it."""
    top = 0.85
    parts = [part("wood_floor_1x1", (0, top - 0.08, 0), scale=(0.9, 1, 0.6)),
             part("wood_floor_1x1", (0, 0.22, 0), scale=(0.8, 0.6, 0.5))]
    for x in (-0.38, 0.38):
        for z in (-0.24, 0.24):
            parts.append(part("wood_pole", (x, (top - 0.1) / 2, z), scale=(0.18, (top - 0.1), 0.18)))
    parts.append(part("seachart", (-0.05, top + 0.005, -0.2), (90, 0, 0), (0.4, 0.4, 0.4)))
    parts.append(part("sextant", (0.25, top + 0.01, 0.1), (0, 30, 0), (0.06, 0.06, 0.06)))
    slots = [((-0.3, top + 0.028, 0.18), 5), ((0.3, top + 0.028, -0.18), -10),
             ((-0.2, 0.33, -0.05), 0), ((-0.2, 0.39, -0.02), 4), ((0.05, 0.33, 0.08), -3), ((0.05, 0.39, 0.05), 8), ((-0.1, 0.45, 0.02), 2),
             ((-0.3, 0.03, 0.0), 90), ((-0.12, 0.03, 0.02), 92), ((0.15, 0.03, -0.02), 88), ((-0.21, 0.09, 0.01), 90)]
    for i, (position, yaw) in enumerate(slots, start=1):
        parts.append(scroll(i, position, yaw))
    colliders = [box((0, top / 2, 0), (0.9, top, 0.6))]
    groups = [{"name": f"scroll_{i}", "pivot": [0, 0, 0]} for i in range(1, SCROLLS + 1)]
    return parts, colliders, groups


def carto_desk():
    """The cartographer's desk: the writing desk with a sea chart, a sextant and map scrolls on top.

    The desk top is a writing slope, 0.80 m high at the front edge line z = 0 and rising 0.28 m per metre towards -z.
    """
    tilt = 15.6

    def top(z):
        return 0.804 - 0.28 * z

    parts = [part("cartodesk", (0, 0, 0), scale=(1.086, 1.086, 1.086))]
    parts.append(part("seachart", (0.1, top(-0.25) + 0.005, -0.25), (90 + tilt, 0, 0), (0.45, 0.45, 0.45)))
    parts.append(part("sextant", (-0.55, top(0.05) + 0.01, 0.05), (tilt, -20, 0), (0.06, 0.06, 0.06)))
    for i, (x, z, lift, yaw) in enumerate([(0.65, 0.15, 0.028, 10), (0.6, 0.12, 0.083, 14), (-0.6, -0.22, 0.028, -5)], start=1):
        parts.append(scroll(i, (x, top(z) + lift, z), yaw, pitch=tilt))
    slots = [((-0.3, 0.03, 0.55), 30), ((0.3, 0.03, 0.6), 75), ((0.8, 0.03, 0.55), 85), ((-0.8, 0.03, 0.5), 60),
             ((0.35, 0.09, 0.58), 78), ((-0.3, 0.09, 0.55), 35), ((0.8, 0.09, 0.52), 86), ((0.05, 0.03, 0.62), 5)]
    for i, (position, yaw) in enumerate(slots, start=4):
        parts.append(scroll(i, position, yaw))
    colliders = [box((0, 0.5, 0), (1.9, 1.0, 0.95))]
    groups = [{"name": f"scroll_{i}", "pivot": [0, 0, 0]} for i in range(1, SCROLLS + 1)]
    return parts, colliders, groups


def amulet(ruby=False):
    """The Pathfinder amulet in the chest bone's space (+z forward, +y up): the valknut disc, a leather cord and five gems.

    With ruby, a large ruby sits in the middle of the valknut (the Pathfinder's Ruby Amulet).
    """
    size, depth = 0.09, 0.16  # the oval pendant is as wide as it is tall and 0.16 of that thick
    bottom = -0.07
    parts = [part("amulet", (0, bottom, depth), (0, 90, 0), (size, size, size))]
    for side in (-1, 1):
        parts.append(log((side * 0.07, 0.16, 0.05), (side * 0.004, bottom + size, depth), 0.003, tint=[0.35, 0.25, 0.18]))
    gems = ["gem_ruby", "gem_amber", "gem_ruby", "gem_amber", "gem_ruby"]
    centre = bottom + size * 0.45
    front = depth + size * 0.08 + 0.002
    for i, gem in enumerate(gems, start=1):
        angle = math.radians(-72 + (i - 1) * 36)
        position = (math.sin(angle) * size * 0.33, centre - math.cos(angle) * size * 0.34, front)
        parts.append(part(gem, position, (90, 0, 0), (0.045, 0.045, 0.045), group=f"gem_{i}", **gem_tint(gem)))
    if ruby:
        parts.append(part("gem_ruby", (0, RUBY_CENTRE * size + bottom, front), (90, 0, 0), (RUBY_SCALE,) * 3, **gem_tint("gem_ruby")))
    groups = [{"name": f"gem_{i}", "pivot": [0, 0, 0]} for i in range(1, 6)]
    return parts, groups


# Where the navigator's table stands on each ship: in front of the helm, to port, on the deck, in the ship root's space.
# The Ashlands ship's stern deck rises 20 degrees towards the stern, so the table follows it.
SHIP_MOUNTS = {
    "Karve": ((-0.5, 0.0, -2.0), (0, 0, 0)),
    "VikingShip": ((-1.0, 0.64, -5.6), (0, 0, 0)),
    "VikingShip_Ashlands": ((-0.8, 3.63, -10.6), (20, 0, 0)),
}


# The ruby's height on the pendant as a share of its size above the bottom, and its scale.
RUBY_CENTRE = 0.5
RUBY_SCALE = 0.075


def gem_tint(gem):
    """The vanilla ruby is untextured red (_Color); in game this tint changes nothing, the preview only shows it red."""
    return {"tint": [1.0, 0.08, 0.1]} if gem == "gem_ruby" else {}


def mooring_post():
    """A mooring post: a thick log 1.5 m above the ground with a cross peg near the top and a coil of rope round it.

    The rope to the ship leaves from the top of the coil, MOORING_ROPE_Y up (the mod uses the same height).
    """
    parts = [post(0, 0, -0.3, 1.5, 0.16), log((-0.32, 1.25, 0), (0.32, 1.25, 0), 0.05)]
    turns, segments, radius = 3, 28, 0.19
    for turn in range(turns):
        y = MOORING_ROPE_Y - 0.12 + turn * 0.06
        for i in range(segments):
            # Each piece reaches a little into the next, so the coil reads as one rope.
            a0 = 2 * math.pi * (i - 0.25) / segments + turn * 0.3
            a1 = 2 * math.pi * (i + 1.25) / segments + turn * 0.3
            parts.append(log((math.sin(a0) * radius, y, math.cos(a0) * radius), (math.sin(a1) * radius, y, math.cos(a1) * radius),
                             0.028, tint=[0.8, 0.66, 0.45]))
    colliders = [box((0, 0.6, 0), (0.4, 1.8, 0.4))]
    return parts, colliders


MOORING_ROPE_Y = 1.0


def navigation_pieces():
    table_parts, table_colliders, table_groups = chart_table()
    desk_parts, desk_colliders, desk_groups = carto_desk()
    amulet_parts, amulet_groups = amulet()
    ruby_parts, ruby_groups = amulet(ruby=True)
    pieces = [
        {"name": "navigatorbord", "parts": table_parts, "colliders": table_colliders, "groups": table_groups,
         "views": views(("front", 180, 20), ("side", 90, 25), ("top", -30, 60))},
        {"name": "kartmakerbenk", "base": "piece_workbench", "parts": desk_parts, "colliders": desk_colliders, "groups": desk_groups,
         "keep": ["roof_check_pint", "connectionEffectPoint", "PlayerBase", "GuidePoint", "AreaMarker"],
         "views": views(("front", 180, 20), ("side", 120, 25), ("top", -30, 60))},
        {"name": "stifinner", "parts": amulet_parts, "groups": amulet_groups},
        {"name": "stifinner_visning", "parts": [piece("stifinner", (0, 0.3, 0))],
         "views": views(("front", 180, 5), ("side", 120, 10), ("angle", -150, 30))},
        {"name": "stifinner_rubin", "parts": ruby_parts, "groups": ruby_groups},
        {"name": "stifinner_rubin_visning", "parts": [piece("stifinner_rubin", (0, 0.3, 0))],
         "views": views(("front", 180, 5), ("side", 120, 10), ("angle", -150, 30))},
        defence("fortoyningspale", "wood_pole2", *mooring_post(), [], views=views(("front", 180, 15), ("side", 90, 15), ("top", -30, 60))),
    ]
    for ship, (position, rotation) in SHIP_MOUNTS.items():
        focus = [position[0], position[1] + 0.6, position[2]]
        pieces.append({"name": f"mount_{ship}", "parts": [part(ship, (0, 0, 0)), piece("navigatorbord", position, rotation)],
                       "views": [{"label": "stern", "yaw": 20, "pitch": 30, "focus": focus, "radius": 2.2},
                                 {"label": "side", "yaw": 90, "pitch": 25, "focus": focus, "radius": 2.2},
                                 {"label": "top", "yaw": 0, "pitch": 70, "focus": focus, "radius": 3.5}]})
    return pieces


def defence(name, base, parts, colliders, snaps, **extra):
    data = {"name": name, "base": base, "parts": parts, "colliders": colliders, "snapPoints": sum((r3(p) for p in snaps), [])}
    data.update({key: value for key, value in extra.items() if value})
    return data


def main():
    pieces = [{"name": f"pal_{key}", "template": True, "parts": palisade_segment(f"pal_{key}")} for key in "abcd"]
    pieces.append(defence("skansevegg", "stake_wall", *wall()))
    pieces.append(defence("skansehjorne", "stake_wall", *corner90(), views=views(("outside", 135, 15), ("inside", -45, 25), ("top", -45, 60))))
    pieces.append(defence("skansehjorne45", "stake_wall", *corner45(), views=views(("outside", 160, 15), ("inside", -20, 30), ("top", -20, 65))))
    gate_parts, gate_colliders, gate_snaps, gate_groups = gatehouse()
    pieces.append(defence("skanseport", "wood_gate", gate_parts, gate_colliders, gate_snaps, groups=gate_groups, keep=["door"]))
    pieces.append(defence("skansetrapp", "stake_wall", *stairs(), views=views(("side", 90, 10), ("front", 0, 20), ("angle", -40, 30))))
    for name, n, levels in (("vakttarn_liten", 2, [WALK, 4.4]), ("vakttarn", 3, [WALK, 4.4]), ("vakttarn_stor", 4, [WALK, 4.4, 6.8])):
        parts, colliders, ladders, snaps = tower(n, levels, name)
        pieces.append(defence(name, "stake_wall", parts, colliders, snaps, ladders=ladders))
    pieces.append(defence("spansk_rytter", "piece_sharpstakes", *cheval_de_frise(), keep=["HIT AREA"],
                          hitArea=box((0, 0.7, 0), (4.6, 1.4, 1.9)),
                          views=views(("front", 180, 15), ("side", 90, 10), ("angle", -40, 30))))
    bridge_parts, bridge_colliders, bridge_snaps, bridge_groups = drawbridge()
    pieces.append(defence("vindebro", "wood_gate", bridge_parts, bridge_colliders, bridge_snaps, groups=bridge_groups, keep=["door"],
                          views=views(("outside", 160, 25), ("side", 90, 10), ("top", 180, 65))))
    pieces.append({"name": "oversikt", "parts": [
        piece("skanseport"),
        piece("skansevegg", (-6.4, 0, 0)),
        piece("skansevegg", (-10.4, 0, 0)),
        piece("skansevegg", (6.4, 0, 0)),
        piece("vakttarn_liten", (9.4, 0, -0.8)),
        piece("skansevegg", (12.4, 0, 0)),
        piece("skansehjorne45", (16.4, 0, 0), (0, 45, 0)),
        piece("skansevegg", (19.23, 0, -2.83), (0, 45, 0)),
        piece("vakttarn_stor", (-14.4, 0, -1.0)),
        piece("skansevegg", (-16.2, 0, -5.0), (0, -90, 0)),
        piece("skansehjorne", (-16.2, 0, -9.0), (0, -90, 0)),
        piece("skansevegg", (-12.2, 0, -9.0), (0, 180, 0)),
        piece("vakttarn", (-8.8, 0, -8.0)),
        piece("skansetrapp", (-6.4, 0, -1.8)),
        piece("spansk_rytter", (-4, 0, 5)),
        piece("spansk_rytter", (5, 0, 5.5), (0, 15, 0)),
    ], "views": views(("outside", 200, 18), ("inside", 20, 30), ("top", 0, 70))})
    pieces += navigation_pieces()
    # The White Hilt portals as the mod places their models, with a 2 m pole for scale.
    pieces.append({"name": "hvithjaltportal", "parts": [
        part("portal", (0, 0, 0), (0, 90, 0), (3.6, 3.6, 3.6)),
        part("portalground", (6, 0.03, 0), scale=(4 / 108.2,) * 3),
        part("eternalfire", (-4, 0, 0), scale=(1.3,) * 3),
        part("wood_pole", (-2.5, 1, 1.5))
    ], "views": views(("front", 180, 15), ("side", 90, 15), ("top", 180, 60))})
    OUT.write_text(json.dumps({"pieces": pieces}, indent=1), encoding="utf-8")
    print(f"wrote {OUT} with {len(pieces)} pieces")


if __name__ == "__main__":
    main()
