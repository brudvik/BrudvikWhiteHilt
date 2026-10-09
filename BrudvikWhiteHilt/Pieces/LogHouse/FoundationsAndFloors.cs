using BrudvikWhiteHilt.Pieces.Defenses;
using BrudvikWhiteHilt.Pieces.Stonework;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Pieces.LogHouse;

/// <summary>
/// A dry-laid stone foundation (grunnmur) for a log house: stone in the building rules, so it holds what stands on it
/// like the ground does, but laid at the workbench like the house on it.
/// </summary>
public abstract class FoundationBase : StoneworkPieceBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    protected FoundationBase(PieceManager instance) : base(instance)
    {
    }

    /// <summary>Stone needed.</summary>
    protected abstract int Stone { get; }

    /// <inheritdoc/>
    protected override string BuildStation => CraftingStations.Workbench;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Stone", Amount = Stone, Recover = true }
    };

    /// <inheritdoc/>
    protected override float Health => 125f * Stone;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Start;
}

/// <summary>A 2 m stone foundation, 1 m high.</summary>
public sealed class StoneFoundation : FoundationBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public StoneFoundation(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "grunnmur";
    /// <inheritdoc/>
    protected override string FullName => "Stone Foundation";
    /// <inheritdoc/>
    protected override string Description => "Big field stones laid dry, 2 m long and 1 m high, reaching 0.6 m into the ground. Walls and floors snap to its top; it keeps a log house off the damp ground and level on a slope.";
    /// <inheritdoc/>
    protected override int Stone => 12;
}

/// <summary>A 4 m stone foundation, 1 m high.</summary>
public sealed class StoneFoundationLong : FoundationBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public StoneFoundationLong(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "grunnmur_4m";
    /// <inheritdoc/>
    protected override string FullName => "Stone Foundation 4 m";
    /// <inheritdoc/>
    protected override string Description => "A stone foundation 4 m long and 1 m high.";
    /// <inheritdoc/>
    protected override int Stone => 24;
}

/// <summary>A big squared stone for a foundation corner or under a beam.</summary>
public sealed class Cornerstone : FoundationBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public Cornerstone(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "hjornestein";
    /// <inheritdoc/>
    protected override string FullName => "Cornerstone";
    /// <inheritdoc/>
    protected override string Description => "A pillar of big stones 1 m high, for the corner of a foundation where the log ends stick out, or alone under a floor.";
    /// <inheritdoc/>
    protected override int Stone => 8;
}

/// <summary>A plank floor larger than the vanilla one, laid from its boards.</summary>
public abstract class PlankFloorBase : DefensePieceBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    protected PlankFloorBase(PieceManager instance) : base(instance)
    {
    }

    /// <summary>Wood needed: as much as the vanilla floors it covers.</summary>
    protected abstract int Wood { get; }

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Wood", Amount = Wood, Recover = true }
    };

    /// <inheritdoc/>
    protected override float Health => 50f * Wood;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Start;
}

/// <summary>A 4 x 4 m plank floor.</summary>
public sealed class PlankFloor4x4 : PlankFloorBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public PlankFloor4x4(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "plankegulv_4x4";
    /// <inheritdoc/>
    protected override string FullName => "Plank Floor 4×4";
    /// <inheritdoc/>
    protected override string Description => "A plank floor 4 m by 4 m, laid at once.";
    /// <inheritdoc/>
    protected override int Wood => 8;
}

/// <summary>A 4 x 2 m plank floor.</summary>
public sealed class PlankFloor4x2 : PlankFloorBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public PlankFloor4x2(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "plankegulv_4x2";
    /// <inheritdoc/>
    protected override string FullName => "Plank Floor 4×2";
    /// <inheritdoc/>
    protected override string Description => "A plank floor 4 m by 2 m.";
    /// <inheritdoc/>
    protected override int Wood => 4;
}

/// <summary>A 2 x 1 m plank floor.</summary>
public sealed class PlankFloor2x1 : PlankFloorBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public PlankFloor2x1(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "plankegulv_2x1";
    /// <inheritdoc/>
    protected override string FullName => "Plank Floor 2×1";
    /// <inheritdoc/>
    protected override string Description => "A plank floor 2 m by 1 m, for a strip along a wall.";
    /// <inheritdoc/>
    protected override int Wood => 1;
}

/// <summary>A 4 x 4 m plank floor on joists, for a loft.</summary>
public sealed class JoistedFloor : PlankFloorBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public JoistedFloor(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "bjelkelag_4x4";
    /// <inheritdoc/>
    protected override string FullName => "Joisted Floor 4×4";
    /// <inheritdoc/>
    protected override string Description => "A plank floor 4 m by 4 m on hewn joists 1 m apart, for a loft: from below you see the joists, not the underside of the boards.";
    /// <inheritdoc/>
    protected override int Wood => 12;
}
