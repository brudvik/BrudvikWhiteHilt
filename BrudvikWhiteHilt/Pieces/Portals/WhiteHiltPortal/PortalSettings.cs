using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Pieces.Portals.WhiteHiltPortal;

/// <summary>
/// Config entries of the White Hilt portals and the Home Stone.
/// </summary>
public static class PortalSettings
{
    private const string Section = "Portals";

    private static ConfigEntry<bool> teleportAnything;
    private static ConfigEntry<bool> ownerOnlyEdit;
    private static ConfigEntry<float> homeCooldownMinutes;
    private static ConfigEntry<float> homeReturnMinutes;
    private static ConfigEntry<bool> sortByDistance;

    /// <summary>
    /// True if the portals let everything through, runes or not.
    /// </summary>
    public static bool TeleportAnything => teleportAnything != null && teleportAnything.Value;

    /// <summary>
    /// True if only a portal's builder may rename it and make it private.
    /// </summary>
    public static bool OwnerOnlyEdit => ownerOnlyEdit == null || ownerOnlyEdit.Value;

    /// <summary>
    /// Minutes the Home Stone rests after use.
    /// </summary>
    public static float HomeCooldownMinutes => homeCooldownMinutes != null ? homeCooldownMinutes.Value : 5f;

    /// <summary>
    /// Minutes after going home in which the Home Stone takes the player back to where they came from; 0 turns it off.
    /// </summary>
    public static float HomeReturnMinutes => homeReturnMinutes != null ? homeReturnMinutes.Value : 2f;

    /// <summary>
    /// True if the travel list is sorted by distance instead of by name. Each player's own choice.
    /// </summary>
    public static bool SortByDistance
    {
        get => sortByDistance != null && sortByDistance.Value;
        set
        {
            if (sortByDistance != null)
            {
                sortByDistance.Value = value;
            }
        }
    }

    /// <summary>
    /// Binds the config entries. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        teleportAnything = WhiteHiltConfig.BindAdminOnly(Section, "TeleportAnything", false,
            "Let everything through the White Hilt portals, ore and metal too. Otherwise the usual rules and the rune posts decide.");
        ownerOnlyEdit = WhiteHiltConfig.BindAdminOnly(Section, "OwnerOnlyEdit", true, "Only the builder of a portal may rename it and make it private.");
        homeCooldownMinutes = WhiteHiltConfig.BindAdminOnly(Section, "HomeCooldownMinutes", 5f, "Minutes before the Home Stone can be used again.",
            new AcceptableValueRange<float>(0f, 240f));
        homeReturnMinutes = WhiteHiltConfig.BindAdminOnly(Section, "HomeReturnMinutes", 2f,
            "Minutes after going home in which the Home Stone takes you back to where you were, even while it rests. 0 turns this off.",
            new AcceptableValueRange<float>(0f, 60f));
        sortByDistance = WhiteHiltConfig.BindLocal(Section, "SortByDistance", false, "Sort the portal list by distance instead of by name.");

        Translations.AddEnglish("whitehilt_portal_travel", "Travel");
        Translations.AddEnglish("whitehilt_portal_take_me_home", "Take me home");
        Translations.AddEnglish("whitehilt_portal_rename", "Name the portal");
        Translations.AddEnglish("whitehilt_portal_name", "Portal name");
        Translations.AddEnglish("whitehilt_portal_notowner", "Only the builder can change this portal");
        Translations.AddEnglish("whitehilt_portal_title", "Portals");
        Translations.AddEnglish("whitehilt_portal_search", "Search...");
        Translations.AddEnglish("whitehilt_portal_sort_name", "Sort: name");
        Translations.AddEnglish("whitehilt_portal_sort_distance", "Sort: distance");
        Translations.AddEnglish("whitehilt_portal_make_private", "Make private");
        Translations.AddEnglish("whitehilt_portal_make_public", "Make public");
        Translations.AddEnglish("whitehilt_portal_set_home", "Set as home");
        Translations.AddEnglish("whitehilt_portal_is_home", "Home");
        Translations.AddEnglish("whitehilt_portal_close", "Close");
        Translations.AddEnglish("whitehilt_portal_none", "No other portals yet");
        Translations.AddEnglish("whitehilt_portal_here", "you are here");
        Translations.AddEnglish("msg_whitehilt_portal_home_set", "This portal is now your home");
        Translations.AddEnglish("msg_whitehilt_portal_gone", "That portal is gone");
    }
}
