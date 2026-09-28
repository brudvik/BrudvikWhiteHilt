using BrudvikWhiteHilt.Pieces.Ships.WhiteHiltShip;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Ships;

/// <summary>
/// Gives shelter and keeps the rain off under the tent of a White Hilt Ship. The tent cloth has no colliders, so the
/// vanilla cover check sees open sky there.
/// </summary>
[HarmonyPatch(typeof(Player), nameof(Player.UpdateCover))]
public static class ShipTentCoverPatch
{
    [HarmonyPostfix]
    private static void Postfix(Player __instance)
    {
        if (__instance == Player.m_localPlayer && WhiteHiltShipUpgrades.IsUnderTent(__instance.GetCenterPoint()))
        {
            __instance.m_coverPercentage = 1f;
            __instance.m_underRoof = true;
        }
    }
}
