using BrudvikWhiteHilt.Navigation.Weather;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Navigation;

/// <summary>
/// Hooks the weather forecast panel to the map and the storm warning to the local player.
/// </summary>
[HarmonyPatch]
public static class WeatherPatches
{
    /// <summary>
    /// Shows the forecast with the large map.
    /// </summary>
    /// <param name="__instance">The map.</param>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.Update))]
    [HarmonyPostfix]
    public static void MinimapUpdate(Minimap __instance)
    {
        ForecastPanel.Update(__instance);
    }

    /// <summary>
    /// Checks for a coming storm.
    /// </summary>
    /// <param name="__instance">The player.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    [HarmonyPostfix]
    public static void PlayerUpdate(Player __instance)
    {
        if (__instance == Player.m_localPlayer)
        {
            StormWarning.Tick(__instance);
        }
    }
}
