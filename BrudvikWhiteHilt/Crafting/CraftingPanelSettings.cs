using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Crafting;

/// <summary>
/// Settings for what the crafting panel and the build menu show about requirements, for crafting several at once, and
/// for finding a recipe in the list.
/// </summary>
public static class CraftingPanelSettings
{
    private const string Section = "CraftingPanel";

    /// <summary>
    /// Show on each requirement's icon how many of it the player has, or ∞ when a chest nearby keeps it unlimited.
    /// </summary>
    public static ConfigEntry<bool> ShowAvailable { get; private set; }

    /// <summary>
    /// Show the same in the build menu.
    /// </summary>
    public static ConfigEntry<bool> ShowInBuildMenu { get; private set; }

    /// <summary>
    /// Show a gold bar under requirements that can become unlimited, filled by how close the best chest is.
    /// </summary>
    public static ConfigEntry<bool> ShowUnlockProgress { get; private set; }

    /// <summary>
    /// Show the arrows next to the Craft button that choose how many to craft at once.
    /// </summary>
    public static ConfigEntry<bool> AmountSelector { get; private set; }

    /// <summary>
    /// The most that can be crafted at once with the arrows.
    /// </summary>
    public static ConfigEntry<int> MaxCraftAmount { get; private set; }

    /// <summary>
    /// Show a search field, the Craftable button and category tabs above the recipe list at every station.
    /// </summary>
    public static ConfigEntry<bool> RecipeTools { get; private set; }

    /// <summary>
    /// List only the recipes the player has the materials for. The Craftable button above the list switches it.
    /// </summary>
    public static ConfigEntry<bool> OnlyCraftable { get; private set; }

    /// <summary>
    /// Sort the recipe list by name, favourites first, instead of the game's own order.
    /// </summary>
    public static ConfigEntry<bool> SortByName { get; private set; }

    /// <summary>
    /// When sorting by name, put what can be crafted now before the rest, as the game does.
    /// </summary>
    public static ConfigEntry<bool> CraftableFirst { get; private set; }

    /// <summary>
    /// Binds the config entries and registers the English texts. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        Translations.AddEnglish("whitehilt_req_have", "You have {0}");
        Translations.AddEnglish("whitehilt_req_have_split", "You have {0}: {1} in your inventory + {2} in chests");
        Translations.AddEnglish("whitehilt_req_unlimited", "Unlimited from a chest nearby");
        Translations.AddEnglish("whitehilt_req_unlimited_elsewhere", "Unlimited in the {0}, but none is nearby");
        Translations.AddEnglish("whitehilt_req_unlimited_off", "Unlimited in the {0}, but using nearby chests is off");
        Translations.AddEnglish("whitehilt_req_unlock", "Unlimited after {0} more ({1}/{2} in the best chest)");
        Translations.AddEnglish("whitehilt_recipes_search", "Search name or material...");
        Translations.AddEnglish("whitehilt_recipes_search_tip", "Finds recipes by name or by what they are made of. Right-click a recipe to make it a favourite; favourites stay at the top.");
        Translations.AddEnglish("whitehilt_recipes_craftable", "Craftable");
        Translations.AddEnglish("whitehilt_recipes_craftable_tip", "Show only what you have the materials for, counting the chests around you.");
        Translations.AddEnglish("whitehilt_recipes_tab_all", "All");
        Translations.AddEnglish("whitehilt_recipes_tab_weapons", "Weapons");
        Translations.AddEnglish("whitehilt_recipes_tab_armor", "Armour and gear");
        Translations.AddEnglish("whitehilt_recipes_tab_tools", "Tools");
        Translations.AddEnglish("whitehilt_recipes_tab_ammo", "Ammunition");
        Translations.AddEnglish("whitehilt_recipes_tab_food", "Food and meads");
        Translations.AddEnglish("whitehilt_recipes_tab_materials", "Materials and other");

        ShowAvailable = WhiteHiltConfig.BindLocal(Section, "ShowAvailable", true,
            "Show on each requirement's icon how many you have, in your inventory and the chests around you, or ∞ when a chest nearby keeps it unlimited.");
        ShowInBuildMenu = WhiteHiltConfig.BindLocal(Section, "ShowInBuildMenu", true, "Show how many you have in the build menu too.");
        ShowUnlockProgress = WhiteHiltConfig.BindLocal(Section, "ShowUnlockProgress", true,
            "Show a gold bar under requirements that can become unlimited, filled by how close the best chest is to unlocking them.");
        AmountSelector = WhiteHiltConfig.BindLocal(Section, "AmountSelector", true,
            "Show arrows next to the Craft button to choose how many to craft at once. The mouse wheel over the number works too.");
        MaxCraftAmount = WhiteHiltConfig.BindAdminOnly(Section, "MaxCraftAmount", 20, "The most that can be crafted at once with the arrows.",
            new AcceptableValueRange<int>(2, 100));
        RecipeTools = WhiteHiltConfig.BindLocal(Section, "RecipeTools", true,
            "Show a search field, a Craftable button and category tabs above the recipe list at every station, the game's own included.");
        OnlyCraftable = WhiteHiltConfig.BindLocal(Section, "OnlyCraftable", false,
            "List only the recipes you have the materials for, counting the chests around you. The Craftable button above the list switches it.");
        SortByName = WhiteHiltConfig.BindLocal(Section, "SortByName", true,
            "Sort the recipe list by name, with your favourites (right-click a recipe) first. Off keeps the game's own order.");
        CraftableFirst = WhiteHiltConfig.BindLocal(Section, "CraftableFirst", false,
            "When sorting by name, put what you can craft now before the rest, as the game does.");
    }
}
