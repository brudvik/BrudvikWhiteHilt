"""The Husvette (house spirit), for a preview picture: a carved wooden gardvord, a small old man with a red pointed
cap and a grey beard, standing on a stump, with a bowl of porridge before him, as folk tradition says he must be given.
The plan is docs/plans/husvette.md; this is its first draft of the model.

The figure is made of simple turned shapes (cylinders, cones, spheres) with flat wood and paint colours, so the mod can
build the same mesh in code.

Usage: python build_husvette.py [--no-render]
Writes BrudvikWhiteHiltUnity/Preview/husvette_layout.json and renders Preview/out/husvette.png.
"""
import json
import math
import pathlib
import subprocess
import sys

import render_decor
from build_borgund import pole
from build_defenses import box, r3, views
from build_trading_post import CATALOG, MESHES, PREVIEW, custom_mesh

HERE = pathlib.Path(__file__).resolve().parent

WOOD = [0.55, 0.38, 0.24]  # oiled pine
WOOD_DARK = [0.38, 0.26, 0.17]
FACE = [0.72, 0.55, 0.42]
BEARD = [0.66, 0.64, 0.6]
CAP = [0.62, 0.12, 0.09]  # the nisse's red cap
EYES = [0.12, 0.1, 0.09]
PORRIDGE = [0.9, 0.82, 0.6]
BUTTER = [0.95, 0.8, 0.3]

SEGMENTS = 24
STUMP = 0.3  # the stump's top, where the figure stands; HusvetteModel in the mod uses the same


def husvette_piece():
    """The Husvette as a building piece (defenses.json): the stump, a log of the game's own, and the colliders. The mod
    adds the carved figure and the porridge in code (HusvetteModel), the same shapes as husvette() draws."""
    parts = [pole((0, -0.05, 0), (0, STUMP, 0), 0.27)]
    colliders = [box((0, STUMP / 2, 0), (0.56, STUMP, 0.56)), box((0, STUMP + 0.42, -0.04), (0.32, 0.84, 0.3))]
    return {"name": "husvette", "base": "wood_pole2", "parts": parts, "colliders": colliders, "snapPoints": [],
            "views": views(("front", 180, 10), ("side", 90, 10), ("angle", -140, 25))}


def lathe(name, profile, tilt=(0.0, 0.0)):
    """A turned shape: profile is [(radius, height), ...] from the bottom up, turned about the y axis. tilt bends the top
    over: the last point is moved by (x, z), the ones below in proportion to their height."""
    top = profile[-1][1] or 1.0
    vertices, triangles = [], []
    for radius, height in profile:
        lean = height / top
        for i in range(SEGMENTS):
            angle = 2 * math.pi * i / SEGMENTS
            vertices.append((radius * math.cos(angle) + tilt[0] * lean * lean, height, radius * math.sin(angle) + tilt[1] * lean * lean))
    for ring in range(len(profile) - 1):
        for i in range(SEGMENTS):
            a = ring * SEGMENTS + i
            b = ring * SEGMENTS + (i + 1) % SEGMENTS
            c = a + SEGMENTS
            d = b + SEGMENTS
            triangles += [a, c, b, b, c, d]
    # Close the bottom and the top with a fan, so no hole shows.
    for level, ring in ((profile[0][1], 0), (profile[-1][1], len(profile) - 1)):
        centre = len(vertices)
        vertices.append((tilt[0] * (level / top) ** 2, level, tilt[1] * (level / top) ** 2))
        for i in range(SEGMENTS):
            triangles += [centre, ring * SEGMENTS + i, ring * SEGMENTS + (i + 1) % SEGMENTS]
    custom_mesh(name, vertices, triangles)


def sphere(name, radius, squash=1.0):
    profile = [(radius * math.sin(math.pi * k / 10), radius * squash * (1 - math.cos(math.pi * k / 10))) for k in range(11)]
    lathe(name, profile)


