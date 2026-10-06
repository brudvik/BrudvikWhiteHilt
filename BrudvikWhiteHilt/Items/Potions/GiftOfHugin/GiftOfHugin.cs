using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfHugin;

/// <summary>
/// The Gift of Hugin potion: brewed like a mead, from a mead base cooked at the Cauldron and fermented. Drinking it
/// gives <see cref="GiftOfHuginEffect"/>.
/// </summary>
public class GiftOfHugin : PotionBase, IFullModeOnly
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public GiftOfHugin(ItemManager instance) : base(instance) {}

    /// <inheritdoc/>
    protected override string BaseName => "GiftOfHugin";

    /// <inheritdoc/>
    protected override string FullName => "Gift of Hugin";

    /// <inheritdoc/>
    protected override string Description => "Grants you maximum skill level";

    /// <inheritdoc/>
    protected override string IconPath => "BrudvikWhiteHilt.Assets.GiftOfHugin.png";

    /// <inheritdoc/>
    protected override RequirementConfig[] MeadBaseRequirements => new[]
    {
        new RequirementConfig { Item = "NeckTail", Amount = 20, Recover = false },
        new RequirementConfig { Item = "Cloudberry", Amount = 20, Recover = false },
        new RequirementConfig { Item = "Blueberries", Amount = 20, Recover = false }
    };

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Plains;

    /// <inheritdoc/>
    protected override SE_Stats CreateEffect()
    {
        var effect = ScriptableObject.CreateInstance<GiftOfHuginEffect>();
        effect.Initialize(FullName);
        effect.SetIcon(IconPath);
        return effect;
    }
}