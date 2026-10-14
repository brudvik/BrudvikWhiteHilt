using BrudvikWhiteHilt.Comfort;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.Comfort;

/// <summary>
/// Comfort over a whole room: the game's list of comfort pieces near a player is widened by the player's own reach and
/// by the Husvettes; reading the Book of the Hall, building comfort and resting feed Homecraft; a Husvette being placed
/// shows its reach.
/// </summary>
[HarmonyPatch]
public static class ComfortPatches
{
    // The game asks for the pieces within 10 m of a player under a roof; the answer now reaches as far as the player's own
    // reach and every Husvette whose room the player stands in. The game then counts one per comfort group, as before.
    [HarmonyPatch(typeof(SE_Rested), nameof(SE_Rested.GetNearbyComfortPieces))]
    [HarmonyPrefix]
    private static bool NearbyPrefix(Vector3 point, ref List<Piece> __result)
    {
        __result = ComfortService.NearbyComfortPieces(point);
        return false;
    }

    [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
    [HarmonyPostfix]
    private static void PlacementPostfix(Player __instance)
    {
        if (__instance == Player.m_localPlayer && __instance.m_placementGhost != null)
        {
            Husvette.ShowOnGhost(__instance.m_placementGhost);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.PlacePiece))]
    [HarmonyPostfix]
    private static void PlacedPostfix(Player __instance, Piece piece)
    {
        if (__instance == Player.m_localPlayer)
        {
            ComfortService.Built(__instance, piece);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    [HarmonyPostfix]
    private static void UpdatePostfix(Player __instance)
    {
        if (__instance == Player.m_localPlayer)
        {
            ComfortService.TickResting(__instance, Time.deltaTime);
        }
    }

    // Reading the Book of the Hall instead of the game's own use of the item.
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UseItem))]
    [HarmonyPrefix]
    private static bool UseBookPrefix(Humanoid __instance, ItemDrop.ItemData item)
    {
        if (!ComfortService.IsBook(item) || __instance is not Player player || player != Player.m_localPlayer)
        {
            return true;
        }

        ComfortService.Read(player, item);
        return false;
    }
}
