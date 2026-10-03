using BrudvikWhiteHilt.Guestbook;
using HarmonyLib;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.Guestbook;

/// <summary>
/// Reports building and tearing down to the guestbooks nearby.
/// </summary>
[HarmonyPatch]
public static class GuestbookPatches
{
    /// <summary>
    /// A piece the local player placed.
    /// </summary>
    /// <param name="__instance">The player.</param>
    /// <param name="piece">The piece prefab.</param>
    /// <param name="pos">Where it was placed.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.PlacePiece))]
    [HarmonyPostfix]
    public static void Placed(Player __instance, Piece piece, Vector3 pos)
    {
        if (__instance == Player.m_localPlayer && piece != null && piece.GetComponent<GuestbookStand>() == null)
        {
            GuestbookStand.Report(GuestEntryKind.Built, pos, piece.m_name);
        }
    }

    /// <summary>
    /// A piece torn down on this machine.
    /// </summary>
    /// <param name="__instance">The piece.</param>
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Remove))]
    [HarmonyPrefix]
    public static void Removed(WearNTear __instance)
    {
        Piece piece = __instance.GetComponent<Piece>();
        if (piece != null && piece.GetComponent<GuestbookStand>() == null)
        {
            GuestbookStand.Report(GuestEntryKind.Removed, __instance.transform.position, piece.m_name);
        }
    }
}
