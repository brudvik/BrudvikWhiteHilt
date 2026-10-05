using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Quartermaster;

/// <summary>
/// Config and texts for the Quartermaster's Table: how far around it the chests are gathered, and how much of an item
/// the store always keeps.
/// </summary>
public static class QuartermasterSettings
{
    private const string Section = "Quartermaster";

    /// <summary>Whether the table opens the store.</summary>
    public static ConfigEntry<bool> Enabled { get; private set; }

    /// <summary>Range around the table, in metres.</summary>
    public static ConfigEntry<float> Range { get; private set; }

    /// <summary>Whether items players stored themselves are offered too, not only unlimited ones.</summary>
    public static ConfigEntry<bool> IncludeFinite { get; private set; }

    /// <summary>How many of an item players stored the store always keeps.</summary>
    public static ConfigEntry<int> KeepAtLeast { get; private set; }

    /// <summary>Whether the range shows as a ring on the ground while placing or looking at the table.</summary>
    public static ConfigEntry<bool> ShowRangeRing { get; private set; }

    /// <summary>
    /// Binds the config entries and adds the English texts. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        Enabled = WhiteHiltConfig.BindAdminOnly(Section, "Enabled", true, "The Quartermaster's Table opens the store of the chests around it.");
        Range = WhiteHiltConfig.BindAdminOnly(Section, "Range", 30f, "How far from the table a chest, cart or ship hold may be, in metres.",
            new AcceptableValueRange<float>(5f, 100f));
        IncludeFinite = WhiteHiltConfig.BindAdminOnly(Section, "IncludeFinite", true,
            "Also offer items players stored themselves, not only items the chests keep without limit.");
        KeepAtLeast = WhiteHiltConfig.BindAdminOnly(Section, "KeepAtLeast", 1,
            "How many of an item players stored themselves always stay in the chests, so the store is never emptied of it.",
            new AcceptableValueRange<int>(0, 50));
        ShowRangeRing = WhiteHiltConfig.BindLocal(Section, "ShowRangeRing", true, "Show the table's range on the ground while placing or looking at it.");

        Translations.AddEnglish("whitehilt_qm_open", "Open the store");
        Translations.AddEnglish("whitehilt_qm_chests", "{0} chests within {1} m");
        Translations.AddEnglish("whitehilt_qm_title", "The Store");
        Translations.AddEnglish("whitehilt_qm_tab_store", "Store");
        Translations.AddEnglish("whitehilt_qm_tab_pack", "Pack for a build");
        Translations.AddEnglish("whitehilt_qm_all", "All");
        Translations.AddEnglish("whitehilt_qm_other", "Other");
        Translations.AddEnglish("whitehilt_qm_search", "Search...");
        Translations.AddEnglish("whitehilt_qm_empty", "There is nothing to take in the chests around the table.");
        Translations.AddEnglish("whitehilt_qm_take_hint", "Click: take a stack. Shift + click: add a stack to the chosen pack list. Right click: watch the stock.");
        Translations.AddEnglish("whitehilt_qm_bag", "Your bag - click to put back");
        Translations.AddEnglish("whitehilt_qm_bag_empty", "Nothing in your bag that a chest here takes.");
        Translations.AddEnglish("whitehilt_qm_taken", "Took {0} x{1}");
        Translations.AddEnglish("whitehilt_qm_bag_full", "Your bag is full");
        Translations.AddEnglish("whitehilt_qm_kept", "The store keeps the last of it");
        Translations.AddEnglish("whitehilt_qm_returned", "Put back {0} x{1}");
        Translations.AddEnglish("whitehilt_qm_no_room", "No chest here has room for it");
        Translations.AddEnglish("whitehilt_qm_hammer", "Selected in the hammer: {0}");
        Translations.AddEnglish("whitehilt_qm_no_blueprints", "No blueprints saved yet, and nothing selected in the hammer.");
        Translations.AddEnglish("whitehilt_qm_count", "Copies");
        Translations.AddEnglish("whitehilt_qm_pack", "Pack");
        Translations.AddEnglish("whitehilt_qm_packed", "Packed everything for {0}");
        Translations.AddEnglish("whitehilt_qm_packed_short", "Packed what the store has for {0}; some is missing");
        Translations.AddEnglish("whitehilt_qm_need", "need {0}, in bag {1}, in store {2}");
        Translations.AddEnglish("whitehilt_qm_unlimited", "unlimited");
        Translations.AddEnglish("whitehilt_qm_close", "Close");
        Translations.AddEnglish("whitehilt_qm_unload", "Unload {0}");
        Translations.AddEnglish("whitehilt_qm_unloaded", "Put {0} items back in the chests");
        Translations.AddEnglish("whitehilt_qm_tab_watch", "Stock warnings");
        Translations.AddEnglish("whitehilt_qm_target", "Into: {0}");
        Translations.AddEnglish("whitehilt_qm_target_bag", "your bag");
        Translations.AddEnglish("whitehilt_qm_target_cargo", "{0} ({1} m)");
        Translations.AddEnglish("whitehilt_qm_target_busy", "Someone else has the cart or ship open");
        Translations.AddEnglish("whitehilt_qm_waiting", "Waiting for the chests...");
        Translations.AddEnglish("whitehilt_qm_list", "List: {0}");
        Translations.AddEnglish("whitehilt_qm_list_default", "Pack list {0}");
        Translations.AddEnglish("whitehilt_qm_list_name", "Name of the list");
        Translations.AddEnglish("whitehilt_qm_list_empty", "The list is empty. Shift + click items in the store to add a stack of each.");
        Translations.AddEnglish("whitehilt_qm_new_list", "New list");
        Translations.AddEnglish("whitehilt_qm_save_list", "Save as list");
        Translations.AddEnglish("whitehilt_qm_delete", "Delete list");
        Translations.AddEnglish("whitehilt_qm_confirm", "Click again");
        Translations.AddEnglish("whitehilt_qm_no_list", "Choose a pack list on the pack page first");
        Translations.AddEnglish("whitehilt_qm_added", "Added {0} x{1} to {2}");
        Translations.AddEnglish("whitehilt_qm_watched", "watched");
        Translations.AddEnglish("whitehilt_qm_watching", "Watching {0}");
        Translations.AddEnglish("whitehilt_qm_unwatched", "No longer watching {0}");
        Translations.AddEnglish("whitehilt_qm_watch_line", "in the store {0}, low below {1}");
        Translations.AddEnglish("whitehilt_qm_watch_hint", "- and + change the limit by 10, with Shift by 1. Everyone using this table sees the same warnings.");
        Translations.AddEnglish("whitehilt_qm_watch_empty", "Nothing is watched. Right click an item in the store to watch it.");
        Translations.AddEnglish("whitehilt_qm_low", "Low: {0} {1}/{2}");
        Translations.AddEnglish("whitehilt_qm_low_more", "and {0} more running low");
    }
}
