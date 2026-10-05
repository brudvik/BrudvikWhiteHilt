"""The Oil Cauldron, the Alarm Bell and the Harbour Crane, and the stone defences in black marble and grausten.

build_defenses.py adds extra_pieces() to the layout it writes; run that script, not this one. The mod builds them in
Pieces/Defenses/Siege and Pieces/Storage.
"""

import math
import random

from build_defenses import box, log, part, post, r3, views
from build_gate_controls import IRON_TINT, ROPE_TINT, coil
from build_stone_defenses import COURSE_TINT, block, masonry, moss, stone_pieces

EMBER_TINT = [1.6, 0.55, 0.2]
TAR_TINT = [0.12, 0.1, 0.08]


def plank(p0, p1, width, thickness, **extra):
    """A plank or beam from p0 to p1, which may slope: width across it, thickness up through it."""
    dx, dy, dz = (b - a for a, b in zip(p0, p1))
    run = math.hypot(dx, dz)
    length = math.hypot(run, dy)
    yaw = math.degrees(math.atan2(-dz, dx))
    pitch = math.degrees(math.atan2(dy, run))
    middle = [(a + b) / 2 for a, b in zip(p0, p1)]
    return part("wood_beam", middle, (0, yaw, pitch), (length / 2, thickness / 0.4, width / 0.4), **extra)


# The oil cauldron's measures, so every part meets the next.
HEARTH_TOP = 0.45
POT_RIM = 1.25  # top of the pot
POT_HEIGHT = 0.5  # the bell is 1.2 m tall, at 0.42
POT_RADIUS = 0.32  # the bell is 1.04 m across, at 0.62
AXLE = POT_RIM + 0.12


def oil_cauldron():
    """A cauldron of boiling pitch on a tipping frame over a small hearth, with a wooden chute out to the front (+z)
    over the parapet. Set on the walk over a gate: tipped, it pours down in front of the wall. The pot, its arms,
    handle and pitch lie in the group "pot", turning about the axle."""
    rng = random.Random("oljegryte")
    # The hearth: a stone block with a ring of stones and embers on top; the frame posts stand on its corners.
    parts = masonry(-0.75, 0.75, 0.0, HEARTH_TOP, -0.5, 0.5, rng)
    for i in range(6):
        a = 2 * math.pi * i / 6
        parts.append(part("rock", (math.sin(a) * 0.3, HEARTH_TOP - 0.02, math.cos(a) * 0.3), (0, rng.uniform(0, 360), 0), (0.3, 0.25, 0.3),
                          tint=[0.4, 0.38, 0.36]))
    for _ in range(4):
        parts.append(part("rock", (rng.uniform(-0.12, 0.12), HEARTH_TOP, rng.uniform(-0.12, 0.12)), (0, rng.uniform(0, 360), 0), (0.2, 0.12, 0.2),
                          tint=EMBER_TINT, detail=True))
    for x in (-0.6, 0.6):
        parts.append(post(x, 0.0, HEARTH_TOP - 0.05, AXLE + 0.12, 0.07))
    parts.append(log((-0.68, AXLE, 0.0), (0.68, AXLE, 0.0), 0.04, tint=IRON_TINT))
    # The pot: the bell turned upside down and squat, an iron bowl with a flared rim, hanging from the axle by two
    # iron arms that hold it at the rim. A wooden handle on its back tips it.
    pot = [part("Bell", (0, POT_RIM, 0), (180, 0, 0), (0.62, 0.42, 0.62), tint=[0.34, 0.33, 0.34])]
    pot.append(part("rock", (0, POT_RIM - 0.1, 0), scale=(0.5, 0.05, 0.5), tint=TAR_TINT))
    for x in (-POT_RADIUS, POT_RADIUS):
        pot.append(log((x, AXLE, 0.0), (x, POT_RIM - 0.12, 0.0), 0.025, tint=IRON_TINT))
    pot.append(log((0.0, POT_RIM - 0.08, -POT_RADIUS + 0.02), (0.0, POT_RIM + 0.35, -0.85), 0.035))
    parts += [dict(item, group="pot") for item in pot]
    # The chute: a trough of three planks from just under the pot's front lip out over the parapet, held by a post.
    start, end = (0.0, POT_RIM - 0.08, POT_RADIUS - 0.04), (0.0, POT_RIM - 0.3, 1.45)
    parts.append(plank(start, end, 0.42, 0.04))
    for x in (-0.21, 0.21):
        parts.append(plank((x, start[1] + 0.07, start[2]), (x, end[1] + 0.07, end[2]), 0.03, 0.14))
    parts.append(post(0.0, 0.62, -0.05, start[1] - 0.06, 0.05))
    colliders = [box((0, HEARTH_TOP / 2, 0), (1.5, HEARTH_TOP, 1.0)), box((0, (HEARTH_TOP + AXLE) / 2 + 0.1, 0), (1.4, AXLE - HEARTH_TOP + 0.2, 0.8)),
                 box((0, POT_RIM - 0.2, 0.9), (0.5, 0.3, 1.1))]
    return parts, colliders, [{"name": "pot", "pivot": [0, AXLE, 0]}]


