using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Ships;

/// <summary>
/// Config for sailing, section "Ships". The rules are server-synced; keys and what is shown are the player's own.
/// </summary>
public static class ShipSettings
{
    private const string Section = "Ships";
    private const string KeySection = "Ships.Keys";

    /// <summary>Holds the ship's course while nobody is at the helm, and back.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyHoldCourse { get; private set; }

    /// <summary>Whether speed, heading and wind are shown under the wind indicator while steering.</summary>
    public static ConfigEntry<bool> ShowShipHud { get; private set; }

    /// <summary>Whether the drift anchor drops when the last person leaves the ship, and weighs when someone takes the helm.</summary>
    public static ConfigEntry<bool> AutoAnchor { get; private set; }

    /// <summary>Minutes between the fishing net's catches, before the Fishing skill shortens it.</summary>
    public static ConfigEntry<float> FishingNetMinutes { get; private set; }

    /// <summary>Whether the fishing net also brings up seaweed, and now and then an amber pearl on the ocean.</summary>
    public static ConfigEntry<bool> FishingNetBycatch { get; private set; }

    /// <summary>Fraction of the ordinary fog left near a White Hilt Ship with the mast wisp; 1 leaves the fog alone.</summary>
    public static ConfigEntry<float> MastWispFogLeft { get; private set; }

    /// <summary>
    /// Binds the config entries. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        KeyHoldCourse = WhiteHiltConfig.BindLocal(KeySection, "HoldCourse", new KeyboardShortcut(KeyCode.H),
            "At the helm: the ship holds its course when you let go of the helm, and stops before shallow water.");
        ShowShipHud = WhiteHiltConfig.BindLocal(Section, "ShowSpeedAndHeading", true,
            "Show speed in knots, heading and the wind under the wind indicator while steering.");
        AutoAnchor = WhiteHiltConfig.BindAdminOnly(Section, "AutoAnchor", true,
            "The White Hilt Ship's drift anchor drops by itself when the last person leaves a still ship, and is weighed when someone takes the helm.");
        FishingNetMinutes = WhiteHiltConfig.BindAdminOnly(Section, "FishingNetMinutes", 2f,
            "Minutes between the fishing net's catches while sailing. The Fishing skill of the sailor shortens it, to half at level 100.",
            new AcceptableValueRange<float>(0.5f, 30f));
        FishingNetBycatch = WhiteHiltConfig.BindAdminOnly(Section, "FishingNetBycatch", true,
            "The fishing net sometimes also brings up seaweed, and on the ocean now and then an amber pearl.");
        MastWispFogLeft = WhiteHiltConfig.BindAdminOnly(Section, "MastWispFogLeft", 0.25f,
            "Near a White Hilt Ship with the Mast Wisp, this fraction of the ordinary fog is left (the Mistlands mist is cleared anyway). 1 leaves the fog alone.",
            new AcceptableValueRange<float>(0f, 1f));

        Translations.AddEnglish("whitehilt_autopilot_on", "Holding course {0}° when you let go of the helm");
        Translations.AddEnglish("whitehilt_autopilot_off", "Course holding off");
        Translations.AddEnglish("whitehilt_autopilot_shallow", "Shallow water ahead: the ship stops holding its course");
        Translations.AddEnglish("whitehilt_compass", "N,NE,E,SE,S,SW,W,NW");
        Translations.AddEnglish("whitehilt_shiphud_wind", "Wind from {0}");
        Translations.AddEnglish("whitehilt_shiphud_course", "Holding course {0}°");
        Translations.AddEnglish("whitehilt_ship_push", "Push the ship");
    }
}
