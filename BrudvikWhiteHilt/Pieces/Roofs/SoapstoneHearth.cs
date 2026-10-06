using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Roofing;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Roofs;

/// <summary>
/// A hearth of carved soapstone, which holds the heat of the fire: it burns longer on its wood and gives more comfort
/// than the vanilla hearth. Goes well under a smoke hole.
/// </summary>
public class SoapstoneHearth : IWhiteHiltCustomPiece
{
    /// <summary>
    /// Prefab name of the soapstone hearth.
    /// </summary>
    public const string PrefabName = "piece_whitehilt_soapstonehearth";

    private const string FullName = "Soapstone Hearth";
    private const string Description = "A hearth of carved soapstone. The stone holds the heat of the fire, so it burns longer on its wood and warms the house more.";

    private static GameObject prefab;
    private static float vanillaSecPerFuel;

    private readonly PieceManager instance;

    /// <inheritdoc/>
    public bool Enabled => true;

    /// <inheritdoc/>
    public ProgressionTier DefaultTier => ProgressionTier.Mountain;

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
    public SoapstoneHearth(PieceManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(PrefabName, FullName, Description);
    }

    /// <summary>
    /// Sets the hearth's burn time and comfort from the config.
    /// </summary>
    public static void ApplyConfig()
    {
        if (prefab == null)
        {
            return;
        }

        Fireplace fireplace = prefab.GetComponent<Fireplace>();
        if (fireplace != null && vanillaSecPerFuel > 0f)
        {
            fireplace.m_secPerFuel = vanillaSecPerFuel * RoofSettings.HearthBurnMultiplier.Value;
        }

        Piece piece = prefab.GetComponent<Piece>();
        if (piece != null)
        {
            piece.m_comfort = RoofSettings.HearthComfort.Value;
        }
    }

    /// <summary>
    /// Adds the soapstone hearth to the hammer.
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
                    new() { Item = RoofMaterials.Soapstone, Amount = 10, Recover = true },
                    new() { Item = "Stone", Amount = 6, Recover = true }
                }
            };

            CustomPiece piece = new(PrefabName, "hearth", pieceConfig);
            prefab = piece.PiecePrefab;
            Fireplace fireplace = prefab.GetComponent<Fireplace>() ?? throw new InvalidOperationException("hearth has no Fireplace");
            vanillaSecPerFuel = fireplace.m_secPerFuel;
            ApplyConfig();

            if (!VisualHelper.IsHeadless)
            {
                // Grey-green soapstone instead of grey granite; the fire keeps its colours.
                VisualHelper.Recolor(prefab, pixel =>
                {
                    Color.RGBToHSV(pixel, out _, out float saturation, out float value);
                    if (saturation > 0.4f)
                    {
                        return pixel;
                    }

                    Color result = Color.Lerp(new Color(value, value, value), new Color(0.58f, 0.66f, 0.6f) * (0.5f + value * 0.7f), 0.75f);
                    result.a = pixel.a / 255f;
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
