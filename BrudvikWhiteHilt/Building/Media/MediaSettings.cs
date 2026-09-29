using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using UnityEngine;

namespace BrudvikWhiteHilt.Building.Media;

/// <summary>
/// Config and texts for the build camera's media mode: photos, photo view and camera-path films. All settings are local.
/// </summary>
public static class MediaSettings
{
    private const string Section = "BuildTools.Media";

    /// <summary>Photo size as a multiple of the screen resolution.</summary>
    public static ConfigEntry<int> PhotoSize { get; private set; }

    /// <summary>Takes a photo without HUD or build helpers.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyPhoto { get; private set; }

    /// <summary>Switches photo view: everything but the world hidden while you fly.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyPhotoView { get; private set; }

    /// <summary>Opens and closes the media panel.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyPanel { get; private set; }

    /// <summary>
    /// Binds the config entries and adds the English texts. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        AddTranslations();
        PhotoSize = WhiteHiltConfig.BindLocal(Section, "PhotoSize", 2,
            "Photo size as a multiple of the screen resolution: 1, 2 or 4. Larger photos take longer to save.");
        KeyPhoto = WhiteHiltConfig.BindLocal(Section, "Photo", new KeyboardShortcut(KeyCode.P), "Take a photo from the build camera, without HUD or build helpers.");
        KeyPhotoView = WhiteHiltConfig.BindLocal(Section, "PhotoView", new KeyboardShortcut(KeyCode.O), "Photo view on and off: only the world is shown, the mouse wheel zooms.");
        KeyPanel = WhiteHiltConfig.BindLocal(Section, "Panel", new KeyboardShortcut(KeyCode.U), "Open and close the media panel with time of day, weather and films.");
    }

    private static void AddTranslations()
    {
        Translations.AddEnglish("whitehilt_media", "Media");
        Translations.AddEnglish("whitehilt_media_photo", "Take photo");
        Translations.AddEnglish("whitehilt_media_photoview", "Photo view");
        Translations.AddEnglish("whitehilt_media_hide_character", "Hide my character");
        Translations.AddEnglish("whitehilt_media_time", "Time");
        Translations.AddEnglish("whitehilt_media_weather", "Weather");
        Translations.AddEnglish("whitehilt_media_real", "Real");
        Translations.AddEnglish("whitehilt_media_hour_back", "-1 h");
        Translations.AddEnglish("whitehilt_media_hour_forward", "+1 h");
        Translations.AddEnglish("whitehilt_media_video", "Film");
        Translations.AddEnglish("whitehilt_media_film_count", "Film {0}/{1}");
        Translations.AddEnglish("whitehilt_media_new", "New");
        Translations.AddEnglish("whitehilt_media_delete", "Delete");
        Translations.AddEnglish("whitehilt_media_title", "Title");
        Translations.AddEnglish("whitehilt_media_author", "Name");
        Translations.AddEnglish("whitehilt_media_date", "Date");
        Translations.AddEnglish("whitehilt_media_fade", "Fade in/out");
        Translations.AddEnglish("whitehilt_media_titlecard", "Title card");
        Translations.AddEnglish("whitehilt_media_point", "Point {0}");
        Translations.AddEnglish("whitehilt_media_point_start", "Start");
        Translations.AddEnglish("whitehilt_media_point_end", "End");
        Translations.AddEnglish("whitehilt_media_point_info", "{0} s to next   pause {1} s   {2}");
        Translations.AddEnglish("whitehilt_media_point_last", "pause {0} s");
        Translations.AddEnglish("whitehilt_media_confirm", "Sure?");
        Translations.AddEnglish("whitehilt_media_smooth", "smooth");
        Translations.AddEnglish("whitehilt_media_linear", "even");
        Translations.AddEnglish("whitehilt_media_no_points", "No points yet. Fly to a spot and press \"Add here\".");
        Translations.AddEnglish("whitehilt_media_add", "Add here");
        Translations.AddEnglish("whitehilt_media_update", "Update");
        Translations.AddEnglish("whitehilt_media_goto", "Go to");
        Translations.AddEnglish("whitehilt_media_up", "Up");
        Translations.AddEnglish("whitehilt_media_down", "Down");
        Translations.AddEnglish("whitehilt_media_transition", "Transition");
        Translations.AddEnglish("whitehilt_media_seconds_less", "Time -");
        Translations.AddEnglish("whitehilt_media_seconds_more", "Time +");
        Translations.AddEnglish("whitehilt_media_hold_less", "Pause -");
        Translations.AddEnglish("whitehilt_media_hold_more", "Pause +");
        Translations.AddEnglish("whitehilt_media_preview", "Preview");
        Translations.AddEnglish("whitehilt_media_play", "Play");
        Translations.AddEnglish("whitehilt_media_footer", "Hold the right mouse button to look around. W A S D fly.\nStart your recorder (OBS, Win+Alt+R) during the countdown. Esc stops.");
        Translations.AddEnglish("msg_whitehilt_media_saved", "Photo saved: {0}");
        Translations.AddEnglish("msg_whitehilt_media_need_points", "A film needs at least two points");
        Translations.AddEnglish("msg_whitehilt_media_camera_only", "Switch on the build camera first");
    }
}
