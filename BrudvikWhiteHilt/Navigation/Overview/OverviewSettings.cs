using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Navigation.Overview;

/// <summary>
/// Config and texts for the exploration overview on the large map, section "Map.Overview". Server-synced.
/// </summary>
public static class OverviewSettings
{
    private const string Section = "Map.Overview";

    /// <summary>Whether the overview can be opened on the large map.</summary>
    public static ConfigEntry<bool> Overview { get; private set; }

    /// <summary>Exploration level needed to see the overview.</summary>
    public static ConfigEntry<int> Level { get; private set; }

    /// <summary>
    /// Binds the config entries and registers the English texts. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        Overview = WhiteHiltConfig.BindAdminOnly(Section, "Overview", true,
            "The large map has an overview of how much of each biome you have uncovered.");
        Level = WhiteHiltConfig.BindAdminOnly(Section, "Level", 25,
            "Exploration level needed to see the overview.", new AcceptableValueRange<int>(0, 100));

        Translations.AddEnglish("whitehilt_overview_title", "Uncovered");
        Translations.AddEnglish("whitehilt_overview_locked", "Exploration {0} is needed");
        Translations.AddEnglish("whitehilt_overview_measuring", "Measuring the map... {0}%");
        Translations.AddEnglish("whitehilt_overview_world", "The whole world");
        Translations.AddEnglish("whitehilt_overview_finds", "Found");
    }
}
