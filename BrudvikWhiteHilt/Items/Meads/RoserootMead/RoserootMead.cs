using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Meads.RoserootMead;

/// <summary>
/// A mead that makes stamina regenerate faster.
/// </summary>
public class RoserootMead : WhiteHiltMeadBase
{
    private readonly ConfigEntry<float> staminaRegenMultiplier;

    /// <summary>
    /// Constructor for the RoserootMead class.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public RoserootMead(ItemManager instance) : base(instance)
    {
        staminaRegenMultiplier = WhiteHiltConfig.BindAdminOnly(ConfigSection, "StaminaRegenMultiplier", 1.5f,
            "Stamina regeneration multiplier while the mead is active.", new AcceptableValueRange<float>(1f, 5f));
    }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltRoserootMead";

    /// <inheritdoc/>
    protected override string FullName => "Roseroot Mead";

    /// <inheritdoc/>
    protected override string Description => "A golden mead brewed with roseroot and crowberries. Your breath comes back faster on the long climbs.";

    /// <inheritdoc/>
    protected override string EffectTooltip => "Stamina regenerates {0}% faster";

    /// <inheritdoc/>
    protected override object[] TooltipValues => new object[] { Helpers.Translations.Percent(staminaRegenMultiplier.Value - 1f) };

    /// <inheritdoc/>
    protected override string CopyMeadFrom => "MeadStaminaMedium";

    /// <inheritdoc/>
    protected override string CopyMeadBaseFrom => "MeadBaseStaminaMedium";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Honey", Amount = 10, Recover = false },
        new() { Item = Foraging.Roseroot.Roseroot.PrefabName, Amount = 5, Recover = false },
        new() { Item = Foraging.Crowberries.Crowberries.PrefabName, Amount = 5, Recover = false }
    };

    /// <inheritdoc/>
    protected override Color Tint => new(1f, 0.8f, 0.5f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override void ConfigureEffect(SE_Stats effect)
    {
        effect.m_staminaRegenMultiplier = staminaRegenMultiplier.Value;
    }
}
