using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Foraging.Reed;

/// <summary>
/// Reed, found at the water's edge in the Swamp, for the reed thatch roofs. Uses wild flax with its blue flowers turned
/// to brown plumes.
/// </summary>
public class Reed : ForageableBase
{
    /// <summary>
    /// Prefab name of the reed item.
    /// </summary>
    public const string PrefabName = "WhiteHiltReed";

    /// <inheritdoc/>
    public override string BaseName => PrefabName;

    /// <inheritdoc/>
    protected override string FullName => "Reed";

    /// <inheritdoc/>
    protected override string Description => "Tall, tough swamp reed with brown plumes. Cut and bound in bundles, it thatches a roof that lasts.";

    /// <inheritdoc/>
    protected override string CopyItemFrom => "Flax";

    /// <inheritdoc/>
    protected override string CopyPickableFrom => "Pickable_Flax_Wild";

    /// <inheritdoc/>
    protected override VegetationConfig Vegetation => new()
    {
        Biome = Heightmap.Biome.Swamp,
        Max = 3,
        GroupSizeMin = 3,
        GroupSizeMax = 6,
        GroupRadius = 3f,
        MinAltitude = -0.5f,
        MaxAltitude = 0.8f,
        MaxTilt = 25f,
        ScaleMin = 1f,
        ScaleMax = 1.3f,
        BlockCheck = true
    };

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override string ExtraDropFrom => "Pickable_Thistle";

    /// <inheritdoc/>
    protected override Heightmap.Biome ExtraDropBiome => Heightmap.Biome.Swamp;

    /// <inheritdoc/>
    protected override void ApplyVisual(GameObject visualRoot)
    {
        // Blue flax flowers become brown reed plumes; the green stalks turn a dry grey-green.
        VisualHelper.RecolorHue(visualRoot, 0.5f, 0.75f, 0.07f, 0.7f, 0.55f);
        VisualHelper.RecolorHue(visualRoot, 0.15f, 0.45f, 0.17f, 0.55f, 0.9f);
    }
}
