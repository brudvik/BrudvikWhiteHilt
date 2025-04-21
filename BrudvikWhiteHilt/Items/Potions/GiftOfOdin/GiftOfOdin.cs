using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfOdin;

/// <summary>
/// This class defines the Gift of Odin potion.
/// </summary>
public class GiftOfOdin : PotionBase
{
    public GiftOfOdin(ItemManager instance) : base(instance) { }

    /// <summary>
    /// The base name of the potion.
    /// </summary>
    protected override string BaseName => "GiftOfOdin";

    /// <summary>
    /// The full name of the potion.
    /// </summary>
    protected override string FullName => "Gift of Odin";

    /// <summary>
    /// The description of the potion.
    /// </summary>
    protected override string Description => "Grants you undwindling health";

    /// <summary>
    /// The path to the icon of the potion.
    /// </summary>
    protected override string IconPath => "BrudvikWhiteHilt.Assets.GiftOfOdin.png";

    /// <summary>
    /// The requirements for crafting the potion.
    /// </summary>
    protected override RequirementConfig[] MeadBaseRequirements => new[]
    {
        new RequirementConfig { Item = "Mushroom", Amount = 20, Recover = false },
        new RequirementConfig { Item = "Raspberry", Amount = 20, Recover = false },
        new RequirementConfig { Item = "Blueberries", Amount = 20, Recover = false }
    };

    /// <summary>
    /// Creates the effect for the potion.
    /// </summary>
    /// <returns></returns>
    protected override StatusEffect CreateEffect()
    {
        var effect = ScriptableObject.CreateInstance<GiftOfOdinEffect>();
        effect.Initialize(FullName);
        effect.SetIcon(IconPath);
        return effect;
    }
}
