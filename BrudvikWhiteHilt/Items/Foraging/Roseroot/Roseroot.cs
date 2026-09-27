using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Foraging.Roseroot;

/// <summary>
/// Roseroot, found on rocky Mountain slopes. Uses the thistle with yellow flowers.
/// </summary>
public class Roseroot : ForageableBase
{
    /// <summary>
    /// Prefab name of the roseroot item.
    /// </summary>
    public const string PrefabName = "WhiteHiltRoseroot";

    /// <inheritdoc/>
    public override string BaseName => PrefabName;

    /// <inheritdoc/>
    protected override string FullName => "Roseroot";

    /// <inheritdoc/>
    protected override string Description => "A hardy Mountain herb with yellow flowers. Its root is said to keep a traveller going.";

    /// <inheritdoc/>
    protected override string CopyItemFrom => "Thistle";

    /// <inheritdoc/>
    protected override string CopyPickableFrom => "Pickable_Thistle";

    /// <inheritdoc/>
    protected override VegetationConfig Vegetation => new()
    {
        Biome = Heightmap.Biome.Mountain,
        Max = 2,
        GroupSizeMin = 2,
        GroupSizeMax = 4,
        GroupRadius = 3f,
        MinAltitude = 1f,
        MaxTilt = 40f,
        ScaleMin = 0.9f,
        ScaleMax = 1.1f,
        BlockCheck = true
    };

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override string ExtraDropFrom => null;

    /// <inheritdoc/>
    protected override void ApplyVisual(GameObject visualRoot)
    {
        VisualHelper.RecolorHue(visualRoot, 0.72f, 0.95f, 0.13f, 1f, 1.3f);
    }
}
