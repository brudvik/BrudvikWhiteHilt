using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Crafting;

/// <summary>
/// Settings for what the crafting panel and the build menu show about requirements, and for crafting several at once.
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

        ShowAvailable = WhiteHiltConfig.BindLocal(Section, "ShowAvailable", true,
            "Show on each requirement's icon how many you have, in your inventory and the chests around you, or ∞ when a chest nearby keeps it unlimited.");
        ShowInBuildMenu = WhiteHiltConfig.BindLocal(Section, "ShowInBuildMenu", true, "Show how many you have in the build menu too.");
        ShowUnlockProgress = WhiteHiltConfig.BindLocal(Section, "ShowUnlockProgress", true,
            "Show a gold bar under requirements that can become unlimited, filled by how close the best chest is to unlocking them.");
        AmountSelector = WhiteHiltConfig.BindLocal(Section, "AmountSelector", true,
            "Show arrows next to the Craft button to choose how many to craft at once. The mouse wheel over the number works too.");
        MaxCraftAmount = WhiteHiltConfig.BindAdminOnly(Section, "MaxCraftAmount", 20, "The most that can be crafted at once with the arrows.",
            new AcceptableValueRange<int>(2, 100));
    }
}
