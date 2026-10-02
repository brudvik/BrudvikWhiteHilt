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
    private static ConfigEntry<float> runePostRange;
    private static ConfigEntry<int> valkyrieCost;
    private static ConfigEntry<bool> valkyrieOncePerDeath;

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
    /// How close a rune post must stand to a portal, in metres; 0 turns the rune posts off.
    /// </summary>
    public static float RunePostRange => runePostRange != null ? runePostRange.Value : 8f;

    /// <summary>
    /// Surtling Cores a trip with the Valkyrie Stone costs.
    /// </summary>
    public static int ValkyrieCost => valkyrieCost != null ? valkyrieCost.Value : 1;

    /// <summary>
    /// True if the Valkyrie Stone takes a player to each death point only once.
    /// </summary>
    public static bool ValkyrieOncePerDeath => valkyrieOncePerDeath == null || valkyrieOncePerDeath.Value;

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
        runePostRange = WhiteHiltConfig.BindAdminOnly(Section, "RunePostRange", 8f,
            "How close, in metres, a rune post must stand to the portal you travel from for its runes to count. 0 turns the rune posts off.",
            new AcceptableValueRange<float>(0f, 30f));
        valkyrieCost = WhiteHiltConfig.BindAdminOnly(Section, "ValkyrieStoneCost", 1,
            "Surtling Cores a trip to your last death point with the Valkyrie Stone costs. 0 makes it free.",
            new AcceptableValueRange<int>(0, 20));
        valkyrieOncePerDeath = WhiteHiltConfig.BindAdminOnly(Section, "ValkyrieStoneOncePerDeath", true,
            "The Valkyrie Stone takes you to each death point only once. Off: as often as you like.");
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
