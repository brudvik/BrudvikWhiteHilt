using BrudvikWhiteHilt.SeaBattles;
using BrudvikWhiteHilt.SeaBattles.Escorts;
using BrudvikWhiteHilt.SeaBattles.Raids;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.SeaBattles;

/// <summary>
/// Hooks the Nail Ship encounter service, the raid service and the escort service onto the game object, so they live as
/// long as a world session, like the Kraken's.
/// </summary>
[HarmonyPatch]
public static class SeaBattleGamePatches
{
    /// <summary>
    /// Adds the encounter, raid and escort services to the game object when a world starts, on clients and on the server.
    /// </summary>
    /// <param name="__instance">The game.</param>
    [HarmonyPatch(typeof(Game), nameof(Game.Start))]
    [HarmonyPostfix]
    public static void GameStart(Game __instance)
    {
        __instance.gameObject.AddComponent<SeaBattleService>();
        __instance.gameObject.AddComponent<RaidService>();
        __instance.gameObject.AddComponent<EscortService>();
    }
}
