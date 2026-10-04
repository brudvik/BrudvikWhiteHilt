"""Render a separate ship layout concept with Blender, without changing the source GLB or game assets.

Run Blender with --background --python AssetSource/Preview/render_ship_layout.py -- <source.glb> [--export <folder>].
Requires the existing ignored Preview/vanilla JSON meshes. Outputs PNGs, a packed .blend and checks.json
in BrudvikWhiteHiltUnity/Preview/out/sailing_ship_layout. Coordinates in the layout JSON are source glTF.
Objects named Collider* are invisible walking and wall colliders; export_ship.py writes them to the manifest.
"""
import hashlib
import json
import math
import pathlib
import sys

import bmesh
import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree


REPO = pathlib.Path(__file__).resolve().parents[2]
VANILLA = REPO / 'BrudvikWhiteHiltUnity' / 'Preview' / 'vanilla'
OUTPUT = REPO / 'BrudvikWhiteHiltUnity' / 'Preview' / 'out' / 'sailing_ship_layout'
SPEC = json.loads(pathlib.Path(__file__).with_name('sailing_ship.layout.json').read_text(encoding='utf-8'))


def point(position):
    return Vector((position[0], -position[2], position[1]))


def material(name, colour):
    result = bpy.data.materials.new(name)
    result.diffuse_color = (*colour, 1)
    result.use_nodes = True
    shader = result.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value = (*colour, 1)
    shader.inputs['Roughness'].default_value = 0.75
    return result


def box(name, position, dimensions, surface):
    bpy.ops.mesh.primitive_cube_add(size=1, location=point(position))
    result = bpy.context.object
    result.name = name
    result.dimensions = (dimensions[0], dimensions[2], dimensions[1])
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    result.data.materials.append(surface)
    return result


def cylinder(name, position, radius, depth, surface, vertices=16):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=point(position))
    result = bpy.context.object
    result.name = name
    result.data.materials.append(surface)
    return result


def beam(name, start, end, thickness, surface):
    start_point, end_point = point(start), point(end)
    midpoint = (start_point + end_point) / 2
    result = box(name, (midpoint.x, midpoint.z, -midpoint.y), (thickness, (end_point - start_point).length, thickness), surface)
    result.rotation_euler = (end_point - start_point).to_track_quat('Z', 'Y').to_euler()
    return result


def baked(obj, name):
    mesh = obj.data.copy()
    result = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(result)
    geometry = bmesh.new()
    geometry.from_mesh(mesh)
    bmesh.ops.transform(geometry, matrix=obj.matrix_world, verts=list(geometry.verts))
    geometry.to_mesh(mesh)
    geometry.free()
    return result


def clip(obj, origin, normal):
    geometry = bmesh.new()
    geometry.from_mesh(obj.data)
    bmesh.ops.bisect_plane(geometry, geom=list(geometry.verts) + list(geometry.edges) + list(geometry.faces),
                          dist=0.0001, plane_co=Vector(origin), plane_no=Vector(normal), clear_outer=True)
    geometry.to_mesh(obj.data)
    geometry.free()


def ladder_opening(obj, position, width, surface):
    opening = box('LookoutOpeningTool', position, (width, 2, width), surface)
    modifier = obj.modifiers.new('LadderOpening', 'BOOLEAN')
    modifier.operation = 'DIFFERENCE'
    modifier.object = opening
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    bpy.data.objects.remove(opening, do_unlink=True)


def imported_piece(mesh_name, name, position, yaw=0, override=None):
    data = json.loads((VANILLA / (mesh_name + '.json')).read_text(encoding='utf-8-sig'))
    parent = bpy.data.objects.new(name, None)
    bpy.context.scene.collection.objects.link(parent)
    all_points = []
    for part_index, part in enumerate(data['parts']):
        vertices = [point(part['vertices'][offset:offset + 3]) for offset in range(0, len(part['vertices']), 3)]
        faces = [tuple(reversed(part['triangles'][offset:offset + 3])) for offset in range(0, len(part['triangles']), 3)]
        mesh = bpy.data.meshes.new(name + str(part_index))
        mesh.from_pydata(vertices, [], faces)
        mesh.update()
        obj = bpy.data.objects.new(mesh.name, mesh)
        bpy.context.scene.collection.objects.link(obj)
        obj.parent = parent
        surface = override
        if surface is None:
            surface = material(mesh.name, (0.55, 0.55, 0.55))
            texture_path = VANILLA / 'textures' / (part.get('texture', '') + '.png')
            if texture_path.exists():
                texture = surface.node_tree.nodes.new('ShaderNodeTexImage')
                texture.image = bpy.data.images.load(str(texture_path), check_existing=True)
                shader = surface.node_tree.nodes.get('Principled BSDF')
                surface.node_tree.links.new(texture.outputs['Color'], shader.inputs['Base Color'])
                uv_layer = mesh.uv_layers.new(name='UVMap')
                for loop in mesh.loops:
                    uv_offset = loop.vertex_index * 2
                    uv_layer.data[loop.index].uv = part['uvs'][uv_offset:uv_offset + 2]
        mesh.materials.append(surface)
        all_points.extend(vertices)
    assert all_points, mesh_name
    lowest = min(vertex.z for vertex in all_points)
    parent.location = point(position) - Vector((0, 0, lowest))
    parent.rotation_euler.z = math.radians(yaw)
    return parent, all_points


