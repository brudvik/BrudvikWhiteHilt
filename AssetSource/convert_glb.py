"""Converts a single-mesh glTF binary (.glb) into an OBJ + PNG pair that Unity imports without extra packages.

The mesh is baked into world space, placed with its base at the origin, scaled to a height of 1
and made double-sided, since the Valheim shaders cull back faces.

Usage: python convert_glb.py <input.glb> <output_dir> <name>
"""
import json
import math
import pathlib
import struct
import sys

COMPONENTS = {5121: ("B", 1), 5123: ("H", 2), 5125: ("I", 4), 5126: ("f", 4)}
TYPE_SIZES = {"SCALAR": 1, "VEC2": 2, "VEC3": 3, "VEC4": 4}
CHUNK_JSON = 0x4E4F534A
CHUNK_BIN = 0x004E4942


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


def texture_image(gltf, primitive):
    material = gltf["materials"][primitive["material"]]
    texture_info = material.get("pbrMetallicRoughness", {}).get("baseColorTexture") or material.get("extensions", {}).get(
        "KHR_materials_pbrSpecularGlossiness", {}
    ).get("diffuseTexture")
    if texture_info is None:
        raise ValueError("Material has no base colour or diffuse texture")
    return gltf["textures"][texture_info["index"]]["source"]


def main(input_path, output_dir, name):
    gltf, binary = read_glb(input_path)
    parts = [
        (node_index, primitive)
        for node_index, node in enumerate(gltf["nodes"])
        if "mesh" in node
        for primitive in gltf["meshes"][node["mesh"]]["primitives"]
    ]
    images = {texture_image(gltf, primitive) for _, primitive in parts}
    if not parts or len(images) != 1:
        raise ValueError(f"Expected mesh parts that share one texture, found {len(images)} textures")

    # All parts share one texture, so they can be merged into one mesh.
    positions, normals, uvs, indices = [], [], [], []
    for node_index, primitive in parts:
        matrix = world_matrix(gltf, node_index)
        offset = len(positions)
        positions += [transform(matrix, p, 1.0) for p in read_accessor(gltf, binary, primitive["attributes"]["POSITION"])]
        normals += [normalize(transform(matrix, n, 0.0)) for n in read_accessor(gltf, binary, primitive["attributes"]["NORMAL"])]
        uvs += read_accessor(gltf, binary, primitive["attributes"]["TEXCOORD_0"])
        indices += [offset + i[0] for i in read_accessor(gltf, binary, primitive["indices"])]

    minimum = [min(p[axis] for p in positions) for axis in range(3)]
    maximum = [max(p[axis] for p in positions) for axis in range(3)]
    height = maximum[1] - minimum[1]
    base = [(minimum[0] + maximum[0]) / 2, minimum[1], (minimum[2] + maximum[2]) / 2]
    positions = [[(p[axis] - base[axis]) / height for axis in range(3)] for p in positions]

    output_dir.mkdir(parents=True, exist_ok=True)
    vertex_count = len(positions)
    # Unity names the mesh after the group, not the object.
    lines = [f"o {name}", f"g {name}"]
    lines += [f"v {x:.6f} {y:.6f} {z:.6f}" for x, y, z in positions]
    # glTF puts the UV origin top-left, OBJ bottom-left.
    lines += [f"vt {u:.6f} {1.0 - v:.6f}" for u, v in uvs]
    lines += [f"vn {x:.6f} {y:.6f} {z:.6f}" for x, y, z in normals]
    lines += [f"vn {-x:.6f} {-y:.6f} {-z:.6f}" for x, y, z in normals]
    for i in range(0, len(indices), 3):
        a, b, c = (index + 1 for index in indices[i : i + 3])
        lines.append(f"f {a}/{a}/{a} {b}/{b}/{b} {c}/{c}/{c}")
        lines.append(f"f {a}/{a}/{a + vertex_count} {c}/{c}/{c + vertex_count} {b}/{b}/{b + vertex_count}")
    (output_dir / f"{name}.obj").write_text("\n".join(lines) + "\n", encoding="ascii")

    image = gltf["images"][images.pop()]
    view = gltf["bufferViews"][image["bufferView"]]
    start = view.get("byteOffset", 0)
    extension = ".jpg" if image.get("mimeType") == "image/jpeg" else ".png"
    (output_dir / f"{name}_albedo{extension}").write_bytes(binary[start : start + view["byteLength"]])

    print(f"{name}: {len(parts)} parts, {vertex_count} vertices, {len(indices) // 3 * 2} triangles (double-sided), original height {height:.4f}")


if __name__ == "__main__":
    if len(sys.argv) != 4:
        sys.exit(__doc__)
    main(pathlib.Path(sys.argv[1]), pathlib.Path(sys.argv[2]), sys.argv[3])
