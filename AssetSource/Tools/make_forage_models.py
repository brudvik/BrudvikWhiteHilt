"""Builds the procedural forageable models with Blender and writes them to AssetSource/Models.

Each model is grown from simple parts (tapered stems, leaves with a fold and a droop, flower heads of small spheres,
lumpy stones, bricks) into one mesh with one palette texture. The palette is a 4 x 4 grid of swatches, each a vertical
gradient with fractal noise; every part maps its UVs into its own swatch, so leaves darken towards the stem and stones
get their grain. convert_glb.py keeps the shared texture as it is.

Bushes are written twice: <name>.glb without its fruit or flowers (what stays after picking) and <name>fruit.glb with
only them. Both carry two tiny marker triangles at the corners of the whole plant, so the converter scales and places
them the same way and the code can stack them (ForageableBase.ReplaceBushMesh).

Sizes are real metres; the bundle stores every model one unit high and the code sets the size in the world.

Usage: python make_forage_models.py [name ...] [--blender <blender.exe>]
"""
import json
import math
import pathlib
import struct
import subprocess
import sys

MODELS = pathlib.Path(__file__).resolve().parents[1] / "Models"
TEXTURE_SIZE = 512
GRID = 4
SWATCH = TEXTURE_SIZE // GRID
MARGIN = 3

# name -> (title, builder); filled by @model below.
BUILDERS = {}


def model(name, title):
    def register(function):
        BUILDERS[name] = (title, function)
        return function
    return register


# ---------------------------------------------------------------------------------------------------------------- palette

