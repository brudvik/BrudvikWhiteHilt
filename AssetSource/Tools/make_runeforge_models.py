"""Builds the models of the two Rune Forge extensions with Blender and writes them to AssetSource/Models:

  bindingstone.glb      a broad runestone with an iron band and a ring round its foot, a serpent band of blood-red runes
                        along its edge and a valknut round a glowing Surtling Core, on a flat footing stone; 1.42 m tall.
                        One 2:1 texture with an emission map (runes and core): the front on the left half, the other sides,
                        the iron and the core on the right half. A 2:1 texture keeps 1024 x 512 in the bundle.
  runeetchingtable.glb  a trestle table with a stone slab half covered in runes, a chisel, a mallet, three rune rings and a
                        bowl of red ochre; 0.9 m tall. Wood, stone, iron and ochre each get their own texture (an atlas).

The front of both faces Blender -y, which is +z in the game. The runes read "HVITHJALT HER BINDES DYRET TIL STALET" on the
stone and the six infusions on the slab.

Usage: python make_runeforge_models.py [--blender <blender.exe>]
"""
import json
import math
import pathlib
import struct
import subprocess
import sys

MODELS = pathlib.Path(__file__).resolve().parents[1] / "Models"
NAMES = ("bindingstone", "runeetchingtable")
SEED = 23

# The binding stone, in metres.
FOOT_HALF_WIDTH, SHOULDER_HALF_WIDTH = 0.46, 0.42
FOOT_HALF_DEPTH, SHOULDER_HALF_DEPTH = 0.17, 0.145
SHOULDER = 1.0
ARCH = 0.42
BULGE = 0.012
FRONT_POINTS = 16
LEAN = 0.03
BAND = (0.30, 0.38)
BAND_OFFSET = 0.014
CORE_HEIGHT = 0.82
CORE_RADIUS = 0.055
CORE_SINK = 0.018

STONE_TEXTURE = (2048, 1024)
FRONT_SCALE = 1.5
FRONT_BOTTOM = -0.03
SIDE_PX_PER_M = 500
GLOW = (1.0, 0.12, 0.04)

SERPENT_HALF_X = 0.30
SERPENT_START = 0.47
SERPENT_HALF_WIDTH = 0.042
STONE_TEXT = "HVITHJALT:HER:BINDES:DYRET:TIL:STALET"

# The rune etching table.
TABLE_TOP = 0.82
TABLE_LENGTH, TABLE_DEPTH = 1.15, 0.62
SLAB_SIZE = (0.48, 0.34, 0.05)
TABLE_TEXTURE = 512
SLAB_ROWS = (("ILD:FROST:GIFT", True), ("TORDEN:VEV", True), ("DYPETS:GREP", False), ("DYR", False))

# Elder futhark staves in a unit box (x right, y up), one polyline per stroke.
RUNES = {
    "A": [[(0.2, 0), (0.2, 1)], [(0.2, 1), (0.8, 0.72)], [(0.2, 0.72), (0.8, 0.44)]],
    "B": [[(0.2, 0), (0.2, 1)], [(0.2, 1), (0.75, 0.75), (0.2, 0.5), (0.75, 0.25), (0.2, 0)]],
    "D": [[(0.1, 0), (0.1, 1)], [(0.9, 0), (0.9, 1)], [(0.1, 1), (0.9, 0)], [(0.1, 0), (0.9, 1)]],
    "E": [[(0.1, 0), (0.1, 1)], [(0.9, 0), (0.9, 1)], [(0.1, 1), (0.5, 0.65), (0.9, 1)]],
    "F": [[(0.2, 0), (0.2, 1)], [(0.2, 0.5), (0.8, 0.8)], [(0.2, 0.78), (0.7, 1)]],
    "G": [[(0.1, 0), (0.9, 1)], [(0.1, 1), (0.9, 0)]],
    "H": [[(0.15, 0), (0.15, 1)], [(0.85, 0), (0.85, 1)], [(0.15, 0.68), (0.85, 0.36)]],
    "I": [[(0.5, 0), (0.5, 1)]],
    "J": [[(0.5, 1), (0.15, 0.7), (0.5, 0.42)], [(0.5, 0.58), (0.85, 0.3), (0.5, 0)]],
    "K": [[(0.75, 0.85), (0.25, 0.5), (0.75, 0.15)]],
    "L": [[(0.25, 0), (0.25, 1)], [(0.25, 1), (0.8, 0.7)]],
    "M": [[(0.1, 0), (0.1, 1)], [(0.9, 0), (0.9, 1)], [(0.1, 1), (0.9, 0.55)], [(0.9, 1), (0.1, 0.55)]],
    "N": [[(0.5, 0), (0.5, 1)], [(0.2, 0.68), (0.8, 0.36)]],
    "O": [[(0.15, 0), (0.75, 0.62), (0.5, 1), (0.25, 0.62), (0.85, 0)]],
    "P": [[(0.2, 0), (0.2, 1)], [(0.2, 1), (0.6, 0.75), (0.85, 0.95)], [(0.2, 0), (0.6, 0.25), (0.85, 0.05)]],
    "R": [[(0.2, 0), (0.2, 1)], [(0.2, 1), (0.8, 0.75), (0.2, 0.5), (0.8, 0)]],
    "S": [[(0.75, 1), (0.25, 0.68), (0.75, 0.32), (0.25, 0)]],
    "T": [[(0.5, 0), (0.5, 1)], [(0.15, 0.7), (0.5, 1), (0.85, 0.7)]],
    "U": [[(0.2, 0), (0.2, 1), (0.8, 0.7), (0.8, 0)]],
    "W": [[(0.2, 0), (0.2, 1), (0.75, 0.8), (0.2, 0.6)]],
}
ALIASES = {"V": "W", "Y": "U", "C": "K", "Q": "K", "X": "K", "Z": "S"}


