using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;
using System;
using System.Linq;

namespace BrudvikWhiteHilt.Monsters;

/// <summary>
/// Config of the Lindorm, the giant spider and the Desert Dragon. Admin only, synced from the server.
/// </summary>
public static class MonsterSettings
{
    private const string LindormSection = "Lindorm";
    private const string SpiderSection = "Giant Spider";
    private const string DragonSection = "Desert Dragon";

    private static string parsedBiomes;
    private static Heightmap.Biome biomes;

    /// <summary>Whether the Lindorm can break out of the ground.</summary>
    public static ConfigEntry<bool> LindormEnabled { get; private set; }

    /// <summary>Global key that must be set before the Lindorm comes; empty for none.</summary>
    public static ConfigEntry<string> LindormRequiredKey { get; private set; }

    /// <summary>Biomes, by name, where the Lindorm lurks.</summary>
    public static ConfigEntry<string> LindormBiomes { get; private set; }

    /// <summary>Whether it only comes at night.</summary>
    public static ConfigEntry<bool> LindormNightOnly { get; private set; }

    /// <summary>Chance per minute, in percent, while the conditions hold.</summary>
    public static ConfigEntry<float> LindormChancePerMinute { get; private set; }

    /// <summary>Minutes before the Lindorm can come for the same player again.</summary>
    public static ConfigEntry<float> LindormCooldownMinutes { get; private set; }

    /// <summary>Health of the Lindorm.</summary>
    public static ConfigEntry<float> LindormHealth { get; private set; }

    /// <summary>Pierce damage of its bite.</summary>
    public static ConfigEntry<float> LindormDamage { get; private set; }

    /// <summary>Size of the Lindorm.</summary>
    public static ConfigEntry<float> LindormScale { get; private set; }

    /// <summary>Seconds without prey before the Lindorm burrows away.</summary>
    public static ConfigEntry<float> LindormGiveUpSeconds { get; private set; }

    /// <summary>Chance, in percent, that the Lindorm drops its trophy.</summary>
    public static ConfigEntry<float> LindormTrophyChance { get; private set; }

    /// <summary>Whether giant spiders and their nests exist.</summary>
    public static ConfigEntry<bool> SpiderEnabled { get; private set; }

    /// <summary>Health of a giant spider.</summary>
    public static ConfigEntry<float> SpiderHealth { get; private set; }

    /// <summary>Pierce damage of its bite.</summary>
    public static ConfigEntry<float> SpiderDamage { get; private set; }

    /// <summary>Poison damage of its bite.</summary>
    public static ConfigEntry<float> SpiderPoison { get; private set; }

    /// <summary>Seconds a bite slows its victim.</summary>
    public static ConfigEntry<float> SpiderWebSeconds { get; private set; }

    /// <summary>Size of a giant spider.</summary>
    public static ConfigEntry<float> SpiderScale { get; private set; }

    /// <summary>Chance, in percent, that a giant spider drops its trophy.</summary>
    public static ConfigEntry<float> SpiderTrophyChance { get; private set; }

    /// <summary>Chance, from 0 to 1, of a nest in a newly generated Black Forest zone.</summary>
    public static ConfigEntry<float> NestChancePerZone { get; private set; }

    /// <summary>Health of a nest.</summary>
    public static ConfigEntry<float> NestHealth { get; private set; }

    /// <summary>Spiders near a nest at most.</summary>
    public static ConfigEntry<int> NestMaxNear { get; private set; }

    /// <summary>Seconds between two spawns of a nest.</summary>
    public static ConfigEntry<float> NestSpawnSeconds { get; private set; }

    /// <summary>Chance, in percent, that a spider from a nest gets a star.</summary>
    public static ConfigEntry<float> NestLevelUpChance { get; private set; }

    /// <summary>Chance, in percent, per spawn check that a lone spider comes out at night.</summary>
    public static ConfigEntry<float> NightSpawnChance { get; private set; }

    /// <summary>Seconds between two spawn checks for lone spiders.</summary>
    public static ConfigEntry<float> NightSpawnSeconds { get; private set; }

    /// <summary>Lone spiders near a player at most.</summary>
    public static ConfigEntry<int> NightSpawnMax { get; private set; }

    /// <summary>Whether Desert Dragons fly over the Plains.</summary>
    public static ConfigEntry<bool> DragonEnabled { get; private set; }

    /// <summary>Global key that must be set before Desert Dragons come; empty for none.</summary>
    public static ConfigEntry<string> DragonRequiredKey { get; private set; }

