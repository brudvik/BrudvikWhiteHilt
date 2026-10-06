using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Cooking.StonePot;

/// <summary>
/// An extension for the <see cref="StonePot"/>: a tray with a mortar and herbs. Placed next to the pot, it raises the
/// pot to level 2.
/// </summary>
public class HerbTray
{
    /// <summary>
    /// Prefab name of the herb tray.
    /// </summary>
    public const string PrefabName = "piece_whitehilt_herbtray";

    private const string FullName = "Herb Tray";
    private const string Description = "A mortar and fresh herbs within reach of the Stone Pot. Place it next to the pot to cook the more demanding dishes.";
    private const float Size = 1f;

    /// <summary>
    /// Creates the extension and registers its English text. The <see cref="StonePot"/> creates it and adds it
    /// to the game together with itself, as it only works next to it.
    /// </summary>
    public HerbTray()
    {
        Translations.AddEnglishNameAndDescription(PrefabName, FullName, Description);
    }

    /// <summary>
    /// Adds the herb tray to the hammer.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    /// <param name="stonePot">The crafting station of the stone pot.</param>
    public void Add(PieceManager instance, CraftingStation stonePot)
    {
        try
        {
            PieceConfig pieceConfig = new()
            {
                Name = Translations.Token(PrefabName),
                Description = Translations.Token($"{PrefabName}_description"),
                PieceTable = PieceTables.Hammer,
                Requirements = new RequirementConfig[]
                {
                    new() { Item = "FineWood", Amount = 2, Recover = true },
                    new() { Item = "Stone", Amount = 5, Recover = true },
                    new() { Item = "Bronze", Amount = 1, Recover = true },
                    new() { Item = "Thistle", Amount = 3, Recover = false }
                }
            };

            CustomPiece piece = new(PrefabName, "cauldron_ext5_mortarandpestle", pieceConfig);
            piece.PiecePrefab.GetComponent<StationExtension>().m_craftingStation = stonePot;
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

    // The vanilla mortar is a whole table, so the tray is placed on the ground at its own size and the colliders shrink to match.
    private static void TryApplyVisual(CustomPiece piece)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        try
        {
            Transform root = piece.PiecePrefab.transform;
            Transform table = root.Find("new") ?? throw new InvalidOperationException("the table new was not found");
            GameObject model = VisualHelper.ReplaceMesh(table.gameObject, ForagingAssets.LoadMesh("herbtray"), ForagingAssets.LoadTexture("herbtray_albedo"), size: Size);
            VisualHelper.FitBoxColliders(root, model);

            PieceFragments.Apply(piece.PiecePrefab);
            Sprite icon = VisualHelper.RenderIcon(piece.PiecePrefab);
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
}
