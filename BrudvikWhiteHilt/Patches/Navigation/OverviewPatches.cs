using BrudvikWhiteHilt.Navigation.Overview;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Navigation;

/// <summary>
/// Shows the exploration overview with the large map.
/// </summary>
[HarmonyPatch]
public static class OverviewPatches
{
    /// <summary>
    /// Keeps the overview panel up to date.
    /// </summary>
    /// <param name="__instance">The map.</param>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.Update))]
    [HarmonyPostfix]
    public static void MinimapUpdate(Minimap __instance)
    {
        OverviewPanel.Update(__instance);
    }
}
