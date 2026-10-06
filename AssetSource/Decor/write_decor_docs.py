"""Writes docs/decor.md from decor.json, so the page lists exactly what the Decor Hammer builds.

The text around the tables is in this script; the tables come from the catalogue. Item names in the costs are the
English names the game shows. Run after changing decor.json: python write_decor_docs.py
"""
import json
import pathlib

HERE = pathlib.Path(__file__).resolve().parent
REPO = HERE.parents[1]

# Display names of the materials the catalogue uses.
ITEMS = {
    "Wood": "Wood", "Stone": "Stone", "Resin": "Resin", "Flint": "Flint", "FineWood": "Fine Wood", "Bronze": "Bronze",
    "Iron": "Iron", "Copper": "Copper", "Chain": "Chain", "Coal": "Coal", "LeatherScraps": "Leather Scraps",
    "WolfPelt": "Wolf Pelt", "LinenThread": "Linen Thread", "BoneFragments": "Bone Fragments", "ElderBark": "Ancient Bark",
    "YggdrasilWood": "Yggdrasil Wood", "Blackwood": "Ashwood", "BeechSeeds": "Beech Seeds", "FirCone": "Fir Cone",
    "Fiddleheadfern": "Fiddlehead", "Thistle": "Thistle", "Dandelion": "Dandelion", "Mushroom": "Mushroom",
    "MushroomYellow": "Yellow Mushroom", "MushroomBlue": "Blue Mushroom", "MushroomMagecap": "Magecap",
    "MushroomJotunPuffs": "Jotun Puffs", "Blueberries": "Blueberries", "Raspberry": "Raspberries", "Cloudberry": "Cloudberries",
    "Flax": "Flax", "Barley": "Barley", "Onion": "Onion", "Turnip": "Turnip", "Carrot": "Carrot", "FishRaw": "Raw Fish",
}

TABS = {
    "Garden": ("🌿 Garden", "Bushes, young trees, ferns, flowers, mushrooms and berry bushes. All of them sway in the wind and bend when you walk through them, as Valheim's own plants do, and none of them can be picked or chopped."),
    "Wilds": ("🪨 Wilds", "Stones, stumps, logs and moss to make a garden or a path look as if it has always been there."),
    "Hearth": ("🍲 Hearth", "Barrels, crates, baskets, bowls, pots and food for the kitchen and the storehouse."),
    "Workshop": ("🔨 Workshop", "Tools, firewood, fences, chains and a trader's wagon for the yard and the smithy."),
    "Home": ("🪑 Home", "Tables, stools and chairs you can sit on, shelves, boxes and pots, from the meadows to the Ashlands."),
    "Textiles": ("🧵 Textiles", "Hanging cloth, hides, curtains, banners and runner rugs."),
    "Lights": ("🕯️ Lights", "Candles, lanterns and fires that burn without fuel. They give light, not heat or comfort."),
    "Norse": ("ᚱ Norse", "Runestones, graves, a dolmen, fuling totems, wrecked ships and other pieces of the old world."),
}


def cost(text):
    parts = []
    for part in text.split(","):
        item, _, amount = part.partition(":")
        parts.append(f"{ITEMS.get(item, item)} ×{amount or 1}")
    return ", ".join(parts)


def notes(entry):
    found = []
    if entry.get("seat"):
        found.append("seat")
    if entry.get("light"):
        found.append("light")
    if "vanilla" in entry:
        found.append("Valheim's own look")
    return ", ".join(found)


def main():
    catalog = json.loads((HERE / "decor.json").read_text(encoding="utf-8"))
    pieces = catalog["pieces"]
    lines = [
        "# 🪴 Decor Hammer",
        "",
        "[← Back to the README](../README.MD)",
        "",
        "The White Hilt Decor Hammer builds decorations only: plants that sway in the wind, stones and stumps, kitchen and"
        " workshop things, furniture, cloth, lights and Norse pieces. There are "
        f"{len(pieces)} of them under eight tabs.",
        "",
        "| Item | Description | Crafting Station | Requirements |",
        "|------|-------------|------------------|--------------|",
        "| **White Hilt Decor Hammer** | Everlasting build tool for decorations. Its pieces show up once you know their"
        " materials | Workbench | Wood ×4, Stone ×2, Resin ×2 |",
        "",
        "- A decoration shows up in the menu once you know all its materials, as vanilla pieces do, so new ones appear as"
        " you reach new biomes.",
        "- Plants and things from the wild (Garden and Wilds) can be placed anywhere. Everything else needs a workbench"
        " nearby, as vanilla furniture does.",
        "- Removing a decoration gives its materials back, so rearranging a room costs nothing.",
        "- Plants, cloth and small things on tables let you walk through them; the hammer still removes them.",
        "- Decorations need no support: a lantern can hang from a beam and a pot can stand on a shelf.",
        "- Stools, chairs and benches can be sat on.",
        "- Bushes, trees and other pieces with Valheim's own look are copies of their looks only: they cannot be chopped,"
        " picked, mined or looted.",
        "",
    ]
    for tab, (title, text) in TABS.items():
        entries = [entry for entry in pieces if entry["category"] == tab]
        lines += [f"## {title}", "", text, ""]
        image = REPO / "docs" / "images" / f"decor_{tab.lower()}.png"
        if image.exists():
            lines += [f'<img src="images/{image.name}" alt="{tab}" title="{tab}" height="260">', ""]
        lines += ["| Decoration | Description | Requirements | Notes |", "|------------|-------------|--------------|-------|"]
        for entry in entries:
            lines.append(f"| **{entry['name']}** | {entry['description']} | {cost(entry['cost'])} | {notes(entry)} |")
        lines.append("")

    lines += [
        "## ⚙️ Settings",
        "",
        "The hammer has the usual `[Content]`, `[Recipes]` and `[Tiers]` entries under `WhiteHiltDecorHammer` (see"
        " [Settings & progression](progression.md)). Turning it off removes the hammer; decorations already built stay.",
        "",
        "## 🧱 For modders",
        "",
        "Every decoration is one line in [`AssetSource/Decor/decor.json`](../AssetSource/Decor/decor.json), which the mod"
        " embeds. A new one with a vanilla look needs nothing else. One with a model from"
        " [Poly Haven](https://polyhaven.com) or a downloaded `.glb` is prepared by `AssetSource/Decor/prepare_decor.py`"
        " and built into the decor bundle by `AssetSource/build_foraging_bundle.ps1 -DecorOnly`. See"
        " [`AssetSource/Decor/README.md`](../AssetSource/Decor/README.md) for the fields, and"
        " [How the mod is built](architecture.md) for how the pieces are made.",
        "",
    ]
    (REPO / "docs" / "decor.md").write_text("\n".join(lines), encoding="utf-8")
    print(f"docs/decor.md: {len(pieces)} decorations")


if __name__ == "__main__":
    main()
