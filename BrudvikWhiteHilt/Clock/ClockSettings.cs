using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Clock;

/// <summary>
/// Config for the clock at the top of the screen, section "Clock". Only whether it is allowed at all is the server's.
/// </summary>
public static class ClockSettings
{
    private const string Section = "Clock";

    /// <summary>Whether the server allows the clock.</summary>
    public static ConfigEntry<bool> AllowClock { get; private set; }

    /// <summary>Whether the player shows the clock.</summary>
    public static ConfigEntry<bool> Enabled { get; private set; }

    /// <summary>Whether the day number is shown before the time.</summary>
    public static ConfigEntry<bool> ShowDay { get; private set; }

    /// <summary>Whether an icon for the weather is shown before the time.</summary>
    public static ConfigEntry<bool> ShowWeather { get; private set; }

    /// <summary>The time is rounded down to this many minutes.</summary>
    public static ConfigEntry<int> RoundMinutes { get; private set; }

    /// <summary>Hours before nightfall the player is warned; 0 for no warning.</summary>
    public static ConfigEntry<float> DuskWarningHours { get; private set; }

    /// <summary>Size of the clock's text.</summary>
    public static ConfigEntry<int> FontSize { get; private set; }

    /// <summary>Distance from the top of the screen, in pixels.</summary>
    public static ConfigEntry<float> OffsetY { get; private set; }

    /// <summary>Shows or hides the clock.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyToggle { get; private set; }

    /// <summary>
    /// Binds the config entries. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        AllowClock = WhiteHiltConfig.BindAdminOnly(Section, "AllowClock", true, "Players may show the clock above the upper-left hotbar.");
        Enabled = WhiteHiltConfig.BindLocal(Section, "Enabled", true,
            "Show the time of day (24 hours) above the upper-left hotbar. Hidden in the inventory, in build mode, on the large map and in menus.");
        ShowDay = WhiteHiltConfig.BindLocal(Section, "ShowDay", true, "Show the day number before the time.");
        ShowWeather = WhiteHiltConfig.BindLocal(Section, "ShowWeather", true, "Show an icon for the weather before the time.");
        RoundMinutes = WhiteHiltConfig.BindLocal(Section, "RoundMinutes", 1, "Round the time down to this many minutes: 1, 5, 10 or 15.");
        DuskWarningHours = WhiteHiltConfig.BindLocal(Section, "DuskWarningHours", 2f,
            "Warn this many in-game hours before night falls at 18:00; the clock turns orange until dark. 0 turns the warning off.");
        FontSize = WhiteHiltConfig.BindLocal(Section, "FontSize", 20, "Size of the clock's text.");
        OffsetY = WhiteHiltConfig.BindLocal(Section, "OffsetY", 12f, "Distance from the top of the screen, in pixels.");
        KeyToggle = WhiteHiltConfig.BindLocal(Section + ".Keys", "ToggleClock", KeyboardShortcut.Empty, "Show or hide the clock.");

        Translations.AddEnglish("whitehilt_clock_day", "Day {0}");
        Translations.AddEnglish("msg_whitehilt_clock_dusk", "Night falls in {0} hours");
        Translations.AddEnglish("msg_whitehilt_clock_dusk_one", "Night falls in an hour");
    }
}
