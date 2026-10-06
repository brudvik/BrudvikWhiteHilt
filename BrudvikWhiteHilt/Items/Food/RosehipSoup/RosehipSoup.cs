using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Food.RosehipSoup;

/// <summary>
/// A stamina-leaning Plains soup of rosehips.
/// </summary>
public class RosehipSoup : WhiteHiltFoodBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public RosehipSoup(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltRosehipSoup";

    /// <inheritdoc/>
    protected override string FullName => "Rosehip Soup";

    /// <inheritdoc/>
    protected override string Description => "A sweet, glowing red soup of rosehips thickened with barley flour. Summer in a bowl, on the coldest night.";

    /// <inheritdoc/>
    protected override string CopyFrom => "CarrotSoup";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = Foraging.Rosehips.Rosehips.PrefabName, Amount = 3, Recover = false },
        new() { Item = "Honey", Amount = 1, Recover = false },
        new() { Item = "BarleyFlour", Amount = 1, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Health => 32f;

    /// <inheritdoc/>
    protected override float Stamina => 64f;

    /// <inheritdoc/>
    protected override Color Tint => new(1f, 0.45f, 0.3f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override float DurationSeconds => 2400f;

    /// <inheritdoc/>
    protected override float Regen => 3f;

    /// <inheritdoc/>
    protected override int MinStationLevel => 2;
}
