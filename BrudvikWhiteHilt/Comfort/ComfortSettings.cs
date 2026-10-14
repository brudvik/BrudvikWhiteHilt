using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Comfort;

/// <summary>
/// Config entries for comfort over a whole room: the Husvette, the book of the hall and Homecraft
/// (docs/plans/husvette.md). All are admin-only and synced from the server.
/// </summary>
public static class ComfortSettings
{
    private const string Section = "Comfort";

    /// <summary>Whether the Husvette, the book and Homecraft widen comfort at all.</summary>
    public static ConfigEntry<bool> Enabled { get; private set; }

    /// <summary>Reach of a Husvette, in metres.</summary>
    public static ConfigEntry<float> HusvetteRadius { get; private set; }

    /// <summary>Whether a burning fire within a Husvette's reach is warm enough to rest by.</summary>
    public static ConfigEntry<bool> HusvetteWarmth { get; private set; }

    /// <summary>Metres the book of the hall adds to a player's own reach.</summary>
    public static ConfigEntry<float> BookBonus { get; private set; }

    /// <summary>The book's price at Hildir and the trading posts.</summary>
    public static ConfigEntry<int> BookPrice { get; private set; }

    /// <summary>Metres Homecraft adds to a player's own reach at level 100.</summary>
    public static ConfigEntry<float> SkillBonus { get; private set; }

    /// <summary>Most a player's own reach can be, in metres.</summary>
    public static ConfigEntry<float> PersonalMax { get; private set; }

    /// <summary>Homecraft experience for every 10 seconds of resting.</summary>
    public static ConfigEntry<float> RestExperience { get; private set; }

    /// <summary>Homecraft experience per point of comfort of a piece built.</summary>
    public static ConfigEntry<float> BuildExperience { get; private set; }

    /// <summary>
    /// Binds the entries. Call once from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        Enabled = WhiteHiltConfig.BindAdminOnly(Section, "Enabled", true,
            "The Husvette, the book of the hall and Homecraft widen the reach of comfort. Off: comfort counts as in the game, " +
            "from 10 m around you.");
        HusvetteRadius = WhiteHiltConfig.BindAdminOnly(Section, "HusvetteRadius", 30f,
            "Comfort pieces within this many metres of a Husvette count for every player under a roof within the same reach.",
            new AcceptableValueRange<float>(5f, 100f));
        HusvetteWarmth = WhiteHiltConfig.BindAdminOnly(Section, "HusvetteWarmth", true,
            "A burning fire within a Husvette's reach is warm enough to rest by, for players under a roof within it.");
        BookBonus = WhiteHiltConfig.BindAdminOnly(Section, "BookBonus", 10f,
            "Metres the Book of the Hall adds to a player's own comfort reach (10 m in the game).", new AcceptableValueRange<float>(0f, 100f));
        BookPrice = WhiteHiltConfig.BindAdminOnly(Section, "BookPrice", 300,
            "Coins the Book of the Hall costs at Hildir and the trading posts. Needs a restart.", new AcceptableValueRange<int>(0, 10000));
        SkillBonus = WhiteHiltConfig.BindAdminOnly(Section, "SkillBonus", 20f,
            "Metres Homecraft adds to a player's own comfort reach at level 100, in proportion below.", new AcceptableValueRange<float>(0f, 100f));
        PersonalMax = WhiteHiltConfig.BindAdminOnly(Section, "PersonalMax", 40f,
            "Most a player's own comfort reach can be, in metres (by default the Husvette's reach plus 10).", new AcceptableValueRange<float>(10f, 200f));
        RestExperience = WhiteHiltConfig.BindAdminOnly(Section, "RestExperience", 1f,
            "Homecraft experience for every 10 seconds a player rests.", new AcceptableValueRange<float>(0f, 20f));
        BuildExperience = WhiteHiltConfig.BindAdminOnly(Section, "BuildExperience", 2f,
            "Homecraft experience per point of comfort of a piece the player builds.", new AcceptableValueRange<float>(0f, 50f));
    }
}
