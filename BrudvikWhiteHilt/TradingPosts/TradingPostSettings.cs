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

    /// <summary>Asking prices that replace the built-in ones: Prefab:Price, comma separated.</summary>
    public static ConfigEntry<string> Prices { get; private set; }

    /// <summary>Whether the posts pay for an item that is unlimited in the world's chests.</summary>
    public static ConfigEntry<bool> BuyUnlimitedGoods { get; private set; }

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
    /// Binds the entries. Call once from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        Enabled = WhiteHiltConfig.BindAdminOnly(Section, "Enabled", true,
            "Make trading posts by the sea. Off: no new ones are made; the posts already in the world stay.");
        PerBiome = WhiteHiltConfig.BindAdminOnly(Section, "PerBiome", 4,
            "How many trading posts each biome that meets the sea gets, where its coast has room for them. Placed once per world; " +
            "a higher number takes effect with whitehilt_tradepost place.", new AcceptableValueRange<int>(0, 10));
        MinDistance = WhiteHiltConfig.BindAdminOnly(Section, "MinDistance", 1000f,
            "Least distance between two trading posts, in metres.", new AcceptableValueRange<float>(200f, 5000f));
        BuildingDistance = WhiteHiltConfig.BindAdminOnly(Section, "BuildingDistance", 60f,
            "Least distance from a new trading post to anything a player has built, in metres.", new AcceptableValueRange<float>(0f, 300f));
        BuyShare = WhiteHiltConfig.BindAdminOnly(Section, "BuyShare", 0.25f,
            "Share of its own asking price a trading post pays for the goods it buys (0.25 = a quarter).", new AcceptableValueRange<float>(0f, 1f));
        Prices = WhiteHiltConfig.BindAdminOnly(Section, "Prices", string.Empty,
            "Asking prices that replace the built-in ones, in coins per piece: Prefab:Price, comma separated, e.g. \"Wood:2, CopperOre:6\". " +
            "What a post pays follows from them by BuyShare.");
        BuyUnlimitedGoods = WhiteHiltConfig.BindAdminOnly(Section, "BuyUnlimitedGoods", true,
            "The trading posts pay for goods even when they are unlimited in the world's chests. Off: they pay nothing for those.");
    }
}
