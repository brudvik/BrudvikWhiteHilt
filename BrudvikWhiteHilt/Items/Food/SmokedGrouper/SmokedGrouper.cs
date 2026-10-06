using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Food.SmokedGrouper;

/// <summary>
/// A health-leaning smoked Plains dish whose buff makes blocking cheaper.
/// </summary>
public class SmokedGrouper : WhiteHiltFoodBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public SmokedGrouper(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltSmokedGrouper";

    /// <inheritdoc/>
    protected override string FullName => "Smoked Grouper with Yarrow";

    /// <inheritdoc/>
    protected override string Description => "A thick grouper fillet smoked over juniper and packed with bitter yarrow. A shield-bearer's meal.";

    /// <inheritdoc/>
    protected override string CopyFrom => "FishCooked";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Fish7", Amount = 1, Recover = false },
        new() { Item = Foraging.Yarrow.Yarrow.PrefabName, Amount = 2, Recover = false },
        new() { Item = Foraging.Juniper.Juniper.PrefabName, Amount = 1, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Health => 66f;

    /// <inheritdoc/>
    protected override float Stamina => 34f;

    /// <inheritdoc/>
    protected override Color Tint => new(0.8f, 0.6f, 0.45f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override float DurationSeconds => 2700f;

    /// <inheritdoc/>
    protected override float Regen => 4f;

    /// <inheritdoc/>
    protected override int MinStationLevel => 3;

    /// <inheritdoc/>
    protected override string BuffTooltip => "Blocking uses 25% less stamina";

    /// <inheritdoc/>
    protected override void ConfigureBuff(SE_Stats effect)
    {
        effect.m_blockStaminaUseModifier = -0.25f;
    }
}
