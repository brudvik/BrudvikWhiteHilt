"""A trading post by the sea, put together for a preview picture. The plan is docs/plans/trading-posts.md; this is its
round A (the model), and the parts and their places here are the ones the mod will build.

The post: a landing of jetty sections on the edge of the bank, a long jetty out to deep water with a jetty head, a gate
with a sign where the jetty meets the landing, two market stalls (the land's goods and the fish), a harbour crane,
mooring posts, cargo on the jetty and a flag at the head. On the land behind: the trader's laft house, its gable and
covered porch towards the jetty and furnished inside, a fire with benches, a wood yard, a fence with a gate round the
yard, paths of stone, plants along the edges and shells and a rock on the beach.

Coordinates: x runs along the shore, the sea lies at +z. The sea bed is the ground plane (y 0); the water's surface is
at WATER, the bank's top at BANK and the deck at DECK. The jetty pieces' own piles reach 3 m below the deck; where the
bed lies deeper, extra piles go down to it (the mod must do the same).

The same parts are written to AssetSource/Preview/tradingpost.json, which the mod embeds and builds every post from
(TradingPostLayout): every piece's prefab, position and yaw relative to the middle of the landing at deck height, +z
towards the sea; the laft house unfolded into its pieces; where the trader stands, the dome, the piles to fill down to
the bed, the ground to level and the ground to paint (earth under the house and the fire, stone on the paths, which
also keeps the grass away). Parts can be limited to some biomes, so flowers do not grow by a post in the Ashlands.
Only the bank, the water and the ship are the preview's own.

Usage: python build_trading_post.py [--no-render]
Writes tradingpost.json and BrudvikWhiteHiltUnity/Preview/tradingpost_layout.json, and renders the latter into
Preview/out/tradingpost.png.
"""
import json
import math
import pathlib
import subprocess
import sys

import compose_preview
import render_decor
from build_defenses import r3
from build_water import FLAG_ARM, GATE_BEAM, GATE_HALF, PENNANT_HEIGHT

HERE = pathlib.Path(__file__).resolve().parent
REPO = HERE.parents[1]
PREVIEW = REPO / "BrudvikWhiteHiltUnity" / "Preview"
MESHES = PREVIEW / "vanilla"
CATALOG = REPO / "AssetSource" / "Decor" / "decor.json"
DEFENSES = HERE / "defenses.json"
GAME_LAYOUT = HERE / "tradingpost.json"

DECK = 4.0  # the walking surface of the landing and the jetty
BANK = 3.9  # the top of the bank, just under the deck
WATER = 2.8  # so the water is about 2.6 m deep at the jetty head
PILE_REACH = 3.0  # how far the jetty pieces' own piles reach below the deck
JETTY_SECTIONS = 4  # 4 m each, out from the landing's edge at z 2
JETTY_ROWS = (-1.0, 1.0)  # two sections side by side: the jetty is 4 m wide, room to walk past the cargo
CANVAS = [0.62, 0.16, 0.13]  # the market stall's red; one colour scheme per biome comes in round B
CANVAS_LIGHT = [0.9, 0.87, 0.8]
DOME_CENTRE = (0.0, 0.0, 5.0)  # in post coordinates: over the jetty's foot, so the house and the jetty head are inside
DOME_RADIUS = 34.7  # a third more than the first 26 m, so ships lie inside on every side of the jetty
TRADER = (-1.7, 0.0, -3.05)  # behind the goods stall, facing the jetty
# The hunter's chair behind the fish stall, and where the hunter sits on it: the seat's attach point, 0.5 m under the
# seat as the decor chairs have it, facing the stall. The chair model faces its -z, so it is turned round.
HUNTER_CHAIR = (2.0, -3.3)
HUNTER = (2.0, 0.07, -3.3)
# Raised whenever the layout changes: the mod builds the posts of an older layout again, keeping their ids.
LAYOUT_VERSION = 4

# The ground behind the landing levelled at the bank's height: (x, z, radius); the smoothing reaches SMOOTH out.
LEVEL = ((-3.0, -11.0, 9.0), (6.0, -11.0, 9.0))
SMOOTH = 12.0

# The trader's house: its middle, and the bottom of its foundation, which puts the floor 0.35 m above the yard.
HOUSE = (0.0, -12.5)
HOUSE_BASE = BANK - 0.75

# Heightmap.Biome flags, for the parts only some biomes get.
MEADOWS, SWAMP, MOUNTAIN, BLACK_FOREST, PLAINS, ASHLANDS, DEEP_NORTH, MISTLANDS = 1, 2, 4, 8, 16, 32, 64, 512
FLOWERING = MEADOWS | BLACK_FOREST | PLAINS
LEAFY = FLOWERING | SWAMP | MISTLANDS
CONIFEROUS = BLACK_FOREST | MOUNTAIN | DEEP_NORTH | SWAMP


