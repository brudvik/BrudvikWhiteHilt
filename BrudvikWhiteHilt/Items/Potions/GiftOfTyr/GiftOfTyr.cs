using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfTyr;

/// <summary>
/// This class defines the Gift of Tyr potion.
/// Grants increased blocking and parry with no knockback.
/// </summary>
public class GiftOfTyr : PotionBase
{
    public GiftOfTyr(ItemManager instance) : base(instance) { }

    /// <summary>
    /// The base name of the potion.
    /// </summary>
    protected override string BaseName => "GiftOfTyr";

    /// <summary>
    /// The full name of the potion.
    /// </summary>
    protected override string FullName => "Gift of Tyr";

    /// <summary>
    /// The description of the potion.
    /// </summary>
    protected override string Description => "Grants you the steadfastness of the one-handed war god";

    /// <summary>
    /// The path to the icon of the potion.
    /// </summary>
    protected override string IconPath => "BrudvikWhiteHilt.Assets.GiftOfTyr.png";

    /// <summary>
    /// The requirements for crafting the potion.
    /// </summary>
    protected override RequirementConfig[] MeadBaseRequirements => new[]
    {
        new RequirementConfig { Item = "TrophyFenring", Amount = 1, Recover = false },
        new RequirementConfig { Item = "Silver", Amount = 10, Recover = false },
        new RequirementConfig { Item = "WolfFang", Amount = 10, Recover = false }
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
        var effect = ScriptableObject.CreateInstance<GiftOfTyrEffect>();
        effect.Initialize(FullName);
        effect.SetIcon(IconPath);
        return effect;
    }
}
