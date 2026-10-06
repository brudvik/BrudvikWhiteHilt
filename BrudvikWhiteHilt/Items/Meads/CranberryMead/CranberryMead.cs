using Jotunn.Configs;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Meads.CranberryMead;

/// <summary>
/// A mead that gives poison resistance.
/// </summary>
public class CranberryMead : WhiteHiltMeadBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public CranberryMead(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltCranberryMead";

    /// <inheritdoc/>
    protected override string FullName => "Cranberry Mead";

    /// <inheritdoc/>
    protected override string Description => "A sour, dark red mead of bog cranberries. The swamp's poisons bite less after a cup.";

    /// <inheritdoc/>
    protected override string EffectTooltip => "Resistant to poison";

    /// <inheritdoc/>
    protected override string CopyMeadFrom => "MeadPoisonResist";

    /// <inheritdoc/>
    protected override string CopyMeadBaseFrom => "MeadBasePoisonResist";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Honey", Amount = 10, Recover = false },
        new() { Item = Foraging.Cranberries.Cranberries.PrefabName, Amount = 10, Recover = false },
        new() { Item = Foraging.Lingonberries.Lingonberries.PrefabName, Amount = 3, Recover = false }
    };

    /// <inheritdoc/>
    protected override Color Tint => new(0.75f, 0.4f, 0.45f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override void ConfigureEffect(SE_Stats effect)
    {
        effect.m_mods = new List<HitData.DamageModPair>
        {
            new() { m_type = HitData.DamageType.Poison, m_modifier = HitData.DamageModifier.Resistant }
        };
    }
}
