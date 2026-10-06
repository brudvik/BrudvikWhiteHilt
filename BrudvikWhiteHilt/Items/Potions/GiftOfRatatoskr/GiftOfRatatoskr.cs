using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfRatatoskr;

/// <summary>
/// The Gift of Ratatoskr potion: brewed like a mead, from a mead base cooked at the Cauldron and fermented. Drinking it
/// gives <see cref="GiftOfRatatoskrEffect"/>.
/// </summary>
public class GiftOfRatatoskr : PotionBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public GiftOfRatatoskr(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "GiftOfRatatoskr";

    /// <inheritdoc/>
    protected override string FullName => "Gift of Ratatoskr";

    /// <inheritdoc/>
    protected override string Description => "Grants you the agility of the squirrel that runs upon Yggdrasil";

    /// <inheritdoc/>
    protected override string IconPath => "BrudvikWhiteHilt.Assets.GiftOfRatatoskr.png";

    /// <inheritdoc/>
    protected override RequirementConfig[] MeadBaseRequirements => new[]
    {
        new RequirementConfig { Item = "Resin", Amount = 5, Recover = false },
        new RequirementConfig { Item = "Blueberries", Amount = 5, Recover = false },
        new RequirementConfig { Item = "Mushroom", Amount = 5, Recover = false }
    };

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.BlackForest;

    /// <inheritdoc/>
    protected override SE_Stats CreateEffect()
    {
        var effect = ScriptableObject.CreateInstance<GiftOfRatatoskrEffect>();
        effect.Initialize(FullName);
        effect.SetIcon(IconPath);
        return effect;
    }
}
