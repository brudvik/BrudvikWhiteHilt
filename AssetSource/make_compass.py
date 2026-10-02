"""Makes the map compass sprites from the Seadogs Compass (Benny Weimer, CC0, Poly Haven) in AssetSource/Compass.

Blender renders the dial (without lid and needle) and the needle from straight above in the same frame
(Tools/render_compass.py). The dial is then cut round at the bezel, its printed N/E/S/W are painted out (the mod writes
the letters itself, in the game's language) and the fine degree rings are softened; the needle's open points are filled
and its north half painted red.
Writes BrudvikWhiteHilt/Assets/Compass/CompassDial.png and CompassNeedle.png (256 x 256, same frame, pivot in the
centre) and a preview at map sizes to %TEMP%\\wh_compass\\preview.png.

Usage: python AssetSource\\make_compass.py [--blender <blender.exe>]
"""
import argparse
import math
import os
import pathlib
import subprocess
import tempfile

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

HERE = pathlib.Path(__file__).resolve().parent
SOURCE = HERE / "Compass" / "seadogs_compass.gltf"
OUT_DIR = HERE.parent / "BrudvikWhiteHilt" / "Assets" / "Compass"
BLENDER = r"C:\Program Files\Blender Foundation\Blender 5.2\blender.exe"

RENDER = 512
HALF_WIDTH = 0.04          # metres the render covers to each side of the centre
BEZEL = 0.0375             # outer edge of the bezel, measured on the render
SPRITE_HALF = 0.0378       # metres the sprite covers to each side, a hair outside the bezel
SIZE = 256

# The printed letters sit in the plain band between the degree ring and the bezel, between two big ticks.
LETTER_BAND = (0.02595, 0.0308)  # metres from the centre
LETTER_HALF_ANGLE = 6.0          # degrees each side of a cardinal point that are painted out
SAMPLE_ANGLES = (6.4, 7.6)       # degrees each side where the plain band is sampled

FACE_BLUR = 3              # render pixels
FACE_CALM = 0.6            # share of the blurred face mixed in

NEEDLE_HUB = 0.0052        # metres; the needle is red beyond this, towards north
NORTH_RED = np.array([0.78, 0.12, 0.08])

# Where the mod writes the letters, as a share of the sprite's half width (matches MapCompass.LetterRadius).
LETTER_RADIUS = 0.74


