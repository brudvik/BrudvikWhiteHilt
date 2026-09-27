using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Foraging.SweetGale;

/// <summary>
/// Sweet gale, a bog shrub from the Swamp that was once used to flavour beer. Uses the thistle with brown catkins.
/// Thistles picked in the Swamp can also give it.
/// </summary>
public class SweetGale : ForageableBase
{
    /// <summary>
    /// Prefab name of the sweet gale item.
    /// </summary>
    public const string PrefabName = "WhiteHiltSweetGale";

    /// <inheritdoc/>
    public override string BaseName => PrefabName;

    /// <inheritdoc/>
    protected override string FullName => "Sweet Gale";

    /// <inheritdoc/>
    protected override string Description => "A bitter, resinous bog shrub. Brewers used it long before hops, and it gives a stew a sharp bite.";

    /// <inheritdoc/>
    protected override string CopyItemFrom => "Thistle";

    /// <inheritdoc/>
    protected override string CopyPickableFrom => "Pickable_Thistle";

    /// <inheritdoc/>
    protected override VegetationConfig Vegetation => new()
    {
        Biome = Heightmap.Biome.Swamp,
        Max = 2,
        GroupSizeMin = 2,
        GroupSizeMax = 4,
        GroupRadius = 3f,
        MinAltitude = 0f,
        MaxTilt = 20f,
        ScaleMin = 0.9f,
        ScaleMax = 1.1f,
        BlockCheck = true
    };

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override void ApplyVisual(GameObject visualRoot)
    {
        VisualHelper.RecolorHue(visualRoot, 0.72f, 0.95f, 0.05f, 0.75f, 0.7f);
    }
}
