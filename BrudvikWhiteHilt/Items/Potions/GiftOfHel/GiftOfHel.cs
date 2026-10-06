using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Monsters;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfHel;

/// <summary>
/// The Gift of Hel potion: brewed like a mead, from a mead base cooked at the Cauldron and fermented. Drinking it gives
/// <see cref="GiftOfHelEffect"/>.
/// </summary>
public class GiftOfHel : PotionBase
{
    /// <summary>
    /// Localization key for the message shown when Hel prevents a death.
    /// </summary>
    public const string SparedMessageKey = "se_giftofhel_spared";

    /// <summary>
    /// Constructor for the Gift of Hel potion.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public GiftOfHel(ItemManager instance) : base(instance)
    {
        Translations.AddEnglish(SparedMessageKey, "Hel has spared you from death!");
    }

    /// <inheritdoc/>
    protected override string BaseName => "GiftOfHel";

    /// <inheritdoc/>
    protected override string FullName => "Gift of Hel";

    /// <inheritdoc/>
    protected override string Description => "Grants you a second chance from the goddess of the underworld";

    /// <inheritdoc/>
    protected override string IconPath => "BrudvikWhiteHilt.Assets.GiftOfHel.png";

    /// <inheritdoc/>
    protected override RequirementConfig[] MeadBaseRequirements => new[]
    {
        new RequirementConfig { Item = MonsterRegistry.GlandName, Amount = 2, Recover = false },
        new RequirementConfig { Item = "Blueberries", Amount = 5, Recover = false },
        new RequirementConfig { Item = "Dandelion", Amount = 5, Recover = false }
    };

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Plains;

    /// <inheritdoc/>
    protected override SE_Stats CreateEffect()
    {
        var effect = ScriptableObject.CreateInstance<GiftOfHelEffect>();
        effect.Initialize(FullName);
        effect.SetIcon(IconPath);
        return effect;
    }
}
