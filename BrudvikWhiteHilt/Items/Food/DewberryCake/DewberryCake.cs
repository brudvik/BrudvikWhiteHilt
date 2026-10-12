using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Food.DewberryCake;

/// <summary>
/// A stamina-leaning Mistlands dish: a heavy barley cake dark with dewberries.
/// </summary>
public class DewberryCake : WhiteHiltFoodBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public DewberryCake(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltDewberryCake";

    /// <inheritdoc/>
    protected override string FullName => "Dewberry Cake";

    /// <inheritdoc/>
    protected override string Description => "A heavy barley cake baked with egg and honey and dark with dewberries.";

    /// <inheritdoc/>
    protected override string CopyFrom => "LoxPie";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = Foraging.Dewberries.Dewberries.PrefabName, Amount = 4, Recover = false },
        new() { Item = "BarleyFlour", Amount = 2, Recover = false },
        new() { Item = "ChickenEgg", Amount = 1, Recover = false },
        new() { Item = "Honey", Amount = 1, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Health => 30f;

    /// <inheritdoc/>
    protected override float Stamina => 82f;

    /// <inheritdoc/>
    protected override Color Tint => new(0.6f, 0.55f, 0.9f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override float DurationSeconds => 1800f;

    /// <inheritdoc/>
    protected override float Regen => 3f;

    /// <inheritdoc/>
    protected override int MinStationLevel => 4;
}
