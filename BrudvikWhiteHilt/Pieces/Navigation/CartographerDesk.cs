using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Pieces.Defenses;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Navigation;

/// <summary>
/// The Cartographer's Desk: a writing desk with charts and a sextant, the crafting station for the Navigator's Table
/// and the Pathfinder's Amulet. It works as an extension of the map table and can only be used near one.
/// </summary>
public class CartographerDesk : DefensePieceBase
{
    /// <summary>
    /// Prefab name of the desk, used as the crafting station of its recipes.
    /// </summary>
    public const string StationPrefabName = "piece_whitehilt_kartmakerbenk";

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run. Registers the English text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public CartographerDesk(PieceManager instance) : base(instance)
    {
        Translations.AddEnglish("msg_whitehilt_needmaptable", "The desk must stand within {0} m of a map table");
    }

    /// <inheritdoc/>
    protected override string LayoutName => "kartmakerbenk";

    /// <inheritdoc/>
    protected override string FullName => "Cartographer's Desk";

    /// <inheritdoc/>
    protected override string Description => "A writing desk with sea charts, a sextant and map scrolls. Place it within {0} m of a map table to make navigation gear.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "FineWood", Amount = 10, Recover = true },
        new() { Item = "Bronze", Amount = 2, Recover = true },
        new() { Item = "DeerHide", Amount = 2, Recover = true },
        new() { Item = "Resin", Amount = 4, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Health => 1000f;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.BlackForest;

    /// <inheritdoc/>
    protected override string Category => PieceCategories.Crafting;

    /// <summary>
    /// Makes the cloned workbench a station of its own that needs no roof, and adds the map table check.
    /// </summary>
    /// <param name="prefab">The piece prefab.</param>
    /// <param name="data">The piece in the layout.</param>
    /// <param name="groups">Moving groups by name.</param>
    protected override void CustomizePrefab(GameObject prefab, DefensePieceData data, IDictionary<string, Transform> groups)
    {
        // Recipes match their station by name, so a unique name keeps the workbench recipes off the desk.
        CraftingStation station = prefab.GetComponent<CraftingStation>();
        station.m_name = Translations.Token(StationPrefabName);
        station.m_craftRequireRoof = false;
        station.m_craftRequireFire = false;
        station.m_showBasicRecipies = false;
        prefab.AddComponent<MapTableProximity>();
        if (!VisualHelper.IsHeadless)
        {
            prefab.AddComponent<SkillScrolls>();
        }
    }
}
