using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Armors.WhiteHiltGreaves;

/// <summary>
/// This class defines the White Hilt Greaves item.
/// </summary>
public class WhiteHiltGreaves : WhiteHiltArmorBase
{
    public WhiteHiltGreaves(ItemManager instance) : base(instance) { }

    /// <summary>
    /// The base name of the White Hilt Greaves item.
    /// </summary>
    protected override string BaseName => "WhiteHiltGreaves";

    /// <summary>
    /// The full name of the White Hilt Greaves item.
    /// </summary>
    protected override string FullName => "White Hilt Greaves";

    /// <summary>
    /// The description of the White Hilt Greaves item.
    /// </summary>
    protected override string Description => "The Indestructible Greaves of Dyrnwyn";

    /// <summary>
    /// The name of the item to copy from.
    /// </summary>
    protected override string CopyFrom => "ArmorFlametalLegs";

    /// <summary>
    /// The requirements for crafting the White Hilt Greaves.
    /// </summary>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Flametal", Amount = 20, Recover = false },
        new() { Item = "LinenThread", Amount = 15, Recover = false },
        new() { Item = "ArmorIronLegs", Amount = 1, Recover = false },
    };

    /// <summary>
    /// Indicates whether the armor is enabled.
    /// </summary>
    public override bool Enabled => false;
}
