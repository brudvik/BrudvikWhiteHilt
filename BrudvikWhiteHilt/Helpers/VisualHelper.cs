using Jotunn.Managers;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace BrudvikWhiteHilt.Helpers;

/// <summary>
/// Changes the look of cloned vanilla prefabs at runtime.
/// </summary>
public static class VisualHelper
{
    private static readonly string[] unusedTextureProperties = { "_BumpMap", "_EmissionMap", "_MetallicGlossMap", "_OcclusionMap" };

    /// <summary>
    /// True on a dedicated server, which has no graphics device and never shows any visuals.
    /// </summary>
    public static bool IsHeadless => SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;

    /// <summary>
    /// Hides the existing meshes under <paramref name="visualRoot"/> and shows <paramref name="mesh"/> in their place,
    /// scaled to the same height and standing on the same base.
    /// </summary>
    /// <param name="visualRoot">Object whose meshes are replaced.</param>
    /// <param name="mesh">Replacement mesh, with its pivot at the base.</param>
    /// <param name="texture">Albedo texture for the replacement mesh.</param>
    /// <param name="hang">Fit the width instead of the height and keep the top in place, for a model that hangs where the old one hung.</param>
    public static void ReplaceMesh(GameObject visualRoot, Mesh mesh, Texture2D texture, bool hang = false)
    {
        Transform root = visualRoot.transform;
        MeshRenderer[] renderers = visualRoot.GetComponentsInChildren<MeshRenderer>(true)
            .Where(renderer => renderer.enabled && IsActiveBelow(renderer.transform, root) && renderer.GetComponent<MeshFilter>()?.sharedMesh != null)
            .ToArray();
        if (renderers.Length == 0)
        {
            throw new InvalidOperationException($"{visualRoot.name} has no mesh to replace.");
        }

        Bounds target = GetLocalBounds(root, renderers);

        Material material = new(renderers[0].sharedMaterial) { name = $"{mesh.name}_material" };
        material.mainTexture = texture;
        if (material.HasProperty("_Color"))
        {
            material.color = Color.white;
        }

        foreach (string property in unusedTextureProperties.Where(material.HasProperty))
        {
            material.SetTexture(property, null);
        }

        foreach (MeshRenderer renderer in renderers)
        {
            renderer.enabled = false;
        }

        float scale = hang
            ? Mathf.Max(target.size.x, target.size.z) / Mathf.Max(mesh.bounds.size.x, mesh.bounds.size.z)
            : target.size.y / mesh.bounds.size.y;
        Vector3 meshBase = new(mesh.bounds.center.x, hang ? mesh.bounds.max.y : mesh.bounds.min.y, mesh.bounds.center.z);
        Vector3 targetBase = new(target.center.x, hang ? target.max.y : target.min.y, target.center.z);

        GameObject model = new($"{mesh.name}_model") { layer = renderers[0].gameObject.layer };
        model.transform.SetParent(root, false);
        model.transform.localScale = Vector3.one * scale;
        model.transform.localPosition = targetBase - meshBase * scale;
        model.AddComponent<MeshFilter>().sharedMesh = mesh;

        MeshRenderer modelRenderer = model.AddComponent<MeshRenderer>();
        modelRenderer.sharedMaterial = material;
        modelRenderer.shadowCastingMode = renderers[0].shadowCastingMode;
        modelRenderer.receiveShadows = renderers[0].receiveShadows;
    }

