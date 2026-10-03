using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Guestbook;

/// <summary>
/// Config and texts for the guestbook: how far around it it notes visitors, building and raids, and how it merges
/// and keeps its entries.
/// </summary>
public static class GuestbookSettings
{
    private const string Section = "Guestbook";

    /// <summary>Radius the guestbook watches, in metres.</summary>
    public static ConfigEntry<float> Radius { get; private set; }

    /// <summary>Game minutes a visitor must have been away to be noted again.</summary>
    public static ConfigEntry<float> VisitGapMinutes { get; private set; }

    /// <summary>Game minutes within which building by the same player is noted as one entry.</summary>
    public static ConfigEntry<float> MergeMinutes { get; private set; }

    /// <summary>Seconds without enemies before a raid is noted as over.</summary>
    public static ConfigEntry<float> RaidQuietSeconds { get; private set; }

    /// <summary>Entries a guestbook keeps.</summary>
    public static ConfigEntry<int> MaxEntries { get; private set; }

    /// <summary>
    /// Binds the config entries and adds the English texts. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        Radius = WhiteHiltConfig.BindAdminOnly(Section, "Radius", 40f, "Radius a guestbook watches, in metres.", new AcceptableValueRange<float>(10f, 150f));
        VisitGapMinutes = WhiteHiltConfig.BindAdminOnly(Section, "VisitGapMinutes", 720f,
            "Game minutes a visitor must have been away to be noted again (a game day is 1440 game minutes, 30 real minutes).", new AcceptableValueRange<float>(1f, 4320f));
        MergeMinutes = WhiteHiltConfig.BindAdminOnly(Section, "MergeMinutes", 240f,
            "Game minutes within which building or tearing down the same piece by the same player is one entry with a count.", new AcceptableValueRange<float>(0f, 1440f));
        RaidQuietSeconds = WhiteHiltConfig.BindAdminOnly(Section, "RaidQuietSeconds", 60f, "Seconds without enemies before a raid is noted as over.",
            new AcceptableValueRange<float>(10f, 600f));
        MaxEntries = WhiteHiltConfig.BindAdminOnly(Section, "MaxEntries", 100, "Entries a guestbook keeps; the oldest go first.", new AcceptableValueRange<int>(10, 500));

        Translations.AddEnglish("whitehilt_guest_read", "Read");
        Translations.AddEnglish("whitehilt_guest_title", "Guestbook");
        Translations.AddEnglish("whitehilt_guest_empty", "Nothing has happened here yet.");
        Translations.AddEnglish("whitehilt_guest_time", "Day {0} {1}");
        Translations.AddEnglish("whitehilt_guest_visit", "{0} came by");
        Translations.AddEnglish("whitehilt_guest_built", "{0} built {1}");
        Translations.AddEnglish("whitehilt_guest_removed", "{0} tore down {1}");
        Translations.AddEnglish("whitehilt_guest_raid", "Raid: {1}");
        Translations.AddEnglish("whitehilt_guest_raidend", "The raid was beaten off after {1} min");
        Translations.AddEnglish("whitehilt_guest_close", "Close");
    }
}
