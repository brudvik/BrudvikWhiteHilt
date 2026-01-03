using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltStaffLightning;

/// <summary>
/// This class represents the White Hilt Staff of Lightning.
/// </summary>
public class WhiteHiltStaffLightning : WhiteHiltWeaponBase
{
    /// <summary>
    /// Constructor for the WhiteHiltStaffLightning class.
    /// </summary>
    /// <param name="instance"></param>
    public WhiteHiltStaffLightning(ItemManager instance) : base(instance) { }

    /// <summary>
    /// The base name of the staff.
    /// </summary>
    protected override string BaseName => "WhiteHiltStaffLightning";

    /// <summary>
    /// The full name of the staff.
    /// </summary>
    protected override string FullName => "White Hilt Staff of Lightning";

    /// <summary>
    /// The description of the staff.
    /// </summary>
    protected override string Description => "The Indestructible Staff of Lightning of Dyrnwyn";

    /// <summary>
    /// The name of the item to copy from.
    /// </summary>
    protected override string CopyFrom => "StaffShield";

    /// <summary>
    /// Indicates whether the White Hilt Staff of Lightning is enabled.
    /// </summary>
    public override bool Enabled => true;

    /// <summary>
    /// The requirements for crafting the White Hilt Staff of Lightning.
    /// </summary>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Thunderstone", Amount = 5, Recover = false },
        new() { Item = "ElderBark", Amount = 10, Recover = false },
        new() { Item = "Guck", Amount = 5, Recover = false }
    };
}