    /// <summary>
    /// Multiplies the colour of every mesh material under <paramref name="root"/> with <paramref name="tint"/>.
    /// </summary>
    /// <param name="root">Object to tint.</param>
    /// <param name="tint">Tint colour.</param>
    public static void Tint(GameObject root, Color tint)
    {
        if (IsHeadless)
        {
            return;
        }

        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true).Where(IsMeshRenderer))
        {
            renderer.sharedMaterials = renderer.sharedMaterials
                .Select(source =>
                {
                    if (source == null || !source.HasProperty("_Color"))
                    {
                        return source;
                    }

                    Material material = new(source);
                    material.color = source.color * tint;
                    return material;
                })
                .ToArray();
        }
    }

    /// <summary>
    /// Replaces the main texture of every mesh material under <paramref name="root"/> with a recoloured copy.
    /// </summary>
    /// <param name="root">Object to recolour.</param>
    /// <param name="recolor">Returns the new colour of a pixel.</param>
    public static void Recolor(GameObject root, Func<Color32, Color32> recolor)
    {
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true).Where(IsMeshRenderer))
        {
            renderer.sharedMaterials = renderer.sharedMaterials
                .Select(source =>
                {
                    if (source == null || source.mainTexture == null)
                    {
                        return source;
                    }

                    Material material = new(source) { mainTexture = RecolorTexture(source.mainTexture, recolor) };
                    return material;
                })
                .ToArray();
        }
    }

    /// <summary>
    /// Moves every clearly coloured pixel with a hue in [<paramref name="hueMin"/>, <paramref name="hueMax"/>] to <paramref name="newHue"/>.
    /// Greys, dark pixels and other hues are left alone, so leaves and stems keep their colour.
    /// </summary>
    /// <param name="root">Object to recolour.</param>
    /// <param name="hueMin">Lowest hue to replace, from 0 to 1.</param>
    /// <param name="hueMax">Highest hue to replace, from 0 to 1.</param>
    /// <param name="newHue">Hue of the replaced pixels, from 0 to 1.</param>
    /// <param name="saturationScale">Multiplier for the saturation of the replaced pixels.</param>
    /// <param name="valueScale">Multiplier for the brightness of the replaced pixels.</param>
    public static void RecolorHue(GameObject root, float hueMin, float hueMax, float newHue, float saturationScale, float valueScale)
    {
        Recolor(root, pixel =>
        {
            Color.RGBToHSV(pixel, out float hue, out float saturation, out float value);
            if (hue < hueMin || hue > hueMax || saturation < 0.25f || value < 0.1f)
            {
                return pixel;
            }

            Color32 result = Color.HSVToRGB(newHue, Mathf.Clamp01(saturation * saturationScale), Mathf.Clamp01(value * valueScale));
            result.a = pixel.a;
            return result;
        });
    }

    /// <summary>
    /// Renders an inventory icon of a prefab.
    /// </summary>
    /// <param name="prefab">Prefab to render.</param>
    /// <returns>The icon, or null if the prefab has nothing visible to render.</returns>
    public static Sprite RenderIcon(GameObject prefab)
    {
        if (IsHeadless)
        {
            return null;
        }

        Sprite icon = RenderManager.Instance.Render(prefab, RenderManager.IsometricRotation);
        if (icon == null)
        {
            Jotunn.Logger.LogWarning($"Could not render an icon for {prefab.name}, keeping the original icon.");
        }

        return icon;
    }

    private static bool IsMeshRenderer(Renderer renderer)
    {
        return renderer is MeshRenderer || renderer is SkinnedMeshRenderer;
    }

    // activeInHierarchy is always false inside Jotunn's disabled prefab container, so walk up to the root instead.
    private static bool IsActiveBelow(Transform transform, Transform root)
    {
        for (Transform current = transform; current != null && current != root; current = current.parent)
        {
            if (!current.gameObject.activeSelf)
            {
                return false;
            }
        }

        return true;
    }

    private static Bounds GetLocalBounds(Transform root, MeshRenderer[] renderers)
    {
        Bounds bounds = default;
        bool first = true;
        foreach (MeshRenderer renderer in renderers)
        {
            Bounds meshBounds = renderer.GetComponent<MeshFilter>().sharedMesh.bounds;
            Matrix4x4 toRoot = root.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 local = meshBounds.center + Vector3.Scale(meshBounds.extents, new Vector3(
                    (corner & 1) == 0 ? -1 : 1,
                    (corner & 2) == 0 ? -1 : 1,
                    (corner & 4) == 0 ? -1 : 1));
                Vector3 point = toRoot.MultiplyPoint3x4(local);
                if (first)
                {
                    bounds = new Bounds(point, Vector3.zero);
                    first = false;
                }
                else
                {
                    bounds.Encapsulate(point);
                }
            }
        }

        return bounds;
    }

    private static Texture2D RecolorTexture(Texture source, Func<Color32, Color32> recolor)
    {
        // Game textures are not CPU-readable, so copy through a render texture first.
        RenderTexture renderTexture = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        RenderTexture previous = RenderTexture.active;
        try
        {
            Graphics.Blit(source, renderTexture);
            RenderTexture.active = renderTexture;

            Texture2D copy = new(source.width, source.height, TextureFormat.RGBA32, mipChain: true, linear: false)
            {
                name = $"{source.name}_recolored",
                wrapMode = source.wrapMode,
                filterMode = source.filterMode
            };
            copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            copy.SetPixels32(copy.GetPixels32().Select(recolor).ToArray());
            copy.Apply(updateMipmaps: true, makeNoLongerReadable: true);
            return copy;
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTexture);
        }
    }
}
