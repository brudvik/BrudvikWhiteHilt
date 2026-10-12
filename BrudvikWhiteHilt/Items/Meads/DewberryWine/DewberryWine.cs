using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Meads.DewberryWine;

/// <summary>
/// A dark wine of dewberries that makes the drinker's steps quieter.
/// </summary>
public class DewberryWine : WhiteHiltMeadBase
{
    private readonly ConfigEntry<float> noiseReduction;

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public DewberryWine(ItemManager instance) : base(instance)
    {
        noiseReduction = WhiteHiltConfig.BindAdminOnly(ConfigSection, "NoiseReduction", 0.5f,
            "How much less noise you make (0.5 = half).", new AcceptableValueRange<float>(0f, 1f));
    }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltDewberryWine";

    /// <inheritdoc/>
    protected override string FullName => "Dewberry Wine";

    /// <inheritdoc/>
    protected override string Description => "A dark wine of dewberries with a breath of lady's mantle. Your steps grow as quiet as the mist.";

    /// <inheritdoc/>
    protected override string EffectTooltip => "You make {0}% less noise";

    /// <inheritdoc/>
    protected override object[] TooltipValues => new object[] { Helpers.Translations.Percent(noiseReduction.Value) };

    /// <inheritdoc/>
    protected override string CopyMeadFrom => "MeadEitrLingering";

    /// <inheritdoc/>
    protected override string CopyMeadBaseFrom => "MeadBaseEitrLingering";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Honey", Amount = 10, Recover = false },
        new() { Item = Foraging.Dewberries.Dewberries.PrefabName, Amount = 10, Recover = false },
        new() { Item = Foraging.LadysMantle.LadysMantle.PrefabName, Amount = 3, Recover = false }
    };

    /// <inheritdoc/>
    protected override Color Tint => new(0.55f, 0.5f, 0.85f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override void ConfigureEffect(SE_Stats effect)
    {
        effect.m_noiseModifier = -noiseReduction.Value;
    }
}
