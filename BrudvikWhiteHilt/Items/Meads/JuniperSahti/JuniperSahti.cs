using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Meads.JuniperSahti;

/// <summary>
/// A juniper ale that halves spirit damage: the smoke of juniper keeps the dead away.
/// </summary>
public class JuniperSahti : WhiteHiltMeadBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public JuniperSahti(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltJuniperSahti";

    /// <inheritdoc/>
    protected override string FullName => "Juniper Sahti";

    /// <inheritdoc/>
    protected override string Description => "A cloudy, resinous ale strained through juniper twigs. Ghosts and wraiths find little to grip in someone who drank it.";

    /// <inheritdoc/>
    protected override string EffectTooltip => "Spirit damage taken is halved";

    /// <inheritdoc/>
    protected override string CopyMeadFrom => "MeadFrostResist";

    /// <inheritdoc/>
    protected override string CopyMeadBaseFrom => "MeadBaseFrostResist";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = Foraging.Juniper.Juniper.PrefabName, Amount = 10, Recover = false },
        new() { Item = "Honey", Amount = 8, Recover = false },
        new() { Item = Foraging.Crowberries.Crowberries.PrefabName, Amount = 3, Recover = false }
    };

    /// <inheritdoc/>
    protected override Color Tint => new(0.9f, 0.85f, 0.55f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override void ConfigureEffect(SE_Stats effect)
    {
        effect.m_mods.Clear();
        effect.m_mods.Add(new HitData.DamageModPair { m_type = HitData.DamageType.Spirit, m_modifier = HitData.DamageModifier.Resistant });
    }
}
