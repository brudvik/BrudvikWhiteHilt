using BrudvikWhiteHilt.Pieces.Portals.WhiteHiltPortal;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Portals;

/// <summary>
/// While the portal travel map is open, clicks on the map pick portals instead of pins, and typing in its search field
/// does not trigger the map's keys.
/// </summary>
[HarmonyPatch(typeof(Minimap))]
public static class PortalTravelMapPatch
{
    /// <summary>
    /// Counts the search field as map text input, so M and the other map keys are left alone while typing.
    /// </summary>
    /// <param name="__result">True while text is being typed.</param>
    [HarmonyPatch(nameof(Minimap.InTextInput))]
    [HarmonyPostfix]
    public static void SearchIsTextInput(ref bool __result)
    {
        __result |= PortalTravelPanel.SearchFocused;
    }

    /// <summary>
    /// A click picks the nearest portal instead of checking off a pin.
    /// </summary>
    /// <param name="__instance">The map.</param>
    /// <returns>False while the travel map is open.</returns>
    [HarmonyPatch(nameof(Minimap.OnMapLeftClick))]
    [HarmonyPrefix]
    public static bool PickPortal(Minimap __instance)
    {
        if (!PortalTravelPanel.IsOpen)
        {
            return true;
        }

        PortalTravelPanel.OnMapClick(__instance.ScreenToWorldPoint(ZInput.pointerPosition), __instance.PinInteractRadius);
        return false;
    }

    /// <summary>
    /// A double-click travels to the nearest portal instead of placing a pin.
    /// </summary>
    /// <param name="__instance">The map.</param>
    /// <returns>False while the travel map is open.</returns>
    [HarmonyPatch(nameof(Minimap.OnMapDblClick))]
    [HarmonyPrefix]
    public static bool TravelToPortal(Minimap __instance)
    {
        if (!PortalTravelPanel.IsOpen)
        {
            return true;
        }

        PortalTravelPanel.OnMapDoubleClick(__instance.ScreenToWorldPoint(ZInput.pointerPosition), __instance.PinInteractRadius);
        return false;
    }
}