def alarm_bell():
    """A bell under a small gabled roof on two posts and a beam, with a rope to ring it. The posts stand in the stone
    base, braced to it front and back; the bell hangs from the beam by an iron eye and swings in the group "bell"."""
    rng = random.Random("alarmklokke")
    top = 2.4  # top of the posts; the beam rests on them
    beam_y = top - 0.08
    parts = masonry(-0.75, 0.75, 0.0, 0.25, -0.45, 0.45, rng)
    for x in (-0.6, 0.6):
        parts.append(post(x, 0.0, 0.1, top, 0.09))
        for z in (-0.38, 0.38):
            parts.append(log((x, 0.8, 0.0), (x, 0.24, z), 0.04))
    parts.append(log((-0.78, beam_y, 0.0), (0.78, beam_y, 0.0), 0.08))
    # The gable: two boards meeting at a ridge right over the beam.
    ridge = beam_y + 0.3
    for side in (1, -1):
        parts.append(plank((-0.82, ridge, 0.0), (-0.82, ridge - 0.3, side * 0.42), 0.04, 0.04))
        parts.append(part("wood_floor_1x1", (0, ridge - 0.17, side * 0.22), (side * 36, 0, 0), (1.75, 0.45, 0.52)))
        parts.append(plank((0.82, ridge, 0.0), (0.82, ridge - 0.3, side * 0.42), 0.04, 0.04))
    parts.append(log((-0.85, ridge - 0.02, 0.0), (0.85, ridge - 0.02, 0.0), 0.04))
    # Boards close the gable ends between the beam and the ridge.
    for x in (-0.8, 0.8):
        for y, half in ((beam_y + 0.1, 0.3), (beam_y + 0.19, 0.18), (beam_y + 0.26, 0.07)):
            parts.append(plank((x, y, -half), (x, y, half), 0.03, 0.09))
    # The bell: 1.2 m tall at full size, so 0.66 m at 0.55; its crown meets the iron eye under the beam.
    bell_scale = 0.55
    crown = beam_y - 0.08 - 0.06
    bell = [part("Bell", (0, crown - 1.19 * bell_scale, 0.0), scale=(bell_scale,) * 3)]
    bell.append(log((0, beam_y - 0.06, 0.0), (0, crown - 0.02, 0.0), 0.04, tint=IRON_TINT))
    mouth = crown - 1.19 * bell_scale
    bell.append(log((0, mouth + 0.05, 0.0), (0.06, 0.95, 0.15), 0.012, tint=ROPE_TINT))
    bell.append(log((0.02, 0.95, 0.15), (0.1, 0.95, 0.15), 0.03))
    parts += [dict(item, group="bell") for item in bell]
    colliders = [box((0, 0.125, 0), (1.5, 0.25, 0.9)), box((0, top / 2 + 0.2, 0), (1.5, top + 0.1, 0.5))]
    return parts, colliders, [{"name": "bell", "pivot": [0, beam_y, 0]}]


# The crane's measures; Pieces/Storage/HarbourCrane.cs uses the same.
CRANE_MAST_TOP = 5.6
CRANE_JIB_ROOT = (0.0, 5.0, 0.0)  # where the jib meets the mast; it turns and luffs about this point
CRANE_TIP = (0.0, 5.45, 4.6)  # the block at the jib's tip, at rest
CRANE_HOOK = 2.6  # height of the hook at rest


