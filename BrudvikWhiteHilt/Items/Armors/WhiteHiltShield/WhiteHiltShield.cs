using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Armors.WhiteHiltShield;

/// <summary>
/// This class defines the White Hilt Shield item.
/// </summary>
public class WhiteHiltShield : WhiteHiltArmorBase
{
    public WhiteHiltShield(ItemManager instance) : base(instance) { }

    /// <summary>
    /// The base name of the White Hilt Shield item.
    /// </summary>
    protected override string BaseName => "WhiteHiltShield";

    /// <summary>
    /// The full name of the White Hilt Shield item.
    /// </summary>
    protected override string FullName => "White Hilt Shield";

    /// <summary>
    /// The description of the White Hilt Shield item.
    /// </summary>
    protected override string Description => "The Indestructible Shield of Dyrnwyn";

    /// <summary>
    /// The name of the item to copy from.
    /// </summary>
    protected override string CopyFrom => "ShieldBanded";

    /// <summary>
    /// The requirements for crafting the White Hilt Shield.
    /// </summary>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Iron", Amount = 30, Recover = false },
        new() { Item = "IronNails", Amount = 100, Recover = false },
        new() { Item = "ShieldIronBuckler", Amount = 1, Recover = false },
    };

    /// <summary>
    /// Indicates whether the tool is enabled.
    /// </summary>
    public override bool Enabled => true;
}