    /// <summary>Chance, in percent, per spawn check that a Desert Dragon comes.</summary>
    public static ConfigEntry<float> DragonSpawnChance { get; private set; }

    /// <summary>Seconds between two spawn checks for Desert Dragons.</summary>
    public static ConfigEntry<float> DragonSpawnSeconds { get; private set; }

    /// <summary>Desert Dragons near a player at most.</summary>
    public static ConfigEntry<int> DragonSpawnMax { get; private set; }

    /// <summary>Health of a Desert Dragon.</summary>
    public static ConfigEntry<float> DragonHealth { get; private set; }

    /// <summary>Fire damage of each flame in its breath.</summary>
    public static ConfigEntry<float> DragonFireDamage { get; private set; }

    /// <summary>Flames in one breath.</summary>
    public static ConfigEntry<int> DragonFlames { get; private set; }

    /// <summary>Seconds between two breaths.</summary>
    public static ConfigEntry<float> DragonBreathSeconds { get; private set; }

    /// <summary>Distance, in metres, it breathes fire from.</summary>
    public static ConfigEntry<float> DragonBreathRange { get; private set; }

    /// <summary>Width, in metres, of the fire from halfway through the breath range.</summary>
    public static ConfigEntry<float> DragonBreathWidth { get; private set; }

    /// <summary>Speed, in metres per second, it flies at when chasing.</summary>
    public static ConfigEntry<float> DragonFlySpeed { get; private set; }

    /// <summary>Lowest height, in metres above the ground, it flies at.</summary>
    public static ConfigEntry<float> DragonFlyHeightMin { get; private set; }

    /// <summary>Highest height, in metres above the ground, it flies at.</summary>
    public static ConfigEntry<float> DragonFlyHeightMax { get; private set; }

    /// <summary>Size of a Desert Dragon.</summary>
    public static ConfigEntry<float> DragonScale { get; private set; }

    /// <summary>Chance, in percent, that a Desert Dragon drops its trophy.</summary>
    public static ConfigEntry<float> DragonTrophyChance { get; private set; }

    /// <summary>Whether its fire also burns buildings.</summary>
    public static ConfigEntry<bool> DragonBurnsBuildings { get; private set; }

    /// <summary>
    /// Binds the config entries. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        LindormEnabled = WhiteHiltConfig.BindAdminOnly(LindormSection, "Enabled", true, "The Lindorm can break out of the ground near players in the forest and the swamp at night.");
        LindormRequiredKey = WhiteHiltConfig.BindAdminOnly(LindormSection, "RequiredKey", "defeated_eikthyr",
            "Global key needed before the Lindorm comes (defeated_eikthyr = Eikthyr is slain). Empty: from the start.");
        LindormBiomes = WhiteHiltConfig.BindAdminOnly(LindormSection, "Biomes", "BlackForest, Swamp", "Biomes, comma separated, where the Lindorm lurks.");
        LindormNightOnly = WhiteHiltConfig.BindAdminOnly(LindormSection, "NightOnly", true, "The Lindorm only comes at night.");
        LindormChancePerMinute = WhiteHiltConfig.BindAdminOnly(LindormSection, "ChancePerMinute", 3f, "Chance per minute, in percent, while every condition holds.",
            new AcceptableValueRange<float>(0f, 100f));
        LindormCooldownMinutes = WhiteHiltConfig.BindAdminOnly(LindormSection, "CooldownMinutes", 30f, "Real minutes before the Lindorm can come for the same player again.",
            new AcceptableValueRange<float>(0f, 1440f));
        LindormHealth = WhiteHiltConfig.BindAdminOnly(LindormSection, "Health", 700f, "Health of the Lindorm.", new AcceptableValueRange<float>(50f, 20000f));
        LindormDamage = WhiteHiltConfig.BindAdminOnly(LindormSection, "Damage", 55f, "Pierce damage of the Lindorm's bite.", new AcceptableValueRange<float>(0f, 1000f));
        LindormScale = WhiteHiltConfig.BindAdminOnly(LindormSection, "Scale", 1.3f, "Size of the Lindorm (1 = about 4 m long). Applies after a restart.",
            new AcceptableValueRange<float>(0.5f, 3f));
        LindormGiveUpSeconds = WhiteHiltConfig.BindAdminOnly(LindormSection, "GiveUpSeconds", 25f, "Seconds without prey in sight before the Lindorm burrows away.",
            new AcceptableValueRange<float>(5f, 600f));
        LindormTrophyChance = WhiteHiltConfig.BindAdminOnly(LindormSection, "TrophyChance", 15f, "Chance, in percent, that the Lindorm drops its trophy. Applies after a restart.",
            new AcceptableValueRange<float>(0f, 100f));

