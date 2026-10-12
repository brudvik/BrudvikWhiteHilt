"""Blender script: turns a rigged, animated glTF (.glb) into an FBX with its textures, for a Unity animated creature.

Run headless (build_foraging_bundle.ps1 does this for every AssetSource/Creatures/<name>.glb):
  blender -b --factory-startup --python export_creature.py -- <file.glb> <name.creature.json> <out dir> <name>

Writes <out>/<name>.fbx, <out>/<name>_<material>_albedo.png / _normal.png / _emission.png and <out>/<name>.materials.json
(material slots in submesh order, with their textures, colour, emission and whether the albedo's alpha cuts out). The json
spec (see AssetSource/Creatures/*.creature.json):
  drop     objects to delete (e.g. parts whose skinning is broken)
  pose     {"take", "frame"}: the pose in which "eyes" positions are given (world space, Blender z up)
  eyes     glowing spheres skinned to one bone: {"bone", "radius", "positions": [[x,y,z], ...], "colour", "emission"}
  clips    [{"name", "take", "loop"}]: actions to keep, renamed; every other action is dropped
  generate [{"name", "base": {"take", "frame", "follow"}, "keys": [{"frame", "rot": {bone prefix: [x, y, z] degrees},
           "wrot": {bone prefix: [x, y, z] degrees}, "loc": {bone prefix: [x, y, z] metres}}]}]: new actions for clips
           the model lacks, keyed at each key frame as the base pose plus local rotations of every bone whose name
           starts with a prefix ("wrot": the same about world axes through the bone's head, easier on a rig whose bone
           axes are tilted), and optionally moved by a world-space offset (Blender axes, z up; "wrot" and "loc" not with
           "follow"); with "follow": true the base take keeps playing
           under the rotations (wings keep flapping), and a following clip also trims a long take: it lasts until its
           last key frame and starts at base.frame; list them in clips with take = name to keep them
  decimate     ratio (0..1): collapse-decimates every skinned mesh of 300 or more faces before the eyes are added
  scale_translations  factor: multiplies every animation translation in the glb before import; for a glb whose mesh and rest
               pose come into Blender 100 times larger than its animation translations (the mesh stretches when a bone moves)
  prune_bones  true: deletes every bone that has no weights and no weighted descendant (Rigify control bones), which
               makes the FBX and its clips a fraction of the size; "generate" prefixes then need kept bones
  texture_size  largest texture side (default 1024)
Shape keys are baked at their current values first (morph targets cannot be decimated and Unity ignores them).
"""
import json
import math
import os
import re
import struct
import sys
import tempfile

import bmesh
import bpy
import mathutils

# How the glb leaves every bone: an unkeyed bone keeps this pose in every clip (a root bone often holds the model's
# upright rotation).
IMPORTED_POSE = {}


def restore_imported_pose(armature):
    """Puts every bone back as the glb left it. A take only moves the bones it keys, so without this a generated clip
    would inherit the last pose of the one before it, and the FBX would store a toppled pose as the default one (Unity
    plays that for every bone a clip does not key)."""
    for bone in armature.pose.bones:
        location, mode, quaternion, euler, scale = IMPORTED_POSE[bone.name]
        bone.location = location
        bone.rotation_mode = mode
        bone.rotation_quaternion = quaternion
        bone.rotation_euler = euler
        bone.scale = scale
    bpy.context.view_layer.update()


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    source, spec_path, out, name = argv[0], argv[1], argv[2], argv[3]
    with open(spec_path, encoding="utf-8-sig") as handle:
        spec = json.load(handle)
    os.makedirs(out, exist_ok=True)

    bpy.ops.wm.read_factory_settings(use_empty=True)
    if "scale_translations" in spec:
        source = scaled_translations(source, spec["scale_translations"])
    bpy.ops.import_scene.gltf(filepath=source)
    drop = set(spec.get("drop", []))
    for obj in list(bpy.data.objects):
        if obj.name in drop or (obj.type == "MESH" and obj.name.startswith("Icosphere") and not obj.data.materials):
            bpy.data.objects.remove(obj)

    armature = next(obj for obj in bpy.data.objects if obj.type == "ARMATURE")
    meshes = [obj for obj in bpy.data.objects if obj.type == "MESH" and any(m.type == "ARMATURE" and m.object == armature for m in obj.modifiers)]
    body = max(meshes, key=lambda obj: len(obj.data.vertices))
    IMPORTED_POSE.update({bone.name: (bone.location.copy(), bone.rotation_mode, bone.rotation_quaternion.copy(),
                                      bone.rotation_euler.copy(), bone.scale.copy()) for bone in armature.pose.bones})

    if spec.get("prune_bones"):
        prune_bones(armature, meshes, [spec["eyes"]["bone"]] if "eyes" in spec else [])
    for obj in meshes:
        bake_shape_keys(obj)
    if "decimate" in spec:
        decimate(meshes, spec["decimate"])

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
    restore_imported_pose(armature)
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


