using BrudvikWhiteHilt.Monsters;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Food.RoastMistStag;

/// <summary>
/// Mist Stag Meat roasted on the iron cooking station, as hare meat is. It has no recipe: the meat is put on the spit.
/// </summary>
public class RoastMistStag : WhiteHiltFoodBase
{
    /// <summary>Prefab name of the cooking station that roasts the meat, the one that cooks hare meat.</summary>
    public const string Station = "piece_cookingstation_iron";

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public RoastMistStag(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Mistlands;

    /// <inheritdoc/>
    protected override string BaseName => WildMonsters.CookedMistStag;

    /// <inheritdoc/>
    protected override string FullName => "Roast Mist Stag";

    /// <inheritdoc/>
    protected override string Description => "Pale stag meat roasted until it smells of smoke. A meal for the long climb through the mist.";

    /// <inheritdoc/>
    protected override string CopyFrom => "CookedDeerMeat";

    /// <inheritdoc/>
    protected override bool HasRecipe => false;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[0];

    /// <inheritdoc/>
    protected override float Health => 66f;

    /// <inheritdoc/>
    protected override float Stamina => 22f;

    /// <inheritdoc/>
    protected override float Regen => 5f;

    /// <inheritdoc/>
    protected override Color Tint => new(0.85f, 0.9f, 1f);

    /// <inheritdoc/>
    protected override void AddConversions(ItemManager items)
    {
        items.AddItemConversion(new CustomItemConversion(new CookingConversionConfig
        {
            Station = Station,
            FromItem = WildMonsters.MistStagMeat,
            ToItem = BaseName,
            CookTime = WildLootSettings.RoastSeconds.Value
        }));
    }
}
