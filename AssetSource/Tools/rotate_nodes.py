"""Rotates mesh parts of a glb about a pivot, before conversion, e.g. a flail's chain and ball swung out along the
haft instead of hanging down (whflail.glb: x 75 about the ring at the end of the haft).

The pivot is in glb world space, as glb_info.py prints part bounds; positive degrees turn by the right-hand rule about
the axis. The nodes keep their parents: each gets a matrix that puts it where the rotation takes it. The file is
changed in place.

Usage:
  python rotate_nodes.py <model.glb> <axis x|y|z> <degrees> <px,py,pz> <node name> [<node name> ...]
  e.g. python rotate_nodes.py AssetSource/Models/whflail.glb x 75 -5.895,-7.41,-3.57 "Chain_low_Default OBJ_0"
"""
import argparse
import math
import pathlib
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1]))
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import convert_glb  # noqa: E402
import glb_edit  # noqa: E402

IDENTITY = [[1, 0, 0, 0], [0, 1, 0, 0], [0, 0, 1, 0], [0, 0, 0, 1]]


def translation(offset):
    return [[1, 0, 0, offset[0]], [0, 1, 0, offset[1]], [0, 0, 1, offset[2]], [0, 0, 0, 1]]


def rotation(axis, degrees):
    c, s = math.cos(math.radians(degrees)), math.sin(math.radians(degrees))
    turn = {"x": [[1, 0, 0], [0, c, -s], [0, s, c]],
            "y": [[c, 0, s], [0, 1, 0], [-s, 0, c]],
            "z": [[c, -s, 0], [s, c, 0], [0, 0, 1]]}[axis]
    return [row + [0] for row in turn] + [[0, 0, 0, 1]]


def inverse(matrix):
    """Inverse of an affine 4x4 matrix (any invertible 3x3 part, so scaled nodes work too)."""
    a = [row[:3] for row in matrix[:3]]
    determinant = (a[0][0] * (a[1][1] * a[2][2] - a[1][2] * a[2][1]) - a[0][1] * (a[1][0] * a[2][2] - a[1][2] * a[2][0])
                   + a[0][2] * (a[1][0] * a[2][1] - a[1][1] * a[2][0]))
    part = [[(a[(j + 1) % 3][(i + 1) % 3] * a[(j + 2) % 3][(i + 2) % 3]
              - a[(j + 1) % 3][(i + 2) % 3] * a[(j + 2) % 3][(i + 1) % 3]) / determinant for j in range(3)] for i in range(3)]
    shift = [-sum(part[i][k] * matrix[k][3] for k in range(3)) for i in range(3)]
    return [part[i] + [shift[i]] for i in range(3)] + [[0, 0, 0, 1]]


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("model", type=pathlib.Path)
    parser.add_argument("axis", choices=["x", "y", "z"])
    parser.add_argument("degrees", type=float)
    parser.add_argument("pivot", help="px,py,pz in glb world space")
    parser.add_argument("nodes", nargs="+", help="node names, as glb_info.py prints them")
    args = parser.parse_args()

    pivot = [float(value) for value in args.pivot.split(",")]
    about_pivot = convert_glb.multiply(translation(pivot),
                                       convert_glb.multiply(rotation(args.axis, args.degrees), translation([-v for v in pivot])))
    gltf, binary = convert_glb.read_glb(args.model)
    parents = {child: index for index, node in enumerate(gltf["nodes"]) for child in node.get("children", [])}
    found = set()
    for index, node in enumerate(gltf["nodes"]):
        if node.get("name") not in args.nodes:
            continue
        world = convert_glb.world_matrix(gltf, index)
        parent = convert_glb.world_matrix(gltf, parents[index]) if index in parents else IDENTITY
        local = convert_glb.multiply(inverse(parent), convert_glb.multiply(about_pivot, world))
        for key in ("translation", "rotation", "scale"):
            node.pop(key, None)
        node["matrix"] = [local[row][column] for column in range(4) for row in range(4)]
        found.add(node["name"])
        print(f"rotated {node['name']}")

    missing = set(args.nodes) - found
    if missing:
        sys.exit(f"no node named {', '.join(sorted(missing))}; nothing written")
    glb_edit.write(args.model, gltf, binary)


if __name__ == "__main__":
    main()
