"""Makes the roof textures for the White Hilt roofs (Pieces/Roofs) and writes them to AssetSource/Textures.

Photo textures come from Poly Haven (CC0, downloaded once into %TEMP%/wh_roof_textures). The wooden shingles, the
dragon-scale shingles and the edge of the turf roof are drawn here from those photos. Every texture is written as
roof_<name>_albedo.jpg and, where it has relief, roof_<name>_normal.jpg (OpenGL convention, as Unity expects), 1024 px.
build_foraging_bundle.ps1 copies them into the asset bundle.

The size of one texture tile in metres is printed at the end; Pieces/Roofs/RoofFamily.cs uses the same numbers.

Usage: python make_roof_textures.py
"""
import io
import json
import os
import pathlib
import tempfile
import urllib.request

import numpy as np
from PIL import Image, ImageFilter

API = "https://api.polyhaven.com"
USER_AGENT = "BrudvikWhiteHilt asset tools (https://github.com/brudvik/BrudvikWhiteHilt)"
OUT = pathlib.Path(__file__).resolve().parents[1] / "Textures"
CACHE = pathlib.Path(tempfile.gettempdir()) / "wh_roof_textures"
SIZE = 1024
RNG = np.random.default_rng(1207)


def fetch(url):
    request = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
    with urllib.request.urlopen(request, timeout=120) as response:
        return response.read()


def poly_haven(asset, kind, res="1k"):
    """Returns a Poly Haven texture map (kind = Diffuse or nor_gl) as a float RGB array 0..1."""
    CACHE.mkdir(parents=True, exist_ok=True)
    path = CACHE / f"{asset}_{kind}_{res}.jpg"
    if not path.exists():
        files = json.loads(fetch(f"{API}/files/{asset}"))
        path.write_bytes(fetch(files[kind][res]["jpg"]["url"]))
    image = Image.open(path).convert("RGB").resize((SIZE, SIZE), Image.LANCZOS)
    return np.asarray(image, dtype=np.float32) / 255.0


def save(name, kind, pixels):
    OUT.mkdir(parents=True, exist_ok=True)
    image = Image.fromarray(np.clip(pixels * 255.0 + 0.5, 0, 255).astype(np.uint8), "RGB")
    path = OUT / f"roof_{name}_{kind}.jpg"
    image.save(path, quality=92)
    print(f"wrote {path.name} {image.size}")


def normal_from_height(height, strength):
    """OpenGL normal map from a tileable height map (0..1)."""
    dx = (np.roll(height, -1, axis=1) - np.roll(height, 1, axis=1)) * strength
    dy = (np.roll(height, 1, axis=0) - np.roll(height, -1, axis=0)) * strength
    normal = np.dstack([-dx, -dy, np.ones_like(height)])
    normal /= np.linalg.norm(normal, axis=2, keepdims=True)
    return normal * 0.5 + 0.5


def blend_normals(base, detail):
    """Adds the relief of a detail normal map (both 0..1 encoded) to a base one."""
    b = base * 2.0 - 1.0
    d = detail * 2.0 - 1.0
    n = np.dstack([b[..., 0] + d[..., 0], b[..., 1] + d[..., 1], b[..., 2] * d[..., 2]])
    n /= np.linalg.norm(n, axis=2, keepdims=True)
    return n * 0.5 + 0.5


def photo(name, asset, grade=None):
    albedo = poly_haven(asset, "Diffuse")
    save(name, "albedo", grade(albedo) if grade else albedo)
    save(name, "normal", poly_haven(asset, "nor_gl"))


def seamless_rows(pixels, skip, blend):
    """Drops the first rows (a ridge band in the photo), stretches the rest back and fades the bottom into the top."""
    image = Image.fromarray(np.clip(pixels[skip:] * 255, 0, 255).astype(np.uint8))
    stretched = np.asarray(image.resize((SIZE, SIZE), Image.LANCZOS), np.float32) / 255.0
    result = stretched.copy()
    for i in range(blend):
        w = i / blend
        result[SIZE - blend + i] = stretched[SIZE - blend + i] * (1.0 - w) + stretched[i] * w
    return result


def greener(pixels):
    """A sod roof in summer: less dry grass, more green."""
    graded = pixels * np.array([0.72, 0.9, 0.56])
    grey = graded.mean(axis=2, keepdims=True)
    return np.clip(grey + (graded - grey) * 1.1, 0.0, 1.0)


