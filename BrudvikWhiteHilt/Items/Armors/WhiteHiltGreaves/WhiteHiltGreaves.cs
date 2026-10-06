using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Armors.WhiteHiltGreaves;

/// <summary>
/// The White Hilt Greaves, cloned from the vanilla <c>ArmorIronLegs</c>.
/// </summary>
public class WhiteHiltGreaves : WhiteHiltArmorBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltGreaves(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltGreaves";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Greaves";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Greaves of Dyrnwyn";

    /// <inheritdoc/>
    protected override string CopyFrom => "ArmorIronLegs";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Iron", Amount = 20, Recover = false },
        new() { Item = "DeerHide", Amount = 10, Recover = false },
        new() { Item = "ArmorBronzeLegs", Amount = 1, Recover = false },
    };

    /// <inheritdoc/>
    public override bool Enabled => true;
}
