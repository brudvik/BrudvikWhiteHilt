using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltBattleaxe;

/// <summary>
/// This class represents the White Hilt Battleaxe.
/// </summary>
public class WhiteHiltBattleaxe : WhiteHiltWeaponBase
{
    /// <summary>
    /// Constructor for the WhiteHiltBattleaxe class.
    /// </summary>
    /// <param name="instance"></param>
    public WhiteHiltBattleaxe(ItemManager instance) : base(instance) { }

    /// <summary>
    /// The base name of the battleaxe.
    /// </summary>
    protected override string BaseName => "WhiteHiltBattleaxe";

    /// <summary>
    /// The full name of the battleaxe.
    /// </summary>
    protected override string FullName => "White Hilt Battleaxe";

    /// <summary>
    /// The description of the battleaxe.
    /// </summary>
    protected override string Description => "The Indestructible Battleaxe of Dyrnwyn";

    /// <summary>
    /// The name of the item to copy from.
    /// </summary>
    protected override string CopyFrom => "AxeBlackMetal";

    /// <summary>
    /// Indicates whether the White Hilt Battleaxe is enabled.
    /// </summary>
    public override bool Enabled => false;

    /// <summary>
    /// The requirements for crafting the White Hilt Battleaxe.
    /// </summary>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "BlackMetal", Amount = 30, Recover = false },
        new() { Item = "LinenThread", Amount = 10, Recover = false },
        new() { Item = "AxeIron", Amount = 1, Recover = false }
    };
}
