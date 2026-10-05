"""Renders the model images for docs/ on a transparent background, without starting the game.

Reads showcase.json, builds one layout from it (defence pieces from defenses.json, bundle models from the converted
OBJs, composed pieces from compose_preview.py specs), renders it with render_preview.ps1 -Transparent and crops each
image to the model into docs/images/<image>.png.

Needs AssetSource/build_foraging_bundle.ps1 to have run once (Unity project and converted OBJs).

Usage: python render_showcase.py [image ...]    # no names renders all
"""
import json
import pathlib
import subprocess
import sys

from PIL import Image

import compose_preview
import palette_surfaces

HERE = pathlib.Path(__file__).resolve().parent
REPO = HERE.parents[1]
PREVIEW = REPO / "BrudvikWhiteHiltUnity" / "Preview"
RAW = PREVIEW / "showcase"
DOCS_IMAGES = REPO / "docs" / "images"
RENDER_SIZE = 1024
MAX_SIZE = 480
PADDING = 0.04


def main(only):
    spec = json.loads((HERE / "showcase.json").read_text(encoding="utf-8-sig"))
    defenses = json.loads((HERE / "defenses.json").read_text(encoding="utf-8"))
    entries = [e for e in spec["images"] if not only or e["image"] in only]
    if not entries:
        sys.exit(f"No showcase entries match {only}")

    # Every defence piece stays in the layout as a template, so showcase pieces can refer to it.
    pieces = [dict(piece, template=True) for piece in defenses["pieces"]]
    converted = set()
    for entry in entries:
        view = entry.get("view")
        if "piece" in entry:
            parts = [{"piece": entry["piece"]}]
        elif "models" in entry:
            parts = []
            for model in entry["models"]:
                if model not in converted:
                    compose_preview.write_mesh(model, *compose_preview.load_obj(model))
                    converted.add(model)
                parts.append({"mesh": f"preview_{model}"})
        elif "compose" in entry:
            compose_preview.main(HERE / entry["compose"], render=False)
            name = json.loads((HERE / entry["compose"]).read_text(encoding="utf-8"))["name"]
            parts = json.loads((PREVIEW / f"{name}_layout.json").read_text())["pieces"][0]["parts"]
        elif "manifest" in entry:
            manifest = json.loads((HERE / entry["manifest"]).read_text(encoding="utf-8"))
            parts = []
            for model in manifest["parts"]:
                name = model["mesh"]
                if name not in converted:
                    compose_preview.write_mesh(name, *compose_preview.load_obj(name))
                    converted.add(name)
                if model.get("rudder") and "rudder" in manifest:
                    parts.append({"mesh": f"preview_{name}", "position": manifest["rudder"]["hinge"],
                                  "rotation": [manifest["rudder"]["tilt"], 0, 0]})
                else:
                    parts.append({"mesh": f"preview_{name}", "position": model["pivot"]})
        elif "parts" in entry:
            parts = entry["parts"]
        else:
            sys.exit(f"{entry['image']}: needs 'piece', 'models', 'compose', 'manifest' or 'parts'")
        # The wall drawer and the ship workshops as the mod surfaces them, in vanilla wood, iron and stone.
        parts = palette_surfaces.surface(parts)
        pieces.append({"name": entry["image"], "parts": parts, "views": [view or spec["defaultView"]]})

    layout = PREVIEW / "showcase_layout.json"
    layout.write_text(json.dumps({"pieces": pieces}, indent=1), encoding="utf-8")
    RAW.mkdir(parents=True, exist_ok=True)
    script = HERE / "render_preview.ps1"
    subprocess.run(["powershell", "-ExecutionPolicy", "Bypass", "-File", str(script), "-Layout", str(layout),
                    "-Out", str(RAW), "-Transparent", "-Size", str(RENDER_SIZE),
                    "-Only", ",".join(e["image"] for e in entries)], check=True)

    DOCS_IMAGES.mkdir(parents=True, exist_ok=True)
    for entry in entries:
        crop(RAW / f"{entry['image']}.png", DOCS_IMAGES / f"{entry['image']}.png")


def crop(source, target):
    """Crops to the visible model with a little padding and scales it down to fit MAX_SIZE."""
    image = Image.open(source).convert("RGBA")
    box = image.getchannel("A").point(lambda a: 255 if a > 8 else 0).getbbox()
    if box is None:
        print(f"  {source.name}: empty render, skipped")
        return
    pad = int(max(box[2] - box[0], box[3] - box[1]) * PADDING)
    box = (max(0, box[0] - pad), max(0, box[1] - pad), min(image.width, box[2] + pad), min(image.height, box[3] + pad))
    image = image.crop(box)
    image.thumbnail((MAX_SIZE, MAX_SIZE), Image.LANCZOS)
    image.save(target, optimize=True)
    print(f"  {target.relative_to(REPO)}: {image.width}x{image.height}, {target.stat().st_size // 1024} KB")


if __name__ == "__main__":
    main(sys.argv[1:])
