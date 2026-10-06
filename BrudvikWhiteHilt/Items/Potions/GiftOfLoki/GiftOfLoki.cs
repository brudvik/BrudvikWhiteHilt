using BrudvikWhiteHilt.Monsters;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfLoki;

/// <summary>
/// The Gift of Loki potion: brewed like a mead, from a mead base cooked at the Cauldron and fermented. Drinking it
/// gives <see cref="GiftOfLokiEffect"/>.
/// </summary>
public class GiftOfLoki : PotionBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public GiftOfLoki(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "GiftOfLoki";

    /// <inheritdoc/>
    protected override string FullName => "Gift of Loki";

    /// <inheritdoc/>
    protected override string Description => "Grants you endless of Eitr";

    /// <inheritdoc/>
    protected override string IconPath => "BrudvikWhiteHilt.Assets.GiftOfLoki.png";

    /// <inheritdoc/>
    protected override RequirementConfig[] MeadBaseRequirements => new[]
    {
        new RequirementConfig { Item = "NeckTail", Amount = 20, Recover = false },
        // Loki made the first fishing net.
        new RequirementConfig { Item = MonsterRegistry.SilkName, Amount = 5, Recover = false },
        new RequirementConfig { Item = "Eitr", Amount = 1, Recover = false }
    };

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Mistlands;

    /// <inheritdoc/>
    protected override SE_Stats CreateEffect()
    {
        var effect = ScriptableObject.CreateInstance<GiftOfLokiEffect>();
        effect.Initialize(FullName);
        effect.SetIcon(IconPath);
        return effect;
    }
}
