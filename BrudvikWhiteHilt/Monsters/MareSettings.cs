using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Monsters;

/// <summary>
/// Config of what the Mare does besides fighting: how near she shows in the mist, her Nightmare and her ride on
/// sleepers. Her health, damage and spawning are in <c>[Monsters.Mare]</c> like the other wild monsters'. Admin only,
/// synced from the server.
/// </summary>
public static class MareSettings
{
    private const string Section = "Monsters.MareNight";

    /// <summary>Metres within which the Mare can be seen.</summary>
    public static ConfigEntry<float> VisibleRange { get; private set; }

    /// <summary>Metres within which she can be seen by a player carrying a Wisp light.</summary>
    public static ConfigEntry<float> WispRange { get; private set; }

    /// <summary>Seconds the Nightmare lasts.</summary>
    public static ConfigEntry<float> NightmareSeconds { get; private set; }

    /// <summary>Chance, in percent, that she rides a player who sleeps in the Mistlands.</summary>
    public static ConfigEntry<float> SleepChance { get; private set; }

    /// <summary>Share of their health a ridden sleeper wakes with.</summary>
    public static ConfigEntry<float> SleepHealth { get; private set; }

    /// <summary>
    /// Binds the config entries. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        VisibleRange = WhiteHiltConfig.BindAdminOnly(Section, "VisibleRange", 10f, "Metres within which the Mare can be seen; farther off she fades into the mist.",
            new AcceptableValueRange<float>(2f, 100f));
        WispRange = WhiteHiltConfig.BindAdminOnly(Section, "WispRange", 25f, "Metres within which a player carrying a Wisp light (the Demister) sees the Mare.",
            new AcceptableValueRange<float>(2f, 100f));
        NightmareSeconds = WhiteHiltConfig.BindAdminOnly(Section, "NightmareSeconds", 60f,
            "Seconds the Nightmare lasts: no stamina comes back while it does.", new AcceptableValueRange<float>(5f, 600f));
        SleepChance = WhiteHiltConfig.BindAdminOnly(Section, "SleepChance", 30f,
            "Chance, in percent, that the Mare rides a player who sleeps in the Mistlands: they wake hurt and in the Nightmare. 0 = never.",
            new AcceptableValueRange<float>(0f, 100f));
        SleepHealth = WhiteHiltConfig.BindAdminOnly(Section, "SleepHealth", 0.5f, "Share of their health a ridden sleeper wakes with (0.5 = half).",
            new AcceptableValueRange<float>(0.05f, 1f));
    }
}