def scaled_translations(source, factor):
    """A copy of the glb whose animation translations are multiplied by a factor, except the ones that never leave the
    node's own translation, which are dropped (Blender would offset those bones from their rest pose); returns its path."""
    with open(source, "rb") as handle:
        data = handle.read()
    offset, gltf, binary = 12, None, bytearray()
    while offset < len(data):
        length, kind = struct.unpack_from("<II", data, offset)
        chunk = data[offset + 8:offset + 8 + length]
        if kind == 0x4E4F534A:
            gltf = json.loads(chunk)
        elif kind == 0x004E4942:
            binary = bytearray(chunk)
        offset += 8 + length

    done = set()
    for animation in gltf.get("animations", []):
        kept = []
        for channel in animation["channels"]:
            if channel["target"]["path"] != "translation":
                kept.append(channel)
                continue
            index = animation["samplers"][channel["sampler"]]["output"]
            accessor = gltf["accessors"][index]
            start = gltf["bufferViews"][accessor["bufferView"]].get("byteOffset", 0) + accessor.get("byteOffset", 0)
            values = struct.unpack_from(f"<{accessor['count'] * 3}f", binary, start)
            rest = gltf["nodes"][channel["target"]["node"]].get("translation", [0, 0, 0])
            if all(abs(values[i] - rest[i % 3]) < 1e-3 for i in range(len(values))):
                continue
            kept.append(channel)
            if index in done:
                continue
            done.add(index)
            struct.pack_into(f"<{accessor['count'] * 3}f", binary, start, *(value * factor for value in values))
            for bound in ("min", "max"):
                if bound in accessor:
                    accessor[bound] = [value * factor for value in accessor[bound]]
        animation["channels"] = kept

    text = json.dumps(gltf, separators=(",", ":")).encode("utf-8")
    text += b" " * (-len(text) % 4)
    binary += b"\0" * (-len(binary) % 4)
    path = os.path.join(tempfile.gettempdir(), "whitehilt_scaled_translations.glb")
    with open(path, "wb") as handle:
        handle.write(struct.pack("<4sII", b"glTF", 2, 12 + 8 + len(text) + 8 + len(binary)))
        handle.write(struct.pack("<II", len(text), 0x4E4F534A) + text)
        handle.write(struct.pack("<II", len(binary), 0x004E4942) + bytes(binary))
    return path


def bake_shape_keys(obj):
    keys = obj.data.shape_keys
    if not keys:
        return
    basis = keys.reference_key
    mixed = [point.co.copy() for point in basis.data]
    for block in keys.key_blocks:
        if block != basis and not block.mute and block.value:
            for index, point in enumerate(block.data):
                mixed[index] += block.value * (point.co - block.relative_key.data[index].co)
    for block in reversed(list(keys.key_blocks)):
        obj.shape_key_remove(block)
    for vertex, position in zip(obj.data.vertices, mixed):
        vertex.co = position


def decimate(meshes, ratio):
    for obj in meshes:
        if len(obj.data.polygons) < 300:
            continue
        before = len(obj.data.polygons)
        modifier = obj.modifiers.new("Decimate", "DECIMATE")
        modifier.decimate_type = "COLLAPSE"
        modifier.ratio = ratio
        modifier.use_collapse_triangulate = True
        # Applied to the unposed mesh, so it has to come before the Armature modifier.
        obj.modifiers.move(len(obj.modifiers) - 1, 0)
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=modifier.name)
        print(f"[WhiteHilt] Decimated {obj.name}: {before} -> {len(obj.data.polygons)} faces")


