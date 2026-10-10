"""Finds the candles of a decor model, for decor.json's "flames".

Takes the vertices of Models/<id>.glb whose height lies between two shares of the model's height, groups them by where
they stand (x and z), and prints each group's middle as shares of the model's bounds, x and z as in the glb, with the
heights the group spans. A band just below the top of the candles gives one group per candle.

Usage: python find_flames.py <id> <from> <to> [gap]    # gap: how far apart groups are, as a share of the height (0.04)
"""
import pathlib
import sys

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent))
sys.path.insert(0, str(HERE.parent / "Tools"))
import convert_glb  # noqa: E402
import glb_info  # noqa: E402


def groups(points, gap):
    """Groups points whose xz distance to a member is below gap."""
    found = []
    for point in points:
        near = [group for group in found if any((point[0] - q[0]) ** 2 + (point[2] - q[2]) ** 2 < gap * gap for q in group)]
        merged = [point]
        for group in near:
            merged += group
            found.remove(group)
        found.append(merged)
    return found


def main(name, low, high, gap):
    gltf, binary = convert_glb.read_glb(HERE / "Models" / f"{name}.glb")
    points = [tuple(p) for *_, part, _ in glb_info.parts(gltf, binary) for p in part]
    minimum = [min(p[axis] for p in points) for axis in range(3)]
    size = [max(p[axis] for p in points) - minimum[axis] for axis in range(3)]

    def share(point, axis):
        return (point[axis] - minimum[axis]) / size[axis] if size[axis] else 0.5

    band = [p for p in points if low <= share(p, 1) <= high]
    print(f"{name}: {size[0]:.3f} x {size[1]:.3f} x {size[2]:.3f} in the glb, {len(band)} vertices in the band")
    for group in sorted(groups(band, gap * size[1]), key=len, reverse=True):
        middle = [sum(p[axis] for p in group) / len(group) for axis in range(3)]
        heights = [share(p, 1) for p in group]
        print(f"  [{share(middle, 0):.3f}, {max(heights):.3f}, {share(middle, 2):.3f}]  "
              f"{len(group)} vertices, from {min(heights):.3f} to {max(heights):.3f}")


if __name__ == "__main__":
    if len(sys.argv) < 4:
        sys.exit(__doc__)
    main(sys.argv[1], float(sys.argv[2]), float(sys.argv[3]), float(sys.argv[4]) if len(sys.argv) > 4 else 0.04)
