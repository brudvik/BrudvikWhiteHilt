using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Foraging.Chanterelle;

/// <summary>
/// Chanterelle mushroom, found at the forest edges of the Meadows.
/// Model: "Chanterelle" by Zacxophone, CC BY 4.0.
/// </summary>
public class Chanterelle : ForageableBase
{
    /// <summary>
    /// Prefab name of the chanterelle item.
    /// </summary>
    public const string PrefabName = "WhiteHiltChanterelle";

    /// <inheritdoc/>
    public override string BaseName => PrefabName;

    /// <inheritdoc/>
    protected override string FullName => "Chanterelle";

    /// <inheritdoc/>
    protected override string Description => "A golden, funnel-shaped mushroom from the edge of the meadow forests. Better cooked than raw.";

    /// <inheritdoc/>
    protected override string CopyItemFrom => "Mushroom";

    /// <inheritdoc/>
    protected override string CopyPickableFrom => "Pickable_Mushroom";

    /// <inheritdoc/>
    protected override VegetationConfig Vegetation => new()
    {
        Biome = Heightmap.Biome.Meadows,
        Max = 3,
        GroupSizeMin = 1,
        GroupSizeMax = 3,
        GroupRadius = 2f,
        InForest = true,
        ForestThresholdMin = 0f,
        ForestThresholdMax = 1.1f,
        MinAltitude = 1f,
        MaxTilt = 25f,
        ScaleMin = 0.9f,
        ScaleMax = 1.2f,
        BlockCheck = true
    };

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override void ApplyVisual(GameObject visualRoot)
    {
        VisualHelper.ReplaceMesh(visualRoot, ForagingAssets.LoadMesh("chanterelle"), ForagingAssets.LoadTexture("chanterelle_albedo"));
    }
}
