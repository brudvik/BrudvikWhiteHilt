"""Turns an engraving of a carving into a height map for a relief.

Usage: python make_heightmap.py <drawing> <out.png> <x0> <y0> <x1> <y1> [--invert]

The 19th-century engravings of the stave church portals (Dietrichson, "De norske stavkirker", 1892, public domain) draw
the carved strands light, with thin dark outlines, on a background of dark hatching. Blurred a little, the hatching
turns into an even dark ground and the strands stay light, so the local brightness tells carving from ground. This
writes that as a height map: ground 0, strands 1, their edges rounded off as a knife leaves them, and the dark outlines
as shallow grooves along the strands. The crop is in the drawing's pixels.
"""

import sys

import numpy as np
from PIL import Image, ImageFilter


def blur(array, radius):
    image = Image.fromarray(np.clip(array * 255, 0, 255).astype(np.uint8))
    return np.asarray(image.filter(ImageFilter.GaussianBlur(radius)), dtype=np.float32) / 255


def smoothstep(edge0, edge1, x):
    t = np.clip((x - edge0) / (edge1 - edge0), 0, 1)
    return t * t * (3 - 2 * t)


def morph(array, size, grow):
    image = Image.fromarray(np.clip(array * 255, 0, 255).astype(np.uint8))
    image = image.filter(ImageFilter.MaxFilter(size) if grow else ImageFilter.MinFilter(size))
    return np.asarray(image, dtype=np.float32) / 255


def heightmap(drawing, box, invert=False, frame=0.05):
    gray = np.asarray(Image.open(drawing).convert("L").crop(box), dtype=np.float32) / 255
    if invert:
        gray = 1 - gray
    # The hatching is a few pixels apart: blurred over that, the ground is an even grey well under the strands.
    tone = blur(gray, 2.5)
    low, high = np.percentile(tone, 25), np.percentile(tone, 80)
    strands = smoothstep(low + 0.35 * (high - low), low + 0.75 * (high - low), tone)
    # Specks of hatching that came out light go, and pin holes in the strands close.
    strands = morph(morph(strands, 5, False), 5, True)
    strands = morph(morph(strands, 3, True), 3, False)
    # Round the strands like a knife leaves them: high along their middle, falling off to the ground.
    rounded = smoothstep(0.15, 0.85, blur(strands, 2.2)) ** 0.7
    lines = smoothstep(0.55, 0.25, blur(gray, 0.8)) * rounded
    height = np.clip(rounded - 0.15 * lines, 0, 1)
    # A plain frame round the panel, as the carving stops short of a board's edge.
    h, w = height.shape
    margin = max(2, int(frame * w))
    ramp_x = np.clip(np.minimum(np.arange(w), np.arange(w)[::-1]) / margin - 0.5, 0, 1)
    ramp_y = np.clip(np.minimum(np.arange(h), np.arange(h)[::-1]) / margin - 0.5, 0, 1)
    return height * ramp_y[:, None] * ramp_x[None, :]


def main():
    drawing, out = sys.argv[1], sys.argv[2]
    box = tuple(int(v) for v in sys.argv[3:7])
    height = heightmap(drawing, box, "--invert" in sys.argv)
    Image.fromarray((height * 255).astype(np.uint8)).save(out)
    print(f"{out}: {height.shape[1]} x {height.shape[0]}, mean {height.mean():.2f}")


if __name__ == "__main__":
    main()
