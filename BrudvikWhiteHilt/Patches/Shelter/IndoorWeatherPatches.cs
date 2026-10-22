using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Shelter;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Shelter;

/// <summary>
/// Keeps fog, snow and rain outside a house, after the game has set the weather.
/// </summary>
[HarmonyPatch]
public static class IndoorWeatherPatches
{
    /// <summary>
    /// Thins the fog under a roof, last, after the storms and the Mast Wisp have changed it.
    /// </summary>
    /// <param name="dt">Seconds since the last update.</param>
    [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.SetEnv))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void SetEnv(float dt)
    {
        // Without a camera the game leaves the fog as it was, and thinning it again would thin it every update.
        if (Utils.GetMainCamera() != null)
        {
            IndoorWeather.ApplyFog(dt);
        }
    }

    /// <summary>
    /// Takes away the snow and rain under a roof each frame.
    /// </summary>
    /// <param name="__instance">The environment manager.</param>
    [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.Update))]
    [HarmonyPostfix]
    public static void Update(EnvMan __instance)
    {
        if (!VisualHelper.IsHeadless)
        {
            IndoorWeather.Tick(__instance);
        }
    }
}
