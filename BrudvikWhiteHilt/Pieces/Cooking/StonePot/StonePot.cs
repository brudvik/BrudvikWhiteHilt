using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Cooking.StonePot;

/// <summary>
/// A small cauldron made from Meadows materials. It only cooks the White Hilt foods. A <see cref="HerbTray"/> next to it gives level 2.
/// </summary>
public class StonePot : IWhiteHiltCustomPiece
{
    /// <summary>
    /// Prefab name of the stone pot, used as the crafting station of the meadow foods.
    /// </summary>
    public const string PrefabName = "piece_whitehilt_stonepot";

    private const string FullName = "Stone Pot";
    private const string Description = "A simple stone pot for cooking what you forage. Place it over a campfire.";
    private const float Scale = 0.85f;

    private readonly PieceManager instance;
    private readonly HerbTray herbTray = new();

    /// <inheritdoc/>
    public bool Enabled => true;

    /// <inheritdoc/>
    public ProgressionTier DefaultTier => ProgressionTier.Start;

    /// <inheritdoc/>
    public string Id => PrefabName;

    /// <inheritdoc/>
    public string DisplayName => FullName;

    /// <inheritdoc/>
    public string NameToken => Translations.Token(PrefabName);

    /// <inheritdoc/>
    public string GatedPrefabName => PrefabName;

    /// <summary>
    /// Constructor for the StonePot class. Registers the English text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public StonePot(PieceManager instance)
    {
        this.instance = instance;
        Translations.AddEnglish(PrefabName, FullName);
        Translations.AddEnglish($"{PrefabName}_description", Description);
    }

    /// <summary>
    /// Adds the stone pot to the hammer.
    /// </summary>
    public void Add()
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
                    new() { Item = "Flint", Amount = 4, Recover = true },
                    new() { Item = "Wood", Amount = 4, Recover = true }
                }
            };

            CustomPiece piece = new(PrefabName, "piece_cauldron", pieceConfig);
            piece.PiecePrefab.transform.localScale = Vector3.one * Scale;
            TryApplyVisual(piece.PiecePrefab);

            // Recipes match their station by name, so a unique name keeps the cauldron recipes out of the pot.
            CraftingStation station = piece.PiecePrefab.GetComponent<CraftingStation>();
            station.m_name = Translations.Token(PrefabName);

            Sprite icon = VisualHelper.RenderIcon(piece.PiecePrefab);
            if (icon != null)
            {
                piece.Piece.m_icon = icon;
                station.m_icon = icon;
            }

            instance.AddPiece(piece);
            herbTray.Add(instance, station);

            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    // Only the pot hanging from the tripod is replaced, so the chain, tripod and fire effects stay vanilla.
    private static void TryApplyVisual(GameObject piecePrefab)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        try
        {
            Transform pot = piecePrefab.transform.Find("new/cauldron (1)")
                ?? throw new InvalidOperationException("the hanging pot new/cauldron (1) was not found");
            Mesh vanillaPot = pot.GetComponent<MeshFilter>().sharedMesh;
            float lidHeight = piecePrefab.transform.InverseTransformPoint(pot.TransformPoint(vanillaPot.bounds.max)).y;
            VisualHelper.ReplaceMesh(pot.gameObject, ForagingAssets.LoadMesh("stonepot"), ForagingAssets.LoadTexture("stonepot_albedo"), hang: true);

            // The stone pot has a lid, so the boiling water would show below it. Steam rises from the lid instead.
            Transform fire = piecePrefab.transform.Find("HaveFire");
            fire?.Find("Waterplane")?.gameObject.SetActive(false);
            fire?.Find("bubbles")?.gameObject.SetActive(false);
            Transform steam = fire?.Find("steam");
            if (steam != null)
            {
                steam.localPosition = new Vector3(steam.localPosition.x, lidHeight, steam.localPosition.z);
            }
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"{FullName}: keeping the vanilla look: {ex.Message}");
        }
    }
}
