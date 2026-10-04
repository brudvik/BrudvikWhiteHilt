using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrudvikWhiteHilt.Navigation.Dowsing;

/// <summary>Server-synced search and signal settings for the Root Dowser.</summary>
public static class RootDowsingSettings
{
    private const string Section = "Gear.WhiteHiltRootDowser";
    private static string parsedItems;
    private static HashSet<string> items = new();

    /// <summary>Item prefab names whose growing plants are tracked.</summary>
    public static ConfigEntry<string> Items { get; private set; }
    /// <summary>Search distance in metres.</summary>
    public static ConfigEntry<float> PingRange { get; private set; }
    /// <summary>Seconds between target searches.</summary>
    public static ConfigEntry<float> ScanSeconds { get; private set; }
    /// <summary>Seconds between pings beside a plant.</summary>
    public static ConfigEntry<float> CloseInterval { get; private set; }
    /// <summary>Seconds between pings at the search boundary.</summary>
    public static ConfigEntry<float> DistantInterval { get; private set; }
    /// <summary>Sound pitch relative to the vanilla Wishbone.</summary>
    public static ConfigEntry<float> PingPitch { get; private set; }
    /// <summary>Distance at which the nearest plant glows.</summary>
    public static ConfigEntry<float> GlowDistance { get; private set; }
    /// <summary>Radius of the plant's green light.</summary>
    public static ConfigEntry<float> GlowRange { get; private set; }
    /// <summary>Brightness of the plant's green light.</summary>
    public static ConfigEntry<float> GlowIntensity { get; private set; }

    /// <summary>Binds the Root Dowser settings before content registration.</summary>
    public static void Initialize()
    {
        Items = WhiteHiltConfig.BindAdminOnly(Section, "Items", "WhiteHiltMadder,WhiteHiltRoseroot",
            "Comma-separated item prefab names. Finds only unpicked plants that give these items, not loose drops or random bonus drops.");
        PingRange = WhiteHiltConfig.BindAdminOnly(Section, "PingRange", 30f,
            "Metres within which the Root Dowser finds the nearest unpicked plant.", new AcceptableValueRange<float>(1f, 150f));
        ScanSeconds = WhiteHiltConfig.BindAdminOnly(Section, "ScanSeconds", 1f,
            "Seconds between searches for the nearest plant.", new AcceptableValueRange<float>(0.1f, 10f));
        CloseInterval = WhiteHiltConfig.BindAdminOnly(Section, "CloseInterval", 1f,
            "Seconds between pings beside a plant.", new AcceptableValueRange<float>(0.1f, 30f));
        DistantInterval = WhiteHiltConfig.BindAdminOnly(Section, "DistantInterval", 5f,
            "Seconds between pings at the search boundary; never faster than CloseInterval.", new AcceptableValueRange<float>(0.1f, 30f));
        PingPitch = WhiteHiltConfig.BindAdminOnly(Section, "PingPitch", 1.3f,
            "Sound pitch relative to the Wishbone. Applied after a restart.", new AcceptableValueRange<float>(0.5f, 2f));
        GlowDistance = WhiteHiltConfig.BindAdminOnly(Section, "GlowDistance", 3f,
            "Metres within which the nearest plant glows green. Zero disables the glow.", new AcceptableValueRange<float>(0f, 30f));
        GlowRange = WhiteHiltConfig.BindAdminOnly(Section, "GlowRange", 1f,
            "Radius in metres of the plant's green light.", new AcceptableValueRange<float>(0.1f, 5f));
        GlowIntensity = WhiteHiltConfig.BindAdminOnly(Section, "GlowIntensity", 0.6f,
            "Brightness of the plant's green light.", new AcceptableValueRange<float>(0f, 3f));
    }

    /// <summary>Checks whether an item is in the current target list.</summary>
    /// <param name="prefabName">Item prefab name.</param>
    /// <returns>Whether plants giving the item are tracked.</returns>
    public static bool IsTrackedItem(string prefabName)
    {
        string value = Items.Value ?? string.Empty;
        if (parsedItems != value)
        {
            parsedItems = value;
            items = new HashSet<string>(value.Split(',').Select(name => name.Trim()).Where(name => name.Length > 0),
                StringComparer.OrdinalIgnoreCase);
        }

        return !string.IsNullOrEmpty(prefabName) && items.Contains(prefabName);
    }
}