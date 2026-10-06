"""Proposes a <name>.weapon.json for a held model: axes, grip and scale, worked out from the mesh itself.

Z (the weapon's length, towards the tip or head) is the model's longest axis, pointing at its wider end: the head of
an axe, mace or polearm. A sword's guard is wider than its blade, so pass --flip-z for swords. X is the widest spread of
the head, its heavier side (an axe's blade) towards +X; --flip-x turns it round. Y completes a frame with determinant
-1, as convert_glb.py wants. The grip is the middle of the mesh at the given fraction of the length from the butt, and
the scale makes the model the given length in metres.

--match <vanilla prefab> tilts the frame like that weapon's mesh under its attach child, for vanilla weapons held
slanted (the atgeirs): the model then lies along the same line, its head on the same side.

Check the result with AssetSource/Preview/weapon_fit.py before building.

Usage:
  python weapon_frame.py <model.glb> <length in m> <grip fraction from the butt> [--flip-z] [--flip-x] [--match <prefab>]
  e.g. python weapon_frame.py AssetSource/Models/whhalberd.glb 2.8 0.3 --flip-x --match AtgeirBlackmetal
Prints the JSON; redirect it into AssetSource/Models/<name>.weapon.json.
"""
import argparse
import json
import pathlib
import sys

import numpy as np

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1]))
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / "Preview"))
import convert_glb  # noqa: E402
import glb_info  # noqa: E402

HEAD_SHARE = 0.25
GRIP_SLICE = 0.03


def model_points(path):
    """Every vertex of the model, in glb space with node transforms applied."""
    gltf, binary = convert_glb.read_glb(path)
    return np.array([point for *_, points, _ in glb_info.parts(gltf, binary) for point in points])


def spread(points, centre, axis):
    """How far the points reach from the axis through the centre."""
    if len(points) == 0:
        return 0.0
    offset = points - centre
    offset -= np.outer(offset @ axis, axis)
    return float(np.linalg.norm(offset, axis=1).max())


def model_frame(points, flip_z, flip_x):
    """The model's frame as rows X, Y, Z in glb space, and the extent of the points along Z from the centre."""
    centre = points.mean(axis=0)
    _, vectors = np.linalg.eigh(np.cov((points - centre).T))
    z = vectors[:, 2]
    along = (points - centre) @ z
    low, high = along.min(), along.max()
    head_end = along > high - HEAD_SHARE * (high - low)
    butt_end = along < low + HEAD_SHARE * (high - low)
    if spread(points[butt_end], centre, z) > spread(points[head_end], centre, z):
        z = -z
    if flip_z:
        z = -z

    along = (points - centre) @ z
    low, high = along.min(), along.max()
    head = points[along > high - HEAD_SHARE * (high - low)] - centre
    across = head - np.outer(head @ z, z)
    _, vectors = np.linalg.eigh(np.cov(across.T))
    x = vectors[:, 2] - (vectors[:, 2] @ z) * z
    x /= np.linalg.norm(x)
    if (across @ x).mean() < 0:
        x = -x
    if flip_x:
        x = -x

    y = np.cross(z, x)
    if np.linalg.det(np.array([x, y, z])) > 0:
        y = -y
    return np.array([x, y, z]), centre, along, low, high


def vanilla_tilt(prefab):
    """The rotation that lays a straight weapon frame along a vanilla weapon's mesh in its attach space."""
    import weapon_fit

    vertices, _, _ = weapon_fit.vanilla_attach(prefab)
    points = np.array(vertices)
    centre = points.mean(axis=0)
    _, vectors = np.linalg.eigh(np.cov((points - centre).T))
    length = vectors[:, 2]
    # The head reaches furthest from the hand, which is the origin of attach space.
    furthest = points[np.argmax(np.linalg.norm(points, axis=1))]
    if (furthest - centre) @ length < 0:
        length = -length

    along = (points - centre) @ length
    head = points[along > along.max() - HEAD_SHARE * (along.max() - along.min())] - centre
    side = (head - np.outer(head @ length, length)).mean(axis=0)
    side -= (side @ length) * length
    side /= np.linalg.norm(side)
    rotation = np.column_stack([side, np.cross(length, side), length])
    if np.linalg.det(rotation) < 0:
        rotation[:, 1] = -rotation[:, 1]
    return rotation


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("model", type=pathlib.Path)
    parser.add_argument("length", type=float, help="length of the model in metres")
    parser.add_argument("grip", type=float, help="where the hand holds it, as a fraction of the length from the butt")
    parser.add_argument("--flip-z", action="store_true", help="the head is the narrower end (swords)")
    parser.add_argument("--flip-x", action="store_true", help="turn the head to the other side")
    parser.add_argument("--match", metavar="PREFAB", help="tilt like this vanilla weapon's mesh")
    args = parser.parse_args()

    points = model_points(args.model)
    axes, centre, along, low, high = model_frame(points, args.flip_z, args.flip_x)
    grip_at = low + args.grip * (high - low)
    near = points[np.abs(along - grip_at) < GRIP_SLICE * (high - low)]
    grip = near.mean(axis=0) if len(near) else centre + grip_at * axes[2]
    if args.match:
        axes = vanilla_tilt(args.match) @ axes

    rounded = lambda vector: [round(float(value), 5) for value in vector]  # noqa: E731
    print(json.dumps({"axes": [rounded(axis) for axis in axes], "grip": rounded(grip),
                      "scale": round(args.length / (high - low), 6)}))


if __name__ == "__main__":
    main()
