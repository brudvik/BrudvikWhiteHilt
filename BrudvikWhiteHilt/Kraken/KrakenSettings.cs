using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;
using System.Linq;

namespace BrudvikWhiteHilt.Kraken;

/// <summary>
/// Config of the Kraken and the octopus. Admin only, synced from the server.
/// </summary>
public static class KrakenSettings
{
    private const string Section = "Kraken";
    private const string OctopusSection = "Octopus";

    private static string parsedEnvironments;
    private static string[] environments = new string[0];

    /// <summary>Whether the Kraken can come at all.</summary>
    public static ConfigEntry<bool> Enabled { get; private set; }

    /// <summary>Global key that must be set before the Kraken comes; empty for none.</summary>
    public static ConfigEntry<string> RequiredKey { get; private set; }

    /// <summary>Weathers, by name, that count as fog.</summary>
    public static ConfigEntry<string> FogWeathers { get; private set; }

    /// <summary>Strongest wind (0 to 1) that still counts as calm.</summary>
    public static ConfigEntry<float> MaxWind { get; private set; }

    /// <summary>Whether it only comes at night.</summary>
    public static ConfigEntry<bool> NightOnly { get; private set; }

    /// <summary>Chance per minute, in percent, while the conditions hold.</summary>
    public static ConfigEntry<float> ChancePerMinute { get; private set; }

    /// <summary>Minutes before the Kraken can come again anywhere in the world.</summary>
    public static ConfigEntry<float> CooldownMinutes { get; private set; }

    /// <summary>Least depth of water under the ship.</summary>
    public static ConfigEntry<float> MinDepth { get; private set; }

    /// <summary>Tentacles that rise around the ship.</summary>
    public static ConfigEntry<int> Tentacles { get; private set; }

    /// <summary>Health of the Kraken.</summary>
    public static ConfigEntry<float> BodyHealth { get; private set; }

    /// <summary>Health of each tentacle.</summary>
    public static ConfigEntry<float> TentacleHealth { get; private set; }

    /// <summary>Blunt damage of the Kraken's slam.</summary>
    public static ConfigEntry<float> BodyDamage { get; private set; }

    /// <summary>Blunt damage of a tentacle's blow.</summary>
    public static ConfigEntry<float> TentacleDamage { get; private set; }

    /// <summary>Share, in percent, of the damage the crew takes.</summary>
    public static ConfigEntry<float> CrewDamagePercent { get; private set; }

    /// <summary>Share, in percent, of the damage the ship takes.</summary>
    public static ConfigEntry<float> ShipDamagePercent { get; private set; }

    /// <summary>Whether the Kraken holds the ship while it lives.</summary>
    public static ConfigEntry<bool> HoldShip { get; private set; }

    /// <summary>Minutes before the Kraken gives up and sinks back into the deep.</summary>
    public static ConfigEntry<float> RetreatMinutes { get; private set; }

    /// <summary>Size of the Kraken.</summary>
    public static ConfigEntry<float> Scale { get; private set; }

    /// <summary>Whether the Kraken and its tentacles use their own sound effects.</summary>
    public static ConfigEntry<bool> Sounds { get; private set; }

    /// <summary>Multiplier on the Kraken's loot, trophy excepted.</summary>
    public static ConfigEntry<float> LootMultiplier { get; private set; }

    /// <summary>Whether octopuses live in the ocean.</summary>
    public static ConfigEntry<bool> OctopusEnabled { get; private set; }

    /// <summary>Octopuses near a player at most.</summary>
    public static ConfigEntry<int> OctopusMaxSpawned { get; private set; }

    /// <summary>Chance, in percent, per spawn check.</summary>
    public static ConfigEntry<float> OctopusSpawnChance { get; private set; }

