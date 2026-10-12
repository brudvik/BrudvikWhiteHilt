using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Monsters;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Meads.HeartwoodDraught;

/// <summary>
/// A mead brewed with Living Heartwood from the Yggdrasil Warden: health regenerates twice as fast.
/// </summary>
public class HeartwoodDraught : WhiteHiltMeadBase
{
    private readonly BepInEx.Configuration.ConfigEntry<float> healthRegenMultiplier;

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public HeartwoodDraught(ItemManager instance) : base(instance)
    {
        healthRegenMultiplier = WhiteHiltConfig.BindAdminOnly(ConfigSection, "HealthRegenMultiplier", 2f,
            "Health regeneration multiplier while the draught is active.", new BepInEx.Configuration.AcceptableValueRange<float>(1f, 5f));
    }

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override string BaseName => WildMonsters.HeartwoodDraught;

    /// <inheritdoc/>
    protected override string FullName => "Heartwood Draught";

    /// <inheritdoc/>
    protected override string Description => "Honey and sap steeped on living heartwood. It tastes of sap and rain, and wounds close as if the wood grew over them.";

    /// <inheritdoc/>
    protected override string EffectTooltip => "Health regenerates {0}% faster";

    /// <inheritdoc/>
    protected override object[] TooltipValues => new object[] { Translations.Percent(healthRegenMultiplier.Value - 1f) };

    /// <inheritdoc/>
    protected override string CopyMeadFrom => "MeadHealthLingering";

    /// <inheritdoc/>
    protected override string CopyMeadBaseFrom => "MeadBaseHealthLingering";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Honey", Amount = 10, Recover = false },
        new() { Item = WildMonsters.LivingHeartwood, Amount = 2, Recover = false },
        new() { Item = "Sap", Amount = 5, Recover = false }
    };

    /// <inheritdoc/>
    protected override Color Tint => new(0.55f, 0.85f, 0.5f);

    /// <inheritdoc/>
    protected override void ConfigureEffect(SE_Stats effect)
    {
        effect.m_healthRegenMultiplier = healthRegenMultiplier.Value;
    }
}
