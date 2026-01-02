using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Armors.WhiteHiltChestplate;

/// <summary>
/// This class defines the White Hilt Chestplate item.
/// </summary>
public class WhiteHiltChestplate : WhiteHiltArmorBase
{
    public WhiteHiltChestplate(ItemManager instance) : base(instance) { }

    /// <summary>
    /// The base name of the White Hilt Chestplate item.
    /// </summary>
    protected override string BaseName => "WhiteHiltChestplate";

    /// <summary>
    /// The full name of the White Hilt Chestplate item.
    /// </summary>
    protected override string FullName => "White Hilt Chestplate";

    /// <summary>
    /// The description of the White Hilt Chestplate item.
    /// </summary>
    protected override string Description => "The Indestructible Chestplate of Dyrnwyn";

    /// <summary>
    /// The name of the item to copy from.
    /// </summary>
    protected override string CopyFrom => "ArmorFlametalChest";

    /// <summary>
    /// The requirements for crafting the White Hilt Chestplate.
    /// </summary>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Flametal", Amount = 20, Recover = false },
        new() { Item = "LinenThread", Amount = 20, Recover = false },
        new() { Item = "ArmorIronChest", Amount = 1, Recover = false },
    };

    /// <summary>
    /// Indicates whether the armor is enabled.
    /// </summary>
    public override bool Enabled => false;
}
