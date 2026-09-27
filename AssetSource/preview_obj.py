"""Renders the silhouette of an OBJ from the front (x/y) and side (z/y) to a PNG, to check orientation.

Usage: python preview_obj.py <input.obj> <output.png>
"""
import pathlib
import struct
import sys
import zlib

SIZE = 200


def write_png(path, width, height, rows):
    raw = b"".join(b"\x00" + bytes(row) for row in rows)

    def chunk(tag, data):
        return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)

    header = struct.pack(">IIBBBBB", width, height, 8, 0, 0, 0, 0)
    path.write_bytes(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", header) + chunk(b"IDAT", zlib.compress(raw)) + chunk(b"IEND", b""))


def rasterize(image, offset, points, triangles):
    for a, b, c in triangles:
        (x1, y1), (x2, y2), (x3, y3) = points[a], points[b], points[c]
        area = (x2 - x1) * (y3 - y1) - (x3 - x1) * (y2 - y1)
        if area == 0:
            continue
        for y in range(max(0, int(min(y1, y2, y3))), min(SIZE, int(max(y1, y2, y3)) + 1)):
            for x in range(max(0, int(min(x1, x2, x3))), min(SIZE, int(max(x1, x2, x3)) + 1)):
                w1 = ((x2 - x) * (y3 - y) - (x3 - x) * (y2 - y)) / area
                w2 = ((x3 - x) * (y1 - y) - (x1 - x) * (y3 - y)) / area
                if w1 >= 0 and w2 >= 0 and w1 + w2 <= 1:
                    image[SIZE - 1 - y][offset + x] = 0


def main(input_path, output_path):
    vertices, triangles = [], []
    for line in input_path.read_text().splitlines():
        parts = line.split()
        if parts and parts[0] == "v":
            vertices.append(tuple(map(float, parts[1:4])))
        elif parts and parts[0] == "f":
            triangles.append(tuple(int(p.split("/")[0]) - 1 for p in parts[1:4]))

    image = [[255] * (SIZE * 2) for _ in range(SIZE)]
    for view, axis in enumerate((0, 2)):
        points = [((v[axis] + 0.75) / 1.5 * SIZE, v[1] * (SIZE - 10) + 5) for v in vertices]
        rasterize(image, view * SIZE, points, triangles)
    write_png(output_path, SIZE * 2, SIZE, image)


if __name__ == "__main__":
    if len(sys.argv) != 3:
        sys.exit(__doc__)
    main(pathlib.Path(sys.argv[1]), pathlib.Path(sys.argv[2]))
