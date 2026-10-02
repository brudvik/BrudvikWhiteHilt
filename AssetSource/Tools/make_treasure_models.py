"""Builds the treasure hunt models with Blender and writes them to AssetSource/Models:

  treasuremound.glb  a low heap of freshly turned earth with a few clods and pebbles, 1.8 x 1.5 m, 0.35 m high
  treasurecairn.glb  a small cairn of stacked stones with a stick standing in it and a faded red rag tied on, 1.7 m
  treasuremap.glb    a rolled-up parchment map with a red ribbon and a wax seal, 0.32 m long

Every part gets its own small noise texture (earth, stone, wood, cloth, parchment, wax). convert_glb.py packs them into
one atlas per model. Sizes are only proportions: the bundle stores every model one unit high, the code scales it.

Usage: python make_treasure_models.py [--blender <blender.exe>]
"""
import json
import math
import pathlib
import struct
import subprocess
import sys

MODELS = pathlib.Path(__file__).resolve().parents[1] / "Models"
NAMES = ("treasuremound", "treasurecairn", "treasuremap")
TEXTURE_SIZE = 256
SEED = 11


def noise_texture(np, rng, base, dark, cells, contrast=1.0, streaks=0.0):
    """An sRGB float image: fractal value noise between dark and base, optionally with vertical streaks (wood grain)."""
    size = TEXTURE_SIZE

    def layer(cell):
        count = max(2, size // cell + 2)
        lattice = rng.random((count, count))
        coords = np.arange(size) / cell
        i0 = np.floor(coords).astype(int)
        f = coords - i0
        f = f * f * (3 - 2 * f)
        a = lattice[np.ix_(i0, i0)]
        b = lattice[np.ix_(i0, i0 + 1)]
        c = lattice[np.ix_(i0 + 1, i0)]
        d = lattice[np.ix_(i0 + 1, i0 + 1)]
        fx, fy = f[None, :], f[:, None]
        return (a * (1 - fx) + b * fx) * (1 - fy) + (c * (1 - fx) + d * fx) * fy

    value, weight, norm = 0.0, 1.0, 0.0
    for octave, cell in enumerate(cells):
        value = value + layer(cell) * weight
        norm += weight
        weight *= 0.55
    value = value / norm
    if streaks > 0.0:
        columns = rng.random(size)
        grain = np.convolve(columns, np.ones(5) / 5, mode="same")[None, :].repeat(size, axis=0)
        value = value * (1 - streaks) + grain * streaks
    value = np.clip((value - 0.5) * contrast + 0.5, 0.0, 1.0)[..., None]
    base, dark = np.array(base), np.array(dark)
    return np.clip(dark + (base - dark) * value, 0.0, 1.0)


def make_material(bpy, np, name, pixels):
    image = bpy.data.images.new(name, TEXTURE_SIZE, TEXTURE_SIZE, alpha=False)
    rgba = np.concatenate([pixels, np.ones((TEXTURE_SIZE, TEXTURE_SIZE, 1))], axis=2).astype(np.float32)
    image.pixels.foreach_set(rgba.ravel())
    image.pack()
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    nodes, links = material.node_tree.nodes, material.node_tree.links
    shader = nodes["Principled BSDF"]
    shader.inputs["Roughness"].default_value = 0.9
    node = nodes.new("ShaderNodeTexImage")
    node.image = image
    links.new(node.outputs["Color"], shader.inputs["Base Color"])
    return material


def finish(bpy, obj, material, smooth=True, top_extent=None):
    """Gives the object its material and UVs inside 0..1: projected from above over top_extent metres, else islands."""
    obj.data.materials.append(material)
    for polygon in obj.data.polygons:
        polygon.use_smooth = smooth
    if top_extent is not None:
        uv_layer = obj.data.uv_layers.new(name="UVMap")
        for loop in obj.data.loops:
            co = obj.data.vertices[loop.vertex_index].co
            uv_layer.data[loop.index].uv = (0.5 + co.x / (2 * top_extent), 0.5 + co.y / (2 * top_extent))
        return obj
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(island_margin=0.02)
    bpy.ops.object.mode_set(mode="OBJECT")
    return obj


def link(bpy, mesh, name):
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    return obj


def rock(bpy, bmesh, rng, name, centre, size, flatten=0.6, subdivisions=2):
    """A lumpy stone: an icosphere pushed in and out by random waves, flattened top and bottom so it stacks."""
    mesh = bpy.data.meshes.new(name)
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=subdivisions, radius=1.0)
    waves = [(rng.normal(size=3), rng.uniform(1.5, 3.5), rng.uniform(0, 6.28), rng.uniform(0.04, 0.1)) for _ in range(5)]
    for vert in bm.verts:
        co = vert.co.copy()
        bump = sum(amplitude * math.sin(frequency * (co.x * d[0] + co.y * d[1] + co.z * d[2]) + phase)
                   for d, frequency, phase, amplitude in waves)
        co = co * (1.0 + bump)
        co.z = math.copysign(min(abs(co.z), flatten + 0.15 * abs(co.z)), co.z)
        vert.co = (centre[0] + co.x * size[0], centre[1] + co.y * size[1], centre[2] + co.z * size[2])
    bm.to_mesh(mesh)
    bm.free()
    return link(bpy, mesh, name)


