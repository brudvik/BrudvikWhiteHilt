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
    /// <param name="size">Longest side of the new model, in local units of <paramref name="visualRoot"/>. Overrides the fitting, for a model with a very different shape.</param>
    /// <returns>The new model.</returns>
    public static GameObject ReplaceMesh(GameObject visualRoot, Mesh mesh, Texture2D texture, bool hang = false, float? size = null)
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

        Vector3 meshSize = mesh.bounds.size;
        float scale = size.HasValue
            ? size.Value / Mathf.Max(meshSize.x, meshSize.y, meshSize.z)
            : hang
                ? Mathf.Max(target.size.x, target.size.z) / Mathf.Max(meshSize.x, meshSize.z)
                : target.size.y / meshSize.y;
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
        return model;
    }

    /// <summary>
    /// Adds a second mesh to a model made by <see cref="ReplaceMesh"/>, e.g. chains hanging from it.
    /// </summary>
    /// <param name="model">The model to attach to. Positions are in its mesh units, where the model is 1 high.</param>
    /// <param name="mesh">Mesh to add, with its pivot at the base.</param>
    /// <param name="texture">Albedo texture for the added mesh.</param>
    /// <param name="basePosition">Where the base of the added mesh goes.</param>
    /// <param name="height">Height of the added mesh.</param>
    /// <param name="rotation">Rotation of the added mesh around its base, or null for none.</param>
    /// <returns>The added model.</returns>
    public static GameObject AddMesh(GameObject model, Mesh mesh, Texture2D texture, Vector3 basePosition, float height, Quaternion? rotation = null)
    {
        MeshRenderer template = model.GetComponent<MeshRenderer>();
        Quaternion localRotation = rotation ?? Quaternion.identity;
        float scale = height / mesh.bounds.size.y;
        Vector3 position = basePosition - localRotation * (new Vector3(mesh.bounds.center.x, mesh.bounds.min.y, mesh.bounds.center.z) * scale);
        return CreateModel(model.transform, mesh, texture, template, position, localRotation, scale);
    }

    /// <summary>
    /// Creates a child object that shows <paramref name="mesh"/> with a copy of <paramref name="template"/>'s material.
    /// </summary>
    /// <param name="parent">Parent of the new object.</param>
    /// <param name="mesh">Mesh to show.</param>
    /// <param name="texture">Albedo texture, or null to keep the template's.</param>
    /// <param name="template">Renderer whose material, shadows and layer are copied.</param>
    /// <param name="localPosition">Position of the mesh pivot.</param>
    /// <param name="localRotation">Rotation.</param>
    /// <param name="scale">Uniform scale.</param>
    /// <returns>The new object.</returns>
    public static GameObject CreateModel(Transform parent, Mesh mesh, Texture2D texture, Renderer template, Vector3 localPosition, Quaternion localRotation, float scale)
    {
        Material material = new(template.sharedMaterial) { name = $"{mesh.name}_material" };
        if (texture != null)
        {
            material.mainTexture = texture;
            if (material.HasProperty("_Color"))
            {
                material.color = Color.white;
            }

            foreach (string property in unusedTextureProperties.Where(material.HasProperty))
            {
                material.SetTexture(property, null);
            }
        }

        GameObject created = new($"{mesh.name}_model") { layer = template.gameObject.layer };
        created.transform.SetParent(parent, false);
        created.transform.localPosition = localPosition;
        created.transform.localRotation = localRotation;
        created.transform.localScale = Vector3.one * scale;
        created.AddComponent<MeshFilter>().sharedMesh = mesh;

        MeshRenderer renderer = created.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = template.shadowCastingMode;
        renderer.receiveShadows = template.receiveShadows;
        return created;
    }

    /// <summary>
    /// Disables every mesh renderer under <paramref name="root"/>, including the inactive worn and broken looks.
    /// </summary>
    /// <param name="root">Object to hide.</param>
    /// <returns>The first renderer, to copy its material from.</returns>
    public static Renderer HideRenderers(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true).Where(IsMeshRenderer).ToArray();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = false;
        }

        return renderers.FirstOrDefault() ?? throw new InvalidOperationException($"{root.name} has no mesh renderer.");
    }

    /// <summary>
    /// Resizes the box colliders directly under <paramref name="root"/> to the bounds of <paramref name="model"/>.
    /// </summary>
    /// <param name="root">Piece root.</param>
    /// <param name="model">A model made by <see cref="ReplaceMesh"/> or <see cref="CreateModel"/>.</param>
    public static void FitBoxColliders(Transform root, GameObject model)
    {
        Mesh mesh = model.GetComponent<MeshFilter>().sharedMesh;
        Vector3 center = root.InverseTransformPoint(model.transform.TransformPoint(mesh.bounds.center));
        Vector3 size = mesh.bounds.size * model.transform.localScale.x;
        foreach (BoxCollider collider in root.GetComponentsInChildren<BoxCollider>(true))
        {
            if (collider.transform.parent != root)
            {
                continue;
            }

            collider.transform.localPosition = Vector3.zero;
            collider.transform.localRotation = Quaternion.identity;
            collider.transform.localScale = Vector3.one;
            collider.center = center;
            collider.size = size;
        }
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

    /// <summary>
    /// Returns a recoloured, GPU-only copy of a texture.
    /// </summary>
    /// <param name="source">Texture to copy. It does not need to be CPU-readable.</param>
    /// <param name="recolor">Returns the new colour of a pixel.</param>
    /// <returns>The recoloured copy.</returns>
    public static Texture2D RecolorTexture(Texture source, Func<Color32, Color32> recolor)
    {
        Color32[] pixels = ReadPixels(source, source.width, source.height).Select(recolor).ToArray();
        return CreateTexture($"{source.name}_recolored", source.width, source.height, pixels, source);
    }

    /// <summary>
    /// Reads the pixels of any texture, scaled to the given size. Rows run bottom to top, as in Unity.
    /// </summary>
    /// <param name="source">Texture to read. It does not need to be CPU-readable.</param>
    /// <param name="width">Width to read at.</param>
    /// <param name="height">Height to read at.</param>
    /// <returns>The pixels, <paramref name="width"/> per row.</returns>
    public static Color32[] ReadPixels(Texture source, int width, int height)
    {
        // Game textures are not CPU-readable, so copy through a render texture first.
        RenderTexture renderTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        RenderTexture previous = RenderTexture.active;
        try
        {
            Graphics.Blit(source, renderTexture);
            RenderTexture.active = renderTexture;
            Texture2D copy = new(width, height, TextureFormat.RGBA32, mipChain: false, linear: false);
            copy.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            Color32[] pixels = copy.GetPixels32();
            UnityEngine.Object.Destroy(copy);
            return pixels;
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTexture);
        }
    }

    /// <summary>
    /// Creates a GPU-only texture from pixels.
    /// </summary>
    /// <param name="name">Texture name.</param>
    /// <param name="width">Width.</param>
    /// <param name="height">Height.</param>
    /// <param name="pixels">Pixels, <paramref name="width"/> per row, bottom to top.</param>
    /// <param name="settingsFrom">Texture to copy wrap and filter mode from, or null.</param>
    /// <returns>The texture.</returns>
    public static Texture2D CreateTexture(string name, int width, int height, Color32[] pixels, Texture settingsFrom = null)
    {
        Texture2D texture = new(width, height, TextureFormat.RGBA32, mipChain: true, linear: false) { name = name };
        if (settingsFrom != null)
        {
            texture.wrapMode = settingsFrom.wrapMode;
            texture.filterMode = settingsFrom.filterMode;
        }

        texture.SetPixels32(pixels);
        texture.Apply(updateMipmaps: true, makeNoLongerReadable: true);
        return texture;
    }
}
