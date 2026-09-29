using BrudvikWhiteHilt.Crafting;
using HarmonyLib;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.Crafting;

/// <summary>
/// Fuels fires, smelters and ovens, feeds ore to smelters and puts raw food on cooking stations from the nearby chests
/// when the inventory has none. With the fill-all key (Shift) held, fuel and ore are filled up from the inventory first,
/// then from the chests.
/// </summary>
[HarmonyPatch]
public static class NearbyContainerStationPatches
{
    /// <summary>
    /// Adds ore to a smelter from the chests.
    /// </summary>
    /// <param name="__instance">The smelter.</param>
    /// <param name="user">The player.</param>
    /// <param name="item">The item used from the hotbar, or null on interact.</param>
    /// <param name="__result">True if ore was added.</param>
    /// <returns>False when the ore came from here.</returns>
    [HarmonyPatch(typeof(Smelter), nameof(Smelter.OnAddOre))]
    [HarmonyPrefix]
    public static bool SmelterOre(Smelter __instance, Humanoid user, ItemDrop.ItemData item, ref bool __result)
    {
        if (item != null || !IsLocalFueling(user))
        {
            return true;
        }

        bool fillAll = NearbyContainers.FillAllHeld;
        if (!fillAll && __instance.FindCookableItem(user.GetInventory()) != null)
        {
            return true;
        }

        int space = __instance.m_maxOre - __instance.GetQueueSize();
        if (space <= 0)
        {
            return true;
        }

        int added = 0;
        string addedName = null;
        foreach (Smelter.ItemConversion conversion in __instance.m_conversion)
        {
            if (conversion.m_from == null || added >= space)
            {
                continue;
            }

            string name = conversion.m_from.m_itemData.m_shared.m_name;
            int count = Gather(user, name, fillAll ? space - added : 1, fillAll);
            for (int i = 0; i < count; i++)
            {
                __instance.m_nview.InvokeRPC("RPC_AddOre", conversion.m_from.gameObject.name, false);
            }

            if (count > 0)
            {
                added += count;
                addedName ??= name;
                if (!fillAll)
                {
                    break;
                }
            }
        }

        if (added == 0)
        {
            return true;
        }

        user.Message(MessageHud.MessageType.Center, $"$msg_added {addedName} x{added}");
        __instance.m_addedOreTime = Time.time;
        if (__instance.m_addOreAnimationDuration > 0f)
        {
            __instance.SetAnimation(active: true);
        }

        __result = true;
        return false;
    }

    /// <summary>
    /// Adds fuel to a smelter or kiln from the chests.
    /// </summary>
    /// <param name="__instance">The smelter.</param>
    /// <param name="user">The player.</param>
    /// <param name="item">The item used from the hotbar, or null on interact.</param>
    /// <param name="__result">True if fuel was added.</param>
    /// <returns>False when the fuel came from here.</returns>
    [HarmonyPatch(typeof(Smelter), nameof(Smelter.OnAddFuel))]
    [HarmonyPrefix]
    public static bool SmelterFuel(Smelter __instance, Humanoid user, ItemDrop.ItemData item, ref bool __result)
    {
        if (item != null || __instance.m_fuelItem == null || !IsLocalFueling(user))
        {
            return true;
        }

        return AddFuel(__instance.m_nview, "RPC_AddFuel", __instance.m_fuelItem, Mathf.FloorToInt(__instance.m_maxFuel - __instance.GetFuel()), user, ref __result);
    }

