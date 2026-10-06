# Decor Hammer catalogue

`decor.json` lists every piece of the White Hilt Decor Hammer, one per line. The mod embeds the file and builds a
piece from each entry when it starts (`BrudvikWhiteHilt/Decor/DecorPieceFactory.cs`), so a new decoration needs no
code.

## Fields

| Field | Meaning |
|-------|---------|
| `id` | Short, unique, lower case. The piece is `piece_whitehilt_decor_<id>`, its model `decor_<id>`. |
| `category` | The build menu tab: `Garden`, `Wilds`, `Hearth`, `Workshop`, `Home`, `Textiles`, `Lights` or `Norse`. Keep the file grouped by tab in this order; the menu shows the tabs in the order they first appear. |
| `vanilla` | A vanilla prefab whose look is copied (meshes, LODs, cloth, lights, particles only). Vegetation keeps its wind. |
| `source` | A model to prepare: `polyhaven:<asset>` (CC0, downloaded) or `file:<name>.glb` (from the folder given to `prepare_decor.py --sources`). |
| `bundle` | A model the main bundle already has (`AssetSource/Models/<name>.glb`). |
| `objects` | With `source`: the node names to keep from the file; leave out to keep everything. |
| `tris` | With `source`: about how many triangles to keep (default 3000). |
| `height` | Height in metres. `prepare_decor.py` fills it in from the model's real size when it is missing. |
| `scale` | Extra uniform scale, mostly to shrink a large vanilla prop. |
| `wind` | `true` for plants: they sway in the wind and bend when walked through. |
| `solid` | `false` lets players walk through it. Plants and cloth default to `false`, everything else to `true`. |
| `material` | `wood` (default), `stone`, `metal` or `cloth`: health, break effects and the piece material a model is lit with. |
| `light` | `candle`, `lantern` or `fire`: a flame and a light that need no fuel. |
| `seat` | Seat height in metres; makes it a chair. |
| `cost` | `Item:amount,Item:amount` with Valheim's prefab names. The materials are given back when it is removed. |
| `name`, `description` | English texts. The Norwegian ones go in `BrudvikWhiteHilt/Translations/Norwegian.json` as `piece_whitehilt_decor_<id>` and `..._description`. |
| `credit` | Required for a `file:` model: `<Title> by <Author> (<licence>)`, also added to the README's credits. |

## Adding a decoration

1. Add the line to `decor.json` and its Norwegian texts.
2. For a `source` model: `python prepare_decor.py --only <id>` writes `Models/<id>.glb` (needs Blender), then
   `..\build_foraging_bundle.ps1 -DecorOnly` rebuilds `BrudvikWhiteHilt/Assets/whitehilt_decor` (needs Unity).
3. `python write_decor_docs.py` updates `docs/decor.md`, and `python ..\Preview\render_decor.py` the gallery images.
4. `dotnet test BrudvikWhiteHilt.Tests` checks the ids, tabs, translations, models and credits.

`Download/` holds Poly Haven's original files and is not committed.
