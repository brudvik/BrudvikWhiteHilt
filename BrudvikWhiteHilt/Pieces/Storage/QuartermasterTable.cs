using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Pieces.Defenses;
using BrudvikWhiteHilt.Progression;
using BrudvikWhiteHilt.Quartermaster;
using Jotunn.Configs;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Storage;

/// <summary>
/// The Quartermaster's Table: a plank counter with a tally board, a ledger, a chest and crates. Using it opens the
/// store of every chest, cart and ship hold around it. It needs no workbench, so it can stand on its own in a
/// storehouse or at an outpost.
/// </summary>
public class QuartermasterTable : DefensePieceBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public QuartermasterTable(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "forradsbord";

    /// <inheritdoc/>
    protected override string FullName => "Quartermaster's Table";

    /// <inheritdoc/>
    protected override string Description => "A counter with a ledger and a tally board. Use it to see everything in the chests, carts and ship holds around it and take what you need, a stack at a time, or to pack the materials for a build.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Wood", Amount = 20, Recover = true },
        new() { Item = "Stone", Amount = 8, Recover = true },
        new() { Item = "LeatherScraps", Amount = 4, Recover = true },
        new() { Item = "Flint", Amount = 4, Recover = true }
    };

    /// <inheritdoc/>
    protected override float Health => 1000f;

    /// <inheritdoc/>
    protected override string Category => PieceCategories.Crafting;

    /// <inheritdoc/>
    protected override string BuildStation => null;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Start;

    /// <inheritdoc/>
    protected override void CustomizePrefab(GameObject prefab, DefensePieceData data, IDictionary<string, Transform> groups)
    {
        // The cloned pole needs a workbench; the table stands on its own.
        prefab.GetComponent<Piece>().m_craftingStation = null;
        if (!VisualHelper.IsHeadless)
        {
            CraftingStation workbench = PrefabManager.Instance.GetPrefab("piece_workbench").GetComponent<CraftingStation>();
            GameObject marker = Object.Instantiate(workbench.m_areaMarker, prefab.transform);
            marker.name = QuartermasterStand.AreaMarkerName;
            marker.transform.localPosition = Vector3.zero;
            marker.SetActive(false);
        }

        prefab.AddComponent<QuartermasterStand>();
    }
}
