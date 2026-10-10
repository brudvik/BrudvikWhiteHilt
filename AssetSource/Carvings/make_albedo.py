"""Paints the wood of a relief from its height map: a carved pine tone with grain running up the panel, the ground
darker (as old carving darkens where the knife went deep and dirt settles) and the strands' tops lighter where hands
and weather have worn them.

Usage: python make_albedo.py <height.png> <out.png> [r g b]
"""

import sys

import numpy as np
from PIL import Image, ImageFilter


def main():
    height = np.asarray(Image.open(sys.argv[1]).convert("L"), dtype=np.float32) / 255
    base = np.array([float(v) for v in sys.argv[3:6]] if len(sys.argv) >= 6 else [0.62, 0.47, 0.32], dtype=np.float32)
    h, w = height.shape
    rng = np.random.default_rng(1180)
    # Grain: noise stretched along the panel's length.
    noise = rng.random((max(2, h // 24), w)).astype(np.float32)
    grain = np.asarray(Image.fromarray((noise * 255).astype(np.uint8)).resize((w, h), Image.BICUBIC), dtype=np.float32) / 255
    grain = 0.9 + 0.2 * grain
    occlusion = np.asarray(Image.fromarray((height * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(4)), dtype=np.float32) / 255
    shade = (0.55 + 0.45 * height) * (0.8 + 0.2 * occlusion) * grain
    rgb = np.clip(base[None, None, :] * shade[:, :, None] * 1.25, 0, 1)
    Image.fromarray((rgb * 255).astype(np.uint8)).save(sys.argv[2])
    print(sys.argv[2])


if __name__ == "__main__":
    main()
