using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfHugin;

/// <summary>
/// This class defines the Gift of Hugin potion.
/// </summary>
public class GiftOfHugin : PotionBase
{
    public GiftOfHugin(ItemManager instance) : base(instance) {}

    /// <summary>
    /// The base name of the potion.
    /// </summary>
    protected override string BaseName => "GiftOfHugin";

    /// <summary>
    /// The full name of the potion.
    /// </summary>
    protected override string FullName => "Gift of Hugin";

    /// <summary>
    /// The description of the potion.
    /// </summary>
    protected override string Description => "Grants you maximum skill level";

    /// <summary>
    /// The path to the icon of the potion.
    /// </summary>
    protected override string IconPath => "BrudvikWhiteHilt.Assets.GiftOfHugin.png";

    /// <summary>
    /// The requirements for crafting the potion.
    /// </summary>
    protected override RequirementConfig[] MeadBaseRequirements => new[]
    {
        new RequirementConfig { Item = "NeckTail", Amount = 20, Recover = false },
        new RequirementConfig { Item = "Cloudberry", Amount = 20, Recover = false },
        new RequirementConfig { Item = "Blueberries", Amount = 20, Recover = false }
    };

    /// <summary>
    /// Indicates whether the potion is enabled.
    /// </summary>
    public override bool Enabled => true;

    /// <summary>
    /// Creates the effect for the potion.
    /// </summary>
    /// <returns></returns>
    protected override StatusEffect CreateEffect()
    {
        var effect = ScriptableObject.CreateInstance<GiftOfHuginEffect>();
        effect.Initialize(FullName);
        effect.SetIcon(IconPath);
        return effect;
    }
}