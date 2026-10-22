"""The household, for preview pictures: the High Seat with its two high-seat pillars, the farmhands, the huskarls and a
captive in chains from a draugr harbour. The plan is docs/plans/household.md; this is its round A (the look).

The people are the player body in vanilla clothes recoloured, as the traders are (build_traders.py): farmhands in plain
wool and linen, huskarls in iron mail and a troll leather hood with a spear and a shield, the captive in a torn, faded
tunic with chains from the wrists. The body stands in its bind pose (arms out), as in the traders' pictures.

The High Seat is made of plain blocks (the game's wood beam scaled), round posts and the carving friezes, so the mod can
build the same piece from a layout.

Usage: python build_household.py [--no-render]
Renders BrudvikWhiteHiltUnity/Preview/out/household.png (the people side by side: front, corner and back) and
highseat.png.
"""
import json
import math
import pathlib
import subprocess
import sys

from PIL import Image

from build_carvings import DEPTH, FRIEZE_LONG
from build_defenses import box, part, r3, views
from build_husvette import lathe
from build_traders import PREVIEW, TEXTURES, VISUAL_SCALE, contact_sheet, recolour, trader_parts

HERE = pathlib.Path(__file__).resolve().parent

WOOD = [0.42, 0.28, 0.17]  # oiled pine, darkened by smoke in the hall
WOOD_DARK = [0.27, 0.18, 0.11]
CUSHION = [0.16, 0.22, 0.4]  # woad-blue wool
IRON = [0.22, 0.22, 0.24]
PAINT_RED = [0.55, 0.14, 0.09]  # the old red of carved and painted Norse woodwork
DAIS = 0.3  # the top of the two steps the seat stands on
SEAT = DAIS + 0.45
PILLAR = 2.75  # the pillars' tops, under the dragon heads

# The farmhands: wool and linen in the plain dyes of a farm (undyed, woad, madder, onion skin). Same keys as the traders.
FARMHANDS = [
    {"biome": "farmhand_arne", "name": "Arne", "main": (0.52, 0.46, 0.38), "accent": (0.72, 0.66, 0.55),
     "legs": (0.33, 0.27, 0.2), "cape": None, "body": "male", "hair": "Hair3", "beard": "Beard2",
     "hair_colour": (0.55, 0.38, 0.2), "tunic": "uni_ArmorTunic1"},
    {"biome": "farmhand_tora", "name": "Tora", "main": (0.3, 0.4, 0.56), "accent": (0.85, 0.82, 0.72),
     "legs": (0.36, 0.3, 0.22), "cape": None, "body": "female", "hair": "Hair14", "beard": None,
     "hair_colour": (0.82, 0.66, 0.4), "tunic": "uni_ArmorTunic2"},
    {"biome": "farmhand_ivar", "name": "Ivar", "main": (0.56, 0.24, 0.16), "accent": (0.8, 0.7, 0.45),
     "legs": (0.28, 0.24, 0.2), "cape": None, "body": "male", "hair": "Hair8", "beard": "Beard9",
     "hair_colour": (0.25, 0.17, 0.1), "tunic": "uni_ArmorTunic3"},
    {"biome": "farmhand_gyda", "name": "Gyda", "main": (0.7, 0.58, 0.3), "accent": (0.4, 0.3, 0.2),
     "legs": (0.32, 0.26, 0.18), "cape": None, "body": "female", "hair": "Hair20", "beard": None,
     "hair_colour": (0.5, 0.2, 0.08), "tunic": "uni_ArmorTunic1"},
]

# The huskarls: iron mail over a coloured kirtle that shows below it, a troll leather hood, a spear and a shield.
HUSKARLS = [
    {"biome": "huskarl_torstein", "name": "Torstein", "main": (0.22, 0.28, 0.42), "body": "male", "hair": "Hair18",
     "beard": "Beard13", "hair_colour": (0.3, 0.2, 0.12)},
    {"biome": "huskarl_hild", "name": "Hild", "main": (0.45, 0.12, 0.1), "body": "female", "hair": "Hair16",
     "beard": None, "hair_colour": (0.86, 0.72, 0.48)},
]