def prune_bones(armature, meshes, wanted):
    used = set(wanted)
    for mesh in meshes:
        names = {group.index: group.name for group in mesh.vertex_groups}
        used |= {names[entry.group] for vertex in mesh.data.vertices for entry in vertex.groups if entry.weight > 0.001}
    keep = set(used)
    for name in used:
        bone = armature.data.bones[name]
        while bone.parent:
            bone = bone.parent
            keep.add(bone.name)

    total = len(armature.data.bones)
    bpy.context.view_layer.objects.active = armature
    bpy.ops.object.mode_set(mode="EDIT")
    for edit_bone in [each for each in armature.data.edit_bones if each.name not in keep]:
        armature.data.edit_bones.remove(edit_bone)
    bpy.ops.object.mode_set(mode="OBJECT")
    for mesh in meshes:
        for group in [each for each in mesh.vertex_groups if each.name not in keep]:
            mesh.vertex_groups.remove(group)
    # The FBX exporter skips an action unless every one of its curves still resolves.
    for action in bpy.data.actions:
        for layer in action.layers:
            for strip in layer.strips:
                for channelbag in strip.channelbags:
                    for curve in list(channelbag.fcurves):
                        found = re.match(r'pose\.bones\["(.*)"\]\.', curve.data_path)
                        if found and found.group(1) not in keep:
                            channelbag.fcurves.remove(curve)
    print(f"[WhiteHilt] Pruned bones: {total} -> {len(armature.data.bones)}")


def join(body, meshes):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in meshes:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = body
    if len(meshes) > 1:
        bpy.ops.object.join()
    bpy.ops.object.material_slot_remove_unused()


def generate_clip(armature, clip):
    """Keys a new action: the base pose with extra local rotations per key frame, smoothed by Blender's curves. With
    "follow": true in the base, the base take keeps playing (looping) from its frame, so e.g. the wings keep flapping,
    and the rotations are blended in between the keys and keyed on every frame."""
    base_action = bpy.data.actions[clip["base"]["take"]]
    armature.animation_data.action = None
    restore_imported_pose(armature)
    if clip["base"].get("follow"):
        if any("loc" in key or "wrot" in key for key in clip["keys"]):
            raise SystemExit(f"{clip['name']}: \"loc\" and \"wrot\" are not supported with \"follow\"")
        generate_following_clip(armature, clip, base_action)
        return
    set_pose(armature, {"take": base_action.name, "frame": clip["base"].get("frame", 0)})
    bones = armature.pose.bones
    base = {bone.name: (bone.location.copy(), bone.rotation_quaternion.copy(), bone.scale.copy()) for bone in bones}

    matrices = {bone.name: bone.matrix.copy() for bone in bones}
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
        shifts = {}
        for prefix, metres in key.get("loc", {}).items():
            matched = [name for name in base if name.startswith(prefix)]
            if not matched:
                raise SystemExit(f"{clip['name']}: no bone starts with '{prefix}'")
            for name in matched:
                shifts[name] = local_shift(armature, bones[name], matrices, metres)
        turns = {}
        for prefix, degrees in key.get("wrot", {}).items():
            matched = [name for name in base if name.startswith(prefix)]
            if not matched:
                raise SystemExit(f"{clip['name']}: no bone starts with '{prefix}'")
            for name in matched:
                turns[name] = world_rotation(armature, bones[name], matrices, degrees)
        for bone in bones:
            location, rotation, scale = base[bone.name]
            bone.rotation_mode = "QUATERNION"
            bone.location = location + shifts[bone.name] if bone.name in shifts else location
            rotated = rotation @ offsets[bone.name] if bone.name in offsets else rotation
            bone.rotation_quaternion = turns[bone.name] @ rotated if bone.name in turns else rotated
            bone.scale = scale
            for path in ("location", "rotation_quaternion", "scale"):
                bone.keyframe_insert(path, frame=key["frame"])
    action.use_fake_user = True
    armature.animation_data.action = None


def local_shift(armature, bone, matrices, metres):
    """A world-space offset as the pose-bone location change that moves the bone (and its children) by it."""
    parent = bone.parent
    parent_pose = matrices[parent.name].to_3x3() if parent else mathutils.Matrix.Identity(3)
    parent_rest = parent.bone.matrix_local if parent else mathutils.Matrix.Identity(4)
    rest = (parent_rest.inverted() @ bone.bone.matrix_local).to_3x3()
    shift = armature.matrix_world.to_3x3().inverted() @ mathutils.Vector(metres)
    return rest.inverted() @ parent_pose.inverted() @ shift


