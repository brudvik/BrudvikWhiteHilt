using BrudvikWhiteHilt.Backpack;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Backpack;

/// <summary>
/// Gives the player the extra inventory rows and the hidden rows: the vanilla row count (bought from traders) is kept,
/// and the extra rows go on top.
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

    /// <summary>
    /// Runs the hotbar switching for the local player.
    /// </summary>
    /// <param name="__instance">The player.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    [HarmonyPostfix]
    public static void PlayerUpdate(Player __instance)
    {
        if (__instance == Player.m_localPlayer)
        {
            HotbarSets.Tick(__instance);
        }
    }

    /// <summary>
    /// Shows ammo and staff casts under the hotbar slots after vanilla has drawn them.
    /// </summary>
    /// <param name="__instance">The hotbar.</param>
    /// <param name="player">The local player.</param>
    [HarmonyPatch(typeof(HotkeyBar), nameof(HotkeyBar.UpdateIcons))]
    [HarmonyPostfix]
    public static void HotkeyBarUpdateIcons(HotkeyBar __instance, Player player)
    {
        HotbarSupply.Update(__instance, player);
    }

    /// <summary>
    /// Lays out the player's grid after vanilla has filled it.
    /// </summary>
    /// <param name="__instance">The grid.</param>
    /// <param name="player">The player the grid shows, null for containers.</param>
    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))]
    [HarmonyPostfix]
    public static void UpdateGui(InventoryGrid __instance, Player player)
    {
        if (player != null && player == Player.m_localPlayer && BackpackLayout.IsLocalInventory(__instance.m_inventory))
        {
            BackpackGui.Arrange(__instance, player);
        }
    }
}