CAPTIVE = {"biome": "captive", "name": "Eirik", "main": (0.42, 0.4, 0.36), "accent": (0.42, 0.4, 0.36),
           "legs": (0.25, 0.23, 0.2), "cape": None, "body": "male", "hair": "Hair24", "beard": "Beard4",
           "hair_colour": (0.38, 0.3, 0.22), "tunic": "uni_ArmorTunic1"}

HAND = 0.78  # how far out the hand is in the bind pose (the spear stands there), and its height
HAND_HEIGHT = 1.38
WRIST = (0.51, 1.04, -0.06)  # the right wrist on the body mesh (x mirrored for the left)
HELMET = (1.8, 0.04, 1.12)  # the iron helmet: height, forward and scale, fitted by eye on the head
MADE = set()


def block(center, size, tint=None, rotation=(0, 0, 0)):
    """A plain block of wood, the game's 2 m beam scaled to size."""
    return part("wood_beam", center, rotation, (size[0] / 2, size[1] / 0.4, size[2] / 0.4), tint=tint)


def upright_band(pattern, x, y0, z, length, width):
    """A carving frieze on end, width wide and length high from y0, centred on x, its back at z, facing +z."""
    scale = (length / FRIEZE_LONG, width, width)
    return part(f"carve_{pattern}", (x + width / 2, y0 + length / 2, z + DEPTH * width / 0.3 / 2), (0, 0, 90), scale,
                tint=PAINT_RED)


def turned(name, profile):
    """A turned shape (build_husvette's lathe), made once per name."""
    if name not in MADE:
        lathe(name, profile)
        MADE.add(name)
    return name


def toward(p0, p1):
    """Rotation and length that turn a mesh standing on +y from p0 so it reaches p1, as build_borgund's pole does."""
    d = [b - a for a, b in zip(p0, p1)]
    length = math.sqrt(sum(v * v for v in d))
    tilt = math.degrees(math.acos(max(-1.0, min(1.0, d[1] / length))))
    yaw = math.degrees(math.atan2(d[0], d[2]))
    return (tilt, yaw, 0), length


def dragon(x, y, z, size, yaw=0.0):
    """A carved dragon head (the ship stem's), size high, its neck standing on (x, y, z) and its snout towards +z
    turned by yaw. The exported mesh lies along x with the snout towards -x, from x -5.06 to -4.48 and y 0.85 to 1.6."""
    s = size / 0.75
    turn = math.radians(yaw)
    # Turned by 90 + yaw about y, the mesh's centre (-4.77, 1.225, 0) lands at (sin, -, cos) * 4.77 s.
    cx, cz = 4.77 * s * math.sin(turn), 4.77 * s * math.cos(turn)
    return part("dragon_head", (x - cx, y - 0.85 * s, z - cz), (0, 90 + yaw, 0), (s, s, s), texture="wood",
                tint=PAINT_RED)


def pillar(x, z):
    """A high-seat pillar: a square post on a moulded foot with a capital, carved with Urnes loops on the front and a
    rope twist on the side towards the seat, and a dragon head looking out over the hall."""
    parts = [block((x, DAIS + 0.06, z), (0.36, 0.12, 0.36), WOOD_DARK),
             block((x, (DAIS + PILLAR) / 2, z), (0.26, PILLAR - DAIS, 0.26), WOOD),
             block((x, PILLAR - 0.1, z), (0.34, 0.08, 0.34), WOOD_DARK),
             block((x, PILLAR - 0.02, z), (0.3, 0.08, 0.3), WOOD)]
    length = PILLAR - DAIS - 0.42
    parts.append(upright_band("slyng", x - 0.11, DAIS + 0.18, z + 0.13, length, 0.22))
    inner = -1 if x > 0 else 1
    band = upright_band("tau", 0, DAIS + 0.18, 0, length, 0.22)
    band["position"] = r3((x + inner * (0.13 + DEPTH * 0.22 / 0.3 / 2), DAIS + 0.18 + length / 2, z + 0.11))
    band["rotation"] = [0, 90 * inner, 90]
    parts.append(band)
    # Rings of red paint where the carving starts and ends.
    for y in (DAIS + 0.15, PILLAR - 0.21):
        parts.append(block((x, y, z), (0.28, 0.04, 0.28), PAINT_RED))
    parts.append(dragon(x, PILLAR + 0.02, z + 0.04, 0.6))
    return parts


