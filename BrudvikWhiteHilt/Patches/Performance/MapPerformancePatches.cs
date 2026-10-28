using System;
using BrudvikWhiteHilt.Performance;
using HarmonyLib;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.Performance;

/// <summary>
/// Takes the hitches out of the map: uncovering sends only the changed part of the fog texture
/// (<see cref="MapFogUpload"/>), and saving the character writes the map from its packed bits or reuses an unchanged
/// one (<see cref="MapSaveCache"/>).
/// </summary>
[HarmonyPatch]
public static class MapPerformancePatches
{
    /// <summary>
    /// Uncovers the circle around the player with a partial upload instead of the game's full one.
    /// </summary>
    /// <param name="__instance">The minimap.</param>
    /// <param name="p">World position.</param>
    /// <param name="radius">Radius in metres.</param>
    /// <returns>False when the circle was uncovered here.</returns>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.Explore), typeof(Vector3), typeof(float))]
    [HarmonyPrefix]
    public static bool ExplorePrefix(Minimap __instance, Vector3 p, float radius)
    {
        try
        {
            return !MapFogUpload.TryExplore(__instance, p, radius);
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning("Map fog upload failed, the game's own method runs: " + ex.Message);
            return true;
        }
    }

    /// <summary>
    /// Hands back the map data from the packed bits or from the last save, when it can.
    /// </summary>
    /// <param name="__instance">The minimap.</param>
    /// <param name="__result">The map data.</param>
    /// <returns>False when <paramref name="__result"/> was made here.</returns>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.GetMapData))]
    [HarmonyPrefix]
    public static bool GetMapDataPrefix(Minimap __instance, ref byte[] __result)
    {
        if (MapSaveCache.TryGetMapData(__instance, out byte[] result))
        {
            __result = result;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Keeps the game's own map data for the next save of an unchanged map.
    /// </summary>
    /// <param name="__result">The map data.</param>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.GetMapData))]
    [HarmonyPostfix]
    public static void GetMapDataPostfix(byte[] __result)
    {
        MapSaveCache.AfterVanilla(__result);
    }

    /// <summary>
    /// Checks the fast map data against the game's the first time a world loads, while the loading screen is still up.
    /// </summary>
    /// <param name="__instance">The minimap.</param>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.LoadMapData))]
    [HarmonyPostfix]
    public static void LoadMapDataPostfix(Minimap __instance)
    {
        MapSaveCache.CheckAgainstGame(__instance);
    }
}
