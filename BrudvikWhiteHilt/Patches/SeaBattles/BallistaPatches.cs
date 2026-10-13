using BrudvikWhiteHilt.Pieces.Ships.Ballista;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.SeaBattles;

/// <summary>
/// Lets the gunner shoot the ship's ballista with the attack button and change its ammunition with the secondary attack.
/// While a player is attached, vanilla takes every attack as a wish to get up: <c>Player.SetControls</c> clears the attack and
/// lets go. These patches take the attack for the ballista before that, so the player stays at it and no fist is swung.
/// </summary>
[HarmonyPatch]
public static class BallistaPatches
{
    /// <summary>
    /// Turns the local gunner's attack into a shot and the secondary attack into a change of ammunition, before vanilla can
    /// turn either into a dismount.
    /// </summary>
    /// <param name="__instance">The player.</param>
    /// <param name="attack">The attack button was pressed.</param>
    /// <param name="attackHold">The attack button is held.</param>
    /// <param name="secondaryAttack">The secondary attack button was pressed.</param>
    /// <param name="secondaryAttackHold">The secondary attack button is held.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.SetControls))]
    [HarmonyPrefix]
    public static void SetControls(Player __instance, ref bool attack, ref bool attackHold, ref bool secondaryAttack, ref bool secondaryAttackHold)
    {
        if (!ShipBallista.LocalManned || __instance != Player.m_localPlayer)
        {
            return;
        }

        if (attack)
        {
            ShipBallista.RequestFire();
        }

        if (secondaryAttack)
        {
            ShipBallista.RequestSwitch();
        }

        attack = false;
        attackHold = false;
        secondaryAttack = false;
        secondaryAttackHold = false;
    }

    /// <summary>
    /// Writes the chosen ammunition under the crosshair while the local player mans the ballista.
    /// </summary>
    /// <param name="__instance">The HUD.</param>
    [HarmonyPatch(typeof(Hud), nameof(Hud.UpdateCrosshair))]
    [HarmonyPostfix]
    public static void UpdateCrosshair(Hud __instance)
    {
        try
        {
            ShipBallista.UpdateCrosshair(__instance);
        }
        catch (System.Exception ex)
        {
            Jotunn.Logger.LogWarning($"Ship ballista: the ammunition line was not shown: {ex.Message}");
        }
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
