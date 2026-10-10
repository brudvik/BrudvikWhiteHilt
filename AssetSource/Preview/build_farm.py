"""The farm: skigard fences and gates, a wattle fence, wattle-and-daub walls, a well sweep and a stream mill.

build_defenses.py adds farm_pieces() to the layout it writes; run that script, not this one. The mod builds them in
Pieces/Farm.

  skigard         the Norwegian round-pole fence: pairs of upright stakes bound with withies, and split poles laid
                  slanting in the bands, overlapping. A section's poles reach past its +x end into the next section, so
                  sections joined end to end run on without a seam, and a lone end slopes down like a real skigard's.
  grind           a gate of the same grey poles, swinging with the hidden vanilla door either way
  flettgjerde     a wattle fence: stakes with withies woven in and out between them
  bindingsverk    a timber frame with clay daub between the timbers (the stone box, tinted to clay), or bare wattle
  bronnvipp       a well sweep: a long pole on a forked post with a stone at its short end; used, it dips the bucket
                  into the well (SwingDriver about z, at 0.4 of the door's swing)
  bekkekvern      a little mill house on stone feet over a stream, a horizontal wheel under its floor and a chute. It
                  is the vanilla windmill without its sails: the hopper at the back and the flour bin at the front stand
                  where the windmill's add and empty switches are, and the wheel (group "wheel") turns while it grinds
"""

import math
import random

from build_borgund import pole
from build_defenses import box, part, r3, rotate, views
from build_defence_extras import plank
from build_gate_controls import ROPE_TINT
from build_log_house import TAR_DOOR_TINT, WALL_VIEWS, log_x, piece_entry

GREY_POLE = [0.6, 0.61, 0.6]  # weathered spruce, as old skigard goes grey
WITHY = [0.5, 0.42, 0.3]
CLAY = [0.8, 0.7, 0.55]
TIMBER = [0.62, 0.5, 0.4]
FENCE_HEIGHT = 1.3
RAIL_RISE = 0.6  # the slant of the skigard's poles: up 0.6 m for every 1 m along


def skigard(length, seed):
    """A skigard along x from -length/2 to +length/2, its poles running on past +length/2 into the next section."""
    rng = random.Random(seed)
    parts = []
    x0 = -length / 2
    for i in range(int(length) + 1):
        x = x0 + i
        shade = rng.choice((0.9, 1.0))
        for z in (-0.07, 0.07):
            parts.append(pole((x + rng.uniform(-0.02, 0.02), -0.1, z), (x, FENCE_HEIGHT + 0.1, z), 0.045, tint=[c * shade for c in GREY_POLE]))
        for y in (0.4, 0.8, 1.15):
            parts.append(pole((x, y, -0.1), (x, y, 0.1), 0.03, tint=WITHY, detail=True))
    run = FENCE_HEIGHT / RAIL_RISE
    step = 0.28
    count = int(length / step)
    for i in range(count):
        start = x0 + i * step
        shade = rng.choice((0.85, 0.95, 1.0))
        parts.append(pole((start, 0.08, 0), (start + run, FENCE_HEIGHT, 0), 0.04, tint=[c * shade for c in GREY_POLE]))
    colliders = [box((0, FENCE_HEIGHT / 2, 0), (length, FENCE_HEIGHT, 0.24))]
    return parts, colliders, [(x0, 0, 0), (-x0, 0, 0)]


def gate_leaf(group, x0, x1, hinge_x, rng):
    """A gate leaf of grey poles from x0 to x1: a frame, a brace from the foot at the hinge up to the far top, and
    upright poles between."""
    parts = []
    top, bottom = 1.15, 0.15
    for y in (bottom, top):
        parts.append(pole((x0, y, 0), (x1, y, 0), 0.04, tint=GREY_POLE, group=group))
    far = x0 if hinge_x > x0 else x1
    parts.append(pole((hinge_x, bottom, 0), (far, top, 0), 0.035, tint=GREY_POLE, group=group))
    count = max(2, round(abs(x1 - x0) / 0.2))
    for i in range(count + 1):
        x = x0 + (x1 - x0) * i / count
        parts.append(pole((x, bottom - 0.08, 0.03), (x, top + 0.1, 0.03), 0.03, tint=[c * rng.choice((0.9, 1.0)) for c in GREY_POLE], group=group))
    collider = box(((x0 + x1) / 2, (bottom + top) / 2, 0), (abs(x1 - x0), top - bottom + 0.2, 0.1), group=group)
    return parts, collider


