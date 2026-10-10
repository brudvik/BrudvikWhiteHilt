"""Draws carved interlace as height maps: strands that weave over and under each other between two raised rims.

Usage: python make_knotwork.py <pattern> <out_prefix> <length_m> <width_m> [px_per_m]

Patterns:
  flette    a three-strand plait, the commonest border of the Viking age
  tau       a two-strand twisted cable, as round the stave church portals' arches
  ringkjede the Borre style's ring chain: rings linked by a band that weaves through them
  slyng     Urnes style: a broad band in long loops with a thin one winding through it

Each strand is a centre line with a depth along it (a helix or a braid seen from the front): at every pixel the
strand standing highest there wins, so where two cross, the one in front passes over and the other dips under it, as
in a real plait. A strand's cross-section is rounded like a knife leaves it, with a narrow groove at its edge where it
meets the ground. Writes <out_prefix>_height.png, <out_prefix>_normal.png (tangent space, for the game's normal map)
and <out_prefix>_albedo.png (pine, darker in the deep ground).
"""

import math
import sys

import numpy as np
from PIL import Image, ImageFilter

RIM = 0.12  # each rim's share of the band's width


def strands_for(pattern, length, width):
    """The strands as functions of x (metres) giving (y, depth, radius) arrays, y and radius in metres, depth 0..1."""
    inner = width * (1 - 2 * RIM)
    mid = width / 2
    if pattern == "flette":
        period = inner * 2.2
        amp = inner * 0.36
        strands = []
        for i in range(3):
            phase = 2 * math.pi * i / 3
            strands.append(lambda x, p=phase: (mid + amp * np.sin(2 * math.pi * x / period + p),
                                               0.5 + 0.5 * np.sin(2 * (2 * math.pi * x / period + p)), inner * 0.11))
        return strands
    if pattern == "tau":
        period = inner * 1.6
        amp = inner * 0.3
        return [lambda x, p=p: (mid + amp * np.cos(2 * math.pi * x / period + p), 0.5 + 0.5 * np.sin(2 * math.pi * x / period + p), inner * 0.16)
                for p in (0.0, math.pi)]
    if pattern == "slyng":
        period = inner * 3.0
        broad = lambda x: (mid + inner * 0.34 * np.sin(2 * math.pi * x / period), 0.5 + 0.5 * np.cos(4 * math.pi * x / period), inner * 0.12)
        thin = lambda x: (mid + inner * 0.4 * np.sin(3 * math.pi * x / period + 1.0), 0.5 - 0.5 * np.cos(4 * math.pi * x / period), inner * 0.05)
        thin2 = lambda x: (mid - inner * 0.4 * np.sin(3 * math.pi * x / period + 1.0), 0.5 + 0.5 * np.sin(4 * math.pi * x / period + 1.0), inner * 0.04)
        return [broad, thin, thin2]
    raise ValueError(pattern)


def rings_for(length, width):
    """The ring chain: rings along the band, each passing over the weaving band at one crossing and under at the next."""
    inner = width * (1 - 2 * RIM)
    radius = inner * 0.36
    step = radius * 2.3
    rings = []
    x = step / 2
    k = 0
    while x < length:
        rings.append((x, width / 2, radius, k))
        x += step
        k += 1
    band = lambda xs: (width / 2 + inner * 0.0 * xs, 0.5 + 0.5 * np.cos(2 * math.pi * xs / step), inner * 0.08)
    return rings, band, step


def profile(distance, radius):
    t = np.clip(distance / radius, 0, 1)
    return np.sqrt(np.clip(1 - t * t, 0, 1))


