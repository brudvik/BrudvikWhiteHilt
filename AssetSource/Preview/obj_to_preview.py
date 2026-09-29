"""Converts a model built for the White Hilt asset bundle into the preview mesh format.

Usage: python obj_to_preview.py <foraging_dir> <out_dir> <name> [<name> ...]

Reads <foraging_dir>/<name>.obj and its <name>_albedo texture (the files build_foraging_bundle.ps1 feeds to Unity) and
writes <out_dir>/<name>.json plus <out_dir>/textures/<name>_albedo.png. Unity mirrors x when it imports an OBJ, so x is
negated here and the triangles flipped, to match what the game shows.
"""

import json
import pathlib
import shutil
import sys


def convert(folder, out, name):
    vertices, uvs, normals, faces = [], [], [], []
    for line in (folder / f"{name}.obj").read_text(encoding="ascii").splitlines():
        parts = line.split()
        if not parts:
            continue
        if parts[0] == "v":
            vertices.append((-float(parts[1]), float(parts[2]), float(parts[3])))
        elif parts[0] == "vt":
            uvs.append((float(parts[1]), float(parts[2])))
        elif parts[0] == "vn":
            normals.append((-float(parts[1]), float(parts[2]), float(parts[3])))
        elif parts[0] == "f":
            faces.append([tuple(int(i) - 1 for i in corner.split("/")) for corner in parts[1:4]])

    index, out_vertices, out_normals, out_uvs, triangles = {}, [], [], [], []
    for face in faces:
        for corner in reversed(face):
            if corner not in index:
                index[corner] = len(out_vertices) // 3
                out_vertices += [round(v, 5) for v in vertices[corner[0]]]
                out_uvs += [round(v, 5) for v in uvs[corner[1]]]
                out_normals += [round(v, 4) for v in normals[corner[2]]]
            triangles.append(index[corner])

    texture = f"{name}_albedo"
    source = next((folder / f"{texture}{ext}" for ext in (".png", ".jpg") if (folder / f"{texture}{ext}").exists()), None)
    if source is not None:
        (out / "textures").mkdir(parents=True, exist_ok=True)
        if source.suffix == ".png":
            shutil.copyfile(source, out / "textures" / f"{texture}.png")
        else:
            from PIL import Image
            Image.open(source).save(out / "textures" / f"{texture}.png")

    data = {"name": name, "parts": [{"texture": texture, "vertices": out_vertices, "normals": out_normals, "uvs": out_uvs, "triangles": triangles}]}
    (out / f"{name}.json").write_text(json.dumps(data), encoding="utf-8")
    print(f"{name}: {len(out_vertices) // 3} vertices, {len(triangles) // 3} triangles")


def main():
    folder, out = pathlib.Path(sys.argv[1]), pathlib.Path(sys.argv[2])
    for name in sys.argv[3:]:
        convert(folder, out, name)


if __name__ == "__main__":
    main()