    /// <summary>
    /// Adds fuel to a cooking station with its own fire, like the stone oven, from the chests.
    /// </summary>
    /// <param name="__instance">The cooking station.</param>
    /// <param name="user">The player.</param>
    /// <param name="item">The item used from the hotbar, or null on interact.</param>
    /// <param name="__result">True if fuel was added.</param>
    /// <returns>False when the fuel came from here.</returns>
    [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.OnAddFuelSwitch))]
    [HarmonyPrefix]
    public static bool CookingStationFuel(CookingStation __instance, Humanoid user, ItemDrop.ItemData item, ref bool __result)
    {
        if (item != null || __instance.m_fuelItem == null || !IsLocalFueling(user))
        {
            return true;
        }

        return AddFuel(__instance.m_nview, "RPC_AddFuel", __instance.m_fuelItem, Mathf.FloorToInt(__instance.m_maxFuel - __instance.GetFuel()), user, ref __result);
    }

    /// <summary>
    /// Adds wood to a fire from the chests.
    /// </summary>
    /// <param name="__instance">The fire.</param>
    /// <param name="user">The player.</param>
    /// <param name="hold">True while the key is held.</param>
    /// <param name="alt">True with the alternate key.</param>
    /// <param name="__result">True if fuel was added.</param>
    /// <returns>False when the fuel came from here.</returns>
    [HarmonyPatch(typeof(Fireplace), nameof(Fireplace.Interact))]
    [HarmonyPrefix]
    public static bool FireplaceFuel(Fireplace __instance, Humanoid user, bool hold, bool alt, ref bool __result)
    {
        if (hold || !__instance.m_canRefill || __instance.m_infiniteFuel || __instance.m_fuelItem == null || !IsLocalFueling(user)
            || !__instance.m_nview.IsValid())
        {
            return true;
        }

        float fuel = __instance.m_nview.GetZDO().GetFloat(ZDOVars.s_fuel);
        if (__instance.m_canTurnOff && !alt && fuel > 0f)
        {
            return true;
        }

        if (!__instance.m_nview.HasOwner())
        {
            __instance.m_nview.ClaimOwnership();
        }

        return AddFuel(__instance.m_nview, null, __instance.m_fuelItem, Mathf.FloorToInt(__instance.m_maxFuel - Mathf.Ceil(fuel)), user, ref __result);
    }

    /// <summary>
    /// Puts raw food from the chests on a cooking station; with the fill-all key, on every free slot.
    /// </summary>
    /// <param name="__instance">The cooking station.</param>
    /// <param name="user">The player.</param>
    /// <param name="__result">True if food was added.</param>
    /// <returns>False when the food came from here.</returns>
    [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.OnInteract))]
    [HarmonyPrefix]
    public static bool CookingStationFood(CookingStation __instance, Humanoid user, ref bool __result)
    {
        if (user != Player.m_localPlayer || !NearbyContainers.IsActive(NearbyContainers.Use.Cooking) || !__instance.m_nview.IsValid()
            || __instance.HaveDoneItem() || __instance.FindCookableItem(user.GetInventory()) != null
            || (__instance.m_requireFire && !__instance.IsFireLit()))
        {
            return true;
        }

        int free = 0;
        for (int slot = 0; slot < __instance.m_slots.Length; slot++)
        {
            __instance.GetSlot(slot, out string itemName, out _, out _, out _);
            if (itemName == string.Empty)
            {
                free++;
            }
        }

        int wanted = NearbyContainers.FillAllHeld ? free : Mathf.Min(free, 1);
        int added = 0;
        if (!__instance.m_nview.HasOwner())
        {
            __instance.m_nview.ClaimOwnership();
        }

        foreach (CookingStation.ItemConversion conversion in __instance.m_conversion)
        {
            if (conversion.m_from == null || added >= wanted)
            {
                continue;
            }

            int count = NearbyContainers.Take(NearbyContainers.Use.Cooking, conversion.m_from.m_itemData.m_shared.m_name, wanted - added);
            for (int i = 0; i < count; i++)
            {
                __instance.m_nview.InvokeRPC("RPC_AddItem", conversion.m_from.gameObject.name, false);
            }

            added += count;
        }

        if (added == 0)
        {
            return true;
        }

        if (__instance.m_skill != Skills.SkillType.None)
        {
            Player.m_localPlayer.RaiseSkill(__instance.m_skill, 0.4f);
        }

        __result = true;
        return false;
    }

    private static bool IsLocalFueling(Humanoid user)
    {
        return user == Player.m_localPlayer && NearbyContainers.IsActive(NearbyContainers.Use.FuelAndOre);
    }

    // Adds fuel from the inventory (only when filling up) and the chests; a null RPC means the fire's single amount RPC.
    private static bool AddFuel(ZNetView nview, string rpc, ItemDrop fuelItem, int space, Humanoid user, ref bool result)
    {
        string name = fuelItem.m_itemData.m_shared.m_name;
        bool fillAll = NearbyContainers.FillAllHeld;
        if (space <= 0 || (!fillAll && user.GetInventory().HaveItem(name)))
        {
            return true;
        }

        int count = Gather(user, name, fillAll ? space : 1, fillAll);
        if (count == 0)
        {
            return true;
        }

        if (rpc == null)
        {
            nview.InvokeRPC("RPC_AddFuelAmount", (float)count);
        }
        else
        {
            for (int i = 0; i < count; i++)
            {
                nview.InvokeRPC(rpc);
            }
        }

        user.Message(MessageHud.MessageType.Center, $"$msg_added {name} x{count}");
        result = true;
        return false;
    }

    // Takes up to amount of an item, from the inventory first if allowed, then from the chests.
    private static int Gather(Humanoid user, string name, int amount, bool fromInventory)
    {
        int count = 0;
        if (fromInventory)
        {
            count = Mathf.Min(amount, user.GetInventory().CountItems(name));
            if (count > 0)
            {
                user.GetInventory().RemoveItem(name, count);
            }
        }

        return count + NearbyContainers.Take(NearbyContainers.Use.FuelAndOre, name, amount - count);
    }
}
