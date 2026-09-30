using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Helpers;

/// <summary>
/// Animated creature visuals from the asset bundle (built from AssetSource/Creatures by BuildCreatures.cs): the rigged
/// model with its Animator, whose Standard materials are swapped for copies of a vanilla creature material.
/// </summary>
public static class CreatureVisual
{
    private static readonly string[] clearedMaps = { "_EmissionMap", "_MetallicGlossMap", "_OcclusionMap", "_DetailAlbedoMap", "_DetailNormalMap" };
    private static readonly Dictionary<(Material Source, Material Template), Material> materials = new();

    /// <summary>
    /// Adds a creature visual under <paramref name="parent"/>.
    /// </summary>
    /// <param name="parent">Parent, e.g. a creature's Visual child.</param>
    /// <param name="creature">Creature name in the bundle (the visual prefab is &lt;creature&gt;_visual).</param>
    /// <param name="template">Vanilla material to copy the shader from; null keeps the bundle materials.</param>
    /// <param name="localPosition">Position of the model's origin (its feet).</param>
    /// <param name="localRotation">Rotation; the model looks along +z.</param>
    /// <param name="scale">Uniform scale.</param>
    /// <returns>The visual's root. Its Animator sits on the first child.</returns>
    public static GameObject Attach(Transform parent, string creature, Material template, Vector3 localPosition, Quaternion localRotation, float scale)
    {
        GameObject visual = Object.Instantiate(ForagingAssets.LoadPrefab($"{creature}_visual"), parent, false);
        visual.name = $"{creature}_visual";
        visual.transform.localPosition = localPosition;
        visual.transform.localRotation = localRotation;
        visual.transform.localScale = Vector3.one * scale;
        foreach (Transform child in visual.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.layer = parent.gameObject.layer;
        }

        if (!VisualHelper.IsHeadless && template != null)
        {
            foreach (SkinnedMeshRenderer renderer in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                renderer.sharedMaterials = renderer.sharedMaterials.Select(source => Convert(source, template)).ToArray();
            }
        }

        return visual;
    }

    /// <summary>
    /// The first material of a vanilla creature's skinned renderer, to use as <c>template</c>.
    /// </summary>
    /// <param name="prefab">Vanilla creature prefab.</param>
    /// <returns>The material, or null.</returns>
    public static Material TemplateOf(GameObject prefab)
    {
        return prefab == null ? null : prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(renderer => renderer.sharedMaterial).FirstOrDefault(material => material != null);
    }

    private static Material Convert(Material source, Material template)
    {
        if (source == null)
        {
            return template;
        }

        if (materials.TryGetValue((source, template), out Material cached) && cached != null)
        {
            return cached;
        }

        Material material = new(template) { name = source.name };
        foreach (string map in clearedMaps.Where(material.HasProperty))
        {
            material.SetTexture(map, null);
        }

        material.mainTexture = source.mainTexture != null ? source.mainTexture : Texture2D.whiteTexture;
        if (material.HasProperty("_BumpMap"))
        {
            material.SetTexture("_BumpMap", source.HasProperty("_BumpMap") ? source.GetTexture("_BumpMap") : null);
        }

        if (material.HasProperty("_Color"))
        {
            material.color = source.color;
        }

        if (material.HasProperty("_EmissionColor"))
        {
            bool glows = source.IsKeywordEnabled("_EMISSION");
            material.SetColor("_EmissionColor", glows ? source.GetColor("_EmissionColor") : Color.black);
            if (glows)
            {
                material.EnableKeyword("_EMISSION");
                if (material.HasProperty("_EmissionMap"))
                {
                    material.SetTexture("_EmissionMap", Texture2D.whiteTexture);
                }
            }
        }

        materials[(source, template)] = material;
        return material;
    }
}
