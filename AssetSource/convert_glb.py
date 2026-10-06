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

A <name>.weapon.json replaces the height-1 placement with the item's "attach" space in metres, so the mesh can sit
under a weapon's attach transform with an identity transform (the hand is the origin):
  {"axes": [<+X>, <+Y>, <+Z>],  glb directions for Unity's axes, as "x"/"-z" or [x, y, z] vectors
   "grip": [x, y, z],           glb point that becomes the origin (the hand)
   "length": 1.2, "measure": 2, extent in metres along that Unity axis (or "scale": metres per glb unit)
   "offset": [x, y, z],        optional shift in metres, applied last
   "split": [{"name": "n", "materials": ["Rope"], "pull": [x, y, z], "span": [axis, length]}]}
     optional parts written as their own OBJ (same texture) and left out of the main mesh; "pull" (glb units)
     bends the part into a V along the span axis, e.g. a crossbow string drawn back to its latch.
A <name>.paint.json recolours parts of the texture: {"paint": [{"material": "Wood"} or {"min": [...], "max": [...]}
(in the final mesh space), "colour": [r, g, b], "strength": 0.9}]}. It may also hold "glow": [{"min": [...],
"max": [...]} or {"material": ...}]: a model without an emission map of its own then gets <name>_emission, lit only on
those triangles (e.g. a blade or carved runes), so a glow colour set in the game lights just that part.

A <name>.fragments.json ({"count": 8}) also writes the model cut into about that many chunks of whole triangles as
<name>_frag0.obj, <name>_frag1.obj, ... in the same space and with the same texture. A piece breaks into them when it is
destroyed (PieceFragments.cs). Small separate parts (a nail, a handle) stay whole in one chunk.

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
    # A material made see-through by its factor (glass, a water surface) cannot blend in Valheim's piece shader, which
    # only cuts out below half alpha: the water of a tub vanished. It is drawn solid instead; a texture's own alpha
    # (leaves, cut-out edges) is kept.
    colour = colour[:3] + (1.0,)
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


def axis_vector(spec):
    if isinstance(spec, str):
        vector = [0.0, 0.0, 0.0]
        vector["xyz".index(spec[-1])] = -1.0 if spec.startswith("-") else 1.0
        return vector
    return normalize(list(spec))


def weapon_space(positions, normals, spec, scale=None):
    """Maps glb positions into a weapon's attach space in metres (Unity axes). Returns positions, normals, scale."""
    axes = [axis_vector(axis) for axis in spec["axes"]]
    for a in range(3):
        for b in range(a + 1, 3):
            if abs(sum(axes[a][k] * axes[b][k] for k in range(3))) > 1e-3:
                raise ValueError("weapon.json axes must be perpendicular")
    x, y, z = axes
    determinant = (x[0] * (y[1] * z[2] - y[2] * z[1]) - x[1] * (y[0] * z[2] - y[2] * z[0]) + x[2] * (y[0] * z[1] - y[1] * z[0]))
    # glTF is right-handed and Unity left-handed, so a mapping that does not mirror the model has determinant -1.
    if determinant > 0:
        raise ValueError("weapon.json axes mirror the model; negate one axis")
    grip = spec["grip"]
    mapped = [[sum((p[k] - grip[k]) * axis[k] for k in range(3)) for axis in axes] for p in positions]
    measure = spec.get("measure", 1)
    extent = max(p[measure] for p in mapped) - min(p[measure] for p in mapped)
    scale = scale or (spec["scale"] if "scale" in spec else spec["length"] / extent)
    offset = spec.get("offset", (0.0, 0.0, 0.0))
    positions = [[v * scale + o for v, o in zip(p, offset)] for p in mapped]
    normals = [normalize([sum(n[k] * axis[k] for k in range(3)) for axis in axes]) for n in normals]
    return positions, normals, scale


