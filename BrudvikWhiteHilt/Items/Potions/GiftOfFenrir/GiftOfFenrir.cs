using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfFenrir;

/// <summary>
/// This class defines the Gift of Fenrir potion.
/// Grants increased attack speed and life steal.
/// </summary>
public class GiftOfFenrir : PotionBase
{
    public GiftOfFenrir(ItemManager instance) : base(instance) { }

    /// <summary>
    /// The base name of the potion.
    /// </summary>
    protected override string BaseName => "GiftOfFenrir";

    /// <summary>
    /// The full name of the potion.
    /// </summary>
    protected override string FullName => "Gift of Fenrir";

    /// <summary>
    /// The description of the potion.
    /// </summary>
    protected override string Description => "Grants you the ferocity of the mighty wolf";

    /// <summary>
    /// The path to the icon of the potion.
    /// </summary>
    protected override string IconPath => "BrudvikWhiteHilt.Assets.GiftOfFenrir.png";

    /// <summary>
    /// The requirements for crafting the potion.
    /// </summary>
    protected override RequirementConfig[] MeadBaseRequirements => new[]
    {
        new RequirementConfig { Item = "Wood", Amount = 5, Recover = false },
        new RequirementConfig { Item = "Mushroom", Amount = 5, Recover = false },
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
        var effect = ScriptableObject.CreateInstance<GiftOfFenrirEffect>();
        effect.Initialize(FullName);
        effect.SetIcon(IconPath);
        return effect;
    }
}
