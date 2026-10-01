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

    /// <summary>Exploration level needed to set route markers at the Navigator's Table.</summary>
    public static ConfigEntry<int> RouteMarkersLevel { get; private set; }

    /// <summary>Exploration level needed to let the ship sail the route on its own.</summary>
    public static ConfigEntry<int> RouteSailLevel { get; private set; }

    /// <summary>Sail force of the White Hilt Ship; the vanilla code default is 0.1.</summary>
    public static ConfigEntry<float> SailForce { get; private set; }

    /// <summary>Whether the wind always blows from behind a White Hilt Ship with people aboard.</summary>
    public static ConfigEntry<bool> AlwaysTailwind { get; private set; }

    /// <summary>Whether a ship can hold its course while nobody is at the helm.</summary>
    public static ConfigEntry<bool> HoldCourse { get; private set; }

    /// <summary>Whether a still ship can be pushed from the shore.</summary>
    public static ConfigEntry<bool> PushShip { get; private set; }

    /// <summary>Speed in m/s a push gives the ship.</summary>
    public static ConfigEntry<float> PushSpeed { get; private set; }

    /// <summary>The ship can only be pushed while slower than this, in m/s.</summary>
    public static ConfigEntry<float> PushMaxShipSpeed { get; private set; }

    /// <summary>Whether the fishing net catches fish.</summary>
    public static ConfigEntry<bool> FishingNet { get; private set; }

    /// <summary>Speed in m/s the ship needs for the fishing net to catch.</summary>
    public static ConfigEntry<float> FishingNetMinSpeed { get; private set; }

    /// <summary>Share the catch time is shortened by at Fishing 100.</summary>
    public static ConfigEntry<float> FishingNetSkillSpeedUp { get; private set; }

    /// <summary>Chance of two fish in one catch at Fishing 100.</summary>
    public static ConfigEntry<float> FishingNetDoubleChance { get; private set; }

    /// <summary>Fishing experience for the sailor per catch.</summary>
    public static ConfigEntry<float> FishingNetSkillRaise { get; private set; }

    /// <summary>Chance per catch of seaweed as bycatch.</summary>
    public static ConfigEntry<float> FishingNetSeaweedChance { get; private set; }

    /// <summary>Chance per catch on the ocean of an amber pearl as bycatch.</summary>
    public static ConfigEntry<float> FishingNetPearlChance { get; private set; }

    /// <summary>Seconds an empty, still ship waits before the drift anchor drops on its own.</summary>
    public static ConfigEntry<float> AutoAnchorSeconds { get; private set; }

    /// <summary>The ship counts as still for the auto anchor below this speed, in m/s.</summary>
    public static ConfigEntry<float> AutoAnchorMaxSpeed { get; private set; }

    /// <summary>Whether the White Hilt Ship's tent gives shelter.</summary>
    public static ConfigEntry<bool> TentShelter { get; private set; }

    /// <summary>Distance in metres from the mast within which the Mast Wisp thins the fog.</summary>
    public static ConfigEntry<float> MastWispReach { get; private set; }

    /// <summary>Whether the White Hilt Ship's deck portal can be used and is listed for travel.</summary>
    public static ConfigEntry<bool> AllowShipPortal { get; private set; }

    /// <summary>Whether route markers can be set at the Navigator's Table.</summary>
    public static ConfigEntry<bool> ShipRoutes { get; private set; }

    /// <summary>Whether a ship can sail its route on its own ("Take me there").</summary>
    public static ConfigEntry<bool> RouteAutopilot { get; private set; }

    /// <summary>Most markers on a ship's route.</summary>
    public static ConfigEntry<int> RouteMaxMarkers { get; private set; }

    /// <summary>Speed in knots above which a ship sailing its route reefs to half sail; 0 never reefs.</summary>
    public static ConfigEntry<float> RouteMaxSpeed { get; private set; }

    /// <summary>Seconds the player who chose "Take me there" has to sit down before the route is called off.</summary>
    public static ConfigEntry<float> RouteSitSeconds { get; private set; }

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
        RouteMarkersLevel = WhiteHiltConfig.BindAdminOnly(Section, "RouteMarkersLevel", 30,
            "Exploration level needed to set route markers on the map at a ship's Navigator's Table.", new AcceptableValueRange<int>(0, 100));
        RouteSailLevel = WhiteHiltConfig.BindAdminOnly(Section, "RouteSailLevel", 50,
            "Exploration level needed for \"Take me there\": the ship sails the route on its own.", new AcceptableValueRange<int>(0, 100));
        SailForce = WhiteHiltConfig.BindAdminOnly(Section, "SailForce", 0.5f,
            "How hard the wind drives the White Hilt Ship's sail. The vanilla code default is 0.1.", new AcceptableValueRange<float>(0.05f, 1f));
        AlwaysTailwind = WhiteHiltConfig.BindAdminOnly(Section, "AlwaysTailwind", true,
            "The wind always blows from behind the White Hilt Ship, as with Moder's power.");
        HoldCourse = WhiteHiltConfig.BindAdminOnly(Section, "HoldCourse", true,
            "Ships can hold their course while nobody is at the helm (key in Ships.Keys). Off also stops a course being held.");
        PushShip = WhiteHiltConfig.BindAdminOnly(Section, "PushShip", true,
            "A ship lying still can be pushed off from the shore with Use.");
        PushSpeed = WhiteHiltConfig.BindAdminOnly(Section, "PushSpeed", 2.5f,
            "Speed in m/s one push gives the ship.", new AcceptableValueRange<float>(0.5f, 10f));
        PushMaxShipSpeed = WhiteHiltConfig.BindAdminOnly(Section, "PushMaxShipSpeed", 1.5f,
            "A ship can only be pushed while it moves slower than this, in m/s.", new AcceptableValueRange<float>(0.1f, 10f));
        FishingNet = WhiteHiltConfig.BindAdminOnly(Section, "FishingNet", true,
            "The White Hilt Ship's fishing net catches fish while sailing. Off keeps the upgrade on the ship, it just catches nothing.");
        FishingNetMinSpeed = WhiteHiltConfig.BindAdminOnly(Section, "FishingNetMinSpeed", 2f,
            "Speed in m/s the ship needs for the fishing net to catch.", new AcceptableValueRange<float>(0f, 20f));
        FishingNetSkillSpeedUp = WhiteHiltConfig.BindAdminOnly(Section, "FishingNetSkillSpeedUp", 0.5f,
            "Share the time between catches is shortened by at Fishing 100 (0.5 = half the time).", new AcceptableValueRange<float>(0f, 0.9f));
        FishingNetDoubleChance = WhiteHiltConfig.BindAdminOnly(Section, "FishingNetDoubleChance", 0.5f,
            "Chance of two fish in one catch at Fishing 100; less at lower levels.", new AcceptableValueRange<float>(0f, 1f));
        FishingNetSkillRaise = WhiteHiltConfig.BindAdminOnly(Section, "FishingNetSkillRaise", 0.5f,
            "Fishing experience the sailor gets for each catch.", new AcceptableValueRange<float>(0f, 10f));
        FishingNetSeaweedChance = WhiteHiltConfig.BindAdminOnly(Section, "FishingNetSeaweedChance", 0.1f,
            "Chance per catch of seaweed as bycatch (needs FishingNetBycatch).", new AcceptableValueRange<float>(0f, 1f));
        FishingNetPearlChance = WhiteHiltConfig.BindAdminOnly(Section, "FishingNetPearlChance", 0.03f,
            "Chance per catch on the ocean of an amber pearl as bycatch (needs FishingNetBycatch).", new AcceptableValueRange<float>(0f, 1f));
        AutoAnchorSeconds = WhiteHiltConfig.BindAdminOnly(Section, "AutoAnchorSeconds", 2f,
            "Seconds an empty, still White Hilt Ship waits before the drift anchor drops on its own (needs AutoAnchor).", new AcceptableValueRange<float>(0f, 600f));
        AutoAnchorMaxSpeed = WhiteHiltConfig.BindAdminOnly(Section, "AutoAnchorMaxSpeed", 2f,
            "The ship counts as still for the auto anchor below this speed, in m/s.", new AcceptableValueRange<float>(0.1f, 20f));
        TentShelter = WhiteHiltConfig.BindAdminOnly(Section, "TentShelter", true,
            "The White Hilt Ship's tent gives shelter and keeps the rain off those under it.");
        MastWispReach = WhiteHiltConfig.BindAdminOnly(Section, "MastWispReach", 15f,
            "Distance in metres from the mast within which the Mast Wisp thins the ordinary fog. The Mistlands mist clearing is the wisp's own and does not change.",
            new AcceptableValueRange<float>(0f, 100f));
        AllowShipPortal = WhiteHiltConfig.BindAdminOnly(Section, "ShipPortal", true,
            "The White Hilt Ship's deck portal can be used and is listed for travel. Off hides the rune circle; the upgrade, its name and privacy stay on the ship.");
        ShipRoutes = WhiteHiltConfig.BindAdminOnly(Section, "ShipRoutes", true,
            "Route markers can be set on the map at a ship's Navigator's Table. Off hides the markers and stops a ship sailing its route; saved markers stay.");
        RouteAutopilot = WhiteHiltConfig.BindAdminOnly(Section, "RouteAutopilot", true,
            "\"Take me there\": a ship can sail its route on its own. Off stops a ship that sails a route.");
        RouteMaxMarkers = WhiteHiltConfig.BindAdminOnly(Section, "RouteMaxMarkers", 5,
            "Most markers on a ship's route.", new AcceptableValueRange<int>(1, 20));
        RouteMaxSpeed = WhiteHiltConfig.BindAdminOnly(Section, "RouteMaxSpeed", 45f,
            "Speed in knots above which a ship sailing its route on its own takes the sail down to half, until it is well below again. 0 never reefs.",
            new AcceptableValueRange<float>(0f, 200f));
        RouteSitSeconds = WhiteHiltConfig.BindAdminOnly(Section, "RouteSitSeconds", 30f,
            "Seconds the player who chose \"Take me there\" has to sit down before the ship sets off. After that the route is called off and must be started again.",
            new AcceptableValueRange<float>(5f, 600f));

        Translations.AddEnglish("whitehilt_autopilot_on", "Holding course {0}° when you let go of the helm");
        Translations.AddEnglish("whitehilt_autopilot_off", "Course holding off");
        Translations.AddEnglish("whitehilt_autopilot_shallow", "Shallow water ahead: the ship stops holding its course");
        Translations.AddEnglish("whitehilt_compass", "N,NE,E,SE,S,SW,W,NW");
        Translations.AddEnglish("whitehilt_shiphud_wind", "Wind from {0}");
        Translations.AddEnglish("whitehilt_shiphud_course", "Holding course {0}°");
        Translations.AddEnglish("whitehilt_shiphud_eta", "Arrival in {0}");
        Translations.AddEnglish("whitehilt_ship_push", "Push the ship");
        Translations.AddEnglish("whitehilt_route_open", "Route markers");
        Translations.AddEnglish("whitehilt_route_need_markers", "Exploration {0} is needed to set route markers");
        Translations.AddEnglish("whitehilt_route_need_sail", "Exploration {0} needed");
        Translations.AddEnglish("whitehilt_route_hint", "Left-click: add a marker ({0}/{1})    Right-click: remove one");
        Translations.AddEnglish("whitehilt_route_full", "The route has all its markers");
        Translations.AddEnglish("whitehilt_route_sail", "Take me there");
        Translations.AddEnglish("whitehilt_route_stop", "Stop sailing");
        Translations.AddEnglish("whitehilt_route_clear", "Clear");
        Translations.AddEnglish("whitehilt_route_close", "Close");
        Translations.AddEnglish("whitehilt_route_plotting", "Plotting a course...");
        Translations.AddEnglish("whitehilt_route_none", "No sea route found to the markers");
        Translations.AddEnglish("whitehilt_route_started", "The ship sails the route");
        Translations.AddEnglish("whitehilt_route_sit", "Sit down within {0} seconds, and the ship sets off");
        Translations.AddEnglish("whitehilt_route_sit_timeout", "Nobody sat down in time: choose \"Take me there\" again");
        Translations.AddEnglish("whitehilt_route_arrived", "The ship has arrived");
        Translations.AddEnglish("whitehilt_route_stopped", "The ship stops sailing the route");
        Translations.AddEnglish("whitehilt_route_taken", "You take the helm: the ship stops sailing the route");
        Translations.AddEnglish("whitehilt_route_shallow", "No way past the rocks or shallows ahead: the ship stops");
        Translations.AddEnglish("whitehilt_route_anchored", "The anchor is down: the ship stops sailing the route");
    }
}
