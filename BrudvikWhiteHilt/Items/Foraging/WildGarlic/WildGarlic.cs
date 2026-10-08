using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Foraging.WildGarlic;

/// <summary>
/// Wild garlic, found in the shade of the Meadows forests. Uses the dandelion model with white flowers.
/// </summary>
public class WildGarlic : ForageableBase
{
    /// <summary>
    /// Prefab name of the wild garlic item.
    /// </summary>
    public const string PrefabName = "WhiteHiltWildGarlic";

    /// <inheritdoc/>
    public override string BaseName => PrefabName;

    /// <inheritdoc/>
    protected override string FullName => "Wild Garlic";

    /// <inheritdoc/>
    protected override string Description => "Broad green leaves with a sharp scent of garlic, growing in the shade of the meadow forests.";

    /// <inheritdoc/>
    protected override string CopyItemFrom => "Dandelion";

    /// <inheritdoc/>
    protected override string CopyPickableFrom => "Pickable_Dandelion";

    /// <inheritdoc/>
    protected override VegetationConfig Vegetation => new()
    {
        Biome = Heightmap.Biome.Meadows,
        // Groups per zone where the ground suits it; the generator keeps trying spots until they are placed.
        Min = 1,
        Max = 2,
        ForcePlacement = true,
        GroupSizeMin = 3,
        GroupSizeMax = 6,
        GroupRadius = 3f,
        InForest = true,
        ForestThresholdMin = 0f,
        ForestThresholdMax = 1f,
        MinAltitude = 1f,
        MaxTilt = 25f,
        ScaleMin = 0.9f,
        ScaleMax = 1.1f,
        BlockCheck = true
    };

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override void ApplyVisual(GameObject visualRoot)
    {
        VisualHelper.Recolor(visualRoot, WhitenYellow);
    }

    private static Color32 WhitenYellow(Color32 pixel)
    {
        Color.RGBToHSV(pixel, out float hue, out float saturation, out float value);
        bool yellow = hue >= 0.1f && hue <= 0.19f && saturation > 0.4f && value > 0.35f;
        if (!yellow)
        {
            return pixel;
        }

        Color32 white = Color.HSVToRGB(hue, saturation * 0.1f, Mathf.Min(1f, value * 1.05f));
        white.a = pixel.a;
        return white;
    }
}
