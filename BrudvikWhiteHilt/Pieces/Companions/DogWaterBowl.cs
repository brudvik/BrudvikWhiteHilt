using BrudvikWhiteHilt.Companions;
using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Companions;

/// <summary>
/// A bark bowl of water. A dog drinks from it now and then, and a puppy that has water at home grows faster.
/// </summary>
public class DogWaterBowl : DogPieceBase
{
    /// <summary>
    /// Prefab name of the water bowl.
    /// </summary>
    public const string Name = "piece_whitehilt_dogwaterbowl";

    // Same bowl as the food bowl: 0.36 m across and 0.12 m high at this size. The inner floor is at 0.05 m and the
    // inner wall is about 0.23 m across two thirds up, where the water stands.
    private const float Size = 0.12f;
    private const float WaterDiameter = 0.24f;
    private const float WaterHeight = 0.08f;

    private static readonly Color32 waterColor = new(46, 88, 104, 255);

    /// <summary>
    /// Constructor for the DogWaterBowl class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public DogWaterBowl(PieceManager instance) : base(instance)
    {
    }

    /// <inheritdoc/>
    protected override string PrefabName => Name;

    /// <inheritdoc/>
    protected override string FullName => "Dog Water Bowl";

    /// <inheritdoc/>
    protected override string Description => "A bark bowl of water. The dog drinks from it, and a puppy with water at home grows faster.";

    /// <inheritdoc/>
    protected override string BasePrefab => "rug_wolf";

    /// <inheritdoc/>
    protected override string CraftingStation => CraftingStations.Workbench;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Wood", Amount = 2, Recover = true },
        new() { Item = "Resin", Amount = 2, Recover = true }
    };

    /// <inheritdoc/>
    protected override void Configure(GameObject prefab)
    {
        AddBox(prefab, "piece_nonsolid", new Vector3(0f, 0.06f, 0f), new Vector3(0.36f, 0.12f, 0.36f));

        // The dog walks up from whichever side it comes; it only needs to find the bowl.
        DogHomePiece bowl = prefab.AddComponent<DogHomePiece>();
        bowl.Kind = DogHomeKind.WaterBowl;
    }

    /// <inheritdoc/>
    protected override void ApplyVisual(Transform visual)
    {
        Renderer template = Template("wood_pole2", "New");
        VisualHelper.CreateModel(visual, ForagingAssets.LoadMesh("dogbowl"), ForagingAssets.LoadTexture("dogbowl_albedo"),
            template, Vector3.zero, Quaternion.identity, Size);

        // A flat disc of water: Unity's cylinder is 1 across and 2 high.
        GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Mesh disc = primitive.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(primitive);
        Texture2D water = VisualHelper.CreateTexture("whitehilt_dogwater", 2, 2, new[] { waterColor, waterColor, waterColor, waterColor });
        GameObject surface = VisualHelper.CreateModel(visual, disc, water, template, new Vector3(0f, WaterHeight, 0f), Quaternion.identity, 1f);
        surface.transform.localScale = new Vector3(WaterDiameter, 0.002f, WaterDiameter);
    }
}
