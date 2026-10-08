using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Foraging.Porcini;

/// <summary>
/// Porcini mushroom, found inside the Black Forest. Red mushrooms picked in the Black Forest can also give it.
/// </summary>
public class Porcini : ForageableBase
{
    /// <summary>
    /// Prefab name of the porcini item.
    /// </summary>
    public const string PrefabName = "WhiteHiltPorcini";

    /// <inheritdoc/>
    public override string BaseName => PrefabName;

    /// <inheritdoc/>
    protected override string FullName => "Porcini";

    /// <inheritdoc/>
    protected override string Description => "A plump, brown-capped mushroom from the shade of the Black Forest. Hearty in a stew.";

    /// <inheritdoc/>
    protected override string CopyItemFrom => "Mushroom";

    /// <inheritdoc/>
    protected override string CopyPickableFrom => "Pickable_Mushroom";

    /// <inheritdoc/>
    protected override VegetationConfig Vegetation => new()
    {
        Biome = Heightmap.Biome.BlackForest,
        // Groups per zone where the ground suits it; the generator keeps trying spots until they are placed.
        Min = 1,
        Max = 3,
        ForcePlacement = true,
        GroupSizeMin = 1,
        GroupSizeMax = 3,
        GroupRadius = 2f,
        InForest = true,
        ForestThresholdMin = 0f,
        ForestThresholdMax = 1f,
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
        VisualHelper.ReplaceMesh(visualRoot, ForagingAssets.LoadMesh("porcini"), ForagingAssets.LoadTexture("porcini_albedo"));
    }
}
