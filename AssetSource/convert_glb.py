"""Converts a glTF binary (.glb) into an OBJ + texture pair that Unity imports without extra packages.

The mesh is baked into world space, placed with its base at the origin, scaled to a height of 1
and made double-sided, since the Valheim shaders cull back faces.

All parts are merged into one mesh. Parts that share one texture keep it as-is. Parts with different textures,
or with only a base colour, are packed side by side into one PNG atlas and their UVs are moved to match.
Tiling UVs (outside 0..1) are wrapped per triangle into the first repeat, and the few repeats a triangle still spans
are baked into its atlas tile.
A model with one texture and an emission map also gets <name>_emission.

A <name>.crop.json next to the .glb ({"min": [x, y, z], "max": [x, y, z]}, in the scaled space described above) keeps
only the triangles whose centre lies in that box, for files that hold several objects in one mesh; the result is
scaled and placed again.

Usage: python convert_glb.py <input.glb> <output_dir> <name>
"""
import io
import json
import math
import pathlib
import struct
import sys

COMPONENTS = {5121: ("B", 1), 5123: ("H", 2), 5125: ("I", 4), 5126: ("f", 4)}
TYPE_SIZES = {"SCALAR": 1, "VEC2": 2, "VEC3": 3, "VEC4": 4}
CHUNK_JSON = 0x4E4F534A
CHUNK_BIN = 0x004E4942
TILE_SIZE = 1024
COLOR_TILE_SIZE = 64


def read_glb(path):
    data = path.read_bytes()
    magic, version, length = struct.unpack_from("<4sII", data, 0)
    if magic != b"glTF" or version != 2:
        raise ValueError(f"{path} is not a glTF 2.0 binary")

    gltf, binary, offset = None, None, 12
    while offset < length:
        chunk_length, chunk_type = struct.unpack_from("<II", data, offset)
        chunk = data[offset + 8 : offset + 8 + chunk_length]
        if chunk_type == CHUNK_JSON:
            gltf = json.loads(chunk)
        elif chunk_type == CHUNK_BIN:
            binary = chunk
        offset += 8 + chunk_length
    return gltf, binary


def read_accessor(gltf, binary, index):
    accessor = gltf["accessors"][index]
    view = gltf["bufferViews"][accessor["bufferView"]]
    fmt, size = COMPONENTS[accessor["componentType"]]
    count = TYPE_SIZES[accessor["type"]]
    stride = view.get("byteStride", size * count)
    start = view.get("byteOffset", 0) + accessor.get("byteOffset", 0)
    return [struct.unpack_from("<" + fmt * count, binary, start + i * stride) for i in range(accessor["count"])]


def multiply(a, b):
    return [[sum(a[r][k] * b[k][c] for k in range(4)) for c in range(4)] for r in range(4)]


def node_matrix(node):
    if "matrix" in node:
        m = node["matrix"]
        return [[m[c * 4 + r] for c in range(4)] for r in range(4)]

    tx, ty, tz = node.get("translation", (0, 0, 0))
    qx, qy, qz, qw = node.get("rotation", (0, 0, 0, 1))
    sx, sy, sz = node.get("scale", (1, 1, 1))
    rotation = [
        [1 - 2 * (qy * qy + qz * qz), 2 * (qx * qy - qz * qw), 2 * (qx * qz + qy * qw)],
        [2 * (qx * qy + qz * qw), 1 - 2 * (qx * qx + qz * qz), 2 * (qy * qz - qx * qw)],
        [2 * (qx * qz - qy * qw), 2 * (qy * qz + qx * qw), 1 - 2 * (qx * qx + qy * qy)],
    ]
    scale = (sx, sy, sz)
    matrix = [[rotation[r][c] * scale[c] for c in range(3)] + [t] for r, t in zip(range(3), (tx, ty, tz))]
    return matrix + [[0, 0, 0, 1]]


def world_matrix(gltf, node_index):
    parents = {child: i for i, node in enumerate(gltf["nodes"]) for child in node.get("children", [])}
    matrix = node_matrix(gltf["nodes"][node_index])
    while node_index in parents:
        node_index = parents[node_index]
        matrix = multiply(node_matrix(gltf["nodes"][node_index]), matrix)
    return matrix


def transform(matrix, vector, w):
    return [sum(matrix[r][c] * (vector[c] if c < 3 else w) for c in range(4)) for r in range(3)]


def normalize(vector):
    length = math.sqrt(sum(v * v for v in vector)) or 1.0
    return [v / length for v in vector]


def texture_source(gltf, primitive):
    """Returns ("image", index, colour) for a textured material, or ("colour", None, colour) for a plain one."""
    material = gltf["materials"][primitive["material"]] if "material" in primitive else {}
    pbr = material.get("pbrMetallicRoughness", {})
    gloss = material.get("extensions", {}).get("KHR_materials_pbrSpecularGlossiness", {})
    colour = tuple(pbr.get("baseColorFactor") or gloss.get("diffuseFactor") or (1.0, 1.0, 1.0, 1.0))
    texture_info = pbr.get("baseColorTexture") or gloss.get("diffuseTexture")
    if texture_info is None:
        return ("colour", None, colour)
    return ("image", gltf["textures"][texture_info["index"]]["source"], colour)


