using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltAtgeir;

/// <summary>
/// This class represents the White Hilt Atgeir.
/// </summary>
public class WhiteHiltAtgeir : WhiteHiltWeaponBase
{
    /// <summary>
    /// Constructor for the WhiteHiltAtgeir class.
    /// </summary>
    /// <param name="instance"></param>
    public WhiteHiltAtgeir(ItemManager instance) : base(instance) { }

    /// <summary>
    /// The base name of the atgeir.
    /// </summary>
    protected override string BaseName => "WhiteHiltAtgeir";

    /// <summary>
    /// The full name of the atgeir.
    /// </summary>
    protected override string FullName => "White Hilt Atgeir";

    /// <summary>
    /// The description of the atgeir.
    /// </summary>
    protected override string Description => "The Indestructible Atgeir of Dyrnwyn";

    /// <summary>
    /// The name of the item to copy from.
    /// </summary>
    protected override string CopyFrom => "AtgeirBlackmetal";

    /// <summary>
    /// Indicates whether the White Hilt Atgeir is enabled.
    /// </summary>
    public override bool Enabled => true;

    /// <summary>
    /// The requirements for crafting the White Hilt Atgeir.
    /// </summary>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "BlackMetal", Amount = 25, Recover = false },
        new() { Item = "LinenThread", Amount = 10, Recover = false },
        new() { Item = "AtgeirBronze", Amount = 1, Recover = false }
    };
}
