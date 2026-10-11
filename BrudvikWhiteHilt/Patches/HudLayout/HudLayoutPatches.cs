using BrudvikWhiteHilt.HudLayout;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.HudLayout;

/// <summary>
/// Lays out the White Hilt HUD over the game's.
/// </summary>
[HarmonyPatch]
public static class HudLayoutPatches
{
    /// <summary>
    /// Notes the game's own HUD elements before any mod adds to the HUD.
    /// </summary>
    /// <param name="__instance">The HUD.</param>
    [HarmonyPatch(typeof(Hud), "Awake")]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static void HudAwake(Hud __instance)
    {
        HudConsole.RememberNative(__instance);
    }

    /// <summary>
    /// Lays out the console after the game's HUD and its animations have placed their elements for this frame.
    /// </summary>
    /// <param name="__instance">The HUD.</param>
    [HarmonyPatch(typeof(Hud), "LateUpdate")]
    [HarmonyPostfix]
    public static void HudLateUpdate(Hud __instance)
    {
        HudConsole.Update(__instance);
        HudTop.Update(__instance);
        HudDamage.Update();
    }

    /// <summary>
    /// Shows where a hit on the local player came from.
    /// </summary>
    /// <param name="__instance">The player hit.</param>
    /// <param name="hit">The hit.</param>
    [HarmonyPatch(typeof(Player), "OnDamaged")]
    [HarmonyPostfix]
    public static void PlayerDamaged(Player __instance, HitData hit)
    {
        HudDamage.Hit(__instance, hit);
    }
}
