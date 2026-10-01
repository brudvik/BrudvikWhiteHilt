using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using UnityEngine;

namespace BrudvikWhiteHilt.Production;

/// <summary>
/// Config and texts for the production timers on smelters, kilns, fermenters, cooking stations, beehives, sap collectors,
/// fires, eggs and tame animals. Everything is the player's own choice; nothing is synced from the server.
/// </summary>
public static class ProductionSettings
{
    private const string Section = "Production";

    /// <summary>Whether timers are added to the hover text.</summary>
    public static ConfigEntry<bool> ShowHover { get; private set; }

    /// <summary>Whether small labels float over the stations near the player.</summary>
    public static ConfigEntry<bool> ShowLabels { get; private set; }

    /// <summary>How far away labels are shown, in metres.</summary>
    public static ConfigEntry<float> LabelRange { get; private set; }

    /// <summary>How far away the overview lists stations, in metres.</summary>
    public static ConfigEntry<float> OverviewRange { get; private set; }

    /// <summary>Opens and closes the overview.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyOverview { get; private set; }

    /// <summary>Whether a message tells when a station near the player is done, stops or is about to burn food.</summary>
    public static ConfigEntry<bool> Notify { get; private set; }

    /// <summary>How far away stations send messages, in metres.</summary>
    public static ConfigEntry<float> NotifyRange { get; private set; }

    /// <summary>Smelters, blast furnaces, kilns, spinning wheels, windmills and the eitr refinery.</summary>
    public static ConfigEntry<bool> Smelters { get; private set; }

    /// <summary>Fermenters.</summary>
    public static ConfigEntry<bool> Fermenters { get; private set; }

    /// <summary>Cooking stations and ovens.</summary>
    public static ConfigEntry<bool> Cooking { get; private set; }

    /// <summary>Beehives.</summary>
    public static ConfigEntry<bool> Beehives { get; private set; }

    /// <summary>Sap collectors.</summary>
    public static ConfigEntry<bool> SapCollectors { get; private set; }

    /// <summary>Fires, braziers and torches the players built.</summary>
    public static ConfigEntry<bool> Fires { get; private set; }

    /// <summary>Eggs that hatch.</summary>
    public static ConfigEntry<bool> Eggs { get; private set; }

    /// <summary>Tame animals that breed.</summary>
    public static ConfigEntry<bool> Animals { get; private set; }

    /// <summary>
    /// True if timers are on for a kind of station.
    /// </summary>
    /// <param name="kind">The kind.</param>
    /// <returns>True if shown.</returns>
    public static bool IsOn(ProductionKind kind)
    {
        ConfigEntry<bool> entry = kind switch
        {
            ProductionKind.Smelter => Smelters,
            ProductionKind.Fermenter => Fermenters,
            ProductionKind.Cooking => Cooking,
            ProductionKind.Beehive => Beehives,
            ProductionKind.SapCollector => SapCollectors,
            ProductionKind.Fire => Fires,
            ProductionKind.Egg => Eggs,
            ProductionKind.Animal => Animals,
            _ => null
        };
        return entry != null && entry.Value;
    }

    /// <summary>
    /// Binds the config entries and adds the English texts. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        ShowHover = WhiteHiltConfig.BindLocal(Section, "ShowHover", true, "Add time left, fuel and why a station has stopped to its hover text.");
        ShowLabels = WhiteHiltConfig.BindLocal(Section, "ShowLabels", true,
            "Show small labels over working stations near you. Fires only get one when they run low or go out.");
        LabelRange = WhiteHiltConfig.BindLocal(Section, "LabelRange", 15f, "How far away labels are shown, in metres.");
        OverviewRange = WhiteHiltConfig.BindLocal(Section, "OverviewRange", 60f, "How far away the overview lists stations, in metres.");
        KeyOverview = WhiteHiltConfig.BindLocal(Section, "KeyOverview", new KeyboardShortcut(KeyCode.K),
            "Open and close the overview of the stations near you. Not in build mode.");
        Notify = WhiteHiltConfig.BindLocal(Section, "Notify", true,
            "Show a message when a station near you is done, stops, or is about to burn the food.");
        NotifyRange = WhiteHiltConfig.BindLocal(Section, "NotifyRange", 40f, "How far away stations send messages, in metres.");

