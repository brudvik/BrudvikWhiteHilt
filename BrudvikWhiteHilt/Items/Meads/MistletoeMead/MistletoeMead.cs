using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Meads.MistletoeMead;

/// <summary>
/// A cloudy mead brewed with mistletoe and dewberries that makes eitr regenerate faster.
/// </summary>
public class MistletoeMead : WhiteHiltMeadBase
{
    private readonly ConfigEntry<float> eitrRegenMultiplier;

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public MistletoeMead(ItemManager instance) : base(instance)
    {
        eitrRegenMultiplier = WhiteHiltConfig.BindAdminOnly(ConfigSection, "EitrRegenMultiplier", 1.5f,
            "Eitr regeneration multiplier while the mead is active.", new AcceptableValueRange<float>(1f, 5f));
    }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltMistletoeMead";

    /// <inheritdoc/>
    protected override string FullName => "Mistletoe Mead";

    /// <inheritdoc/>
    protected override string Description => "A cloudy mead brewed with mistletoe and dewberries. What killed Baldr fills a mage with eitr.";

    /// <inheritdoc/>
    protected override string EffectTooltip => "Eitr regenerates {0}% faster";

    /// <inheritdoc/>
    protected override object[] TooltipValues => new object[] { Helpers.Translations.Percent(eitrRegenMultiplier.Value - 1f) };

    /// <inheritdoc/>
    protected override string CopyMeadFrom => "MeadEitrMinor";

    /// <inheritdoc/>
    protected override string CopyMeadBaseFrom => "MeadBaseEitrMinor";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Honey", Amount = 10, Recover = false },
        new() { Item = Foraging.Mistletoe.Mistletoe.PrefabName, Amount = 5, Recover = false },
        new() { Item = Foraging.Dewberries.Dewberries.PrefabName, Amount = 5, Recover = false }
    };

    /// <inheritdoc/>
    protected override Color Tint => new(0.75f, 0.85f, 0.6f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override void ConfigureEffect(SE_Stats effect)
    {
        effect.m_eitrRegenMultiplier = eitrRegenMultiplier.Value;
    }
}
