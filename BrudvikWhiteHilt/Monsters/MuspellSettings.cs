using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Monsters;

/// <summary>
/// Config of the Muspell Giant and the Surtstone Amulet. Admin only, synced from the server.
/// </summary>
public static class MuspellSettings
{
    private const string Section = "Muspell Giant";

    /// <summary>Whether the Muspell Giant walks the Ashlands.</summary>
    public static ConfigEntry<bool> Enabled { get; private set; }

    /// <summary>Global key that must be set before it comes; empty for none.</summary>
    public static ConfigEntry<string> RequiredKey { get; private set; }

    /// <summary>Chance per minute, in percent, that a giant comes for a player out in the Ashlands.</summary>
    public static ConfigEntry<float> ChancePerMinute { get; private set; }

    /// <summary>Real minutes before a giant can come for the same player again.</summary>
    public static ConfigEntry<float> CooldownMinutes { get; private set; }

    /// <summary>Health of the giant.</summary>
    public static ConfigEntry<float> Health { get; private set; }

    /// <summary>Share of each attack's blunt damage it adds as fire.</summary>
    public static ConfigEntry<float> FireShare { get; private set; }

    /// <summary>Multiplier of the troll's blunt damage.</summary>
    public static ConfigEntry<float> DamageMultiplier { get; private set; }

    /// <summary>Size of the giant compared with a troll.</summary>
    public static ConfigEntry<float> Scale { get; private set; }

    /// <summary>Fire damage per second of a burning pool.</summary>
    public static ConfigEntry<float> PoolDamage { get; private set; }

    /// <summary>Radius of a burning pool.</summary>
    public static ConfigEntry<float> PoolRadius { get; private set; }

    /// <summary>Seconds a burning pool lasts.</summary>
    public static ConfigEntry<float> PoolSeconds { get; private set; }

    /// <summary>Seconds between two pools left in its steps.</summary>
    public static ConfigEntry<float> StepPoolSeconds { get; private set; }

    /// <summary>Whether it uses its own sound effects.</summary>
    public static ConfigEntry<bool> Sounds { get; private set; }

    /// <summary>Chance, in percent, that it drops its trophy.</summary>
    public static ConfigEntry<float> TrophyChance { get; private set; }

    /// <summary>How much less heat the Surtstone Amulet lets the lava build up, as a share.</summary>
    public static ConfigEntry<float> AmuletHeatResistance { get; private set; }

    /// <summary>How the Surtstone Amulet changes the fire damage taken.</summary>
    public static ConfigEntry<HitData.DamageModifier> AmuletFire { get; private set; }

    /// <summary>
    /// Binds the config entries. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        Enabled = WhiteHiltConfig.BindAdminOnly(Section, "Enabled", true, "A Muspell Giant now and then walks the Ashlands.");
        RequiredKey = WhiteHiltConfig.BindAdminOnly(Section, "RequiredKey", "defeated_queen",
            "Global key needed before the Muspell Giant comes (defeated_queen = the Queen is slain). Empty: from the start.");
        ChancePerMinute = WhiteHiltConfig.BindAdminOnly(Section, "ChancePerMinute", 3f,
            "Chance per minute, in percent, that a Muspell Giant comes for a player out in the Ashlands, when none is near. 0 = none.",
            new AcceptableValueRange<float>(0f, 100f));
        CooldownMinutes = WhiteHiltConfig.BindAdminOnly(Section, "CooldownMinutes", 45f,
            "Real minutes before a Muspell Giant can come for the same player again.", new AcceptableValueRange<float>(0f, 600f));
        Health = WhiteHiltConfig.BindAdminOnly(Section, "Health", 9000f, "Health of the Muspell Giant. Applies after a restart.", new AcceptableValueRange<float>(100f, 50000f));
        DamageMultiplier = WhiteHiltConfig.BindAdminOnly(Section, "DamageMultiplier", 3f,
            "Its blows and rocks do the troll's blunt damage times this (3 = a punch of 180). Applies after a restart.", new AcceptableValueRange<float>(0.1f, 10f));
        FireShare = WhiteHiltConfig.BindAdminOnly(Section, "FireShare", 0.5f,
            "Fire damage each blow and rock adds, as a share of its blunt damage (0.5 = half as much fire); fire also sets you burning. Applies after a restart.",
            new AcceptableValueRange<float>(0f, 3f));
        Scale = WhiteHiltConfig.BindAdminOnly(Section, "Scale", 2.2f, "Size of the Muspell Giant compared with a troll (2.2 = about 9 m tall). Applies after a restart.",
            new AcceptableValueRange<float>(1f, 4f));
        PoolDamage = WhiteHiltConfig.BindAdminOnly(Section, "PoolDamage", 20f, "Fire damage per second of a burning pool it leaves in its steps and where its rocks land.",
            new AcceptableValueRange<float>(0f, 500f));
        PoolRadius = WhiteHiltConfig.BindAdminOnly(Section, "PoolRadius", 2.2f, "Radius, in metres, of a burning pool.", new AcceptableValueRange<float>(0.5f, 8f));
        PoolSeconds = WhiteHiltConfig.BindAdminOnly(Section, "PoolSeconds", 8f, "Seconds a burning pool lasts.", new AcceptableValueRange<float>(1f, 60f));
        StepPoolSeconds = WhiteHiltConfig.BindAdminOnly(Section, "StepPoolSeconds", 2f, "Seconds between two pools it leaves in its steps while it walks. 0 = none in its steps.",
            new AcceptableValueRange<float>(0f, 30f));
        Sounds = WhiteHiltConfig.BindAdminOnly(Section, "Sounds", true,
            "Use the Muspell Giant's own roars, heavy steps and roaring fire. Applies after a restart.");
        TrophyChance = WhiteHiltConfig.BindAdminOnly(Section, "TrophyChance", 25f, "Chance, in percent, that the Muspell Giant drops its trophy. Applies after a restart.",
            new AcceptableValueRange<float>(0f, 100f));
        AmuletHeatResistance = WhiteHiltConfig.BindAdminOnly(Section, "AmuletHeatResistance", 0.25f,
            "How much less heat the Surtstone Amulet lets the lava build up (0.25 = 25%); it adds up with armour that does the same. Applies when it is put on.",
            new AcceptableValueRange<float>(0f, 1f));
        AmuletFire = WhiteHiltConfig.BindAdminOnly(Section, "AmuletFire", HitData.DamageModifier.Resistant,
            "How the Surtstone Amulet changes the fire damage you take while it is worn.");
    }
}
