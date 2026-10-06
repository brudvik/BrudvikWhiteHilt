using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Food.SmokedWolfJerky;

/// <summary>
/// A stamina-leaning smoked dish that makes running cheaper.
/// </summary>
public class SmokedWolfJerky : WhiteHiltFoodBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public SmokedWolfJerky(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltSmokedWolfJerky";

    /// <inheritdoc/>
    protected override string FullName => "Smoked Wolf Jerky";

    /// <inheritdoc/>
    protected override string Description => "Thin strips of wolf meat rubbed with wild garlic and roseroot, then smoked hard. Trail food for long treks.";

    /// <inheritdoc/>
    protected override string CopyFrom => "CookedWolfMeat";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "WolfMeat", Amount = 1, Recover = false },
        new() { Item = Foraging.WildGarlic.WildGarlic.PrefabName, Amount = 1, Recover = false },
        new() { Item = Foraging.Roseroot.Roseroot.PrefabName, Amount = 1, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Health => 34f;

    /// <inheritdoc/>
    protected override float Stamina => 58f;

    /// <inheritdoc/>
    protected override Color Tint => new(0.7f, 0.5f, 0.4f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override float DurationSeconds => 2400f;

    /// <inheritdoc/>
    protected override float Regen => 3f;

    /// <inheritdoc/>
    protected override int MinStationLevel => 3;

    /// <inheritdoc/>
    protected override string BuffTooltip => "Running and jumping use 20% less stamina";

    /// <inheritdoc/>
    protected override void ConfigureBuff(SE_Stats effect)
    {
        effect.m_runStaminaDrainModifier = -0.2f;
        effect.m_jumpStaminaUseModifier = -0.2f;
    }
}
