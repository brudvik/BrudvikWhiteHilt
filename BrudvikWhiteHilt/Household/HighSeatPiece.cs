using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Pieces.Defenses;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Household;

/// <summary>
/// The High Seat as a building piece: the carved seat between the two high-seat pillars from the layout
/// (build_household.py, as approved in round A), a place to sit, the farm's reach ring copied from the workbench's, and
/// the <see cref="HighSeat"/> behaviour.
/// </summary>
public class HighSeatPiece : DefensePieceBase
{
    // Where the player sits, facing out over the hall. The game's sitting pose stands on its point with the feet and has
    // the seat half a metre above it, so the point is on the top step (the layout's DAIS) under the cushion.
    private static readonly Vector3 sitPoint = new(0f, 0.32f, -0.15f);

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public HighSeatPiece(PieceManager instance) : base(instance)
    {
    }

    /// <inheritdoc/>
    public override bool Enabled => HouseholdSettings.Enabled.Value;

    /// <inheritdoc/>
    protected override string LayoutName => "highseat";

    /// <inheritdoc/>
    protected override string FullName => "High Seat";

    /// <inheritdoc/>
    protected override string Description => "The master's seat between the high-seat pillars, the heart of a farm. Sit in it to lead the household that lives and works within its reach.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "FineWood", Amount = 12, Recover = true },
        new() { Item = "RoundLog", Amount = 8, Recover = true },
        new() { Item = "BronzeNails", Amount = 10, Recover = true }
    };

    /// <inheritdoc/>
    protected override float Health => 1200f;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.BlackForest;

    /// <inheritdoc/>
    protected override string Category => PieceCategories.Furniture;

    /// <inheritdoc/>
    protected override string BuildStation => CraftingStations.Workbench;

    /// <summary>
    /// Adds the place to sit, the reach ring, comfort as a chair and the High Seat's behaviour.
    /// </summary>
    /// <param name="prefab">The piece prefab.</param>
    /// <param name="data">The piece in the layout.</param>
    /// <param name="groups">Moving groups by name.</param>
    protected override void CustomizePrefab(GameObject prefab, DefensePieceData data, IDictionary<string, Transform> groups)
    {
        GameObject sit = new(HighSeat.SitPointName);
        sit.transform.SetParent(prefab.transform, false);
        sit.transform.localPosition = sitPoint;

        if (!VisualHelper.IsHeadless)
        {
            CraftingStation workbench = PrefabManager.Instance.GetPrefab("piece_workbench").GetComponent<CraftingStation>();
            GameObject marker = Object.Instantiate(workbench.m_areaMarker, prefab.transform);
            marker.name = HighSeat.AreaMarkerName;
            marker.transform.localPosition = Vector3.zero;
            marker.SetActive(false);
        }

        Piece piece = prefab.GetComponent<Piece>();
        piece.m_comfort = 2;
        piece.m_comfortGroup = Piece.ComfortGroup.Chair;
        prefab.AddComponent<HighSeat>();
    }
}
