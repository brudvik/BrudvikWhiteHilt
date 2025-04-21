using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Tools.WhiteHiltCultivator;

/// <summary>
/// This class defines the White Hilt Cultivator item.
/// </summary>
public class WhiteHiltCultivator : WhiteHiltToolBase
{
    /// <summary>
    /// Constructor for the WhiteHiltCultivator class.
    /// </summary>
    /// <param name="instance"></param>
    public WhiteHiltCultivator(ItemManager instance) : base(instance) { }

    /// <summary>
    /// The base name of the cultivator.
    /// </summary>
    protected override string BaseName => "WhiteHiltCultivator";

    /// <summary>
    /// The full name of the cultivator.
    /// </summary>
    protected override string FullName => "White Hilt Cultivator";

    /// <summary>
    /// The description of the cultivator.
    /// </summary>
    protected override string Description => "The Indestructible Cultivator of Dyrnwyn";

    /// <summary>
    /// The name of the item to copy from.
    /// </summary>
    protected override string CopyFrom => "Cultivator";
}
