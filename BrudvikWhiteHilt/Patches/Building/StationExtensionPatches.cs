using BrudvikWhiteHilt.Building;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Building;

/// <summary>
/// Workbench extensions: placed where they fit instead of apart from every other extension, within a configurable reach
/// of their station, with the station's reach shown as a ring while one is placed.
/// </summary>
[HarmonyPatch]
public static class StationExtensionPatches
{
    // The game asks this only while an extension is placed: is another extension within the piece's space requirement?
    [HarmonyPatch(typeof(StationExtension), nameof(StationExtension.OtherExtensionInRange))]
    [HarmonyPrefix]
    private static bool InRangePrefix(StationExtension __instance, float radius, ref bool __result)
    {
        __result = StationExtensions.InTheWay(__instance, radius);
        return false;
    }

    // Every extension that comes to life, the placement ghost included, gets the configured reach.
    [HarmonyPatch(typeof(StationExtension), nameof(StationExtension.Awake))]
    [HarmonyPostfix]
    private static void AwakePostfix(StationExtension __instance)
    {
        StationExtensions.Apply(__instance);
    }

    [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
    [HarmonyPostfix]
    private static void PlacementPostfix(Player __instance)
    {
        if (__instance == Player.m_localPlayer)
        {
            StationExtensions.UpdateRing(__instance);
        }
    }
}