def gate(width, seed):
    """A skigard gate width wide between two stout posts: one leaf for 2 m, two for 4 m."""
    rng = random.Random(seed)
    half = width / 2
    parts = [pole((x, -0.2, 0), (x, FENCE_HEIGHT + 0.15, 0), 0.08, tint=GREY_POLE) for x in (-half, half)]
    colliders = [box((x, FENCE_HEIGHT / 2, 0), (0.18, FENCE_HEIGHT, 0.18)) for x in (-half, half)]
    if width <= 2:
        leaf, collider = gate_leaf("leaf", -half + 0.1, half - 0.1, half - 0.1, rng)
        groups = [{"name": "leaf", "pivot": [half - 0.1, 0, 0]}]
        parts += leaf
        colliders.append(collider)
    else:
        left, left_collider = gate_leaf("leaf_left", -half + 0.1, -0.02, -half + 0.1, rng)
        right, right_collider = gate_leaf("leaf_right", 0.02, half - 0.1, half - 0.1, rng)
        groups = [{"name": "leaf_left", "pivot": [-half + 0.1, 0, 0]}, {"name": "leaf_right", "pivot": [half - 0.1, 0, 0]}]
        parts += left + right
        colliders += [left_collider, right_collider]
    return parts, colliders, [(-half, 0, 0), (half, 0, 0)], groups


def wattle(x0, x1, y0, y1, rng, z=0.0, stakes=0.3):
    """Withies woven in and out of upright stakes from x0 to x1 and y0 to y1."""
    parts = []
    count = max(2, round((x1 - x0) / stakes))
    for i in range(count + 1):
        x = x0 + (x1 - x0) * i / count
        parts.append(pole((x, y0 - 0.05, z), (x, y1 + 0.05, z), 0.03, tint=WITHY))
    rows = max(2, round((y1 - y0) / 0.07))
    for j in range(rows):
        y = y0 + (y1 - y0) * (j + 0.5) / rows
        shade = rng.choice((0.85, 1.0))
        parts.append(pole((x0, y, z + (0.025 if j % 2 else -0.025)), (x1, y, z + (-0.025 if j % 2 else 0.025)), 0.022,
                          tint=[c * shade for c in WITHY], detail=j % 2 == 1))
    return parts


def wattle_fence(seed):
    """A wattle fence 2 m long and 1 m high."""
    rng = random.Random(seed)
    parts = wattle(-1, 1, 0.05, 1.0, rng)
    for x in (-1, 1):
        parts.append(pole((x, -0.2, 0), (x, 1.15, 0), 0.05, tint=WITHY))
    return parts, [box((0, 0.55, 0), (2, 1.1, 0.12))], [(-1, 0, 0), (1, 0, 0)]


def timber_frame(length, rng):
    """The frame of a wattle-and-daub wall: a sill, a plate, posts at the ends and a brace."""
    half = length / 2
    parts = [plank((-half, 0.1, 0), (half, 0.1, 0), 0.22, 0.2, tint=TIMBER), plank((-half, 1.9, 0), (half, 1.9, 0), 0.22, 0.2, tint=TIMBER)]
    for x in (-half + 0.08, half - 0.08):
        parts.append(plank((x, 0.2, 0), (x, 1.8, 0), 0.22, 0.16, tint=TIMBER))
    if length >= 2:
        parts.append(plank((-half + 0.16, 0.2, 0), (-half + 0.9, 1.8, 0), 0.22, 0.14, tint=TIMBER))
    return parts


def daub_wall(length, seed):
    """A timber frame with clay daub between the timbers, 2 m high."""
    rng = random.Random(seed)
    half = length / 2
    parts = timber_frame(length, rng)
    parts.append(part("stonebox", (0, 1.0, 0), scale=(length - 0.3, 1.6, 0.16), tint=CLAY))
    # A patch where the daub has fallen and the wattle shows.
    if length >= 2:
        x = half - 0.6
        parts += wattle(x - 0.25, x + 0.25, 0.45, 0.8, rng, z=0.09, stakes=0.25)
    colliders = [box((0, 1.0, 0), (length, 2.0, 0.24))]
    snaps = [(x, y, 0) for x in (-half, half) for y in (0, 2)]
    return parts, colliders, snaps


