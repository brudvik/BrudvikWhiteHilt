using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Runes.FlameRune;

/// <summary>
/// The Flame Rune: smithed blank at the Rune Forge, given a colour at the Paint Bench from dyes (see
/// <see cref="ColourRunes"/>), and etched at the Rune Etching Table into any White Hilt weapon or shield, which then
/// burns in that colour (see <see cref="Weapons.WeaponFlame"/>): the sword's and the staffs' own flames change colour,
/// other gear catches fire along its head. It needs no bound trophy; Alt + Use on the table puts the flame out.
/// </summary>
public class FlameRune : EtchingRuneBase
{
    /// <summary>
    /// Prefab name of the flame rune.
    /// </summary>
    public const string Name = "WhiteHiltFlameRune";

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public FlameRune(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => Name;

    /// <inheritdoc/>
    protected override string FullName => "Flame Rune";

    /// <inheritdoc/>
    protected override string Description => "A runestone with surtling fire smouldering in its carved sign. Colour it at the Paint Bench, then etch it into a White Hilt weapon or shield at the Rune Etching Table to set it burning in that colour.";

    /// <inheritdoc/>
    protected override RequirementConfig[] ExtraRequirements => new RequirementConfig[]
    {
        new() { Item = "SurtlingCore", Amount = 2, Recover = false },
        new() { Item = "Coal", Amount = 5, Recover = false }
    };

    /// <inheritdoc/>
    protected override int MaxStackSize => 1;

    /// <summary>
    /// Firebolt runestone from Magic runestones by reddification; its sign glows in the rune's colour.
    /// </summary>
    protected override string StoneModel => "whflamerune";

    /// <inheritdoc/>
    protected override Color BodyColor => new(0.18f, 0.15f, 0.13f);

    /// <inheritdoc/>
    protected override Color RuneColor => new(1f, 0.45f, 0.1f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <summary>
    /// Registers the English texts of colouring it.
    /// </summary>
    public static void RegisterTranslations()
    {
        Translations.AddEnglish("whitehilt_paint_flame", "Flame Rune");
        Translations.AddEnglish("msg_whitehilt_flame_norune", "You have no Flame Rune to colour");
        Translations.AddEnglish("msg_whitehilt_flame_coloured", "The Flame Rune takes the colour");
    }

    /// <summary>
    /// Whether an item is a flame rune.
    /// </summary>
    /// <param name="item">The item, or null.</param>
    /// <returns>True for a flame rune.</returns>
    public static bool IsFlameRune(ItemDrop.ItemData item) => ColourRunes.Is(item, Name);
}
