"""Exports vanilla Valheim prefabs as baked meshes for offline preview renders.

Usage: python export_vanilla.py <output_dir> <prefab>[:<child/path>=<name>] ...

Each prefab becomes <output_dir>/<prefab>.json: every enabled MeshRenderer that is active up to the root, baked into
the root's space, one part per material, plus <output_dir>/textures/<texture>.png. With :<child/path>=<name>, only that
child is exported (even if it is inactive), in its own space, as <name>.json. The output contains game assets,
so it must stay out of git (the BrudvikWhiteHiltUnity project is git-ignored).
"""

import glob
import json
import math
import os
import pathlib
import sys

import UnityPy
from UnityPy.helpers.MeshHelper import MeshHandler

BUNDLES = r"C:\Program Files (x86)\Steam\steamapps\common\Valheim\valheim_Data\StreamingAssets\SoftRef\Bundles"
DEFAULT_RESOURCES = r"C:\Program Files (x86)\Steam\steamapps\common\Valheim\valheim_Data\Resources\unity default resources"
MAX_TEXTURE = 512


def component(game_object, type_name):
    for entry in game_object.m_Components:
        pointer = entry.component if hasattr(entry, "component") else entry
        if pointer.type.name == type_name:
            return pointer.read()
    return None


def trs(transform):
    p, q, s = transform.m_LocalPosition, transform.m_LocalRotation, transform.m_LocalScale
    x, y, z, w = q.x, q.y, q.z, q.w
    r = [
        [1 - 2 * (y * y + z * z), 2 * (x * y - z * w), 2 * (x * z + y * w)],
        [2 * (x * y + z * w), 1 - 2 * (x * x + z * z), 2 * (y * z - x * w)],
        [2 * (x * z - y * w), 2 * (y * z + x * w), 1 - 2 * (x * x + y * y)],
    ]
    scale = (s.x, s.y, s.z)
    return [[r[i][0] * scale[0], r[i][1] * scale[1], r[i][2] * scale[2], (p.x, p.y, p.z)[i]] for i in range(3)] + [[0, 0, 0, 1]]


def multiply(a, b):
    return [[sum(a[i][k] * b[k][j] for k in range(4)) for j in range(4)] for i in range(4)]


def apply(m, v, w):
    return [m[i][0] * v[0] + m[i][1] * v[1] + m[i][2] * v[2] + m[i][3] * w for i in range(3)]


def main_texture(material):
    for key, value in material.m_SavedProperties.m_TexEnvs:
        if key == "_MainTex" and value.m_Texture.path_id:
            return value.m_Texture.read()
    return None


def export(env, prefab, output, texture_dir, saved_textures, child_path=None, name=None):
    root = None
    for obj in env.objects:
        if obj.type.name == "GameObject" and obj.read().m_Name == prefab:
            transform = component(obj.read(), "Transform")
            if transform is not None and transform.m_Father.path_id == 0:
                root = transform
                break
    if root is None:
        print(f"{prefab}: not found")
        return

    forced = child_path is not None
    if forced:
        for part_name in child_path.split("/"):
            children = [child.read() for child in root.m_Children]
            root = next((child for child in children if child.m_GameObject.read().m_Name == part_name), None)
            if root is None:
                print(f"{prefab}: child {child_path} not found")
                return
    name = name or prefab

    parts = {}
    snap_points = []

    def walk(transform, matrix, is_root):
        game_object = transform.m_GameObject.read()
        if not is_root and not forced and not game_object.m_IsActive:
            if game_object.m_Name.startswith("$hud_snappoint") or game_object.m_Name.startswith("_snappoint"):
                p = apply(multiply(matrix, trs(transform)), (0, 0, 0), 1)
                snap_points.append([round(v, 3) for v in p])
            return
        local = matrix if is_root else multiply(matrix, trs(transform))
        renderer = component(game_object, "MeshRenderer")
        mesh_filter = component(game_object, "MeshFilter")
        if renderer is not None and (renderer.m_Enabled or forced) and mesh_filter is not None and mesh_filter.m_Mesh.path_id:
            mesh = mesh_filter.m_Mesh.read()
            handler = MeshHandler(mesh)
            handler.process()
            submeshes = handler.get_triangles()
            for index, triangles in enumerate(submeshes):
                if index >= len(renderer.m_Materials):
                    break
                material = renderer.m_Materials[index].read()
                if "snow" in material.m_Name.lower():
                    continue
                texture = main_texture(material)
                texture_name = texture.m_Name if texture is not None else ""
                if texture is not None and texture_name not in saved_textures:
                    image = texture.image
                    image.thumbnail((MAX_TEXTURE, MAX_TEXTURE))
                    image.save(texture_dir / f"{texture_name}.png")
                    saved_textures.add(texture_name)
                part = parts.setdefault(texture_name, {"texture": texture_name, "vertices": [], "normals": [], "uvs": [], "triangles": []})
                used = sorted({i for tri in triangles for i in tri})
                remap = {}
                for i in used:
                    remap[i] = len(part["vertices"]) // 3
                    part["vertices"] += [round(v, 4) for v in apply(local, handler.m_Vertices[i], 1)]
                    normal = apply(local, handler.m_Normals[i], 0) if handler.m_Normals else [0, 1, 0]
                    length = math.sqrt(sum(v * v for v in normal)) or 1
                    part["normals"] += [round(v / length, 4) for v in normal]
                    uv = handler.m_UV0[i] if handler.m_UV0 else (0, 0)
                    part["uvs"] += [round(uv[0], 4), round(uv[1], 4)]
                for tri in triangles:
                    part["triangles"] += [remap[i] for i in tri]
        for child in transform.m_Children:
            walk(child.read(), local, False)

    identity = [[1, 0, 0, 0], [0, 1, 0, 0], [0, 0, 1, 0], [0, 0, 0, 1]]
    walk(root, identity, True)

    data = {"name": name, "parts": list(parts.values()), "snapPoints": sum(snap_points, [])}
    (output / f"{name}.json").write_text(json.dumps(data), encoding="utf-8")
    vertices = [part["vertices"] for part in parts.values()]
    xs = [v for part in vertices for v in part[0::3]]
    ys = [v for part in vertices for v in part[1::3]]
    zs = [v for part in vertices for v in part[2::3]]
    bounds = f"x {min(xs):.2f}..{max(xs):.2f} y {min(ys):.2f}..{max(ys):.2f} z {min(zs):.2f}..{max(zs):.2f}" if xs else "empty"
    print(f"{name}: {len(parts)} parts, {len(xs)} vertices, {bounds}, snap points {snap_points}")


def main():
    output = pathlib.Path(sys.argv[1])
    texture_dir = output / "textures"
    texture_dir.mkdir(parents=True, exist_ok=True)
    prefabs = sys.argv[2:]
    contents = {path: open(path, "rb").read() for path in glob.glob(BUNDLES + r"\*") if os.path.isfile(path)}
    saved_textures = {path.stem for path in texture_dir.glob("*.png")}
    for argument in prefabs:
        prefab, _, rest = argument.partition(":")
        child_path, _, name = rest.partition("=")
        files = [path for path, data in contents.items() if prefab.encode() in data]
        if not files:
            print(f"{prefab}: no bundle contains the name")
            continue
        try:
            # Some prefabs point into Unity's built-in resources.
            export(UnityPy.load(*files, DEFAULT_RESOURCES), prefab, output, texture_dir, saved_textures, child_path or None, name or None)
        except Exception as error:
            print(f"{prefab}: failed: {error!r}")


if __name__ == "__main__":
    main()
