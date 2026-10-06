using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Indestructible;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Accessories.WhiteHiltMegingjord;

/// <summary>
/// The White Hilt Megingjord, cloned from the vanilla <c>BeltStrength</c>.
/// </summary>
public class WhiteHiltMegingjord : WhiteHiltAccessoryBase
{
    private const string EffectKey = "se_whitehiltmegingjord";

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltMegingjord(ItemManager instance) : base(instance)
    {
        Translations.AddEnglish(EffectKey, "Dyrnwyn's Strength");
        Translations.AddEnglish($"{EffectKey}_tooltip", "Carry weight increased by 700");
    }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltMegingjord";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Megingjord";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Belt of Dyrnwyn. Grants immense carrying capacity (+700).";

    /// <inheritdoc/>
    protected override string CopyFrom => "BeltStrength";

    /// <inheritdoc/>
    public override bool Enabled => false;

    /// <inheritdoc/>
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
        effect.m_name = Translations.Token(EffectKey);
        effect.m_tooltip = Translations.Token($"{EffectKey}_tooltip");
        effect.m_addMaxCarryWeight = 700f;
        return effect;
    }
}
