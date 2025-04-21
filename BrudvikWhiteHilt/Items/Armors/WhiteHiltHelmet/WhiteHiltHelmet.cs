using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Armors.WhiteHiltHelmet;

/// <summary>
/// This class defines the White Hilt Helmet item.
/// </summary>
public class WhiteHiltHelmet : WhiteHiltArmorBase
{
    public WhiteHiltHelmet(ItemManager instance) : base(instance) { }

    /// <summary>
    /// The base name of the White Hilt Helmet item.
    /// </summary>
    protected override string BaseName => "WhiteHiltHelmet";

    /// <summary>
    /// The full name of the White Hilt Helmet item.
    /// </summary>
    protected override string FullName => "White Hilt Helmet";

    /// <summary>
    /// The description of the White Hilt Helmet item.
    /// </summary>
    protected override string Description => "The Indestructible Helmet of Dyrnwyn";

    /// <summary>
    /// The name of the item to copy from.
    /// </summary>
    protected override string CopyFrom => "HelmetFlametal";

    /// <summary>
    /// The requirements for crafting the White Hilt Helmet.
    /// </summary>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Iron", Amount = 30, Recover = false },
        new() { Item = "IronNails", Amount = 100, Recover = false },
        new() { Item = "HelmetIron", Amount = 1, Recover = false },
    };
}
