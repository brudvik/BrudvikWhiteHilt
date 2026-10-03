using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Treasure;
using HarmonyLib;
using System;
using System.Linq;

namespace BrudvikWhiteHilt.Patches.Treasure;

/// <summary>
/// Hooks the treasure service onto the game, marks a map when it is bought from Hildir, unrolls a map when it is used,
/// and tells in its tooltip whether its treasure still waits.
/// </summary>
[HarmonyPatch]
public static class TreasurePatches
{
    [HarmonyPatch(typeof(Game), nameof(Game.Start))]
    [HarmonyPostfix]
    private static void GameStartPostfix(Game __instance)
    {
        __instance.gameObject.AddComponent<TreasureService>();
    }

    [HarmonyPatch(typeof(Trader), nameof(Trader.OnBought))]
    [HarmonyPostfix]
    private static void BoughtPostfix(Trader.TradeItem item)
    {
        Player player = Player.m_localPlayer;
        string bought = item?.m_prefab != null ? item.m_prefab.name : null;
        if (player == null || (bought != TreasureMapItem.PrefabName && bought != TreasureMapItem.HuntPrefabName))
        {
            return;
        }

        ItemDrop.ItemData map = player.GetInventory().GetAllItems()
            .LastOrDefault(candidate => candidate.m_dropPrefab != null && candidate.m_dropPrefab.name == bought && TreasureMapItem.GetId(candidate) == null);
        if (map != null)
        {
            TreasureMapItem.Assign(map, item.m_price);
            if (bought == TreasureMapItem.HuntPrefabName)
            {
                TreasureMapItem.SetHunt(map, 1, Math.Max(1, TreasureSettings.HuntSteps.Value));
            }

            TreasureService.RequestSite(player, map);
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UseItem))]
    [HarmonyPrefix]
    private static bool UseItemPrefix(Humanoid __instance, ItemDrop.ItemData item)
    {
        if (!TreasureMapItem.IsMap(item) || __instance is not Player player || player != Player.m_localPlayer)
        {
            return true;
        }

        if (TreasureMapItem.GetSite(item) == null)
        {
            TreasureService.RequestSite(player, item);
            return false;
        }

        if (InventoryGui.IsVisible())
        {
            InventoryGui.instance.Hide();
        }

        TreasureMapPanel.Toggle(item);
        return false;
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(bool))]
    [HarmonyPostfix]
    private static void TooltipPostfix(ItemDrop.ItemData item, ref string __result)
    {
        if (!TreasureMapItem.IsMap(item))
        {
            return;
        }

        string key = TreasureMapItem.IsPlundered(item) ? "whitehilt_treasure_tip_plundered"
            : TreasureMapItem.GetSite(item) == null ? "whitehilt_treasure_tip_unmarked"
            : "whitehilt_treasure_tip_waiting";
        __result += "\n\n<color=orange>" + Translations.Word(key) + "</color>";
        int steps = TreasureMapItem.GetSteps(item);
        if (steps > 1)
        {
            __result += "\n" + string.Format(Translations.Word("whitehilt_treasure_tip_step"), TreasureMapItem.GetStep(item), steps);
        }
    }
}
