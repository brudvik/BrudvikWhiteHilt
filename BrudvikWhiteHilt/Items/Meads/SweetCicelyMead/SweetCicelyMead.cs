using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Meads.SweetCicelyMead;

/// <summary>
/// A pale, anise-sweet mead of sweet cicely that makes sneaking cheaper, for stalking through the mist.
/// </summary>
public class SweetCicelyMead : WhiteHiltMeadBase
{
    private readonly ConfigEntry<float> sneakStaminaReduction;

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public SweetCicelyMead(ItemManager instance) : base(instance)
    {
        sneakStaminaReduction = WhiteHiltConfig.BindAdminOnly(ConfigSection, "SneakStaminaReduction", 0.5f,
            "How much less stamina sneaking uses (0.5 = half).", new AcceptableValueRange<float>(0f, 1f));
    }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltSweetCicelyMead";

    /// <inheritdoc/>
    protected override string FullName => "Sweet Cicely Mead";

    /// <inheritdoc/>
    protected override string Description => "A pale mead that tastes of anise and needs little honey. Soft on the tongue and soft on the feet.";

    /// <inheritdoc/>
    protected override string EffectTooltip => "Sneaking uses {0}% less stamina";

    /// <inheritdoc/>
    protected override object[] TooltipValues => new object[] { Helpers.Translations.Percent(sneakStaminaReduction.Value) };

    /// <inheritdoc/>
    protected override string CopyMeadFrom => "MeadStaminaMinor";

    /// <inheritdoc/>
    protected override string CopyMeadBaseFrom => "MeadBaseStaminaMinor";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Honey", Amount = 4, Recover = false },
        new() { Item = Foraging.SweetCicely.SweetCicely.PrefabName, Amount = 8, Recover = false },
        new() { Item = Foraging.WoodSorrel.WoodSorrel.PrefabName, Amount = 3, Recover = false }
    };

    /// <inheritdoc/>
    protected override Color Tint => new(0.9f, 0.95f, 0.8f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override void ConfigureEffect(SE_Stats effect)
    {
        effect.m_sneakStaminaUseModifier = -sneakStaminaReduction.Value;
    }
}
