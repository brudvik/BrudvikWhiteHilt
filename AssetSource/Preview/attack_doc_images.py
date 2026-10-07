"""
Makes the attack style documentation images from the preview strips that AssetSource/Unity/BuildAttackClips.cs renders
(BrudvikWhiteHiltUnity/Preview/attacks): the Rune Sword's three cuts from the side, its whirl from above, the vanilla
sword cuts against the Rune Sword's strike, and the README tile.

Each strip has one row per cut and one 256 px frame per 0.1 s; the frames picked below show each cut's stages.

Usage: python AssetSource/Preview/attack_doc_images.py
"""
import pathlib

from PIL import Image

ROOT = pathlib.Path(__file__).resolve().parents[2]
STRIPS = ROOT / "BrudvikWhiteHiltUnity" / "Preview" / "attacks"
DOCS = ROOT / "docs" / "images"
FRAME = 256
CROP = (30, 0, 226, 250)
BACKGROUND = (41, 46, 56)


def frame(strip, row, column):
    sheet = Image.open(STRIPS / f"{strip}.png").convert("RGB")
    tile = sheet.crop((column * FRAME, row * FRAME, (column + 1) * FRAME, (row + 1) * FRAME))
    return tile.crop(CROP)


def grid(rows, name):
    width, height = CROP[2] - CROP[0], CROP[3] - CROP[1]
    image = Image.new("RGB", (max(len(r) for r in rows) * width, len(rows) * height), BACKGROUND)
    for y, cells in enumerate(rows):
        for x, (strip, row, column) in enumerate(cells):
            image.paste(frame(strip, row, column), (x * width, y * height))
    path = DOCS / name
    image.save(path, optimize=True)
    print(path, image.size)


def main():
    grid([[("rune_side", 0, c) for c in (0, 1, 2, 3, 5, 6)],
          [("rune_side", 1, c) for c in (6, 1, 2, 3, 4, 5)],
          [("rune_side", 2, c) for c in (0, 2, 3, 4, 5, 6)]], "attack_runesword_cuts.png")
    grid([[("rune_top", 2, c) for c in range(7)]], "attack_runesword_whirl_top.png")
    grid([[("vanilla_sword_side", 0, c) for c in range(1, 7)],
          [("rune_side", 0, c) for c in range(1, 7)]], "attack_sword_vs_rune.png")
    grid([[("rune_side", 1, 3), ("rune_side", 2, 3)]], "readme/tile_attack_styles.png")


if __name__ == "__main__":
    main()
