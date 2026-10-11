using BrudvikWhiteHilt.Items.Summoning;
using BrudvikWhiteHilt.Monsters;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Monsters;

/// <summary>
/// Flies the White Dragon in and off instead of letting it fight, and keeps it out of boss fights unless allowed.
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
