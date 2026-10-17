using Jotunn.Configs;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Meads.WildThymeTea;

/// <summary>
/// A brew of wild thyme from the lava fields that gives fire resistance.
/// </summary>
public class WildThymeTea : WhiteHiltMeadBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public WildThymeTea(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltWildThymeTea";

    /// <inheritdoc/>
    protected override string FullName => "Wild Thyme Tea";

    /// <inheritdoc/>
    protected override string Description => "A strong, fragrant brew of wild thyme picked on the lava. What grows in the fire teaches the body to bear it.";

    /// <inheritdoc/>
    protected override string EffectTooltip => "Resistant to fire";

    /// <inheritdoc/>
    protected override string CopyMeadFrom => "MeadHealthMinor";

    /// <inheritdoc/>
    protected override string CopyMeadBaseFrom => "MeadBaseHealthMinor";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Honey", Amount = 5, Recover = false },
        new() { Item = Foraging.WildThyme.WildThyme.PrefabName, Amount = 8, Recover = false },
        new() { Item = "Fiddleheadfern", Amount = 1, Recover = false }
    };

    /// <inheritdoc/>
    protected override Color Tint => new(0.8f, 0.5f, 0.75f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override void ConfigureEffect(SE_Stats effect)
    {
        effect.m_mods = new List<HitData.DamageModPair>
        {
            new() { m_type = HitData.DamageType.Fire, m_modifier = HitData.DamageModifier.Resistant }
        };
    }
}