        SpiderEnabled = WhiteHiltConfig.BindAdminOnly(SpiderSection, "Enabled", true, "Giant spiders nest in the Black Forest and roam it at night. Off: no new nests and no lone spiders.");
        SpiderHealth = WhiteHiltConfig.BindAdminOnly(SpiderSection, "Health", 120f, "Health of a giant spider.", new AcceptableValueRange<float>(10f, 5000f));
        SpiderDamage = WhiteHiltConfig.BindAdminOnly(SpiderSection, "Damage", 18f, "Pierce damage of a giant spider's bite.", new AcceptableValueRange<float>(0f, 500f));
        SpiderPoison = WhiteHiltConfig.BindAdminOnly(SpiderSection, "Poison", 15f, "Poison damage of a giant spider's bite, dealt over time.", new AcceptableValueRange<float>(0f, 500f));
        SpiderWebSeconds = WhiteHiltConfig.BindAdminOnly(SpiderSection, "WebSeconds", 3f, "Seconds a bite slows its victim with web. 0 = no slow.",
            new AcceptableValueRange<float>(0f, 30f));
        SpiderScale = WhiteHiltConfig.BindAdminOnly(SpiderSection, "Scale", 1f, "Size of a giant spider (1 = about 1.6 m across). Applies after a restart.",
            new AcceptableValueRange<float>(0.3f, 3f));
        SpiderTrophyChance = WhiteHiltConfig.BindAdminOnly(SpiderSection, "TrophyChance", 10f, "Chance, in percent, that a giant spider drops its trophy. Applies after a restart.",
            new AcceptableValueRange<float>(0f, 100f));
        NestChancePerZone = WhiteHiltConfig.BindAdminOnly(SpiderSection, "NestChancePerZone", 0.15f,
            "Chance of a nest in each Black Forest zone (64 x 64 m): in newly generated land, and once in land generated before the spiders came ([OldLand]).",
            new AcceptableValueRange<float>(0f, 1f));
        NestHealth = WhiteHiltConfig.BindAdminOnly(SpiderSection, "NestHealth", 300f, "Health of a nest.", new AcceptableValueRange<float>(10f, 10000f));
        NestMaxNear = WhiteHiltConfig.BindAdminOnly(SpiderSection, "NestMaxNear", 3, "Spiders a nest keeps around it at most.", new AcceptableValueRange<int>(1, 10));
        NestSpawnSeconds = WhiteHiltConfig.BindAdminOnly(SpiderSection, "NestSpawnSeconds", 20f, "Seconds between two spiders from a nest. Applies after a restart.",
            new AcceptableValueRange<float>(5f, 600f));
        NestLevelUpChance = WhiteHiltConfig.BindAdminOnly(SpiderSection, "NestLevelUpChance", 10f,
            "Chance, in percent, that a spider from a nest gets a star. Applies after a restart.", new AcceptableValueRange<float>(0f, 100f));
        NightSpawnChance = WhiteHiltConfig.BindAdminOnly(SpiderSection, "NightSpawnChance", 20f,
            "Chance, in percent, per spawn check that a lone spider comes out in the Black Forest at night, away from nests. 0 = none.", new AcceptableValueRange<float>(0f, 100f));
        NightSpawnSeconds = WhiteHiltConfig.BindAdminOnly(SpiderSection, "NightSpawnSeconds", 240f, "Seconds between two spawn checks for lone spiders.",
            new AcceptableValueRange<float>(10f, 3600f));
        NightSpawnMax = WhiteHiltConfig.BindAdminOnly(SpiderSection, "NightSpawnMax", 1, "Lone spiders around a player at most.", new AcceptableValueRange<int>(1, 10));

