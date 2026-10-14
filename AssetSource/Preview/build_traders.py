"""The traders of the trading posts, one per biome, for preview pictures. The plan is docs/plans/trading-posts.md; this
is its round B (the trader).

Each trader is the player body in vanilla clothes recoloured in the biome's colours: a tunic (its colour painted on
the body, as the game's _ChestTex does, plus the skirt mesh), trousers (_LegsTex), sometimes a cape, and a hair style
and beard from the character creator, tinted like the game tints them. The stall's awning gets the same colours.

The meshes are Valheim's own, exported in their bind pose with export_vanilla.py (Player, PlayerFemale, ArmorTunic*,
Cape*, Hair*, Beard*), so the body stands in the bind pose (arms out), not in the pose the game gives it.

Usage: python build_traders.py [--no-render]
Renders BrudvikWhiteHiltUnity/Preview/out/traders.png (all eight: front, corner and back, Meadows to the Deep North from
the left) and tradingpost_blackforest.png (one at the stall).
"""
import json
import pathlib
import subprocess
import sys

import numpy as np
from PIL import Image

import build_trading_post

HERE = pathlib.Path(__file__).resolve().parent
REPO = HERE.parents[1]
PREVIEW = REPO / "BrudvikWhiteHiltUnity" / "Preview"
TEXTURES = PREVIEW / "vanilla" / "textures"
DECOR = REPO / "BrudvikWhiteHiltUnity" / "Assets" / "Decor"

# Per biome: the trader's name, the cloth colour, the trim colour, the trousers, the cape (none, "troll" or "nord")
# and its colour, the body, hair and beard (the character creator's names) with the hair colour, and the tunic's cut.
# The mod builds the traders from the same table (traders.json, written by main).
TRADERS = [
    {"biome": "Meadows", "name": "Asbjørn", "main": (0.3, 0.45, 0.2), "accent": (0.93, 0.83, 0.5), "legs": (0.42, 0.33, 0.22),
     "cape": None, "body": "male", "hair": "Hair12", "beard": "Beard5", "hair_colour": (0.85, 0.66, 0.38),
     "tunic": "uni_ArmorTunic1"},
    {"biome": "BlackForest", "name": "Gunnar", "main": (0.2, 0.32, 0.22), "accent": (0.8, 0.5, 0.22), "legs": (0.3, 0.24, 0.17),
     "cape": "troll", "cape_colour": (0.36, 0.27, 0.19), "body": "male", "hair": "Hair18", "beard": "Beard12",
     "hair_colour": (0.36, 0.22, 0.12), "tunic": "uni_ArmorTunic2"},
    {"biome": "Swamp", "name": "Ragnhild", "main": (0.34, 0.35, 0.2), "accent": (0.66, 0.68, 0.56), "legs": (0.22, 0.2, 0.15),
     "cape": "nord", "cape_colour": (0.27, 0.29, 0.2), "body": "female", "hair": "Hair16", "beard": None,
     "hair_colour": (0.15, 0.12, 0.1), "tunic": "uni_ArmorTunic1"},
    {"biome": "Mountain", "name": "Hallvard", "main": (0.36, 0.52, 0.72), "accent": (0.94, 0.95, 0.97), "legs": (0.55, 0.57, 0.6),
     "cape": "troll", "cape_colour": (0.9, 0.9, 0.92), "body": "male", "hair": "Hair5", "beard": "Beard13",
     "hair_colour": (0.86, 0.86, 0.84), "tunic": "uni_ArmorTunic2"},
    {"biome": "Plains", "name": "Sigrid", "main": (0.82, 0.62, 0.2), "accent": (0.66, 0.2, 0.12), "legs": (0.45, 0.3, 0.18),
     "cape": None, "body": "female", "hair": "Hair20", "beard": None, "hair_colour": (0.62, 0.24, 0.1),
     "tunic": "uni_ArmorTunic3"},
    {"biome": "Mistlands", "name": "Ylva", "main": (0.36, 0.24, 0.5), "accent": (0.55, 0.8, 0.95), "legs": (0.16, 0.15, 0.2),
     "cape": "nord", "cape_colour": (0.2, 0.16, 0.28), "body": "female", "hair": "Hair31", "beard": None,
     "hair_colour": (0.1, 0.09, 0.1), "tunic": "uni_ArmorTunic2"},
    {"biome": "Ashlands", "name": "Brand", "main": (0.58, 0.12, 0.08), "accent": (0.98, 0.6, 0.18), "legs": (0.17, 0.15, 0.15),
     "cape": "troll", "cape_colour": (0.16, 0.14, 0.14), "body": "male", "hair": "Hair11", "beard": "Beard18",
     "hair_colour": (0.12, 0.1, 0.09), "tunic": "uni_ArmorTunic3"},
    {"biome": "DeepNorth", "name": "Frøydis", "main": (0.84, 0.88, 0.93), "accent": (0.22, 0.42, 0.68), "legs": (0.32, 0.36, 0.44),
     "cape": "nord", "cape_colour": (0.88, 0.9, 0.94), "body": "female", "hair": "Hair12", "beard": None,
     "hair_colour": (0.92, 0.88, 0.76), "tunic": "uni_ArmorTunic2"},
]

