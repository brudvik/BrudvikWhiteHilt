"""Makes the gold trim mask of the White Hilt Uniform Cape from the vanilla troll hide cape's UV layout.

Reads the worn cape mesh (CapeTrollHide attach_skin/cape2) from the game's item bundle with UnityPy and keeps a band just
inside the cape's outer edge, drawn in UV space (seams inside the cloth are skipped). Writes BrudvikWhiteHilt/Assets/Uniform/CapeTrim.png (white = trim, rows
top-down as usual for images); the plugin embeds it and paints the band gold.

Usage: python AssetSource/Tools/make_cape_trim.py
"""
import pathlib
from collections import Counter

import UnityPy
from PIL import Image, ImageChops, ImageDraw
from UnityPy.helpers.MeshHelper import MeshHandler

BUNDLE = r"C:\Program Files (x86)\Steam\steamapps\common\Valheim\valheim_Data\StreamingAssets\SoftRef\Bundles\c4210710"
OUTPUT = pathlib.Path(__file__).resolve().parents[2] / "BrudvikWhiteHilt" / "Assets" / "Uniform" / "CapeTrim.png"
SIZE = 512
# Pixels from the edge: the band runs from INNER to OUTER, so a thin black edge stays outside it.
OUTER = 4
INNER = 9


def cape_mesh():
    env = UnityPy.load(BUNDLE)
    for obj in env.objects:
        if obj.type.name != "Mesh":
            continue
        mesh = obj.read()
        if mesh.m_Name == "cape2":
            return mesh
    raise SystemExit("cape2 mesh not found")


def uv(handler, index):
    u, v = handler.m_UV0[index][:2]
    return u * SIZE, (1 - v) * SIZE


def main():
    handler = MeshHandler(cape_mesh())
    handler.process()
    mask = Image.new("L", (SIZE, SIZE), 0)
    draw = ImageDraw.Draw(mask)
    triangles = [triangle for submesh in handler.get_triangles() for triangle in submesh]
    for triangle in triangles:
        draw.polygon([uv(handler, i) for i in triangle], fill=255)

    # The cape's real edge: edges of one triangle only, by position, so UV seams inside the cloth get no trim.
    position = [tuple(round(c, 5) for c in handler.m_Vertices[i]) for i in range(len(handler.m_Vertices))]
    count = Counter()
    for triangle in triangles:
        for a, b in zip(triangle, (*triangle[1:], triangle[0])):
            count[frozenset((position[a], position[b]))] += 1

    def band(width):
        lines = Image.new("L", (SIZE, SIZE), 0)
        line_draw = ImageDraw.Draw(lines)
        for triangle in triangles:
            for a, b in zip(triangle, (*triangle[1:], triangle[0])):
                if count[frozenset((position[a], position[b]))] == 1:
                    line_draw.line([uv(handler, a), uv(handler, b)], fill=255, width=width * 2 + 1)
                    for end in (a, b):
                        x, y = uv(handler, end)
                        line_draw.ellipse([x - width, y - width, x + width, y + width], fill=255)
        return ImageChops.multiply(lines, mask)

    trim = ImageChops.subtract(band(INNER), band(OUTER))
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    # RGB, since Unity may load a greyscale PNG as an alpha-only texture.
    trim.convert("RGB").save(OUTPUT, optimize=True)
    print(f"{OUTPUT}: {sum(trim.histogram()[1:])} trim pixels")


if __name__ == "__main__":
    main()
