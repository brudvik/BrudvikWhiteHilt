using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Pieces.Defenses;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Textiles;

/// <summary>
/// A warp-weighted loom: the crafting station for linen cloth, and the place where capes are dyed with a Paint Pot.
/// </summary>
public class Loom : DefensePieceBase
{
    /// <summary>Prefab name of the loom, used as the crafting station of its recipes.</summary>
    public const string StationPrefabName = "piece_whitehilt_vev";

    /// <summary>
    /// Constructor for the Loom class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public Loom(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "vev";

    /// <inheritdoc/>
    protected override string FullName => "Loom";

    /// <inheritdoc/>
    protected override string Description => "An upright loom with stone weights on the warp. Weaves linen thread into cloth; stand at it and use a Paint Pot to dye the cape you wear.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "FineWood", Amount = 8, Recover = true },
        new() { Item = "Stone", Amount = 6, Recover = true },
        new() { Item = "LinenThread", Amount = 5, Recover = true }
    };

    /// <inheritdoc/>
    protected override float Health => 600f;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Plains;

    /// <inheritdoc/>
    protected override string Category => PieceCategories.Crafting;

    /// <summary>
    /// True if a crafting station is a loom.
    /// </summary>
    /// <param name="station">The station.</param>
    /// <returns>True if so.</returns>
    public static bool IsLoom(CraftingStation station)
    {
        return station != null && station.m_name == Translations.Token(StationPrefabName);
    }

    // Recipes match their station by name, so a unique name keeps the workbench recipes off the loom.
    /// <inheritdoc/>
    protected override void CustomizePrefab(GameObject prefab, DefensePieceData data, IDictionary<string, Transform> groups)
    {
        CraftingStation station = prefab.GetComponent<CraftingStation>();
        station.m_name = Translations.Token(StationPrefabName);
        station.m_craftRequireRoof = false;
        station.m_craftRequireFire = false;
        station.m_showBasicRecipies = false;
    }
}
