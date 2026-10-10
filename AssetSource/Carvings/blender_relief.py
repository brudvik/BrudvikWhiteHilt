"""Builds a relief panel from a height map and writes it as a .glb with its wood texture. Run in Blender:

blender -b --factory-startup -P blender_relief.py -- <height.png> <out.glb> <width_m> <depth_m> <tris> <albedo.png>

The panel stands in x (width) and up (height, from the height map's aspect), its back flat and the carving standing
out towards the game's +z by up to depth. Blender is z-up and glTF y-up, so it is built here standing in Blender's
x-z plane with the carving towards -y, which the exporter turns into +z. A grid as fine as the map is displaced, then decimated to about tris triangles,
so flat ground costs little and the strands keep their shape. The texture is mapped once over the face.
"""

import sys

import bpy
import bmesh

args = sys.argv[sys.argv.index("--") + 1:]
height_path, out_path = args[0], args[1]
width, depth, tris = float(args[2]), float(args[3]), int(args[4])
albedo_path = args[5]

bpy.ops.wm.read_factory_settings(use_empty=True)
image = bpy.data.images.load(height_path)
height_m = width * image.size[1] / image.size[0]
# A grid of cells about cell metres across; the map is scaled to it first, so every vertex is the mean of the pixels
# round it rather than one pixel picked out.
cell = float(args[6]) if len(args) > 6 else 0.006
cols = max(2, round(width / cell))
rows = max(2, round(height_m / cell))
image.scale(cols + 1, rows + 1)
px_w, px_h = image.size
pixels = list(image.pixels)
mesh = bpy.data.meshes.new("relief")
bm = bmesh.new()
uv_layer = bm.loops.layers.uv.new()
verts = []
for j in range(rows + 1):
    row = []
    for i in range(cols + 1):
        u, v = i / cols, j / rows
        sx, sy = i, j
        h = pixels[(sy * px_w + sx) * 4]
        row.append(bm.verts.new(((u - 0.5) * width, -h * depth, (1 - v) * height_m)))
    verts.append(row)
for j in range(rows):
    for i in range(cols):
        face = bm.faces.new((verts[j][i], verts[j + 1][i], verts[j + 1][i + 1], verts[j][i + 1]))
        for loop, (du, dv) in zip(face.loops, ((0, 0), (0, 1), (1, 1), (1, 0))):
            loop[uv_layer].uv = ((i + du) / cols, 1 - (j + dv) / rows)
# A back and sides, so the panel is solid from every side.
bm.to_mesh(mesh)
bm.free()
obj = bpy.data.objects.new("relief", mesh)
bpy.context.collection.objects.link(obj)
bpy.context.view_layer.objects.active = obj
obj.select_set(True)
solid = obj.modifiers.new("solid", "SOLIDIFY")
solid.thickness = 0.04
solid.offset = -1.0
bpy.ops.object.modifier_apply(modifier="solid")
faces = len(obj.data.polygons) * 2
if faces > tris:
    # Flat ground first, as few large faces, then the rest down to the budget.
    planar = obj.modifiers.new("planar", "DECIMATE")
    planar.decimate_type = "DISSOLVE"
    planar.angle_limit = 0.02
    bpy.ops.object.modifier_apply(modifier="planar")
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.quads_convert_to_tris()
    bpy.ops.object.mode_set(mode="OBJECT")
    faces = len(obj.data.polygons)
    if faces > tris:
        dec = obj.modifiers.new("decimate", "DECIMATE")
        dec.ratio = tris / faces
        bpy.ops.object.modifier_apply(modifier="decimate")
bpy.ops.object.shade_smooth()

material = bpy.data.materials.new("relief")
material.use_nodes = True
texture = material.node_tree.nodes.new("ShaderNodeTexImage")
texture.image = bpy.data.images.load(albedo_path)
bsdf = material.node_tree.nodes["Principled BSDF"]
material.node_tree.links.new(texture.outputs["Color"], bsdf.inputs["Base Color"])
obj.data.materials.append(material)

bpy.ops.export_scene.gltf(filepath=out_path, export_format="GLB", use_selection=True)
print(f"{out_path}: {len(obj.data.polygons)} faces, {width:.2f} x {height_m:.2f} m")
