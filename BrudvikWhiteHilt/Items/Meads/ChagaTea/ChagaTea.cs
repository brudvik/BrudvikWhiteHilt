using Jotunn.Configs;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Meads.ChagaTea;

/// <summary>
/// A black, bitter tea of chaga and lungwort lichen that toughens the skin against pierce damage.
/// </summary>
public class ChagaTea : WhiteHiltMeadBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public ChagaTea(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltChagaTea";

    /// <inheritdoc/>
    protected override string FullName => "Chaga Tea";

    /// <inheritdoc/>
    protected override string Description => "A black, bitter tea of chaga and lungwort, steeped for a day. Stings and bites find a tougher hide.";

    /// <inheritdoc/>
    protected override string EffectTooltip => "Pierce damage taken is a quarter less";

    /// <inheritdoc/>
    protected override string CopyMeadFrom => "MeadTasty";

    /// <inheritdoc/>
    protected override string CopyMeadBaseFrom => "MeadBaseTasty";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Honey", Amount = 5, Recover = false },
        new() { Item = Foraging.Chaga.Chaga.PrefabName, Amount = 3, Recover = false },
        new() { Item = Foraging.LungwortLichen.LungwortLichen.PrefabName, Amount = 4, Recover = false }
    };

    /// <inheritdoc/>
    protected override Color Tint => new(0.45f, 0.35f, 0.3f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override void ConfigureEffect(SE_Stats effect)
    {
        effect.m_mods = new List<HitData.DamageModPair>
        {
            new() { m_type = HitData.DamageType.Pierce, m_modifier = HitData.DamageModifier.SlightlyResistant }
        };
    }
}
