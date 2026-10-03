using BrudvikWhiteHilt.OldLand;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.OldLand;

/// <summary>
/// Hooks the old land filler onto the game object.
/// </summary>
[HarmonyPatch]
public static class OldLandPatches
{
    [HarmonyPatch(typeof(Game), nameof(Game.Start))]
    [HarmonyPostfix]
    private static void GameStartPostfix(Game __instance)
    {
        __instance.gameObject.AddComponent<OldLandFiller>();
    }
}
