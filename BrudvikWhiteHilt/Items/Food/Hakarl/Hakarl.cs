using BrudvikWhiteHilt.Monsters;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Food.Hakarl;

/// <summary>
/// Shark Meat fermented in a fermenter: hákarl. Raw shark is poison; left to ferment, it feeds a crew for an hour.
/// </summary>
public class Hakarl : WhiteHiltFoodBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public Hakarl(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Swamp;

    /// <inheritdoc/>
    protected override string BaseName => WildMonsters.Hakarl;

    /// <inheritdoc/>
    protected override string FullName => "Hákarl";

    /// <inheritdoc/>
    protected override string Description => "Greenland shark, buried and left to rot until it is safe to eat. It smells of ammonia, and it lasts all day.";

    /// <inheritdoc/>
    protected override string CopyFrom => "FishCooked";

    /// <inheritdoc/>
    protected override bool HasRecipe => false;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[0];

    /// <inheritdoc/>
    protected override float Health => 46f;

    /// <inheritdoc/>
    protected override float Stamina => 40f;

    /// <inheritdoc/>
    protected override float DurationSeconds => 3600f;

    /// <inheritdoc/>
    protected override float Regen => 3f;

    /// <inheritdoc/>
    protected override Color Tint => new(0.85f, 0.88f, 0.8f);

    /// <inheritdoc/>
    protected override void AddConversions(ItemManager items)
    {
        items.AddItemConversion(new CustomItemConversion(new FermenterConversionConfig
        {
            Station = Fermenters.Fermenter,
            FromItem = WildMonsters.SharkMeat,
            ToItem = BaseName,
            ProducedItems = WildLootSettings.HakarlYield.Value
        }));
    }
}
