using HarmonyLib;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.Building;

/// <summary>
/// Turns a spiral stair snapped onto another so its treads go on from where the other's stop.
/// </summary>
/// <remarks>
/// The spiral stair rises 2 m in three quarters of a turn (a whole turn would leave too little headroom under the
/// treads above), so the next one up must be turned a quarter back to start where the one below ends. Valheim's
/// snapping moves the ghost but never turns it, and stacked stairs came out with their treads crossing. Both snap points
/// lie on the post, so turning the ghost does not move it off them.
/// </remarks>
[HarmonyPatch]
internal static class SpiralStairPatches
{
    private const string SpiralStair = "piece_whitehilt_vindeltrapp";

    // The turn of one stair's treads, about +y.
    private const float Turn = 270f;

    private static Transform ghostPoint;
    private static Transform otherPoint;

    [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacementGhost)), HarmonyPrefix]
    private static void Forget()
    {
        ghostPoint = null;
        otherPoint = null;
    }

    [HarmonyPatch(typeof(Player), nameof(Player.FindClosestSnapPoints)), HarmonyPostfix]
    private static void Remember(bool __result, ref Transform a, ref Transform b)
    {
        if (__result)
        {
            ghostPoint = a;
            otherPoint = b;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacementGhost)), HarmonyPostfix]
    private static void TurnOnStair(Player __instance)
    {
        GameObject ghost = __instance.m_placementGhost;
        if (__instance != Player.m_localPlayer || ghost == null || !ghost.activeSelf || ghostPoint == null || otherPoint == null
            || global::Utils.GetPrefabName(ghost) != SpiralStair)
        {
            return;
        }

        Piece other = otherPoint.GetComponentInParent<Piece>();
        if (other == null || other.gameObject == ghost || global::Utils.GetPrefabName(other.gameObject) != SpiralStair)
        {
            return;
        }

        // On top of the other (the ghost's foot on its head) it goes on a further three quarters of a turn; under it,
        // it ends where the other begins.
        bool above = ghostPoint.localPosition.y < otherPoint.localPosition.y;
        ghost.transform.rotation = other.transform.rotation * Quaternion.Euler(0f, above ? Turn : -Turn, 0f);
    }
}
