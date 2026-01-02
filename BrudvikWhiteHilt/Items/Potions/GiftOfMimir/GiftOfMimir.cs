using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfMimir;

/// <summary>
/// This class defines the Gift of Mimir potion.
/// Grants enhanced awareness and ability to locate resources.
/// </summary>
public class GiftOfMimir : PotionBase
{
    public GiftOfMimir(ItemManager instance) : base(instance) { }

    /// <summary>
    /// The base name of the potion.
    /// </summary>
    protected override string BaseName => "GiftOfMimir";

    /// <summary>
    /// The full name of the potion.
    /// </summary>
    protected override string FullName => "Gift of Mimir";

    /// <summary>
    /// The description of the potion.
    /// </summary>
    protected override string Description => "Grants you the wisdom of the guardian of knowledge";

    /// <summary>
    /// The path to the icon of the potion.
    /// </summary>
    protected override string IconPath => "BrudvikWhiteHilt.Assets.GiftOfMimir.png";

    /// <summary>
    /// The requirements for crafting the potion.
    /// </summary>
    protected override RequirementConfig[] MeadBaseRequirements => new[]
    {
        new RequirementConfig { Item = "YggdrasilWood", Amount = 10, Recover = false },
        new RequirementConfig { Item = "Sap", Amount = 10, Recover = false },
        new RequirementConfig { Item = "Eitr", Amount = 2, Recover = false }
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
        var effect = ScriptableObject.CreateInstance<GiftOfMimirEffect>();
        effect.Initialize(FullName);
        effect.SetIcon(IconPath);
        return effect;
    }
}