def emissive_source(gltf, primitive):
    """Returns the image index of the material's emission map, or None."""
    material = gltf["materials"][primitive["material"]] if "material" in primitive else {}
    texture_info = material.get("emissiveTexture")
    return None if texture_info is None else gltf["textures"][texture_info["index"]]["source"]


def image_bytes(gltf, binary, index):
    image = gltf["images"][index]
    view = gltf["bufferViews"][image["bufferView"]]
    start = view.get("byteOffset", 0)
    return image, binary[start : start + view["byteLength"]]


def build_atlas(gltf, binary, sources, repeats):
    """Packs one tile per source side by side, each repeated as often as its UVs need. Returns the PNG bytes."""
    from PIL import Image

    tiles = []
    for kind, index, colour in sources:
        rgba = tuple(round(max(0.0, min(1.0, c)) * 255) for c in colour)
        if kind == "colour":
            tiles.append(Image.new("RGBA", (COLOR_TILE_SIZE, COLOR_TILE_SIZE), rgba))
            continue
        tile = Image.open(io.BytesIO(image_bytes(gltf, binary, index)[1])).convert("RGBA")
        tile = tile.resize((min(tile.width, TILE_SIZE), min(tile.height, TILE_SIZE)))
        if rgba != (255, 255, 255, 255):
            tile = Image.merge("RGBA", [band.point(lambda v, f=f: v * f // 255) for band, f in zip(tile.split(), rgba)])
        across, down = repeats.get((kind, index, colour), (1, 1))
        if (across, down) != (1, 1):
            repeated = Image.new("RGBA", (tile.width * across, tile.height * down))
            for x in range(across):
                for y in range(down):
                    repeated.paste(tile, (x * tile.width, y * tile.height))
            tile = repeated.resize((TILE_SIZE, TILE_SIZE))
        tiles.append(tile)

    size = max(max(tile.width, tile.height) for tile in tiles)
    atlas = Image.new("RGBA", (size * len(tiles), size))
    for column, tile in enumerate(tiles):
        atlas.paste(tile.resize((size, size)), (column * size, 0))
    buffer = io.BytesIO()
    atlas.save(buffer, "PNG")
    return buffer.getvalue()


def crop(positions, normals, corners, indices, box):
    """Keeps the triangles whose centre lies inside box["min"]..box["max"] and drops the vertices nobody uses."""
    low, high = box["min"], box["max"]
    kept, kept_corners = [], []
    for i in range(0, len(indices), 3):
        triangle = indices[i:i + 3]
        centre = [sum(positions[v][axis] for v in triangle) / 3 for axis in range(3)]
        if all(low[axis] <= centre[axis] <= high[axis] for axis in range(3)):
            kept += triangle
            kept_corners += corners[i:i + 3]
    if not kept:
        raise ValueError("The crop box keeps no triangles")
    remap = {}
    for v in kept:
        remap.setdefault(v, len(remap))
    order = sorted(remap, key=remap.get)
    return ([positions[v] for v in order], [normals[v] for v in order], kept_corners, [remap[v] for v in kept])


def wrap_triangles(uvs, triangle_indices):
    """Shifts each triangle's UVs by whole repeats so they start in the first one. Returns the corner UVs and how many
    repeats across and down the widest triangle still spans."""
    corners, across, down = [], 1, 1
    for i in range(0, len(triangle_indices), 3):
        triangle = [uvs[v] for v in triangle_indices[i:i + 3]]
        shift_u = math.floor(min(u for u, _ in triangle) + 1e-4)
        shift_v = math.floor(min(v for _, v in triangle) + 1e-4)
        shifted = [(u - shift_u, v - shift_v) for u, v in triangle]
        across = max(across, math.ceil(max(u for u, _ in shifted) - 1e-4))
        down = max(down, math.ceil(max(v for _, v in shifted) - 1e-4))
        corners += shifted
    return corners, across, down


def main(input_path, output_dir, name):
    gltf, binary = read_glb(input_path)
    parts = [
        (node_index, primitive)
        for node_index, node in enumerate(gltf["nodes"])
        if "mesh" in node
        for primitive in gltf["meshes"][node["mesh"]]["primitives"]
    ]
    if not parts:
        raise ValueError("The model has no mesh parts")

    part_sources = [texture_source(gltf, primitive) for _, primitive in parts]
    sources = list(dict.fromkeys(part_sources))
    # One plain texture is copied as-is, anything else goes into an atlas.
    use_atlas = len(sources) > 1 or sources[0][0] == "colour" or tuple(sources[0][2]) != (1.0, 1.0, 1.0, 1.0)

    positions, normals, corners, indices = [], [], [], []
    corner_sources, repeats = [], {}
    for (node_index, primitive), source in zip(parts, part_sources):
        matrix = world_matrix(gltf, node_index)
        offset = len(positions)
        attributes = primitive["attributes"]
        part_positions = read_accessor(gltf, binary, attributes["POSITION"])
        positions += [transform(matrix, p, 1.0) for p in part_positions]
        normals += [normalize(transform(matrix, n, 0.0)) for n in read_accessor(gltf, binary, attributes["NORMAL"])]
        part_uvs = read_accessor(gltf, binary, attributes["TEXCOORD_0"]) if "TEXCOORD_0" in attributes and source[0] == "image" else [(0.5, 0.5)] * len(part_positions)
        part_indices = [i[0] for i in read_accessor(gltf, binary, primitive["indices"])]
        if use_atlas:
            part_corners, across, down = wrap_triangles(part_uvs, part_indices)
            known = repeats.get(source, (1, 1))
            repeats[source] = (max(known[0], across), max(known[1], down))
        else:
            part_corners = [part_uvs[i] for i in part_indices]
        corners += part_corners
        corner_sources += [source] * len(part_indices)
        indices += [offset + i for i in part_indices]

    if use_atlas:
        columns = len(sources)
        uvs = []
        for (u, v), source in zip(corners, corner_sources):
            across, down = repeats[source]
            u = min(max(u / across, 0.0), 1.0)
            v = min(max(v / down, 0.0), 1.0)
            uvs.append(((u + sources.index(source)) / columns, v))
        corners = uvs

    minimum = [min(p[axis] for p in positions) for axis in range(3)]
    maximum = [max(p[axis] for p in positions) for axis in range(3)]
    height = maximum[1] - minimum[1]
    base = [(minimum[0] + maximum[0]) / 2, minimum[1], (minimum[2] + maximum[2]) / 2]
    positions = [[(p[axis] - base[axis]) / height for axis in range(3)] for p in positions]

    crop_file = input_path.with_suffix(".crop.json")
    if crop_file.exists():
        positions, normals, corners, indices = crop(positions, normals, corners, indices, json.loads(crop_file.read_text()))
        minimum = [min(p[axis] for p in positions) for axis in range(3)]
        maximum = [max(p[axis] for p in positions) for axis in range(3)]
        cropped_height = maximum[1] - minimum[1]
        base = [(minimum[0] + maximum[0]) / 2, minimum[1], (minimum[2] + maximum[2]) / 2]
        positions = [[(p[axis] - base[axis]) / cropped_height for axis in range(3)] for p in positions]
        height *= cropped_height

    output_dir.mkdir(parents=True, exist_ok=True)
    vertex_count = len(positions)
    # Unity names the mesh after the group, not the object.
    lines = [f"o {name}", f"g {name}"]
    lines += [f"v {x:.6f} {y:.6f} {z:.6f}" for x, y, z in positions]
    # glTF puts the UV origin top-left, OBJ bottom-left. UVs are per corner, so a vertex may carry several.
    lines += [f"vt {u:.6f} {1.0 - v:.6f}" for u, v in corners]
    lines += [f"vn {x:.6f} {y:.6f} {z:.6f}" for x, y, z in normals]
    lines += [f"vn {-x:.6f} {-y:.6f} {-z:.6f}" for x, y, z in normals]
    for i in range(0, len(indices), 3):
        a, b, c = (index + 1 for index in indices[i : i + 3])
        ta, tb, tc = i + 1, i + 2, i + 3
        lines.append(f"f {a}/{ta}/{a} {b}/{tb}/{b} {c}/{tc}/{c}")
        lines.append(f"f {a}/{ta}/{a + vertex_count} {c}/{tc}/{c + vertex_count} {b}/{tb}/{b + vertex_count}")
    (output_dir / f"{name}.obj").write_text("\n".join(lines) + "\n", encoding="ascii")

    if use_atlas:
        (output_dir / f"{name}_albedo.png").write_bytes(build_atlas(gltf, binary, sources, repeats))
    else:
        image, data = image_bytes(gltf, binary, sources[0][1])
        extension = ".jpg" if image.get("mimeType") == "image/jpeg" else ".png"
        (output_dir / f"{name}_albedo{extension}").write_bytes(data)

    emissive = {emissive_source(gltf, primitive) for _, primitive in parts}
    has_emission = not use_atlas and len(emissive) == 1 and None not in emissive
    if has_emission:
        image, data = image_bytes(gltf, binary, emissive.pop())
        extension = ".jpg" if image.get("mimeType") == "image/jpeg" else ".png"
        (output_dir / f"{name}_emission{extension}").write_bytes(data)

    print(f"{name}: {len(parts)} parts, {len(sources)} textures{' (atlas)' if use_atlas else ''}{' + emission' if has_emission else ''}, "
          f"{vertex_count} vertices, {len(indices) // 3 * 2} triangles (double-sided), original height {height:.4f}")


if __name__ == "__main__":
    if len(sys.argv) != 4:
        sys.exit(__doc__)
    main(pathlib.Path(sys.argv[1]), pathlib.Path(sys.argv[2]), sys.argv[3])
