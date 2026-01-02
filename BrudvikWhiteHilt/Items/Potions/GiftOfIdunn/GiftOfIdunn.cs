using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfIdunn;

/// <summary>
/// This class defines the Gift of Idunn potion.
/// Grants extended food and potion duration.
/// </summary>
public class GiftOfIdunn : PotionBase
{
    public GiftOfIdunn(ItemManager instance) : base(instance) { }

    /// <summary>
    /// The base name of the potion.
    /// </summary>
    protected override string BaseName => "GiftOfIdunn";

    /// <summary>
    /// The full name of the potion.
    /// </summary>
    protected override string FullName => "Gift of Idunn";

    /// <summary>
    /// The description of the potion.
    /// </summary>
    protected override string Description => "Grants you the blessing of the goddess of eternal youth";

    /// <summary>
    /// The path to the icon of the potion.
    /// </summary>
    protected override string IconPath => "BrudvikWhiteHilt.Assets.GiftOfIdunn.png";

    /// <summary>
    /// The requirements for crafting the potion.
    /// </summary>
    protected override RequirementConfig[] MeadBaseRequirements => new[]
    {
        new RequirementConfig { Item = "Resin", Amount = 5, Recover = false },
        new RequirementConfig { Item = "Raspberry", Amount = 5, Recover = false },
        new RequirementConfig { Item = "Dandelion", Amount = 5, Recover = false }
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
        var effect = ScriptableObject.CreateInstance<GiftOfIdunnEffect>();
        effect.Initialize(FullName);
        effect.SetIcon(IconPath);
        return effect;
    }
}
