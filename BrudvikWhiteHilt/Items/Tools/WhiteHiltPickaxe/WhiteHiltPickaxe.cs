using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Tools.WhiteHiltPickaxe;

/// <summary>
/// This class defines the White Hilt Pickaxe item.
/// </summary>
public class WhiteHiltPickaxe : WhiteHiltToolBase
{
    /// <summary>
    /// Constructor for the WhiteHiltPickaxe class.
    /// </summary>
    /// <param name="instance"></param>
    public WhiteHiltPickaxe(ItemManager instance) : base(instance) { }

    /// <summary>
    /// The base name of the pickaxe.
    /// </summary>
    protected override string BaseName => "WhiteHiltPickaxe";

    /// <summary>
    /// The full name of the pickaxe.
    /// </summary>
    protected override string FullName => "White Hilt Pickaxe";

    /// <summary>
    /// The description of the pickaxe.
    /// </summary>
    protected override string Description => "The Indestructible Pickaxe of Dyrnwyn";

    /// <summary>
    /// The name of the item to copy from.
    /// </summary>
    protected override string CopyFrom => "PickaxeIron";

    /// <summary>
    /// Indicates whether the tool is enabled.
    /// </summary>
    public override bool Enabled => true;
}
