using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;

namespace BrudvikWhiteHilt.Progression;

/// <summary>
/// Config entries for White Hilt: progression, and per-item settings bound by the items themselves.
/// All entries are admin-only and synced from the server.
/// </summary>
public static class WhiteHiltConfig
{
    private const string GeneralSection = "General";
    private const string TiersSection = "Tiers";
    private const string ContentSection = "Content";
    private const string RecipesSection = "Recipes";
    private const string MigrationsKey = "AppliedMigrations";

    // Settings whose default changed; old files still hold the old value, so each list is reset once per file.
    private static readonly (string Id, (string Section, string Key)[] Resets)[] Migrations =
    {
        ("balance-0.28", new[]
        {
            ("Gear.Armor", "ArmorPerLevelBonus"),
            ("Gear.Weapons", "DamageMultiplierBonus"),
            ("Gear.Weapons", "BonusDamagePerLevel"),
            ("Potions.GiftOfOdin", "DurationMinutes"),
            ("Potions.GiftOfOdin", "HealthRegenBonus"),
            ("Potions.GiftOfIdunn", "DurationMinutes"),
            ("Potions.GiftOfIdunn", "HealthRegenMultiplier"),
            ("Potions.GiftOfIdunn", "StaminaRegenMultiplier"),
            ("Potions.GiftOfIdunn", "EitrRegenMultiplier"),
            ("Potions.GiftOfIdunn", "HealPerSecond"),
        }),
        ("armor-0.48", new[]
        {
            ("Gear.Armor", "ArmorPerLevelBonus"),
        }),
    };

    // Settings that no longer exist; they are removed from the config file so it does not keep dead entries.
    private static readonly (string Section, string Key)[] RetiredEntries =
    {
        ("Gear.Indestructible", "ArmorBonus"),
        ("Gear.Weapons", "BonusDamage"),
        ("Potions.GiftOfOdin", "MaxHealth"),
        ("Potions.GiftOfOdin", "HealPerFrame"),
        ("Potions.GiftOfMimir", "DurationMinutes"),
        ("Potions.GiftOfMimir", "InitialRevealRadius"),
        ("Potions.GiftOfMimir", "RevealRadius"),
        ("Potions.GiftOfMimir", "RevealIntervalSeconds"),
        ("Potions.GiftOfMimir", "CreatureRange"),
        ("Potions.GiftOfMimir", "CreatureRefreshSeconds"),
        (ContentSection, "GiftOfMimir"),
        (RecipesSection, "GiftOfMimir"),
        (TiersSection, "GiftOfMimir"),
        ("Fishing.Net", "MendItem"),
        ("Fishing.Net", "MendAmount"),
        ("Chests.Collection", "StationDistance"),
    };

    // Renamed sections; their values move over before anything is bound.
    private static readonly (string Old, string New)[] MovedSections =
    {
        ("Ranching", "Husbandry"),
        ("Defenses", "Defences"),
    };

    // Settings that moved to another section or key; the value moves over before anything is bound.
    private static readonly (string Section, string Key, string NewSection, string NewKey)[] MovedEntries =
    {
        ("Portals", "MapTableExtensionRange", "Navigation", "MapTableRange"),
    };

    private static readonly Dictionary<string, ConfigEntry<TierOverride>> tierOverrides = new();
    private static readonly Dictionary<string, ConfigEntry<bool>> enabledEntries = new();
    private static readonly Dictionary<string, ConfigEntry<bool>> featureGates = new();
    private static readonly Dictionary<string, ConfigEntry<string>> recipeOverrides = new();
    private static readonly Dictionary<string, string> sectionLabels = new();
    private static readonly Dictionary<(string Section, string Key), string> keyLabels = new();
    private static ConfigFile configFile;

    /// <summary>
    /// The plugin's config file.
    /// </summary>
    public static ConfigFile File => configFile;

    /// <summary>
    /// Selected progression mode.
    /// </summary>
    public static ConfigEntry<ProgressionMode> Mode { get; private set; }

    /// <summary>
    /// Whether a message is shown when a new tier unlocks in linear mode.
    /// </summary>
    public static ConfigEntry<bool> ShowUnlockMessages { get; private set; }

