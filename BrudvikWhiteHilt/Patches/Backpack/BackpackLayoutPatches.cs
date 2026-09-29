using BrudvikWhiteHilt.Backpack;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Backpack;

/// <summary>
/// Gives the player the extra inventory rows: the vanilla row count (bought from traders) is kept, and the extra rows go on top.
/// </summary>
[HarmonyPatch]
public static class BackpackLayoutPatches
{
    /// <summary>
    /// Stores the vanilla row count and sizes the inventory with the extra rows.
    /// </summary>
    /// <param name="__instance">The player.</param>
    /// <param name="rows">Rows the game gives the player.</param>
    /// <returns>False, the vanilla method is replaced.</returns>
    [HarmonyPatch(typeof(Player), nameof(Player.SetInventorySize))]
    [HarmonyPrefix]
    public static bool SetInventorySize(Player __instance, int rows)
    {
        BackpackLayout.SetVanillaRows(__instance, rows);
        return false;
    }

    /// <summary>
    /// Sizes the inventory when the player spawns; vanilla only does so after rows were bought.
    /// </summary>
    /// <param name="__instance">The player.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    [HarmonyPostfix]
    public static void OnSpawned(Player __instance)
    {
        if (__instance == Player.m_localPlayer)
        {
            BackpackLayout.Apply(__instance);
        }
    }
}
