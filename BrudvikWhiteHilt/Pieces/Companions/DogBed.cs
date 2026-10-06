using BrudvikWhiteHilt.Companions;
using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Companions;

/// <summary>
/// A low wicker basket lined with a wolf pelt. Under a roof it is the dog's bed, and the dog sleeps in it at night.
/// </summary>
public class DogBed : DogPieceBase
{
    /// <summary>
    /// Prefab name of the dog bed.
    /// </summary>
    public const string Name = "piece_whitehilt_dogbed";

    // The model is 1 high, 5.88 wide (x) and 4.65 deep: 1.88 x 1.49 m and 0.32 m high at this size.
    private const float Size = 0.32f;

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public DogBed(PieceManager instance) : base(instance)
    {
    }

    /// <inheritdoc/>
    protected override string PrefabName => Name;

    /// <inheritdoc/>
    protected override string FullName => "Dog Bed";

    /// <inheritdoc/>
    protected override string Description => "A low wicker basket lined with a pelt. Put it under a roof; the dog sleeps in it at night.";

    /// <inheritdoc/>
    protected override string BasePrefab => "rug_wolf";

    /// <inheritdoc/>
    protected override string CraftingStation => CraftingStations.Workbench;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Wood", Amount = 4, Recover = true },
        new() { Item = "LeatherScraps", Amount = 4, Recover = true },
        new() { Item = "DeerHide", Amount = 1, Recover = true }
    };

    /// <inheritdoc/>
    protected override void KeepBeforeStrip(GameObject prefab, Transform visual)
    {
        KeepPelt(prefab, visual, new Vector3(0f, 0.08f, 0f), 90f, 0.35f);
    }

    /// <inheritdoc/>
    protected override void Configure(GameObject prefab)
    {
        // Non-solid, so the dog can lie in it.
        AddBox(prefab, "piece_nonsolid", new Vector3(0f, 0.16f, 0f), new Vector3(1.88f, 0.32f, 1.49f));

        DogHomePiece bed = prefab.AddComponent<DogHomePiece>();
        bed.Kind = DogHomeKind.Bed;
        bed.RestOffset = new Vector3(0f, 0.03f, 0f);
        bed.RestYaw = 90f;
        bed.ApproachOffset = Vector3.zero;
    }

    /// <inheritdoc/>
    protected override void ApplyVisual(Transform visual)
    {
        VisualHelper.CreateModel(visual, ForagingAssets.LoadMesh("dogbed"), ForagingAssets.LoadTexture("dogbed_albedo"),
            Template("wood_pole2", "New"), Vector3.zero, Quaternion.identity, Size);
    }
}