    /// <summary>
    /// Binds the general config entries.
    /// </summary>
    /// <param name="config">The plugin's config file.</param>
    public static void Initialize(ConfigFile config)
    {
        configFile = config;
        MoveRenamedEntries();

        Mode = config.Bind(
            GeneralSection,
            "Mode",
            // Linear is how a new player meets the mod; a config file that already holds a mode keeps it.
            ProgressionMode.Linear,
            AdminOnly("Linear (default): only the White Hilt tools are available at the start. Weapons, armor, potions and the " +
                      "ship unlock when you first obtain the key material of each biome (Bronze, Iron, Silver, Black Metal, Eitr, " +
                      "Flametal), and their recipes also cost some of that material.\n" +
                      "Full: every White Hilt recipe is available as soon as its materials are known."));

        ShowUnlockMessages = config.Bind(
            GeneralSection,
            "ShowUnlockMessages",
            true,
            AdminOnly("Show a message listing the newly available White Hilt items when a tier unlocks in linear mode."));
    }

    /// <summary>
    /// Binds the on/off switch, the recipe override and the tier override for an item or piece.
    /// </summary>
    /// <param name="entry">The item or piece.</param>
    public static void BindEntry(IWhiteHiltProgressionEntry entry)
    {
        foreach (string section in new[] { ContentSection, RecipesSection, TiersSection })
        {
            keyLabels[(section, entry.Id)] = entry.NameToken;
        }

        enabledEntries[entry.Id] = configFile.Bind(
            ContentSection,
            entry.Id,
            true,
            AdminOnly($"{entry.DisplayName}. Off: it can no longer be crafted or built. Existing copies are kept."));

        recipeOverrides[entry.Id] = configFile.Bind(
            RecipesSection,
            entry.Id,
            string.Empty,
            AdminOnly($"{entry.DisplayName}. Empty: the built-in recipe.\n" +
                      "Otherwise a comma separated list of Prefab:Amount or Prefab:Amount:AmountPerLevel, e.g. \"Iron:10:5, FineWood:4\". " +
                      "Unknown prefabs are skipped with a warning."));

        tierOverrides[entry.Id] = configFile.Bind(
            TiersSection,
            entry.Id,
            TierOverride.Default,
            AdminOnly($"{entry.DisplayName}. " + (entry is IFullModeOnly ? "Default: not available in linear mode.\n" : $"Default tier: {entry.DefaultTier}.\n") +
                      "Tiers only apply in linear mode. Never disables the recipe in both modes, existing copies are kept."));
    }

    /// <summary>
    /// Gets the configured tier override for an item or piece.
    /// </summary>
    /// <param name="id">The entry's identifier.</param>
    /// <returns>The configured override, or <see cref="TierOverride.Default"/> if none is bound.</returns>
    public static TierOverride GetTierOverride(string id)
    {
        return tierOverrides.TryGetValue(id, out var entry) ? entry.Value : TierOverride.Default;
    }

    /// <summary>
    /// Gets whether an item or piece is switched on.
    /// </summary>
    /// <param name="id">The entry's identifier.</param>
    /// <returns>False only if the config switches it off.</returns>
    public static bool IsEnabled(string id)
    {
        if (featureGates.TryGetValue(id, out var gate) && !gate.Value)
        {
            return false;
        }

        return !enabledEntries.TryGetValue(id, out var entry) || entry.Value;
    }

    /// <summary>
    /// Ties an item or piece to the switch of the feature it belongs to: while the switch is off, the entry can no
    /// longer be crafted or built, as if its own [Content] switch were off.
    /// </summary>
    /// <param name="id">The entry's identifier.</param>
    /// <param name="feature">The feature's on/off setting.</param>
    public static void AddFeatureGate(string id, ConfigEntry<bool> feature)
    {
        featureGates[id] = feature;
    }

    /// <summary>
    /// Gets the configured recipe of an item or piece.
    /// </summary>
    /// <param name="id">The entry's identifier.</param>
    /// <returns>The recipe text, or an empty string for the built-in recipe.</returns>
    public static string GetRecipeOverride(string id)
    {
        return recipeOverrides.TryGetValue(id, out var entry) ? entry.Value?.Trim() ?? string.Empty : string.Empty;
    }

