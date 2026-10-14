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
    /// What the recipe list is sorted by. The sort button above the list switches it.
    /// </summary>
    public static ConfigEntry<RecipeSort> SortBy { get; private set; }

    /// <summary>
    /// Show beside each recipe what its item gives: a food's health, stamina and eitr, a potion's effect, a piece of
    /// gear's damage, armour or block.
    /// </summary>
    public static ConfigEntry<bool> ShowStats { get; private set; }

    /// <summary>
    /// How health, stamina and eitr are weighed when food is sorted Best for me.
    /// </summary>
    public static ConfigEntry<FoodProfile> Profile { get; private set; }

    /// <summary>The weight of health in the Custom profile.</summary>
    public static ConfigEntry<float> HealthWeight { get; private set; }

    /// <summary>The weight of stamina in the Custom profile.</summary>
    public static ConfigEntry<float> StaminaWeight { get; private set; }

    /// <summary>The weight of eitr in the Custom profile.</summary>
    public static ConfigEntry<float> EitrWeight { get; private set; }

    /// <summary>
    /// The weights of the chosen profile.
    /// </summary>
    public static (float Health, float Stamina, float Eitr) Weights =>
        RecipeStats.Weights(Profile.Value, (HealthWeight.Value, StaminaWeight.Value, EitrWeight.Value));

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
        Translations.AddEnglish("whitehilt_req_have_short", "have {0}");
        Translations.AddEnglish("whitehilt_req_unlimited_short", "unlimited");
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
        Translations.AddEnglish("whitehilt_recipes_sort_tip",
            "Click to sort by something else: name, the health, stamina or eitr a food gives, all three together, the best food for your profile (in the settings), or the strength of gear.");
        Translations.AddEnglish("whitehilt_recipes_sort_name", "Name");
        Translations.AddEnglish("whitehilt_recipes_sort_health", "Health");
        Translations.AddEnglish("whitehilt_recipes_sort_stamina", "Stamina");
        Translations.AddEnglish("whitehilt_recipes_sort_eitr", "Eitr");
        Translations.AddEnglish("whitehilt_recipes_sort_total", "Total");
        Translations.AddEnglish("whitehilt_recipes_sort_bestforme", "Best for me");
        Translations.AddEnglish("whitehilt_recipes_sort_strength", "Strength");
        Translations.AddEnglish("whitehilt_recipes_stat_damage", "dmg");
        Translations.AddEnglish("whitehilt_recipes_stat_armor", "armour");
        Translations.AddEnglish("whitehilt_recipes_stat_block", "block");

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
        SortBy = WhiteHiltConfig.BindLocal(Section, "SortBy", RecipeSort.Name,
            "What the recipe list is sorted by: Name, the Health, Stamina or Eitr a food gives, Total (all three), BestForMe (weighed by FoodProfile) " +
            "or Strength (a weapon's damage, armour's armour, a shield's block). Favourites stay first. The sort button above the list switches it.");
        ShowStats = WhiteHiltConfig.BindLocal(Section, "ShowStats", true,
            "Show beside each recipe what it gives: a food's health, stamina and eitr (in the game's colours) and how long it lasts, " +
            "a potion's or mead's effect, a weapon's damage, armour's armour or a shield's block.");
        Profile = WhiteHiltConfig.BindLocal(Section, "FoodProfile", FoodProfile.Balanced,
            "How health, stamina and eitr are weighed when food is sorted Best for me: Balanced (1, 1, 1), Warrior (1.5, 1, 0.5), " +
            "Archer (1, 1.5, 0.5), Mage (1, 0.5, 1.5), or Custom (the three weights below).");
        HealthWeight = WhiteHiltConfig.BindLocal(Section, "HealthWeight", 1f, "The weight of health in the Custom food profile.",
            new AcceptableValueRange<float>(0f, 3f));
        StaminaWeight = WhiteHiltConfig.BindLocal(Section, "StaminaWeight", 1f, "The weight of stamina in the Custom food profile.",
            new AcceptableValueRange<float>(0f, 3f));
        EitrWeight = WhiteHiltConfig.BindLocal(Section, "EitrWeight", 1f, "The weight of eitr in the Custom food profile.",
            new AcceptableValueRange<float>(0f, 3f));
    }
}
