using BrudvikWhiteHilt.Items.Binding;
using HarmonyLib;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.Gear;

/// <summary>
/// A creature in Dread runs from the nearest player instead of thinking.
/// </summary>
[HarmonyPatch]
public static class DreadPatch
{
    private const float FleeFromRange = 60f;

    /// <summary>
    /// Makes a creature in Dread flee.
    /// </summary>
    /// <param name="__instance">The creature's AI.</param>
    /// <param name="dt">Time step.</param>
    /// <param name="__result">True when the AI handled the frame.</param>
    /// <returns>False to skip the vanilla AI while fleeing.</returns>
    [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateAI))]
    [HarmonyPrefix]
    private static bool UpdateAI(MonsterAI __instance, float dt, ref bool __result)
    {
        Character character = __instance.m_character;
        if (character == null || __instance.m_nview == null || !__instance.m_nview.IsValid() || !__instance.m_nview.IsOwner()
            || character.IsTamed() || !character.GetSEMan().HaveStatusEffect(DreadEffect.Hash))
        {
            return true;
        }

        Vector3 position = character.transform.position;
        Player player = Player.GetClosestPlayer(position, FleeFromRange);
        Vector3 from = player != null ? player.transform.position
            : __instance.m_targetCreature != null ? __instance.m_targetCreature.transform.position : position - character.transform.forward;
        __instance.Flee(dt, from);
        __result = true;
        return false;
    }
}
