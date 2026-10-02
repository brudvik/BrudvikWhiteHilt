using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Smithing.RuneForge;

/// <summary>
/// A <see cref="RuneForge"/> extension: a blood-red runestone where black beast trophies are bound to White Hilt
/// weapons and shields.
/// </summary>
public class BindingStone
{
    /// <summary>
    /// Prefab name of the binding stone.
    /// </summary>
    public const string PrefabName = "piece_whitehilt_bindingstone";

    private const string FullName = "Binding Stone";
    private const string Description = "A runestone where the trophy of a black beast is bound to a White Hilt weapon or shield, which then strikes or blocks harder. Place it next to the Rune Forge.";
    private const float Size = 1.1f;

    /// <summary>
    /// Constructor for the BindingStone class. Registers the English text.
    /// </summary>
    public BindingStone()
    {
        Translations.AddEnglishNameAndDescription(PrefabName, FullName, Description);
        Translations.AddEnglish("whitehilt_runeforge_ext_noforge", "Needs a Rune Forge nearby");
        BindingStoneComponent.RegisterTranslations();
    }

    /// <summary>
    /// Adds the binding stone to the hammer.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    /// <param name="runeForge">The crafting station of the rune forge.</param>
    public void Add(PieceManager instance, CraftingStation runeForge)
    {
        try
        {
            PieceConfig pieceConfig = new()
            {
                Name = Translations.Token(PrefabName),
                Description = Translations.Token($"{PrefabName}_description"),
                PieceTable = PieceTables.Hammer,
                CraftingStation = RuneForge.PrefabName,
                Requirements = new RequirementConfig[]
                {
                    new() { Item = "Stone", Amount = 20, Recover = true },
                    new() { Item = "Chain", Amount = 2, Recover = true },
                    new() { Item = "SurtlingCore", Amount = 1, Recover = false }
                }
            };

            // The forge cooler is about as big as the stone, so its collider fits well enough.
            CustomPiece piece = new(PrefabName, "forge_ext5", pieceConfig);
            piece.PiecePrefab.GetComponent<StationExtension>().m_craftingStation = runeForge;
            piece.PiecePrefab.AddComponent<BindingStoneComponent>();
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
            Transform cooler = piece.PiecePrefab.transform.Find("new") ?? throw new InvalidOperationException("the cooler new was not found");
            VisualHelper.ReplaceMesh(cooler.gameObject, ForagingAssets.LoadMesh("homestone"), ForagingAssets.LoadTexture("homestone_albedo"), size: Size);

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