def custom_mesh(name, vertices, triangles, uvs=None, texture=""):
    """Writes a mesh in the preview's own format (Unity space), both sides, flat normals per triangle."""
    out_v, out_n, out_uv, out_t = [], [], [], []
    uvs = uvs or [(0.0, 0.0)] * len(vertices)
    for flip in (False, True):
        for a, b, c in zip(triangles[0::3], triangles[1::3], triangles[2::3]):
            if flip:
                b, c = c, b
            p = [vertices[i] for i in (a, b, c)]
            u = [p[1][k] - p[0][k] for k in range(3)]
            v = [p[2][k] - p[0][k] for k in range(3)]
            n = [u[1] * v[2] - u[2] * v[1], u[2] * v[0] - u[0] * v[2], u[0] * v[1] - u[1] * v[0]]
            length = math.sqrt(sum(x * x for x in n)) or 1.0
            n = [x / length for x in n]
            for i in (a, b, c):
                out_t.append(len(out_v))
                out_v.append(vertices[i])
                out_n.append(n)
                out_uv.append(uvs[i])
    data = {"parts": [{
        "texture": texture,
        "vertices": [c for v in out_v for c in v],
        "normals": [c for n in out_n for c in n],
        "uvs": [c for uv in out_uv for c in uv],
        "triangles": out_t,
    }]}
    (MESHES / f"{name}.json").write_text(json.dumps(data))


def grid(name, x0, x1, z0, z1, step, height):
    """A height field from x0..x1, z0..z1 with height(x, z)."""
    xs = [x0 + i * step for i in range(int((x1 - x0) / step) + 1)]
    zs = [z0 + i * step for i in range(int((z1 - z0) / step) + 1)]
    vertices = [(x, height(x, z), z) for z in zs for x in xs]
    triangles = []
    w = len(xs)
    for j in range(len(zs) - 1):
        for i in range(w - 1):
            a, b, c, d = j * w + i, j * w + i + 1, (j + 1) * w + i, (j + 1) * w + i + 1
            triangles += [a, c, b, b, c, d]
    custom_mesh(name, vertices, triangles)


def bank_height(x, z):
    """The bank: flat land at BANK, a short beach, then the bed falling to the ground plane 10 m out. Inside LEVEL it
    lies at BANK exactly, as the post's terrain modifiers make it in the game."""
    near = min(math.hypot(x - cx, z - cz) - radius for cx, cz, radius in LEVEL)
    natural = natural_height(x, z)
    if near <= 0:
        return BANK
    if z > -1.5:
        return natural  # the beach and the bed as they are
    t = min(1.0, near / (SMOOTH - LEVEL[0][2]))
    return BANK + (natural - BANK) * t * t


def natural_height(x, z):
    wobble = 0.08 * math.sin(x * 0.7) + 0.06 * math.sin(z * 1.3 + x * 0.3)
    shore = -1.0 + 0.6 * math.sin(x * 0.25)
    if z < shore:
        return BANK - 0.05 + wobble * (1 if z < shore - 3 else 0.3)
    t = min(1.0, (z - shore) / 11.0)
    return max(0.02, (BANK - 0.05) * (1 - t) ** 1.3)


