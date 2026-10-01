using BrudvikWhiteHilt.Monsters;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Monsters;

/// <summary>
/// Hooks the Lindorm service onto the game object, and keeps a Lindorm still and unhurt while it is below ground.
/// </summary>
[HarmonyPatch]
public static class MonsterPatches
{
    [HarmonyPatch(typeof(Game), nameof(Game.Start))]
    [HarmonyPostfix]
    private static void GameStartPostfix(Game __instance)
    {
        __instance.gameObject.AddComponent<LindormService>();
    }

    [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateAI))]
    [HarmonyPrefix]
    private static bool MonsterAIUpdatePrefix(MonsterAI __instance, ref bool __result)
    {
        LindormBurrow burrow = __instance.GetComponent<LindormBurrow>();
        if (burrow == null || !burrow.IsHidden)
        {
            return true;
        }

        __instance.StopMoving();
        __result = true;
        return false;
    }

    [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
    [HarmonyPrefix]
    private static bool CharacterDamagePrefix(Character __instance)
    {
        LindormBurrow burrow = __instance.GetComponent<LindormBurrow>();
        return burrow == null || !burrow.IsHidden;
    }
}
