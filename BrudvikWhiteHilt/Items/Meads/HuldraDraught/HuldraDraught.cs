using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Monsters;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Meads.HuldraDraught;

/// <summary>
/// A draught brewed with Huldra Bark: for a short while you are hidden like the hidden folk. Creatures neither see nor
/// hear you unless they are right beside you, and those that hunted you lose you (<see cref="HuldraVeilEffect"/>). An
/// attack of your own breaks the veil.
/// </summary>
public class HuldraDraught : WhiteHiltMeadBase
{
    /// <summary>Prefab name of the draught.</summary>
    public const string Name = "WhiteHiltHuldraDraught";

    private readonly BepInEx.Configuration.ConfigEntry<float> revealRange;

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public HuldraDraught(ItemManager instance) : base(instance)
    {
        revealRange = WhiteHiltConfig.BindAdminOnly(ConfigSection, "RevealRange", 2.5f,
            "Metres within which creatures still sense you while the draught is active.", new BepInEx.Configuration.AcceptableValueRange<float>(0f, 20f));
        HuldraVeilEffect.RevealRange = () => revealRange.Value;
    }

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Mistlands;

    /// <inheritdoc/>
    protected override string BaseName => Name;

    /// <inheritdoc/>
    protected override string FullName => "Huldra Draught";

    /// <inheritdoc/>
    protected override string Description => "Bark from a Huldra's hollow back, steeped in mist and honey. For a while you are one of the hidden folk.";

    /// <inheritdoc/>
    protected override string EffectTooltip => "Creatures cannot see or hear you beyond {0} m; your own attack breaks the veil";

    /// <inheritdoc/>
    protected override object[] TooltipValues => new object[] { Translations.Number(revealRange.Value) };

    /// <inheritdoc/>
    protected override string CopyMeadFrom => "MeadStaminaLingering";

    /// <inheritdoc/>
    protected override string CopyMeadBaseFrom => "MeadBaseStaminaLingering";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Honey", Amount = 10, Recover = false },
        new() { Item = WildMonsters.HuldraBark, Amount = 2, Recover = false },
        new() { Item = "Sap", Amount = 3, Recover = false }
    };

    /// <inheritdoc/>
    protected override Color Tint => new(0.62f, 0.75f, 0.68f);

    /// <inheritdoc/>
    protected override float DurationSeconds => 90f;

    /// <inheritdoc/>
    protected override SE_Stats CreateEffectInstance()
    {
        return ScriptableObject.CreateInstance<HuldraVeilEffect>();
    }

    /// <inheritdoc/>
    protected override void ConfigureEffect(SE_Stats effect)
    {
        effect.m_noiseModifier = -1f;
        effect.m_stealthModifier = -1f;
    }
}
