using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfBaldur;

/// <summary>
/// This class defines the Gift of Baldur potion.
/// Grants stealth/invisibility to enemies.
/// </summary>
public class GiftOfBaldur : PotionBase
{
    public GiftOfBaldur(ItemManager instance) : base(instance) { }

    /// <summary>
    /// The base name of the potion.
    /// </summary>
    protected override string BaseName => "GiftOfBaldur";

    /// <summary>
    /// The full name of the potion.
    /// </summary>
    protected override string FullName => "Gift of Baldur";

    /// <summary>
    /// The description of the potion.
    /// </summary>
    protected override string Description => "Grants you the invulnerability of the shining god";

    /// <summary>
    /// The path to the icon of the potion.
    /// </summary>
    protected override string IconPath => "BrudvikWhiteHilt.Assets.GiftOfBaldur.png";

    /// <summary>
    /// The requirements for crafting the potion.
    /// </summary>
    protected override RequirementConfig[] MeadBaseRequirements => new[]
    {
        new RequirementConfig { Item = "Crystal", Amount = 10, Recover = false },
        new RequirementConfig { Item = "GreydwarfEye", Amount = 20, Recover = false },
        new RequirementConfig { Item = "TrophyWraith", Amount = 1, Recover = false }
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
        var effect = ScriptableObject.CreateInstance<GiftOfBaldurEffect>();
        effect.Initialize(FullName);
        effect.SetIcon(IconPath);
        return effect;
    }
}