        DragonEnabled = WhiteHiltConfig.BindAdminOnly(DragonSection, "Enabled", true, "Fire-breathing Desert Dragons fly over the Plains once Moder is slain.");
        DragonRequiredKey = WhiteHiltConfig.BindAdminOnly(DragonSection, "RequiredKey", "defeated_dragon",
            "Global key needed before Desert Dragons come (defeated_dragon = Moder is slain). Empty: from the start.");
        DragonSpawnChance = WhiteHiltConfig.BindAdminOnly(DragonSection, "SpawnChance", 10f, "Chance, in percent, per spawn check that a Desert Dragon comes. 0 = none.",
            new AcceptableValueRange<float>(0f, 100f));
        DragonSpawnSeconds = WhiteHiltConfig.BindAdminOnly(DragonSection, "SpawnSeconds", 300f, "Seconds between two spawn checks for Desert Dragons.",
            new AcceptableValueRange<float>(10f, 3600f));
        DragonSpawnMax = WhiteHiltConfig.BindAdminOnly(DragonSection, "SpawnMax", 1, "Desert Dragons around a player at most.", new AcceptableValueRange<int>(1, 10));
        DragonHealth = WhiteHiltConfig.BindAdminOnly(DragonSection, "Health", 500f, "Health of a Desert Dragon. Applies after a restart.", new AcceptableValueRange<float>(10f, 20000f));
        DragonFireDamage = WhiteHiltConfig.BindAdminOnly(DragonSection, "FireDamage", 15f, "Fire damage of each flame in its breath; fire also sets you burning. Applies after a restart.",
            new AcceptableValueRange<float>(0f, 1000f));
        DragonFlames = WhiteHiltConfig.BindAdminOnly(DragonSection, "Flames", 12, "Flames in one breath. Applies after a restart.", new AcceptableValueRange<int>(1, 30));
        DragonBreathSeconds = WhiteHiltConfig.BindAdminOnly(DragonSection, "BreathSeconds", 8f, "Seconds between two breaths at most. Applies after a restart.",
            new AcceptableValueRange<float>(2f, 60f));
        DragonBreathRange = WhiteHiltConfig.BindAdminOnly(DragonSection, "BreathRange", 25f, "Distance, in metres, a Desert Dragon breathes fire from. Applies after a restart.",
            new AcceptableValueRange<float>(5f, 40f));
        DragonBreathWidth = WhiteHiltConfig.BindAdminOnly(DragonSection, "BreathWidth", 3f,
            "Width, in metres, of the fire from halfway through the breath range, about where it meets the ground. It leaves the mouth about as wide as the mouth. Applies after a restart.",
            new AcceptableValueRange<float>(0.5f, 10f));
        DragonFlySpeed = WhiteHiltConfig.BindAdminOnly(DragonSection, "FlySpeed", 11f, "Speed, in metres per second, a Desert Dragon flies at when it chases you. Applies after a restart.",
            new AcceptableValueRange<float>(2f, 30f));
        DragonFlyHeightMin = WhiteHiltConfig.BindAdminOnly(DragonSection, "FlyHeightMin", 5f, "Lowest height, in metres above the ground, a Desert Dragon flies at. Applies after a restart.",
            new AcceptableValueRange<float>(2f, 50f));
        DragonFlyHeightMax = WhiteHiltConfig.BindAdminOnly(DragonSection, "FlyHeightMax", 12f, "Highest height, in metres above the ground, a Desert Dragon flies at. Applies after a restart.",
            new AcceptableValueRange<float>(2f, 50f));
        DragonScale = WhiteHiltConfig.BindAdminOnly(DragonSection, "Scale", 1f, "Size of a Desert Dragon (1 = about 8 m from wingtip to wingtip). Applies after a restart.",
            new AcceptableValueRange<float>(0.3f, 3f));
        DragonTrophyChance = WhiteHiltConfig.BindAdminOnly(DragonSection, "TrophyChance", 10f, "Chance, in percent, that a Desert Dragon drops its trophy. Applies after a restart.",
            new AcceptableValueRange<float>(0f, 100f));
        DragonBurnsBuildings = WhiteHiltConfig.BindAdminOnly(DragonSection, "BurnsBuildings", false,
            "Its fire also damages the buildings it hits. Off: its fire only hurts players and creatures.");
    }

    /// <summary>
    /// Whether the Lindorm lurks in a biome.
    /// </summary>
    /// <param name="biome">The biome.</param>
    /// <returns>True if it does.</returns>
    public static bool IsLindormBiome(Heightmap.Biome biome)
    {
        if (parsedBiomes != LindormBiomes.Value)
        {
            parsedBiomes = LindormBiomes.Value;
            biomes = Heightmap.Biome.None;
            foreach (string part in (parsedBiomes ?? string.Empty).Split(',').Select(part => part.Trim()).Where(part => part.Length > 0))
            {
                if (Enum.TryParse(part, true, out Heightmap.Biome parsed))
                {
                    biomes |= parsed;
                }
            }
        }

        return biome != Heightmap.Biome.None && (biomes & biome) != 0;
    }
}
