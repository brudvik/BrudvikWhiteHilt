using BrudvikWhiteHilt.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Navigation.Discoveries;

/// <summary>
/// The five groups discoveries are sorted into, in the order the filter panel shows them.
/// </summary>
public enum DiscoveryGroup : byte
{
    /// <summary>Burial chambers, caves, crypts and other dungeons.</summary>
    Dungeons,

    /// <summary>Villages, camps, abandoned houses, ruined towers and outposts.</summary>
    Settlements,

    /// <summary>Wild berries, mushrooms, seeds and other plants.</summary>
    Plants,

    /// <summary>Ore deposits, tar pits and other resources.</summary>
    Resources,

    /// <summary>Runestones, boss altars, dragon eggs and traders.</summary>
    Landmarks,
}

/// <summary>
/// What kinds of discoveries there are: vanilla locations by prefab name, and pickables and deposits by the item they
/// give. A kind is a key: <c>i:&lt;item&gt;</c> for an item, <c>l:&lt;id&gt;</c> for a location. The server sorts and
/// sends the keys; a client looks up their names and icons here.
/// </summary>
public static class DiscoveryCatalog
{
    /// <summary>
    /// Key prefix of a kind given by an item.
    /// </summary>
    public const string ItemPrefix = "i:";

    /// <summary>
    /// Key prefix of a location kind.
    /// </summary>
    public const string LocationPrefix = "l:";

    private const string PinIcon = "pin:";

    // Checked in order against the start of a location's prefab name.
    private static readonly LocationRule[] locationRules =
    {
        new("SunkenCrypt", "sunkencrypt", DiscoveryGroup.Dungeons, "TrophyDraugr", "Sunken crypt"),
        new("Crypt", "burialchamber", DiscoveryGroup.Dungeons, "TrophySkeleton", "Burial chamber"),
        new("TrollCave", "trollcave", DiscoveryGroup.Dungeons, "TrophyFrostTroll", "Troll cave"),
        new("MountainCave", "frostcave", DiscoveryGroup.Dungeons, "TrophyCultist", "Frost cave"),
        new("Mistlands_DvergrTownEntrance", "infestedmine", DiscoveryGroup.Dungeons, "TrophySeeker", "Infested mine"),
        new("MorgenHole", "morgenhole", DiscoveryGroup.Dungeons, "TrophyMorgen", "Morgen's lair"),
        new("CharredFortress", "charredfortress", DiscoveryGroup.Dungeons, "TrophyCharredMelee", "Charred fortress"),
        new("FortressRuins", "charredfortress", DiscoveryGroup.Dungeons, "TrophyCharredMelee", "Charred fortress"),
        new("Hildir_crypt", "hildirquest", DiscoveryGroup.Dungeons, "HildirKey_forestcrypt", "Hildir's lost chest"),
        new("Hildir_cave", "hildirquest", DiscoveryGroup.Dungeons, "HildirKey_forestcrypt", "Hildir's lost chest"),
        new("Hildir_plainsfortress", "hildirquest", DiscoveryGroup.Dungeons, "HildirKey_forestcrypt", "Hildir's lost chest"),

        new("WoodVillage", "draugrvillage", DiscoveryGroup.Settlements, "TrophyDraugrElite", "Draugr village"),
        new("GoblinCamp", "fulingcamp", DiscoveryGroup.Settlements, "TrophyGoblin", "Fuling camp"),
        new("Greydwarf_camp", "greydwarfcamp", DiscoveryGroup.Settlements, "TrophyGreydwarfShaman", "Greydwarf camp"),
        new("WoodHouse", "house", DiscoveryGroup.Settlements, "wood_door", "Abandoned house"),
        new("WoodFarm", "house", DiscoveryGroup.Settlements, "wood_door", "Abandoned house"),
        new("AbandonedLogCabin", "house", DiscoveryGroup.Settlements, "wood_door", "Abandoned house"),
        new("SwampHut", "house", DiscoveryGroup.Settlements, "wood_door", "Abandoned house"),
        new("StoneTower", "tower", DiscoveryGroup.Settlements, "stone_wall_2x1", "Ruined tower"),
        new("Ruin", "tower", DiscoveryGroup.Settlements, "stone_wall_2x1", "Ruined tower"),
        new("SwampRuin", "tower", DiscoveryGroup.Settlements, "stone_wall_2x1", "Ruined tower"),
        new("Mistlands_GuardTower", "tower", DiscoveryGroup.Settlements, "stone_wall_2x1", "Ruined tower"),
        new("CharredTowerRuins", "tower", DiscoveryGroup.Settlements, "stone_wall_2x1", "Ruined tower"),
        new("Mistlands_Harbour", "dvergr", DiscoveryGroup.Settlements, "Lantern", "Dvergr outpost"),
        new("Mistlands_Excavation", "dvergr", DiscoveryGroup.Settlements, "Lantern", "Dvergr outpost"),
        new("Mistlands_Lighthouse", "dvergr", DiscoveryGroup.Settlements, "Lantern", "Dvergr outpost"),

        new("TarPit", "tarpit", DiscoveryGroup.Resources, "Tar", "Tar pit"),

        new("Runestone_", "runestone", DiscoveryGroup.Landmarks, PinIcon + nameof(Minimap.PinType.Icon4), "Runestone"),
        new("Vendor_BlackForest", "haldor", DiscoveryGroup.Landmarks, "Coins", "Haldor the trader"),
        new("Hildir_camp", "hildir", DiscoveryGroup.Landmarks, "Coins", "Hildir the trader"),
        new("BogWitch_Camp", "bogwitch", DiscoveryGroup.Landmarks, "Coins", "The Bog Witch"),
        new("DragonEgg", "dragonegg", DiscoveryGroup.Landmarks, "DragonEgg", "Dragon eggs"),
        new("Eikthyrnir", "eikthyr", DiscoveryGroup.Landmarks, "TrophyEikthyr", "Eikthyr's altar"),
        new("GDKing", "elder", DiscoveryGroup.Landmarks, "TrophyTheElder", "The Elder's altar"),
        new("Bonemass", "bonemass", DiscoveryGroup.Landmarks, "TrophyBonemass", "Bonemass's altar"),
        new("Dragonqueen", "moder", DiscoveryGroup.Landmarks, "TrophyDragonQueen", "Moder's altar"),
        new("GoblinKing", "yagluth", DiscoveryGroup.Landmarks, "TrophyGoblinKing", "Yagluth's altar"),
        new("Mistlands_DvergrBossEntrance", "queen", DiscoveryGroup.Landmarks, "TrophySeekerQueen", "The Queen's lair"),
        new("FaderLocation", "fader", DiscoveryGroup.Landmarks, "TrophyFader", "Fader's altar"),
    };

