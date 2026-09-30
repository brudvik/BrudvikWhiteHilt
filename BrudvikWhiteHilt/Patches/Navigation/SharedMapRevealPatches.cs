using BrudvikWhiteHilt.Navigation;
using HarmonyLib;
using System.Collections;

namespace BrudvikWhiteHilt.Patches.Navigation;

/// <summary>
/// Hooks <see cref="SharedMapReveal"/> into the minimap: checks the skill each frame, and keeps shared map drawn as the
/// player's own when more is shared or the map is loaded again.
/// </summary>
[HarmonyPatch]
public static class SharedMapRevealPatches
{
    /// <summary>
    /// Checks whether the reveal should be on.
    /// </summary>
    /// <param name="__instance">The minimap.</param>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.Update))]
    [HarmonyPostfix]
    public static void UpdatePostfix(Minimap __instance)
    {
        if (Player.m_localPlayer != null)
        {
            SharedMapReveal.Update(__instance);
        }
    }

    /// <summary>
    /// Draws a pixel shared from a map table as the player's own while the reveal is on.
    /// </summary>
    /// <param name="__instance">The minimap.</param>
    /// <param name="x">Pixel column.</param>
    /// <param name="y">Pixel row.</param>
    /// <param name="__result">True if the pixel was newly shared.</param>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.ExploreOthers))]
    [HarmonyPostfix]
    public static void ExploreOthersPostfix(Minimap __instance, int x, int y, bool __result)
    {
        if (__result)
        {
            SharedMapReveal.OnSharedPixel(__instance, x, y);
        }
    }

    /// <summary>
    /// Draws the loaded shared map as the player's own again while the reveal is on.
    /// </summary>
    /// <param name="__instance">The minimap.</param>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.ResetAndExplore), typeof(BitArray), typeof(BitArray))]
    [HarmonyPostfix]
    public static void ResetAndExplorePostfix(Minimap __instance)
    {
        if (SharedMapReveal.Revealed)
        {
            SharedMapReveal.Redraw(__instance);
        }
    }

    /// <summary>
    /// Vanilla clears the fog texture, so the reveal has to be drawn again.
    /// </summary>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.Reset))]
    [HarmonyPostfix]
    public static void ResetPostfix()
    {
        SharedMapReveal.OnMapReset();
    }
}
