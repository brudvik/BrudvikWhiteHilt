using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltCrossbow;

/// <summary>
/// The White Hilt Crossbow, cloned from the vanilla <c>CrossbowArbalest</c> and the White Hilt model <c>whcrossbow</c>
/// from the asset bundle.
/// </summary>
public class WhiteHiltCrossbow : WhiteHiltWeaponBase
{
    // Centre of the vanilla bolt on the new stock, in attach space, just ahead of the drawn string.
    private static readonly Vector3 boltPosition = new(0f, 0.12f, 0.52f);
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltCrossbow(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltCrossbow";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Crossbow";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Crossbow of Dyrnwyn";

    /// <inheritdoc/>
    protected override string CopyFrom => "CrossbowArbalest";

    /// <summary>
    /// LowPoly Crossbow Asset by iedalton, with a white stock.
    /// </summary>
    protected override string ModelName => "whcrossbow";

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Iron", Amount = 20, Recover = false },
        new() { Item = "Root", Amount = 10, Recover = false },
        new() { Item = "BowFineWood", Amount = 1, Recover = false }
    };

    /// <summary>
    /// Adds the string, and gives the loaded look (shown by the vanilla WeaponLoadState) the same model with the
    /// string drawn back and the vanilla bolt on the stock.
    /// </summary>
    /// <param name="model">The unloaded crossbow model under the attach child.</param>
    protected override void OnModelApplied(GameObject model)
    {
        Mesh mesh = model.GetComponent<MeshFilter>().sharedMesh;
        MeshRenderer renderer = model.GetComponent<MeshRenderer>();
        VisualHelper.CreateModel(model.transform, ForagingAssets.LoadMesh("whcrossbowstring"), null, renderer, Vector3.zero, Quaternion.identity, 1f);

        Transform loaded = model.transform.parent.Find("Loaded");
        if (loaded == null || !loaded.TryGetComponent(out MeshFilter loadedFilter))
        {
            return;
        }

        loaded.localPosition = Vector3.zero;
        loaded.localRotation = Quaternion.identity;
        loaded.localScale = Vector3.one;
        loadedFilter.sharedMesh = mesh;
        loaded.GetComponent<MeshRenderer>().sharedMaterials = renderer.sharedMaterials;
        VisualHelper.CreateModel(loaded, ForagingAssets.LoadMesh("whcrossbowstringloaded"), null, renderer, Vector3.zero, Quaternion.identity, 1f);

        Transform bolt = loaded.Find("default");
        if (bolt != null)
        {
            bolt.localPosition = boltPosition;
            bolt.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }
}
