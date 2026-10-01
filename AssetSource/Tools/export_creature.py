"""Blender script: turns a rigged, animated glTF (.glb) into an FBX with its textures, for a Unity animated creature.

Run headless (build_foraging_bundle.ps1 does this for every AssetSource/Creatures/<name>.glb):
  blender -b --factory-startup --python export_creature.py -- <file.glb> <name.creature.json> <out dir> <name>

Writes <out>/<name>.fbx, <out>/<name>_<material>_albedo.png / _normal.png and <out>/<name>.materials.json (material slots
in submesh order, with their textures, colour and emission). The json spec (see AssetSource/Creatures/*.creature.json):
  drop     objects to delete (e.g. parts whose skinning is broken)
  pose     {"take", "frame"}: the pose in which "eyes" positions are given (world space, Blender z up)
  eyes     glowing spheres skinned to one bone: {"bone", "radius", "positions": [[x,y,z], ...], "colour", "emission"}
  clips    [{"name", "take", "loop"}]: actions to keep, renamed; every other action is dropped
  generate [{"name", "base": {"take", "frame"}, "keys": [{"frame", "rot": {bone prefix: [x, y, z] degrees}}]}]: new actions
           for clips the model lacks, keyed at each key frame as the base pose plus local rotations of every bone whose
           name starts with a prefix; list them in clips with take = name to keep them
  texture_size  largest texture side (default 1024)
"""
import json
import math
import os
import re
import sys

import bmesh
import bpy
import mathutils


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    source, spec_path, out, name = argv[0], argv[1], argv[2], argv[3]
    with open(spec_path, encoding="utf-8-sig") as handle:
        spec = json.load(handle)
    os.makedirs(out, exist_ok=True)

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=source)
    drop = set(spec.get("drop", []))
    for obj in list(bpy.data.objects):
        if obj.name in drop or (obj.type == "MESH" and obj.name.startswith("Icosphere") and not obj.data.materials):
            bpy.data.objects.remove(obj)

    armature = next(obj for obj in bpy.data.objects if obj.type == "ARMATURE")
    meshes = [obj for obj in bpy.data.objects if obj.type == "MESH" and any(m.type == "ARMATURE" and m.object == armature for m in obj.modifiers)]
    body = max(meshes, key=lambda obj: len(obj.data.vertices))

    set_pose(armature, spec.get("pose"))
    if "eyes" in spec:
        meshes.append(add_eyes(armature, body, spec["eyes"]))

    join(body, meshes)
    for clip in spec.get("generate", []):
        generate_clip(armature, clip)
    keep_clips(spec.get("clips", []))
    materials = export_materials(body, name, out, spec.get("texture_size", 1024))
    with open(os.path.join(out, f"{name}.materials.json"), "w", encoding="utf-8") as handle:
        json.dump({"materials": materials}, handle, indent=1)

    armature.animation_data.action = None
    bpy.ops.object.select_all(action="DESELECT")
    armature.select_set(True)
    body.select_set(True)
    bpy.context.view_layer.objects.active = armature
    bpy.ops.export_scene.fbx(
        filepath=os.path.join(out, f"{name}.fbx"), use_selection=True, object_types={"ARMATURE", "MESH"},
        apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y", add_leaf_bones=False,
        bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False, bake_anim_force_startend_keying=True,
        bake_anim_simplify_factor=0.0, mesh_smooth_type="FACE", path_mode="STRIP", embed_textures=False)
    print(f"[WhiteHilt] Exported {name}: {len(body.data.vertices)} vertices, {len(materials)} materials, "
          f"{len(bpy.data.actions)} clips {[action.name for action in bpy.data.actions]}")


def set_pose(armature, pose):
    if not pose:
        return
    action = bpy.data.actions[pose["take"]]
    armature.animation_data.action = action
    if hasattr(armature.animation_data, "action_slot") and action.slots:
        armature.animation_data.action_slot = action.slots[0]
    bpy.context.scene.frame_set(int(pose.get("frame", 0)))


def add_eyes(armature, body, eyes):
    """Spheres given in posed world space, moved back into the body's bind space and skinned fully to one bone."""
    bone = armature.data.bones[eyes["bone"]]
    pose_bone = armature.pose.bones[eyes["bone"]]
    deform = pose_bone.matrix @ bone.matrix_local.inverted()
    to_local = body.matrix_world.inverted() @ armature.matrix_world @ deform.inverted() @ armature.matrix_world.inverted()

    mesh = bpy.data.meshes.new("eyes")
    work = bmesh.new()
    for position in eyes["positions"]:
        matrix = mathutils.Matrix.Translation(position) @ mathutils.Matrix.Scale(eyes["radius"], 4)
        bmesh.ops.create_uvsphere(work, u_segments=16, v_segments=10, radius=1.0, matrix=matrix)
    for vertex in work.verts:
        vertex.co = to_local @ vertex.co
    work.to_mesh(mesh)
    work.free()

    material = bpy.data.materials.new("Eye")
    material.diffuse_color = (*eyes.get("colour", [1, 0.3, 0.05]), 1.0)
    material["whitehilt_emission"] = eyes.get("emission", 3.0)
    mesh.materials.append(material)

    obj = bpy.data.objects.new("eyes", mesh)
    bpy.context.scene.collection.objects.link(obj)
    obj.parent = body.parent
    obj.matrix_world = body.matrix_world
    group = obj.vertex_groups.new(name=eyes["bone"])
    group.add(list(range(len(mesh.vertices))), 1.0, "REPLACE")
    modifier = obj.modifiers.new("Armature", "ARMATURE")
    modifier.object = armature
    return obj


