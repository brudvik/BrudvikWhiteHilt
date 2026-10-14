using BrudvikWhiteHilt.SeaBattles;
using BrudvikWhiteHilt.SeaBattles.Damage;
using HarmonyLib;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.SeaBattles;

/// <summary>
/// Keeps the Nail Ships from hurting each other: the game's collision damage between two of them, and the hits of their
/// own crews and ballistas. Both start with a cheap check for loaded Nail Ships, as they run for every collision or hit.
/// </summary>
[HarmonyPatch]
public static class FriendlyFirePatches
{
    /// <summary>
    /// Drops the impact of a Nail Ship against another Nail Ship. The game's impact effect would damage both hulls. A
    /// collision with the player's ship or anything else goes on as before.
    /// </summary>
    /// <param name="__instance">The impact effect, on the ship that was struck.</param>
    /// <param name="info">The collision.</param>
    /// <returns>False to skip the game's method.</returns>
    [HarmonyPatch(typeof(ImpactEffect), nameof(ImpactEffect.OnCollisionEnter))]
    [HarmonyPrefix]
    public static bool Collision(ImpactEffect __instance, Collision info)
    {
        if (RaiderShip.Loaded < 2 || info == null || info.collider == null)
        {
            return true;
        }

        try
        {
            bool self = RaiderShip.Is(__instance.GetComponentInParent<Ship>());
            return !FriendlyFire.IgnoresCollision(self, RaiderShip.Is(info.collider.GetComponentInParent<Ship>()));
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"Friendly fire: a ship collision was not looked at: {ex}");
            return true;
        }
    }

    /// <summary>
    /// Whether a hit on a piece is friendly fire against a Nail Ship, to be dropped before it does anything.
    /// </summary>
    /// <param name="piece">The piece that was hit.</param>
    /// <param name="hit">The hit.</param>
    /// <returns>True when the hit is to be ignored.</returns>
    public static bool IsFriendlyFire(WearNTear piece, HitData hit)
    {
        if (RaiderShip.Loaded == 0 || piece == null || hit == null)
        {
            return false;
        }

        if (!RaiderShip.Is(piece.GetComponentInParent<Ship>()))
        {
            return false;
        }

        bool hasAttacker = !hit.m_attacker.IsNone();
        return FriendlyFire.IgnoresHit(true, hasAttacker, hasAttacker && IsRaider(hit.m_attacker), hit.m_hitType == HitData.HitType.Turret);
    }

    // A creature is a Nail crew member or a raid guard when its ZDO names a ship or a site, as RaiderCrew.Attach reads it.
    // The ZDO is used so that the creature need not be loaded on this machine.
    private static bool IsRaider(ZDOID attacker)
    {
        ZDO zdo = ZDOMan.instance?.GetZDO(attacker);
        return zdo != null && (!zdo.GetZDOID(RaiderKeys.Ship).IsNone() || !string.IsNullOrEmpty(zdo.GetString(RaiderKeys.Site)));
    }
}
