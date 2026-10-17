"""Blender script: dresses the Mixamo character Arissa as the Huldra and writes AssetSource/Creatures/huldra.glb.

Run headless on the glb merge_mixamo.py made from the Mixamo downloads (kept outside the repo, see that script):
  blender -b --factory-startup --python dress_huldra.py -- <huldra_base.glb> <out huldra.glb>

What it changes:
- drops her cloak with its hood and her daggers, so she stands in her top and skirt, bare-headed;
- gives her long fair hair, which the model lacks (it is only painted on the scalp under the hood): a cap shrink-wrapped
  to the head and one thick braid down the back, skinned to the head, the neck and the spine so it follows her;
- gives her a cow's tail, the Huldra's mark, skinned to the hips;
- on her texture, paints out the war paint on her face and the tattoos on her arm, turns her eyes blue and the hair
  painted at her temples fair, and her tanned skin paler.
"""
import math
import sys

import bmesh
import bpy
import mathutils
import numpy

HAIR = (0.93, 0.8, 0.5)
# Points (Blender world, z up, the back is +y) the braid runs through, from the back of the head to the waist, with
# the bones that carry each part.
BRAID = [((0.0, 0.07, 1.63), "Head"), ((0.0, 0.085, 1.52), "Neck"), ((0.0, 0.118, 1.41), "Spine2"),
         ((0.0, 0.118, 1.30), "Spine2"), ((0.0, 0.08, 1.20), "Spine1"), ((0.0, 0.06, 1.10), "Spine1")]
TAIL = [(0.0, 0.095, 1.0), (0.0, 0.135, 0.86), (0.0, 0.15, 0.72), (0.0, 0.135, 0.58)]


def bone(name):
    return f"mixamorig:{name}"


def hair_material():
    """A fair, streaked hair texture made here, so the hair needs no download."""
    size = 256
    rng = numpy.random.default_rng(7)
    strands = numpy.repeat(rng.normal(0.0, 1.0, (1, size)), size, axis=0)
    strands = numpy.cumsum(rng.normal(0.0, 0.15, (size, size)), axis=0) * 0.05 + strands
    shade = numpy.clip(0.85 + 0.12 * numpy.tanh(strands), 0.0, 1.0)
    pixels = numpy.ones((size, size, 4), dtype=numpy.float32)
    for channel, value in enumerate(HAIR):
        pixels[:, :, channel] = value * shade
    image = bpy.data.images.new("huldra_hair", size, size)
    image.pixels.foreach_set(pixels.ravel())
    image.pack()
    material = bpy.data.materials.new("huldra_hair")
    material.use_nodes = True
    shader = material.node_tree.nodes["Principled BSDF"]
    texture = material.node_tree.nodes.new("ShaderNodeTexImage")
    texture.image = image
    material.node_tree.links.new(texture.outputs["Color"], shader.inputs["Base Color"])
    shader.inputs["Roughness"].default_value = 0.6
    return material


def skin(obj, armature, weights):
    """Skins obj to the armature with one bone weight list per vertex: [(bone, weight), ...]."""
    groups = {}
    for index, pairs in enumerate(weights):
        for name, weight in pairs:
            group = groups.get(name) or obj.vertex_groups.new(name=bone(name))
            groups[name] = group
            group.add([index], weight, "REPLACE")
    modifier = obj.modifiers.new("Armature", "ARMATURE")
    modifier.object = armature
    obj.parent = armature
    obj.matrix_parent_inverse = armature.matrix_world.inverted()


def cap(body, armature, material):
    """A cap of hair over the scalp: the head's own top and back, pushed out a little."""
    head = body.vertex_groups[bone("Head")].index
    mesh = bmesh.new()
    mesh.from_mesh(body.data)
    mesh.transform(body.matrix_world)
    keep = set()
    for vertex in mesh.verts:
        weight = next((g.weight for g in body.data.vertices[vertex.index].groups if g.group == head), 0.0)
        p = vertex.co
        # The face (front, below the hairline) stays bare; the top, the back and the sides above the ears get hair.
        scalp = p.z > 1.705 or (p.y > -0.035 and p.z > 1.585) or (p.y > -0.085 and p.z > 1.665)
        if weight > 0.6 and scalp:
            keep.add(vertex.index)
    bmesh.ops.delete(mesh, geom=[v for v in mesh.verts if v.index not in keep], context="VERTS")
    for vertex in mesh.verts:
        vertex.co += vertex.normal * 0.009
    data = bpy.data.meshes.new("huldra_hair_cap")
    mesh.to_mesh(data)
    obj = bpy.data.objects.new("huldra_hair_cap", data)
    bpy.context.collection.objects.link(obj)
    data.materials.append(material)
    unwrap(obj)
    skin(obj, armature, [[("Head", 1.0)]] * len(data.vertices))
    return obj


