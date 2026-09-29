using BrudvikWhiteHilt.Items.Portals;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Portals;

/// <summary>
/// Using the Home Stone from the hotbar or inventory takes the player home, and its rest shows again after respawning.
/// </summary>
[HarmonyPatch]
public static class HomeStonePatch
{
    /// <summary>
    /// Uses the Home Stone instead of the vanilla item use.
    /// </summary>
    /// <param name="__instance">The character using the item.</param>
    /// <param name="item">The item.</param>
    /// <returns>False for the Home Stone.</returns>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UseItem))]
    [HarmonyPrefix]
    public static bool UseHomeStone(Humanoid __instance, ItemDrop.ItemData item)
    {
        if (!HomeStone.IsHomeStone(item) || __instance is not Player player || player != Player.m_localPlayer)
        {
            return true;
        }

        HomeStone.Use(player);
        return false;
    }

    /// <summary>
    /// Shows the rest again after the player spawns, since dying clears status effects.
    /// </summary>
    /// <param name="__instance">The player.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    [HarmonyPostfix]
    public static void ShowRestAfterSpawn(Player __instance)
    {
        if (__instance == Player.m_localPlayer)
        {
            HomeStone.ShowRest(__instance);
        }
    }
}