def harbour_crane():
    """A wooden harbour crane on a stone base: a mast, a jib held up by a stay from the mast top, a stone counterweight
    on the short arm behind, a winch at the foot of the mast, and a crate hanging from the hook.

    Three groups move. "jib" holds the jib, its arm, counterweight, blocks and stay, and turns about the mast and luffs
    about CRANE_JIB_ROOT, so the tip can reach a ship's hold. "fall" is the rope from the tip down to the hook at rest; the
    mod stretches it as the hook goes up and down. "hook" holds the hook and the crate, hanging under the tip."""
    rng = random.Random("havnekran")
    base_top = 0.86
    parts = masonry(-1.0, 1.0, -0.3, 0.8, -1.0, 1.0, rng)
    parts.append(block(-1.03, 1.03, 0.78, base_top, -1.03, 1.03, tint=COURSE_TINT, detail=True))
    parts += moss(-1.0, 1.0, base_top, 0.6, 1.0, rng, 2)
    parts.append(post(0, 0, 0.5, CRANE_MAST_TOP, 0.2))
    for x in (-0.75, 0.75):
        parts.append(log((x, base_top, 0.0), (0.0, 2.2, 0.0), 0.07))
    # The jib, its short arm and counterweight, the pulley blocks and the stay and running rope from the mast top.
    tip = CRANE_TIP
    jib = [log(CRANE_JIB_ROOT, tip, 0.13), log(CRANE_JIB_ROOT, (0.0, 5.12, -1.55), 0.11)]
    jib += masonry(-0.35, 0.35, 4.55, 5.03, -1.85, -1.25, rng)
    jib.append(block(-0.09, 0.09, tip[1] - 0.02, tip[1] + 0.22, tip[2] - 0.12, tip[2] + 0.12, tint=[0.55, 0.42, 0.3]))
    jib.append(log((0.0, CRANE_MAST_TOP - 0.1, 0.1), (0.0, tip[1] + 0.05, tip[2] - 0.15), 0.022, tint=ROPE_TINT))
    jib.append(log((0.0, CRANE_MAST_TOP - 0.25, 0.14), (0.0, tip[1] + 0.18, tip[2] - 0.1), 0.016, tint=ROPE_TINT))
    parts += [dict(item, group="jib") for item in jib]
    parts.append(block(-0.09, 0.09, CRANE_MAST_TOP - 0.3, CRANE_MAST_TOP - 0.05, -0.12, 0.12, tint=[0.55, 0.42, 0.3]))
    # The fall: the rope hanging from the tip to the hook at rest, which the mod stretches as the hook goes up and down.
    parts.append(log((0.0, 0.0, 0.0), (0.0, -(tip[1] - CRANE_HOOK), 0.0), 0.018, tint=ROPE_TINT, group="fall"))
    # The hook and the crate slung from it, hanging from the hook's top at the group's pivot.
    hook = [log((0.0, 0.0, 0.0), (0.0, -0.3, 0.0), 0.025, tint=IRON_TINT),
            log((0.0, -0.3, 0.0), (0.0, -0.36, 0.08), 0.022, tint=IRON_TINT),
            log((0.0, -0.36, 0.08), (0.0, -0.26, 0.13), 0.02, tint=IRON_TINT)]
    crate_top = -0.75
    for dx in (-0.24, 0.24):
        for dz in (-0.24, 0.24):
            hook.append(log((0.0, -0.3, 0.0), (dx, crate_top, dz), 0.012, tint=ROPE_TINT))
    hook.append(block(-0.3, 0.3, crate_top - 0.55, crate_top, -0.3, 0.3, tint=[0.62, 0.48, 0.34]))
    for y in (crate_top - 0.12, crate_top - 0.43):
        hook.append(block(-0.31, 0.31, y - 0.03, y + 0.03, -0.31, 0.31, tint=IRON_TINT))
    parts += [dict(item, group="hook") for item in hook]
    # The rope from the mast-top block down behind the mast to the winch drum, and the winch.
    drum = (0.0, 1.2, -0.55)
    parts.append(log((0.0, CRANE_MAST_TOP - 0.25, -0.12), (0.0, drum[1] + 0.13, drum[2]), 0.016, tint=ROPE_TINT))
    parts.append(log((-0.6, drum[1], drum[2]), (0.6, drum[1], drum[2]), 0.13))
    for x in (-0.68, 0.68):
        parts.append(post(x, drum[2], base_top - 0.05, drum[1] + 0.1, 0.06))
        side = 1 if x > 0 else -1
        parts.append(log((x, drum[1], drum[2]), (x + side * 0.1, drum[1], drum[2]), 0.03, tint=IRON_TINT))
        parts.append(log((x + side * 0.1, drum[1], drum[2]), (x + side * 0.1, drum[1] + 0.3, drum[2]), 0.025, tint=IRON_TINT))
        parts.append(log((x + side * 0.1, drum[1] + 0.3, drum[2]), (x + side * 0.27, drum[1] + 0.3, drum[2]), 0.03))
    parts += coil(0.6, 0.6, base_top + 0.01, 0.22, 3, "havnekran_tau")
    colliders = [box((0, 0.25, 0), (2.0, 1.1, 2.0)), box((0, 3.2, 0), (0.45, 4.8, 0.45))]
    groups = [{"name": "jib", "pivot": [0, 0, 0]}, {"name": "fall", "pivot": [tip[0], tip[1], tip[2]]},
              {"name": "hook", "pivot": [tip[0], CRANE_HOOK, tip[2]]}]
    # The fall and hook parts are drawn about their pivots; place them there.
    for item in parts:
        if item.get("group") == "fall":
            item["position"] = r3([a + b for a, b in zip(item["position"], tip)])
        elif item.get("group") == "hook":
            item["position"] = r3([a + b for a, b in zip(item["position"], (tip[0], CRANE_HOOK, tip[2]))])
    return parts, colliders, groups


