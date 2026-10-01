using BrudvikWhiteHilt.Mastery;
using HarmonyLib;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.Mastery;

/// <summary>
/// The local player's side of the skill milestones: shared levels, the hidden effect, Blocking, Lookout, skill loss
/// on death and the skill book page.
/// </summary>
[HarmonyPatch]
public static class MasteryPlayerPatches
{
    private const float ShieldWallPulse = 0.5f;
    private const float PerfectBlockWindow = 0.25f;

    private static Humanoid blocker;
    private static float nextShieldWall;

    /// <summary>
    /// Lets the player receive replanting requests from other machines.
    /// </summary>
    /// <param name="__instance">The player.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
    [HarmonyPostfix]
    private static void RegisterRpcs(Player __instance)
    {
        Gathering.RegisterRpc(__instance);
    }

    /// <summary>
    /// Shares levels, keeps the hidden effect on, raises the Shield Wall and checks the Lookout key.
    /// </summary>
    /// <param name="__instance">The player.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    [HarmonyPostfix]
    private static void UpdateLocal(Player __instance)
    {
        if (__instance != Player.m_localPlayer || __instance.IsDead())
        {
            return;
        }

        SkillLevels.Share(__instance);
        TemporaryPins.Update();
        Lookout.Update(__instance);
        SEMan seman = __instance.GetSEMan();
        if (!seman.HaveStatusEffect(MasteryEffects.GuardHash) && ObjectDB.instance?.GetStatusEffect(MasteryEffects.GuardHash) is StatusEffect guard)
        {
            seman.AddStatusEffect(guard);
        }

        RaiseShieldWall(__instance);
    }

    /// <summary>
    /// Adds the Blocking skill's extra health.
    /// </summary>
    /// <param name="__instance">The player.</param>
    /// <param name="hp">Max health.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.GetTotalFoodValue))]
    [HarmonyPostfix]
    private static void AddHealth(Player __instance, ref float hp)
    {
        hp += Perks.ExtraHealth(__instance.GetSkillLevel(Skills.SkillType.Blocking));
    }

    /// <summary>
    /// Notes who blocks, for Iron Guard, and whether it is a perfect parry, for Riposte.
    /// </summary>
    /// <param name="__instance">The blocker.</param>
    /// <param name="__state">True for a perfect parry.</param>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.BlockAttack))]
    [HarmonyPrefix]
    private static void BeforeBlock(Humanoid __instance, out bool __state)
    {
        blocker = __instance;
        ItemDrop.ItemData shield = __instance.GetCurrentBlocker();
        __state = shield != null && shield.m_shared.m_timedBlockBonus > 1f && __instance.m_blockTimer != -1f && __instance.m_blockTimer < PerfectBlockWindow;
    }

    /// <summary>
    /// Readies a Riposte after a perfect parry.
    /// </summary>
    /// <param name="__instance">The blocker.</param>
    /// <param name="__state">True for a perfect parry.</param>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.BlockAttack))]
    [HarmonyFinalizer]
    private static void AfterBlock(Humanoid __instance, bool __state)
    {
        blocker = null;
        if (__state && __instance is Player player && player == Player.m_localPlayer && player.HaveStamina() && Perks.Riposte.Has(player))
        {
            MasteryEffects.Start(player, MasteryEffects.RiposteHash, MasterySettings.RiposteSeconds.Value);
        }
    }

    /// <summary>
    /// Iron Guard: more block power while the player blocks.
    /// </summary>
    /// <param name="__result">Block power.</param>
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetBlockPower), typeof(float))]
    [HarmonyPostfix]
    private static void IronGuard(ref float __result)
    {
        if (blocker is Player player && Perks.IronGuard.Has(player))
        {
            __result *= 1f + MasterySettings.IronGuardBonus.Value;
        }
    }

    /// <summary>
    /// Last Stand: a deadly blow leaves 1 health, and nothing hurts for a moment after.
    /// </summary>
    /// <param name="__instance">The character hit.</param>
    /// <param name="hit">The hit.</param>
    /// <returns>False to ignore the hit.</returns>
    [HarmonyPatch(typeof(Character), nameof(Character.ApplyDamage))]
    [HarmonyPrefix]
    private static bool HoldOn(Character __instance, HitData hit)
    {
        if (__instance is not Player player || player != Player.m_localPlayer || player.IsDead())
        {
            return true;
        }

        if (MasteryEffects.InLastStandGrace(player))
        {
            return false;
        }

        float damage = hit.GetTotalDamage() * Game.m_localDamgeTakenRate;
        float health = player.GetHealth();
        if (damage <= 0f || health - damage > 0f || !Perks.LastStand.Has(player) || player.GetSEMan().HaveStatusEffect(MasteryEffects.LastStandHash))
        {
            return true;
        }

        hit.ApplyModifier(Mathf.Max(0f, health - 1f) / damage);
        MasteryEffects.Start(player, MasteryEffects.LastStandHash, MasterySettings.LastStandCooldownMinutes.Value * 60f);
        player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_laststand");
        return true;
    }

    /// <summary>
    /// Scales the skill loss on death by the server's setting.
    /// </summary>
    /// <param name="__instance">The skills.</param>
    /// <returns>False when handled here.</returns>
    [HarmonyPatch(typeof(Skills), nameof(Skills.OnDeath))]
    [HarmonyPrefix]
    private static bool ScaleSkillLoss(Skills __instance)
    {
        float multiplier = MasterySettings.DeathLossMultiplier.Value;
        if (Mathf.Approximately(multiplier, 1f))
        {
            return true;
        }

        if (multiplier > 0f)
        {
            __instance.LowerAllSkills(Mathf.Clamp01(__instance.m_DeathLowerFactor * Game.m_skillReductionRate * multiplier));
        }

        return false;
    }

    /// <summary>
    /// Shows each skill's milestones and what it gives at the player's level.
    /// </summary>
    /// <param name="__instance">The skills dialog.</param>
    /// <param name="player">The player.</param>
    [HarmonyPatch(typeof(SkillsDialog), nameof(SkillsDialog.Setup))]
    [HarmonyPostfix]
    private static void ShowMilestones(SkillsDialog __instance, Player player)
    {
        SkillBook.Fill(__instance, player);
    }

    private static void RaiseShieldWall(Player player)
    {
        if (Time.time < nextShieldWall || !player.IsBlocking())
        {
            return;
        }

        ItemDrop.ItemData shield = player.GetCurrentBlocker();
        if (shield == null || shield.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Shield || shield.m_shared.m_timedBlockBonus > 1f || !Perks.ShieldWall.Has(player))
        {
            return;
        }

        nextShieldWall = Time.time + ShieldWallPulse;
        foreach (Player other in Player.GetAllPlayers())
        {
            if (other != null && !other.IsDead() && Vector3.Distance(other.transform.position, player.transform.position) <= MasterySettings.ShieldWallRange.Value)
            {
                other.GetSEMan().AddStatusEffect(MasteryEffects.ShieldWallHash, resetTime: true);
            }
        }
    }
}
