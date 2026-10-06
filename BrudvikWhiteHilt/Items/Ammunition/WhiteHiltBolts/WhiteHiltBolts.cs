using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Ammunition.WhiteHiltBolts;

/// <summary>
/// The White Hilt Bolts, cloned from the vanilla <c>BoltIron</c>.
/// </summary>
public class WhiteHiltBolts : WhiteHiltAmmunitionBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltBolts(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltBolts";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Bolts";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Bolts of Dyrnwyn. Enhanced with fire and spirit damage.";

    /// <inheritdoc/>
    protected override string CopyFrom => "BoltIron";

    /// <inheritdoc/>
    protected override int CraftAmount => 200;

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Iron", Amount = 2, Recover = false },
        new() { Item = "Feathers", Amount = 3, Recover = false },
        new() { Item = "Wood", Amount = 5, Recover = false }
    };
}
