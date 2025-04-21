using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Tools.WhiteHiltHammer;

/// <summary>
/// This class defines the White Hilt Hammer item.
/// </summary>
public class WhiteHiltHammer : WhiteHiltToolBase
{
    /// <summary>
    /// Constructor for the WhiteHiltHammer class.
    /// </summary>
    /// <param name="instance"></param>
    public WhiteHiltHammer(ItemManager instance) : base(instance) { }

    /// <summary>
    /// The base name of the hammer.
    /// </summary>
    protected override string BaseName => "WhiteHiltHammer";

    /// <summary>
    /// The full name of the hammer.
    /// </summary>
    protected override string FullName => "White Hilt Hammer";

    /// <summary>
    /// The description of the hammer.
    /// </summary>
    protected override string Description => "The Indestructible Hammer of Dyrnwyn";

    /// <summary>
    /// The name of the item to copy from.
    /// </summary>
    protected override string CopyFrom => "Hammer";

    /// <summary>
    /// The requirements for crafting the White Hilt Hammer.
    /// </summary>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Wood", Amount = 5, Recover = false },
        new() { Item = "Stone", Amount = 1, Recover = false },
        new() { Item = "Resin", Amount = 1, Recover = false }
    };
}