def seat_parts():
    """The High Seat facing +z: two steps up to a broad seat with carved arms, a tall back crowned with crossed gable
    boards, a cushion and a fur, and the two high-seat pillars beside it with dragon heads. Only meshes the game has,
    so the mod builds the same piece from the layout (high_seat_piece)."""
    parts = [block((0, 0.06, 0.05), (3.2, 0.12, 2.0), WOOD_DARK),
             block((0, 0.12 + (DAIS - 0.12) / 2, -0.1), (2.8, DAIS - 0.12, 1.5), WOOD)]
    parts.append(part("carve_tau", (0, 0.12, 0.65 + DEPTH * 0.12 / 0.3 / 2), scale=(2.6 / FRIEZE_LONG, 0.12, 0.12),
                      tint=PAINT_RED))
    # The seat: a chest-like box with a carved front and a board on top a little proud of it.
    parts.append(block((0, DAIS + 0.2, -0.2), (1.4, 0.4, 0.62), WOOD))
    parts.append(part("carve_flette", (0, DAIS + 0.06, 0.11 + DEPTH * 0.28 / 0.3 / 2),
                      scale=(1.3 / FRIEZE_LONG, 0.28, 0.28), tint=PAINT_RED))
    parts.append(block((0, SEAT - 0.025, -0.18), (1.48, 0.05, 0.7), WOOD_DARK))
    parts.append(block((0, SEAT + 0.04, -0.2), (1.2, 0.08, 0.56), CUSHION))
    parts.append(part("rug_wolf", (0.15, SEAT + 0.02, -0.2), (0, 75, 8), (0.22, 1, 0.2), texture="rug_wolf"))
    # The arms: a side board each with a frieze on the outside, a rail, and a small dragon's head at the front.
    for x in (-0.72, 0.72):
        out = 1 if x > 0 else -1
        parts.append(block((x, SEAT + 0.17, -0.2), (0.06, 0.34, 0.62), WOOD))
        parts.append(part("carve_slyng", (x + out * (0.03 + DEPTH * 0.26 / 0.3 / 2), SEAT + 0.04, -0.2),
                          (0, 90 * out, 0), (0.6 / FRIEZE_LONG, 0.26, 0.26), tint=PAINT_RED))
        parts.append(block((x, SEAT + 0.36, -0.17), (0.1, 0.05, 0.72), WOOD_DARK))
        parts.append(dragon(x, SEAT + 0.385, 0.14, 0.22))
    # The back: boards to 1.3 m above the seat with a carved panel, a top rail, and crossed gable boards over it.
    back = SEAT + 1.3
    parts.append(block((0, SEAT + 0.65, -0.48), (1.4, 1.3, 0.06), WOOD))
    for x in (-0.7, 0.7):
        parts.append(block((x, SEAT + 0.65, -0.47), (0.1, 1.3, 0.1), WOOD_DARK))
    parts.append(upright_band("slyng", -0.15, SEAT + 0.2, -0.45, 0.85, 0.3))
    parts.append(block((0, back, -0.47), (1.56, 0.12, 0.1), WOOD_DARK))
    parts.append(part("carve_ringkjede", (0, back - 0.2, -0.45 + DEPTH * 0.16 / 0.3 / 2),
                      scale=(1.3 / FRIEZE_LONG, 0.16, 0.16), tint=PAINT_RED))
    rise, run = 0.55, 0.94
    slope = math.degrees(math.atan2(rise, run))
    length = math.hypot(rise, run) + 0.06
    for side in (-1, 1):
        # Each board rises from the rail's end to past the peak, so the two cross over it as on a stave church gable.
        centre = (side * (0.8 - run / 2), back + rise / 2, -0.42)
        parts.append(part("wood_beam", centre, (0, 0, side * slope), (length / 2, 0.12 / 0.4, 0.06 / 0.4), tint=PAINT_RED))
    for x in (-1.2, 1.2):
        parts += pillar(x, -0.35)
    return parts