CAPES = {"troll": ("uni_CapeTrollHide", "troll_diffuse"), "nord": ("uni_CapeDeepNorth", "NordCape_d")}
# The vanilla items the mod copies for each part: the tunic's cut, the trousers and the capes.
GAME_ITEMS = {"uni_ArmorTunic1": "ArmorTunic1", "uni_ArmorTunic2": "ArmorTunic2", "uni_ArmorTunic3": "ArmorTunic3",
              "legs": "ArmorTrollLeatherLegs", "troll": "CapeTrollHide", "nord": "CapeDeepNorth"}
GAME_LOOKS = HERE / "traders.json"
SPACING = 1.7
# The Player prefab's Visual is scaled to 0.95. The body and clothes were exported with it, hair and beards from
# their own prefabs without it; in the game they hang under Visual and get it too.
VISUAL_SCALE = 0.95


def hsv(rgb):
    """Hue (0..360), saturation and value of an RGB array in 0..1."""
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    high, low = rgb.max(-1), rgb.min(-1)
    delta = np.where(high - low == 0, 1e-6, high - low)
    hue = np.select([high == r, high == g], [(g - b) / delta % 6, (b - r) / delta + 2], (r - g) / delta + 4) * 60
    saturation = np.where(high == 0, 0, (high - low) / np.maximum(high, 1e-6))
    return hue, saturation, high


def recolour(source, target, main, accent=None, keep_browns=False):
    """Paints a cloth texture in main, keeping its light and shade: the cloth's own brightness, relative to its
    middle, scales the new colour. Saturated gold and orange lines (the trims) become accent; white embroidery and,
    with keep_browns, leather belts stay as they are."""
    image = Image.open(TEXTURES / f"{source}.png").convert("RGBA")
    data = np.asarray(image).astype(np.float32) / 255
    rgb, alpha = data[..., :3], data[..., 3]
    hue, saturation, value = hsv(rgb)
    luminance = rgb @ np.array([0.3, 0.59, 0.11])
    visible = alpha > 0.1
    trim = visible & (saturation > 0.45) & (hue > 25) & (hue < 60) & (value > 0.45)
    white = visible & (saturation < 0.18) & (value > 0.7)
    brown = visible & (hue > 8) & (hue < 40) & (value < 0.45) & (saturation > 0.3) if keep_browns else np.zeros_like(visible)
    cloth = visible & ~trim & ~white & ~brown
    middle = np.median(luminance[cloth]) if cloth.any() else 0.5
    shade = np.clip(luminance / max(middle, 1e-3), 0.45, 1.6)[..., None]
    out = rgb.copy()
    out[cloth] = (np.array(main) * shade)[cloth]
    if accent is not None and trim.any():
        trim_middle = np.median(luminance[trim])
        trim_shade = np.clip(luminance / max(trim_middle, 1e-3), 0.6, 1.4)[..., None]
        out[trim] = (np.array(accent) * trim_shade)[trim]
    result = np.concatenate([np.clip(out, 0, 1), alpha[..., None]], -1)
    Image.fromarray((result * 255).astype(np.uint8), "RGBA").save(TEXTURES / f"{target}.png")
    return target


