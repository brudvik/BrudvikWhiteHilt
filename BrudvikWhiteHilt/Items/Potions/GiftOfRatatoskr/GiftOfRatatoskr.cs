using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfRatatoskr;

/// <summary>
/// This class defines the Gift of Ratatoskr potion.
/// Grants increased sprint speed and stamina efficiency.
/// </summary>
public class GiftOfRatatoskr : PotionBase
{
    public GiftOfRatatoskr(ItemManager instance) : base(instance) { }

    /// <summary>
    /// The base name of the potion.
    /// </summary>
    protected override string BaseName => "GiftOfRatatoskr";

    /// <summary>
    /// The full name of the potion.
    /// </summary>
    protected override string FullName => "Gift of Ratatoskr";

    /// <summary>
    /// The description of the potion.
    /// </summary>
    protected override string Description => "Grants you the agility of the squirrel that runs upon Yggdrasil";

    /// <summary>
    /// The path to the icon of the potion.
    /// </summary>
    protected override string IconPath => "BrudvikWhiteHilt.Assets.GiftOfRatatoskr.png";

    /// <summary>
    /// The requirements for crafting the potion.
    /// </summary>
    protected override RequirementConfig[] MeadBaseRequirements => new[]
    {
        new RequirementConfig { Item = "Resin", Amount = 5, Recover = false },
        new RequirementConfig { Item = "Blueberries", Amount = 5, Recover = false },
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
        var effect = ScriptableObject.CreateInstance<GiftOfRatatoskrEffect>();
        effect.Initialize(FullName);
        effect.SetIcon(IconPath);
        return effect;
    }
}