COLLIDER = None


def wood_like_hull(name):
    """Material for new woodwork, using the hull's own tiling wood texture."""
    hull = bpy.data.materials['Madeira']
    image = next(node.image for node in hull.node_tree.nodes if node.type == 'TEX_IMAGE' and node.image)
    result = material(name, (0.25, 0.19, 0.15))
    texture = result.node_tree.nodes.new('ShaderNodeTexImage')
    texture.image = image
    result.node_tree.links.new(texture.outputs['Color'], result.node_tree.nodes.get('Principled BSDF').inputs['Base Color'])
    return result


def box_uvs(obj, tile):
    """World-scaled box projection, with the grain along the ship on decks and along each beam's length."""
    mesh = obj.data
    layer = mesh.uv_layers.active or mesh.uv_layers.new(name='UVMap')
    to_world = obj.matrix_world
    for polygon in mesh.polygons:
        normal = (to_world.to_3x3() @ polygon.normal).normalized()
        axis = max(range(3), key=lambda index: abs(normal[index]))
        for loop_index in polygon.loop_indices:
            world = to_world @ mesh.vertices[mesh.loops[loop_index].vertex_index].co
            if axis == 2:
                uv = (world.y, world.x)
            elif axis == 0:
                uv = (world.y, world.z)
            else:
                uv = (world.x, world.z)
            layer.data[loop_index].uv = (uv[0] / tile, uv[1] / tile)


def collider(name, centre, size, pitch=0.0, yaw=0.0):
    """Invisible box in ship space; pitch tips local +z down, yaw turns local +z towards +x (Unity Euler order)."""
    result = box('Collider' + name, tuple(centre), size, COLLIDER)
    result.rotation_euler = (pitch, 0, yaw)
    result.hide_render = True
    return result


def slab(name, centre_x, width, start, end, thickness, surface=None):
    """Box whose top face runs from start to end, each (z, top height); a collider unless a surface is given."""
    (z0, h0), (z1, h1) = sorted((tuple(start), tuple(end)))
    length = math.hypot(z1 - z0, h1 - h0)
    pitch = -math.atan2(h1 - h0, z1 - z0)
    normal = Vector((0, (z1 - z0) / length, -(h1 - h0) / length))
    centre = Vector((centre_x, (h0 + h1) / 2, (z0 + z1) / 2)) - normal * thickness / 2
    if surface is None:
        return collider(name, centre, (width, thickness, length), pitch)
    result = box(name, tuple(centre), (width, thickness, length), surface)
    result.rotation_euler = (pitch, 0, 0)
    return result


def wall(name, start, end, base, height, thickness=0.12):
    """Upright invisible wall between two (x, z) points."""
    dx, dz = end[0] - start[0], end[1] - start[1]
    return collider(name, ((start[0] + end[0]) / 2, base + height / 2, (start[1] + end[1]) / 2),
                    (thickness, height, math.hypot(dx, dz)), 0.0, math.atan2(dx, dz))


def rail_line(name, start, end, height, surface, spacing=1.0):
    """Visible posts and a top rail between two (x, z, base) points."""
    count = max(1, math.ceil(math.hypot(end[0] - start[0], end[1] - start[1]) / spacing))
    for index in range(count + 1):
        t = index / count
        x, z, base = (start[axis] + (end[axis] - start[axis]) * t for axis in range(3))
        beam(name + 'Post', (x, base - 0.05, z), (x, base + height, z), 0.07, surface)
    beam(name + 'Top', (start[0], start[2] + height, start[1]), (end[0], end[2] + height, end[1]), 0.08, surface)


