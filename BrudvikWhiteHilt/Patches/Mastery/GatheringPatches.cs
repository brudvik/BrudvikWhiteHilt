using BrudvikWhiteHilt.Mastery;
using HarmonyLib;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.Mastery;

/// <summary>
/// Woodcutting and Pickaxes milestones, on the machine that owns the tree, log, stump or rock.
/// </summary>
[HarmonyPatch]
public static class GatheringPatches
{
    /// <summary>
    /// Watches a hit on a tree.
    /// </summary>
    /// <param name="__instance">The tree.</param>
    /// <param name="hit">The hit.</param>
    /// <param name="__state">The context.</param>
    [HarmonyPatch(typeof(TreeBase), nameof(TreeBase.RPC_Damage))]
    [HarmonyPrefix]
    private static void BeforeTreeHit(TreeBase __instance, HitData hit, out Gathering.Context __state)
    {
        __state = null;
        if (MasterySettings.Woodcutting.Value && __instance.m_nview != null && __instance.m_nview.IsOwner())
        {
            __state = Gathering.Begin(hit, Skills.SkillType.WoodCutting);
            if (__state != null)
            {
                Gathering.PrepareTree(__state, hit);
            }
        }
    }

    /// <summary>
    /// What a felled tree gives on top.
    /// </summary>
    /// <param name="__instance">The tree.</param>
    /// <param name="__state">The context.</param>
    [HarmonyPatch(typeof(TreeBase), nameof(TreeBase.RPC_Damage))]
    [HarmonyFinalizer]
    private static void AfterTreeHit(TreeBase __instance, Gathering.Context __state)
    {
        if (__state == null)
        {
            return;
        }

        Gathering.End();
        if (__state.Felled)
        {
            Gathering.FinishTree(__state, __instance);
        }
    }

    /// <summary>
    /// Lets the tree fall the way the player faces.
    /// </summary>
    /// <param name="hitDir">Direction of the push.</param>
    [HarmonyPatch(typeof(TreeBase), nameof(TreeBase.SpawnLog))]
    [HarmonyPrefix]
    private static void AimFall(ref Vector3 hitDir)
    {
        Gathering.Context context = Gathering.Current;
        if (context != null)
        {
            context.Felled = true;
            hitDir = context.FallDirection;
        }
    }

    /// <summary>
    /// Marks a new log as old growth, and lets it knock down trees with Domino Felling.
    /// </summary>
    /// <param name="__instance">The log.</param>
    [HarmonyPatch(typeof(TreeLog), nameof(TreeLog.Awake))]
    [HarmonyPostfix]
    private static void MarkLog(TreeLog __instance)
    {
        Gathering.Context context = Gathering.Current;
        ZNetView nview = __instance.GetComponent<ZNetView>();
        if (context == null || nview == null || !nview.IsValid() || !nview.IsOwner())
        {
            return;
        }

        if (context.OldGrowth)
        {
            nview.GetZDO().Set(Gathering.OldGrowthKey, true);
        }

        if (context.Domino)
        {
            __instance.gameObject.AddComponent<DominoLog>().Setup(context.Attacker.GetZDOID());
        }
    }

    /// <summary>
    /// Watches a hit on a log.
    /// </summary>
    /// <param name="__instance">The log.</param>
    /// <param name="hit">The hit.</param>
    /// <param name="__state">The context.</param>
    [HarmonyPatch(typeof(TreeLog), nameof(TreeLog.RPC_Damage))]
    [HarmonyPrefix]
    private static void BeforeLogHit(TreeLog __instance, HitData hit, out Gathering.Context __state)
    {
        __state = null;
        if (MasterySettings.Woodcutting.Value && __instance.m_nview != null && __instance.m_nview.IsOwner())
        {
            __state = Gathering.Begin(hit, Skills.SkillType.WoodCutting);
            if (__state != null)
            {
                // Logs split from an old growth log are old growth too.
                __state.OldGrowth = __instance.m_nview.GetZDO().GetBool(Gathering.OldGrowthKey);
            }
        }
    }

    /// <summary>
    /// Clean splits and old growth wood.
    /// </summary>
    /// <param name="__state">The context.</param>
    [HarmonyPatch(typeof(TreeLog), nameof(TreeLog.RPC_Damage))]
    [HarmonyFinalizer]
    private static void AfterLogHit(Gathering.Context __state)
    {
        if (__state != null)
        {
            Gathering.End();
            Gathering.FinishWood(__state, __state.OldGrowth);
        }
    }