def canvas_texture():
    """Red and white stripes like the market stall's awning, for the flag."""
    from PIL import Image
    image = Image.new("RGB", (256, 64))
    for x in range(256):
        for y in range(64):
            colour = CANVAS if (y // 16) % 2 == 0 else CANVAS_LIGHT
            shade = 0.92 + 0.08 * ((x * 7 + y * 13) % 5) / 4
            image.putpixel((x, y), tuple(int(c * shade * 255) for c in colour))
    (MESHES / "textures").mkdir(parents=True, exist_ok=True)
    image.save(MESHES / "textures" / "tp_canvas.png")


def pennant_mesh():
    """A long swallow-tailed pennant 2.4 m long and 0.9 m high at the hoist, flying along +x from the pole."""
    vertices = [(0, 0, 0), (0, 0.9, 0), (2.4, 0.75, 0.15), (1.9, 0.45, 0.1), (2.4, 0.15, 0.15), (1.2, 0.45, 0.05)]
    uvs = [(0, 0), (0, 1), (1, 0.85), (0.8, 0.5), (1, 0.15), (0.5, 0.5)]
    triangles = [0, 1, 5, 1, 2, 5, 5, 2, 3, 5, 3, 4, 0, 5, 4]
    custom_mesh("tp_pennant", vertices, triangles, uvs, "tp_canvas")


class Post:
    """Collects the parts of the post: what the preview draws (parts) and what the mod builds (placements)."""

    def __init__(self):
        catalog = json.loads(CATALOG.read_text(encoding="utf-8"))
        self.entries = {entry["id"]: entry for entry in catalog["pieces"]}
        self.parts = []
        self.placements = []
        self.piles = []
        self.paint = []

    def decor(self, model_id, x, z, yaw=0.0, y=DECK, ground=False, biomes=0):
        """A Decor Hammer model standing at (x, y, z), its footprint centred there, turned by yaw. With ground, the mod
        stands it on the ground where it is instead of at y; with biomes, only posts in those biomes get it."""
        name, _, _ = render_decor.load(self.entries[model_id])
        data = {"mesh": f"preview_{name}", "position": r3((x, y, z))}
        if yaw:
            data["rotation"] = [0, yaw, 0]
        self.parts.append(data)
        self.place(f"piece_whitehilt_decor_{model_id}", x, y, z, yaw, ground, biomes)

    def yard(self, model_id, x, z, yaw=0.0, lift=0.0, biomes=0):
        """A Decor Hammer model in the levelled yard, lift metres above the ground (on a bench, a table)."""
        self.decor(model_id, x, z, yaw, BANK + lift, biomes=biomes)

    def vanilla(self, prefab, x, y, z, yaw=0.0):
        """A vanilla piece, drawn with its mesh of the same name."""
        data = {"mesh": prefab, "position": r3((x, y, z))}
        if yaw:
            data["rotation"] = [0, yaw, 0]
        self.parts.append(data)
        self.place(prefab, x, y, z, yaw)

    def earth(self, x, z, radius, kind="dirt"):
        """Ground painted round (x, z): dirt or paved. Painted ground grows no grass."""
        self.paint.append({"centre": r3((x, BANK - DECK, z)), "radius": radius, "type": kind})

    def height(self, model_id):
        return self.entries[model_id]["height"]

    def piece(self, name, x, z, yaw=0, y=DECK):
        data = {"piece": name, "position": r3((x, y, z))}
        if yaw:
            data["rotation"] = [0, yaw, 0]
        self.parts.append(data)
        self.placements.append({"piece": name, "position": (x, y - DECK, z), "yaw": yaw})

    def on_land(self, model_id, x, z, yaw=0.0, biomes=0):
        self.decor(model_id, x, z, yaw, bank_height(x, z), ground=True, biomes=biomes)

    def place(self, prefab, x, y, z, yaw, ground=False, biomes=0):
        data = {"prefab": prefab, "position": r3((x, y - DECK, z)), "yaw": round(yaw % 360, 1)}
        if ground:
            data["ground"] = True
        if biomes:
            data["biomes"] = biomes
        self.placements.append(data)


def deep_piles(post, points):
    """Places under the jetty where the mod stands extra piles down to the bed when the bed lies deeper than the jetty
    piece's own piles reach; the preview draws them where its own bed is that deep."""
    top = DECK - PILE_REACH + 0.3
    for x, z in points:
        post.piles.append({"position": r3((x, top - DECK, z))})
        if bank_height(x, z) < top - 0.2:
            post.parts.append({"piece": "handelspaele", "position": r3((x, top, z))})


def trader_house(post):
    """The trader's laft house behind the stalls: 8 x 4 m on a stone foundation, its gable towards the jetty with the
    door in it and the roof carried 2 m further out on two posts over a porch, so the door is under cover. Windows in
    both long walls, a plank floor over all of it, and inside a bed, a chest, shelves, a table with the trader's map
    and accounts, a bench and light.

    The house is drawn in its own frame like the lafthus of the log house pieces, its ridge along +x and the door at
    +x, and turned so +x points to the sea."""
    cx, cz = HOUSE
    turn = -90  # the house's +x to the post's +z

    def where(lx, lz):
        x, _, z = compose_preview.rotate_y((lx, 0, lz), turn)
        return cx + x, cz + z

    def piece(name, lx, lz, yaw=0, ly=0.0):
        x, z = where(lx, lz)
        post.piece(name, x, z, yaw + turn, HOUSE_BASE + ly)

    def vanilla(prefab, lx, ly, lz, yaw=0):
        x, z = where(lx, lz)
        post.vanilla(prefab, x, HOUSE_BASE + ly, z, yaw + turn)

    def thing(model_id, lx, lz, yaw=0, ly=0.0):
        x, z = where(lx, lz)
        post.decor(model_id, x, z, yaw + turn, HOUSE_BASE + ly)

    floor = 1.1  # the floor's top: the floor pieces' top at 0.08 over their snap height 1.02
    # Foundation along the four sides and round the porch, cornerstones at the corners.
    for lx in (-3, -1, 1, 3):
        piece("grunnmur", lx, 2)
        piece("grunnmur", lx, -2)
    for lz in (-1, 1):
        piece("grunnmur", -4, lz, 90)
        piece("grunnmur", 4, lz, 90)
        piece("grunnmur", 6, lz, 90)
    for lz in (-2, 2):
        piece("grunnmur", 5, lz)
    for lx, lz in ((-4, -2), (4, -2), (-4, 2), (4, 2), (6, -2), (6, 2)):
        piece("hjornestein", lx, lz)
    # Two stones as a step up to the porch.
    for lz in (-0.42, 0.42):
        piece("hjornestein", 6.75, lz, 0, -0.08)

    # The long walls (plain) with a window in each, the shutters outwards; the gable walls offset, with the door in
    # the front one and a small opening in the back one.
    piece("laftvegg", -3, 2, 0, 1)
    piece("laftvegg_vindu", -1, 2, 0, 1)
    piece("laftvegg", 1, 2, 0, 1)
    piece("laftvegg_vindu", 3, 2, 0, 1)
    piece("laftvegg", -3, -2, 0, 1)
    piece("laftvegg", -1, -2, 0, 1)
    piece("laftvegg_vindu", 1, -2, 180, 1)
    piece("laftvegg", 3, -2, 0, 1)
    piece("laftvegg_forskutt", -4, -1, 90, 1)
    piece("laftvegg_forskutt_glugg", -4, 1, -90, 1)
    piece("laftvegg_forskutt_1m", 4, -1.5, 90, 1)
    piece("plankedor", 4, 0, 90, 1)
    piece("laftvegg_forskutt_1m", 4, 1.5, 90, 1)
    piece("laftehjorne", -4, -2, 180, 1)
    piece("laftehjorne", 4, 2, 0, 1)
    piece("laftehjorne_speilet", 4, -2, 90, 1)
    piece("laftehjorne_speilet", -4, 2, -90, 1)
    for lx in (-4, 4):
        piece("laftgavl_45", lx, 1, 90, 3)
        piece("laftgavl_45", lx, -1, -90, 3)

    # Floors: the room and the porch.
    piece("plankegulv_4x4", -2, 0, 0, 1.02)
    piece("plankegulv_4x4", 2, 0, 0, 1.02)
    piece("plankegulv_4x2", 5, 0, 90, 1.02)

    # The vanilla 45 degree roof, 1 m past the back gable and on over the porch to 3 m past the front one.
    for lx in (-4, -2, 0, 2, 4, 6):
        vanilla("wood_roof_45", lx, 4, 1)
        vanilla("wood_roof_45", lx, 4, -1, 180)
    # The porch: a post on each front cornerstone and beams to them under the eaves, a tie beam across the room.
    for lz in (-1.8, 1.8):
        vanilla("wood_pole", 6, 1.5, lz)
        vanilla("wood_pole", 6, 2.5, lz)
        vanilla("wood_beam", 5.05, 2.8, lz)
    for lz in (-0.9, 0.9):
        vanilla("wood_beam", 6, 2.8, lz, 90)
        vanilla("wood_beam", 0, 3.2, lz, 90)

    # On the porch: a cask and a stool by the door, a lamp at each side of the way in.
    thing("barrel_wine", 5.3, 1.35, 0, floor)
    thing("stool", 5.3, -1.35, 0, floor)
    for lz in (-1.05, 1.05):
        thing("hanging_lamp", 6, lz, 0, 2.6 - post.height("hanging_lamp"))

    # Inside: the bed along the back wall with a chest at its foot, shelves in the other back corner.
    thing("carved_bed", -2.65, 1.0, 90, floor)
    thing("iron_bound_chest", -1.0, 1.45, 180, floor)
    thing("candlestick", -0.8, 1.45, 0, floor + post.height("iron_bound_chest"))
    thing("shelves_dark", -3.62, -1.0, 90, floor)
    thing("jar_tall", -3.62, -1.5, 0, floor + post.height("shelves_dark"))
    thing("brass_pot", -3.62, -0.6, 0, floor + post.height("shelves_dark"))
    # The table under the window with the trader's map, accounts and a candle; two chairs facing it.
    thing("table_plain", 0.6, -1.3, 0, floor)
    top = floor + post.height("table_plain")
    thing("map_scroll", 0.3, -1.35, 10, top)
    thing("scroll", 0.95, -1.45, 70, top)
    thing("table_candle", 1.3, -1.15, 0, top)
    thing("tankard", -0.1, -1.1, 0, top)
    for lx in (0.1, 1.1):
        thing("chair", lx, -0.45, 0, floor)  # the chair model faces its -z
    # A painted bench and a candle stand along the other wall, goods by the door, and a lamp from the tie beam.
    thing("painted_bench", 1.4, 1.62, 0, floor)  # faces its -z, into the room
    thing("candlestand", 2.6, 1.55, 0, floor)
    thing("wooden_box", 3.3, -1.45, 0, floor)
    thing("basket_lidded", 3.3, -1.45, 15, floor + post.height("wooden_box"))
    thing("grain_sack", 2.55, -1.5, -20, floor)
    thing("broom", 3.7, 1.25, 90, floor)
    thing("hanging_lamp", 0, 0, 0, 3.0 - post.height("hanging_lamp"))

    # Earth under the house and round it, so no grass grows through the floor.
    for lx in (-4.5, -1.5, 1.5, 4.5, 6.5):
        x, z = where(lx, 0)
        post.earth(x, z, 3.3)


def yard(post):
    """The land round the house: the wood yard on the left, the fire with its benches on the right, a stone path from
    the porch to both and out through a gate in the fence that closes the yard on three sides, and plants where the
    yard meets the land around (only in the green biomes)."""
    # The wood yard: a chopping block, wood stacked along the house wall, the handcart.
    post.yard("chopping_block", -4.6, -11.0, 30)
    post.yard("firewood", -3.05, -12.4, 90)
    post.yard("firewood", -3.05, -13.6, 95)
    post.yard("handcart", -5.9, -5.8, 200)
    post.earth(-4.4, -11.5, 2.4)

    # The fire: a grate over it, benches behind and to the right facing it, two stools on the house side, wood by it.
    # A bench's seat faces its +z.
    fx, fz = 6.5, -11.5
    post.yard("fire_pit", fx, fz)
    post.yard("cooking_grate", fx, fz)
    post.yard("tavern_bench", fx, fz - 2.5)
    post.yard("tavern_bench", fx + 2.5, fz, -90)
    post.yard("stool", fx - 2.2, fz + 0.6)
    post.yard("stool", fx - 2.25, fz - 0.7)
    post.yard("tankard", fx - 0.6, fz - 2.5, 0, post.height("tavern_bench"))
    post.yard("mug", fx + 0.6, fz - 2.55, 0, post.height("tavern_bench"))
    post.yard("firewood", fx + 2.7, fz - 2.8, 30)
    post.earth(fx, fz, 3.4)

    # The path: from the porch step to the right, past the fire and out through the gate, and to the left to the wood
    # yard; lanterns along it for the night.
    for x, z in ((0, -5.3), (1.2, -5.7), (2.5, -6.1), (3.8, -6.5), (5.0, -6.9), (6.2, -7.3), (6.5, -8.3), (7.4, -7.3),
                 (8.6, -7.1), (9.8, -7.0), (11.0, -7.0), (12.0, -7.0), (-1.2, -5.7), (-2.5, -6.3), (-3.6, -7.1),
                 (-4.3, -8.2), (-4.5, -9.3)):
        post.earth(x, z, 0.85, "paved")
    post.yard("lantern_wood", 3.4, -5.6)
    post.yard("lantern_wood", 8.4, -6.5)
    post.yard("lantern_wood", -3.3, -5.9)

    # The fence: skigard down the left side, along the back and up the right side, with a gate in the right side where
    # the path goes out; open to the landing. A lantern on each gate post.
    for z in (-6.0, -10.0, -14.0, -17.0):
        post.piece("skigard" if z != -17.0 else "skigard_2m", -7.5, z, 90, BANK)
    for x in (-5.5, -1.5, 2.5, 6.5):
        post.piece("skigard", x, -18.0, 0, BANK)
    post.piece("skigard_2m", 9.5, -18.0, 0, BANK)
    for z in (-16.0, -12.0):
        post.piece("skigard", 10.5, z, 90, BANK)
    post.piece("skigard_2m", 10.5, -9.0, 90, BANK)
    post.piece("grind", 10.5, -7.0, 90, BANK)
    post.piece("skigard_2m", 10.5, -5.0, 90, BANK)
    for z in (-8.0, -6.0):
        post.yard("lantern_wood", 10.5, z, 0, 1.3)

    # Plants: flowers by the porch and the house, shrubs and young trees outside the fence, ferns behind the house,
    # stones along the path and a boulder by the gate (every biome).
    post.yard("flowers_yellow", -1.7, -5.9, 0, biomes=FLOWERING)
    post.yard("flowers_yellow", 1.75, -6.0, 40, biomes=FLOWERING)
    post.yard("flowers_blue", -2.8, -10.2, 0, biomes=FLOWERING)
    post.yard("flowers_red", 2.9, -9.4, 0, biomes=FLOWERING)
    post.yard("dandelions", -6.6, -16.6, 0, biomes=FLOWERING)
    post.yard("dandelions", 9.6, -16.9, 0, biomes=FLOWERING)
    post.yard("fern_tall", -3.0, -17.2, 0, biomes=LEAFY)
    post.yard("fern_tall", 3.0, -17.3, 60, biomes=LEAFY)
    post.yard("shrub_round", -8.8, -9.5, 0, biomes=LEAFY)
    post.yard("conifer_shrub", -8.9, -14.5, 0, biomes=CONIFEROUS)
    post.yard("leafy_shrub", -2.0, -19.6, 0, biomes=LEAFY)
    post.yard("leafy_shrub", 8.0, -19.7, 90, biomes=LEAFY)
    post.yard("shrub_round", 11.9, -12.5, 40, biomes=LEAFY)
    post.on_land("birch_tree", -10.5, -20.5, 0, biomes=FLOWERING)
    post.on_land("birch_tree", 13.5, -19.0, 120, biomes=FLOWERING)
    post.yard("stone", 3.0, -7.2, 30)
    post.yard("stone", 8.0, -8.1, -40)
    post.yard("boulder", 12.3, -9.6, 20)


def shore(post, end):
    """The beach and the water by the jetty: shells and a rock on the beach, an old fish rack on the shore, and three
    old piles standing in the water by the jetty head."""
    for x, z, yaw in ((-8.0, 0.6, 0), (-7.1, 2.3, 70), (-9.6, -0.4, 130), (8.3, 1.8, 40), (9.6, 0.2, 200)):
        post.on_land("sea_shell", x, z, yaw)
    post.on_land("coast_rock", -13.0, 3.0, 30)
    post.on_land("boulder", 10.5, 2.5, 60)
    post.on_land("drying_fish", -10.0, -3.0, 10)
    post.on_land("pier_posts", -4.3, end - 1.0, 15)


def trading_post():
    post = Post()

    # The ground: the bank, the beach and the sea.
    grid("tp_bank", -40, 40, -40, 30, 0.5, bank_height)
    grid("tp_water", -40, 40, -8, 50, 4.0, lambda x, z: WATER)
    canvas_texture()
    pennant_mesh()
    post.parts += [
        {"mesh": "tp_bank", "position": [0, 0, 0], "tint": [0.42, 0.47, 0.27]},
        {"mesh": "tp_water", "position": [0, 0, 0], "tint": [0.16, 0.3, 0.38]},
    ]

    # The landing: nine jetty sections side by side, 12 m along the shore and 6 m deep (z -4..2).
    for x in (-4, 0, 4):
        for z in (-3, -1, 1):
            post.piece("brygge", x, z)
    deep_piles(post, [(x + dx, 2 - 0.15) for x in (-4, 0, 4) for dx in (-1.8, 0, 1.8)])

    # The jetty straight out from the middle of the landing, two sections wide, and its head with the stairs down to
    # the sea.
    end = 2 + 4 * JETTY_SECTIONS
    for row in JETTY_ROWS:
        for i in range(JETTY_SECTIONS):
            post.piece("brygge", row, 4 + 4 * i, 90)
        post.piece("bryggehode", row, end + 1, -90)
    deep_piles(post, [(row + dx, z) for z in range(3, end + 2, 2) for row in JETTY_ROWS for dx in (-0.85, 0.85)])

    # The gate where the jetty meets the landing: two posts, a beam with the sign under it and a lamp at each end.
    post.piece("handelsport", 0, 2.2)
    for x in (-GATE_HALF - 0.3, GATE_HALF + 0.3):
        post.decor("hanging_lamp", x, 2.2, y=DECK + GATE_BEAM - 0.1 - post.height("hanging_lamp"))

    # Two stalls with a path between them, straight from the gate to the house door: the land's goods on the left,
    # the fish on the right.
    post.decor("market_stall", -1.7, -2.3, 180)
    post.decor("market_stall", 2.0, -2.3, 180)
    post.decor("basket_carrots", -2.3, -1.25, 10)
    post.decor("basket_turnips", -1.3, -1.3, -25)
    post.decor("grain_sack", -3.15, -1.9, 20)
    post.decor("fish_barrel", 1.5, -1.25)
    post.decor("fish_barrel", 2.6, -1.3, 60)
    post.decor("drying_fish", 4.5, -2.6, 90)
    post.decor("table_rough", 4.6, -0.2, 90)
    post.decor("basket", 4.6, -0.3, 20, y=DECK + post.height("table_rough"))
    post.decor("bucket_water", 3.6, 0.6)

    # Light at the stalls, so the trader is seen at night: a candle stand beside the goods stall, behind the counter
    # where the trader stands (the other side is the path to the house), and a lantern on the fish stall's corner. The
    # mod also hangs a soft light over the trader.
    post.decor("candlestand", -3.15, -3.5)
    post.decor("lantern_wood", 3.3, -3.4)

    # The chair behind the fish stall, where a hunter now and then sits and sells the land's trophies.
    post.decor("chair", HUNTER_CHAIR[0], HUNTER_CHAIR[1], 180)

    # The stock on the left of the landing: crates stacked two high, barrels and sacks.
    post.decor("crate_lidded", -5.2, -3.4, 5)
    post.decor("crate_lidded", -5.2, -3.4, -10, y=DECK + post.height("crate_lidded"))
    post.decor("crate_lidded", -4.4, -3.45, 8)
    post.decor("crate", -4.45, -3.45, -20, y=DECK + post.height("crate_lidded"))
    post.decor("barrel_old", -5.3, -2.3)
    post.decor("barrel_old2", -5.25, -1.4, 40)
    post.decor("market_barrel", -4.4, -2.2)
    post.decor("seed_sack", -4.3, -1.25, -30)
    post.decor("slatted_crate", -5.1, 0.2, 70)
    post.decor("market_crate", -4.2, 0.6, -10)

    # The harbour crane on the landing's right front corner, its jib over the water, for unloading.
    post.piece("havnekran", 5.0, 1.0)

    # Along the jetty: cargo waiting to be loaded by the left edge, leaving the way clear, two of our own mooring posts
    # on the right edge by the ship, and lanterns along it for the night.
    post.decor("barrel_farm", -1.55, 8.6)
    post.decor("barrel_farm", -1.5, 9.5, 30)
    post.decor("crate_lidded", -1.55, 10.5, 90)
    post.decor("grain_sack", -1.5, 11.3, 60)
    for z in (9.0, 15.0):
        post.piece("fortoyningspale", 1.8, z)
    for x, z in ((-1.75, 5.0), (1.75, 12.0), (-1.75, 16.0)):
        post.decor("lantern_wood", x, z)

    # The flag at the jetty head, seen from the sea, and a lamp on its arm for the night. The mod hangs the pennant.
    post.piece("flaggstang", -1.75, end + 1.6)
    post.parts.append({"mesh": "tp_pennant", "position": r3((-1.75, DECK + PENNANT_HEIGHT, end + 1.6))})
    post.decor("hanging_lamp", -1.75, end + 2.0, y=DECK + FLAG_ARM - 0.05 - post.height("hanging_lamp"))

    trader_house(post)
    yard(post)
    shore(post, end)

    # A longship tied up along the jetty, for the scale.
    post.parts.append({"mesh": "Karve", "position": r3((4.0, WATER - 0.25, 12.5)), "rotation": [0, 180, 0], "tint": [0.5, 0.36, 0.24]})

    # The painted ground as flat discs just over the bank, earth brown and stone grey, so the picture shows it.
    for i, spot in enumerate(post.paint):
        x, _, z = spot["centre"]
        radius = spot["radius"]
        lift = 0.02 if spot["type"] == "dirt" else 0.03
        ring = [(x + radius * math.cos(a * math.pi / 12), BANK + lift, z + radius * math.sin(a * math.pi / 12)) for a in range(24)]
        custom_mesh(f"tp_paint_{i}", [(x, BANK + lift, z)] + ring, [k for a in range(24) for k in (0, 1 + a, 1 + (a + 1) % 24)])
        tint = [0.4, 0.3, 0.2] if spot["type"] == "dirt" else [0.55, 0.55, 0.52]
        post.parts.append({"mesh": f"tp_paint_{i}", "position": [0, 0, 0], "tint": tint})

    post.end = end
    return post, {
        "name": "tradingpost",
        "parts": post.parts,
        "views": [
            {"label": "from the sea", "yaw": 205, "pitch": 16, "focus": [0, DECK + 1, 2], "radius": 15},
            {"label": "landing", "yaw": 160, "pitch": 24, "focus": [0, DECK + 1.2, -3], "radius": 8},
            {"label": "top", "yaw": 180, "pitch": 70, "focus": [0, DECK, -2], "radius": 22},
            {"label": "yard", "yaw": 215, "pitch": 28, "focus": [2, DECK, -10], "radius": 13},
            {"label": "porch", "yaw": 160, "pitch": 12, "focus": [0, DECK + 1, -7.5], "radius": 6},
            {"label": "fire", "yaw": 230, "pitch": 30, "focus": [6.5, DECK, -11.5], "radius": 6},
        ],
    }


def game_layout(post):
    """The layout the mod builds a post from: the placements with every composite piece (the laft house) unfolded into
    the pieces it is made of, in post coordinates."""
    pieces = {piece["name"]: piece for piece in json.loads(DEFENSES.read_text())["pieces"]}
    parts = []

    def unfold(name, position, yaw):
        piece = pieces[name]
        if "base" in piece:
            parts.append({"prefab": f"piece_whitehilt_{name}", "position": r3(position), "yaw": round(yaw % 360, 1)})
            return
        for child in piece["parts"]:
            rotation = child.get("rotation", [0, 0, 0])
            assert rotation[0] == 0 and rotation[2] == 0, f"{name}: only turns about y can be unfolded"
            offset = compose_preview.rotate_y(child["position"], yaw)
            where = [position[i] + offset[i] for i in range(3)]
            if child.get("piece"):
                unfold(child["piece"], where, yaw + rotation[1])
            else:
                # Vanilla meshes in a composite are the vanilla pieces of the same name, such as the roof.
                parts.append({"prefab": child["mesh"], "position": r3(where), "yaw": round((yaw + rotation[1]) % 360, 1)})

    for placement in post.placements:
        if "piece" in placement:
            unfold(placement["piece"], placement["position"], placement["yaw"])
        else:
            parts.append(placement)

    bank = round(BANK - DECK, 3)
    return {
        "note": "Generated by AssetSource/Preview/build_trading_post.py; do not edit. Post coordinates: the middle of the "
                "landing at deck height, +z towards the sea.",
        "version": LAYOUT_VERSION,
        "head": [0, round(post.end + 1, 3)],
        "trader": {"position": r3(TRADER), "yaw": 0},
        "hunter": {"position": r3(HUNTER), "yaw": 0},
        "dome": {"centre": r3(DOME_CENTRE), "radius": DOME_RADIUS},
        "piles": post.piles,
        # The land behind the landing (the house, the fire, the yard) is levelled at the bank's height by two round
        # terrain modifiers, which keep their shape however the post is turned; their smoothing ends under the landing.
        # Round paint modifiers lay earth and stone on it.
        "level": [{"centre": [x, bank, z], "radius": radius, "smooth": SMOOTH} for x, z, radius in LEVEL],
        "paint": post.paint,
        "parts": parts,
    }


def write_game_layout(layout):
    """Writes the game layout one part per line, so a change shows as a small diff."""
    head = {key: value for key, value in layout.items() if key != "parts"}
    text = json.dumps(head, separators=(",", ":"))[:-1] + ',"parts":[\n'
    text += ",\n".join(json.dumps(part, separators=(",", ":")) for part in layout["parts"]) + "\n]}\n"
    GAME_LAYOUT.write_text(text, encoding="utf-8")
    print(f"wrote {GAME_LAYOUT} with {len(layout['parts'])} parts")


def templates(names):
    """The pieces the post refers to from the building layout, with every piece they refer to in turn."""
    pieces = {piece["name"]: piece for piece in json.loads(DEFENSES.read_text())["pieces"]}
    wanted, queue = {}, list(names)
    while queue:
        name = queue.pop()
        if name in wanted:
            continue
        wanted[name] = dict(pieces[name], template=True)
        queue += [part["piece"] for part in pieces[name]["parts"] if part.get("piece")]
    return list(wanted.values())


def main(render):
    built, post = trading_post()
    write_game_layout(game_layout(built))
    used = {part["piece"] for part in post["parts"] if part.get("piece")}
    layout = {"pieces": templates(used) + [post]}
    path = PREVIEW / "tradingpost_layout.json"
    path.write_text(json.dumps(layout, indent=1))
    if render:
        subprocess.run(["powershell", "-ExecutionPolicy", "Bypass", "-File", str(HERE / "render_preview.ps1"), "-Layout", str(path),
                        "-Only", "tradingpost"], check=True)
        print(PREVIEW / "out" / "tradingpost.png")
    else:
        print(path)


if __name__ == "__main__":
    main("--no-render" not in sys.argv[1:])
