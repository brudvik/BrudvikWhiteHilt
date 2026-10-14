using BrudvikWhiteHilt.Crafting;
using BrudvikWhiteHilt.Helpers;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.TradingPosts;

/// <summary>
/// A trading post's store in the game's store window: the goods its trader sells (<see cref="TradingPostGoods"/>) and
/// what the post pays for the goods a player sells there, both by the player's Trading (<see cref="TradingSkill"/>).
/// The window itself is the game's; the patches in <c>TradingPostPatches</c> let it buy the post's goods instead of the
/// valuables Haldor buys, and show the gear the player's Trading has not yet opened greyed out.
/// </summary>
public static class TradingPostStore
{
    private const string LockedColour = "#8A8A8A";

    // The gear in the store last made that the player's Trading has not opened, with the level each needs.
    private static readonly Dictionary<Trader.TradeItem, int> locked = new();

    /// <summary>
    /// Registers the store's texts. Call from the plugin's Awake.
    /// </summary>
    public static void RegisterTranslations()
    {
        Translations.AddEnglish("whitehilt_trading_locked", "Trading $1");
        Translations.AddEnglish("whitehilt_trader_known", "Known for $1");
    }

    /// <summary>
    /// Greys out the store window's entries the player's Trading has not opened, and says the level each needs.
    /// </summary>
    /// <param name="items">The entries the window shows, in its order.</param>
    /// <param name="elements">The window's list elements, in the same order.</param>
    public static void ShowLocked(List<Trader.TradeItem> items, List<GameObject> elements)
    {
        for (int i = 0; i < items.Count && i < elements.Count; i++)
        {
            int level = LockedAt(items[i]);
            if (level <= 0 || elements[i] == null)
            {
                continue;
            }

            Transform element = elements[i].transform;
            TMP_Text name = element.Find("name")?.GetComponent<TMP_Text>();
            if (name != null)
            {
                name.text += $" <color={LockedColour}>({Localization.instance.Localize("$whitehilt_trading_locked", level.ToString())})</color>";
                name.color = Color.grey;
            }

            TMP_Text price = Utils.FindChild(element, "price")?.GetComponent<TMP_Text>();
            if (price != null)
            {
                price.color = Color.grey;
            }

            Image icon = element.Find("icon")?.GetComponent<Image>();
            if (icon != null)
            {
                // The colour the game gives what the player cannot afford.
                icon.color = new Color(1f, 0f, 1f, 0f);
            }
        }
    }

    /// <summary>
    /// The level of Trading a piece in the store needs, or 0 when it is open to the player.
    /// </summary>
    /// <param name="item">The store's entry.</param>
    /// <returns>The level.</returns>
    public static int LockedAt(Trader.TradeItem item)
    {
        return item != null && locked.TryGetValue(item, out int level) ? level : 0;
    }

