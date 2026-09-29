using BrudvikWhiteHilt.Clock;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Clock;

/// <summary>
/// Updates the clock at the top of the screen with the HUD.
/// </summary>
[HarmonyPatch]
public static class ClockPatches
{
    /// <summary>
    /// Shows, hides and updates the clock after the game's HUD update.
    /// </summary>
    /// <param name="__instance">The HUD.</param>
    [HarmonyPatch(typeof(Hud), nameof(Hud.Update))]
    [HarmonyPostfix]
    public static void HudUpdate(Hud __instance)
    {
        GameClock.Update(__instance);
    }
}
