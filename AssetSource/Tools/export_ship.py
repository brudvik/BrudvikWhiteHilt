"""Blender: --background <concept.blend> --python export_ship.py -- <output-folder>.

Export structure in metres, pre-mirroring OBJ X for Unity. Illustrative furniture is excluded.
"""
import json
import pathlib
import sys

import bpy
import numpy
from mathutils import Vector


REPO = pathlib.Path(__file__).resolve().parents[2]
PREFIXES = ('MainDeck', 'Underdeck', 'Stair', 'Step', 'CeilingBeam', 'UpperLookout', 'Bulkhead', 'DeckRail', 'RoomLining',
            'Lantern', 'Rudder', 'LookoutPost', 'LookoutRail', 'MastLadder', 'MainMastFoot', 'MiddleLookout')
COLLIDERS = ('Collider', 'UnderdeckFloor', 'UnderdeckPost', 'CeilingBeam')
WOOD = ('Madeira', 'Concept oak', 'Rudder wood')


def unity(vector):
    return [vector.x, vector.z, -vector.y]


def brighter(image, factor, cache):
    if image.name not in cache:
        copy = image.copy()
        pixels = numpy.array(copy.pixels[:], dtype=numpy.float32).reshape(-1, 4)
        pixels[:, :3] = numpy.minimum(1.0, pixels[:, :3] * factor)
        copy.pixels.foreach_set(pixels.ravel())
        cache[image.name] = copy
    return cache[image.name]


def logo_sail(image, triangles, settings):
    """Copy of the sail texture with the White Hilt logo painted in its middle, keeping the cloth's shading."""
    width, height = settings['width'], settings['height']
    sail = image.copy()
    sail.scale(width, height)
    pixels = numpy.array(sail.pixels[:], dtype=numpy.float32).reshape(height, width, 4)
    xs = [vertex.x for coordinates, uvs, normal in triangles for vertex in coordinates]
    ys = [vertex.z for coordinates, uvs, normal in triangles for vertex in coordinates]
    logo_width = int(round(settings['diameter'] / (max(xs) - min(xs)) * width))
    logo_height = int(round(settings['diameter'] / (max(ys) - min(ys)) * height))
    logo = bpy.data.images.load(str(REPO / 'BrudvikWhiteHilt' / 'Assets' / 'Logo' / 'WhiteHiltLogo.png'))
    logo.scale(logo_width, logo_height)
    art = numpy.array(logo.pixels[:], dtype=numpy.float32).reshape(logo_height, logo_width, 4)
    left, bottom = (width - logo_width) // 2, (height - logo_height) // 2
    region = pixels[bottom:bottom + logo_height, left:left + logo_width]
    luminance = region[..., :3] @ numpy.array([0.299, 0.587, 0.114], dtype=numpy.float32)
    shade = numpy.clip(luminance / max(float(luminance.mean()), 1e-3), 0.75, 1.1)[..., None]
    alpha = art[..., 3:4] * 0.95
    region[..., :3] = region[..., :3] * (1 - alpha) + art[..., :3] * shade * alpha
    sail.pixels.foreach_set(pixels.ravel())
    return sail


