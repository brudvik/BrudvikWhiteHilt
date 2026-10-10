using BepInEx.Configuration;
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

    /// <summary>Switches between the White Hilt HUD and the game's own.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyToggleStyle { get; private set; }

    /// <summary>Shows every part of the console while held, with <see cref="Calm"/>.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyShowAll { get; private set; }

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
        KeyToggleStyle = WhiteHiltConfig.BindLocal(Section + ".Keys", "ToggleStyle", KeyboardShortcut.Empty,
            "Switch between the White Hilt HUD and the game's own.");
        KeyShowAll = WhiteHiltConfig.BindLocal(Section + ".Keys", "ShowAll", KeyboardShortcut.Empty,
            "With Calm: show every part of the console while the key is held.");
    }
}