def braid(armature, material):
    """One thick braid: lobes that alternate side to side along the path, thinner towards the end, and a tuft."""
    mesh = bmesh.new()
    weights = []
    path = [mathutils.Vector(p) for p, _ in BRAID]
    lobes = 16
    for i in range(lobes):
        t = i / (lobes - 1) * (len(path) - 1)
        k = min(int(t), len(path) - 2)
        centre = path[k].lerp(path[k + 1], t - k)
        radius = 0.034 - 0.014 * i / lobes
        centre.x += 0.012 * (1 if i % 2 else -1)
        lobe = bmesh.ops.create_uvsphere(mesh, u_segments=8, v_segments=6, radius=radius)
        for vertex in lobe["verts"]:
            vertex.co = mathutils.Vector((vertex.co.x, vertex.co.y * 0.8, vertex.co.z * 1.5)) + centre
        upper, lower = BRAID[k][1], BRAID[k + 1][1]
        share = t - k
        weights += [[(upper, 1.0 - share), (lower, share)] if upper != lower else [(upper, 1.0)]] * len(lobe["verts"])
    tip = path[-1] + mathutils.Vector((0.0, -0.005, -0.05))
    tuft = bmesh.ops.create_cone(mesh, segments=8, radius1=0.012, radius2=0.03, depth=0.08)
    for vertex in tuft["verts"]:
        vertex.co += tip
    weights += [[(BRAID[-1][1], 1.0)]] * len(tuft["verts"])
    return finish(mesh, "huldra_braid", armature, material, weights)


def tail(armature, material):
    """A cow's tail from the small of the back, hanging behind the legs, with a tuft at the end."""
    mesh = bmesh.new()
    path = [mathutils.Vector(p) for p in TAIL]
    rings, sides = 12, 8
    ring_verts = []
    for i in range(rings):
        t = i / (rings - 1) * (len(path) - 1)
        k = min(int(t), len(path) - 2)
        centre = path[k].lerp(path[k + 1], t - k)
        direction = (path[k + 1] - path[k]).normalized()
        side = direction.cross(mathutils.Vector((1.0, 0.0, 0.0))).normalized()
        across = mathutils.Vector((1.0, 0.0, 0.0))
        radius = 0.016 - 0.006 * i / rings
        ring_verts.append([mesh.verts.new(centre + (side * math.cos(a) + across * math.sin(a)) * radius)
                           for a in (2 * math.pi * s / sides for s in range(sides))])
    for upper, lower in zip(ring_verts, ring_verts[1:]):
        for s in range(sides):
            mesh.faces.new((upper[s], upper[(s + 1) % sides], lower[(s + 1) % sides], lower[s]))
    tuft = bmesh.ops.create_cone(mesh, segments=10, radius1=0.035, radius2=0.01, depth=0.16)
    for vertex in tuft["verts"]:
        vertex.co += path[-1] + mathutils.Vector((0.0, -0.005, -0.08))
    weights = [[("Hips", 1.0)]] * len(mesh.verts)
    return finish(mesh, "huldra_tail", armature, material, weights)


def finish(mesh, name, armature, material, weights):
    mesh.verts.ensure_lookup_table()
    data = bpy.data.meshes.new(name)
    mesh.to_mesh(data)
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    data.materials.append(material)
    unwrap(obj)
    skin(obj, armature, weights)
    return obj


def unwrap(obj):
    bpy.ops.object.select_all(action="DESELECT")
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(66))
    bpy.ops.object.mode_set(mode="OBJECT")


