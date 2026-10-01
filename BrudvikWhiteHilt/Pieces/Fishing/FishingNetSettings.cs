using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrudvikWhiteHilt.Pieces.Fishing;

/// <summary>
/// Config for the Net Winch and the Shore Net, section "Fishing.Net". Server-synced.
/// </summary>
public static class FishingNetSettings
{
    private const string Section = "Fishing.Net";

    private static string baitText;
    private static HashSet<string> baits = new();
    private static bool bound;

    /// <summary>Minutes for one net in deep enough water to catch a fish.</summary>
    public static ConfigEntry<float> Minutes { get; private set; }

    /// <summary>Nets one winch can take the catch from.</summary>
    public static ConfigEntry<int> MaxNets { get; private set; }

    /// <summary>How far, in metres, a net may lie from its winch.</summary>
    public static ConfigEntry<float> Range { get; private set; }

    /// <summary>Water depth in metres a net needs to catch.</summary>
    public static ConfigEntry<float> MinDepth { get; private set; }

    /// <summary>Nets closer than this, in metres, share the fish between them.</summary>
    public static ConfigEntry<float> CrowdRadius { get; private set; }

    /// <summary>Hours the nets go on catching while nobody is near.</summary>
    public static ConfigEntry<float> CatchUpHours { get; private set; }

    /// <summary>Rows in the fish barrel, 4 slots each.</summary>
    public static ConfigEntry<int> BarrelRows { get; private set; }

    /// <summary>Chance a fish is one size bigger.</summary>
    public static ConfigEntry<float> BiggerFishChance { get; private set; }

    /// <summary>Fishing experience per fish, given when the barrel is opened.</summary>
    public static ConfigEntry<float> SkillRaise { get; private set; }

    /// <summary>Prefab names of the bait, comma separated.</summary>
    public static ConfigEntry<string> BaitItems { get; private set; }

    /// <summary>Share of the catching time while there is bait in the barrel.</summary>
    public static ConfigEntry<float> BaitTime { get; private set; }

    /// <summary>Fish the nets catch before they must be mended. 0: never.</summary>
    public static ConfigEntry<int> WearCatches { get; private set; }

    /// <summary>Prefab name of the item that mends the nets.</summary>
    public static ConfigEntry<string> MendItem { get; private set; }

    /// <summary>How many of the mending item one mending takes.</summary>
    public static ConfigEntry<int> MendAmount { get; private set; }

    /// <summary>Whether the nets also bring up seaweed, and on the ocean now and then an amber pearl.</summary>
    public static ConfigEntry<bool> Bycatch { get; private set; }

    /// <summary>Chance per fish of seaweed as bycatch.</summary>
    public static ConfigEntry<float> SeaweedChance { get; private set; }

    /// <summary>Chance per fish on the ocean of an amber pearl as bycatch.</summary>
    public static ConfigEntry<float> PearlChance { get; private set; }

    /// <summary>
    /// Binds the config entries. Runs from the winch's constructor, in the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        if (bound)
        {
            return;
        }

