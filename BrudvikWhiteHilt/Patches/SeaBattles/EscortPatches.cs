using BrudvikWhiteHilt.SeaBattles.Escorts;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.SeaBattles;

/// <summary>
/// Puts the escort contracts into the game: using an Escort Contract asks the server for a trading ship.
/// </summary>
[HarmonyPatch]
public static class EscortPatches
{
    /// <summary>
    /// Using an Escort Contract asks for a trading ship instead of equipping or consuming the item. Only for the local
    /// player; the contract is used up when the ship has been raised.
    /// </summary>
    /// <param name="__instance">Who uses the item.</param>
    /// <param name="item">The item.</param>
    /// <returns>False when the item is an Escort Contract, so the game's own use does not run.</returns>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UseItem))]
    [HarmonyPrefix]
    public static bool UseContract(Humanoid __instance, ItemDrop.ItemData item)
    {
        if (!EscortContract.Is(item) || __instance is not Player player || player != Player.m_localPlayer)
        {
            return true;
        }

        EscortService.Request(player);
        return false;
    }
}
