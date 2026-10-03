using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Meads.HopAle;

/// <summary>
/// A hopped ale that makes you sleep deep: Rested lasts longer while it is in you.
/// </summary>
public class HopAle : WhiteHiltMeadBase
{
    /// <summary>
    /// Status effect name of the ale, for the Rested patch.
    /// </summary>
    public const string EffectName = "SE_WhiteHiltHopAle";

    private static ConfigEntry<float> restedBonus;

    /// <summary>
    /// Constructor for the HopAle class.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public HopAle(ItemManager instance) : base(instance)
    {
        restedBonus = WhiteHiltConfig.BindAdminOnly(ConfigSection, "RestedBonus", 0.5f,
            "How much longer Rested lasts while the ale is active (0.5 = 50%).", new AcceptableValueRange<float>(0f, 3f));
    }

    /// <summary>
    /// How much longer Rested lasts, as a share.
    /// </summary>
    public static float RestedBonus => restedBonus?.Value ?? 0f;

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltHopAle";

    /// <inheritdoc/>
    protected override string FullName => "Hop Ale";

    /// <inheritdoc/>
    protected override string Description => "A clear, bitter ale brewed with wild hops. It keeps for months, and after a mug you sleep like the dead.";

    /// <inheritdoc/>
    protected override string EffectTooltip => "Rested lasts {0}% longer";

    /// <inheritdoc/>
    protected override object[] TooltipValues => new object[] { Helpers.Translations.Number(RestedBonus * 100f) };

    /// <inheritdoc/>
    protected override string CopyMeadFrom => "MeadStaminaMedium";

    /// <inheritdoc/>
    protected override string CopyMeadBaseFrom => "MeadBaseStaminaMedium";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = Foraging.WildHops.WildHops.PrefabName, Amount = 8, Recover = false },
        new() { Item = "Barley", Amount = 6, Recover = false },
        new() { Item = "Honey", Amount = 2, Recover = false }
    };

    /// <inheritdoc/>
    protected override Color Tint => new(1f, 0.9f, 0.55f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override float DurationSeconds => 1800f;

    /// <inheritdoc/>
    protected override SE_Stats CreateEffectInstance()
    {
        return ScriptableObject.CreateInstance<DeepSleepEffect>();
    }

    /// <inheritdoc/>
    protected override void ConfigureEffect(SE_Stats effect)
    {
    }
}

/// <summary>
/// While active, Rested lasts longer; an active Rested is stretched when the ale is drunk.
/// </summary>
public class DeepSleepEffect : SE_Stats
{
    /// <inheritdoc/>
    public override void Setup(Character character)
    {
        base.Setup(character);
        StatusEffect rested = character?.GetSEMan().GetStatusEffect(SEMan.s_statusEffectRested);
        if (rested != null)
        {
            rested.m_ttl *= 1f + HopAle.RestedBonus;
        }
    }
}
