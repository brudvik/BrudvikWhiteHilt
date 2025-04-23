using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfFreya;

/// <summary>
/// This class defines the Gift of Freya potion.
/// </summary>
public class GiftOfFreya : PotionBase
{
    public GiftOfFreya(ItemManager instance) : base(instance) { }

    /// <summary>
    /// The base name of the potion.
    /// </summary>
    protected override string BaseName => "GiftOfFreya";

    /// <summary>
    /// The full name of the potion.
    /// </summary>
    protected override string FullName => "Gift of Freya";

    /// <summary>
    /// The description of the potion.
    /// </summary>
    protected override string Description => "Grants you immense stamina";

    /// <summary>
    /// The path to the icon of the potion.
    /// </summary>
    protected override string IconPath => "BrudvikWhiteHilt.Assets.GiftOfFreya.png";

    /// <summary>
    /// The requirements for crafting the potion.
    /// </summary>
    protected override RequirementConfig[] MeadBaseRequirements => new[]
    {
        new RequirementConfig { Item = "Honey", Amount = 20, Recover = false },
        new RequirementConfig { Item = "Raspberry", Amount = 20, Recover = false },
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
        var effect = ScriptableObject.CreateInstance<GiftOfFreyaEffect>();
        effect.Initialize(FullName);
        effect.SetIcon(IconPath);
        return effect;
    }
}
