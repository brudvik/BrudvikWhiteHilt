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

    /// <summary>Scrolls the HUD tape after the camera and its camera effects have updated.</summary>
    [HarmonyPatch(typeof(GameCamera), "LateUpdate")]
    [HarmonyPostfix]
    public static void CameraLateUpdate()
    {
        HudCompass.Update();
    }

    /// <summary>Places newly created boss bars below the tape regardless of late-update ordering.</summary>
    [HarmonyPatch(typeof(EnemyHud), "LateUpdate")]
    [HarmonyPostfix]
    public static void EnemyLateUpdate()
    {
        HudCompassLayout.Update();
    }
}
