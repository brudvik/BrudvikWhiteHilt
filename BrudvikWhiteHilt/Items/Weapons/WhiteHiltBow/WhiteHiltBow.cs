using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltBow;

/// <summary>
/// This class represents the White Hilt Bow.
/// </summary>
public class WhiteHiltBow : WhiteHiltWeaponBase
{
    /// <summary>
    /// Constructor for the WhiteHiltBow class.
    /// </summary>
    /// <param name="instance"></param>
    public WhiteHiltBow(ItemManager instance) : base(instance) { }

    /// <summary>
    /// The base name of the bow.
    /// </summary>
    protected override string BaseName => "WhiteHiltBow";

    /// <summary>
    /// The full name of the bow.
    /// </summary>
    protected override string FullName => "White Hilt Bow";

    /// <summary>
    /// The description of the bow.
    /// </summary>
    protected override string Description => "The Indestructible Bow of Dyrnwyn";

    /// <summary>
    /// The name of the item to copy from.
    /// </summary>
    protected override string CopyFrom => "BowAshlands";

    /// <summary>
    /// The requirements for crafting the White Hilt Bow.
    /// </summary>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "FineWood", Amount = 20, Recover = false },
        new() { Item = "Feathers", Amount = 20, Recover = false },
        new() { Item = "BowFineWood", Amount = 1, Recover = false }
    };
}
