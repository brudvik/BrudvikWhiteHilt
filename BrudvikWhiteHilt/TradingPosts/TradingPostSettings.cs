using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;
using System.Collections.Generic;

namespace BrudvikWhiteHilt.TradingPosts;

/// <summary>
/// Config entries for the trading posts. All are admin-only and synced from the server.
/// </summary>
public static class TradingPostSettings
{
    private const string Section = "TradingPosts";

    private static string pricesText;
    private static Dictionary<string, float> prices = new();
    private static string levelsText;
    private static List<int> levels = new();

    /// <summary>Whether trading posts are made. Posts already in the world stay either way.</summary>
    public static ConfigEntry<bool> Enabled { get; private set; }

    /// <summary>How many posts each biome that meets the sea gets.</summary>
    public static ConfigEntry<int> PerBiome { get; private set; }

    /// <summary>Least distance between two posts, in metres.</summary>
    public static ConfigEntry<float> MinDistance { get; private set; }

    /// <summary>Least distance from a post to anything a player has built, in metres.</summary>
    public static ConfigEntry<float> BuildingDistance { get; private set; }

    /// <summary>Share of its own asking price a post pays for what it buys.</summary>
    public static ConfigEntry<float> BuyShare { get; private set; }

    /// <summary>Share of its asking price a post pays a player with Trading 100.</summary>
    public static ConfigEntry<float> MaxBuyShare { get; private set; }

    /// <summary>Share off what a post sells for a player with Trading 100.</summary>
    public static ConfigEntry<float> BuyDiscount { get; private set; }

    /// <summary>The Trading levels that open each step of a post's gear, comma separated.</summary>
    public static ConfigEntry<string> UnlockLevels { get; private set; }

    /// <summary>A post's gear costs its recipe's materials times this.</summary>
    public static ConfigEntry<float> GearPriceFactor { get; private set; }

    /// <summary>Coins traded for one point of Trading.</summary>
    public static ConfigEntry<float> ExperiencePerCoins { get; private set; }

    /// <summary>Asking prices that replace the built-in ones: Prefab:Price, comma separated.</summary>
    public static ConfigEntry<string> Prices { get; private set; }

    /// <summary>Whether the posts pay for an item that is unlimited in the world's chests.</summary>
    public static ConfigEntry<bool> BuyUnlimitedGoods { get; private set; }

    /// <summary>Coins a Trading Post Chart costs at Hildir.</summary>
    public static ConfigEntry<int> ChartPrice { get; private set; }

    /// <summary>Coins a Mine Chart costs at the Mistlands posts; 0 takes it out of their stores.</summary>
    public static ConfigEntry<int> MineChartPrice { get; private set; }

    /// <summary>Percent chance that the hunter sits at a post when a player comes there.</summary>
    public static ConfigEntry<int> HunterChance { get; private set; }

    /// <summary>Coins a trophy costs per coin a plain material of its biome is worth.</summary>
    public static ConfigEntry<float> TrophyWorth { get; private set; }

    /// <summary>
    /// The configured asking prices, read again only when the setting changed.
    /// </summary>
    public static IReadOnlyDictionary<string, float> ConfiguredPrices
    {
        get
        {
            string text = Prices?.Value ?? string.Empty;
            if (text != pricesText)
            {
                pricesText = text;
                prices = TradingPostGoods.ParsePrices(text);
            }

            return prices;
        }
    }

    /// <summary>
    /// The configured unlock levels, read again only when the setting changed.
    /// </summary>
    public static IReadOnlyList<int> Levels
    {
        get
        {
            string text = UnlockLevels?.Value ?? string.Empty;
            if (text != levelsText)
            {
                levelsText = text;
                levels = TradingGear.ParseLevels(text);
            }

            return levels;
        }
    }

