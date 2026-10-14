using BrudvikWhiteHilt.TradingPosts;
using HarmonyLib;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.TradingPosts;

/// <summary>
/// Hooks the trading posts onto the game and keeps their pieces whole: a piece of a post takes no damage, needs no
/// support, does not wear in rain or ash, does not burn and cannot be removed with the hammer. A post's trader (and its
/// hunter, who sells trophies and buys nothing) runs on its own component instead of Haldor's behaviour.
/// </summary>
[HarmonyPatch]
public static class TradingPostPatches
{
    [HarmonyPatch(typeof(Game), nameof(Game.Start))]
    [HarmonyPostfix]
    private static void GameStartPostfix(Game __instance)
    {
        __instance.gameObject.AddComponent<TradingPostService>();
        __instance.gameObject.AddComponent<TradingPostPlacer>();
    }

    // The flags are not saved, so they are set again every time a post's piece comes to life.
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Awake))]
    [HarmonyPostfix]
    private static void AwakePostfix(WearNTear __instance)
    {
        if (!IsPostPart(__instance))
        {
            return;
        }

        __instance.m_noSupportWear = true;
        __instance.m_noRoofWear = true;
        __instance.m_ashDamageImmune = true;
        __instance.m_burnable = false;
    }

    // Every blow, fire and fall reaches a piece through its owner's damage handler; a post's pieces skip it.
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.RPC_Damage))]
    [HarmonyPrefix]
    private static bool DamagePrefix(WearNTear __instance)
    {
        return !IsPostPart(__instance);
    }

    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.CanBeRemoved))]
    [HarmonyPostfix]
    private static void WearCanBeRemovedPostfix(WearNTear __instance, ref bool __result)
    {
        if (__result && IsPostPart(__instance))
        {
            __result = false;
        }
    }

    [HarmonyPatch(typeof(Piece), nameof(Piece.CanBeRemoved))]
    [HarmonyPostfix]
    private static void PieceCanBeRemovedPostfix(Piece __instance, ref bool __result)
    {
        if (__result && __instance.m_nview != null && TradingPostKeys.IsPostPart(__instance.m_nview.GetZDO()))
        {
            __result = false;
        }
    }

    // A post's trader turns, greets and talks by its own component; Haldor's would drive animator parameters the player
    // body does not have.
    [HarmonyPatch(typeof(Trader), nameof(Trader.Update))]
    [HarmonyPrefix]
    private static bool TraderUpdatePrefix(Trader __instance)
    {
        return __instance.GetComponent<TradingPostTrader>() == null;
    }

    [HarmonyPatch(typeof(Trader), nameof(Trader.RandomTalk))]
    [HarmonyPrefix]
    private static bool TraderRandomTalkPrefix(Trader __instance)
    {
        return __instance.GetComponent<TradingPostTrader>() == null;
    }

    // The store window's lines (opening it, buying, selling) go through the trader's own Say, with an emote for
    // Haldor's animation.
    [HarmonyPatch(typeof(Trader), nameof(Trader.Say), typeof(string), typeof(string))]
    [HarmonyPrefix]
    private static bool TraderSayPrefix(Trader __instance, string text, string trigger)
    {
        TradingPostTrader trader = __instance.GetComponent<TradingPostTrader>();
        if (trader == null)
        {
            return true;
        }

        trader.Say(text, trigger);
        return false;
    }

    // At a post the store window buys the post's goods and fish, not the valuables Haldor buys.
    [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.GetSellableItem))]
    [HarmonyPostfix]
    private static void SellablePostfix(StoreGui __instance, ref ItemDrop.ItemData __result)
    {
        TradingPostTrader trader = __instance.m_trader != null ? __instance.m_trader.GetComponent<TradingPostTrader>() : null;
        if (trader != null && trader.IsHunter)
        {
            // The hunter only sells.
            __result = null;
        }
        else if (trader != null && Player.m_localPlayer != null)
        {
            __result = TradingPostSellPicker.Current(trader.Biome, Player.m_localPlayer.GetInventory());
        }
    }

    // Sells the stack chosen with the step button at the post's price, the way the game sells a valuable.
    [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.SellItem))]
    [HarmonyPrefix]
    private static bool SellPrefix(StoreGui __instance)
    {
        TradingPostTrader trader = __instance.m_trader != null ? __instance.m_trader.GetComponent<TradingPostTrader>() : null;
        Player player = Player.m_localPlayer;
        if (trader == null || player == null)
        {
            return true;
        }

        if (trader.IsHunter)
        {
            return false;
        }

        Inventory inventory = player.GetInventory();
        ItemDrop.ItemData item = TradingPostSellPicker.Current(trader.Biome, inventory);
        int coins = item != null ? TradingPostStore.PayFor(trader.Biome, item) : 0;
        if (coins <= 0)
        {
            return false;
        }

        ItemDrop coin = __instance.m_coinPrefab;
        bool cheated = inventory.ItemCheated(item.m_shared.m_name);
        string sold = item.m_stack <= 1 ? item.m_shared.m_name : $"{item.m_stack}x{item.m_shared.m_name}";
        Sprite icon = item.m_shared.m_icons != null && item.m_shared.m_icons.Length > 0 ? item.m_shared.m_icons[0] : null;
        inventory.RemoveItem(item);
        inventory.AddItem(coin.gameObject.name, coins, coin.m_itemData.m_quality, coin.m_itemData.m_variant, 0L, string.Empty, cheated);
        __instance.m_sellEffects.Create(__instance.transform.position, Quaternion.identity);
        player.Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize("$msg_sold", sold, coins.ToString()), 0, icon);
        __instance.m_trader.OnSold();
        TradingSkill.Raise(player, coins);
        __instance.FillList();
        return false;
    }

    // Reading a Trading Post Chart asks the server for the biome's posts instead of the game's own use of the item.
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UseItem))]
    [HarmonyPrefix]
    private static bool UseChartPrefix(Humanoid __instance, ItemDrop.ItemData item)
    {
        int biome = TradingPostCharts.BiomeOf(item);
        if (biome < 0 || __instance is not Player player || player != Player.m_localPlayer)
        {
            return true;
        }

        TradingPostService.RequestChart(biome);
        return false;
    }

    // At a post the sell button says what it sells and a button beside it steps to the next stack; elsewhere the window
    // is the game's own.
    [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.Update))]
    [HarmonyPostfix]
    private static void StoreUpdatePostfix(StoreGui __instance)
    {
        if (__instance.m_rootPanel != null && __instance.m_rootPanel.activeSelf)
        {
            TradingPostTrader trader = __instance.m_trader != null ? __instance.m_trader.GetComponent<TradingPostTrader>() : null;
            TradingPostSellPicker.Update(__instance, trader != null && !trader.IsHunter ? trader : null);
        }
    }

    [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.Show))]
    [HarmonyPostfix]
    private static void StoreShowPostfix()
    {
        TradingPostSellPicker.Reset();
    }

    // A post's goods are made again for the player who opens its store: their prices and the gear their Trading has
    // opened. Visiting a post also names its pin on the player's map after what it is known for.
    [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.Show))]
    [HarmonyPrefix]
    private static void StoreShowPrefix(Trader trader)
    {
        TradingPostTrader post = trader != null ? trader.GetComponent<TradingPostTrader>() : null;
        if (post == null || Player.m_localPlayer == null)
        {
            return;
        }

        if (post.IsHunter)
        {
            trader.m_items = TradingPostTrophies.BuildItems(post.Biome, Player.m_localPlayer);
            return;
        }

        trader.m_items = TradingPostStore.BuildItems(post.Biome, post.PostId, Player.m_localPlayer);
        GearKind? kind = TradingPostGear.Specialty(post.Biome, post.PostId);
        if (kind != null)
        {
            TradingPostCharts.MarkVisited(trader.transform.position, kind.Value);
        }
    }

    [HarmonyPatch(typeof(StoreGui), "FillList")]
    [HarmonyPostfix]
    private static void FillListPostfix(StoreGui __instance)
    {
        if (__instance.m_trader != null && __instance.m_trader.GetComponent<TradingPostTrader>() != null)
        {
            TradingPostStore.ShowLocked(__instance.m_trader.GetAvailableItems(), __instance.m_itemList);
        }
    }

    // Gear the player's Trading has not opened cannot be bought, whatever the purse holds.
    [HarmonyPatch(typeof(StoreGui), "CanAfford")]
    [HarmonyPostfix]
    private static void CanAffordPostfix(Trader.TradeItem item, ref bool __result)
    {
        if (__result && TradingPostStore.LockedAt(item) > 0)
        {
            __result = false;
        }
    }

    [HarmonyPatch(typeof(Trader), nameof(Trader.OnBought))]
    [HarmonyPostfix]
    private static void BoughtPostfix(Trader __instance, Trader.TradeItem item)
    {
        if (item != null && __instance.GetComponent<TradingPostTrader>() != null)
        {
            TradingSkill.Raise(Player.m_localPlayer, item.m_price);
        }
    }

    // A post's trader says what the post is known for.
    [HarmonyPatch(typeof(Trader), nameof(Trader.GetHoverText))]
    [HarmonyPostfix]
    private static void HoverPostfix(Trader __instance, ref string __result)
    {
        TradingPostTrader post = __instance.GetComponent<TradingPostTrader>();
        GearKind? kind = post != null && !post.IsHunter ? TradingPostGear.Specialty(post.Biome, post.PostId) : null;
        if (kind != null)
        {
            __result += "\n" + Localization.instance.Localize("$whitehilt_trader_known", TradingPostGear.KindName(kind.Value));
        }
    }

    private static bool IsPostPart(WearNTear piece)
    {
        return piece.m_nview != null && TradingPostKeys.IsPostPart(piece.m_nview.GetZDO());
    }
}
