using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrudvikWhiteHilt.Difficulty;

/// <summary>
/// Config for the dynamic difficulty: the pressure score and creature stars and sizes.
/// Everything is admin-only and synced from the server.
/// </summary>
public static class DifficultySettings
{
    private const string Section = "Difficulty";
    private const string StarsSection = "Difficulty.Stars";
    private const string SizeSection = "Difficulty.Size";

    private static readonly Dictionary<Heightmap.Biome, int> biomeMaxStars = new();
    private static readonly HashSet<string> largeCreatures = new(StringComparer.OrdinalIgnoreCase);
    private static string parsedBiomeMaxStars;
    private static string parsedLargeCreatures;

    /// <summary>Whether the dynamic difficulty is on at all.</summary>
    public static ConfigEntry<bool> Enabled { get; private set; }

    /// <summary>Players online for full weight of the player factor.</summary>
    public static ConfigEntry<int> PlayersForMax { get; private set; }

    /// <summary>World days for full weight of the time factor.</summary>
    public static ConfigEntry<int> DaysForMax { get; private set; }

    /// <summary>Visited biomes for full weight of the biome factor.</summary>
    public static ConfigEntry<int> BiomesForMax { get; private set; }

    /// <summary>Weight of the player factor.</summary>
    public static ConfigEntry<float> WeightPlayers { get; private set; }

    /// <summary>Weight of the time factor.</summary>
    public static ConfigEntry<float> WeightDays { get; private set; }

    /// <summary>Weight of the biome factor.</summary>
    public static ConfigEntry<float> WeightBiomes { get; private set; }

    /// <summary>Weight of the White Hilt gear factor.</summary>
    public static ConfigEntry<float> WeightGear { get; private set; }

    /// <summary>Minutes the pressure takes to follow a change.</summary>
    public static ConfigEntry<float> SmoothingMinutes { get; private set; }

    /// <summary>Fixed pressure for testing; below 0 is automatic.</summary>
    public static ConfigEntry<float> OverridePressure { get; private set; }

    /// <summary>Whether creature stars are changed.</summary>
    public static ConfigEntry<bool> StarsEnabled { get; private set; }

    /// <summary>Multiplier of the vanilla level-up chance at full pressure.</summary>
    public static ConfigEntry<float> LevelUpChanceMax { get; private set; }

    /// <summary>Chance in percent, at full pressure, that a 2 star creature becomes 3 stars.</summary>
    public static ConfigEntry<float> Chance3Stars { get; private set; }

    /// <summary>Chance in percent, at full pressure, that a 3 star creature becomes 4 stars.</summary>
    public static ConfigEntry<float> Chance4Stars { get; private set; }

    /// <summary>Chance in percent, at full pressure, that a 4 star creature becomes 5 stars.</summary>
    public static ConfigEntry<float> Chance5Stars { get; private set; }

    /// <summary>Visited biomes needed for 3 star creatures.</summary>
    public static ConfigEntry<int> BiomesFor3Stars { get; private set; }

    /// <summary>Visited biomes needed for 4 star creatures.</summary>
    public static ConfigEntry<int> BiomesFor4Stars { get; private set; }

    /// <summary>Visited biomes needed for 5 star creatures.</summary>
    public static ConfigEntry<int> BiomesFor5Stars { get; private set; }

    /// <summary>Highest star count per biome, as "Biome:stars" pairs.</summary>
    public static ConfigEntry<string> BiomeMaxStars { get; private set; }

    /// <summary>Extra health per star above 2, as a share of base health.</summary>
    public static ConfigEntry<float> HealthPerExtraStar { get; private set; }

    /// <summary>Extra damage per star above 2, as a share of base damage.</summary>
    public static ConfigEntry<float> DamagePerExtraStar { get; private set; }

    /// <summary>Extra health for all creatures at full pressure.</summary>
    public static ConfigEntry<float> PressureHealthBonus { get; private set; }

    /// <summary>Extra damage for all creatures at full pressure.</summary>
    public static ConfigEntry<float> PressureDamageBonus { get; private set; }

    /// <summary>Highest total health multiplier of a normal creature.</summary>
    public static ConfigEntry<float> MaxHealthMultiplier { get; private set; }

    /// <summary>Highest total damage multiplier of any creature.</summary>
    public static ConfigEntry<float> MaxDamageMultiplier { get; private set; }

    /// <summary>Loot multiplier of 3 star creatures.</summary>
    public static ConfigEntry<int> Drops3Stars { get; private set; }

    /// <summary>Loot multiplier of 4 star creatures.</summary>
    public static ConfigEntry<int> Drops4Stars { get; private set; }

    /// <summary>Loot multiplier of 5 star creatures.</summary>
    public static ConfigEntry<int> Drops5Stars { get; private set; }

    /// <summary>Whether raids get extra stars and the pressure bonus.</summary>
    public static ConfigEntry<bool> IncludeRaids { get; private set; }

    /// <summary>Whether creatures with 3 or more stars grow.</summary>
    public static ConfigEntry<bool> SizeEnabled { get; private set; }

    /// <summary>Growth per star above 2.</summary>
    public static ConfigEntry<float> SizePerExtraStar { get; private set; }

