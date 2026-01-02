using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Ammunition.WhiteHiltBolts;

/// <summary>
/// This class represents the White Hilt Bolts.
/// High-damage crossbow bolts crafted in large quantities.
/// </summary>
public class WhiteHiltBolts : WhiteHiltAmmunitionBase
{
    /// <summary>
    /// Constructor for the WhiteHiltBolts class.
    /// </summary>
    /// <param name="instance"></param>
    public WhiteHiltBolts(ItemManager instance) : base(instance) { }

    /// <summary>
    /// The base name of the bolts.
    /// </summary>
    protected override string BaseName => "WhiteHiltBolts";

    /// <summary>
    /// The full name of the bolts.
    /// </summary>
    protected override string FullName => "White Hilt Bolts";

    /// <summary>
    /// The description of the bolts.
    /// </summary>
    protected override string Description => "The Indestructible Bolts of Dyrnwyn. Enhanced with fire and spirit damage.";

    /// <summary>
    /// The name of the item to copy from.
    /// </summary>
    protected override string CopyFrom => "BoltCarapace";

    /// <summary>
    /// The amount crafted per recipe.
    /// </summary>
    protected override int CraftAmount => 200;

    /// <summary>
    /// Indicates whether the White Hilt Bolts are enabled.
    /// </summary>
    public override bool Enabled => true;

    /// <summary>
    /// The requirements for crafting the White Hilt Bolts.
    /// </summary>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "BlackMetal", Amount = 2, Recover = false },
        new() { Item = "Feathers", Amount = 3, Recover = false },
        new() { Item = "Iron", Amount = 5, Recover = false }
    };
}
