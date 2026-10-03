using BrudvikWhiteHilt.Items.Foraging.Peat;
using HarmonyLib;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.Foraging;

/// <summary>
/// Lets peat from the hotbar feed a fire that burns wood, worth more than one piece of wood.
/// </summary>
[HarmonyPatch(typeof(Fireplace), nameof(Fireplace.UseItem))]
public static class PeatFuelPatch
{
    private const string PeatName = "$item_whitehiltpeat";
    private const string WoodName = "$item_wood";

    [HarmonyPrefix]
    private static bool Prefix(Fireplace __instance, Humanoid user, ItemDrop.ItemData item, ref bool __result)
    {
        if (item?.m_shared.m_name != PeatName || !__instance.m_canRefill || __instance.m_infiniteFuel
            || __instance.m_fuelItem == null || __instance.m_fuelItem.m_itemData.m_shared.m_name != WoodName
            || __instance.m_nview == null || !__instance.m_nview.IsValid())
        {
            return true;
        }

        __result = true;
        float fuel = __instance.m_nview.GetZDO().GetFloat(ZDOVars.s_fuel);
        if (Mathf.CeilToInt(fuel) >= __instance.m_maxFuel)
        {
            user.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_cantaddmore", item.m_shared.m_name));
            return false;
        }

        user.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_fireadding", item.m_shared.m_name));
        user.GetInventory().RemoveItem(item, 1);
        __instance.AddFuel(Mathf.Min(Peat.FuelValue, __instance.m_maxFuel - fuel));
        return false;
    }
}
