using BrudvikWhiteHilt.Items.Roofing;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Coatings.Whetstone;

/// <summary>
/// A whetstone of mountain slate, like the ones the Norse quarried and traded: hones the edge for more slash and pierce
/// damage for a number of hits.
/// </summary>
public class Whetstone : WeaponCoatingBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public Whetstone(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltWhetstone";

    /// <inheritdoc/>
    protected override string FullName => "Whetstone";

    /// <inheritdoc/>
    protected override string Description => "A bar of fine mountain slate on a cord. A few strokes and the edge bites deeper. Use it from the inventory.";

    /// <inheritdoc/>
    protected override string EffectTooltip => "Honed edge: +{0}% slash and pierce damage for {1} hits";

    /// <inheritdoc/>
    protected override string ModelName => "whetstone";

    /// <inheritdoc/>
    protected override string CraftingStation => CraftingStations.Stonecutter;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = RoofMaterials.Slate, Amount = 2, Recover = false },
        new() { Item = "LeatherScraps", Amount = 1, Recover = false }
    };

    /// <inheritdoc/>
    protected override float DefaultStrength => 0.15f;

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Mountain;

    /// <inheritdoc/>
    protected override void Configure(WeaponCoatingEffect coating, float value)
    {
        coating.EdgeBonus = value;
        coating.AddedShare = 0f;
    }
}