def shingles(name, wood, wood_normal, rounded, tint):
    """Tarred pine shingles, 4 courses per tile (one tile = 1 m). Grain runs down the roof (texture v)."""
    courses = 4
    course = SIZE // courses
    # Planks run sideways in the photo; turned a quarter, the grain runs down the roof.
    grain = np.rot90(wood).copy()
    grain_normal = np.rot90(wood_normal).copy()
    albedo = np.zeros((SIZE, SIZE, 3), np.float32)
    height = np.zeros((SIZE, SIZE), np.float32)
    detail = np.zeros((SIZE, SIZE, 3), np.float32)
    dy = np.arange(course)[:, None].astype(np.float32)
    t = dy / course
    # Covered part darker, butt lighter; a soft shadow under the course above.
    light = (0.72 + 0.38 * t) * np.where(dy < 18, 0.55 + 0.45 * dy / 18.0, 1.0)
    for c in range(courses):
        top = c * course
        widths = []
        total = 0
        while total < SIZE:
            width = int(RNG.integers(100, 170)) if not rounded else 128
            widths.append(width)
            total += width
        # The last shingle closes the tile so it repeats sideways.
        widths[-1] -= total - SIZE
        x = (c % 2) * (64 if rounded else int(RNG.integers(30, 80)))
        for width in widths:
            source_x = int(RNG.integers(0, SIZE - width))
            source_y = int(RNG.integers(0, SIZE - course))
            shade = float(RNG.uniform(0.82, 1.12))
            dx = np.arange(width)[None, :].astype(np.float32)
            gap = (dx < 3) | (dx >= width - 3)
            # Image rows run top to bottom and texture v bottom to top, so the butt of a course is at the bottom of its band.
            if rounded:
                radius = width / 2.0
                edge = course - radius + np.sqrt(np.maximum(radius * radius - (dx - radius) ** 2, 0.0))
            else:
                edge = np.full_like(dx, course)
            inside = dy <= edge
            columns = (x + np.arange(width)) % SIZE
            colour = grain[source_y:source_y + course, source_x:source_x + width] * shade * tint
            shingle_light = (light * np.where(gap, 0.35, 1.0))[..., None]
            block = albedo[top:top + course][:, columns]
            block = np.where(inside[..., None], colour * shingle_light, block)
            albedo[top:top + course, columns] = block
            h = np.where(inside, 0.15 + 0.85 * t - np.where(gap, 0.3, 0.0), height[top:top + course][:, columns])
            height[top:top + course, columns] = h
            n = np.where(inside[..., None], grain_normal[source_y:source_y + course, source_x:source_x + width], detail[top:top + course][:, columns])
            detail[top:top + course, columns] = n
            x += width
        # Behind the rounded butts the course below shows through: fill with dark shadow.
        band = albedo[top:top + course]
        empty = band.sum(axis=2) == 0
        band[empty] = np.array([0.05, 0.035, 0.025])
        detail[top:top + course][empty] = np.array([0.5, 0.5, 1.0])
    normal = blend_normals(normal_from_height(height, 6.0), detail)
    save(name, "albedo", albedo)
    save(name, "normal", normal)


def turf_edge(grass, soil, wood):
    """The cut edge of the turf roof, bottom to top: roof boards, birch bark, soil, grass. One tile = 2 m sideways."""
    height = SIZE // 2
    albedo = np.zeros((height, SIZE, 3), np.float32)
    # Rows are top to bottom in the image: grass at the top, boards at the bottom.
    for row in range(height):
        f = 1.0 - row / height
        if f < 0.16:
            line = wood[(row * 3) % SIZE] * 0.85
        elif f < 0.22:
            noise = RNG.uniform(0.85, 1.0, SIZE)[:, None]
            line = np.array([0.86, 0.84, 0.78]) * noise
            dashes = RNG.random(SIZE) < 0.06
            line[dashes] = np.array([0.12, 0.1, 0.09])
        elif f < 0.82:
            line = soil[(row * 2) % SIZE] * 0.8
        else:
            line = grass[(row * 2) % SIZE] * (0.75 + 0.25 * (f - 0.82) / 0.18)
        albedo[row] = line
    image = Image.fromarray(np.clip(albedo * 255, 0, 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.6))
    save("turfedge", "albedo", np.asarray(image, np.float32) / 255.0)


def main():
    photo("turf", "leafy_grass", greener)
    photo("slate", "roof_slates_02")
    photo("reed", "thatch_roof_angled")
    # The golden thatch photo has a ridge band along its top; it is cut off so the straw tiles down the roof.
    save("straw", "albedo", seamless_rows(poly_haven("reed_roof_04", "Diffuse"), 80, 96))
    save("straw", "normal", seamless_rows(poly_haven("reed_roof_04", "nor_gl"), 80, 96))

    wood = poly_haven("dark_wooden_planks", "Diffuse")
    wood_normal = poly_haven("dark_wooden_planks", "nor_gl")
    # Pine tar darkens and warms the wood.
    shingles("shingle", wood, wood_normal, rounded=False, tint=np.array([0.78, 0.6, 0.45]))
    shingles("scale", wood, wood_normal, rounded=True, tint=np.array([0.62, 0.46, 0.34]))
    turf_edge(greener(poly_haven("leafy_grass", "Diffuse")), poly_haven("brown_mud_leaves_01", "Diffuse"), wood)

    sizes = {}
    for asset in ("leafy_grass", "roof_slates_02", "thatch_roof_angled", "reed_roof_04", "dark_wooden_planks", "brown_mud_leaves_01"):
        info = json.loads(fetch(f"{API}/info/{asset}"))
        sizes[asset] = [round(d / 1000.0, 2) for d in info.get("dimensions", [0, 0])]
    print("tile size in metres:", json.dumps(sizes))


if __name__ == "__main__":
    main()