def world_rotation(armature, bone, matrices, degrees):
    """A rotation about world axes through the bone's head, as the local rotation applied before the bone's own."""
    parent = bone.parent
    parent_pose = matrices[parent.name].to_3x3() if parent else mathutils.Matrix.Identity(3)
    parent_rest = parent.bone.matrix_local if parent else mathutils.Matrix.Identity(4)
    chain = (parent_pose @ (parent_rest.inverted() @ bone.bone.matrix_local).to_3x3()).normalized()
    world = armature.matrix_world.to_3x3().normalized()
    turn = mathutils.Euler([math.radians(angle) for angle in degrees]).to_matrix()
    return (chain.inverted() @ world.inverted() @ turn @ world @ chain).to_quaternion()


def key_offsets(clip, frame):
    """The extra rotations at a frame: each bone prefix's degrees eased between the keys around it (0 where unkeyed)."""
    keys = sorted(clip["keys"], key=lambda key: key["frame"])
    prefixes = {prefix for key in keys for prefix in key.get("rot", {})}
    offsets = {}
    for prefix in prefixes:
        before = max((key for key in keys if key["frame"] <= frame), key=lambda key: key["frame"], default=keys[0])
        after = min((key for key in keys if key["frame"] >= frame), key=lambda key: key["frame"], default=keys[-1])
        a = before.get("rot", {}).get(prefix, [0, 0, 0])
        b = after.get("rot", {}).get(prefix, [0, 0, 0])
        span = after["frame"] - before["frame"]
        t = 0.0 if span == 0 else (frame - before["frame"]) / span
        t = t * t * (3 - 2 * t)
        offsets[prefix] = [x + (y - x) * t for x, y in zip(a, b)]
    return offsets


def generate_following_clip(armature, clip, base_action):
    start, end = (int(round(value)) for value in base_action.frame_range)
    length = max(1, end - start)
    first = int(clip["base"].get("frame", start))
    last_key = max(key["frame"] for key in clip["keys"])
    bones = armature.pose.bones

    # The base take's pose at every frame, read before the new action replaces it on the armature.
    poses = []
    for frame in range(last_key + 1):
        set_pose(armature, {"take": base_action.name, "frame": start + (first - start + frame) % length})
        poses.append({bone.name: (bone.location.copy(), bone.rotation_quaternion.copy(), bone.scale.copy()) for bone in bones})

    action = bpy.data.actions.new(clip["name"])
    armature.animation_data.action = action
    for frame in range(last_key + 1):
        offsets = {}
        for prefix, degrees in key_offsets(clip, frame).items():
            matched = [name for name in poses[frame] if name.startswith(prefix)]
            if not matched:
                raise SystemExit(f"{clip['name']}: no bone starts with '{prefix}'")
            for name in matched:
                offsets[name] = mathutils.Euler([math.radians(angle) for angle in degrees]).to_quaternion()
        for bone in bones:
            location, rotation, scale = poses[frame][bone.name]
            bone.rotation_mode = "QUATERNION"
            bone.location = location
            bone.rotation_quaternion = rotation @ offsets[bone.name] if bone.name in offsets else rotation
            bone.scale = scale
            for path in ("location", "rotation_quaternion", "scale"):
                bone.keyframe_insert(path, frame=frame)
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
        entry = {"name": key, "albedo": None, "normal": None, "colour": [1, 1, 1, 1], "emission": None,
                 "emission_map": None, "cutout": False}
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
            # A texture-driven alpha is a cut-out (leaf cards); an emission texture lights only its bright parts.
            alpha_input = bsdf.inputs.get("Alpha")
            entry["cutout"] = bool(alpha_input is not None and alpha_input.is_linked)
            strength = bsdf.inputs["Emission Strength"].default_value if "Emission Strength" in bsdf.inputs else 1.0
            image = linked_image(bsdf.inputs.get("Emission Color"))
            if image is not None and strength > 0:
                entry["emission_map"] = save_image(image, f"{name}_{key}_emission", out, size)
                entry["emission"] = [round(strength, 4)] * 3
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
