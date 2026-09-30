using BrudvikWhiteHilt.Difficulty;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.Difficulty;

/// <summary>
/// Hooks creature stars into the game: the three spawn paths, level-up chances, levels, health, damage, loot,
/// level visuals, ragdoll size and the star row over creatures.
/// </summary>
[HarmonyPatch]
public static class CreatureLevelPatches
{
    [HarmonyPatch(typeof(SpawnSystem), nameof(SpawnSystem.Spawn))]
    [HarmonyPrefix]
    private static void SpawnSystemSpawnPrefix(bool eventSpawner)
    {
        CreatureStars.BeginSpawn(eventSpawner);
    }

    [HarmonyPatch(typeof(SpawnSystem), nameof(SpawnSystem.Spawn))]
    [HarmonyFinalizer]
    private static Exception SpawnSystemSpawnFinalizer(Exception __exception)
    {
        CreatureStars.EndSpawn();
        return __exception;
    }

    [HarmonyPatch(typeof(CreatureSpawner), nameof(CreatureSpawner.Spawn))]
    [HarmonyPrefix]
    private static void CreatureSpawnerSpawnPrefix()
    {
        CreatureStars.BeginSpawn(false);
    }

    [HarmonyPatch(typeof(CreatureSpawner), nameof(CreatureSpawner.Spawn))]
    [HarmonyFinalizer]
    private static Exception CreatureSpawnerSpawnFinalizer(Exception __exception)
    {
        CreatureStars.EndSpawn();
        return __exception;
    }

    [HarmonyPatch(typeof(SpawnArea), nameof(SpawnArea.SpawnOne))]
    [HarmonyPrefix]
    private static void SpawnAreaSpawnOnePrefix()
    {
        CreatureStars.BeginSpawn(false);
    }

    [HarmonyPatch(typeof(SpawnArea), nameof(SpawnArea.SpawnOne))]
    [HarmonyFinalizer]
    private static Exception SpawnAreaSpawnOneFinalizer(Exception __exception)
    {
        CreatureStars.EndSpawn();
        return __exception;
    }

    [HarmonyPatch(typeof(SpawnSystem), nameof(SpawnSystem.GetLevelUpChance), new[] { typeof(Vector3), typeof(float) })]
    [HarmonyPostfix]
    private static void SpawnSystemLevelUpChancePostfix(ref float __result)
    {
        __result *= CreatureStars.LevelUpChanceMultiplier();
    }

    [HarmonyPatch(typeof(SpawnArea), nameof(SpawnArea.GetLevelUpChance))]
    [HarmonyPostfix]
    private static void SpawnAreaLevelUpChancePostfix(ref float __result)
    {
        __result *= CreatureStars.LevelUpChanceMultiplier();
    }

    [HarmonyPatch(typeof(Character), nameof(Character.Awake))]
    [HarmonyPostfix]
    private static void CharacterAwakePostfix(Character __instance)
    {
        CreatureStars.OnAwake(__instance);
    }

    [HarmonyPatch(typeof(Character), nameof(Character.SetLevel))]
    [HarmonyPrefix]
    private static void SetLevelPrefix(Character __instance, ref int level)
    {
        CreatureStars.OnSetLevel(__instance, ref level);
    }

    [HarmonyPatch(typeof(Character), nameof(Character.SetLevel))]
    [HarmonyPostfix]
    private static void SetLevelPostfix(Character __instance)
    {
        CreatureStars.ApplySize(__instance);
    }

    [HarmonyPatch(typeof(Character), nameof(Character.SetupMaxHealth))]
    [HarmonyPostfix]
    private static void SetupMaxHealthPostfix(Character __instance)
    {
        float factor = CreatureStars.HealthFactor(__instance);
        if (factor > 0f)
        {
            __instance.SetMaxHealth(__instance.GetMaxHealthBase() * factor);
        }
    }

    [HarmonyPatch(typeof(Attack), nameof(Attack.GetLevelDamageFactor))]
    [HarmonyPostfix]
    private static void LevelDamageFactorPostfix(Attack __instance, ref float __result)
    {
        __result = CreatureStars.DamageFactor(__instance.m_character, __result);
    }

    // Vanilla multiplies loot by 2 to the power of the stars; the multiplier is passed through CreatureStars.DropFactor.
    [HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.GenerateDropList))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> GenerateDropListTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo max = AccessTools.Method(typeof(Mathf), nameof(Mathf.Max), new[] { typeof(int), typeof(int) });
        MethodInfo adjust = AccessTools.Method(typeof(CreatureStars), nameof(CreatureStars.DropFactor));
        bool patched = false;
        foreach (CodeInstruction instruction in instructions)
        {
            yield return instruction;
            if (!patched && instruction.Calls(max))
            {
                patched = true;
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Call, adjust);
            }
        }

        if (!patched)
        {
            Jotunn.Logger.LogWarning("Difficulty: could not find the loot multiplier in CharacterDrop.GenerateDropList; 3 to 5 star loot uses the vanilla formula.");
        }
    }

    [HarmonyPatch(typeof(LevelEffects), nameof(LevelEffects.SetupLevelVisualization))]
    [HarmonyPrefix]
    private static void SetupLevelVisualizationPrefix(LevelEffects __instance, ref int level)
    {
        int setups = __instance.m_levelSetups.Count;
        if (setups > 0 && level - 1 > setups)
        {
            level = setups + 1;
        }
    }

    [HarmonyPatch(typeof(LevelEffects), nameof(LevelEffects.GetColorChanges))]
    [HarmonyPostfix]
    private static void GetColorChangesPostfix(LevelEffects __instance, ref float hue, ref float saturation, ref float value)
    {
        int setups = __instance.m_levelSetups.Count;
        Character character = __instance.m_character;
        if (character == null || setups == 0 || character.GetLevel() - 1 <= setups)
        {
            return;
        }

        LevelEffects.LevelSetup setup = __instance.m_levelSetups[setups - 1];
        hue = setup.m_hue;
        saturation = setup.m_saturation;
        value = setup.m_value;
    }

    // A ragdoll that does not inherit its creature's scale would shrink back to normal size on death.
    [HarmonyPatch(typeof(EffectList), nameof(EffectList.Create))]
    [HarmonyPostfix]
    private static void EffectListCreatePostfix(Transform baseParent, GameObject[] __result)
    {
        if (baseParent == null || __result == null)
        {
            return;
        }

        StarScale scale = baseParent.GetComponent<StarScale>();
        if (scale == null || Mathf.Approximately(scale.Applied, 1f))
        {
            return;
        }

        foreach (GameObject created in __result)
        {
            if (created != null && created.GetComponent<Ragdoll>() != null && (created.transform.localScale - baseParent.localScale).sqrMagnitude > 0.0001f)
            {
                created.transform.localScale *= scale.Applied;
            }
        }
    }

    [HarmonyPatch(typeof(EnemyHud), nameof(EnemyHud.UpdateHuds))]
    [HarmonyPostfix]
    private static void UpdateHudsPostfix(EnemyHud __instance)
    {
        foreach (EnemyHud.HudData hud in __instance.m_huds.Values)
        {
            if (hud.m_character != null && hud.m_level3 != null && (hud.m_character.GetLevel() > 3 || hud.m_level3.childCount > 2))
            {
                StarHud.Apply(hud.m_level3, hud.m_character.GetLevel());
            }
        }
    }
}
