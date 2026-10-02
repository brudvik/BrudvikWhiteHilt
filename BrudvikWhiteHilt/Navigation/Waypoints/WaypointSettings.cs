using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Navigation;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Navigation.Waypoints;

/// <summary>
/// Config and texts for the ruby amulet's target, section "Gear.WhiteHiltPathfinderRuby". The rules are server-synced;
/// the arrow on the screen is each player's own.
/// </summary>
public static class WaypointSettings
{
    private const string Section = "Gear.WhiteHiltPathfinderRuby";

    /// <summary>Metres from the target at which it counts as reached and is removed.</summary>
    public static ConfigEntry<float> ArrivalRadius { get; private set; }

    /// <summary>Degrees between the way the player moves and the target from which it is the wrong way.</summary>
    public static ConfigEntry<float> WrongWayAngle { get; private set; }

    /// <summary>Seconds of moving the wrong way before the warning.</summary>
    public static ConfigEntry<float> WrongWaySeconds { get; private set; }

    /// <summary>Metres from the target within which there is no warning.</summary>
    public static ConfigEntry<float> WrongWayMinDistance { get; private set; }

    /// <summary>Metres per second the player must move for a warning.</summary>
    public static ConfigEntry<float> WrongWayMinSpeed { get; private set; }

    /// <summary>Seconds before the warning can come again.</summary>
    public static ConfigEntry<float> WrongWayCooldown { get; private set; }

    /// <summary>Whether the arrow at the top of the screen is shown.</summary>
    public static ConfigEntry<bool> HudArrow { get; private set; }

    /// <summary>Pixels from the top of the screen to the arrow.</summary>
    public static ConfigEntry<float> HudArrowTop { get; private set; }

    /// <summary>Size of the arrow in pixels.</summary>
    public static ConfigEntry<float> HudArrowSize { get; private set; }

    /// <summary>Whether the distance is written under the arrow.</summary>
    public static ConfigEntry<bool> ShowDistance { get; private set; }

    /// <summary>
    /// Binds the config entries and registers the English texts. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        ArrivalRadius = WhiteHiltConfig.BindAdminOnly(Section, "ArrivalRadius", 15f,
            "Metres from the target at which you have reached it; the target is then removed.", new AcceptableValueRange<float>(2f, 200f));
        WrongWayAngle = WhiteHiltConfig.BindAdminOnly(Section, "WrongWayAngle", 135f,
            "Degrees between the way you move and the way to the target from which you are going the wrong way (180 = straight away from it).",
            new AcceptableValueRange<float>(90f, 180f));
        WrongWaySeconds = WhiteHiltConfig.BindAdminOnly(Section, "WrongWaySeconds", 4f,
            "Seconds you must move the wrong way before the ruby tells you.", new AcceptableValueRange<float>(1f, 30f));
        WrongWayMinDistance = WhiteHiltConfig.BindAdminOnly(Section, "WrongWayMinDistance", 50f,
            "Metres from the target within which the ruby does not tell you that you are going the wrong way.", new AcceptableValueRange<float>(0f, 1000f));
        WrongWayMinSpeed = WhiteHiltConfig.BindAdminOnly(Section, "WrongWayMinSpeed", 1f,
            "Metres per second you must move before it counts as going the wrong way; standing and turning around does not.",
            new AcceptableValueRange<float>(0.1f, 10f));
        WrongWayCooldown = WhiteHiltConfig.BindAdminOnly(Section, "WrongWayCooldown", 30f,
            "Seconds before the wrong-way message can come again.", new AcceptableValueRange<float>(5f, 600f));
        HudArrow = WhiteHiltConfig.BindLocal(Section, "HudArrow", true,
            "Show the arrow toward the target at the top of the screen. The marker and the arrow on the minimap stay.");
        HudArrowTop = WhiteHiltConfig.BindLocal(Section, "HudArrowTop", 110f,
            "Pixels from the top of the screen to the arrow.", new AcceptableValueRange<float>(0f, 1000f));
        HudArrowSize = WhiteHiltConfig.BindLocal(Section, "HudArrowSize", 44f,
            "Size of the arrow in pixels.", new AcceptableValueRange<float>(16f, 128f));
        ShowDistance = WhiteHiltConfig.BindLocal(Section, "ShowDistance", true,
            "Write the distance to the target under the arrow.");
        WhiteHiltConfig.SetSectionLabel(Section, Translations.Token(Translations.ItemKey(RubyPathfinderAmulet.RubyPrefabName)));

        Translations.AddEnglish("whitehilt_waypoint_default", "Target");
        Translations.AddEnglish("msg_whitehilt_waypoint_set", "Target set: {0}");
        Translations.AddEnglish("msg_whitehilt_waypoint_cleared", "Target removed");
        Translations.AddEnglish("msg_whitehilt_waypoint_arrived", "You have reached {0}");
        Translations.AddEnglish("msg_whitehilt_waypoint_wrongway", "You are going the wrong way: {0} lies to the {1}");
    }
}