def triangle_mask(size, positions, indices, corners, corner_materials, rule, wrap):
    """A mask (PIL "L" image) of the texture under the triangles a paint or glow rule selects, and how many it selects."""
    from PIL import Image, ImageDraw, ImageFilter

    width, height = size
    mask = Image.new("L", size, 0)
    draw = ImageDraw.Draw(mask)
    selected = 0
    for i in range(0, len(indices), 3):
        if "material" in rule:
            if corner_materials[i] != rule["material"]:
                continue
        else:
            centre = [sum(positions[v][axis] for v in indices[i:i + 3]) / 3 for axis in range(3)]
            if not all(rule["min"][axis] <= centre[axis] <= rule["max"][axis] for axis in range(3)):
                continue
        triangle = corners[i:i + 3]
        if wrap:
            shift_u, shift_v = math.floor(min(u for u, _ in triangle)), math.floor(min(v for _, v in triangle))
            triangle = [(u - shift_u, v - shift_v) for u, v in triangle]
        draw.polygon([(u * width, v * height) for u, v in triangle], fill=255)
        selected += 1
    if selected == 0:
        raise ValueError(f"rule {rule} selects no triangles")
    return mask.filter(ImageFilter.MaxFilter(3)), selected


def paint(image, positions, indices, corners, corner_materials, rules, wrap):
    """Recolours the texture under the triangles each rule selects, keeping the shading of the original."""
    from PIL import Image, ImageStat

    image = image.convert("RGB")
    for rule in rules:
        mask, selected = triangle_mask(image.size, positions, indices, corners, corner_materials, rule, wrap)
        grey = image.convert("L")
        mean = max(1.0, ImageStat.Stat(grey, mask).mean[0])
        tinted = Image.merge("RGB", [
            grey.point(lambda level, c=c: min(255, round(c * 255 * (0.6 + 0.4 * level / mean))))
            for c in rule["colour"]])
        image.paste(Image.blend(image, tinted, rule.get("strength", 0.9)), mask=mask)
        print(f"  painted {selected} triangles {rule.get('material', '')}")
    buffer = io.BytesIO()
    image.save(buffer, "PNG")
    return buffer.getvalue()


