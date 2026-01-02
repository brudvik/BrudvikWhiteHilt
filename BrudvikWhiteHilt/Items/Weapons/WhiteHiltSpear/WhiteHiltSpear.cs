using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltSpear;

/// <summary>
/// This class represents the White Hilt Spear.
/// </summary>
public class WhiteHiltSpear : WhiteHiltWeaponBase
{
    /// <summary>
    /// Constructor for the WhiteHiltSpear class.
    /// </summary>
    /// <param name="instance"></param>
    public WhiteHiltSpear(ItemManager instance) : base(instance) { }

    /// <summary>
    /// The base name of the spear.
    /// </summary>
    protected override string BaseName => "WhiteHiltSpear";

    /// <summary>
    /// The full name of the spear.
    /// </summary>
    protected override string FullName => "White Hilt Spear";

    /// <summary>
    /// The description of the spear.
    /// </summary>
    protected override string Description => "The Indestructible Spear of Dyrnwyn";

    /// <summary>
    /// The name of the item to copy from.
    /// </summary>
    protected override string CopyFrom => "SpearCarapace";

    /// <summary>
    /// Indicates whether the White Hilt Spear is enabled.
    /// </summary>
    public override bool Enabled => true;

    /// <summary>
    /// The requirements for crafting the White Hilt Spear.
    /// </summary>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Chitin", Amount = 20, Recover = false },
        new() { Item = "FineWood", Amount = 10, Recover = false },
        new() { Item = "SpearFlint", Amount = 1, Recover = false }
    };
}
