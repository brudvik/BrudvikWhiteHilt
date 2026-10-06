using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Meads.YarrowGruit;

/// <summary>
/// A gruit ale bittered with yarrow, the wound herb: health regenerates much faster while you are badly hurt.
/// </summary>
public class YarrowGruit : WhiteHiltMeadBase
{
    private static ConfigEntry<float> threshold;
    private static ConfigEntry<float> multiplier;

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public YarrowGruit(ItemManager instance) : base(instance)
    {
        threshold = WhiteHiltConfig.BindAdminOnly(ConfigSection, "WoundedBelow", 0.5f,
            "Share of max health below which the gruit speeds up healing (0.5 = half).", new AcceptableValueRange<float>(0.1f, 1f));
        multiplier = WhiteHiltConfig.BindAdminOnly(ConfigSection, "WoundedRegenMultiplier", 2.5f,
            "Health regeneration multiplier while wounded.", new AcceptableValueRange<float>(1f, 10f));
    }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltYarrowGruit";

    /// <inheritdoc/>
    protected override string FullName => "Yarrow Gruit";

    /// <inheritdoc/>
    protected override string Description => "An old ale bittered with yarrow instead of hops. Warriors drank it before battle: wounds close fast with it in the blood.";

    /// <inheritdoc/>
    protected override string EffectTooltip => "Below {0}% health, health regenerates {1} times as fast";

    /// <inheritdoc/>
    protected override object[] TooltipValues => new object[]
    {
        Helpers.Translations.Number(threshold.Value * 100f), Helpers.Translations.Number(multiplier.Value)
    };

    /// <inheritdoc/>
    protected override string CopyMeadFrom => "MeadHealthMedium";

    /// <inheritdoc/>
    protected override string CopyMeadBaseFrom => "MeadBaseHealthMedium";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = Foraging.Yarrow.Yarrow.PrefabName, Amount = 8, Recover = false },
        new() { Item = "Barley", Amount = 5, Recover = false },
        new() { Item = "Honey", Amount = 3, Recover = false }
    };

    /// <inheritdoc/>
    protected override Color Tint => new(0.85f, 0.8f, 0.6f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override SE_Stats CreateEffectInstance()
    {
        return ScriptableObject.CreateInstance<WoundHealingEffect>();
    }

    /// <inheritdoc/>
    protected override void ConfigureEffect(SE_Stats effect)
    {
        if (effect is WoundHealingEffect healing)
        {
            healing.Threshold = threshold.Value;
            healing.Multiplier = multiplier.Value;
        }
    }
}

/// <summary>
/// Faster health regeneration while the character is below a share of its health.
/// </summary>
public class WoundHealingEffect : SE_Stats
{
    /// <summary>Share of max health below which healing speeds up.</summary>
    public float Threshold = 0.5f;

    /// <summary>Health regeneration multiplier while wounded.</summary>
    public float Multiplier = 2.5f;

    /// <inheritdoc/>
    public override void ModifyHealthRegen(ref float regenMultiplier)
    {
        base.ModifyHealthRegen(ref regenMultiplier);
        if (m_character != null && m_character.GetHealthPercentage() < Threshold)
        {
            regenMultiplier *= Multiplier;
        }
    }
}
