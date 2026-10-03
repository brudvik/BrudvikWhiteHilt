using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using System.Collections.Generic;

namespace BrudvikWhiteHilt.Pieces.Roofs;

/// <summary>
/// Config for the White Hilt roofs and their materials: sections "Roofs", "Roofs.&lt;Covering&gt;", "Roofs.Gable",
/// "Roofs.Garden" and "Roofs.Materials". Server-synced.
/// </summary>
public static class RoofSettings
{
    private const string Section = "Roofs";
    private const string GableSection = "Roofs.Gable";
    private const string GardenSection = "Roofs.Garden";
    private const string MaterialSection = "Roofs.Materials";

    private static readonly Dictionary<RoofCovering, CoveringEntries> coverings = new();

    /// <summary>Extra cost of a smoke hole piece on top of its covering's recipe.</summary>
    public static ConfigEntry<string> SmokeHoleExtra { get; private set; }

    /// <summary>Whether smoke hole hatches close by themselves when rain starts and open when it stops.</summary>
    public static ConfigEntry<bool> HatchFollowsRain { get; private set; }

    /// <summary>How far below a smoke hole a burning fire keeps its hatch open in the rain.</summary>
    public static ConfigEntry<float> HatchFireRange { get; private set; }

    /// <summary>How much more indoors it sounds under a turf, slate or thatch roof, 0 to 1.</summary>
    public static ConfigEntry<float> QuietRoofBonus { get; private set; }

    /// <summary>Seconds between checks whether a roof goes on below an eave, which hides its overhang.</summary>
    public static ConfigEntry<float> EaveCheckSeconds { get; private set; }

    /// <summary>Whether the gable ornaments can be built.</summary>
    public static ConfigEntry<bool> GableEnabled { get; private set; }

    /// <summary>Recipe of the gable ornaments.</summary>
    public static ConfigEntry<string> GableRecipe { get; private set; }

    /// <summary>Whether roseroot can be planted on turf roofs.</summary>
    public static ConfigEntry<bool> GardenEnabled { get; private set; }

    /// <summary>In-game minutes before planted roseroot can be picked, and between picks.</summary>
    public static ConfigEntry<float> GardenGrowMinutes { get; private set; }

    /// <summary>Roseroot one pick gives.</summary>
    public static ConfigEntry<int> GardenPickAmount { get; private set; }

    /// <summary>Chance that a felled birch or birch log also gives birch bark.</summary>
    public static ConfigEntry<float> BirchBarkChance { get; private set; }

    /// <summary>Most birch bark one tree or log gives.</summary>
    public static ConfigEntry<int> BirchBarkMax { get; private set; }

    /// <summary>Chance that digging grassland with a pickaxe gives a turf.</summary>
    public static ConfigEntry<float> TurfChance { get; private set; }

    /// <summary>How deep below the untouched ground a dig can be and still give turf.</summary>
    public static ConfigEntry<float> TurfMaxDepth { get; private set; }

    /// <summary>Biomes where digging gives turf.</summary>
    public static ConfigEntry<Heightmap.Biome> TurfBiomes { get; private set; }

    /// <summary>Chance per stone from a Mountain rock that slate comes with it.</summary>
    public static ConfigEntry<float> SlateChance { get; private set; }

    /// <summary>Chance per stone from a Mountain rock that soapstone comes with it.</summary>
    public static ConfigEntry<float> SoapstoneChance { get; private set; }

    /// <summary>Chance of a slate outcrop in each newly generated Mountain zone.</summary>
    public static ConfigEntry<float> SlateOutcropPerZone { get; private set; }

    /// <summary>Chance that harvesting barley or flax also gives straw.</summary>
    public static ConfigEntry<float> StrawChance { get; private set; }

    /// <summary>Seconds the Tar Kiln takes for one pine tar.</summary>
    public static ConfigEntry<float> TarKilnSeconds { get; private set; }

    /// <summary>Core wood the Tar Kiln holds.</summary>
    public static ConfigEntry<int> TarKilnCapacity { get; private set; }

    /// <summary>How much longer the Soapstone Hearth burns on its fuel than the vanilla hearth.</summary>
    public static ConfigEntry<float> HearthBurnMultiplier { get; private set; }

    /// <summary>Comfort the Soapstone Hearth gives.</summary>
    public static ConfigEntry<int> HearthComfort { get; private set; }

