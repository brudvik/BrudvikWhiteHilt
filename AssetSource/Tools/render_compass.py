"""Blender script: renders parts of the Seadogs Compass straight from above on a transparent background.

Run by AssetSource/make_compass.py:
  blender -b -P render_compass.py -- <gltf> <out.png> <dial|needle> <size px> <half width m>
The camera looks down on the compass centre (the needle's pivot) and covers half width metres to each side, so the
dial and the needle come out in the same frame. The needle's turn is reset so it points north (up in the picture).
"""
import math
import sys

import bpy

argv = sys.argv[sys.argv.index("--") + 1:]
gltf, out, part = argv[0], argv[1], argv[2]
size = int(argv[3])
half = float(argv[4])

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=gltf)

keep = {"dial": "seadogs_compass", "needle": "seadogs_compass_needle"}[part]
for obj in list(bpy.data.objects):
    if obj.type == "MESH" and obj.name != keep:
        bpy.data.objects.remove(obj, do_unlink=True)

for obj in bpy.data.objects:
    if obj.type == "MESH" and part == "needle":
        obj.rotation_mode = "QUATERNION"
        obj.rotation_quaternion = (1, 0, 0, 0)
bpy.context.view_layer.update()

scene = bpy.context.scene
camera_data = bpy.data.cameras.new("top")
camera_data.type = "ORTHO"
camera_data.ortho_scale = half * 2
camera = bpy.data.objects.new("top", camera_data)
camera.location = (0, 0, 1)
scene.collection.objects.link(camera)
scene.camera = camera

sun = bpy.data.lights.new("sun", "SUN")
sun.energy = 3.0
sun_object = bpy.data.objects.new("sun", sun)
sun_object.rotation_euler = (math.radians(25), math.radians(-15), 0)
scene.collection.objects.link(sun_object)

world = bpy.data.worlds.new("sky")
world.use_nodes = True
world.node_tree.nodes["Background"].inputs[0].default_value = (1, 1, 1, 1)
world.node_tree.nodes["Background"].inputs[1].default_value = 0.6
scene.world = world

scene.render.engine = "CYCLES"
scene.cycles.samples = 64
scene.cycles.device = "CPU"
scene.render.film_transparent = True
scene.render.resolution_x = size
scene.render.resolution_y = size
scene.render.image_settings.file_format = "PNG"
scene.render.image_settings.color_mode = "RGBA"
scene.view_settings.view_transform = "Standard"
scene.render.filepath = out
bpy.ops.render.render(write_still=True)
print(f"[compass] wrote {out}")
