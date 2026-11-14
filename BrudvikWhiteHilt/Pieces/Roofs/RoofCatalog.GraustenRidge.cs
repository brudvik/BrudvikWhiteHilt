using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Roofs;

/// <summary>
/// The ridges the game's red Grausten Roof lacks, for its slope and for its arched slope. The vanilla roof
/// (<c>piece_grausten_roof_45</c>) comes as a slope, corners and arches, but has nothing for the top: where its two
/// slopes end 2 m apart, a gap is left, which the thatch roof fills with its ridge (<c>wood_roof_top_45</c>). Both roofs
/// have the same 2 m by 2 m slope and the same snap points, so these ridges snap as the thatch ridge does. They are built
/// from the vanilla roof's own tiles: the lower half of its rows of tiles on each side, rising 1 m to the peak, with a
/// row of round ridge tiles over the top. Under the tiles the plain ridge has the vanilla slope's stone underlay, cut to
/// the same half; the arched ridge a vault of stone that carries on the curve of the arched slopes, so two of them and
/// the ridge make one barrel vault. The other pieces of the red roof come from <see cref="RoofMeshBuilder"/> as the
/// <see cref="RoofCovering.RedGrausten"/> covering.
/// </summary>
public static partial class RoofCatalog
{
    private const string VanillaGraustenRoof = "piece_grausten_roof_45";
    private const string VanillaGraustenPrefix = "piece_grausten_roof";
    private const string VanillaRidge = "wood_roof_top_45";
    private const int StoneSubmesh = 0;
    private const int TileSubmesh = 1;
    private const float KeepRowsBelow = 0.1f;
    private const float RidgeTileRadius = 0.16f;
    private const float RidgeTileLength = 0.42f;
    private const float RidgeHalfLength = 1.22f;
    private const int RidgeTileSides = 16;
    private const int VaultSteps = 16;
    private const float VaultCrown = -0.25f;
    private const float VaultCurve = 0.255f;
    private const float VaultTop = 0.2f;

    private static readonly float Root2 = Mathf.Sqrt(2f);

    // Up the slope and out of it, in the vanilla roof's own space, where the slope runs from (y -1, z 1) to (y 1, z -1).
    private static readonly Vector3 upSlope = new Vector3(0f, 1f, -1f) / Mathf.Sqrt(2f);
    private static readonly Vector3 outOfSlope = new Vector3(0f, 1f, 1f) / Mathf.Sqrt(2f);

    private static readonly List<GameObject> graustenRidges = new();

