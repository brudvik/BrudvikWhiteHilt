using BrudvikWhiteHilt.Pieces.Navigation;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Navigation;

/// <summary>
/// The Cartographer's Desk is an extension of the map table: it can only be used near one.
/// </summary>
[HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.CheckUsable))]
public static class CartographerDeskPatch
{
    /// <summary>
    /// Refuses the desk away from a map table.
    /// </summary>
    /// <param name="__instance">The crafting station.</param>
    /// <param name="player">The player.</param>
    /// <param name="showMessage">True to tell the player why not.</param>
    /// <param name="__result">True if the station can be used.</param>
    [HarmonyPostfix]
    public static void Postfix(CraftingStation __instance, Player player, bool showMessage, ref bool __result)
    {
        if (!__result)
        {
            return;
        }

        MapTableProximity proximity = __instance.GetComponent<MapTableProximity>();
        if (proximity == null || proximity.IsNearMapTable())
        {
            return;
        }

        if (showMessage)
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_needmaptable");
        }

        __result = false;
    }
}
