using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Runes.BloodRune;

/// <summary>
/// An etching rune of iron and blood. Etched with henbane it gives the Berserker's Rage: the more wounded the wielder,
/// the harder the weapon hits.
/// </summary>
public class BloodRune : EtchingRuneBase
{
    /// <summary>
    /// Prefab name of the blood rune.
    /// </summary>
    public const string Name = "WhiteHiltBloodRune";

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public BloodRune(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => Name;

    /// <inheritdoc/>
    protected override string FullName => "Blood Rune";

    /// <inheritdoc/>
    protected override string Description => "An iron ring quenched in the blood of leeches and carved with the runes of rage. Etched into a bound weapon with henbane, it hits harder the more you bleed.";

    /// <inheritdoc/>
    protected override RequirementConfig[] ExtraRequirements => new RequirementConfig[]
    {
        new() { Item = "Bloodbag", Amount = 5, Recover = false }
    };

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Plains;

    /// <inheritdoc/>
    protected override Color BodyColor => new(0.4f, 0.38f, 0.37f);

    /// <inheritdoc/>
    protected override Color RuneColor => new(0.85f, 0.08f, 0.08f);

    /// <inheritdoc/>
    public override bool Enabled => true;
}