# ---------------------------------------------------------------- textures


def value_noise(np, rng, height, width, cell):
    lattice = rng.random((int(height // cell) + 3, int(width // cell) + 3))
    y, x = np.arange(height) / cell, np.arange(width) / cell
    y0, x0 = np.floor(y).astype(int), np.floor(x).astype(int)
    fy, fx = y - y0, x - x0
    fy, fx = (fy * fy * (3 - 2 * fy))[:, None], (fx * fx * (3 - 2 * fx))[None, :]
    a, b = lattice[np.ix_(y0, x0)], lattice[np.ix_(y0, x0 + 1)]
    c, d = lattice[np.ix_(y0 + 1, x0)], lattice[np.ix_(y0 + 1, x0 + 1)]
    return (a * (1 - fx) + b * fx) * (1 - fy) + (c * (1 - fx) + d * fx) * fy


def fbm(np, rng, height, width, cells):
    total, norm, weight = 0.0, 0.0, 1.0
    for cell in cells:
        total = total + value_noise(np, rng, height, width, cell) * weight
        norm += weight
        weight *= 0.55
    return total / norm


def colourise(np, value, dark, light, contrast=1.0):
    value = np.clip((value - 0.5) * contrast + 0.5, 0.0, 1.0)[..., None]
    return np.array(dark) + (np.array(light) - np.array(dark)) * value


def stone_colour(np, rng, height, width):
    stone = colourise(np, fbm(np, rng, height, width, (160, 64, 24, 8, 3)), (0.17, 0.17, 0.18), (0.47, 0.46, 0.45), 1.5)
    specks = rng.random((height, width)) < 0.015
    stone[specks] = np.minimum(stone[specks] * 1.35, 1.0)
    return stone


def iron_colour(np, rng, height, width):
    iron = colourise(np, fbm(np, rng, height, width, (48, 16, 4)), (0.1, 0.1, 0.11), (0.3, 0.3, 0.32), 1.3)
    rust = np.clip((fbm(np, rng, height, width, (40, 12)) - 0.6) * 6, 0, 1)[..., None]
    return iron * (1 - rust) + np.array((0.33, 0.17, 0.08)) * rust


def wood_colour(np, rng, size):
    value = fbm(np, rng, size, size, (64, 16, 4)) * 0.4
    grain = np.convolve(rng.random(size), np.ones(5) / 5, mode="same")[None, :].repeat(size, axis=0)
    rings = 0.5 + 0.5 * np.sin(np.arange(size)[None, :] * 0.35 + fbm(np, rng, size, size, (96, 32)) * 9)
    return colourise(np, value + grain * 0.4 + rings * 0.2, (0.2, 0.13, 0.08), (0.47, 0.34, 0.21), 1.3)


class Carving:
    """Lines cut into a texture: 'cut' is the groove with its dark rim, 'paint' the red filling."""

    def __init__(self, np, height, width):
        self.np = np
        self.cut = np.zeros((height, width))
        self.paint = np.zeros((height, width))

    def line(self, p0, p1, radius, painted=True):
        np = self.np
        rim = radius + 2.0
        x0 = max(0, int(math.floor(min(p0[0], p1[0]) - rim - 1)))
        x1 = min(self.cut.shape[1], int(math.ceil(max(p0[0], p1[0]) + rim + 2)))
        y0 = max(0, int(math.floor(min(p0[1], p1[1]) - rim - 1)))
        y1 = min(self.cut.shape[0], int(math.ceil(max(p0[1], p1[1]) + rim + 2)))
        if x0 >= x1 or y0 >= y1:
            return
        px, py = np.meshgrid(np.arange(x0, x1) + 0.5, np.arange(y0, y1) + 0.5)
        dx, dy = p1[0] - p0[0], p1[1] - p0[1]
        length2 = dx * dx + dy * dy
        t = np.clip(((px - p0[0]) * dx + (py - p0[1]) * dy) / length2, 0, 1) if length2 > 0 else 0.0
        distance = np.hypot(px - (p0[0] + t * dx), py - (p0[1] + t * dy))
        self.cut[y0:y1, x0:x1] = np.maximum(self.cut[y0:y1, x0:x1], np.clip(rim + 0.5 - distance, 0, 1))
        if painted:
            self.paint[y0:y1, x0:x1] = np.maximum(self.paint[y0:y1, x0:x1], np.clip(radius + 0.5 - distance, 0, 1))
        else:
            self.cut[y0:y1, x0:x1] = np.maximum(self.cut[y0:y1, x0:x1], np.clip(radius + 1.5 - distance, 0, 1))

    def polyline(self, points, radius, painted=True):
        for a, b in zip(points, points[1:]):
            self.line(a, b, radius, painted)

    def apply(self, np, colour, rng):
        """Darkens the grooves and fills the painted ones with red ochre."""
        height, width = self.cut.shape
        ochre = colourise(np, fbm(np, rng, height, width, (12, 4)), (0.36, 0.04, 0.03), (0.62, 0.1, 0.06), 1.2)
        colour = colour * (1 - 0.55 * self.cut[..., None])
        return colour * (1 - self.paint[..., None]) + ochre * self.paint[..., None]


def glyph_width(char):
    return 0.45 if char == ":" else 1.0


def write_runes(carving, text, place, height, width, gap, radius, painted=True):
    """Writes runes along a line. place(s) gives (point, right, up) in pixels at s pixels along the line."""
    s = 0.0
    for char in text:
        char = ALIASES.get(char, char)
        advance = width * glyph_width(char) + gap
        point, right, up = place(s + advance / 2)

        def to_px(gx, gy):
            x = (gx - 0.5) * width
            y = (gy - 0.5) * height
            return (point[0] + right[0] * x + up[0] * y, point[1] + right[1] * x + up[1] * y)

        if char == ":":
            for gy in (0.3, 0.7):
                p = (point[0] + up[0] * (gy - 0.5) * height, point[1] + up[1] * (gy - 0.5) * height)
                carving.line(p, p, radius * 1.3, painted)
        else:
            for stroke in RUNES[char]:
                carving.polyline([to_px(*p) for p in stroke], radius, painted)
        s += advance
    return s


def text_length(text, width, gap):
    return sum(width * glyph_width(ALIASES.get(c, c)) + gap for c in text)


# ---------------------------------------------------------------- binding stone geometry


def side_wobble(z, x):
    """Left and right edges of the stone wander a little, differently."""
    if x < 0:
        return 1.0 + 0.03 * math.sin(4.3 * z + 0.7) + 0.015 * math.sin(11.0 * z + 2.0)
    return 1.0 + 0.025 * math.sin(5.1 * z + 2.1) + 0.012 * math.sin(9.0 * z + 0.4)


def cross_section(z):
    """Half width and half depth of the stone at height z (before the wobble)."""
    if z <= SHOULDER:
        t = max(0.0, z) / SHOULDER
        return (FOOT_HALF_WIDTH + (SHOULDER_HALF_WIDTH - FOOT_HALF_WIDTH) * t,
                FOOT_HALF_DEPTH + (SHOULDER_HALF_DEPTH - FOOT_HALF_DEPTH) * t)
    phi = math.asin(min(1.0, (z - SHOULDER) / ARCH))
    return max(0.06, SHOULDER_HALF_WIDTH * math.cos(phi)), SHOULDER_HALF_DEPTH * (0.6 + 0.4 * math.cos(phi))


def ring_points(z, half_width, half_depth, offset=0.0):
    """A rounded rectangle, counter-clockwise seen from above, starting at the front left. Same count for every ring."""
    w, d = half_width + offset, half_depth + offset
    rc = min(0.8 * half_depth, 0.95 * half_width) + offset
    inner_x, inner_y = w - rc, d - rc

    def face(sign):
        # Front (sign -1) runs left to right, back (sign 1) right to left; both bulge a little in the middle.
        for i in range(FRONT_POINTS):
            x = sign * inner_x - sign * 2 * inner_x * i / FRONT_POINTS
            bulge = BULGE * (1 - (x / inner_x) ** 2) if inner_x > 1e-6 else 0.0
            yield x, sign * (d + bulge)

    def corner(cx, cy, start):
        for i in range(6):
            angle = math.radians(start + 90 * i / 6)
            yield cx + rc * math.cos(angle), cy + rc * math.sin(angle)

    points = list(face(-1))
    points += list(corner(inner_x, -inner_y, -90)) + [(w, -inner_y)] + list(corner(inner_x, inner_y, 0))
    points += list(face(1))
    points += list(corner(-inner_x, inner_y, 90)) + [(-w, inner_y)] + list(corner(-inner_x, -inner_y, 180))
    return [(x * side_wobble(z, x), y, z) for x, y in points]


def stone_rings():
    heights = [i * 0.05 for i in range(21)]
    rings = [(z,) + cross_section(z) for z in heights]
    for step in range(1, 15):
        phi = math.radians(86 * math.sin(math.pi / 2 * step / 14))
        z = SHOULDER + ARCH * math.sin(phi)
        rings.append((z,) + cross_section(z))
    return rings


def front_uv(x, z):
    return 0.25 + x / (2 * FRONT_SCALE), (z - FRONT_BOTTOM) / FRONT_SCALE


def side_uv(a, b):
    width, height = STONE_TEXTURE
    return 0.75 + a * SIDE_PX_PER_M / width, 0.25 + (b + 0.07) * SIDE_PX_PER_M / height


def iron_uv(a, b, centre):
    return 0.625 + a * 0.2, 0.125 + (b - centre) * 0.4


def core_uv(x, dz):
    return 0.875 + x / CORE_RADIUS * 0.05, 0.125 + dz / CORE_RADIUS * 0.1


def loft(bm, rings, close_top=None):
    verts = [[bm.verts.new(p) for p in ring] for ring in rings]
    count = len(verts[0])
    for lower, upper in zip(verts, verts[1:]):
        for k in range(count):
            n = (k + 1) % count
            bm.faces.new((lower[k], lower[n], upper[n], upper[k]))
    if close_top is not None:
        top = bm.verts.new(close_top)
        last = verts[-1]
        for k in range(count):
            bm.faces.new((last[k], last[(k + 1) % count], top))
    return verts


def set_uvs(bm, mapper):
    bm.normal_update()
    layer = bm.loops.layers.uv.new("UVMap")
    for face in bm.faces:
        for loop in face.loops:
            loop[layer].uv = mapper(loop.vert.co, face.normal)


def mesh_object(bpy, bm, name, material, smooth=True):
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    for polygon in mesh.polygons:
        polygon.use_smooth = smooth
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    obj.data.materials.append(material)
    return obj


def lumpy_rock(bmesh, bm, rng, centre, size, flatten=0.6, subdivisions=2):
    """An icosphere pushed in and out by random waves and flattened top and bottom."""
    result = bmesh.ops.create_icosphere(bm, subdivisions=subdivisions, radius=1.0)
    waves = [(rng.normal(size=3), rng.uniform(1.5, 3.5), rng.uniform(0, 6.28), rng.uniform(0.03, 0.07)) for _ in range(5)]
    for vert in result["verts"]:
        co = vert.co.copy()
        bump = sum(a * math.sin(f * (co.x * d[0] + co.y * d[1] + co.z * d[2]) + p) for d, f, p, a in waves)
        co = co * (1.0 + bump)
        co.z = math.copysign(min(abs(co.z), flatten + 0.15 * abs(co.z)), co.z)
        vert.co = (centre[0] + co.x * size[0], centre[1] + co.y * size[1], centre[2] + co.z * size[2])


def binding_stone_texture(np, rng):
    width, height = STONE_TEXTURE
    colour = stone_colour(np, rng, height, width)
    # Lichen on the back and sides only, so the carving stays clean.
    lichen = np.clip((fbm(np, rng, height, width, (120, 48, 16, 5)) - 0.68) * 4, 0, 1)[..., None] * 0.45
    lichen[:, : width // 2] = 0
    colour = colour * (1 - lichen) + np.array((0.5, 0.52, 0.4)) * lichen
    half = width // 2
    quarter = height // 4
    colour[:quarter, half:half + half // 2] = iron_colour(np, rng, quarter, half // 2)

    carving = Carving(np, height, width)
    scale = width / (2 * FRONT_SCALE)

    def px(x, z):
        u, v = front_uv(x, z)
        return u * width, v * height

    # The serpent: its centre line runs up the left edge, over the arch and down the right edge.
    path = [(-SERPENT_HALF_X, SERPENT_START + (SHOULDER - SERPENT_START) * i / 20) for i in range(20)]
    path += [(-SERPENT_HALF_X * math.cos(math.pi * i / 48), SHOULDER + SERPENT_HALF_X * math.sin(math.pi * i / 48)) for i in range(48)]
    path += [(SERPENT_HALF_X, SHOULDER - (SHOULDER - SERPENT_START) * i / 20) for i in range(21)]
    path = [px(*p) for p in path]
    lengths = [0.0]
    for a, b in zip(path, path[1:]):
        lengths.append(lengths[-1] + math.hypot(b[0] - a[0], b[1] - a[1]))
    total = lengths[-1]

    def place(s):
        s = min(max(s, 0.0), total - 1e-6)
        index = max(i for i in range(len(lengths) - 1) if lengths[i] <= s)
        a, b = path[index], path[index + 1]
        t = (s - lengths[index]) / max(lengths[index + 1] - lengths[index], 1e-9)
        point = (a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t)
        length = math.hypot(b[0] - a[0], b[1] - a[1])
        right = ((b[0] - a[0]) / length, (b[1] - a[1]) / length)
        return point, right, (-right[1], right[0])

    tail = 0.15 * scale
    half_width = SERPENT_HALF_WIDTH * scale
    for sign in (-1, 1):
        edge = []
        for step in range(0, int(total) + 1, 4):
            point, _, up = place(step)
            taper = min(1.0, (total - step) / tail)
            edge.append((point[0] + up[0] * half_width * taper * sign, point[1] + up[1] * half_width * taper * sign))
        carving.polyline(edge, 3.5)

    # The head below the start of the band: broad where it meets the band, narrowing to the snout, with an eye.
    start, right, up = place(0)
    head = (start[0] - right[0] * 0.055 * scale, start[1] - right[1] * 0.055 * scale)
    outline = []
    for i in range(33):
        angle = 2 * math.pi * i / 32
        along = 0.07 * scale * math.cos(angle)
        across = 0.05 * scale * math.sin(angle) * (0.72 + 0.28 * math.cos(angle))
        outline.append((head[0] + right[0] * along + up[0] * across, head[1] + right[1] * along + up[1] * across))
    carving.polyline(outline, 3.5)
    eye = (head[0] + right[0] * 0.012 * scale + up[0] * 0.018 * scale, head[1] + right[1] * 0.012 * scale + up[1] * 0.018 * scale)
    carving.line(eye, eye, 6.0)

    rune_height, rune_width, rune_gap = 0.05 * scale, 0.03 * scale, 0.012 * scale
    length = text_length(STONE_TEXT, rune_width, rune_gap)
    begin = 0.04 * scale + (total - tail - 0.04 * scale - length) / 2
    write_runes(carving, STONE_TEXT, lambda s: place(begin + s), rune_height, rune_width, rune_gap, 3.0)

    # The valknut round the core: three triangles pushed apart so they interlock.
    centre = px(0.0, CORE_HEIGHT)
    for index in range(3):
        offset_angle = math.radians(120 + 120 * index)
        cx = centre[0] + 0.072 * scale * math.cos(offset_angle)
        cy = centre[1] + 0.072 * scale * math.sin(offset_angle)
        corners = [(cx + 0.12 * scale * math.cos(math.radians(90 + 120 * k)), cy + 0.12 * scale * math.sin(math.radians(90 + 120 * k)))
                   for k in range(4)]
        carving.polyline(corners, 3.5)
    ring = [(centre[0] + 0.075 * scale * math.cos(2 * math.pi * i / 48), centre[1] + 0.075 * scale * math.sin(2 * math.pi * i / 48))
            for i in range(49)]
    carving.polyline(ring, 3.0)

    colour = carving.apply(np, colour, rng)
    emission = np.zeros_like(colour)
    emission[...] = np.array(GLOW) * (carving.paint + 0.15 * (carving.cut - carving.paint).clip(0, 1))[..., None]

    # The Surtling Core: a white-hot middle cooling to red at the rim, with dark cracks.
    cy, cx = np.mgrid[0:quarter, 0:half // 2]
    cx = (cx + 0.5 - half // 4) / (0.05 * width)
    cy = (cy + 0.5 - quarter / 2) / (0.1 * height)
    radius = np.clip(np.hypot(cx, cy), 0, 1)[..., None]
    hot = np.array((1.0, 0.85, 0.45)) * (1 - radius) + np.array((1.0, 0.3, 0.04)) * radius
    hot = hot * (1 - radius ** 3) + np.array((0.35, 0.04, 0.02)) * radius ** 3
    cracks = (np.abs(fbm(np, rng, quarter, half // 2, (24, 8)) - 0.5) < 0.03)[..., None]
    hot = np.where(cracks, hot * 0.35, hot)
    colour[:quarter, half + half // 2:] = hot
    emission[:quarter, half + half // 2:] = hot * (1.1 - 0.6 * radius)
    return colour, emission


def build_binding_stone(bpy, bmesh, np, rng):
    from mathutils import Vector

    colour, emission = binding_stone_texture(np, rng)
    material = make_material(bpy, np, "bindingstone", colour, emission)
    parts = []

    def stone_mapper(co, normal):
        if normal.y < -0.45:
            return front_uv(co.x, co.z)
        if normal.z > 0.7:
            return side_uv(co.x, 1.2 + co.y)
        if abs(normal.x) >= abs(normal.y):
            return side_uv(co.y if normal.x > 0 else -co.y, co.z)
        return side_uv(-co.x, co.z)

    rings = stone_rings()
    bm = bmesh.new()
    loft(bm, [ring_points(z, w, d) for z, w, d in rings], close_top=(0.0, 0.0, rings[-1][0] + 0.004))
    set_uvs(bm, stone_mapper)
    parts.append(mesh_object(bpy, bm, "stone", material))

    # The footing stone the runestone stands in.
    bm = bmesh.new()
    lumpy_rock(bmesh, bm, rng, (0.0, 0.0, 0.03), (0.64, 0.36, 0.09))
    set_uvs(bm, lambda co, normal: side_uv(co.x, 0.55 + co.y))
    parts.append(mesh_object(bpy, bm, "footing", material))

    # The iron band round the foot, with lips down to the stone.
    def band_mapper(centre):
        return lambda co, normal: iron_uv(co.y if abs(normal.x) > 0.8 else co.x, co.z, centre)

    bottom, top = BAND
    stone_bottom = ring_points(bottom, *cross_section(bottom))
    stone_top = ring_points(top, *cross_section(top))
    band_bottom = ring_points(bottom, *cross_section(bottom), offset=BAND_OFFSET)
    band_top = ring_points(top, *cross_section(top), offset=BAND_OFFSET)
    bm = bmesh.new()
    loft(bm, [stone_bottom, band_bottom, band_top, stone_top])
    set_uvs(bm, band_mapper((bottom + top) / 2))
    parts.append(mesh_object(bpy, bm, "band", material))

    # A staple on the front of the band and a ring hanging from it.
    front_y = band_bottom[FRONT_POINTS // 2][1]
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    for vert in bm.verts:
        vert.co = (vert.co.x * 0.03, front_y - 0.008 + vert.co.y * 0.016, 0.31 + vert.co.z * 0.04)
    set_uvs(bm, band_mapper(0.31))
    parts.append(mesh_object(bpy, bm, "staple", material, smooth=False))

    major, minor = 0.05, 0.011
    ring_centre = (0.0, front_y - 0.02, 0.30 - major + minor)
    bm = bmesh.new()
    torus(bm, major, minor, 24, 8, lambda p: (ring_centre[0] + p[0], ring_centre[1] + p[2], ring_centre[2] + p[1]))
    set_uvs(bm, lambda co, normal: iron_uv(co.x * 2, co.z * 2, ring_centre[2] * 2))
    parts.append(mesh_object(bpy, bm, "ring", material))

    # The Surtling Core, half sunk into the front.
    _, half_depth = cross_section(CORE_HEIGHT)
    core_y = -(half_depth + BULGE) + CORE_SINK
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=20, v_segments=12, radius=CORE_RADIUS)
    for vert in bm.verts:
        vert.co = (vert.co.x, core_y + vert.co.y, CORE_HEIGHT + vert.co.z)
    set_uvs(bm, lambda co, normal: core_uv(co.x, co.z - CORE_HEIGHT))
    parts.append(mesh_object(bpy, bm, "core", material))

    # The stone leans a little to one side.
    for obj in parts:
        for vert in obj.data.vertices:
            vert.co.x += LEAN * max(0.0, vert.co.z)
    return parts


def torus(bm, major, minor, segments, sides, place):
    """A torus round the local z axis; place maps (x, y, z) to the final position."""
    rows = []
    for i in range(segments):
        a = 2 * math.pi * i / segments
        row = []
        for j in range(sides):
            b = 2 * math.pi * j / sides
            r = major + minor * math.cos(b)
            row.append(bm.verts.new(place((r * math.cos(a), r * math.sin(a), minor * math.sin(b)))))
        rows.append(row)
    for i in range(segments):
        for j in range(sides):
            a, b = rows[i][j], rows[(i + 1) % segments][j]
            c, d = rows[(i + 1) % segments][(j + 1) % sides], rows[i][(j + 1) % sides]
            bm.faces.new((a, b, c, d))


# ---------------------------------------------------------------- rune etching table


def slab_texture(np, rng):
    size = TABLE_TEXTURE
    colour = stone_colour(np, rng, size, size)
    carving = Carving(np, size, size)
    scale = size / 0.52
    rune_height, rune_width, rune_gap = 0.038 * scale, 0.022 * scale, 0.009 * scale
    left = 0.5 * size - 0.2 * scale
    for row, (text, painted) in enumerate(SLAB_ROWS):
        baseline = 0.5 * size + (0.105 - 0.068 * row) * scale
        for guide in (baseline - rune_height * 0.62, baseline + rune_height * 0.62):
            carving.line((left - 6, guide), (0.5 * size + 0.2 * scale + 6, guide), 0.8, painted=False)
        write_runes(carving, text, lambda s: ((left + s, baseline), (1.0, 0.0), (0.0, 1.0)),
                    rune_height, rune_width, rune_gap, 2.6, painted)
    return carving.apply(np, colour, rng)


def make_material(bpy, np, name, colour, emission=None):
    def image(suffix, pixels):
        height, width = pixels.shape[:2]
        result = bpy.data.images.new(f"{name}_{suffix}", width, height, alpha=False)
        rgba = np.concatenate([np.clip(pixels, 0, 1), np.ones((height, width, 1))], axis=2).astype(np.float32)
        result.pixels.foreach_set(rgba.ravel())
        result.pack()
        return result

    material = bpy.data.materials.new(name)
    material.use_nodes = True
    nodes, links = material.node_tree.nodes, material.node_tree.links
    shader = nodes["Principled BSDF"]
    shader.inputs["Roughness"].default_value = 0.85
    albedo = nodes.new("ShaderNodeTexImage")
    albedo.image = image("albedo", colour)
    links.new(albedo.outputs["Color"], shader.inputs["Base Color"])
    if emission is not None:
        glow = nodes.new("ShaderNodeTexImage")
        glow.image = image("emission", emission)
        links.new(glow.outputs["Color"], shader.inputs["Emission Color"])
        shader.inputs["Emission Strength"].default_value = 1.0
    return material


def build_etching_table(bpy, bmesh, np, rng):
    from mathutils import Matrix, Vector

    wood = make_material(bpy, np, "etching_wood", wood_colour(np, rng, TABLE_TEXTURE))
    slab = make_material(bpy, np, "etching_slab", slab_texture(np, rng))
    iron = make_material(bpy, np, "etching_iron", iron_colour(np, rng, TABLE_TEXTURE, TABLE_TEXTURE))
    ochre = make_material(bpy, np, "etching_ochre", colourise(np, fbm(np, rng, TABLE_TEXTURE, TABLE_TEXTURE, (16, 5, 2)),
                                                              (0.33, 0.05, 0.03), (0.62, 0.13, 0.06), 1.4))
    low = Vector((-0.62, -0.34, 0.0))
    uv_scale = 0.8
    parts = []

    def box_uv(grain):
        """World box projection with the wood grain (texture v) along the given axis."""
        def mapper(co, normal):
            axis = max(range(3), key=lambda i: abs(normal[i]))
            others = [i for i in range(3) if i != axis]
            if grain in others:
                others = [i for i in others if i != grain] + [grain]
            return tuple(0.02 + (co[i] - low[i]) * uv_scale for i in others)
        return mapper

    def add(name, material, build, matrix, mapper, smooth=False):
        bm = bmesh.new()
        build(bm)
        bmesh.ops.transform(bm, matrix=matrix, verts=bm.verts)
        set_uvs(bm, mapper)
        parts.append(mesh_object(bpy, bm, name, material, smooth))

    def cube(size):
        def build(bm):
            bmesh.ops.create_cube(bm, size=1.0)
            bmesh.ops.scale(bm, vec=size, verts=bm.verts)
        return build

    def cylinder(radius, depth, segments=12, radius2=None):
        def build(bm):
            bmesh.ops.create_cone(bm, cap_ends=True, segments=segments, radius1=radius,
                                  radius2=radius if radius2 is None else radius2, depth=depth)
        return build

    def at(x, y, z, yaw=0.0, pitch=0.0, roll=0.0):
        return (Matrix.Translation((x, y, z)) @ Matrix.Rotation(math.radians(yaw), 4, "Z")
                @ Matrix.Rotation(math.radians(pitch), 4, "Y") @ Matrix.Rotation(math.radians(roll), 4, "X"))

    # Three boards on top, legs, aprons and a stretcher low down.
    board = (TABLE_DEPTH - 0.016) / 3
    for index in range(3):
        y = -TABLE_DEPTH / 2 + board / 2 + index * (board + 0.008)
        add(f"board{index}", wood, cube((TABLE_LENGTH, board, 0.07)), at(0.0, y, TABLE_TOP - 0.035), box_uv(0))
    leg_x, leg_y = TABLE_LENGTH / 2 - 0.1, TABLE_DEPTH / 2 - 0.09
    for sx in (-1, 1):
        for sy in (-1, 1):
            add(f"leg{sx}{sy}", wood, cube((0.075, 0.075, TABLE_TOP - 0.07)), at(sx * leg_x, sy * leg_y, (TABLE_TOP - 0.07) / 2), box_uv(2))
        add(f"end_apron{sx}", wood, cube((0.035, 2 * leg_y, 0.09)), at(sx * leg_x, 0.0, TABLE_TOP - 0.115), box_uv(1))
        add(f"foot{sx}", wood, cube((0.06, 2 * leg_y + 0.06, 0.06)), at(sx * leg_x, 0.0, 0.17), box_uv(1))
    for sy in (-1, 1):
        add(f"apron{sy}", wood, cube((2 * leg_x, 0.035, 0.09)), at(0.0, sy * leg_y, TABLE_TOP - 0.115), box_uv(0))
    add("stretcher", wood, cube((2 * leg_x, 0.06, 0.06)), at(0.0, 0.0, 0.17), box_uv(0))

    # The slab being carved, a little askew.
    sx, sy, sz = SLAB_SIZE

    def build_slab(bm):
        bmesh.ops.create_cube(bm, size=1.0)
        bmesh.ops.scale(bm, vec=SLAB_SIZE, verts=bm.verts)
        bmesh.ops.bevel(bm, geom=list(bm.edges), offset=0.008, segments=2, affect="EDGES")
        bm.normal_update()
        layer = bm.loops.layers.uv.new("UVMap")
        for face in bm.faces:
            for loop in face.loops:
                co = loop.vert.co
                if face.normal.z > 0.7:
                    loop[layer].uv = (0.5 + co.x / 0.52, 0.5 + co.y / 0.52)
                else:
                    along = co.x if abs(face.normal.y) > abs(face.normal.x) else co.y
                    loop[layer].uv = (0.5 + along / 0.52, 0.06 + co.z / 0.52)

    bm = bmesh.new()
    build_slab(bm)
    bmesh.ops.transform(bm, matrix=at(-0.14, 0.03, TABLE_TOP + sz / 2, yaw=5), verts=bm.verts)
    parts.append(mesh_object(bpy, bm, "slab", slab, smooth=False))

    # The chisel: wooden handle, iron shank and a flat blade, lying along x before it is turned.
    chisel = at(0.25, -0.2, TABLE_TOP + 0.015, yaw=-28)
    add("chisel_handle", wood, cylinder(0.015, 0.11), chisel @ at(-0.055, 0, 0, pitch=90), box_uv(0), smooth=True)
    add("chisel_shank", iron, cylinder(0.006, 0.08, 8), chisel @ at(0.04, 0, 0, pitch=90), box_uv(0), smooth=True)
    add("chisel_blade", iron, cube((0.035, 0.02, 0.004)), chisel @ at(0.095, 0, -0.004), box_uv(0))

    # The mallet: a round head on its side and a handle sloping down to the table.
    head_radius = 0.042
    mallet = at(0.43, 0.08, TABLE_TOP + head_radius, yaw=200)
    add("mallet_head", wood, cylinder(head_radius, 0.13, 16), mallet @ at(0, 0, 0, roll=90), box_uv(1), smooth=True)
    add("mallet_handle", wood, cylinder(0.012, 0.25, 8), mallet @ at(0.13, 0, -0.012, pitch=90 + 7), box_uv(0), smooth=True)

    # Three rune rings, one lying on the other two.
    for index, (x, y, z) in enumerate(((0.2, 0.19, 0.009), (0.27, 0.215, 0.009), (0.235, 0.2, 0.027))):
        def build_ring(bm):
            torus(bm, 0.038, 0.009, 24, 8, lambda p: p)
        add(f"rune{index}", iron, build_ring, at(x, y, TABLE_TOP + z), box_uv(0), smooth=True)

    # A wooden bowl of red ochre.
    def build_bowl(bm):
        profile = [(0.0, 0.0), (0.045, 0.0), (0.062, 0.012), (0.068, 0.032), (0.066, 0.045), (0.058, 0.045),
                   (0.056, 0.03), (0.044, 0.012), (0.0, 0.01)]
        lathe(bm, profile, 20)

    add("bowl", wood, build_bowl, at(0.45, -0.17, TABLE_TOP), box_uv(2), smooth=True)
    add("ochre", ochre, cylinder(0.054, 0.006, 20), at(0.45, -0.17, TABLE_TOP + 0.03), box_uv(2), smooth=True)
    return parts


def lathe(bm, profile, segments):
    rings = []
    for r, z in profile:
        if r == 0.0:
            rings.append([bm.verts.new((0.0, 0.0, z))])
        else:
            rings.append([bm.verts.new((r * math.cos(2 * math.pi * i / segments), r * math.sin(2 * math.pi * i / segments), z))
                          for i in range(segments)])
    for lower, upper in zip(rings, rings[1:]):
        for i in range(segments):
            j = (i + 1) % segments
            if len(lower) == 1:
                bm.faces.new((lower[0], upper[j], upper[i]))
            elif len(upper) == 1:
                bm.faces.new((lower[i], lower[j], upper[0]))
            else:
                bm.faces.new((lower[i], lower[j], upper[j], upper[i]))


# ---------------------------------------------------------------- driver


def build_in_blender(folder):
    import bmesh
    import bpy
    import numpy as np

    builders = {"bindingstone": build_binding_stone, "runeetchingtable": build_etching_table}
    for name in NAMES:
        bpy.ops.wm.read_factory_settings(use_empty=True)
        rng = np.random.default_rng(SEED + NAMES.index(name))
        parts = builders[name](bpy, bmesh, np, rng)
        bpy.ops.object.select_all(action="DESELECT")
        for obj in parts:
            obj.select_set(True)
        output = pathlib.Path(folder) / f"{name}.glb"
        bpy.ops.export_scene.gltf(filepath=str(output), export_format="GLB", use_selection=True, export_yup=True)
        faces = sum(len(obj.data.polygons) for obj in parts)
        points = [obj.matrix_world @ v.co for obj in parts for v in obj.data.vertices]
        low = [min(p[i] for p in points) for i in range(3)]
        high = [max(p[i] for p in points) for i in range(3)]
        size = ", ".join(f"{h - l:.3f}" for l, h in zip(low, high))
        print(f"RUNEFORGE {name}: {len(parts)} parts, {faces} faces, size x/y/z {size}, z {low[2]:.3f}..{high[2]:.3f}")


def add_asset_info(path, title):
    data = path.read_bytes()
    json_length = struct.unpack_from("<I", data, 12)[0]
    gltf = json.loads(data[20:20 + json_length])
    gltf["asset"]["extras"] = {
        "title": title,
        "author": "BrudvikWhiteHilt (generated by AssetSource/Tools/make_runeforge_models.py)",
        "license": "Same as BrudvikWhiteHilt",
    }
    chunk = json.dumps(gltf, separators=(",", ":")).encode("utf-8")
    chunk += b" " * (-len(chunk) % 4)
    rest = data[20 + json_length:]
    header = struct.pack("<4sII", b"glTF", 2, 12 + 8 + len(chunk) + len(rest))
    path.write_bytes(header + struct.pack("<I4s", len(chunk), b"JSON") + chunk + rest)


def main():
    if "bpy" in sys.modules:
        build_in_blender(sys.argv[sys.argv.index("--") + 1])
        return

    blender = sys.argv[sys.argv.index("--blender") + 1] if "--blender" in sys.argv else None
    if blender is None:
        found = sorted(pathlib.Path(r"C:\Program Files\Blender Foundation").glob("*/blender.exe"))
        if not found:
            raise SystemExit("Blender not found; pass --blender")
        blender = str(found[-1])
    result = subprocess.run([blender, "-b", "--factory-startup", "--python-exit-code", "1", "--python", __file__, "--", str(MODELS)],
                            capture_output=True, text=True, encoding="utf-8", errors="replace")
    for line in result.stdout.splitlines():
        if line.startswith("RUNEFORGE"):
            print(line)
    if result.returncode != 0:
        print(result.stdout[-3000:], result.stderr[-3000:])
        raise SystemExit("Blender failed")
    titles = {"bindingstone": "Binding Stone", "runeetchingtable": "Rune Etching Table"}
    for name in NAMES:
        path = MODELS / f"{name}.glb"
        add_asset_info(path, titles[name])
        print(f"Wrote {path}")


if __name__ == "__main__":
    main()
