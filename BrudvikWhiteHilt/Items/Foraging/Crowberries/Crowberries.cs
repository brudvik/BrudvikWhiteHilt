using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Foraging.Crowberries;

/// <summary>
/// Crowberries, found on open Mountain slopes, and sometimes dropped by wolves.
/// Uses a small blueberry bush with near-black berries.
/// </summary>
public class Crowberries : ForageableBase
{
    /// <summary>
    /// Prefab name of the crowberries item.
    /// </summary>
    public const string PrefabName = "WhiteHiltCrowberries";

    /// <inheritdoc/>
    public override string BaseName => PrefabName;

    /// <inheritdoc/>
    protected override string FullName => "Crowberries";

    /// <inheritdoc/>
    protected override string Description => "Small black berries that cling to the windswept Mountain slopes.";

    /// <inheritdoc/>
    protected override string CopyItemFrom => "Blueberries";

    /// <inheritdoc/>
    protected override string CopyPickableFrom => "BlueberryBush";

    /// <inheritdoc/>
    protected override VegetationConfig Vegetation => new()
    {
        Biome = Heightmap.Biome.Mountain,
        // Groups per zone where the ground suits it; the generator keeps trying spots until they are placed.
        Min = 1,
        Max = 2,
        ForcePlacement = true,
        GroupSizeMin = 1,
        GroupSizeMax = 3,
        GroupRadius = 3f,
        MinAltitude = 1f,
        MaxTilt = 30f,
        ScaleMin = 0.6f,
        ScaleMax = 0.8f,
        BlockCheck = true
    };

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override string ExtraDropFrom => null;

    /// <inheritdoc/>
    protected override string CreatureDropFrom => "Wolf";

    /// <inheritdoc/>
    protected override void ApplyVisual(GameObject visualRoot)
    {
        VisualHelper.RecolorHue(visualRoot, 0.55f, 0.8f, 0.75f, 0.5f, 0.35f);
    }
}
