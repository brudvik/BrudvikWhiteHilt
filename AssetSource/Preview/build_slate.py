"""Draws the slate floor pieces (Pieces/Stonework/SlatePieces.cs) for the preview renderer.

Run: python build_slate.py, then render_preview.ps1 -Layout AssetSource/Preview/slate.json for images.

The mod builds the slabs at runtime as boxes with texture coordinates in metres (the slate texture covers 3 m, as on the
roof). This writes the same boxes as preview meshes into the git-ignored BrudvikWhiteHiltUnity/Preview/vanilla folder,
with the roof's slate texture, and slate.json, a layout with one piece per slab set.
"""

import json
import math
import pathlib

from PIL import Image

HERE = pathlib.Path(__file__).resolve().parent
ROOT = HERE.parents[1]
MESHES = ROOT / "BrudvikWhiteHiltUnity" / "Preview" / "vanilla"
TEXTURE = "roof_slate_albedo"
METRES = 3.0

# The same slabs as SlatePieces.cs: centre, size, yaw.
STEPS = [((0, (0.25 * (i + 1) - 0.13) / 2, 0.75 - 0.5 * i), (2, 0.25 * (i + 1) + 0.13, 0.5), 0) for i in range(4)]
PIECES = {
    "slatefloor": [((0, 0.02, 0), (2, 0.12, 2), 0)],
    "slatefloor1x1": [((0, 0.02, 0), (1, 0.12, 1), 0)],
    "slatesteps": STEPS,
    "slatepath": [((-0.5, 0.02, -0.5), (0.82, 0.08, 0.74), 4), ((0.48, 0.02, -0.55), (0.88, 0.08, 0.68), -7),
                  ((-0.47, 0.02, 0.42), (0.84, 0.08, 0.9), -3), ((0.5, 0.02, 0.46), (0.76, 0.08, 0.84), 8)],
}

FACES = [  # normal, two in-face axes
    ((0, 1, 0), (1, 0, 0), (0, 0, 1)), ((0, -1, 0), (1, 0, 0), (0, 0, -1)),
    ((1, 0, 0), (0, 0, -1), (0, 1, 0)), ((-1, 0, 0), (0, 0, 1), (0, 1, 0)),
    ((0, 0, 1), (1, 0, 0), (0, 1, 0)), ((0, 0, -1), (-1, 0, 0), (0, 1, 0)),
]


def slab_mesh(slabs):
    vertices, normals, uvs, triangles = [], [], [], []
    for centre, size, yaw in slabs:
        a = math.radians(yaw)

        def turn(v):
            return (v[0] * math.cos(a) + v[2] * math.sin(a), v[1], -v[0] * math.sin(a) + v[2] * math.cos(a))

        for normal, u, v in FACES:
            start = len(vertices) // 3
            for su, sv in ((-1, -1), (1, -1), (1, 1), (-1, 1)):
                local = [0.5 * size[k] * (normal[k] + su * u[k] + sv * v[k]) for k in range(3)]
                world = [c + t for c, t in zip(centre, turn(local))]
                vertices += world
                normals += turn(normal)
                if abs(normal[1]) > 0.5:
                    uv = (world[0], world[2])
                elif abs(normal[0]) > 0.5:
                    uv = (local[2] + centre[2], world[1])
                else:
                    uv = (local[0] + centre[0], world[1])
                uvs += [uv[0] / METRES, uv[1] / METRES]
            # Unity winds front faces clockwise seen from outside, in its left-handed space: the corners already run
            # clockwise when u x v is the normal.
            cross = (u[1] * v[2] - u[2] * v[1], u[2] * v[0] - u[0] * v[2], u[0] * v[1] - u[1] * v[0])
            if cross == normal:
                triangles += [start, start + 1, start + 2, start, start + 2, start + 3]
            else:
                triangles += [start, start + 2, start + 1, start, start + 3, start + 2]
    return {"parts": [{"texture": TEXTURE, "vertices": vertices, "normals": normals, "uvs": uvs, "triangles": triangles}]}


def main():
    (MESHES / "textures").mkdir(parents=True, exist_ok=True)
    Image.open(ROOT / "AssetSource" / "Textures" / f"{TEXTURE}.jpg").convert("RGB").save(MESHES / "textures" / f"{TEXTURE}.png")
    layout = []
    for name, slabs in PIECES.items():
        (MESHES / f"preview_{name}.json").write_text(json.dumps(slab_mesh(slabs)), encoding="utf-8")
        layout.append({"name": name, "parts": [{"mesh": f"preview_{name}", "position": [0, 0, 0]}],
                       "views": [{"label": "angle", "yaw": 150, "pitch": 35}]})
    (HERE / "slate.json").write_text(json.dumps({"pieces": layout}, indent=1), encoding="utf-8")
    print(f"wrote {len(PIECES)} slate pieces")


if __name__ == "__main__":
    main()
