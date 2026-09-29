"""Draws the clock's weather icons into BrudvikWhiteHilt/Assets/Weather*.png (64 x 64, drawn at 4x and scaled down)."""
import math
import os

from PIL import Image, ImageDraw, ImageFilter

BIG = 256
SIZE = 64
OUTLINE = (43, 29, 16, 255)
LINE = 12
OUT_DIR = os.path.join(os.path.dirname(__file__), "..", "BrudvikWhiteHilt", "Assets")

SUN = (255, 196, 64, 255)
MOON = (236, 228, 196, 255)
CLOUD = (214, 214, 222, 255)
DARK_CLOUD = (120, 120, 132, 255)
RAIN = (96, 170, 255, 255)
SNOW = (236, 246, 255, 255)
BOLT = (255, 226, 80, 255)
FOG = (200, 200, 208, 255)
EMBER = (255, 120, 40, 255)


def canvas():
    image = Image.new("RGBA", (BIG, BIG), (0, 0, 0, 0))
    return image, ImageDraw.Draw(image)


def sun(draw, cx=128, cy=128, r=56, rays=True):
    if rays:
        for i in range(8):
            a = i * math.pi / 4
            x1, y1 = cx + math.cos(a) * (r + 14), cy + math.sin(a) * (r + 14)
            x2, y2 = cx + math.cos(a) * (r + 44), cy + math.sin(a) * (r + 44)
            draw.line((x1, y1, x2, y2), fill=OUTLINE, width=LINE + 14)
            draw.line((x1, y1, x2, y2), fill=SUN, width=LINE)
    draw.ellipse((cx - r, cy - r, cx + r, cy + r), fill=SUN, outline=OUTLINE, width=LINE)


def moon(image):
    mask = Image.new("L", (BIG, BIG), 0)
    m = ImageDraw.Draw(mask)
    m.ellipse((40, 36, 216, 212), fill=255)
    m.ellipse((96, 12, 252, 168), fill=0)
    image.paste(Image.new("RGBA", (BIG, BIG), OUTLINE), (0, 0), mask)
    image.paste(Image.new("RGBA", (BIG, BIG), MOON), (0, 0), mask.filter(ImageFilter.MinFilter(2 * (LINE // 2) + 1)))


def cloud(draw, fill=CLOUD, top=64):
    parts = [(36, top + 56, 116, top + 136), (84, top + 16, 184, top + 116), (148, top + 48, 224, top + 124)]
    base = (52, top + 84, 208, top + 136)
    for box in parts:
        draw.ellipse(box, fill=OUTLINE)
    draw.rounded_rectangle(base, radius=26, fill=OUTLINE)
    shrink = LINE
    for x1, y1, x2, y2 in parts:
        draw.ellipse((x1 + shrink, y1 + shrink, x2 - shrink, y2 - shrink), fill=fill)
    draw.rounded_rectangle((base[0] + shrink, base[1] + shrink - 20, base[2] - shrink, base[3] - shrink), radius=16, fill=fill)


def drops(draw, color, top=184):
    for x in (84, 132, 180):
        draw.line((x + 8, top, x - 8, top + 40), fill=OUTLINE, width=LINE + 12)
        draw.line((x + 8, top, x - 8, top + 40), fill=color, width=LINE)


def flakes(draw, top=196):
    for x, y in ((80, top), (128, top + 22), (176, top)):
        for i in range(3):
            a = i * math.pi / 3
            dx, dy = math.cos(a) * 20, math.sin(a) * 20
            draw.line((x - dx, y - dy, x + dx, y + dy), fill=OUTLINE, width=LINE + 8)
        for i in range(3):
            a = i * math.pi / 3
            dx, dy = math.cos(a) * 18, math.sin(a) * 18
            draw.line((x - dx, y - dy, x + dx, y + dy), fill=SNOW, width=LINE - 2)


def bolt(draw):
    points = [(152, 136), (92, 212), (128, 212), (100, 254), (180, 176), (144, 176), (176, 136)]
    draw.polygon(points, fill=BOLT, outline=OUTLINE, width=LINE - 4)


def fog(draw):
    for y, x1, x2 in ((96, 40, 216), (140, 24, 200), (184, 56, 232)):
        draw.line((x1, y, x2, y), fill=OUTLINE, width=LINE + 16)
        draw.line((x1, y, x2, y), fill=FOG, width=LINE + 2)


def embers(draw):
    for x, y in ((84, 200), (136, 226), (184, 196)):
        draw.ellipse((x - 14, y - 14, x + 14, y + 14), fill=EMBER, outline=OUTLINE, width=6)


def save(image, name):
    image.resize((SIZE, SIZE), Image.LANCZOS).save(os.path.join(OUT_DIR, f"Weather{name}.png"))


def main():
    image, draw = canvas()
    sun(draw)
    save(image, "Sun")

    image, _ = canvas()
    moon(image)
    save(image, "Moon")

    image, draw = canvas()
    sun(draw, 96, 96, 44)
    cloud(draw, top=76)
    save(image, "Cloud")

    image, draw = canvas()
    cloud(draw, top=24)
    drops(draw, RAIN)
    save(image, "Rain")

    image, draw = canvas()
    cloud(draw, DARK_CLOUD, top=12)
    bolt(draw)
    save(image, "Storm")

    image, draw = canvas()
    cloud(draw, top=24)
    flakes(draw)
    save(image, "Snow")

    image, draw = canvas()
    fog(draw)
    save(image, "Fog")

    image, draw = canvas()
    cloud(draw, DARK_CLOUD, top=24)
    embers(draw)
    save(image, "Ash")


if __name__ == "__main__":
    main()
