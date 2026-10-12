using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;
using System.Collections.Generic;

namespace BrudvikWhiteHilt.Monsters;

/// <summary>
/// Config of one wild monster, in the section <c>Monsters.&lt;Name&gt;</c>. Admin only, synced from the server. The keys
/// are the same for all five, so their names and descriptions are translated once, under the family <c>Monsters</c>.
/// </summary>
internal sealed class WildMonsterSettings
{
    private static readonly Dictionary<string, WildMonsterSettings> settings = new();

    /// <summary>Whether the monster spawns.</summary>
    public ConfigEntry<bool> Enabled { get; }

    /// <summary>Health.</summary>
    public ConfigEntry<float> Health { get; }

    /// <summary>Damage of the first attack.</summary>
    public ConfigEntry<float> Damage { get; }

    /// <summary>Damage of the second attack, or null when there is only one attack.</summary>
    public ConfigEntry<float> SecondDamage { get; }

    /// <summary>Extra damage of the first attack (poison, fire), or null when it has none.</summary>
    public ConfigEntry<float> ExtraDamage { get; }

    /// <summary>Size.</summary>
    public ConfigEntry<float> Scale { get; }

    /// <summary>Visual growth per star.</summary>
    public ConfigEntry<float> StarScale { get; }

    /// <summary>Chance, in percent, per spawn check.</summary>
    public ConfigEntry<float> SpawnChance { get; }

    /// <summary>Seconds between spawn checks.</summary>
    public ConfigEntry<float> SpawnSeconds { get; }

    /// <summary>Most near a player.</summary>
    public ConfigEntry<int> MaxSpawned { get; }

    /// <summary>Fewest in a group, or null when it does not come in groups.</summary>
    public ConfigEntry<int> GroupMin { get; }

    /// <summary>Most in a group, or null when it does not come in groups.</summary>
    public ConfigEntry<int> GroupMax { get; }

    /// <summary>Chance, in percent, that the trophy drops.</summary>
    public ConfigEntry<float> TrophyChance { get; }

    /// <summary>Whether it uses its own sounds.</summary>
    public ConfigEntry<bool> Sounds { get; }

    /// <summary>Metres at which it wakes, or null when it never sleeps.</summary>
    public ConfigEntry<float> WakeRange { get; }

    /// <summary>Global key it needs before it spawns, or null when it needs none.</summary>
    public ConfigEntry<string> RequiredKey { get; }

    // Binds every entry of a monster; the plugin has not bound anything else in the section before.
    private WildMonsterSettings(WildMonster monster)
    {
        string section = monster.Section;
        string name = monster.EnglishName;
        WhiteHiltConfig.SetSectionLabel(section, Helpers.Translations.Token(monster.NameKey));
        Enabled = WhiteHiltConfig.BindAdminOnly(section, "Enabled", true, $"The {name} spawns in the wild. Off: no new ones.");
        Health = WhiteHiltConfig.BindAdminOnly(section, "Health", monster.Health, $"Health of the {name}. Applies after a restart.",
            new AcceptableValueRange<float>(10f, 20000f));
        WildAttack first = monster.Attacks[0];
        Damage = WhiteHiltConfig.BindAdminOnly(section, "Damage", first.Damage, $"{first.Description} Applies after a restart.",
            new AcceptableValueRange<float>(0f, 1000f));
        if (monster.Attacks.Length > 1)
        {
            WildAttack second = monster.Attacks[1];
            SecondDamage = WhiteHiltConfig.BindAdminOnly(section, "SecondDamage", second.Damage, $"{second.Description} Applies after a restart.",
                new AcceptableValueRange<float>(0f, 1000f));
        }

        if (first.ExtraDamage > 0f)
        {
            ExtraDamage = WhiteHiltConfig.BindAdminOnly(section, "ExtraDamage", first.ExtraDamage,
                $"{first.ExtraType} damage added to the first attack. Applies after a restart.", new AcceptableValueRange<float>(0f, 1000f));
        }

        Scale = WhiteHiltConfig.BindAdminOnly(section, "Scale", 1f, $"Size of the {name} (1 = {monster.SizeText}). Applies after a restart.",
            new AcceptableValueRange<float>(0.3f, 3f));
        StarScale = WhiteHiltConfig.BindAdminOnly(section, "StarScale", 0.15f,
            "Visual growth per star up to two stars (0.15 = +15% / +30%). Applies after a restart.", new AcceptableValueRange<float>(0f, 0.5f));
        SpawnChance = WhiteHiltConfig.BindAdminOnly(section, "SpawnChance", monster.SpawnChance,
            $"Chance, in percent, per spawn check that a {name} comes. 0 = none.", new AcceptableValueRange<float>(0f, 100f));
        SpawnSeconds = WhiteHiltConfig.BindAdminOnly(section, "SpawnSeconds", monster.SpawnSeconds, $"Seconds between two spawn checks for the {name}.",
            new AcceptableValueRange<float>(10f, 3600f));
        MaxSpawned = WhiteHiltConfig.BindAdminOnly(section, "MaxSpawned", monster.MaxSpawned, $"Most {name}s around a player.",
            new AcceptableValueRange<int>(1, 10));
        if (monster.InGroups)
        {
            GroupMin = WhiteHiltConfig.BindAdminOnly(section, "GroupMin", monster.GroupMin, $"Fewest {name}s in a group.", new AcceptableValueRange<int>(1, 10));
            GroupMax = WhiteHiltConfig.BindAdminOnly(section, "GroupMax", monster.GroupMax, $"Most {name}s in a group.", new AcceptableValueRange<int>(1, 10));
        }

        TrophyChance = WhiteHiltConfig.BindAdminOnly(section, "TrophyChance", monster.TrophyChance,
            $"Chance, in percent, that the {name} drops its trophy. Applies after a restart.", new AcceptableValueRange<float>(0f, 100f));
        Sounds = WhiteHiltConfig.BindAdminOnly(section, "Sounds", true,
            $"Use the {name}'s own sounds for idle, alert, attack, injury and death. Applies after a restart.");
        if (monster.WakeRange > 0f)
        {
            WakeRange = WhiteHiltConfig.BindAdminOnly(section, "WakeRange", monster.WakeRange,
                $"Metres from the {name} at which a player wakes it. It stands still like a dead tree until then. Applies after a restart.",
                new AcceptableValueRange<float>(1f, 40f));
        }

        if (monster.RequiredKey != null)
        {
            RequiredKey = WhiteHiltConfig.BindAdminOnly(section, "RequiredKey", monster.RequiredKey,
                $"Global key needed before the {name} spawns ({monster.RequiredKey} = the boss is slain). Empty: from the start.");
        }
    }

    /// <summary>
    /// Binds the settings of every wild monster and of their loot. Call from the plugin's Awake, once the config is set up.
    /// </summary>
    public static void Initialize()
    {
        WildLootSettings.Initialize();
        foreach (WildMonster monster in WildMonsters.All)
        {
            settings[monster.Id] = new WildMonsterSettings(monster);
        }
    }

    /// <summary>
    /// Gets the settings of a monster.
    /// </summary>
    /// <param name="monster">The monster.</param>
    /// <returns>Its settings.</returns>
    public static WildMonsterSettings Of(WildMonster monster)
    {
        return settings[monster.Id];
    }
}
