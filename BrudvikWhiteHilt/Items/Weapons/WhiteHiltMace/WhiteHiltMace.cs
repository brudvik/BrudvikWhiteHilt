using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltMace;

/// <summary>
/// This class represents the White Hilt Mace.
/// </summary>
public class WhiteHiltMace : WhiteHiltWeaponBase
{
    /// <summary>
    /// Constructor for the WhiteHiltMace class.
    /// </summary>
    /// <param name="instance"></param>
    public WhiteHiltMace(ItemManager instance) : base(instance) { }

    /// <summary>
    /// The base name of the mace.
    /// </summary>
    protected override string BaseName => "WhiteHiltMace";

    /// <summary>
    /// The full name of the mace.
    /// </summary>
    protected override string FullName => "White Hilt Mace";

    /// <summary>
    /// The description of the mace.
    /// </summary>
    protected override string Description => "The Indestructible Mace of Dyrnwyn";

    /// <summary>
    /// The name of the item to copy from.
    /// </summary>
    protected override string CopyFrom => "MacePorcupine";

    /// <summary>
    /// Indicates whether the White Hilt Mace is enabled.
    /// </summary>
    public override bool Enabled => true;

    /// <summary>
    /// The requirements for crafting the White Hilt Mace.
    /// </summary>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Iron", Amount = 20, Recover = false },
        new() { Item = "Needle", Amount = 5, Recover = false },
        new() { Item = "MaceIron", Amount = 1, Recover = false }
    };
}
