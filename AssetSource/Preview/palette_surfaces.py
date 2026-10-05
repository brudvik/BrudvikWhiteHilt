"""The surfaces the mod gives the models made of flat palette swatches (the wall drawer and the ship workshops), for
the preview: the same as BrudvikWhiteHilt/Helpers/PaletteSurfaces.cs.

Each swatch's faces become a part of their own, with texture coordinates in metres projected along each face's facing,
and the vanilla texture and tint of its surface. render_showcase.py uses parts() for the "surfaced" entries.
"""
import json
import pathlib

HERE = pathlib.Path(__file__).resolve().parent
MESHES = HERE.parents[1] / "BrudvikWhiteHiltUnity" / "Preview" / "vanilla"

# Texture, tint and metres one repeat covers: PaletteSurfaces.Top, Frame, Iron and Stone.
SURFACES = {
    "top": ("wood", [0.82, 0.74, 0.62], 0.9),
    "frame": ("Planks5c_low", [1.25, 1.15, 1.0], 0.8),
    "iron": ("metalwall", [0.42, 0.42, 0.45], 0.4),
    "stone": ("stone", [0.8, 0.8, 0.8], 0.7),
}

# Each model's swatches by the u coordinate of their middle: as WallDrawer.cs and ShipWorkshop.cs give them.
SWATCHES = {
    "walldrawer": [(0.167, "iron"), (0.5, "top"), (0.833, "frame")],
    "shipworkbench": [(0.167, "frame"), (0.5, "iron"), (0.833, "top")],
    "shipforge": [(0.167, "frame"), (0.5, "iron"), (0.833, "top")],
    "shipstonecutter": [(0.125, "iron"), (0.375, "frame"), (0.625, "stone"), (0.875, "top")],
}


# The scale the mod gives each model in the game, which the texture coordinates are in metres of: the wall drawer
# 0.7 x 0.3 x 0.22 m (WallDrawer.cs), the workshops 1.1 m wide (ShipWorkshop.cs).
GAME_SCALE = {
    "walldrawer": (0.7 / 2.34, 0.3 / 1.0, 0.22 / 1.36),
    "shipworkbench": (1.1 / 1.18,) * 3, "shipforge": (1.1 / 1.18,) * 3, "shipstonecutter": (1.1 / 1.10,) * 3,
}


def surface(layout_parts):
    """Replaces each part of a palette model in a layout with its surfaced parts, at the same place and scale."""
    out = []
    for part in layout_parts:
        model = part.get("mesh", "").removeprefix("preview_")
        if model not in SWATCHES:
            out.append(part)
            continue
        for surfaced in parts(model, GAME_SCALE[model]):
            out.append(dict(part, mesh=surfaced["mesh"], tint=surfaced["tint"]))
    return out


def parts(model, scale=(1, 1, 1)):
    """Writes the surfaced meshes of a model and returns its layout parts; texture coordinates in metres at a scale."""
    data = json.loads((MESHES / f"preview_{model}.json").read_text())
    source = data["parts"][0]
    v, n, uv, tri = source["vertices"], source.get("normals"), source["uvs"], source["triangles"]
    groups = {}
    for i in range(0, len(tri), 3):
        corners = tri[i:i + 3]
        u = uv[corners[0] * 2]
        role = min(SWATCHES[model], key=lambda swatch: abs(swatch[0] - u))[1]
        groups.setdefault(role, []).append(corners)
    layout = []
    for role, triangles in groups.items():
        metres = SURFACES[role][2]
        verts, norms, uvs = [], [], []
        for corners in triangles:
            p = [[v[k * 3 + i] * scale[i] for i in range(3)] for k in corners]
            e1 = [p[1][i] - p[0][i] for i in range(3)]
            e2 = [p[2][i] - p[0][i] for i in range(3)]
            facing = [e1[1] * e2[2] - e1[2] * e2[1], e1[2] * e2[0] - e1[0] * e2[2], e1[0] * e2[1] - e1[1] * e2[0]]
            axis = max(range(3), key=lambda i: abs(facing[i]))
            length = sum(x * x for x in facing) ** 0.5 or 1
            for k, point in zip(corners, p):
                verts += [v[k * 3 + i] for i in range(3)]
                norms += [n[k * 3 + i] for i in range(3)] if n else [x / length for x in facing]
                s, t = (point[2], point[1]) if axis == 0 else (point[0], point[2]) if axis == 1 else (point[0], point[1])
                uvs += [s / metres, t / metres]
        name = f"surfaced_{model}_{role}"
        mesh = {"name": name, "parts": [{"texture": SURFACES[role][0], "vertices": verts, "normals": norms, "uvs": uvs,
                                         "triangles": list(range(len(verts) // 3))}]}
        (MESHES / f"{name}.json").write_text(json.dumps(mesh))
        layout.append({"mesh": name, "tint": SURFACES[role][1]})
    return layout