    /// <summary>
    /// Binds an admin-only, server-synced config entry. <see cref="Initialize"/> must have run first.
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <param name="section">Config section.</param>
    /// <param name="key">Config key.</param>
    /// <param name="defaultValue">Default value.</param>
    /// <param name="description">Description shown in the config file.</param>
    /// <param name="acceptableValues">Optional allowed range or list.</param>
    /// <returns>The bound entry.</returns>
    public static ConfigEntry<T> BindAdminOnly<T>(string section, string key, T defaultValue, string description, AcceptableValueBase acceptableValues = null)
    {
        return configFile.Bind(section, key, defaultValue, new ConfigDescription(description, acceptableValues, new ConfigurationManagerAttributes { IsAdminOnly = true }));
    }

    /// <summary>
    /// Binds a config entry that each player sets for themselves; it is not synced from the server.
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <param name="section">Config section.</param>
    /// <param name="key">Config key.</param>
    /// <param name="defaultValue">Default value.</param>
    /// <param name="description">Description shown in the config file.</param>
    /// <param name="acceptableValues">Optional allowed range or list.</param>
    /// <returns>The bound entry.</returns>
    public static ConfigEntry<T> BindLocal<T>(string section, string key, T defaultValue, string description, AcceptableValueBase acceptableValues = null)
    {
        return configFile.Bind(section, key, defaultValue, new ConfigDescription(description, acceptableValues));
    }

    /// <summary>
    /// Whether the config file already holds a value for an entry, bound or not. Call before binding the entry to
    /// tell a first start from a later one.
    /// </summary>
    /// <param name="section">Config section.</param>
    /// <param name="key">Config key.</param>
    /// <returns>True if the file has the entry.</returns>
    public static bool IsStored(string section, string key)
    {
        ConfigDefinition definition = new(section, key);
        return configFile.ContainsKey(definition) || Orphans()?.ContainsKey(definition) == true;
    }

    /// <summary>
    /// Names a config section in the settings window, e.g. a food's section after the food.
    /// </summary>
    /// <param name="section">Config section.</param>
    /// <param name="token">Localization token of its name.</param>
    public static void SetSectionLabel(string section, string token)
    {
        sectionLabels[section] = token;
    }

    /// <summary>
    /// Gets the token set with <see cref="SetSectionLabel"/>.
    /// </summary>
    /// <param name="section">Config section.</param>
    /// <returns>The token, or null.</returns>
    public static string GetSectionLabel(string section)
    {
        return sectionLabels.TryGetValue(section, out string token) ? token : null;
    }

    /// <summary>
    /// Gets the name token of an entry whose key is an item or piece, as in [Content].
    /// </summary>
    /// <param name="section">Config section.</param>
    /// <param name="key">Config key.</param>
    /// <returns>The token, or null.</returns>
    public static string GetKeyLabel(string section, string key)
    {
        return keyLabels.TryGetValue((section, key), out string token) ? token : null;
    }

