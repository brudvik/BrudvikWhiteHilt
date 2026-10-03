using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Navigation.Compass;

/// <summary>Local settings for the horizontal HUD compass.</summary>
public static class HudCompassSettings
{
    private const string Section = "HUD.Compass";

    /// <summary>Whether the compass is enabled.</summary>
    public static ConfigEntry<bool> Enabled { get; private set; }
    /// <summary>Tape width in UI units before scale.</summary>
    public static ConfigEntry<float> CompassWidth { get; private set; }
    /// <summary>Tape height in UI units before scale.</summary>
    public static ConfigEntry<float> CompassHeight { get; private set; }
    /// <summary>Total visible angular interval.</summary>
    public static ConfigEntry<float> VisibleDegrees { get; private set; }
    /// <summary>Interval between small ticks.</summary>
    public static ConfigEntry<int> TickInterval { get; private set; }
    /// <summary>Interval between medium ticks.</summary>
    public static ConfigEntry<int> MediumTickInterval { get; private set; }
    /// <summary>Interval between degree labels.</summary>
    public static ConfigEntry<int> NumberInterval { get; private set; }
    /// <summary>Interval between large ticks.</summary>
    public static ConfigEntry<int> MajorTickInterval { get; private set; }
    /// <summary>Whether degree labels are visible.</summary>
    public static ConfigEntry<bool> ShowDegrees { get; private set; }
    /// <summary>Whether the eight direction labels are visible.</summary>
    public static ConfigEntry<bool> ShowCardinalDirections { get; private set; }
    /// <summary>Whether world markers are visible.</summary>
    public static ConfigEntry<bool> MarkerVisibility { get; private set; }
    /// <summary>Marker icon size in UI units.</summary>
    public static ConfigEntry<float> MarkerSize { get; private set; }
    /// <summary>Whether own unchecked saved map pins are shown.</summary>
    public static ConfigEntry<bool> ShowOwnPins { get; private set; }
    /// <summary>Whether known boss pins are shown.</summary>
    public static ConfigEntry<bool> ShowBosses { get; private set; }
    /// <summary>Whether own death pins are shown.</summary>
    public static ConfigEntry<bool> ShowDeath { get; private set; }
    /// <summary>Distance down from the HUD's top edge.</summary>
    public static ConfigEntry<float> VerticalPosition { get; private set; }
    /// <summary>Horizontal offset from the HUD's centre.</summary>
    public static ConfigEntry<float> HorizontalPosition { get; private set; }
    /// <summary>Scale of the entire tape.</summary>
    public static ConfigEntry<float> Scale { get; private set; }
    /// <summary>Opacity of tape, indicator and markers.</summary>
    public static ConfigEntry<float> Opacity { get; private set; }
    /// <summary>Share of each half of the tape used for edge fading.</summary>
    public static ConfigEntry<float> EdgeFade { get; private set; }
    /// <summary>Opacity of the subdued background.</summary>
    public static ConfigEntry<float> BackgroundOpacity { get; private set; }
    /// <summary>Gap between compass and the HUD below it.</summary>
    public static ConfigEntry<float> LayoutGap { get; private set; }
    /// <summary>Seconds between checks for new and removed map pins.</summary>
    public static ConfigEntry<float> MarkerRefreshSeconds { get; private set; }