def repaint(image):
    """Paints out the face paint and the arm tattoos, turns the eyes blue and the painted hair fair."""
    width, height = image.size
    pixels = numpy.array(image.pixels[:], dtype=numpy.float32).reshape(height, width, 4)
    # Boxes in texture pixels from the top left, as the texture is drawn.
    def box(x0, y0, x1, y1):
        return slice(height - y1, height - y0), slice(x0, x1)

    rgb = pixels[:, :, :3]
    value = rgb.max(axis=2)
    low = rgb.min(axis=2)
    saturation = numpy.where(value > 0, (value - low) / numpy.maximum(value, 1e-6), 0)
    red, green, blue = rgb[:, :, 0], rgb[:, :, 1], rgb[:, :, 2]
    mask = numpy.zeros((height, width), dtype=bool)
    # War paint across the nose and mouth: dark, red-brown marks on the skin.
    face = box(760, 115, 900, 185)
    mask[face] |= value[face] < 0.55
    # The sun tattoo and the bands on the upper arm: dark ink on skin.
    arm = box(345, 5, 532, 130)
    mask[arm] |= value[arm] < 0.55
    # Two pixels more round each mark, so its soft edge goes too.
    for _ in range(2):
        mask |= numpy.roll(mask, 1, 0) | numpy.roll(mask, -1, 0) | numpy.roll(mask, 1, 1) | numpy.roll(mask, -1, 1)
    inpaint(pixels, mask)

    # Brown irises to blue.
    eye = box(325, 978, 400, 1020)
    iris = (saturation[eye] > 0.2) & (red[eye] > blue[eye]) & (value[eye] > 0.12)
    shade = value[eye][iris]
    pixels[eye][iris, 0] = shade * 0.35
    pixels[eye][iris, 1] = shade * 0.62
    pixels[eye][iris, 2] = numpy.minimum(shade * 1.35 + 0.15, 1.0)
    # The auburn hair painted beside the face (its two tails), fair like the new hair. The skin there is lighter.
    for temples in (box(650, 60, 745, 268), box(885, 55, 1010, 268)):
        value = pixels[:, :, :3].max(axis=2)
        auburn = value[temples] < 0.55
        shade = numpy.clip(value[temples][auburn] * 1.6, 0, 1)
        for channel, colour in enumerate(HAIR):
            pixels[temples][auburn, channel] = shade * colour
    # Fair Nordic skin: the tanned, orange skin paler and less saturated. Clothes, eyes and hair are not skin-coloured.
    rgb = pixels[:, :, :3]
    value = rgb.max(axis=2)
    low = rgb.min(axis=2)
    saturation = numpy.where(value > 0, (value - low) / numpy.maximum(value, 1e-6), 0)
    skin_tone = (saturation > 0.38) & (saturation < 0.75) & (value > 0.45) & (rgb[:, :, 0] > rgb[:, :, 1]) & (rgb[:, :, 1] > rgb[:, :, 2])
    grey = value[skin_tone][:, None]
    pixels[skin_tone, :3] = numpy.clip((rgb[skin_tone] * 0.55 + grey * 0.45) * 1.12, 0, 1)
    image.pixels.foreach_set(pixels.ravel())
    image.pack()


def inpaint(pixels, mask):
    """Fills masked pixels from their neighbours, ring by ring, so a mark takes the colour of the skin round it."""
    mask = mask.copy()
    for _ in range(64):
        if not mask.any():
            return
        known = ~mask
        total = numpy.zeros_like(pixels[:, :, :3])
        count = numpy.zeros(mask.shape, dtype=numpy.float32)
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            shifted_known = numpy.roll(known, (dy, dx), axis=(0, 1))
            shifted = numpy.roll(pixels[:, :, :3], (dy, dx), axis=(0, 1))
            total += shifted * shifted_known[:, :, None]
            count += shifted_known
        fill = mask & (count > 0)
        pixels[fill, :3] = total[fill] / count[fill][:, None]
        mask &= ~fill


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    source, out = argv[0], argv[1]
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=source)
    for obj in list(bpy.data.objects):
        if obj.type == "MESH" and (obj.name in ("arissa:Cloak_Geo", "arissa:Weapons_Geo") or obj.parent is None):
            bpy.data.objects.remove(obj, do_unlink=True)

    armature = next(obj for obj in bpy.data.objects if obj.type == "ARMATURE")
    body = bpy.data.objects["arissa:Body_Geo"]
    repaint(bpy.data.images["Arissa_DIFF_diffuse"])
    material = hair_material()
    cap(body, armature, material)
    braid(armature, material)
    tail(armature, material)

    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.gltf(filepath=out, export_format="GLB", use_selection=True, export_animations=True,
                              export_animation_mode="ACTIONS", export_force_sampling=True, export_yup=True)
    print(f"wrote {out}")


main()
