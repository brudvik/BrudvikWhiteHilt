using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfNjord;

/// <summary>
/// This class defines the Gift of Njord potion.
/// Grants the ability to breathe underwater and swim effortlessly.
/// </summary>
public class GiftOfNjord : PotionBase
{
    public GiftOfNjord(ItemManager instance) : base(instance) { }

    /// <summary>
    /// The base name of the potion.
    /// </summary>
    protected override string BaseName => "GiftOfNjord";

    /// <summary>
    /// The full name of the potion.
    /// </summary>
    protected override string FullName => "Gift of Njord";

    /// <summary>
    /// The description of the potion.
    /// </summary>
    protected override string Description => "Grants you the blessing of the god of seas and winds";

    /// <summary>
    /// The path to the icon of the potion.
    /// </summary>
    protected override string IconPath => "BrudvikWhiteHilt.Assets.GiftOfNjord.png";

    /// <summary>
    /// The requirements for crafting the potion.
    /// </summary>
    protected override RequirementConfig[] MeadBaseRequirements => new[]
    {
        new RequirementConfig { Item = "FishAnglerRaw", Amount = 10, Recover = false },
        new RequirementConfig { Item = "Chitin", Amount = 5, Recover = false },
        new RequirementConfig { Item = "Bloodbag", Amount = 5, Recover = false }
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
        var effect = ScriptableObject.CreateInstance<GiftOfNjordEffect>();
        effect.Initialize(FullName);
        effect.SetIcon(IconPath);
        return effect;
    }
}
