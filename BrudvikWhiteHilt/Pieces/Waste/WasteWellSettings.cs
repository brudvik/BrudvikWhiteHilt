using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrudvikWhiteHilt.Pieces.Waste;

/// <summary>
/// Config for the Waste Well, section "WasteWell". Server-synced.
/// </summary>
public static class WasteWellSettings
{
    private const string Section = "WasteWell";

    private static string keepText;
    private static HashSet<string> keep = new();

    /// <summary>Seconds after the well is closed before what lies in it is gone.</summary>
    public static ConfigEntry<float> DisposeDelaySeconds { get; private set; }

    /// <summary>How far from the well, in metres, it collects items lying on the ground.</summary>
    public static ConfigEntry<float> CollectRadius { get; private set; }

    /// <summary>How long an item must have lain on the ground before the well takes it.</summary>
    public static ConfigEntry<float> MinItemAgeSeconds { get; private set; }

    /// <summary>Prefab names the well never collects from the ground, comma separated.</summary>
    public static ConfigEntry<string> KeepItems { get; private set; }

    /// <summary>
    /// Binds the config entries. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        DisposeDelaySeconds = WhiteHiltConfig.BindAdminOnly(Section, "DisposeDelaySeconds", 5f,
            "Seconds after the Waste Well is closed before what was thrown in is gone. Open it again before then to take things back.",
            new AcceptableValueRange<float>(0f, 60f));
        CollectRadius = WhiteHiltConfig.BindAdminOnly(Section, "CollectRadius", 8f,
            "How far from the Waste Well, in metres, it collects items lying on the ground when collecting is switched on for it.",
            new AcceptableValueRange<float>(1f, 30f));
        MinItemAgeSeconds = WhiteHiltConfig.BindAdminOnly(Section, "MinItemAgeSeconds", 30f,
            "How long an item must have lain on the ground before the Waste Well collects it, so freshly dropped items can still be picked up.",
            new AcceptableValueRange<float>(0f, 600f));
        KeepItems = WhiteHiltConfig.BindAdminOnly(Section, "KeepItems", string.Empty,
            "Prefab names of items the Waste Well never collects from the ground, comma separated, e.g. TrophyDeer,Coins.");

        Translations.AddEnglish("whitehilt_wastewell_collect_on", "Collecting items within {0} m");
        Translations.AddEnglish("whitehilt_wastewell_collect_off", "Not collecting items from the ground");
        Translations.AddEnglish("whitehilt_wastewell_toggle", "Switch collecting from the ground");
        Translations.AddEnglish("whitehilt_wastewell_hint", "What is thrown in is gone {0} s after the well is closed");
        Translations.AddEnglish("msg_whitehilt_wastewell_gone", "The well swallowed {0} items");
    }

    /// <summary>
    /// True if the item is never collected from the ground.
    /// </summary>
    /// <param name="prefabName">Prefab name of the item.</param>
    /// <returns>True to leave it lying.</returns>
    public static bool IsKept(string prefabName)
    {
        string text = KeepItems.Value ?? string.Empty;
        if (text != keepText)
        {
            keepText = text;
            keep = new HashSet<string>(text.Split(',').Select(name => name.Trim()).Where(name => name.Length > 0), StringComparer.OrdinalIgnoreCase);
        }

        return keep.Contains(prefabName);
    }
}
