using BrudvikWhiteHilt.Items.Summoning;
using BrudvikWhiteHilt.Monsters;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Monsters;

/// <summary>
/// Flies the White Dragon in, off and down in its dives instead of letting it fight, sends it after whatever hits its
/// caller, raises its health and damage by the power it was called with, and keeps it out of boss fights unless allowed.
/// </summary>
[HarmonyPatch]
public static class WhiteDragonPatches
{
    [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateAI))]
    [HarmonyPrefix]
    private static bool FlyInOrOff(MonsterAI __instance, float dt, ref bool __result)
    {
        if (!WhiteDragonAlly.Any || !WhiteDragonAlly.Steer(__instance, dt))
        {
            return true;
        }

        __result = true;
        return false;
    }

    // Runs where the hit player is simulated, which is their own machine.
    [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
    [HarmonyPostfix]
    private static void GuardCaller(Character __instance, HitData hit)
    {
        if (WhiteDragonAlly.Any && hit != null && __instance == Player.m_localPlayer)
        {
            WhiteDragonAlly.CallerHit(Player.m_localPlayer, hit.m_attacker);
        }
    }

    // After the difficulty settings have set the star bonus.
    [HarmonyPatch(typeof(Attack), nameof(Attack.GetLevelDamageFactor))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void PowerDamage(Attack __instance, ref float __result)
    {
        __result *= WhiteDragonAlly.Power(__instance.m_character);
    }

    [HarmonyPatch(typeof(Character), nameof(Character.SetupMaxHealth))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void PowerHealth(Character __instance)
    {
        float power = WhiteDragonAlly.Power(__instance);
        if (power > 1f)
        {
            __instance.SetMaxHealth(__instance.GetMaxHealth() * power);
        }
    }

    [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.IsEnemy), typeof(Character), typeof(Character))]
    [HarmonyPostfix]
    private static void SpareBosses(Character a, Character b, ref bool __result)
    {
        if (__result && WhiteDragonAlly.Any && !DragonHorn.FightsBosses.Value
            && ((WhiteDragonAlly.Is(a) && IsBoss(b)) || (WhiteDragonAlly.Is(b) && IsBoss(a))))
        {
            __result = false;
        }
    }

    private static bool IsBoss(Character character)
    {
        return character.IsBoss() || character.GetFaction() == Character.Faction.Boss;
    }
}
