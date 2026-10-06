using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Pieces.Stonework;

/// <summary>
/// A dry stone wall of field stones without mortar, as round Norwegian farms and fields. The walls snap end to end.
/// </summary>
public abstract class DryStoneWallBase : StoneworkPieceBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    protected DryStoneWallBase(PieceManager instance) : base(instance)
    {
    }

    /// <summary>Stone needed.</summary>
    protected abstract int Stone { get; }

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Stone", Amount = Stone, Recover = true }
    };

    /// <inheritdoc/>
    protected override float Health => 1500f;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Start;

    /// <inheritdoc/>
    protected override string Category => PieceCategories.Building;
}

/// <summary>A 2 m dry stone wall, 0.9 m high.</summary>
public sealed class DryStoneWall : DryStoneWallBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public DryStoneWall(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "torrmur";
    /// <inheritdoc/>
    protected override string FullName => "Dry Stone Wall";
    /// <inheritdoc/>
    protected override string Description => "Field stones stacked without mortar, 2 m long and 0.9 m high. The walls snap end to end round a yard or a field.";
    /// <inheritdoc/>
    protected override int Stone => 12;
}

/// <summary>A 1 m dry stone wall, 0.9 m high.</summary>
public sealed class DryStoneWallShort : DryStoneWallBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public DryStoneWallShort(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "torrmur_1m";
    /// <inheritdoc/>
    protected override string FullName => "Dry Stone Wall 1 m";
    /// <inheritdoc/>
    protected override string Description => "Field stones stacked without mortar, 1 m long and 0.9 m high, to close a gap.";
    /// <inheritdoc/>
    protected override int Stone => 6;
}

/// <summary>A corner of dry stone wall, 1 m each way.</summary>
public sealed class DryStoneWallCorner : DryStoneWallBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public DryStoneWallCorner(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "torrmur_hjorne";
    /// <inheritdoc/>
    protected override string FullName => "Dry Stone Wall Corner";
    /// <inheritdoc/>
    protected override string Description => "A corner of dry stone wall, 1 m each way and 0.9 m high, bound together where the walls meet.";
    /// <inheritdoc/>
    protected override int Stone => 10;
}

/// <summary>A low 2 m field wall, 0.6 m high.</summary>
public sealed class FieldWall : DryStoneWallBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public FieldWall(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "steingard";
    /// <inheritdoc/>
    protected override string FullName => "Field Wall";
    /// <inheritdoc/>
    protected override string Description => "A low wall of stones cleared from the field, 2 m long and 0.6 m high. Keeps the animals in and marks the land.";
    /// <inheritdoc/>
    protected override int Stone => 8;
}
