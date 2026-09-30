using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Painting;

/// <summary>
/// The Paint Bench: a table with a palette, a bucket and brushes. Use crafts the White Hilt Paint Brush; Shift + Use
/// opens the colour wheel to mix paint pots from dyes in the inventory and nearby chests.
/// </summary>
public class PaintBench : IWhiteHiltCustomPiece
{
    /// <summary>Prefab name, also the crafting station of the brush.</summary>
    public const string PrefabName = "piece_whitehilt_paintbench";

    private const string FullName = "Paint Bench";
    private const string Description = "A table for mixing paint. Shift + Use opens the colour wheel; Use crafts the paint brush.";
    private const float Size = 2.2f;

    private static readonly string[] looks = { "New", "Worn", "Broken" };

    // Props on the table top, in the table model's units: the table is 1 high (0.76 m in game) and long along z.
    private static readonly (string Mesh, Vector3 Base, float Height, float Yaw)[] props =
    {
        ("palette", new Vector3(0.05f, 1f, -0.7f), 0.022f, 20f),
        ("paintbucket", new Vector3(0f, 1f, 0.8f), 0.4f, 0f),
        ("brushcup", new Vector3(-0.2f, 1f, 0.1f), 0.46f, 0f)
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
    public PaintBench(PieceManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(PrefabName, FullName, Description);
    }

    /// <summary>
    /// True for the Paint Bench's crafting station.
    /// </summary>
    /// <param name="station">The station.</param>
    /// <returns>True for the bench.</returns>
    public static bool IsBench(CraftingStation station)
    {
        return station != null && station.m_name == Translations.Token(PrefabName);
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
                CraftingStation = CraftingStations.Workbench,
                Requirements = new RequirementConfig[]
                {
                    new() { Item = "FineWood", Amount = 10, Recover = true },
                    new() { Item = "Bronze", Amount = 3, Recover = true },
                    new() { Item = "Resin", Amount = 6, Recover = true },
                    new() { Item = "LeatherScraps", Amount = 4, Recover = true }
                }
            };

            CustomPiece piece = new(PrefabName, "piece_workbench", pieceConfig);

            // Recipes match their station by name, so a unique name keeps the workbench recipes off the bench.
            CraftingStation station = piece.PiecePrefab.GetComponent<CraftingStation>();
            station.m_name = NameToken;
            station.m_craftRequireRoof = false;
            station.m_craftRequireFire = false;
            station.m_showBasicRecipies = false;

            TryApplyVisual(piece, station);
            instance.AddPiece(piece);
            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    private static void TryApplyVisual(CustomPiece piece, CraftingStation station)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        try
        {
            Transform root = piece.PiecePrefab.transform;
            Mesh table = ForagingAssets.LoadMesh("painttable");
            Texture2D tableTexture = ForagingAssets.LoadTexture("painttable_albedo");
            GameObject newLook = null;
            foreach (string look in looks)
            {
                Transform lookRoot = root.Find(look) ?? throw new InvalidOperationException($"the look {look} was not found");
                GameObject model = VisualHelper.ReplaceMesh(lookRoot.gameObject, table, tableTexture, size: Size);
                newLook ??= model;
                foreach ((string mesh, Vector3 basePosition, float height, float yaw) in props)
                {
                    TryAddProp(model, mesh, basePosition, height, yaw);
                }
            }

            VisualHelper.FitBoxColliders(root, newLook);
            Sprite icon = VisualHelper.RenderIcon(piece.PiecePrefab);
            if (icon != null)
            {
                piece.Piece.m_icon = icon;
                station.m_icon = icon;
            }
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"{FullName}: keeping the vanilla look until its models are in the bundle: {ex.Message}");
        }
    }

    // A missing prop leaves the rest of the bench as it is.
    private static void TryAddProp(GameObject table, string mesh, Vector3 basePosition, float height, float yaw)
    {
        try
        {
            VisualHelper.AddMesh(table, ForagingAssets.LoadMesh(mesh), ForagingAssets.LoadTexture($"{mesh}_albedo"), basePosition, height,
                Quaternion.Euler(0f, yaw, 0f));
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"{FullName}: no {mesh} on the table: {ex.Message}");
        }
    }
}
