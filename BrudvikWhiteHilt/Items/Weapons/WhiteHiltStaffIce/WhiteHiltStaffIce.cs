using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltStaffIce;

/// <summary>
/// This class represents the White Hilt Staff of Ice.
/// </summary>
public class WhiteHiltStaffIce : WhiteHiltWeaponBase
{
    /// <summary>
    /// Constructor for the WhiteHiltStaffIce class.
    /// </summary>
    /// <param name="instance"></param>
    public WhiteHiltStaffIce(ItemManager instance) : base(instance) { }

    /// <summary>
    /// The base name of the staff.
    /// </summary>
    protected override string BaseName => "WhiteHiltStaffIce";

    /// <summary>
    /// The full name of the staff.
    /// </summary>
    protected override string FullName => "White Hilt Staff of Ice";

    /// <summary>
    /// The description of the staff.
    /// </summary>
    protected override string Description => "The Indestructible Staff of Ice of Dyrnwyn";

    /// <summary>
    /// The name of the item to copy from.
    /// </summary>
    protected override string CopyFrom => "StaffIceShards";

    /// <summary>
    /// Indicates whether the White Hilt Staff of Ice is enabled.
    /// </summary>
    public override bool Enabled => true;

    /// <summary>
    /// The requirements for crafting the White Hilt Staff of Ice.
    /// </summary>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "FreezeGland", Amount = 10, Recover = false },
        new() { Item = "YggdrasilWood", Amount = 10, Recover = false },
        new() { Item = "Eitr", Amount = 5, Recover = false }
    };
}
