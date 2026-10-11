using BrudvikWhiteHilt.Monsters;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Monsters;

/// <summary>
/// Hooks the Lindorm service onto the game object, keeps a Lindorm still and unhurt while it is below ground, and keeps
/// dragon fire off buildings unless the config allows it.
/// </summary>
[HarmonyPatch]
public static class MonsterPatches
{
    [HarmonyPatch(typeof(Game), nameof(Game.Start))]
    [HarmonyPostfix]
    private static void GameStartPostfix(Game __instance)
    {
        __instance.gameObject.AddComponent<LindormService>();
        __instance.gameObject.AddComponent<DragonGroundFireDamage>();
    }

    [HarmonyPatch(typeof(Attack), nameof(Attack.ProjectileAttackTriggered))]
    [HarmonyPrefix]
    private static void BreathPrefix(Attack __instance)
    {
        __instance.m_character?.GetComponent<DragonFire>()?.BeginBreath();
        __instance.m_character?.GetComponent<DragonBlast>()?.BeginBreath();
    }

    [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateAI))]
    [HarmonyPrefix]
    private static bool MonsterAIUpdatePrefix(MonsterAI __instance, ref bool __result)
    {
        if (LindormBurrow.Instances.Count == 0)
        {
            return true;
        }

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
        if (LindormBurrow.Instances.Count == 0)
        {
            return true;
        }

        LindormBurrow burrow = __instance.GetComponent<LindormBurrow>();
        return burrow == null || !burrow.IsHidden;
    }

    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Damage))]
    [HarmonyPrefix]
    private static bool WearNTearDamagePrefix(HitData hit)
    {
        Character attacker = hit.GetAttacker();
        if (attacker == null || attacker.GetComponent<DragonFire>() == null)
        {
            return true;
        }

        // A tame dragon, such as the horn's White Dragon, never burns buildings.
        return MonsterSettings.DragonBurnsBuildings.Value && !attacker.IsTamed();
    }
}
