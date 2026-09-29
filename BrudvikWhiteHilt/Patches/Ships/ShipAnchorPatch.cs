using BrudvikWhiteHilt.Pieces.Ships.WhiteHiltShip;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Ships;

/// <summary>
/// Keeps the sail furled and the oars still while a White Hilt Ship lies at anchor, and tells the helmsman why.
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
}
