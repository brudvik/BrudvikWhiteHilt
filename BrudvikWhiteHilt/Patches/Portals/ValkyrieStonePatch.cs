using BrudvikWhiteHilt.Pieces.Portals.ValkyrieStone;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Portals;

/// <summary>
/// Makes each new local death available for Valkyrie Stone travel, even at the same coordinates.
/// </summary>
[HarmonyPatch]
public static class ValkyrieStonePatch
{
    /// <summary>
    /// Clears the previous trip before death processing and the subsequent respawn save.
    /// </summary>
    /// <param name="__instance">The player whose death is being processed.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
    [HarmonyPrefix]
    public static void ResetTravelOnDeath(Player __instance)
    {
        if (__instance == Player.m_localPlayer && __instance.m_nview != null && __instance.m_nview.IsOwner())
        {
            ValkyrieStoneComponent.ResetUsedDeath(__instance);
        }
    }
}