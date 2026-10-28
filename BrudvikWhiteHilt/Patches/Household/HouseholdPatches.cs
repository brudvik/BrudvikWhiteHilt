using BrudvikWhiteHilt.Household;
using BrudvikWhiteHilt.Patches.Companions;
using HarmonyLib;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.Household;

/// <summary>
/// Hooks the household into the game's creature AI and the placing of the High Seat.
/// </summary>
[HarmonyPatch]
public static class HouseholdPatches
{
    // The last placement problem told, so it is told once and not every frame.
    private static string lastProblem;

    /// <summary>
    /// Lets a householder take up following again and its goal steer it after the AI has had its turn. Costs a count and a return for every creature
    /// when no householder is loaded.
    /// </summary>
    /// <param name="__instance">The creature's AI.</param>
    /// <param name="dt">Frame time.</param>
    /// <param name="__result">True when the AI ran, which it does on the creature's owner.</param>
    [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateAI))]
    [HarmonyPostfix]
    private static void SteerHouseholder(MonsterAI __instance, float dt, bool __result)
    {
        if (Householder.All.Count == 0 || !__result)
        {
            return;
        }

        AiParts parts = AiParts.Of(__instance);
        if (parts.Member != null)
        {
            parts.Member.UpdateAI();
        }

        if (parts.Probe != null)
        {
            parts.Probe.UpdateGoal(dt);
        }
    }

    /// <summary>
    /// Shows a High Seat's reach while it is placed, and forbids placing it where another High Seat's farm reaches.
    /// </summary>
    /// <param name="__instance">The building player.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacementGhost))]
    [HarmonyPostfix]
    private static void CheckHighSeatPlacement(Player __instance)
    {
        GameObject ghost = __instance.m_placementGhost;
        if (__instance != Player.m_localPlayer || ghost == null || !ghost.activeSelf)
        {
            lastProblem = null;
            return;
        }

        string problem = HighSeat.CheckGhost(ghost);
        if (problem == null || __instance.m_placementStatus != Player.PlacementStatus.Valid)
        {
            lastProblem = problem;
            return;
        }

        __instance.m_placementStatus = Player.PlacementStatus.Invalid;
        __instance.SetPlacementGhostValid(false);
        if (problem != lastProblem)
        {
            lastProblem = problem;
            __instance.Message(MessageHud.MessageType.TopLeft, problem);
        }
    }
}
