using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.TradingPosts;

/// <summary>
/// The Trading skill. It rises with every trade at a trading post and with an escort's pay; the posts pay a player
/// with more of it a larger share, sell to them for less, and open more of their gear to them.
/// </summary>
public static class TradingSkill
{
    private const string Identifier = "com.jotunn.BrudvikWhiteHilt.trading";
    private const string NameKey = "whitehilt_skill_trading";

    private static SkillConfig config;

    /// <summary>
    /// The skill type, valid after <see cref="Register"/>.
    /// </summary>
    public static Skills.SkillType Type { get; private set; }

    /// <summary>
    /// Registers the skill and its English text. Call from the plugin's Awake.
    /// </summary>
    public static void Register()
    {
        Translations.AddEnglishNameAndDescription(NameKey, "Trading",
            "Trading at the trading posts. The posts pay you more and ask less as it grows, and their traders show you more of their gear.");
        config = new SkillConfig
        {
            Identifier = Identifier,
            Name = Translations.Token(NameKey),
            Description = Translations.Token($"{NameKey}_description"),
            IncreaseStep = 1f
        };
        Type = SkillManager.Instance.AddSkill(config);
    }

    /// <summary>
    /// Gives the skill the coins' icon. Must happen before the first player spawns.
    /// </summary>
    public static void SetIconFromCoins()
    {
        Sprite icon = PrefabManager.Instance.GetPrefab("Coins")?.GetComponent<ItemDrop>()?.m_itemData.GetIcon();
        if (config != null && config.Icon == null && icon != null)
        {
            config.Icon = icon;
        }
    }

    /// <summary>
    /// A player's Trading, 0 to 100; 0 for none.
    /// </summary>
    /// <param name="player">The player, or null.</param>
    /// <returns>The level.</returns>
    public static float Level(Player player)
    {
        return player != null ? player.GetSkillLevel(Type) : 0f;
    }

    /// <summary>
    /// The share of its asking price a post pays a player.
    /// </summary>
    /// <param name="player">The player, or null for level 0.</param>
    /// <returns>The share.</returns>
    public static float BuyShare(Player player)
    {
        return TradingGear.BuyShare(Level(player), TradingPostSettings.BuyShare.Value, TradingPostSettings.MaxBuyShare.Value);
    }

    /// <summary>
    /// What a player pays for something at a post's asking price.
    /// </summary>
    /// <param name="price">The asking price.</param>
    /// <param name="player">The player, or null for level 0.</param>
    /// <returns>Coins.</returns>
    public static int Price(int price, Player player)
    {
        return TradingGear.Discounted(price, Level(player), TradingPostSettings.BuyDiscount.Value);
    }

    /// <summary>
    /// Raises the local player's Trading for coins paid or received.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="coins">The coins.</param>
    public static void Raise(Player player, int coins)
    {
        float points = TradingGear.Experience(coins, TradingPostSettings.ExperiencePerCoins.Value);
        if (player != null && player == Player.m_localPlayer && points > 0f)
        {
            player.RaiseSkill(Type, points);
        }
    }
}
