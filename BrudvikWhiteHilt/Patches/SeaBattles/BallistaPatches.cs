using BrudvikWhiteHilt.Pieces.Ships.Ballista;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.SeaBattles;

/// <summary>
/// Lets the gunner shoot the ship's ballista with the attack button. While a player is attached, vanilla takes every
/// attack as a wish to get up: <c>Player.SetControls</c> clears the attack and lets go. These patches take the attack
/// for the ballista before that, so the player stays at it and no fist is swung.
/// </summary>
[HarmonyPatch]
public static class BallistaPatches
{
    /// <summary>
    /// Turns the local gunner's attack into a shot, before vanilla can turn it into a dismount.
    /// </summary>
    /// <param name="__instance">The player.</param>
    /// <param name="attack">The attack button was pressed.</param>
    /// <param name="attackHold">The attack button is held.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.SetControls))]
    [HarmonyPrefix]
    public static void SetControls(Player __instance, ref bool attack, ref bool attackHold)
    {
        if (!ShipBallista.LocalManned || __instance != Player.m_localPlayer)
        {
            return;
        }

        if (attack)
        {
            ShipBallista.RequestFire();
        }

        attack = false;
        attackHold = false;
    }

    /// <summary>
    /// Lets the local player go of the ballista when they no longer stand at it.
    /// </summary>
    /// <param name="__instance">The player.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    [HarmonyPostfix]
    public static void PlayerUpdate(Player __instance)
    {
        if (__instance == Player.m_localPlayer)
        {
            ShipBallista.Tick(__instance);
        }
    }
}
