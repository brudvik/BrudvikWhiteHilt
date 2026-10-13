using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.SeaBattles.Fleets;

/// <summary>
/// Config and English texts of the Nail Ship fleets, section <c>SeaBattles.Fleets</c>. Admin only, synced from the
/// server. The words of the fleet's messages and of the flagship are registered here; the saga line is with the other
/// saga texts in <c>SagaSettings</c>, and the jarl's name is registered by <c>WildMonsterRegistry</c> from its
/// <c>WildMonster</c> entry.
/// </summary>
public static class FleetSettings
{
    /// <summary>The fleet sizes a new config file starts with: two ships after Moder, three after Yagluth.</summary>
    public const string DefaultFleetTiers = "defeated_dragon=2:35; defeated_goblinking=3:50";

    /// <summary>The jarl's level per boss a new config file starts with: two stars after Moder, up to five after Fader.</summary>
    public const string DefaultJarlLevels = "defeated_dragon=3; defeated_goblinking=4; defeated_queen=5; defeated_fader=6";

    private const string Section = "SeaBattles.Fleets";

    /// <summary>Nail Ships can come as fleets.</summary>
    public static ConfigEntry<bool> Enabled { get; private set; }

    /// <summary>The fleet sizes per global key, <c>key=size:chance%</c>, see <see cref="FleetRules.ParseTiers"/>.</summary>
    public static ConfigEntry<string> FleetTiers { get; private set; }

    /// <summary>Metres between the ships in the wedge.</summary>
    public static ConfigEntry<float> FormationSpacing { get; private set; }

    /// <summary>The escorts leave the wedge when the flagship is this close to its target.</summary>
    public static ConfigEntry<float> FormationBreak { get; private set; }

    /// <summary>An escort on the flagship's side keeps this many times the broadside distance.</summary>
    public static ConfigEntry<float> EscortSpread { get; private set; }

    /// <summary>Health of the flagship's hull.</summary>
    public static ConfigEntry<float> FlagshipHullHealth { get; private set; }

    /// <summary>Extra crew on the flagship besides the jarl.</summary>
    public static ConfigEntry<int> FlagshipExtraCrew { get; private set; }

    /// <summary>Least level (stars plus one) of the flagship's crew.</summary>
    public static ConfigEntry<int> FlagshipCrewLevel { get; private set; }

    /// <summary>Level (stars plus one) of the jarl per global key: <c>key=level; ...</c>, the last key that is set wins.</summary>
    public static ConfigEntry<string> JarlLevels { get; private set; }

    /// <summary>Extra draws from the loot list for the flagship's hold.</summary>
    public static ConfigEntry<int> FlagshipLootRolls { get; private set; }

    /// <summary>Whether the flagship's hold always carries a Raid Chart.</summary>
    public static ConfigEntry<bool> FlagshipRaidChart { get; private set; }

    /// <summary>The escorts flee when the flagship is a prize or sunk.</summary>
    public static ConfigEntry<bool> FleeWhenFlagshipFalls { get; private set; }

    /// <summary>The escorts flee when the jarl is slain.</summary>
    public static ConfigEntry<bool> FleeWhenJarlFalls { get; private set; }

    /// <summary>
    /// Binds the config entries and adds the English texts. Call from the plugin's Awake, after
    /// <see cref="SeaBattleSettings.Initialize"/>.
    /// </summary>
    public static void Initialize()
    {
        Enabled = WhiteHiltConfig.BindAdminOnly(Section, "Enabled", true,
            "A Nail Ship can come with company: a fleet that sails in a wedge behind a flagship led by a draugr jarl.");
        FleetTiers = WhiteHiltConfig.BindAdminOnly(Section, "FleetTiers", DefaultFleetTiers,
            "The fleet size per global key: key=size:chance%; next key=... The last key that is set wins; size is 1 to 3 ships. " +
            "No key set: always one ship.");
        FormationSpacing = WhiteHiltConfig.BindAdminOnly(Section, "FormationSpacing", 30f,
            "Metres between the ships in the wedge.", new AcceptableValueRange<float>(10f, 80f));
        FormationBreak = WhiteHiltConfig.BindAdminOnly(Section, "FormationBreak", 80f,
            "The escorts leave the wedge and fight on their own when the flagship is this close to its target, or no longer approaches. Metres.",
            new AcceptableValueRange<float>(20f, 300f));
        EscortSpread = WhiteHiltConfig.BindAdminOnly(Section, "EscortSpread", 1.6f,
            "An escort on the same side as the flagship keeps this many times the broadside distance.",
            new AcceptableValueRange<float>(1f, 3f));
        FlagshipHullHealth = WhiteHiltConfig.BindAdminOnly(Section, "FlagshipHullHealth", 2000f,
            "Health of the flagship's hull. Applies after a restart.", new AcceptableValueRange<float>(100f, 20000f));
        FlagshipExtraCrew = WhiteHiltConfig.BindAdminOnly(Section, "FlagshipExtraCrew", 2,
            "Extra crew on the flagship besides the jarl.", new AcceptableValueRange<int>(0, 10));
        FlagshipCrewLevel = WhiteHiltConfig.BindAdminOnly(Section, "FlagshipCrewLevel", 2,
            "Least level of the flagship's crew (stars plus one).", new AcceptableValueRange<int>(1, 6));
        JarlLevels = WhiteHiltConfig.BindAdminOnly(Section, "JarlLevels", DefaultJarlLevels,
            "Level of the jarl (stars plus one) per global key: key=level; next key=... The last key that is set wins, so he " +
            "grows stronger as the world goes on. No key set: the first entry's level.");
        FlagshipLootRolls = WhiteHiltConfig.BindAdminOnly(Section, "FlagshipLootRolls", 4,
            "Extra draws from the loot list for the flagship's hold.", new AcceptableValueRange<int>(0, 20));
        FlagshipRaidChart = WhiteHiltConfig.BindAdminOnly(Section, "FlagshipRaidChart", true,
            "The flagship's hold always carries a Raid Chart.");
        FleeWhenFlagshipFalls = WhiteHiltConfig.BindAdminOnly(Section, "FleeWhenFlagshipFalls", true,
            "The other ships of the fleet turn and run when the flagship is taken or sunk.");
        FleeWhenJarlFalls = WhiteHiltConfig.BindAdminOnly(Section, "FleeWhenJarlFalls", true,
            "The other ships of the fleet turn and run when the jarl is slain.");

        AddTranslations();
    }

    // English texts of the fleet. The jarl's own name comes with its WildMonster entry; the saga line is in SagaSettings.
    private static void AddTranslations()
    {
        Translations.AddEnglish("whitehilt_nailflagship", "Nail Flagship");
        Translations.AddEnglish("msg_whitehilt_fleet_sighted", "A fleet on the horizon! The Nail Ships come in force.");
        Translations.AddEnglish("msg_whitehilt_jarl_falls", "The jarl has fallen!");
        Translations.AddEnglish("msg_whitehilt_fleet_breaks", "The fleet breaks and runs!");
    }
}
