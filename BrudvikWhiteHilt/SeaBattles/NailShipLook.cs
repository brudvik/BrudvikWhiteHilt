using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Pieces.Ships.WhiteHiltShip;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.SeaBattles;

/// <summary>
/// The Nail Ship's look: the longship's hull and sail turned dark and grey-green as rotted wood and old cloth, and the
/// shields of the trader dressing along the rail shown and bleached like bone. Nothing here runs on a dedicated server,
/// and a part that fails keeps its vanilla look.
/// </summary>
internal static class NailShipLook
{
    // Rotted wood: a little green, mostly dark.
    private const float HullHue = 0.36f;
    private const float HullValue = 0.45f;

    // Old cloth.
    private const float SailHue = 0.3f;
    private const float SailValue = 0.35f;

    // Bone.
    private const float BoneHue = 0.12f;

    // The flagship's sail: blood red.
    private const float FlagshipSailHue = 0.99f;
    private const float FlagshipSailSaturation = 0.8f;
    private const float FlagshipSailValue = 1.9f;

    /// <summary>
    /// Changes the look of the cloned ship prefab.
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
            Jotunn.Logger.LogWarning("Nail Ship: ship/visual not found, keeping the vanilla look.");
            return;
        }

        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        TryStep("hull", () => PaintHull(ship.transform, renderers));
        TryStep("sail", () => WhiteHiltShipLook.PaintMaterials(
            renderers.Where(renderer => renderer is SkinnedMeshRenderer).ToArray(),
            name => name.StartsWith("sail_diffuse", StringComparison.Ordinal), PaintSail));
        TryStep("shields", () => ShowShields(visual));
    }

    /// <summary>
    /// Gives the flagship, a clone of the finished Nail Ship, a blood-red sail. Only the sail's material copy changes: the
    /// painted texture of the Nail Ship is recoloured again, so the grain of the cloth stays.
    /// </summary>
    /// <param name="flagship">The cloned flagship prefab.</param>
    public static void ApplyFlagship(GameObject flagship)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        Transform visual = flagship.transform.Find("ship/visual");
        if (visual == null)
        {
            Jotunn.Logger.LogWarning("Nail Flagship: ship/visual not found, keeping the Nail Ship's sail.");
            return;
        }

        TryStep("flagship sail", () => WhiteHiltShipLook.PaintMaterials(
            visual.GetComponentsInChildren<Renderer>(true).Where(renderer => renderer is SkinnedMeshRenderer).ToArray(),
            name => name.StartsWith("sail_diffuse", StringComparison.Ordinal), PaintFlagshipSail));
    }

    private static void TryStep(string part, Action step)
    {
        try
        {
            step();
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"Nail Ship: keeping the vanilla {part}: {ex.Message}");
        }
    }

    // Every texture the hull renderer's own material uses, as the hull, its worn and its broken copies share them. The
    // hull's renderer is found through the template, not as the first renderer, which is the mast's cloth helper.
    private static void PaintHull(Transform root, Renderer[] renderers)
    {
        string hull = WhiteHiltShipUpgradeSetup.HullTemplate(root).sharedMaterial.mainTexture.name;
        WhiteHiltShipLook.PaintMaterials(renderers, name => name == hull,
            source => VisualHelper.RecolorTexture(source, pixel => Recolour(pixel, HullHue, 0.4f, 0.12f, HullValue, 0f)));
    }

    // Blood red: the Nail Ship's grey-green cloth gets a red hue, a fixed saturation and its light brought back up.
    private static Texture2D PaintFlagshipSail(Texture source)
    {
        return VisualHelper.RecolorTexture(source, pixel => Recolour(pixel, FlagshipSailHue, 0f, FlagshipSailSaturation, FlagshipSailValue, 0.04f));
    }

    private static Texture2D PaintSail(Texture source)
    {
        return VisualHelper.RecolorTexture(source, pixel => Recolour(pixel, SailHue, 0.2f, 0.04f, SailValue, 0.05f));
    }

    // The vanilla longship has shields along the rail in its trader dressing; only the shields are switched on, as on
    // the White Hilt Ship. Their paint becomes bone.
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
            bool shield = child.name.StartsWith("Shield", StringComparison.Ordinal);
            child.gameObject.SetActive(shield);
            if (shield)
            {
                shields.AddRange(child.GetComponentsInChildren<Renderer>(true));
            }
        }

        customize.gameObject.SetActive(true);
        WhiteHiltShipLook.PaintMaterials(shields.ToArray(), _ => true,
            source => VisualHelper.RecolorTexture(source, pixel => Recolour(pixel, BoneHue, 0.3f, 0.1f, 0.7f, 0.3f)));
    }

    // Keeps the grain of the vanilla texture (its brightness) and gives it a new hue, less saturation and less light.
    private static Color32 Recolour(Color32 pixel, float hue, float saturationScale, float saturationOffset, float valueScale, float valueOffset)
    {
        Color.RGBToHSV(pixel, out _, out float saturation, out float value);
        Color32 painted = Color.HSVToRGB(hue, Mathf.Clamp01(saturation * saturationScale + saturationOffset), Mathf.Clamp01(value * valueScale + valueOffset));
        painted.a = pixel.a;
        return painted;
    }
}
