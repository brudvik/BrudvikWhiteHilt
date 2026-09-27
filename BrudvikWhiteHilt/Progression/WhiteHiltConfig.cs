using BepInEx.Configuration;
using System.Collections.Generic;

namespace BrudvikWhiteHilt.Progression;

/// <summary>
/// Config entries for White Hilt: progression, and per-item settings bound by the items themselves.
/// All entries are admin-only and synced from the server.
/// </summary>
public static class WhiteHiltConfig
{
    private const string GeneralSection = "General";
    private const string TiersSection = "Tiers";

    private static readonly Dictionary<string, ConfigEntry<TierOverride>> tierOverrides = new();
    private static ConfigFile configFile;

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
    /// Binds the tier override entry for an item or piece.
    /// </summary>
    /// <param name="entry">The item or piece.</param>
    public static void BindTierOverride(IWhiteHiltProgressionEntry entry)
    {
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

    private static ConfigDescription AdminOnly(string description)
    {
        return new ConfigDescription(description, null, new ConfigurationManagerAttributes { IsAdminOnly = true });
    }
}