def body_texture(trader, name):
    """The body with the tunic's colour and the trousers painted on, as the player shader lays _ChestTex and _LegsTex
    over the skin."""
    skin = "PlayerCharacter_01_fem" if trader["body"] == "female" else "PlayerCharacter_01"
    body = Image.open(TEXTURES / f"{skin}.png").convert("RGBA")
    legs = recolour("TrollLeatherArmorLegs_d", f"{name}_legs", trader["legs"], keep_browns=True)
    chest = recolour("Tunic1body_d", f"{name}_chest", trader["main"], trader["accent"])
    for layer in (legs, chest):
        body.alpha_composite(Image.open(TEXTURES / f"{layer}.png").convert("RGBA").resize(body.size))
    body.save(TEXTURES / f"{name}_body.png")
    return f"{name}_body"


def awning_texture(trader):
    """The market stall's texture with the awning's red stripes in the biome's colour; the white stays."""
    source = Image.open(DECOR / "decor_market_stall_albedo.png").convert("RGBA")
    data = np.asarray(source).astype(np.float32) / 255
    rgb = data[..., :3]
    hue, saturation, value = hsv(rgb)
    awning = np.zeros(saturation.shape, bool)
    awning[:, int(saturation.shape[1] * 0.75):] = True
    red = awning & ((hue < 20) | (hue > 340)) & (saturation > 0.3)
    luminance = rgb @ np.array([0.3, 0.59, 0.11])
    middle = np.median(luminance[red])
    shade = np.clip(luminance / middle, 0.5, 1.5)[..., None]
    out = rgb.copy()
    out[red] = (np.array(trader["main"]) * shade)[red]
    result = np.concatenate([np.clip(out, 0, 1), data[..., 3:]], -1)
    name = f"tp_awning_{trader['biome'].lower()}"
    Image.fromarray((result * 255).astype(np.uint8), "RGBA").save(TEXTURES / f"{name}.png")
    return name


def trader_parts(trader, position, yaw=0.0):
    """The meshes of one trader standing at position, facing +z turned by yaw."""
    name = f"tp_trader_{trader['biome'].lower()}"
    at = {"position": list(position)}
    if yaw:
        at["rotation"] = [0, yaw, 0]
    body = "uni_PlayerFemale" if trader["body"] == "female" else "uni_Player"
    parts = [dict(at, mesh=body, texture=body_texture(trader, name))]
    skirt = recolour("ClothesCasual1_d", f"{name}_skirt", trader["main"], trader["accent"], keep_browns=True)
    parts.append(dict(at, mesh=trader["tunic"], texture=skirt))
    if trader["cape"]:
        mesh, texture = CAPES[trader["cape"]]
        parts.append(dict(at, mesh=mesh, texture=recolour(texture, f"{name}_cape", trader["cape_colour"], trader["accent"])))
    for style in (trader["hair"], trader["beard"]):
        if style:
            parts.append(dict(at, mesh=style, tint=list(trader["hair_colour"]), scale=[VISUAL_SCALE] * 3))
    return parts


def portraits():
    """One piece per trader, seen close from the front, the front corner and the back."""
    pieces = []
    for trader in TRADERS:
        pieces.append({
            "name": f"trader_{trader['biome'].lower()}",
            "parts": trader_parts(trader, [0, 0, 0]),
            "views": [
                {"label": "front", "yaw": 180, "pitch": 3, "focus": [0, 1.02, 0], "radius": 1.2},
                {"label": "corner", "yaw": 215, "pitch": 6, "focus": [0, 1.02, 0], "radius": 1.2},
                {"label": "back", "yaw": 0, "pitch": 3, "focus": [0, 1.02, 0], "radius": 1.2},
            ],
        })
    return pieces


