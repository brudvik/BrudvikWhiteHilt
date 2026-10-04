using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Bestiary;

/// <summary>Server-synchronized settings for the buildable field guide.</summary>
public static class BeastBookSettings
{
    private const string Section = "Bestiary";
    private static ConfigEntry<float> health;
    private static ConfigEntry<float> readingDistance;
    private static ConfigEntry<float> refreshSeconds;

    /// <summary>Health of the book and stand.</summary>
    public static float Health => health.Value;
    /// <summary>Distance beyond which reading ends.</summary>
    public static float ReadingDistance => readingDistance.Value;
    /// <summary>Interval for refreshing recipes and configured counter values.</summary>
    public static float RefreshSeconds => refreshSeconds.Value;

    /// <summary>Registers settings and English UI text before content discovery.</summary>
    public static void Initialize()
    {
        health = WhiteHiltConfig.BindAdminOnly(Section, "BookHealth", 300f, "Health of the Black Bestiary and its stand.", new AcceptableValueRange<float>(1f, 10000f));
        readingDistance = WhiteHiltConfig.BindAdminOnly(Section, "ReadingDistance", 8f, "Metres you may move away from the book before it closes.", new AcceptableValueRange<float>(1f, 30f));
        refreshSeconds = WhiteHiltConfig.BindAdminOnly(Section, "RefreshSeconds", 1f, "Seconds between updates of the open page's recipes and settings.", new AcceptableValueRange<float>(0.2f, 10f));
        Translations.AddEnglish("whitehilt_bestiary_title", "Black Bestiary");
        Translations.AddEnglish("whitehilt_bestiary_read", "Read");
        Translations.AddEnglish("whitehilt_bestiary_close", "Close");
        Translations.AddEnglish("whitehilt_bestiary_previous", "Previous beast");
        Translations.AddEnglish("whitehilt_bestiary_next", "Next beast");
        Translations.AddEnglish("whitehilt_bestiary_empty_title", "A warning");
        Translations.AddEnglish("whitehilt_bestiary_empty", "The Meadows may seem peaceful. But beyond the forest's edge, beneath the mountains and deep below the waves, something stirs that the light does not reach. Not every shadow belongs to an ordinary beast.\n\nThe remaining pages are unwritten. Venture into unfamiliar lands, and the book will reveal what waits there. Until then: keep your fire close, and listen when the woods fall silent.");
        Translations.AddEnglish("whitehilt_bestiary_range", "Where it comes");
        Translations.AddEnglish("whitehilt_bestiary_danger", "What makes it dangerous");
        Translations.AddEnglish("whitehilt_bestiary_counter", "Material weakness");
        Translations.AddEnglish("whitehilt_bestiary_recipe", "How to make the counter");
        Translations.AddEnglish("whitehilt_bestiary_gather", "Where to gather");
        Translations.AddEnglish("whitehilt_bestiary_unlock", "Guardian defeated: {0}");
        Translations.AddEnglish("whitehilt_bestiary_yield", "Makes {0}");
        Translations.AddEnglish("whitehilt_bestiary_station", "{0}, level {1}");
        Translations.AddEnglish("whitehilt_bestiary_disabled", "This recipe is currently disabled or locked by the server's progression rules.");
        Translations.AddEnglish("whitehilt_bestiary_unavailable", "This recipe is not currently registered.");
        Translations.AddEnglish("whitehilt_counter_bonus", "Material counter to {0}: +{1}% extra damage before armour. No material bonus against other creatures.");
        Translations.AddEnglish("whitehilt_counter_treatment", "Hold a compatible weapon and apply from the inventory. Only that weapon is treated, for {1} eligible attacks (misses count). A new material treatment replaces the previous one. Target: {0}.");
        Translations.AddEnglish("whitehilt_counter_wrongweapon", "Hold the right weapon first: a pickaxe for Stonebreaker, a blunt weapon for Berserker, or a slashing weapon for Carapace.");
        Translations.AddEnglish("whitehilt_counter_pickaxe", "Required weapon: pickaxe. Mining also uses a charge.");
        Translations.AddEnglish("whitehilt_counter_blunt", "Required weapon: mace, club or sledge.");
        Translations.AddEnglish("whitehilt_counter_slash", "Required weapon: sword or axe. Woodcutting also uses a charge.");
        Translations.AddEnglish("whitehilt_counter_arrow", "Shoot with a bow. The preparation travels with the arrow; ordinary arrows and runes do not gain its material bonus.");
    }
}