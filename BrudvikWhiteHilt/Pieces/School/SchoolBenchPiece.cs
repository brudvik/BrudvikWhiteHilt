using BrudvikWhiteHilt.Decor;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using BrudvikWhiteHilt.School;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.School;

/// <summary>
/// The School Bench: the game's own bench with a long table in front of it, set with open scrolls, a wax tablet, an
/// open book, inkhorns, rolled scrolls and rune staves. Using the table opens <see cref="SchoolPanel"/>; sitting on the
/// bench studies.
/// </summary>
public class SchoolBenchPiece : IWhiteHiltCustomPiece
{
    /// <summary>Prefab name.</summary>
    public const string PrefabName = "piece_whitehilt_schoolbench";

    private const string FullName = "School Bench";
    private const string Description = "A long table set with scrolls, tablets and books, and a bench to sit on. Write down what you know for others, " +
        "read what they wrote, and sit to study the skills they are better at.";

    // The table stands in front of the bench, where the sitters face (+z); measured from the models (offline preview
    // AssetSource/Preview/schoolbench.preview.json).
    private const float TableLength = 2.4f;
    private const float TableDepth = 1.126f;
    private const float TableHeight = 0.854f;
    private const float TableZ = 0.86f;
    private const float TopThickness = 0.08f;

    // Props on the table top, in the table model's units: the table is 1 high (0.854 m) and long along x.
    private static readonly (string Mesh, Vector3 Base, float Height, float Yaw)[] props =
    {
        ("schoolscroll", new Vector3(-0.94f, 1f, -0.32f), 0.0433f, -6f),
        ("waxtablet", new Vector3(0f, 1f, -0.4f), 0.0164f, 3f),
        ("openbook", new Vector3(0.94f, 1f, -0.36f), 0.0258f, 4f),
        ("schoolscroll", new Vector3(0.42f, 1f, 0.12f), 0.0433f, 78f),
        ("inkhorn", new Vector3(-0.42f, 1f, -0.18f), 0.283f, 30f),
        ("runerack", new Vector3(-0.05f, 1f, 0.52f), 0.13f, 0f),
        ("mapscroll", new Vector3(-1.12f, 1f, 0.3f), 0.055f, 80f),
        ("mapscroll", new Vector3(-1.04f, 1f, 0.3f), 0.055f, 95f),
        ("mapscroll", new Vector3(-1.08f, 1.05f, 0.32f), 0.055f, 70f),
        ("treasuremap", new Vector3(0.88f, 1f, 0.45f), 0.07f, 20f),
        ("decor_candlestick", new Vector3(1.22f, 1f, 0.42f), 0.26f, 0f),
        ("decor_table_candle", new Vector3(-0.62f, 1f, 0.45f), 0.117f, 0f),
        ("decor_cup_wooden", new Vector3(0.68f, 1f, 0.18f), 0.111f, 0f),
        ("decor_jar_small", new Vector3(1.2f, 1f, -0.05f), 0.3f, 0f),
        ("inkhorn", new Vector3(0.5f, 1f, -0.22f), 0.283f, 200f),
        ("waxtablet", new Vector3(-0.15f, 1f, 0.16f), 0.0164f, -18f),
        ("openbook", new Vector3(-0.72f, 1f, 0.24f), 0.0258f, 165f),
        ("decor_scroll", new Vector3(0.1f, 1f, 0.3f), 0.025f, 75f),
        ("decor_scroll", new Vector3(0.14f, 1f, 0.36f), 0.025f, 95f),
        ("decor_scroll", new Vector3(1.18f, 1f, 0.15f), 0.025f, 10f)
    };

    private readonly PieceManager instance;

    /// <inheritdoc/>
    public bool Enabled => true;

    /// <inheritdoc/>
    public ProgressionTier DefaultTier => ProgressionTier.BlackForest;

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
    public SchoolBenchPiece(PieceManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(PrefabName, FullName, Description);
    }

