using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Tools.WhiteHiltHoe;

/// <summary>
/// This class defines the White Hilt Hoe item.
/// </summary>
public class WhiteHiltHoe : WhiteHiltToolBase
{
    /// <summary>
    /// Constructor for the WhiteHiltHoe class.
    /// </summary>
    /// <param name="instance"></param>
    public WhiteHiltHoe(ItemManager instance) : base(instance) { }

    /// <summary>
    /// The base name of the hoe.
    /// </summary>
    protected override string BaseName => "WhiteHiltHoe";

    /// <summary>
    /// The full name of the hoe.
    /// </summary>
    protected override string FullName => "White Hilt Hoe";

    /// <summary>
    /// The description of the hoe.
    /// </summary>
    protected override string Description => "The Indestructible Hoe of Dyrnwyn";

    /// <summary>
    /// The name of the item to copy from.
    /// </summary>
    protected override string CopyFrom => "Hoe";
}
