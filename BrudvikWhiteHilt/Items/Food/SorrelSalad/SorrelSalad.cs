using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Food.SorrelSalad;

/// <summary>
/// A stamina-leaning Mountain dish of fresh mountain sorrel.
/// </summary>
public class SorrelSalad : WhiteHiltFoodBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public SorrelSalad(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltSorrelSalad";

    /// <inheritdoc/>
    protected override string FullName => "Mountain Sorrel Salad";

    /// <inheritdoc/>
    protected override string Description => "Sour mountain sorrel tossed with onion and wild garlic. Fresh greens in a land of snow and stone.";

    /// <inheritdoc/>
    protected override string CopyFrom => "Salad";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = Foraging.MountainSorrel.MountainSorrel.PrefabName, Amount = 3, Recover = false },
        new() { Item = "Onion", Amount = 1, Recover = false },
        new() { Item = Foraging.WildGarlic.WildGarlic.PrefabName, Amount = 1, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Health => 28f;

    /// <inheritdoc/>
    protected override float Stamina => 48f;

    /// <inheritdoc/>
    protected override Color Tint => new(0.75f, 1f, 0.7f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override float DurationSeconds => 2100f;

    /// <inheritdoc/>
    protected override float Regen => 4f;

    /// <inheritdoc/>
    protected override int MinStationLevel => 2;
}