def stair_colliders(name, centre_x, width, bottom, top, steps):
    """Ramp through the centres of the tread tops, so feet stay within half a rise of them, and a landing for the top tread."""
    (bottom_z, bottom_y), (top_z, top_y) = bottom, top
    half = (top_z - bottom_z) / steps / 2
    slab('Ramp' + name, centre_x, width, (bottom_z - half, bottom_y), (top_z - half, top_y), 0.2)
    collider('Landing' + name, (centre_x, top_y - 0.1, top_z - half / 2), (width, 0.2, abs(half)))


def flight(entry, surface):
    """Visible treads of a short stair and the colliders players walk on."""
    (bottom_z, bottom), (top_z, top) = entry['bottom'], entry['top']
    steps = entry['steps']
    for index in range(steps):
        z0 = bottom_z + (top_z - bottom_z) * index / steps
        z1 = bottom_z + (top_z - bottom_z) * (index + 1) / steps
        height = bottom + (top - bottom) * (index + 1) / steps
        base = bottom - 0.05
        box('StepTread' + entry['name'], (entry['centreX'], (height + base) / 2, (z0 + z1) / 2),
            (entry['width'], height - base, abs(z1 - z0)), surface)
    stair_colliders(entry['name'], entry['centreX'], entry['width'], entry['bottom'], entry['top'], steps)


