using BrudvikWhiteHilt.Helpers;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Companions;

public static partial class DogRegistry
{
    /// <summary>
    /// Number of coat colours.
    /// </summary>
    public static int ColorCount => colors.Length;

    /// <summary>
    /// Prefab name of the puppy in a coat colour.
    /// </summary>
    /// <param name="color">Index of the coat colour.</param>
    /// <returns>The prefab name.</returns>
    public static string GetPuppyPrefab(int color)
    {
        return colors[Mathf.Clamp(color, 0, colors.Length - 1)].PrefabName;
    }

    /// <summary>
    /// Returns the coat colour of a puppy item.
    /// </summary>
    /// <param name="item">An inventory item.</param>
    /// <param name="color">Index of the coat colour.</param>
    /// <returns>True when the item is a puppy.</returns>
    public static bool IsPuppy(ItemDrop.ItemData item, out int color)
    {
        string prefabName = item?.m_dropPrefab != null ? item.m_dropPrefab.name : null;
        color = Array.FindIndex(colors, candidate => candidate.PrefabName == prefabName);
        return color >= 0;
    }

    /// <summary>
    /// Returns the dog's body material in a coat colour, with a greying muzzle for an old dog and its coat when it wears one,
    /// made the first time it is needed.
    /// </summary>
    /// <param name="color">Index of the coat colour.</param>
    /// <param name="greyStep">How grey the muzzle is, 0 (none) to <see cref="GreySteps"/>.</param>
    /// <param name="coat">True when the dog wears its coat.</param>
    /// <param name="source">The vanilla wolf material.</param>
    /// <returns>The coloured material.</returns>
    public static Material GetCoatMaterial(int color, int greyStep, bool coat, Material source)
    {
        color = Mathf.Clamp(color, 0, colors.Length - 1);
        greyStep = Mathf.Clamp(greyStep, 0, GreySteps);
        int key = color * 100 + greyStep * 2 + (coat ? 1 : 0);
        if (!coatMaterials.TryGetValue(key, out Material material) || material == null)
        {
            material = new Material(source) { mainTexture = CreateCoatTexture(source.mainTexture, colors[color].Recolor, greyStep, coat) };
            coatMaterials[key] = material;
        }

        return material;
    }

    // The coat colour everywhere, the fur of the muzzle blended towards grey, and the coat painted over the back.
    // Both masks were worked out offline from the wolf's UVs.
    private static Texture2D CreateCoatTexture(Texture source, Func<Color32, Color32> recolor, int greyStep, bool coat)
    {
        if (greyStep == 0 && !coat)
        {
            return VisualHelper.RecolorTexture(source, recolor);
        }

        int width = source.width;
        int height = source.height;
        Color32[] pixels = VisualHelper.ReadPixels(source, width, height);
        float grey = 0.85f * greyStep / GreySteps;
        for (int i = 0; i < pixels.Length; i++)
        {
            Color32 original = pixels[i];
            int x = (i % width) * MaskSize / width;
            int y = (i / width) * MaskSize / height;
            Color.RGBToHSV(original, out _, out _, out float shade);
            if (coat && InMask(coatRows, x, y))
            {
                pixels[i] = WithAlpha(Color.HSVToRGB(0.075f, 0.6f, 0.16f + 0.3f * shade), original.a);
                continue;
            }

            Color32 recoloured = recolor(original);
            pixels[i] = greyStep > 0 && InMask(muzzleRows, x, y) && IsFur(original, out float value)
                ? Color32.Lerp(recoloured, WithAlpha(Color.HSVToRGB(0f, 0f, Mathf.Max(0.75f, value)), original.a), grey)
                : recoloured;
        }

        return VisualHelper.CreateTexture(source.name + "_whitehilt_dog", width, height, pixels, source);
    }

    private static bool InMask(Dictionary<int, ulong> rows, int x, int y)
    {
        return rows.TryGetValue(y, out ulong row) && ((row >> (MaskSize - 1 - x)) & 1UL) != 0;
    }

