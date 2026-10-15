using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.TradingPosts;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.SeaBattles.Escorts;

/// <summary>
/// A merchant who comes to the player's own jetty: now and then, while the player is at home by a Jetty Head they built,
/// a trading ship moors beside it for a game day with a few goods of a land the world has opened, at a little over the
/// posts' prices. The helm is its counter. Each machine looks after its own player's jetty; the day of the last roll and
/// the last visit are kept with the player.
/// </summary>
public class MerchantVisit : MonoBehaviour
{
    private const float CheckInterval = 20f;
    private const float HomeRange = 30f;
    private const float OtherMerchantRange = 300f;
    private const string JettyHead = "piece_whitehilt_bryggehode";
    private const string RolledKey = "whitehilt_merchant_rolled";
    private const string VisitedKey = "whitehilt_merchant_visited";
    private const string StoreName = "merchant_store";

    private static readonly List<Piece> nearby = new();

    private float nextCheck;

    /// <summary>
    /// Registers the English texts. Call from the plugin's Awake.
    /// </summary>
    public static void RegisterTranslations()
    {
        Translations.AddEnglish("msg_whitehilt_merchant_arrived", "A merchant ship has moored at your jetty. It stays for a day; trade at its helm.");
        Translations.AddEnglish("whitehilt_merchant_name", "Travelling Merchant");
        Translations.AddEnglish("whitehilt_merchant_trade", "Trade with the merchant");
    }

    /// <summary>
    /// Opens the visiting merchant's store at its helm, with its goods chosen for this ship.
    /// </summary>
    /// <param name="trade">The visiting trading ship.</param>
    public static void OpenStore(TradeShip trade)
    {
        if (trade == null || StoreGui.instance == null || ObjectDB.instance == null)
        {
            return;
        }

        Trader trader = StoreTrader(trade);
        trader.m_items = Goods(trade);
        StoreGui.instance.Show(trader);
    }

    // The store's trader: a switched-off Trader on a child at the helm, so the store window has what it needs without
    // the trader's own looking about, for which the ship has no body; its lines go to the skipper (VisitingMerchant).
    private static Trader StoreTrader(TradeShip trade)
    {
        Transform existing = trade.transform.Find(StoreName);
        if (existing != null && existing.GetComponent<Trader>() is Trader found)
        {
            return found;
        }

        ShipControlls helm = trade.GetComponentInChildren<ShipControlls>(true);
        GameObject holder = new(StoreName);
        holder.transform.SetParent(trade.transform, false);
        holder.transform.position = helm != null ? helm.transform.position : trade.transform.position;
        Trader trader = holder.AddComponent<Trader>();
        trader.enabled = false;
        trader.m_name = "$whitehilt_merchant_name";
        trader.m_randomBuy = new List<string> { "$whitehilt_trader_buy1", "$whitehilt_trader_buy2" };
        trader.m_randomSell = new List<string> { "$whitehilt_trader_sell1", "$whitehilt_trader_sell2" };
        trader.m_randomStartTrade = new List<string> { "$whitehilt_trader_start1", "$whitehilt_trader_start2" };
        trader.m_randomTalk = new List<string> { "$whitehilt_trader_talk1" };
        trader.m_randomGreets = new List<string> { "$whitehilt_trader_greet1" };
        trader.m_randomGoodbye = new List<string> { "$whitehilt_trader_bye1" };
        holder.AddComponent<VisitingMerchant>();
        return trader;
    }

    // A few of the post goods of the merchant's land, the same few for everyone who trades with this ship, a little dearer
    // than at a post for bringing them to the door.
    private static List<Trader.TradeItem> Goods(TradeShip trade)
    {
        ZDO zdo = trade.GetComponent<ZNetView>().GetZDO();
        int biome = zdo.GetInt(TradeKeys.VisitBiome);
        System.Random random = new(zdo.m_uid.GetHashCode());
        List<Trader.TradeItem> items = TradingPostStore.BuildItems(biome)
            .Where(item => item.m_prefab != null && item.m_price > 0)
            .OrderBy(_ => random.Next())
            .Take(Mathf.Max(1, TradingPostSettings.VisitGoods.Value))
            .ToList();
        foreach (Trader.TradeItem item in items)
        {
            item.m_price = Mathf.Max(1, Mathf.CeilToInt(item.m_price * TradingPostSettings.VisitMarkup.Value));
        }

        return items;
    }

