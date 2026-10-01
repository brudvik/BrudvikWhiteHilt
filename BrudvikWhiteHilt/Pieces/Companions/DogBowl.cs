using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Pieces.Ranching.FeedingTrough;
using Jotunn.Configs;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Companions;

/// <summary>
/// A bark bowl that holds a little food. A hungry dog, or any tame animal, walks over and eats from it like from a trough.
/// </summary>
public class DogBowl : DogPieceBase
{
    /// <summary>
    /// Prefab name of the dog bowl.
    /// </summary>
    public const string Name = "piece_whitehilt_dogbowl";

    // The model is 1 high and 2.96 across: 0.36 m across and 0.12 m high at this size.
    private const float Size = 0.12f;

    /// <summary>
    /// Constructor for the DogBowl class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public DogBowl(PieceManager instance) : base(instance)
    {
    }

    /// <inheritdoc/>
    protected override string PrefabName => Name;

    /// <inheritdoc/>
    protected override string FullName => "Dog Bowl";

    /// <inheritdoc/>
    protected override string Description => "A bark bowl. Put food in it, and a hungry dog within {0} metres comes and eats.";

    /// <inheritdoc/>
    protected override string BasePrefab => "piece_chest_wood";

    /// <inheritdoc/>
    protected override string CraftingStation => CraftingStations.Workbench;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Wood", Amount = 2, Recover = true }
    };

    /// <inheritdoc/>
    protected override void Configure(GameObject prefab)
    {
        AddBox(prefab, "piece_nonsolid", new Vector3(0f, 0.06f, 0f), new Vector3(0.36f, 0.12f, 0.36f));

        Container container = prefab.GetComponent<Container>() ?? throw new InvalidOperationException("the chest has no Container.");
        container.m_name = Translations.Token(PrefabName);
        container.m_width = 2;
        container.m_height = 1;
        container.m_open = null;
        container.m_closed = null;
        prefab.AddComponent<FeedingTroughComponent>();
    }

    /// <inheritdoc/>
    protected override void ApplyVisual(Transform visual)
    {
        VisualHelper.CreateModel(visual, ForagingAssets.LoadMesh("dogbowl"), ForagingAssets.LoadTexture("dogbowl_albedo"),
            Template("wood_pole2", "New"), Vector3.zero, Quaternion.identity, Size);
    }
}
