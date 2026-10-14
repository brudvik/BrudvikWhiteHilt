"""A trading post by the sea, put together for a preview picture. The plan is docs/plans/trading-posts.md; this is its
round A (the model), and the parts and their places here are the ones the mod will build.

The post: a landing of jetty sections on the edge of the bank, a long jetty out to deep water with a jetty head, a gate
with a sign where the jetty meets the landing, two market stalls (the land's goods and the fish), the trader's small
laft house behind them, a harbour crane, mooring posts, cargo on the jetty, a flag at the head, and a fire with
benches on the land.

Coordinates: x runs along the shore, the sea lies at +z. The sea bed is the ground plane (y 0); the water's surface is
at WATER, the bank's top at BANK and the deck at DECK. The jetty pieces' own piles reach 3 m below the deck; where the
bed lies deeper, extra piles go down to it (the mod must do the same).

The same parts are written to AssetSource/Preview/tradingpost.json, which the mod embeds and builds every post from
(TradingPostLayout): every piece's prefab, position and yaw relative to the middle of the landing at deck height, +z
towards the sea; the laft house unfolded into its pieces; where the trader stands, the dome, the piles to fill down to
the bed, and the ground to level. Only the bank, the water and the ship are the preview's own.

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
from build_water import FLAG_ARM, GATE_BEAM, PENNANT_HEIGHT

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
CANVAS = [0.62, 0.16, 0.13]  # the market stall's red; one colour scheme per biome comes in round B
CANVAS_LIGHT = [0.9, 0.87, 0.8]
DOME_CENTRE = (0.0, 0.0, 5.0)  # in post coordinates: over the jetty's foot, so the house and the jetty head are inside
DOME_RADIUS = 26.0
TRADER = (-1.7, 0.0, -3.05)  # behind the goods stall, facing the jetty


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
    """The bank: flat land at BANK, a short beach, then the bed falling to the ground plane 10 m out."""
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

    def decor(self, model_id, x, z, yaw=0.0, y=DECK, ground=False):
        """A Decor Hammer model standing at (x, y, z), its footprint centred there, turned by yaw. With ground, the mod
        stands it on the ground where it is instead of at y."""
        name, _, _ = render_decor.load(self.entries[model_id])
        data = {"mesh": f"preview_{name}", "position": r3((x, y, z))}
        if yaw:
            data["rotation"] = [0, yaw, 0]
        self.parts.append(data)
        self.place(f"piece_whitehilt_decor_{model_id}", x, y, z, yaw, ground)

    def height(self, model_id):
        return self.entries[model_id]["height"]

    def piece(self, name, x, z, yaw=0, y=DECK):
        data = {"piece": name, "position": r3((x, y, z))}
        if yaw:
            data["rotation"] = [0, yaw, 0]
        self.parts.append(data)
        self.placements.append({"piece": name, "position": (x, y - DECK, z), "yaw": yaw})

    def on_land(self, model_id, x, z, yaw=0.0):
        self.decor(model_id, x, z, yaw, bank_height(x, z), ground=True)

    def place(self, prefab, x, y, z, yaw, ground=False):
        data = {"prefab": prefab, "position": r3((x, y - DECK, z)), "yaw": round(yaw % 360, 1)}
        if ground:
            data["ground"] = True
        self.placements.append(data)


def deep_piles(post, points):
    """Places under the jetty where the mod stands extra piles down to the bed when the bed lies deeper than the jetty
    piece's own piles reach; the preview draws them where its own bed is that deep."""
    top = DECK - PILE_REACH + 0.3
    for x, z in points:
        post.piles.append({"position": r3((x, top - DECK, z))})
        if bank_height(x, z) < top - 0.2:
            post.parts.append({"piece": "handelspaele", "position": r3((x, top, z))})


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

    # The jetty straight out from the middle of the landing, and its head with the stair down to the sea.
    end = 2 + 4 * JETTY_SECTIONS
    for i in range(JETTY_SECTIONS):
        post.piece("brygge", 0, 4 + 4 * i, 90)
    post.piece("bryggehode", 0, end + 1, -90)
    deep_piles(post, [(dx, z) for z in range(3, end + 2, 2) for dx in (-0.85, 0.85)])

    # The gate where the jetty meets the landing: two posts, a beam with the sign under it and a lamp at each end.
    post.piece("handelsport", 0, 2.2)
    for x in (-1.45, 1.45):
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

    # Along the jetty: cargo waiting to be loaded, and two of our own mooring posts by the ship.
    post.decor("barrel_farm", -0.55, 8.6)
    post.decor("barrel_farm", -0.5, 9.5, 30)
    post.decor("crate_lidded", -0.55, 10.5, 90)
    post.decor("grain_sack", -0.5, 11.3, 60)
    for z in (9.0, 15.0):
        post.piece("fortoyningspale", 0.8, z)

    # The flag at the jetty head, seen from the sea, and a lamp on its arm for the night. The mod hangs the pennant.
    post.piece("flaggstang", -0.75, end + 1.6)
    post.parts.append({"mesh": "tp_pennant", "position": r3((-0.75, DECK + PENNANT_HEIGHT, end + 1.6))})
    post.decor("hanging_lamp", -0.75, end + 2.0, y=DECK + FLAG_ARM - 0.05 - post.height("hanging_lamp"))

    # The trader's house on the land behind the stalls, its door facing the gate down the path.
    house_z = -8.8
    post.piece("lafthus", 0, house_z, 0, BANK - 0.85)
    post.on_land("chopping_block", -4.2, -6.4, 30)
    post.on_land("firewood", -4.0, -7.6, 80)
    post.on_land("handcart", -6.8, -6.0, 200)
    post.on_land("barrel_wine", 3.6, -5.6)

    # A fire with benches on the land to the right, where travellers sit.
    post.on_land("fire_pit", 8.5, -6.5)
    post.on_land("tavern_bench", 8.5, -8.9)
    post.on_land("tavern_bench", 11.0, -6.5, 90)

    # A longship tied up along the jetty, for the scale.
    post.parts.append({"mesh": "Karve", "position": r3((3.0, WATER - 0.25, 12.5)), "rotation": [0, 180, 0], "tint": [0.5, 0.36, 0.24]})

    post.end = end
    return post, {
        "name": "tradingpost",
        "parts": post.parts,
        "views": [
            {"label": "from the sea", "yaw": 205, "pitch": 16, "focus": [0, DECK + 1, 2], "radius": 15},
            {"label": "landing", "yaw": 160, "pitch": 24, "focus": [0, DECK + 1.2, -3], "radius": 8},
            {"label": "top", "yaw": 180, "pitch": 70, "focus": [0, DECK, 4], "radius": 17},
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
        "head": [0, round(post.end + 1, 3)],
        "trader": {"position": r3(TRADER), "yaw": 0},
        "dome": {"centre": r3(DOME_CENTRE), "radius": DOME_RADIUS},
        "piles": post.piles,
        "level": [{"min": [-9, -12.5], "max": [13, -4], "height": bank}],
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
