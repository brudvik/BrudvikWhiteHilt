"""Renders a preview of a model put together the way the mod does it in game, without starting the game.

A spec names a base model, fitted like VisualHelper.ReplaceMesh(size:) or by height, and props added on it like
VisualHelper.AddMesh (base position in the base model's units, where it is 1 high, a height and a yaw). The models are
the converted OBJs from build_foraging_bundle.ps1, read as Unity imports them (x negated). The result is rendered with
render_preview.ps1 into BrudvikWhiteHiltUnity/Preview/out/<name>.png.

Spec (JSON):
  { "name": "paintbench",
    "base": { "mesh": "painttable", "size": 2.2 },            # or "height": metres
    "props": [ { "mesh": "palette", "base": [0.05, 1, -0.7], "height": 0.022, "yaw": 20 } ],
    "views": [ { "label": "front", "yaw": 90, "pitch": 15 } ] }  # optional; "focus"/"radius"/"zoom" as in PreviewRender

Usage: python compose_preview.py <spec.json> [--no-render]
"""
import json
import math
import pathlib
import shutil
import subprocess
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
SOURCE = REPO / "BrudvikWhiteHiltUnity" / "Assets" / "Foraging"
MESHES = REPO / "BrudvikWhiteHiltUnity" / "Preview" / "vanilla"
LAYOUTS = REPO / "BrudvikWhiteHiltUnity" / "Preview"
DEFAULT_VIEWS = [
    {"label": "front", "yaw": 90, "pitch": 15},
    {"label": "angle", "yaw": 140, "pitch": 35},
    {"label": "top", "yaw": 90, "pitch": 80},
]


def load_obj(name):
    """Reads a converted OBJ as Unity imports it: x negated, winding flipped back. One vertex per corner."""
    positions, uvs, normals = [], [], []
    vertices, out_normals, out_uvs, triangles = [], [], [], []
    path = SOURCE / f"{name}.obj"
    if not path.exists():
        sys.exit(f"{path} not found; run AssetSource\\build_foraging_bundle.ps1 first")
    for line in path.read_text().splitlines():
        parts = line.split()
        if not parts:
            continue
        if parts[0] == "v":
            x, y, z = map(float, parts[1:4])
            positions.append((-x, y, z))
        elif parts[0] == "vt":
            uvs.append(tuple(map(float, parts[1:3])))
        elif parts[0] == "vn":
            x, y, z = map(float, parts[1:4])
            normals.append((-x, y, z))
        elif parts[0] == "f":
            corners = []
            for corner in parts[1:4]:
                v, t, n = (int(i) - 1 for i in corner.split("/"))
                vertices.append(positions[v])
                out_uvs.append(uvs[t])
                out_normals.append(normals[n])
                corners.append(len(vertices) - 1)
            triangles += [corners[0], corners[2], corners[1]]
    return vertices, out_normals, out_uvs, triangles


def bounds(vertices):
    low = [min(v[a] for v in vertices) for a in range(3)]
    high = [max(v[a] for v in vertices) for a in range(3)]
    return low, high


def write_mesh(name, vertices, normals, uvs, triangles):
    texture = f"{name}_albedo"
    (MESHES / "textures").mkdir(parents=True, exist_ok=True)
    source = next((SOURCE / f"{texture}{ext}" for ext in (".png", ".jpg") if (SOURCE / f"{texture}{ext}").exists()), None)
    if source is None or source.suffix != ".png":
        print(f"  {name}: no PNG texture, rendered untextured")
    else:
        shutil.copyfile(source, MESHES / "textures" / f"{texture}.png")
    data = {"parts": [{
        "texture": texture,
        "vertices": [c for v in vertices for c in v],
        "normals": [c for n in normals for c in n],
        "uvs": [c for uv in uvs for c in uv],
        "triangles": triangles,
    }]}
    (MESHES / f"preview_{name}.json").write_text(json.dumps(data))


def rotate_y(vector, yaw):
    """Unity's Quaternion.Euler(0, yaw, 0) applied to a vector."""
    radians = math.radians(yaw)
    x, y, z = vector
    return (x * math.cos(radians) + z * math.sin(radians), y, -x * math.sin(radians) + z * math.cos(radians))


def main(spec_path, render):
    spec = json.loads(pathlib.Path(spec_path).read_text())
    base = spec["base"]
    mesh = load_obj(base["mesh"])
    write_mesh(base["mesh"], *mesh)
    low, high = bounds(mesh[0])
    size = [h - l for l, h in zip(low, high)]
    # Like ReplaceMesh: the longest side to "size", or the height to "height"; the base stands at the origin.
    scale = base["size"] / max(size) if "size" in base else base["height"] / size[1]
    origin = [-(low[0] + high[0]) / 2 * scale, -low[1] * scale, -(low[2] + high[2]) / 2 * scale]
    parts = [{"mesh": f"preview_{base['mesh']}", "position": origin, "scale": [scale] * 3}]
    print(f"{base['mesh']}: {[round(s * scale, 3) for s in size]} m, 1 unit = {scale:.3f} m")

    for prop in spec.get("props", []):
        mesh = load_obj(prop["mesh"])
        write_mesh(prop["mesh"], *mesh)
        low, high = bounds(mesh[0])
        yaw = prop.get("yaw", 0.0)
        # Like AddMesh: the prop's base (bottom centre) goes to "base", scaled to "height", both in base-model units.
        prop_scale = prop["height"] / (high[1] - low[1])
        pivot = rotate_y(((low[0] + high[0]) / 2 * prop_scale, low[1] * prop_scale, (low[2] + high[2]) / 2 * prop_scale), yaw)
        local = [b - p for b, p in zip(prop["base"], pivot)]
        position = [origin[a] + local[a] * scale for a in range(3)]
        world_scale = prop_scale * scale
        parts.append({"mesh": f"preview_{prop['mesh']}", "position": position, "rotation": [0, yaw, 0], "scale": [world_scale] * 3})
        print(f"{prop['mesh']}: {[round((h - l) * world_scale, 3) for l, h in zip(low, high)]} m, base at "
              f"{[round(origin[a] + prop['base'][a] * scale, 3) for a in range(3)]}")

    layout = {"pieces": [{"name": spec["name"], "parts": parts, "views": spec.get("views", DEFAULT_VIEWS)}]}
    layout_path = LAYOUTS / f"{spec['name']}_layout.json"
    layout_path.write_text(json.dumps(layout, indent=1))
    if render:
        script = REPO / "AssetSource" / "Preview" / "render_preview.ps1"
        subprocess.run(["powershell", "-ExecutionPolicy", "Bypass", "-File", str(script), "-Layout", str(layout_path)], check=True)
        print(REPO / "BrudvikWhiteHiltUnity" / "Preview" / "out" / f"{spec['name']}.png")
    else:
        print(layout_path)


if __name__ == "__main__":
    if len(sys.argv) < 2:
        sys.exit(__doc__)
    main(sys.argv[1], "--no-render" not in sys.argv[2:])
