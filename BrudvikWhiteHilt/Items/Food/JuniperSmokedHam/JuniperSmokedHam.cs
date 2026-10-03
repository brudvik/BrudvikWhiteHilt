using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Food.JuniperSmokedHam;

/// <summary>
/// A health-leaning smoked Mountain dish whose buff halves frost damage.
/// </summary>
public class JuniperSmokedHam : WhiteHiltFoodBase
{
    /// <summary>
    /// Constructor for the JuniperSmokedHam class.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public JuniperSmokedHam(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltJuniperSmokedHam";

    /// <inheritdoc/>
    protected override string FullName => "Juniper-Smoked Wolf Ham";

    /// <inheritdoc/>
    protected override string Description => "Wolf haunch rubbed with juniper berries and hung in juniper smoke for days. The warmth of it stays in your bones.";

    /// <inheritdoc/>
    protected override string CopyFrom => "WolfMeatSkewer";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "WolfMeat", Amount = 2, Recover = false },
        new() { Item = Foraging.Juniper.Juniper.PrefabName, Amount = 3, Recover = false },
        new() { Item = "Onion", Amount = 1, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Health => 58f;

    /// <inheritdoc/>
    protected override float Stamina => 30f;

    /// <inheritdoc/>
    protected override Color Tint => new(0.6f, 0.45f, 0.4f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override float DurationSeconds => 2400f;

    /// <inheritdoc/>
    protected override float Regen => 3f;

    /// <inheritdoc/>
    protected override int MinStationLevel => 3;

    /// <inheritdoc/>
    protected override string BuffTooltip => "Frost damage taken is halved";

    /// <inheritdoc/>
    protected override void ConfigureBuff(SE_Stats effect)
    {
        effect.m_mods.Add(new HitData.DamageModPair { m_type = HitData.DamageType.Frost, m_modifier = HitData.DamageModifier.Resistant });
    }
}
