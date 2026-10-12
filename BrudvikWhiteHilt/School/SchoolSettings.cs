using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.School;

/// <summary>
/// Config and texts for the School Bench: what can be taught there, how fast reading raises a skill and how far a
/// reader can get.
/// </summary>
public static class SchoolSettings
{
    private const string Section = "School";

    /// <summary>Whether reading at the bench raises skills.</summary>
    public static ConfigEntry<bool> TeachSkills { get; private set; }

    /// <summary>Whether reading teaches materials, stations and with them recipes and pieces.</summary>
    public static ConfigEntry<bool> TeachRecipes { get; private set; }

    /// <summary>Whether reading teaches the texts of runestones and the like.</summary>
    public static ConfigEntry<bool> TeachTexts { get; private set; }

    /// <summary>Whether reading gives the map pins others wrote in the bench.</summary>
    public static ConfigEntry<bool> TeachPins { get; private set; }

    /// <summary>Most map pins a bench keeps.</summary>
    public static ConfigEntry<int> MaxPins { get; private set; }

    /// <summary>How near, in metres, a pin of the same kind counts as the same pin.</summary>
    public static ConfigEntry<float> PinMergeDistance { get; private set; }

    /// <summary>Whether a material is taught only to a reader who has been to a biome where it is found.</summary>
    public static ConfigEntry<bool> BiomeRule { get; private set; }

    /// <summary>Share of the best written level a reader can reach by studying.</summary>
    public static ConfigEntry<float> SkillShare { get; private set; }

    /// <summary>Seconds between two study steps while seated at the bench.</summary>
    public static ConfigEntry<float> StudySeconds { get; private set; }

    /// <summary>Skill experience one study step gives.</summary>
    public static ConfigEntry<float> StudyExperience { get; private set; }

    /// <summary>Most names kept in a bench's list of those who wrote in it.</summary>
    public static ConfigEntry<int> MaxWriters { get; private set; }

    /// <summary>How near a School Bench a player must be to share and fetch blueprints, in metres.</summary>
    public static ConfigEntry<float> BlueprintRange { get; private set; }

    /// <summary>
    /// Binds the config entries and adds the English texts. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        TeachSkills = WhiteHiltConfig.BindAdminOnly(Section, "TeachSkills", true, "Sitting at a School Bench raises your skills towards what others have written in it.");
        TeachRecipes = WhiteHiltConfig.BindAdminOnly(Section, "TeachRecipes", true,
            "Reading at a School Bench teaches the materials and crafting stations others know, and so the recipes and pieces made from them.");
        TeachTexts = WhiteHiltConfig.BindAdminOnly(Section, "TeachTexts", true, "Reading at a School Bench teaches the runestone texts others have read.");
        TeachPins = WhiteHiltConfig.BindAdminOnly(Section, "TeachPins", true,
            "Reading at a School Bench gives you the map pins others wrote in it: places they marked and found.");
        MaxPins = WhiteHiltConfig.BindAdminOnly(Section, "MaxPins", 1000, "Most map pins a bench keeps.", new AcceptableValueRange<int>(50, 4000));
        PinMergeDistance = WhiteHiltConfig.BindAdminOnly(Section, "PinMergeDistance", 5f,
            "A pin of the same kind closer than this many metres counts as the same and is not added again.", new AcceptableValueRange<float>(1f, 50f));
        BiomeRule = WhiteHiltConfig.BindAdminOnly(Section, "BiomeRule", true,
            "A material is only taught to a reader who has been to a biome where it is found, or who can learn everything it is made from.");
        SkillShare = WhiteHiltConfig.BindAdminOnly(Section, "SkillShare", 0.5f, "Share of the best written level of a skill a reader can reach by studying it.",
            new AcceptableValueRange<float>(0.05f, 1f));
        StudySeconds = WhiteHiltConfig.BindAdminOnly(Section, "StudySeconds", 2f, "Seconds between two study steps while you sit at the bench.",
            new AcceptableValueRange<float>(0.5f, 30f));
        StudyExperience = WhiteHiltConfig.BindAdminOnly(Section, "StudyExperience", 2f,
            "Skill experience one study step gives, the same for every skill. A level needs about half its number to the power 1.5 (level 30: 86).",
            new AcceptableValueRange<float>(0.1f, 50f));
        MaxWriters = WhiteHiltConfig.BindAdminOnly(Section, "MaxWriters", 50, "Most names a bench keeps in its list of those who wrote in it; the oldest go first.",
            new AcceptableValueRange<int>(5, 500));
        BlueprintRange = WhiteHiltConfig.BindAdminOnly(Section, "BlueprintRange", 6f, "How near a School Bench you must be to share and fetch blueprints, in metres.",
            new AcceptableValueRange<float>(2f, 30f));
        AddTranslations();
    }

    // The English texts of the bench; Norwegian.json has the translations.
    private static void AddTranslations()
    {
        Translations.AddEnglish("whitehilt_school_open", "Open");
        Translations.AddEnglish("whitehilt_school_writers", "{0} have written here");
        Translations.AddEnglish("whitehilt_school_nobody", "Nobody has written here yet");
        Translations.AddEnglish("whitehilt_school_sit", "Sit on the bench to study");
        Translations.AddEnglish("whitehilt_school_write", "Write what I know");
        Translations.AddEnglish("whitehilt_school_read", "Read");
        Translations.AddEnglish("whitehilt_school_blueprints", "Shared blueprints");
        Translations.AddEnglish("whitehilt_school_close", "Close");
        Translations.AddEnglish("whitehilt_school_skills", "Skills");
        Translations.AddEnglish("whitehilt_school_skill_row", "{0}: {1} ({2}), you {3}, studying to {4}");
        Translations.AddEnglish("whitehilt_school_skill_done", "{0}: {1} ({2}), you {3}");
        Translations.AddEnglish("whitehilt_school_knowledge", "Materials: {0}   Stations: {1}   Texts: {2}   Map pins: {3}");
        Translations.AddEnglish("whitehilt_school_new", "New to you: {0} materials, {1} stations, {2} texts, {3} map pins");
        Translations.AddEnglish("whitehilt_school_writer_row", "{0}, day {1}");
        Translations.AddEnglish("whitehilt_school_writers_title", "Written by");
        Translations.AddEnglish("msg_whitehilt_school_written", "You wrote down what you know");
        Translations.AddEnglish("msg_whitehilt_school_learned", "You learned {0} materials, {1} stations, {2} texts and {3} map pins");
        Translations.AddEnglish("msg_whitehilt_school_nothing", "There is nothing here you can learn yet");
        Translations.AddEnglish("msg_whitehilt_school_caughtup", "You have studied all you can learn here");
    }
}
