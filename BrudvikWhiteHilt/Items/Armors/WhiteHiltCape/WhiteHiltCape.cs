using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Armors.WhiteHiltCape;

/// <summary>
/// This class defines the White Hilt Cape item.
/// </summary>
public class WhiteHiltCape : WhiteHiltArmorBase
{
    public WhiteHiltCape(ItemManager instance) : base(instance) { }

    /// <summary>
    /// The base name of the White Hilt Cape item.
    /// </summary>
    protected override string BaseName => "WhiteHiltCape";

    /// <summary>
    /// The full name of the White Hilt Cape item.
    /// </summary>
    protected override string FullName => "White Hilt Cape";

    /// <summary>
    /// The description of the White Hilt Cape item.
    /// </summary>
    protected override string Description => "The Indestructible Cape of Dyrnwyn";

    /// <summary>
    /// The name of the item to copy from.
    /// </summary>
    protected override string CopyFrom => "CapeFeather";

    /// <summary>
    /// The requirements for crafting the White Hilt Cape.
    /// </summary>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Feathers", Amount = 30, Recover = false },
        new() { Item = "TrophyWraith", Amount = 3, Recover = false },
        new() { Item = "CapeTrollHide", Amount = 1, Recover = false }
    };

    /// <summary>
    /// Indicates whether the tool is enabled.
    /// </summary>
    public override bool Enabled => true;
}
