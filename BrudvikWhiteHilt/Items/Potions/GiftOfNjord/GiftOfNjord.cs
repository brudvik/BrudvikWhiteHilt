using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfNjord;

/// <summary>
/// The Gift of Njord potion: brewed like a mead, from a mead base cooked at the Cauldron and fermented. Drinking it
/// gives <see cref="GiftOfNjordEffect"/>.
/// </summary>
public class GiftOfNjord : PotionBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public GiftOfNjord(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "GiftOfNjord";

    /// <inheritdoc/>
    protected override string FullName => "Gift of Njord";

    /// <inheritdoc/>
    protected override string Description => "Grants you the blessing of the god of seas and winds";

    /// <inheritdoc/>
    protected override string IconPath => "BrudvikWhiteHilt.Assets.GiftOfNjord.png";

    /// <inheritdoc/>
    protected override RequirementConfig[] MeadBaseRequirements => new[]
    {
        new RequirementConfig { Item = "FishAnglerRaw", Amount = 10, Recover = false },
        new RequirementConfig { Item = "Chitin", Amount = 5, Recover = false },
        new RequirementConfig { Item = "Bloodbag", Amount = 5, Recover = false }
    };

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Swamp;

    /// <inheritdoc/>
    protected override SE_Stats CreateEffect()
    {
        var effect = ScriptableObject.CreateInstance<GiftOfNjordEffect>();
        effect.Initialize(FullName);
        effect.SetIcon(IconPath);
        return effect;
    }
}