def husvette():
    """The figure, about 1 m to the cap's tip, on a stump 0.3 m high; it faces +z. The porridge stands on the stump
    before his feet."""
    stump = STUMP
    # The stump: a short log, wide enough for the figure and his bowl, as the building piece has it.
    parts = list(husvette_piece()["parts"])
    # The body: a squat robe widening at the hem, with a belt.
    lathe("hv_body", [(0.15, 0.0), (0.16, 0.1), (0.13, 0.32), (0.1, 0.42), (0.07, 0.45)])
    parts.append({"mesh": "hv_body", "position": [0, stump, -0.05], "tint": WOOD})
    lathe("hv_belt", [(0.139, 0.0), (0.139, 0.035)])
    parts.append({"mesh": "hv_belt", "position": [0, stump + 0.24, -0.05], "tint": WOOD_DARK})
    # The head, the nose and the eyes.
    sphere("hv_head", 0.105)
    parts.append({"mesh": "hv_head", "position": [0, stump + 0.41, -0.03], "tint": FACE})
    sphere("hv_nose", 0.028)
    parts.append({"mesh": "hv_nose", "position": [0, stump + 0.43, 0.075], "tint": FACE})
    sphere("hv_eye", 0.012)
    for x in (-0.038, 0.038):
        parts.append({"mesh": "hv_eye", "position": [x, stump + 0.465, 0.062], "tint": EYES})
    # The beard down over his chest to his belt.
    lathe("hv_beard", [(0.02, 0.0), (0.07, 0.05), (0.085, 0.13), (0.075, 0.2), (0.0, 0.25)])
    parts.append({"mesh": "hv_beard", "position": [0, stump + 0.22, 0.01], "tint": BEARD})
    # The red cap, tall and bending back with its tip.
    lathe("hv_cap", [(0.112, 0.0), (0.108, 0.03), (0.08, 0.13), (0.04, 0.25), (0.0, 0.34)], tilt=(0.0, -0.11))
    parts.append({"mesh": "hv_cap", "position": [0, stump + 0.5, -0.04], "tint": CAP})
    lathe("hv_brim", [(0.116, 0.0), (0.116, 0.03)])
    parts.append({"mesh": "hv_brim", "position": [0, stump + 0.5, -0.04], "tint": BEARD})
    # The porridge on the stump before his feet, with a knob of butter in the middle.
    entries = {entry["id"]: entry for entry in json.loads(CATALOG.read_text(encoding="utf-8"))["pieces"]}
    bowl = dict(entries["bowl_carved"], height=0.08)
    name, _, _ = render_decor.load(bowl)
    parts.append({"mesh": f"preview_{name}", "position": r3((0.0, stump, 0.17))})
    lathe("hv_porridge", [(0.07, 0.0), (0.07, 0.01)])
    parts.append({"mesh": "hv_porridge", "position": r3((0.0, stump + 0.06, 0.17)), "tint": PORRIDGE})
    sphere("hv_butter", 0.018, squash=0.6)
    parts.append({"mesh": "hv_butter", "position": r3((0.0, stump + 0.068, 0.17)), "tint": BUTTER})
    return parts


def main(render):
    parts = husvette()
    piece = {
        "name": "husvette",
        "parts": parts,
        "views": [
            {"label": "front", "yaw": 180, "pitch": 8, "focus": [0, 0.55, 0], "radius": 0.7},
            {"label": "corner", "yaw": 215, "pitch": 14, "focus": [0, 0.55, 0], "radius": 0.7},
            {"label": "side", "yaw": 270, "pitch": 6, "focus": [0, 0.55, 0], "radius": 0.7},
        ],
    }
    path = PREVIEW / "husvette_layout.json"
    path.write_text(json.dumps({"pieces": [piece]}, indent=1))
    if render:
        subprocess.run(["powershell", "-ExecutionPolicy", "Bypass", "-File", str(HERE / "render_preview.ps1"), "-Layout", str(path),
                        "-Only", "husvette"], check=True)
        print(PREVIEW / "out" / "husvette.png")


if __name__ == "__main__":
    main("--no-render" not in sys.argv[1:])