def contact_sheet(names, out):
    """Puts the traders' pictures side by side: one column per trader, front, corner and back under each other."""
    columns = []
    for name in names:
        image = Image.open(PREVIEW / "out" / f"{name}.png").convert("RGB")
        width = image.width // 3
        views = [image.crop((i * width, 0, (i + 1) * width, image.height)) for i in range(3)]
        column = Image.new("RGB", (width, image.height * 3))
        for i, view in enumerate(views):
            column.paste(view, (0, i * image.height))
        columns.append(column)
    sheet = Image.new("RGB", (sum(c.width for c in columns), columns[0].height))
    x = 0
    for column in columns:
        sheet.paste(column, (x, 0))
        x += column.width
    sheet.thumbnail((2400, 2400))
    sheet.save(out)
    print(out)


def at_the_stall(trader):
    """The trading post with this trader behind the goods stall and the awnings in the biome's colours."""
    _, post = build_trading_post.trading_post()
    awning = awning_texture(trader)
    for part in post["parts"]:
        if part.get("mesh") == "preview_decor_market_stall":
            part["texture"] = awning
    deck = build_trading_post.DECK
    post["parts"] += trader_parts(trader, [-1.7, deck, -3.05])
    post["name"] = f"tradingpost_{trader['biome'].lower()}"
    post["views"] = [
        {"label": "at the stall", "yaw": 160, "pitch": 6, "focus": [-1.7, deck + 1.35, -2.6], "radius": 2.0},
        {"label": "landing", "yaw": 160, "pitch": 22, "focus": [0, deck + 1.2, -3], "radius": 8},
    ]
    return post


def write_game_looks():
    """Writes the traders as the mod reads them (TraderLooks): one entry per biome with the vanilla items to copy."""
    looks = []
    for trader in TRADERS:
        looks.append({
            "biome": trader["biome"],
            "name": trader["name"],
            "female": trader["body"] == "female",
            "hair": trader["hair"],
            "beard": trader["beard"] or "",
            "hairColour": list(trader["hair_colour"]),
            "main": list(trader["main"]),
            "accent": list(trader["accent"]),
            "legs": list(trader["legs"]),
            "tunic": GAME_ITEMS[trader["tunic"]],
            "trousers": GAME_ITEMS["legs"],
            "cape": GAME_ITEMS[trader["cape"]] if trader["cape"] else "",
            "capeColour": list(trader.get("cape_colour", (1.0, 1.0, 1.0))),
        })
    text = '{"note":"Generated by AssetSource/Preview/build_traders.py; do not edit.","traders":[\n'
    text += ",\n".join(json.dumps(look, ensure_ascii=False, separators=(",", ":")) for look in looks) + "\n]}\n"
    GAME_LOOKS.write_text(text, encoding="utf-8")
    print(f"wrote {GAME_LOOKS}")


def main(render):
    write_game_looks()
    post = at_the_stall(TRADERS[1])
    used = {part["piece"] for part in post["parts"] if part.get("piece")}
    people = portraits()
    layout = {"pieces": build_trading_post.templates(used) + people + [post]}
    path = PREVIEW / "traders_layout.json"
    path.write_text(json.dumps(layout, indent=1))
    if render:
        subprocess.run(["powershell", "-ExecutionPolicy", "Bypass", "-File", str(HERE / "render_preview.ps1"), "-Layout", str(path),
                        "-Only", ",".join([piece["name"] for piece in people] + [post["name"]])], check=True)
        contact_sheet([piece["name"] for piece in people], PREVIEW / "out" / "traders.png")
    print(path)


if __name__ == "__main__":
    main("--no-render" not in sys.argv[1:])