    private static readonly Dictionary<string, LocationRule> rulesById = locationRules
        .GroupBy(rule => rule.Id).ToDictionary(group => group.Key, group => group.First());

    private static readonly Dictionary<string, Sprite> icons = new();

    /// <summary>
    /// Registers the English texts. Call from the plugin's Awake.
    /// </summary>
    public static void RegisterTranslations()
    {
        foreach (LocationRule rule in rulesById.Values)
        {
            Translations.AddEnglish($"whitehilt_disc_l_{rule.Id}", rule.English);
        }

        Translations.AddEnglish("whitehilt_disc_group_dungeons", "Caves and crypts");
        Translations.AddEnglish("whitehilt_disc_group_settlements", "Settlements");
        Translations.AddEnglish("whitehilt_disc_group_plants", "Berries and plants");
        Translations.AddEnglish("whitehilt_disc_group_resources", "Resources");
        Translations.AddEnglish("whitehilt_disc_group_landmarks", "Landmarks");
        Translations.AddEnglish("whitehilt_disc_title", "Munin's memory");
        Translations.AddEnglish("whitehilt_disc_locked", "Needs Exploration {0}");
        Translations.AddEnglish("whitehilt_disc_hint", "Click an icon to show or hide it on the map");
        Translations.AddEnglish("whitehilt_disc_ripe", "{0} ready to pick");
        Translations.AddEnglish("whitehilt_disc_found", "{0} found");
        Translations.AddEnglish("whitehilt_disc_unlimited", "Unlimited in chests");
        Translations.AddEnglish("whitehilt_disc_current_biome", "Found in this biome");
        Translations.AddEnglish("whitehilt_disc_show_all", "Show all");
        Translations.AddEnglish("whitehilt_disc_show_all_hint", "Show every kind found on the map");
        Translations.AddEnglish("whitehilt_disc_hide_all", "Hide all");
        Translations.AddEnglish("whitehilt_disc_hide_all_hint", "Hide every kind from the map");
        Translations.AddEnglish("whitehilt_disc_hide_unlimited", "Hide unlimited: {0}");
        Translations.AddEnglish("whitehilt_disc_hide_unlimited_hint", "Keeps everything you have unlimited in chests off the map, also what becomes unlimited later");
        Translations.AddEnglish("whitehilt_disc_hidden_unlimited", "Hidden: unlimited items are hidden");
        Translations.AddEnglish("whitehilt_disc_on", "on");
        Translations.AddEnglish("whitehilt_disc_off", "off");
    }

