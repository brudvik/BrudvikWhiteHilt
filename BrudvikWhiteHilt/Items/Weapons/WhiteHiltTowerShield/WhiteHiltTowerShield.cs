using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltTowerShield;

/// <summary>
/// This class represents the White Hilt Tower Shield.
/// </summary>
public class WhiteHiltTowerShield : WhiteHiltWeaponBase
{
    /// <summary>
    /// Constructor for the WhiteHiltTowerShield class.
    /// </summary>
    /// <param name="instance"></param>
    public WhiteHiltTowerShield(ItemManager instance) : base(instance) { }

    /// <summary>
    /// The base name of the tower shield.
    /// </summary>
    protected override string BaseName => "WhiteHiltTowerShield";

    /// <summary>
    /// The full name of the tower shield.
    /// </summary>
    protected override string FullName => "White Hilt Tower Shield";

    /// <summary>
    /// The description of the tower shield.
    /// </summary>
    protected override string Description => "The Indestructible Tower Shield of Dyrnwyn";

    /// <summary>
    /// The name of the item to copy from.
    /// </summary>
    protected override string CopyFrom => "ShieldBlackmetalTower";

    /// <summary>
    /// Indicates whether the White Hilt Tower Shield is enabled.
    /// </summary>
    public override bool Enabled => false;

    /// <summary>
    /// The requirements for crafting the White Hilt Tower Shield.
    /// </summary>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "BlackMetal", Amount = 30, Recover = false },
        new() { Item = "Chain", Amount = 10, Recover = false },
        new() { Item = "ShieldIronTower", Amount = 1, Recover = false }
    };
}
