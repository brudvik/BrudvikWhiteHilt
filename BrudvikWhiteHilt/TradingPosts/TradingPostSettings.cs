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

    /// <summary>
    /// Binds the entries. Call once from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        Enabled = WhiteHiltConfig.BindAdminOnly(Section, "Enabled", true,
            "Make trading posts by the sea. Off: no new ones are made; the posts already in the world stay.");
    }
}
