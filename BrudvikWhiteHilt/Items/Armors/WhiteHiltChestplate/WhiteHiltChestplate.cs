using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Armors.WhiteHiltChestplate;

/// <summary>
/// The White Hilt Chestplate, cloned from the vanilla <c>ArmorIronChest</c>.
/// </summary>
public class WhiteHiltChestplate : WhiteHiltArmorBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltChestplate(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltChestplate";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Chestplate";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Chestplate of Dyrnwyn";

    /// <inheritdoc/>
    protected override string CopyFrom => "ArmorIronChest";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Iron", Amount = 20, Recover = false },
        new() { Item = "DeerHide", Amount = 10, Recover = false },
        new() { Item = "ArmorBronzeChest", Amount = 1, Recover = false },
    };

    /// <inheritdoc/>
    public override bool Enabled => true;
}
