using BrudvikWhiteHilt.Pieces.Defenses;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.LogHouse;

/// <summary>A 2 m log wall with a window closed by two shutters that swing out.</summary>
public abstract class ShutteredLogWallBase : LogHouseDoorBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    protected ShutteredLogWallBase(PieceManager instance) : base(instance)
    {
    }

    /// <inheritdoc/>
    protected override (string Group, Vector3 Axis)[] OneWaySwings => new[]
    {
        ("leaf_right", Vector3.up),
        ("leaf_left", Vector3.down)
    };

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "RoundLog", Amount = 4, Recover = true },
        new() { Item = "Wood", Amount = 2, Recover = true }
    };

    /// <inheritdoc/>
    protected override float Health => 800f;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.BlackForest;
}

/// <summary>A plain log wall with a shuttered window.</summary>
public sealed class LogWallWindow : ShutteredLogWallBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public LogWallWindow(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "laftvegg_vindu";
    /// <inheritdoc/>
    protected override string FullName => "Log Wall with Window";
    /// <inheritdoc/>
    protected override string Description => "A log wall 2 m long with a window 1 m wide and 1 m high, closed by two shutters that swing out. Like other windows they close by themselves when rain or night comes.";
}

/// <summary>An offset log wall with a shuttered window.</summary>
public sealed class OffsetLogWallWindow : ShutteredLogWallBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public OffsetLogWallWindow(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "laftvegg_forskutt_vindu";
    /// <inheritdoc/>
    protected override string FullName => "Offset Log Wall with Window";
    /// <inheritdoc/>
    protected override string Description => "An offset log wall 2 m long with a shuttered window 1 m wide, for the walls built with the Offset Log Wall.";
}

/// <summary>A plain log wall with a glugg, a small open window.</summary>
public sealed class LogWallGlugg : LogPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public LogWallGlugg(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "laftvegg_glugg";
    /// <inheritdoc/>
    protected override string FullName => "Log Wall with Glugg";
    /// <inheritdoc/>
    protected override string Description => "A log wall 2 m long with a glugg: a small opening one log high and 0.6 m wide, framed in boards, as in the oldest houses.";
    /// <inheritdoc/>
    protected override int Logs => 4;
}

/// <summary>An offset log wall with a glugg.</summary>
public sealed class OffsetLogWallGlugg : LogPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public OffsetLogWallGlugg(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "laftvegg_forskutt_glugg";
    /// <inheritdoc/>
    protected override string FullName => "Offset Log Wall with Glugg";
    /// <inheritdoc/>
    protected override string Description => "An offset log wall 2 m long with a glugg, a small framed opening one log high.";
    /// <inheritdoc/>
    protected override int Logs => 4;
}

/// <summary>A floor hatch with a ladder down to a cellar.</summary>
public sealed class FloorHatch : LogHouseDoorBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public FloorHatch(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "lem";
    /// <inheritdoc/>
    protected override string FullName => "Floor Hatch";
    /// <inheritdoc/>
    protected override string Description => "A plank floor 2 × 2 m with a hatch in one corner and a ladder 2 m down under it, to a cellar. Use the hatch to lift its lid; use the ladder below to climb up, and from the floor beside the hatch the alternate key and Use to climb down.";
    /// <inheritdoc/>
    protected override (string Group, Vector3 Axis)[] OneWaySwings => new[] { ("lid", Vector3.left) };
    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Wood", Amount = 8, Recover = true }
    };
    /// <inheritdoc/>
    protected override float Health => 300f;
    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Start;
}

/// <summary>A wooden piece of the house's inside that costs only wood: stairs, ladders and railings.</summary>
public abstract class TimberPieceBase : DefensePieceBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    protected TimberPieceBase(PieceManager instance) : base(instance)
    {
    }

    /// <summary>Wood needed.</summary>
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

/// <summary>A stair 1 m wide reaching a loft 2 m up.</summary>
public sealed class NarrowStair : TimberPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public NarrowStair(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "smal_trapp";
    /// <inheritdoc/>
    protected override string FullName => "Narrow Stair";
    /// <inheritdoc/>
    protected override string Description => "A stair 1 m wide rising 2 m over 4 m, as steep as the vanilla stair, up to a loft or the next storey in one piece.";
    /// <inheritdoc/>
    protected override int Wood => 6;
}

/// <summary>A spiral stair round a post.</summary>
public sealed class SpiralStair : TimberPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public SpiralStair(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "vindeltrapp";
    /// <inheritdoc/>
    protected override string FullName => "Spiral Stair";
    /// <inheritdoc/>
    protected override string Description => "Treads round a post with a handrail, rising 2 m in three quarters of a turn, 2.4 m across. Stack another on top, turned a quarter back, for the next storey.";
    /// <inheritdoc/>
    protected override int Wood => 14;
}

/// <summary>A ladder up to a loft.</summary>
public sealed class LoftLadder : TimberPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public LoftLadder(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "stige";
    /// <inheritdoc/>
    protected override string FullName => "Loft Ladder";
    /// <inheritdoc/>
    protected override string Description => "A ladder 2.3 m long against the edge of a loft 2 m up. Use it to climb up; on the loft, the alternate key and Use take you down.";
    /// <inheritdoc/>
    protected override int Wood => 4;
}

/// <summary>A 2 m railing.</summary>
public sealed class Railing : TimberPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public Railing(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "rekkverk";
    /// <inheritdoc/>
    protected override string FullName => "Railing";
    /// <inheritdoc/>
    protected override string Description => "A railing 2 m long and 1 m high of hewn posts, rails and balusters, for a loft, a gallery or a stair well.";
    /// <inheritdoc/>
    protected override int Wood => 3;
}

/// <summary>A 1 m railing.</summary>
public sealed class RailingShort : TimberPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public RailingShort(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "rekkverk_1m";
    /// <inheritdoc/>
    protected override string FullName => "Railing 1 m";
    /// <inheritdoc/>
    protected override string Description => "A railing 1 m long, to close a gap.";
    /// <inheritdoc/>
    protected override int Wood => 2;
}

/// <summary>A railing along a stair.</summary>
public sealed class StairRailing : TimberPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public StairRailing(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "trapperekkverk";
    /// <inheritdoc/>
    protected override string FullName => "Stair Railing";
    /// <inheritdoc/>
    protected override string Description => "A railing at the slope of the vanilla stair and the Narrow Stair, 2 m along it and rising 1 m. Two go along a Narrow Stair.";
    /// <inheritdoc/>
    protected override int Wood => 3;
}

/// <summary>A stone wall for a dug-out cellar.</summary>
public sealed class CellarWall : FoundationBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public CellarWall(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "kjellermur";
    /// <inheritdoc/>
    protected override string FullName => "Cellar Wall";
    /// <inheritdoc/>
    protected override string Description => "Field stones laid dry, 2 m long and 2 m high, for the walls of a cellar dug out under the house. The floor above can rest on it, and a Floor Hatch leads down.";
    /// <inheritdoc/>
    protected override int Stone => 20;
}
