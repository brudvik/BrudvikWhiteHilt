"""Builds the White Hilt fishing net with Blender and writes AssetSource/Models/fishnet.glb.

A 10 m stretch of net as it hangs in the water: a head rope with wooden floats at the water line, a diamond mesh of
twine that bellies out a little with the current, and a foot rope weighed down by stones. The twine is real geometry
(thin four-sided strands), so it needs no transparent texture. Each part only has a base colour; the converter packs
them into one atlas.

The water line is y 0 in glTF space. The sizes must match FishingNet (NetLength, NetBelowWater) in the mod.

Usage: python make_fishing_net.py [--blender <blender.exe>]
"""
import json
import math
import pathlib
import struct
import subprocess
import sys

LENGTH = 10.0
DEPTH = 2.4
MESH = 0.4
TWINE = 0.014
BELLY = 0.25
BELLY_STEPS = 4
HEAD_ROPE = 0.024
FOOT_ROPE = 0.02
ROPE_SIDES = 6
FLOAT_SPACING = 0.8
FLOAT_RADIUS = 0.1
FLOAT_LENGTH = 0.24
FLOAT_Y = 0.03
STONE_SPACING = 1.25
STONE_SIZE = (0.15, 0.1, 0.12)
STONE_Y = -DEPTH - 0.02

COLOURS = {
    "Twine": (0.30, 0.26, 0.19),
    "Rope": (0.48, 0.39, 0.26),
    "Float": (0.58, 0.43, 0.26),
    "Stone": (0.40, 0.40, 0.38),
}

OUTPUT = pathlib.Path(__file__).resolve().parents[1] / "Models" / "fishnet.glb"


def belly(y):
    """How far the net bellies out (glTF z) at depth y; 0 at both ropes."""
    return BELLY * math.sin(math.pi * min(max(-y / DEPTH, 0.0), 1.0))


def strands():
    """Diagonal twine strands clipped to the net, as lists of glTF points."""
    lines = []
    offset = -LENGTH / 2 - DEPTH
    while offset < LENGTH / 2 + DEPTH:
        for slope in (1.0, -1.0):
            # Line y = slope * (x - offset) for y in [-DEPTH, 0], clipped to x in [-LENGTH/2, LENGTH/2].
            x_top, x_bottom = offset, offset - DEPTH / slope
            x0, x1 = sorted((x_top, x_bottom))
            x0, x1 = max(x0, -LENGTH / 2), min(x1, LENGTH / 2)
            if x1 - x0 < MESH * 0.25:
                continue
            points = []
            for step in range(BELLY_STEPS + 1):
                x = x0 + (x1 - x0) * step / BELLY_STEPS
                y = slope * (x - offset)
                points.append((x, y, belly(y)))
            lines.append(points)
        offset += MESH
    return lines


