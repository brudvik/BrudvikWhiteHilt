using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrudvikWhiteHilt.Navigation.Discoveries;

/// <summary>
/// Config for the discoveries Munin's Perch shows on the map, section "Map.Discoveries". What the server finds and
/// shares is the admin's; how it is drawn is each player's.
/// </summary>
public static class DiscoverySettings
{
    private const string Section = "Map.Discoveries";

    private static HashSet<string> resourceItems = new();
    private static HashSet<string> ignoredItems = new();
    private static HashSet<string> plantItems = new();

    /// <summary>Whether the server finds discoveries at all.</summary>
    public static ConfigEntry<bool> AllowDiscoveries { get; private set; }

    /// <summary>Whether only what is uncovered on the map table's map counts, rather than everywhere players have been.</summary>
    public static ConfigEntry<bool> OnlyMapTableMap { get; private set; }

    /// <summary>Seconds between two scans of the world.</summary>
    public static ConfigEntry<float> ScanInterval { get; private set; }

    /// <summary>Size in metres of the squares in which plants and deposits of one kind are counted as one marker.</summary>
    public static ConfigEntry<float> ClusterSize { get; private set; }

    /// <summary>Exploration level for the markers on the large map.</summary>
    public static ConfigEntry<int> LargeMapLevel { get; private set; }

    /// <summary>Exploration level for the markers on the minimap.</summary>
    public static ConfigEntry<int> MinimapLevel { get; private set; }

    /// <summary>Whether silver veins are shown; the wishbone is meant to find them.</summary>
    public static ConfigEntry<bool> AllowSilver { get; private set; }

    /// <summary>Whether caves, crypts and other dungeons are shown.</summary>
    public static ConfigEntry<bool> AllowDungeons { get; private set; }

    /// <summary>Whether villages, camps, ruins and towers are shown.</summary>
    public static ConfigEntry<bool> AllowSettlements { get; private set; }

    /// <summary>Whether berries, mushrooms and other wild plants are shown.</summary>
    public static ConfigEntry<bool> AllowPlants { get; private set; }

    /// <summary>Whether ore deposits and other resources are shown.</summary>
    public static ConfigEntry<bool> AllowResources { get; private set; }

    /// <summary>Whether runestones, boss altars and traders are shown.</summary>
    public static ConfigEntry<bool> AllowLandmarks { get; private set; }

    /// <summary>Items whose deposits and pickables count as resources.</summary>
    public static ConfigEntry<string> ResourceItemList { get; private set; }

    /// <summary>Items whose pickables are never shown.</summary>
    public static ConfigEntry<string> IgnoredItemList { get; private set; }

    /// <summary>Items whose pickables count as plants even though they are no food and do not grow back.</summary>
    public static ConfigEntry<string> PlantItemList { get; private set; }

    /// <summary>Whether the player sees the markers on the minimap too.</summary>
    public static ConfigEntry<bool> ShowOnMinimap { get; private set; }

    /// <summary>Marker size on the large map, in pixels.</summary>
    public static ConfigEntry<float> LargeIconSize { get; private set; }

    /// <summary>Marker size on the minimap, in pixels.</summary>
    public static ConfigEntry<float> SmallIconSize { get; private set; }

    /// <summary>Most markers drawn at once.</summary>
    public static ConfigEntry<int> MaxMarkers { get; private set; }

