using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Runes.GlowRune;

/// <summary>
/// The Glow Rune: smithed blank at the Rune Forge, given a colour at the Paint Bench from dyes (see
/// <see cref="ColourRunes"/>), and etched at the Rune Etching Table into any White Hilt weapon or shield, which then
/// glows in that colour (see <see cref="Weapons.WeaponGlow"/>). Unlike the other etching runes it needs no bound
/// trophy. A black rune, or Shift + Use on the table, puts a glow out.
/// </summary>
public class GlowRune : EtchingRuneBase
{
    /// <summary>
    /// Prefab name of the glow rune.
    /// </summary>
    public const string Name = "WhiteHiltGlowRune";

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
    protected override string Description => "A runestone whose carved sign holds a spark of surtling fire. Colour it at the Paint Bench, then etch it into a White Hilt weapon or shield at the Rune Etching Table to make it glow.";

    /// <inheritdoc/>
    protected override RequirementConfig[] ExtraRequirements => new RequirementConfig[]
    {
        new() { Item = "SurtlingCore", Amount = 1, Recover = false },
        new() { Item = "Resin", Amount = 5, Recover = false }
    };

    /// <inheritdoc/>
    protected override int MaxStackSize => 1;

    /// <summary>
    /// Stormfist runestone from Magic runestones by reddification; its sign glows in the rune's colour.
    /// </summary>
    protected override string StoneModel => "whglowrune";

    /// <inheritdoc/>
    protected override Color BodyColor => new(0.85f, 0.83f, 0.78f);

    /// <inheritdoc/>
    protected override Color RuneColor => new(1f, 0.95f, 0.7f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <summary>
    /// Registers the English texts of colouring it.
    /// </summary>
    public static void RegisterTranslations()
    {
        Translations.AddEnglish("whitehilt_paint_glow", "Glow Rune");
        Translations.AddEnglish("msg_whitehilt_glow_norune", "You have no Glow Rune to colour");
        Translations.AddEnglish("msg_whitehilt_glow_coloured", "The Glow Rune takes the colour");
    }

    /// <summary>
    /// Whether an item is a glow rune.
    /// </summary>
    /// <param name="item">The item, or null.</param>
    /// <returns>True for a glow rune.</returns>
    public static bool IsGlowRune(ItemDrop.ItemData item) => ColourRunes.Is(item, Name);
}
