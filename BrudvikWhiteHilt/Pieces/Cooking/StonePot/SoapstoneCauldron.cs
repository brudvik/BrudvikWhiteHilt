using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Roofing;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Cooking.StonePot;

/// <summary>
/// An extension for the <see cref="StonePot"/>: a heavy cauldron carved out of soapstone, which holds the heat long, as the
/// Norse cooked in them. Placed next to the pot together with the <see cref="HerbTray"/> and the <see cref="SmokeOven"/>, it
/// raises the pot to level 4. It is carved at the Stonecutter, which must be near when it is built.
/// </summary>
public class SoapstoneCauldron
{
    /// <summary>
    /// Prefab name of the soapstone cauldron.
    /// </summary>
    public const string PrefabName = "piece_whitehilt_soapstonecauldron";

    private const string FullName = "Soapstone Cauldron";
    private const string Description = "A heavy cauldron carved out of soapstone. It holds the heat for hours. Place it next to the Stone Pot, with the Herb Tray and the Smoke Oven, to cook the finest dishes. Carved near a Stonecutter.";

    // Longest side in metres.
    private const float Size = 0.75f;

    /// <summary>
    /// Constructor for the SoapstoneCauldron class. Registers the English text.
    /// </summary>
    public SoapstoneCauldron()
    {
        Translations.AddEnglishNameAndDescription(PrefabName, FullName, Description);
    }

    /// <summary>
    /// Adds the soapstone cauldron to the hammer.
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
                CraftingStation = CraftingStations.Stonecutter,
                Requirements = new RequirementConfig[]
                {
                    new() { Item = RoofMaterials.Soapstone, Amount = 6, Recover = true },
                    new() { Item = "Stone", Amount = 6, Recover = true },
                    new() { Item = "Iron", Amount = 2, Recover = true }
                }
            };

            // Same known extension as the Herb Tray, so the look can be swapped the same way.
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

    // The stone pot's model on the ground, in the grey-green of the Soapstone Hearth.
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
            GameObject model = VisualHelper.ReplaceMesh(table.gameObject, ForagingAssets.LoadMesh("stonepot"), ForagingAssets.LoadTexture("stonepot_albedo"), size: Size);
            VisualHelper.FitBoxColliders(root, model);
            VisualHelper.Recolor(model, pixel =>
            {
                Color.RGBToHSV(pixel, out _, out _, out float value);
                Color result = Color.Lerp(new Color(value, value, value), new Color(0.58f, 0.66f, 0.6f) * (0.5f + value * 0.7f), 0.75f);
                result.a = pixel.a / 255f;
                return result;
            });

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
