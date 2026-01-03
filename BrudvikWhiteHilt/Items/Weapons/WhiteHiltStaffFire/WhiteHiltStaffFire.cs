using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltStaffFire;

/// <summary>
/// This class represents the White Hilt Staff of Fire.
/// </summary>
public class WhiteHiltStaffFire : WhiteHiltWeaponBase
{
    /// <summary>
    /// Constructor for the WhiteHiltStaffFire class.
    /// </summary>
    /// <param name="instance"></param>
    public WhiteHiltStaffFire(ItemManager instance) : base(instance) { }

    /// <summary>
    /// The base name of the staff.
    /// </summary>
    protected override string BaseName => "WhiteHiltStaffFire";

    /// <summary>
    /// The full name of the staff.
    /// </summary>
    protected override string FullName => "White Hilt Staff of Fire";

    /// <summary>
    /// The description of the staff.
    /// </summary>
    protected override string Description => "The Indestructible Staff of Fire of Dyrnwyn";

    /// <summary>
    /// The name of the item to copy from.
    /// </summary>
    protected override string CopyFrom => "StaffFireball";

    /// <summary>
    /// Indicates whether the White Hilt Staff of Fire is enabled.
    /// </summary>
    public override bool Enabled => true;

    /// <summary>
    /// The requirements for crafting the White Hilt Staff of Fire.
    /// </summary>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "SurtlingCore", Amount = 10, Recover = false },
        new() { Item = "ElderBark", Amount = 10, Recover = false },
        new() { Item = "Guck", Amount = 5, Recover = false }
    };
}
