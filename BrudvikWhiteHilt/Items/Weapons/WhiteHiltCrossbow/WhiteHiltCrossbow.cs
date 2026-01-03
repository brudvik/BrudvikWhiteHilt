using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltCrossbow;

/// <summary>
/// This class represents the White Hilt Crossbow.
/// </summary>
public class WhiteHiltCrossbow : WhiteHiltWeaponBase
{
    /// <summary>
    /// Constructor for the WhiteHiltCrossbow class.
    /// </summary>
    /// <param name="instance"></param>
    public WhiteHiltCrossbow(ItemManager instance) : base(instance) { }

    /// <summary>
    /// The base name of the crossbow.
    /// </summary>
    protected override string BaseName => "WhiteHiltCrossbow";

    /// <summary>
    /// The full name of the crossbow.
    /// </summary>
    protected override string FullName => "White Hilt Crossbow";

    /// <summary>
    /// The description of the crossbow.
    /// </summary>
    protected override string Description => "The Indestructible Crossbow of Dyrnwyn";

    /// <summary>
    /// The name of the item to copy from.
    /// </summary>
    protected override string CopyFrom => "BowHuntsman";

    /// <summary>
    /// Indicates whether the White Hilt Crossbow is enabled.
    /// </summary>
    public override bool Enabled => true;

    /// <summary>
    /// The requirements for crafting the White Hilt Crossbow.
    /// </summary>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Iron", Amount = 20, Recover = false },
        new() { Item = "Root", Amount = 10, Recover = false },
        new() { Item = "BowFineWood", Amount = 1, Recover = false }
    };
}
