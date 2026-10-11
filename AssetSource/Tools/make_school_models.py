"""Builds the props of the School Bench with Blender and writes them to AssetSource/Models:

  schoolscroll.glb  an open parchment scroll, rolled up at both ends on wooden rods, written in runes, 0.30 x 0.44 m
  waxtablet.glb     an open wax tablet: two wooden frames with dark wax scratched with runes, and a bone stylus
  inkhorn.glb       a cow horn of ink in a wooden stand, with a goose quill in it, 0.2 m high
  openbook.glb      an open book in leather covers, its pages written in runes, 0.34 x 0.24 m

The runes are drawn into the textures from a small set of Elder Futhark strokes. Sizes are only proportions: the
bundle stores every model one unit high, the code scales it.

Usage: python make_school_models.py [--blender <blender.exe>]
"""
import json
import math
import pathlib
import struct
import subprocess
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import make_treasure_models as treasure  # noqa: E402  (shared noise textures, materials, UVs and asset info)

MODELS = pathlib.Path(__file__).resolve().parents[1] / "Models"
NAMES = ("schoolscroll", "waxtablet", "inkhorn", "openbook")
TITLES = {
    "schoolscroll": "Open scroll",
    "waxtablet": "Wax tablet with stylus",
    "inkhorn": "Inkhorn with quill",
    "openbook": "Open book",
}
SEED = 23

# Elder Futhark runes as strokes in a 1 x 1 cell, y down: (x0, y0, x1, y1).
RUNES = [
    [(0.2, 0, 0.2, 1), (0.2, 0.35, 0.7, 0.1), (0.2, 0.6, 0.7, 0.35)],
    [(0.2, 0, 0.2, 1), (0.2, 0, 0.75, 0.3), (0.75, 0.3, 0.75, 1)],
    [(0.2, 0, 0.2, 1), (0.2, 0.25, 0.65, 0.5), (0.65, 0.5, 0.2, 0.75)],
    [(0.2, 0, 0.2, 1), (0.2, 0.1, 0.7, 0.35), (0.2, 0.4, 0.7, 0.65)],
    [(0.2, 0, 0.2, 1), (0.2, 0, 0.7, 0.25), (0.7, 0.25, 0.2, 0.5), (0.2, 0.5, 0.7, 1)],
    [(0.6, 0, 0.2, 0.4), (0.2, 0.4, 0.6, 0.8)],
    [(0.1, 0, 0.7, 1), (0.7, 0, 0.1, 1)],
    [(0.15, 0, 0.15, 1), (0.75, 0, 0.75, 1), (0.15, 0.3, 0.75, 0.6)],
    [(0.45, 0, 0.45, 1), (0.2, 0.35, 0.7, 0.6)],
    [(0.45, 0, 0.45, 1)],
    [(0.7, 0, 0.2, 0.4), (0.2, 0.4, 0.7, 0.6), (0.7, 0.6, 0.2, 1)],
    [(0.45, 0, 0.45, 1), (0.45, 0, 0.1, 0.3), (0.45, 0, 0.8, 0.3)],
    [(0.2, 0, 0.2, 1), (0.2, 0, 0.7, 0.25), (0.7, 0.25, 0.2, 0.5), (0.2, 0.5, 0.7, 0.75), (0.7, 0.75, 0.2, 1)],
    [(0.15, 0, 0.15, 1), (0.75, 0, 0.75, 1), (0.15, 0, 0.75, 0.5), (0.75, 0, 0.15, 0.5)],
    [(0.2, 0, 0.2, 1), (0.2, 0, 0.7, 0.3)],
    [(0.45, 0, 0.45, 1), (0.45, 0.4, 0.1, 0), (0.45, 0.4, 0.8, 0)],
]


def draw_line(image, x0, y0, x1, y1, colour, width):
    """Paints a line of the given width (pixels) into an H x W x 3 float image, rows counted from the top."""
    height, size = image.shape[0], image.shape[1]
    steps = int(max(abs(x1 - x0), abs(y1 - y0)) * 2) + 1
    radius = max(0, int(round(width / 2)))
    for step in range(steps + 1):
        t = step / steps
        x, y = int(round(x0 + (x1 - x0) * t)), int(round(y0 + (y1 - y0) * t))
        row = height - 1 - y
        image[max(0, row - radius):min(height, row + radius + 1), max(0, x - radius):min(size, x + radius + 1)] = colour


