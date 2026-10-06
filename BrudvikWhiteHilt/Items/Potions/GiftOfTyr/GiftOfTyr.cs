using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfTyr;

/// <summary>
/// The Gift of Tyr potion: brewed like a mead, from a mead base cooked at the Cauldron and fermented. Drinking it gives
/// <see cref="GiftOfTyrEffect"/>.
/// </summary>
public class GiftOfTyr : PotionBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public GiftOfTyr(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "GiftOfTyr";

    /// <inheritdoc/>
    protected override string FullName => "Gift of Tyr";

    /// <inheritdoc/>
    protected override string Description => "Grants you the steadfastness of the one-handed war god";

    /// <inheritdoc/>
    protected override string IconPath => "BrudvikWhiteHilt.Assets.GiftOfTyr.png";

    /// <inheritdoc/>
    protected override RequirementConfig[] MeadBaseRequirements => new[]
    {
        new RequirementConfig { Item = "Wood", Amount = 5, Recover = false },
        new RequirementConfig { Item = "Raspberry", Amount = 5, Recover = false },
        new RequirementConfig { Item = "Resin", Amount = 5, Recover = false }
    };

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.BlackForest;

    /// <inheritdoc/>
    protected override SE_Stats CreateEffect()
    {
        var effect = ScriptableObject.CreateInstance<GiftOfTyrEffect>();
        effect.Initialize(FullName);
        effect.SetIcon(IconPath);
        return effect;
    }
}
