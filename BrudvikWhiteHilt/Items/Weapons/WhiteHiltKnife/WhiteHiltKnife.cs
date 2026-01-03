using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltKnife;

/// <summary>
/// This class represents the White Hilt Knife.
/// </summary>
public class WhiteHiltKnife : WhiteHiltWeaponBase
{
    /// <summary>
    /// Constructor for the WhiteHiltKnife class.
    /// </summary>
    /// <param name="instance"></param>
    public WhiteHiltKnife(ItemManager instance) : base(instance) { }

    /// <summary>
    /// The base name of the knife.
    /// </summary>
    protected override string BaseName => "WhiteHiltKnife";

    /// <summary>
    /// The full name of the knife.
    /// </summary>
    protected override string FullName => "White Hilt Knife";

    /// <summary>
    /// The description of the knife.
    /// </summary>
    protected override string Description => "The Indestructible Knife of Dyrnwyn";

    /// <summary>
    /// The name of the item to copy from.
    /// </summary>
    protected override string CopyFrom => "KnifeChitin";

    /// <summary>
    /// Indicates whether the White Hilt Knife is enabled.
    /// </summary>
    public override bool Enabled => true;

    /// <summary>
    /// The requirements for crafting the White Hilt Knife.
    /// </summary>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Iron", Amount = 10, Recover = false },
        new() { Item = "LeatherScraps", Amount = 5, Recover = false },
        new() { Item = "KnifeFlint", Amount = 1, Recover = false }
    };
}
