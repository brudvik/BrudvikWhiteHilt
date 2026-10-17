using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.Foraging;

/// <summary>
/// Lets foraged fuel (peat, tinder fungus) from the hotbar feed a fire that burns wood, worth more than one piece of wood.
/// </summary>
[HarmonyPatch(typeof(Fireplace), nameof(Fireplace.UseItem))]
public static class ForageFuelPatch
{
    private const string WoodName = "$item_wood";

    // Shared item name -> how much wood one of it is worth.
    private static readonly Dictionary<string, Func<float>> fuels = new();

    /// <summary>
    /// Lets an item feed wood fires.
    /// </summary>
    /// <param name="sharedName">Shared item name, e.g. $item_whitehiltpeat.</param>
    /// <param name="woodWorth">How much wood one of it is worth.</param>
    public static void Register(string sharedName, Func<float> woodWorth)
    {
        fuels[sharedName] = woodWorth;
    }

    // Lets a registered fuel be put in a fire that burns wood, worth more fuel than a log; anything else goes to the
    // vanilla code.
    [HarmonyPrefix]
    private static bool Prefix(Fireplace __instance, Humanoid user, ItemDrop.ItemData item, ref bool __result)
    {
        if (item == null || !fuels.TryGetValue(item.m_shared.m_name, out Func<float> woodWorth) || !__instance.m_canRefill
            || __instance.m_infiniteFuel || __instance.m_fuelItem == null || __instance.m_fuelItem.m_itemData.m_shared.m_name != WoodName
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
        __instance.AddFuel(Mathf.Min(woodWorth(), __instance.m_maxFuel - fuel));
        return false;
    }
}
