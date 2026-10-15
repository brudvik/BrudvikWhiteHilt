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

    /// <summary>Whether what a post has been sold lately lowers what it pays.</summary>
    public static ConfigEntry<bool> Market { get; private set; }

    /// <summary>How much less a post pays for each stack of a good it has been sold lately.</summary>
    public static ConfigEntry<float> MarketDropPerStack { get; private set; }

    /// <summary>The least share of the full price a post pays.</summary>
    public static ConfigEntry<float> MarketLeast { get; private set; }

    /// <summary>Stacks of a good a post sells on in a game day, so its price comes back.</summary>
    public static ConfigEntry<float> MarketRecoverPerDay { get; private set; }

    /// <summary>How much more the posts pay while a great storm rages and for a day after.</summary>
    public static ConfigEntry<float> StormDemand { get; private set; }

    /// <summary>Whether a merchant now and then moors at a player's own jetty.</summary>
    public static ConfigEntry<bool> Visits { get; private set; }

    /// <summary>Chance in percent per game day, at home by the jetty, that a merchant comes.</summary>
    public static ConfigEntry<float> VisitChance { get; private set; }

    /// <summary>Game days at least between two visits.</summary>
    public static ConfigEntry<int> VisitMinDays { get; private set; }

    /// <summary>How many goods a visiting merchant carries.</summary>
    public static ConfigEntry<int> VisitGoods { get; private set; }

    /// <summary>A visiting merchant's prices as a share of the posts'.</summary>
    public static ConfigEntry<float> VisitMarkup { get; private set; }

    /// <summary>Coins a Trading Post Chart costs at Hildir.</summary>
    public static ConfigEntry<int> ChartPrice { get; private set; }

    /// <summary>Coins a Mine Chart costs at the Mistlands posts; 0 takes it out of their stores.</summary>
    public static ConfigEntry<int> MineChartPrice { get; private set; }

    /// <summary>Coins per plain material of a biome that a piece of its traders' garb costs.</summary>
    public static ConfigEntry<float> GarbWorth { get; private set; }

    /// <summary>Percent better prices at a biome's posts per piece of its traders' garb worn.</summary>
    public static ConfigEntry<float> GarbTradeBonus { get; private set; }

    /// <summary>Percent better prices more at a biome's posts for the whole garb worn.</summary>
    public static ConfigEntry<float> GarbSetTradeBonus { get; private set; }

    /// <summary>Percent chance, once a game day, that a monster plagues a post when a player comes there.</summary>
    public static ConfigEntry<int> TerrorChance { get; private set; }

    /// <summary>Coins per plain material of a biome that the head of its plague's monster is paid.</summary>
    public static ConfigEntry<float> TerrorBounty { get; private set; }

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
        Market = WhiteHiltConfig.BindAdminOnly(Section, "Market", true,
            "Supply and demand: the more of a good a post has been sold lately, the less it pays for more of it; the price comes back over the days. Each post keeps its own, shared by all players.");
        MarketDropPerStack = WhiteHiltConfig.BindAdminOnly(Section, "MarketDropPerStack", 0.1f,
            "How much less a post pays for each stack of a good it has been sold lately (0.1 = 10% per stack).", new AcceptableValueRange<float>(0f, 0.5f));
        MarketLeast = WhiteHiltConfig.BindAdminOnly(Section, "MarketLeast", 0.4f,
            "The least share of the full price a post pays, however much it has been sold.", new AcceptableValueRange<float>(0.05f, 1f));
        MarketRecoverPerDay = WhiteHiltConfig.BindAdminOnly(Section, "MarketRecoverPerDay", 1.5f,
            "Stacks of a good a post sells on in a game day, so its price comes back.", new AcceptableValueRange<float>(0f, 20f));
        StormDemand = WhiteHiltConfig.BindAdminOnly(Section, "StormDemand", 0.25f,
            "How much more the posts pay while a great storm rages and for a game day after it (0.25 = 25%).", new AcceptableValueRange<float>(0f, 2f));
        Visits = WhiteHiltConfig.BindAdminOnly(Section, "Visits", true,
            "Now and then a trading ship moors beside a Jetty Head a player built, while they are at home, and stays a game day with a few goods of a land the world has opened. Trade at its helm.");
        VisitChance = WhiteHiltConfig.BindAdminOnly(Section, "VisitChance", 25f,
            "Chance in percent per game day, while the player is at home by their jetty, that a merchant comes.", new AcceptableValueRange<float>(0f, 100f));
        VisitMinDays = WhiteHiltConfig.BindAdminOnly(Section, "VisitMinDays", 3,
            "Game days at least between two visits to a player.", new AcceptableValueRange<int>(0, 100));
        VisitGoods = WhiteHiltConfig.BindAdminOnly(Section, "VisitGoods", 8,
            "How many goods a visiting merchant carries.", new AcceptableValueRange<int>(1, 30));
        VisitMarkup = WhiteHiltConfig.BindAdminOnly(Section, "VisitMarkup", 1.25f,
            "A visiting merchant's prices as a share of the posts' (1.25 = 25% dearer, for coming to the door).", new AcceptableValueRange<float>(0.5f, 3f));
        ChartPrice = WhiteHiltConfig.BindAdminOnly(Section, "ChartPrice", 150,
            "Coins a Trading Post Chart costs at Hildir. Needs a restart.", new AcceptableValueRange<int>(0, 5000));
        MineChartPrice = WhiteHiltConfig.BindAdminOnly(Section, "MineChartPrice", 250,
            "Coins a Mine Chart costs at the Mistlands trading posts. It marks the nearest infested mine the reader has not found. 0: not sold.",
            new AcceptableValueRange<int>(0, 5000));
        GarbWorth = WhiteHiltConfig.BindAdminOnly(Section, "GarbWorth", 120f,
            "Coins a piece of a biome's traders' garb costs per coin the biome's plain material is worth (Meadows 1, Deep North 10).",
            new AcceptableValueRange<float>(1f, 2000f));
        GarbTradeBonus = WhiteHiltConfig.BindAdminOnly(Section, "GarbTradeBonus", 3f,
            "Percent better prices at a biome's trading posts for each piece of its traders' garb worn, buying and selling.",
            new AcceptableValueRange<float>(0f, 20f));
        GarbSetTradeBonus = WhiteHiltConfig.BindAdminOnly(Section, "GarbSetTradeBonus", 3f,
            "Percent better prices more at a biome's trading posts for the whole of its traders' garb worn.",
            new AcceptableValueRange<float>(0f, 20f));
        TerrorChance = WhiteHiltConfig.BindAdminOnly(Section, "TerrorChance", 15,
            "Percent chance, once a game day, that a named monster plagues a trading post when a player comes there. The trader hands out a bounty " +
            "notice and pays for the monster's head. 0: no plagues.", new AcceptableValueRange<int>(0, 100));
        TerrorBounty = WhiteHiltConfig.BindAdminOnly(Section, "TerrorBounty", 800f,
            "Coins paid for a plague's head per coin the biome's plain material is worth (Meadows 1, Deep North 10).",
            new AcceptableValueRange<float>(1f, 10000f));
        HunterChance = WhiteHiltConfig.BindAdminOnly(Section, "HunterChance", 20,
            "Percent chance that a hunter who sells the biome's trophies sits at a trading post when a player comes there. The answer holds " +
            "for a game day. 0: no hunter.", new AcceptableValueRange<int>(0, 100));
        TrophyWorth = WhiteHiltConfig.BindAdminOnly(Section, "TrophyWorth", 15f,
            "A trophy at the hunter costs what a plain material of its biome is worth times this (three times as much for the great beasts' " +
            "trophies).", new AcceptableValueRange<float>(1f, 1000f));
    }
}
