using BrudvikWhiteHilt.Pieces.Defenses;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Bestiary;

/// <summary>A field guide on the existing guestbook stand, built with the hammer.</summary>
public sealed class BeastBookPiece : DefensePieceBase
{
    /// <inheritdoc/>
    protected override string LayoutName => "svartbok";
    /// <inheritdoc/>
    protected override string VisualLayoutName => "gjestebok";
    /// <inheritdoc/>
    protected override string FullName => "Black Bestiary";
    /// <inheritdoc/>
    protected override string Description => "A book of the nine black beasts: their dangers, material weaknesses, and the recipes and gathering places of their counters.";
    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Wood", Amount = 6, Recover = true },
        new() { Item = "LeatherScraps", Amount = 3, Recover = true },
        new() { Item = "Feathers", Amount = 1, Recover = false },
        new() { Item = "Coal", Amount = 2, Recover = false }
    };
    /// <inheritdoc/>
    protected override float Health => BeastBookSettings.Health;
    /// <inheritdoc/>
    protected override string Category => PieceCategories.Furniture;
    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Start;

    /// <summary>Creates the book's build entry.</summary>
    /// <param name="manager">Piece manager.</param>
    public BeastBookPiece(PieceManager manager) : base(manager) { }

    /// <inheritdoc/>
    protected override void CustomizePrefab(GameObject prefab, DefensePieceData data, IDictionary<string, Transform> groups)
    {
        prefab.AddComponent<BeastBookStand>();
    }
}