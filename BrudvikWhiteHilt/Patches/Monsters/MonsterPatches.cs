using BrudvikWhiteHilt.Monsters;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Monsters;

/// <summary>
/// Hooks the Lindorm, Lava Worm and Muspell Giant services onto the game object, keeps a Lindorm or Lava Worm still and unhurt while it
/// is below ground, a Nidhogg Spawn still while it roars and a Huldra while she lures, and keeps dragon fire off buildings unless the config allows it.
/// </summary>
[HarmonyPatch]
public static class MonsterPatches
{
    [HarmonyPatch(typeof(Game), nameof(Game.Start))]
    [HarmonyPostfix]
    private static void GameStartPostfix(Game __instance)
    {
        __instance.gameObject.AddComponent<LindormService>();
        __instance.gameObject.AddComponent<LavaWormService>();
        __instance.gameObject.AddComponent<MuspellService>();
        __instance.gameObject.AddComponent<DragonGroundFireDamage>();
    }

    // A wild monster that only comes in bad weather learns the weathers before the spawn checks.
    [HarmonyPatch(typeof(SpawnSystem), nameof(SpawnSystem.UpdateSpawning))]
    [HarmonyPrefix]
    private static void UpdateSpawningPrefix()
    {
        WildMonsterRegistry.RefreshWeather();
    }

    // A creature cloned from a vanilla one plays its own attack sound as it starts an attack.
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.StartAttack))]
    [HarmonyPostfix]
    private static void AttackVoicePostfix(Humanoid __instance, bool __result)
    {
        if (__result && !__instance.IsPlayer())
        {
            __instance.GetComponent<CreatureAttackVoice>()?.Attack();
        }
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
        if (LindormBurrow.Instances.Count == 0 && SpawnRoar.RoaringCount == 0 && HuldraLure.LuringCount == 0)
        {
            return true;
        }

        // A Huldra who lures stands and faces the one she sings to.
        if (HuldraLure.IsLuring(__instance, out Player prey))
        {
            __instance.StopMoving();
            if (prey != null)
            {
                __instance.LookAt(prey.GetHeadPoint());
            }

            __result = true;
            return false;
        }

        // A Lindorm below ground and a Nidhogg Spawn in its roar both stand still.
        LindormBurrow burrow = __instance.GetComponent<LindormBurrow>();
        bool hidden = burrow != null && burrow.IsHidden;
        if (!hidden && !SpawnRoar.IsRoaring(__instance))
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
