using BrudvikWhiteHilt.SeaBattles.Weapons;
using HarmonyLib;
using System;

namespace BrudvikWhiteHilt.Patches.SeaBattles;

/// <summary>
/// The hook that lets the grapnel rope pull a ship. It starts with a cheap static check, as it runs for every ship on every
/// physics step.
/// </summary>
[HarmonyPatch]
public static class GrapplePatches
{
    /// <summary>
    /// Pulls a ship towards the other end of its ropes after the game's physics step. Only the owner of the ship's body does
    /// it, and only to its own ship.
    /// </summary>
    /// <param name="__instance">The ship.</param>
    [HarmonyPatch(typeof(Ship), nameof(Ship.CustomFixedUpdate))]
    [HarmonyPostfix]
    public static void Pull(Ship __instance)
    {
        if (!GrappleLink.Any)
        {
            return;
        }

        try
        {
            GrappleLink.Apply(__instance);
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"Grapnel rope: a ship was not pulled: {ex}");
        }
    }
}