def height_map(pattern, length, width, ppm):
    w, h = int(length * ppm), int(width * ppm)
    xs = (np.arange(w) + 0.5) / ppm
    ys = (np.arange(h) + 0.5) / ppm
    X, Y = np.meshgrid(xs, ys)
    height = np.zeros((h, w), dtype=np.float32)

    def add_strand(cy, depth, radius):
        # The vertical distance, shortened by the strand's slope, is near enough the true distance for a smooth curve.
        slope = np.gradient(cy, xs)
        distance = np.abs(Y - cy[None, :]) / np.sqrt(1 + slope[None, :] ** 2)
        body = profile(distance, radius)
        level = 0.45 + 0.35 * depth[None, :] + 0.2 * body
        strand = np.where(distance < radius, level, 0)
        # A groove round the strand where it lies on another, so the one beneath reads as passing under.
        edge = (distance >= radius) & (distance < radius * 1.25)
        np.copyto(height, np.where(edge & (height > 0), height * 0.7, height))
        np.maximum(height, strand, out=height)

    if pattern == "ringkjede":
        rings, band, step = rings_for(length, width)
        for cx, cy, radius, k in rings:
            angle = np.arctan2(Y - cy, X - cx)
            distance = np.abs(np.hypot(X - cx, Y - cy) - radius)
            thickness = width * (1 - 2 * RIM) * 0.07
            depth = 0.5 + 0.5 * np.cos(2 * angle + k * math.pi)
            body = profile(distance, thickness)
            ring = np.where(distance < thickness, 0.45 + 0.35 * depth + 0.2 * body, 0)
            np.maximum(height, ring, out=height)
        cy, depth, radius = band(xs)
        add_strand(np.full_like(xs, width / 2), depth, radius)
    else:
        for strand in strands_for(pattern, length, width):
            cy, depth, radius = strand(xs)
            add_strand(cy, depth, radius)
    # The rims: flat raised bands along both edges, with a small step down to the ground.
    rim = int(RIM * h)
    height[:rim, :] = np.maximum(height[:rim, :], 0.75)
    height[-rim:, :] = np.maximum(height[-rim:, :], 0.75)
    # Round off the knife's edges a little.
    image = Image.fromarray((np.clip(height, 0, 1) * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(ppm / 600))
    return np.asarray(image, dtype=np.float32) / 255


def normal_map(height, ppm, depth_m):
    """Tangent-space normals from the height map, depth_m metres deep at height 1, in Unity's convention (y up)."""
    dz_dx = np.gradient(height, axis=1) * depth_m * ppm
    dz_dy = -np.gradient(height, axis=0) * depth_m * ppm
    n = np.stack([-dz_dx, -dz_dy, np.ones_like(height)], axis=-1)
    n /= np.linalg.norm(n, axis=-1, keepdims=True)
    return ((n * 0.5 + 0.5) * 255).astype(np.uint8)


def albedo(height, seed=1180):
    h, w = height.shape
    rng = np.random.default_rng(seed)
    noise = rng.random((h, max(2, w // 40))).astype(np.float32)
    grain = np.asarray(Image.fromarray((noise * 255).astype(np.uint8)).resize((w, h), Image.BICUBIC), dtype=np.float32) / 255
    occlusion = np.asarray(Image.fromarray((height * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(6)), dtype=np.float32) / 255
    shade = (0.5 + 0.5 * height) * (0.75 + 0.25 * occlusion) * (0.9 + 0.2 * grain)
    base = np.array([0.66, 0.5, 0.34], dtype=np.float32)
    return (np.clip(base[None, None, :] * shade[:, :, None] * 1.2, 0, 1) * 255).astype(np.uint8)


def main():
    pattern, prefix = sys.argv[1], sys.argv[2]
    length, width = float(sys.argv[3]), float(sys.argv[4])
    ppm = int(sys.argv[5]) if len(sys.argv) > 5 else 1024
    height = height_map(pattern, length, width, ppm)
    Image.fromarray((height * 255).astype(np.uint8)).save(f"{prefix}_height.png")
    Image.fromarray(normal_map(height, ppm, 0.03)).save(f"{prefix}_normal.png")
    Image.fromarray(albedo(height)).save(f"{prefix}_albedo.png")
    print(f"{prefix}: {height.shape[1]} x {height.shape[0]} px")


if __name__ == "__main__":
    main()
