using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfBaldur;

/// <summary>
/// The Gift of Baldur potion: brewed like a mead, from a mead base cooked at the Cauldron and fermented. Drinking it
/// gives <see cref="GiftOfBaldurEffect"/>.
/// </summary>
public class GiftOfBaldur : PotionBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public GiftOfBaldur(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "GiftOfBaldur";

    /// <inheritdoc/>
    protected override string FullName => "Gift of Baldur";

    /// <inheritdoc/>
    protected override string Description => "Grants you the unseen grace of the shining god";

    /// <inheritdoc/>
    protected override string IconPath => "BrudvikWhiteHilt.Assets.GiftOfBaldur.png";

    /// <inheritdoc/>
    protected override RequirementConfig[] MeadBaseRequirements => new[]
    {
        new RequirementConfig { Item = "Wood", Amount = 5, Recover = false },
        new RequirementConfig { Item = "Stone", Amount = 5, Recover = false },
        new RequirementConfig { Item = "Resin", Amount = 5, Recover = false }
    };

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Plains;

    /// <inheritdoc/>
    protected override SE_Stats CreateEffect()
    {
        var effect = ScriptableObject.CreateInstance<GiftOfBaldurEffect>();
        effect.Initialize(FullName);
        effect.SetIcon(IconPath);
        return effect;
    }
}