def build_mound(bpy, bmesh, np, rng):
    earth = make_material(bpy, np, "treasuremound_earth",
                          noise_texture(np, rng, (0.3, 0.22, 0.15), (0.12, 0.08, 0.05), (64, 24, 8, 3), contrast=1.6))
    pebble = make_material(bpy, np, "treasuremound_pebble",
                           noise_texture(np, rng, (0.55, 0.53, 0.5), (0.3, 0.29, 0.27), (48, 12, 4), contrast=1.3))
    rings, segments = 14, 40
    radius_x, radius_y, height, skirt = 0.9, 0.75, 0.35, 0.1
    mesh = bpy.data.meshes.new("mound")
    bm = bmesh.new()
    centre = bm.verts.new((0.0, 0.0, height))
    grid = []
    phases = rng.uniform(0, 6.28, 4)
    for ring in range(1, rings + 1):
        t = ring / rings
        row = []
        for segment in range(segments):
            angle = 2 * math.pi * segment / segments
            wobble = 1.0 + 0.08 * math.sin(3 * angle + phases[0]) + 0.05 * math.sin(5 * angle + phases[1])
            r = t * wobble
            lump = 0.06 * math.sin(7 * angle + 9 * t + phases[2]) * math.sin(math.pi * t) + rng.normal(0, 0.012)
            z = height * max(0.0, 1 - t * t) ** 1.3 + lump * height
            if ring == rings:
                z = -skirt
            row.append(bm.verts.new((r * radius_x * math.cos(angle), r * radius_y * math.sin(angle), z)))
        grid.append(row)
    for segment in range(segments):
        nxt = (segment + 1) % segments
        bm.faces.new((centre, grid[0][segment], grid[0][nxt]))
        for ring in range(rings - 1):
            bm.faces.new((grid[ring][segment], grid[ring + 1][segment], grid[ring + 1][nxt], grid[ring][nxt]))
    bm.to_mesh(mesh)
    bm.free()
    parts = [finish(bpy, link(bpy, mesh, "mound"), earth, top_extent=1.0)]

    # Clods of earth and a few stones dug up with it.
    for index in range(9):
        angle = rng.uniform(0, 2 * math.pi)
        distance = rng.uniform(0.15, 0.75)
        x, y = distance * radius_x * math.cos(angle), distance * radius_y * math.sin(angle)
        z = height * max(0.0, 1 - distance * distance) ** 1.3
        size = rng.uniform(0.05, 0.11)
        is_pebble = index % 3 == 0
        obj = rock(bpy, bmesh, rng, f"clod{index}", (x, y, z), (size, size * rng.uniform(0.7, 1.0), size * 0.6),
                   subdivisions=1)
        parts.append(finish(bpy, obj, pebble if is_pebble else earth))
    return parts


