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
    private const string BalanceMigration = "balance-0.28";

    // Settings whose default changed in the 0.28 balance pass; old files still hold the old value.
    private static readonly (string Section, string Key)[] BalanceResets =
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
    };

    private static readonly (string Section, string Key)[] RetiredEntries =
    {
        ("Gear.Indestructible", "ArmorBonus"),
        ("Gear.Weapons", "BonusDamage"),
        ("Potions.GiftOfOdin", "MaxHealth"),
        ("Potions.GiftOfOdin", "HealPerFrame"),
    };

    private static readonly Dictionary<string, ConfigEntry<TierOverride>> tierOverrides = new();
    private static readonly Dictionary<string, ConfigEntry<bool>> enabledEntries = new();
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

        Mode = config.Bind(
            GeneralSection,
            "Mode",
            ProgressionMode.Full,
            AdminOnly("Full: every White Hilt recipe is available as soon as its materials are known.\n" +
                      "Linear: only the White Hilt tools are available at the start. Weapons, armor, potions and the ship unlock " +
                      "when you first obtain the key material of each biome (Bronze, Iron, Silver, Black Metal, Eitr, Flametal), " +
                      "and their recipes also cost some of that material."));

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
            AdminOnly($"{entry.DisplayName}. Default tier: {entry.DefaultTier}.\n" +
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
        return !enabledEntries.TryGetValue(id, out var entry) || entry.Value;
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
        if (!done.Contains(BalanceMigration))
        {
            foreach ((string section, string key) in BalanceResets)
            {
                ConfigDefinition definition = new(section, key);
                if (configFile.ContainsKey(definition))
                {
                    ConfigEntryBase entry = configFile[definition];
                    entry.BoxedValue = entry.DefaultValue;
                }
            }

            done.Add(BalanceMigration);
            orphans[marker] = string.Join(",", done);
            changed = true;
            Jotunn.Logger.LogInfo($"Config migration {BalanceMigration} applied.");
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

    private static ConfigDescription AdminOnly(string description)
    {
        return new ConfigDescription(description, null, new ConfigurationManagerAttributes { IsAdminOnly = true });
    }
}
