using BrudvikWhiteHilt.Items.Meads.HuldraDraught;
using HarmonyLib;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.Monsters;

/// <summary>
/// The Huldra Draught's veil: creatures neither see nor hear a veiled player beyond a few metres, so they do not find
/// them and lose them if they hunted them; and the player's own attack breaks the veil.
/// </summary>
[HarmonyPatch]
public static class HuldraVeilPatches
{
    private static readonly int veilHash = $"SE_{HuldraDraught.Name}".GetStableHashCode();

    [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.CanSenseTarget),
        new[] { typeof(Transform), typeof(Vector3), typeof(float), typeof(float), typeof(float), typeof(bool), typeof(bool), typeof(Character), typeof(bool), typeof(bool) })]
    [HarmonyPrefix]
    private static bool CanSensePrefix(Transform me, Character target, ref bool __result)
    {
        return Sensed(me, target, ref __result);
    }

    [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.CanSeeTarget),
        new[] { typeof(Transform), typeof(Vector3), typeof(float), typeof(float), typeof(bool), typeof(bool), typeof(Character) })]
    [HarmonyPrefix]
    private static bool CanSeePrefix(Transform me, Character target, ref bool __result)
    {
        return Sensed(me, target, ref __result);
    }

    [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.CanHearTarget), new[] { typeof(Transform), typeof(float), typeof(Character) })]
    [HarmonyPrefix]
    private static bool CanHearPrefix(Transform me, Character target, ref bool __result)
    {
        return Sensed(me, target, ref __result);
    }

    // The player who attacks shows themselves.
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.StartAttack))]
    [HarmonyPostfix]
    private static void AttackPostfix(Humanoid __instance, bool __result)
    {
        if (__result && __instance == Player.m_localPlayer && __instance.GetSEMan().HaveStatusEffect(veilHash))
        {
            __instance.GetSEMan().RemoveStatusEffect(veilHash);
        }
    }

    private static bool Sensed(Transform me, Character target, ref bool result)
    {
        if (me == null || !HuldraVeilEffect.HiddenFrom(target, me.position))
        {
            return true;
        }

        result = false;
        return false;
    }
}