    /// <summary>
    /// Removes retired settings from the config file and, once per file, resets settings whose default changed.
    /// Must run after every setting is bound.
    /// </summary>
    public static void ApplyMigrations()
    {
        var orphans = Orphans();
        if (orphans == null)
        {
            Jotunn.Logger.LogWarning("Config migrations skipped: orphaned entries not found.");
            return;
        }

        bool changed = false;
        foreach ((string section, string key) in RetiredEntries)
        {
            changed |= orphans.Remove(new ConfigDefinition(section, key));
        }

        ConfigDefinition marker = new(GeneralSection, MigrationsKey);
        orphans.TryGetValue(marker, out string applied);
        List<string> done = (applied ?? string.Empty).Split(',').Select(id => id.Trim()).Where(id => id.Length > 0).ToList();
        foreach ((string id, (string Section, string Key)[] resets) in Migrations)
        {
            if (done.Contains(id))
            {
                continue;
            }

            foreach ((string section, string key) in resets)
            {
                ConfigDefinition definition = new(section, key);
                if (configFile.ContainsKey(definition))
                {
                    ConfigEntryBase entry = configFile[definition];
                    entry.BoxedValue = entry.DefaultValue;
                }
            }

            done.Add(id);
            orphans[marker] = string.Join(",", done);
            changed = true;
            Jotunn.Logger.LogInfo($"Config migration {id} applied.");
        }

        foreach (var migration in new[]
        {
            (Id: "spider-0.72.1", Section: "Giant Spider", Values: new[] { ("Scale", 1f), ("Damage", 18f), ("Poison", 15f) }),
            (Id: "lindorm-0.72.2", Section: "Lindorm", Values: new[] { ("Scale", 1.3f), ("Damage", 55f) }),
            (Id: "dragon-0.72.3", Section: "Desert Dragon", Values: new[] { ("Health", 500f) }),
            (Id: "dragon-0.77.0", Section: "Desert Dragon", Values: new[] { ("Health", 800f), ("FireDamage", 15f) }),
            (Id: "kraken-0.79.0", Section: "Kraken", Values: new[] { ("Tentacles", 4f), ("BodyHealth", 4000f), ("TentacleHealth", 500f), ("BodyDamage", 90f), ("TentacleDamage", 45f), ("ShipDamagePercent", 30f) }),
            (Id: "ship-lantern-0.80.6", Section: "Ships", Values: new[] { ("LanternBrightness", 2.5f), ("LanternRange", 2f) }),
            (Id: "freya-0.72.3", Section: "Potions.GiftOfFreya", Values: new[] { ("DurationMinutes", 20f) }),
            (Id: "fenrir-0.72.3", Section: "Potions.GiftOfFenrir", Values: new[] { ("AttackSpeed", 1.5f), ("LifeSteal", 0.15f) }),
            (Id: "eir-0.72.3", Section: "Potions.GiftOfEir", Values: new[] { ("DurationMinutes", 10f), ("HealShare", 0.5f), ("HealthRegenMultiplier", 2f) })
        })
        {
            if (done.Contains(migration.Id))
            {
                continue;
            }
            foreach ((string key, float previous) in migration.Values)
            {
                ConfigDefinition definition = new(migration.Section, key);
                if (configFile.ContainsKey(definition))
                {
                    ConfigEntryBase entry = configFile[definition];
                    if ((entry.BoxedValue is float value && value == previous)
                        || (entry.BoxedValue is int count && count == previous))
                    {
                        entry.BoxedValue = entry.DefaultValue;
                    }
                }
            }
            done.Add(migration.Id);
            orphans[marker] = string.Join(",", done);
            changed = true;
            Jotunn.Logger.LogInfo($"Config migration {migration.Id} applied; custom values kept.");
        }

        if (changed)
        {
            configFile.Save();
        }
    }

    // Values in the file that no Bind has claimed yet; BepInEx keeps them private.
    private static Dictionary<ConfigDefinition, string> Orphans()
    {
        return AccessTools.Property(typeof(ConfigFile), "OrphanedEntries")?.GetValue(configFile) as Dictionary<ConfigDefinition, string>;
    }

    // Carries the values of renamed sections and settings over to their new names, so a player's config survives a
    // rename.
    private static void MoveRenamedEntries()
    {
        var orphans = Orphans();
        if (orphans == null)
        {
            return;
        }

        List<(ConfigDefinition From, ConfigDefinition To)> moves = orphans.Keys
            .SelectMany(definition => MovedSections
                .Where(moved => definition.Section == moved.Old)
                .Select(moved => (definition, new ConfigDefinition(moved.New, definition.Key))))
            .Concat(MovedEntries.Select(moved => (new ConfigDefinition(moved.Section, moved.Key), new ConfigDefinition(moved.NewSection, moved.NewKey))))
            .ToList();
        foreach ((ConfigDefinition from, ConfigDefinition to) in moves)
        {
            if (orphans.TryGetValue(from, out string value))
            {
                orphans.Remove(from);
                if (!orphans.ContainsKey(to))
                {
                    orphans[to] = value;
                    Jotunn.Logger.LogInfo($"Config: [{from.Section}] {from.Key} moved to [{to.Section}] {to.Key}.");
                }
            }
        }
    }

    private static ConfigDescription AdminOnly(string description)
    {
        return new ConfigDescription(description, null, new ConfigurationManagerAttributes { IsAdminOnly = true });
    }
}