def value_noise(np, rng, size, cells):
    def layer(cell):
        count = max(2, size // cell + 2)
        lattice = rng.random((count, count))
        coords = np.arange(size) / cell
        i0 = np.floor(coords).astype(int)
        f = coords - i0
        f = f * f * (3 - 2 * f)
        a = lattice[np.ix_(i0, i0)]
        b = lattice[np.ix_(i0, i0 + 1)]
        c = lattice[np.ix_(i0 + 1, i0)]
        d = lattice[np.ix_(i0 + 1, i0 + 1)]
        fx, fy = f[None, :], f[:, None]
        return (a * (1 - fx) + b * fx) * (1 - fy) + (c * (1 - fx) + d * fx) * fy

    value, weight, norm = 0.0, 1.0, 0.0
    for cell in cells:
        value = value + layer(cell) * weight
        norm += weight
        weight *= 0.55
    return value / norm


def swatch_pixels(np, rng, spec):
    """spec: dict(bottom, top, noise=0.12, cells=(32, 8, 3), streaks=0, spots=None (colour), spot_share=0)."""
    size = SWATCH
    bottom, top = np.array(spec["bottom"], float), np.array(spec.get("top", spec["bottom"]), float)
    t = np.linspace(0.0, 1.0, size)[:, None, None]
    pixels = bottom + (top - bottom) * t
    noise = value_noise(np, rng, size, spec.get("cells", (32, 8, 3)))
    streaks = spec.get("streaks", 0.0)
    if streaks > 0:
        columns = np.convolve(rng.random(size), np.ones(3) / 3, mode="same")[None, :].repeat(size, axis=0)
        noise = noise * (1 - streaks) + columns * streaks
    amount = spec.get("noise", 0.12)
    pixels = pixels * (1.0 + (noise[..., None] - 0.5) * 2 * amount)
    spots = spec.get("spots")
    if spots is not None:
        mask = value_noise(np, rng, size, spec.get("spot_cells", (6, 3))) > 1.0 - spec.get("spot_share", 0.3)
        pixels = np.where(mask[..., None], np.array(spots, float) * (0.85 + 0.3 * noise[..., None]), pixels)
    return np.clip(pixels, 0.0, 1.0)


class Palette:
    def __init__(self, np, rng, specs):
        if len(specs) > GRID * GRID:
            raise ValueError("too many swatches")
        self.index = {name: i for i, name in enumerate(specs)}
        image = np.zeros((TEXTURE_SIZE, TEXTURE_SIZE, 3))
        for name, i in self.index.items():
            row, column = divmod(i, GRID)
            image[row * SWATCH:(row + 1) * SWATCH, column * SWATCH:(column + 1) * SWATCH] = swatch_pixels(np, rng, specs[name])
        self.pixels = image

    def uv(self, name, s, t):
        """Maps (s, t) in 0..1 into the swatch; t = 0 is the bottom colour."""
        row, column = divmod(self.index[name], GRID)
        span = SWATCH - 2 * MARGIN
        u = (column * SWATCH + MARGIN + min(max(s, 0.0), 1.0) * span) / TEXTURE_SIZE
        # Blender images start at the bottom row, like the rows of the numpy image above.
        v = (row * SWATCH + MARGIN + min(max(t, 0.0), 1.0) * span) / TEXTURE_SIZE
        return u, v


# ---------------------------------------------------------------------------------------------------------------- geometry

class Geometry:
    """Collects triangles and quads with UVs into two bmeshes: the plant and its fruit."""

    def __init__(self, bmesh, Vector, palette):
        self.bmesh, self.Vector, self.palette = bmesh, Vector, palette
        self.meshes = {"main": bmesh.new(), "fruit": bmesh.new()}
        for bm in self.meshes.values():
            bm.loops.layers.uv.new("UVMap")
        self.target = "main"

    def face(self, points, uvs):
        bm = self.meshes[self.target]
        verts = [bm.verts.new(tuple(p)) for p in points]
        try:
            face = bm.faces.new(verts)
        except ValueError:
            return
        layer = bm.loops.layers.uv["UVMap"]
        for loop, uv in zip(face.loops, uvs):
            loop[layer].uv = uv
        face.smooth = True

    def grid(self, rows, swatch, st):
        """rows: list of rows of points; st(i, j) -> (s, t). Quads between neighbouring rows."""
        p = self.palette
        for i in range(len(rows) - 1):
            for j in range(len(rows[i]) - 1):
                quad = [rows[i][j], rows[i][j + 1], rows[i + 1][j + 1], rows[i + 1][j]]
                uvs = [p.uv(swatch, *st(i, j)), p.uv(swatch, *st(i, j + 1)), p.uv(swatch, *st(i + 1, j + 1)), p.uv(swatch, *st(i + 1, j))]
                self.face(quad, uvs)

    def tube(self, points, radii, swatch, segments=6, cap=True):
        """A stem along a polyline, tapering through radii (one per point)."""
        V = self.Vector
        points = [V(p) for p in points]
        lengths = [0.0]
        for a, b in zip(points, points[1:]):
            lengths.append(lengths[-1] + (b - a).length)
        total = max(lengths[-1], 1e-6)
        tangent = (points[1] - points[0]).normalized()
        normal = tangent.orthogonal().normalized()
        rows = []
        for i, point in enumerate(points):
            if 0 < i < len(points) - 1:
                new_tangent = (points[i + 1] - points[i - 1]).normalized()
            else:
                new_tangent = (points[min(i + 1, len(points) - 1)] - points[max(i - 1, 0)]).normalized()
            # Parallel transport keeps the rings from twisting.
            normal = (normal - new_tangent * normal.dot(new_tangent)).normalized()
            tangent = new_tangent
            binormal = tangent.cross(normal)
            ring = []
            for k in range(segments + 1):
                angle = 2 * math.pi * k / segments
                ring.append(point + (normal * math.cos(angle) + binormal * math.sin(angle)) * radii[i])
            rows.append(ring)
        self.grid(rows, swatch, lambda i, j: (j / segments, lengths[i] / total))
        if cap and radii[-1] > 0.0015:
            tip = points[-1] + tangent * radii[-1] * 0.6
            uv = self.palette.uv(swatch, 0.5, 1.0)
            for k in range(segments):
                self.face([rows[-1][k], rows[-1][k + 1], tip], [uv, uv, uv])

    def leaf(self, base, direction, up, length, width, swatch, profile="lance", segments=6, fold=0.25, droop=0.3, twist=0.0):
        """A leaf blade with a midrib fold; droop bends it down towards the tip."""
        V = self.Vector
        base, direction, up = V(base), V(direction).normalized(), V(up).normalized()
        side = direction.cross(up)
        if side.length < 1e-4:
            side = direction.orthogonal()
        side.normalize()
        normal = side.cross(direction).normalized()
        rows = []
        for i in range(segments + 1):
            t = i / segments
            centre = base + direction * (length * t) - normal * (droop * length * t * t)
            half = width * 0.5 * leaf_profile(profile, t)
            angle = twist * t
            s_dir = side * math.cos(angle) + normal * math.sin(angle)
            lift = normal * (half * math.sin(fold))
            spread = s_dir * (half * math.cos(fold))
            rows.append([centre - spread + lift, centre, centre + spread + lift])
        self.grid(rows, swatch, lambda i, j: (j / 2, i / segments))

    def blob(self, centre, radius, swatch, subdivisions=1, squash=(1.0, 1.0, 1.0), rng=None, bumps=0.0, flat_bottom=None):
        """A small sphere (berry, flower, bud) or, with bumps, a lumpy stone."""
        V = self.Vector
        bm = self.bmesh.new()
        self.bmesh.ops.create_icosphere(bm, subdivisions=subdivisions, radius=1.0)
        waves = []
        if bumps > 0 and rng is not None:
            waves = [(rng.normal(size=3), rng.uniform(1.5, 4.0), rng.uniform(0, 6.28), rng.uniform(0.3, 1.0) * bumps) for _ in range(6)]
        p = self.palette
        for face in bm.faces:
            points, uvs = [], []
            for vert in face.verts:
                co = V(vert.co)
                bump = sum(a * math.sin(f * (co.x * d[0] + co.y * d[1] + co.z * d[2]) + ph) for d, f, ph, a in waves)
                co = co * (1.0 + bump)
                if flat_bottom is not None:
                    co.z = max(co.z, flat_bottom)
                s = 0.5 + math.atan2(co.y, co.x) / (2 * math.pi)
                t = 0.5 + 0.5 * max(-1.0, min(1.0, co.z))
                points.append(V(centre) + V((co.x * squash[0], co.y * squash[1], co.z * squash[2])) * radius)
                uvs.append(p.uv(swatch, s, t))
            # Keep one side of the seam so a face does not stretch over the whole swatch.
            us = [u for u, _ in uvs]
            if max(us) - min(us) > 0.5 * (SWATCH / TEXTURE_SIZE):
                left = min(us)
                uvs = [(left, v) for u, v in uvs]
            self.face(points, uvs)
        bm.free()

    def box(self, centre, size, swatch, rotation=0.0, top_swatch=None, jitter=None):
        """A brick, turned about z; the top face may use its own swatch (e.g. moss on peat)."""
        V = self.Vector
        hx, hy, hz = size[0] / 2, size[1] / 2, size[2] / 2
        c, s = math.cos(rotation), math.sin(rotation)

        def corner(x, y, z):
            if jitter is not None:
                x += jitter.uniform(-0.08, 0.08) * size[0]
                y += jitter.uniform(-0.08, 0.08) * size[1]
                z += jitter.uniform(-0.05, 0.05) * size[2]
            return V((centre[0] + x * c - y * s, centre[1] + x * s + y * c, centre[2] + z))

        corners = {(i, j, k): corner(i * hx, j * hy, k * hz) for i in (-1, 1) for j in (-1, 1) for k in (-1, 1)}
        faces = [
            ([(-1, -1, -1), (1, -1, -1), (1, -1, 1), (-1, -1, 1)], swatch),
            ([(1, -1, -1), (1, 1, -1), (1, 1, 1), (1, -1, 1)], swatch),
            ([(1, 1, -1), (-1, 1, -1), (-1, 1, 1), (1, 1, 1)], swatch),
            ([(-1, 1, -1), (-1, -1, -1), (-1, -1, 1), (-1, 1, 1)], swatch),
            ([(-1, -1, 1), (1, -1, 1), (1, 1, 1), (-1, 1, 1)], top_swatch or swatch),
            ([(-1, 1, -1), (1, 1, -1), (1, -1, -1), (-1, -1, -1)], swatch),
        ]
        square = [(0, 0), (1, 0), (1, 1), (0, 1)]
        for keys, sw in faces:
            self.face([corners[k] for k in keys], [self.palette.uv(sw, *st) for st in square])

    def flower(self, centre, facing, radius, petals, swatch, centre_swatch=None, cup=0.35, segments=2):
        """A star of petals around a small centre, opening towards facing."""
        V = self.Vector
        facing = V(facing).normalized()
        side = facing.orthogonal().normalized()
        other = facing.cross(side)
        for k in range(petals):
            angle = 2 * math.pi * k / petals
            out = (side * math.cos(angle) + other * math.sin(angle))
            direction = (out * math.cos(cup) + facing * math.sin(cup)).normalized()
            self.leaf(V(centre), direction, facing, radius, radius * 0.7, swatch, profile="petal", segments=segments, fold=0.15, droop=-0.1)
        if centre_swatch:
            self.blob(V(centre) + facing * radius * 0.1, radius * 0.25, centre_swatch, subdivisions=0)

    def bounds(self):
        points = [v.co for bm in self.meshes.values() for v in bm.verts]
        lo = self.Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
        hi = self.Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
        return lo, hi


def leaf_profile(profile, t):
    if profile == "lance":
        return math.sin(math.pi * min(t * 1.05, 1.0)) ** 0.8 * (1 - 0.25 * t)
    if profile == "ovate":
        return math.sin(math.pi * t ** 0.75)
    if profile == "round":
        return math.sqrt(max(0.0, math.sin(math.pi * t)))
    if profile == "blade":
        return (1 - t) ** 0.7 if t > 0.02 else 0.6
    if profile == "linear":
        return 0.9 * math.sin(math.pi * min(t * 1.02, 1.0)) ** 0.3
    if profile == "petal":
        return math.sin(math.pi * min(t, 1.0)) ** 0.6 * (0.5 + 0.5 * t)
    raise ValueError(profile)


def spread_directions(count, rng, tilt, jitter=0.25):
    """Directions fanned around z, tilt radians from vertical."""
    start = rng.uniform(0, 2 * math.pi)
    result = []
    for k in range(count):
        angle = start + 2 * math.pi * k / count + rng.uniform(-jitter, jitter)
        lean = tilt + rng.uniform(-0.15, 0.15)
        result.append((math.sin(lean) * math.cos(angle), math.sin(lean) * math.sin(angle), math.cos(lean)))
    return result


def curve(start, direction, length, bend, steps, gravity=(0.0, 0.0, -1.0)):
    """Points along a stem that starts in direction and bends towards gravity by bend (radians over its length)."""
    import mathutils
    V = mathutils.Vector
    point, direction, gravity = V(start), V(direction).normalized(), V(gravity).normalized()
    points = [point.copy()]
    step = length / steps
    for _ in range(steps):
        axis = direction.cross(gravity)
        if axis.length > 1e-5:
            direction = (mathutils.Matrix.Rotation(bend / steps, 3, axis.normalized()) @ direction).normalized()
        point = point + direction * step
        points.append(point.copy())
    return points


# ---------------------------------------------------------------------------------------------------------------- swamp

@model("bogbean", "Bog bean")
def build_bogbean(geo, rng, np):
    V = geo.Vector
    for direction in spread_directions(5, rng, 0.35):
        height = rng.uniform(0.22, 0.32)
        stalk = curve((0, 0, 0), direction, height, 0.5, 5)
        geo.tube(stalk, [0.006 - 0.003 * i / 5 for i in range(6)], "stalk", segments=5)
        tip = stalk[-1]
        heading = (stalk[-1] - stalk[-2]).normalized()
        flat = V((heading.x, heading.y, 0.0))
        flat = flat.normalized() if flat.length > 1e-3 else V((1, 0, 0))
        for k, turn in enumerate((-0.9, 0.0, 0.9)):
            c, s = math.cos(turn), math.sin(turn)
            leaflet = V((flat.x * c - flat.y * s, flat.x * s + flat.y * c, 0.15))
            geo.leaf(tip, leaflet, (0, 0, 1), rng.uniform(0.07, 0.09), rng.uniform(0.045, 0.055), "leaf", profile="ovate",
                     segments=6, fold=0.2, droop=0.25)
    # One flower stalk with a raceme of fringed, pink-backed white stars and pink buds on top.
    stalk = curve((0.01, 0.0, 0.0), (0.05, 0.02, 1.0), 0.38, 0.12, 6)
    geo.tube(stalk, [0.005] * 7, "stalk", segments=5)
    top = stalk[-1]
    for k in range(10):
        t = k / 9
        angle = k * 2.4
        out = V((math.cos(angle), math.sin(angle), 0.0))
        point = top - V((0, 0, 0.11 * (1 - t))) + out * 0.012
        if t < 0.75:
            geo.tube([point - out * 0.012, point + out * 0.004], [0.0015, 0.0015], "stalk", segments=3, cap=False)
            geo.flower(point + out * 0.006, (out * 0.8 + V((0, 0, 0.3))), 0.016, 5, "petal", "anther", cup=0.25)
        else:
            geo.blob(point, 0.006, "bud", subdivisions=1, squash=(1, 1, 1.4))


@model("labradortea", "Marsh Labrador tea")
def build_labradortea(geo, rng, np):
    V = geo.Vector
    tips = []
    for direction in spread_directions(9, rng, 0.5):
        length = rng.uniform(0.45, 0.7)
        stem = curve((0, 0, 0), direction, length, -0.25, 6, gravity=(0, 0, 1))
        geo.tube(stem, [0.014 * (1 - 0.6 * i / 6) for i in range(7)], "wood", segments=5)
        branches = [stem]
        # A side twig from the middle of each stem doubles the tips.
        side_start = stem[3]
        side_heading = (stem[3] - stem[2]).normalized()
        side_out = (side_heading.orthogonal().normalized() + side_heading).normalized()
        twig = curve(side_start, side_out, length * 0.45, -0.3, 3, gravity=(0, 0, 1))
        geo.tube(twig, [0.007, 0.006, 0.005, 0.004], "wood", segments=4)
        branches.append(twig)
        for branch in branches:
            for i in range(1, len(branch)):
                point = branch[i]
                heading = (branch[i] - branch[i - 1]).normalized()
                first = heading.orthogonal().normalized()
                second = heading.cross(first)
                for k in range(7):
                    angle = rng.uniform(0, 2 * math.pi)
                    out = first * math.cos(angle) + second * math.sin(angle)
                    direction_leaf = (out * 0.75 + heading * 0.55).normalized()
                    geo.leaf(point + heading * rng.uniform(-0.02, 0.02), direction_leaf, heading, rng.uniform(0.05, 0.07), 0.013,
                             "leaf", profile="linear", segments=3, fold=0.6, droop=0.2)
            tips.append((branch[-1], (branch[-1] - branch[-2]).normalized()))
    # Domed clusters of small white flowers at the tips: the part that is picked.
    geo.target = "fruit"
    for tip, heading in tips:
        for k in range(10):
            angle = rng.uniform(0, 2 * math.pi)
            spread = rng.uniform(0.0, 0.04)
            side = heading.orthogonal().normalized()
            other = heading.cross(side)
            offset = (side * math.cos(angle) + other * math.sin(angle)) * spread
            point = tip + heading * (0.025 + 0.025 * (1 - spread / 0.04)) + offset
            geo.tube([tip, point], [0.0015, 0.0015], "stalk", segments=3, cap=False)
            geo.blob(point, 0.011, "flower", subdivisions=1)
    geo.target = "main"


@model("cattail", "Cattail")
def build_cattail(geo, rng, np):
    V = geo.Vector
    for direction in spread_directions(15, rng, 0.22, jitter=0.6):
        length = rng.uniform(0.9, 1.4)
        geo.leaf(V((direction[0] * 0.04, direction[1] * 0.04, 0)), direction, (0, 0, 1), length, 0.04, "blade",
                 profile="blade", segments=8, fold=0.35, droop=rng.uniform(0.15, 0.4), twist=rng.uniform(-0.6, 0.6))
    for k in range(3):
        lean = (rng.uniform(-0.08, 0.08), rng.uniform(-0.08, 0.08), 1.0)
        height = rng.uniform(1.25, 1.55)
        stem = curve((rng.uniform(-0.03, 0.03), rng.uniform(-0.03, 0.03), 0), lean, height, 0.05, 6)
        geo.tube(stem, [0.01 - 0.004 * i / 6 for i in range(7)], "stem", segments=5)
        top = stem[-1]
        heading = (stem[-1] - stem[-2]).normalized()
        spike = [top + heading * (0.022 * i) for i in range(9)]
        radii = [0.008, 0.02, 0.024, 0.025, 0.025, 0.025, 0.024, 0.02, 0.008]
        geo.tube(spike, radii, "spike", segments=8)
        geo.tube([spike[-1], spike[-1] + heading * 0.12], [0.003, 0.001], "stem", segments=4)


@model("meadowsweet", "Meadowsweet")
def build_meadowsweet(geo, rng, np):
    V = geo.Vector
    for k in range(3):
        lean = (rng.uniform(-0.12, 0.12), rng.uniform(-0.12, 0.12), 1.0)
        height = rng.uniform(0.95, 1.15)
        stem = curve((rng.uniform(-0.04, 0.04), rng.uniform(-0.04, 0.04), 0), lean, height, 0.1, 8)
        geo.tube(stem, [0.013 - 0.006 * i / 8 for i in range(9)], "stem", segments=5)
        # Pinnate leaves on the lower stem.
        for i in (1, 2, 3, 5):
            angle = rng.uniform(0, 2 * math.pi)
            out = V((math.cos(angle), math.sin(angle), 0.35)).normalized()
            rachis = curve(stem[i], out, 0.3, 0.6, 4)
            geo.tube(rachis, [0.004] * 5, "stalk", segments=3, cap=False)
            for j in range(1, 5):
                heading = (rachis[j] - rachis[j - 1]).normalized()
                side = heading.cross(V((0, 0, 1))).normalized()
                for sign in (-1, 1):
                    geo.leaf(rachis[j], (side * sign + heading * 0.4), (0, 0, 1), 0.1 - 0.012 * j, 0.05, "leaf",
                             profile="ovate", segments=4, fold=0.25, droop=0.2)
            geo.leaf(rachis[-1], (rachis[-1] - rachis[-2]), (0, 0, 1), 0.11, 0.08, "leaf", profile="ovate", segments=4, fold=0.25, droop=0.2)
        # Frothy cymes of cream flowers at the top.
        top = stem[-1]
        for j in range(9):
            angle = rng.uniform(0, 2 * math.pi)
            out = V((math.cos(angle) * 0.6, math.sin(angle) * 0.6, 1.0)).normalized()
            branch = curve(top - V((0, 0, rng.uniform(0.0, 0.12))), out, rng.uniform(0.06, 0.12), 0.2, 3)
            geo.tube(branch, [0.0018] * 4, "stalk", segments=3, cap=False)
            for n in range(6):
                point = branch[-1] + V((rng.normal(0, 0.018), rng.normal(0, 0.018), rng.normal(0.005, 0.012)))
                geo.blob(point, rng.uniform(0.007, 0.011), "flower", subdivisions=0)


@model("sphagnum", "Sphagnum moss cushion")
def build_sphagnum(geo, rng, np):
    geo.blob((0, 0, -0.02), 0.22, "moss", subdivisions=4, squash=(1.0, 0.85, 0.42), rng=rng, bumps=0.06, flat_bottom=-0.1)
    for k in range(5):
        angle = rng.uniform(0, 2 * math.pi)
        distance = rng.uniform(0.12, 0.2)
        geo.blob((math.cos(angle) * distance, math.sin(angle) * distance * 0.85, -0.01), rng.uniform(0.07, 0.1), "mossred",
                 subdivisions=3, squash=(1.0, 1.0, 0.55), rng=rng, bumps=0.08, flat_bottom=-0.1)


@model("bogiron", "Lumps of bog iron in the mud")
def build_bogiron(geo, rng, np):
    for k in range(4):
        angle = rng.uniform(0, 2 * math.pi)
        distance = 0.0 if k == 0 else rng.uniform(0.09, 0.15)
        size = rng.uniform(0.07, 0.11) if k else 0.13
        geo.blob((math.cos(angle) * distance, math.sin(angle) * distance, 0.0), size, "rust", subdivisions=3,
                 squash=(1.0, rng.uniform(0.75, 1.0), 0.6), rng=rng, bumps=0.2, flat_bottom=-0.1)


@model("bogironlump", "A lump of bog iron")
def build_bogironlump(geo, rng, np):
    geo.blob((0, 0, 0), 0.1, "rust", subdivisions=3, squash=(1.0, 0.8, 0.7), rng=rng, bumps=0.14)


@model("peatstack", "Cut peat drying in a stack")
def build_peatstack(geo, rng, np):
    layers = [(0.0, 0.0, 3), (0.06, 0.5, 2)]
    for z, turn, count in layers:
        for k in range(count):
            offset = (k - (count - 1) / 2) * 0.13
            c, s = math.cos(turn), math.sin(turn)
            geo.box((offset * c, offset * s, 0.03 + z), (0.12, 0.3, 0.06), "peat", rotation=turn + rng.uniform(-0.08, 0.08),
                    top_swatch="peattop", jitter=rng)


@model("peatbrick", "A brick of cut peat")
def build_peatbrick(geo, rng, np):
    geo.box((0, 0, 0.03), (0.12, 0.3, 0.06), "peat", top_swatch="peattop", jitter=rng)


# ---------------------------------------------------------------------------------------------------------------- mountains

def needle_spray(geo, rng, point, heading, count, length, swatch):
    """A tuft of stiff needles pointing out and forward from a twig, as on juniper."""
    side = heading.orthogonal().normalized()
    other = heading.cross(side)
    for k in range(count):
        angle = 2 * math.pi * k / count + rng.uniform(-0.3, 0.3)
        out = side * math.cos(angle) + other * math.sin(angle)
        geo.leaf(point, (out * 0.8 + heading * 0.6), heading, length * rng.uniform(0.8, 1.1), length * 0.24, swatch,
                 profile="linear", segments=2, fold=0.3, droop=0.0)


@model("juniper", "Common juniper")
def build_juniper(geo, rng, np):
    V = geo.Vector
    berries = []
    for direction in spread_directions(6, rng, 0.35):
        length = rng.uniform(0.6, 0.95)
        stem = curve((0, 0, 0), direction, length, -0.15, 6, gravity=(0, 0, 1))
        geo.tube(stem, [0.016 * (1 - 0.7 * i / 6) for i in range(7)], "wood", segments=5)
        for i in range(1, 7):
            heading = (stem[i] - stem[i - 1]).normalized()
            for k in range(2):
                angle = rng.uniform(0, 2 * math.pi)
                side = heading.orthogonal().normalized()
                out = (side * math.cos(angle) + heading.cross(side) * math.sin(angle))
                twig = curve(stem[i], (out * 0.8 + heading * 0.5), rng.uniform(0.08, 0.16), -0.2, 3, gravity=(0, 0, 1))
                geo.tube(twig, [0.004, 0.003, 0.0025, 0.002], "wood", segments=3, cap=False)
                for j in range(1, 4):
                    twig_heading = (twig[j] - twig[j - 1]).normalized()
                    needle_spray(geo, rng, twig[j], twig_heading, 5, 0.055, "needle")
                if rng.random() < 0.8:
                    berries.append(twig[2] + V((rng.normal(0, 0.015), rng.normal(0, 0.015), rng.normal(0, 0.012))))
            needle_spray(geo, rng, stem[i], heading, 6, 0.06, "needle")
    geo.target = "fruit"
    for point in berries:
        geo.blob(point, 0.012, "berry", subdivisions=2)
    geo.target = "main"


@model("angelica", "Angelica")
def build_angelica(geo, rng, np):
    V = geo.Vector
    # Big, twice-divided leaves around the foot.
    for direction in spread_directions(5, rng, 0.9):
        stalk = curve((0, 0, 0.02), direction, 0.45, 0.5, 4)
        geo.tube(stalk, [0.012, 0.01, 0.008, 0.006, 0.005], "stem", segments=5, cap=False)
        for j in (2, 3, 4):
            heading = (stalk[j] - stalk[j - 1]).normalized()
            side = heading.cross(V((0, 0, 1))).normalized()
            for sign in (-1, 1):
                geo.leaf(stalk[j], (side * sign + heading * 0.5), (0, 0, 1), 0.16, 0.08, "leaf", profile="ovate",
                         segments=4, fold=0.3, droop=0.25)
        geo.leaf(stalk[-1], (stalk[-1] - stalk[-2]), (0, 0, 1), 0.18, 0.1, "leaf", profile="ovate", segments=4, fold=0.3, droop=0.25)
    # One stout, hollow-looking stem with three globes of flowers.
    stem = curve((0, 0, 0), (0.03, 0.02, 1), 1.35, 0.08, 8)
    geo.tube(stem, [0.03 - 0.015 * i / 8 for i in range(9)], "stem", segments=7)
    heads = [(stem[-1], (stem[-1] - stem[-2]).normalized(), 0.16)]
    for i, angle in ((5, 0.4), (6, 3.6)):
        out = V((math.cos(angle), math.sin(angle), 1.1)).normalized()
        branch = curve(stem[i], out, 0.32, -0.25, 4, gravity=(0, 0, 1))
        geo.tube(branch, [0.012, 0.011, 0.01, 0.009, 0.008], "stem", segments=5)
        heads.append((branch[-1], (branch[-1] - branch[-2]).normalized(), 0.11))
    for centre_point, heading, radius in heads:
        side = heading.orthogonal().normalized()
        other = heading.cross(side)
        for k in range(26):
            # Rays spread over a dome, each ending in a little ball of green-white flowers.
            phi = math.acos(1 - 1.4 * (k + 0.5) / 26)
            theta = k * 2.4
            ray = (heading * math.cos(phi) + (side * math.cos(theta) + other * math.sin(theta)) * math.sin(phi)).normalized()
            tip = centre_point + ray * radius
            geo.tube([centre_point, tip], [0.0025, 0.002], "ray", segments=3, cap=False)
            geo.blob(tip, radius * 0.16, "flower", subdivisions=2)


@model("icelandmoss", "Iceland moss")
def build_icelandmoss(geo, rng, np):
    V = geo.Vector
    for k in range(34):
        angle = rng.uniform(0, 2 * math.pi)
        distance = rng.uniform(0.0, 0.12)
        base = V((math.cos(angle) * distance, math.sin(angle) * distance, 0.0))
        up = V((rng.normal(0, 0.35), rng.normal(0, 0.35), 1.0)).normalized()
        # Each lobe forks once into two curled straps.
        mid = base + up * rng.uniform(0.03, 0.05)
        geo.leaf(base, up, (math.cos(angle), math.sin(angle), 0), (mid - base).length, 0.022, "lobe", profile="linear",
                 segments=2, fold=0.9, droop=0.0)
        for sign in (-1, 1):
            side = up.cross(V((math.cos(angle), math.sin(angle), 0))).normalized()
            geo.leaf(mid, (up + side * 0.6 * sign), (math.cos(angle), math.sin(angle), 0), rng.uniform(0.035, 0.05), 0.018,
                     "lobe", profile="lance", segments=3, fold=1.0, droop=-0.3)


def snag(geo, rng, length, radius):
    """A weathered dead branch lying on the ground, with a few broken side twigs; returns points along it."""
    V = geo.Vector
    points = [V((-length / 2 + length * i / 6, rng.normal(0, 0.02), radius * 0.6 + 0.01 * math.sin(i))) for i in range(7)]
    geo.tube(points, [radius * (1 - 0.4 * i / 6) for i in range(7)], "deadwood", segments=6)
    twigs = []
    for i in (1, 3, 4):
        angle = rng.uniform(-1.2, 1.2) + (math.pi if i % 2 else 0)
        out = V((0.4, math.sin(angle), abs(math.cos(angle)) * 0.8 + 0.3)).normalized()
        twig = curve(points[i], out, rng.uniform(0.12, 0.25), 0.2, 3)
        geo.tube(twig, [radius * 0.45, radius * 0.35, radius * 0.25, radius * 0.18], "deadwood", segments=4)
        twigs.append(twig)
    return points, twigs


def lichen_tuft(geo, rng, point, normal, size, swatch):
    """A small shrubby tuft of forked lichen branches."""
    V = geo.Vector
    normal = V(normal).normalized()
    for k in range(5):
        direction = (normal + V((rng.normal(0, 0.6), rng.normal(0, 0.6), rng.normal(0, 0.6)))).normalized()
        tip = point + direction * size * rng.uniform(0.6, 1.0)
        geo.tube([point, tip], [size * 0.12, size * 0.07], swatch, segments=3)
        for j in range(2):
            fork = (direction + V((rng.normal(0, 0.7), rng.normal(0, 0.7), rng.normal(0, 0.7)))).normalized()
            geo.tube([tip, tip + fork * size * 0.45], [size * 0.07, size * 0.04], swatch, segments=3)


@model("wolflichen", "Wolf lichen on a dead branch")
def build_wolflichen(geo, rng, np):
    V = geo.Vector
    points, twigs = snag(geo, rng, 0.75, 0.035)
    geo.target = "fruit"
    for i in range(1, 6):
        for k in range(3):
            angle = rng.uniform(-0.3, math.pi + 0.3)
            normal = V((0, math.cos(angle), math.sin(angle)))
            lichen_tuft(geo, rng, points[i] + normal * 0.028, normal, rng.uniform(0.07, 0.1), "lichen")
    for twig in twigs:
        lichen_tuft(geo, rng, twig[2], (twig[2] - twig[1]), 0.08, "lichen")
    geo.target = "main"


@model("wolflichenitem", "A handful of wolf lichen")
def build_wolflichenitem(geo, rng, np):
    V = geo.Vector
    for k in range(6):
        angle = rng.uniform(0, 2 * math.pi)
        point = V((math.cos(angle) * 0.03, math.sin(angle) * 0.03, 0.0))
        lichen_tuft(geo, rng, point, (math.cos(angle) * 0.4, math.sin(angle) * 0.4, 1), 0.06, "lichen")


@model("mountainsorrel", "Mountain sorrel")
def build_mountainsorrel(geo, rng, np):
    V = geo.Vector
    for direction in spread_directions(9, rng, 1.0, jitter=0.4):
        stalk = curve((0, 0, 0.01), direction, rng.uniform(0.08, 0.13), 0.6, 3)
        geo.tube(stalk, [0.0035, 0.003, 0.0028, 0.0025], "stalk", segments=4, cap=False)
        heading = (stalk[-1] - stalk[-2]).normalized()
        flat = V((heading.x, heading.y, 0.25)).normalized()
        geo.leaf(stalk[-1], flat, (0, 0, 1), rng.uniform(0.05, 0.07), rng.uniform(0.06, 0.075), "leaf", profile="round",
                 segments=5, fold=0.25, droop=0.2)
    # Upright stems with loose red panicles of flat, winged fruits.
    for k in range(3):
        lean = (rng.normal(0, 0.15), rng.normal(0, 0.15), 1.0)
        stem = curve((rng.normal(0, 0.01), rng.normal(0, 0.01), 0), lean, rng.uniform(0.24, 0.32), 0.15, 5)
        geo.tube(stem, [0.003] * 6, "stalk", segments=4)
        for i in (3, 4, 5):
            for n in range(5):
                offset = V((rng.normal(0, 0.012), rng.normal(0, 0.012), rng.uniform(-0.02, 0.02)))
                point = stem[i] + offset
                geo.tube([stem[i], point], [0.0012, 0.0012], "stalk", segments=3, cap=False)
                geo.blob(point, 0.0055, "fruit", subdivisions=1, squash=(1.0, 1.0, 0.35))


@model("rocklichen", "A stone crusted with rock lichen")
def build_rocklichen(geo, rng, np):
    V = geo.Vector
    geo.blob((0, 0, 0.0), 0.2, "stone", subdivisions=3, squash=(1.0, 0.8, 0.55), rng=rng, bumps=0.1, flat_bottom=-0.2)
    geo.target = "fruit"
    # Flat rosettes of crust lichen hugging the top of the stone.
    for k in range(14):
        angle = rng.uniform(0, 2 * math.pi)
        distance = rng.uniform(0.0, 0.14)
        x, y = math.cos(angle) * distance, math.sin(angle) * distance * 0.8
        z = 0.12 * math.sqrt(max(0.0, 1 - (distance / 0.2) ** 2)) + 0.01
        geo.blob((x, y, z), rng.uniform(0.05, 0.08), "crust", subdivisions=2, squash=(1.0, 1.0, 0.15))
    geo.target = "main"


@model("rocklichenitem", "Scraped rock lichen")
def build_rocklichenitem(geo, rng, np):
    for k in range(5):
        angle = rng.uniform(0, 2 * math.pi)
        geo.blob((math.cos(angle) * 0.04, math.sin(angle) * 0.04, k * 0.006), rng.uniform(0.03, 0.045), "crust",
                 subdivisions=2, squash=(1.0, 0.9, 0.18), rng=rng, bumps=0.15)


@model("whetstone", "Whetstone")
def build_whetstone(geo, rng, np):
    points = [(-0.11 + 0.22 * i / 6, 0.0, 0.012) for i in range(7)]
    radii = [0.01, 0.014, 0.015, 0.015, 0.015, 0.014, 0.011]
    geo.tube(points, radii, "slate", segments=6)
    geo.tube([(-0.115, 0.0, 0.012), (-0.135, 0.0, 0.03)], [0.003, 0.003], "cord", segments=4)


@model("oilflask", "Small clay flask")
def build_oilflask(geo, rng, np):
    profile = [(0.0, 0.0), (0.035, 0.003), (0.045, 0.03), (0.042, 0.06), (0.025, 0.085), (0.012, 0.095), (0.012, 0.115)]
    points = [(0, 0, z) for _, z in profile]
    geo.tube(points, [r for r, _ in profile], "clay", segments=10)
    geo.tube([(0, 0, 0.11), (0, 0, 0.13)], [0.013, 0.011], "cork", segments=8)
    geo.tube([(0.0125, 0, 0.1), (0.03, 0, 0.08), (0.04, 0, 0.05)], [0.0025, 0.0025, 0.0025], "cord", segments=4)


# ---------------------------------------------------------------------------------------------------------------- plains

def pinnate(geo, rng, start, direction, length, pairs, leaflet, swatch, stalk_swatch, droop=0.4):
    """A leaf stalk with pairs of leaflets and one at the tip."""
    V = geo.Vector
    rachis = curve(start, direction, length, droop, max(2, pairs))
    geo.tube(rachis, [0.0025] * len(rachis), stalk_swatch, segments=3, cap=False)
    for j in range(1, len(rachis)):
        heading = (rachis[j] - rachis[j - 1]).normalized()
        side = heading.cross(V((0, 0, 1)))
        side = side.normalized() if side.length > 1e-3 else heading.orthogonal().normalized()
        for sign in (-1, 1):
            geo.leaf(rachis[j], (side * sign + heading * 0.5), (0, 0, 1), leaflet, leaflet * 0.55, swatch, profile="ovate",
                     segments=3, fold=0.25, droop=0.15)
    geo.leaf(rachis[-1], (rachis[-1] - rachis[-2]), (0, 0, 1), leaflet * 1.1, leaflet * 0.6, swatch, profile="ovate", segments=3, fold=0.25, droop=0.15)


@model("rosehip", "Dog rose with hips")
def build_rosehip(geo, rng, np):
    V = geo.Vector
    hips = []
    for direction in spread_directions(9, rng, 0.45):
        length = rng.uniform(0.8, 1.2)
        cane = curve((0, 0, 0), direction, length, 0.55, 7)
        geo.tube(cane, [0.012 * (1 - 0.6 * i / 7) for i in range(8)], "cane", segments=5)
        for i in range(2, 8):
            heading = (cane[i] - cane[i - 1]).normalized()
            side = heading.orthogonal().normalized()
            # Thorns.
            for k in range(2):
                angle = rng.uniform(0, 2 * math.pi)
                out = side * math.cos(angle) + heading.cross(side) * math.sin(angle)
                geo.tube([cane[i] + out * 0.006, cane[i] + out * 0.02 - heading * 0.006], [0.003, 0.0005], "thorn", segments=3)
            angle = rng.uniform(0, 2 * math.pi)
            out = (side * math.cos(angle) + heading.cross(side) * math.sin(angle) + V((0, 0, 0.3))).normalized()
            pinnate(geo, rng, cane[i], out, 0.14, 2, 0.055, "leaf", "cane", droop=0.3)
            if i >= 4:
                hips.append((cane[i] + out * 0.03, out))
    geo.target = "fruit"
    for point, out in hips:
        for k in range(rng.integers(1, 3)):
            hang = (out + V((rng.normal(0, 0.3), rng.normal(0, 0.3), -0.8))).normalized()
            tip = point + hang * 0.035
            geo.tube([point, tip], [0.0018, 0.0015], "cane", segments=3, cap=False)
            geo.blob(tip + hang * 0.014, 0.017, "hip", subdivisions=2, squash=(1.0, 1.0, 1.35))
            geo.blob(tip + hang * 0.035, 0.005, "sepal", subdivisions=1)
    geo.target = "main"


def corymb(geo, rng, top, radius, count, swatch, size):
    """A flat-topped head of tiny flowers."""
    V = geo.Vector
    for k in range(count):
        r = radius * math.sqrt(rng.random())
        angle = rng.uniform(0, 2 * math.pi)
        point = top + V((math.cos(angle) * r, math.sin(angle) * r, rng.normal(0, size * 0.3)))
        geo.blob(point, size, swatch, subdivisions=1, squash=(1.0, 1.0, 0.6))


@model("yarrow", "Yarrow")
def build_yarrow(geo, rng, np):
    V = geo.Vector
    # Feathery, finely cut leaves around the foot.
    for direction in spread_directions(8, rng, 0.9, jitter=0.4):
        rachis = curve((0, 0, 0.01), direction, rng.uniform(0.12, 0.18), 0.6, 6)
        geo.tube(rachis, [0.002] * 7, "leaf", segments=3, cap=False)
        for j in range(1, 7):
            heading = (rachis[j] - rachis[j - 1]).normalized()
            side = heading.cross(V((0, 0, 1))).normalized()
            for sign in (-1, 1):
                geo.leaf(rachis[j], (side * sign + heading * 0.3), (0, 0, 1), 0.02, 0.005, "leaf", profile="linear", segments=2, fold=0.2, droop=0.1)
    for k in range(3):
        lean = (rng.normal(0, 0.12), rng.normal(0, 0.12), 1.0)
        stem = curve((rng.normal(0, 0.015), rng.normal(0, 0.015), 0), lean, rng.uniform(0.38, 0.5), 0.1, 6)
        geo.tube(stem, [0.0035] * 7, "stem", segments=4)
        for i in (2, 4):
            heading = (stem[i] - stem[i - 1]).normalized()
            geo.leaf(stem[i], (heading.orthogonal() + heading).normalized(), (0, 0, 1), 0.05, 0.01, "leaf", profile="linear", segments=3, fold=0.2, droop=0.2)
        top = stem[-1]
        for j in range(5):
            angle = j * 1.256 + rng.normal(0, 0.2)
            tip = top + V((math.cos(angle) * 0.02, math.sin(angle) * 0.02, 0.012))
            geo.tube([top - V((0, 0, 0.02)), tip], [0.0015, 0.0012], "stem", segments=3, cap=False)
        corymb(geo, rng, top + V((0, 0, 0.015)), 0.04, 45, "flower", 0.0045)


@model("caraway", "Caraway")
def build_caraway(geo, rng, np):
    V = geo.Vector
    for direction in spread_directions(6, rng, 0.9, jitter=0.4):
        rachis = curve((0, 0, 0.01), direction, rng.uniform(0.12, 0.16), 0.5, 5)
        geo.tube(rachis, [0.002] * 6, "leaf", segments=3, cap=False)
        for j in range(1, 6):
            heading = (rachis[j] - rachis[j - 1]).normalized()
            side = heading.cross(V((0, 0, 1))).normalized()
            for sign in (-1, 1):
                for n in range(2):
                    geo.leaf(rachis[j], (side * sign + heading * (0.2 + n * 0.5)), (0, 0, 1), 0.016, 0.003, "leaf", profile="linear", segments=2, fold=0.0, droop=0.1)
    stem = curve((0, 0, 0), (0.03, 0.0, 1.0), 0.45, 0.05, 6)
    geo.tube(stem, [0.004] * 7, "stem", segments=4)
    branches = [stem]
    for i, angle in ((3, 0.5), (4, 2.6), (5, 4.4)):
        out = V((math.cos(angle), math.sin(angle), 1.3)).normalized()
        branch = curve(stem[i], out, rng.uniform(0.15, 0.22), -0.1, 3, gravity=(0, 0, 1))
        geo.tube(branch, [0.0025] * 4, "stem", segments=3)
        branches.append(branch)
    for branch in branches:
        top = branch[-1]
        for j in range(8):
            angle = j * 0.785 + rng.normal(0, 0.15)
            ray_tip = top + V((math.cos(angle) * 0.035, math.sin(angle) * 0.035, 0.03))
            geo.tube([top, ray_tip], [0.0012, 0.001], "stem", segments=3, cap=False)
            for n in range(5):
                a2 = rng.uniform(0, 2 * math.pi)
                point = ray_tip + V((math.cos(a2) * 0.007, math.sin(a2) * 0.007, 0.006))
                geo.blob(point, 0.0028, "flower", subdivisions=1)


@model("woad", "Woad in flower")
def build_woad(geo, rng, np):
    V = geo.Vector
    for direction in spread_directions(8, rng, 1.0, jitter=0.4):
        geo.leaf((0, 0, 0.02), direction, (0, 0, 1), rng.uniform(0.16, 0.22), 0.05, "leaf", profile="lance", segments=5, fold=0.25, droop=0.35)
    stem = curve((0, 0, 0), (0.02, 0.03, 1.0), 0.75, 0.08, 7)
    geo.tube(stem, [0.006 - 0.002 * i / 7 for i in range(8)], "stem", segments=5)
    for i in (1, 2, 3):
        heading = (stem[i] - stem[i - 1]).normalized()
        out = heading.orthogonal().normalized()
        geo.leaf(stem[i], (out + heading * 1.5), (0, 0, 1), 0.09, 0.025, "leaf", profile="lance", segments=4, fold=0.2, droop=0.1)
    for i in range(4, 8):
        for k in range(2):
            angle = rng.uniform(0, 2 * math.pi)
            out = V((math.cos(angle), math.sin(angle), 1.2)).normalized()
            branch = curve(stem[i], out, rng.uniform(0.1, 0.18), -0.2, 3, gravity=(0, 0, 1))
            geo.tube(branch, [0.0022] * 4, "stem", segments=3, cap=False)
            for n in range(9):
                point = branch[-1] + V((rng.normal(0, 0.02), rng.normal(0, 0.02), rng.normal(0.01, 0.015)))
                geo.blob(point, 0.006, "flower", subdivisions=1)
    corymb(geo, rng, stem[-1] + V((0, 0, 0.01)), 0.035, 18, "flower", 0.006)


@model("madder", "Madder")
def build_madder(geo, rng, np):
    V = geo.Vector
    for direction in spread_directions(6, rng, 0.9, jitter=0.5):
        stem = curve((0, 0, 0.01), direction, rng.uniform(0.4, 0.6), 0.7, 6)
        geo.tube(stem, [0.0035] * 7, "stem", segments=4, cap=False)
        for i in range(1, 7):
            heading = (stem[i] - stem[i - 1]).normalized()
            side = heading.orthogonal().normalized()
            other = heading.cross(side)
            # Whorls of narrow leaves at each node.
            for k in range(5):
                angle = 2 * math.pi * k / 5
                out = side * math.cos(angle) + other * math.sin(angle)
                geo.leaf(stem[i], (out + heading * 0.2), heading, 0.045, 0.012, "leaf", profile="lance", segments=3, fold=0.15, droop=0.2)
            if i >= 4:
                for n in range(4):
                    point = stem[i] + V((rng.normal(0, 0.015), rng.normal(0, 0.015), 0.01 + abs(rng.normal(0, 0.01))))
                    geo.blob(point, 0.0035, "flower", subdivisions=1)


@model("madderroot", "Madder roots")
def build_madderroot(geo, rng, np):
    V = geo.Vector
    for k in range(6):
        angle = rng.uniform(0, 2 * math.pi)
        direction = V((math.cos(angle), math.sin(angle), rng.uniform(-0.15, 0.15)))
        root = curve((0, 0, 0.012), direction, rng.uniform(0.12, 0.2), rng.uniform(-0.6, 0.6), 6, gravity=(0, 0, 1))
        geo.tube(root, [0.006 * (1 - 0.6 * i / 6) for i in range(7)], "root", segments=5)


@model("henbane", "Henbane in flower")
def build_henbane(geo, rng, np):
    V = geo.Vector
    for direction in spread_directions(7, rng, 1.0, jitter=0.4):
        geo.leaf((0, 0, 0.02), direction, (0, 0, 1), rng.uniform(0.14, 0.2), 0.08, "leaf", profile="ovate", segments=5, fold=0.3, droop=0.3)
    for k in range(2):
        lean = (rng.normal(0, 0.15), rng.normal(0, 0.15), 1.0)
        stem = curve((rng.normal(0, 0.02), rng.normal(0, 0.02), 0), lean, rng.uniform(0.45, 0.6), 0.25, 6)
        geo.tube(stem, [0.007 - 0.003 * i / 6 for i in range(7)], "stem", segments=5)
        for i in range(2, 7):
            heading = (stem[i] - stem[i - 1]).normalized()
            out = heading.orthogonal().normalized()
            angle = i * 2.4
            out = (out * math.cos(angle) + heading.cross(out) * math.sin(angle)).normalized()
            geo.leaf(stem[i], (out + heading * 0.4), heading, 0.08, 0.04, "leaf", profile="ovate", segments=4, fold=0.3, droop=0.2)
            # A pale funnel flower with a purple throat in the leaf axil.
            mouth = stem[i] + out * 0.03 + heading * 0.02
            facing = (out + heading * 0.6).normalized()
            geo.tube([mouth - facing * 0.025, mouth], [0.004, 0.012], "flower", segments=7, cap=False)
            geo.blob(mouth - facing * 0.004, 0.006, "throat", subdivisions=1, squash=(1, 1, 0.5))


@model("ergot", "Ergot")
def build_ergot(geo, rng, np):
    V = geo.Vector
    for k in range(7):
        angle = rng.uniform(0, 2 * math.pi)
        direction = V((math.cos(angle) * 0.6, math.sin(angle) * 0.6, rng.uniform(0.2, 0.9))).normalized()
        spur = curve((math.cos(angle) * 0.008, math.sin(angle) * 0.008, 0.004), direction, rng.uniform(0.03, 0.045), 0.6, 4)
        geo.tube(spur, [0.004, 0.0045, 0.004, 0.003, 0.0012], "spur", segments=5)


@model("hops", "Wild hops on a dead stake")
def build_hops(geo, rng, np):
    V = geo.Vector
    geo.tube([(0, 0, 0), (0.02, 0.0, 0.8), (0.03, 0.01, 1.55)], [0.03, 0.025, 0.02], "stake", segments=6)
    geo.target = "main"
    cones = []
    for vine in range(2):
        phase = vine * math.pi
        points = []
        for i in range(40):
            t = i / 39
            z = 0.05 + t * 1.45
            angle = phase + t * 9.5
            centre = V((0.02 * t * 1.5, 0.005 * t, z))
            points.append(centre + V((math.cos(angle) * 0.04, math.sin(angle) * 0.04, 0.0)))
        geo.tube(points, [0.004] * len(points), "vine", segments=4, cap=False)
        for i in range(3, 40, 3):
            heading = (points[i] - points[i - 1]).normalized()
            out = (points[i] - V((points[i].x * 0.0, 0.0, points[i].z))).normalized()
            out = V((out.x, out.y, 0.3)).normalized()
            geo.leaf(points[i], out, (0, 0, 1), rng.uniform(0.12, 0.16), 0.13, "leaf", profile="round", segments=4, fold=0.3, droop=0.3)
            if i > 12:
                cones.append((points[i], out))
    geo.target = "fruit"
    for point, out in cones:
        for k in range(2):
            hang = (out * 0.6 + V((rng.normal(0, 0.3), rng.normal(0, 0.3), -0.8))).normalized()
            tip = point + hang * 0.05
            geo.tube([point, tip], [0.0018, 0.0018], "vine", segments=3, cap=False)
            geo.blob(tip + hang * 0.025, 0.02, "cone", subdivisions=2, squash=(0.85, 0.85, 1.5))
    geo.target = "main"


@model("hopcones", "A handful of hop cones")
def build_hopcones(geo, rng, np):
    for k in range(6):
        angle = rng.uniform(0, 2 * math.pi)
        distance = 0.0 if k == 0 else rng.uniform(0.02, 0.035)
        geo.blob((math.cos(angle) * distance, math.sin(angle) * distance, 0.012 + (0.01 if k == 0 else 0.0)), 0.02, "cone",
                 subdivisions=2, squash=(0.85, 0.85, 1.4))


@model("milkpail", "Pail of lox milk")
def build_milkpail(geo, rng, np):
    profile = [(0.075, 0.0), (0.08, 0.004), (0.085, 0.06), (0.09, 0.12), (0.092, 0.13)]
    geo.tube([(0, 0, z) for _, z in profile], [r for r, _ in profile], "wood", segments=14, cap=False)
    geo.blob((0, 0, 0.0), 0.075, "wood", subdivisions=2, squash=(1.0, 1.0, 0.02))
    geo.blob((0, 0, 0.112), 0.087, "milk", subdivisions=3, squash=(1.0, 1.0, 0.02))
    for z in (0.03, 0.1):
        geo.tube([(0, 0, z - 0.004), (0, 0, z + 0.004)], [0.087 + z * 0.05, 0.087 + z * 0.05], "band", segments=14, cap=False)
    handle = [(-0.09, 0, 0.12), (-0.07, 0, 0.19), (0.0, 0, 0.22), (0.07, 0, 0.19), (0.09, 0, 0.12)]
    geo.tube(handle, [0.005] * 5, "band", segments=4, cap=False)


SWATCHES = {
    "bogbean": {
        "leaf": {"bottom": (0.12, 0.26, 0.08), "top": (0.3, 0.48, 0.16)},
        "stalk": {"bottom": (0.32, 0.18, 0.14), "top": (0.25, 0.42, 0.14)},
        "petal": {"bottom": (0.85, 0.55, 0.62), "top": (0.97, 0.95, 0.94), "noise": 0.05},
        "anther": {"bottom": (0.45, 0.2, 0.25), "top": (0.6, 0.3, 0.32)},
        "bud": {"bottom": (0.8, 0.42, 0.5), "top": (0.92, 0.6, 0.66)},
    },
    "labradortea": {
        "wood": {"bottom": (0.24, 0.16, 0.11), "top": (0.38, 0.26, 0.17), "streaks": 0.4},
        "leaf": {"bottom": (0.1, 0.18, 0.07), "top": (0.2, 0.3, 0.1)},
        "stalk": {"bottom": (0.45, 0.4, 0.25), "top": (0.55, 0.5, 0.35)},
        "flower": {"bottom": (0.88, 0.88, 0.82), "top": (0.98, 0.98, 0.95), "noise": 0.05},
    },
    "cattail": {
        "blade": {"bottom": (0.2, 0.32, 0.12), "top": (0.55, 0.58, 0.3), "streaks": 0.3},
        "stem": {"bottom": (0.25, 0.35, 0.15), "top": (0.4, 0.45, 0.22)},
        "spike": {"bottom": (0.2, 0.11, 0.05), "top": (0.28, 0.16, 0.08), "noise": 0.25, "cells": (8, 4, 2)},
    },
    "meadowsweet": {
        "stem": {"bottom": (0.4, 0.15, 0.12), "top": (0.35, 0.3, 0.16)},
        "stalk": {"bottom": (0.3, 0.38, 0.16), "top": (0.4, 0.45, 0.2)},
        "leaf": {"bottom": (0.1, 0.22, 0.08), "top": (0.22, 0.36, 0.13)},
        "flower": {"bottom": (0.88, 0.84, 0.66), "top": (0.98, 0.95, 0.82), "noise": 0.06},
    },
    "sphagnum": {
        "moss": {"bottom": (0.28, 0.36, 0.12), "top": (0.52, 0.56, 0.2), "noise": 0.4, "cells": (4, 2),
                 "spots": (0.48, 0.16, 0.16), "spot_share": 0.3, "spot_cells": (5, 3)},
        "mossred": {"bottom": (0.36, 0.12, 0.12), "top": (0.56, 0.22, 0.2), "noise": 0.4, "cells": (4, 2),
                    "spots": (0.45, 0.5, 0.18), "spot_share": 0.28, "spot_cells": (5, 3)},
    },
    "bogiron": {
        "rust": {"bottom": (0.17, 0.09, 0.05), "top": (0.42, 0.2, 0.08), "noise": 0.5, "cells": (8, 4, 2),
                 "spots": (0.5, 0.33, 0.12), "spot_share": 0.25, "spot_cells": (5, 3)},
    },
    "peatstack": {
        "peat": {"bottom": (0.14, 0.09, 0.05), "top": (0.24, 0.16, 0.09), "noise": 0.35, "cells": (16, 5, 2), "streaks": 0.5},
        "peattop": {"bottom": (0.22, 0.24, 0.1), "top": (0.3, 0.3, 0.14), "noise": 0.4, "cells": (8, 3),
                    "spots": (0.18, 0.12, 0.07), "spot_share": 0.35},
    },
}
SWATCHES["bogironlump"] = SWATCHES["bogiron"]
SWATCHES["peatbrick"] = SWATCHES["peatstack"]
SWATCHES.update({
    "juniper": {
        "wood": {"bottom": (0.22, 0.16, 0.12), "top": (0.36, 0.28, 0.2), "streaks": 0.4},
        "needle": {"bottom": (0.12, 0.22, 0.14), "top": (0.3, 0.42, 0.34), "noise": 0.15},
        "berry": {"bottom": (0.1, 0.12, 0.2), "top": (0.38, 0.45, 0.6), "noise": 0.25, "cells": (4, 2)},
    },
    "angelica": {
        "stem": {"bottom": (0.38, 0.2, 0.28), "top": (0.38, 0.5, 0.25), "noise": 0.1},
        "leaf": {"bottom": (0.15, 0.3, 0.1), "top": (0.3, 0.48, 0.16)},
        "ray": {"bottom": (0.35, 0.45, 0.22), "top": (0.55, 0.6, 0.3)},
        "flower": {"bottom": (0.6, 0.7, 0.42), "top": (0.88, 0.9, 0.72), "noise": 0.15, "cells": (4, 2)},
    },
    "icelandmoss": {
        "lobe": {"bottom": (0.3, 0.24, 0.16), "top": (0.55, 0.5, 0.32), "noise": 0.25, "cells": (6, 3),
                 "spots": (0.62, 0.32, 0.26), "spot_share": 0.12, "spot_cells": (4, 2)},
    },
    "wolflichen": {
        "deadwood": {"bottom": (0.4, 0.38, 0.35), "top": (0.62, 0.6, 0.56), "streaks": 0.6, "noise": 0.2},
        "lichen": {"bottom": (0.62, 0.72, 0.12), "top": (0.88, 0.9, 0.3), "noise": 0.15, "cells": (4, 2)},
    },
    "mountainsorrel": {
        "stalk": {"bottom": (0.45, 0.18, 0.15), "top": (0.35, 0.45, 0.18)},
        "leaf": {"bottom": (0.22, 0.42, 0.14), "top": (0.4, 0.58, 0.2), "noise": 0.12},
        "fruit": {"bottom": (0.55, 0.12, 0.1), "top": (0.78, 0.25, 0.18), "noise": 0.15},
    },
    "rocklichen": {
        "stone": {"bottom": (0.3, 0.3, 0.31), "top": (0.48, 0.47, 0.46), "noise": 0.35, "cells": (12, 5, 2)},
        "crust": {"bottom": (0.5, 0.42, 0.5), "top": (0.72, 0.68, 0.7), "noise": 0.35, "cells": (4, 2),
                  "spots": (0.4, 0.25, 0.42), "spot_share": 0.35, "spot_cells": (5, 3)},
    },
    "whetstone": {
        "slate": {"bottom": (0.24, 0.27, 0.3), "top": (0.36, 0.39, 0.42), "noise": 0.2, "streaks": 0.5},
        "cord": {"bottom": (0.42, 0.32, 0.2), "top": (0.52, 0.4, 0.26)},
    },
    "oilflask": {
        "clay": {"bottom": (0.42, 0.26, 0.18), "top": (0.6, 0.4, 0.28), "noise": 0.2, "cells": (8, 3)},
        "cork": {"bottom": (0.5, 0.38, 0.24), "top": (0.62, 0.5, 0.32), "noise": 0.3},
        "cord": {"bottom": (0.42, 0.32, 0.2), "top": (0.52, 0.4, 0.26)},
    },
})
SWATCHES["wolflichenitem"] = SWATCHES["wolflichen"]
SWATCHES["rocklichenitem"] = SWATCHES["rocklichen"]
SWATCHES.update({
    "rosehip": {
        "cane": {"bottom": (0.32, 0.2, 0.14), "top": (0.3, 0.4, 0.18), "noise": 0.1},
        "thorn": {"bottom": (0.4, 0.22, 0.16), "top": (0.6, 0.45, 0.3)},
        "leaf": {"bottom": (0.14, 0.28, 0.1), "top": (0.28, 0.45, 0.16)},
        "hip": {"bottom": (0.55, 0.06, 0.04), "top": (0.9, 0.22, 0.08), "noise": 0.1},
        "sepal": {"bottom": (0.2, 0.15, 0.1), "top": (0.3, 0.22, 0.14)},
    },
    "yarrow": {
        "leaf": {"bottom": (0.18, 0.3, 0.12), "top": (0.3, 0.44, 0.2)},
        "stem": {"bottom": (0.3, 0.38, 0.2), "top": (0.4, 0.46, 0.26)},
        "flower": {"bottom": (0.88, 0.82, 0.82), "top": (0.98, 0.96, 0.94), "noise": 0.06},
    },
    "caraway": {
        "leaf": {"bottom": (0.2, 0.36, 0.14), "top": (0.34, 0.5, 0.2)},
        "stem": {"bottom": (0.32, 0.42, 0.2), "top": (0.42, 0.5, 0.26)},
        "flower": {"bottom": (0.9, 0.9, 0.86), "top": (0.98, 0.98, 0.96), "noise": 0.05},
    },
    "woad": {
        "leaf": {"bottom": (0.18, 0.3, 0.3), "top": (0.34, 0.46, 0.44), "noise": 0.1},
        "stem": {"bottom": (0.26, 0.36, 0.3), "top": (0.36, 0.46, 0.36)},
        "flower": {"bottom": (0.85, 0.75, 0.1), "top": (1.0, 0.9, 0.25), "noise": 0.1},
    },
    "madder": {
        "stem": {"bottom": (0.3, 0.38, 0.16), "top": (0.38, 0.46, 0.2)},
        "leaf": {"bottom": (0.18, 0.32, 0.1), "top": (0.3, 0.46, 0.16)},
        "flower": {"bottom": (0.75, 0.78, 0.3), "top": (0.9, 0.9, 0.45)},
    },
    "madderroot": {
        "root": {"bottom": (0.45, 0.08, 0.06), "top": (0.7, 0.18, 0.1), "noise": 0.25, "streaks": 0.4},
    },
    "henbane": {
        "leaf": {"bottom": (0.24, 0.32, 0.2), "top": (0.4, 0.48, 0.32), "noise": 0.15},
        "stem": {"bottom": (0.3, 0.36, 0.22), "top": (0.4, 0.46, 0.3)},
        "flower": {"bottom": (0.5, 0.25, 0.45), "top": (0.92, 0.88, 0.62), "noise": 0.08, "streaks": 0.6},
        "throat": {"bottom": (0.25, 0.08, 0.22), "top": (0.4, 0.12, 0.35)},
    },
    "ergot": {
        "spur": {"bottom": (0.08, 0.05, 0.08), "top": (0.25, 0.12, 0.25), "noise": 0.2},
    },
    "hops": {
        "stake": {"bottom": (0.35, 0.33, 0.3), "top": (0.5, 0.48, 0.44), "streaks": 0.6, "noise": 0.2},
        "vine": {"bottom": (0.28, 0.38, 0.14), "top": (0.36, 0.48, 0.18)},
        "leaf": {"bottom": (0.16, 0.32, 0.1), "top": (0.28, 0.46, 0.16)},
        "cone": {"bottom": (0.5, 0.68, 0.3), "top": (0.72, 0.82, 0.45), "noise": 0.12, "cells": (4, 2)},
    },
    "milkpail": {
        "wood": {"bottom": (0.4, 0.28, 0.17), "top": (0.55, 0.4, 0.26), "streaks": 0.6, "noise": 0.15},
        "band": {"bottom": (0.25, 0.24, 0.22), "top": (0.36, 0.35, 0.32)},
        "milk": {"bottom": (0.93, 0.92, 0.86), "top": (0.98, 0.97, 0.93), "noise": 0.03},
    },
})
SWATCHES["hopcones"] = SWATCHES["hops"]


# ---------------------------------------------------------------------------------------------------------------- export

def add_markers(geo, lo, hi):
    """Two tiny triangles at the corners of the whole plant, so both exports get the same bounds."""
    uv = geo.palette.uv(next(iter(geo.palette.index)), 0.5, 0.5)
    V = geo.Vector
    for corner, sign in ((lo, 1), (hi, -1)):
        p = V(corner)
        geo.face([p, p + V((sign * 1e-4, 0, 0)), p + V((0, sign * 1e-4, 0))], [uv, uv, uv])


def build_in_blender(folder, names):
    import bmesh
    import bpy
    import mathutils
    import numpy as np

    for name in names:
        bpy.ops.wm.read_factory_settings(use_empty=True)
        rng = np.random.default_rng(sum(map(ord, name)))
        palette = Palette(np, rng, SWATCHES[name])
        geo = Geometry(bmesh, mathutils.Vector, palette)
        BUILDERS[name][1](geo, rng, np)

        image = bpy.data.images.new(f"{name}_palette", TEXTURE_SIZE, TEXTURE_SIZE, alpha=False)
        rgba = np.concatenate([palette.pixels, np.ones((TEXTURE_SIZE, TEXTURE_SIZE, 1))], axis=2).astype(np.float32)
        image.pixels.foreach_set(rgba.ravel())
        image.pack()
        material = bpy.data.materials.new(f"{name}_material")
        material.use_nodes = True
        nodes = material.node_tree.nodes
        shader = nodes["Principled BSDF"]
        shader.inputs["Roughness"].default_value = 0.85
        texture = nodes.new("ShaderNodeTexImage")
        texture.image = image
        material.node_tree.links.new(texture.outputs["Color"], shader.inputs["Base Color"])

        has_fruit = len(geo.meshes["fruit"].faces) > 0
        lo, hi = geo.bounds()
        outputs = [(name, "main")]
        if has_fruit:
            outputs.append((f"{name}fruit", "fruit"))
            for part in ("main", "fruit"):
                geo.target = part
                add_markers(geo, lo, hi)
        for output_name, part in outputs:
            for obj in list(bpy.data.objects):
                bpy.data.objects.remove(obj)
            bm = geo.meshes[part]
            # Shared corners make the stems and berries shade smoothly.
            bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-6)
            mesh = bpy.data.meshes.new(output_name)
            bm.to_mesh(mesh)
            obj = bpy.data.objects.new(output_name, mesh)
            bpy.context.scene.collection.objects.link(obj)
            obj.data.materials.append(material)
            bpy.ops.object.select_all(action="DESELECT")
            obj.select_set(True)
            bpy.context.view_layer.objects.active = obj
            path = pathlib.Path(folder) / f"{output_name}.glb"
            bpy.ops.export_scene.gltf(filepath=str(path), export_format="GLB", use_selection=True, export_yup=True)
            size = hi - lo
            print(f"FORAGE {output_name}: {len(mesh.polygons)} faces, {size.x:.2f} x {size.y:.2f} x {size.z:.2f} m")


