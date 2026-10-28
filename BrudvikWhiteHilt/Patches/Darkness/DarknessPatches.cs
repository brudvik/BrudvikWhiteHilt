using BrudvikWhiteHilt.Darkness;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Darkness;

/// <summary>
/// Hooks the creeping darkness onto the game object, darkens the light the game has just set below ground, and lets
/// the shadows hunt only players without a light.
/// </summary>
[HarmonyPatch]
public static class DarknessPatches
{
    [HarmonyPatch(typeof(Game), nameof(Game.Start))]
    [HarmonyPostfix]
    private static void GameStartPostfix(Game __instance)
    {
        __instance.gameObject.AddComponent<DarknessService>();
        __instance.gameObject.AddComponent<DarknessClient>();
        __instance.gameObject.AddComponent<DarknessLights>();
        __instance.gameObject.AddComponent<DarknessSounds>();
        __instance.gameObject.AddComponent<DarknessSilence>();
        __instance.gameObject.AddComponent<DarknessDoors>();
        __instance.gameObject.AddComponent<DeepNorthNight>();
    }

    [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.SetEnv))]
    [HarmonyPostfix]
    private static void SetEnvPostfix()
    {
        DarknessLights.ApplyEnvironment();
    }

    // A shadow sees no one but a player standing without a light in the black.
    [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.FindEnemy))]
    [HarmonyPostfix]
    private static void FindEnemyPostfix(BaseAI __instance, ref Character __result)
    {
        if (DarkShadow.AnyLoaded && __instance.GetComponent<DarkShadow>() != null)
        {
            __result = DarkShadow.Prey(__instance);
        }
    }
}
