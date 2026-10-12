using Jotunn.Configs;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Meads.RowanBrew;

/// <summary>
/// A brew of rowanberries and fire moss that gives lightning resistance, as the rowan saved Thor.
/// </summary>
public class RowanBrew : WhiteHiltMeadBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public RowanBrew(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltRowanBrew";

    /// <inheritdoc/>
    protected override string FullName => "Rowan Brew";

    /// <inheritdoc/>
    protected override string Description => "A fierce brew of rowanberries and fire moss. The rowan saved Thor, and lightning finds less to bite in you.";

    /// <inheritdoc/>
    protected override string EffectTooltip => "Resistant to lightning";

    /// <inheritdoc/>
    protected override string CopyMeadFrom => "MeadStaminaLingering";

    /// <inheritdoc/>
    protected override string CopyMeadBaseFrom => "MeadBaseStaminaLingering";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Honey", Amount = 10, Recover = false },
        new() { Item = Foraging.Rowanberries.Rowanberries.PrefabName, Amount = 10, Recover = false },
        new() { Item = Foraging.FireMoss.FireMoss.PrefabName, Amount = 3, Recover = false }
    };

    /// <inheritdoc/>
    protected override Color Tint => new(1f, 0.6f, 0.35f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override void ConfigureEffect(SE_Stats effect)
    {
        effect.m_mods = new List<HitData.DamageModPair>
        {
            new() { m_type = HitData.DamageType.Lightning, m_modifier = HitData.DamageModifier.Resistant }
        };
    }
}
