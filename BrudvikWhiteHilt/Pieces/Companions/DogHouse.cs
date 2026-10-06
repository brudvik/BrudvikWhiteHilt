using BrudvikWhiteHilt.Companions;
using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Companions;

/// <summary>
/// A small wooden dog house lined with a wolf pelt. It marks the dog's home, and the dog shelters in it in the rain.
/// </summary>
public class DogHouse : DogPieceBase
{
    /// <summary>
    /// Prefab name of the dog house.
    /// </summary>
    public const string Name = "piece_whitehilt_doghouse";

    // The model is 1 high, 0.84 wide and 0.98 deep with its door on +z; walls stand at x ±0.4 and z ±0.45.
    private const float Size = 2f;

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public DogHouse(PieceManager instance) : base(instance)
    {
    }

    /// <inheritdoc/>
    protected override string PrefabName => Name;

    /// <inheritdoc/>
    protected override string FullName => "Dog House";

    /// <inheritdoc/>
    protected override string Description => "A small house for a dog, lined with a pelt. Where it stands is the dog's home, and the dog shelters in it from the rain. Use it while your grown dog follows you to move it in.";

    /// <inheritdoc/>
    protected override string BasePrefab => "rug_wolf";

    /// <inheritdoc/>
    protected override string CraftingStation => CraftingStations.Workbench;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Wood", Amount = 12, Recover = true },
        new() { Item = "Resin", Amount = 2, Recover = true },
        new() { Item = "DeerHide", Amount = 2, Recover = true }
    };

    /// <inheritdoc/>
    protected override void KeepBeforeStrip(GameObject prefab, Transform visual)
    {
        KeepPelt(prefab, visual, new Vector3(0f, 0.05f, 0f), 0f, 0.41f);
    }

    /// <inheritdoc/>
    protected override void Configure(GameObject prefab)
    {
        // Walls and roof block, the inside stays open so the dog can lie there.
        AddBox(prefab, "piece", new Vector3(-0.8f, 0.6f, 0f), new Vector3(0.1f, 1.2f, 1.94f));
        AddBox(prefab, "piece", new Vector3(0.8f, 0.6f, 0f), new Vector3(0.1f, 1.2f, 1.94f));
        AddBox(prefab, "piece", new Vector3(0f, 0.6f, -0.92f), new Vector3(1.7f, 1.2f, 0.1f));
        AddBox(prefab, "piece", new Vector3(-0.64f, 0.6f, 0.9f), new Vector3(0.36f, 1.2f, 0.1f));
        AddBox(prefab, "piece", new Vector3(0.64f, 0.6f, 0.9f), new Vector3(0.36f, 1.2f, 0.1f));
        AddBox(prefab, "piece", new Vector3(0f, 1.1f, 0.9f), new Vector3(0.88f, 0.2f, 0.1f));
        AddBox(prefab, "piece", new Vector3(0f, 1.55f, 0f), new Vector3(1.7f, 0.7f, 1.96f));

        DogHomePiece home = prefab.AddComponent<DogHomePiece>();
        home.Kind = DogHomeKind.House;
        home.RestOffset = new Vector3(0f, 0.05f, -0.1f);
        home.RestYaw = 0f;
        home.ApproachOffset = new Vector3(0f, 0f, 1.6f);
        prefab.AddComponent<DogHouseHome>();
    }

    /// <inheritdoc/>
    protected override void ApplyVisual(Transform visual)
    {
        VisualHelper.CreateModel(visual, ForagingAssets.LoadMesh("doghouse"), ForagingAssets.LoadTexture("doghouse_albedo"),
            Template("wood_pole2", "New"), Vector3.zero, Quaternion.identity, Size);
    }
}