    /// <summary>Highest growth factor.</summary>
    public static ConfigEntry<float> MaxSize { get; private set; }

    /// <summary>Share of the growth that large creatures get.</summary>
    public static ConfigEntry<float> LargeCreatureFactor { get; private set; }

    /// <summary>Prefab names of large creatures, comma separated.</summary>
    public static ConfigEntry<string> LargeCreatures { get; private set; }

    /// <summary>
    /// Binds the config entries. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        AcceptableValueRange<float> share = new(0f, 1f);
        AcceptableValueRange<float> weight = new(0f, 10f);
        AcceptableValueRange<float> percent = new(0f, 100f);
        AcceptableValueRange<int> biomes = new(1, 7);

        Enabled = WhiteHiltConfig.BindAdminOnly(Section, "Enabled", true,
            "Creatures get stronger and more often starred as the world progresses: players online, days played, biomes visited and White Hilt gear worn are combined into a pressure from 0 to 1.");
        PlayersForMax = WhiteHiltConfig.BindAdminOnly(Section, "PlayersForMax", 5, "Players online that give the full player factor.", new AcceptableValueRange<int>(2, 20));
        DaysForMax = WhiteHiltConfig.BindAdminOnly(Section, "DaysForMax", 120, "World days that give the full time factor.", new AcceptableValueRange<int>(1, 1000));
        BiomesForMax = WhiteHiltConfig.BindAdminOnly(Section, "BiomesForMax", 7, "Visited biomes (Meadows to Ashlands) that give the full biome factor.", new AcceptableValueRange<int>(2, 7));
        WeightPlayers = WhiteHiltConfig.BindAdminOnly(Section, "WeightPlayers", 0.15f, "Weight of the players online. The game already strengthens creatures near several players, so keep this low.", weight);
        WeightDays = WhiteHiltConfig.BindAdminOnly(Section, "WeightDays", 0.20f, "Weight of the world days.", weight);
        WeightBiomes = WhiteHiltConfig.BindAdminOnly(Section, "WeightBiomes", 0.45f, "Weight of the biomes visited, by the furthest player online.", weight);
        WeightGear = WhiteHiltConfig.BindAdminOnly(Section, "WeightGear", 0.20f, "Weight of the share of players online wearing a White Hilt weapon or at least two White Hilt armour pieces.", weight);
        SmoothingMinutes = WhiteHiltConfig.BindAdminOnly(Section, "SmoothingMinutes", 10f, "Minutes the pressure takes to follow a change, so it does not jump when someone logs in or out.", new AcceptableValueRange<float>(0f, 120f));
        OverridePressure = WhiteHiltConfig.BindAdminOnly(Section, "OverridePressure", -1f, "Fixed pressure from 0 to 1 for testing. Below 0 computes it.", new AcceptableValueRange<float>(-1f, 1f));

        StarsEnabled = WhiteHiltConfig.BindAdminOnly(StarsSection, "Enabled", true,
            "Raise the chance of starred creatures and allow up to 5 stars. Turned off while Creature Level and Loot Control is installed. Bosses, tame creatures and fish are never changed.");
        LevelUpChanceMax = WhiteHiltConfig.BindAdminOnly(StarsSection, "LevelUpChanceMax", 2f, "Multiplier of the vanilla chance of 1 and 2 stars at full pressure (vanilla is 10% per star).", new AcceptableValueRange<float>(1f, 5f));
        Chance3Stars = WhiteHiltConfig.BindAdminOnly(StarsSection, "Chance3Stars", 25f, "Chance in percent, at full pressure, that a 2 star creature gets a 3rd star.", percent);
        Chance4Stars = WhiteHiltConfig.BindAdminOnly(StarsSection, "Chance4Stars", 20f, "Chance in percent, at full pressure, that a 3 star creature gets a 4th star.", percent);
        Chance5Stars = WhiteHiltConfig.BindAdminOnly(StarsSection, "Chance5Stars", 15f, "Chance in percent, at full pressure, that a 4 star creature gets a 5th star.", percent);
        BiomesFor3Stars = WhiteHiltConfig.BindAdminOnly(StarsSection, "BiomesFor3Stars", 4, "Biomes the furthest player online must have visited before 3 star creatures appear.", biomes);
        BiomesFor4Stars = WhiteHiltConfig.BindAdminOnly(StarsSection, "BiomesFor4Stars", 5, "Biomes visited before 4 star creatures appear.", biomes);
        BiomesFor5Stars = WhiteHiltConfig.BindAdminOnly(StarsSection, "BiomesFor5Stars", 7, "Biomes visited before 5 star creatures appear.", biomes);
        BiomeMaxStars = WhiteHiltConfig.BindAdminOnly(StarsSection, "BiomeMaxStars",
            "Meadows:2, BlackForest:3, Swamp:4, Mountain:5, Plains:5, Mistlands:5, AshLands:5, DeepNorth:5, Ocean:5",
            "Highest star count of creatures spawning in each biome.");
        HealthPerExtraStar = WhiteHiltConfig.BindAdminOnly(StarsSection, "HealthPerExtraStar", 0.5f, "Extra health per star above 2, as a share of base health (vanilla stars give 1.0).", new AcceptableValueRange<float>(0f, 2f));
        DamagePerExtraStar = WhiteHiltConfig.BindAdminOnly(StarsSection, "DamagePerExtraStar", 0.25f, "Extra damage per star above 2, as a share of base damage (vanilla stars give 0.5).", new AcceptableValueRange<float>(0f, 1f));
        PressureHealthBonus = WhiteHiltConfig.BindAdminOnly(StarsSection, "PressureHealthBonus", 0.2f, "Extra health of every new creature at full pressure (0.2 = +20%).", share);
        PressureDamageBonus = WhiteHiltConfig.BindAdminOnly(StarsSection, "PressureDamageBonus", 0.1f, "Extra damage of every new creature at full pressure (0.1 = +10%).", share);
        MaxHealthMultiplier = WhiteHiltConfig.BindAdminOnly(StarsSection, "MaxHealthMultiplier", 6f, "Highest total health multiplier of a creature.", new AcceptableValueRange<float>(3f, 20f));
        MaxDamageMultiplier = WhiteHiltConfig.BindAdminOnly(StarsSection, "MaxDamageMultiplier", 3f, "Highest total damage multiplier of any creature.", new AcceptableValueRange<float>(2f, 10f));
        Drops3Stars = WhiteHiltConfig.BindAdminOnly(StarsSection, "Drops3Stars", 5, "Loot multiplier of 3 star creatures (vanilla formula would give 8).", new AcceptableValueRange<int>(1, 32));
        Drops4Stars = WhiteHiltConfig.BindAdminOnly(StarsSection, "Drops4Stars", 6, "Loot multiplier of 4 star creatures (vanilla formula would give 16).", new AcceptableValueRange<int>(1, 32));
        Drops5Stars = WhiteHiltConfig.BindAdminOnly(StarsSection, "Drops5Stars", 7, "Loot multiplier of 5 star creatures (vanilla formula would give 32).", new AcceptableValueRange<int>(1, 32));
        IncludeRaids = WhiteHiltConfig.BindAdminOnly(StarsSection, "IncludeRaids", false, "Raids also get the extra stars and the pressure bonus.");

