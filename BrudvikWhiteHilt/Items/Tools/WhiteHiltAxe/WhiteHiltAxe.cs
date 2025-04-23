using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Tools.WhiteHiltAxe;

/// <summary>
/// This class defines the White Hilt Axe item.
/// </summary>
public class WhiteHiltAxe : WhiteHiltToolBase
{
    /// <summary>
    /// Constructor for the WhiteHiltAxe class.
    /// </summary>
    /// <param name="instance"></param>
    public WhiteHiltAxe(ItemManager instance) : base(instance) { }

    /// <summary>
    /// The base name of the axe.
    /// </summary>
    protected override string BaseName => "WhiteHiltAxe";

    /// <summary>
    /// The full name of the axe.
    /// </summary>
    protected override string FullName => "White Hilt Axe";

    /// <summary>
    /// The description of the axe.
    /// </summary>
    protected override string Description => "The Indestructible Axe of Dyrnwyn";

    /// <summary>
    /// The name of the item to copy from.
    /// </summary>
    protected override string CopyFrom => "AxeIron";

    /// <summary>
    /// Indicates whether the tool is enabled.
    /// </summary>
    public override bool Enabled => true;
}
