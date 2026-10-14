using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.TradingPosts;

/// <summary>
/// Config entries for the trading posts. All are admin-only and synced from the server.
/// </summary>
public static class TradingPostSettings
{
    private const string Section = "TradingPosts";

    /// <summary>Whether trading posts are made. Posts already in the world stay either way.</summary>
    public static ConfigEntry<bool> Enabled { get; private set; }

    /// <summary>How many posts each biome that meets the sea gets.</summary>
    public static ConfigEntry<int> PerBiome { get; private set; }

    /// <summary>Least distance between two posts, in metres.</summary>
    public static ConfigEntry<float> MinDistance { get; private set; }

    /// <summary>Least distance from a post to anything a player has built, in metres.</summary>
    public static ConfigEntry<float> BuildingDistance { get; private set; }

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
    }
}
