using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Indestructible;
using BrudvikWhiteHilt.Painting;
using BrudvikWhiteHilt.Pieces.Painting;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Painting;

/// <summary>
/// The White Hilt Paint Brush: an everlasting build tool, like the White Hilt Hammer, whose build menu paints, stains,
/// cleans and picks colours on building pieces.
/// </summary>
public class WhiteHiltPaintBrush : IWhiteHiltCustomItem
{
    /// <summary>Prefab name of the brush.</summary>
    public const string PrefabName = "WhiteHiltPaintBrush";

    /// <summary>Name of the brush's piece table.</summary>
    public const string TableName = "_WhiteHiltPaintBrushPieceTable";

    private const string FullName = "White Hilt Paint Brush";
    private const string Description = "An everlasting brush. Load it by using a paint pot, then paint or stain building pieces; the mouse wheel sets how wide it reaches.";

    private readonly ItemManager instance;

    /// <inheritdoc/>
    public bool Enabled => true;

    /// <inheritdoc/>
    public ProgressionTier DefaultTier => ProgressionTier.BlackForest;

    /// <inheritdoc/>
    public string Id => PrefabName;

    /// <inheritdoc/>
    public string DisplayName => FullName;

    /// <inheritdoc/>
    public string NameToken => Translations.Token(Translations.ItemKey(PrefabName));

    /// <inheritdoc/>
    public string GatedPrefabName => PrefabName;

    /// <summary>
    /// Registers the English text.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public WhiteHiltPaintBrush(ItemManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(PrefabName), FullName, Description);
        PaintBrush.RegisterTranslations();
    }

    /// <summary>
    /// Adds the brush, its piece table and its four actions.
    /// </summary>
    public void Add()
    {
        try
        {
            AddPieceTable();

            IndestructibleItem brush = new(PrefabName, "Hammer", new ItemConfig
            {
                Name = NameToken,
                Description = Translations.Token($"{Translations.ItemKey(PrefabName)}_description"),
                CraftingStation = PaintBench.PrefabName,
                PieceTable = TableName,
                Requirements = new RequirementConfig[]
                {
                    new() { Item = "FineWood", Amount = 2 },
                    new() { Item = "Bronze", Amount = 1 },
                    new() { Item = "Feathers", Amount = 3 }
                }
            });

            brush.ItemData.m_homeItemsStaminaModifier -= 1.0f;
            TryApplyVisual(brush);
            instance.AddItem(brush);
            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    private static void AddPieceTable()
    {
        PieceManager.Instance.AddPieceTable(new CustomPieceTable(TableName, new PieceTableConfig
        {
            CanRemovePieces = false
        }));

        string repair = RepairPrefabName();
        foreach ((PaintBrush.BrushAction action, string prefab, _, _) in PaintBrush.Actions)
        {
            CustomPiece piece = new(prefab, repair, new PieceConfig
            {
                Name = Translations.Token(prefab),
                Description = Translations.Token($"{prefab}_description"),
                PieceTable = TableName,
                Icon = PaintBrush.CreateIcon(action)
            });
            piece.Piece.m_repairPiece = true;
            piece.Piece.m_removePiece = false;
            PieceManager.Instance.AddPiece(piece);
        }
    }

    // The hammer's own repair piece, so the actions aim and click like repairing.
    private static string RepairPrefabName()
    {
        GameObject hammer = PrefabManager.Instance.GetPrefab("Hammer") ?? throw new InvalidOperationException("Hammer not found");
        GameObject repair = hammer.GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces.m_pieces
            .FirstOrDefault(prefab => prefab != null && prefab.GetComponent<Piece>() is Piece piece && piece.m_repairPiece);
        return repair != null ? repair.name : throw new InvalidOperationException("the hammer's repair piece was not found");
    }

    private static void TryApplyVisual(IndestructibleItem brush)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        try
        {
            VisualHelper.ReplaceWeaponMesh(brush.ItemPrefab, ForagingAssets.LoadMesh("whpaintbrush"), ForagingAssets.LoadTexture("whpaintbrush_albedo"));
            Sprite icon = VisualHelper.RenderIcon(brush.ItemPrefab);
            if (icon != null)
            {
                ItemDrop.ItemData.SharedData shared = brush.ItemDrop.m_itemData.m_shared;
                shared.m_icons = Enumerable.Repeat(icon, Math.Max(1, shared.m_icons.Length)).ToArray();
            }
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"{FullName}: keeping the hammer's look until its model is in the bundle: {ex.Message}");
        }
    }
}
