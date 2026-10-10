using BrudvikWhiteHilt.Pieces.Defenses;
using BrudvikWhiteHilt.Pieces.LogHouse;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Pieces.Carvings;

// Carved interlace: friezes for the face of a wall, bands for posts and a carved portal. The carving is a relief model
// with a normal map (AssetSource/Carvings/build_friezes.py); the layout places it (AssetSource/Preview/build_carvings.py).

/// <summary>A carved frieze or band, of fine wood, built near a Workbench.</summary>
public abstract class CarvingPieceBase : DefensePieceBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    protected CarvingPieceBase(PieceManager instance) : base(instance)
    {
    }

    /// <summary>The interlace: flette, tau, ringkjede or slyng.</summary>
    protected abstract string Pattern { get; }

    /// <summary>The pattern's English name, e.g. "Plait".</summary>
    protected abstract string PatternName { get; }

    /// <inheritdoc/>
    protected override string Category => PieceCategories.Furniture;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "FineWood", Amount = 2, Recover = true }
    };

    /// <inheritdoc/>
    protected override float Health => 200f;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.BlackForest;
}

/// <summary>A carved frieze 2 m long for the face of a wall.</summary>
public abstract class FriezeBase : CarvingPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    protected FriezeBase(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => $"frise_{Pattern}";
    /// <inheritdoc/>
    protected override string FullName => $"Carved Frieze, {PatternName}";
    /// <inheritdoc/>
    protected override string Description => $"A board 2 m long and 0.3 m high carved with {PatternName.ToLowerInvariant()} between two rims, for the face of a wall, over a door or under the eaves.";
}

/// <summary>A carved band for a post or a door frame.</summary>
public abstract class PostBandBase : CarvingPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    protected PostBandBase(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => $"stolpebord_{Pattern}";
    /// <inheritdoc/>
    protected override string FullName => $"Carved Post Board, {PatternName}";
    /// <inheritdoc/>
    protected override string Description => $"A board 2 m high and 0.3 m wide carved with {PatternName.ToLowerInvariant()}, for the face of a post or up the sides of a doorway.";
}

/// <summary>Plait frieze.</summary>
public sealed class FriezePlait : FriezeBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public FriezePlait(PieceManager instance) : base(instance) { }
    /// <inheritdoc/>
    protected override string Pattern => "flette";
    /// <inheritdoc/>
    protected override string PatternName => "Plait";
}

/// <summary>Cable frieze.</summary>
public sealed class FriezeCable : FriezeBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public FriezeCable(PieceManager instance) : base(instance) { }
    /// <inheritdoc/>
    protected override string Pattern => "tau";
    /// <inheritdoc/>
    protected override string PatternName => "Cable";
}

/// <summary>Ring chain frieze.</summary>
public sealed class FriezeRingChain : FriezeBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public FriezeRingChain(PieceManager instance) : base(instance) { }
    /// <inheritdoc/>
    protected override string Pattern => "ringkjede";
    /// <inheritdoc/>
    protected override string PatternName => "Ring Chain";
}

/// <summary>Urnes loops frieze.</summary>
public sealed class FriezeUrnes : FriezeBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public FriezeUrnes(PieceManager instance) : base(instance) { }
    /// <inheritdoc/>
    protected override string Pattern => "slyng";
    /// <inheritdoc/>
    protected override string PatternName => "Urnes Loops";
}

/// <summary>Plait post board.</summary>
public sealed class PostBandPlait : PostBandBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public PostBandPlait(PieceManager instance) : base(instance) { }
    /// <inheritdoc/>
    protected override string Pattern => "flette";
    /// <inheritdoc/>
    protected override string PatternName => "Plait";
}

/// <summary>Cable post board.</summary>
public sealed class PostBandCable : PostBandBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public PostBandCable(PieceManager instance) : base(instance) { }
    /// <inheritdoc/>
    protected override string Pattern => "tau";
    /// <inheritdoc/>
    protected override string PatternName => "Cable";
}

/// <summary>Ring chain post board.</summary>
public sealed class PostBandRingChain : PostBandBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public PostBandRingChain(PieceManager instance) : base(instance) { }
    /// <inheritdoc/>
    protected override string Pattern => "ringkjede";
    /// <inheritdoc/>
    protected override string PatternName => "Ring Chain";
}

/// <summary>Urnes loops post board.</summary>
public sealed class PostBandUrnes : PostBandBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public PostBandUrnes(PieceManager instance) : base(instance) { }
    /// <inheritdoc/>
    protected override string Pattern => "slyng";
    /// <inheritdoc/>
    protected override string PatternName => "Urnes Loops";
}

/// <summary>A doorway between posts carved with Urnes loops, a ring chain over the door.</summary>
public sealed class CarvedPortal : LogHouseDoorBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public CarvedPortal(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "utskaret_portal";
    /// <inheritdoc/>
    protected override string FullName => "Carved Portal";
    /// <inheritdoc/>
    protected override string Description => "A doorway 2 m wide and 3 m high between broad posts carved with Urnes loops up both faces and a ring chain over the door, as round the stave church portals. The door is 1.2 m wide and 2.6 m high.";
    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "FineWood", Amount = 12, Recover = true },
        new() { Item = "Wood", Amount = 4, Recover = true },
        new() { Item = "Resin", Amount = 2, Recover = false }
    };
    /// <inheritdoc/>
    protected override float Health => 700f;
    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.BlackForest;
}
