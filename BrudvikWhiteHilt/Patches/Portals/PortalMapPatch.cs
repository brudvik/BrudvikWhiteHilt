using BrudvikWhiteHilt.Pieces.Portals.PortalMap;
using HarmonyLib;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.Portals;

/// <summary>
/// Hooks the portal map into the game: the service on the game object, the pins on the map and the hover text.
/// </summary>
[HarmonyPatch]
public static class PortalMapPatch
{
    private const float PinCheckInterval = 2f;

    private static float nextPinCheck;

    [HarmonyPatch(typeof(Game), nameof(Game.Start))]
    [HarmonyPostfix]
    private static void GameStartPostfix(Game __instance)
    {
        __instance.gameObject.AddComponent<PortalMapService>();
    }

    [HarmonyPatch(typeof(Minimap), nameof(Minimap.Start))]
    [HarmonyPostfix]
    private static void MinimapStartPostfix(Minimap __instance)
    {
        PortalMapPins.MarkersOnTop(__instance);
        PortalMapPins.Refresh();
    }

    [HarmonyPatch(typeof(Minimap), nameof(Minimap.UpdatePins))]
    [HarmonyPostfix]
    private static void UpdatePinsPostfix(Minimap __instance)
    {
        PortalMapPins.ScalePins(__instance);
    }

    [HarmonyPatch(typeof(Minimap), nameof(Minimap.Update))]
    [HarmonyPostfix]
    private static void MinimapUpdatePostfix(Minimap __instance)
    {
        if (Time.time >= nextPinCheck)
        {
            nextPinCheck = Time.time + PinCheckInterval;
            PortalMapPins.EnsurePins(__instance);
        }
    }

    // The large map shows the biome under the cursor at the top; over a portal pin it shows the portal instead.
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.UpdateBiome))]
    [HarmonyPostfix]
    private static void UpdateBiomePostfix(Minimap __instance)
    {
        if (__instance.m_mode != Minimap.MapMode.Large || !ZInput.IsMouseActive())
        {
            return;
        }

        string text = PortalMapPins.GetHoverText(__instance.ScreenToWorldPoint(ZInput.pointerPosition), __instance.PinInteractRadius);
        if (text != null)
        {
            __instance.m_biomeNameLarge.text = text;
        }
    }
}