    // A clone of the vanilla Grausten Roof (its health, stone, ash resistance, Stonecutter and build menu group), with its
    // look, colliders and snap points replaced by those of a ridge. Named, priced and switched on and off as a piece of the
    // red Grausten roof covering.
    private static void CreateGraustenRidge(bool arched)
    {
        GameObject vanilla = PrefabManager.Instance.GetPrefab(VanillaGraustenRoof) ?? throw new InvalidOperationException($"{VanillaGraustenRoof} not found");
        GameObject thatchRidge = PrefabManager.Instance.GetPrefab(VanillaRidge) ?? throw new InvalidOperationException($"{VanillaRidge} not found");
        RoofFamily family = RoofFamily.Get(RoofCovering.RedGrausten);
        string shapeToken = arched ? "whitehilt_roofshape_archedridge" : "whitehilt_roofshape_ridge";
        PieceConfig config = new()
        {
            Name = Translations.Token(family.NameKey) + " " + Translations.Token(shapeToken) + " 45°",
            Description = Translations.Token($"{family.NameKey}_description"),
            PieceTable = PieceTables.Hammer,
            Requirements = ToConfig(RoofSettings.Recipe(RoofCovering.RedGrausten))
        };

        string prefabName = arched
            ? $"piece_whitehilt_roof_{RoofCoverings.Id(RoofCovering.RedGrausten)}_archedridge_45"
            : PrefabName(RoofCovering.RedGrausten, RoofShape.Ridge, RoofPitch.Medium);
        CustomPiece piece = new(prefabName, VanillaGraustenRoof, config);
        GameObject prefab = piece.PiecePrefab;
        foreach (Transform child in prefab.transform.Cast<Transform>().ToList())
        {
            UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        foreach (Component component in prefab.GetComponents<Collider>().Cast<Component>().Concat(prefab.GetComponents<LODGroup>()))
        {
            UnityEngine.Object.DestroyImmediate(component);
        }

        foreach (Transform snap in thatchRidge.transform.Cast<Transform>().Where(child => child.CompareTag("snappoint")))
        {
            GameObject copy = new(snap.name) { tag = "snappoint", layer = prefab.layer };
            copy.transform.SetParent(prefab.transform, false);
            copy.transform.localPosition = snap.localPosition;
            copy.transform.localRotation = snap.localRotation;
            copy.SetActive(false);
        }

        GameObject visual = new("New") { layer = prefab.layer };
        visual.transform.SetParent(prefab.transform, false);
        AddRidgeColliders(prefab);

        WearNTear wearNTear = prefab.GetComponent<WearNTear>() ?? throw new InvalidOperationException($"{prefab.name} has no WearNTear.");
        wearNTear.m_new = visual;
        wearNTear.m_worn = visual;
        wearNTear.m_broken = visual;
        wearNTear.m_wet = null;
        wearNTear.m_snow = null;
        wearNTear.m_snowWorn = null;
        wearNTear.m_snowBroken = null;
        wearNTear.m_fragmentRoots = null;
        wearNTear.m_health = RoofSettings.Health(RoofCovering.RedGrausten);

        RoofInfo info = prefab.AddComponent<RoofInfo>();
        info.m_covering = RoofCovering.RedGrausten;
        info.m_shape = RoofShape.Ridge;
        info.m_pitch = RoofPitch.Medium;

        if (!VisualHelper.IsHeadless)
        {
            BuildGraustenRidgeLook(vanilla, visual.transform, arched);
            Sprite icon = VisualHelper.RenderIcon(prefab);
            if (icon != null)
            {
                piece.Piece.m_icon = icon;
            }
        }

        piece.Piece.m_enabled = RoofSettings.IsEnabled(RoofCovering.RedGrausten);
        PieceManager.Instance.AddPiece(piece);
        graustenRidges.Add(prefab);
    }

    // The vanilla roof's stone and tile materials, in the order of its submeshes.
    private static Material[] VanillaGraustenMaterials()
    {
        MeshRenderer renderer = VanillaGraustenModel(out _);
        return renderer.sharedMaterials;
    }

    // The renderer and the readable mesh of the vanilla roof, with stone in its first submesh and tiles in its second.
    private static MeshRenderer VanillaGraustenModel(out MeshFilter filter)
    {
        GameObject vanilla = PrefabManager.Instance.GetPrefab(VanillaGraustenRoof) ?? throw new InvalidOperationException($"{VanillaGraustenRoof} not found");
        Transform model = vanilla.transform.Find("New/default");
        filter = model != null ? model.GetComponent<MeshFilter>() : null;
        if (filter == null)
        {
            Transform look = vanilla.transform.Find("New");
            filter = look != null ? look.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault() : null;
        }

        MeshRenderer renderer = filter != null ? filter.GetComponent<MeshRenderer>() : null;
        Mesh source = filter != null ? filter.sharedMesh : null;
        if (source == null || renderer == null || !source.isReadable || source.subMeshCount <= TileSubmesh || renderer.sharedMaterials.Length <= TileSubmesh)
        {
            throw new InvalidOperationException($"{VanillaGraustenRoof} has no readable mesh with stone and tiles.");
        }

        return renderer;
    }

    // One slab on each side, as the vanilla roof's collider is one slab for its slope, and a beam along the peak.
    private static void AddRidgeColliders(GameObject prefab)
    {
        GameObject holder = new("collider") { layer = prefab.layer };
        holder.transform.SetParent(prefab.transform, false);
        foreach (float side in new[] { 1f, -1f })
        {
            GameObject slab = new("slab") { layer = prefab.layer };
            slab.transform.SetParent(holder.transform, false);
            slab.transform.localPosition = new Vector3(0f, 0.533f, 0.533f * side);
            slab.transform.localRotation = Quaternion.Euler(45f * side, 0f, 0f);
            BoxCollider box = slab.AddComponent<BoxCollider>();
            box.size = new Vector3(2f, 0.2f, Root2);
        }
    }

    // The vanilla roof's tiles, cut to the lower half of the slope, once on each side, the stone under them (the vanilla
    // underlay cut the same way, or for the arched ridge a vault), and the ridge tiles over the peak.
    private static void BuildGraustenRidgeLook(GameObject vanilla, Transform visual, bool arched)
    {
        MeshRenderer renderer = VanillaGraustenModel(out MeshFilter filter);
        Matrix4x4 toRoof = vanilla.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
        Material[] shared = renderer.sharedMaterials;
        AddModel(visual, "roof", HalfSlopes(filter.sharedMesh, toRoof, !arched, out float tileTop), new[] { shared[StoneSubmesh], shared[TileSubmesh] });
        AddModel(visual, "ridge", RidgeTiles(tileTop), new[] { shared[TileSubmesh] });
        if (arched)
        {
            AddModel(visual, "vault", Vault(), new[] { shared[StoneSubmesh] });
        }
    }

    // The stone under the arched ridge, along x: its top 0.2 m over the plane of the slopes, under the tiles, its
    // underside a curve that goes on from where the arched slopes end at the edges of the ridge and rises, flattening,
    // to its crown in the middle.
    private static Mesh Vault()
    {
        List<Vector3> vertices = new();
        List<Vector3> normals = new();
        List<Vector2> uvs = new();
        List<int> triangles = new();
        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 facing)
        {
            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            bool flip = Vector3.Dot(normal, facing) < 0f;
            int start = vertices.Count;
            foreach (Vector3 point in new[] { a, b, c, d })
            {
                vertices.Add(point);
                normals.Add(flip ? -normal : normal);
                uvs.Add(Mathf.Abs(facing.x) > 0.5f ? new Vector2(point.z, point.y) * 0.5f : new Vector2(point.x, point.z + point.y) * 0.5f);
            }

            triangles.AddRange(flip ? new[] { start, start + 2, start + 1, start, start + 3, start + 2 } : new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
        }

        float Top(float z) => 1f - Mathf.Abs(z) + VaultTop;
        float Bottom(float z) => VaultCrown - VaultCurve * z * z;
        for (int i = 0; i < VaultSteps; i++)
        {
            float z0 = -1f + 2f * i / VaultSteps;
            float z1 = -1f + 2f * (i + 1) / VaultSteps;
            Vector3 up0 = new(0f, Top(z0), z0);
            Vector3 up1 = new(0f, Top(z1), z1);
            Vector3 down0 = new(0f, Bottom(z0), z0);
            Vector3 down1 = new(0f, Bottom(z1), z1);
            Vector3 left = Vector3.left;
            Vector3 right = Vector3.right;
            float zm = (z0 + z1) / 2f;
            Quad(up0 - right, up0 + right, up1 + right, up1 - right, new Vector3(0f, 1f, -Mathf.Sign(zm)));
            Quad(down0 - right, down0 + right, down1 + right, down1 - right, Vector3.down);
            Quad(up0 + left, up1 + left, down1 + left, down0 + left, left);
            Quad(up0 + right, up1 + right, down1 + right, down0 + right, right);
        }

        foreach (float z in new[] { -1f, 1f })
        {
            Vector3 top = new(0f, Top(z), z);
            Vector3 bottom = new(0f, Bottom(z), z);
            Quad(top + Vector3.left, top + Vector3.right, bottom + Vector3.right, bottom + Vector3.left, new Vector3(0f, 0f, z));
        }

        Mesh mesh = new() { name = "grausten_roof_45_arched_ridge_vault" };
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        return mesh;
    }

    // The two sides of the ridge as one mesh with the vanilla submeshes. Each side is the lower half of the vanilla
    // slope: from its bottom edge, where the slope below ends, to its middle, 1 m higher, at the peak. The side facing
    // +z is the vanilla slope moved 1 m up; the other is that turned round.
    private static Mesh HalfSlopes(Mesh source, Matrix4x4 toRoof, bool withUnderlay, out float tileTop)
    {
        Vector3[] vertices = source.vertices.Select(vertex => toRoof.MultiplyPoint3x4(vertex)).ToArray();
        Vector3[] normals = source.normals.Select(normal => toRoof.MultiplyVector(normal).normalized).ToArray();
        Vector2[] uvs = source.uv;
        bool hasUv = uvs.Length == vertices.Length;
        bool hasNormals = normals.Length == vertices.Length;

        int[] tiles = KeepLowerRows(source.GetTriangles(TileSubmesh), vertices);
        tileTop = tiles.Length == 0 ? 0.28f : tiles.Max(index => Vector3.Dot(vertices[index], outOfSlope));
        (int[] stone, Dictionary<int, (Vector3 Position, Vector2 Uv)> moved) = withUnderlay
            ? CutUnderlay(source.GetTriangles(StoneSubmesh), vertices, normals, uvs, hasUv)
            : (new int[0], new Dictionary<int, (Vector3, Vector2)>());

        List<Vector3> outVertices = new();
        List<Vector3> outNormals = new();
        List<Vector2> outUvs = new();
        List<int>[] outTriangles = { new(), new() };
        foreach (bool turned in new[] { false, true })
        {
            Dictionary<int, int> remap = new();
            int Map(int index)
            {
                if (remap.TryGetValue(index, out int mapped))
                {
                    return mapped;
                }

                Vector3 position = moved.TryGetValue(index, out var cut) ? cut.Position : vertices[index];
                Vector2 uv = moved.TryGetValue(index, out cut) ? cut.Uv : hasUv ? uvs[index] : Vector2.zero;
                Vector3 normal = hasNormals ? normals[index] : Vector3.up;
                if (turned)
                {
                    position = new Vector3(-position.x, position.y, -position.z);
                    normal = new Vector3(-normal.x, normal.y, -normal.z);
                }

                position.y += 1f;
                mapped = outVertices.Count;
                outVertices.Add(position);
                outNormals.Add(normal);
                outUvs.Add(uv);
                remap[index] = mapped;
                return mapped;
            }

            outTriangles[0].AddRange(stone.Select(Map));
            outTriangles[1].AddRange(tiles.Select(Map));
        }

        Mesh mesh = new() { name = "grausten_roof_45_ridge" };
        mesh.SetVertices(outVertices);
        mesh.SetNormals(outNormals);
        mesh.SetUVs(0, outUvs);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(outTriangles[0], 0);
        mesh.SetTriangles(outTriangles[1], 1);
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        return mesh;
    }

    // The rows of tiles on the lower half of the slope: each tile is its own closed piece, kept or dropped whole, so the
    // tiles keep their size and the lap of the rows. The last row kept reaches just over the peak, under the ridge tiles.
    private static int[] KeepLowerRows(int[] triangles, Vector3[] vertices)
    {
        // Union-find over welded positions: a tile is a box whose corners are split for their normals.
        Dictionary<Vector3Int, Vector3Int> roots = new();
        Vector3Int Key(int index) => Vector3Int.RoundToInt(vertices[index] * 1000f);
        Vector3Int Root(Vector3Int key)
        {
            if (!roots.TryGetValue(key, out Vector3Int up))
            {
                roots[key] = key;
                return key;
            }

            if (up == key)
            {
                return key;
            }

            Vector3Int top = Root(up);
            roots[key] = top;
            return top;
        }

        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3Int a = Root(Key(triangles[i]));
            for (int corner = 1; corner < 3; corner++)
            {
                Vector3Int b = Root(Key(triangles[i + corner]));
                if (a != b)
                {
                    roots[b] = a;
                }
            }
        }

        Dictionary<Vector3Int, List<int>> pieces = new();
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3Int root = Root(Key(triangles[i]));
            if (!pieces.TryGetValue(root, out List<int> list))
            {
                pieces[root] = list = new List<int>();
            }

            list.Add(triangles[i]);
            list.Add(triangles[i + 1]);
            list.Add(triangles[i + 2]);
        }

