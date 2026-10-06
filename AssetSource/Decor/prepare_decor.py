"""Prepares the models of the Decor Hammer: AssetSource/Decor/Models/<id>.glb for every entry in decor.json with a
"source", ready for build_foraging_bundle.ps1.

Sources:
  "polyhaven:<asset>"  downloaded from Poly Haven (CC0) at 1k into Download/<asset>/ (git-ignored). Poly Haven keeps a
                       plant's transparency in a separate alpha map, so it is merged into the colour texture first.
  "file:<name>.glb"    read from the folder given with --sources (default D:/3dmodels), for models downloaded by hand,
                       such as CC BY models from Sketchfab.

Blender (blender_prepare.py) then keeps only the entry's "objects" (node names; the whole file without them), joins
them, decimates to about "tris" triangles, shrinks the textures to 512 px and exports one glb. Entries without a
"height" get the model's real height in metres written back into decor.json, as Poly Haven models are to scale.

Usage: python prepare_decor.py [--only <id> ...] [--force] [--sources <folder>] [--blender <blender.exe>]
"""
import argparse
import json
import pathlib
import subprocess
import sys
import urllib.request

HERE = pathlib.Path(__file__).resolve().parent
CATALOG = HERE / "decor.json"
MODELS = HERE / "Models"
DOWNLOAD = HERE / "Download"
USER_AGENT = {"User-Agent": "BrudvikWhiteHilt-asset-pipeline/1.0 (github.com/brudvik/BrudvikWhiteHilt)"}


def fetch(url):
    return urllib.request.urlopen(urllib.request.Request(url, headers=USER_AGENT), timeout=120).read()


def download_polyhaven(asset):
    """Downloads the 1k glTF of a Poly Haven model with its textures; returns the .gltf path."""
    folder = DOWNLOAD / asset
    files = json.loads(fetch(f"https://api.polyhaven.com/files/{asset}"))
    gltf = files["gltf"]["1k"]["gltf"]
    target = folder / pathlib.Path(gltf["url"]).name
    if not target.exists():
        folder.mkdir(parents=True, exist_ok=True)
        for path, include in gltf["include"].items():
            (folder / path).parent.mkdir(parents=True, exist_ok=True)
            (folder / path).write_bytes(fetch(include["url"]))
        target.write_bytes(fetch(gltf["url"]))
        merge_alpha(files, folder, target)
    return target


def merge_alpha(files, folder, gltf_path):
    """Puts Poly Haven's separate alpha map into the colour texture, which glTF's alpha mask reads."""
    alpha = files.get("Alpha", {}).get("1k")
    if not alpha:
        return
    from PIL import Image

    gltf = json.loads(gltf_path.read_text(encoding="utf-8"))
    masked = {material["pbrMetallicRoughness"]["baseColorTexture"]["index"]
              for material in gltf.get("materials", [])
              if material.get("alphaMode") in ("MASK", "BLEND") and "baseColorTexture" in material.get("pbrMetallicRoughness", {})}
    if not masked:
        return
    entry = alpha.get("png") or alpha.get("jpg")
    mask = Image.open(__import__("io").BytesIO(fetch(entry["url"]))).convert("L")
    for texture in masked:
        image = gltf["images"][gltf["textures"][texture]["source"]]
        colour_path = folder / image["uri"]
        colour = Image.open(colour_path).convert("RGB")
        merged = colour.copy()
        merged.putalpha(mask.resize(colour.size))
        png = colour_path.with_suffix(".png")
        merged.save(png)
        image["uri"] = png.relative_to(folder).as_posix()
        image["mimeType"] = "image/png"
    gltf_path.write_text(json.dumps(gltf), encoding="utf-8")


def find_blender(given):
    if given:
        return given
    found = sorted(pathlib.Path(r"C:\Program Files\Blender Foundation").glob("*/blender.exe"))
    if not found:
        sys.exit("Blender not found; pass --blender")
    return str(found[-1])


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--only", nargs="*")
    parser.add_argument("--force", action="store_true")
    parser.add_argument("--sources", default="D:/3dmodels")
    parser.add_argument("--blender")
    args = parser.parse_args()

    catalog = json.loads(CATALOG.read_text(encoding="utf-8"))
    blender = find_blender(args.blender)
    MODELS.mkdir(exist_ok=True)
    changed = False
    for entry in catalog["pieces"]:
        if "source" not in entry or (args.only and entry["id"] not in args.only):
            continue
        output = MODELS / f"{entry['id']}.glb"
        if output.exists() and not args.force and "height" in entry:
            continue

        kind, _, name = entry["source"].partition(":")
        if kind == "polyhaven":
            source = download_polyhaven(name)
        elif kind == "file":
            source = pathlib.Path(args.sources) / name
        else:
            sys.exit(f"{entry['id']}: unknown source {entry['source']}")

        spec = {"objects": entry.get("objects", []), "tris": entry.get("tris", 3000), "texture": entry.get("texture", 512)}
        result = subprocess.run(
            [blender, "-b", "--factory-startup", "--python-exit-code", "1", "--python", str(HERE / "blender_prepare.py"),
             "--", str(source), str(output), json.dumps(spec)],
            capture_output=True, text=True)
        report = [line for line in result.stdout.splitlines() if line.startswith("[decor]")]
        if result.returncode != 0 or not report:
            print(result.stdout[-3000:], result.stderr[-3000:])
            sys.exit(f"{entry['id']}: Blender failed")
        print(f"{entry['id']}: {report[-1][8:]}")
        size = json.loads(report[-1][8:])
        if "height" not in entry:
            entry["height"] = round(size["height"], 3)
            changed = True

    if changed:
        write_catalog(catalog)


def write_catalog(catalog):
    """Writes decor.json back with one piece per line, as it is kept."""
    lines = ["{", f'  "note": {json.dumps(catalog["note"])},', '  "pieces": [']
    pieces = catalog["pieces"]
    for index, piece in enumerate(pieces):
        # A blank line between categories, so the file stays easy to read and edit by hand.
        if index > 0 and piece["category"] != pieces[index - 1]["category"]:
            lines.append("")
        lines.append("    " + json.dumps(piece, ensure_ascii=False) + ("," if index < len(pieces) - 1 else ""))
    lines += ["  ]", "}", ""]
    CATALOG.write_text("\n".join(lines), encoding="utf-8")


if __name__ == "__main__":
    main()