def add_asset_info(path, title):
    data = path.read_bytes()
    json_length = struct.unpack_from("<I", data, 12)[0]
    gltf = json.loads(data[20:20 + json_length])
    gltf["asset"]["extras"] = {
        "title": title,
        "author": "BrudvikWhiteHilt (generated by AssetSource/Tools/make_forage_models.py)",
        "license": "Same as BrudvikWhiteHilt",
    }
    chunk = json.dumps(gltf, separators=(",", ":")).encode("utf-8")
    chunk += b" " * (-len(chunk) % 4)
    rest = data[20 + json_length:]
    header = struct.pack("<4sII", b"glTF", 2, 12 + 8 + len(chunk) + len(rest))
    path.write_bytes(header + struct.pack("<I4s", len(chunk), b"JSON") + chunk + rest)


def main():
    if "bpy" in sys.modules:
        arguments = sys.argv[sys.argv.index("--") + 1:]
        build_in_blender(arguments[0], arguments[1:])
        return

    arguments = [a for a in sys.argv[1:]]
    blender = None
    if "--blender" in arguments:
        blender = arguments[arguments.index("--blender") + 1]
        arguments.remove("--blender")
        arguments.remove(blender)
    names = arguments or list(BUILDERS)
    unknown = [n for n in names if n not in BUILDERS]
    if unknown:
        raise SystemExit(f"Unknown model(s): {', '.join(unknown)}")
    if blender is None:
        found = sorted(pathlib.Path(r"C:\Program Files\Blender Foundation").glob("*/blender.exe"))
        if not found:
            raise SystemExit("Blender not found; pass --blender")
        blender = str(found[-1])
    result = subprocess.run([blender, "-b", "--factory-startup", "--python-exit-code", "1", "--python", __file__, "--", str(MODELS), *names],
                            capture_output=True, text=True, encoding="utf-8", errors="replace")
    written = []
    for line in result.stdout.splitlines():
        if line.startswith("FORAGE"):
            print(line)
            written.append(line.split()[1].rstrip(":"))
    if result.returncode != 0:
        print(result.stdout[-4000:], result.stderr[-3000:])
        raise SystemExit("Blender failed")
    for output_name in written:
        base = output_name[:-5] if output_name.endswith("fruit") and output_name[:-5] in BUILDERS else output_name
        title = BUILDERS[base][0] + (" (fruit)" if base != output_name else "")
        add_asset_info(MODELS / f"{output_name}.glb", title)


if __name__ == "__main__":
    main()
