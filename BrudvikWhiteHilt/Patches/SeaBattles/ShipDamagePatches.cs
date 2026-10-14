using BrudvikWhiteHilt.SeaBattles;
using BrudvikWhiteHilt.SeaBattles.Damage;
using HarmonyLib;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.SeaBattles;

/// <summary>
/// The hooks that put holes and flooding into the game's ships: the hit on a hull, the ship's depth in the water and the
/// force of its sail. All of them start with a cheap static check, as they run for every hit or every physics step.
/// </summary>
[HarmonyPatch]
public static class ShipDamagePatches
{
    /// <summary>
    /// Looks at a hit on a hull before the game works out what the hull's resistances leave of it. A hit from a Nail Ship's
    /// own side is dropped, so it does neither harm nor holes; any other hit goes on, and a ship that cannot be harmed
    /// still gets its holes. It runs on the hull's owner and does not change the hit.
    /// </summary>
    /// <param name="__instance">The hull that was hit.</param>
    /// <param name="hit">The hit, as it arrived.</param>
    /// <returns>False to skip the game's method.</returns>
    [HarmonyPatch(typeof(WearNTear), "RPC_Damage")]
    [HarmonyPrefix]
    public static bool Hit(WearNTear __instance, HitData hit)
    {
        if (hit == null || (!ShipDamage.Loaded && RaiderShip.Loaded == 0))
        {
            return true;
        }

        try
        {
            if (FriendlyFirePatches.IsFriendlyFire(__instance, hit))
            {
                return false;
            }

            if (ShipDamage.Loaded)
            {
                ShipDamage.Of(__instance)?.TryHit(hit);
            }
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"Ship damage: a hit on a hull was not looked at: {ex}");
        }

        return true;
    }

    /// <summary>
    /// Sets how deep a flooded ship lies after the game's physics step. Only a ship with a hole or water in it is touched.
    /// </summary>
    /// <param name="__instance">The ship.</param>
    [HarmonyPatch(typeof(Ship), nameof(Ship.CustomFixedUpdate))]
    [HarmonyPostfix]
    public static void Buoyancy(Ship __instance)
    {
        if (!ShipDamage.Any)
        {
            return;
        }

        try
        {
            ShipDamage.Of(__instance)?.ApplyBuoyancy();
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"Ship damage: the depth of a flooded ship was not set: {ex}");
        }
    }

    /// <summary>
    /// Takes force out of the sail of a ship that holds water. It runs after the game's method and after the Nail Ship's own
    /// tailwind, which replaces it, so every ship slows alike.
    /// </summary>
    /// <param name="__instance">The ship.</param>
    /// <param name="__result">The sail force.</param>
    [HarmonyPatch(typeof(Ship), "GetSailForce")]
    [HarmonyPostfix]
    public static void FloodedSail(Ship __instance, ref Vector3 __result)
    {
        if (!ShipDamage.Any)
        {
            return;
        }

        ShipDamage damage = ShipDamage.Of(__instance);
        if (damage != null)
        {
            __result *= damage.SailFactor();
        }
    }
}
