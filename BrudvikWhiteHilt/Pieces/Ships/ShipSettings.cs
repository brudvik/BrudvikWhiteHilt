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

    /// <summary>
    /// Binds the config entries. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        KeyHoldCourse = WhiteHiltConfig.BindLocal(KeySection, "HoldCourse", new KeyboardShortcut(KeyCode.H),
            "At the helm: the ship holds its course when you let go of the helm, and stops before shallow water.");

        Translations.AddEnglish("whitehilt_autopilot_on", "Holding course {0}° when you let go of the helm");
        Translations.AddEnglish("whitehilt_autopilot_off", "Course holding off");
        Translations.AddEnglish("whitehilt_autopilot_shallow", "Shallow water ahead: the ship stops holding its course");
    }
}
