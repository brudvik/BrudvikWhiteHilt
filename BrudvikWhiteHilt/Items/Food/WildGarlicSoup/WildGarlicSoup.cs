using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Food.WildGarlicSoup;

/// <summary>
/// A stamina-leaning Meadows soup.
/// </summary>
public class WildGarlicSoup : WhiteHiltFoodBase
{
    /// <summary>
    /// Constructor for the WildGarlicSoup class.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public WildGarlicSoup(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltWildGarlicSoup";

    /// <inheritdoc/>
    protected override string FullName => "Wild Garlic Soup";

    /// <inheritdoc/>
    protected override string Description => "A light green soup of wild garlic, chanterelle and raspberries. Keeps your breath going on long runs.";

    /// <inheritdoc/>
    protected override string CopyFrom => "TurnipStew";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = Foraging.WildGarlic.WildGarlic.PrefabName, Amount = 2, Recover = false },
        new() { Item = Foraging.Chanterelle.Chanterelle.PrefabName, Amount = 1, Recover = false },
        new() { Item = "Raspberry", Amount = 2, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Health => 18f;

    /// <inheritdoc/>
    protected override float Stamina => 32f;

    /// <inheritdoc/>
    protected override Color Tint => new(0.7f, 1f, 0.55f);

    /// <inheritdoc/>
    public override bool Enabled => true;
}
