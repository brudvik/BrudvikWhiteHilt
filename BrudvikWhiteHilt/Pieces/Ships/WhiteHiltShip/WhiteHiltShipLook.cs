using BrudvikWhiteHilt.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Ships.WhiteHiltShip;

/// <summary>
/// Gives the cloned longship the White Hilt look: a carved dragon figurehead, a white sail with gold edge stripes and the
/// White Hilt logo, a whitewashed hull with gold fittings and shields with the logo along the rail.
/// </summary>
public static class WhiteHiltShipLook
{
    private const int SailSize = 1024;
    private const float DragonHeightFactor = 1.4f;

    // The vanilla sail has five vertical stripes of 0.203 u: red, white, red, white, red. The middle three become one field.
    private const float SailFieldStart = 0.203f;
    private const float SailFieldEnd = 0.805f;
    private const float SailStripeWidth = 0.203f;

    private const int ShieldTextureSize = 512;
    private const float ShieldLogoRadius = 0.2187f * 0.95f;

    // Relative to the skull's height: back towards the stern and down, so the neck sits in the stem.
    private const float DragonBackOffset = 0.18f;
    private const float DragonDownOffset = 0.22f;

    private static readonly Color cream = new(0.94f, 0.91f, 0.84f);

    // A logo 4 m across; the sail cloth is 9.03 m per u and 7.36 m per v.
    private static readonly Vector2 sailLogoRadius = new(2f / 9.03f, 2f / 7.36f);
    private static readonly Vector2 shieldFaceCenter = new(0.246f, 0.7338f);

    /// <summary>
    /// Changes the look of the ship prefab. Each part keeps its vanilla look if changing it fails.
    /// </summary>
    /// <param name="ship">The cloned ship prefab.</param>
    public static void Apply(GameObject ship)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        Transform visual = ship.transform.Find("ship/visual");
        if (visual == null)
        {
            Jotunn.Logger.LogWarning("White Hilt Ship: ship/visual not found, keeping the vanilla look.");
            return;
        }

