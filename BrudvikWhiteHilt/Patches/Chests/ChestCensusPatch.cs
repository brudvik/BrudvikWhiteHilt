using BrudvikWhiteHilt.Chests;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Chests;

/// <summary>
/// Takes a chest census when a world starts, before any chest is restocked.
/// </summary>
[HarmonyPatch(typeof(Game), "Start")]
public static class ChestCensusPatch
{
    /// <summary>
    /// Counts the chests once the world is loaded.
    /// </summary>
    [HarmonyPostfix]
    public static void Postfix()
    {
        ChestCensus.OnWorldStarted();
    }
}
