using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Potions.GiftOfFenrir;
using BrudvikWhiteHilt.Items.Potions.GiftOfOdin;
using HarmonyLib;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.Potions;

/// <summary>
/// Adds Gift of Odin's max health, which the game otherwise recalculates from food every second.
/// </summary>
[HarmonyPatch(typeof(Player), nameof(Player.GetTotalFoodValue))]
public static class OdinMaxHealthPatch
{
    private static void Postfix(Player __instance, ref float hp)
    {
        if (StatusEffectHelper.Has<GiftOfOdinEffect>(__instance))
        {
            hp += GiftOfOdinEffect.BonusMaxHealth;
        }
    }
}

/// <summary>
/// Speeds up the local player's attack animations while Gift of Fenrir is active.
/// </summary>
[HarmonyPatch(typeof(CharacterAnimEvent), nameof(CharacterAnimEvent.CustomFixedUpdate))]
public static class FenrirAttackSpeedPatch
{
    private static void Postfix(CharacterAnimEvent __instance)
    {
        Character character = __instance.m_character;
        if (character == null
            || character != Player.m_localPlayer
            || !character.InAttack()
            || __instance.m_pauseTimer > 0f
            || !StatusEffectHelper.Has<GiftOfFenrirEffect>(character))
        {
            return;
        }

        __instance.m_animator.speed = GiftOfFenrirEffect.AttackSpeed;
    }
}

/// <summary>
/// Heals the local player for part of the damage dealt while Gift of Fenrir is active.
/// </summary>
[HarmonyPatch(typeof(Character), nameof(Character.Damage))]
public static class FenrirLifeStealPatch
{
    private static void Prefix(Character __instance, HitData hit)
    {
        Player player = Player.m_localPlayer;
        if (hit == null || player == null || __instance == player || hit.m_attacker != player.GetZDOID())
        {
            return;
        }

        float damage = hit.GetTotalDamage();
        if (damage > 0f && StatusEffectHelper.Has<GiftOfFenrirEffect>(player))
        {
            player.Heal(damage * GiftOfFenrirEffect.LifeSteal);
        }
    }
}
