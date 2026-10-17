using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Pieces.Structure.Darkwood;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Structure.Planking;

/// <summary>
/// A wooden floor in a shape the vanilla floors do not have, cut from the vanilla 2 × 2 m wood floor by
/// <see cref="FloorCutting"/> when the game starts: no model of its own, and nothing of the game's shipped. A clone of
/// the vanilla floor, so it builds, carries, wears and breaks as one, and costs its recipe for the area.
/// </summary>
public abstract class WoodFloorPieceBase : IWhiteHiltCustomPiece
{
    private const string Floor = "wood_floor";
    private const string SmallFloor = "wood_floor_1x1";
    private const string SnapTag = "snappoint";

    // The vanilla floor's collider: a slab from just under the planks to just over them.
    private const float ColliderBottom = -0.033f;
    private const float ColliderTop = 0.097f;

    private static readonly List<WoodFloorPieceBase> all = new();
    private static FloorSource source;
    private static bool sourceRead;
    private static bool orderHooked;

    private readonly PieceManager instance;

    /// <summary>Prefab name of the new piece.</summary>
    protected abstract string PrefabName { get; }

    /// <summary>Name shown to players.</summary>
    protected abstract string FullName { get; }

    /// <summary>Description, in English.</summary>
    protected abstract string Description { get; }

    /// <summary>The floor's outline.</summary>
    protected abstract FloorShape Shape { get; }

    /// <inheritdoc/>
    public ProgressionTier DefaultTier => ProgressionTier.Start;

    /// <inheritdoc/>
    public bool Enabled => true;

    /// <inheritdoc/>
    public string Id => PrefabName;

    /// <inheritdoc/>
    public string DisplayName => FullName;

    /// <inheritdoc/>
    public string NameToken => Translations.Token(PrefabName);

    /// <inheritdoc/>
    public string GatedPrefabName => PrefabName;

