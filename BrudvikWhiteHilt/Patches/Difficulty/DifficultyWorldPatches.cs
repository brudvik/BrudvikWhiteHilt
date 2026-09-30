using BrudvikWhiteHilt.Difficulty;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Difficulty;

/// <summary>
/// Hooks the difficulty service onto the game object and tints the night during a blood moon.
/// </summary>
[HarmonyPatch]
public static class DifficultyWorldPatches
{
    [HarmonyPatch(typeof(Game), nameof(Game.Start))]
    [HarmonyPostfix]
    private static void GameStartPostfix(Game __instance)
    {
        __instance.gameObject.AddComponent<DifficultyService>();
    }

    [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.SetEnv))]
    [HarmonyPostfix]
    private static void SetEnvPostfix(EnvMan __instance, float nightInt, float dt)
    {
        if (Utils.GetMainCamera() != null)
        {
            BloodMoonSky.Apply(__instance, nightInt, dt);
        }
    }
}
