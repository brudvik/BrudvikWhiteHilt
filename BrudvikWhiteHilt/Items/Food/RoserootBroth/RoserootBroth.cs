using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Food.RoserootBroth;

/// <summary>
/// A stamina-leaning Mountains broth.
/// </summary>
public class RoserootBroth : WhiteHiltFoodBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public RoserootBroth(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltRoserootBroth";

    /// <inheritdoc/>
    protected override string FullName => "Roseroot Broth";

    /// <inheritdoc/>
    protected override string Description => "A golden broth of roseroot, lingonberries and onion. Gives you the breath for the steepest climbs.";

    /// <inheritdoc/>
    protected override string CopyFrom => "OnionSoup";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = Foraging.Roseroot.Roseroot.PrefabName, Amount = 3, Recover = false },
        new() { Item = Foraging.Lingonberries.Lingonberries.PrefabName, Amount = 1, Recover = false },
        new() { Item = "Onion", Amount = 1, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Health => 30f;

    /// <inheritdoc/>
    protected override float Stamina => 55f;

    /// <inheritdoc/>
    protected override Color Tint => new(1f, 0.9f, 0.5f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override float DurationSeconds => 2100f;

    /// <inheritdoc/>
    protected override float Regen => 4f;

    /// <inheritdoc/>
    protected override int MinStationLevel => 2;
}
