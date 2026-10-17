using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.Building;

/// <summary>
/// Fits a snapped ghost to the piece it snapped to, where Valheim's snapping (which moves a ghost but never turns it,
/// and only joins point to point) leaves it wrong.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>A spiral stair rises 2 m in three quarters of a turn (a whole turn leaves too little headroom under the treads
/// above), so one put onto another is turned to start where the other ends. The game snaps only within half a metre
/// of the point inside the post, which the ghost rarely is when aimed at the treads, so a spiral stair's ghost standing
/// within another's treads near its head (or under its foot) goes there without snapping.</item>
/// <item>A log corner snapped to the end of a log wall is turned so the log ends of that kind of wall (plain or offset)
/// run along it. Which way the other wall goes is the player's choice of the plain or the mirrored corner.</item>
/// <item>A carved frieze or post board has its snap points on its back, a wall on its centre line, so a frieze snapped
/// to a wall sat inside it. It is moved out along its face to the wall's surface.</item>
/// </list>
/// The spiral stair and the corner snap at points on their own axis, so turning them keeps them on the point.
/// </remarks>
[HarmonyPatch]
internal static class SnapFitPatches
{
    private const string Prefix = "piece_whitehilt_";
    private const string SpiralStair = Prefix + "vindeltrapp";
    private const string LogCorner = Prefix + "laftehjorne";
    private const string MirroredLogCorner = Prefix + "laftehjorne_speilet";
    private const string LogWall = Prefix + "laftvegg";
    private const string OffsetLogWall = Prefix + "laftvegg_forskutt";
    private const string Frieze = Prefix + "frise_";
    private const string PostBoard = Prefix + "stolpebord_";

    // The turn of one spiral stair's treads, about +y, its rise, and how far its treads reach from the post.
    private const float StairTurn = 270f;
    private const float StairRise = 2f;
    private const float StairRadius = 1.2f;

    // How far over or under a spiral stair's head or foot a spiral stair's ghost still goes onto it.
    private const float StackReach = 1.2f;

    private static readonly List<Piece> nearby = new();

    // How far in front of a frieze the search for the wall's face starts.
    private const float Reach = 2f;

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
    private static void Fit(Player __instance)
    {
        GameObject ghost = __instance.m_placementGhost;
        if (__instance != Player.m_localPlayer || ghost == null || !ghost.activeSelf)
        {
            return;
        }

        // A spiral stair goes onto another wherever its ghost stands on the other's treads, not only when aimed at the
        // snap point inside the post, which the game's half-metre snapping would otherwise ask for.
        if (global::Utils.GetPrefabName(ghost) == SpiralStair && StackSpiralStair(ghost.transform))
        {
            return;
        }

        if (ghostPoint == null || otherPoint == null)
        {
            return;
        }

        Piece other = otherPoint.GetComponentInParent<Piece>();
        if (other == null || other.gameObject == ghost)
        {
            return;
        }

        string ghostName = global::Utils.GetPrefabName(ghost);
        string otherName = global::Utils.GetPrefabName(other.gameObject);
        if ((ghostName == LogCorner || ghostName == MirroredLogCorner) && otherName.StartsWith(LogWall))
        {
            TurnCorner(ghost.transform, ghostName == MirroredLogCorner, other.transform, otherName.StartsWith(OffsetLogWall));
        }
        else if ((ghostName.StartsWith(Frieze) || ghostName.StartsWith(PostBoard))
            && !otherName.StartsWith(Frieze) && !otherName.StartsWith(PostBoard))
        {
            MoveToFace(ghost.transform, other, ghostName.StartsWith(Frieze) ? 0.15f : 1f);
        }
    }

    // Puts a spiral stair's ghost on top of the nearest spiral stair whose treads it stands within, its head within
    // reach of the ghost's foot, or under one whose foot is within reach of the ghost's head, where no stair stands yet.
    // On top it goes on a further three quarters of a turn; under, it ends where the other begins.
    private static bool StackSpiralStair(Transform ghost)
    {
        nearby.Clear();
        Piece.GetAllPiecesInRadius(ghost.position, StairRise + StackReach + StairRadius, nearby);
        Transform best = null;
        bool bestAbove = false;
        float bestMiss = float.MaxValue;
        foreach (Piece piece in nearby)
        {
            if (piece == null || piece.gameObject == ghost.gameObject || global::Utils.GetPrefabName(piece.gameObject) != SpiralStair)
            {
                continue;
            }

            Transform stair = piece.transform;
            Vector3 offset = ghost.position - stair.position;
            float across = new Vector2(offset.x, offset.z).magnitude;
            if (across > StairRadius)
            {
                continue;
            }

            foreach (bool above in new[] { true, false })
            {
                float miss = Mathf.Abs(offset.y - (above ? StairRise : -StairRise));
                if (miss < StackReach && miss < bestMiss && !SpiralStairAt(stair.position + Vector3.up * (above ? StairRise : -StairRise), ghost.gameObject))
                {
                    best = stair;
                    bestAbove = above;
                    bestMiss = miss;
                }
            }
        }

        if (best == null)
        {
            return false;
        }

        ghost.rotation = best.rotation * Quaternion.Euler(0f, bestAbove ? StairTurn : -StairTurn, 0f);
        ghost.position = best.position + Vector3.up * (bestAbove ? StairRise : -StairRise);
        return true;
    }

    private static bool SpiralStairAt(Vector3 position, GameObject ghost)
    {
        return nearby.Any(piece => piece != null && piece.gameObject != ghost && (piece.transform.position - position).sqrMagnitude < 0.01f
            && global::Utils.GetPrefabName(piece.gameObject) == SpiralStair);
    }

    // The plain corner's log ends run along +x for the plain wall and along +z for the offset one; the mirrored corner's
    // the other way round. The wall runs from the snapped end towards its middle.
    private static void TurnCorner(Transform corner, bool mirrored, Transform wall, bool offsetWall)
    {
        float end = otherPoint.localPosition.x;
        if (Mathf.Abs(end) < 0.01f)
        {
            return;
        }

        Vector3 along = end > 0f ? Vector3.left : Vector3.right;
        Vector3 arm = offsetWall != mirrored ? Vector3.forward : Vector3.right;
        corner.rotation = wall.rotation * Quaternion.Euler(0f, Vector3.SignedAngle(arm, along, Vector3.up), 0f);
    }

    // Looks back from in front of the frieze for the other piece's surface and moves the frieze's back onto it. Facing
    // the other way, it finds the far face and goes there. Where the frieze is not over the piece, it stays.
    private static void MoveToFace(Transform frieze, Piece other, float middleHeight)
    {
        Vector3 face = frieze.forward;
        Vector3 middle = frieze.TransformPoint(0f, middleHeight, 0f);
        Ray ray = new(middle + face * Reach, -face);
        float nearest = float.MaxValue;
        foreach (Collider collider in other.GetComponentsInChildren<Collider>())
        {
            if (!collider.isTrigger && collider.Raycast(ray, out RaycastHit hit, Reach) && hit.distance < nearest)
            {
                nearest = hit.distance;
            }
        }

        if (nearest < float.MaxValue)
        {
            frieze.position += face * (Reach - nearest);
        }
    }
}
