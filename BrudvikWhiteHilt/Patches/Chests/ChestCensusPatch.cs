using BrudvikWhiteHilt.Chests;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Chests;

/// <summary>
/// Takes a chest census on the server as soon as the world is loaded, before any chest is restocked.
/// </summary>
[HarmonyPatch(typeof(ZNet), "ServerLoadWorld")]
public static class ChestCensusPatch
{
    /// <summary>
    /// Counts the chests. Game.Start can run before ZNet.Start has loaded the world, which gave an empty count.
    /// </summary>
    [HarmonyPostfix]
    public static void Postfix()
    {
        ChestCensus.OnWorldStarted();
    }
}
