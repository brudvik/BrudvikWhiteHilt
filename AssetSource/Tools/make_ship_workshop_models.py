"""Blender: --background --python make_ship_workshop_models.py.

Creates compact ship workshop frames and a wall drawer. Forge's existing anvil is added at runtime.
"""
import math
import pathlib

import bpy
from mathutils import Vector


OUTPUT = pathlib.Path(__file__).resolve().parents[1] / 'Models'


def material(name, colour):
    surface = bpy.data.materials.new(name)
    surface.diffuse_color = (*colour, 1)
    surface.use_nodes = True
    shader = surface.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value = (*colour, 1)
    shader.inputs['Roughness'].default_value = 0.8
    return surface


def box(name, centre, size, surface, bevel=0.012):
    bpy.ops.mesh.primitive_cube_add(size=1, location=centre)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(surface)
    if bevel:
        modifier = obj.modifiers.new('Worn edges', 'BEVEL')
        modifier.width = bevel
        modifier.segments = 1
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    return obj


def rod(name, start, end, radius, surface):
    direction = Vector(end) - Vector(start)
    bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=radius, depth=direction.length,
                                      location=(Vector(start) + Vector(end)) / 2)
    obj = bpy.context.object
    obj.name = name
    obj.rotation_euler = direction.to_track_quat('Z', 'Y').to_euler()
    obj.data.materials.append(surface)
    return obj


def frame(wood, iron, pale):
    for sideways in (-0.46, 0.46):
        for depth in (-0.22, 0.22):
            box('Trestle leg', (sideways, depth, 0.39), (0.09, 0.09, 0.78), wood)
        box('Trestle foot', (sideways, 0, 0.055), (0.15, 0.62, 0.11), wood)
        box('Iron strap', (sideways, -0.265, 0.62), (0.12, 0.022, 0.1), iron, 0.003)
    for depth in (-0.22, 0.22):
        box('Cross member', (0, depth, 0.31), (0.98, 0.07, 0.075), wood)
    for plank in range(4):
        box('Worktop plank', (0, -0.225 + plank * 0.15, 0.81), (1.1, 0.145, 0.075), pale, 0.006)
    box('Lower shelf', (0, 0, 0.36), (0.82, 0.44, 0.04), wood)
    for sideways in (-0.48, 0.48):
        for depth in (-0.21, 0.21):
            box('Top nail', (sideways, depth, 0.852), (0.026, 0.026, 0.009), iron, 0.002)


def mallet(wood, iron):
    rod('Mallet handle', (0.12, -0.2, 0.875), (0.43, -0.08, 0.875), 0.02, wood)
    head = box('Mallet head', (0.43, -0.08, 0.9), (0.07, 0.14, 0.07), iron)
    head.rotation_euler.z = -0.4


def export(name):
    bpy.context.view_layer.update()
    objects = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
    points = [obj.matrix_world @ vertex.co for obj in objects for vertex in obj.data.vertices]
    dimensions = [max(point[axis] for point in points) - min(point[axis] for point in points) for axis in range(3)]
    faces = sum(len(obj.data.polygons) for obj in objects)
    assert dimensions[0] <= 1.15 and dimensions[1] <= 0.7 and faces < 1500, (name, dimensions, faces)
    OUTPUT.mkdir(parents=True, exist_ok=True)
    bpy.ops.export_scene.gltf(filepath=str(OUTPUT / (name + '.glb')), export_format='GLB',
                              export_yup=True, export_materials='EXPORT')
    print('WORKSHOP MODEL %s: %s metres, %d faces' % (name, dimensions, faces), flush=True)


def main():
    for name in ('shipworkbench', 'shipforge', 'shipstonecutter', 'walldrawer'):
        bpy.ops.wm.read_factory_settings(use_empty=True)
        wood = material('Dark oak', (0.24, 0.17, 0.10))
        pale = material('White Hilt worktop', (0.73, 0.72, 0.63))
        iron = material('Forged iron', (0.16, 0.20, 0.23))
        stone = material('Slate', (0.42, 0.47, 0.46))
        if name == 'walldrawer':
            box('Drawer shell', (0, 0, 0.15), (0.7, 0.34, 0.3), wood)
            box('Drawer front', (0, -0.165, 0.15), (0.7, 0.035, 0.3), pale)
            for sideways in (-0.3, 0.3):
                box('Corner strap', (sideways, -0.19, 0.15), (0.045, 0.015, 0.26), iron, 0.002)
            for sideways in (-0.055, 0.055):
                rod('Handle mount', (sideways, -0.185, 0.05), (sideways, -0.225, 0.05), 0.009, iron)
            rod('Drawer handle', (-0.055, -0.225, 0.05), (0.055, -0.225, 0.05), 0.011, iron)
        else:
            frame(wood, iron, pale)
            if name == 'shipworkbench':
                mallet(wood, iron)
                box('Saw blade', (-0.17, 0.03, 0.87), (0.44, 0.1, 0.02), iron, 0.002)
                box('Saw grip', (-0.41, 0.03, 0.9), (0.09, 0.14, 0.055), wood)
                for tooth in range(14):
                    box('Saw tooth', (-0.37 + tooth * 0.031, -0.028, 0.87), (0.018, 0.025, 0.022), iron, 0)
                box('Planed board', (0.09, 0.2, 0.88), (0.5, 0.09, 0.045), wood)
            elif name == 'shipforge':
                mallet(wood, iron)
                for sideways in (-0.03, 0.04):
                    rod('Tongs', (sideways + 0.28, 0.2, 0.87), (sideways + 0.02, 0.1, 0.87), 0.008, iron)
                box('Metal billet', (0.34, 0.07, 0.89), (0.2, 0.075, 0.06), iron)
            else:
                mallet(wood, wood)
                block = box('Working stone', (-0.24, 0.04, 0.925), (0.4, 0.3, 0.15), stone, 0.025)
                block.rotation_euler.z = 0.12
                for depth in (-0.01, 0.09):
                    rod('Chisel', (0.03, depth, 0.874), (0.27, depth, 0.874), 0.01, iron)
                for chip in range(4):
                    box('Stone chip', (-0.4 + chip * 0.11, -0.18, 0.87), (0.045, 0.035, 0.03), stone, 0.008)
        export(name)


if __name__ == '__main__':
    main()