    /// <summary>
    /// Binds the config entries. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        AllowDiscoveries = WhiteHiltConfig.BindAdminOnly(Section, "AllowDiscoveries", true,
            "While Munin's Perch stands at a map table, caves, settlements, berries, resources and landmarks that players have found show on everyone's map.");
        OnlyMapTableMap = WhiteHiltConfig.BindAdminOnly(Section, "OnlyMapTableMap", true,
            "Only what is uncovered on the map of the map table the perch stands at counts as found; players add to it by recording their map there. Off: everything near where players have been.");
        ScanInterval = WhiteHiltConfig.BindAdminOnly(Section, "ScanInterval", 60f,
            "Seconds between two searches of the world for discoveries.", new AcceptableValueRange<float>(10f, 3600f));
        ClusterSize = WhiteHiltConfig.BindAdminOnly(Section, "ClusterSize", 64f,
            "Plants and deposits of one kind within a square of this many metres count as one marker, with their number.", new AcceptableValueRange<float>(16f, 512f));
        LargeMapLevel = WhiteHiltConfig.BindAdminOnly(Section, "LargeMapLevel", 20,
            "Exploration level a player needs to see discoveries on the large map.", new AcceptableValueRange<int>(0, 100));
        MinimapLevel = WhiteHiltConfig.BindAdminOnly(Section, "MinimapLevel", 50,
            "Exploration level a player needs to see discoveries on the minimap.", new AcceptableValueRange<int>(0, 100));
        AllowSilver = WhiteHiltConfig.BindAdminOnly(Section, "AllowSilver", false,
            "Show silver veins. Off by default: the wishbone is the way to find them.");
        AllowDungeons = WhiteHiltConfig.BindAdminOnly(Section, "AllowDungeons", true, "Show burial chambers, troll caves, sunken crypts, frost caves and other dungeons.");
        AllowSettlements = WhiteHiltConfig.BindAdminOnly(Section, "AllowSettlements", true, "Show draugr villages, fuling camps, abandoned houses, ruined towers and dvergr outposts.");
        AllowPlants = WhiteHiltConfig.BindAdminOnly(Section, "AllowPlants", true, "Show wild berries, mushrooms, seeds and other plants.");
        AllowResources = WhiteHiltConfig.BindAdminOnly(Section, "AllowResources", true, "Show ore deposits, tar pits and other resources.");
        AllowLandmarks = WhiteHiltConfig.BindAdminOnly(Section, "AllowLandmarks", true, "Show runestones, boss altars, dragon eggs and traders.");
        ResourceItemList = WhiteHiltConfig.BindAdminOnly(Section, "ResourceItems",
            "CopperOre,TinOre,SilverOre,Obsidian,IronScrap,FlametalOreNew,FlametalOre,Chitin,BlackMarble,SoftTissue,Guck,Tar",
            "Items (prefab names, comma separated) whose deposits and pickables show as resources.");
        IgnoredItemList = WhiteHiltConfig.BindAdminOnly(Section, "IgnoredItems",
            "Flint,Stone,Wood,StoneRock,Coins,Amber,AmberPearl,Ruby,SilverNecklace,BoneFragments",
            "Items (prefab names, comma separated) whose pickables are never shown, e.g. loose stones and treasure.");
        PlantItemList = WhiteHiltConfig.BindAdminOnly(Section, "PlantItems", "Flax,Barley,Thistle,Dandelion,Fiddleheadfern",
            "Items (prefab names, comma separated) whose pickables show as plants although they are no food and do not grow back.");

        ShowOnMinimap = WhiteHiltConfig.BindLocal(Section, "ShowOnMinimap", true, "Show the discoveries you picked on the minimap too, once your Exploration is high enough.");
        LargeIconSize = WhiteHiltConfig.BindLocal(Section, "LargeIconSize", 26f, "Size of a discovery marker on the large map, in pixels.", new AcceptableValueRange<float>(12f, 64f));
        SmallIconSize = WhiteHiltConfig.BindLocal(Section, "SmallIconSize", 16f, "Size of a discovery marker on the minimap, in pixels.", new AcceptableValueRange<float>(8f, 48f));
        MaxMarkers = WhiteHiltConfig.BindLocal(Section, "MaxMarkers", 400, "Most discovery markers drawn at once; zoom in to see the rest.", new AcceptableValueRange<int>(20, 3000));

        ResourceItemList.SettingChanged += (_, _) => ParseLists();
        IgnoredItemList.SettingChanged += (_, _) => ParseLists();
        PlantItemList.SettingChanged += (_, _) => ParseLists();
        ParseLists();
    }

    /// <summary>
    /// True if pickables and deposits of the item show as resources.
    /// </summary>
    /// <param name="item">Item prefab name.</param>
    /// <returns>True for a resource.</returns>
    public static bool IsResource(string item)
    {
        return resourceItems.Contains(item);
    }

    /// <summary>
    /// True if pickables of the item are never shown.
    /// </summary>
    /// <param name="item">Item prefab name.</param>
    /// <returns>True to leave it out.</returns>
    public static bool IsIgnored(string item)
    {
        return ignoredItems.Contains(item);
    }

    /// <summary>
    /// True if pickables of the item always show as plants.
    /// </summary>
    /// <param name="item">Item prefab name.</param>
    /// <returns>True for a plant.</returns>
    public static bool IsPlantItem(string item)
    {
        return plantItems.Contains(item);
    }

    /// <summary>
    /// Whether the admin lets a group show.
    /// </summary>
    /// <param name="group">The group.</param>
    /// <returns>True if it may show.</returns>
    public static bool Allows(DiscoveryGroup group)
    {
        return group switch
        {
            DiscoveryGroup.Dungeons => AllowDungeons.Value,
            DiscoveryGroup.Settlements => AllowSettlements.Value,
            DiscoveryGroup.Plants => AllowPlants.Value,
            DiscoveryGroup.Resources => AllowResources.Value,
            _ => AllowLandmarks.Value,
        };
    }

    private static void ParseLists()
    {
        resourceItems = Parse(ResourceItemList.Value);
        ignoredItems = Parse(IgnoredItemList.Value);
        plantItems = Parse(PlantItemList.Value);
    }

    private static HashSet<string> Parse(string list)
    {
        return new HashSet<string>((list ?? string.Empty).Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Trim()).Where(item => item.Length > 0));
    }
}