    /// <summary>
    /// Binds the config entries. Call from the plugin's Awake, before the roofs are created.
    /// </summary>
    public static void Initialize()
    {
        foreach (RoofCovering covering in RoofCoverings.All)
        {
            RoofFamily family = RoofFamily.Get(covering);
            string section = $"{Section}.{covering}";
            WhiteHiltConfig.SetSectionLabel(section, Translations.Token(family.NameKey));
            coverings[covering] = new CoveringEntries(
                WhiteHiltConfig.BindAdminOnly(section, "Enabled", true,
                    $"{family.EnglishName} can be built. Off: the pieces leave the hammer; roofs already built stay."),
                WhiteHiltConfig.BindAdminOnly(section, "Recipe", family.DefaultRecipe,
                    $"What one piece of {family.EnglishName} costs, as Prefab:Amount, comma separated. Unknown prefabs are skipped."),
                WhiteHiltConfig.BindAdminOnly(section, "Health", family.DefaultHealth,
                    $"Health of one piece of {family.EnglishName}. Vanilla thatch has 400.", new AcceptableValueRange<float>(50f, 5000f)));
        }

        SmokeHoleExtra = WhiteHiltConfig.BindAdminOnly(Section, "SmokeHoleExtra", "Wood:2",
            "What a smoke hole piece costs on top of its covering's recipe (the frame and the hatch), as Prefab:Amount, comma separated.");
        HatchFollowsRain = WhiteHiltConfig.BindAdminOnly(Section, "HatchFollowsRain", true,
            "Smoke hole hatches close by themselves when rain starts and open when it stops, unless a fire burns below.");
        HatchFireRange = WhiteHiltConfig.BindAdminOnly(Section, "HatchFireRange", 6f,
            "How far below a smoke hole, in metres, a burning fire keeps its hatch open in the rain.", new AcceptableValueRange<float>(0f, 20f));
        QuietRoofBonus = WhiteHiltConfig.BindAdminOnly(Section, "QuietRoofBonus", 0.25f,
            "How much more the weather is muffled under turf, slate, straw and reed (0 to 1, on top of the usual muffling under a roof).",
            new AcceptableValueRange<float>(0f, 1f));
        EaveCheckSeconds = WhiteHiltConfig.BindAdminOnly(Section, "EaveCheckSeconds", 3f,
            "Seconds between checks whether the roof goes on below an eave; there the overhang and the turf log are hidden.",
            new AcceptableValueRange<float>(0.5f, 30f));

        WhiteHiltConfig.SetSectionLabel(GableSection, Translations.Token("whitehilt_roof_gable"));
        GableEnabled = WhiteHiltConfig.BindAdminOnly(GableSection, "Enabled", true,
            "The dragon gables can be built. Off: they leave the hammer; gables already built stay.");
        GableRecipe = WhiteHiltConfig.BindAdminOnly(GableSection, "Recipe", "FineWood:4, WhiteHilt_LindormScale:2, WhiteHilt_PineTar:1",
            "What one dragon gable costs, as Prefab:Amount, comma separated.");

        GardenEnabled = WhiteHiltConfig.BindAdminOnly(GardenSection, "Enabled", true,
            "Roseroot can be planted on turf roofs (use a Roseroot on the roof).");
        GardenGrowMinutes = WhiteHiltConfig.BindAdminOnly(GardenSection, "GrowMinutes", 60f,
            "In-game minutes before roseroot planted on a turf roof can be picked, and between picks.", new AcceptableValueRange<float>(1f, 10000f));
        GardenPickAmount = WhiteHiltConfig.BindAdminOnly(GardenSection, "PickAmount", 2,
            "Roseroot one pick from a turf roof gives.", new AcceptableValueRange<int>(1, 20));

        BirchBarkChance = WhiteHiltConfig.BindAdminOnly(MaterialSection, "BirchBarkChance", 0.6f,
            "Chance that a felled birch, or a birch log split up, also gives birch bark. 0 turns it off.", new AcceptableValueRange<float>(0f, 1f));
        BirchBarkMax = WhiteHiltConfig.BindAdminOnly(MaterialSection, "BirchBarkMax", 2,
            "Most birch bark one birch or log gives (at least 1).", new AcceptableValueRange<int>(1, 10));
        TurfChance = WhiteHiltConfig.BindAdminOnly(MaterialSection, "TurfChance", 1f,
            "Chance that digging grassland with a pickaxe gives a turf. 0 turns it off.", new AcceptableValueRange<float>(0f, 1f));
        TurfMaxDepth = WhiteHiltConfig.BindAdminOnly(MaterialSection, "TurfMaxDepth", 0.5f,
            "Turf only lies on top: digging deeper than this many metres below the untouched ground gives none.", new AcceptableValueRange<float>(0.1f, 5f));
        TurfBiomes = WhiteHiltConfig.BindAdminOnly(MaterialSection, "TurfBiomes", Heightmap.Biome.Meadows | Heightmap.Biome.BlackForest | Heightmap.Biome.Plains,
            "Biomes where digging gives turf.");
        SlateChance = WhiteHiltConfig.BindAdminOnly(MaterialSection, "SlateChance", 0.25f,
            "Chance, for each stone a Mountain rock drops, that a slate comes with it. 0 turns it off.", new AcceptableValueRange<float>(0f, 1f));
        SoapstoneChance = WhiteHiltConfig.BindAdminOnly(MaterialSection, "SoapstoneChance", 0.08f,
            "Chance, for each stone a Mountain rock drops, that a soapstone comes with it. 0 turns it off.", new AcceptableValueRange<float>(0f, 1f));
        SlateOutcropPerZone = WhiteHiltConfig.BindAdminOnly(MaterialSection, "SlateOutcropPerZone", 0.12f,
            "Chance of a slate outcrop in each Mountain zone: in newly generated land, and once in land generated before the outcrops came ([OldLand]).", new AcceptableValueRange<float>(0f, 1f));
        StrawChance = WhiteHiltConfig.BindAdminOnly(MaterialSection, "StrawChance", 1f,
            "Chance that harvesting barley or flax, wild or grown, also gives straw. 0 turns it off.", new AcceptableValueRange<float>(0f, 1f));
        TarKilnSeconds = WhiteHiltConfig.BindAdminOnly(MaterialSection, "TarKilnSeconds", 40f,
            "Seconds the Tar Kiln takes to turn one core wood into pine tar.", new AcceptableValueRange<float>(1f, 3600f));
        TarKilnCapacity = WhiteHiltConfig.BindAdminOnly(MaterialSection, "TarKilnCapacity", 25,
            "Core wood the Tar Kiln holds.", new AcceptableValueRange<int>(1, 100));
        HearthBurnMultiplier = WhiteHiltConfig.BindAdminOnly(MaterialSection, "HearthBurnMultiplier", 2f,
            "How much longer the Soapstone Hearth burns on its wood than the vanilla hearth: soapstone holds the heat.",
            new AcceptableValueRange<float>(1f, 10f));
        HearthComfort = WhiteHiltConfig.BindAdminOnly(MaterialSection, "HearthComfort", 3,
            "Comfort the Soapstone Hearth gives (the vanilla hearth gives 2).", new AcceptableValueRange<int>(0, 10));
    }