def bare_wattle_wall(seed):
    """A timber frame with bare wattle between the timbers, for a shed or a pen."""
    rng = random.Random(seed)
    parts = timber_frame(2.0, rng) + wattle(-0.84, 0.84, 0.2, 1.8, rng)
    return parts, [box((0, 1.0, 0), (2, 2.0, 0.22))], [(x, y, 0) for x in (-1, 1) for y in (0, 2)]


SWEEP_PIVOT = (-2.2, 2.0, 0)
SWEEP_REACH = 2.25  # from the fork to the tip
SWEEP_REST = 48.8  # degrees above level at rest; used, the sweep turns 36 degrees down, the tip over the well
ROD = 2.3


def well_sweep(seed):
    """A well sweep (brønnvippe): a timber curb round the well at the origin, a forked post and a long pole whose short
    end carries a stone. The pole turns in the group "sweep" about the fork; the rod and the bucket hang from its tip in
    the group "bucket", which follows the tip without turning, so the bucket goes straight down into the well."""
    rng = random.Random(seed)
    parts = []
    # The curb: four boards a side, 1.1 m square and 0.7 m high.
    for side in range(4):
        a = math.radians(90 * side)
        for y in (0.1, 0.3, 0.5, 0.68):
            p0 = (0.55 * math.cos(a) - 0.55 * math.sin(a), y, 0.55 * math.sin(a) + 0.55 * math.cos(a))
            p1 = (0.55 * math.cos(a) + 0.55 * math.sin(a), y, 0.55 * math.sin(a) - 0.55 * math.cos(a))
            parts.append(plank(p0, p1, 0.08, 0.18, tint=TIMBER))
    parts.append(part("stonebox", (0, 0.05, 0), scale=(1.0, 0.04, 1.0), tint=[0.08, 0.1, 0.12]))
    # The forked post.
    px, py, _ = SWEEP_PIVOT
    parts.append(pole((px, -0.3, 0), (px, py - 0.25, 0), 0.12, tint=TIMBER))
    for z in (-0.12, 0.12):
        parts.append(pole((px, py - 0.3, 0), (px, py + 0.15, z), 0.06, tint=TIMBER))
    parts.append(pole((px, py, -0.15), (px, py, 0.15), 0.03, tint=ROPE_TINT))
    # The sweep at rest, its tip up and short of the well.
    a = math.radians(SWEEP_REST)
    tip = (px + SWEEP_REACH * math.cos(a), py + SWEEP_REACH * math.sin(a), 0)
    tail = (px - 0.5 * SWEEP_REACH * math.cos(a), py - 0.5 * SWEEP_REACH * math.sin(a), 0)
    sweep = [pole(tail, tip, 0.07, tint=GREY_POLE)]
    sweep.append(part("rock", (tail[0] + 0.15, tail[1] - 0.18, 0), (0, rng.uniform(0, 360), 0), (0.42, 0.42, 0.42), tint=[0.6, 0.6, 0.6]))
    sweep.append(pole((tail[0] + 0.15, tail[1] - 0.05, 0), (tail[0] + 0.15, tail[1] - 0.25, 0), 0.02, tint=ROPE_TINT, detail=True))
    parts += [dict(p, group="sweep") for p in sweep]
    foot = (tip[0], tip[1] - ROD, 0)
    bucket = [pole(tip, foot, 0.03, tint=GREY_POLE), part("stonebox", (foot[0], foot[1] - 0.15, 0), scale=(0.28, 0.3, 0.28), tint=[0.45, 0.35, 0.25])]
    parts += [dict(p, group="bucket") for p in bucket]
    colliders = [box((0, 0.35, 0), (1.2, 0.7, 1.2)), box((px, py / 2, 0), (0.3, py, 0.3)),
                 box((foot[0], foot[1] + ROD / 2, 0), (0.3, ROD, 0.3), group="bucket")]
    groups = [{"name": "sweep", "pivot": list(SWEEP_PIVOT)}, {"name": "bucket", "pivot": [round(v, 3) for v in tip]}]
    return parts, colliders, [(0, 0, 0)], groups


