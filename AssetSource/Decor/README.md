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
| `texture` | With `source`: the largest texture side in pixels (default 512); more for carving or runes that must stay legible. |
| `cutBelow` | With `source`: the share of the height at the bottom to cut away, for a scan standing on the floor it was scanned on. |
| `height` | Height in metres. `prepare_decor.py` fills it in from the model's real size when it is missing. |
| `scale` | Extra uniform scale, mostly to shrink a large vanilla prop. |
| `wind` | `true` for plants: they sway in the wind and bend when walked through. |
| `solid` | `false` lets players walk through it. Plants and cloth default to `false`, everything else to `true`. |
| `material` | `wood` (default), `stone`, `metal` or `cloth`: health, break effects and the piece material a model is lit with. |
| `light` | `candle`, `lantern` or `fire`: a flame and a light that need no fuel. |
| `lightAt` | Where the flame sits, as a share of the height from the foot (0 to 1); by default the top of a candle, the middle of a lantern and the bottom of a fire. |
| `flames` | Where each flame sits, `[[x, y, z], ...]` as shares (0 to 1) of the model's bounds, x and z as in the glb, y from the foot; replaces `lightAt`. One point per candle, at the wick or at the foot of a flame the model has; the light goes in the middle of them. See **Flames** below. |
| `seat` | Height of the seat surface in metres; makes it a chair. See **Chairs, stools and benches** below. |
| `sizes` | Size copies to add as pieces of their own, `<id>_<size>`: `small` (0.6 times the size, half the cost), `large` (1.5 times, half again the cost) or `huge` (twice, double). Not for tools, seats or anything that has a real size. A copy's id must not be another entry's (`rock_mossy_small` is). Its Norwegian name is the decoration's with `(liten)`, `(stor)` or `(svær)`. |
| `seatYaw` | Which way one sits, in degrees about the vertical from the model's +z (default 0). `180` for a chair whose back is on +z. |
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

## Size

`prepare_decor.py` fills in a model's real height, but Valheim is not built to life size, and a decoration at its real
size looks lost next to the game's own things. Scale it as Valheim scales its own:

- **Small things in the hand, on a table or a shelf**, about twice life size: Valheim's tankard is 0.26 m high, an onion
  0.27 m, a mushroom 0.37 m. Mugs and cups 0.2 to 0.25 m, plates 0.4 to 0.5 m across, a spoon 0.45 m.
- **Tools and weapons**, about one and a half times: the vanilla hammer is 0.47 m long, the flint knife 0.36 m, the
  bronze axe 0.9 m, the iron sword 1.27 m.
- **Furniture**, about a quarter more: the vanilla table is 0.83 m high and 2.5 m long, the bench 0.54 m high, the chest
  1.65 m wide. See also the chairs below.
- **Candles and lamps** between the two: a candlestick about 0.35 m, a wall candle 0.5 m, a lantern 0.6 m (the vanilla
  wall torch is 0.95 m).

A `small` copy is 0.6 times the size, so give `sizes` only to what is still worth placing at that size.

## Flames

Without `flames` the flame burns in the middle of the model's bounds, at `lightAt`. That is right for a candle on a
round foot, but wrong for a wall candle (the plate behind it widens the bounds), a candelabrum (several candles and
none in the middle) or a lantern with a tall handle. Find the candles with
`python find_flames.py <id> <from> <to>`: it groups the model's vertices between two shares of its height by where they
stand and prints each group's place as shares, ready for `flames`. Take the band just below the top of the candles; a
Poly Haven candleholder has its own flame meshes, and the flame goes at their foot so the burning one covers them.

## Chairs, stools and benches

Check every new seat on three points; the first decor chairs got all three wrong.

1. **Size like Valheim's own**, not the real-world size `prepare_decor.py` fills in: Valheim's furniture is bigger
   than life. Stools about 0.55 m high (the vanilla stool is 0.6), chairs 1.2 to 1.3 m to the top of the back, benches
   about 0.5 m. Raise `height` and set `seat` to match (`seat` is about the stool's height, and about 0.45 of a chair's).
2. **`seat` is the seat surface, not where the body goes.** The sitting animation lifts the body about half a metre
   above the chair's attach point, so `DecorPieceFactory.AddSeat` puts the point `SitLift` (0.5 m) below the seat,
   never lower than `LowestAttach` (-0.15 m), as Valheim's chairs do (their attach points are 0 to 0.1 m high under
   seats of 0.5 to 0.6 m). Never set `seat` to a lower value to fix a body that floats: fix the model's size instead.
3. **The back on -z.** One sits facing +z, so a chair's back must rise on its -z side. Measure which side it is on
   before adding it: take the vertices well above the seat (the back) and see whether their mean lies at -z or +z of
   the middle. For a bundle model use `BrudvikWhiteHiltUnity/Assets/Decor/decor_<id>.obj` (Unity mirrors x on import,
   z is as written); for a vanilla prop its mesh, e.g. via `AssetSource/Tools/vanilla_prefab.py`. A back on +z needs
   `"seatYaw": 180` (the Wooden Chair has it); one on ±x needs 90 or 270. Stools and backless benches face either way.

Then sit on it in the game: the body should rest on the seat with the back behind it. Existing pieces in the world
take new sizes and seats when the game loads, since they come from the prefab.

## Support

`DecorPieceFactory.SetUpWearNTear` sets two flags whose names mislead:

- `m_supports` is whether the piece carries what is set on it. With `false`, Valheim's placement check
  (`Player.UpdatePlacementGhost`) refuses anything aimed at the piece, so no pot could stand on a decor table. Solid
  decorations have `true`; plants and cloth, which one walks through, `false`.
- `m_noSupportWear = true` means the piece is *worn down* without support, the opposite of what it says. Decorations
  have `false`, so a lantern may hang in the air.
