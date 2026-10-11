using BepInEx.Configuration;
using BrudvikWhiteHilt.Clock;
using BrudvikWhiteHilt.Navigation.Compass;
using BrudvikWhiteHilt.Party;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.HudLayout;

/// <summary>
/// Which HUD the player sees: White Hilt's console, or the game's own layout with White Hilt's additions where they
/// have always been.
/// </summary>
public enum HudStyle
{
    /// <summary>Health, stamina, eitr, food, the hotbar and status effects gathered in one console at the bottom centre.</summary>
    WhiteHilt,

    /// <summary>The game's own HUD.</summary>
    Vanilla,
}

/// <summary>
/// When the key hints show in the White Hilt HUD: the game's, at the right of the console, and the build tools', at
/// its left.
/// </summary>
public enum HudHints
{
    /// <summary>A few seconds after taking out a tool or weapon, or opening the inventory, then they fade.</summary>
    Brief,

    /// <summary>Whenever the game would show them.</summary>
    Always,

    /// <summary>Only while the show-all key is held, and in the inventory.</summary>
    Hidden,
}

/// <summary>
/// A ready-made HUD layout that sets the sizes and the quiet screen at once.
/// </summary>
public enum HudPreset
{
    /// <summary>The settings as they are; choosing it changes nothing.</summary>
    Custom,

    /// <summary>Smaller, with the quiet screen on.</summary>
    Compact,

    /// <summary>The default sizes.</summary>
    Standard,

    /// <summary>Larger text and icons and the key hints always shown, for a TV or a large screen.</summary>
    Large,
}

/// <summary>
/// Each player's own HUD settings, section "HUD".
/// </summary>
public static class HudSettings
{
    private const string Section = "HUD";

    /// <summary>The HUD the player sees.</summary>
    public static ConfigEntry<HudStyle> Style { get; private set; }

    /// <summary>Size of the console's bars, food and status effects; the hotbar keeps the game's size.</summary>
    public static ConfigEntry<float> Scale { get; private set; }

    /// <summary>Distance from the bottom of the screen to the hotbar, in pixels.</summary>
    public static ConfigEntry<float> BottomMargin { get; private set; }

    /// <summary>Status effects per row beside the hotbar.</summary>
    public static ConfigEntry<int> StatusColumns { get; private set; }

    /// <summary>Most status effects shown; the last place counts the rest.</summary>
    public static ConfigEntry<int> MaxStatusEffects { get; private set; }

    /// <summary>Whether bars, food, status effects and the hotbar fade while nothing about them changes.</summary>
    public static ConfigEntry<bool> Calm { get; private set; }

    /// <summary>Seconds a part stays after it last changed, with <see cref="Calm"/>.</summary>
    public static ConfigEntry<float> CalmSeconds { get; private set; }

    /// <summary>How much of a faded part still shows, with <see cref="Calm"/>.</summary>
    public static ConfigEntry<float> CalmOpacity { get; private set; }

    /// <summary>Corner radius of the panel behind the console and the band behind the top, in pixels.</summary>
    public static ConfigEntry<float> PanelRadius { get; private set; }

    /// <summary>What sits behind the compass, clock and waypoint.</summary>
    public static ConfigEntry<TopBackground> Top { get; private set; }

    /// <summary>How dark the background behind the top is.</summary>
    public static ConfigEntry<float> TopOpacity { get; private set; }

    /// <summary>How far down the darkening reaches with <see cref="TopBackground.Fade"/>, in pixels.</summary>
    public static ConfigEntry<float> TopFadeHeight { get; private set; }

    /// <summary>How dark the rounded panel behind the console is; 0 for none.</summary>
    public static ConfigEntry<float> PanelOpacity { get; private set; }

    /// <summary>The minimap in the White Hilt HUD: round with a compass ring, or the game's own.</summary>
    public static ConfigEntry<MinimapStyle> Minimap { get; private set; }

    /// <summary>Whether pins and the waypoint beyond the round map show on its ring.</summary>
    public static ConfigEntry<bool> MinimapRingMarkers { get; private set; }

    /// <summary>How dark the round map's ring is.</summary>
    public static ConfigEntry<float> MinimapRingOpacity { get; private set; }

    /// <summary>A ready-made layout; choosing one sets the sizes and the quiet screen.</summary>
    public static ConfigEntry<HudPreset> Preset { get; private set; }

    /// <summary>Whether worn gear shows among the status effects.</summary>
    public static ConfigEntry<bool> ShowWear { get; private set; }

    /// <summary>Durability share under which a piece of gear shows.</summary>
    public static ConfigEntry<float> WearWarning { get; private set; }

    /// <summary>Whether a heavy load shows among the status effects.</summary>
    public static ConfigEntry<bool> ShowWeight { get; private set; }

    /// <summary>Share of the carry weight from which the load shows.</summary>
    public static ConfigEntry<float> WeightWarning { get; private set; }

