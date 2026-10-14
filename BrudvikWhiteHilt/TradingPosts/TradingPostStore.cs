using BrudvikWhiteHilt.Crafting;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.TradingPosts;

/// <summary>
/// A trading post's store in the game's store window: the goods its trader sells (<see cref="TradingPostGoods"/>) and
/// what the post pays for the goods a player sells there. The window itself is the game's; the patches in
/// <c>TradingPostPatches</c> let it buy the post's goods instead of the valuables Haldor buys.
/// </summary>
public static class TradingPostStore
{
    /// <summary>
    /// The goods a biome's trader sells, each behind the world key that opens the biome.
    /// </summary>
    /// <param name="biome">The biome index (<see cref="TradingPostGoods.Biomes"/>).</param>
    /// <returns>The store's list.</returns>
    public static List<Trader.TradeItem> BuildItems(int biome)
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
                m_price = Mathf.Max(1, Mathf.RoundToInt(TradingPostGoods.AskingPrice(prefab, prices) * stack)),
                m_requiredGlobalKey = TradingPostGoods.OpenKey(biome),
                // The store window plays these after every purchase without checking for none.
                m_buyPlayerEffects = new EffectList()
            });
        }

        return items;
    }

    /// <summary>
    /// What a post pays for a whole stack a player sells there: nothing for goods it does not buy, and, with
    /// <see cref="TradingPostSettings.BuyUnlimitedGoods"/> off, nothing for goods that are unlimited in the world.
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

        float each = TradingPostGoods.BuyPrice(biome, prefab, item.m_quality, TradingPostSettings.BuyShare.Value, TradingPostSettings.ConfiguredPrices);
        return Mathf.FloorToInt(each * item.m_stack);
    }

    /// <summary>
    /// The first stack in the player's inventory a post buys, in inventory order, as the game's window sells the first
    /// valuable it finds.
    /// </summary>
    /// <param name="biome">The post's biome index.</param>
    /// <param name="inventory">The player's inventory.</param>
    /// <returns>The stack, or null.</returns>
    public static ItemDrop.ItemData FirstSellable(int biome, Inventory inventory)
    {
        foreach (ItemDrop.ItemData item in inventory.GetAllItemsInGridOrder())
        {
            if (!item.m_equipped && PayFor(biome, item) > 0)
            {
                return item;
            }
        }

        return null;
    }
}
