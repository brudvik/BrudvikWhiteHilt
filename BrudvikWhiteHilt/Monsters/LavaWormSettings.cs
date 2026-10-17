using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Monsters;

/// <summary>
/// Config of the Lava Worm and the armour made from its scales. Admin only, synced from the server.
/// </summary>
public static class LavaWormSettings
{
    private const string Section = "Lava Worm";

    /// <summary>Whether the Lava Worm can rise out of the lava.</summary>
    public static ConfigEntry<bool> Enabled { get; private set; }

    /// <summary>Global key that must be set before the Lava Worm comes; empty for none.</summary>
    public static ConfigEntry<string> RequiredKey { get; private set; }

    /// <summary>How close to the lava, in metres, a player must be.</summary>
    public static ConfigEntry<float> LavaRange { get; private set; }

    /// <summary>Chance per minute, in percent, while every condition holds.</summary>
    public static ConfigEntry<float> ChancePerMinute { get; private set; }

    /// <summary>Minutes before the Lava Worm can come for the same player again.</summary>
    public static ConfigEntry<float> CooldownMinutes { get; private set; }

    /// <summary>Health of the Lava Worm.</summary>
    public static ConfigEntry<float> Health { get; private set; }

    /// <summary>Pierce damage of its bite.</summary>
    public static ConfigEntry<float> Damage { get; private set; }

    /// <summary>Fire damage of its bite.</summary>
    public static ConfigEntry<float> FireDamage { get; private set; }

    /// <summary>How hard its bite drags its prey towards it, and so towards the lava.</summary>
    public static ConfigEntry<float> Drag { get; private set; }

    /// <summary>Size of the Lava Worm.</summary>
    public static ConfigEntry<float> Scale { get; private set; }

    /// <summary>Visual growth per star, up to two stars.</summary>
    public static ConfigEntry<float> StarScale { get; private set; }

    /// <summary>Whether the Lava Worm uses its own sound effects.</summary>
    public static ConfigEntry<bool> Sounds { get; private set; }

    /// <summary>Seconds without prey before it sinks back into the lava.</summary>
    public static ConfigEntry<float> GiveUpSeconds { get; private set; }

    /// <summary>How far from where it rose it follows its prey.</summary>
    public static ConfigEntry<float> LeashRange { get; private set; }

    /// <summary>Chance, in percent, that it drops its trophy.</summary>
    public static ConfigEntry<float> TrophyChance { get; private set; }

    /// <summary>How much less heat each piece of Lava Worm armour lets the lava build up, as a share.</summary>
    public static ConfigEntry<float> ArmorHeatResistance { get; private set; }

    /// <summary>How the whole set of Lava Worm armour changes the fire damage taken.</summary>
    public static ConfigEntry<HitData.DamageModifier> ArmorSetFire { get; private set; }

    /// <summary>
    /// Binds the config entries. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        Enabled = WhiteHiltConfig.BindAdminOnly(Section, "Enabled", true, "The Lava Worm can rise out of the lava near players in the Ashlands.");
        RequiredKey = WhiteHiltConfig.BindAdminOnly(Section, "RequiredKey", "defeated_queen",
            "Global key needed before the Lava Worm comes (defeated_queen = the Queen is slain). Empty: from the start.");
        LavaRange = WhiteHiltConfig.BindAdminOnly(Section, "LavaRange", 14f, "How close to the lava, in metres, a player must be for the Lava Worm to come.",
            new AcceptableValueRange<float>(4f, 40f));
        ChancePerMinute = WhiteHiltConfig.BindAdminOnly(Section, "ChancePerMinute", 4f, "Chance per minute, in percent, while every condition holds.",
            new AcceptableValueRange<float>(0f, 100f));
        CooldownMinutes = WhiteHiltConfig.BindAdminOnly(Section, "CooldownMinutes", 20f, "Real minutes before the Lava Worm can come for the same player again.",
            new AcceptableValueRange<float>(0f, 1440f));
        Health = WhiteHiltConfig.BindAdminOnly(Section, "Health", 3500f, "Health of the Lava Worm. Applies after a restart.", new AcceptableValueRange<float>(50f, 30000f));
        Damage = WhiteHiltConfig.BindAdminOnly(Section, "Damage", 120f, "Pierce damage of the Lava Worm's bite. Applies after a restart.",
            new AcceptableValueRange<float>(0f, 1000f));
        FireDamage = WhiteHiltConfig.BindAdminOnly(Section, "FireDamage", 80f, "Fire damage of the Lava Worm's bite; fire also sets you burning. Applies after a restart.",
            new AcceptableValueRange<float>(0f, 1000f));
        Drag = WhiteHiltConfig.BindAdminOnly(Section, "Drag", 150f, "How hard a bite drags its prey towards the worm, and the lava it rose from. 0 = no drag. Applies after a restart.",
            new AcceptableValueRange<float>(0f, 500f));
        Scale = WhiteHiltConfig.BindAdminOnly(Section, "Scale", 2.2f, "Size of the Lava Worm (1 = about 4 m long). Applies after a restart.",
            new AcceptableValueRange<float>(0.5f, 4f));
        StarScale = WhiteHiltConfig.BindAdminOnly(Section, "StarScale", 0.12f,
            "Visual growth per star up to two stars (0.12 = +12% / +24%). Higher stars also use the difficulty size settings. Applies after a restart.",
            new AcceptableValueRange<float>(0f, 0.5f));
        Sounds = WhiteHiltConfig.BindAdminOnly(Section, "Sounds", true,
            "Use the Lava Worm's own roars, hisses and bubbling lava for idle, alert, bite, injury and death. Applies after a restart.");
        GiveUpSeconds = WhiteHiltConfig.BindAdminOnly(Section, "GiveUpSeconds", 20f, "Seconds without prey in sight before the Lava Worm sinks back into the lava.",
            new AcceptableValueRange<float>(5f, 600f));
        LeashRange = WhiteHiltConfig.BindAdminOnly(Section, "LeashRange", 18f, "How far, in metres, from where it rose the Lava Worm follows its prey before it sinks back.",
            new AcceptableValueRange<float>(5f, 100f));
        TrophyChance = WhiteHiltConfig.BindAdminOnly(Section, "TrophyChance", 15f, "Chance, in percent, that the Lava Worm drops its trophy. Applies after a restart.",
            new AcceptableValueRange<float>(0f, 100f));
        ArmorHeatResistance = WhiteHiltConfig.BindAdminOnly(Section, "ArmorHeatResistance", 0.25f,
            "How much less heat each piece of Lava Worm armour lets the lava build up (0.25 = 25%; the pieces add up, so the set gives 75%). 0.34 or more makes the whole set immune to lava. Applies when a piece is put on.",
            new AcceptableValueRange<float>(0f, 0.5f));
        ArmorSetFire = WhiteHiltConfig.BindAdminOnly(Section, "ArmorSetFire", HitData.DamageModifier.VeryResistant,
            "How the whole set of Lava Worm armour changes the fire damage you take.");
    }
}
