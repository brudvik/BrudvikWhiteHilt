using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfBrokkr;

/// <summary>
/// This class defines the Gift of Brokkr potion.
/// Grants crafting bonuses.
/// </summary>
public class GiftOfBrokkr : PotionBase
{
    public GiftOfBrokkr(ItemManager instance) : base(instance) { }

    /// <summary>
    /// The base name of the potion.
    /// </summary>
    protected override string BaseName => "GiftOfBrokkr";

    /// <summary>
    /// The full name of the potion.
    /// </summary>
    protected override string FullName => "Gift of Brokkr";

    /// <summary>
    /// The description of the potion.
    /// </summary>
    protected override string Description => "Grants you the skill of the legendary dwarf smith";

    /// <summary>
    /// The path to the icon of the potion.
    /// </summary>
    protected override string IconPath => "BrudvikWhiteHilt.Assets.GiftOfBrokkr.png";

    /// <summary>
    /// The requirements for crafting the potion.
    /// </summary>
    protected override RequirementConfig[] MeadBaseRequirements => new[]
    {
        new RequirementConfig { Item = "DvergrNeedle", Amount = 5, Recover = false },
        new RequirementConfig { Item = "SoftTissue", Amount = 10, Recover = false },
        new RequirementConfig { Item = "BlackMetal", Amount = 5, Recover = false }
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
        var effect = ScriptableObject.CreateInstance<GiftOfBrokkrEffect>();
        effect.Initialize(FullName);
        effect.SetIcon(IconPath);
        return effect;
    }
}