# The mill house stands on four stone feet with its floor at FLOOR; the windmill's switches and output it keeps are at
# add (0, 1.52, -1.46), empty (0, 1.14, 1.53) and output (0, 0.94, 2.35).
FLOOR = 0.8
HOUSE = 1.2  # half the house's width


def stream_mill(seed):
    """A stream mill (bekkekvern) as on Norwegian farms: a little log house on stone feet, a horizontal wheel under its
    floor turned by water from a chute, the hopper on the back wall and the flour bin at the front."""
    rng = random.Random(seed)
    parts = []
    for sx in (-1, 1):
        for sz in (-1, 1):
            parts.append(part("rock", (sx * (HOUSE - 0.2), -0.35, sz * (HOUSE - 0.2)), (0, rng.uniform(0, 360), 0), (0.55, 1.6, 0.6), tint=[0.6, 0.6, 0.58]))
    # The floor and the log walls, 1.6 m high.
    parts.append(part("wood_floor", (0, FLOOR - 0.08, 0), scale=(HOUSE, 1, HOUSE)))
    for i, y in enumerate((FLOOR + 0.2, FLOOR + 0.6, FLOOR + 1.0, FLOOR + 1.4)):
        offset = 0.1 if i % 2 else 0
        for yaw in (0, 90, 180, 270):
            log = log_x(-HOUSE - 0.2, HOUSE + 0.2, y + offset, rng, z=HOUSE)
            log["scale"] = r3([log["scale"][0] * 0.75, log["scale"][1], log["scale"][2] * 0.75])
            parts.append(rotate(log, yaw))
    # A 45 degree roof of thatch over the house, its ridge along x.
    top = FLOOR + 1.6
    half = HOUSE + 0.1
    parts.append(part("wood_roof_45", (0, top + half / 2, half / 2), scale=(HOUSE + 0.3, half / 2, half / 2)))
    parts.append(part("wood_roof_45", (0, top + half / 2, -half / 2), (0, 180, 0), (HOUSE + 0.3, half / 2, half / 2)))
    for x in (-HOUSE, HOUSE):
        for z, side in ((half / 2, 1), (-half / 2, -1)):
            parts.append(plank((x, top, side * half), (x, top + half, 0), 0.06, 0.2, tint=TAR_DOOR_TINT))
    # Boards up the gable ends.
    for x in (-HOUSE, HOUSE):
        for i in range(10):
            z0 = -half + 2 * half * i / 10
            z1 = -half + 2 * half * (i + 1) / 10
            height = half - abs((z0 + z1) / 2)
            parts.append(plank((x, top, (z0 + z1) / 2), (x, top + height, (z0 + z1) / 2), (z1 - z0) * 0.98, 0.05, tint=TAR_DOOR_TINT))
    # The hopper on the back wall over the add switch, and the flour bin at the front over the empty switch.
    parts.append(part("stonebox", (0, 1.62, -HOUSE - 0.28), scale=(0.6, 0.4, 0.5), tint=[0.62, 0.5, 0.38]))
    parts.append(part("stonebox", (0, 1.38, -HOUSE - 0.28), scale=(0.3, 0.12, 0.3), tint=[0.5, 0.4, 0.3]))
    parts.append(part("stonebox", (0, 0.5, HOUSE + 0.35), scale=(0.8, 0.55, 0.55), tint=[0.62, 0.5, 0.38]))
    parts.append(part("stonebox", (0, 0.79, HOUSE + 0.35), scale=(0.7, 0.04, 0.45), tint=[0.92, 0.88, 0.8]))
    parts.append(plank((0, 1.05, HOUSE + 0.05), (0, 0.85, HOUSE + 0.4), 0.18, 0.05, tint=TIMBER))
    # The chute bringing water down to the wheel from the back, at the side.
    for side in (-0.12, 0.12):
        parts.append(plank((0.9 + side, 1.0, -3.0), (0.35 + side, 0.3, -0.35), 0.04, 0.2, tint=TAR_DOOR_TINT))
    parts.append(plank((0.9, 0.9, -3.0), (0.35, 0.2, -0.35), 0.24, 0.04, tint=TAR_DOOR_TINT))
    for z in (-2.6, -1.8):
        parts.append(pole((0.8, -0.4, z), (0.8, 0.75 + (z + 3.0) * -0.25, z), 0.06, tint=TIMBER))
    # The horizontal wheel (kallhjul) under the floor: a shaft and twelve paddles, turning in the group "wheel".
    wheel = [pole((0, 0.05, 0), (0, FLOOR - 0.1, 0), 0.08, tint=TIMBER)]
    for i in range(12):
        a = 2 * math.pi * i / 12
        wheel.append(plank((0.1 * math.sin(a), 0.3, 0.1 * math.cos(a)), (0.6 * math.sin(a), 0.3, 0.6 * math.cos(a)), 0.04, 0.22, tint=TIMBER))
    parts += [dict(p, group="wheel") for p in wheel]
    colliders = [box((0, FLOOR + 0.8, 0), (2 * HOUSE + 0.2, 1.6, 2 * HOUSE + 0.2)), box((0, top + 0.5, 0), (2 * HOUSE, 1.0, 2 * HOUSE)),
                 box((0, 0.5, HOUSE + 0.35), (0.8, 0.55, 0.55))]
    colliders += [box((sx * (HOUSE - 0.2), FLOOR / 2 - 0.2, sz * (HOUSE - 0.2)), (0.5, FLOOR + 0.4, 0.5)) for sx in (-1, 1) for sz in (-1, 1)]
    groups = [{"name": "wheel", "pivot": [0, 0, 0]}]
    return parts, colliders, [(0, 0, 0)], groups


