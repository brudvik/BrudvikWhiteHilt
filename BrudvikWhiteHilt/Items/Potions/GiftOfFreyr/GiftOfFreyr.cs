using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfFreyr;

/// <summary>
/// This class defines the Gift of Freyr potion.
/// Grants bonuses related to animals and taming.
/// </summary>
public class GiftOfFreyr : PotionBase
{
    public GiftOfFreyr(ItemManager instance) : base(instance) { }

    /// <summary>
    /// The base name of the potion.
    /// </summary>
    protected override string BaseName => "GiftOfFreyr";

    /// <summary>
    /// The full name of the potion.
    /// </summary>
    protected override string FullName => "Gift of Freyr";

    /// <summary>
    /// The description of the potion.
    /// </summary>
    protected override string Description => "Grants you the blessing of the god of fertility and peace";

    /// <summary>
    /// The path to the icon of the potion.
    /// </summary>
    protected override string IconPath => "BrudvikWhiteHilt.Assets.GiftOfFreyr.png";

    /// <summary>
    /// The requirements for crafting the potion.
    /// </summary>
    protected override RequirementConfig[] MeadBaseRequirements => new[]
    {
        new RequirementConfig { Item = "Stone", Amount = 5, Recover = false },
        new RequirementConfig { Item = "Raspberry", Amount = 5, Recover = false },
        new RequirementConfig { Item = "Mushroom", Amount = 5, Recover = false }
    };

    /// <summary>
    /// Indicates whether the potion is enabled.
    /// </summary>
    public override bool Enabled => true;

    /// <summary>
    /// Creates the effect for the potion.
    /// </summary>
    /// <returns></returns>
    protected override SE_Stats CreateEffect()
    {
        var effect = ScriptableObject.CreateInstance<GiftOfFreyrEffect>();
        effect.Initialize(FullName);
        effect.SetIcon(IconPath);
        return effect;
    }
}