        return pieces.Values.Where(tile => tile.Average(index => Vector3.Dot(vertices[index], upSlope)) < KeepRowsBelow).SelectMany(tile => tile).ToArray();
    }

    // The stone underlay is one beam along the slope; its upper end is moved down to the middle, the peak of the ridge.
    // Its sides keep their texture scale: a moved corner takes the texture of the point halfway along, found from its
    // twin at the lower end.
    private static (int[] Triangles, Dictionary<int, (Vector3, Vector2)> Moved) CutUnderlay(int[] triangles, Vector3[] vertices, Vector3[] normals, Vector2[] uvs, bool hasUv)
    {
        bool hasNormals = normals.Length == vertices.Length;
        (Vector2Int, Vector3Int) Profile(int index) => (ProfileOf(vertices[index]),
            hasNormals ? Vector3Int.RoundToInt(normals[index] * 10f) : Vector3Int.zero);
        Dictionary<int, (Vector3, Vector2)> moved = new();
        int[] used = triangles.Distinct().ToArray();
        Dictionary<(Vector2Int, Vector3Int), int> lowerEnd = new();
        foreach (int index in used)
        {
            if (Vector3.Dot(vertices[index], upSlope) < 0f)
            {
                lowerEnd[Profile(index)] = index;
            }
        }

        foreach (int index in used)
        {
            float along = Vector3.Dot(vertices[index], upSlope);
            if (along <= 0f)
            {
                continue;
            }

            Vector3 position = vertices[index] - upSlope * along;
            Vector2 uv = hasUv ? uvs[index] : Vector2.zero;
            if (hasUv && lowerEnd.TryGetValue(Profile(index), out int twin))
            {
                float twinAlong = Vector3.Dot(vertices[twin], upSlope);
                float share = Mathf.Approximately(along, twinAlong) ? 1f : -twinAlong / (along - twinAlong);
                uv = Vector2.LerpUnclamped(uvs[twin], uvs[index], share);
            }

            moved[index] = (position, uv);
        }

        return (triangles, moved);
    }

    // Where a point lies across the underlay beam: along the ridge and out of the slope, to the centimetre.
    private static Vector2Int ProfileOf(Vector3 point)
    {
        return new Vector2Int(Mathf.RoundToInt(point.x * 100f), Mathf.RoundToInt(Vector3.Dot(point, outOfSlope) * 100f));
    }

    // Round ridge tiles laid along the peak, each a little narrower at its far end, so the row shows where one laps the
    // next. They sit on the peak where the two sides' tiles meet.
    private static Mesh RidgeTiles(float tileTop)
    {
        float centre = 1f + Root2 * tileTop + RidgeTileRadius * 0.15f;
        List<Vector3> vertices = new();
        List<Vector3> normals = new();
        List<Vector2> uvs = new();
        List<int> triangles = new();
        int count = Mathf.CeilToInt(2f * RidgeHalfLength / (RidgeTileLength - 0.04f));
        float step = 2f * RidgeHalfLength / count;
        for (int tile = 0; tile < count; tile++)
        {
            float start = -RidgeHalfLength + tile * step - 0.02f;
            float end = Mathf.Min(start + step + 0.04f, RidgeHalfLength);
            AddRidgeTile(vertices, normals, uvs, triangles, start, end, centre, RidgeTileRadius, RidgeTileRadius * 0.9f);
        }

        Mesh mesh = new() { name = "grausten_roof_45_ridge_tiles" };
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        return mesh;
    }

    // One ridge tile: a tapered tube along x with closed ends.
    private static void AddRidgeTile(List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> triangles,
        float start, float end, float centre, float startRadius, float endRadius)
    {
        int first = vertices.Count;
        for (int side = 0; side <= RidgeTileSides; side++)
        {
            float angle = side * Mathf.PI * 2f / RidgeTileSides;
            Vector3 normal = new(0f, Mathf.Sin(angle), Mathf.Cos(angle));
            vertices.Add(new Vector3(start, centre, 0f) + normal * startRadius);
            vertices.Add(new Vector3(end, centre, 0f) + normal * endRadius);
            normals.Add(normal);
            normals.Add(normal);
            float u = (float)side / RidgeTileSides;
            uvs.Add(new Vector2(u, 0f));
            uvs.Add(new Vector2(u, (end - start) * 0.5f));
        }

        for (int side = 0; side < RidgeTileSides; side++)
        {
            int a = first + side * 2;
            triangles.AddRange(new[] { a, a + 1, a + 3, a, a + 3, a + 2 });
        }

        foreach ((float x, float radius, float facing) in new[] { (start, startRadius, -1f), (end, endRadius, 1f) })
        {
            int middle = vertices.Count;
            vertices.Add(new Vector3(x, centre, 0f));
            normals.Add(new Vector3(facing, 0f, 0f));
            uvs.Add(new Vector2(0.5f, 0.5f));
            for (int side = 0; side <= RidgeTileSides; side++)
            {
                float angle = side * Mathf.PI * 2f / RidgeTileSides;
                vertices.Add(new Vector3(x, centre + Mathf.Sin(angle) * radius, Mathf.Cos(angle) * radius));
                normals.Add(new Vector3(facing, 0f, 0f));
                uvs.Add(new Vector2(0.5f + Mathf.Cos(angle) * 0.5f, 0.5f + Mathf.Sin(angle) * 0.5f));
            }

            for (int side = 0; side < RidgeTileSides; side++)
            {
                int a = middle + 1 + side;
                if (facing > 0f)
                {
                    triangles.AddRange(new[] { middle, a + 1, a });
                }
                else
                {
                    triangles.AddRange(new[] { middle, a, a + 1 });
                }
            }
        }
    }

    // The ridges and the other pieces of the red roof follow the vanilla Grausten Roof pieces in the build menu.
    private static void PlaceAfterVanillaGrausten(List<GameObject> pieces)
    {
        List<GameObject> red = graustenRidges.Concat(entries.Where(entry => entry.Covering == RoofCovering.RedGrausten).Select(entry => entry.Prefab))
            .Where(pieces.Contains).ToList();
        if (red.Count == 0)
        {
            return;
        }

        pieces.RemoveAll(red.Contains);
        int last = pieces.FindLastIndex(prefab => prefab != null && prefab.name.StartsWith(VanillaGraustenPrefix, StringComparison.Ordinal));
        pieces.InsertRange(last < 0 ? pieces.Count : last + 1, red);
    }
}