def high_seat():
    """The High Seat as the preview shows it: the piece, a turned knob where the gable boards cross, and the hall
    around it."""
    parts = seat_parts()
    back = SEAT + 1.3
    parts.append({"mesh": turned("hs_knob", [(0.04, 0.0), (0.06, 0.04), (0.05, 0.09), (0.0, 0.14)]),
                  "position": r3((0, back + 0.45, -0.42)), "tint": WOOD})
    # A wolf rug on the floor before the steps, lying across it.
    parts.append(part("rug_wolf", (0, 0.0, 1.6), (0, 90, 0), (0.55, 1, 0.55), texture="rug_wolf"))
    # The hall's wall behind, of dark logs, with a round shield hung on each side.
    for y in range(8):
        parts.append(part("wood_pole_log", (0, 0.18 + y * 0.36, -0.95), (0, 0, 90), (0.7, 2.6 / 2.22, 0.7),
                          tint=WOOD_DARK))
    for x in (-2.0, 2.0):
        parts.append(part("preview_whroundshield", (x, 1.7, -0.78), (90, 0, 0), texture="whroundshield_albedo"))
    return parts


# The game multiplies a tint into the wood's own brown texture, where the preview paints it on plain grey, so the same
# colours come out near black in the game. These are the preview's colours as the game needs them: light oiled pine, a
# darker trim, woad blue and the old red, as the other carved pieces use them.
GAME_TINTS = {
    tuple(WOOD): [0.78, 0.66, 0.54],
    tuple(WOOD_DARK): [0.55, 0.44, 0.36],
    tuple(CUSHION): [0.4, 0.55, 1.0],
    tuple(PAINT_RED): [1.0, 0.3, 0.2],
}


def high_seat_piece():
    """The High Seat as a building piece (defenses.json): the seat and pillars, and colliders for the steps, the seat,
    its back and the pillars. The mod puts the place to sit at (0, SEAT, -0.15), facing +z (HighSeatPiece). The game's
    longship head keeps its own material, so the preview's wood texture is left out."""
    parts = []
    for data in seat_parts():
        data = dict(data)
        if data.get("texture") == "wood":
            del data["texture"]
        if "tint" in data:
            data["tint"] = GAME_TINTS.get(tuple(data["tint"]), data["tint"])
        parts.append(data)
    colliders = [box((0, DAIS / 2, -0.05), (2.8, DAIS, 1.6)),
                 box((0, (DAIS + SEAT) / 2 + 0.1, -0.2), (1.5, SEAT - DAIS + 0.2, 0.7)),
                 box((0, SEAT + 0.75, -0.47), (1.6, 1.5, 0.12))]
    colliders += [box((x, (DAIS + PILLAR) / 2, -0.35), (0.36, PILLAR - DAIS, 0.36)) for x in (-1.2, 1.2)]
    return {"name": "highseat", "base": "wood_pole2", "parts": parts, "colliders": colliders, "snapPoints": [],
            "views": views(("front", 180, 10), ("side", 90, 10), ("angle", -140, 25))}


def huskarl_parts(huskarl, position):
    """A huskarl: the body with iron mail and trousers painted on, the mail and leg meshes, the kirtle's hem below the
    mail, the iron helmet with its mail hood, a spear upright by the right hand and a round shield leaning by the left
    leg. The helmet hides the hair; the beard shows under it."""
    name = f"hh_{huskarl['biome']}"
    at = {"position": list(position)}
    skin = "PlayerCharacter_01_fem" if huskarl["body"] == "female" else "PlayerCharacter_01"
    body = Image.open(TEXTURES / f"{skin}.png").convert("RGBA")
    kirtle = recolour("Tunic1body_d", f"{name}_chest", huskarl["main"])
    for layer in ("IronArmorLegs_d", kirtle, "IronArmorChestPlayer_d"):
        body.alpha_composite(Image.open(TEXTURES / f"{layer}.png").convert("RGBA").resize(body.size))
    body.save(TEXTURES / f"{name}_body.png")
    mesh = "uni_PlayerFemale" if huskarl["body"] == "female" else "uni_Player"
    parts = [dict(at, mesh=mesh, texture=f"{name}_body")]
    parts.append(dict(at, mesh="uni_ArmorIronChest", texture="IronArmorChest_d"))
    parts.append(dict(at, mesh="uni_ArmorIronLegs", texture="IronArmorLegs_d"))
    if huskarl["beard"]:
        parts.append(dict(at, mesh=huskarl["beard"], tint=list(huskarl["hair_colour"]), scale=[VISUAL_SCALE] * 3))
    x, y, z = position
    # The helmet is the game's own, exported on its own in the head's space (export_vanilla.py HelmetIron:attach).
    # In the head's space its crown points along +z; turned -90 degrees about x it points up.
    parts.append(part("helmet_iron", (x, y + HELMET[0], z + HELMET[1]), (-90, 0, 0), (HELMET[2],) * 3, texture="helmet_iron_d"))
    parts.append(part("preview_whspear", (x - HAND, y + 1.2, z), (-90, 0, 0), texture="whspear_albedo"))
    parts.append(part("preview_whroundshield", (x + 0.42, y + 0.45, z + 0.12), (80, -20, 0),
                      texture="whroundshield_albedo"))
    return parts


