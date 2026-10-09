using BrudvikWhiteHilt.Pieces.Defenses;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Pieces.LogHouse;

/// <summary>
/// A piece of laft, the Norwegian log wall: core wood logs lying on top of each other, crossing at the corners. The
/// shapes are in the layout (AssetSource/Preview/build_log_house.py), which explains the plain and offset kinds.
/// </summary>
public abstract class LogPieceBase : DefensePieceBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    protected LogPieceBase(PieceManager instance) : base(instance)
    {
    }

    /// <summary>Core wood needed.</summary>
    protected abstract int Logs { get; }

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "RoundLog", Amount = Logs, Recover = true }
    };

    /// <inheritdoc/>
    protected override float Health => 200f * Logs;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.BlackForest;
}

/// <summary>A 2 m log wall, a whole log on the floor.</summary>
public sealed class LogWall : LogPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public LogWall(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "laftvegg";
    /// <inheritdoc/>
    protected override string FullName => "Log Wall";
    /// <inheritdoc/>
    protected override string Description => "Four core wood logs laid on each other, 2 m long and 2 m high. Build two opposite walls of a house with it and the other two with the Offset Log Wall, so the logs cross at the corners.";
    /// <inheritdoc/>
    protected override int Logs => 4;
}

/// <summary>A 1 m log wall.</summary>
public sealed class LogWallShort : LogPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public LogWallShort(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "laftvegg_1m";
    /// <inheritdoc/>
    protected override string FullName => "Log Wall 1 m";
    /// <inheritdoc/>
    protected override string Description => "A log wall 1 m long and 2 m high, to close a gap.";
    /// <inheritdoc/>
    protected override int Logs => 2;
}

/// <summary>A 4 m log wall of long logs.</summary>
public sealed class LogWallLong : LogPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public LogWallLong(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "laftvegg_4m";
    /// <inheritdoc/>
    protected override string FullName => "Log Wall 4 m";
    /// <inheritdoc/>
    protected override string Description => "A log wall of whole 4 m logs, 2 m high, without a joint in the middle.";
    /// <inheritdoc/>
    protected override int Logs => 8;
}

/// <summary>A 2 m log wall whose courses lie half a log higher than the plain wall's.</summary>
public sealed class OffsetLogWall : LogPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public OffsetLogWall(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "laftvegg_forskutt";
    /// <inheritdoc/>
    protected override string FullName => "Offset Log Wall";
    /// <inheritdoc/>
    protected override string Description => "A log wall 2 m long and 2 m high whose logs lie half a log higher than the plain Log Wall's, the lowest sunk half into the floor or foundation. Build the two walls that meet the plain ones with it, so the logs cross at the corners.";
    /// <inheritdoc/>
    protected override int Logs => 4;
}

/// <summary>A 1 m offset log wall.</summary>
public sealed class OffsetLogWallShort : LogPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public OffsetLogWallShort(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "laftvegg_forskutt_1m";
    /// <inheritdoc/>
    protected override string FullName => "Offset Log Wall 1 m";
    /// <inheritdoc/>
    protected override string Description => "An offset log wall 1 m long and 2 m high, to close a gap.";
    /// <inheritdoc/>
    protected override int Logs => 2;
}

/// <summary>A 4 m offset log wall.</summary>
public sealed class OffsetLogWallLong : LogPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public OffsetLogWallLong(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "laftvegg_forskutt_4m";
    /// <inheritdoc/>
    protected override string FullName => "Offset Log Wall 4 m";
    /// <inheritdoc/>
    protected override string Description => "An offset log wall of whole 4 m logs, 2 m high.";
    /// <inheritdoc/>
    protected override int Logs => 8;
}

/// <summary>A 1 m high offset log wall, for under a 26° gable.</summary>
public sealed class OffsetLogWallLow : LogPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public OffsetLogWallLow(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "laftvegg_forskutt_lav";
    /// <inheritdoc/>
    protected override string FullName => "Low Offset Log Wall";
    /// <inheritdoc/>
    protected override string Description => "An offset log wall 2 m long and 1 m high. Under a 26° gable it raises the inner half of the gable, as the roof rises 1 m over every 2 m.";
    /// <inheritdoc/>
    protected override int Logs => 2;
}

/// <summary>The crossing log ends at the corner of a house.</summary>
public sealed class LogCorner : LogPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public LogCorner(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "laftehjorne";
    /// <inheritdoc/>
    protected override string FullName => "Log Corner";
    /// <inheritdoc/>
    protected override string Description => "The log ends that cross where two log walls meet and stick out past the corner. Set it on the corner, 2 m high, and turn it until its logs follow the walls; where they do not, the mirrored corner fits.";
    /// <inheritdoc/>
    protected override int Logs => 3;
}

/// <summary>The log corner with the two kinds of wall swapped, for the other two corners of a house.</summary>
public sealed class LogCornerMirrored : LogPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public LogCornerMirrored(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "laftehjorne_speilet";
    /// <inheritdoc/>
    protected override string FullName => "Log Corner, Mirrored";
    /// <inheritdoc/>
    protected override string Description => "The log corner the other way round. A house takes two of each: the plain corner at two opposite corners and this one at the other two.";
    /// <inheritdoc/>
    protected override int Logs => 3;
}

/// <summary>Where an inner wall meets an outer one, its logs running through and out the other side.</summary>
public sealed class LogJoint : LogPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public LogJoint(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "laftekryss";
    /// <inheritdoc/>
    protected override string FullName => "Log Wall Joint";
    /// <inheritdoc/>
    protected override string Description => "Where an inner log wall meets an outer one, its offset logs run through the outer wall and stick out on the outside, as in krysslaft.";
    /// <inheritdoc/>
    protected override int Logs => 2;
}

/// <summary>Half a log gable for the 26° roof.</summary>
public sealed class LogGable26 : LogPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public LogGable26(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "laftgavl_26";
    /// <inheritdoc/>
    protected override string FullName => "Log Gable 26°";
    /// <inheritdoc/>
    protected override string Description => "Offset logs cut to the 26° roof, 2 m wide and 1 m high, with barge boards over their ends. Set one on each half of the end wall; turn it so it rises towards the ridge.";
    /// <inheritdoc/>
    protected override int Logs => 2;
}

/// <summary>Half a log gable for the 45° roof.</summary>
public sealed class LogGable45 : LogPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public LogGable45(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "laftgavl_45";
    /// <inheritdoc/>
    protected override string FullName => "Log Gable 45°";
    /// <inheritdoc/>
    protected override string Description => "Offset logs cut to the 45° roof, 2 m wide and 2 m high, with barge boards over their ends. Set one on each half of the end wall; turn it so it rises towards the ridge.";
    /// <inheritdoc/>
    protected override int Logs => 3;
}
