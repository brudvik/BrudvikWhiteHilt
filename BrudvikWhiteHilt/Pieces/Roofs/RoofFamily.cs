using BrudvikWhiteHilt.Helpers;

namespace BrudvikWhiteHilt.Pieces.Roofs;

/// <summary>
/// Name, description, default recipe and properties of each roof covering.
/// </summary>
public sealed class RoofFamily
{
    private static readonly RoofFamily[] families =
    {
        new(RoofCovering.Turf, "Turf roof",
            "Sod over layers of birch bark on roof boards, held by a turf log along the eave, as on most Viking houses. It does not burn, keeps the weather out best of all, and roseroot can be planted on it.",
            "Wood:2, WhiteHilt_BirchBark:1, WhiteHilt_Turf:2", 600f, fireproof: true, quiet: true),
        new(RoofCovering.Reed, "Reed thatch",
            "Thick thatch of swamp reed, grey with age, as on the houses of the southern lands. It burns, but keeps the weather out well.",
            "Wood:1, WhiteHiltReed:3", 450f, fireproof: false, quiet: true),
        new(RoofCovering.Shingle, "Shingle roof",
            "Pine shingles split by hand and brushed with pine tar, with ridge boards on top, as on the stave churches.",
            "FineWood:2, WhiteHilt_PineTar:1", 500f, fireproof: false, quiet: false),
        new(RoofCovering.ScaleShingle, "Scale shingle roof",
            "Tarred shingles with rounded ends that lie like the scales of a dragon, as on the finest stave churches.",
            "FineWood:2, WhiteHilt_PineTar:1, Resin:1", 500f, fireproof: false, quiet: false),
        new(RoofCovering.Slate, "Slate roof",
            "Heavy slabs of slate laid in courses on roof boards, as in the fjords of the west. It does not burn, keeps the weather out well and is the strongest roof of all.",
            "Wood:2, WhiteHilt_Slate:3", 800f, fireproof: true, quiet: true),
        new(RoofCovering.Straw, "Straw thatch",
            "Thick, golden thatch of barley and flax straw. It burns, but keeps the weather out well.",
            "Wood:1, WhiteHilt_Straw:3", 450f, fireproof: false, quiet: true)
    };

    private RoofFamily(RoofCovering covering, string englishName, string englishDescription, string defaultRecipe, float defaultHealth, bool fireproof, bool quiet)
    {
        Covering = covering;
        EnglishName = englishName;
        EnglishDescription = englishDescription;
        DefaultRecipe = defaultRecipe;
        DefaultHealth = defaultHealth;
        Fireproof = fireproof;
        Quiet = quiet;
    }

    /// <summary>The covering.</summary>
    public RoofCovering Covering { get; }

    /// <summary>Name in English, e.g. "Turf roof".</summary>
    public string EnglishName { get; }

    /// <summary>Description in English.</summary>
    public string EnglishDescription { get; }

    /// <summary>Recipe of one piece, as Prefab:Amount pairs.</summary>
    public string DefaultRecipe { get; }

    /// <summary>Health of one piece.</summary>
    public float DefaultHealth { get; }

    /// <summary>True if it does not burn.</summary>
    public bool Fireproof { get; }

    /// <summary>True if the weather sounds more muffled under it.</summary>
    public bool Quiet { get; }

    /// <summary>Translation key of the name.</summary>
    public string NameKey => $"whitehilt_roof_{RoofCoverings.Id(Covering)}";

    /// <summary>
    /// Gets a covering's family.
    /// </summary>
    /// <param name="covering">The covering.</param>
    /// <returns>The family.</returns>
    public static RoofFamily Get(RoofCovering covering)
    {
        foreach (RoofFamily family in families)
        {
            if (family.Covering == covering)
            {
                return family;
            }
        }

        return families[0];
    }

    /// <summary>
    /// Registers the English names of the coverings and shapes. Call from the plugin's Awake.
    /// </summary>
    public static void RegisterTranslations()
    {
        foreach (RoofFamily family in families)
        {
            Translations.AddEnglishNameAndDescription(family.NameKey, family.EnglishName, family.EnglishDescription);
        }

        Translations.AddEnglish("whitehilt_roofshape_ridge", "ridge");
        Translations.AddEnglish("whitehilt_roofshape_icorner", "inner corner");
        Translations.AddEnglish("whitehilt_roofshape_ocorner", "outer corner");
        Translations.AddEnglish("whitehilt_roofshape_smokehole", "smoke hole");
        Translations.AddEnglish("whitehilt_roofshape_smokehole_description",
            "A smoke hole (ljore) with a hatch lets the smoke of a fire below out. Use it to open or close the hatch.");
        Translations.AddEnglishNameAndDescription("whitehilt_roof_gable", "Dragon gable",
            "Crossed barge boards with carved dragon heads for the end of a ridge, as on the stave churches. Place it on the end of a ridge.");
        Translations.AddEnglish("whitehilt_roof_hatch_open", "Close the hatch");
        Translations.AddEnglish("whitehilt_roof_hatch_closed", "Open the hatch");
        Translations.AddEnglish("whitehilt_roof_garden_plant", "Plant roseroot");
        Translations.AddEnglish("whitehilt_roof_garden_growing", "Roseroot growing");
        Translations.AddEnglish("whitehilt_roof_garden_pick", "Pick roseroot");
        Translations.AddEnglish("whitehilt_roof_garden_planted", "Roseroot planted on the roof");
    }
}
