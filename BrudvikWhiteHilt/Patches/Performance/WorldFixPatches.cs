using System;
using BrudvikWhiteHilt.Performance;
using HarmonyLib;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.Performance;

/// <summary>
/// Two faults where the game loses what a player did: fuel and ore put into an object another player runs, and a
/// loading screen held up for ever by an object destroyed behind the game's back.
/// </summary>
[HarmonyPatch]
public static class WorldFixPatches
{
    /// <summary>
    /// Takes over a smelter or kiln before ore is put in, see <see cref="ClaimBeforeFeeding"/>.
    /// </summary>
    /// <param name="__instance">The smelter.</param>
    /// <param name="user">Who feeds it.</param>
    [HarmonyPatch(typeof(Smelter), nameof(Smelter.OnAddOre))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static void SmelterOre(Smelter __instance, Humanoid user)
    {
        ClaimBeforeFeeding(__instance.m_nview, user);
    }

    /// <summary>
    /// Takes over a smelter or kiln before fuel is put in.
    /// </summary>
    /// <param name="__instance">The smelter.</param>
    /// <param name="user">Who feeds it.</param>
    [HarmonyPatch(typeof(Smelter), nameof(Smelter.OnAddFuel))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static void SmelterFuel(Smelter __instance, Humanoid user)
    {
        ClaimBeforeFeeding(__instance.m_nview, user);
    }

    /// <summary>
    /// Takes over an oven before fuel is put in.
    /// </summary>
    /// <param name="__instance">The cooking station.</param>
    /// <param name="user">Who feeds it.</param>
    [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.OnAddFuelSwitch))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static void OvenFuel(CookingStation __instance, Humanoid user)
    {
        ClaimBeforeFeeding(__instance.m_nview, user);
    }

    /// <summary>
    /// Takes over a fire before it is fed by hand. Vanilla only takes over a fire nobody runs.
    /// </summary>
    /// <param name="__instance">The fire.</param>
    /// <param name="user">Who feeds it.</param>
    [HarmonyPatch(typeof(Fireplace), nameof(Fireplace.Interact))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static void FireInteract(Fireplace __instance, Humanoid user)
    {
        if (__instance.m_canRefill)
        {
            ClaimBeforeFeeding(__instance.m_nview, user);
        }
    }

    /// <summary>
    /// Takes over a fire before fuel from the hotbar is put in.
    /// </summary>
    /// <param name="__instance">The fire.</param>
    /// <param name="user">Who feeds it.</param>
    [HarmonyPatch(typeof(Fireplace), nameof(Fireplace.UseItem))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static void FireUseItem(Fireplace __instance, Humanoid user)
    {
        if (__instance.m_canRefill)
        {
            ClaimBeforeFeeding(__instance.m_nview, user);
        }
    }

    /// <summary>
    /// Clears the dead objects that keep an area from being ready, see <see cref="DeadInstances"/>.
    /// </summary>
    /// <param name="__instance">The scene.</param>
    /// <param name="point">The point asked about.</param>
    /// <param name="__result">Whether the area is ready.</param>
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.IsAreaReady))]
    [HarmonyPostfix]
    public static void AreaReady(ZNetScene __instance, Vector3 point, bool __result)
    {
        if (__result || !VanillaFixSettings.DeadObjects.Active)
        {
            return;
        }

        try
        {
            DeadInstances.Repair(__instance, point);
        }
        catch (Exception ex)
        {
            VanillaFixSettings.DeadObjects.Fail("clearing dead objects", ex);
        }
    }

    /// <summary>
    /// Makes the local player the owner of an object before it is fed. The game takes the fuel or ore from the inventory
    /// and then sends <c>RPC_AddFuel</c> or <c>RPC_AddOre</c> to the object's owner, whose handler drops the call unless
    /// it is still the owner when the call arrives. So the item was destroyed when the owner had just left or handed the
    /// object on, or when nobody owned it. Owned here, the call is handled at once on this machine. Our own
    /// fill-from-chests prefixes send the same calls, so this runs first.
    /// </summary>
    /// <param name="view">The object's network view.</param>
    /// <param name="user">Who feeds it.</param>
    private static void ClaimBeforeFeeding(ZNetView view, Humanoid user)
    {
        if (!VanillaFixSettings.FeedOwnership.Active || user == null || user != Player.m_localPlayer
            || view == null || !view.IsValid() || view.IsOwner())
        {
            return;
        }

        view.ClaimOwnership();
    }
}
