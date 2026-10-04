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

    /// <summary>Exploration level needed for explorer mode, which sails the route along the coast.</summary>
    public static ConfigEntry<int> RouteExploreLevel { get; private set; }

    /// <summary>How close to land explorer mode keeps, in 32 m steps from the nearest shallow water.</summary>
    public static ConfigEntry<int> RouteExploreCoastCells { get; private set; }

    /// <summary>How many times longer open water counts than the same stretch along the coast in explorer mode.</summary>
    public static ConfigEntry<float> RouteExploreOpenWaterCost { get; private set; }

    /// <summary>Within this distance of land, in metres, explorer mode never sails with full sail.</summary>
    public static ConfigEntry<float> RouteExploreNearLand { get; private set; }

    /// <summary>Metres the camera can zoom further out than vanilla's limit at the helm.</summary>
    public static ConfigEntry<float> CameraExtraZoom { get; private set; }

    /// <summary>Whether everyone aboard a ship can zoom out as far as the helmsman.</summary>
    public static ConfigEntry<bool> CameraZoomAllAboard { get; private set; }

    /// <summary>Whether the camera swings around the ship when it sets off on a route; the player's own.</summary>
    public static ConfigEntry<bool> RouteCameraSweep { get; private set; }

    /// <summary>Whether the HUD is hidden during the route camera sweep.</summary>
    public static ConfigEntry<bool> RouteCameraSweepHideHud { get; private set; }

    /// <summary>Seconds the route camera sweep takes, hold included.</summary>
    public static ConfigEntry<float> RouteCameraSweepSeconds { get; private set; }

    /// <summary>Seconds the sweep stays still in front of the sail.</summary>
    public static ConfigEntry<float> RouteCameraSweepHoldSeconds { get; private set; }

    /// <summary>The sweep's distance from the sail, in ship lengths.</summary>
    public static ConfigEntry<float> RouteCameraSweepDistance { get; private set; }

    /// <summary>The sweep's height above the middle of the sail, in ship lengths.</summary>
    public static ConfigEntry<float> RouteCameraSweepHeight { get; private set; }

    /// <summary>Degrees to the side of the bow where the sweep stops to show the sail.</summary>
    public static ConfigEntry<float> RouteCameraSweepAngle { get; private set; }

    /// <summary>Seconds the camera takes back to the player when the sweep is interrupted.</summary>
    public static ConfigEntry<float> RouteCameraSweepCancelSeconds { get; private set; }

    /// <summary>Degrees the view must turn within about a second to interrupt the sweep.</summary>
    public static ConfigEntry<float> RouteCameraSweepCancelLook { get; private set; }

    /// <summary>Whether the depth under the ship is shown in the ship's read-out; the player's own.</summary>
    public static ConfigEntry<bool> ShowDepth { get; private set; }

    /// <summary>Whether shallows and rocks ahead are warned of; the player's own.</summary>
    public static ConfigEntry<bool> ShoalWarning { get; private set; }

    /// <summary>Whether the warning rings the ship's bell.</summary>
    public static ConfigEntry<bool> ShoalBell { get; private set; }

    /// <summary>Water shallower than this, in metres, counts as shallow.</summary>
    public static ConfigEntry<float> ShoalDepth { get; private set; }

    /// <summary>Seconds of sailing ahead that are sounded.</summary>
    public static ConfigEntry<float> ShoalLookaheadSeconds { get; private set; }

    /// <summary>Metres ahead of the bow that are always sounded.</summary>
    public static ConfigEntry<float> ShoalMinLookahead { get; private set; }

    /// <summary>Metres ahead of the bow that are sounded at most.</summary>
    public static ConfigEntry<float> ShoalMaxLookahead { get; private set; }

    /// <summary>Speed in m/s below which there is no warning.</summary>
    public static ConfigEntry<float> ShoalMinSpeed { get; private set; }

    /// <summary>Seconds before the warning can come again.</summary>
    public static ConfigEntry<float> ShoalCooldown { get; private set; }

    /// <summary>Seconds between depth and obstacle scans.</summary>
    public static ConfigEntry<float> SoundingInterval { get; private set; }

    /// <summary>Seconds between reminders of continuous danger.</summary>
    public static ConfigEntry<float> ShoalRepeatSeconds { get; private set; }

    /// <summary>Seconds of clear water before another danger counts as a new encounter.</summary>
    public static ConfigEntry<float> ShoalClearSeconds { get; private set; }

    /// <summary>Whether the White Hilt Ship prefers its helmsman as network owner when its containers are idle.</summary>
    public static ConfigEntry<bool> HelmOwnership { get; private set; }

    /// <summary>Seconds between checks of the helmsman's network ownership.</summary>
    public static ConfigEntry<float> HelmOwnershipInterval { get; private set; }

    /// <summary>Seconds ownership is reserved for a container open request.</summary>
    public static ConfigEntry<float> ContainerOwnershipGrace { get; private set; }

    /// <summary>Whether this client logs White Hilt Ship ownership and update timing.</summary>
    public static ConfigEntry<bool> ShipDiagnostics { get; private set; }

    /// <summary>Seconds between diagnostic samples.</summary>
    public static ConfigEntry<float> ShipDiagnosticsInterval { get; private set; }

    /// <summary>Whether a fall from a moving ship is called out to those aboard.</summary>
    public static ConfigEntry<bool> ManOverboard { get; private set; }

    /// <summary>Speed in m/s the ship must have for a fall to count.</summary>
    public static ConfigEntry<float> OverboardMinSpeed { get; private set; }

    /// <summary>Whether a ship sailing its route or holding its course stops.</summary>
    public static ConfigEntry<bool> OverboardStopShip { get; private set; }

    /// <summary>Seconds after which the alert ends by itself.</summary>
    public static ConfigEntry<float> OverboardTimeout { get; private set; }

    /// <summary>Whether a lifeline can be thrown.</summary>
    public static ConfigEntry<bool> Lifeline { get; private set; }

    /// <summary>Metres a lifeline reaches.</summary>
    public static ConfigEntry<float> LifelineRange { get; private set; }

    /// <summary>Seconds from throwing the lifeline until the player is aboard.</summary>
    public static ConfigEntry<float> LifelineDelay { get; private set; }

    /// <summary>Metres from a Mooring Post within which a ship can be moored.</summary>
    public static ConfigEntry<float> MooringRange { get; private set; }

    /// <summary>Local lantern intensity relative to the vanilla lamp; applied after a restart.</summary>
    public static ConfigEntry<float> LanternBrightness { get; private set; }

    /// <summary>Local lantern range relative to the vanilla lamp; applied after a restart.</summary>
    public static ConfigEntry<float> LanternRange { get; private set; }

    /// <summary>Seconds the lantern flickers before the Kraken's tentacles appear.</summary>
    public static ConfigEntry<float> LanternWarningSeconds { get; private set; }

    /// <summary>Seconds between synchronized lantern flicker changes.</summary>
    public static ConfigEntry<float> LanternFlickerSeconds { get; private set; }

    /// <summary>Metres from the targeted ship within which Kraken suppresses its lantern.</summary>
    public static ConfigEntry<float> LanternKrakenRange { get; private set; }

    /// <summary>
    /// Binds the config entries. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        LanternBrightness = WhiteHiltConfig.BindLocal(Section, "LanternBrightness", 1f,
            "How many times brighter the Ship Lantern shines than the vanilla lamp. Needs a restart.");
        LanternRange = WhiteHiltConfig.BindLocal(Section, "LanternRange", 3f,
            "How many times further the Ship Lantern reaches than the vanilla lamp. Needs a restart.");
        LanternWarningSeconds = WhiteHiltConfig.BindAdminOnly(Section, "LanternWarningSeconds", 3f,
            "Seconds the lantern flickers before Kraken's tentacles appear, capped at their spawn delay.", new AcceptableValueRange<float>(0f, 7f));
        LanternFlickerSeconds = WhiteHiltConfig.BindAdminOnly(Section, "LanternFlickerSeconds", 0.18f,
            "Seconds between lantern flicker changes during the Kraken warning.", new AcceptableValueRange<float>(0.05f, 1f));
        LanternKrakenRange = WhiteHiltConfig.BindAdminOnly(Section, "LanternKrakenRange", 60f,
            "Metres from the ship Kraken targeted within which its lantern flickers and goes out. Other ships are unaffected.", new AcceptableValueRange<float>(1f, 200f));
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
        RouteExploreLevel = WhiteHiltConfig.BindAdminOnly(Section, "RouteExploreLevel", 50,
            "Exploration level needed for explorer mode: the ship sails the route on its own, as close to land as it safely can.",
            new AcceptableValueRange<int>(0, 100));
        RouteExploreCoastCells = WhiteHiltConfig.BindAdminOnly(Section, "RouteExploreCoastCells", 1,
            "How close to land explorer mode keeps, in steps of 32 m out from the nearest shallow water. 1 is as close as the ship safely gets.",
            new AcceptableValueRange<int>(1, 5));
        RouteExploreOpenWaterCost = WhiteHiltConfig.BindAdminOnly(Section, "RouteExploreOpenWaterCost", 3f,
            "How hard explorer mode avoids open water: a stretch away from the coast counts as this many times as long. Higher follows more of every bay; 1 is the fastest route.",
            new AcceptableValueRange<float>(1f, 10f));
        RouteExploreNearLand = WhiteHiltConfig.BindAdminOnly(Section, "RouteExploreNearLand", 120f,
            "Within this distance of land, in metres, explorer mode never sails with full sail, only half sail or rowing.",
            new AcceptableValueRange<float>(20f, 500f));
        CameraExtraZoom = WhiteHiltConfig.BindAdminOnly(Section, "CameraExtraZoom", 2f,
            "Metres the camera can zoom further out at the helm than the vanilla limit. 0 keeps the vanilla limit.",
            new AcceptableValueRange<float>(0f, 10f));
        CameraZoomAllAboard = WhiteHiltConfig.BindAdminOnly(Section, "CameraZoomAllAboard", true,
            "Everyone aboard a ship, also those standing on deck or sitting, can zoom out as far as the one at the helm.");
        RouteCameraSweep = WhiteHiltConfig.BindLocal(Section, "RouteCameraSweep", true,
            "When a ship sets off on its route (\"Take me there\" or explorer mode), the camera of everyone sitting aboard swings out around the ship, past the front of the sail, and back behind you. A quick swing of the mouse takes the camera back at once.");
        RouteCameraSweepHideHud = WhiteHiltConfig.BindLocal(Section, "RouteCameraSweepHideHud", true,
            "Hide the HUD while the camera swings around the ship.");
        RouteCameraSweepSeconds = WhiteHiltConfig.BindLocal(Section, "RouteCameraSweepSeconds", 10f,
            "Seconds the camera takes to swing around the ship, the pause in front of the sail included.", new AcceptableValueRange<float>(4f, 30f));
        RouteCameraSweepHoldSeconds = WhiteHiltConfig.BindLocal(Section, "RouteCameraSweepHoldSeconds", 1.5f,
            "Seconds the camera stays still in front of the sail. At most half the whole swing.", new AcceptableValueRange<float>(0f, 10f));
        RouteCameraSweepDistance = WhiteHiltConfig.BindLocal(Section, "RouteCameraSweepDistance", 1.3f,
            "How far out the camera swings from the sail, in ship lengths, so small and large ships fill the picture alike.", new AcceptableValueRange<float>(0.5f, 4f));
        RouteCameraSweepHeight = WhiteHiltConfig.BindLocal(Section, "RouteCameraSweepHeight", 0.3f,
            "How high the camera swings above the middle of the sail, in ship lengths. 0 is level with the sail.", new AcceptableValueRange<float>(-0.3f, 2f));
        RouteCameraSweepAngle = WhiteHiltConfig.BindLocal(Section, "RouteCameraSweepAngle", 30f,
            "Degrees to the side of the bow where the camera stops to show the sail, so the figurehead and the mast leave the sail free. 0 is straight ahead.", new AcceptableValueRange<float>(0f, 90f));
        RouteCameraSweepCancelSeconds = WhiteHiltConfig.BindLocal(Section, "RouteCameraSweepCancelSeconds", 0.5f,
            "Seconds the camera takes back to you when the swing is interrupted by the mouse, standing up or opening a menu.", new AcceptableValueRange<float>(0.1f, 3f));
        RouteCameraSweepCancelLook = WhiteHiltConfig.BindLocal(Section, "RouteCameraSweepCancelLook", 90f,
            "Degrees you must turn the view within about a second to take the camera back. Looking calmly up and around, or zooming, does not interrupt the swing.", new AcceptableValueRange<float>(10f, 720f));
        ShowDepth = WhiteHiltConfig.BindLocal(Section, "ShowDepth", true,
            "Show the depth of the water under the ship in the read-out under the wind indicator.");
        ShoalWarning = WhiteHiltConfig.BindLocal(Section, "ShoalWarning", true,
            "Warn when shallow water or rocks lie ahead, while you steer or ride a ship that sails its route.");
        ShoalBell = WhiteHiltConfig.BindLocal(Section, "ShoalBell", true,
            "The shoal warning rings the ship's bell.");
        ShoalDepth = WhiteHiltConfig.BindLocal(Section, "ShoalDepth", 3.5f,
            "Water shallower than this, in metres, counts as shallow. Ships need about 2 m.", new AcceptableValueRange<float>(1f, 20f));
        ShoalLookaheadSeconds = WhiteHiltConfig.BindLocal(Section, "ShoalLookaheadSeconds", 5f,
            "Seconds of sailing ahead that are sounded, so the faster the ship, the further ahead.", new AcceptableValueRange<float>(1f, 30f));
        ShoalMinLookahead = WhiteHiltConfig.BindLocal(Section, "ShoalMinLookahead", 15f,
            "Metres ahead of the bow that are always sounded.", new AcceptableValueRange<float>(5f, 100f));
        ShoalMaxLookahead = WhiteHiltConfig.BindLocal(Section, "ShoalMaxLookahead", 60f,
            "Metres ahead of the bow that are sounded at most.", new AcceptableValueRange<float>(10f, 200f));
        ShoalMinSpeed = WhiteHiltConfig.BindLocal(Section, "ShoalMinSpeed", 2f,
            "Below this speed, in m/s, there is no warning, so it stays quiet while you lay to.", new AcceptableValueRange<float>(0f, 20f));
        ShoalCooldown = WhiteHiltConfig.BindLocal(Section, "ShoalCooldown", 8f,
            "Minimum seconds between warnings, also for a new danger after clear water.", new AcceptableValueRange<float>(1f, 120f));
        SoundingInterval = WhiteHiltConfig.BindLocal(Section, "SoundingInterval", 1f,
            "Seconds between depth and obstacle scans; the read-out uses the latest result between scans.", new AcceptableValueRange<float>(0.2f, 5f));
        ShoalRepeatSeconds = WhiteHiltConfig.BindLocal(Section, "ShoalRepeatSeconds", 60f,
            "Seconds between reminders while danger persists, for example when sailing along shallow shores.", new AcceptableValueRange<float>(8f, 600f));
        ShoalClearSeconds = WhiteHiltConfig.BindLocal(Section, "ShoalClearSeconds", 5f,
            "Seconds of clear water at warning speed before the next danger counts as a new encounter.", new AcceptableValueRange<float>(0f, 60f));
        HelmOwnership = WhiteHiltConfig.BindAdminOnly(Section, "HelmOwnership", true,
            "Prefer the White Hilt Ship's helmsman as network owner. Wait while either container is in use or opening; sailing speed is unchanged.");
        HelmOwnershipInterval = WhiteHiltConfig.BindAdminOnly(Section, "HelmOwnershipInterval", 0.5f,
            "Seconds between checks for transferring White Hilt Ship ownership to its helmsman.", new AcceptableValueRange<float>(0.1f, 5f));
        ContainerOwnershipGrace = WhiteHiltConfig.BindAdminOnly(Section, "ContainerOwnershipGrace", 2f,
            "Seconds after a container open request before helm ownership may resume. An open container keeps ownership until closed.", new AcceptableValueRange<float>(1f, 30f));
        ShipDiagnostics = WhiteHiltConfig.BindLocal(Section, "ShipDiagnostics", false,
            "Log White Hilt Ship owner, helmsman, container use, ZDO revision timing and peak frame time on this client while aboard.");
        ShipDiagnosticsInterval = WhiteHiltConfig.BindLocal(Section, "ShipDiagnosticsInterval", 5f,
            "Seconds between local ship diagnostic samples. ZDO revision timing includes inventory and control changes, not only movement.", new AcceptableValueRange<float>(1f, 60f));
        ManOverboard = WhiteHiltConfig.BindAdminOnly(Section, "ManOverboard", true,
            "When someone falls from a moving ship, everyone aboard is told, with the bell, a pin on the map and an arrow on the minimap toward them.");
        OverboardMinSpeed = WhiteHiltConfig.BindAdminOnly(Section, "OverboardMinSpeed", 2f,
            "Speed in m/s the ship must have for a fall to count, so stepping off at a dock does not.", new AcceptableValueRange<float>(0f, 20f));
        OverboardStopShip = WhiteHiltConfig.BindAdminOnly(Section, "OverboardStopShip", true,
            "A ship that sails its route or holds its course stops when someone falls overboard. A ship someone steers is left to them.");
        OverboardTimeout = WhiteHiltConfig.BindAdminOnly(Section, "OverboardTimeout", 300f,
            "Seconds after which the man overboard alert ends by itself.", new AcceptableValueRange<float>(30f, 3600f));
        Lifeline = WhiteHiltConfig.BindAdminOnly(Section, "Lifeline", true,
            "Someone on the deck of the ship can throw a lifeline (Use) to the one in the water and pull them back aboard.");
        LifelineRange = WhiteHiltConfig.BindAdminOnly(Section, "LifelineRange", 25f,
            "Metres a lifeline reaches.", new AcceptableValueRange<float>(5f, 100f));
        LifelineDelay = WhiteHiltConfig.BindAdminOnly(Section, "LifelineDelay", 1.5f,
            "Seconds from throwing the lifeline until the one in the water is pulled aboard.", new AcceptableValueRange<float>(0f, 10f));
        MooringRange = WhiteHiltConfig.BindAdminOnly(Section, "MooringRange", 20f,
            "Metres from a Mooring Post within which a ship can be moored to it.", new AcceptableValueRange<float>(5f, 60f));

        Translations.AddEnglish("whitehilt_autopilot_on", "Holding course {0}° when you let go of the helm");
        Translations.AddEnglish("whitehilt_autopilot_off", "Course holding off");
        Translations.AddEnglish("whitehilt_autopilot_shallow", "Shallow water ahead: the ship stops holding its course");
        Translations.AddEnglish("whitehilt_compass", "N,NE,E,SE,S,SW,W,NW");
        Translations.AddEnglish("whitehilt_shiphud_wind", "Wind from {0}");
        Translations.AddEnglish("whitehilt_shiphud_course", "Holding course {0}°");
        Translations.AddEnglish("whitehilt_shiphud_eta", "Arrival in {0}");
        Translations.AddEnglish("whitehilt_shiphud_depth", "Depth {0} m");
        Translations.AddEnglish("whitehilt_shoal_shallow", "Shallow water ahead!");
        Translations.AddEnglish("whitehilt_shoal_blocked", "Rocks or something in the way ahead!");
        Translations.AddEnglish("whitehilt_overboard_alert", "Man overboard: {0}!");
        Translations.AddEnglish("whitehilt_overboard_self", "Overboard! Your crew has been told");
        Translations.AddEnglish("whitehilt_overboard_ship_pin", "Ship");
        Translations.AddEnglish("whitehilt_overboard_lifeline", "Throw a lifeline to {0}");
        Translations.AddEnglish("whitehilt_overboard_pulled", "{0} throws you a lifeline");
        Translations.AddEnglish("whitehilt_ship_push", "Push the ship");
        Translations.AddEnglish("whitehilt_route_open", "Route markers");
        Translations.AddEnglish("whitehilt_route_need_markers", "Exploration {0} is needed to set route markers");
        Translations.AddEnglish("whitehilt_route_need_sail", "Exploration {0} needed");
        Translations.AddEnglish("whitehilt_route_hint", "Left-click: add a marker ({0}/{1})    Right-click: remove one");
        Translations.AddEnglish("whitehilt_route_full", "The route has all its markers");
        Translations.AddEnglish("whitehilt_route_sail", "Take me there");
        Translations.AddEnglish("whitehilt_route_explore", "Explorer mode");
        Translations.AddEnglish("whitehilt_route_explore_started", "The ship explores along the coast");
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