    /// <summary>
    /// Adds the bench to the hammer.
    /// </summary>
    public void Add()
    {
        try
        {
            PieceConfig pieceConfig = new()
            {
                Name = NameToken,
                Description = Translations.Token($"{PrefabName}_description"),
                PieceTable = PieceTables.Hammer,
                Category = PieceCategories.Furniture,
                CraftingStation = CraftingStations.Workbench,
                Requirements = new RequirementConfig[]
                {
                    new() { Item = "FineWood", Amount = 10, Recover = true },
                    new() { Item = "Wood", Amount = 10, Recover = true },
                    new() { Item = "LeatherScraps", Amount = 5, Recover = true },
                    new() { Item = "Coal", Amount = 4, Recover = false },
                    new() { Item = "Feathers", Amount = 2, Recover = false }
                }
            };

            CustomPiece piece = new(PrefabName, "piece_bench01", pieceConfig);
            GameObject prefab = piece.PiecePrefab;
            AddTableCollider(prefab);
            prefab.AddComponent<SchoolBench>();
            TryApplyVisual(piece);
            instance.AddPiece(piece);
            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    // The table top is solid, so it is what the cursor finds (and the panel opens), and nobody walks through it. It is
    // added on every machine, a dedicated server included, as the piece's shape belongs to it there too.
    private static void AddTableCollider(GameObject prefab)
    {
        Transform bench = prefab.transform.Find("collider");
        GameObject table = new("SchoolTable") { layer = bench != null ? bench.gameObject.layer : prefab.layer };
        table.transform.SetParent(prefab.transform, false);
        BoxCollider top = table.AddComponent<BoxCollider>();
        top.center = new Vector3(0f, TableHeight - TopThickness / 2f, TableZ);
        top.size = new Vector3(TableLength, TopThickness, TableDepth);
    }

    // Sets the table and its props in front of the vanilla bench, which keeps its own look, its seats and its worn and
    // broken states. A missing model leaves the bench as it is.
    private static void TryApplyVisual(CustomPiece piece)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        try
        {
            GameObject prefab = piece.PiecePrefab;
            Transform look = prefab.transform.Find("New/high") ?? throw new InvalidOperationException("the bench has no New/high mesh");
            Renderer template = look.GetComponent<Renderer>();
            Mesh mesh = ForagingAssets.LoadMesh("schoolbench") ?? throw new InvalidOperationException("schoolbench is not in the bundle");
            Bounds bounds = mesh.bounds;
            float scale = TableLength / Mathf.Max(bounds.size.x, bounds.size.z);
            Vector3 position = new Vector3(0f, 0f, TableZ) - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z) * scale;
            GameObject table = VisualHelper.CreateModel(prefab.transform.Find("SchoolTable"), mesh, ForagingAssets.LoadTexture("schoolbench_albedo"),
                template, position, Quaternion.identity, scale);
            foreach ((string name, Vector3 basePosition, float height, float yaw) in props)
            {
                TryAddProp(table, name, basePosition, height, yaw);
            }

            Sprite icon = VisualHelper.RenderIcon(prefab);
            if (icon != null)
            {
                piece.Piece.m_icon = icon;
            }
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"{FullName}: keeping the plain bench until its models are in the bundle: {ex.Message}");
        }
    }

    // A missing prop leaves the rest of the table as it is. Decor props come from the decor bundle.
    private static void TryAddProp(GameObject table, string name, Vector3 basePosition, float height, float yaw)
    {
        try
        {
            bool decor = name.StartsWith("decor_", StringComparison.Ordinal);
            Mesh mesh = decor ? DecorAssets.LoadMesh(name) : ForagingAssets.LoadMesh(name);
            Texture2D texture = decor ? DecorAssets.LoadTexture($"{name}_albedo") : ForagingAssets.LoadTexture($"{name}_albedo");
            if (mesh == null)
            {
                return;
            }

            VisualHelper.AddMesh(table, mesh, texture, basePosition, height, Quaternion.Euler(0f, yaw, 0f));
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"{FullName}: no {name} on the table: {ex.Message}");
        }
    }
}
