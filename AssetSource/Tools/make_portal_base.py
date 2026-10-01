"""Builds the stone base under the White Hilt Rune Circle with Blender and writes AssetSource/Models/portalbase.glb.

A round slab of dark, glassy stone: a flat top for the rune circle, a rounded rim down to the ground and a skirt that
reaches below it, so the slab does not float on uneven ground. The texture is mapped from the centre outwards along the
profile (top, rim, skirt), so the rim is not squeezed. The emission map is grey: a band of light on the rim that bleeds
a little into the stone, and faint wisps inside it. The colour and strength are set in code (PortalBaseGlow).

The sizes must match WhiteHiltGroundPortal (BaseRadius, BaseBevel, BaseHeight, BaseSkirt).

Usage: python make_portal_base.py [--blender <blender.exe>]
"""
import json
import math
import pathlib
import struct
import subprocess
import sys

TOP_RADIUS = 2.22
BEVEL = 0.18
HEIGHT = 0.12
SKIRT = 0.30
SEGMENTS = 128
BEVEL_STEPS = 10
TOP_RINGS = (0.5, 1.0, 1.5, 2.0)
TEXTURE_SIZE = 1024
RHO_MAX = 0.49
SEED = 7

OUTPUT = pathlib.Path(__file__).resolve().parents[1] / "Models" / "portalbase.glb"


def profile():
    """(radius, height) from the centre of the top, over the rim, down the skirt."""
    points = [(0.0, HEIGHT)] + [(r, HEIGHT) for r in TOP_RINGS] + [(TOP_RADIUS, HEIGHT)]
    for step in range(1, BEVEL_STEPS + 1):
        t = math.pi / 2 * step / BEVEL_STEPS
        points.append((TOP_RADIUS + BEVEL * math.sin(t), HEIGHT * math.cos(t)))
    points.append((TOP_RADIUS + BEVEL, -SKIRT))
    lengths = [0.0]
    for (r0, y0), (r1, y1) in zip(points, points[1:]):
        lengths.append(lengths[-1] + math.hypot(r1 - r0, y1 - y0))
    return points, lengths


def textures(np, lengths):
    """Albedo and emission as sRGB float arrays (rows from the bottom, like Blender's pixels)."""
    rng = np.random.default_rng(SEED)
    total = lengths[-1]
    rim_start = lengths[len(TOP_RINGS) + 1]
    rim_end = lengths[-2]
    size = TEXTURE_SIZE
    u = (np.arange(size) + 0.5) / size - 0.5
    du, dv = np.meshgrid(u, u)
    rho = np.hypot(du, dv)
    theta = np.arctan2(dv, du)
    s = rho / RHO_MAX * total
    x, y = s * np.cos(theta), s * np.sin(theta)

    def noise(cell):
        count = 256
        lattice = rng.random((count, count))
        gx, gy = x / cell, y / cell
        x0, y0 = np.floor(gx).astype(int), np.floor(gy).astype(int)
        fx, fy = gx - x0, gy - y0
        fx, fy = fx * fx * (3 - 2 * fx), fy * fy * (3 - 2 * fy)
        x0, y0, x1, y1 = x0 % count, y0 % count, (x0 + 1) % count, (y0 + 1) % count
        top = lattice[y0, x0] * (1 - fx) + lattice[y0, x1] * fx
        bottom = lattice[y1, x0] * (1 - fx) + lattice[y1, x1] * fx
        return top * (1 - fy) + bottom * fy

    def fbm(cell, octaves):
        value, weight, norm = 0.0, 1.0, 0.0
        for octave in range(octaves):
            value = value + noise(cell / 2 ** octave) * weight
            norm += weight
            weight *= 0.5
        return value / norm

    def smoothstep(low, high, value):
        t = np.clip((value - low) / (high - low), 0.0, 1.0)
        return t * t * (3 - 2 * t)

    on_top = s < rim_start
    on_rim = (s >= rim_start) & (s <= rim_end)
    rim_middle = (rim_start + rim_end) / 2

    base = np.array([0.045, 0.05, 0.07])
    wisp = np.array([0.15, 0.18, 0.25])
    edge = np.array([0.08, 0.1, 0.14])
    clouds = smoothstep(0.52, 0.85, fbm(0.9, 5))[..., None]
    albedo = base + (wisp - base) * clouds * 0.6
    albedo = albedo * (0.8 + 0.4 * fbm(0.35, 4))[..., None]
    albedo = albedo + rng.normal(0.0, 0.006, (size, size, 1))
    albedo = albedo + (rng.random((size, size)) > 0.9985)[..., None] * 0.1
    rim_band = np.exp(-((s - rim_middle) / (rim_end - rim_start)) ** 2)[..., None]
    albedo = albedo + (edge - albedo) * rim_band * 0.5
    albedo = np.where((s > rim_end)[..., None], albedo * 0.7, albedo)

    variation = 0.8 + 0.2 * fbm(0.5, 3)
    glow = np.exp(-((s - rim_middle) / ((rim_end - rim_start) * 0.45)) ** 2) * variation
    bleed = np.where(s < rim_middle, np.exp(-(rim_middle - s) / 0.3) * 0.45, 0.0)
    inner = np.where(on_top, smoothstep(0.6, 0.9, fbm(0.6, 4)) * 0.18, 0.0)
    emission = np.maximum(glow, bleed) + inner
    emission = np.where(on_top | on_rim, emission, 0.0)
    emission = np.clip(emission, 0.0, 1.0)[..., None].repeat(3, axis=2)
    return np.clip(albedo, 0.0, 1.0), emission