def build_cairn(bpy, bmesh, np, rng):
    stone = make_material(bpy, np, "treasurecairn_stone",
                          noise_texture(np, rng, (0.5, 0.49, 0.46), (0.2, 0.2, 0.19), (64, 20, 6, 2), contrast=1.4))
    wood = make_material(bpy, np, "treasurecairn_wood",
                         noise_texture(np, rng, (0.45, 0.33, 0.22), (0.2, 0.14, 0.09), (32, 8), contrast=1.2, streaks=0.6))
    cloth = make_material(bpy, np, "treasurecairn_cloth",
                          noise_texture(np, rng, (0.62, 0.16, 0.12), (0.36, 0.1, 0.08), (40, 10, 3), contrast=1.1))
    parts = []
    layers = [  # (count, ring radius, stone size, z)
        (5, 0.24, (0.17, 0.15, 0.11), 0.08),
        (3, 0.13, (0.14, 0.12, 0.1), 0.26),
        (2, 0.07, (0.11, 0.1, 0.08), 0.42),
        (1, 0.0, (0.09, 0.08, 0.07), 0.55),
    ]
    for level, (count, ring, size, z) in enumerate(layers):
        start = rng.uniform(0, 2 * math.pi)
        for index in range(count):
            angle = start + 2 * math.pi * index / count
            jitter = rng.uniform(0.85, 1.15)
            centre = (ring * math.cos(angle), ring * math.sin(angle), z)
            obj = rock(bpy, bmesh, rng, f"stone{level}_{index}", centre, tuple(s * jitter for s in size))
            parts.append(finish(bpy, obj, stone))

    # The stick stands in the middle of the cairn, leaning a little.
    stick_length, stick_radius, lean = 1.55, 0.028, math.radians(4)
    mesh = bpy.data.meshes.new("stick")
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=8, radius1=stick_radius, radius2=stick_radius * 0.7, depth=stick_length)
    for vert in bm.verts:
        x, y, z = vert.co
        z += stick_length / 2 + 0.15
        vert.co = (x + math.sin(lean) * z, y, z)
    bm.to_mesh(mesh)
    bm.free()
    parts.append(finish(bpy, link(bpy, mesh, "stick"), wood, smooth=False))

    # The rag: a band knotted round the stick near the top and a strip hanging down from it in the wind.
    top = 0.15 + stick_length - 0.12
    knot_x = math.sin(lean) * top
    mesh = bpy.data.meshes.new("rag")
    bm = bmesh.new()
    band = []
    for segment in range(10):
        angle = 2 * math.pi * segment / 10
        r = stick_radius + 0.012
        band.append((bm.verts.new((knot_x + r * math.cos(angle), r * math.sin(angle), top - 0.03)),
                     bm.verts.new((knot_x + r * math.cos(angle), r * math.sin(angle), top + 0.03))))
    for segment in range(10):
        a, b = band[segment], band[(segment + 1) % 10]
        bm.faces.new((a[0], b[0], b[1], a[1]))
    steps, length, width = 10, 0.42, 0.09
    strip = []
    for step in range(steps + 1):
        t = step / steps
        x = knot_x + stick_radius + t * length * 0.85
        z = top - t * length * 0.5 + 0.03 * math.sin(t * 7)
        y = 0.03 * math.sin(t * 5 + 1)
        w = width * (1 - 0.35 * t)
        strip.append((bm.verts.new((x, y, z + w / 2)), bm.verts.new((x, y + 0.01, z - w / 2))))
    for step in range(steps):
        a, b = strip[step], strip[step + 1]
        bm.faces.new((a[0], a[1], b[1], b[0]))
    bm.to_mesh(mesh)
    bm.free()
    parts.append(finish(bpy, link(bpy, mesh, "rag"), cloth))
    return parts


