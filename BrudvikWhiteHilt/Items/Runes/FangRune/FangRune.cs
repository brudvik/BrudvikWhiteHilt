using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Runes.FangRune;

/// <summary>
/// An etching rune of iron and wolf fangs. Etched with wolf lichen it gives Wolfsbane: poison that bites beasts
/// hardest.
/// </summary>
public class FangRune : EtchingRuneBase
{
    /// <summary>
    /// Prefab name of the fang rune.
    /// </summary>
    public const string Name = "WhiteHiltFangRune";

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public FangRune(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => Name;

    /// <inheritdoc/>
    protected override string FullName => "Fang Rune";

    /// <inheritdoc/>
    protected override string Description => "An iron ring set with wolf fangs and carved with hunting runes. Etched into a bound weapon with wolf lichen, it poisons beasts.";

    /// <inheritdoc/>
    protected override RequirementConfig[] ExtraRequirements => new RequirementConfig[]
    {
        new() { Item = "WolfFang", Amount = 3, Recover = false },
        new() { Item = "Silver", Amount = 1, Recover = false }
    };

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Mountain;

    /// <inheritdoc/>
    protected override Color BodyColor => new(0.75f, 0.72f, 0.68f);

    /// <inheritdoc/>
    protected override Color RuneColor => new(0.85f, 0.92f, 0.2f);

    /// <inheritdoc/>
    public override bool Enabled => true;
}
