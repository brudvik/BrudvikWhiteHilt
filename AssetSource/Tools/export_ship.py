"""Blender: --background <concept.blend> --python export_ship.py -- <output-folder>.

Export structure in metres, pre-mirroring OBJ X for Unity. Illustrative furniture is excluded.
"""
import json
import pathlib
import sys

import bpy


PREFIXES = ('MainDeck', 'Underdeck', 'Stair', 'CeilingBeam', 'UpperLookout',
            'LookoutPost', 'LookoutRail', 'MastLadder', 'MainMastFoot', 'MiddleLookout')


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
    for index, (key, group) in enumerate(sorted(groups.items())):
        name = 'sailing_ship_' + str(index)
        lines = ['g ' + name]
        vertex_index = 1
        normal_index = 1
        sail = key.startswith('Velas')
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
            for corner in range(3):
                lines.append('vn %.7f %.7f %.7f' % (-normal.x, normal.z, -normal.y))
            lines.append('f ' + ' '.join('%d/%d/%d' % (vertex_index + offset, vertex_index + offset, normal_index + offset) for offset in range(3)))
            normal_index += 3
            if sail or key.split('.')[0] == 'Material':
                for corner in range(3):
                    lines.append('vn %.7f %.7f %.7f' % (normal.x, -normal.z, normal.y))
                lines.append('f ' + ' '.join('%d/%d/%d' % (vertex_index + offset, vertex_index + offset, normal_index + offset) for offset in (2, 1, 0)))
                normal_index += 3
            vertex_index += 3
        (output / (name + '.obj')).write_text('\n'.join(lines) + '\n', encoding='ascii')
        surface = group['surface']
        images = [node.image for node in surface.node_tree.nodes if node.type == 'TEX_IMAGE' and node.image] if surface and surface.use_nodes else []
        texture_name = name + '_albedo'
        if images:
            image = images[0]
        else:
            colour = surface.diffuse_color[:] if surface else (0.48, 0.33, 0.18, 1)
            image = bpy.data.images.new(texture_name, width=4, height=4)
            image.pixels = list(colour) * 16
        image.filepath_raw = str(output / (texture_name + '.png'))
        image.file_format = 'PNG'
        image.save()
        manifest['parts'].append({'mesh': name, 'texture': texture_name, 'sail': sail, 'pivot': pivot,
                                  'triangles': len(group['triangles'])})
    for obj in selected:
        if obj.name.startswith(('MainDeck', 'UnderdeckFloor', 'StairTread', 'UnderdeckPost', 'CeilingBeam')):
            vertices = [obj.matrix_world @ vertex.co for vertex in obj.data.vertices]
            minimum = [min(vertex[axis] for vertex in vertices) for axis in range(3)]
            maximum = [max(vertex[axis] for vertex in vertices) for axis in range(3)]
            centre = [(minimum[axis] + maximum[axis]) / 2 for axis in range(3)]
            size = [maximum[axis] - minimum[axis] for axis in range(3)]
            manifest['colliders'].append({'name': obj.name, 'centre': [centre[0], centre[2], -centre[1]],
                                          'size': [size[0], size[2], size[1]]})
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