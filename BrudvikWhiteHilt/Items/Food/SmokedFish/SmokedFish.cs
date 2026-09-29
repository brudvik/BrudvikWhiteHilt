using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Food.SmokedFish;

/// <summary>
/// A health-leaning smoked dish that makes you a better swimmer.
/// </summary>
public class SmokedFish : WhiteHiltFoodBase
{
    /// <summary>
    /// Constructor for the SmokedFish class.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public SmokedFish(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltSmokedFish";

    /// <inheritdoc/>
    protected override string FullName => "Smoked Fish";

    /// <inheritdoc/>
    protected override string Description => "Perch cold-smoked over sweet gale and served with lingonberries. A sailor's meal.";

    /// <inheritdoc/>
    protected override string CopyFrom => "FishCooked";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Fish1", Amount = 1, Recover = false },
        new() { Item = Foraging.SweetGale.SweetGale.PrefabName, Amount = 1, Recover = false },
        new() { Item = Foraging.Lingonberries.Lingonberries.PrefabName, Amount = 2, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Health => 56f;

    /// <inheritdoc/>
    protected override float Stamina => 32f;

    /// <inheritdoc/>
    protected override Color Tint => new(0.8f, 0.6f, 0.45f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override float DurationSeconds => 2400f;

    /// <inheritdoc/>
    protected override float Regen => 4f;

    /// <inheritdoc/>
    protected override int MinStationLevel => 3;

    /// <inheritdoc/>
    protected override string BuffTooltip => "Swim 50% faster, swimming uses 50% less stamina";

    /// <inheritdoc/>
    protected override void ConfigureBuff(SE_Stats effect)
    {
        effect.m_swimSpeedModifier = 0.5f;
        effect.m_swimStaminaUseModifier = -0.5f;
    }
}