    /// <summary>Whether the creature you fight shows in the strip.</summary>
    public static ConfigEntry<bool> ShowTarget { get; private set; }

    /// <summary>Whether stations near you that need you show in the strip.</summary>
    public static ConfigEntry<bool> ShowProduction { get; private set; }

    /// <summary>Metres around you the stations in the strip are looked for.</summary>
    public static ConfigEntry<float> ProductionRange { get; private set; }

    /// <summary>Whether a red arc shows the side a hit came from.</summary>
    public static ConfigEntry<bool> DamageDirection { get; private set; }

    /// <summary>When the key hints show.</summary>
    public static ConfigEntry<HudHints> KeyHints { get; private set; }

    /// <summary>Seconds the key hints stay, with <see cref="HudHints.Brief"/>.</summary>
    public static ConfigEntry<float> KeyHintSeconds { get; private set; }

    /// <summary>Switches between the White Hilt HUD and the game's own.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyToggleStyle { get; private set; }

    /// <summary>Shows every part of the console while held, with <see cref="Calm"/>.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyShowAll { get; private set; }

    /// <summary>
    /// Sets the sizes, the quiet screen and the key hints of a ready-made layout.
    /// </summary>
    /// <param name="preset">The layout; Custom leaves everything as it is.</param>
    public static void ApplyPreset(HudPreset preset)
    {
        (float scale, float margin, int columns, int max, bool calm, HudHints hints, float compass, float party, int clock) = preset switch
        {
            HudPreset.Compact => (0.8f, 8f, 5, 10, true, HudHints.Brief, 0.85f, 0.85f, 17),
            HudPreset.Standard => (1f, 16f, 4, 8, false, HudHints.Brief, 1f, 1f, 20),
            HudPreset.Large => (1.35f, 24f, 3, 6, false, HudHints.Always, 1.3f, 1.3f, 26),
            _ => (0f, 0f, 0, 0, false, HudHints.Brief, 0f, 0f, 0)
        };
        if (preset == HudPreset.Custom)
        {
            return;
        }

        Scale.Value = scale;
        BottomMargin.Value = margin;
        StatusColumns.Value = columns;
        MaxStatusEffects.Value = max;
        Calm.Value = calm;
        KeyHints.Value = hints;
        HudCompassSettings.Scale.Value = compass;
        PartySettings.Scale.Value = party;
        ClockSettings.FontSize.Value = clock;
    }

    /// <summary>Whether the White Hilt console is the chosen HUD.</summary>
    public static bool WhiteHilt => Style != null && Style.Value == HudStyle.WhiteHilt;

