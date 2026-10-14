using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Pieces.Ships.WhiteHiltShip;
using System;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.SeaBattles.Escorts;

/// <summary>
/// The trading ship's look: the longship's red and white sail turned blue and cream, and the barrels and crates of its
/// own trader dressing shown on deck as cargo, without the shields. Everything is a copy that lives on the cloned prefab,
/// no networked object. Nothing here runs on a dedicated server, and a part that fails keeps its vanilla look.
/// </summary>
internal static class TradeShipLook
{
    // Haldor's colours: a deep sea blue and the cream of undyed wool.
    private const float BlueHue = 0.58f;
    private const float BlueSaturation = 0.7f;
    private const float CreamHue = 0.12f;
    private const float CreamSaturation = 0.16f;

    // Names of the dressing the longship keeps in its storage group: barrels and crates. The shields are left out.
    private const string BarrelPrefix = "barrell";
    private const string CratePrefix = "default";

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
            Jotunn.Logger.LogWarning("Trading Ship: ship/visual not found, keeping the vanilla look.");
            return;
        }

        TryStep("sail", () => WhiteHiltShipLook.PaintMaterials(
            visual.GetComponentsInChildren<Renderer>(true).Where(renderer => renderer is SkinnedMeshRenderer).ToArray(),
            name => name.StartsWith("sail_diffuse", StringComparison.Ordinal), PaintSail));
        TryStep("cargo", () => ShowCargo(visual));
    }

    private static void TryStep(string part, Action step)
    {
        try
        {
            step();
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"Trading Ship: keeping the vanilla {part}: {ex.Message}");
        }
    }

    // The vanilla sail has red and white stripes: the red becomes blue and the white cream, and the grain of the cloth stays.
    private static Texture2D PaintSail(Texture source)
    {
        return VisualHelper.RecolorTexture(source, PaintSailPixel);
    }

    private static Color32 PaintSailPixel(Color32 pixel)
    {
        Color.RGBToHSV(pixel, out float hue, out float saturation, out float value);
        bool red = saturation > 0.35f && (hue < 0.05f || hue > 0.93f);
        Color painted = red
            ? Color.HSVToRGB(BlueHue, BlueSaturation, Mathf.Clamp01(value * 0.95f + 0.05f))
            : Color.HSVToRGB(CreamHue, CreamSaturation, Mathf.Clamp01(value * 1.05f + 0.08f));
        painted.a = pixel.a / 255f;
        return painted;
    }

    // The longship carries a trader's dressing in its Customize group, all switched off: tent, lamp, shields, barrels and
    // crates. Only the barrels and crates are switched on, so the deck looks loaded with goods.
    private static void ShowCargo(Transform visual)
    {
        Transform customize = visual.Find("Customize") ?? throw new InvalidOperationException("Customize not found");
        Transform storage = customize.Find("storage") ?? throw new InvalidOperationException("Customize/storage not found");

        foreach (Transform child in customize)
        {
            child.gameObject.SetActive(child == storage);
        }

        int shown = 0;
        foreach (Transform child in storage)
        {
            bool cargo = child.name.StartsWith(BarrelPrefix, StringComparison.Ordinal) || child.name.StartsWith(CratePrefix, StringComparison.Ordinal);
            child.gameObject.SetActive(cargo);
            shown += cargo ? 1 : 0;
        }

        if (shown == 0)
        {
            throw new InvalidOperationException("no barrels or crates in Customize/storage");
        }

        customize.gameObject.SetActive(true);
    }
}