def glow_map(albedo, positions, indices, corners, corner_materials, rules, wrap):
    """An emission map lit only under the triangles the glow rules select, with the albedo's shading so detail shows;
    the game multiplies it by the glow colour."""
    from PIL import Image, ImageChops

    grey = albedo.convert("L").point(lambda level: 128 + level // 2)
    lit = Image.new("L", albedo.size, 0)
    for rule in rules:
        mask, selected = triangle_mask(albedo.size, positions, indices, corners, corner_materials, rule, wrap)
        lit = ImageChops.lighter(lit, mask)
        print(f"  glow on {selected} triangles {rule.get('material', '')}")
    glow = Image.composite(grey, Image.new("L", albedo.size, 0), lit).convert("RGB")
    buffer = io.BytesIO()
    glow.save(buffer, "PNG")
    return buffer.getvalue()


def subdivide(positions, normals, corners, indices, parts):
    """Halves the longest edge of every triangle until no edge is longer than 1/parts of the model's largest side, so
    long planks made of a few thin triangles still break across, not into slivers. Returns new mesh arrays."""
    extent = max(max(p[k] for p in positions) - min(p[k] for p in positions) for k in range(3))
    limit = (extent / parts) ** 2
    positions, normals = list(positions), list(normals)
    midpoints, out_corners, out_indices = {}, [], []
    stack = [(indices[i:i + 3], corners[i:i + 3]) for i in range(0, len(indices), 3)]
    while stack:
        triangle, uvs = stack.pop()
        lengths = [sum((positions[triangle[k]][axis] - positions[triangle[(k + 1) % 3]][axis]) ** 2 for axis in range(3)) for k in range(3)]
        k = max(range(3), key=lengths.__getitem__)
        if lengths[k] <= limit:
            out_indices += triangle
            out_corners += uvs
            continue
        a, b, c = (triangle[(k + n) % 3] for n in range(3))
        ua, ub, uc = (uvs[(k + n) % 3] for n in range(3))
        key = (min(a, b), max(a, b))
        if key not in midpoints:
            midpoints[key] = len(positions)
            positions.append([(positions[a][axis] + positions[b][axis]) / 2 for axis in range(3)])
            normals.append(normalize([normals[a][axis] + normals[b][axis] for axis in range(3)]))
        m = midpoints[key]
        um = ((ua[0] + ub[0]) / 2, (ua[1] + ub[1]) / 2)
        stack.append(([a, m, c], [ua, um, uc]))
        stack.append(([m, b, c], [um, ub, uc]))
    return positions, normals, out_corners, out_indices


def fragment(positions, indices, count):
    """Groups the triangles into about `count` chunks: area-weighted k-means on the triangle centres, then every
    separate part smaller than half a chunk moves whole into the chunk that holds most of it. Returns the triangle
    lists (first corner index), the same on every run."""
    triangles = list(range(0, len(indices), 3))
    centres, areas = [], []
    for i in triangles:
        a, b, c = (positions[v] for v in indices[i:i + 3])
        centres.append([(a[k] + b[k] + c[k]) / 3 for k in range(3)])
        u, w = [b[k] - a[k] for k in range(3)], [c[k] - a[k] for k in range(3)]
        cross = [u[1] * w[2] - u[2] * w[1], u[2] * w[0] - u[0] * w[2], u[0] * w[1] - u[1] * w[0]]
        areas.append(math.sqrt(sum(x * x for x in cross)) / 2 + 1e-12)

    def distance(p, q):
        return (p[0] - q[0]) ** 2 + (p[1] - q[1]) ** 2 + (p[2] - q[2]) ** 2

    # Seeds from cutting the largest group in two along its longest side at the area median, so the chunks start
    # balanced (farthest-point seeds stuck on the corners) and do not change between builds.
    total = sum(areas)
    groups = [list(range(len(triangles)))]
    while len(groups) < min(count, len(triangles)):
        group = max(groups, key=lambda g: sum(areas[t] for t in g))
        if len(group) < 2:
            break
        extent = [max(centres[t][k] for t in group) - min(centres[t][k] for t in group) for k in range(3)]
        axis = extent.index(max(extent))
        ordered = sorted(group, key=lambda t: centres[t][axis])
        half, running, cut = sum(areas[t] for t in group) / 2, 0.0, 1
        for n, t in enumerate(ordered):
            running += areas[t]
            if running >= half:
                cut = max(1, min(len(ordered) - 1, n + 1))
                break
        groups.remove(group)
        groups += [ordered[:cut], ordered[cut:]]
    seeds = [[sum(centres[t][k] * areas[t] for t in g) / sum(areas[t] for t in g) for k in range(3)] for g in groups]

    labels = [0] * len(triangles)
    for _ in range(10):
        labels = [min(range(len(seeds)), key=lambda s: distance(c, seeds[s])) for c in centres]
        sums = [[0.0, 0.0, 0.0, 0.0] for _ in seeds]
        for c, a, label in zip(centres, areas, labels):
            for k in range(3):
                sums[label][k] += c[k] * a
            sums[label][3] += a
        seeds = [[s[k] / s[3] for k in range(3)] if s[3] > 0 else seed for s, seed in zip(sums, seeds)]

    # Separate parts share no vertex position (glTF splits vertices at UV seams, so weld by position first).
    parent = list(range(len(positions)))

    def find(v):
        while parent[v] != v:
            parent[v] = parent[parent[v]]
            v = parent[v]
        return v

    welded = {}
    for v, p in enumerate(positions):
        key = tuple(round(x, 4) for x in p)
        if key in welded:
            parent[find(v)] = find(welded[key])
        else:
            welded[key] = v
    for i in triangles:
        for corner in (1, 2):
            parent[find(indices[i + corner])] = find(indices[i])

    parts = {}
    for t, i in enumerate(triangles):
        parts.setdefault(find(indices[i]), []).append(t)
    for members in parts.values():
        if sum(areas[t] for t in members) >= total / count / 2:
            continue
        shares = {}
        for t in members:
            shares[labels[t]] = shares.get(labels[t], 0.0) + areas[t]
        owner = max(shares, key=shares.get)
        for t in members:
            labels[t] = owner

    chunks = {}
    for t, label in enumerate(labels):
        chunks.setdefault(label, []).append(triangles[t])
    return [chunks[label] for label in sorted(chunks)]


def subset(positions, normals, corners, indices, triangles):
    """Returns the mesh made of the given triangles (their first corner index), without unused vertices."""
    remap, kept_corners, kept = {}, [], []
    for i in triangles:
        for corner in range(i, i + 3):
            kept.append(remap.setdefault(indices[corner], len(remap)))
            kept_corners.append(corners[corner])
    order = sorted(remap, key=remap.get)
    return [positions[v] for v in order], [normals[v] for v in order], kept_corners, kept


def write_obj(path, name, positions, normals, corners, indices, weapon):
    """Writes a double-sided OBJ. UVs are per corner, so a vertex may carry several."""
    vertex_count = len(positions)
    # Unity names the mesh after the group, not the object.
    lines = [f"o {name}", f"g {name}"]
    # Unity negates x when it imports an OBJ; weapon space is already in Unity axes, so undo that in advance.
    mirror = -1.0 if weapon else 1.0
    lines += [f"v {x * mirror:.6f} {y:.6f} {z:.6f}" for x, y, z in positions]
    # glTF puts the UV origin top-left, OBJ bottom-left.
    lines += [f"vt {u:.6f} {1.0 - v:.6f}" for u, v in corners]
    lines += [f"vn {x * mirror:.6f} {y:.6f} {z:.6f}" for x, y, z in normals]
    lines += [f"vn {-x * mirror:.6f} {-y:.6f} {-z:.6f}" for x, y, z in normals]
    for i in range(0, len(indices), 3):
        a, b, c = (index + 1 for index in indices[i : i + 3])
        ta, tb, tc = i + 1, i + 2, i + 3
        lines.append(f"f {a}/{ta}/{a} {b}/{tb}/{b} {c}/{tc}/{c}")
        lines.append(f"f {a}/{ta}/{a + vertex_count} {c}/{tc}/{c + vertex_count} {b}/{tb}/{b + vertex_count}")
    path.write_text("\n".join(lines) + "\n", encoding="ascii")


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
    corner_sources, corner_materials, repeats = [], [], {}
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
        material_name = gltf["materials"][primitive["material"]].get("name") if "material" in primitive else None
        corner_materials += [material_name] * len(part_indices)
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

    weapon_file = input_path.with_suffix(".weapon.json")
    weapon = json.loads(weapon_file.read_text()) if weapon_file.exists() else None
    splits = weapon.get("split", []) if weapon else []
    if weapon:
        raw_positions, raw_normals = positions, normals
        positions, normals, scale = weapon_space(positions, normals, weapon)
        height = 1.0 / scale
    else:
        minimum = [min(p[axis] for p in positions) for axis in range(3)]
        maximum = [max(p[axis] for p in positions) for axis in range(3)]
        height = maximum[1] - minimum[1]
        base = [(minimum[0] + maximum[0]) / 2, minimum[1], (minimum[2] + maximum[2]) / 2]
        positions = [[(p[axis] - base[axis]) / height for axis in range(3)] for p in positions]

    crop_file = input_path.with_suffix(".crop.json")
    if not weapon and crop_file.exists():
        positions, normals, corners, indices = crop(positions, normals, corners, indices, json.loads(crop_file.read_text()))
        minimum = [min(p[axis] for p in positions) for axis in range(3)]
        maximum = [max(p[axis] for p in positions) for axis in range(3)]
        cropped_height = maximum[1] - minimum[1]
        base = [(minimum[0] + maximum[0]) / 2, minimum[1], (minimum[2] + maximum[2]) / 2]
        positions = [[(p[axis] - base[axis]) / cropped_height for axis in range(3)] for p in positions]
        height *= cropped_height

    output_dir.mkdir(parents=True, exist_ok=True)
    vertex_count = len(positions)
    split_materials = {material for split in splits for material in split["materials"]}
    main_triangles = [i for i in range(0, len(indices), 3) if corner_materials[i] not in split_materials]
    write_obj(output_dir / f"{name}.obj", name, *subset(positions, normals, corners, indices, main_triangles), bool(weapon))
    for split in splits:
        triangles = [i for i in range(0, len(indices), 3) if corner_materials[i] in split["materials"]]
        part_positions, part_normals, part_corners, part_indices = subset(raw_positions, raw_normals, corners, indices, triangles)
        if "pull" in split:
            axis, length = split["span"]
            part_positions = [[v + pull * max(0.0, 1.0 - abs(p[axis]) / length) for v, pull in zip(p, split["pull"])] for p in part_positions]
        part_positions, part_normals, _ = weapon_space(part_positions, part_normals, weapon, scale)
        write_obj(output_dir / f"{split['name']}.obj", split["name"], part_positions, part_normals, part_corners, part_indices, True)
        print(f"  split {split['name']}: {len(part_indices) // 3} triangles")

    fragments_file = input_path.with_suffix(".fragments.json")
    if not weapon and fragments_file.exists():
        mesh = subdivide(positions, normals, corners, indices, 12)
        chunks = fragment(mesh[0], mesh[3], json.loads(fragments_file.read_text())["count"])
        for n, triangles in enumerate(chunks):
            write_obj(output_dir / f"{name}_frag{n}.obj", f"{name}_frag{n}", *subset(*mesh, triangles), False)
        print(f"  fragments: {len(chunks)} chunks of {', '.join(str(len(triangles)) for triangles in chunks)} triangles")

    paint_file = input_path.with_suffix(".paint.json")
    if use_atlas:
        data, extension = build_atlas(gltf, binary, sources, repeats), ".png"
    else:
        image, data = image_bytes(gltf, binary, sources[0][1])
        extension = ".jpg" if image.get("mimeType") == "image/jpeg" else ".png"
    glow_rules = []
    if paint_file.exists():
        from PIL import Image

        paint_spec = json.loads(paint_file.read_text())
        glow_rules = paint_spec.get("glow", [])
        if paint_spec.get("paint"):
            data = paint(Image.open(io.BytesIO(data)), positions, indices, corners, corner_materials, paint_spec["paint"], not use_atlas)
            extension = ".png"
    (output_dir / f"{name}_albedo{extension}").write_bytes(data)

    emissive = {emissive_source(gltf, primitive) for _, primitive in parts}
    has_emission = not use_atlas and len(emissive) == 1 and None not in emissive
    if has_emission:
        image, data = image_bytes(gltf, binary, emissive.pop())
        extension = ".jpg" if image.get("mimeType") == "image/jpeg" else ".png"
        (output_dir / f"{name}_emission{extension}").write_bytes(data)
    elif glow_rules:
        from PIL import Image

        albedo = Image.open(io.BytesIO((output_dir / f"{name}_albedo{extension}").read_bytes()))
        (output_dir / f"{name}_emission.png").write_bytes(glow_map(albedo, positions, indices, corners, corner_materials, glow_rules, not use_atlas))
        has_emission = True

    print(f"{name}: {len(parts)} parts, {len(sources)} textures{' (atlas)' if use_atlas else ''}{' + emission' if has_emission else ''}, "
          f"{vertex_count} vertices, {len(indices) // 3 * 2} triangles (double-sided), original height {height:.4f}")


if __name__ == "__main__":
    if len(sys.argv) != 4:
        sys.exit(__doc__)
    main(pathlib.Path(sys.argv[1]), pathlib.Path(sys.argv[2]), sys.argv[3])
