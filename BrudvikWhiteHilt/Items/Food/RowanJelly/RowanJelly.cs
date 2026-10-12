using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Food.RowanJelly;

/// <summary>
/// A stamina-leaning Ashlands jelly of rowanberries, vineberries and honey.
/// </summary>
public class RowanJelly : WhiteHiltFoodBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public RowanJelly(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltRowanJelly";

    /// <inheritdoc/>
    protected override string FullName => "Rowanberry Jelly";

    /// <inheritdoc/>
    protected override string Description => "A bitter-sweet jelly of rowanberries set with honey. Runners carry it in their packs.";

    /// <inheritdoc/>
    protected override string CopyFrom => "SpicyMarmalade";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = Foraging.Rowanberries.Rowanberries.PrefabName, Amount = 4, Recover = false },
        new() { Item = "Vineberry", Amount = 2, Recover = false },
        new() { Item = "Honey", Amount = 2, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Health => 34f;

    /// <inheritdoc/>
    protected override float Stamina => 98f;

    /// <inheritdoc/>
    protected override Color Tint => new(1f, 0.7f, 0.45f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override float DurationSeconds => 1800f;

    /// <inheritdoc/>
    protected override float Regen => 4f;

    /// <inheritdoc/>
    protected override int MinStationLevel => 4;
}