    /// <summary>
    /// Binds the entries. Call once from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        Enabled = WhiteHiltConfig.BindAdminOnly(Section, "Enabled", true,
            "Make trading posts by the sea. Off: no new ones are made; the posts already in the world stay.");
        PerBiome = WhiteHiltConfig.BindAdminOnly(Section, "PerBiome", 4,
            "How many trading posts each biome gets, where its coast (or the shore below it) has room for them. Placed once per world; " +
            "a higher number takes effect with whitehilt_tradepost place.", new AcceptableValueRange<int>(0, 10));
        MinDistance = WhiteHiltConfig.BindAdminOnly(Section, "MinDistance", 1000f,
            "Least distance between two trading posts, in metres.", new AcceptableValueRange<float>(200f, 5000f));
        BuildingDistance = WhiteHiltConfig.BindAdminOnly(Section, "BuildingDistance", 60f,
            "Least distance from a new trading post to anything a player has built, in metres.", new AcceptableValueRange<float>(0f, 300f));
        BuyShare = WhiteHiltConfig.BindAdminOnly(Section, "BuyShare", 0.6f,
            "Share of its own asking price a trading post pays for the goods it buys (0.6 = three fifths).", new AcceptableValueRange<float>(0f, 1f));
        MaxBuyShare = WhiteHiltConfig.BindAdminOnly(Section, "MaxBuyShare", 0.9f,
            "Share of its own asking price a trading post pays a player with Trading 100; it grows from BuyShare at level 0.",
            new AcceptableValueRange<float>(0f, 1f));
        BuyDiscount = WhiteHiltConfig.BindAdminOnly(Section, "BuyDiscount", 0.1f,
            "Share off what a trading post sells for a player with Trading 100 (0.1 = a tenth off); less at lower levels.",
            new AcceptableValueRange<float>(0f, 0.9f));
        UnlockLevels = WhiteHiltConfig.BindAdminOnly(Section, "UnlockLevels", "20,40,60,80",
            "The Trading levels that open a post's gear, comma separated: the cheapest part opens at the first, the dearest at the last. " +
            "Empty: all of it is open at once.");
        GearPriceFactor = WhiteHiltConfig.BindAdminOnly(Section, "GearPriceFactor", 1.5f,
            "A trading post's gear costs the materials of its recipe at the posts' prices times this.", new AcceptableValueRange<float>(0.1f, 10f));
        ExperiencePerCoins = WhiteHiltConfig.BindAdminOnly(Section, "ExperiencePerCoins", 5f,
            "Coins bought or sold for at a trading post, or earned on an escort, for one point of Trading. 0: the skill does not rise.",
            new AcceptableValueRange<float>(0f, 1000f));
        Prices = WhiteHiltConfig.BindAdminOnly(Section, "Prices", string.Empty,
            "Asking prices that replace the built-in ones, in coins per piece: Prefab:Price, comma separated, e.g. \"Wood:2, CopperOre:6\". " +
            "What a post pays follows from them by BuyShare.");
        BuyUnlimitedGoods = WhiteHiltConfig.BindAdminOnly(Section, "BuyUnlimitedGoods", true,
            "The trading posts pay for goods even when they are unlimited in the world's chests. Off: they pay nothing for those.");
        ChartPrice = WhiteHiltConfig.BindAdminOnly(Section, "ChartPrice", 150,
            "Coins a Trading Post Chart costs at Hildir. Needs a restart.", new AcceptableValueRange<int>(0, 5000));
        MineChartPrice = WhiteHiltConfig.BindAdminOnly(Section, "MineChartPrice", 250,
            "Coins a Mine Chart costs at the Mistlands trading posts. It marks the nearest infested mine the reader has not found. 0: not sold.",
            new AcceptableValueRange<int>(0, 5000));
        HunterChance = WhiteHiltConfig.BindAdminOnly(Section, "HunterChance", 20,
            "Percent chance that a hunter who sells the biome's trophies sits at a trading post when a player comes there. The answer holds " +
            "for a game day. 0: no hunter.", new AcceptableValueRange<int>(0, 100));
        TrophyWorth = WhiteHiltConfig.BindAdminOnly(Section, "TrophyWorth", 15f,
            "A trophy at the hunter costs what a plain material of its biome is worth times this (three times as much for the great beasts' " +
            "trophies).", new AcceptableValueRange<float>(1f, 1000f));
    }
}
