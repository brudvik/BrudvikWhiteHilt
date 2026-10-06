using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfThor;

/// <summary>
/// The Gift of Thor potion: brewed like a mead, from a mead base cooked at the Cauldron and fermented. Drinking it
/// gives <see cref="GiftOfThorEffect"/>.
/// </summary>
public class GiftOfThor : PotionBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public GiftOfThor(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "GiftOfThor";

    /// <inheritdoc/>
    protected override string FullName => "Gift of Thor";

    /// <inheritdoc/>
    protected override string Description => "Grants you the strength of the thunder god";

    /// <inheritdoc/>
    protected override string IconPath => "BrudvikWhiteHilt.Assets.GiftOfThor.png";

    /// <inheritdoc/>
    protected override RequirementConfig[] MeadBaseRequirements => new[]
    {
        new RequirementConfig { Item = "Thunderstone", Amount = 3, Recover = false },
        new RequirementConfig { Item = "Iron", Amount = 10, Recover = false },
        new RequirementConfig { Item = "Honey", Amount = 10, Recover = false }
    };

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Mountain;

    /// <inheritdoc/>
    protected override SE_Stats CreateEffect()
    {
        var effect = ScriptableObject.CreateInstance<GiftOfThorEffect>();
        effect.Initialize(FullName);
        effect.SetIcon(IconPath);
        return effect;
    }
}