def extra_pieces():
    """The Oil Cauldron, the Alarm Bell, the Harbour Crane, and every stone defence in black marble and grausten."""
    pieces = []
    for name, build, base, keep in (("oljegryte", oil_cauldron, "stone_wall_4x2", None), ("alarmklokke", alarm_bell, "wood_pole2", None),
                                    ("havnekran", harbour_crane, "stone_wall_4x2", None)):
        parts, colliders, groups = build()
        piece = {"name": name, "base": base, "parts": parts, "colliders": colliders, "snapPoints": [], "groups": groups,
                 "views": views(("front", 160, 12), ("side", 90, 10), ("angle", -140, 25))}
        pieces.append(piece)
    plain = {piece["name"]: piece for piece in stone_pieces()}
    for stone in ("marmor", "grausten"):
        pieces += [as_variant(piece, plain[piece["name"][:-len(stone) - 1]]) for piece in stone_pieces(stone)]
    return pieces


SPIKE = ("stake", [0.22, 0.2, 0.2])  # grausten's iron spikes on the merlons, which plain stone has not


def as_variant(variant, plain):
    """Writes a stone defence of another stone as the plain stone piece it is built on, with what differs: the texture
    of each mesh, tints swapped mesh by mesh, and parts only it has. The mod and the preview put it back together.
    Fails if the two differ in any other way, so a change to one cannot silently go missing from the other."""
    extra = [part for part in variant["parts"] if (part.get("mesh"), part.get("tint")) == SPIKE]
    parts = [part for part in variant["parts"] if (part.get("mesh"), part.get("tint")) != SPIKE]
    assert len(parts) == len(plain["parts"]), variant["name"]
    textures, tints = {}, {}
    for mine, theirs in zip(parts, plain["parts"]):
        mesh = mine.get("mesh")
        same = {key: value for key, value in mine.items() if key not in ("texture", "tint")}
        assert same == {key: value for key, value in theirs.items() if key not in ("texture", "tint")}, (variant["name"], mine, theirs)
        if mine.get("texture") != theirs.get("texture"):
            assert textures.setdefault(mesh, mine.get("texture")) == mine.get("texture") and theirs.get("texture") is None, (variant["name"], mesh)
        if mine.get("tint") != theirs.get("tint"):
            key = (mesh, tuple(theirs.get("tint") or ()))
            assert tints.setdefault(key, mine.get("tint")) == mine.get("tint"), (variant["name"], key)
    for field in ("base", "colliders", "snapPoints", "ladders", "groups", "keep", "hitArea"):
        assert variant.get(field) == plain.get(field), (variant["name"], field)
    return {"name": variant["name"], "of": plain["name"],
            "textures": [{"mesh": mesh, "texture": texture} for mesh, texture in sorted(textures.items())],
            "tints": [{"mesh": mesh, "from": list(old), "to": new} for (mesh, old), new in tints.items()],
            "extraParts": extra, "views": variant.get("views")}
