using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Ammunition.WhiteHiltArrows;

/// <summary>
/// The White Hilt Arrows, cloned from the vanilla <c>ArrowIron</c>.
/// </summary>
public class WhiteHiltArrows : WhiteHiltAmmunitionBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltArrows(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltArrows";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Arrows";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Arrows of Dyrnwyn. Enhanced with fire and spirit damage.";

    /// <inheritdoc/>
    protected override string CopyFrom => "ArrowIron";

    /// <inheritdoc/>
    protected override int CraftAmount => 200;

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Iron", Amount = 2, Recover = false },
        new() { Item = "Feathers", Amount = 5, Recover = false },
        new() { Item = "Wood", Amount = 10, Recover = false }
    };
}