def captive_parts(position):
    """The captive: a torn, faded tunic, an iron cuff on each wrist and a chain from each cuff down to a ring on the
    stake behind him."""
    parts = trader_parts(CAPTIVE, position)
    x, y, z = position
    cuff = turned("hs_cuff", [(0.052, 0.0), (0.058, 0.01), (0.058, 0.05), (0.052, 0.06)])
    for side in (-1, 1):
        wrist = (x + side * WRIST[0], y + WRIST[1], z + WRIST[2])
        # The forearm runs out and down at about 39 degrees below level; the cuff sits round it.
        parts.append(part(cuff, (wrist[0] - side * 0.02, wrist[1] + 0.025, wrist[2]), (0, 0, side * 141), tint=IRON))
        ring = (x + side * 0.1, y + 0.55, z - 0.3)
        rotation, length = toward(wrist, ring)
        parts.append(part("chains", wrist, rotation, (1.0, length, 0.12), texture="chains_albedo"))
    parts.append(part("stake", (x, y, z - 0.4), scale=(0.7, 0.55, 0.7)))
    return parts


def portrait(name, parts):
    return {
        "name": name,
        "parts": parts,
        "views": [
            {"label": "front", "yaw": 180, "pitch": 3, "focus": [0, 1.02, 0], "radius": 1.25},
            {"label": "corner", "yaw": 215, "pitch": 6, "focus": [0, 1.02, 0], "radius": 1.25},
            {"label": "back", "yaw": 0, "pitch": 3, "focus": [0, 1.02, 0], "radius": 1.25},
        ],
    }


def main(render):
    people = [portrait(f"household_{person['biome']}", trader_parts(person, [0, 0, 0])) for person in FARMHANDS]
    people += [portrait(f"household_{person['biome']}", huskarl_parts(person, [0, 0, 0])) for person in HUSKARLS]
    people.append(portrait("household_captive", captive_parts([0, 0, 0])))
    seat = {
        "name": "highseat",
        "parts": high_seat() + trader_parts(FARMHANDS[0], [2.4, 0, 1.0], yaw=-20),
        "views": [
            {"label": "front", "yaw": 180, "pitch": 8, "focus": [0, 1.4, 0], "radius": 4.2},
            {"label": "corner", "yaw": 215, "pitch": 14, "focus": [0, 1.4, 0], "radius": 4.2},
            {"label": "seat", "yaw": 160, "pitch": 6, "focus": [-0.4, 1.5, -0.3], "radius": 2.2},
        ],
    }
    path = PREVIEW / "household_layout.json"
    path.write_text(json.dumps({"pieces": people + [seat]}, indent=1))
    if render:
        names = [piece["name"] for piece in people]
        subprocess.run(["powershell", "-ExecutionPolicy", "Bypass", "-File", str(HERE / "render_preview.ps1"), "-Layout", str(path),
                        "-Only", ",".join(names + ["highseat"])], check=True)
        contact_sheet(names, PREVIEW / "out" / "household.png")
        print(PREVIEW / "out" / "highseat.png")
    print(path)


if __name__ == "__main__":
    main("--no-render" not in sys.argv[1:])
