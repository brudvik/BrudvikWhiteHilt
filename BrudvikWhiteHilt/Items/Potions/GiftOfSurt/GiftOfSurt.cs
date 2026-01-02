using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfSurt;

/// <summary>
/// This class defines the Gift of Surt potion.
/// Grants immunity to fire and cold damage.
/// </summary>
public class GiftOfSurt : PotionBase
{
    public GiftOfSurt(ItemManager instance) : base(instance) { }

    /// <summary>
    /// The base name of the potion.
    /// </summary>
    protected override string BaseName => "GiftOfSurt";

    /// <summary>
    /// The full name of the potion.
    /// </summary>
    protected override string FullName => "Gift of Surt";

    /// <summary>
    /// The description of the potion.
    /// </summary>
    protected override string Description => "Grants you the power of the fire giant from Muspelheim";

    /// <summary>
    /// The path to the icon of the potion.
    /// </summary>
    protected override string IconPath => "BrudvikWhiteHilt.Assets.GiftOfSurt.png";

    /// <summary>
    /// The requirements for crafting the potion.
    /// </summary>
    protected override RequirementConfig[] MeadBaseRequirements => new[]
    {
        new RequirementConfig { Item = "SurtlingCore", Amount = 5, Recover = false },
        new RequirementConfig { Item = "Coal", Amount = 20, Recover = false },
        new RequirementConfig { Item = "Flametal", Amount = 2, Recover = false }
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
        var effect = ScriptableObject.CreateInstance<GiftOfSurtEffect>();
        effect.Initialize(FullName);
        effect.SetIcon(IconPath);
        return effect;
    }
}
