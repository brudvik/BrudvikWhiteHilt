using BrudvikWhiteHilt.Pieces.Defenses;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Stonework;

/// <summary>
/// A hnefatafl board set up for a game, the board game of the Norse: dark attackers along the edges and light defenders
/// round an amber king. A piece of furniture for the table that gives comfort.
/// </summary>
public class HnefataflBoard : DefensePieceBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run. Registers the English text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public HnefataflBoard(PieceManager instance) : base(instance)
    {
    }

    /// <inheritdoc/>
    protected override string LayoutName => "hnefatafl";

    /// <inheritdoc/>
    protected override string FullName => "Hnefatafl Board";

    /// <inheritdoc/>
    protected override string Description => "The king's table, set up for a game: dark stones besiege the light ones round an amber king. Put it on a table for a cosier home.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Wood", Amount = 2, Recover = true },
        new() { Item = "Stone", Amount = 6, Recover = true },
        new() { Item = "Amber", Amount = 1, Recover = true }
    };

    /// <inheritdoc/>
    protected override float Health => 100f;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Start;

    /// <inheritdoc/>
    protected override string Category => PieceCategories.Furniture;

    /// <inheritdoc/>
    protected override string BuildStation => CraftingStations.Stonecutter;

    /// <summary>
    /// Gives comfort and lets the board stand on tables.
    /// </summary>
    /// <param name="prefab">The piece prefab.</param>
    /// <param name="data">The piece in the layout.</param>
    /// <param name="groups">Moving groups by name.</param>
    protected override void CustomizePrefab(GameObject prefab, DefensePieceData data, IDictionary<string, Transform> groups)
    {
        Piece piece = prefab.GetComponent<Piece>();
        piece.m_comfort = 1;
        piece.m_comfortGroup = Piece.ComfortGroup.None;
        piece.m_groundOnly = false;
        piece.m_groundPiece = false;
    }
}
