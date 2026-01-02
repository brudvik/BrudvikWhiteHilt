using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfThor;

/// <summary>
/// This class defines the Gift of Thor potion.
/// Grants increased mining/chopping yield and faster tool speed.
/// </summary>
public class GiftOfThor : PotionBase
{
    public GiftOfThor(ItemManager instance) : base(instance) { }

    /// <summary>
    /// The base name of the potion.
    /// </summary>
    protected override string BaseName => "GiftOfThor";

    /// <summary>
    /// The full name of the potion.
    /// </summary>
    protected override string FullName => "Gift of Thor";

    /// <summary>
    /// The description of the potion.
    /// </summary>
    protected override string Description => "Grants you the strength of the thunder god";

    /// <summary>
    /// The path to the icon of the potion.
    /// </summary>
    protected override string IconPath => "BrudvikWhiteHilt.Assets.GiftOfThor.png";

    /// <summary>
    /// The requirements for crafting the potion.
    /// </summary>
    protected override RequirementConfig[] MeadBaseRequirements => new[]
    {
        new RequirementConfig { Item = "Thunderstone", Amount = 3, Recover = false },
        new RequirementConfig { Item = "Iron", Amount = 10, Recover = false },
        new RequirementConfig { Item = "Honey", Amount = 10, Recover = false }
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
        var effect = ScriptableObject.CreateInstance<GiftOfThorEffect>();
        effect.Initialize(FullName);
        effect.SetIcon(IconPath);
        return effect;
    }
}
