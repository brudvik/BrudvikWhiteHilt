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

    /// <summary>Extra percentage points per additional player aboard the same ship.</summary>
    public static ConfigEntry<float> ChancePerExtraPlayer { get; private set; }

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

    /// <summary>Whether one tentacle sweeps beneath an installed ship tent.</summary>
    public static ConfigEntry<bool> TentSweep { get; private set; }
    /// <summary>Minimum seconds between tent sweeps.</summary>
    public static ConfigEntry<float> TentSweepInterval { get; private set; }
    /// <summary>Maximum additional random seconds between tent sweeps.</summary>
    public static ConfigEntry<float> TentSweepJitter { get; private set; }
    /// <summary>Warning seconds before a tent sweep.</summary>
    public static ConfigEntry<float> TentSweepWarning { get; private set; }
    /// <summary>Seconds for a tentacle to enter and withdraw.</summary>
    public static ConfigEntry<float> TentSweepSeconds { get; private set; }
    /// <summary>Radius of the sweeping tentacle in metres.</summary>
    public static ConfigEntry<float> TentSweepRadius { get; private set; }
    /// <summary>Blunt damage of a tent sweep before encounter scaling.</summary>
    public static ConfigEntry<float> TentSweepDamage { get; private set; }
    /// <summary>Knockback force of a tent sweep.</summary>
    public static ConfigEntry<float> TentSweepPush { get; private set; }
    /// <summary>Maximum distance from the Kraken to its target ship for tent sweeps.</summary>
    public static ConfigEntry<float> TentSweepRange { get; private set; }

    /// <summary>Share, in percent, of the damage the crew takes.</summary>
    public static ConfigEntry<float> CrewDamagePercent { get; private set; }

    /// <summary>Share, in percent, of the damage the ship takes.</summary>
    public static ConfigEntry<float> ShipDamagePercent { get; private set; }

    /// <summary>Extra damage to players, in percent of solo damage, per additional crew member at spawn.</summary>
    public static ConfigEntry<float> CrewDamagePerExtraPlayer { get; private set; }

    /// <summary>Extra damage to the ship, in percent of solo ship damage, per additional crew member at spawn.</summary>
    public static ConfigEntry<float> ShipDamagePerExtraPlayer { get; private set; }

    /// <summary>Whether the Kraken holds the ship while it lives.</summary>
    public static ConfigEntry<bool> HoldShip { get; private set; }

    /// <summary>Whether the grip periodically raises the hull.</summary>
    public static ConfigEntry<bool> LiftShip { get; private set; }
    /// <summary>Maximum lift above the ship's normal waterline, in metres.</summary>
    public static ConfigEntry<float> LiftHeight { get; private set; }
    /// <summary>Seconds between hull-lift cycles.</summary>
    public static ConfigEntry<float> LiftInterval { get; private set; }
    /// <summary>Seconds of warning before a lift.</summary>
    public static ConfigEntry<float> LiftWarningSeconds { get; private set; }
    /// <summary>Seconds spent raising and lowering the hull.</summary>
    public static ConfigEntry<float> LiftSeconds { get; private set; }
    /// <summary>Maximum vertical acceleration applied by the grip.</summary>
    public static ConfigEntry<float> LiftAcceleration { get; private set; }
    /// <summary>Health fraction below which the Kraken becomes enraged.</summary>
    public static ConfigEntry<float> EnrageHealthShare { get; private set; }
    /// <summary>Damage multiplier while enraged.</summary>
    public static ConfigEntry<float> EnrageDamage { get; private set; }
    /// <summary>Animation and grip-cycle speed multiplier while enraged.</summary>
    public static ConfigEntry<float> EnrageSpeed { get; private set; }
    /// <summary>Seconds between melee attacks.</summary>
    public static ConfigEntry<float> AttackInterval { get; private set; }
    /// <summary>Volume of Kraken and tentacle sound effects.</summary>
    public static ConfigEntry<float> SoundVolume { get; private set; }
    /// <summary>Audible range of Kraken sounds.</summary>
    public static ConfigEntry<float> SoundRange { get; private set; }
    /// <summary>Pitch of Kraken sound effects.</summary>
    public static ConfigEntry<float> SoundPitch { get; private set; }
    /// <summary>Seconds between ambient Kraken calls.</summary>
    public static ConfigEntry<float> AmbientSeconds { get; private set; }
    /// <summary>Range within which a living Kraken tempers Odin and Freya.</summary>
    public static ConfigEntry<float> PotionRange { get; private set; }
    /// <summary>Share of Odin's health and fall protection retained during a Kraken fight.</summary>
    public static ConfigEntry<float> OdinBonusShare { get; private set; }
    /// <summary>Share of Odin's healing and regeneration bonus retained.</summary>
    public static ConfigEntry<float> OdinHealingShare { get; private set; }
    /// <summary>Share of Freya's stamina benefits retained during a Kraken fight.</summary>
    public static ConfigEntry<float> FreyaShare { get; private set; }

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
        ChancePerMinute = WhiteHiltConfig.BindAdminOnly(Section, "ChancePerMinute", 8f, "Base chance per minute for one player, in percent, while every condition holds. 0 disables natural attacks.",
            new AcceptableValueRange<float>(0f, 100f));
        ChancePerExtraPlayer = WhiteHiltConfig.BindAdminOnly(Section, "ChancePerExtraPlayer", 8f,
            "Extra percentage points per additional player aboard the same ship. Total chance is capped at 100%. 0 disables the crew bonus.",
            new AcceptableValueRange<float>(0f, 100f));
        CooldownMinutes = WhiteHiltConfig.BindAdminOnly(Section, "CooldownMinutes", 90f, "Real minutes before the Kraken can come again, anywhere in the world.",
            new AcceptableValueRange<float>(0f, 1440f));
        MinDepth = WhiteHiltConfig.BindAdminOnly(Section, "MinDepth", 25f, "Least depth of water under the ship, in metres.", new AcceptableValueRange<float>(5f, 200f));
        Tentacles = WhiteHiltConfig.BindAdminOnly(Section, "Tentacles", 6, "Tentacles that rise around the ship.", new AcceptableValueRange<int>(0, 8));
        BodyHealth = WhiteHiltConfig.BindAdminOnly(Section, "BodyHealth", 8000f, "Health of the Kraken.", new AcceptableValueRange<float>(100f, 100000f));
        TentacleHealth = WhiteHiltConfig.BindAdminOnly(Section, "TentacleHealth", 900f, "Health of each tentacle.", new AcceptableValueRange<float>(10f, 20000f));
        BodyDamage = WhiteHiltConfig.BindAdminOnly(Section, "BodyDamage", 140f, "Blunt damage of the Kraken's slam.", new AcceptableValueRange<float>(0f, 1000f));
        TentacleDamage = WhiteHiltConfig.BindAdminOnly(Section, "TentacleDamage", 70f, "Blunt damage of a tentacle's blow.", new AcceptableValueRange<float>(0f, 1000f));
        TentSweep = WhiteHiltConfig.BindAdminOnly(Section, "TentSweep", true, "One existing tentacle can sweep through the side opening of an installed White Hilt Ship tent.");
        TentSweepInterval = WhiteHiltConfig.BindAdminOnly(Section, "TentSweepInterval", 18f, "Minimum seconds between tent sweep attempts.", new AcceptableValueRange<float>(5f, 120f));
        TentSweepJitter = WhiteHiltConfig.BindAdminOnly(Section, "TentSweepJitter", 12f, "Maximum extra random seconds between tent sweep attempts.", new AcceptableValueRange<float>(0f, 120f));
        TentSweepWarning = WhiteHiltConfig.BindAdminOnly(Section, "TentSweepWarning", 2f, "Warning seconds before the tentacle enters; move away from its path.", new AcceptableValueRange<float>(1f, 10f));
        TentSweepSeconds = WhiteHiltConfig.BindAdminOnly(Section, "TentSweepSeconds", 3f, "Seconds for the tentacle to enter and withdraw.", new AcceptableValueRange<float>(1f, 10f));
        TentSweepRadius = WhiteHiltConfig.BindAdminOnly(Section, "TentSweepRadius", 0.65f, "Tent sweep hit radius in metres.", new AcceptableValueRange<float>(0.1f, 1f));
        TentSweepDamage = WhiteHiltConfig.BindAdminOnly(Section, "TentSweepDamage", 10f, "Blunt damage before crew and enrage scaling; the sweep mainly knocks sailors out of shelter.", new AcceptableValueRange<float>(0f, 1000f));
        TentSweepPush = WhiteHiltConfig.BindAdminOnly(Section, "TentSweepPush", 30f, "Knockback force towards the opposite side opening.", new AcceptableValueRange<float>(0f, 100f));
        TentSweepRange = WhiteHiltConfig.BindAdminOnly(Section, "TentSweepRange", 60f, "Maximum Kraken-to-target-ship distance for tent sweeps, in metres.", new AcceptableValueRange<float>(5f, 200f));
        CrewDamagePercent = WhiteHiltConfig.BindAdminOnly(Section, "CrewDamagePercent", 100f, "Share of the damage the crew takes, in percent. 0 = the crew is never hurt.",
            new AcceptableValueRange<float>(0f, 500f));
        ShipDamagePercent = WhiteHiltConfig.BindAdminOnly(Section, "ShipDamagePercent", 50f, "Share of the damage the ship takes, in percent. 0 = the ship is never hurt.",
            new AcceptableValueRange<float>(0f, 500f));
        CrewDamagePerExtraPlayer = WhiteHiltConfig.BindAdminOnly(Section, "CrewDamagePerExtraPlayer", 15f,
            "Extra percent of solo damage to players per additional player aboard when the Kraken rises. Applies to body and tentacles, on top of vanilla scaling. 0 disables the bonus.",
            new AcceptableValueRange<float>(0f, 100f));
        ShipDamagePerExtraPlayer = WhiteHiltConfig.BindAdminOnly(Section, "ShipDamagePerExtraPlayer", 3f,
            "Extra percent of solo ship damage per additional player aboard when the Kraken rises. Applies to body and tentacles. 0 disables the bonus.",
            new AcceptableValueRange<float>(0f, 100f));
        HoldShip = WhiteHiltConfig.BindAdminOnly(Section, "HoldShip", true, "The Kraken holds the ship fast while it lives.");
        LiftShip = WhiteHiltConfig.BindAdminOnly(Section, "LiftShip", true, "Periodically raise and lower the held ship using owner-controlled physics.");
        LiftHeight = WhiteHiltConfig.BindAdminOnly(Section, "LiftHeight", 1.8f, "Maximum hull lift above its normal waterline, in metres.", new AcceptableValueRange<float>(0f, 4f));
        LiftInterval = WhiteHiltConfig.BindAdminOnly(Section, "LiftInterval", 14f, "Seconds between hull-lift cycles.", new AcceptableValueRange<float>(5f, 120f));
        LiftWarningSeconds = WhiteHiltConfig.BindAdminOnly(Section, "LiftWarningSeconds", 3f, "Warning seconds before raising the hull.", new AcceptableValueRange<float>(1f, 10f));
        LiftSeconds = WhiteHiltConfig.BindAdminOnly(Section, "LiftSeconds", 4f, "Seconds spent raising and lowering the hull.", new AcceptableValueRange<float>(2f, 15f));
        LiftAcceleration = WhiteHiltConfig.BindAdminOnly(Section, "LiftAcceleration", 8f, "Maximum grip acceleration in metres per second squared.", new AcceptableValueRange<float>(1f, 20f));
        EnrageHealthShare = WhiteHiltConfig.BindAdminOnly(Section, "EnrageHealthShare", 0.5f, "Health fraction below which the Kraken becomes enraged. 0 disables enrage.", new AcceptableValueRange<float>(0f, 1f));
        EnrageDamage = WhiteHiltConfig.BindAdminOnly(Section, "EnrageDamage", 1.5f, "Damage multiplier for body and tentacles while enraged.", new AcceptableValueRange<float>(1f, 3f));
        EnrageSpeed = WhiteHiltConfig.BindAdminOnly(Section, "EnrageSpeed", 1.4f, "Body animation and hull-lift cycle speed while enraged; warning duration is preserved.", new AcceptableValueRange<float>(1f, 2f));
        AttackInterval = WhiteHiltConfig.BindAdminOnly(Section, "AttackInterval", 3f, "Minimum interval between melee attacks, in seconds. Applies after a restart.", new AcceptableValueRange<float>(1f, 15f));
        SoundVolume = WhiteHiltConfig.BindAdminOnly(Section, "SoundVolume", 1f, "Volume of Kraken and tentacle calls. Applies after a restart.", new AcceptableValueRange<float>(0f, 1f));
        SoundRange = WhiteHiltConfig.BindAdminOnly(Section, "SoundRange", 180f, "Audible range of Kraken and tentacle sounds, in metres. Applies after a restart.", new AcceptableValueRange<float>(10f, 500f));
        SoundPitch = WhiteHiltConfig.BindAdminOnly(Section, "SoundPitch", 0.75f, "Pitch of the deep Kraken calls. Applies after a restart.", new AcceptableValueRange<float>(0.5f, 1.5f));
        AmbientSeconds = WhiteHiltConfig.BindAdminOnly(Section, "AmbientSeconds", 9f, "Seconds between ambient calls from the body.", new AcceptableValueRange<float>(3f, 60f));
        PotionRange = WhiteHiltConfig.BindAdminOnly(Section, "PotionRange", 80f, "Range in metres where a living, non-retreating Kraken tempers Odin and Freya. 0 disables attenuation.", new AcceptableValueRange<float>(0f, 200f));
        OdinBonusShare = WhiteHiltConfig.BindAdminOnly(Section, "OdinBonusShare", 0.6f, "Share of Odin's max-health and fall-protection bonus retained near Kraken; 1 keeps full power.", new AcceptableValueRange<float>(0.1f, 1f));
        OdinHealingShare = WhiteHiltConfig.BindAdminOnly(Section, "OdinHealingShare", 0.25f, "Share of Odin's instant healing, passive healing and regeneration bonus retained near Kraken.", new AcceptableValueRange<float>(0.1f, 1f));
        FreyaShare = WhiteHiltConfig.BindAdminOnly(Section, "FreyaShare", 0.5f, "Share of Freya's stamina benefits retained near Kraken. Costs blend from normal to Freya's configured cost.", new AcceptableValueRange<float>(0.1f, 1f));
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
