"""Render a separate ship layout concept with Blender, without changing the source GLB or game assets.

Run Blender with --background --python AssetSource/Preview/render_ship_layout.py -- <source.glb>.
Requires the existing ignored Preview/vanilla JSON meshes. Outputs PNGs, a packed .blend and checks.json
in BrudvikWhiteHiltUnity/Preview/out/sailing_ship_layout. Coordinates in the layout JSON are source glTF.
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


def main():
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
    hull_vertices = [vertex.co.copy() for vertex in hull.data.vertices]
    tree = BVHTree.FromPolygons(hull_vertices, [tuple(face.vertices) for face in hull.data.polygons])
    room, stairs, lookout = SPEC['room'], SPEC['stairs'], SPEC['lookout']
    start_z = room['centreZ'] - room['length'] / 2
    end_z = room['centreZ'] + room['length'] / 2
    widths = []
    for length in range(-3, 8):
        origin = point((0, room['floorTop'], length))
        right_hit = tree.ray_cast(origin, Vector((1, 0, 0)), 10)[0]
        left_hit = tree.ray_cast(origin, Vector((-1, 0, 0)), 10)[0]
        assert right_hit is not None and left_hit is not None, length
        width = right_hit.x - left_hit.x
        assert width >= room['width'] + 0.2, (length, width)
        widths.append(round(width, 3))
    headroom = room['mainDeckTop'] - room['mainDeckThickness'] - room['floorTop']
    assert headroom >= 2.5, headroom
    assert len(SPEC['upgrades']) == 9

    geometry = bmesh.new()
    geometry.from_mesh(hull.data)
    geometry.normal_update()
    deck_faces = [face for face in geometry.faces if start_z <= -face.calc_center_median().y <= end_z
                  and 1.6 < face.calc_center_median().z < 2.6 and abs(face.normal.z) > 0.7]
    assert deck_faces
    bmesh.ops.delete(geometry, geom=deck_faces, context='FACES')
    geometry.to_mesh(hull.data)
    geometry.free()
    for obj in source_objects:
        obj.hide_render = obj.name in ('Object_4', 'Object_50')
        obj.hide_viewport = obj.hide_render
    oak = material('Concept oak', (0.48, 0.33, 0.18))
    gold = material('Reference crew', (0.8, 0.48, 0.12))
    brass = material('Rail fittings', (0.62, 0.45, 0.16))
    cloth = material('White Hilt canvas', (0.82, 0.84, 0.8))
    teal = material('Deck portal placeholder', (0.12, 0.58, 0.54))
    iron = material('Iron fittings', (0.16, 0.19, 0.19))
    structural = []
    lower_floor = box('UnderdeckFloor', (0, room['floorTop'] - room['floorThickness'] / 2, room['centreZ']),
                      (room['width'], room['floorThickness'], room['length']), oak)
    structural.append(lower_floor)
    hole_min_x = stairs['centreX'] - stairs['width'] / 2
    hole_max_x = stairs['centreX'] + stairs['width'] / 2
    hole_min_z, hole_max_z = stairs['bottomZ'], stairs['bottomZ'] + stairs['run']
    rectangles = [(-room['mainDeckWidth'] / 2, hole_min_x, start_z, end_z),
                  (hole_max_x, room['mainDeckWidth'] / 2, start_z, end_z),
                  (hole_min_x, hole_max_x, start_z, hole_min_z),
                  (hole_min_x, hole_max_x, hole_max_z, end_z)]
    for index, (min_x, max_x, min_z, max_z) in enumerate(rectangles):
        structural.append(box('MainDeck' + str(index), ((min_x + max_x) / 2,
                              room['mainDeckTop'] - room['mainDeckThickness'] / 2, (min_z + max_z) / 2),
                              (max_x - min_x, room['mainDeckThickness'], max_z - min_z), oak))
    for index in range(stairs['steps']):
        top = room['floorTop'] + (room['mainDeckTop'] - room['floorTop']) * (index + 1) / stairs['steps']
        length = stairs['bottomZ'] + stairs['run'] * (index + 0.5) / stairs['steps']
        box('StairTread' + str(index), (stairs['centreX'], top - 0.06, length),
            (stairs['width'], 0.12, stairs['run'] / stairs['steps']), oak)
    for side in (-1, 1):
        rail_x = stairs['centreX'] + side * stairs['width'] / 2
        beam('StairStringer', (rail_x, room['floorTop'], hole_min_z),
             (rail_x, room['mainDeckTop'], hole_max_z), 0.12, oak)
        beam('StairHandrail', (rail_x, room['floorTop'] + stairs['handrailHeight'], hole_min_z),
             (rail_x, room['mainDeckTop'] + stairs['handrailHeight'], hole_max_z), 0.07, brass)
    for length in (-3, 2, 7):
        for sideways in (-2.5, 2.5):
            box('UnderdeckPost', (sideways, (room['floorTop'] + room['mainDeckTop'] - 0.2) / 2, length),
                (0.12, headroom, 0.12), oak)
        structural.append(box('CeilingBeam', (0, room['mainDeckTop'] - 0.26, length), (5.2, 0.12, 0.12), oak))

    furniture_bounds = []
    for entry in SPEC['furniture']:
        if '--export' in arguments:
            continue
        if 'mesh' in entry:
            parent, vertices = imported_piece(entry['mesh'], entry['name'], entry['position'], entry.get('yaw', 0))
            transformed = [parent.matrix_world @ vertex for vertex in vertices]
            bpy.context.view_layer.update()
            transformed = [parent.matrix_world @ vertex for vertex in vertices]
            bounds = {axis: [min(vertex[axis] for vertex in transformed), max(vertex[axis] for vertex in transformed)]
                      for axis in range(3)}
            assert bounds[2][1] < room['mainDeckTop'] - room['mainDeckThickness'], (entry['name'], bounds)
            assert max(abs(bounds[0][0]), abs(bounds[0][1])) <= room['width'] / 2, (entry['name'], bounds)
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
    cylinder('MainMastFoot', (0, (room['floorTop'] + room['mainDeckTop']) / 2, lookout['mastZ']),
             0.18, room['mainDeckTop'] - room['floorTop'], oak)
    underdeck_objects = {obj for obj in scene.objects if obj.type == 'MESH' and obj not in source_objects
                         and obj != hull and min((obj.matrix_world @ vertex.co).z for vertex in obj.data.vertices)
                         < room['mainDeckTop']}

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
        beam('MastLadderRail', (sideways, room['mainDeckTop'], lookout['mastZ']),
             (sideways, lookout['floorTop'] + 0.8, lookout['mastZ']), 0.06, oak)
    height = room['mainDeckTop'] + lookout['rungSpacing']
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
                for length in (-1.1, 1.1):
                    beam('TentPole', (position[0] + sideways, position[1], position[2] + length),
                         (position[0] + sideways, position[1] + 2.1, position[2] + length), 0.06, oak)
            box('TentCanvas', (position[0], position[1] + 2.15, position[2]), (1.8, 0.06, 2.4), cloth)
        elif identifier == 'ship_portal':
            cylinder(identifier, (position[0], position[1] + 0.04, position[2]), 0.6, 0.08, teal, 32)
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

    if '--export' in arguments:
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
              'vanillaFurnitureBounds': furniture_bounds, 'upgradeCount': len(SPEC['upgrades']),
              'mastLadderOpeningsChecked': True,
              'stairRise': (room['mainDeckTop'] - room['floorTop']) / stairs['steps'],
              'stairTreadDepth': stairs['run'] / stairs['steps']}
    (OUTPUT / 'checks.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    bpy.ops.file.pack_all()
    bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT / 'sailing_ship_concept.blend'))
    print('LAYOUT COMPLETE: geometry checks passed; source unchanged; no game prefab created', flush=True)


if __name__ == '__main__':
    main()