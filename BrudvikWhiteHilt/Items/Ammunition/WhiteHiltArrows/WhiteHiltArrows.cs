using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Ammunition.WhiteHiltArrows;

/// <summary>
/// This class represents the White Hilt Arrows.
/// High-damage arrows crafted in large quantities.
/// </summary>
public class WhiteHiltArrows : WhiteHiltAmmunitionBase
{
    /// <summary>
    /// Constructor for the WhiteHiltArrows class.
    /// </summary>
    /// <param name="instance"></param>
    public WhiteHiltArrows(ItemManager instance) : base(instance) { }

    /// <summary>
    /// The base name of the arrows.
    /// </summary>
    protected override string BaseName => "WhiteHiltArrows";

    /// <summary>
    /// The full name of the arrows.
    /// </summary>
    protected override string FullName => "White Hilt Arrows";

    /// <summary>
    /// The description of the arrows.
    /// </summary>
    protected override string Description => "The Indestructible Arrows of Dyrnwyn. Enhanced with fire and spirit damage.";

    /// <summary>
    /// The name of the item to copy from.
    /// </summary>
    protected override string CopyFrom => "ArrowIron";

    /// <summary>
    /// The amount crafted per recipe.
    /// </summary>
    protected override int CraftAmount => 200;

    /// <summary>
    /// Indicates whether the White Hilt Arrows are enabled.
    /// </summary>
    public override bool Enabled => true;

    /// <summary>
    /// The requirements for crafting the White Hilt Arrows.
    /// </summary>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Iron", Amount = 2, Recover = false },
        new() { Item = "Feathers", Amount = 5, Recover = false },
        new() { Item = "Wood", Amount = 10, Recover = false }
    };
}
