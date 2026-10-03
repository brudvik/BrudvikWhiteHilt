using BrudvikWhiteHilt.Items.Meads.HopAle;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Meads;

/// <summary>
/// Rested lasts longer for someone who drank Hop Ale.
/// </summary>
[HarmonyPatch]
public static class DeepSleepPatch
{
    private static readonly int hopAleHash = HopAle.EffectName.GetStableHashCode();

    /// <summary>
    /// Stretches Rested when it starts.
    /// </summary>
    /// <param name="__instance">The Rested effect.</param>
    [HarmonyPatch(typeof(SE_Rested), nameof(SE_Rested.Setup))]
    [HarmonyPostfix]
    private static void Setup(SE_Rested __instance)
    {
        Stretch(__instance);
    }

    /// <summary>
    /// Stretches Rested when resting renews it.
    /// </summary>
    /// <param name="__instance">The Rested effect.</param>
    [HarmonyPatch(typeof(SE_Rested), nameof(SE_Rested.ResetTime))]
    [HarmonyPostfix]
    private static void ResetTime(SE_Rested __instance)
    {
        Stretch(__instance);
    }

    // Only a timer that was just restarted, so renewing does not stretch it again and again.
    private static void Stretch(SE_Rested rested)
    {
        if (rested.m_time <= 0f && rested.m_character != null && rested.m_character.GetSEMan().HaveStatusEffect(hopAleHash))
        {
            rested.m_ttl *= 1f + HopAle.RestedBonus;
        }
    }
}