        bound = true;
        Minutes = WhiteHiltConfig.BindAdminOnly(Section, "Minutes", 6f,
            "Minutes for one Shore Net in deep enough water to catch a fish.", new AcceptableValueRange<float>(0.5f, 120f));
        MaxNets = WhiteHiltConfig.BindAdminOnly(Section, "MaxNets", 2,
            "Shore Nets one Net Winch takes the catch from.", new AcceptableValueRange<int>(1, 8));
        Range = WhiteHiltConfig.BindAdminOnly(Section, "Range", 25f,
            "How far, in metres, a Shore Net may lie from its Net Winch.", new AcceptableValueRange<float>(5f, 60f));
        MinDepth = WhiteHiltConfig.BindAdminOnly(Section, "MinDepth", 1.5f,
            "Water depth in metres a Shore Net needs to catch. A net that is only partly that deep catches less.", new AcceptableValueRange<float>(0.5f, 10f));
        CrowdRadius = WhiteHiltConfig.BindAdminOnly(Section, "CrowdRadius", 8f,
            "Shore Nets closer to each other than this, in metres, share the fish between them. 0: they never do.", new AcceptableValueRange<float>(0f, 30f));
        CatchUpHours = WhiteHiltConfig.BindAdminOnly(Section, "CatchUpHours", 2f,
            "Hours the nets go on catching while nobody is near. 0: they only catch while someone is near.", new AcceptableValueRange<float>(0f, 48f));
        BarrelRows = WhiteHiltConfig.BindAdminOnly(Section, "BarrelRows", 2,
            "Rows in the Net Winch's fish barrel, 4 slots each. Fewer rows hide the fish in the removed rows until there are more again.", new AcceptableValueRange<int>(1, 4));
        BiggerFishChance = WhiteHiltConfig.BindAdminOnly(Section, "BiggerFishChance", 0.1f,
            "Chance that a fish from the nets is one size bigger.", new AcceptableValueRange<float>(0f, 1f));
        SkillRaise = WhiteHiltConfig.BindAdminOnly(Section, "SkillRaise", 0.5f,
            "Fishing experience for each fish in the barrel, given to the one who opens it.", new AcceptableValueRange<float>(0f, 10f));
        BaitItems = WhiteHiltConfig.BindAdminOnly(Section, "BaitItems",
            "FishingBait,FishingBaitForest,FishingBaitSwamp,FishingBaitOcean,FishingBaitPlains,FishingBaitCave,FishingBaitMistlands,FishingBaitAshlands,FishingBaitDeepNorth,Entrails,NeckTail",
            "Prefab names of the bait that can go in the barrel, comma separated. Each fish uses one. Empty: no bait.");
        BaitTime = WhiteHiltConfig.BindAdminOnly(Section, "BaitTime", 0.6f,
            "Share of the catching time while there is bait in the barrel: 0.6 catches 40% faster.", new AcceptableValueRange<float>(0.1f, 1f));
        WearCatches = WhiteHiltConfig.BindAdminOnly(Section, "WearCatches", 40,
            "Fish the nets of a winch catch before they are torn and must be mended. 0: they never tear.", new AcceptableValueRange<int>(0, 1000));
        MendItem = WhiteHiltConfig.BindAdminOnly(Section, "MendItem", "LeatherScraps",
            "Prefab name of the item that mends the nets.");
        MendAmount = WhiteHiltConfig.BindAdminOnly(Section, "MendAmount", 4,
            "How many of the mending item one mending takes. 0: mending is free.", new AcceptableValueRange<int>(0, 50));
        Bycatch = WhiteHiltConfig.BindAdminOnly(Section, "Bycatch", true,
            "The nets now and then bring up seaweed too, and on the ocean an amber pearl.");
        SeaweedChance = WhiteHiltConfig.BindAdminOnly(Section, "SeaweedChance", 0.1f,
            "Chance per fish of seaweed as bycatch (needs Bycatch).", new AcceptableValueRange<float>(0f, 1f));
        PearlChance = WhiteHiltConfig.BindAdminOnly(Section, "PearlChance", 0.03f,
            "Chance per fish on the ocean of an amber pearl as bycatch (needs Bycatch).", new AcceptableValueRange<float>(0f, 1f));
    }

    /// <summary>
    /// True if the item is bait for the nets.
    /// </summary>
    /// <param name="prefabName">Prefab name of the item.</param>
    /// <returns>True for bait.</returns>
    public static bool IsBait(string prefabName)
    {
        string text = BaitItems.Value ?? string.Empty;
        if (text != baitText)
        {
            baitText = text;
            baits = new HashSet<string>(text.Split(',').Select(name => name.Trim()).Where(name => name.Length > 0), StringComparer.OrdinalIgnoreCase);
        }

        return baits.Contains(prefabName);
    }
}
