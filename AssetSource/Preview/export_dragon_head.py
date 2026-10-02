"""Exports the longship's carved dragon head mesh (VikingShip/ship/visual/unused/dragon_head) as preview JSON."""
import json
import pathlib
import sys

import UnityPy

BUNDLE = r"C:\Program Files (x86)\Steam\steamapps\common\Valheim\valheim_Data\StreamingAssets\SoftRef\Bundles\c4210710"
out = pathlib.Path(sys.argv[1])
env = UnityPy.load(BUNDLE)
for obj in env.objects:
    if obj.type.name != "Mesh":
        continue
    mesh = obj.read()
    if mesh.m_Name != "dragon_head":
        continue
    vertices, normals, uvs, faces = [], [], [], []
    for line in mesh.export().splitlines():
        parts = line.split()
        if not parts:
            continue
        if parts[0] == "v":
            vertices.append([float(v) for v in parts[1:4]])
        elif parts[0] == "vn":
            normals.append([float(v) for v in parts[1:4]])
        elif parts[0] == "vt":
            uvs.append([float(v) for v in parts[1:3]])
        elif parts[0] == "f":
            faces.append([int(p.split("/")[0]) - 1 for p in parts[1:4]])
    # UnityPy's OBJ export negates x; undo it so the mesh is in Unity space again.
    flat_v = [c for v in vertices for c in (-v[0], v[1], v[2])]
    flat_n = [c for n in normals for c in (-n[0], n[1], n[2])] if len(normals) == len(vertices) else []
    flat_uv = [c for uv in uvs for c in uv] if len(uvs) == len(vertices) else [0.0] * (2 * len(vertices))
    tris = [i for f in faces for i in (f[0], f[2], f[1])]
    out.write_text(json.dumps({"name": "dragon_head", "parts": [{"texture": "wood", "vertices": flat_v, "normals": flat_n, "uvs": flat_uv, "triangles": tris}]}))
    print("wrote", out, len(vertices), "vertices")
    break