def write_runes(image, rng, box, cell, colour, width, rows_gap=1.6, dot=True):
    """Fills box (x0, y0, x1, y1 in pixels, y down) with lines of runes, split into words by dots."""
    x0, y0, x1, y1 = box
    y = y0
    while y + cell <= y1:
        x = x0
        word = rng.integers(2, 7)
        while x + cell * 0.8 <= x1:
            if word == 0:
                if dot:
                    draw_line(image, x + cell * 0.2, y + cell * 0.5, x + cell * 0.2, y + cell * 0.5, colour, width * 1.6)
                x += cell * 0.5
                word = rng.integers(2, 7)
                continue
            for sx, sy, ex, ey in RUNES[rng.integers(len(RUNES))]:
                jitter = rng.normal(0, 0.03, 4)
                draw_line(image, x + (sx + jitter[0]) * cell * 0.7, y + (sy + jitter[1]) * cell,
                          x + (ex + jitter[2]) * cell * 0.7, y + (ey + jitter[3]) * cell, colour, width)
            x += cell * 0.75
            word -= 1
        y += cell * rows_gap


def parchment(np, rng, size, base=(0.86, 0.77, 0.58), dark=(0.66, 0.55, 0.36)):
    """A parchment image of the given size, darker towards the edges."""
    treasure.TEXTURE_SIZE = size
    image = treasure.noise_texture(np, rng, base, dark, (size // 6, size // 20, size // 80 or 2), contrast=1.1)
    coords = np.linspace(-1, 1, size)
    edge = np.clip(np.maximum(abs(coords)[None, :], abs(coords)[:, None]) - 0.75, 0, 1) * 1.2
    return np.clip(image * (1 - edge[..., None] * 0.35), 0, 1)


def image_material(bpy, np, name, pixels, roughness=0.9):
    size = pixels.shape[0]
    image = bpy.data.images.new(name, size, size, alpha=False)
    rgba = np.concatenate([pixels, np.ones((size, size, 1))], axis=2).astype(np.float32)
    image.pixels.foreach_set(rgba.ravel())
    image.pack()
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    nodes, links = material.node_tree.nodes, material.node_tree.links
    shader = nodes["Principled BSDF"]
    shader.inputs["Roughness"].default_value = roughness
    node = nodes.new("ShaderNodeTexImage")
    node.image = image
    links.new(node.outputs["Color"], shader.inputs["Base Color"])
    return material


def noise_material(bpy, np, rng, name, base, dark, cells, contrast=1.0, streaks=0.0):
    treasure.TEXTURE_SIZE = 256
    return treasure.make_material(bpy, np, name, treasure.noise_texture(np, rng, base, dark, cells, contrast, streaks))


def planar_uv(obj, x_range, y_range):
    """UVs from the object's x and y over the given ranges, so a texture lies flat on it as drawn."""
    uv_layer = obj.data.uv_layers.new(name="UVMap")
    for loop in obj.data.loops:
        co = obj.data.vertices[loop.vertex_index].co
        uv_layer.data[loop.index].uv = ((co.x - x_range[0]) / (x_range[1] - x_range[0]),
                                        (co.y - y_range[0]) / (y_range[1] - y_range[0]))


def grid(bpy, bmesh, name, columns, rows, point):
    """A sheet of (columns x rows) quads; point(u, v) gives the corner positions, u and v in 0..1."""
    mesh = bpy.data.meshes.new(name)
    bm = bmesh.new()
    verts = [[bm.verts.new(point(c / columns, r / rows)) for c in range(columns + 1)] for r in range(rows + 1)]
    for r in range(rows):
        for c in range(columns):
            bm.faces.new((verts[r][c], verts[r][c + 1], verts[r + 1][c + 1], verts[r + 1][c]))
    bm.to_mesh(mesh)
    bm.free()
    return treasure.link(bpy, mesh, name)


def box(bpy, bmesh, name, low, high):
    mesh = bpy.data.meshes.new(name)
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    for vert in bm.verts:
        vert.co = tuple(low[a] + (vert.co[a] + 0.5) * (high[a] - low[a]) for a in range(3))
    bm.to_mesh(mesh)
    bm.free()
    return treasure.link(bpy, mesh, name)


def cylinder(bpy, bmesh, name, radius1, radius2, start, end, segments=16, caps=True):
    """A cylinder or cone from start to end (Blender coordinates)."""
    import mathutils

    mesh = bpy.data.meshes.new(name)
    bm = bmesh.new()
    axis = mathutils.Vector([e - s for s, e in zip(start, end)])
    bmesh.ops.create_cone(bm, cap_ends=caps, segments=segments, radius1=radius1, radius2=radius2, depth=axis.length)
    # create_cone builds along +z around the origin.
    rotation = mathutils.Vector((0, 0, 1)).rotation_difference(axis.normalized()).to_matrix()
    middle = mathutils.Vector([(s + e) / 2 for s, e in zip(start, end)])
    for vert in bm.verts:
        vert.co = rotation @ vert.co + middle
    bm.to_mesh(mesh)
    bm.free()
    return treasure.link(bpy, mesh, name)


def build_scroll(bpy, bmesh, np, rng):
    width, length, radius = 0.30, 0.40, 0.018
    page = parchment(np, rng, 512)
    write_runes(page, rng, (60, 50, 452, 470), 22, (0.16, 0.1, 0.06), 1.4)
    # A red rune at the start, the way scribes marked a new text.
    for sx, sy, ex, ey in RUNES[3]:
        draw_line(page, 30 + sx * 24, 40 + sy * 34, 30 + ex * 24, 40 + ey * 34, (0.5, 0.08, 0.05), 2.2)
    sheet_material = image_material(bpy, np, "schoolscroll_sheet", page)
    roll_material = noise_material(bpy, np, rng, "schoolscroll_roll", (0.84, 0.74, 0.55), (0.62, 0.5, 0.33), (48, 16, 4), 1.1)
    wood = noise_material(bpy, np, rng, "schoolscroll_wood", (0.42, 0.28, 0.16), (0.2, 0.12, 0.07), (32, 8), 1.2, 0.6)

    # The sheet lies on the table and rises into the rolls at both ends.
    def point(u, v):
        y = (v - 0.5) * length
        rise = max(0.0, abs(y) - (length / 2 - radius * 1.5)) / (radius * 1.5)
        return ((u - 0.5) * width, y, 0.001 + radius * rise * rise + 0.002 * math.sin(u * 7 + v * 3))

    sheet = grid(bpy, bmesh, "sheet", 12, 20, point)
    planar_uv(sheet, (-width / 2, width / 2), (-length / 2, length / 2))
    sheet.data.materials.append(sheet_material)
    parts = [sheet]
    for sign in (-1, 1):
        y = sign * length / 2
        roll = cylinder(bpy, bmesh, f"roll{sign}", radius, radius, (-width / 2, y, radius), (width / 2, y, radius), 20)
        parts.append(treasure.finish(bpy, roll, roll_material))
        rod = cylinder(bpy, bmesh, f"rod{sign}", 0.007, 0.007, (-width / 2 - 0.025, y, radius), (width / 2 + 0.025, y, radius), 10)
        parts.append(treasure.finish(bpy, rod, wood))
        for end in (-1, 1):
            knob = cylinder(bpy, bmesh, f"knob{sign}{end}", 0.011, 0.011,
                            (end * (width / 2 + 0.018), y, radius), (end * (width / 2 + 0.03), y, radius), 12)
            parts.append(treasure.finish(bpy, knob, wood))
    return parts


def build_tablet(bpy, bmesh, np, rng):
    leaf_w, leaf_h, frame, depth, sink = 0.13, 0.18, 0.012, 0.014, 0.004
    wood = noise_material(bpy, np, rng, "waxtablet_wood", (0.5, 0.34, 0.2), (0.24, 0.15, 0.08), (32, 8), 1.2, 0.6)
    wax_image = np.ones((256, 256, 3)) * np.array((0.16, 0.13, 0.07))
    wax_image *= 0.85 + 0.3 * rng.random((256, 256, 1))
    write_runes(wax_image, rng, (14, 16, 242, 240), 18, (0.55, 0.47, 0.3), 1.0, dot=False)
    wax = image_material(bpy, np, "waxtablet_wax", np.clip(wax_image, 0, 1), roughness=0.5)
    bone = noise_material(bpy, np, rng, "waxtablet_bone", (0.88, 0.84, 0.74), (0.66, 0.6, 0.5), (24, 6), 1.1)
    leather = noise_material(bpy, np, rng, "waxtablet_leather", (0.36, 0.22, 0.12), (0.18, 0.1, 0.05), (32, 8), 1.2)
    parts = []
    for sign in (-1, 1):
        x0 = 0.004 if sign > 0 else -0.004 - leaf_w
        x1 = x0 + leaf_w
        y0, y1 = -leaf_h / 2, leaf_h / 2
        parts.append(treasure.finish(bpy, box(bpy, bmesh, f"base{sign}", (x0, y0, 0), (x1, y1, depth - sink)), wood, smooth=False))
        for name, low, high in (("l", (x0, y0, 0), (x0 + frame, y1, depth)), ("r", (x1 - frame, y0, 0), (x1, y1, depth)),
                                ("b", (x0, y0, 0), (x1, y0 + frame, depth)), ("t", (x0, y1 - frame, 0), (x1, y1, depth))):
            parts.append(treasure.finish(bpy, box(bpy, bmesh, f"frame{sign}{name}", low, high), wood, smooth=False))
        panel = box(bpy, bmesh, f"wax{sign}", (x0 + frame, y0 + frame, depth - sink), (x1 - frame, y1 - frame, depth - sink + 0.0015))
        planar_uv(panel, (x0 + frame, x1 - frame), (y0 + frame, y1 - frame))
        panel.data.materials.append(wax)
        parts.append(panel)
    for y in (-0.05, 0.05):
        parts.append(treasure.finish(bpy, box(bpy, bmesh, f"hinge{y}", (-0.012, y - 0.012, depth * 0.3), (0.012, y + 0.012, depth * 0.6)),
                                     leather, smooth=False))
    stylus = cylinder(bpy, bmesh, "stylus", 0.0035, 0.0012, (leaf_w + 0.02, -0.07, 0.004), (leaf_w + 0.035, 0.07, 0.004), 8)
    parts.append(treasure.finish(bpy, stylus, bone))
    spatula = box(bpy, bmesh, "stylus_end", (leaf_w + 0.012, -0.09, 0.001), (leaf_w + 0.026, -0.07, 0.006))
    parts.append(treasure.finish(bpy, spatula, bone, smooth=False))
    return parts


def build_inkhorn(bpy, bmesh, np, rng):
    wood = noise_material(bpy, np, rng, "inkhorn_wood", (0.45, 0.3, 0.17), (0.22, 0.14, 0.07), (32, 8), 1.2, 0.6)
    horn = noise_material(bpy, np, rng, "inkhorn_horn", (0.62, 0.52, 0.36), (0.22, 0.17, 0.12), (40, 10, 3), 1.3, 0.3)
    ink = noise_material(bpy, np, rng, "inkhorn_ink", (0.04, 0.03, 0.03), (0.0, 0.0, 0.0), (16, 4), 1.0)
    feather = noise_material(bpy, np, rng, "inkhorn_feather", (0.92, 0.9, 0.86), (0.62, 0.6, 0.56), (64, 6), 1.0, 0.7)
    parts = [treasure.finish(bpy, cylinder(bpy, bmesh, "stand", 0.04, 0.036, (0, 0, 0), (0, 0, 0.025), 20), wood)]
    # The horn curves a little and stands with its tip in the stand.
    segments = 8
    for index in range(segments):
        t0, t1 = index / segments, (index + 1) / segments
        start = (0.012 * t0 * t0, 0, 0.012 + 0.1 * t0)
        end = (0.012 * t1 * t1, 0, 0.012 + 0.1 * t1)
        parts.append(treasure.finish(bpy, cylinder(bpy, bmesh, f"horn{index}", 0.008 + 0.018 * t0, 0.008 + 0.018 * t1, start, end, 18, caps=False), horn))
    parts.append(treasure.finish(bpy, cylinder(bpy, bmesh, "ink", 0.024, 0.024, (0.012, 0, 0.104), (0.012, 0, 0.106), 18), ink))
    # A goose quill leaning out of the horn: a shaft and a vane that narrows to the tip.
    base, tip = (0.012, 0.0, 0.07), (0.07, 0.02, 0.24)
    parts.append(treasure.finish(bpy, cylinder(bpy, bmesh, "shaft", 0.0025, 0.0012, base, tip, 8), feather))

    def vane(u, v):
        x = base[0] + (tip[0] - base[0]) * (0.35 + 0.65 * v)
        y = base[1] + (tip[1] - base[1]) * (0.35 + 0.65 * v)
        z = base[2] + (tip[2] - base[2]) * (0.35 + 0.65 * v)
        half = 0.014 * math.sin(math.pi * min(1.0, v * 1.15)) * (1.0 if v < 0.9 else (1 - v) * 10)
        return (x - (u - 0.4) * half * 2, y + (u - 0.4) * half * 0.4, z + 0.002 * math.sin(u * 3))

    parts.append(treasure.finish(bpy, grid(bpy, bmesh, "vane", 4, 16, vane), feather))
    return parts


def build_book(bpy, bmesh, np, rng):
    page_w, page_h, block, cover = 0.16, 0.22, 0.02, 0.006
    leather = noise_material(bpy, np, rng, "openbook_leather", (0.36, 0.2, 0.11), (0.16, 0.08, 0.04), (40, 10, 3), 1.3)
    edges = noise_material(bpy, np, rng, "openbook_edges", (0.82, 0.74, 0.58), (0.6, 0.52, 0.38), (64, 2), 1.0, 0.8)
    parts = [treasure.finish(bpy, box(bpy, bmesh, "cover", (-page_w - 0.012, -page_h / 2 - 0.01, 0), (page_w + 0.012, page_h / 2 + 0.01, cover)),
                             leather, smooth=False)]
    for sign in (-1, 1):
        image = parchment(np, rng, 256)
        write_runes(image, rng, (26, 22, 232, 236), 13, (0.16, 0.1, 0.06), 1.0)
        material = image_material(bpy, np, f"openbook_page{sign}", image)

        # The pages bow up from the spine and fall to the outer edge.
        def surface(u, v, sign=sign):
            x = sign * u * page_w
            return (x, (v - 0.5) * page_h, cover + block * (0.55 + 0.45 * math.sin(math.pi * min(1.0, u * 1.3))) * (1 - 0.6 * u))

        top = grid(bpy, bmesh, f"page{sign}", 10, 6, surface)
        planar_uv(top, (min(0, sign * page_w), max(0, sign * page_w)), (-page_h / 2, page_h / 2))
        top.data.materials.append(material)
        parts.append(top)
        side = box(bpy, bmesh, f"block{sign}", (min(0, sign * page_w) + 0.002, -page_h / 2, cover), (max(0, sign * page_w) - 0.002, page_h / 2, cover + block * 0.45))
        parts.append(treasure.finish(bpy, side, edges, smooth=False))
    parts.append(treasure.finish(bpy, cylinder(bpy, bmesh, "spine", 0.008, 0.008, (0, -page_h / 2, cover + 0.004), (0, page_h / 2, cover + 0.004), 10), leather))
    return parts


def build_in_blender(folder):
    import bmesh
    import bpy
    import numpy as np

    builders = {"schoolscroll": build_scroll, "waxtablet": build_tablet, "inkhorn": build_inkhorn, "openbook": build_book}
    for name in NAMES:
        bpy.ops.wm.read_factory_settings(use_empty=True)
        rng = np.random.default_rng(SEED + NAMES.index(name))
        parts = builders[name](bpy, bmesh, np, rng)
        bpy.ops.object.select_all(action="DESELECT")
        for obj in parts:
            obj.select_set(True)
        output = pathlib.Path(folder) / f"{name}.glb"
        bpy.ops.export_scene.gltf(filepath=str(output), export_format="GLB", use_selection=True, export_yup=True)
        faces = sum(len(obj.data.polygons) for obj in parts)
        print(f"SCHOOL {name}: {len(parts)} parts, {faces} faces")


def add_asset_info(path, title):
    data = path.read_bytes()
    json_length = struct.unpack_from("<I", data, 12)[0]
    gltf = json.loads(data[20:20 + json_length])
    gltf["asset"]["extras"] = {
        "title": title,
        "author": "BrudvikWhiteHilt (generated by AssetSource/Tools/make_school_models.py)",
        "license": "Same as BrudvikWhiteHilt",
    }
    chunk = json.dumps(gltf, separators=(",", ":")).encode("utf-8")
    chunk += b" " * (-len(chunk) % 4)
    rest = data[20 + json_length:]
    header = struct.pack("<4sII", b"glTF", 2, 12 + 8 + len(chunk) + len(rest))
    path.write_bytes(header + struct.pack("<I4s", len(chunk), b"JSON") + chunk + rest)


def main():
    if "bpy" in sys.modules:
        build_in_blender(sys.argv[sys.argv.index("--") + 1])
        return

    blender = sys.argv[sys.argv.index("--blender") + 1] if "--blender" in sys.argv else None
    if blender is None:
        found = sorted(pathlib.Path(r"C:\Program Files\Blender Foundation").glob("*/blender.exe"))
        if not found:
            raise SystemExit("Blender not found; pass --blender")
        blender = str(found[-1])
    result = subprocess.run([blender, "-b", "--factory-startup", "--python-exit-code", "1", "--python", __file__, "--", str(MODELS)],
                            capture_output=True, text=True, encoding="utf-8", errors="replace")
    for line in result.stdout.splitlines():
        if line.startswith("SCHOOL"):
            print(line)
    if result.returncode != 0:
        print(result.stdout[-3000:], result.stderr[-3000:])
        raise SystemExit("Blender failed")
    for name in NAMES:
        path = MODELS / f"{name}.glb"
        add_asset_info(path, TITLES[name])
        print(f"Wrote {path}")


if __name__ == "__main__":
    main()