    /// <summary>
    /// Watches a hit on a stump or a small rock.
    /// </summary>
    /// <param name="__instance">The destructible.</param>
    /// <param name="hit">The hit.</param>
    /// <param name="__state">The context.</param>
    [HarmonyPatch(typeof(Destructible), nameof(Destructible.RPC_Damage))]
    [HarmonyPrefix]
    private static void BeforeDestructibleHit(Destructible __instance, HitData hit, out Gathering.Context __state)
    {
        __state = null;
        bool wood = hit.m_skill == Skills.SkillType.WoodCutting && MasterySettings.Woodcutting.Value;
        bool rock = hit.m_skill == Skills.SkillType.Pickaxes && MasterySettings.Mining.Value;
        if ((wood || rock) && __instance.m_nview != null && __instance.m_nview.IsOwner() && Gathering.Current == null)
        {
            __state = Gathering.Begin(hit, hit.m_skill);
        }
    }

    /// <summary>
    /// Extra wood, ore and finds from a broken stump or rock.
    /// </summary>
    /// <param name="__instance">The destructible.</param>
    /// <param name="__state">The context.</param>
    [HarmonyPatch(typeof(Destructible), nameof(Destructible.RPC_Damage))]
    [HarmonyFinalizer]
    private static void AfterDestructibleHit(Destructible __instance, Gathering.Context __state)
    {
        if (__state == null)
        {
            return;
        }

        Gathering.End();
        if (__state.Skill == Skills.SkillType.WoodCutting)
        {
            Gathering.FinishWood(__state, oldGrowth: false);
        }
        else
        {
            Gathering.FinishRock(__state, rich: false, __instance.transform.position);
        }
    }

    /// <summary>
    /// Watches a hit on part of a large rock.
    /// </summary>
    /// <param name="__instance">The rock.</param>
    /// <param name="hit">The hit.</param>
    /// <param name="__state">The context.</param>
    [HarmonyPatch(typeof(MineRock5), nameof(MineRock5.DamageArea))]
    [HarmonyPrefix]
    private static void BeforeMineRock5Hit(MineRock5 __instance, HitData hit, out Gathering.Context __state)
    {
        __state = MasterySettings.Mining.Value && __instance.m_nview != null && __instance.m_nview.IsOwner()
            ? Gathering.Begin(hit, Skills.SkillType.Pickaxes)
            : null;
    }

    /// <summary>
    /// Extra ore, rich veins and finds from a broken part of a large rock.
    /// </summary>
    /// <param name="__instance">The rock.</param>
    /// <param name="hit">The hit.</param>
    /// <param name="__state">The context.</param>
    [HarmonyPatch(typeof(MineRock5), nameof(MineRock5.DamageArea))]
    [HarmonyFinalizer]
    private static void AfterMineRock5Hit(MineRock5 __instance, HitData hit, Gathering.Context __state)
    {
        if (__state == null)
        {
            return;
        }

        Gathering.End();
        if (__state.Drops.Count > 0)
        {
            Gathering.FinishRock(__state, Gathering.IsRich(__instance.m_nview, __state), hit.m_point);
        }
    }

    /// <summary>
    /// Watches a hit on a rock with separate parts, such as obsidian.
    /// </summary>
    /// <param name="__instance">The rock.</param>
    /// <param name="hit">The hit.</param>
    /// <param name="__state">The context.</param>
    [HarmonyPatch(typeof(MineRock), nameof(MineRock.RPC_Hit))]
    [HarmonyPrefix]
    private static void BeforeMineRockHit(MineRock __instance, HitData hit, out Gathering.Context __state)
    {
        __state = MasterySettings.Mining.Value && __instance.m_nview != null && __instance.m_nview.IsOwner()
            ? Gathering.Begin(hit, Skills.SkillType.Pickaxes)
            : null;
    }

    /// <summary>
    /// Extra ore, rich veins and finds from a broken part of a rock.
    /// </summary>
    /// <param name="__instance">The rock.</param>
    /// <param name="hit">The hit.</param>
    /// <param name="__state">The context.</param>
    [HarmonyPatch(typeof(MineRock), nameof(MineRock.RPC_Hit))]
    [HarmonyFinalizer]
    private static void AfterMineRockHit(MineRock __instance, HitData hit, Gathering.Context __state)
    {
        if (__state == null)
        {
            return;
        }

        Gathering.End();
        if (__state.Drops.Count > 0)
        {
            Gathering.FinishRock(__state, Gathering.IsRich(__instance.m_nview, __state), hit.m_point);
        }
    }
}
