using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Food.MountainStew;

/// <summary>
/// A health-leaning Mountains stew.
/// </summary>
public class MountainStew : WhiteHiltFoodBase
{
    /// <summary>
    /// Constructor for the MountainStew class.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public MountainStew(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltMountainStew";

    /// <inheritdoc/>
    protected override string FullName => "Mountain Stew";

    /// <inheritdoc/>
    protected override string Description => "Wolf meat slow-cooked with crowberries and porcini. Warms you through the Mountain cold.";

    /// <inheritdoc/>
    protected override string CopyFrom => "SerpentStew";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = Foraging.Crowberries.Crowberries.PrefabName, Amount = 3, Recover = false },
        new() { Item = Foraging.Porcini.Porcini.PrefabName, Amount = 1, Recover = false },
        new() { Item = "WolfMeat", Amount = 1, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Health => 52f;

    /// <inheritdoc/>
    protected override float Stamina => 34f;

    /// <inheritdoc/>
    protected override Color Tint => new(0.6f, 0.5f, 0.7f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override float DurationSeconds => 2100f;

    /// <inheritdoc/>
    protected override float Regen => 4f;

    /// <inheritdoc/>
    protected override int MinStationLevel => 2;
}
