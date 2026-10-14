using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Pieces;
using BrudvikWhiteHilt.Pieces.Defenses;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.TradingPosts;

/// <summary>
/// The base of the trading posts' own pieces: made from the layout like the defences, placed only by the mod
/// (<see cref="TradingPostBuilder"/>), never on the hammer and without settings of their own.
/// </summary>
public abstract class TradingPostPieceBase : DefensePieceBase, IUnlistedPiece
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    protected TradingPostPieceBase(PieceManager instance) : base(instance)
    {
    }

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => Array.Empty<RequirementConfig>();

    /// <inheritdoc/>
    protected override float Health => 10000f;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Start;
}

/// <summary>
/// The gate where a trading post's jetty meets the landing: two posts, a beam and a sign under it.
/// </summary>
public sealed class TradingPostGate : TradingPostPieceBase
{
    /// <summary>
    /// Creates the definition.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public TradingPostGate(PieceManager instance) : base(instance)
    {
    }

    /// <inheritdoc/>
    protected override string LayoutName => "handelsport";

    /// <inheritdoc/>
    protected override string FullName => "Trading Post Gate";

    /// <inheritdoc/>
    protected override string Description => "The gate of a trading post.";
}

/// <summary>
/// The flag pole at the head of a trading post's jetty, with an arm for a lamp and a long pennant at the top.
/// </summary>
public sealed class TradingPostFlagPole : TradingPostPieceBase
{
    // build_water.py PENNANT_HEIGHT: where the pennant's hoist starts on the pole.
    private const float PennantHeight = 5.5f;

    /// <summary>
    /// Creates the definition.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public TradingPostFlagPole(PieceManager instance) : base(instance)
    {
    }

    /// <inheritdoc/>
    protected override string LayoutName => "flaggstang";

    /// <inheritdoc/>
    protected override string FullName => "Trading Post Flag";

    /// <inheritdoc/>
    protected override string Description => "The flag of a trading post, seen from the sea.";

    /// <summary>
    /// Hangs the pennant on the pole. Its colours follow the post's biome once the traders come (plan phase 4); for
    /// now it has the market stall's red and white.
    /// </summary>
    /// <param name="prefab">The piece prefab.</param>
    /// <param name="data">The piece in the layout.</param>
    /// <param name="groups">Moving groups by name.</param>
    protected override void CustomizePrefab(GameObject prefab, DefensePieceData data, IDictionary<string, Transform> groups)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        Transform visual = prefab.transform.Find("New");
        Renderer template = visual != null ? visual.GetComponentInChildren<MeshRenderer>(true) : null;
        if (template == null)
        {
            return;
        }

        GameObject pennant = VisualHelper.CreateModel(visual, TradingPostPennant.Mesh, TradingPostPennant.Texture, template,
            new Vector3(0f, PennantHeight, 0f), Quaternion.identity, 1f);
        pennant.name = "pennant";
    }
}

/// <summary>
/// A long pile the mod stands under a trading post's jetty where the sea bed lies deeper than the jetty's own piles.
/// </summary>
public sealed class TradingPostPile : TradingPostPieceBase
{
    /// <summary>Prefab name of the pile, for the builder.</summary>
    public const string PilePrefabName = "piece_whitehilt_handelspaele";

    /// <summary>
    /// Creates the definition.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public TradingPostPile(PieceManager instance) : base(instance)
    {
    }

    /// <inheritdoc/>
    protected override string LayoutName => "handelspaele";

    /// <inheritdoc/>
    protected override string FullName => "Trading Post Pile";

    /// <inheritdoc/>
    protected override string Description => "A long pile under a trading post's jetty.";
}
