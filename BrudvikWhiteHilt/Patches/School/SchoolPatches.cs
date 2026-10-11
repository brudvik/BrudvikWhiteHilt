using BrudvikWhiteHilt.School;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.School;

/// <summary>
/// Forgets where items are found when a world starts, since another world may have other mods' vegetation.
/// </summary>
[HarmonyPatch]
public static class SchoolPatches
{
    [HarmonyPatch(typeof(Game), nameof(Game.Start))]
    [HarmonyPostfix]
    private static void GameStartPostfix()
    {
        LearnableItems.Reset();
    }
}
