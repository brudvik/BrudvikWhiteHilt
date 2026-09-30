"""Searches Sketchfab for downloadable models and draws a numbered contact sheet of the thumbnails.

Only CC0 and CC BY models are listed (see the license rules in .github/skills/valheim-custom-model). Look at the sheet
before recommending a model: names and tags lie.

Usage:
  python find_models.py "paint brush" "paint bucket" [--max-faces 6000] [--count 24] [--out DIR]
  python find_models.py --uids <uid> [<uid> ...] [--out DIR]     (sheet of chosen models, with descriptions)
Writes <out>/candidates.json and <out>/sheet.png (default %TEMP%\\wh_models).
"""
import argparse
import io
import json
import os
import pathlib
import urllib.parse
import urllib.request

from PIL import Image, ImageDraw

API = "https://api.sketchfab.com/v3"
LICENSES = {"cc0": "CC0", "by": "CC BY"}
TILE_W, TILE_H, LABEL_H = 240, 180, 44


def fetch(url):
    request = urllib.request.Request(url, headers={"User-Agent": "BrudvikWhiteHilt-tools"})
    with urllib.request.urlopen(request, timeout=30) as response:
        return response.read()


def thumbnail(model, width=256):
    images = sorted(model.get("thumbnails", {}).get("images", []), key=lambda image: abs(image["width"] - width))
    return images[0]["url"] if images else None


def search(terms, max_faces, count):
    found = {}
    for term in terms:
        for license_key in LICENSES:
            query = urllib.parse.urlencode({"type": "models", "q": term, "downloadable": "true", "license": license_key,
                                            "max_face_count": max_faces, "count": count})
            try:
                results = json.loads(fetch(f"{API}/search?{query}"))["results"]
            except Exception as error:
                print(f"search failed: {term} {license_key}: {error}")
                continue
            for model in results:
                found.setdefault(model["uid"], {
                    "term": term, "uid": model["uid"], "name": model["name"], "user": model["user"]["username"],
                    "license": LICENSES[license_key], "faces": model.get("faceCount") or 0, "likes": model.get("likeCount") or 0,
                    "thumb": thumbnail(model), "url": f"https://sketchfab.com/3d-models/{model['uid']}",
                })
    return sorted(found.values(), key=lambda row: (row["term"], row["faces"]))


def details(uids):
    rows = []
    for uid in uids:
        model = json.loads(fetch(f"{API}/models/{uid}"))
        description = (model.get("description") or "").replace("\n", " ")[:300]
        rows.append({
            "term": "chosen", "uid": uid, "name": model["name"], "user": model["user"]["username"],
            "license": (model.get("license") or {}).get("label", "?"), "faces": model.get("faceCount") or 0,
            "likes": model.get("likeCount") or 0, "thumb": thumbnail(model, 480), "url": f"https://sketchfab.com/3d-models/{uid}",
            "description": description,
        })
    return rows


def sheet(rows, path):
    columns = 6
    lines = (len(rows) + columns - 1) // columns
    image = Image.new("RGB", (TILE_W * columns, (TILE_H + LABEL_H) * max(lines, 1)), "white")
    draw = ImageDraw.Draw(image)
    for index, row in enumerate(rows):
        x, y = (index % columns) * TILE_W, (index // columns) * (TILE_H + LABEL_H)
        if row["thumb"]:
            try:
                thumb = Image.open(io.BytesIO(fetch(row["thumb"]))).convert("RGB")
                thumb.thumbnail((TILE_W - 8, TILE_H - 8))
                image.paste(thumb, (x + 4, y + 4))
            except Exception as error:
                print(f"no thumbnail for {row['uid']}: {error}")
        label = f"{index + 1}. {row['name'][:34]}\n{row['license']} {row['faces']}f  {row['user'][:18]}"
        draw.text((x + 4, y + TILE_H + 2), label.encode("ascii", "replace").decode(), fill="black")
    image.save(path)


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("terms", nargs="*")
    parser.add_argument("--uids", nargs="+")
    parser.add_argument("--max-faces", type=int, default=6000)
    parser.add_argument("--count", type=int, default=24)
    parser.add_argument("--out", default=os.path.join(os.environ.get("TEMP", "."), "wh_models"))
    args = parser.parse_args()
    if not args.terms and not args.uids:
        parser.error("give search terms or --uids")

    out = pathlib.Path(args.out)
    out.mkdir(parents=True, exist_ok=True)
    rows = details(args.uids) if args.uids else search(args.terms, args.max_faces, args.count)
    (out / "candidates.json").write_text(json.dumps(rows, indent=1))
    for index, row in enumerate(rows, 1):
        print(f"{index:3}. {row['term'][:16]:16} {row['license']:6} {row['faces']:>6}f  {row['user'][:18]:18} {row['name'][:44]:44} {row['url']}")
        if row.get("description"):
            print(f"      {row['description']}")
    sheet(rows, out / "sheet.png")
    print(out / "sheet.png")


if __name__ == "__main__":
    main()
