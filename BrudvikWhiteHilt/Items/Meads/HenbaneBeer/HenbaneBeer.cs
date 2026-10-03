using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Meads.HenbaneBeer;

/// <summary>
/// Henbane beer, the berserker's brew: more damage and no stagger, but less armor, and a crash when it wears off.
/// </summary>
public class HenbaneBeer : WhiteHiltMeadBase
{
    private static ConfigEntry<float> damageBonus;
    private static ConfigEntry<float> armorPenalty;
    private static ConfigEntry<float> crashShare;

    /// <summary>
    /// Constructor for the HenbaneBeer class.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public HenbaneBeer(ItemManager instance) : base(instance)
    {
        damageBonus = WhiteHiltConfig.BindAdminOnly(ConfigSection, "DamageBonus", 0.3f,
            "Extra blunt, slash and pierce damage while the beer is active (0.3 = 30%).", new AcceptableValueRange<float>(0f, 2f));
        armorPenalty = WhiteHiltConfig.BindAdminOnly(ConfigSection, "ArmorPenalty", 0.5f,
            "Share of armor lost while the beer is active (0.5 = half).", new AcceptableValueRange<float>(0f, 1f));
        crashShare = WhiteHiltConfig.BindAdminOnly(ConfigSection, "CrashHealthShare", 0.2f,
            "Share of max health lost when the beer wears off; it never kills.", new AcceptableValueRange<float>(0f, 0.9f));
    }

    /// <summary>
    /// Share of max health lost when the beer wears off.
    /// </summary>
    public static float CrashShare => crashShare?.Value ?? 0f;

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltHenbaneBeer";

    /// <inheritdoc/>
    protected override string FullName => "Henbane Beer";

    /// <inheritdoc/>
    protected override string Description => "A dark, sickly-sweet beer brewed with henbane. The berserkers drank it and howled: no pain, no fear, no sense.";

    /// <inheritdoc/>
    protected override string EffectTooltip => "Berserkergang: +{0}% damage, no stagger, {1}% less armor; you lose {2}% health when it ends";

    /// <inheritdoc/>
    protected override object[] TooltipValues => new object[]
    {
        Helpers.Translations.Number(damageBonus.Value * 100f), Helpers.Translations.Number(armorPenalty.Value * 100f),
        Helpers.Translations.Number(crashShare.Value * 100f)
    };

    /// <inheritdoc/>
    protected override string CopyMeadFrom => "MeadBugRepellent";

    /// <inheritdoc/>
    protected override string CopyMeadBaseFrom => "MeadBaseBugRepellent";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = Foraging.Henbane.Henbane.PrefabName, Amount = 4, Recover = false },
        new() { Item = "Barley", Amount = 6, Recover = false },
        new() { Item = "Honey", Amount = 4, Recover = false }
    };

    /// <inheritdoc/>
    protected override Color Tint => new(0.55f, 0.35f, 0.5f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override float DurationSeconds => 300f;

    /// <inheritdoc/>
    protected override SE_Stats CreateEffectInstance()
    {
        return ScriptableObject.CreateInstance<BerserkerEffect>();
    }

    /// <inheritdoc/>
    protected override void ConfigureEffect(SE_Stats effect)
    {
        float bonus = damageBonus.Value;
        effect.m_percentigeDamageModifiers = new HitData.DamageTypes { m_blunt = bonus, m_slash = bonus, m_pierce = bonus };
        effect.m_armorMultiplier = -armorPenalty.Value;
        effect.m_staggerModifier = -1f;
    }
}

/// <summary>
/// The berserker's rage; when it ends the body pays for it.
/// </summary>
public class BerserkerEffect : SE_Stats
{
    /// <inheritdoc/>
    public override void Stop()
    {
        if (m_character != null && m_character.m_nview != null && m_character.m_nview.IsOwner() && !m_character.IsDead())
        {
            float health = m_character.GetHealth() - m_character.GetMaxHealth() * HenbaneBeer.CrashShare;
            m_character.SetHealth(Mathf.Max(1f, health));
        }

        base.Stop();
    }
}
