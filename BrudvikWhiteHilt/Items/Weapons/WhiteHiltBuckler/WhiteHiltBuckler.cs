using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltBuckler;

/// <summary>
/// This class represents the White Hilt Buckler.
/// </summary>
public class WhiteHiltBuckler : WhiteHiltWeaponBase
{
    /// <summary>
    /// Constructor for the WhiteHiltBuckler class.
    /// </summary>
    /// <param name="instance"></param>
    public WhiteHiltBuckler(ItemManager instance) : base(instance) { }

    /// <summary>
    /// The base name of the buckler.
    /// </summary>
    protected override string BaseName => "WhiteHiltBuckler";

    /// <summary>
    /// The full name of the buckler.
    /// </summary>
    protected override string FullName => "White Hilt Buckler";

    /// <summary>
    /// The description of the buckler.
    /// </summary>
    protected override string Description => "The Indestructible Buckler of Dyrnwyn";

    /// <summary>
    /// The name of the item to copy from.
    /// </summary>
    protected override string CopyFrom => "ShieldBanded";

    /// <summary>
    /// Indicates whether the White Hilt Buckler is enabled.
    /// </summary>
    public override bool Enabled => true;

    /// <summary>
    /// The requirements for crafting the White Hilt Buckler.
    /// </summary>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Iron", Amount = 15, Recover = false },
        new() { Item = "Chain", Amount = 5, Recover = false },
        new() { Item = "ShieldBronzeBuckler", Amount = 1, Recover = false }
    };
}
