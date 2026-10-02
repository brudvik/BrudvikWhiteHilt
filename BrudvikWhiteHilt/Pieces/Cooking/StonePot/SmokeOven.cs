using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Cooking.StonePot;

/// <summary>
/// An extension for the <see cref="StonePot"/>: a clay oven for smoking fish and meat. Placed next to the pot together with the
/// <see cref="HerbTray"/>, it raises the pot to level 3.
/// </summary>
public class SmokeOven
{
    /// <summary>
    /// Prefab name of the smoke oven.
    /// </summary>
    public const string PrefabName = "piece_whitehilt_smokeoven";

    private const string FullName = "Smoke Oven";
    private const string Description = "A clay oven with a chimney for smoking fish and meat. Place it next to the Stone Pot to cook the smoked dishes.";

    // Longest side in metres; the model is about twice as wide as it is tall.
    private const float Size = 2.4f;
    private const float FireScale = 0.35f;

    // Measured on the converted model (height 1), with x negated because Unity mirrors OBJ files.
    private static readonly Vector3 coalInMesh = new(0.1f, 0.2f, 0f);
    private static readonly Vector3 chimneyInMesh = new(0.534f, 1.02f, -0.01f);

    /// <summary>
    /// Constructor for the SmokeOven class. Registers the English text.
    /// </summary>
    public SmokeOven()
    {
        Translations.AddEnglishNameAndDescription(PrefabName, FullName, Description);
    }

    /// <summary>
    /// Adds the smoke oven to the hammer.
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
                    new() { Item = "Stone", Amount = 10, Recover = true },
                    new() { Item = "Wood", Amount = 6, Recover = true },
                    new() { Item = "Iron", Amount = 2, Recover = true },
                    new() { Item = "Resin", Amount = 5, Recover = false }
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
            GameObject model = VisualHelper.ReplaceMesh(table.gameObject, ForagingAssets.LoadMesh("smokeoven"), ForagingAssets.LoadTexture("smokeoven_albedo"), size: Size);
            VisualHelper.FitBoxColliders(root, model);

            PieceFragments.Apply(piece.PiecePrefab);
            Sprite icon = VisualHelper.RenderIcon(piece.PiecePrefab);
            if (icon != null)
            {
                piece.Piece.m_icon = icon;
            }

            AddFireAndSmoke(root, model);
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"{FullName}: keeping the vanilla look: {ex.Message}");
        }
    }

    // Flames, light and crackle from the campfire glow on the coal, and its smoke rises from the chimney.
    private static void AddFireAndSmoke(Transform root, GameObject model)
    {
        try
        {
            FireEffects.AddFlames(root, "WhiteHiltOvenFire", InRoot(root, model, coalInMesh), FireScale);
            FireEffects.AddSmoke(root, "WhiteHiltOvenSmoke", InRoot(root, model, chimneyInMesh));
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"{FullName}: no fire or smoke: {ex.Message}");
        }
    }

    private static Vector3 InRoot(Transform root, GameObject model, Vector3 pointInMesh)
    {
        return root.InverseTransformPoint(model.transform.TransformPoint(pointInMesh));
    }
}
