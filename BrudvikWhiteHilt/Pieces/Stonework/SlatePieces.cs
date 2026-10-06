using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Roofing;
using BrudvikWhiteHilt.Pieces.Roofs;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Stonework;

/// <summary>
/// A floor piece of slate slabs, built near the Stonecutter. It is a vanilla wooden floor or stair underneath, so it
/// snaps
/// and collides like one, but it is stone and looks like the slate roof: thin slabs whose slates keep their size
/// however
/// the slab is cut. AssetSource/Preview/build_slate.py draws the same slabs for the documentation.
/// </summary>
public abstract class SlatePieceBase : IWhiteHiltCustomPiece
{
    // The slate texture covers this many metres, as on the roof.
    private const float TextureMetres = 3f;

    private static Mesh unitCube;
    private readonly PieceManager instance;

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run. Registers the English text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    protected SlatePieceBase(PieceManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(Id, FullName, Description);
    }

    /// <inheritdoc/>
    public bool Enabled => true;

    /// <inheritdoc/>
    public ProgressionTier DefaultTier => ProgressionTier.Mountain;

    /// <inheritdoc/>
    public string Id => $"piece_whitehilt_{Name}";

    /// <inheritdoc/>
    public string DisplayName => FullName;

    /// <inheritdoc/>
    public string NameToken => Translations.Token(Id);

    /// <inheritdoc/>
    public string GatedPrefabName => Id;

    /// <summary>Lower-case part of the prefab name.</summary>
    protected abstract string Name { get; }

    /// <summary>Name shown to players, in English.</summary>
    protected abstract string FullName { get; }

    /// <summary>Description, in English.</summary>
    protected abstract string Description { get; }

    /// <summary>Vanilla wooden floor or stair the piece is cloned from.</summary>
    protected abstract string CopyFrom { get; }

    /// <summary>Slate needed.</summary>
    protected abstract int Slate { get; }

    /// <summary>The slabs: centre, size and turn about the vertical, in the piece's space.</summary>
    protected abstract IEnumerable<(Vector3 Center, Vector3 Size, float Yaw)> Slabs { get; }

    /// <summary>
    /// Adds the piece to the hammer.
    /// </summary>
    public void Add()
    {
        try
        {
            PieceConfig pieceConfig = new()
            {
                Name = NameToken,
                Description = Translations.Token($"{Id}_description"),
                PieceTable = PieceTables.Hammer,
                Category = PieceCategories.Building,
                CraftingStation = CraftingStations.Stonecutter,
                Requirements = new RequirementConfig[] { new() { Item = RoofMaterials.Slate, Amount = Slate, Recover = true } }
            };

            CustomPiece piece = new(Id, CopyFrom, pieceConfig);
            GameObject prefab = piece.PiecePrefab;
            WearNTear wear = prefab.GetComponent<WearNTear>();
            WearNTear stone = PrefabManager.Instance.GetPrefab("stone_wall_1x1")?.GetComponent<WearNTear>();
            wear.m_materialType = WearNTear.MaterialType.Stone;
            wear.m_noRoofWear = false;
            if (stone != null)
            {
                wear.m_health = stone.m_health;
                wear.m_hitEffect = stone.m_hitEffect;
                wear.m_destroyedEffect = stone.m_destroyedEffect;
            }

            TryApplyVisual(piece, wear);
            instance.AddPiece(piece);
            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    // Hides the cloned wooden look and shows slate slabs built in code instead, with the roofs' slate material. The
    // slabs are the same whole, worn or broken, as stone does not show wear the way wood does.
    private void TryApplyVisual(CustomPiece piece, WearNTear wear)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        try
        {
            GameObject prefab = piece.PiecePrefab;
            VisualHelper.HideRenderers(prefab);
            GameObject visual = new("SlateSlabs") { layer = prefab.layer };
            visual.transform.SetParent(prefab.transform, false);
            visual.AddComponent<MeshFilter>().sharedMesh = BuildMesh(Slabs);
            MeshRenderer renderer = visual.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = RoofCatalog.SlateMaterial();

            // The wooden looks are hidden; the slabs are the same whole, worn or broken.
            wear.m_new = visual;
            wear.m_worn = visual;
            wear.m_broken = visual;
            wear.m_wet = null;
            wear.m_snow = null;
            wear.m_snowWorn = null;
            wear.m_snowBroken = null;
            wear.m_fragmentRoots = null;

            Sprite icon = VisualHelper.RenderIcon(prefab);
            if (icon != null)
            {
                piece.Piece.m_icon = icon;
            }
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"{FullName}: keeping the vanilla look: {ex.Message}");
        }
    }

