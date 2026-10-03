using BrudvikWhiteHilt.Items.Meads.LabradorTeaBrew;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Meads;

/// <summary>
/// Biting insects do not sense a player who drank the Labrador Tea Brew. Read from the player's data, so it works on the
/// machine that runs the creature.
/// </summary>
[HarmonyPatch(typeof(BaseAI), nameof(BaseAI.CanSenseTarget), new[] { typeof(Character), typeof(bool) })]
public static class InsectWardPatch
{
    [HarmonyPostfix]
    private static void Postfix(BaseAI __instance, Character target, ref bool __result)
    {
        if (!__result || target is not Player || target.m_nview == null || !target.m_nview.IsValid()
            || !target.m_nview.GetZDO().GetBool(InsectWardEffect.WardKey))
        {
            return;
        }

        if (LabradorTeaBrew.Ignores(global::Utils.GetPrefabName(__instance.gameObject)))
        {
            __result = false;
        }
    }
}