    // A sleeping puppy baked from the game's own wolf at start-up, so nothing of the game is shipped; the recoloured trophy if that fails.
    private static void ShapePuppy(CustomItem item, Func<Color32, Color32> recolor)
    {
        try
        {
            Mesh baked = BakeWolf(RestPose.Pose.Sleep, out Texture texture, out _);
            baked.name = "whitehilt_puppy";
            VisualHelper.ReplaceMesh(item.ItemPrefab, baked, VisualHelper.RecolorTexture(texture, recolor), size: 0.5f);
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"{item.ItemPrefab.name}: no puppy model, the trophy is used: {ex.Message}");
            VisualHelper.Recolor(item.ItemPrefab, recolor);
        }
    }

    /// <summary>
    /// Bakes the game's own wolf in a resting pose into a static mesh, in the space of the wolf's Visual object.
    /// </summary>
    /// <param name="pose">The pose.</param>
    /// <param name="texture">The wolf's texture.</param>
    /// <param name="visualScale">Scale of the Visual object in the wolf prefab.</param>
    /// <returns>The mesh.</returns>
    public static Mesh BakeWolf(RestPose.Pose pose, out Texture texture, out float visualScale)
    {
        Transform visual = PrefabManager.Instance.GetPrefab("Wolf")?.transform.Find("Visual");
        SkinnedMeshRenderer source = visual != null ? visual.GetComponentInChildren<SkinnedMeshRenderer>(true) : null;
        if (source == null || source.sharedMaterial == null || source.sharedMaterial.mainTexture == null)
        {
            throw new InvalidOperationException("the wolf's skinned mesh was not found");
        }

        texture = source.sharedMaterial.mainTexture;
        visualScale = visual.localScale.x;
        GameObject holder = new("whitehilt_wolf_bake");
        holder.SetActive(false);
        try
        {
            GameObject copy = UnityEngine.Object.Instantiate(visual.gameObject, holder.transform, false);
            RestPose.ApplyTo(copy.transform, pose);
            SkinnedMeshRenderer skin = copy.GetComponentInChildren<SkinnedMeshRenderer>(true);
            Mesh baked = new() { name = "whitehilt_wolf_" + pose };
            skin.BakeMesh(baked, true);
            if (baked.vertexCount == 0)
            {
                throw new InvalidOperationException("baking the wolf gave no vertices");
            }

            // BakeMesh leaves the vertices in the renderer's own orientation; turn them into the wolf's, so it lies upright.
            Matrix4x4 toRoot = copy.transform.worldToLocalMatrix * Matrix4x4.TRS(skin.transform.position, skin.transform.rotation, Vector3.one);
            baked.vertices = baked.vertices.Select(vertex => toRoot.MultiplyPoint3x4(vertex)).ToArray();
            baked.normals = baked.normals.Select(normal => toRoot.MultiplyVector(normal).normalized).ToArray();
            baked.RecalculateBounds();
            return baked;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(holder);
        }
    }

    // The wolf texture is grey fur with a lighter belly; eyes, nose and mouth are coloured or dark and stay as they are.
    private static bool IsFur(Color32 pixel, out float value)
    {
        Color.RGBToHSV(pixel, out _, out float saturation, out value);
        return saturation < 0.25f && value >= 0.12f;
    }

    private static Color32 WithAlpha(Color color, byte alpha)
    {
        Color32 result = color;
        result.a = alpha;
        return result;
    }

    private static Color32 RecolorBrown(Color32 pixel)
    {
        if (!IsFur(pixel, out float value))
        {
            return pixel;
        }

        return WithAlpha(value > 0.82f ? Color.HSVToRGB(0.09f, 0.18f, value) : Color.HSVToRGB(0.07f, 0.55f, value * 0.7f), pixel.a);
    }

    private static Color32 RecolorPiebald(Color32 pixel)
    {
        if (!IsFur(pixel, out float value))
        {
            return pixel;
        }

        return WithAlpha(value > 0.62f ? Color.HSVToRGB(0f, 0f, Mathf.Min(1f, value * 1.1f)) : Color.HSVToRGB(0f, 0f, value * 0.22f), pixel.a);
    }

    private static Color32 RecolorGolden(Color32 pixel)
    {
        if (!IsFur(pixel, out float value))
        {
            return pixel;
        }

        return WithAlpha(value > 0.85f ? Color.HSVToRGB(0.11f, 0.15f, value) : Color.HSVToRGB(0.1f, 0.55f, Mathf.Min(1f, value * 1.1f)), pixel.a);
    }

    private sealed class CoatColor
    {
        public CoatColor(string prefabName, string englishName, Func<Color32, Color32> recolor)
        {
            PrefabName = prefabName;
            EnglishName = englishName;
            Recolor = recolor;
        }

        public string PrefabName { get; }

        public string EnglishName { get; }

        public Func<Color32, Color32> Recolor { get; }
    }
}