    // Now and then, at home by a jetty the player built, rolls once a game day for a visit.
    private void Update()
    {
        if (Time.time < nextCheck)
        {
            return;
        }

        nextCheck = Time.time + CheckInterval;
        Player player = Player.m_localPlayer;
        if (player == null || player.IsDead() || EnvMan.instance == null || ZNet.instance == null || TradingPostSettings.Visits == null
            || !TradingPostSettings.Visits.Value || Storms.StormState.Visible)
        {
            return;
        }

        int day = EnvMan.instance.GetDay();
        if (Read(player, RolledKey) == day)
        {
            return;
        }

        Piece jetty = OwnJetty(player);
        if (jetty == null)
        {
            return;
        }

        Write(player, RolledKey, day);
        if (day - Read(player, VisitedKey) < TradingPostSettings.VisitMinDays.Value
            || Random.Range(0f, 100f) >= TradingPostSettings.VisitChance.Value || MerchantNear(jetty.transform.position))
        {
            return;
        }

        TradeShip ship = TradeShip.RaiseVisit(jetty.transform, player.GetPlayerID(), PickBiome(), EnvMan.instance.m_dayLengthSec);
        if (ship != null)
        {
            Write(player, VisitedKey, day);
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_merchant_arrived");
        }
    }

    // A Jetty Head close by that this player built.
    private static Piece OwnJetty(Player player)
    {
        nearby.Clear();
        Piece.GetAllPiecesInRadius(player.transform.position, HomeRange, nearby);
        long id = player.GetPlayerID();
        return nearby.FirstOrDefault(piece => piece != null && piece.GetCreator() == id && Utils.GetPrefabName(piece.gameObject) == JettyHead);
    }

    private static bool MerchantNear(Vector3 position)
    {
        return Ship.Instances.OfType<Ship>().Any(other => other != null && TradeShip.Is(other)
            && Vector3.Distance(other.transform.position, position) <= OtherMerchantRange);
    }

    // One of the lands whose goods the world has opened, at random.
    private static int PickBiome()
    {
        List<int> open = Enumerable.Range(0, TradingPostGoods.Biomes.Length)
            .Where(biome =>
            {
                string key = TradingPostGoods.OpenKey(biome);
                return string.IsNullOrEmpty(key) || (ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(key));
            })
            .ToList();
        return open.Count > 0 ? open[Random.Range(0, open.Count)] : 0;
    }

    private static int Read(Player player, string key)
    {
        return player.m_customData.TryGetValue(key, out string text) && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int day)
            ? day
            : int.MinValue / 2;
    }

    private static void Write(Player player, string key, int day)
    {
        player.m_customData[key] = day.ToString(CultureInfo.InvariantCulture);
    }
}

/// <summary>
/// Marks the visiting merchant's store, so the store window's lines are shown over the ship's skipper instead of by the
/// switched-off trader, which has no body to speak from.
/// </summary>
public class VisitingMerchant : MonoBehaviour
{
    private const float BubbleHeight = 2.2f;
    private const float BubbleTime = 4f;

    /// <summary>
    /// Shows a line of the store window.
    /// </summary>
    /// <param name="text">The line, with $tokens.</param>
    public void Say(string text)
    {
        if (Chat.instance != null && !string.IsNullOrEmpty(text))
        {
            Chat.instance.SetNpcText(gameObject, Vector3.up * BubbleHeight, 20f, BubbleTime, string.Empty, text, false);
        }
    }
}