def export(output):
    output = pathlib.Path(output)
    output.mkdir(parents=True, exist_ok=True)
    selected = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH' and
                ((obj.name.startswith('Object_') and not obj.hide_render) or
                 obj.name == 'ConceptHull' or obj.name.startswith(PREFIXES))]
    assert any(obj.name == 'ConceptHull' for obj in selected)
    assert sum(obj.name.startswith('StairTread') for obj in selected) == 14
    groups = {}
    dependencies = bpy.context.evaluated_depsgraph_get()
    for obj in selected:
        evaluated = obj.evaluated_get(dependencies)
        mesh = evaluated.to_mesh()
        mesh.calc_loop_triangles()
        for triangle in mesh.loop_triangles:
            surface = mesh.materials[triangle.material_index] if mesh.materials else None
            key = surface.name if surface else 'Wood'
            if key.split('.')[0] == 'Velas':
                key += ':' + obj.name
            group = groups.setdefault(key, {'surface': surface, 'triangles': []})
            coordinates = [obj.matrix_world @ mesh.vertices[vertex].co for vertex in triangle.vertices]
            uvs = [mesh.uv_layers.active.data[loop].uv[:] for loop in triangle.loops] if mesh.uv_layers.active else [(0.5, 0.5)] * 3
            normal = (obj.matrix_world.to_3x3().inverted().transposed() @ triangle.normal).normalized()
            group['triangles'].append((coordinates, uvs, normal))
        evaluated.to_mesh_clear()
    manifest = {'parts': [], 'colliders': [], 'prisms': [], 'coordinates': 'Unity metres; source glTF axes; no normalization'}
    scene = bpy.context.scene
    brightness = float(scene.get('whitehilt_brightness', 1.0))
    logo = json.loads(scene.get('whitehilt_logo', '{}'))
    if 'whitehilt_rudder' in scene:
        manifest['rudder'] = json.loads(scene['whitehilt_rudder'])
    brightened = {}
    for index, (key, group) in enumerate(sorted(groups.items())):
        name = 'sailing_ship_' + str(index)
        lines = ['g ' + name]
        vertex_index = 1
        normal_index = 1
        uv_index = 1
        sail = key.startswith('Velas')
        logo_group = sail and bool(logo) and key.endswith(':' + logo['sail'])
        pivot = [0, 0, 0]
        if sail:
            vertices = [vertex for coordinates, uvs, normal in group['triangles'] for vertex in coordinates]
            pivot = [(min(vertex.x for vertex in vertices) + max(vertex.x for vertex in vertices)) / 2,
                     max(vertex.z for vertex in vertices),
                     -(min(vertex.y for vertex in vertices) + max(vertex.y for vertex in vertices)) / 2]
        for coordinates, uvs, normal in group['triangles']:
            for vertex in coordinates:
                lines.append('v %.7f %.7f %.7f' % (-(vertex.x - pivot[0]), vertex.z - pivot[1], -vertex.y - pivot[2]))
            for uv in uvs:
                lines.append('vt %.7f %.7f' % tuple(uv))
            mirrored_uv = uv_index
            if logo_group:
                mirrored_uv = uv_index + 3
                for uv in uvs:
                    lines.append('vt %.7f %.7f' % (1.0 - uv[0], uv[1]))
            # In game u runs along +x, so the logo reads correctly from astern; faces towards the bow get mirrored u.
            primary_bow = -normal.y > 0
            # Negating x mirrors the triangle, so the winding is reversed to keep the front face outward.
            # Every part is two-sided: the hull and decks are also seen from inside the lower room.
            faces = (((-normal.x, normal.z, -normal.y), (2, 1, 0), mirrored_uv if primary_bow else uv_index),
                     ((normal.x, -normal.z, normal.y), (0, 1, 2), uv_index if primary_bow else mirrored_uv))
            for vn, order, uv_base in faces:
                for corner in range(3):
                    lines.append('vn %.7f %.7f %.7f' % vn)
                lines.append('f ' + ' '.join('%d/%d/%d' % (vertex_index + offset, uv_base + offset, normal_index + offset) for offset in order))
                normal_index += 3
            vertex_index += 3
            uv_index += 6 if logo_group else 3
        (output / (name + '.obj')).write_text('\n'.join(lines) + '\n', encoding='ascii')
        surface = group['surface']
        images = [node.image for node in surface.node_tree.nodes if node.type == 'TEX_IMAGE' and node.image] if surface and surface.use_nodes else []
        texture_name = name + '_albedo'
        if images:
            image = images[0]
            if key.split('.')[0] in WOOD and brightness != 1.0:
                image = brighter(image, brightness, brightened)
            if logo_group:
                image = logo_sail(image, group['triangles'], logo)
        else:
            colour = surface.diffuse_color[:] if surface else (0.48, 0.33, 0.18, 1)
            image = bpy.data.images.new(texture_name, width=4, height=4)
            image.pixels = list(colour) * 16
        image.filepath_raw = str(output / (texture_name + '.png'))
        image.file_format = 'PNG'
        image.save()
        manifest['parts'].append({'mesh': name, 'texture': texture_name, 'sail': sail, 'rudder': key.startswith('Rudder'),
                                  'pivot': pivot, 'triangles': len(group['triangles'])})
    for obj in bpy.context.scene.objects:
        if obj.type == 'MESH' and obj.name.startswith(COLLIDERS):
            # Boxes keep their own rotation; size is measured in the box's local axes.
            local = [vertex.co for vertex in obj.data.vertices]
            minimum = [min(vertex[axis] for vertex in local) for axis in range(3)]
            maximum = [max(vertex[axis] for vertex in local) for axis in range(3)]
            centre = obj.matrix_world @ Vector([(minimum[axis] + maximum[axis]) / 2 for axis in range(3)])
            size = [maximum[axis] - minimum[axis] for axis in range(3)]
            rotation = obj.matrix_world.to_3x3().normalized()
            manifest['colliders'].append({'name': obj.name, 'centre': unity(centre), 'size': [size[0], size[2], size[1]],
                                          'forward': unity(rotation @ Vector((0, -1, 0))),
                                          'up': unity(rotation @ Vector((0, 0, 1)))})
        if obj.name.startswith(('UpperLookoutFloor', 'MiddleLookout')):
            evaluated = obj.evaluated_get(dependencies)
            mesh = evaluated.to_mesh()
            mesh.calc_loop_triangles()
            for triangle in mesh.loop_triangles:
                if triangle.normal.z < 0.7:
                    continue
                vertices = [obj.matrix_world @ mesh.vertices[index].co for index in triangle.vertices]
                flattened = []
                for depth in (0, -0.15):
                    for vertex in vertices:
                        flattened.extend((vertex.x, vertex.z + depth, -vertex.y))
                manifest['prisms'].append({'vertices': flattened})
            evaluated.to_mesh_clear()
    assert len(manifest['parts']) >= 4
    assert len(manifest['colliders']) >= 19
    assert len(manifest['prisms']) > 10
    (output / 'sailing_ship.assets.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
    print('SHIP EXPORT COMPLETE: %d parts, %d structural colliders, %d triangles' %
          (len(manifest['parts']), len(manifest['colliders']), sum(part['triangles'] for part in manifest['parts'])), flush=True)


if __name__ == '__main__':
    export(sys.argv[sys.argv.index('--') + 1])