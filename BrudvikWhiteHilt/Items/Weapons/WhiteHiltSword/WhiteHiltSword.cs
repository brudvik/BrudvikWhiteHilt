using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltSword;

/// <summary>
/// Class for the White Hilt Sword item.
/// </summary>
public class WhiteHiltSword : WhiteHiltWeaponBase
{
    /// <summary>
    /// Constructor for the WhiteHiltSword class.
    /// </summary>
    /// <param name="instance"></param>
    public WhiteHiltSword(ItemManager instance) : base(instance) { }

    /// <summary>
    /// The base name of the sword.
    /// </summary>
    protected override string BaseName => "WhiteHiltSword";

    /// <summary>
    /// The full name of the sword.
    /// </summary>
    protected override string FullName => "White Hilt Sword";

    /// <summary>
    /// The description of the sword.
    /// </summary>
    protected override string Description => "The Indestructible Sword of Dyrnwyn";

    /// <summary>
    /// The name of the item to copy from.
    /// </summary>
    protected override string CopyFrom => "SwordDyrnwyn";

    /// <summary>
    /// Indicates whether the White Hilt Bow is enabled.
    /// </summary>
    public override bool Enabled => true;

    /// <summary>
    /// The requirements for crafting the White Hilt Sword.
    /// </summary>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Bronze", Amount = 20, Recover = false },
        new() { Item = "SurtlingCore", Amount = 5, Recover = false },
        new() { Item = "SwordBronze", Amount = 1, Recover = false }
    };
}
