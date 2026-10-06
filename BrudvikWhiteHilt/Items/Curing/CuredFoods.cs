using BrudvikWhiteHilt.Items.Food;
using BrudvikWhiteHilt.Pieces.Cooking;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Curing;

/// <summary>
/// Base class for food cured on the drying rack: it has no recipe, it is hung up raw and taken down cured.
/// </summary>
public abstract class CuredFoodBase : WhiteHiltFoodBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    protected CuredFoodBase(ItemManager instance) : base(instance) { }

    /// <summary>Prefab name of what is hung on the rack.</summary>
    protected abstract string HungItem { get; }

    /// <summary>Game days it hangs, from the settings.</summary>
    protected abstract float Days { get; }

    /// <inheritdoc/>
    protected override bool HasRecipe => false;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[0];

    /// <inheritdoc/>
    protected override void AddConversions(ItemManager items)
    {
        items.AddItemConversion(new CustomItemConversion(new CookingConversionConfig
        {
            Station = DryingRack.RackPrefabName,
            FromItem = HungItem,
            ToItem = BaseName,
            CookTime = Days * CuringSettings.DaySeconds
        }));
        DryingRack.RegisterCookTime(HungItem, () => Days * CuringSettings.DaySeconds);
    }
}

/// <summary>
/// A boar ham cured on the drying rack. Hearty and long-lasting; it keeps some of the cold out.
/// </summary>
public class CuredHam : CuredFoodBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public CuredHam(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltCuredHam";

    /// <inheritdoc/>
    protected override string FullName => "Cured Ham";

    /// <inheritdoc/>
    protected override string Description => "A boar ham dried on the rack for days until it is dark and firm. Thin slices of it last all day.";

    /// <inheritdoc/>
    protected override string CopyFrom => "CookedMeat";

    /// <inheritdoc/>
    protected override string HungItem => SeasonedHam.PrefabName;

    /// <inheritdoc/>
    protected override float Days => CuringSettings.HamDays.Value;

    /// <inheritdoc/>
    protected override float Health => 46f;

    /// <inheritdoc/>
    protected override float Stamina => 24f;

    /// <inheritdoc/>
    protected override Color Tint => new(0.65f, 0.35f, 0.3f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override float DurationSeconds => 3600f;

    /// <inheritdoc/>
    protected override float Regen => 3f;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.BlackForest;

    /// <inheritdoc/>
    protected override string BuffTooltip => "Frost damage taken is a quarter less";

    /// <inheritdoc/>
    protected override void ConfigureBuff(SE_Stats effect)
    {
        effect.m_mods = new() { new HitData.DamageModPair { m_type = HitData.DamageType.Frost, m_modifier = HitData.DamageModifier.SlightlyResistant } };
    }
}

/// <summary>
/// A sausage cured on the drying rack. Light to carry and long-lasting.
/// </summary>
public class CuredSausage : CuredFoodBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public CuredSausage(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltCuredSausage";

    /// <inheritdoc/>
    protected override string FullName => "Cured Sausage";

    /// <inheritdoc/>
    protected override string Description => "A hard, dry sausage of boar and deer. It keeps for ever and goes in every pack.";

    /// <inheritdoc/>
    protected override string CopyFrom => "Sausages";

    /// <inheritdoc/>
    protected override string HungItem => RawSausage.PrefabName;

    /// <inheritdoc/>
    protected override float Days => CuringSettings.SausageDays.Value;

    /// <inheritdoc/>
    protected override float Health => 28f;

    /// <inheritdoc/>
    protected override float Stamina => 46f;

    /// <inheritdoc/>
    protected override Color Tint => new(0.6f, 0.4f, 0.35f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override float DurationSeconds => 3600f;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.BlackForest;

    /// <inheritdoc/>
    protected override string BuffTooltip => "+40 carry weight";

    /// <inheritdoc/>
    protected override void ConfigureBuff(SE_Stats effect)
    {
        effect.m_addMaxCarryWeight = 40f;
    }
}

/// <summary>
/// Coral cod dried on the rack into stockfish: the sailor's food, which lasts longest of all.
/// </summary>
public class Stockfish : CuredFoodBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public Stockfish(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltStockfish";

    /// <inheritdoc/>
    protected override string FullName => "Stockfish";

    /// <inheritdoc/>
    protected override string Description => "Cod dried hard in the wind on a rack. It keeps for years and fed every crew that sailed west.";

    /// <inheritdoc/>
    protected override string CopyFrom => "FishCooked";

    /// <inheritdoc/>
    protected override string HungItem => "Fish8";

    /// <inheritdoc/>
    protected override float Days => CuringSettings.StockfishDays.Value;

    /// <inheritdoc/>
    protected override float Health => 30f;

    /// <inheritdoc/>
    protected override float Stamina => 50f;

    /// <inheritdoc/>
    protected override Color Tint => new(0.85f, 0.8f, 0.65f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override float DurationSeconds => 4500f;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Swamp;

    /// <inheritdoc/>
    protected override string BuffTooltip => "Stamina regenerates 20% faster";

    /// <inheritdoc/>
    protected override void ConfigureBuff(SE_Stats effect)
    {
        effect.m_staminaRegenMultiplier = 1.2f;
    }
}

/// <summary>
/// Pike fermented in a tub: rakfisk. Strong stuff for a strong stomach.
/// </summary>
public class Rakfisk : WhiteHiltFoodBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public Rakfisk(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltRakfisk";

    /// <inheritdoc/>
    protected override string FullName => "Rakfisk";

    /// <inheritdoc/>
    protected override string Description => "Pike fermented in its tub until it smells to Helheim and back. Whoever keeps it down fears no poison.";

    /// <inheritdoc/>
    protected override string CopyFrom => "FishCooked";

    /// <inheritdoc/>
    protected override bool HasRecipe => false;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[0];

    /// <inheritdoc/>
    protected override float Health => 40f;

    /// <inheritdoc/>
    protected override float Stamina => 34f;

    /// <inheritdoc/>
    protected override Color Tint => new(0.7f, 0.75f, 0.6f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override float DurationSeconds => 3000f;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.BlackForest;

    /// <inheritdoc/>
    protected override string BuffTooltip => "Poison damage taken is halved";

    /// <inheritdoc/>
    protected override void ConfigureBuff(SE_Stats effect)
    {
        effect.m_mods = new() { new HitData.DamageModPair { m_type = HitData.DamageType.Poison, m_modifier = HitData.DamageModifier.Resistant } };
    }

    /// <inheritdoc/>
    protected override void AddConversions(ItemManager items)
    {
        items.AddItemConversion(new CustomItemConversion(new FermenterConversionConfig
        {
            Station = Fermenters.Fermenter,
            FromItem = RakfiskTub.PrefabName,
            ToItem = BaseName,
            ProducedItems = CuringSettings.RakfiskYield.Value
        }));
    }
}
