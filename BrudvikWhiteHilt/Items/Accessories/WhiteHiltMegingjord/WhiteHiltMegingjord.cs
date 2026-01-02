using BrudvikWhiteHilt.Items.Indestructible;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Accessories.WhiteHiltMegingjord;

/// <summary>
/// This class represents the White Hilt Megingjord.
/// An enhanced belt that grants massive carry weight bonus.
/// </summary>
public class WhiteHiltMegingjord : WhiteHiltAccessoryBase
{
    /// <summary>
    /// Constructor for the WhiteHiltMegingjord class.
    /// </summary>
    /// <param name="instance"></param>
    public WhiteHiltMegingjord(ItemManager instance) : base(instance) { }

    /// <summary>
    /// The base name of the megingjord.
    /// </summary>
    protected override string BaseName => "WhiteHiltMegingjord";

    /// <summary>
    /// The full name of the megingjord.
    /// </summary>
    protected override string FullName => "White Hilt Megingjord";

    /// <summary>
    /// The description of the megingjord.
    /// </summary>
    protected override string Description => "The Indestructible Belt of Dyrnwyn. Grants immense carrying capacity (+700).";

    /// <summary>
    /// The name of the item to copy from.
    /// </summary>
    protected override string CopyFrom => "BeltStrength";

    /// <summary>
    /// Indicates whether the White Hilt Megingjord is enabled.
    /// </summary>
    public override bool Enabled => true;

    /// <summary>
    /// The requirements for crafting the White Hilt Megingjord.
    /// </summary>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Iron", Amount = 10, Recover = false },
        new() { Item = "YmirRemains", Amount = 10, Recover = false },
        new() { Item = "BeltStrength", Amount = 1, Recover = false }
    };

    /// <summary>
    /// Configures the megingjord stats - enhanced carry weight.
    /// </summary>
    /// <param name="item">The item to configure.</param>
    protected override void ConfigureStats(IndestructibleItem item)
    {
        // Original Megingjord gives +150, we give +450 (3x the original)
        item.ItemData.m_equipStatusEffect = CreateCarryWeightEffect();
    }

    /// <summary>
    /// Creates a status effect for enhanced carry weight.
    /// </summary>
    private static SE_Stats CreateCarryWeightEffect()
    {
        SE_Stats effect = ScriptableObject.CreateInstance<SE_Stats>();
        effect.name = "WhiteHiltMegingjordEffect";
        effect.m_name = "Dyrnwyn's Strength";
        effect.m_tooltip = "Carry weight increased by 700";
        effect.m_addMaxCarryWeight = 700f;
        return effect;
    }
}
