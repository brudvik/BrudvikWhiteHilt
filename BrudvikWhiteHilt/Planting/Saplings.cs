using BrudvikWhiteHilt.Helpers;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Planting;

/// <summary>
/// Saplings cloned from the vanilla birch sapling that grow into other vanilla trees. They borrow the meshes and materials
/// of those trees, so nothing of their own is needed.
/// </summary>
public static class Saplings
{
    private const string Template = "Birch_Sapling";
    private const string LeafPrefix = "birchleafs";
    private const float AshwoodScale = 0.06f;

    private static readonly string[] states = { "healthy", "unhealthy" };
    private static readonly HashSet<string> dressed = new();
    private static readonly Heightmap.Biome allBiomes = Enum.GetValues(typeof(Heightmap.Biome)).Cast<Heightmap.Biome>()
        .Aggregate(Heightmap.Biome.None, (all, biome) => all | biome);

    /// <summary>
    /// Creates the sapling prefabs. Call once from <c>PrefabManager.OnVanillaPrefabsAvailable</c>.
    /// </summary>
    public static void Create()
    {
        if (Plantables.OtherModInstalled())
        {
            return;
        }

        foreach (SaplingDefinition sapling in PlantingSettings.Definitions.OfType<SaplingDefinition>())
        {
            try
            {
                GameObject clone = PrefabManager.Instance.CreateClonedPrefab(sapling.Prefab, Template);
                if (clone != null)
                {
                    PrefabManager.Instance.AddPrefab(clone);
                }
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogError($"Planting: could not create {sapling.Prefab}.");
                Jotunn.Logger.LogError(ex);
            }
        }
    }

    /// <summary>
    /// Sets how the sapling grows and what it grows into, and gives it its look the first time. Runs even when the sapling
    /// is left out of the cultivator, so saplings already in the world keep growing into the right tree.
    /// </summary>
    /// <param name="sapling">The sapling definition.</param>
    /// <param name="piece">The sapling's piece.</param>
    /// <returns>False if the trees it grows into are missing.</returns>
    public static bool Configure(SaplingDefinition sapling, Piece piece)
    {
        Plant plant = piece.GetComponent<Plant>();
        if (plant == null)
        {
            return false;
        }

        GameObject[] trees = sapling.GrownPrefabs.Select(name => ZNetScene.instance.GetPrefab(name)).Where(tree => tree != null).ToArray();
        if (trees.Length == 0)
        {
            return false;
        }

        bool anyBiome = PlantingSettings.SaplingsAnyBiome.Value;
        plant.m_name = piece.m_name;
        plant.m_grownPrefabs = trees;
        plant.m_growTime = sapling.GrowthTime.Value;
        plant.m_growTimeMax = sapling.GrowthTime.Value;
        plant.m_minScale = Mathf.Min(sapling.MinScale.Value, sapling.MaxScale.Value);
        plant.m_maxScale = Mathf.Max(sapling.MinScale.Value, sapling.MaxScale.Value);
        plant.m_growRadius = sapling.GrowRadius.Value;
        plant.m_biome = anyBiome ? allBiomes : sapling.Biomes;
        piece.m_onlyInBiome = anyBiome ? Heightmap.Biome.None : sapling.Biomes;
        if (sapling.TolerateHeat)
        {
            plant.m_tolerateHeat = true;
        }

        if (!VisualHelper.IsHeadless && dressed.Add(sapling.Prefab))
        {
            Dress(sapling, piece.gameObject);
        }

        return true;
    }

    // Dresses a birch sapling clone as another tree: bark, autumn leaves, a Yggdrasil shoot or ashwood, borrowed from
    // the vanilla trees.
    private static void Dress(SaplingDefinition sapling, GameObject prefab)
    {
        Transform healthy = prefab.transform.Find(states[0]);
        Material trunk = healthy?.Find(Template)?.GetComponent<MeshRenderer>()?.sharedMaterial;
        if (trunk == null)
        {
            Jotunn.Logger.LogWarning($"Planting: {sapling.Prefab} has no birch trunk to dress, it keeps the birch look.");
            return;
        }

        switch (sapling.Look)
        {
            case SaplingLook.AncientBark:
                Material bark = WithShader(Renderer("SwampTree1", "swamptree1")?.sharedMaterials.FirstOrDefault(), trunk.shader);
                ForEachState(prefab, state =>
                {
                    RemoveLeaves(state);
                    SetMaterial(state.Find(Template), bark);
                });
                break;

            case SaplingLook.AutumnLeaves:
                SetLeaves(healthy, Renderer("Birch1_aut", "Lod0")?.sharedMaterials.FirstOrDefault());
                break;

            case SaplingLook.YggaShoot:
                Material[] shoot = Renderer("YggaShoot_small1", "beech")?.sharedMaterials;
                if (shoot != null && shoot.Length > 1)
                {
                    ForEachState(prefab, state => SetMaterial(state.Find(Template), shoot[1]));
                    SetLeaves(healthy, shoot[0]);
                }

                break;

            case SaplingLook.Ashwood:
                MeshRenderer tree = Renderer("AshlandsTree3", "default");
                Mesh mesh = tree?.GetComponent<MeshFilter>()?.sharedMesh;
                Material wood = WithShader(tree?.sharedMaterials.FirstOrDefault(), trunk.shader);
                if (mesh == null || wood == null)
                {
                    break;
                }

                if (tree.sharedMaterials[0].HasProperty("_EmissiveTex") && wood.HasProperty("_EmissionMap"))
                {
                    wood.SetTexture("_EmissionMap", tree.sharedMaterials[0].GetTexture("_EmissiveTex"));
                    wood.EnableKeyword("_EMISSION");
                }

                ForEachState(prefab, state =>
                {
                    RemoveLeaves(state);
                    Transform part = state.Find(Template);
                    if (part == null)
                    {
                        return;
                    }

                    part.GetComponent<MeshFilter>().sharedMesh = mesh;
                    part.localScale = Vector3.one * (AshwoodScale / Mathf.Max(0.01f, state.localScale.x));
                    SetMaterial(part, wood);
                });
                break;
        }
    }

    private static MeshRenderer Renderer(string prefab, string child)
    {
        return ZNetScene.instance.GetPrefab(prefab)?.transform.Find(child)?.GetComponent<MeshRenderer>();
    }

    // A copy, so the vanilla tree keeps its own material.
    private static Material WithShader(Material source, Shader shader)
    {
        if (source == null)
        {
            return null;
        }

        return new Material(source) { shader = shader };
    }

    private static void ForEachState(GameObject prefab, Action<Transform> action)
    {
        foreach (string name in states)
        {
            Transform state = prefab.transform.Find(name);
            if (state != null)
            {
                action(state);
            }
        }
    }

    private static void RemoveLeaves(Transform state)
    {
        foreach (Transform leaf in state.Cast<Transform>().Where(child => child.name.StartsWith(LeafPrefix)).ToList())
        {
            UnityEngine.Object.DestroyImmediate(leaf.gameObject);
        }
    }

    private static void SetLeaves(Transform state, Material material)
    {
        if (state == null || material == null)
        {
            return;
        }

        foreach (Transform leaf in state.Cast<Transform>().Where(child => child.name.StartsWith(LeafPrefix)))
        {
            SetMaterial(leaf, material);
        }
    }

    private static void SetMaterial(Transform part, Material material)
    {
        MeshRenderer renderer = part != null ? part.GetComponent<MeshRenderer>() : null;
        if (renderer != null && material != null)
        {
            renderer.sharedMaterials = new[] { material };
        }
    }
}
