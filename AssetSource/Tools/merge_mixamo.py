"""Blender script: merges a Mixamo character and its animations, each downloaded as its own FBX, into one glb that
AssetSource/Creatures takes (one armature, one action per animation, named as given).

Mixamo's licence lets the result go into the mod but not the raw downloads into the repo, so keep those elsewhere and
commit only the glb this writes.

Run headless:
  blender -b --factory-startup --python merge_mixamo.py -- <character.fbx> <out.glb> <clip>=<animation.fbx> ...
The character is the FBX downloaded With Skin (T-pose); each animation is downloaded Without Skin, for the same
character, at 30 fps. Its action gets the clip's name, e.g. idle="D:/3dmodels/arissa/Happy Idle.fbx".
"""
import sys

import bpy


def imported(path):
    """Imports an FBX and returns the objects it added."""
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=path, use_anim=True, ignore_leaf_bones=True, automatic_bone_orientation=False)
    return [obj for obj in bpy.data.objects if obj not in before]


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    character, out, clips = argv[0], argv[1], [arg.split("=", 1) for arg in argv[2:]]
    bpy.ops.wm.read_factory_settings(use_empty=True)

    parts = imported(character)
    armature = next(obj for obj in parts if obj.type == "ARMATURE")
    # The T-pose take that comes with the character is not a clip.
    if armature.animation_data and armature.animation_data.action:
        bpy.data.actions.remove(armature.animation_data.action)
    armature.animation_data_create()

    for name, path in clips:
        added = imported(path)
        source = next(obj for obj in added if obj.type == "ARMATURE")
        action = source.animation_data.action
        action.name = name
        action.use_fake_user = True
        # Every clip as its own NLA strip, so the glTF exporter writes each as its own animation.
        track = armature.animation_data.nla_tracks.new()
        track.name = name
        track.strips.new(name, int(action.frame_range[0]), action)
        track.mute = True
        for obj in added:
            bpy.data.objects.remove(obj, do_unlink=True)
        print(f"clip {name}: frames {action.frame_range[0]:.0f}-{action.frame_range[1]:.0f}")

    bpy.ops.object.select_all(action="DESELECT")
    for obj in parts:
        obj.select_set(True)
    bpy.ops.export_scene.gltf(filepath=out, export_format="GLB", use_selection=True, export_animations=True,
                              export_animation_mode="NLA_TRACKS", export_force_sampling=True, export_yup=True)
    print(f"wrote {out}")


main()
