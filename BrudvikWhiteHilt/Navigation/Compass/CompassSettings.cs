using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Navigation.Compass;

/// <summary>
/// Config for the map compass, section "Map.Compass". Each player's own.
/// </summary>
public static class CompassSettings
{
    private const string Section = "Map.Compass";

    /// <summary>Whether the compass shows in the bottom-right corner of the minimap.</summary>
    public static ConfigEntry<bool> Minimap { get; private set; }

    /// <summary>Whether the compass shows in the top-left corner of the large map.</summary>
    public static ConfigEntry<bool> LargeMap { get; private set; }

    /// <summary>Whether the bearing in degrees shows under the compass.</summary>
    public static ConfigEntry<bool> Degrees { get; private set; }

    /// <summary>Width of the compass on the minimap, in pixels.</summary>
    public static ConfigEntry<float> MinimapSize { get; private set; }

    /// <summary>Width of the compass on the large map, in pixels.</summary>
    public static ConfigEntry<float> LargeMapSize { get; private set; }

    /// <summary>
    /// Binds the config entries and registers the English texts. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        Minimap = WhiteHiltConfig.BindLocal(Section, "Minimap", true,
            "Show a compass in the bottom-right corner of the minimap. The map is north up, so the letters stay put and the needle points where you look.");
        LargeMap = WhiteHiltConfig.BindLocal(Section, "LargeMap", true,
            "Show the compass in the top-left corner of the large map too.");
        Degrees = WhiteHiltConfig.BindLocal(Section, "Degrees", true,
            "Show the bearing in degrees under the compass: 0 is north, 90 east, 180 south and 270 west.");
        MinimapSize = WhiteHiltConfig.BindLocal(Section, "MinimapSize", 56f,
            "Width of the compass on the minimap, in pixels.", new AcceptableValueRange<float>(32f, 120f));
        LargeMapSize = WhiteHiltConfig.BindLocal(Section, "LargeMapSize", 110f,
            "Width of the compass on the large map, in pixels.", new AcceptableValueRange<float>(48f, 220f));

        Translations.AddEnglish("whitehilt_compass_n", "N");
        Translations.AddEnglish("whitehilt_compass_e", "E");
        Translations.AddEnglish("whitehilt_compass_s", "S");
        Translations.AddEnglish("whitehilt_compass_w", "W");
    }
}
