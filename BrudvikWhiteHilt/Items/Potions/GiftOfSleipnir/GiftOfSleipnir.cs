using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfSleipnir;

/// <summary>
/// This class defines the Gift of Sleipnir potion.
/// Grants increased movement speed, no fall damage, and higher jumps.
/// </summary>
public class GiftOfSleipnir : PotionBase
{
    public GiftOfSleipnir(ItemManager instance) : base(instance) { }

    /// <summary>
    /// The base name of the potion.
    /// </summary>
    protected override string BaseName => "GiftOfSleipnir";

    /// <summary>
    /// The full name of the potion.
    /// </summary>
    protected override string FullName => "Gift of Sleipnir";

    /// <summary>
    /// The description of the potion.
    /// </summary>
    protected override string Description => "Grants you the speed of Odin's eight-legged horse";

    /// <summary>
    /// The path to the icon of the potion.
    /// </summary>
    protected override string IconPath => "BrudvikWhiteHilt.Assets.GiftOfSleipnir.png";

    /// <summary>
    /// The requirements for crafting the potion.
    /// </summary>
    protected override RequirementConfig[] MeadBaseRequirements => new[]
    {
        new RequirementConfig { Item = "Honey", Amount = 10, Recover = false },
        new RequirementConfig { Item = "Thistle", Amount = 10, Recover = false },
        new RequirementConfig { Item = "LoxMeat", Amount = 5, Recover = false }
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
        var effect = ScriptableObject.CreateInstance<GiftOfSleipnirEffect>();
        effect.Initialize(FullName);
        effect.SetIcon(IconPath);
        return effect;
    }
}