    /// <summary>Binds each player's own compass settings.</summary>
    public static void Initialize()
    {
        Enabled = WhiteHiltConfig.BindLocal(Section, "Enabled", true, "Show the horizontal camera-heading compass above the clock.");
        CompassWidth = WhiteHiltConfig.BindLocal(Section, "CompassWidth", 700f, "Width before scale; constrained to available HUD space.", new AcceptableValueRange<float>(200f, 1600f));
        CompassHeight = WhiteHiltConfig.BindLocal(Section, "CompassHeight", 52f, "Height before scale.", new AcceptableValueRange<float>(45f, 120f));
        VisibleDegrees = WhiteHiltConfig.BindLocal(Section, "VisibleDegrees", 120f, "Total visible degrees, centred on the exact camera heading.", new AcceptableValueRange<float>(30f, 180f));
        TickInterval = WhiteHiltConfig.BindLocal(Section, "TickInterval", 5, "Degrees between small ticks.", new AcceptableValueList<int>(1, 2, 3, 5, 6, 10, 15, 30, 45, 90));
        MediumTickInterval = WhiteHiltConfig.BindLocal(Section, "MediumTickInterval", 15, "Degrees between medium ticks.", new AcceptableValueList<int>(5, 10, 15, 30, 45, 90));
        NumberInterval = WhiteHiltConfig.BindLocal(Section, "NumberInterval", 15, "Degrees between numbers; direction labels take precedence.", new AcceptableValueList<int>(5, 10, 15, 30, 45, 90));
        MajorTickInterval = WhiteHiltConfig.BindLocal(Section, "MajorTickInterval", 45, "Degrees between large ticks.", new AcceptableValueList<int>(15, 30, 45, 90));
        ShowDegrees = WhiteHiltConfig.BindLocal(Section, "ShowDegrees", true, "Show three-digit degree labels on the moving tape.");
        ShowCardinalDirections = WhiteHiltConfig.BindLocal(Section, "ShowCardinalDirections", true, "Show N, NE, E, SE, S, SW, W and NW; cardinal directions are larger.");
        MarkerVisibility = WhiteHiltConfig.BindLocal(Section, "MarkerVisibility", true, "Show world markers; markers outside the visible angle are hidden, not clamped.");
        MarkerSize = WhiteHiltConfig.BindLocal(Section, "MarkerSize", 14f, "World marker icon size before scale.", new AcceptableValueRange<float>(8f, 24f));
        ShowOwnPins = WhiteHiltConfig.BindLocal(Section, "ShowOwnPins", true, "Show your unchecked saved map pins; shared and custom pins are excluded.");
        ShowBosses = WhiteHiltConfig.BindLocal(Section, "ShowBosses", true, "Show known boss locations already present on your map.");
        ShowDeath = WhiteHiltConfig.BindLocal(Section, "ShowDeath", true, "Show your death markers already present on your map.");
        VerticalPosition = WhiteHiltConfig.BindLocal(Section, "VerticalPosition", 8f, "Distance down from the top of the HUD.", new AcceptableValueRange<float>(0f, 1000f));
        HorizontalPosition = WhiteHiltConfig.BindLocal(Section, "HorizontalPosition", 0f, "Offset from the HUD centre; constrained to keep the tape on screen.", new AcceptableValueRange<float>(-1600f, 1600f));
        Scale = WhiteHiltConfig.BindLocal(Section, "Scale", 1f, "Scale of the entire compass.", new AcceptableValueRange<float>(0.5f, 2f));
        Opacity = WhiteHiltConfig.BindLocal(Section, "Opacity", 0.9f, "Overall opacity; zero hides the compass without reserving space.", new AcceptableValueRange<float>(0f, 1f));
        EdgeFade = WhiteHiltConfig.BindLocal(Section, "EdgeFade", 0.2f, "Share of each half used for fading at the edges; zero disables fading.", new AcceptableValueRange<float>(0f, 1f));
        BackgroundOpacity = WhiteHiltConfig.BindLocal(Section, "BackgroundOpacity", 0.08f, "Dark background opacity; zero removes the background.", new AcceptableValueRange<float>(0f, 0.5f));
        LayoutGap = WhiteHiltConfig.BindLocal(Section, "LayoutGap", 12f, "Gap before boss bars, clock and waypoint arrow below the compass.", new AcceptableValueRange<float>(0f, 60f));
        MarkerRefreshSeconds = WhiteHiltConfig.BindLocal(Section, "MarkerRefreshSeconds", 0.5f, "Seconds between map pin membership checks; bearings still update every frame.", new AcceptableValueRange<float>(0.1f, 5f));
    }
}