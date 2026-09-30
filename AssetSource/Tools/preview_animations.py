"""Renders every animation of a rigged glb or fbx with Blender, as a contact sheet: one row per action, four moments,
each from the front (-y in Blender) and the side (+x). Also prints the posed bounds of each mesh part at the first
frame, which shows parts whose skinning is broken (they end up hundreds of metres away; drop them in the
creature.json). Use it on a download before writing its AssetSource/Creatures/<name>.creature.json; after the bundle
is built, AssetSource/Preview/render_creatures.ps1 shows the Unity result.

Usage:
  python preview_animations.py <file.glb|fbx> [--drop Object_15,Object_19] [--focus x,y,z --size 6] [--out DIR]
Writes <out>/<file>_animations.png (default %TEMP%\\wh_models). --focus/--size frame a close-up (Blender world
space, z up) instead of the whole model, e.g. to place eyes. Needs Blender (found under Program Files, or --blender).
"""
import argparse
import os
import pathlib
import re
import subprocess
import tempfile

from PIL import Image, ImageDraw

MOMENTS = 4
TILE = 240

BLENDER_SCRIPT = r'''
import bpy, sys, os, mathutils
argv = sys.argv[sys.argv.index("--") + 1:]
source, out, drop, focus, size = argv[0], argv[1], [d for d in argv[2].split(",") if d], argv[3], float(argv[4])
bpy.ops.wm.read_factory_settings(use_empty=True)
if source.lower().endswith(".fbx"):
    bpy.ops.import_scene.fbx(filepath=source)
else:
    bpy.ops.import_scene.gltf(filepath=source)
for obj in list(bpy.data.objects):
    if obj.name in drop or (obj.type == "MESH" and obj.name.startswith("Icosphere") and not obj.data.materials):
        bpy.data.objects.remove(obj)
scene = bpy.context.scene
scene.render.engine = "BLENDER_WORKBENCH"
scene.display.shading.color_type = "MATERIAL" if source.lower().endswith(".fbx") else "TEXTURE"
scene.display.shading.light = "STUDIO"
scene.render.resolution_x = scene.render.resolution_y = %TILE%
armature = next(o for o in scene.objects if o.type == "ARMATURE")
meshes = [o for o in scene.objects if o.type == "MESH"]
data = bpy.data.cameras.new("camera"); data.type = "ORTHO"
camera = bpy.data.objects.new("camera", data); scene.collection.objects.link(camera); scene.camera = camera

def bounds(objects):
    graph = bpy.context.evaluated_depsgraph_get()
    low = mathutils.Vector((1e18,) * 3); high = -low
    for obj in objects:
        evaluated = obj.evaluated_get(graph); mesh = evaluated.to_mesh()
        for vertex in mesh.vertices:
            point = evaluated.matrix_world @ vertex.co
            low = mathutils.Vector(map(min, low, point)); high = mathutils.Vector(map(max, high, point))
        evaluated.to_mesh_clear()
    return low, high

if armature.animation_data is None:
    armature.animation_data_create()
for index, action in enumerate(bpy.data.actions):
    armature.animation_data.action = action
    if hasattr(armature.animation_data, "action_slot") and action.slots:
        armature.animation_data.action_slot = action.slots[0]
    start, end = action.frame_range
    for moment in range(%MOMENTS%):
        scene.frame_set(int(start + (end - start) * moment / (%MOMENTS% - 1)))
        if index == 0 and moment == 0:
            for obj in meshes:
                low, high = bounds([obj])
                print(f"PART {obj.name} min {tuple(round(v, 2) for v in low)} max {tuple(round(v, 2) for v in high)}")
        if focus:
            centre, extent = mathutils.Vector([float(v) for v in focus.split(",")]), size
        else:
            low, high = bounds(meshes)
            centre, extent = (low + high) / 2, max(high - low) * 1.1
        data.ortho_scale = extent
        data.clip_end = extent * 20
        for view, direction in (("front", mathutils.Vector((0, -1, 0))), ("side", mathutils.Vector((1, 0, 0)))):
            camera.location = centre + direction * extent * 5
            camera.rotation_euler = (-direction).to_track_quat("-Z", "Z").to_euler()
            scene.render.filepath = os.path.join(out, f"{index:02}_{moment}_{view}.png")
            bpy.ops.render.render(write_still=True)
    print(f"ACTION {index:02} {action.name} frames {start:.0f}-{end:.0f}")
'''


def find_blender(given):
    if given:
        return given
    found = sorted(pathlib.Path(r"C:\Program Files\Blender Foundation").glob("*/blender.exe"))
    if not found:
        raise SystemExit("Blender not found; pass --blender")
    return str(found[-1])


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("file", type=pathlib.Path)
    parser.add_argument("--drop", default="")
    parser.add_argument("--focus", default="")
    parser.add_argument("--size", type=float, default=6.0)
    parser.add_argument("--out", default=os.path.join(os.environ.get("TEMP", "."), "wh_models"))
    parser.add_argument("--blender")
    args = parser.parse_args()

    out = pathlib.Path(args.out)
    out.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory() as work:
        script = pathlib.Path(work) / "render.py"
        script.write_text(BLENDER_SCRIPT.replace("%TILE%", str(TILE)).replace("%MOMENTS%", str(MOMENTS)), encoding="utf-8")
        result = subprocess.run([find_blender(args.blender), "-b", "--factory-startup", "--python-exit-code", "1", "--python", str(script), "--",
                                 str(args.file.resolve()), work, args.drop, args.focus, str(args.size)],
                                capture_output=True, text=True, encoding="utf-8", errors="replace")
        actions = []
        for line in result.stdout.splitlines():
            if line.startswith("PART"):
                print(line)
            elif line.startswith("ACTION"):
                actions.append(line.split(" ", 2)[2])
                print(line)
        if result.returncode != 0:
            print(result.stdout[-3000:], result.stderr[-3000:])
            raise SystemExit("Blender failed")

        label_height = 16
        sheet = Image.new("RGB", (TILE * MOMENTS * 2, (TILE + label_height) * max(1, len(actions))), "white")
        draw = ImageDraw.Draw(sheet)
        for row, name in enumerate(actions):
            y = row * (TILE + label_height)
            draw.text((4, y + 2), name.encode("ascii", "replace").decode(), fill="black")
            for moment in range(MOMENTS):
                for column, view in enumerate(("front", "side")):
                    tile = pathlib.Path(work) / f"{row:02}_{moment}_{view}.png"
                    if tile.exists():
                        sheet.paste(Image.open(tile).convert("RGB"), ((moment * 2 + column) * TILE, y + label_height))
    target = out / f"{re.sub(r'[^A-Za-z0-9_-]+', '_', args.file.stem)}_animations.png"
    sheet.save(target)
    print(target)


if __name__ == "__main__":
    main()
