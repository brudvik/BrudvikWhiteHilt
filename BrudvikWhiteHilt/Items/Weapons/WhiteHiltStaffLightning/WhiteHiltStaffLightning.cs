using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

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
    /// Dark scepter from Weapon Set by Asylum Nox, with a white grip wrap.
    /// </summary>
    protected override string ModelName => "whstaff";

    /// <summary>
    /// Lights a green flame on the scepter head.
    /// </summary>
    /// <param name="model">The staff model under the attach child.</param>
    protected override void OnModelApplied(GameObject model)
    {
        StaffFlame.Apply(model, new Color(0.3f, 1f, 0.35f));
    }

    /// <summary>
    /// Indicates whether the White Hilt Staff of Lightning is enabled.
    /// </summary>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Plains;

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
