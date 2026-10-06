using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Food.BogFishStew;

/// <summary>
/// A health-leaning Swamp dish of trollfish and cattail root.
/// </summary>
public class BogFishStew : WhiteHiltFoodBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public BogFishStew(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltBogFishStew";

    /// <inheritdoc/>
    protected override string FullName => "Bog Fish Stew";

    /// <inheritdoc/>
    protected override string Description => "Trollfish boiled with cattail root and wild garlic into a thick grey stew. More filling than it looks.";

    /// <inheritdoc/>
    protected override string CopyFrom => "TurnipStew";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Fish5", Amount = 1, Recover = false },
        new() { Item = Foraging.Cattail.Cattail.PrefabName, Amount = 2, Recover = false },
        new() { Item = Foraging.WildGarlic.WildGarlic.PrefabName, Amount = 1, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Health => 50f;

    /// <inheritdoc/>
    protected override float Stamina => 30f;

    /// <inheritdoc/>
    protected override Color Tint => new(0.75f, 0.72f, 0.62f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override float DurationSeconds => 1950f;

    /// <inheritdoc/>
    protected override float Regen => 3f;
}
