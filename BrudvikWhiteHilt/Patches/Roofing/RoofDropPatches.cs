using BrudvikWhiteHilt.Items.Roofing;
using BrudvikWhiteHilt.Pieces.Roofs;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.Roofing;

/// <summary>
/// Extra drops for the roof materials: birch bark from felled birches and their logs, slate and soapstone from
/// Mountain rocks. The vanilla object that is breaking is remembered while its drop table is rolled.
/// </summary>
public static class RoofDropPatches
{
    private static GameObject breaking;

    private static void Enter(Component component)
    {
        breaking = component != null ? component.gameObject : null;
    }

    private static Exception Leave(Exception __exception)
    {
        breaking = null;
        return __exception;
    }

    [HarmonyPatch(typeof(TreeLog), "Destroy", new[] { typeof(HitData), typeof(bool) })]
    private static class TreeLogDestroy
    {
        private static void Prefix(TreeLog __instance) => Enter(__instance);

        private static Exception Finalizer(Exception __exception) => Leave(__exception);
    }

    [HarmonyPatch(typeof(TreeBase), "RPC_Damage")]
    private static class TreeBaseDamage
    {
        private static void Prefix(TreeBase __instance) => Enter(__instance);

        private static Exception Finalizer(Exception __exception) => Leave(__exception);
    }

    [HarmonyPatch(typeof(MineRock), "RPC_Hit")]
    private static class MineRockHit
    {
        private static void Prefix(MineRock __instance) => Enter(__instance);

        private static Exception Finalizer(Exception __exception) => Leave(__exception);
    }

    [HarmonyPatch(typeof(MineRock5), "DamageArea")]
    private static class MineRock5Damage
    {
        private static void Prefix(MineRock5 __instance) => Enter(__instance);

        private static Exception Finalizer(Exception __exception) => Leave(__exception);
    }

    [HarmonyPatch(typeof(DropOnDestroyed), "OnDestroyed")]
    private static class DropOnDestroyedDrops
    {
        private static void Prefix(DropOnDestroyed __instance) => Enter(__instance);

        private static Exception Finalizer(Exception __exception) => Leave(__exception);
    }

    [HarmonyPatch(typeof(DropTable), nameof(DropTable.GetDropList), new Type[0])]
    private static class AddRoofMaterials
    {
        // Adds roofing materials to what breaks: birch bark from birches, slate and soapstone from mountain rock.
        private static void Postfix(List<GameObject> __result)
        {
            if (breaking == null || __result == null || __result.Count == 0 || ZNetScene.instance == null)
            {
                return;
            }

            string name = global::Utils.GetPrefabName(breaking);
            if (name.StartsWith("Birch", StringComparison.OrdinalIgnoreCase))
            {
                AddBirchBark(__result);
            }
            else if (!name.StartsWith(RoofMaterials.OutcropName, StringComparison.Ordinal)
                && (Heightmap.FindBiome(breaking.transform.position) & Heightmap.Biome.Mountain) != 0)
            {
                AddStones(__result);
            }
        }

        private static void AddBirchBark(List<GameObject> drops)
        {
            if (!drops.Exists(drop => drop != null && drop.name == "Wood") || UnityEngine.Random.value >= RoofSettings.BirchBarkChance.Value)
            {
                return;
            }

            GameObject bark = ZNetScene.instance.GetPrefab(RoofMaterials.BirchBark);
            int count = UnityEngine.Random.Range(1, Mathf.Max(1, RoofSettings.BirchBarkMax.Value) + 1);
            for (int i = 0; bark != null && i < count; i++)
            {
                drops.Add(bark);
            }
        }

        // For each stone dropped, a chance of slate and of soapstone as well.
        private static void AddStones(List<GameObject> drops)
        {
            int stones = drops.FindAll(drop => drop != null && drop.name == "Stone").Count;
            GameObject slate = ZNetScene.instance.GetPrefab(RoofMaterials.Slate);
            GameObject soapstone = ZNetScene.instance.GetPrefab(RoofMaterials.Soapstone);
            for (int i = 0; i < stones; i++)
            {
                if (slate != null && UnityEngine.Random.value < RoofSettings.SlateChance.Value)
                {
                    drops.Add(slate);
                }

                if (soapstone != null && UnityEngine.Random.value < RoofSettings.SoapstoneChance.Value)
                {
                    drops.Add(soapstone);
                }
            }
        }
    }

    // Digging grassland with a pickaxe cuts a turf, as long as the sod is still there.
    [HarmonyPatch(typeof(Attack), nameof(Attack.SpawnOnHitTerrain))]
    private static class CutTurf
    {
        // Digging near the surface of a turf biome with a pickaxe sometimes cuts a turf; not under water, deep down or
        // in tilled soil.
        private static void Postfix(GameObject __result, Vector3 hitPoint, Character character, ItemDrop.ItemData weapon)
        {
            if (__result == null || character == null || character != Player.m_localPlayer || weapon?.m_shared == null
                || weapon.m_shared.m_skillType != Skills.SkillType.Pickaxes || ZNetScene.instance == null)
            {
                return;
            }

            if ((Heightmap.FindBiome(hitPoint) & RoofSettings.TurfBiomes.Value) == 0 || UnityEngine.Random.value >= RoofSettings.TurfChance.Value)
            {
                return;
            }

            if (ZoneSystem.instance != null && hitPoint.y < ZoneSystem.instance.m_waterLevel)
            {
                return;
            }

            if (WorldGenerator.instance != null && WorldGenerator.instance.GetHeight(hitPoint.x, hitPoint.z) - hitPoint.y > RoofSettings.TurfMaxDepth.Value)
            {
                return;
            }

            Heightmap heightmap = Heightmap.FindHeightmap(hitPoint);
            if (heightmap != null && heightmap.IsCultivated(hitPoint))
            {
                return;
            }

            GameObject turf = ZNetScene.instance.GetPrefab(RoofMaterials.Turf);
            if (turf != null)
            {
                ItemDrop.OnCreateNew(UnityEngine.Object.Instantiate(turf, hitPoint + Vector3.up * 0.4f, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f)));
            }
        }
    }
}
