using BrudvikWhiteHilt.Quartermaster;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Quartermaster;

/// <summary>
/// Shows the range of a Quartermaster's Table while it is being placed.
/// </summary>
[HarmonyPatch]
public static class QuartermasterPatches
{
    /// <summary>
    /// The local player's placement ghost.
    /// </summary>
    /// <param name="__instance">The player.</param>
    [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
    [HarmonyPostfix]
    public static void Placement(Player __instance)
    {
        if (__instance == Player.m_localPlayer && __instance.m_placementGhost != null)
        {
            QuartermasterStand.ShowOnGhost(__instance.m_placementGhost);
        }
    }
}
