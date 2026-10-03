using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Meads.MeadowsweetMead;

/// <summary>
/// A sweet mead that dulls pain: more armor and less stagger.
/// </summary>
public class MeadowsweetMead : WhiteHiltMeadBase
{
    private readonly ConfigEntry<float> armorMultiplier;
    private readonly ConfigEntry<float> staggerReduction;

    /// <summary>
    /// Constructor for the MeadowsweetMead class.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public MeadowsweetMead(ItemManager instance) : base(instance)
    {
        armorMultiplier = WhiteHiltConfig.BindAdminOnly(ConfigSection, "ArmorMultiplier", 0.15f,
            "Extra armor while the mead is active (0.15 = 15%).", new AcceptableValueRange<float>(0f, 2f));
        staggerReduction = WhiteHiltConfig.BindAdminOnly(ConfigSection, "StaggerReduction", 0.25f,
            "How much less you are staggered while the mead is active (0.25 = 25%).", new AcceptableValueRange<float>(0f, 1f));
    }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltMeadowsweetMead";

    /// <inheritdoc/>
    protected override string FullName => "Meadowsweet Mead";

    /// <inheritdoc/>
    protected override string Description => "A pale, honey-sweet mead steeped with meadowsweet. Takes the sting out of every blow.";

    /// <inheritdoc/>
    protected override string EffectTooltip => "Armor +{0}%, you are staggered {1}% less";

    /// <inheritdoc/>
    protected override object[] TooltipValues => new object[]
    {
        Helpers.Translations.Percent(armorMultiplier.Value), Helpers.Translations.Percent(staggerReduction.Value)
    };

    /// <inheritdoc/>
    protected override string CopyMeadFrom => "MeadTasty";

    /// <inheritdoc/>
    protected override string CopyMeadBaseFrom => "MeadBaseTasty";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Honey", Amount = 10, Recover = false },
        new() { Item = Foraging.Meadowsweet.Meadowsweet.PrefabName, Amount = 8, Recover = false },
        new() { Item = Foraging.Cranberries.Cranberries.PrefabName, Amount = 4, Recover = false }
    };

    /// <inheritdoc/>
    protected override Color Tint => new(1f, 0.95f, 0.72f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override void ConfigureEffect(SE_Stats effect)
    {
        effect.m_armorMultiplier = armorMultiplier.Value;
        effect.m_staggerModifier = -staggerReduction.Value;
    }
}
