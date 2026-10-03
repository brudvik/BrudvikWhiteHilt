using BrudvikWhiteHilt.Items.Binding;
using HarmonyLib;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.Gear;

/// <summary>
/// Rowan's Ward: a foe whose blow a shield etched with the Thunder Rune blocks is struck by lightning.
/// </summary>
[HarmonyPatch]
public static class WardPatch
{
    private const float MaxRange = 10f;

    /// <summary>
    /// True while the ward's own lightning is dealt, so the weapon's rune does not act on it.
    /// </summary>
    public static bool Striking { get; private set; }

    /// <summary>
    /// Strikes the attacker after a successful block.
    /// </summary>
    /// <param name="__instance">The one who blocked.</param>
    /// <param name="attacker">Who struck.</param>
    /// <param name="__result">True when the blow was blocked.</param>
    [HarmonyPatch(typeof(Humanoid), "BlockAttack")]
    [HarmonyPostfix]
    private static void BlockAttack(Humanoid __instance, Character attacker, bool __result)
    {
        if (!__result || attacker == null || attacker.IsDead() || __instance != Player.m_localPlayer
            || Vector3.Distance(attacker.transform.position, __instance.transform.position) > MaxRange)
        {
            return;
        }

        float damage = GearBinding.WardDamage(__instance.GetCurrentBlocker());
        if (damage <= 0f)
        {
            return;
        }

        HitData hit = new()
        {
            m_point = attacker.GetCenterPoint(),
            m_dir = (attacker.transform.position - __instance.transform.position).normalized,
            m_attacker = __instance.GetZDOID(),
            m_hitType = HitData.HitType.PlayerHit,
            m_statusEffectHash = SEMan.s_statusEffectLightning
        };
        hit.m_damage.m_lightning = damage;
        Striking = true;
        try
        {
            attacker.Damage(hit);
        }
        finally
        {
            Striking = false;
        }
    }
}