def build_in_blender(output):
    import bmesh
    import bpy
    import numpy as np

    bpy.ops.wm.read_factory_settings(use_empty=True)
    points, lengths = profile()
    total = lengths[-1]

    mesh = bpy.data.meshes.new("portalbase")
    bm = bmesh.new()
    uv_layer = bm.loops.layers.uv.new("UVMap")
    rings = []
    for radius, height in points[1:]:
        rings.append([bm.verts.new((radius * math.cos(2 * math.pi * i / SEGMENTS), radius * math.sin(2 * math.pi * i / SEGMENTS), height))
                      for i in range(SEGMENTS)])
    centre = bm.verts.new((0.0, 0.0, HEIGHT))

    def uv(vertex, length):
        angle = math.atan2(vertex.co.y, vertex.co.x)
        rho = RHO_MAX * length / total
        return 0.5 + rho * math.cos(angle), 0.5 + rho * math.sin(angle)

    def face(verts, ring_lengths):
        created = bm.faces.new(verts)
        created.smooth = True
        for loop, length in zip(created.loops, ring_lengths):
            loop[uv_layer].uv = (0.5, 0.5) if loop.vert is centre else uv(loop.vert, length)
        return created

    for i in range(SEGMENTS):
        j = (i + 1) % SEGMENTS
        face([centre, rings[0][i], rings[0][j]], [0.0, lengths[1], lengths[1]])
        for ring in range(len(rings) - 1):
            inner, outer = lengths[ring + 1], lengths[ring + 2]
            face([rings[ring][i], rings[ring + 1][i], rings[ring + 1][j], rings[ring][j]], [inner, outer, outer, inner])

    bottom = bm.faces.new(list(reversed(rings[-1])))
    bottom.smooth = False
    for loop in bottom.loops:
        loop[uv_layer].uv = uv(loop.vert, total)
    for edge in bottom.edges:
        edge.smooth = False

    bm.normal_update()
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new("portalbase", mesh)
    bpy.context.scene.collection.objects.link(obj)

    albedo, emission = textures(np, lengths)
    material = bpy.data.materials.new("PortalBase")
    material.use_nodes = True
    nodes, links = material.node_tree.nodes, material.node_tree.links
    shader = nodes["Principled BSDF"]
    shader.inputs["Roughness"].default_value = 0.25
    for name, pixels, socket in (("portalbase_albedo", albedo, "Base Color"), ("portalbase_emission", emission, "Emission Color")):
        image = bpy.data.images.new(name, TEXTURE_SIZE, TEXTURE_SIZE, alpha=False)
        rgba = np.concatenate([pixels, np.ones((TEXTURE_SIZE, TEXTURE_SIZE, 1))], axis=2).astype(np.float32)
        image.pixels.foreach_set(rgba.ravel())
        image.pack()
        node = nodes.new("ShaderNodeTexImage")
        node.image = image
        links.new(node.outputs["Color"], shader.inputs[socket])
    shader.inputs["Emission Strength"].default_value = 1.0
    mesh.materials.append(material)

    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.ops.export_scene.gltf(filepath=str(output), export_format="GLB", use_selection=True, export_yup=True)
    print(f"PORTALBASE {len(mesh.vertices)} vertices, {len(mesh.polygons)} faces, profile length {total:.3f} m")


def add_asset_info(path):
    """Writes the author and license into asset.extras, where glb_info.py and the model skill look for them."""
    data = path.read_bytes()
    json_length = struct.unpack_from("<I", data, 12)[0]
    gltf = json.loads(data[20:20 + json_length])
    gltf["asset"]["extras"] = {
        "title": "White Hilt Rune Circle base",
        "author": "BrudvikWhiteHilt (generated by AssetSource/Tools/make_portal_base.py)",
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
        if line.startswith("PORTALBASE"):
            print(line)
    if result.returncode != 0 or not OUTPUT.exists():
        print(result.stdout[-3000:], result.stderr[-3000:])
        raise SystemExit("Blender failed")
    add_asset_info(OUTPUT)
    print(f"Wrote {OUTPUT}")


if __name__ == "__main__":
    main()
