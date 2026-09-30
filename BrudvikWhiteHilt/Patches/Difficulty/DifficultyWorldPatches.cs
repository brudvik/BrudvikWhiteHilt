using BrudvikWhiteHilt.Difficulty;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Difficulty;

/// <summary>
/// Hooks the difficulty service onto the game object.
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
}
