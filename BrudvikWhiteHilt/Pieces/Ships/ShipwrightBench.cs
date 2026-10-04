using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Roofing;
using BrudvikWhiteHilt.Pieces.Defenses;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Ships;

/// <summary>
/// The Shipwright's Bench: a workbench with a ship's anchor, an anchor chain, a coil of rope, a fishing net and a tar
/// bucket, the crafting station for the White Hilt Ship upgrades and the Ship Hammer, and the station the Harbour Anchor
/// and the Mooring Post are built at. It stands in the open on a jetty: it needs no roof and does not rot in the rain.
/// </summary>
public class ShipwrightBench : DefensePieceBase
{
    /// <summary>
    /// Prefab name of the bench, used as the crafting station of its recipes.
    /// </summary>
    public const string StationPrefabName = "piece_whitehilt_skipsbyggerbenk";

    // The tip of the rushlight on the bench top (build_defenses.py BENCH_LIGHT, plus the rushlight's 0.5 m).
    private static readonly Vector3 LightPosition = new(-1.3f, 1.51f, 0.38f);

    /// <summary>
    /// Constructor for the ShipwrightBench class. Registers the English text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public ShipwrightBench(PieceManager instance) : base(instance)
    {
    }

    /// <inheritdoc/>
    protected override string LayoutName => "skipsbyggerbenk";

    /// <inheritdoc/>
    protected override string FullName => "Shipwright's Bench";

    /// <inheritdoc/>
    protected override string Description => "A sturdy bench with an anchor, rope, net and a bucket of tar. Make ship upgrades and the Ship Hammer here. It needs no roof, so it can stand out on the jetty.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "FineWood", Amount = 10, Recover = true },
        new() { Item = RoofMaterials.PineTar, Amount = 2, Recover = true },
        new() { Item = "Bronze", Amount = 2, Recover = true },
        new() { Item = "LeatherScraps", Amount = 4, Recover = true }
    };

    /// <inheritdoc/>
    protected override float Health => 1000f;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.BlackForest;

    /// <inheritdoc/>
    protected override string Category => PieceCategories.Crafting;

    /// <summary>
    /// Makes the cloned workbench a station of its own that works in the open, and lights the rushlight.
    /// </summary>
    /// <param name="prefab">The piece prefab.</param>
    /// <param name="data">The piece in the layout.</param>
    /// <param name="groups">Moving groups by name.</param>
    protected override void CustomizePrefab(GameObject prefab, DefensePieceData data, IDictionary<string, Transform> groups)
    {
        // Recipes match their station by name, so a unique name keeps the workbench recipes off the bench.
        CraftingStation station = prefab.GetComponent<CraftingStation>();
        station.m_name = Translations.Token(StationPrefabName);
        station.m_craftRequireRoof = false;
        station.m_craftRequireFire = false;
        station.m_showBasicRecipies = false;

        // Tarred wood on a jetty: rain and an open sky do not wear it down.
        WearNTear wear = prefab.GetComponent<WearNTear>();
        if (wear != null)
        {
            wear.m_noRoofWear = false;
        }

        if (VisualHelper.IsHeadless)
        {
            return;
        }

        GameObject lightObject = new("RushlightGlow");
        lightObject.transform.SetParent(prefab.transform, false);
        lightObject.transform.localPosition = LightPosition;
        Light glow = lightObject.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.color = new Color(1f, 0.72f, 0.38f);
        glow.range = 3f;
        glow.intensity = 0.8f;
        glow.shadows = LightShadows.None;
    }
}
