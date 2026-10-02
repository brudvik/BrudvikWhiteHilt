using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using UnityEngine;

namespace BrudvikWhiteHilt.Backpack;

/// <summary>
/// Config for the backpack, section "Backpack". The rules are server-synced; keys and what is shown are the player's own.
/// </summary>
public static class BackpackSettings
{
    private const string Section = "Backpack";
    private const string KeySection = "Backpack.Keys";

    /// <summary>Rows added to every player's inventory, on top of the vanilla rows.</summary>
    public static ConfigEntry<int> ExtraRows { get; private set; }

    /// <summary>Whether the hotbar switches to the build bar when a build tool is taken out, and back when it is put away.</summary>
    public static ConfigEntry<bool> AutoSwitchHotbar { get; private set; }

    /// <summary>Whether the name of the active bar is shown next to the hotbar.</summary>
    public static ConfigEntry<bool> ShowHotbarLabel { get; private set; }

    /// <summary>Whether bows, crossbows and fishing rods on the hotbar show their ammo and how much is left.</summary>
    public static ConfigEntry<bool> ShowHotbarAmmo { get; private set; }

    /// <summary>The ammo count turns red at or below this; 0 for never.</summary>
    public static ConfigEntry<int> LowAmmoWarning { get; private set; }

    /// <summary>Whether staffs on the hotbar show how many casts the current eitr allows.</summary>
    public static ConfigEntry<bool> ShowHotbarCasts { get; private set; }

    /// <summary>Whether the shield in its slot is taken up and put away with a one-handed weapon.</summary>
    public static ConfigEntry<bool> ShieldFollowsWeapon { get; private set; }

    /// <summary>Whether new coins go into an empty coin slot before the grid.</summary>
    public static ConfigEntry<bool> CoinsToCoinSlots { get; private set; }

    /// <summary>Switches between the travel bar and the build bar.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyHotbar { get; private set; }

    /// <summary>Eats from the first food slot.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyFood1 { get; private set; }

    /// <summary>Eats from the second food slot.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyFood2 { get; private set; }

    /// <summary>Eats from the third food slot.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyFood3 { get; private set; }

    /// <summary>Drinks from the first potion slot.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyPotion1 { get; private set; }

    /// <summary>Drinks from the second potion slot.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyPotion2 { get; private set; }

    /// <summary>How far right of the inventory the equipment panel sits.</summary>
    public static ConfigEntry<float> PanelOffsetX { get; private set; }

    /// <summary>How far up from the top of the inventory the equipment panel sits.</summary>
    public static ConfigEntry<float> PanelOffsetY { get; private set; }

    /// <summary>
    /// Binds the config entries. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        AddTranslations();

        ExtraRows = WhiteHiltConfig.BindAdminOnly(Section, "ExtraRows", 1,
            "Rows added to every player's inventory, on top of the rows the game gives (4, or more when bought from a trader).",
            new AcceptableValueRange<int>(0, 2));

