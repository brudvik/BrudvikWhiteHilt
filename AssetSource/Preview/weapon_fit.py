"""Overlays a weapon model (glb + weapon.json) on the vanilla weapon it replaces, in the item's attach space.

The hand is the origin of attach space. The vanilla meshes are drawn red, the new model blue, the origin as a black
cross and the vanilla trail (base/tip), if any, as green dots. Three orthographic views: X/Y, Z/Y and X/Z.

Usage: python weapon_fit.py <model.glb> <vanilla prefab> [<out.png>]
"""
import pathlib
import sys

import UnityPy
from PIL import Image, ImageDraw
from UnityPy.helpers.MeshHelper import MeshHandler

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent.parent))
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import convert_glb  # noqa: E402
from export_vanilla import apply, component, multiply, trs  # noqa: E402

ITEM_BUNDLE = r"C:\Program Files (x86)\Steam\steamapps\common\Valheim\valheim_Data\StreamingAssets\SoftRef\Bundles\c4210710"
VIEW = 360


def vanilla_attach(prefab):
    """Returns (vertices, triangles, named points) of every active mesh under the item's attach child, in attach space."""
    env = UnityPy.load(ITEM_BUNDLE)
    for obj in env.objects:
        if obj.type.name != "GameObject":
            continue
        game_object = obj.read()
        if game_object.m_Name != prefab or component(game_object, "Rigidbody") is None:
            continue
        root = component(game_object, "Transform")
        attach = next(c.read() for c in root.m_Children if c.read().m_GameObject.read().m_Name == "attach")
        vertices, triangles, points = [], [], {}

        def walk(transform, matrix, is_attach):
            child = transform.m_GameObject.read()
            local = matrix if is_attach else multiply(matrix, trs(transform))
            if child.m_Name in ("base", "tip"):
                points[child.m_Name] = apply(local, (0, 0, 0), 1)
            if not is_attach and not child.m_IsActive and child.m_Name not in ("equiped",):
                return
            mesh_filter = component(child, "MeshFilter")
            renderer = component(child, "MeshRenderer")
            if mesh_filter is not None and renderer is not None and mesh_filter.m_Mesh.path_id:
                handler = MeshHandler(mesh_filter.m_Mesh.read())
                handler.process()
                offset = len(vertices)
                vertices.extend(apply(local, v, 1) for v in handler.m_Vertices)
                for submesh in handler.get_triangles():
                    triangles.extend(tuple(offset + i for i in tri) for tri in submesh)
            for grandchild in transform.m_Children:
                walk(grandchild.read(), local, False)

        walk(attach, [[1, 0, 0, 0], [0, 1, 0, 0], [0, 0, 1, 0], [0, 0, 0, 1]], True)
        return vertices, triangles, points
    raise ValueError(f"{prefab} not found")


def model_attach(glb):
    """Returns (vertices, triangles) of the model mapped into attach space by its weapon.json."""
    import json

    gltf, binary = convert_glb.read_glb(glb)
    positions, normals, triangles = [], [], []
    for index, node in enumerate(gltf["nodes"]):
        if "mesh" not in node:
            continue
        matrix = convert_glb.world_matrix(gltf, index)
        for primitive in gltf["meshes"][node["mesh"]]["primitives"]:
            offset = len(positions)
            positions += [convert_glb.transform(matrix, p, 1.0) for p in convert_glb.read_accessor(gltf, binary, primitive["attributes"]["POSITION"])]
            normals += [(0.0, 1.0, 0.0)] * (len(positions) - offset)
            flat = [i[0] for i in convert_glb.read_accessor(gltf, binary, primitive["indices"])]
            triangles += [tuple(offset + i for i in flat[t:t + 3]) for t in range(0, len(flat), 3)]
    spec = json.loads(glb.with_suffix(".weapon.json").read_text())
    positions, _, scale = convert_glb.weapon_space(positions, normals, spec)
    return positions, triangles, scale


def bounds(vertices):
    return [(round(min(v[a] for v in vertices), 3), round(max(v[a] for v in vertices), 3)) for a in range(3)]


def main(glb, prefab, out):
    vanilla, vanilla_triangles, points = vanilla_attach(prefab)
    model, model_triangles, scale = model_attach(glb)
    print(f"vanilla {prefab}: {bounds(vanilla)} points {({k: [round(c, 3) for c in v] for k, v in points.items()})}")
    print(f"model {glb.stem}: {bounds(model)} (scale {scale:.4f})")

    everything = vanilla + model + [(0, 0, 0)]
    views = [(0, 1, "X right, Y up"), (2, 1, "Z right, Y up"), (0, 2, "X right, Z up")]
    sheet = Image.new("RGB", (VIEW * 3, VIEW + 16), "white")
    for column, (u, v, label) in enumerate(views):
        low = min(min(p[u] for p in everything), min(p[v] for p in everything))
        high = max(max(p[u] for p in everything), max(p[v] for p in everything))
        span = (high - low) * 1.1 or 1.0
        centre_u = (min(p[u] for p in everything) + max(p[u] for p in everything)) / 2
        centre_v = (min(p[v] for p in everything) + max(p[v] for p in everything)) / 2

        def pixel(p):
            return ((p[u] - centre_u) / span * VIEW + VIEW / 2, VIEW / 2 - (p[v] - centre_v) / span * VIEW)

        layers = []
        for vertices, triangles, colour in ((vanilla, vanilla_triangles, (220, 40, 40)), (model, model_triangles, (40, 80, 230))):
            layer = Image.new("L", (VIEW, VIEW), 0)
            draw = ImageDraw.Draw(layer)
            for triangle in triangles:
                draw.polygon([pixel(vertices[i]) for i in triangle], fill=255)
            layers.append((layer, colour))
        view = Image.new("RGB", (VIEW, VIEW), "white")
        for layer, colour in layers:
            tint = Image.new("RGB", (VIEW, VIEW), colour)
            view.paste(Image.blend(view, tint, 0.55), mask=layer)
        draw = ImageDraw.Draw(view)
        x, y = pixel((0, 0, 0))
        draw.line((x - 8, y, x + 8, y), fill="black", width=2)
        draw.line((x, y - 8, x, y + 8), fill="black", width=2)
        for point in points.values():
            px, py = pixel(point)
            draw.ellipse((px - 4, py - 4, px + 4, py + 4), fill=(20, 160, 20))
        sheet.paste(view, (column * VIEW, 16))
        ImageDraw.Draw(sheet).text((column * VIEW + 4, 2), label, fill="black")
    out.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(out)
    print(out)


if __name__ == "__main__":
    if len(sys.argv) < 3:
        sys.exit(__doc__)
    glb_path = pathlib.Path(sys.argv[1])
    default = pathlib.Path(__file__).resolve().parents[2] / "BrudvikWhiteHiltUnity" / "Preview" / "out" / f"{glb_path.stem}_fit.png"
    main(glb_path, sys.argv[2], pathlib.Path(sys.argv[3]) if len(sys.argv) > 3 else default)
