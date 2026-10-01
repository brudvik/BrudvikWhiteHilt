using BrudvikWhiteHilt.Pieces.Ships;
using HarmonyLib;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.Ships;

/// <summary>
/// Lets the camera zoom further out at the helm and, if set, for everyone aboard a ship.
/// </summary>
[HarmonyPatch(typeof(GameCamera), nameof(GameCamera.UpdateCamera))]
public static class ShipCameraZoomPatch
{
    private static GameCamera camera;
    private static float vanillaMaxDistance;
    private static float vanillaMaxDistanceBoat;

    /// <summary>
    /// Sets the zoom limits that vanilla clamps the camera distance to this frame.
    /// </summary>
    /// <param name="__instance">The game camera.</param>
    [HarmonyPrefix]
    public static void Prefix(GameCamera __instance)
    {
        if (camera != __instance)
        {
            camera = __instance;
            vanillaMaxDistance = __instance.m_maxDistance;
            vanillaMaxDistanceBoat = __instance.m_maxDistanceBoat;
        }

        float boat = vanillaMaxDistanceBoat + ShipSettings.CameraExtraZoom.Value;
        __instance.m_maxDistanceBoat = boat;
        __instance.m_maxDistance = ShipSettings.CameraZoomAllAboard.Value && IsAboard(Player.m_localPlayer)
            ? Mathf.Max(vanillaMaxDistance, boat)
            : vanillaMaxDistance;
    }

    private static bool IsAboard(Player player)
    {
        // The ship's trigger volume, as vanilla's own "onboard" count; keeps the zoom while jumping on deck.
        return player != null && (player.InNumShipVolumes > 0 || player.IsAttachedToShip());
    }
}
