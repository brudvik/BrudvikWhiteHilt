using BrudvikWhiteHilt.Pieces.Navigation;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Navigation;

/// <summary>
/// Map clicks while setting route markers from the Navigator's Table: left-click adds, right-click removes, and a
/// double-click does not open the pin name box.
/// </summary>
[HarmonyPatch(typeof(Minimap))]
public static class ShipRoutePatches
{
    /// <summary>
    /// Adds a marker while planning.
    /// </summary>
    /// <param name="__instance">The map.</param>
    /// <returns>False while planning.</returns>
    [HarmonyPatch(nameof(Minimap.OnMapLeftClick))]
    [HarmonyPrefix]
    public static bool LeftClick(Minimap __instance)
    {
        if (!ShipRoutePlanner.Planning)
        {
            return true;
        }

        ShipRoutePlanner.OnMapClick(__instance.ScreenToWorldPoint(ZInput.pointerPosition));
        return false;
    }

    /// <summary>
    /// No pin name box while planning.
    /// </summary>
    /// <returns>False while planning.</returns>
    [HarmonyPatch(nameof(Minimap.OnMapDblClick))]
    [HarmonyPrefix]
    public static bool DoubleClick()
    {
        return !ShipRoutePlanner.Planning;
    }

    /// <summary>
    /// Removes a marker while planning; the route's pins are not removed like the player's own.
    /// </summary>
    /// <param name="__instance">The map.</param>
    /// <returns>False while planning or over a route pin.</returns>
    [HarmonyPatch(nameof(Minimap.RemovePinUnderPointer))]
    [HarmonyPrefix]
    public static bool RightClick(Minimap __instance)
    {
        if (ShipRoutePlanner.Planning)
        {
            ShipRoutePlanner.OnMapRightClick(__instance.ScreenToWorldPoint(ZInput.pointerPosition));
            return false;
        }

        return !ShipRoutePlanner.IsRoutePin(__instance.GetClosestPinToCursor());
    }
}
