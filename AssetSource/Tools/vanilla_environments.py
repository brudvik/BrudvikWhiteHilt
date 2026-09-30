"""Prints Valheim's weather table, read from the game's bundles: every environment with its fog (day/night), wind range
and whether it is wet, and which environments each biome rolls with their weights. Use it to pick weather names for
config (e.g. the Kraken's FogWeathers) instead of guessing.

Usage:
  python vanilla_environments.py
Takes a while the first time: it looks for the bundle holding EnvMan by its text.
"""
import sys

import UnityPy

sys.path.insert(0, __import__("os").path.dirname(__file__))
import vanilla_prefab  # noqa: E402


def main():
    for path in sorted(vanilla_prefab.GAME.rglob("*")):
        if not path.is_file() or path.stat().st_size < 10000:
            continue
        data = path.read_bytes()
        if b"m_fogDensityNight" not in data and b"ThunderStorm" not in data:
            continue
        try:
            env = UnityPy.load(str(path))
        except Exception:
            continue
        for obj in env.objects:
            if obj.type.name != "MonoBehaviour":
                continue
            try:
                tree = obj.read_typetree()
            except Exception:
                continue
            if "m_environments" not in tree or "m_biomes" not in tree:
                continue
            print(f"EnvMan in {path.name}")
            for biome in tree["m_biomes"]:
                weights = ", ".join(f"{entry['m_environment']} {entry['m_weight']:g}" for entry in biome["m_environments"])
                print(f"  biome {biome.get('m_name')} ({biome.get('m_biome')}): {weights}")
            for setup in tree["m_environments"]:
                print(f"  env {setup['m_name']:24} fog day {setup.get('m_fogDensityDay', 0):.3f} night {setup.get('m_fogDensityNight', 0):.3f}"
                      f"  wind {setup.get('m_windMin', 0):.1f}-{setup.get('m_windMax', 0):.1f}  wet {setup.get('m_isWet')}")
            return
    print("EnvMan not found")


if __name__ == "__main__":
    main()
