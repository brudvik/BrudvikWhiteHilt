using BrudvikWhiteHilt.Navigation.Compass;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Navigation;

/// <summary>
/// Hooks the compass to the map.
/// </summary>
[HarmonyPatch]
public static class CompassPatches
{
    /// <summary>
    /// Turns the compass needle with the camera.
    /// </summary>
    /// <param name="__instance">The map.</param>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.Update))]
    [HarmonyPostfix]
    public static void MinimapUpdate(Minimap __instance)
    {
        MapCompass.Update(__instance);
    }
}
