using BrudvikWhiteHilt.Pieces.Ships.WhiteHiltShip;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Ships;

/// <summary>
/// Keeps the sail furled and the oars still while a White Hilt Ship lies at anchor or any ship is moored, and tells the
/// helmsman why.
/// </summary>
[HarmonyPatch]
public static class ShipAnchorPatch
{
    /// <summary>
    /// Stops the helmsman from setting sail or rowing while the anchor is down.
    /// </summary>
    /// <param name="__instance">The ship.</param>
    /// <returns>False to skip the vanilla speed change.</returns>
    [HarmonyPatch(typeof(Ship), nameof(Ship.Forward))]
    [HarmonyPatch(typeof(Ship), nameof(Ship.Backward))]
    [HarmonyPrefix]
    private static bool BlockWhileAnchored(Ship __instance)
    {
        WhiteHiltShipUpgrades upgrades = __instance.GetComponent<WhiteHiltShipUpgrades>();
        if (upgrades == null || !upgrades.IsAnchored)
        {
            return true;
        }

        if (Player.m_localPlayer != null)
        {
            Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_whitehilt_ship_anchor_down");
        }

        return false;
    }

    /// <summary>
    /// Keeps an anchored White Hilt Ship where it lies after each physics step.
    /// </summary>
    /// <param name="__instance">The ship.</param>
    [HarmonyPatch(typeof(Ship), nameof(Ship.CustomFixedUpdate))]
    [HarmonyPostfix]
    private static void HoldAnchored(Ship __instance)
    {
        __instance.GetComponent<WhiteHiltShipUpgrades>()?.HoldAnchor();
    }

    /// <summary>
    /// Stops the helmsman from setting sail or rowing while any ship is moored to a Mooring Post.
    /// </summary>
    /// <param name="__instance">The ship.</param>
    /// <returns>False to skip the vanilla speed change.</returns>
    [HarmonyPatch(typeof(Ship), nameof(Ship.Forward))]
    [HarmonyPatch(typeof(Ship), nameof(Ship.Backward))]
    [HarmonyPrefix]
    private static bool BlockWhileMoored(Ship __instance)
    {
        Pieces.Ships.ShipMooring mooring = __instance.GetComponent<Pieces.Ships.ShipMooring>();
        if (mooring == null || !mooring.IsMoored)
        {
            return true;
        }

        if (Player.m_localPlayer != null)
        {
            Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_whitehilt_ship_moored");
        }

        return false;
    }
}