    /// <summary>
    /// Binds the config entries. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        Enabled = WhiteHiltConfig.BindAdminOnly(Section, "Enabled", true, "The Kraken can rise under ships on calm, foggy nights at sea.");
        RequiredKey = WhiteHiltConfig.BindAdminOnly(Section, "RequiredKey", "defeated_bonemass",
            "Global key needed before the Kraken comes (defeated_bonemass = Bonemass is slain). Empty: from the start.");
        FogWeathers = WhiteHiltConfig.BindAdminOnly(Section, "FogWeathers", "Misty", "Weathers, comma separated, that count as fog.");
        MaxWind = WhiteHiltConfig.BindAdminOnly(Section, "MaxWind", 0.35f, "Strongest wind that still counts as calm (0 to 1).", new AcceptableValueRange<float>(0f, 1f));
        NightOnly = WhiteHiltConfig.BindAdminOnly(Section, "NightOnly", true, "The Kraken only comes at night.");
        ChancePerMinute = WhiteHiltConfig.BindAdminOnly(Section, "ChancePerMinute", 8f, "Chance per minute, in percent, while every condition holds.",
            new AcceptableValueRange<float>(0f, 100f));
        CooldownMinutes = WhiteHiltConfig.BindAdminOnly(Section, "CooldownMinutes", 90f, "Real minutes before the Kraken can come again, anywhere in the world.",
            new AcceptableValueRange<float>(0f, 1440f));
        MinDepth = WhiteHiltConfig.BindAdminOnly(Section, "MinDepth", 25f, "Least depth of water under the ship, in metres.", new AcceptableValueRange<float>(5f, 200f));
        Tentacles = WhiteHiltConfig.BindAdminOnly(Section, "Tentacles", 4, "Tentacles that rise around the ship.", new AcceptableValueRange<int>(0, 8));
        BodyHealth = WhiteHiltConfig.BindAdminOnly(Section, "BodyHealth", 4000f, "Health of the Kraken.", new AcceptableValueRange<float>(100f, 100000f));
        TentacleHealth = WhiteHiltConfig.BindAdminOnly(Section, "TentacleHealth", 500f, "Health of each tentacle.", new AcceptableValueRange<float>(10f, 20000f));
        BodyDamage = WhiteHiltConfig.BindAdminOnly(Section, "BodyDamage", 90f, "Blunt damage of the Kraken's slam.", new AcceptableValueRange<float>(0f, 1000f));
        TentacleDamage = WhiteHiltConfig.BindAdminOnly(Section, "TentacleDamage", 45f, "Blunt damage of a tentacle's blow.", new AcceptableValueRange<float>(0f, 1000f));
        CrewDamagePercent = WhiteHiltConfig.BindAdminOnly(Section, "CrewDamagePercent", 100f, "Share of the damage the crew takes, in percent. 0 = the crew is never hurt.",
            new AcceptableValueRange<float>(0f, 500f));
        ShipDamagePercent = WhiteHiltConfig.BindAdminOnly(Section, "ShipDamagePercent", 30f, "Share of the damage the ship takes, in percent. 0 = the ship is never hurt.",
            new AcceptableValueRange<float>(0f, 500f));
        HoldShip = WhiteHiltConfig.BindAdminOnly(Section, "HoldShip", true, "The Kraken holds the ship fast while it lives.");
        RetreatMinutes = WhiteHiltConfig.BindAdminOnly(Section, "RetreatMinutes", 5f, "Minutes before the Kraken sinks back into the deep.",
            new AcceptableValueRange<float>(1f, 60f));
        Scale = WhiteHiltConfig.BindAdminOnly(Section, "Scale", 1.5f, "Size of the Kraken (1 = about 10 m long). Applies to new Krakens after a restart.",
            new AcceptableValueRange<float>(0.5f, 3f));
        Sounds = WhiteHiltConfig.BindAdminOnly(Section, "Sounds", true,
            "Use the Kraken's own deep watery sounds for idle, alert, slam, injury and death, with a separate tentacle lash. Applies after a restart.");
        LootMultiplier = WhiteHiltConfig.BindAdminOnly(Section, "LootMultiplier", 1f,
            "Multiplier on the meat, ink and chitin the Kraken drops (4-6, 3-5 and 6-10 at 1). The trophy stays one. Applies after a restart.",
            new AcceptableValueRange<float>(0f, 10f));

        OctopusEnabled = WhiteHiltConfig.BindAdminOnly(OctopusSection, "Enabled", true, "Octopuses swim in the ocean and can be caught with a fishing rod.");
        OctopusMaxSpawned = WhiteHiltConfig.BindAdminOnly(OctopusSection, "MaxSpawned", 2, "Octopuses near a player at most.", new AcceptableValueRange<int>(0, 10));
        OctopusSpawnChance = WhiteHiltConfig.BindAdminOnly(OctopusSection, "SpawnChance", 20f, "Chance per spawn check, in percent.", new AcceptableValueRange<float>(0f, 100f));
    }

    /// <summary>
    /// Whether the weather counts as fog.
    /// </summary>
    /// <param name="env">Current weather.</param>
    /// <returns>True for fog.</returns>
    public static bool IsFog(EnvSetup env)
    {
        if (env == null)
        {
            return false;
        }

        if (parsedEnvironments != FogWeathers.Value)
        {
            parsedEnvironments = FogWeathers.Value;
            environments = (parsedEnvironments ?? string.Empty).Split(',').Select(part => part.Trim().ToLowerInvariant()).Where(part => part.Length > 0).ToArray();
        }

        string name = env.m_name?.ToLowerInvariant() ?? string.Empty;
        return environments.Contains(name);
    }
}