def build_map(bpy, bmesh, np, rng):
    paper = noise_texture(np, rng, (0.86, 0.76, 0.55), (0.62, 0.5, 0.32), (48, 16, 4), contrast=1.2)
    # Faint lines of ink showing through the outside of the roll.
    for row in range(20, TEXTURE_SIZE, 37):
        paper[row:row + 2, 30:220] *= 0.82
    parchment = make_material(bpy, np, "treasuremap_parchment", paper)
    ribbon = make_material(bpy, np, "treasuremap_ribbon",
                           noise_texture(np, rng, (0.58, 0.1, 0.08), (0.36, 0.05, 0.04), (16, 4), contrast=1.0, streaks=0.4))
    wax = make_material(bpy, np, "treasuremap_wax",
                        noise_texture(np, rng, (0.45, 0.06, 0.05), (0.22, 0.03, 0.02), (24, 6), contrast=1.3))
    length, radius = 0.32, 0.034
    parts = []

    def tube(name, r_outer, r_inner, depth, material, x=0.0, segments=24):
        mesh = bpy.data.meshes.new(name)
        bm = bmesh.new()
        outer = bmesh.ops.create_cone(bm, cap_ends=False, segments=segments, radius1=r_outer, radius2=r_outer, depth=depth)
        if r_inner > 0:
            inner = bmesh.ops.create_cone(bm, cap_ends=False, segments=segments, radius1=r_inner, radius2=r_inner, depth=depth)
            bmesh.ops.reverse_faces(bm, faces=[face for face in bm.faces if all(v in inner["verts"] for v in face.verts)])
            for sign in (-1, 1):
                ring_outer = sorted([v for v in outer["verts"] if v.co.z * sign > 0], key=lambda v: math.atan2(v.co.y, v.co.x))
                ring_inner = sorted([v for v in inner["verts"] if v.co.z * sign > 0], key=lambda v: math.atan2(v.co.y, v.co.x))
                for i in range(segments):
                    j = (i + 1) % segments
                    quad = (ring_outer[i], ring_outer[j], ring_inner[j], ring_inner[i])
                    bm.faces.new(quad if sign > 0 else tuple(reversed(quad)))
        for vert in bm.verts:
            vert.co = (vert.co.z + x, vert.co.x, vert.co.y + radius)
        bm.to_mesh(mesh)
        bm.free()
        return finish(bpy, link(bpy, mesh, name), material)

    # The roll is a thick paper tube, so the spiral of the sheet shows at both ends.
    parts.append(tube("roll", radius, radius * 0.45, length, parchment))
    parts.append(tube("inner", radius * 0.45, 0.0, length * 0.98, parchment))
    parts.append(tube("ribbon", radius * 1.08, radius * 0.98, 0.026, ribbon, x=0.02))

    mesh = bpy.data.meshes.new("seal")
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=16, radius1=0.016, radius2=0.014, depth=0.007)
    for vert in bm.verts:
        x, y, z = vert.co
        vert.co = (x + 0.02, y, z + radius * 2.07 + 0.003)
    bm.to_mesh(mesh)
    bm.free()
    parts.append(finish(bpy, link(bpy, mesh, "seal"), wax))
    return parts


def build_in_blender(folder):
    import bmesh
    import bpy
    import numpy as np

    builders = {"treasuremound": build_mound, "treasurecairn": build_cairn, "treasuremap": build_map}
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
        print(f"TREASURE {name}: {len(parts)} parts, {faces} faces")


def add_asset_info(path, title):
    data = path.read_bytes()
    json_length = struct.unpack_from("<I", data, 12)[0]
    gltf = json.loads(data[20:20 + json_length])
    gltf["asset"]["extras"] = {
        "title": title,
        "author": "BrudvikWhiteHilt (generated by AssetSource/Tools/make_treasure_models.py)",
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
        if line.startswith("TREASURE"):
            print(line)
    if result.returncode != 0:
        print(result.stdout[-3000:], result.stderr[-3000:])
        raise SystemExit("Blender failed")
    titles = {"treasuremound": "Dug earth", "treasurecairn": "Cairn with a marker stick", "treasuremap": "Treasure map"}
    for name in NAMES:
        path = MODELS / f"{name}.glb"
        add_asset_info(path, titles[name])
        print(f"Wrote {path}")


if __name__ == "__main__":
    main()