def render(part, out):
    script = HERE / "Tools" / "render_compass.py"
    subprocess.run([BLENDER, "-b", "-P", str(script), "--", str(SOURCE), str(out), part, str(RENDER), str(HALF_WIDTH)],
                   check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    return np.asarray(Image.open(out).convert("RGBA")).astype(np.float64) / 255.0


def polar(n, half_width):
    centre = (n - 1) / 2
    ys, xs = np.mgrid[0:n, 0:n]
    dx = (xs - centre) / (n / 2) * half_width
    dy = (centre - ys) / (n / 2) * half_width
    radius = np.hypot(dx, dy)
    bearing = np.degrees(np.arctan2(dx, dy)) % 360  # 0 = up (north), clockwise
    return radius, bearing


def sample(image, radius_m, bearing_deg):
    n = image.shape[0]
    centre = (n - 1) / 2
    scale = (n / 2) / HALF_WIDTH
    x = centre + np.sin(np.radians(bearing_deg)) * radius_m * scale
    y = centre - np.cos(np.radians(bearing_deg)) * radius_m * scale
    x0 = np.clip(np.floor(x).astype(int), 0, n - 2)
    y0 = np.clip(np.floor(y).astype(int), 0, n - 2)
    fx = (x - x0)[..., None]
    fy = (y - y0)[..., None]
    top = image[y0, x0] * (1 - fx) + image[y0, x0 + 1] * fx
    bottom = image[y0 + 1, x0] * (1 - fx) + image[y0 + 1, x0 + 1] * fx
    return top * (1 - fy) + bottom * fy


def paint_out_letters(dial):
    radius, bearing = polar(dial.shape[0], HALF_WIDTH)
    result = dial.copy()
    lo, hi = LETTER_BAND
    for cardinal in (0, 90, 180, 270):
        offset = (bearing - cardinal + 180) % 360 - 180
        mask = (radius >= lo) & (radius <= hi) & (np.abs(offset) <= LETTER_HALF_ANGLE)
        r = radius[mask]
        t = (offset[mask] + LETTER_HALF_ANGLE) / (2 * LETTER_HALF_ANGLE)
        left = np.zeros((r.size, 4))
        right = np.zeros((r.size, 4))
        steps = np.linspace(SAMPLE_ANGLES[0], SAMPLE_ANGLES[1], 7)
        for step in steps:
            left += sample(dial, r, cardinal - step)
            right += sample(dial, r, cardinal + step)
        left /= len(steps)
        right /= len(steps)
        result[mask] = left * (1 - t[:, None]) + right * t[:, None]
    return result


def calm_face(dial):
    """Softens the fine degree rings inside the letter band, which only flicker at map size."""
    radius, _ = polar(dial.shape[0], HALF_WIDTH)
    blurred = np.asarray(Image.fromarray((dial * 255).round().astype(np.uint8), "RGBA").filter(ImageFilter.GaussianBlur(FACE_BLUR)))
    blurred = blurred.astype(np.float64) / 255
    weight = np.clip((LETTER_BAND[0] - radius) / 0.001, 0, 1)[..., None] * FACE_CALM
    return dial * (1 - weight) + blurred * weight


def cut_round(image, edge_m):
    radius, _ = polar(image.shape[0], HALF_WIDTH)
    pixel = HALF_WIDTH / (image.shape[0] / 2)
    alpha = np.clip((edge_m - radius) / pixel + 0.5, 0, 1)
    result = image.copy()
    result[..., 3] *= alpha
    return result


def fill_holes(image):
    """The needle's points are open frames; fills them so the needle reads at map size."""
    n = image.shape[0]
    solid = Image.fromarray(((image[..., 3] > 0.5) * 255).astype(np.uint8), "L").copy()
    ImageDraw.floodfill(solid, (0, 0), 128)
    holes = np.asarray(solid) == 0
    result = image.copy()
    edge = image[..., :3][image[..., 3] > 0.5].mean(axis=0)
    result[holes, :3] = edge * 0.8
    result[holes, 3] = 1.0
    return result


def paint_needle(needle):
    needle = fill_holes(needle)
    n = needle.shape[0]
    centre = (n - 1) / 2
    ys, _ = np.mgrid[0:n, 0:n]
    north = (centre - ys) / (n / 2) * HALF_WIDTH > NEEDLE_HUB
    result = needle.copy()
    light = needle[..., :3].mean(axis=2, keepdims=True)
    red = np.clip(NORTH_RED * (0.55 + 2.2 * light), 0, 1)
    result[..., :3] = np.where(north[..., None], red, needle[..., :3])
    return result


def to_sprite(image):
    n = image.shape[0]
    keep = int(round(SPRITE_HALF / HALF_WIDTH * n / 2))
    centre = n // 2
    crop = image[centre - keep:centre + keep, centre - keep:centre + keep]
    rgb = crop[..., :3] * crop[..., 3:4]
    premultiplied = np.concatenate([rgb, crop[..., 3:4]], axis=2)
    big = Image.fromarray((premultiplied * 255).round().astype(np.uint8), "RGBA")
    small = np.asarray(big.resize((SIZE, SIZE), Image.LANCZOS)).astype(np.float64) / 255
    alpha = small[..., 3:4]
    colour = np.where(alpha > 0, small[..., :3] / np.maximum(alpha, 1e-6), 0)
    return Image.fromarray((np.clip(np.concatenate([colour, alpha], axis=2), 0, 1) * 255).round().astype(np.uint8), "RGBA")


def preview(dial, needle, out):
    font_path = r"C:\Windows\Fonts\georgiab.ttf"
    sheet = Image.new("RGBA", (420, 200), (70, 86, 60, 255))
    x = 10
    for size, heading in ((64, 0), (64, 245), (110, 245), (150, 60)):
        tile = dial.resize((size, size), Image.LANCZOS)
        rotated = needle.rotate(-heading, resample=Image.BICUBIC).resize((size, size), Image.LANCZOS)
        tile.alpha_composite(rotated)
        draw = ImageDraw.Draw(tile)
        font = ImageFont.truetype(font_path, max(8, int(size * 0.17)))
        for letter, angle in (("N", 0), ("Ø", 90), ("S", 180), ("V", 270)):
            cx = size / 2 + math.sin(math.radians(angle)) * LETTER_RADIUS * size / 2
            cy = size / 2 - math.cos(math.radians(angle)) * LETTER_RADIUS * size / 2
            colour = (150, 20, 14, 255) if letter == "N" else (28, 22, 16, 255)
            draw.text((cx, cy), letter, font=font, fill=colour, anchor="mm")
        sheet.alpha_composite(tile, (x, 10))
        x += size + 10
    sheet.save(out)


def main():
    global BLENDER
    parser = argparse.ArgumentParser()
    parser.add_argument("--blender", default=BLENDER)
    BLENDER = parser.parse_args().blender

    work = pathlib.Path(tempfile.gettempdir()) / "wh_compass"
    work.mkdir(exist_ok=True)
    dial = render("dial", work / "raw_dial.png")
    needle = render("needle", work / "raw_needle.png")

    dial_sprite = to_sprite(cut_round(calm_face(paint_out_letters(dial)), BEZEL))
    needle_sprite = to_sprite(paint_needle(needle))

    OUT_DIR.mkdir(parents=True, exist_ok=True)
    dial_sprite.save(OUT_DIR / "CompassDial.png")
    needle_sprite.save(OUT_DIR / "CompassNeedle.png")
    preview(dial_sprite, needle_sprite, work / "preview.png")
    for name in ("CompassDial.png", "CompassNeedle.png"):
        print(OUT_DIR / name, os.path.getsize(OUT_DIR / name), "bytes")
    print(work / "preview.png")


if __name__ == "__main__":
    main()
