using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Roofing;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Roofs;

/// <summary>
/// A tar kiln (tjæremile): burns resinous core wood slowly into pine tar for the shingle roofs. A charcoal kiln with
/// another recipe and a darker, tarred look.
/// </summary>
public class TarKiln : IWhiteHiltCustomPiece
{
    /// <summary>
    /// Prefab name of the tar kiln.
    /// </summary>
    public const string PrefabName = "piece_whitehilt_tarkiln";

    private const string FullName = "Tar Kiln";
    private const string Description = "A kiln that burns resinous core wood slowly into pine tar, for shingle roofs that last.";

    private static GameObject prefab;

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
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run. Registers the English text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public TarKiln(PieceManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(PrefabName, FullName, Description);
    }

    /// <summary>
    /// Sets the kiln's speed and capacity from the config.
    /// </summary>
    public static void ApplyConfig()
    {
        Smelter smelter = prefab != null ? prefab.GetComponent<Smelter>() : null;
        if (smelter != null)
        {
            smelter.m_secPerProduct = RoofSettings.TarKilnSeconds.Value;
            smelter.m_maxOre = RoofSettings.TarKilnCapacity.Value;
        }
    }

    /// <summary>
    /// Adds the tar kiln to the hammer.
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
                    new() { Item = "Stone", Amount = 20, Recover = true },
                    new() { Item = "RoundLog", Amount = 5, Recover = true },
                    new() { Item = "Resin", Amount = 10, Recover = true }
                }
            };

            CustomPiece piece = new(PrefabName, "charcoal_kiln", pieceConfig);
            prefab = piece.PiecePrefab;
            Smelter smelter = prefab.GetComponent<Smelter>() ?? throw new InvalidOperationException("charcoal_kiln has no Smelter");
            ItemDrop coreWood = PrefabManager.Instance.GetPrefab("RoundLog")?.GetComponent<ItemDrop>() ?? throw new InvalidOperationException("RoundLog not found");
            ItemDrop tar = PrefabManager.Instance.GetPrefab(RoofMaterials.PineTar)?.GetComponent<ItemDrop>() ?? throw new InvalidOperationException($"{RoofMaterials.PineTar} not found");
            smelter.m_name = Translations.Token(PrefabName);
            smelter.m_addOreTooltip = "$piece_smelter_add $item_roundlog";
            smelter.m_conversion = new List<Smelter.ItemConversion> { new() { m_from = coreWood, m_to = tar } };
            ApplyConfig();

            if (!VisualHelper.IsHeadless)
            {
                // Soot and tar: darker and warmer than the charcoal kiln.
                VisualHelper.Recolor(prefab, pixel =>
                {
                    Color colour = pixel;
                    Color result = new(colour.r * 0.62f, colour.g * 0.5f, colour.b * 0.4f, colour.a);
                    return result;
                });
                Sprite icon = VisualHelper.RenderIcon(prefab);
                if (icon != null)
                {
                    piece.Piece.m_icon = icon;
                }
            }

            instance.AddPiece(piece);
            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }
}