def join(body, meshes):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in meshes:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = body
    if len(meshes) > 1:
        bpy.ops.object.join()
    bpy.ops.object.material_slot_remove_unused()


def generate_clip(armature, clip):
    """Keys a new action: the base pose with extra local rotations per key frame, smoothed by Blender's curves."""
    base_action = bpy.data.actions[clip["base"]["take"]]
    set_pose(armature, {"take": base_action.name, "frame": clip["base"].get("frame", 0)})
    bones = armature.pose.bones
    base = {bone.name: (bone.location.copy(), bone.rotation_quaternion.copy(), bone.scale.copy()) for bone in bones}

    action = bpy.data.actions.new(clip["name"])
    armature.animation_data.action = action
    for key in clip["keys"]:
        offsets = {}
        for prefix, degrees in key.get("rot", {}).items():
            matched = [name for name in base if name.startswith(prefix)]
            if not matched:
                raise SystemExit(f"{clip['name']}: no bone starts with '{prefix}'")
            for name in matched:
                offsets[name] = mathutils.Euler([math.radians(angle) for angle in degrees]).to_quaternion()
        for bone in bones:
            location, rotation, scale = base[bone.name]
            bone.rotation_mode = "QUATERNION"
            bone.location = location
            bone.rotation_quaternion = rotation @ offsets[bone.name] if bone.name in offsets else rotation
            bone.scale = scale
            for path in ("location", "rotation_quaternion", "scale"):
                bone.keyframe_insert(path, frame=key["frame"])
    action.use_fake_user = True
    armature.animation_data.action = None


def keep_clips(clips):
    wanted = {clip["take"]: clip["name"] for clip in clips}
    for action in list(bpy.data.actions):
        if action.name in wanted:
            action.name = wanted[action.name]
            action.use_fake_user = True
        else:
            bpy.data.actions.remove(action)
    missing = [take for take in wanted if take not in [a.name for a in bpy.data.actions] and wanted[take] not in [a.name for a in bpy.data.actions]]
    if missing:
        raise SystemExit(f"Takes not found: {missing}")


def export_materials(body, name, out, size):
    materials = []
    for slot in body.material_slots:
        material = slot.material
        key = re.sub(r"[^a-z0-9]+", "", material.name.lower()) or f"m{len(materials)}"
        entry = {"name": key, "albedo": None, "normal": None, "colour": [1, 1, 1, 1], "emission": None}
        bsdf = None
        if material.node_tree:
            bsdf = next((node for node in material.node_tree.nodes if node.type == "BSDF_PRINCIPLED"), None)
        if bsdf is not None:
            entry["colour"] = [round(c, 4) for c in bsdf.inputs["Base Color"].default_value]
            image = linked_image(bsdf.inputs["Base Color"])
            if image is not None:
                entry["albedo"] = save_image(image, f"{name}_{key}_albedo", out, size)
                entry["colour"] = [1, 1, 1, 1]
            normal_input = bsdf.inputs.get("Normal")
            if normal_input is not None and normal_input.is_linked:
                normal_node = normal_input.links[0].from_node
                image = linked_image(normal_node.inputs.get("Color")) if normal_node.type == "NORMAL_MAP" else None
                if image is not None:
                    entry["normal"] = save_image(image, f"{name}_{key}_normal", out, size)
        else:
            entry["colour"] = [round(c, 4) for c in material.diffuse_color]
        if "whitehilt_emission" in material:
            entry["colour"] = [round(c, 4) for c in material.diffuse_color]
            entry["emission"] = [round(c * material["whitehilt_emission"], 4) for c in material.diffuse_color[:3]]
        materials.append(entry)
    return materials


def linked_image(socket):
    """The image feeding a socket directly or through a few colour nodes."""
    if socket is None or not socket.is_linked:
        return None
    node = socket.links[0].from_node
    for _ in range(4):
        if node.type == "TEX_IMAGE":
            return node.image
        inputs = [s for s in node.inputs if s.is_linked]
        if not inputs:
            return None
        node = inputs[0].links[0].from_node
    return None


def save_image(image, file_name, out, size):
    copy = image.copy()
    width, height = copy.size
    if max(width, height) > size:
        scale = size / max(width, height)
        copy.scale(max(1, math.floor(width * scale)), max(1, math.floor(height * scale)))
    # Images from a .glb are packed; a fresh image with the same pixels saves straight to a file, without colour management.
    width, height = copy.size
    target = bpy.data.images.new(file_name, width, height, alpha=True)
    target.pixels.foreach_set(tuple(copy.pixels))
    path = os.path.join(out, file_name + ".png")
    target.filepath_raw = path
    target.file_format = "PNG"
    target.save()
    return file_name


main()