def main():
    global COLLIDER
    arguments = sys.argv[sys.argv.index('--') + 1:]
    source = pathlib.Path(arguments[0])
    source_digest = hashlib.sha256(source.read_bytes()).hexdigest()
    OUTPUT.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(source))
    scene = bpy.context.scene
    source_objects = [obj for obj in scene.objects if obj.type == 'MESH']
    originals = {obj.name: obj for obj in source_objects}
    hull = baked(originals['Object_4'], 'ConceptHull')
    tree = BVHTree.FromPolygons([vertex.co.copy() for vertex in hull.data.vertices],
                                [tuple(face.vertices) for face in hull.data.polygons])
    room, stairs, lookout, waist = SPEC['room'], SPEC['stairs'], SPEC['lookout'], SPEC['waist']
    start_z = room['centreZ'] - room['length'] / 2
    end_z = room['centreZ'] + room['length'] / 2

    def half_width(height, length):
        origin = point((0, height, length))
        right = tree.ray_cast(origin, Vector((1, 0, 0)), 10)[0]
        left = tree.ray_cast(origin, Vector((-1, 0, 0)), 10)[0]
        assert right is not None and left is not None, (height, length)
        return min(right.x, -left.x)

    widths = [round(2 * half_width(room['floorTop'], length), 3) for length in range(-3, 8)]
    assert min(widths) >= room['width'] + 0.2, widths
    headroom = room['mainDeckTop'] - room['mainDeckThickness'] - room['floorTop']
    assert headroom >= 2.5, headroom
    assert len(SPEC['upgrades']) == 9

    for obj in source_objects:
        obj.hide_render = obj.name in SPEC['hidden']
        obj.hide_viewport = obj.hide_render
    oak = wood_like_hull('Concept oak')
    gold = material('Reference crew', (0.8, 0.48, 0.12))
    brass = material('Rail fittings', (0.62, 0.45, 0.16))
    cloth = material('White Hilt canvas', (0.82, 0.84, 0.8))
    teal = material('Deck portal placeholder', (0.12, 0.58, 0.54))
    COLLIDER = material('Collider', (1, 0.05, 0.05))
    deck_top, floor_top = room['mainDeckTop'], room['floorTop']

    # Lower room: floor, walls following the hull and bulkheads at both ends.
    tiers = []
    for bottom, top in room['wallTiers']:
        inner = min(half_width(height, length) for length in [start_z + 0.25 * step for step in range(int(room['length'] * 4) + 1)]
                    for height in [bottom + (top - bottom) * step / 4 for step in range(5)]) - 0.02
        tiers.append((bottom, top, inner))
    assert all(inner >= room['width'] / 2 + 0.1 for bottom, top, inner in tiers), tiers
    floor_half = min(room['floorWidth'] / 2, tiers[0][2])
    structural = []
    lower_floor = box('UnderdeckFloor', (0, floor_top - room['floorThickness'] / 2, room['centreZ']),
                      (2 * floor_half, room['floorThickness'], room['length']), oak)
    structural.append(lower_floor)
    for bottom, top, inner in tiers:
        for side in (-1, 1):
            collider('RoomWall', (side * (inner + 0.1), (bottom + top) / 2, room['centreZ']), (0.2, top - bottom, room['length']))
            # Planking where the wall collider is, so drawers hang on a visible wall rather than in front of the hull.
            box('RoomLining', (side * (inner + 0.03), (bottom + top) / 2, room['centreZ']), (0.06, top - bottom, room['length']), oak)
        for length, outward in ((start_z, -1), (end_z, 1)):
            collider('BulkheadWall', (0, (bottom + top) / 2, length + outward * 0.05), (2 * inner + 0.4, top - bottom, 0.1))
    for length, outward in ((start_z, -1), (end_z, 1)):
        bottom, top = floor_top - room['floorThickness'], deck_top - room['mainDeckThickness']
        heights = [bottom + (top - bottom) * step / 12 for step in range(13)]
        outline = [(half_width(height, length) - 0.01, height) for height in heights]
        vertices = [point((x, height, length)) for x, height in outline]
        vertices += [point((-x, height, length)) for x, height in reversed(outline)]
        mesh = bpy.data.meshes.new('Bulkhead')
        mesh.from_pydata(vertices, [], [tuple(range(len(vertices)))])
        mesh.update()
        panel = bpy.data.objects.new('Bulkhead', mesh)
        scene.collection.objects.link(panel)
        solidify = panel.modifiers.new('Thickness', 'SOLIDIFY')
        solidify.thickness = 0.1
        solidify.offset = 0
        panel.location = point((0, 0, outward * 0.05))
        panel.data.materials.append(oak)
        bpy.context.view_layer.objects.active = panel
        bpy.ops.object.modifier_apply(modifier='Thickness')

    # Waist deck around the stair opening; the visible planks stop at the hull's own side planking.
    hole_min_x = stairs['centreX'] - stairs['width'] / 2
    hole_max_x = stairs['centreX'] + stairs['width'] / 2
    hole_min_z, hole_max_z = stairs['holeMinZ'], stairs['holeMinZ'] + stairs['run']
    assert hole_max_z <= end_z - 1.0
    for prefix, half, is_collider in (('MainDeck', waist['visualHalfWidth'], False), ('WaistDeck', waist['walkHalfWidth'], True)):
        rectangles = [(-half, hole_min_x, waist['startZ'], waist['endZ']),
                      (hole_max_x, half, waist['startZ'], waist['endZ']),
                      (hole_min_x, hole_max_x, waist['startZ'], hole_min_z),
                      (hole_min_x, hole_max_x, hole_max_z, waist['endZ'])]
        for index, (min_x, max_x, min_z, max_z) in enumerate(rectangles):
            if max_x - min_x < 0.01:
                continue
            centre = ((min_x + max_x) / 2, deck_top - room['mainDeckThickness'] / 2, (min_z + max_z) / 2)
            size = (max_x - min_x, room['mainDeckThickness'], max_z - min_z)
            if is_collider:
                collider(prefix + str(index), centre, size)
            else:
                structural.append(box(prefix + str(index), centre, size, oak))

    # Main stair: descends towards the bow from the opening's aft end.
    rise = (deck_top - floor_top) / stairs['steps']
    for index in range(stairs['steps']):
        top = floor_top + rise * (index + 1)
        length = hole_max_z - stairs['run'] * (index + 0.5) / stairs['steps']
        box('StairTread' + str(index), (stairs['centreX'], top - 0.06, length), (stairs['width'], 0.12, stairs['run'] / stairs['steps']), oak)
    handrail_end = (deck_top - room['mainDeckThickness'] - 0.05 - floor_top - stairs['handrailHeight']) / (deck_top - floor_top)
    for side in (-1, 1):
        rail_x = stairs['centreX'] + side * stairs['width'] / 2
        beam('StairStringer', (rail_x, floor_top, hole_max_z), (rail_x, deck_top, hole_min_z), 0.12, oak)
        beam('StairHandrail', (rail_x, floor_top + stairs['handrailHeight'], hole_max_z),
             (rail_x, floor_top + stairs['handrailHeight'] + (deck_top - floor_top) * handrail_end,
              hole_max_z - stairs['run'] * handrail_end), 0.07, brass)
        # The inboard rail starts further forward, leaving room to pass between the main mast and the opening.
        rail_start = hole_min_z + (stairs['inboardRailGap'] if side < 0 else 0.0)
        collider('OpeningRail', (rail_x, deck_top + 0.475, (rail_start + hole_max_z) / 2), (0.1, 0.95, hole_max_z - rail_start))
        rail_line('DeckRailOpening', (rail_x, rail_start + 0.05, deck_top), (rail_x, hole_max_z - 0.05, deck_top), 0.95, oak)
    collider('OpeningRail', (stairs['centreX'], deck_top + 0.475, hole_max_z), (stairs['width'], 0.95, 0.1))
    rail_line('DeckRailOpening', (hole_min_x + 0.05, hole_max_z, deck_top), (hole_max_x - 0.05, hole_max_z, deck_top), 0.95, oak)
    stair_colliders('MainStair', stairs['centreX'], stairs['width'], (hole_max_z, floor_top), (hole_min_z, deck_top), stairs['steps'])
    stair_angle = math.degrees(math.atan2(deck_top - floor_top, stairs['run']))
    assert stair_angle < 35, stair_angle

    for length in (-3, 2, 7):
        for sideways in (-2.5, 2.5):
            box('UnderdeckPost', (sideways, (floor_top + deck_top - 0.2) / 2, length), (0.12, headroom, 0.12), oak)
        if hole_min_z < length < hole_max_z:
            left, right = -2.6, hole_min_x - 0.05
            structural.append(box('CeilingBeam', ((left + right) / 2, deck_top - 0.26, length), (right - left, 0.12, 0.12), oak))
        else:
            structural.append(box('CeilingBeam', (0, deck_top - 0.26, length), (5.2, 0.12, 0.12), oak))

    # Raised decks of the source model, walls along their rails and the stairs between the levels.
    quarter, poop = SPEC['quarterdeck'], SPEC['poop']
    slab('Quarterdeck', 0, 2 * quarter['halfWidth'], quarter['front'], quarter['back'], quarter['thickness'])
    slab('Poop', 0, 2 * poop['halfWidth'], poop['front'], poop['back'], poop['thickness'])
    # The poop's front face is upright, so a tilted slab would lean out over the quarterdeck.
    block = poop['frontBlock']
    collider('PoopFront', (0, (block['bottom'] + block['top']) / 2, (poop['front'][0] + block['backZ']) / 2),
             (2 * poop['halfWidth'], block['top'] - block['bottom'], poop['front'][0] - block['backZ']))
    for part in SPEC['forecastle']:
        slab('Forecastle', 0, 2 * part['halfWidth'], part['from'], part['to'], part['thickness'])
    for side in (-1, 1):
        for deck, rail_height in ((quarter, 1.1), (poop, 1.1)):
            (z0, h0), (z1, h1) = deck['front'], deck['back']
            slab('DeckWall', side * (deck['wallX'] + 0.06), 0.12, (z0, h0 + rail_height), (z1, h1 + rail_height), rail_height + 0.4)
        corners = [(side * x, z) for x, z in SPEC['forecastleWall']]
        for start, end in zip(corners, corners[1:]):
            wall('ForecastleWall', start, end, 3.3, 1.25)
        x = side * waist['railX']
        gaps = [waist['boardingGap']] if side < 0 else []
        segments, previous = [], waist['startZ']
        for gap_start, gap_end in gaps:
            segments.append((previous, gap_start))
            previous = gap_end
        segments.append((previous, waist['endZ']))
        for z0, z1 in segments:
            collider('WaistRail', (x, deck_top + waist['railHeight'] / 2, (z0 + z1) / 2), (0.1, waist['railHeight'], z1 - z0))
            rail_line('DeckRailWaist', (x, z0 + 0.05, deck_top), (x, z1 - 0.05, deck_top), waist['railHeight'], oak)
    wall('SternWall', (-poop['halfWidth'], poop['sternZ'] - 0.06), (poop['halfWidth'], poop['sternZ'] - 0.06), 4.6, 1.2)
    lanterns = SPEC['sternLanterns']
    for side in (-1, 1):
        x = side * lanterns['x']
        beam('LanternPost', (x, lanterns['bottom'], lanterns['z']), (x, lanterns['top'], lanterns['z']), 0.1, oak)
        beam('LanternArm', (x, lanterns['top'] - 0.05, lanterns['z'] + 0.05), (x, lanterns['top'] - 0.05, lanterns['z'] - lanterns['arm']), 0.07, oak)
    rudder = SPEC['rudder']
    tilt = math.radians(rudder['tilt'])
    for step in range(11):
        along = rudder['top'] * step / 10
        height = rudder['hinge'][1] + along * math.cos(tilt)
        length = rudder['hinge'][2] + along * math.sin(tilt)
        stern = tree.ray_cast(point((0, height, -30)), Vector((0, -1, 0)), 60)[0]
        assert stern is None or length < -stern.y, ('rudder hinge inside the hull', height, length, -stern.y)
    rudder_wood = wood_like_hull('Rudder wood')
    # Built upright around the hinge at the origin; the game tilts it along the sternpost and turns it with the helm.
    rudder_parts = [box('RudderBlade', (0, sum(rudder['blade']) / 2, -rudder['chord'] / 2 - 0.03),
                        (rudder['thickness'], rudder['blade'][1] - rudder['blade'][0], rudder['chord']), rudder_wood),
                    box('RudderStock', (0, (rudder['blade'][1] + rudder['top']) / 2, -0.08),
                        (rudder['thickness'], rudder['top'] - rudder['blade'][1], 0.14), rudder_wood)]
    if '--export' not in arguments:
        hinge = bpy.data.objects.new('RudderHinge', None)
        scene.collection.objects.link(hinge)
        hinge.location = point(rudder['hinge'])
        hinge.rotation_euler = (math.radians(rudder['tilt']), 0, 0)
        for part in rudder_parts:
            part.parent = hinge
    for edge in SPEC['edgeRails']:
        for min_x, max_x in edge['spans']:
            collider('EdgeRail' + edge['name'], ((min_x + max_x) / 2, edge['base'] + 0.475, edge['z']), (max_x - min_x, 0.95, 0.1))
            rail_line('DeckRail' + edge['name'], (min_x + 0.05, edge['z'], edge['base']), (max_x - 0.05, edge['z'], edge['base']), 0.95, oak)
    for entry in SPEC['flights']:
        flight(entry, oak)
    for mast in SPEC['masts']:
        collider(mast['name'] + 'Mast', (0, (mast['bottom'] + mast['top']) / 2, mast['z']), (0.36, mast['top'] - mast['bottom'], 0.36))

    furniture_bounds = []
    for entry in SPEC['furniture']:
        if '--export' in arguments:
            continue
        if 'mesh' in entry:
            parent, vertices = imported_piece(entry['mesh'], entry['name'], entry['position'], entry.get('yaw', 0))
            bpy.context.view_layer.update()
            transformed = [parent.matrix_world @ vertex for vertex in vertices]
            bounds = {axis: [min(vertex[axis] for vertex in transformed), max(vertex[axis] for vertex in transformed)]
                      for axis in range(3)}
            assert bounds[2][1] < deck_top - room['mainDeckThickness'], (entry['name'], bounds)
            assert max(abs(bounds[0][0]), abs(bounds[0][1])) <= room['width'] / 2, (entry['name'], bounds)
            assert start_z <= -bounds[1][1] and -bounds[1][0] <= end_z, (entry['name'], bounds)
            furniture_bounds.append({'name': entry['name'], 'blender_bounds': bounds})
        else:
            dimensions = entry['box']
            position = list(entry['position'])
            position[1] += dimensions[1] / 2
            if entry['name'] == 'Table':
                box('TableTop', (position[0], position[1] + dimensions[1] / 2 - 0.04, position[2]),
                    (dimensions[0], 0.08, dimensions[2]), oak)
                for sideways in (-0.5, 0.5):
                    for length in (-0.25, 0.25):
                        box('TableLeg', (position[0] + sideways, position[1], position[2] + length), (0.08, 0.72, 0.08), oak)
            else:
                box(entry['name'], position, dimensions, oak)
    for entry in SPEC['crew']:
        if '--export' in arguments:
            continue
        imported_piece('uni_Player', entry['name'], entry['position'], entry['yaw'], gold)
    cylinder('MainMastFoot', (0, (floor_top + deck_top) / 2, lookout['mastZ']), 0.18, deck_top - floor_top, oak)
    underdeck_objects = {obj for obj in scene.objects if obj.type == 'MESH' and obj not in source_objects
                         and obj != hull and not obj.name.startswith('Collider')
                         and min((obj.matrix_world @ vertex.co).z for vertex in obj.data.vertices) < deck_top}

    lookout_floor = cylinder('UpperLookoutFloor', (0, lookout['floorTop'] - 0.1, lookout['mastZ']),
                             lookout['diameter'] / 2, 0.2, oak, 32)
    ladder_opening(lookout_floor, (lookout['ladderX'], lookout['floorTop'], lookout['mastZ']),
                   lookout['openingWidth'], oak)
    middle_lookout = baked(originals['Object_20'], 'MiddleLookoutWithOpening')
    originals['Object_20'].hide_render = True
    originals['Object_20'].hide_viewport = True
    ladder_opening(middle_lookout, (lookout['ladderX'], 11.5, lookout['mastZ']), lookout['openingWidth'], oak)
    bpy.context.view_layer.update()
    for platform in (lookout_floor, middle_lookout):
        platform_tree = BVHTree.FromPolygons([platform.matrix_world @ vertex.co for vertex in platform.data.vertices],
                                            [tuple(face.vertices) for face in platform.data.polygons])
        origin = point((lookout['ladderX'], 22, lookout['mastZ']))
        assert platform_tree.ray_cast(origin, Vector((0, 0, -1)), 25)[0] is None, platform.name
    for index in range(16):
        angle = math.tau * index / 16
        next_angle = math.tau * (index + 1) / 16
        rail_radius = lookout['diameter'] / 2 - 0.05
        start = (rail_radius * math.cos(angle), lookout['floorTop'], lookout['mastZ'] + rail_radius * math.sin(angle))
        end = (start[0], start[1] + lookout['railHeight'], start[2])
        beam('LookoutPost', start, end, 0.07, oak)
        next_end = (rail_radius * math.cos(next_angle), end[1], lookout['mastZ'] + rail_radius * math.sin(next_angle))
        beam('LookoutRail', end, next_end, 0.07, brass)
    for side in (-1, 1):
        sideways = lookout['ladderX'] + side * lookout['ladderWidth'] / 2
        beam('MastLadderRail', (sideways, deck_top, lookout['mastZ']),
             (sideways, lookout['floorTop'] + 0.8, lookout['mastZ']), 0.06, oak)
    height = deck_top + lookout['rungSpacing']
    while height < lookout['floorTop']:
        beam('MastLadderRung', (lookout['ladderX'] - lookout['ladderWidth'] / 2, height, lookout['mastZ']),
             (lookout['ladderX'] + lookout['ladderWidth'] / 2, height, lookout['mastZ']), 0.045, brass)
        height += lookout['rungSpacing']

    for upgrade in SPEC['upgrades']:
        if '--export' in arguments:
            continue
        position = upgrade['position']
        identifier = upgrade['id']
        if identifier == 'cargo_barrels':
            for offset in (-0.35, 0.35):
                cylinder(identifier, (position[0] + offset, position[1] + 0.45, position[2]), 0.3, 0.9, oak)
        elif identifier == 'tent':
            for sideways in (-0.85, 0.85):
                for length in (-1.0, 1.0):
                    beam('TentPole', (position[0] + sideways, position[1], position[2] + length),
                         (position[0] + sideways, position[1] + 2.1, position[2] + length), 0.06, oak)
            box('TentCanvas', (position[0], position[1] + 2.15, position[2]), (1.8, 0.06, 2.2), cloth)
        elif identifier == 'ship_portal':
            cylinder(identifier, (position[0], position[1] + 0.04, position[2]), 0.7, 0.08, teal, 32)
        elif identifier == 'brazier':
            imported_piece('preview_eternalfire', identifier, position)
        elif identifier == 'drift_anchor':
            parent, vertices = imported_piece('preview_shipanchor', identifier, position)
            parent.scale = (0.6, 0.6, 0.6)
        elif identifier == 'fishing_net':
            box(identifier, (position[0], position[1] - 0.35, position[2]), (0.08, 1.5, 1.3), teal)
        elif identifier == 'sea_chest':
            box(identifier, (position[0], position[1] + 0.35, position[2]), (1.0, 0.7, 0.6), oak)
        else:
            cylinder(identifier, position, 0.15, 0.35, gold if identifier == 'lantern' else teal)
    table = SPEC['navigatorTable']
    if '--export' not in arguments:
        imported_piece('cartodesk', 'NavigatorTable', table['position'], table['yaw'])
    bpy.context.view_layer.update()
    for obj in scene.objects:
        if obj.type == 'MESH' and obj not in source_objects and any(surface in (oak, rudder_wood) for surface in obj.data.materials):
            box_uvs(obj, 1.5)

    if '--export' in arguments:
        scene['whitehilt_brightness'] = SPEC['hullBrightness']
        scene['whitehilt_logo'] = json.dumps(SPEC['logo'])
        scene['whitehilt_rudder'] = json.dumps({'hinge': rudder['hinge'], 'tilt': rudder['tilt']})
        import importlib.util
        module_spec = importlib.util.spec_from_file_location('ship_export', REPO / 'AssetSource' / 'Tools' / 'export_ship.py')
        module = importlib.util.module_from_spec(module_spec)
        module_spec.loader.exec_module(module)
        module.export(arguments[arguments.index('--export') + 1])
        assert hashlib.sha256(source.read_bytes()).hexdigest() == source_digest
        return

    scene.render.engine = 'CYCLES'
    scene.cycles.samples = 32
    scene.render.resolution_x = 1600
    scene.render.resolution_y = 1100
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.world = bpy.data.worlds.new('LayoutWorld')
    scene.world.use_nodes = True
    scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.73, 0.78, 0.8, 1)
    scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 0.8
    scene.view_settings.view_transform = 'Standard'
    for name, position in [('Key', (16, -12, 30)), ('Fill', (-16, 10, 25))]:
        light_data = bpy.data.lights.new(name, 'AREA')
        light_data.energy = 4500
        light_data.size = 18
        light = bpy.data.objects.new(name, light_data)
        scene.collection.objects.link(light)
        light.location = position
        light.rotation_euler = (point((0, 3, 2)) - light.location).to_track_quat('-Z', 'Y').to_euler()
    camera_data = bpy.data.cameras.new('LayoutCamera')
    camera = bpy.data.objects.new('LayoutCamera', camera_data)
    scene.collection.objects.link(camera)
    camera_data.type = 'ORTHO'
    camera_data.clip_end = 300
    scene.camera = camera

    def render(name, location, target, scale):
        camera.location = location
        camera.rotation_euler = (Vector(target) - camera.location).to_track_quat('-Z', 'Y').to_euler()
        camera_data.ortho_scale = scale
        scene.render.filepath = str(OUTPUT / (name + '.png'))
        bpy.ops.render.render(write_still=True)
        print('RENDERED ' + name, flush=True)

    render('exterior', (28, 32, 25), (0, -5, 9), 36)
    previous_visibility = {obj: obj.hide_render for obj in scene.objects}
    for obj in source_objects:
        if any(surface and surface.name.split('.')[0] in ('Velas', 'Material') for surface in obj.data.materials):
            obj.hide_render = True
    render('deck', (18, 23, 27), (0, -3, 2), 26)
    for obj in scene.objects:
        if obj.type == 'MESH':
            obj.hide_render = obj not in underdeck_objects
    for obj in structural:
        obj.hide_render = obj != lower_floor
    hull.hide_render = True
    cutaway = baked(hull, 'HullCutaway')
    clip(cutaway, (0, 0, 0), (1, 0, 0))
    for obj in source_objects:
        obj.hide_render = True
    render('underdeck', (15, 12, 14), (0, -2, 0.3), 16)
    cutaway.hide_render = True
    cutaway.hide_viewport = True
    render('floor_plan', (0, -2, 25), (0, -2, 0), 18)
    for obj, hidden in previous_visibility.items():
        obj.hide_render = hidden
    render('lookout', (9, 8, 23), (0, -0.63, 18.8), 7)
    camera.location = (28, 32, 25)
    camera.rotation_euler = (Vector((0, -5, 9)) - camera.location).to_track_quat('-Z', 'Y').to_euler()
    camera_data.ortho_scale = 36
    assert hashlib.sha256(source.read_bytes()).hexdigest() == source_digest
    report = {'sourceSha256': source_digest, 'sourceUnchanged': True, 'status': 'concept, not game tested',
              'headroom': headroom, 'roomArea': room['length'] * room['width'], 'hullWidthsAtFloor': widths,
              'wallTiers': tiers, 'vanillaFurnitureBounds': furniture_bounds, 'upgradeCount': len(SPEC['upgrades']),
              'mastLadderOpeningsChecked': True, 'mainStairAngle': stair_angle,
              'stairRise': rise, 'stairTreadDepth': stairs['run'] / stairs['steps']}
    (OUTPUT / 'checks.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    bpy.ops.file.pack_all()
    bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT / 'sailing_ship_concept.blend'))
    print('LAYOUT COMPLETE: geometry checks passed; source unchanged; no game prefab created', flush=True)


if __name__ == '__main__':
    main()
