using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using System;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Saga;

/// <summary>
/// Config and texts for the saga: what a deed is worth in renown, how close one must be to share it, and what each rank
/// of renown gives.
/// </summary>
public static class SagaSettings
{
    private const string Section = "Saga";

    private static string[] creatures = Array.Empty<string>();

    /// <summary>Whether the saga is kept.</summary>
    public static ConfigEntry<bool> Enabled { get; private set; }

    /// <summary>How near a deed a player must be to have it in the saga, in metres.</summary>
    public static ConfigEntry<float> WitnessRange { get; private set; }

    /// <summary>Renown for a slain boss.</summary>
    public static ConfigEntry<int> RenownBoss { get; private set; }

    /// <summary>Renown for a slain black beast.</summary>
    public static ConfigEntry<int> RenownBeast { get; private set; }

    /// <summary>Renown for a slain creature from the list, e.g. the Kraken.</summary>
    public static ConfigEntry<int> RenownMonster { get; private set; }

    /// <summary>Renown for a treasure dug up.</summary>
    public static ConfigEntry<int> RenownTreasure { get; private set; }

    /// <summary>Renown for a new biome.</summary>
    public static ConfigEntry<int> RenownBiome { get; private set; }

    /// <summary>Renown for a skill reaching 25, 50, 75 or 100.</summary>
    public static ConfigEntry<int> RenownSkill { get; private set; }

    /// <summary>Renown for each rank.</summary>
    public static ConfigEntry<int> RenownPerRank { get; private set; }

    /// <summary>Highest rank.</summary>
    public static ConfigEntry<int> MaxRank { get; private set; }

    /// <summary>Carry weight added per rank.</summary>
    public static ConfigEntry<float> CarryPerRank { get; private set; }

    /// <summary>Max stamina added per rank.</summary>
    public static ConfigEntry<float> StaminaPerRank { get; private set; }

    /// <summary>Creatures whose death is a deed, besides bosses and black beasts.</summary>
    public static ConfigEntry<string> Creatures { get; private set; }

    /// <summary>Opens and closes the saga.</summary>
    public static ConfigEntry<KeyboardShortcut> Key { get; private set; }

    /// <summary>
    /// Binds the config entries and adds the English texts. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        Enabled = WhiteHiltConfig.BindAdminOnly(Section, "Enabled", true, "Keep each player's saga of deeds, with renown and its rewards.");
        WitnessRange = WhiteHiltConfig.BindAdminOnly(Section, "WitnessRange", 50f, "How near a slain foe or a dug-up treasure a player must be to have it in the saga, in metres.",
            new AcceptableValueRange<float>(5f, 300f));
        RenownBoss = BindRenown("RenownBoss", 10, "a slain boss");
        RenownBeast = BindRenown("RenownBeast", 3, "a slain black beast");
        RenownMonster = BindRenown("RenownMonster", 5, "a slain creature from the Creatures list");
        RenownTreasure = BindRenown("RenownTreasure", 3, "a treasure dug up");
        RenownBiome = BindRenown("RenownBiome", 2, "the first steps into a biome");
        RenownSkill = BindRenown("RenownSkill", 1, "a skill reaching 25, 50, 75 or 100");
        RenownPerRank = WhiteHiltConfig.BindAdminOnly(Section, "RenownPerRank", 20, "Renown for each rank.", new AcceptableValueRange<int>(1, 500));
        MaxRank = WhiteHiltConfig.BindAdminOnly(Section, "MaxRank", 10, "Highest rank.", new AcceptableValueRange<int>(0, 50));
        CarryPerRank = WhiteHiltConfig.BindAdminOnly(Section, "CarryPerRank", 10f, "Carry weight added per rank.", new AcceptableValueRange<float>(0f, 100f));
        StaminaPerRank = WhiteHiltConfig.BindAdminOnly(Section, "StaminaPerRank", 3f, "Max stamina added per rank.", new AcceptableValueRange<float>(0f, 50f));
        Creatures = WhiteHiltConfig.BindAdminOnly(Section, "Creatures", "WhiteHilt_Kraken,WhiteHilt_Lindorm,WhiteHilt_GiantSpider",
            "Comma-separated prefab names of creatures whose death is a deed, besides bosses and black beasts.");
        Creatures.SettingChanged += (_, _) => ReadCreatures();
        ReadCreatures();
        Key = WhiteHiltConfig.BindLocal("Saga.Keys", "OpenSaga", new KeyboardShortcut(KeyCode.F8), "Opens or closes your saga.");

        Translations.AddEnglish("whitehilt_saga_title", "The saga of {0}");
        Translations.AddEnglish("whitehilt_saga_rank", "Rank {0} of {1}   Renown {2}{3}");
        Translations.AddEnglish("whitehilt_saga_next", "   (next rank at {0})");
        Translations.AddEnglish("whitehilt_saga_reward", "Your renown gives +{0} carry weight and +{1} stamina");
        Translations.AddEnglish("whitehilt_saga_empty", "No deeds yet. Slay bosses and black beasts, dig up treasure, see new lands and master skills.");
        Translations.AddEnglish("whitehilt_saga_day", "Day {0}");
        Translations.AddEnglish("whitehilt_saga_boss", "{0} fell");
        Translations.AddEnglish("whitehilt_saga_beast", "A black beast was slain: {0}");
        Translations.AddEnglish("whitehilt_saga_monster", "{0} was slain");
        Translations.AddEnglish("whitehilt_saga_treasure", "A treasure was dug up");
        Translations.AddEnglish("whitehilt_saga_biome", "First steps into {0}");
        Translations.AddEnglish("whitehilt_saga_skill", "{0} reached {1}");
        Translations.AddEnglish("whitehilt_saga_close", "Close");
        Translations.AddEnglish("msg_whitehilt_saga_deed", "The saga tells: {0} (+{1} renown)");
        Translations.AddEnglish("msg_whitehilt_saga_rank", "Your renown grows: rank {0}");
    }

    /// <summary>
    /// True if a creature's death is a deed by the list.
    /// </summary>
    /// <param name="prefabName">The creature's prefab name.</param>
    /// <returns>True if so.</returns>
    public static bool IsListed(string prefabName)
    {
        return creatures.Contains(prefabName);
    }

    private static ConfigEntry<int> BindRenown(string key, int value, string what)
    {
        return WhiteHiltConfig.BindAdminOnly(Section, key, value, $"Renown for {what}.", new AcceptableValueRange<int>(0, 100));
    }

    private static void ReadCreatures()
    {
        creatures = Creatures.Value.Split(',').Select(name => name.Trim()).Where(name => name.Length > 0).ToArray();
    }
}