def farm_pieces():
    fence_views = views(("side", 160, 12), ("end", 90, 10), ("angle", -140, 25))
    pieces = [
        piece_entry("skigard", "wood_pole2", *skigard(4, "skigard"), views=fence_views),
        piece_entry("skigard_2m", "wood_pole2", *skigard(2, "skigard_2m"), views=fence_views),
        piece_entry("flettgjerde", "wood_pole2", *wattle_fence("flettgjerde"), views=fence_views),
        piece_entry("bindingsverk", "wood_wall_roof", *daub_wall(2, "bindingsverk"), views=WALL_VIEWS),
        piece_entry("bindingsverk_1m", "wood_wall_roof", *daub_wall(1, "bindingsverk_1m"), views=WALL_VIEWS),
        piece_entry("flettverk", "wood_wall_roof", *bare_wattle_wall("flettverk"), views=WALL_VIEWS),
    ]
    for name, width in (("grind", 2), ("dobbelgrind", 4)):
        parts, colliders, snaps, groups = gate(width, name)
        pieces.append(piece_entry(name, "wood_door" if width <= 2 else "wood_gate", parts, colliders, snaps, groups=groups, keep=["door"],
                                  views=fence_views))
    parts, colliders, snaps, groups = well_sweep("bronnvipp")
    pieces.append(piece_entry("bronnvipp", "wood_door", parts, colliders, snaps, groups=groups, keep=["door"],
                              views=views(("side", 180, 10), ("angle", 140, 20))))
    parts, colliders, snaps, groups = stream_mill("bekkekvern")
    pieces.append(piece_entry("bekkekvern", "windmill", parts, colliders, snaps, groups=groups,
                              keep=["add_switch", "empty_switch", "output", "_enabled", "PlayerBase"],
                              views=views(("front", 160, 15), ("back", -20, 20), ("angle", 110, 30))))
    pieces.append(overview())
    return pieces


def overview():
    """A corner of a farm for the documentation: skigard round a field with a gate, the mill and the well."""
    def at(name, x, z, yaw=0, y=0.0):
        data = {"piece": name, "position": r3((x, y, z))}
        if yaw:
            data["rotation"] = [0, yaw, 0]
        return data

    parts = [at("skigard", -4, 4), at("skigard", 0, 4), at("grind", 3, 4), at("skigard", 6, 4)]
    parts += [at("skigard", -6, 2, 90), at("skigard", -6, -2, 90)]
    parts += [at("bekkekvern", 3, -2), at("bronnvipp", -2, -1)]
    parts += [at("flettgjerde", 7, 1, 90), at("bindingsverk", 7, -1, 90)]
    return {"name": "gard", "parts": parts, "views": views(("front", 160, 25), ("angle", -140, 30), ("top", 160, 65))}