    /// <summary>
    /// Binds the config entries. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        Style = WhiteHiltConfig.BindLocal(Section, "Style", HudStyle.WhiteHilt,
            "WhiteHilt gathers health, stamina, eitr, food, the guardian power, the hotbar and status effects in one console at the bottom centre, with the clock and waypoint at the ends of the compass. Vanilla keeps the game's own HUD.");
        Scale = WhiteHiltConfig.BindLocal(Section, "Scale", 1f, "Size of the console's bars, food and status effects. The hotbar keeps the game's size.",
            new AcceptableValueRange<float>(0.6f, 1.6f));
        BottomMargin = WhiteHiltConfig.BindLocal(Section, "BottomMargin", 16f, "Distance from the bottom of the screen to the hotbar, in pixels.",
            new AcceptableValueRange<float>(0f, 400f));
        StatusColumns = WhiteHiltConfig.BindLocal(Section, "StatusColumns", 4, "Status effects per row to the right of the hotbar.",
            new AcceptableValueRange<int>(2, 10));
        MaxStatusEffects = WhiteHiltConfig.BindLocal(Section, "MaxStatusEffects", 8,
            "Most status effects shown to the right of the hotbar; when there are more, the last place shows how many are left out.",
            new AcceptableValueRange<int>(2, 30));
        Calm = WhiteHiltConfig.BindLocal(Section, "Calm", false,
            "A quiet screen: a bar shows while it is not full, the food while some runs low, the status effects for a while after one comes or goes and the hotbar after a hotbar key or a change of weapon; else they fade. Everything shows in the inventory.");
        CalmSeconds = WhiteHiltConfig.BindLocal(Section, "CalmSeconds", 4f, "With Calm: seconds a part stays after it last changed.",
            new AcceptableValueRange<float>(1f, 30f));
        CalmOpacity = WhiteHiltConfig.BindLocal(Section, "CalmOpacity", 0f, "With Calm: how much of a faded part still shows; 0 hides it.",
            new AcceptableValueRange<float>(0f, 0.8f));
        PanelOpacity = WhiteHiltConfig.BindLocal(Section, "PanelOpacity", 0.45f,
            "How dark the rounded panel behind the console is, so the bars, numbers and icons read against snow and sky. 0 for no panel.",
            new AcceptableValueRange<float>(0f, 0.9f));
        PanelRadius = WhiteHiltConfig.BindLocal(Section, "PanelRadius", 14f,
            "Corner radius, in pixels, of the panel behind the console and the band behind the top. 0 for square corners.",
            new AcceptableValueRange<float>(0f, 40f));
        Top = WhiteHiltConfig.BindLocal(Section, "TopBackground", TopBackground.Band,
            "What sits behind the compass, clock and waypoint so they read against a bright sky: Band, one rounded band round them as the panel behind the console; Fade, the top of the screen darkened and fading out downwards; or None.");
        TopOpacity = WhiteHiltConfig.BindLocal(Section, "TopOpacity", 0.45f, "How dark the band or the darkening behind the top is.",
            new AcceptableValueRange<float>(0.05f, 0.9f));
        TopFadeHeight = WhiteHiltConfig.BindLocal(Section, "TopFadeHeight", 140f,
            "With TopBackground Fade: how far down from the top of the screen the darkening reaches, in pixels.",
            new AcceptableValueRange<float>(40f, 600f));
        Minimap = WhiteHiltConfig.BindLocal(Section, "Minimap", MinimapStyle.Round,
            "The minimap in the White Hilt HUD: Round, in a dark compass ring with N, E, S and W and what lies beyond the map on the ring; or Vanilla, the game's own square map.");
        MinimapRingMarkers = WhiteHiltConfig.BindLocal(Section, "MinimapRingMarkers", true,
            "With the round minimap: show your pins, bosses, your death and the waypoint that lie beyond what the map shows on its ring, in the direction they lie.");
        MinimapRingOpacity = WhiteHiltConfig.BindLocal(Section, "MinimapRingOpacity", 0.8f, "How dark the round minimap's ring is.",
            new AcceptableValueRange<float>(0.2f, 1f));
        ShowWear = WhiteHiltConfig.BindLocal(Section, "ShowWear", true,
            "Show worn gear you have on among the status effects, with how much is left; it flashes red when broken.");
        WearWarning = WhiteHiltConfig.BindLocal(Section, "WearWarning", 0.2f, "Gear shows when its durability is under this share.",
            new AcceptableValueRange<float>(0.05f, 0.9f));
        ShowWeight = WhiteHiltConfig.BindLocal(Section, "ShowWeight", true,
            "Show your load among the status effects when it nears what you can carry; red when it is too much.");
        WeightWarning = WhiteHiltConfig.BindLocal(Section, "WeightWarning", 0.85f, "The load shows from this share of what you can carry.",
            new AcceptableValueRange<float>(0.5f, 1f));
        ShowTarget = WhiteHiltConfig.BindLocal(Section, "ShowTarget", true,
            "Show the creature under the crosshair, or the nearest one hunting you, in the strip over the console: its stars, health and what it is weak to, resists and is immune to.");
        ShowProduction = WhiteHiltConfig.BindLocal(Section, "ShowProduction", true,
            "Show kilns, smelters and other stations near you that have stopped, need you or are done in the strip over the console.");
        ProductionRange = WhiteHiltConfig.BindLocal(Section, "ProductionRange", 20f, "Metres around you the stations in the strip are looked for.",
            new AcceptableValueRange<float>(5f, 60f));
        DamageDirection = WhiteHiltConfig.BindLocal(Section, "DamageDirection", true,
            "Show a red arc at the edge of the screen on the side a hit came from, also from behind. With either HUD.");
        Preset = WhiteHiltConfig.BindLocal(Section, "Preset", HudPreset.Custom,
            "A ready-made layout: Compact (smaller, quiet screen), Standard or Large (for a TV or a large screen). Choosing one sets the console's, compass's, clock's and party list's sizes, the quiet screen and the key hints; Custom changes nothing.");
        Preset.SettingChanged += (_, _) => ApplyPreset(Preset.Value);
        KeyHints = WhiteHiltConfig.BindLocal(Section, "KeyHints", HudHints.Brief,
            "When the key hints show beside the console: Brief, for a few seconds after taking out a tool or weapon; Always; or Hidden. They always show in the inventory and while the ShowAll key is held.");
        KeyHintSeconds = WhiteHiltConfig.BindLocal(Section, "KeyHintSeconds", 6f, "With Brief key hints: seconds they stay.",
            new AcceptableValueRange<float>(1f, 30f));
        KeyToggleStyle = WhiteHiltConfig.BindLocal(Section + ".Keys", "ToggleStyle", KeyboardShortcut.Empty,
            "Switch between the White Hilt HUD and the game's own.");
        KeyShowAll = WhiteHiltConfig.BindLocal(Section + ".Keys", "ShowAll", KeyboardShortcut.Empty,
            "With Calm: show every part of the console while the key is held.");
    }
}
