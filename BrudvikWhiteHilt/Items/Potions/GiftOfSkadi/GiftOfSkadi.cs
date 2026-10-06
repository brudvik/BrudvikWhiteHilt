using BrudvikWhiteHilt.Items.Foraging.Crowberries;
using BrudvikWhiteHilt.Items.Foraging.Roseroot;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfSkadi;

/// <summary>
/// The Gift of Skadi potion: brewed like a mead, from a mead base cooked at the Cauldron and fermented. Drinking it
/// gives <see cref="GiftOfSkadiEffect"/>.
/// </summary>
public class GiftOfSkadi : PotionBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public GiftOfSkadi(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "GiftOfSkadi";

    /// <inheritdoc/>
    protected override string FullName => "Gift of Skadi";

    /// <inheritdoc/>
    protected override string Description => "Grants you the blessing of the winter goddess";

    /// <inheritdoc/>
    protected override string IconPath => "BrudvikWhiteHilt.Assets.GiftOfSkadi.png";

    /// <inheritdoc/>
    protected override RequirementConfig[] MeadBaseRequirements => new[]
    {
        new RequirementConfig { Item = Crowberries.PrefabName, Amount = 5, Recover = false },
        new RequirementConfig { Item = Roseroot.PrefabName, Amount = 5, Recover = false },
        new RequirementConfig { Item = "Mushroom", Amount = 5, Recover = false }
    };

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Swamp;

    /// <inheritdoc/>
    protected override SE_Stats CreateEffect()
    {
        var effect = ScriptableObject.CreateInstance<GiftOfSkadiEffect>();
        effect.Initialize(FullName);
        effect.SetIcon(IconPath);
        return effect;
    }
}
