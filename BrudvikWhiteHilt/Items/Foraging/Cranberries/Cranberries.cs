using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Foraging.Cranberries;

/// <summary>
/// Cranberries, found on the bogs of the Swamp. Uses the blueberry bush with dark red berries.
/// Red mushrooms picked in the Swamp can also give them.
/// </summary>
public class Cranberries : ForageableBase
{
    /// <summary>
    /// Prefab name of the cranberries item.
    /// </summary>
    public const string PrefabName = "WhiteHiltCranberries";

    /// <inheritdoc/>
    public override string BaseName => PrefabName;

    /// <inheritdoc/>
    protected override string FullName => "Cranberries";

    /// <inheritdoc/>
    protected override string Description => "Dark red, sour berries that grow on the wet tussocks of the Swamp.";

    /// <inheritdoc/>
    protected override string CopyItemFrom => "Blueberries";

    /// <inheritdoc/>
    protected override string CopyPickableFrom => "BlueberryBush";

    /// <inheritdoc/>
    protected override VegetationConfig Vegetation => new()
    {
        Biome = Heightmap.Biome.Swamp,
        Max = 2,
        GroupSizeMin = 1,
        GroupSizeMax = 3,
        GroupRadius = 3f,
        MinAltitude = 0f,
        MaxTilt = 20f,
        ScaleMin = 0.7f,
        ScaleMax = 0.9f,
        BlockCheck = true
    };

    /// <inheritdoc/>
    public override bool Enabled => true;

    // The Swamp has no berry bushes, so the red mushroom stands in for existing worlds.
    /// <inheritdoc/>
    protected override string ExtraDropFrom => "Pickable_Mushroom";

    /// <inheritdoc/>
    protected override float ExtraDropChance => 0.4f;

    /// <inheritdoc/>
    protected override void ApplyVisual(GameObject visualRoot)
    {
        VisualHelper.RecolorHue(visualRoot, 0.55f, 0.8f, 0.97f, 1.15f, 0.75f);
    }
}
