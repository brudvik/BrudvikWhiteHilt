"""Builds the carved friezes for the main asset bundle: one model per interlace pattern.

Usage: python build_friezes.py [--blender <blender.exe>]

For each pattern, make_knotwork.py draws the carving 2 m long and 0.3 m high, blender_relief.py turns it into a relief
whose coarse grid (CELL) carries the strands' shape, and the normal map carries the finer detail the grid cannot.
Writes AssetSource/Models/carve_<pattern>.glb (with its albedo, which build_foraging_bundle.ps1 converts) and
AssetSource/Textures/carve_<pattern>_normal.png (copied into the bundle as the model's normal map, which
VanillaMeshLibrary sets on its material). The layout places them: build_carvings.py in AssetSource/Preview.
"""

import argparse
import pathlib
import subprocess
import sys

HERE = pathlib.Path(__file__).resolve().parent
MODELS = HERE.parent / "Models"
TEXTURES = HERE.parent / "Textures"
WORK = HERE / "Work"  # git-ignored intermediate maps
PATTERNS = ["flette", "tau", "ringkjede", "slyng"]
LENGTH, WIDTH = 2.0, 0.3
PPM = 512  # pixels per metre: 1024 x 154 for a frieze
DEPTH = 0.03  # how far the carving stands out
CELL = 0.012  # the relief mesh's grid
TRIS = 2500


def find_blender(given):
    if given:
        return given
    found = sorted(pathlib.Path(r"C:\Program Files\Blender Foundation").glob("*/blender.exe"))
    if not found:
        sys.exit("Blender not found; pass --blender")
    return str(found[-1])


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--blender")
    args = parser.parse_args()
    blender = find_blender(args.blender)
    WORK.mkdir(exist_ok=True)
    for pattern in PATTERNS:
        prefix = WORK / f"carve_{pattern}"
        subprocess.run([sys.executable, str(HERE / "make_knotwork.py"), pattern, str(prefix), str(LENGTH), str(WIDTH), str(PPM),
                        str(DEPTH), str(CELL)], check=True)
        result = subprocess.run([blender, "-b", "--factory-startup", "--python-exit-code", "1", "-P", str(HERE / "blender_relief.py"), "--",
                                 f"{prefix}_height.png", str(MODELS / f"carve_{pattern}.glb"), str(LENGTH), str(DEPTH), str(TRIS),
                                 f"{prefix}_albedo.png", str(CELL)], capture_output=True, text=True)
        if result.returncode != 0:
            print(result.stdout[-2000:], result.stderr[-2000:])
            sys.exit(f"{pattern}: Blender failed")
        print([line for line in result.stdout.splitlines() if "faces" in line][-1])
        (TEXTURES / f"carve_{pattern}_normal.png").write_bytes(pathlib.Path(f"{prefix}_normal.png").read_bytes())


if __name__ == "__main__":
    main()
