"""Blender part of prepare_decor.py: one model in, one small glb out.

Keeps the mesh objects named in "objects" (or under a node of that name; every mesh when the list is empty), joins
them with their transforms applied, decimates them to about "tris" triangles, shrinks every texture to at most
"texture" pixels and exports a glb. Prints a "[decor] {...}" line with the result's size in metres.

Usage: blender -b --factory-startup --python blender_prepare.py -- <input .gltf/.glb> <output .glb> <json spec>
"""
import json
import re
import sys

import bpy


def base_name(name):
    # Blender adds .001, .002 ... when names clash.
    return re.sub(r"\.\d{3}$", "", name)


def wanted(obj, names, exact):
    """Whether the object or a node above it is asked for. A name that exists as such (\"Wooden Crate\") matches only
    itself, not its numbered copies (\"Wooden Crate.001\"); other names match with Blender's .001 suffixes left off."""
    if not names:
        return True
    while obj is not None:
        if obj.name in names or (base_name(obj.name) in names and base_name(obj.name) not in exact):
            return True
        obj = obj.parent
    return False


def colour_image(tree):
    """The image feeding the Principled BSDF's base colour, if any, also through mix or colour nodes on the way."""
    for node in tree.nodes:
        if node.type != "BSDF_PRINCIPLED":
            continue
        stack = [link.from_node for link in node.inputs["Base Color"].links]
        seen = set()
        while stack:
            upstream = stack.pop(0)
            if upstream.name in seen:
                continue
            seen.add(upstream.name)
            if upstream.type == "TEX_IMAGE" and upstream.image is not None:
                return upstream.image
            stack += [link.from_node for socket in upstream.inputs for link in socket.links]
    return None


def uses_alpha(tree):
    for node in tree.nodes:
        if node.type == "BSDF_PRINCIPLED" and node.inputs["Alpha"].links:
            return True
    return False


def main():
    args = sys.argv[sys.argv.index("--") + 1:]
    source, output, spec = args[0], args[1], json.loads(args[2])
    names = set(spec.get("objects", []))

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=source)

    exact = {obj.name for obj in bpy.context.scene.objects if obj.name in names}
    keep = [obj for obj in bpy.context.scene.objects if obj.type == "MESH" and wanted(obj, names, exact)]
    if not keep:
        raise RuntimeError(f"no meshes match {sorted(names)}")

    # Bake each kept mesh's world transform into it, then drop everything else.
    for obj in keep:
        matrix = obj.matrix_world.copy()
        obj.parent = None
        obj.matrix_world = matrix
    for obj in list(bpy.context.scene.objects):
        if obj not in keep:
            bpy.data.objects.remove(obj, do_unlink=True)

    bpy.ops.object.select_all(action="DESELECT")
    for obj in keep:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = keep[0]
    if len(keep) > 1:
        bpy.ops.object.join()
    model = bpy.context.view_layer.objects.active
    # A decoration stands still, and Blender applies no modifier to a mesh with shape keys (Poly Haven's horse statue).
    if model.data.shape_keys is not None:
        model.shape_key_clear()
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

    triangles = sum(len(polygon.vertices) - 2 for polygon in model.data.polygons)
    target = spec.get("tris", 3000)
    if triangles > target:
        decimate = model.modifiers.new("decimate", "DECIMATE")
        decimate.ratio = max(0.01, target / triangles)
        decimate.use_collapse_triangulate = True
        bpy.ops.object.modifier_apply(modifier=decimate.name)
    final = sum(len(polygon.vertices) - 2 for polygon in model.data.polygons)

    # Only the colour textures, shrunk so the bundle stays small (Valheim's own are rarely larger): the mod lights the
    # models with Valheim's materials, so normal and roughness maps would only take room.
    limit = spec.get("texture", 512)
    for slot in model.material_slots:
        if slot.material is None or not slot.material.use_nodes:
            continue
        tree = slot.material.node_tree
        colour = colour_image(tree)
        for node in list(tree.nodes):
            if node.type == "TEX_IMAGE" and node.image is not colour:
                tree.nodes.remove(node)
        if colour is None:
            continue
        if max(colour.size) > limit:
            scale = limit / max(colour.size)
            colour.scale(max(1, round(colour.size[0] * scale)), max(1, round(colour.size[1] * scale)))
        # JPEG unless the alpha is used (leaves cut out of cards).
        colour.file_format = "PNG" if slot.material.blend_method != "OPAQUE" or uses_alpha(tree) else "JPEG"
        colour.pack()

    bpy.ops.export_scene.gltf(filepath=output, export_format="GLB", use_selection=False, export_image_format="AUTO",
                              export_animations=False, export_skins=False, export_morph=False)

    size = model.dimensions
    # Blender is Z-up; the height is along Z.
    print("[decor] " + json.dumps({"height": size.z, "width": size.x, "depth": size.y, "tris": final, "from": triangles}))


main()
