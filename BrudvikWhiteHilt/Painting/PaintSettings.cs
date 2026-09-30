using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Painting;

/// <summary>
/// Config and text for painting, section "Painting".
/// </summary>
public static class PaintSettings
{
    private const string Section = "Painting";

    /// <summary>How many pieces one paint pot covers.</summary>
    public const int PotUses = 20;

    /// <summary>Largest brush radius, in metres.</summary>
    public static ConfigEntry<float> MaxRadius { get; private set; }

    /// <summary>The player's brush radius, 0 for only the aimed piece.</summary>
    public static ConfigEntry<float> Radius { get; private set; }

    /// <summary>The player's saved colours, as comma-separated hex.</summary>
    public static ConfigEntry<string> Favourites { get; private set; }

    /// <summary>
    /// Binds the config entries and registers the English text. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        MaxRadius = WhiteHiltConfig.BindAdminOnly(Section, "MaxRadius", 8f, "Largest radius of the paint brush, in metres.",
            new AcceptableValueRange<float>(0f, 30f));
        Radius = WhiteHiltConfig.BindLocal(Section, "Radius", 0f, "Paint brush radius in metres; 0 paints only the piece you aim at. The mouse wheel changes it.");
        Favourites = WhiteHiltConfig.BindLocal(Section, "Favourites", "F2EEDC,2B2B2B,8E2B22,2F4F7F,3F6B35,C9A227,6B4A2E,7A4E8C",
            "Your saved colours at the Paint Bench, as hex.");

        Translations.AddEnglish("whitehilt_paint_mix", "Mix paint");
        Translations.AddEnglish("whitehilt_paint_title", "Paint Bench");
        Translations.AddEnglish("whitehilt_paint_brightness", "Brightness");
        Translations.AddEnglish("whitehilt_paint_save", "Save colour");
        Translations.AddEnglish("whitehilt_paint_close", "Close");
        Translations.AddEnglish("whitehilt_paint_match", "Match {0}%");
        Translations.AddEnglish("whitehilt_paint_binder", "Resin (binder)");
        Translations.AddEnglish("whitehilt_paint_have", "have {0}");
        Translations.AddEnglish("whitehilt_paint_missing", "You lack what this colour needs. Closest mix from all dyes:");
        Translations.AddEnglish("whitehilt_paint_no_dyes", "No dyes found");
        Translations.AddEnglish("whitehilt_paint_hint", "Pick a colour on the wheel, or type it. The dyes are found in your inventory and in nearby chests.");
        Translations.AddEnglish("msg_whitehilt_paint_mixed", "You mix a pot of paint");
        Translations.AddEnglish("msg_whitehilt_paint_full", "Your inventory is full");
        Translations.AddEnglish("msg_whitehilt_paint_lacking", "You lack the dyes for this colour");
        Translations.AddEnglish("msg_whitehilt_brush_loaded", "The brush is loaded with {0}");
        Translations.AddEnglish("msg_whitehilt_brush_none", "You need a White Hilt Paint Brush");
        Translations.AddEnglish("msg_whitehilt_brush_empty", "Load the brush first: use a paint pot");
        Translations.AddEnglish("msg_whitehilt_brush_no_paint", "You have no paint of this colour left");
        Translations.AddEnglish("msg_whitehilt_brush_painted", "Painted {0} pieces");
        Translations.AddEnglish("msg_whitehilt_brush_cleaned", "Paint removed from {0} pieces");
        Translations.AddEnglish("msg_whitehilt_brush_nothing", "Nothing to paint here");
        Translations.AddEnglish("msg_whitehilt_brush_picked", "Colour picked: {0}");
        Translations.AddEnglish("msg_whitehilt_brush_unpainted", "That piece is not painted");
        Translations.AddEnglish("whitehilt_brush_hud", "Brush {0}   {1} left   radius {2}");
        Translations.AddEnglish("whitehilt_brush_hud_empty", "Brush not loaded: use a paint pot   radius {0}");
        Translations.AddEnglish("whitehilt_brush_single", "one piece");
        Translations.AddEnglish("whitehilt_paint_tooltip", "Colour");
    }
}