        TryStep("figurehead", () => ReplaceFigurehead(visual));
        TryStep("sail", () => PaintSail(ship.transform));
        TryStep("hull", () => PaintMaterials(visual, name => name.StartsWith("ship_diffuse"), PaintHull));
        TryStep("shields", () => ShowShields(visual));
        TryStep("tent", () => PaintTent(visual));
    }

    private static void TryStep(string part, Action step)
    {
        try
        {
            step();
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"White Hilt Ship: keeping the vanilla {part}: {ex.Message}");
        }
    }

    // The skull on the bow sits in the new, worn and broken hull alike, so every copy gets the dragon.
    private static void ReplaceFigurehead(Transform visual)
    {
        Mesh dragon = ForagingAssets.LoadMesh("shipdragon");
        Texture2D texture = ForagingAssets.LoadTexture("shipdragon_albedo");

        // The dragon looks along +z; the bow of the longship points along -x.
        Quaternion forward = Quaternion.Euler(0f, -90f, 0f);
        Vector3 dragonBase = new(dragon.bounds.center.x, dragon.bounds.min.y, dragon.bounds.center.z);

        MeshFilter[] skulls = visual.GetComponentsInChildren<MeshFilter>(true)
            .Where(filter => filter.sharedMesh != null && filter.sharedMesh.name == "skull_head")
            .ToArray();
        if (skulls.Length == 0)
        {
            throw new InvalidOperationException("no skull_head on the bow");
        }

        foreach (MeshFilter skull in skulls)
        {
            MeshRenderer renderer = skull.GetComponent<MeshRenderer>();
            Bounds bounds = skull.sharedMesh.bounds;
            float scale = bounds.size.y * DragonHeightFactor / dragon.bounds.size.y;
            Vector3 skullBase = new(bounds.center.x, bounds.min.y, bounds.center.z);
            skullBase += new Vector3(DragonBackOffset, -DragonDownOffset, 0f) * bounds.size.y;
            VisualHelper.CreateModel(skull.transform, dragon, texture, renderer, skullBase - forward * (dragonBase * scale), forward, scale);
            renderer.enabled = false;
        }
    }

    // Paints the White Hilt sail over the vanilla one: a red field with white stripes and the logo in the middle,
    // keeping the vanilla cloth's grain so it does not look flat.
    private static void PaintSail(Transform ship)
    {
        Renderer[] sails = ship.GetComponentsInChildren<SkinnedMeshRenderer>(true)
            .Where(renderer => renderer.sharedMaterials.Any(material => material != null && material.HasProperty("_MainTex") && material.mainTexture != null && material.mainTexture.name.StartsWith("sail_diffuse")))
            .ToArray<Renderer>();
        if (sails.Length == 0)
        {
            throw new InvalidOperationException("no sail with the sail_diffuse texture");
        }

        PaintMaterials(sails, name => name.StartsWith("sail_diffuse"), source =>
        {
            Color32[] vanilla = VisualHelper.ReadPixels(source, SailSize, SailSize);
            Color32[] pixels = new Color32[vanilla.Length];
            int stripeShift = Mathf.RoundToInt(SailStripeWidth * SailSize);
            for (int y = 0; y < SailSize; y++)
            {
                for (int x = 0; x < SailSize; x++)
                {
                    float u = (x + 0.5f) / SailSize;
                    bool field = u >= SailFieldStart && u < SailFieldEnd;
                    bool middleStripe = u >= SailFieldStart + SailStripeWidth && u < SailFieldEnd - SailStripeWidth;

                    // The middle red stripe takes the grain of the white stripe beside it, so the field is one cloth.
                    Color32 cloth = vanilla[y * SailSize + (middleStripe ? x - stripeShift : x)];
                    pixels[y * SailSize + x] = PaintSailPixel(cloth, field);
                }
            }

            WhiteHiltLogo.Paint(pixels, SailSize, SailSize, new Vector2(0.5f, 0.5f), sailLogoRadius, ClothShade);
            return VisualHelper.CreateTexture("whitehilt_sail", SailSize, SailSize, pixels, source);
        });
    }

    // The red stripes along the edges become gold; the field between them is plain white cloth.
    private static Color32 PaintSailPixel(Color32 pixel, bool field)
    {
        Color.RGBToHSV(pixel, out float hue, out float saturation, out float value);
        bool red = saturation > 0.35f && (hue < 0.05f || hue > 0.93f);
        Color painted = field
            ? cream * Mathf.Clamp01(value * 1.1f + 0.1f)
            : red
                ? Color.HSVToRGB(0.11f, 0.7f, Mathf.Clamp01(value * 1.25f + 0.1f))
                : Color.Lerp(pixel, cream * Mathf.Clamp01(value * 1.1f + 0.1f), 0.5f);
        painted.a = 1f;
        return painted;
    }

    // Keeps the folds and grain of the cloth below in the logo painted over it.
    private static float ClothShade(Color32 cloth)
    {
        Color.RGBToHSV(cloth, out _, out _, out float value);
        return Mathf.Clamp(value / 0.9f, 0.75f, 1.05f);
    }

    // Wood is whitewashed; the metal fittings, found through the metallic map, turn gold.
    private static Texture2D PaintHull(Material material, Texture source)
    {
        int width = source.width;
        int height = source.height;
        Color32[] pixels = VisualHelper.ReadPixels(source, width, height);
        Texture metalMap = material.HasProperty("_MetallicGlossMap") ? material.GetTexture("_MetallicGlossMap") : null;
        Color32[] metal = metalMap != null ? VisualHelper.ReadPixels(metalMap, width, height) : null;

        for (int i = 0; i < pixels.Length; i++)
        {
            Color.RGBToHSV(pixels[i], out _, out _, out float value);
            Color painted = metal != null && metal[i].r > 127
                ? Color.HSVToRGB(0.11f, 0.75f, Mathf.Clamp01(value * 1.4f + 0.1f))
                : Color.Lerp(pixels[i], cream * Mathf.Clamp01(value * 1.25f + 0.15f), 0.6f);
            painted.a = pixels[i].a / 255f;
            pixels[i] = painted;
        }

        return VisualHelper.CreateTexture($"{source.name}_whitehilt", width, height, pixels, source);
    }

    // The vanilla longship has shields along the rail in its trader dressing. Only the shields are switched on.
    private static void ShowShields(Transform visual)
    {
        Transform customize = visual.Find("Customize") ?? throw new InvalidOperationException("Customize not found");
        Transform storage = customize.Find("storage") ?? throw new InvalidOperationException("Customize/storage not found");

        foreach (Transform child in customize)
        {
            child.gameObject.SetActive(child == storage);
        }

        List<Renderer> shields = new();
        foreach (Transform child in storage)
        {
            bool shield = child.name.StartsWith("Shield");
            child.gameObject.SetActive(shield);
            if (shield)
            {
                TurnUpright(child, visual.up);
                shields.AddRange(child.GetComponentsInChildren<Renderer>(true));
            }
        }

        customize.gameObject.SetActive(true);
        PaintMaterials(shields.ToArray(), _ => true, (_, source) => PaintShield(source));
    }

    private static Texture2D PaintShield(Texture source)
    {
        int size = source.name.StartsWith("shieldwood_d") ? ShieldTextureSize : source.width;
        Color32[] pixels = VisualHelper.ReadPixels(source, size, size).Select(PaintShieldPixel).ToArray();
        if (size == ShieldTextureSize)
        {
            WhiteHiltLogo.Paint(pixels, size, size, shieldFaceCenter, Vector2.one * ShieldLogoRadius, ClothShade);
        }

        return VisualHelper.CreateTexture($"{source.name}_whitehilt", size, size, pixels, source);
    }

    // The shields hang turned every way, so each is turned about its own face until the logo stands upright.
    // The painted face of ShieldIron looks along local -z, and the top of the texture points along local -y.
    private static void TurnUpright(Transform shield, Vector3 up)
    {
        Vector3 normal = shield.TransformDirection(Vector3.back);
        Vector3 textureUp = Vector3.ProjectOnPlane(shield.TransformDirection(Vector3.down), normal);
        Vector3 wanted = Vector3.ProjectOnPlane(up, normal);
        if (wanted.sqrMagnitude < 0.01f || textureUp.sqrMagnitude < 0.01f)
        {
            return;
        }

        shield.RotateAround(shield.position, normal, Vector3.SignedAngle(textureUp, wanted, normal));
    }

    // The banded shield has red and white fields, a dark iron rim and a dark boss: fields become gold and cream,
    // the iron polished gold.
    private static Color32 PaintShieldPixel(Color32 pixel)
    {
        Color.RGBToHSV(pixel, out float hue, out float saturation, out float value);
        if (value < 0.05f)
        {
            return pixel;
        }

        bool red = saturation > 0.4f && (hue < 0.06f || hue > 0.92f);
        Color painted = red
            ? Color.HSVToRGB(0.11f, 0.55f, Mathf.Clamp01(value * 1.1f + 0.1f))
            : value > 0.45f
                ? cream * Mathf.Clamp01(value * 0.5f + 0.55f)
                : Color.HSVToRGB(0.12f, 0.8f, Mathf.Clamp01(value * 1.6f + 0.35f));
        painted.a = pixel.a / 255f;
        return painted;
    }

    // The tent is red cloth with cream edges on wooden holders: the cloth becomes cream, the edges gold.
    private static void PaintTent(Transform visual)
    {
        Transform customize = visual.Find("Customize") ?? throw new InvalidOperationException("Customize not found");
        Renderer[] tent = customize.Cast<Transform>()
            .Where(child => child.name.StartsWith("ShipTen"))
            .SelectMany(child => child.GetComponentsInChildren<Renderer>(true))
            .ToArray();
        PaintMaterials(tent, _ => true, (_, source) => VisualHelper.RecolorTexture(source, PaintTentPixel));
    }

    private static Color32 PaintTentPixel(Color32 pixel)
    {
        Color.RGBToHSV(pixel, out float hue, out float saturation, out float value);
        bool red = saturation > 0.4f && (hue < 0.06f || hue > 0.92f);
        bool edge = saturation < 0.3f && value > 0.55f;
        if (!red && !edge)
        {
            return pixel;
        }

        Color painted = red
            ? cream * Mathf.Clamp01(value * 1.2f + 0.3f)
            : Color.HSVToRGB(0.12f, 0.75f, Mathf.Clamp01(value * 1.05f));
        painted.a = pixel.a / 255f;
        return painted;
    }

    private static void PaintMaterials(Transform root, Func<string, bool> matches, Func<Material, Texture, Texture2D> paint)
    {
        PaintMaterials(root.GetComponentsInChildren<Renderer>(true), matches, paint);
    }

    private static void PaintMaterials(Renderer[] renderers, Func<string, bool> matches, Func<Texture, Texture2D> paint)
    {
        PaintMaterials(renderers, matches, (_, source) => paint(source));
    }

    // Materials are shared between the hull copies, so each vanilla material is painted once and reused.
    private static void PaintMaterials(Renderer[] renderers, Func<string, bool> matches, Func<Material, Texture, Texture2D> paint)
    {
        Dictionary<Material, Material> painted = new();
        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < materials.Length; i++)
            {
                Material source = materials[i];
                if (source == null || !source.HasProperty("_MainTex") || source.mainTexture == null || !matches(source.mainTexture.name))
                {
                    continue;
                }

                if (!painted.TryGetValue(source, out Material replacement))
                {
                    replacement = new Material(source) { name = $"{source.name}_whitehilt", mainTexture = paint(source, source.mainTexture) };
                    painted[source] = replacement;
                }

                materials[i] = replacement;
                changed = true;
            }

            if (changed)
            {
                renderer.sharedMaterials = materials;
            }
        }

        if (painted.Count == 0)
        {
            throw new InvalidOperationException("no matching material found");
        }
    }
}