def build_in_blender(output):
    import bmesh
    import bpy
    from mathutils import Matrix, Vector

    bpy.ops.wm.read_factory_settings(use_empty=True)

    def blender(point):
        x, y, z = point
        return Vector((x, -z, y))

    mesh = bpy.data.meshes.new("fishnet")
    bm = bmesh.new()
    uv_layer = bm.loops.layers.uv.new("UVMap")
    names = list(COLOURS)

    def tube(points, radius, sides, material):
        points = [blender(p) for p in points]
        rings = []
        for i, point in enumerate(points):
            ahead = points[min(i + 1, len(points) - 1)] - points[max(i - 1, 0)]
            ahead.normalize()
            side = ahead.cross(Vector((0.0, 0.0, 1.0)))
            if side.length < 1e-4:
                side = ahead.cross(Vector((1.0, 0.0, 0.0)))
            side.normalize()
            up = side.cross(ahead)
            rings.append([bm.verts.new(point + (side * math.cos(2 * math.pi * k / sides) + up * math.sin(2 * math.pi * k / sides)) * radius)
                          for k in range(sides)])
        for a, b in zip(rings, rings[1:]):
            for k in range(sides):
                face = bm.faces.new([a[k], a[(k + 1) % sides], b[(k + 1) % sides], b[k]])
                face.material_index = names.index(material)
        for ring in (rings[0], list(reversed(rings[-1]))):
            face = bm.faces.new(ring)
            face.material_index = names.index(material)

    def blob(centre, size, material, kind):
        before = set(bm.verts)
        matrix = Matrix.Translation(blender(centre)) @ Matrix.Diagonal((size[0], size[2], size[1], 1.0))
        if kind == "float":
            bmesh.ops.create_uvsphere(bm, u_segments=8, v_segments=5, radius=1.0, matrix=matrix)
        else:
            bmesh.ops.create_icosphere(bm, subdivisions=1, radius=1.0, matrix=matrix)
        for face in {face for vert in set(bm.verts) - before for face in vert.link_faces}:
            face.material_index = names.index(material)

    for points in strands():
        tube(points, TWINE, 4, "Twine")
    tube([(-LENGTH / 2, 0.0, 0.0), (LENGTH / 2, 0.0, 0.0)], HEAD_ROPE, ROPE_SIDES, "Rope")
    tube([(-LENGTH / 2, -DEPTH, 0.0), (LENGTH / 2, -DEPTH, 0.0)], FOOT_ROPE, ROPE_SIDES, "Rope")

    count = int((LENGTH - FLOAT_SPACING) / FLOAT_SPACING) + 1
    start = -(count - 1) * FLOAT_SPACING / 2
    for i in range(count):
        blob((start + i * FLOAT_SPACING, FLOAT_Y, 0.0), (FLOAT_LENGTH / 2, FLOAT_RADIUS, FLOAT_RADIUS), "Float", "float")

    count = int((LENGTH - STONE_SPACING) / STONE_SPACING) + 1
    start = -(count - 1) * STONE_SPACING / 2
    for i in range(count):
        blob((start + i * STONE_SPACING, STONE_Y, 0.0), STONE_SIZE, "Stone", "stone")

    for face in bm.faces:
        face.smooth = face.material_index != names.index("Twine")
        for loop in face.loops:
            loop[uv_layer].uv = (0.5, 0.5)

    bm.normal_update()
    bm.to_mesh(mesh)
    bm.free()

    for name in names:
        material = bpy.data.materials.new(name)
        material.use_nodes = True
        shader = material.node_tree.nodes["Principled BSDF"]
        shader.inputs["Base Color"].default_value = (*COLOURS[name], 1.0)
        shader.inputs["Roughness"].default_value = 0.9
        mesh.materials.append(material)

    obj = bpy.data.objects.new("fishnet", mesh)
    bpy.context.scene.collection.objects.link(obj)
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.ops.export_scene.gltf(filepath=str(output), export_format="GLB", use_selection=True, export_yup=True)

    ys = [v.co.z for v in mesh.vertices]
    xs = [v.co.x for v in mesh.vertices]
    print(f"FISHNET {len(mesh.vertices)} vertices, {len(mesh.polygons)} faces, x {min(xs):.3f}..{max(xs):.3f}, "
          f"y {min(ys):.3f}..{max(ys):.3f} (water line 0)")


def add_asset_info(path):
    """Writes the author and license into asset.extras, where glb_info.py and the model skill look for them."""
    data = path.read_bytes()
    json_length = struct.unpack_from("<I", data, 12)[0]
    gltf = json.loads(data[20:20 + json_length])
    gltf["asset"]["extras"] = {
        "title": "White Hilt fishing net",
        "author": "BrudvikWhiteHilt (generated by AssetSource/Tools/make_fishing_net.py)",
        "license": "Same as BrudvikWhiteHilt",
    }
    chunk = json.dumps(gltf, separators=(",", ":")).encode("utf-8")
    chunk += b" " * (-len(chunk) % 4)
    rest = data[20 + json_length:]
    header = struct.pack("<4sII", b"glTF", 2, 12 + 8 + len(chunk) + len(rest))
    path.write_bytes(header + struct.pack("<I4s", len(chunk), b"JSON") + chunk + rest)


def main():
    if "bpy" in sys.modules:
        build_in_blender(pathlib.Path(sys.argv[sys.argv.index("--") + 1]))
        return

    blender = sys.argv[sys.argv.index("--blender") + 1] if "--blender" in sys.argv else None
    if blender is None:
        found = sorted(pathlib.Path(r"C:\Program Files\Blender Foundation").glob("*/blender.exe"))
        if not found:
            raise SystemExit("Blender not found; pass --blender")
        blender = str(found[-1])
    result = subprocess.run([blender, "-b", "--factory-startup", "--python-exit-code", "1", "--python", __file__, "--", str(OUTPUT)],
                            capture_output=True, text=True, encoding="utf-8", errors="replace")
    for line in result.stdout.splitlines():
        if line.startswith("FISHNET"):
            print(line)
    if result.returncode != 0 or not OUTPUT.exists():
        print(result.stdout[-3000:], result.stderr[-3000:])
        raise SystemExit("Blender failed")
    add_asset_info(OUTPUT)
    print(f"Wrote {OUTPUT}")


if __name__ == "__main__":
    main()
