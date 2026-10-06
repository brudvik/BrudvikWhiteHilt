using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Painting;
using Jotunn.Configs;
using Jotunn.Managers;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Runes.GlowRune;

/// <summary>
/// The Glow Rune: smithed blank at the Rune Forge, given a colour at the Paint Bench from dyes, and etched at the Rune
/// Etching Table into any White Hilt weapon or shield, which then glows in that colour (see
/// <see cref="Weapons.WeaponGlow"/>). Unlike the other etching runes it needs no bound trophy. A black rune puts a glow
/// out. Each rune keeps its own colour, so they do not stack.
/// </summary>
public class GlowRune : EtchingRuneBase
{
    /// <summary>
    /// Prefab name of the glow rune.
    /// </summary>
    public const string Name = "WhiteHiltGlowRune";

    private const string ColorKey = "whitehilt_glow_rune_color";

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public GlowRune(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => Name;

    /// <inheritdoc/>
    protected override string FullName => "Glow Rune";

    /// <inheritdoc/>
    protected override string Description => "A pale ring with a spark of surtling fire in its runes. Colour it at the Paint Bench, then etch it into a White Hilt weapon or shield at the Rune Etching Table to make it glow. A black rune puts a glow out.";

    /// <inheritdoc/>
    protected override RequirementConfig[] ExtraRequirements => new RequirementConfig[]
    {
        new() { Item = "SurtlingCore", Amount = 1, Recover = false },
        new() { Item = "Resin", Amount = 5, Recover = false }
    };

    /// <inheritdoc/>
    protected override int MaxStackSize => 1;

    /// <inheritdoc/>
    protected override Color BodyColor => new(0.85f, 0.83f, 0.78f);

    /// <inheritdoc/>
    protected override Color RuneColor => new(1f, 0.95f, 0.7f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <summary>
    /// Registers the English texts of colouring and etching it.
    /// </summary>
    public static void RegisterTranslations()
    {
        Translations.AddEnglish("whitehilt_glow_rune_colour", "Colour: {0}");
        Translations.AddEnglish("whitehilt_glow_rune_blank", "Not yet coloured: colour it at the Paint Bench");
        Translations.AddEnglish("whitehilt_paint_glow", "Colour Glow Rune");
        Translations.AddEnglish("msg_whitehilt_glow_norune", "You have no Glow Rune to colour");
        Translations.AddEnglish("msg_whitehilt_glow_coloured", "The Glow Rune takes the colour");
    }

    /// <summary>
    /// Whether an item is a glow rune.
    /// </summary>
    /// <param name="item">The item, or null.</param>
    /// <returns>True for a glow rune.</returns>
    public static bool IsGlowRune(ItemDrop.ItemData item)
    {
        return item?.m_dropPrefab != null && item.m_dropPrefab.name == Name;
    }

    /// <summary>
    /// The colour of a glow rune.
    /// </summary>
    /// <param name="item">The rune.</param>
    /// <param name="color">Its colour.</param>
    /// <returns>False if it is not a coloured glow rune.</returns>
    public static bool TryGetColor(ItemDrop.ItemData item, out Color32 color)
    {
        color = default;
        return IsGlowRune(item) && item.m_customData.TryGetValue(ColorKey, out string hex) && PaintColor.TryParseHex(hex, out color);
    }

    /// <summary>
    /// The glow rune in the player's inventory to colour next: an uncoloured one first.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>The rune, or null if the player has none.</returns>
    public static ItemDrop.ItemData FindToColour(Player player)
    {
        ItemDrop.ItemData[] runes = player.GetInventory().GetAllItems().Where(IsGlowRune).ToArray();
        return runes.FirstOrDefault(rune => !TryGetColor(rune, out _)) ?? runes.FirstOrDefault();
    }

    /// <summary>
    /// Gives a glow rune a colour.
    /// </summary>
    /// <param name="item">The rune.</param>
    /// <param name="color">The colour.</param>
    public static void SetColor(ItemDrop.ItemData item, Color32 color)
    {
        item.m_customData[ColorKey] = PaintColor.ToHex(color);
    }

    /// <summary>
    /// The tooltip line of a glow rune: its colour, or how to give it one.
    /// </summary>
    /// <param name="item">The rune.</param>
    /// <returns>The line, or nothing for other items.</returns>
    public static string TooltipText(ItemDrop.ItemData item)
    {
        if (!IsGlowRune(item))
        {
            return string.Empty;
        }

        Localization localization = Localization.instance;
        return "\n" + (TryGetColor(item, out Color32 color)
            ? string.Format(localization.Localize("$whitehilt_glow_rune_colour"), PaintColor.Swatch(color))
            : "<color=orange>" + localization.Localize("$whitehilt_glow_rune_blank") + "</color>");
    }
}
