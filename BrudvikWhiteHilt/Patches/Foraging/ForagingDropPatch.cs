using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.Foraging;

/// <summary>
/// Lets vanilla pickables drop White Hilt forageables, depending on the biome they grow in.
/// </summary>
[HarmonyPatch(typeof(Pickable), nameof(Pickable.RPC_Pick))]
public static class ForagingDropPatch
{
    private static readonly List<ExtraDrop> extraDrops = new();

    /// <summary>
    /// Adds an extra drop to a vanilla pickable.
    /// </summary>
    /// <param name="pickableName">Prefab name of the vanilla pickable.</param>
    /// <param name="biome">Biomes where the extra drop can happen.</param>
    /// <param name="itemName">Prefab name of the dropped item.</param>
    /// <param name="chance">Returns the chance per pick, from 0 to 1. Read on every pick, so config changes apply at once.</param>
    public static void Register(string pickableName, Heightmap.Biome biome, string itemName, Func<float> chance)
    {
        extraDrops.Add(new ExtraDrop(pickableName, biome, itemName, chance));
    }

    [HarmonyPrefix]
    private static void Prefix(Pickable __instance, out bool __state)
    {
        // RPC_Pick only drops anything on the owner, and only if the pickable was not picked yet.
        __state = __instance.m_nview != null && __instance.m_nview.IsValid() && __instance.m_nview.IsOwner() && !__instance.m_picked;
    }

    // After a pickable is picked: drops the extra items registered for it in this biome, each by its own chance.
    [HarmonyPostfix]
    private static void Postfix(Pickable __instance, bool __state)
    {
        if (!__state || extraDrops.Count == 0 || ZNetScene.instance == null)
        {
            return;
        }

        string pickableName = global::Utils.GetPrefabName(__instance.gameObject);
        Heightmap.Biome biome = Heightmap.FindBiome(__instance.transform.position);
        int offset = 1;
        foreach (ExtraDrop drop in extraDrops)
        {
            if (drop.PickableName != pickableName || (drop.Biome & biome) == 0 || UnityEngine.Random.value > drop.Chance())
            {
                continue;
            }

            GameObject itemPrefab = ZNetScene.instance.GetPrefab(drop.ItemName);
            if (itemPrefab != null)
            {
                __instance.Drop(itemPrefab, offset++, 1);
            }
        }
    }

    private sealed class ExtraDrop
    {
        public ExtraDrop(string pickableName, Heightmap.Biome biome, string itemName, Func<float> chance)
        {
            PickableName = pickableName;
            Biome = biome;
            ItemName = itemName;
            Chance = chance;
        }

        public string PickableName { get; }

        public Heightmap.Biome Biome { get; }

        public string ItemName { get; }

        public Func<float> Chance { get; }
    }
}
