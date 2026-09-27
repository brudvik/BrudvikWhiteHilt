using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Foraging.Lingonberries;

/// <summary>
/// Lingonberries, found in the Black Forest. Uses the blueberry bush with red berries.
/// </summary>
public class Lingonberries : ForageableBase
{
    /// <summary>
    /// Prefab name of the lingonberries item.
    /// </summary>
    public const string PrefabName = "WhiteHiltLingonberries";

    /// <inheritdoc/>
    public override string BaseName => PrefabName;

    /// <inheritdoc/>
    protected override string FullName => "Lingonberries";

    /// <inheritdoc/>
    protected override string Description => "Tart red berries from the forest floor of the Black Forest.";

    /// <inheritdoc/>
    protected override string CopyItemFrom => "Blueberries";

    /// <inheritdoc/>
    protected override string CopyPickableFrom => "BlueberryBush";

    /// <inheritdoc/>
    protected override VegetationConfig Vegetation => new()
    {
        Biome = Heightmap.Biome.BlackForest,
        Max = 2,
        GroupSizeMin = 1,
        GroupSizeMax = 2,
        GroupRadius = 3f,
        InForest = true,
        ForestThresholdMin = 0f,
        ForestThresholdMax = 1.1f,
        MinAltitude = 1f,
        MaxTilt = 25f,
        ScaleMin = 0.8f,
        ScaleMax = 1f,
        BlockCheck = true
    };

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override void ApplyVisual(GameObject visualRoot)
    {
        VisualHelper.RecolorHue(visualRoot, 0.55f, 0.8f, 0.99f, 1.1f, 1.15f);
    }
}