        AutoSwitchHotbar = WhiteHiltConfig.BindLocal(Section, "AutoSwitchHotbar", true,
            "Switch to the build bar when you take out a hammer, hoe or cultivator, and back to the travel bar when you put it away.");
        ShowHotbarLabel = WhiteHiltConfig.BindLocal(Section, "ShowHotbarLabel", true, "Show which bar is active next to the hotbar.");
        ShowHotbarAmmo = WhiteHiltConfig.BindLocal(Section, "ShowHotbarAmmo", true,
            "Under a bow, crossbow or fishing rod on the hotbar, show the arrows, bolts or bait it will use and how many are left.");
        LowAmmoWarning = WhiteHiltConfig.BindLocal(Section, "LowAmmoWarning", 20, "The ammo count turns red at or below this many. 0 turns the warning off.");
        ShowHotbarCasts = WhiteHiltConfig.BindLocal(Section, "ShowHotbarCasts", true,
            "Under a staff on the hotbar, show how many casts your current eitr allows.");
        ShieldFollowsWeapon = WhiteHiltConfig.BindLocal(Section, "ShieldFollowsWeapon", true,
            "Take up the shield in the shield slot when you take a one-handed weapon, and put it away with the weapon.");
        CoinsToCoinSlots = WhiteHiltConfig.BindLocal(Section, "CoinsToCoinSlots", true,
            "New coins go into an empty coin slot before the inventory grid. Off: they go to the grid like other items.");
        PanelOffsetX = WhiteHiltConfig.BindLocal(Section, "PanelOffsetX", 12f, "How far right of the inventory the equipment panel sits, in pixels.");
        PanelOffsetY = WhiteHiltConfig.BindLocal(Section, "PanelOffsetY", 0f, "How far up from the top of the inventory the equipment panel sits, in pixels (negative is down).");
        KeyHotbar = WhiteHiltConfig.BindLocal(KeySection, "SwitchHotbar", new KeyboardShortcut(KeyCode.Alpha9),
            "Switch between the travel bar and the build bar.");
        KeyFood1 = WhiteHiltConfig.BindLocal(KeySection, "EatFood1", new KeyboardShortcut(KeyCode.Alpha1, KeyCode.LeftAlt), "Eat from the first food slot.");
        KeyFood2 = WhiteHiltConfig.BindLocal(KeySection, "EatFood2", new KeyboardShortcut(KeyCode.Alpha2, KeyCode.LeftAlt), "Eat from the second food slot.");
        KeyFood3 = WhiteHiltConfig.BindLocal(KeySection, "EatFood3", new KeyboardShortcut(KeyCode.Alpha3, KeyCode.LeftAlt), "Eat from the third food slot.");
        KeyPotion1 = WhiteHiltConfig.BindLocal(KeySection, "DrinkPotion1", new KeyboardShortcut(KeyCode.Alpha4, KeyCode.LeftAlt), "Drink from the first potion slot.");
        KeyPotion2 = WhiteHiltConfig.BindLocal(KeySection, "DrinkPotion2", new KeyboardShortcut(KeyCode.Alpha5, KeyCode.LeftAlt), "Drink from the second potion slot.");
    }

    private static void AddTranslations()
    {
        Translations.AddEnglish("whitehilt_hotbar_travel", "Travel bar");
        Translations.AddEnglish("whitehilt_hotbar_build", "Build bar");
        Translations.AddEnglish("whitehilt_hotbar_short_travel", "Travel");
        Translations.AddEnglish("whitehilt_hotbar_short_build", "Build");
        Translations.AddEnglish("whitehilt_hotbar_stored", "{0}   [{1}] switches");
        Translations.AddEnglish("whitehilt_backpack_equipment", "Equipment");
        Translations.AddEnglish("whitehilt_backpack_helmet", "Helmet");
        Translations.AddEnglish("whitehilt_backpack_chest", "Chest");
        Translations.AddEnglish("whitehilt_backpack_legs", "Legs");
        Translations.AddEnglish("whitehilt_backpack_cape", "Cape");
        Translations.AddEnglish("whitehilt_backpack_trinket", "Trinket");
        Translations.AddEnglish("whitehilt_backpack_slot_hint", "Drag an item here, or right-click it in your inventory.");
        Translations.AddEnglish("whitehilt_backpack_food", "Food and potions");
        Translations.AddEnglish("whitehilt_backpack_food_slot", "Food");
        Translations.AddEnglish("whitehilt_backpack_food_empty", "That food slot is empty");
        Translations.AddEnglish("whitehilt_backpack_food_hint", "Drag food here. The key on the slot eats it.");
        Translations.AddEnglish("whitehilt_backpack_potion", "Potion");
        Translations.AddEnglish("whitehilt_backpack_food_total",
            "Total  <color=#ff8080>{0}</color> health  <color=#ffff80>{1}</color> stamina  <color=#9999ff>{2}</color> eitr");
        Translations.AddEnglish("whitehilt_backpack_potion_empty", "That potion slot is empty");
        Translations.AddEnglish("whitehilt_backpack_potion_hint", "Drag a potion or mead here. The key on the slot drinks it.");
        Translations.AddEnglish("whitehilt_backpack_accessories", "Accessories");
        Translations.AddEnglish("whitehilt_backpack_accessory", "Accessory");
        Translations.AddEnglish("whitehilt_backpack_accessory_hint",
            "Belts, Wisplight, Wishbone and the like. The last four also take the Home Stone, the Swamp Key and the Pathfinder's Amulet. All are worn at once, each kind once. Right-click a belt in your inventory to put it on.");
        Translations.AddEnglish("whitehilt_backpack_duplicate", "You already wear one of those");
        Translations.AddEnglish("whitehilt_backpack_hands", "Shield, ammo and coins");
        Translations.AddEnglish("whitehilt_backpack_shield", "Shield");
        Translations.AddEnglish("whitehilt_backpack_shield_hint",
            "Drag a shield here. It is taken up with a one-handed weapon when your other hand is free, and put away with the weapon.");
        Translations.AddEnglish("whitehilt_backpack_ammo", "Ammo");
        Translations.AddEnglish("whitehilt_backpack_ammo_hint",
            "Drag arrows or bolts here. They are used before any others when you take up a bow or crossbow.");
        Translations.AddEnglish("whitehilt_backpack_coins", "Coins");
        Translations.AddEnglish("whitehilt_backpack_coins_hint", "Coins you pick up or are paid go here first. Traders take from them as from the rest of your inventory.");
    }
}
