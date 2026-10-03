using BrudvikWhiteHilt.Guestbook;
using BrudvikWhiteHilt.Pieces.Defenses;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Guestbook;

/// <summary>
/// A guestbook on a stand: everyone can read who came by, who built and tore down what nearby, and when the place
/// was raided.
/// </summary>
public class GuestbookPiece : DefensePieceBase
{
    /// <summary>
    /// Constructor for the GuestbookPiece class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public GuestbookPiece(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "gjestebok";

    /// <inheritdoc/>
    protected override string FullName => "Guestbook";

    /// <inheritdoc/>
    protected override string Description => "An open book on a stand. It notes who comes by, who builds and tears down what nearby, and when the place is raided. Anyone can read it.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Wood", Amount = 6, Recover = true },
        new() { Item = "LeatherScraps", Amount = 3, Recover = true },
        new() { Item = "Feathers", Amount = 1, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Health => 300f;

    /// <inheritdoc/>
    protected override string Category => PieceCategories.Furniture;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Start;

    /// <inheritdoc/>
    protected override void CustomizePrefab(GameObject prefab, DefensePieceData data, IDictionary<string, Transform> groups)
    {
        prefab.AddComponent<GuestbookStand>();
    }
}
