using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltSledge;

/// <summary>
/// This class represents the White Hilt Sledge.
/// </summary>
public class WhiteHiltSledge : WhiteHiltWeaponBase
{
    /// <summary>
    /// Constructor for the WhiteHiltSledge class.
    /// </summary>
    /// <param name="instance"></param>
    public WhiteHiltSledge(ItemManager instance) : base(instance) { }

    /// <summary>
    /// The base name of the sledge.
    /// </summary>
    protected override string BaseName => "WhiteHiltSledge";

    /// <summary>
    /// The full name of the sledge.
    /// </summary>
    protected override string FullName => "White Hilt Sledge";

    /// <summary>
    /// The description of the sledge.
    /// </summary>
    protected override string Description => "The Indestructible Sledge of Dyrnwyn";

    /// <summary>
    /// The name of the item to copy from.
    /// </summary>
    protected override string CopyFrom => "SledgeIron";

    /// <summary>
    /// Indicates whether the White Hilt Sledge is enabled.
    /// </summary>
    public override bool Enabled => false;

    /// <summary>
    /// The requirements for crafting the White Hilt Sledge.
    /// </summary>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Iron", Amount = 30, Recover = false },
        new() { Item = "YmirRemains", Amount = 10, Recover = false },
        new() { Item = "SledgeStagbreaker", Amount = 1, Recover = false }
    };
}
