using BrudvikWhiteHilt.Kraken;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Food.OctopusStew;

/// <summary>
/// A balanced stew of octopus, turnip and wild garlic.
/// </summary>
public class OctopusStew : WhiteHiltFoodBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public OctopusStew(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltOctopusStew";

    /// <inheritdoc/>
    protected override string FullName => "Octopus Stew";

    /// <inheritdoc/>
    protected override string Description => "Octopus simmered soft with turnip and wild garlic. Tastes of salt spray and deep water.";

    /// <inheritdoc/>
    protected override string CopyFrom => "FishAndBread";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = KrakenRegistry.OctopusName, Amount = 1, Recover = false },
        new() { Item = "Turnip", Amount = 2, Recover = false },
        new() { Item = Foraging.WildGarlic.WildGarlic.PrefabName, Amount = 1, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Health => 50f;

    /// <inheritdoc/>
    protected override float Stamina => 40f;

    /// <inheritdoc/>
    protected override Color Tint => new(0.95f, 0.6f, 0.55f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override float DurationSeconds => 2100f;

    /// <inheritdoc/>
    protected override float Regen => 3f;

    /// <inheritdoc/>
    protected override int MinStationLevel => 2;
}
