using BrudvikWhiteHilt.Difficulty;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Difficulty;

/// <summary>
/// Notes each hit on the local player as it arrives and reports it afterwards when it was hard, and writes the recent
/// hits when the player dies (<see cref="BigHitLog"/>).
/// </summary>
[HarmonyPatch]
public static class BigHitLogPatches
{
    [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static void Before(Character __instance, HitData hit, out BigHitLog.Pending __state)
    {
        __state = __instance is Player player && player == Player.m_localPlayer && __instance.m_nview != null && __instance.m_nview.IsOwner()
            ? BigHitLog.Begin(player, hit)
            : null;
    }

    [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void After(Character __instance, HitData hit, BigHitLog.Pending __state)
    {
        if (__state != null)
        {
            BigHitLog.End(__instance as Player, hit, __state);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
    [HarmonyPrefix]
    private static void Died(Player __instance)
    {
        if (__instance == Player.m_localPlayer && __instance.m_nview != null && __instance.m_nview.IsOwner())
        {
            BigHitLog.OnDeath(__instance);
        }
    }
}