    internal static Dictionary<string, Heightmap.Biome> GetLocationBiomes()
    {
        Dictionary<string, Heightmap.Biome> result = new();
        if (ZoneSystem.instance == null)
        {
            return result;
        }

        foreach (ZoneSystem.ZoneLocation location in ZoneSystem.instance.m_locations)
        {
            if (TryClassifyLocation(location.m_prefabName, out string key, out _))
            {
                result.TryGetValue(key, out Heightmap.Biome biomes);
                result[key] = biomes | location.m_biome;
            }
        }

        return result;
    }

    /// <summary>
    /// Key of the kind given by an item.
    /// </summary>
    /// <param name="item">Item prefab name.</param>
    /// <returns>The key.</returns>
    public static string ItemKey(string item)
    {
        return ItemPrefix + item;
    }

    /// <summary>
    /// Finds the kind of a vanilla location.
    /// </summary>
    /// <param name="prefabName">Prefab name of the location.</param>
    /// <param name="key">Its kind key.</param>
    /// <param name="group">Its group.</param>
    /// <returns>False for a location that is not shown.</returns>
    public static bool TryClassifyLocation(string prefabName, out string key, out DiscoveryGroup group)
    {
        key = null;
        group = default;
        if (string.IsNullOrEmpty(prefabName))
        {
            return false;
        }

        foreach (LocationRule rule in locationRules)
        {
            if (prefabName.StartsWith(rule.Prefix, StringComparison.Ordinal))
            {
                key = LocationPrefix + rule.Id;
                group = rule.Group;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The icon of a kind: the item's icon, or the trophy, item or piece that stands for a location.
    /// </summary>
    /// <param name="key">Kind key.</param>
    /// <returns>The icon, or null if none was found.</returns>
    public static Sprite GetIcon(string key)
    {
        if (icons.TryGetValue(key, out Sprite cached) && cached != null)
        {
            return cached;
        }

        string source = key.StartsWith(LocationPrefix, StringComparison.Ordinal) && rulesById.TryGetValue(key.Substring(LocationPrefix.Length), out LocationRule rule)
            ? rule.Icon
            : key.Substring(ItemPrefix.Length);
        Sprite icon = FindIcon(source);
        if (icon != null)
        {
            icons[key] = icon;
        }

        return icon;
    }

    /// <summary>
    /// The localized name of a kind.
    /// </summary>
    /// <param name="key">Kind key.</param>
    /// <returns>The name.</returns>
    public static string GetLabel(string key)
    {
        if (key.StartsWith(LocationPrefix, StringComparison.Ordinal))
        {
            return Localization.instance.Localize($"$whitehilt_disc_l_{key.Substring(LocationPrefix.Length)}");
        }

        ItemDrop item = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(key.Substring(ItemPrefix.Length))?.GetComponent<ItemDrop>() : null;
        return item != null ? Localization.instance.Localize(item.m_itemData.m_shared.m_name) : key.Substring(ItemPrefix.Length);
    }

    /// <summary>
    /// The localized name of a group.
    /// </summary>
    /// <param name="group">The group.</param>
    /// <returns>The name.</returns>
    public static string GetGroupLabel(DiscoveryGroup group)
    {
        return Localization.instance.Localize($"$whitehilt_disc_group_{group.ToString().ToLowerInvariant()}");
    }

    private static Sprite FindIcon(string source)
    {
        if (source.StartsWith(PinIcon, StringComparison.Ordinal))
        {
            return Minimap.instance != null && Enum.TryParse(source.Substring(PinIcon.Length), out Minimap.PinType type)
                ? Minimap.instance.GetSprite(type)
                : null;
        }

        ItemDrop item = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(source)?.GetComponent<ItemDrop>() : null;
        if (item != null && item.m_itemData.m_shared.m_icons != null && item.m_itemData.m_shared.m_icons.Length > 0)
        {
            return item.m_itemData.m_shared.m_icons[0];
        }

        return ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(source)?.GetComponent<Piece>()?.m_icon : null;
    }

    private sealed class LocationRule
    {
        public LocationRule(string prefix, string id, DiscoveryGroup group, string icon, string english)
        {
            Prefix = prefix;
            Id = id;
            Group = group;
            Icon = icon;
            English = english;
        }

        public string Prefix { get; }

        public string Id { get; }

        public DiscoveryGroup Group { get; }

        public string Icon { get; }

        public string English { get; }
    }
}
