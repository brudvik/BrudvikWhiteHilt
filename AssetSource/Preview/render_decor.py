"""Renders one gallery image per Decor Hammer tab into docs/images/decor_<tab>.png, without starting the game.

Lays the tab's models out side by side at their size in the game (decor.json's heights), in rows of about ROW_WIDTH
metres, and renders them with render_preview.ps1 -Transparent like render_showcase.py. Pieces with a vanilla look are
left out, as their meshes are Valheim's and not part of the repository.

Needs AssetSource/build_foraging_bundle.ps1 to have run once (Unity project and converted OBJs).

Usage: python render_decor.py [tab ...]    # no names renders every tab
"""
import json
import pathlib
import subprocess
import sys

import compose_preview
import render_showcase

HERE = pathlib.Path(__file__).resolve().parent
REPO = HERE.parents[1]
CATALOG = REPO / "AssetSource" / "Decor" / "decor.json"
UNITY_ASSETS = REPO / "BrudvikWhiteHiltUnity" / "Assets"
PREVIEW = REPO / "BrudvikWhiteHiltUnity" / "Preview"
RAW = PREVIEW / "decor"
ROW_WIDTH = 5.0
GAP = 0.35
VIEW = {"yaw": 160, "pitch": 22}


def load(entry):
    """The entry's converted OBJ, scaled to its height in metres, with its footprint width and depth."""
    if "bundle" in entry:
        compose_preview.SOURCE = UNITY_ASSETS / "Foraging"
        name = entry["bundle"]
    else:
        compose_preview.SOURCE = UNITY_ASSETS / "Decor"
        name = f"decor_{entry['id']}"
    vertices, normals, uvs, triangles = compose_preview.load_obj(name)
    height = entry.get("height", 1.0)
    vertices = [(x * height, y * height, z * height) for x, y, z in vertices]
    compose_preview.write_mesh(name, vertices, normals, uvs, triangles)
    xs = [v[0] for v in vertices]
    zs = [v[2] for v in vertices]
    return name, (min(xs), max(xs)), (min(zs), max(zs))


def gallery(entries):
    """Places the models in rows from left to right, each row behind the one before."""
    parts, x, z, row_depth = [], 0.0, 0.0, 0.0
    for entry in entries:
        name, (min_x, max_x), (min_z, max_z) = load(entry)
        width, depth = max_x - min_x, max_z - min_z
        if x > 0 and x + width > ROW_WIDTH:
            x, z, row_depth = 0.0, z + row_depth + GAP, 0.0
        parts.append({"mesh": f"preview_{name}", "position": [x - min_x, 0, z - min_z]})
        x += width + GAP
        row_depth = max(row_depth, depth)
    return parts


def main(only):
    catalog = json.loads(CATALOG.read_text(encoding="utf-8"))
    tabs = []
    for entry in catalog["pieces"]:
        if entry["category"] not in tabs:
            tabs.append(entry["category"])
    tabs = [tab for tab in tabs if not only or tab.lower() in [name.lower() for name in only]]

    pieces = []
    for tab in tabs:
        models = [entry for entry in catalog["pieces"] if entry["category"] == tab and "vanilla" not in entry]
        if models:
            pieces.append({"name": f"decor_{tab.lower()}", "parts": gallery(models), "views": [VIEW]})

    layout = PREVIEW / "decor_layout.json"
    layout.write_text(json.dumps({"pieces": pieces}, indent=1), encoding="utf-8")
    RAW.mkdir(parents=True, exist_ok=True)
    subprocess.run(["powershell", "-ExecutionPolicy", "Bypass", "-File", str(HERE / "render_preview.ps1"), "-Layout", str(layout),
                    "-Out", str(RAW), "-Transparent", "-Size", "1400", "-Only", ",".join(piece["name"] for piece in pieces)],
                   check=True)
    render_showcase.DOCS_IMAGES.mkdir(parents=True, exist_ok=True)
    for piece in pieces:
        render_showcase.MAX_SIZE = 900
        render_showcase.crop(RAW / f"{piece['name']}.png", render_showcase.DOCS_IMAGES / f"{piece['name']}.png")


if __name__ == "__main__":
    main(sys.argv[1:])
