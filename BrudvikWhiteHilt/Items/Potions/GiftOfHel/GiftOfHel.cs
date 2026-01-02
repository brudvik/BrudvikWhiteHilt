using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfHel;

/// <summary>
/// This class defines the Gift of Hel potion.
/// Grants a one-time resurrection effect - respawn with all items on death.
/// </summary>
public class GiftOfHel : PotionBase
{
    public GiftOfHel(ItemManager instance) : base(instance) { }

    /// <summary>
    /// The base name of the potion.
    /// </summary>
    protected override string BaseName => "GiftOfHel";

    /// <summary>
    /// The full name of the potion.
    /// </summary>
    protected override string FullName => "Gift of Hel";

    /// <summary>
    /// The description of the potion.
    /// </summary>
    protected override string Description => "Grants you a second chance from the goddess of the underworld";

    /// <summary>
    /// The path to the icon of the potion.
    /// </summary>
    protected override string IconPath => "BrudvikWhiteHilt.Assets.GiftOfHel.png";

    /// <summary>
    /// The requirements for crafting the potion.
    /// </summary>
    protected override RequirementConfig[] MeadBaseRequirements => new[]
    {
        new RequirementConfig { Item = "TrophySkeleton", Amount = 5, Recover = false },
        new RequirementConfig { Item = "BoneFragments", Amount = 30, Recover = false },
        new RequirementConfig { Item = "Eitr", Amount = 3, Recover = false }
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
        var effect = ScriptableObject.CreateInstance<GiftOfHelEffect>();
        effect.Initialize(FullName);
        effect.SetIcon(IconPath);
        return effect;
    }
}
