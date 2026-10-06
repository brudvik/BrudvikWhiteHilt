using Jotunn.Configs;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Meads.LingonberryMead;

/// <summary>
/// A mead that gives frost resistance, which also keeps Cold and Freezing away.
/// </summary>
public class LingonberryMead : WhiteHiltMeadBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public LingonberryMead(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltLingonberryMead";

    /// <inheritdoc/>
    protected override string FullName => "Lingonberry Mead";

    /// <inheritdoc/>
    protected override string Description => "A deep red mead of lingonberries, bittered with sweet gale. It warms you from the inside.";

    /// <inheritdoc/>
    protected override string EffectTooltip => "Resistant to frost. Keeps the cold away";

    /// <inheritdoc/>
    protected override string CopyMeadFrom => "MeadHealthMedium";

    /// <inheritdoc/>
    protected override string CopyMeadBaseFrom => "MeadBaseHealthMedium";

    // Sweet gale is a Swamp ingredient, the same tier as the vanilla frost resistance mead.
    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Honey", Amount = 10, Recover = false },
        new() { Item = Foraging.Lingonberries.Lingonberries.PrefabName, Amount = 10, Recover = false },
        new() { Item = Foraging.SweetGale.SweetGale.PrefabName, Amount = 3, Recover = false }
    };

    /// <inheritdoc/>
    protected override Color Tint => new(0.85f, 0.55f, 0.65f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override void ConfigureEffect(SE_Stats effect)
    {
        effect.m_mods = new List<HitData.DamageModPair>
        {
            new() { m_type = HitData.DamageType.Frost, m_modifier = HitData.DamageModifier.Resistant }
        };
    }
}
