using BrudvikWhiteHilt.Navigation;
using HarmonyLib;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.Navigation;

/// <summary>
/// Widens the circle the map uncovers around the local player (Navigator's Table, Pathfinder's Amulet) and counts the
/// newly uncovered map for the Exploration skill. Vanilla still does the uncovering; only its radius changes for the tick.
/// </summary>
[HarmonyPatch(typeof(Minimap), nameof(Minimap.UpdateExplore))]
public static class MinimapExplorePatch
{
    private static bool counting;
    private static int newPixels;

    /// <summary>
    /// On the frames where vanilla uncovers the map, sets the radius and starts counting new map pixels.
    /// </summary>
    /// <param name="__instance">The minimap.</param>
    /// <param name="player">The local player.</param>
    /// <param name="__state">The vanilla radius, restored afterwards.</param>
    [HarmonyPrefix]
    public static void Prefix(Minimap __instance, Player player, out float __state)
    {
        __state = __instance.m_exploreRadius;
        if (player == null || __instance.m_exploreTimer + Time.deltaTime <= __instance.m_exploreInterval)
        {
            return;
        }

        __instance.m_exploreRadius = ExplorationSkill.GetExploreRadius(player, __state);
        counting = true;
        newPixels = 0;
    }

    /// <summary>
    /// Restores the radius and hands the newly uncovered area to the skill.
    /// </summary>
    /// <param name="__instance">The minimap.</param>
    /// <param name="player">The local player.</param>
    /// <param name="__state">The vanilla radius.</param>
    [HarmonyPostfix]
    public static void Postfix(Minimap __instance, Player player, float __state)
    {
        __instance.m_exploreRadius = __state;
        if (!counting)
        {
            return;
        }

        counting = false;
        ExplorationSkill.OnExplored(player, newPixels * __instance.m_pixelSize * __instance.m_pixelSize);
    }

    /// <summary>
    /// Counts the map pixels uncovered for the first time while <see cref="Prefix"/> has counting on.
    /// </summary>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.Explore), typeof(int), typeof(int))]
    public static class CountNewPixels
    {
        /// <summary>
        /// Counts one pixel if it was new.
        /// </summary>
        /// <param name="__result">True if the pixel was uncovered for the first time.</param>
        [HarmonyPostfix]
        public static void Postfix(bool __result)
        {
            if (counting && __result)
            {
                newPixels++;
            }
        }
    }
}
