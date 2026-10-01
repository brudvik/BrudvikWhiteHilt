using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrudvikWhiteHilt.Navigation.Dowsing;

/// <summary>
/// Config and texts for the Stone Dowser, section "Gear.WhiteHiltStoneDowser". All of it is server-synced.
/// </summary>
public static class StoneDowsingSettings
{
    private const string Section = "Gear.WhiteHiltStoneDowser";

    private static string parsedItems;
    private static HashSet<string> items = new();
    private static string parsedLocations;
    private static HashSet<string> locations = new();

    /// <summary>How far, in metres, the dowser looks for a clearing with rocks left.</summary>
    public static ConfigEntry<float> SearchRadius { get; private set; }

    /// <summary>Seconds between the dowser's questions to the server.</summary>
    public static ConfigEntry<float> RefreshSeconds { get; private set; }

    /// <summary>Metres within which the dowser pings toward a single rock.</summary>
    public static ConfigEntry<float> PingRange { get; private set; }

    /// <summary>Whether the clearing is marked on the map.</summary>
    public static ConfigEntry<bool> ShowPin { get; private set; }

    /// <summary>Comma-separated item prefabs whose pickables the dowser finds.</summary>
    public static ConfigEntry<string> Items { get; private set; }

    /// <summary>Comma-separated location prefabs the dowser leads to.</summary>
    public static ConfigEntry<string> Locations { get; private set; }

    /// <summary>
    /// Binds the config entries and registers the English texts. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        SearchRadius = WhiteHiltConfig.BindAdminOnly(Section, "SearchRadius", 3000f,
            "How far, in metres, the Stone Dowser looks for a clearing that still has rocks.", new AcceptableValueRange<float>(100f, 20000f));
        RefreshSeconds = WhiteHiltConfig.BindAdminOnly(Section, "RefreshSeconds", 30f,
            "Seconds between each time the Stone Dowser looks for the nearest clearing.", new AcceptableValueRange<float>(5f, 600f));
        PingRange = WhiteHiltConfig.BindAdminOnly(Section, "PingRange", 40f,
            "Metres within which the Stone Dowser pings toward the nearest rock, faster the closer you come.", new AcceptableValueRange<float>(5f, 150f));
        ShowPin = WhiteHiltConfig.BindAdminOnly(Section, "ShowPin", true,
            "Mark the clearing on the map. Off: the dowser only tells the direction and distance.");
        Items = WhiteHiltConfig.BindAdminOnly(Section, "Items", "StoneRock",
            "Comma-separated item prefabs; pickables that give them are what the dowser counts and pings toward. StoneRock is the rock the Mysterious Rock is made from.");
        Locations = WhiteHiltConfig.BindAdminOnly(Section, "Locations", "BigRockClearing",
            "Comma-separated location prefabs the dowser leads to, when they still have rocks.");

        Translations.AddEnglish("whitehilt_dowser_pin", "Rocks");
        Translations.AddEnglish("msg_whitehilt_dowser_found", "The Stone Dowser tugs {0}: rocks about {1} m away");
        Translations.AddEnglish("msg_whitehilt_dowser_none", "The Stone Dowser feels no rocks within {0} m");
        Translations.AddEnglish("whitehilt_dowser_dir_n", "north");
        Translations.AddEnglish("whitehilt_dowser_dir_ne", "north-east");
        Translations.AddEnglish("whitehilt_dowser_dir_e", "east");
        Translations.AddEnglish("whitehilt_dowser_dir_se", "south-east");
        Translations.AddEnglish("whitehilt_dowser_dir_s", "south");
        Translations.AddEnglish("whitehilt_dowser_dir_sw", "south-west");
        Translations.AddEnglish("whitehilt_dowser_dir_w", "west");
        Translations.AddEnglish("whitehilt_dowser_dir_nw", "north-west");
    }

    /// <summary>
    /// True for an item prefab the dowser finds.
    /// </summary>
    /// <param name="prefabName">Prefab name of the item.</param>
    /// <returns>True when it is in <see cref="Items"/>.</returns>
    public static bool IsTrackedItem(string prefabName)
    {
        return Parse(Items.Value, ref parsedItems, ref items).Contains(prefabName);
    }

    /// <summary>
    /// True for a location the dowser leads to.
    /// </summary>
    /// <param name="prefabName">Prefab name of the location.</param>
    /// <returns>True when it is in <see cref="Locations"/>.</returns>
    public static bool IsTrackedLocation(string prefabName)
    {
        return !string.IsNullOrEmpty(prefabName) && Parse(Locations.Value, ref parsedLocations, ref locations).Contains(prefabName);
    }

    private static HashSet<string> Parse(string value, ref string parsed, ref HashSet<string> set)
    {
        if (value != parsed)
        {
            parsed = value;
            set = new HashSet<string>((value ?? string.Empty).Split(',').Select(name => name.Trim()).Where(name => name.Length > 0),
                StringComparer.OrdinalIgnoreCase);
        }

        return set;
    }
}