        SizeEnabled = WhiteHiltConfig.BindAdminOnly(SizeSection, "Enabled", true, "Creatures with 3 or more stars grow. Not indoors, as in dungeons.");
        SizePerExtraStar = WhiteHiltConfig.BindAdminOnly(SizeSection, "SizePerExtraStar", 0.08f, "Growth per star above 2 (0.08 = +8%).", new AcceptableValueRange<float>(0f, 0.3f));
        MaxSize = WhiteHiltConfig.BindAdminOnly(SizeSection, "MaxSize", 1.3f, "Highest growth factor on top of the vanilla 2 star size.", new AcceptableValueRange<float>(1f, 2f));
        LargeCreatureFactor = WhiteHiltConfig.BindAdminOnly(SizeSection, "LargeCreatureFactor", 0.5f, "Share of the growth large creatures get, so they do not get stuck.", share);
        LargeCreatures = WhiteHiltConfig.BindAdminOnly(SizeSection, "LargeCreatures",
            "Troll, Lox, Abomination, SeekerBrute, StoneGolem, Serpent, BonemawSerpent, Gjall, Morgen, GoblinBrute",
            "Prefab names of large creatures, comma separated.");
    }

    /// <summary>
    /// Highest star count of creatures spawning in a biome.
    /// </summary>
    /// <param name="biome">The biome.</param>
    /// <returns>Stars, from 0 to 5.</returns>
    public static int GetBiomeMaxStars(Heightmap.Biome biome)
    {
        if (parsedBiomeMaxStars != BiomeMaxStars.Value)
        {
            parsedBiomeMaxStars = BiomeMaxStars.Value;
            biomeMaxStars.Clear();
            foreach (string pair in parsedBiomeMaxStars.Split(','))
            {
                string[] parts = pair.Split(':');
                if (parts.Length == 2 && Enum.TryParse(parts[0].Trim(), true, out Heightmap.Biome parsed) && int.TryParse(parts[1].Trim(), out int stars))
                {
                    biomeMaxStars[parsed] = Math.Max(0, Math.Min(5, stars));
                }
            }
        }

        return biomeMaxStars.TryGetValue(biome, out int max) ? max : 5;
    }

    /// <summary>
    /// Whether a creature counts as large for the growth.
    /// </summary>
    /// <param name="prefabName">The creature's prefab name.</param>
    /// <returns>True if it is large.</returns>
    public static bool IsLargeCreature(string prefabName)
    {
        if (parsedLargeCreatures != LargeCreatures.Value)
        {
            parsedLargeCreatures = LargeCreatures.Value;
            largeCreatures.Clear();
            largeCreatures.UnionWith(SplitList(parsedLargeCreatures));
        }

        return largeCreatures.Contains(prefabName);
    }

    private static IEnumerable<string> SplitList(string value)
    {
        return (value ?? string.Empty).Split(',').Select(part => part.Trim()).Where(part => part.Length > 0);
    }
}
