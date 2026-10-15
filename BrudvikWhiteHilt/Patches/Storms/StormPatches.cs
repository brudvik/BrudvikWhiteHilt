using BrudvikWhiteHilt.Storms;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Storms;

/// <summary>
/// Hooks the storms onto the game object, puts their weather where the local player is and thickens the fog as the
/// view closes in.
/// </summary>
[HarmonyPatch]
public static class StormPatches
{
    [HarmonyPatch(typeof(Game), nameof(Game.Start))]
    [HarmonyPostfix]
    private static void GameStartPostfix(Game __instance)
    {
        __instance.gameObject.AddComponent<StormService>();
        __instance.gameObject.AddComponent<StormClient>();
        __instance.gameObject.AddComponent<StormAmbience>();
        __instance.gameObject.AddComponent<StormHazards>();
    }

    // After the game's own forced weathers: a dungeon, a raid, a boss or a special place still wins.
    [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.GetEnvironmentOverride))]
    [HarmonyPostfix]
    private static void GetEnvironmentOverridePostfix(EnvMan __instance, ref string __result)
    {
        if (string.IsNullOrEmpty(__result))
        {
            __result = StormWeather.Override(__instance);
        }
    }

    [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.SetEnv))]
    [HarmonyPostfix]
    private static void SetEnvPostfix()
    {
        StormWeather.ApplyFog();
    }
}
