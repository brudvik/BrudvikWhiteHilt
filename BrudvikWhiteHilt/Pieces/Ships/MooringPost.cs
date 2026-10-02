using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Pieces.Defenses;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Ships;

/// <summary>
/// The Mooring Post: a thick post with a coil of rope, built from vanilla meshes. Use it to moor the nearest ship, which
/// then lies still where it is until it is cast off at the post.
/// </summary>
public class MooringPost : DefensePieceBase
{
    /// <summary>
    /// Height of the rope coil on the post, where the rope to the ship leaves (build_defenses.py MOORING_ROPE_Y).
    /// </summary>
    public const float RopeHeight = 1f;

    /// <summary>
    /// Constructor for the MooringPost class. Registers the English texts.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public MooringPost(PieceManager instance) : base(instance)
    {
        Translations.AddEnglish("whitehilt_moor_moor", "Moor the ship");
        Translations.AddEnglish("whitehilt_moor_castoff", "Cast off");
        Translations.AddEnglish("whitehilt_moor_noship", "No ship within {0} m");
        Translations.AddEnglish("msg_whitehilt_moor_moored", "The ship is moored");
        Translations.AddEnglish("msg_whitehilt_moor_castoff", "The ship is cast off");
        Translations.AddEnglish("msg_whitehilt_ship_moored", "The ship is moored: cast off at the mooring post");
        Translations.AddDynamic($"{PrefabName}_description", () => new object[] { Translations.Number(ShipSettings.MooringRange.Value) });
    }

    /// <inheritdoc/>
    protected override string LayoutName => "fortoyningspale";

    /// <inheritdoc/>
    protected override string FullName => "Mooring Post";

    /// <inheritdoc/>
    protected override string Description => "A thick post with a coil of rope. Use it to moor the nearest ship within {0} m: the ship lies still where it is, rocking on the waves, and cannot sail until it is cast off here.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "FineWood", Amount = 4, Recover = true },
        new() { Item = "Iron", Amount = 1, Recover = true },
        new() { Item = "LeatherScraps", Amount = 4, Recover = true }
    };

    /// <inheritdoc/>
    protected override float Health => 500f;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Swamp;

    /// <inheritdoc/>
    protected override string Category => PieceCategories.Misc;

    /// <summary>
    /// Adds the mooring to the post.
    /// </summary>
    /// <param name="prefab">The piece prefab.</param>
    /// <param name="data">The piece in the layout.</param>
    /// <param name="groups">Moving groups by name.</param>
    protected override void CustomizePrefab(GameObject prefab, DefensePieceData data, IDictionary<string, Transform> groups)
    {
        prefab.AddComponent<MooringPostComponent>();
    }
}
