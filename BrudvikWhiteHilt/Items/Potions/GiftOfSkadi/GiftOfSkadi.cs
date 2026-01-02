using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfSkadi;

/// <summary>
/// This class defines the Gift of Skadi potion.
/// Grants immunity to frost and freezing effects.
/// </summary>
public class GiftOfSkadi : PotionBase
{
    public GiftOfSkadi(ItemManager instance) : base(instance) { }

    /// <summary>
    /// The base name of the potion.
    /// </summary>
    protected override string BaseName => "GiftOfSkadi";

    /// <summary>
    /// The full name of the potion.
    /// </summary>
    protected override string FullName => "Gift of Skadi";

    /// <summary>
    /// The description of the potion.
    /// </summary>
    protected override string Description => "Grants you the blessing of the winter goddess";

    /// <summary>
    /// The path to the icon of the potion.
    /// </summary>
    protected override string IconPath => "BrudvikWhiteHilt.Assets.GiftOfSkadi.png";

    /// <summary>
    /// The requirements for crafting the potion.
    /// </summary>
    protected override RequirementConfig[] MeadBaseRequirements => new[]
    {
        new RequirementConfig { Item = "FreezeGland", Amount = 5, Recover = false },
        new RequirementConfig { Item = "WolfPelt", Amount = 5, Recover = false },
        new RequirementConfig { Item = "Obsidian", Amount = 10, Recover = false }
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
        var effect = ScriptableObject.CreateInstance<GiftOfSkadiEffect>();
        effect.Initialize(FullName);
        effect.SetIcon(IconPath);
        return effect;
    }
}
