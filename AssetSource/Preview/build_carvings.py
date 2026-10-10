"""Carved interlace for walls, posts and doorways: the friezes made by AssetSource/Carvings/build_friezes.py.

build_defenses.py adds carving_pieces() to the layout it writes; run that script, not this one. The mod builds them in
Pieces/Carvings.

The frieze models come out of convert_glb.py 1 m high and FRIEZE_LONG long, their carving facing +z and centred on z 0;
scaled, they are 2 m long and 0.3 m high. A frieze hangs on the face of a wall: its back at z 0, its carving out
towards +z. A band is the same frieze standing on end, for a post or a door frame.
"""

import random

from build_defenses import box, part, r3, views
from build_log_house import TAR_DOOR_TINT, door_frame, door_leaf, piece_entry

PATTERNS = ["flette", "tau", "ringkjede", "slyng"]
FRIEZE_LONG = 6.838  # the converted model's length at a height of 1
HEIGHT = 0.3
DEPTH = 0.072  # the carving's full depth once scaled
SCALE = (2.0 / FRIEZE_LONG, HEIGHT, HEIGHT)


def frieze(pattern):
    """A frieze 2 m long and 0.3 m high on the face of a wall, from y 0 up."""
    parts = [part(f"carve_{pattern}", (0, 0, DEPTH / 2), scale=SCALE)]
    colliders = [box((0, HEIGHT / 2, DEPTH / 2), (2.0, HEIGHT, DEPTH))]
    snaps = [(x, y, 0) for x in (-1, 1) for y in (0, HEIGHT)]
    return parts, colliders, snaps


def band(pattern, x=0.0, y0=0.0, z=0.0, length=2.0):
    """The frieze on end, 0.3 m wide and length high from y0, centred on x, its back at z."""
    scale = (length / FRIEZE_LONG, HEIGHT, HEIGHT)
    return part(f"carve_{pattern}", (x + HEIGHT / 2, y0 + length / 2, z + DEPTH / 2), (0, 0, 90), scale)


def post_band(pattern):
    parts = [band(pattern)]
    colliders = [box((0, 1.0, DEPTH / 2), (HEIGHT, 2.0, DEPTH))]
    snaps = [(0, 0, 0), (0, 2.0, 0)]
    return parts, colliders, snaps


def carved_portal():
    """A doorway 2 m wide and 3 m high between broad posts carved with Urnes loops up both faces, a ring chain over
    the door, and a tarred door 1.2 m wide and 2.6 m high: carving as round the stave church portals."""
    rng = random.Random("utskaret_portal")
    post = 0.4
    parts, colliders = door_frame(rng, post=post, height=3.0)
    face = 0.29
    for z, flip in ((face, 0), (-face, 180)):
        for x in (-1 + post / 2, 1 - post / 2):
            item = band("slyng", x, 0.05, 0, 2.9)
            if flip:
                item = dict(item, position=r3((-item["position"][0] + 2 * x, item["position"][1], -face - DEPTH / 2)),
                            rotation=[0, 180, 90])
            else:
                item = dict(item, position=r3((item["position"][0], item["position"][1], face + DEPTH / 2)))
            parts.append(item)
        lintel = part("carve_ringkjede", (0, 2.65, z + (DEPTH / 2 if not flip else -DEPTH / 2)), (0, flip, 0),
                      (1.2 / FRIEZE_LONG, HEIGHT, HEIGHT))
        parts.append(lintel)
    leaf, collider = door_leaf("leaf", -0.6, 0.6, rng, TAR_DOOR_TINT, height=2.6)
    parts += leaf
    colliders.append(collider)
    colliders.append(box((0, 2.8, 0), (1.2, 0.4, 0.58)))
    groups = [{"name": "leaf", "pivot": [0.6, 0, 0]}]
    snaps = [(x, y, 0) for x in (-1, 1) for y in (0, 2.0, 3.0)]
    return parts, colliders, snaps, groups


def carving_pieces():
    near = views(("front", 180, 10), ("angle", 140, 25))
    pieces = []
    for pattern in PATTERNS:
        pieces.append(piece_entry(f"frise_{pattern}", "wood_pole2", *frieze(pattern), views=near))
        pieces.append(piece_entry(f"stolpebord_{pattern}", "wood_pole2", *post_band(pattern), views=near))
    parts, colliders, snaps, groups = carved_portal()
    pieces.append(piece_entry("utskaret_portal", "wood_door", parts, colliders, snaps, groups=groups, keep=["door"],
                              views=views(("outside", 180, 10), ("inside", 0, 10), ("angle", 140, 25))))
    pieces.append({"name": "friser", "parts": [
        {"piece": f"frise_{p}", "position": [0, 0.45 * i, 0]} for i, p in enumerate(PATTERNS)
    ] + [{"piece": "utskaret_portal", "position": [3, 0, 0]}], "views": views(("front", 180, 10), ("angle", 150, 20))})
    return pieces