    /// <summary>
    /// Registers the English text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    protected WoodFloorPieceBase(PieceManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(PrefabName, FullName, Description);
    }

    /// <summary>
    /// Adds the piece to the hammer.
    /// </summary>
    public void Add()
    {
        try
        {
            GameObject floor = PrefabManager.Instance.GetPrefab(Floor) ?? throw new InvalidOperationException($"{Floor} not found");

            // The vanilla floor's recipe for 4 m², so for this floor's area: rounded, at least 1 of each.
            float factor = Shape.Area / 4f;
            PieceConfig config = new()
            {
                Name = Translations.Token(PrefabName),
                Description = Translations.Token($"{PrefabName}_description"),
                PieceTable = PieceTables.Hammer,
                Requirements = floor.GetComponent<Piece>().m_resources
                    .Where(requirement => requirement.m_resItem != null)
                    .Select(requirement => new RequirementConfig
                    {
                        Item = requirement.m_resItem.name,
                        Amount = Mathf.Max(1, Mathf.RoundToInt(requirement.m_amount * factor)),
                        Recover = requirement.m_recover
                    })
                    .ToArray()
            };

            CustomPiece piece = new(PrefabName, Floor, config);
            Assemble(piece.PiecePrefab, ReadFloor(floor));

            Sprite icon = VisualHelper.RenderIcon(piece.PiecePrefab);
            if (icon != null)
            {
                piece.Piece.m_icon = icon;
            }

            instance.AddPiece(piece);
            all.Add(this);
            if (!orderHooked)
            {
                orderHooked = true;
                PieceManager.OnPiecesRegistered += PlaceAfterTheFloors;
            }

            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    // The vanilla floor's new and worn models with their materials, read once for all the floors. A dedicated server
    // draws nothing, so it reads nothing and builds only the collider and snap points.
    private static FloorSource ReadFloor(GameObject floor)
    {
        if (sourceRead)
        {
            return source;
        }

        sourceRead = true;
        if (VisualHelper.IsHeadless)
        {
            return null;
        }

        Transform root = floor.transform;
        FloorSource read = new();
        foreach (string state in new[] { "New", "Worn" })
        {
            Transform holder = root.Find(state) ?? throw new InvalidOperationException($"{Floor} has no {state}");
            MeshFilter filter = holder.Cast<Transform>()
                .Where(child => child.gameObject.activeSelf && child.name.StartsWith("_Combined"))
                .Select(child => child.GetComponent<MeshFilter>())
                .FirstOrDefault(found => found != null && found.sharedMesh != null);
            Material material = filter != null ? filter.GetComponent<MeshRenderer>()?.sharedMaterial : null;
            if (filter == null || material == null || !filter.sharedMesh.isReadable)
            {
                throw new InvalidOperationException($"{Floor}: {state} has no readable model");
            }

            Matrix4x4 matrix = root.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            read.Models[state] = MeshData.FromMesh(filter.sharedMesh, 0, matrix);
            read.Materials[state] = material;
        }

        source = read;
        return source;
    }

    // Replaces the clone's look, collider and snap points with the cut ones; keeps its network, piece and wear.
    private void Assemble(GameObject prefab, FloorSource models)
    {
        Transform root = prefab.transform;
        int layer = LayerMask.NameToLayer("piece");
        foreach (Transform child in root.Cast<Transform>().ToList())
        {
            UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        foreach (Component component in prefab.GetComponents<Collider>().Cast<Component>().Concat(prefab.GetComponents<LODGroup>()))
        {
            UnityEngine.Object.DestroyImmediate(component);
        }

        GameObject fresh = Holder(root, "New", models, layer);
        GameObject worn = Holder(root, "Worn", models, layer);
        worn.SetActive(false);

        // A convex collider for each part of the floor, as a floor with an opening is not convex.
        int part = 0;
        foreach (Vector2[] corners in Shape.Parts())
        {
            (Vector3[] vertices, int[] triangles) = FloorCutting.Slab(corners, ColliderBottom, ColliderTop);
            Mesh slab = new() { name = $"{PrefabName}_collider_{part++}" };
            slab.SetVertices(vertices);
            slab.SetTriangles(triangles, 0);
            slab.RecalculateBounds();
            GameObject collider = new("collider") { layer = layer };
            collider.transform.SetParent(root, false);
            MeshCollider meshCollider = collider.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = slab;
            meshCollider.convex = true;
        }

        foreach (Vector3 point in Shape.SnapPoints())
        {
            GameObject snap = new("$hud_snappoint") { tag = SnapTag };
            snap.transform.SetParent(root, false);
            snap.transform.localPosition = point;
            snap.SetActive(false);
        }

        WearNTear wearNTear = prefab.GetComponent<WearNTear>() ?? throw new InvalidOperationException($"{prefab.name} has no WearNTear.");
        wearNTear.m_new = fresh;
        wearNTear.m_worn = worn;
        wearNTear.m_broken = worn;
        wearNTear.m_wet = null;
        wearNTear.m_snow = null;
        wearNTear.m_snowWorn = null;
        wearNTear.m_snowBroken = null;
        wearNTear.m_fragmentRoots = null;
    }

    private GameObject Holder(Transform root, string state, FloorSource models, int layer)
    {
        GameObject holder = new(state) { layer = layer };
        holder.transform.SetParent(root, false);
        if (models == null)
        {
            return holder;
        }

        GameObject model = new("model") { layer = layer };
        model.transform.SetParent(holder.transform, false);
        model.AddComponent<MeshFilter>().sharedMesh = MeshData.ToMesh($"{PrefabName}_{state}", FloorCutting.Cut(Shape, models.Models[state]));
        model.AddComponent<MeshRenderer>().sharedMaterial = models.Materials[state];
        return holder;
    }

    // The new floors follow the vanilla wood floors in the hammer, in the order they were added.
    private static void PlaceAfterTheFloors()
    {
        PieceTable table = PieceManager.Instance.GetPieceTable(PieceTables.Hammer);
        if (table == null)
        {
            return;
        }

        List<GameObject> pieces = table.m_pieces;
        List<GameObject> ours = all.Select(entry => pieces.FirstOrDefault(prefab => prefab != null && prefab.name == entry.PrefabName))
            .Where(prefab => prefab != null)
            .ToList();
        if (ours.Count == 0)
        {
            return;
        }

        pieces.RemoveAll(ours.Contains);
        int after = pieces.FindIndex(prefab => prefab != null && prefab.name == SmallFloor);
        if (after < 0)
        {
            after = pieces.FindIndex(prefab => prefab != null && prefab.name == Floor);
        }

        pieces.InsertRange(after < 0 ? pieces.Count : after + 1, ours);
    }

    // The vanilla floor's models and materials by wear state.
    private sealed class FloorSource
    {
        public readonly Dictionary<string, MeshData> Models = new();
        public readonly Dictionary<string, Material> Materials = new();
    }
}

/// <summary>Half of the 2 × 2 m wood floor, cut corner to corner.</summary>
public sealed class WoodFloorTriangle2 : WoodFloorPieceBase
{
    /// <summary>Registers the piece's text.</summary>
    /// <param name="instance">The piece manager.</param>
    public WoodFloorTriangle2(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string PrefabName => "piece_whitehilt_wood_floor_triangle";
    /// <inheritdoc/>
    protected override string FullName => "Wood Floor Triangle 2×2";
    /// <inheritdoc/>
    protected override string Description => "Half of a 2 × 2 m wood floor, sawn corner to corner, for a cut-off corner or a bay. Two of them make a square floor.";
    /// <inheritdoc/>
    protected override FloorShape Shape => FloorShape.Triangle(2f);
}

/// <summary>Half of the 1 × 1 m wood floor, cut corner to corner.</summary>
public sealed class WoodFloorTriangle1 : WoodFloorPieceBase
{
    /// <summary>Registers the piece's text.</summary>
    /// <param name="instance">The piece manager.</param>
    public WoodFloorTriangle1(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string PrefabName => "piece_whitehilt_wood_floor_triangle_1x1";
    /// <inheritdoc/>
    protected override string FullName => "Wood Floor Triangle 1×1";
    /// <inheritdoc/>
    protected override string Description => "Half of a 1 × 1 m wood floor, sawn corner to corner, to close a small corner.";
    /// <inheritdoc/>
    protected override FloorShape Shape => FloorShape.Triangle(1f);
}

/// <summary>A wood floor 2 × 1 m with one end cut at 45°.</summary>
public sealed class WoodFloorAngledEnd : WoodFloorPieceBase
{
    /// <summary>Registers the piece's text.</summary>
    /// <param name="instance">The piece manager.</param>
    public WoodFloorAngledEnd(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string PrefabName => "piece_whitehilt_wood_floor_angled";
    /// <inheritdoc/>
    protected override string FullName => "Wood Floor 2×1, Angled End";
    /// <inheritdoc/>
    protected override string Description => "A wood floor 1 m wide whose end is sawn at 45°: 2 m long on one side, 1 m on the other. For the edge of a deck or a jetty that turns.";
    /// <inheritdoc/>
    protected override FloorShape Shape => FloorShape.AngledEnd(false);
}

/// <summary>The angled-end floor the other way round.</summary>
public sealed class WoodFloorAngledEndMirrored : WoodFloorPieceBase
{
    /// <summary>Registers the piece's text.</summary>
    /// <param name="instance">The piece manager.</param>
    public WoodFloorAngledEndMirrored(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string PrefabName => "piece_whitehilt_wood_floor_angled_mirrored";
    /// <inheritdoc/>
    protected override string FullName => "Wood Floor 2×1, Angled End, Mirrored";
    /// <inheritdoc/>
    protected override string Description => "The angled-end wood floor the other way round, with the long side on the left.";
    /// <inheritdoc/>
    protected override FloorShape Shape => FloorShape.AngledEnd(true);
}

/// <summary>An octagonal wood floor with 2 m sides.</summary>
public sealed class WoodFloorOctagon2 : WoodFloorPieceBase
{
    /// <summary>Registers the piece's text.</summary>
    /// <param name="instance">The piece manager.</param>
    public WoodFloorOctagon2(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string PrefabName => "piece_whitehilt_wood_floor_octagon";
    /// <inheritdoc/>
    protected override string FullName => "Octagonal Wood Floor 2 m";
    /// <inheritdoc/>
    protected override string Description => "An eight-sided wood floor with 2 m sides, 4.8 m across, for a tower or a pavilion. A 2 m wall stands on each side; turn every other wall 45°.";
    /// <inheritdoc/>
    protected override FloorShape Shape => FloorShape.Octagon(2f);
}

/// <summary>An octagonal wood floor with 4 m sides.</summary>
public sealed class WoodFloorOctagon4 : WoodFloorPieceBase
{
    /// <summary>Registers the piece's text.</summary>
    /// <param name="instance">The piece manager.</param>
    public WoodFloorOctagon4(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string PrefabName => "piece_whitehilt_wood_floor_octagon_4";
    /// <inheritdoc/>
    protected override string FullName => "Octagonal Wood Floor 4 m";
    /// <inheritdoc/>
    protected override string Description => "An eight-sided wood floor with 4 m sides, 9.7 m across, for a large tower or a hall. Two 2 m walls stand on each side; it snaps at the corners and the middle of each side.";
    /// <inheritdoc/>
    protected override FloorShape Shape => FloorShape.Octagon(4f);
}

/// <summary>The 2 m octagonal wood floor with an eight-sided stairwell in the middle.</summary>
public sealed class WoodFloorOctagon2Stairwell : WoodFloorPieceBase
{
    /// <summary>Width of the stairwell from side to side: room for the 2.4 m Spiral Stair.</summary>
    private const float Well = 2.6f;

    /// <summary>Registers the piece's text.</summary>
    /// <param name="instance">The piece manager.</param>
    public WoodFloorOctagon2Stairwell(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string PrefabName => "piece_whitehilt_wood_floor_octagon_stairwell";
    /// <inheritdoc/>
    protected override string FullName => "Octagonal Wood Floor 2 m, Stairwell";
    /// <inheritdoc/>
    protected override string Description => "The 2 m octagonal wood floor with an eight-sided stairwell 2.6 m across in the middle, for the next storey of a tower. The Spiral Stair stands in it: its foot snaps to the middle of the floor below, and its head to the middle of the stairwell. A Loft Ladder stands against a side.";
    /// <inheritdoc/>
    protected override FloorShape Shape => FloorShape.OctagonWithOctagonalWell(Well);
}

/// <summary>The 4 m octagonal wood floor with a square opening 4 × 4 m in the middle.</summary>
public sealed class WoodFloorOctagon4Stairwell : WoodFloorPieceBase
{
    /// <summary>Registers the piece's text.</summary>
    /// <param name="instance">The piece manager.</param>
    public WoodFloorOctagon4Stairwell(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string PrefabName => "piece_whitehilt_wood_floor_octagon_4_stairwell";
    /// <inheritdoc/>
    protected override string FullName => "Octagonal Wood Floor 4 m, Stairwell";
    /// <inheritdoc/>
    protected override string Description => "The 4 m octagonal wood floor with a square opening 4 × 4 m in the middle, on the grid of the vanilla floor. Lay vanilla floors in it and leave out what the way up needs: one 2 × 2 m for a Floor Hatch or a ladder, two side by side for two vanilla stairs, or all four round the Spiral Stair. Railings stand on its edges.";
    /// <inheritdoc/>
    protected override FloorShape Shape => FloorShape.OctagonWithSquareWell();
}
