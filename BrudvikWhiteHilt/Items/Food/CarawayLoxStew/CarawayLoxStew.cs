using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Food.CarawayLoxStew;

/// <summary>
/// A health-leaning Plains stew of lox meat spiced with caraway.
/// </summary>
public class CarawayLoxStew : WhiteHiltFoodBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public CarawayLoxStew(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltCarawayLoxStew";

    /// <inheritdoc/>
    protected override string FullName => "Caraway Lox Stew";

    /// <inheritdoc/>
    protected override string Description => "Chunks of lox simmered with onion, turnip and a fistful of caraway. Heavy, warm and smelling of anise.";

    /// <inheritdoc/>
    protected override string CopyFrom => "SerpentStew";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "LoxMeat", Amount = 1, Recover = false },
        new() { Item = Foraging.Caraway.Caraway.PrefabName, Amount = 2, Recover = false },
        new() { Item = "Onion", Amount = 1, Recover = false },
        new() { Item = "Turnip", Amount = 1, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Health => 64f;

    /// <inheritdoc/>
    protected override float Stamina => 36f;

    /// <inheritdoc/>
    protected override Color Tint => new(0.85f, 0.65f, 0.5f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override float DurationSeconds => 2400f;

    /// <inheritdoc/>
    protected override float Regen => 4f;

    /// <inheritdoc/>
    protected override int MinStationLevel => 2;
}