    /// <summary>
    /// The goods a biome's trader sells, each behind the world key that opens the biome, at a player's prices; and the
    /// post's own gear, the pieces the player's Trading has not opened marked locked.
    /// </summary>
    /// <param name="biome">The biome index (<see cref="TradingPostGoods.Biomes"/>).</param>
    /// <param name="postId">The post's id, or null for no gear.</param>
    /// <param name="player">The player who trades, or null for the prices at level 0.</param>
    /// <returns>The store's list.</returns>
    public static List<Trader.TradeItem> BuildItems(int biome, string postId = null, Player player = null)
    {
        List<Trader.TradeItem> items = new();
        if (ObjectDB.instance == null)
        {
            return items;
        }

        IReadOnlyDictionary<string, float> prices = TradingPostSettings.ConfiguredPrices;
        foreach (string prefab in TradingPostGoods.Materials(biome))
        {
            ItemDrop drop = ObjectDB.instance.GetItemPrefab(prefab)?.GetComponent<ItemDrop>();
            if (drop == null)
            {
                continue;
            }

            int stack = Mathf.Min(TradingPostGoods.StackSold(prefab), drop.m_itemData.m_shared.m_maxStackSize);
            items.Add(new Trader.TradeItem
            {
                m_prefab = drop,
                m_stack = stack,
                m_tooltip = string.Empty,
                m_price = TradingSkill.Price(Mathf.Max(1, Mathf.RoundToInt(TradingPostGoods.AskingPrice(prefab, prices) * stack)), player),
                m_requiredGlobalKey = TradingPostGoods.OpenKey(biome),
                // The store window plays these after every purchase without checking for none.
                m_buyPlayerEffects = new EffectList()
            });
        }

        AddGear(items, biome, postId, player);

        // And the Book of the Hall, the knowledge of furnishing a great hall.
        Trader.TradeItem book = Comfort.ComfortService.TradeItem();
        if (book != null)
        {
            items.Add(book);
        }

        // Every post's trader sells the Escort Contract, behind the key of the sea battles, as Haldor did.
        ItemDrop contract = ObjectDB.instance.GetItemPrefab(SeaBattles.Escorts.EscortContract.PrefabName)?.GetComponent<ItemDrop>();
        if (contract != null && SeaBattles.Escorts.EscortSettings.Enabled.Value && SeaBattles.SeaBattleSettings.Enabled.Value)
        {
            items.Add(new Trader.TradeItem
            {
                m_prefab = contract,
                m_stack = 1,
                m_tooltip = string.Empty,
                m_price = SeaBattles.Escorts.EscortSettings.ContractPrice.Value,
                m_requiredGlobalKey = SeaBattles.SeaBattleSettings.RequiredKey.Value?.Trim() ?? string.Empty,
                m_buyPlayerEffects = new EffectList()
            });
        }

        return items;
    }

    /// <summary>
    /// What a post pays the local player for a whole stack they sell there: nothing for goods it does not buy, and,
    /// with <see cref="TradingPostSettings.BuyUnlimitedGoods"/> off, nothing for goods that are unlimited in the world.
    /// </summary>
    /// <param name="biome">The post's biome index.</param>
    /// <param name="item">The stack.</param>
    /// <returns>Coins.</returns>
    public static int PayFor(int biome, ItemDrop.ItemData item)
    {
        if (item?.m_dropPrefab == null)
        {
            return 0;
        }

        string prefab = item.m_dropPrefab.name;
        if (!TradingPostSettings.BuyUnlimitedGoods.Value && NearbyContainers.Unlimited?.GetUnlimitedChest(prefab, item.m_shared) != null)
        {
            return 0;
        }

        float each = TradingPostGoods.BuyPrice(biome, prefab, item.m_quality, TradingSkill.BuyShare(Player.m_localPlayer), TradingPostSettings.ConfiguredPrices);
        return Mathf.FloorToInt(each * item.m_stack);
    }

    // The post's gear at quality 1, cheapest first, behind the biome's key; what the player's Trading has not opened is
    // listed as locked.
    private static void AddGear(List<Trader.TradeItem> items, int biome, string postId, Player player)
    {
        if (postId == null)
        {
            return;
        }

        locked.Clear();
        List<GearEntry> gear = TradingPostGear.ForPost(biome, postId);
        float level = TradingSkill.Level(player);
        IReadOnlyList<int> levels = TradingPostSettings.Levels;
        for (int rank = 0; rank < gear.Count; rank++)
        {
            ItemDrop drop = ObjectDB.instance.GetItemPrefab(gear[rank].Prefab)?.GetComponent<ItemDrop>();
            if (drop == null)
            {
                continue;
            }

            Trader.TradeItem item = new()
            {
                m_prefab = drop,
                m_stack = Mathf.Min(gear[rank].Stack, drop.m_itemData.m_shared.m_maxStackSize),
                m_tooltip = string.Empty,
                m_price = TradingSkill.Price(gear[rank].Price, player),
                m_requiredGlobalKey = TradingPostGoods.OpenKey(biome),
                m_buyPlayerEffects = new EffectList()
            };
            int needs = TradingGear.UnlockLevel(rank, gear.Count, levels);
            if (level < needs)
            {
                locked[item] = needs;
            }

            items.Add(item);
        }
    }
}
