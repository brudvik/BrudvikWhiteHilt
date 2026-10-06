using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfSleipnir;

/// <summary>
/// The Gift of Sleipnir potion: brewed like a mead, from a mead base cooked at the Cauldron and fermented. Drinking it
/// gives <see cref="GiftOfSleipnirEffect"/>.
/// </summary>
public class GiftOfSleipnir : PotionBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public GiftOfSleipnir(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "GiftOfSleipnir";

    /// <inheritdoc/>
    protected override string FullName => "Gift of Sleipnir";

    /// <inheritdoc/>
    protected override string Description => "Grants you the speed of Odin's eight-legged horse";

    /// <inheritdoc/>
    protected override string IconPath => "BrudvikWhiteHilt.Assets.GiftOfSleipnir.png";

    /// <inheritdoc/>
    protected override RequirementConfig[] MeadBaseRequirements => new[]
    {
        new RequirementConfig { Item = "Honey", Amount = 10, Recover = false },
        new RequirementConfig { Item = "Thistle", Amount = 10, Recover = false },
        new RequirementConfig { Item = "LoxMeat", Amount = 5, Recover = false }
    };

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Plains;

    /// <inheritdoc/>
    protected override SE_Stats CreateEffect()
    {
        var effect = ScriptableObject.CreateInstance<GiftOfSleipnirEffect>();
        effect.Initialize(FullName);
        effect.SetIcon(IconPath);
        return effect;
    }
}