        Smelters = BindKind("Smelters", "Smelters, blast furnaces, kilns, spinning wheels, windmills and the eitr refinery.");
        Fermenters = BindKind("Fermenters", "Fermenters.");
        Cooking = BindKind("Cooking", "Cooking stations and ovens.");
        Beehives = BindKind("Beehives", "Beehives.");
        SapCollectors = BindKind("SapCollectors", "Sap collectors.");
        Fires = BindKind("Fires", "Fires, braziers and torches built by players.");
        Eggs = BindKind("Eggs", "Eggs that hatch.");
        Animals = BindKind("Animals", "Tame animals that breed.");

        AddTranslations();
    }

    private static ConfigEntry<bool> BindKind(string key, string what)
    {
        return WhiteHiltConfig.BindLocal(Section + ".Stations", key, true, "Timers for: " + what);
    }

    private static void AddTranslations()
    {
        Translations.AddEnglish("whitehilt_prod_overview", "Production");
        Translations.AddEnglish("whitehilt_prod_overview_empty", "Nothing is working near you.");
        Translations.AddEnglish("whitehilt_prod_next", "Next in {0}");
        Translations.AddEnglish("whitehilt_prod_all", "All {1} done in {0}");
        Translations.AddEnglish("whitehilt_prod_ready_in", "Ready in {0}");
        Translations.AddEnglish("whitehilt_prod_ready", "ready");
        Translations.AddEnglish("whitehilt_prod_done", "done");
        Translations.AddEnglish("whitehilt_prod_full", "full");
        Translations.AddEnglish("whitehilt_prod_full_in", "Full in {0}");
        Translations.AddEnglish("whitehilt_prod_stopped", "Stopped: {0}");
        Translations.AddEnglish("whitehilt_prod_nofuel", "no {0}");
        Translations.AddEnglish("whitehilt_prod_smoke", "the smoke has nowhere to go");
        Translations.AddEnglish("whitehilt_prod_nowind", "no wind");
        Translations.AddEnglish("whitehilt_prod_wind", "Times follow the wind");
        Translations.AddEnglish("whitehilt_prod_fuel_lasts", "{1} lasts {0}");
        Translations.AddEnglish("whitehilt_prod_fuel_enough", "Enough {0} for the queue");
        Translations.AddEnglish("whitehilt_prod_fuel_short", "{1} runs out in {0}: {2} more needed");
        Translations.AddEnglish("whitehilt_prod_ferment_reset", "The timer starts over until it is covered");
        Translations.AddEnglish("whitehilt_prod_cooking", "{1} cooking, next in {0}");
        Translations.AddEnglish("whitehilt_prod_cooked", "{0} ready to take");
        Translations.AddEnglish("whitehilt_prod_burns", "Burns in {0}");
        Translations.AddEnglish("whitehilt_prod_watched", "A watchful cook keeps it from burning");
        Translations.AddEnglish("whitehilt_prod_burnt", "{0} burnt");
        Translations.AddEnglish("whitehilt_prod_needfire", "no fire");
        Translations.AddEnglish("whitehilt_prod_rootempty", "the root is drained");
        Translations.AddEnglish("whitehilt_prod_out", "the fire is out");
        Translations.AddEnglish("whitehilt_prod_wet", "the fire is wet");
        Translations.AddEnglish("whitehilt_prod_egg_hatches", "Hatches in {0}");
        Translations.AddEnglish("whitehilt_prod_egg_stacked", "eggs in a stack do not hatch");
        Translations.AddEnglish("whitehilt_prod_egg_heat", "needs warmth from a fire");
        Translations.AddEnglish("whitehilt_prod_egg_roof", "needs a roof");
        Translations.AddEnglish("whitehilt_prod_birth", "Gives birth in {0}");
        Translations.AddEnglish("whitehilt_prod_love", "Love {0}/{1}");
        Translations.AddEnglish("whitehilt_prod_hungry", "hungry");
        Translations.AddEnglish("whitehilt_prod_crowded", "too many animals near");
        Translations.AddEnglish("whitehilt_prod_nopartner", "no partner near");
        Translations.AddEnglish("whitehilt_prod_msg_done", "{0} is done");
        Translations.AddEnglish("whitehilt_prod_msg_ready", "{0} is ready");
        Translations.AddEnglish("whitehilt_prod_msg_full", "{0} is full");
        Translations.AddEnglish("whitehilt_prod_msg_stopped", "{0} stopped: {1}");
        Translations.AddEnglish("whitehilt_prod_msg_burning", "{0}: the food is about to burn!");
        Translations.AddEnglish("whitehilt_prod_msg_birth", "{0} gave birth");
    }
}
