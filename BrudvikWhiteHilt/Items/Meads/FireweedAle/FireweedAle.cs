using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Meads.FireweedAle;

/// <summary>
/// A red-gold ale of fireweed that makes running cheaper and the drinker faster.
/// </summary>
public class FireweedAle : WhiteHiltMeadBase
{
    private readonly ConfigEntry<float> runStaminaReduction;
    private readonly ConfigEntry<float> speedBonus;

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public FireweedAle(ItemManager instance) : base(instance)
    {
        runStaminaReduction = WhiteHiltConfig.BindAdminOnly(ConfigSection, "RunStaminaReduction", 0.25f,
            "How much less stamina running uses (0.25 = a quarter).", new AcceptableValueRange<float>(0f, 1f));
        speedBonus = WhiteHiltConfig.BindAdminOnly(ConfigSection, "SpeedBonus", 0.1f,
            "How much faster you move (0.1 = 10%).", new AcceptableValueRange<float>(0f, 0.5f));
    }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltFireweedAle";

    /// <inheritdoc/>
    protected override string FullName => "Fireweed Ale";

    /// <inheritdoc/>
    protected override string Description => "A red-gold ale brewed with fireweed. Light in the legs across the hot ash.";

    /// <inheritdoc/>
    protected override string EffectTooltip => "Running uses {0}% less stamina and you move {1}% faster";

    /// <inheritdoc/>
    protected override object[] TooltipValues => new object[]
    {
        Helpers.Translations.Percent(runStaminaReduction.Value), Helpers.Translations.Percent(speedBonus.Value)
    };

    /// <inheritdoc/>
    protected override string CopyMeadFrom => "MeadStrength";

    /// <inheritdoc/>
    protected override string CopyMeadBaseFrom => "MeadBaseStrength";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = Foraging.Fireweed.Fireweed.PrefabName, Amount = 8, Recover = false },
        new() { Item = "Barley", Amount = 5, Recover = false },
        new() { Item = "Honey", Amount = 3, Recover = false }
    };

    /// <inheritdoc/>
    protected override Color Tint => new(0.95f, 0.55f, 0.6f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override void ConfigureEffect(SE_Stats effect)
    {
        effect.m_runStaminaDrainModifier = -runStaminaReduction.Value;
        effect.m_speedModifier = speedBonus.Value;
    }
}