    // Boxes with texture coordinates in metres, so the slates are the same size on every face and every slab.
    private Mesh BuildMesh(IEnumerable<(Vector3 Center, Vector3 Size, float Yaw)> slabs)
    {
        unitCube ??= CubeMesh();
        Vector3[] cubeVertices = unitCube.vertices;
        Vector3[] cubeNormals = unitCube.normals;
        int[] cubeTriangles = unitCube.triangles;
        List<Vector3> vertices = new();
        List<Vector3> normals = new();
        List<Vector2> uvs = new();
        List<int> triangles = new();
        foreach ((Vector3 center, Vector3 size, float yaw) in slabs)
        {
            Quaternion turn = Quaternion.Euler(0f, yaw, 0f);
            int start = vertices.Count;
            for (int i = 0; i < cubeVertices.Length; i++)
            {
                Vector3 local = Vector3.Scale(cubeVertices[i], size);
                Vector3 normal = cubeNormals[i];
                Vector3 world = center + turn * local;
                vertices.Add(world);
                normals.Add(turn * normal);
                Vector2 uv = Mathf.Abs(normal.y) > 0.5f ? new Vector2(world.x, world.z)
                    : Mathf.Abs(normal.x) > 0.5f ? new Vector2(local.z + center.z, world.y) : new Vector2(local.x + center.x, world.y);
                uvs.Add(uv / TextureMetres);
            }

            foreach (int index in cubeTriangles)
            {
                triangles.Add(start + index);
            }
        }

        Mesh mesh = new() { name = $"{Id}_slabs" };
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh CubeMesh()
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Mesh mesh = cube.GetComponent<MeshFilter>().sharedMesh;
        UnityEngine.Object.DestroyImmediate(cube);
        return mesh;
    }
}

/// <summary>A 2 × 2 m floor of slate slabs, on a wooden floor's frame.</summary>
public sealed class SlateFloor : SlatePieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public SlateFloor(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string Name => "slatefloor";
    /// <inheritdoc/>
    protected override string FullName => "Slate Floor";
    /// <inheritdoc/>
    protected override string Description => "A 2 × 2 m floor of thin slate slabs, like the slate roof. Stone: it wants the ground or stone under it.";
    /// <inheritdoc/>
    protected override string CopyFrom => "wood_floor";
    /// <inheritdoc/>
    protected override int Slate => 4;
    /// <inheritdoc/>
    protected override IEnumerable<(Vector3 Center, Vector3 Size, float Yaw)> Slabs => new[] { (new Vector3(0f, 0.02f, 0f), new Vector3(2f, 0.12f, 2f), 0f) };
}

/// <summary>A 1 × 1 m floor of slate slabs.</summary>
public sealed class SlateFloorSmall : SlatePieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public SlateFloorSmall(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string Name => "slatefloor1x1";
    /// <inheritdoc/>
    protected override string FullName => "Slate Floor 1 × 1";
    /// <inheritdoc/>
    protected override string Description => "A 1 × 1 m floor of thin slate slabs, to fill in round the larger ones.";
    /// <inheritdoc/>
    protected override string CopyFrom => "wood_floor_1x1";
    /// <inheritdoc/>
    protected override int Slate => 1;
    /// <inheritdoc/>
    protected override IEnumerable<(Vector3 Center, Vector3 Size, float Yaw)> Slabs => new[] { (new Vector3(0f, 0.02f, 0f), new Vector3(1f, 0.12f, 1f), 0f) };
}

/// <summary>Slate steps rising 1 m over 2 m, as the wooden stair.</summary>
public sealed class SlateSteps : SlatePieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public SlateSteps(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string Name => "slatesteps";
    /// <inheritdoc/>
    protected override string FullName => "Slate Steps";
    /// <inheritdoc/>
    protected override string Description => "Four broad steps of stacked slate, rising 1 m over 2 m like the wooden stair.";
    /// <inheritdoc/>
    protected override string CopyFrom => "wood_stair";
    /// <inheritdoc/>
    protected override int Slate => 6;

    // The wooden stair rises towards -z, from y 0 at z +1 to y 1 at z -1.
    /// <inheritdoc/>
    protected override IEnumerable<(Vector3 Center, Vector3 Size, float Yaw)> Slabs
    {
        get
        {
            const float bottom = -0.13f;
            for (int step = 0; step < 4; step++)
            {
                float top = 0.25f * (step + 1);
                yield return (new Vector3(0f, (top + bottom) / 2f, 0.75f - 0.5f * step), new Vector3(2f, top - bottom, 0.5f), 0f);
            }
        }
    }
}

/// <summary>A 2 × 2 m path of loose slate flagstones.</summary>
public sealed class SlatePath : SlatePieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public SlatePath(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string Name => "slatepath";
    /// <inheritdoc/>
    protected override string FullName => "Slate Path";
    /// <inheritdoc/>
    protected override string Description => "Flagstones of slate laid loosely in the grass, 2 × 2 m. Lay them end to end for a path to the door.";
    /// <inheritdoc/>
    protected override string CopyFrom => "wood_floor";
    /// <inheritdoc/>
    protected override int Slate => 3;
    /// <inheritdoc/>
    protected override IEnumerable<(Vector3 Center, Vector3 Size, float Yaw)> Slabs => new[]
    {
        (new Vector3(-0.5f, 0.02f, -0.5f), new Vector3(0.82f, 0.08f, 0.74f), 4f),
        (new Vector3(0.48f, 0.02f, -0.55f), new Vector3(0.88f, 0.08f, 0.68f), -7f),
        (new Vector3(-0.47f, 0.02f, 0.42f), new Vector3(0.84f, 0.08f, 0.9f), -3f),
        (new Vector3(0.5f, 0.02f, 0.46f), new Vector3(0.76f, 0.08f, 0.84f), 8f)
    };
}
