using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Navigation.Weather;

/// <summary>
/// Config for the weather forecast and the storm warning, section "Navigation.Forecast". The bell is each player's own.
/// </summary>
public static class ForecastSettings
{
    private const string Section = "Navigation.Forecast";

    /// <summary>Whether the forecast panel shows on the large map.</summary>
    public static ConfigEntry<bool> Forecast { get; private set; }

    /// <summary>Metres from a map table or Cartographer's Desk within which the forecast shows.</summary>
    public static ConfigEntry<float> Range { get; private set; }

    /// <summary>Periods after the current one foretold at Exploration 0.</summary>
    public static ConfigEntry<int> PeriodsAtLevelZero { get; private set; }

    /// <summary>Periods after the current one foretold at Exploration 100.</summary>
    public static ConfigEntry<int> PeriodsAtLevel100 { get; private set; }

    /// <summary>Whether those aboard a ship with a Navigator's Table are warned of a coming storm.</summary>
    public static ConfigEntry<bool> StormWarning { get; private set; }

    /// <summary>Minutes before a storm that the warning comes.</summary>
    public static ConfigEntry<float> StormWarningMinutes { get; private set; }

    /// <summary>Whether the storm warning rings the ship's bell.</summary>
    public static ConfigEntry<bool> StormBell { get; private set; }

    /// <summary>
    /// Binds the config entries and registers the English texts. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        Forecast = WhiteHiltConfig.BindAdminOnly(Section, "Forecast", true,
            "The large map shows a weather forecast near a map table or a Cartographer's Desk, and aboard a ship with a Navigator's Table.");
        Range = WhiteHiltConfig.BindAdminOnly(Section, "Range", 10f,
            "Metres from a map table or a Cartographer's Desk within which the forecast shows.", new AcceptableValueRange<float>(2f, 50f));
        PeriodsAtLevelZero = WhiteHiltConfig.BindAdminOnly(Section, "PeriodsAtLevelZero", 1,
            "Weather periods (about 11 minutes each) after the current one foretold at Exploration 0.", new AcceptableValueRange<int>(0, 10));
        PeriodsAtLevel100 = WhiteHiltConfig.BindAdminOnly(Section, "PeriodsAtLevel100", 4,
            "Weather periods after the current one foretold at Exploration 100; levels in between get a share.", new AcceptableValueRange<int>(0, 10));
        StormWarning = WhiteHiltConfig.BindAdminOnly(Section, "StormWarning", true,
            "Everyone aboard a ship with a Navigator's Table is told when a storm is coming.");
        StormWarningMinutes = WhiteHiltConfig.BindAdminOnly(Section, "StormWarningMinutes", 3f,
            "Minutes before a storm that the warning comes.", new AcceptableValueRange<float>(0.5f, 11f));
        StormBell = WhiteHiltConfig.BindLocal(Section, "StormBell", true,
            "The storm warning rings the ship's bell.");

        WeatherForecast.RegisterEnglish();
        Translations.AddEnglish("whitehilt_forecast_title", "Weather, {0}");
        Translations.AddEnglish("whitehilt_forecast_now", "Now");
        Translations.AddEnglish("whitehilt_forecast_in", "In {0} min");
        Translations.AddEnglish("whitehilt_forecast_forced", "Something else rules the weather here now.");
        Translations.AddEnglish("whitehilt_forecast_more", "More Exploration foretells further ahead.");
        Translations.AddEnglish("whitehilt_storm_coming", "A storm is coming in {0} min!");
    }

    /// <summary>
    /// How many periods after the current one a player foretells.
    /// </summary>
    /// <param name="level">The player's Exploration level.</param>
    /// <returns>The number of periods.</returns>
    public static int PeriodsAhead(int level)
    {
        return UnityEngine.Mathf.RoundToInt(UnityEngine.Mathf.Lerp(PeriodsAtLevelZero.Value, PeriodsAtLevel100.Value, level / 100f));
    }
}