    /// <summary>
    /// Whether a covering can be built.
    /// </summary>
    /// <param name="covering">The covering.</param>
    /// <returns>True if switched on.</returns>
    public static bool IsEnabled(RoofCovering covering)
    {
        return !coverings.TryGetValue(covering, out CoveringEntries entries) || entries.Enabled.Value;
    }

    /// <summary>
    /// The recipe of a covering.
    /// </summary>
    /// <param name="covering">The covering.</param>
    /// <returns>Prefab:Amount pairs, comma separated.</returns>
    public static string Recipe(RoofCovering covering)
    {
        return coverings.TryGetValue(covering, out CoveringEntries entries) ? entries.Recipe.Value : RoofFamily.Get(covering).DefaultRecipe;
    }

    /// <summary>
    /// The health of one piece of a covering.
    /// </summary>
    /// <param name="covering">The covering.</param>
    /// <returns>The health.</returns>
    public static float Health(RoofCovering covering)
    {
        return coverings.TryGetValue(covering, out CoveringEntries entries) ? entries.Health.Value : RoofFamily.Get(covering).DefaultHealth;
    }

    private sealed class CoveringEntries
    {
        public CoveringEntries(ConfigEntry<bool> enabled, ConfigEntry<string> recipe, ConfigEntry<float> health)
        {
            Enabled = enabled;
            Recipe = recipe;
            Health = health;
        }

        public ConfigEntry<bool> Enabled { get; }

        public ConfigEntry<string> Recipe { get; }

        public ConfigEntry<float> Health { get; }
    }
}
