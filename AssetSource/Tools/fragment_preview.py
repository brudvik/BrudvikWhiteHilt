"""Draws the destruction chunks of converted models (<name>_frag*.obj from a .fragments.json) in flat colours, pushed
apart, from two sides, and prints each chunk's share of the surface. Use it to check that a model breaks into sensible
pieces before building the bundle.

Usage: python fragment_preview.py <out.png> <name> [<name> ...] [--dir <folder with the converted .obj files>]
The folder defaults to the Unity project's Assets/Foraging, which build_foraging_bundle.ps1 fills.
"""
import argparse
import math
import pathlib

from PIL import Image, ImageDraw

COLOURS = [(230, 80, 70), (70, 160, 230), (90, 200, 90), (240, 200, 60), (180, 100, 220), (60, 210, 200), (240, 140, 50), (200, 200, 200)]
SIZE = 360
DEFAULT_DIR = pathlib.Path(__file__).resolve().parents[2] / "BrudvikWhiteHiltUnity" / "Assets" / "Foraging"


def read_obj(path):
    vertices, faces = [], []
    for line in path.read_text().splitlines():
        if line.startswith("v "):
            vertices.append([float(x) for x in line.split()[1:4]])
        elif line.startswith("f "):
            faces.append([int(part.split("/")[0]) - 1 for part in line.split()[1:4]])
    # convert_glb.py writes every face twice; the second one is the back side.
    return vertices, faces[::2]


def area(a, b, c):
    u, w = [b[k] - a[k] for k in range(3)], [c[k] - a[k] for k in range(3)]
    cross = [u[1] * w[2] - u[2] * w[1], u[2] * w[0] - u[0] * w[2], u[0] * w[1] - u[1] * w[0]]
    return math.sqrt(sum(x * x for x in cross)) / 2


def view(point, yaw, pitch):
    x, y, z = point
    x, z = x * math.cos(yaw) - z * math.sin(yaw), x * math.sin(yaw) + z * math.cos(yaw)
    y, z = y * math.cos(pitch) - z * math.sin(pitch), y * math.sin(pitch) + z * math.cos(pitch)
    return x, y, z


def draw_model(folder, name):
    paths = sorted(folder.glob(f"{name}_frag*.obj"), key=lambda p: int(p.stem.split("_frag")[1]))
    if not paths:
        raise SystemExit(f"No {name}_frag*.obj in {folder}; does {name} have a .fragments.json?")
    chunks = [read_obj(path) for path in paths]
    points = [v for vertices, _ in chunks for v in vertices]
    centre = [sum(p[k] for p in points) / len(points) for k in range(3)]
    span = max(max(p[k] for p in points) - min(p[k] for p in points) for k in range(3))

    areas, triangles = [], []
    for index, (vertices, faces) in enumerate(chunks):
        areas.append(sum(area(*(vertices[i] for i in face)) for face in faces))
        middle = [sum(v[k] for v in vertices) / len(vertices) for k in range(3)]
        push = [(middle[k] - centre[k]) * 0.6 for k in range(3)]
        for face in faces:
            triangles.append((index, [[vertices[i][k] - centre[k] + push[k] for k in range(3)] for i in face]))
    total = sum(areas)
    print(f"{name}: {len(chunks)} chunks, surface " + ", ".join(f"{100 * a / total:.0f}%" for a in areas))

    row = Image.new("RGB", (SIZE * 2, SIZE), (30, 30, 34))
    for column, (yaw, pitch, shade) in enumerate([(0.6, -0.35, 1.0), (2.4, -0.35, 0.75)]):
        image = Image.new("RGB", (SIZE, SIZE), (30, 30, 34))
        draw = ImageDraw.Draw(image)
        projected = sorted(((sum(view(c, yaw, pitch)[2] for c in corners) / 3, index, [view(c, yaw, pitch) for c in corners])
                            for index, corners in triangles), key=lambda item: -item[0])
        scale = SIZE / (span * 1.9)
        for _, index, corners in projected:
            colour = tuple(int(c * shade) for c in COLOURS[index % len(COLOURS)])
            draw.polygon([(SIZE / 2 + p[0] * scale, SIZE / 2 - p[1] * scale) for p in corners], fill=colour,
                         outline=tuple(int(c * 0.7) for c in colour))
        draw.text((6, 6), name, fill=(255, 255, 255))
        row.paste(image, (column * SIZE, 0))
    return row


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("out")
    parser.add_argument("names", nargs="+")
    parser.add_argument("--dir", default=str(DEFAULT_DIR))
    args = parser.parse_args()
    rows = [draw_model(pathlib.Path(args.dir), name) for name in args.names]
    sheet = Image.new("RGB", (SIZE * 2, SIZE * len(rows)))
    for n, row in enumerate(rows):
        sheet.paste(row, (0, n * SIZE))
    sheet.save(args.out)
    print(f"Wrote {args.out}")


if __name__ == "__main__":
    main()
