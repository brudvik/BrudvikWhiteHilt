"""Prints weapon.json "axes" that line a model up with a vanilla mesh child's local frame.

Give the glb direction that should match the child's local +X, +Y and +Z (e.g. "y" "z" "-x" or "[0.7,0.7,0]"). The child's rotation
under attach is read from the game, so the result also works for tilted vanilla meshes such as bows.

Usage: python frame_axes.py <prefab> <child name under attach> <glb for local X> <glb for local Y> <glb for local Z>
"""
import json
import pathlib
import sys

import UnityPy

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent.parent))
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import convert_glb  # noqa: E402
from export_vanilla import component  # noqa: E402
from weapon_fit import ITEM_BUNDLE  # noqa: E402


def rotation(q):
    x, y, z, w = q.x, q.y, q.z, q.w
    return [
        [1 - 2 * (y * y + z * z), 2 * (x * y - z * w), 2 * (x * z + y * w)],
        [2 * (x * y + z * w), 1 - 2 * (x * x + z * z), 2 * (y * z - x * w)],
        [2 * (x * z - y * w), 2 * (y * z + x * w), 1 - 2 * (x * x + y * y)],
    ]


def main(prefab, child_name, glb_axes):
    env = UnityPy.load(ITEM_BUNDLE)
    for obj in env.objects:
        if obj.type.name != "GameObject":
            continue
        game_object = obj.read()
        if game_object.m_Name != prefab or component(game_object, "Rigidbody") is None:
            continue
        root = component(game_object, "Transform")
        attach = next(c.read() for c in root.m_Children if c.read().m_GameObject.read().m_Name == "attach")
        child = next(c.read() for c in attach.m_Children if c.read().m_GameObject.read().m_Name == child_name)
        r = rotation(child.m_LocalRotation)
        g = [convert_glb.axis_vector(json.loads(a) if a.startswith("[") else a) for a in glb_axes]
        axes = [[round(sum(r[i][j] * g[j][k] for j in range(3)), 5) for k in range(3)] for i in range(3)]
        p, s = child.m_LocalPosition, child.m_LocalScale
        # The hand is the attach origin; in the child's local (mesh) space that is -R^T p / scale.
        hand = [round(-sum(r[j][i] * (p.x, p.y, p.z)[j] for j in range(3)) / s.x, 4) for i in range(3)]
        print(f'"axes": {axes},')
        print(f"child position {[round(p.x, 4), round(p.y, 4), round(p.z, 4)]} scale {round(s.x, 4)}, hand in mesh space {hand}")
        return
    raise ValueError(f"{prefab} not found")


if __name__ == "__main__":
    if len(sys.argv) != 6:
        sys.exit(__doc__)
    main(sys.argv[1], sys.argv[2], sys.argv[3:])
