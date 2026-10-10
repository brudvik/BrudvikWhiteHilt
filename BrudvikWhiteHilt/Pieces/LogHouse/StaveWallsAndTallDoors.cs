using BrudvikWhiteHilt.Pieces.Defenses;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.LogHouse;

/// <summary>
/// A piece of stave wall: tarred boards standing between a sill and a wall plate, as on the stave churches. The shapes
/// are in the layout (AssetSource/Preview/build_stave.py).
/// </summary>
public abstract class StavePieceBase : DefensePieceBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    protected StavePieceBase(PieceManager instance) : base(instance)
    {
    }

    /// <summary>Wood needed.</summary>
    protected abstract int Wood { get; }

    /// <summary>Resin for the tar.</summary>
    protected virtual int Resin => 1;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Wood", Amount = Wood, Recover = true },
        new() { Item = "Resin", Amount = Resin, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Health => 75f * Wood;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Start;
}

/// <summary>A 2 x 2 m stave wall.</summary>
public sealed class StaveWall : StavePieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public StaveWall(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "stavvegg";
    /// <inheritdoc/>
    protected override string FullName => "Stave Wall";
    /// <inheritdoc/>
    protected override string Description => "Tarred boards standing on end between a sill and a wall plate, as on the stave churches, 2 m long and 2 m high.";
    /// <inheritdoc/>
    protected override int Wood => 6;
}

/// <summary>A 1 x 2 m stave wall.</summary>
public sealed class StaveWallShort : StavePieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public StaveWallShort(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "stavvegg_1m";
    /// <inheritdoc/>
    protected override string FullName => "Stave Wall 1 m";
    /// <inheritdoc/>
    protected override string Description => "A stave wall 1 m long and 2 m high, to close a gap.";
    /// <inheritdoc/>
    protected override int Wood => 3;
}

/// <summary>A 4 x 2 m stave wall.</summary>
public sealed class StaveWallLong : StavePieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public StaveWallLong(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "stavvegg_4m";
    /// <inheritdoc/>
    protected override string FullName => "Stave Wall 4 m";
    /// <inheritdoc/>
    protected override string Description => "A stave wall 4 m long and 2 m high on one sill.";
    /// <inheritdoc/>
    protected override int Wood => 12;
    /// <inheritdoc/>
    protected override int Resin => 2;
}

/// <summary>A 2 x 4 m stave wall for a hall.</summary>
public sealed class StaveWallTall : StavePieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public StaveWallTall(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "stavvegg_hoy";
    /// <inheritdoc/>
    protected override string FullName => "Tall Stave Wall";
    /// <inheritdoc/>
    protected override string Description => "A stave wall 2 m long and 4 m high, for a hall or a church: its boards run the whole height, and the svalgang's roof meets it.";
    /// <inheritdoc/>
    protected override int Wood => 12;
    /// <inheritdoc/>
    protected override int Resin => 2;
}

/// <summary>A 2 x 1 m stave wall, over a 3 m door or under a gable.</summary>
public sealed class StaveWallLow : StavePieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public StaveWallLow(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "stavvegg_lav";
    /// <inheritdoc/>
    protected override string FullName => "Low Stave Wall";
    /// <inheritdoc/>
    protected override string Description => "A stave wall 2 m long and 1 m high, over a door 3 m high in a 4 m wall, or under a 26° gable.";
    /// <inheritdoc/>
    protected override int Wood => 3;
}

/// <summary>A round corner stave, 2 m.</summary>
public sealed class CornerStave : StavePieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public CornerStave(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "hjornestav";
    /// <inheritdoc/>
    protected override string FullName => "Corner Stave";
    /// <inheritdoc/>
    protected override string Description => "A round post 2 m high for the corner where two stave walls meet, with a cap.";
    /// <inheritdoc/>
    protected override int Wood => 4;
}

/// <summary>A round corner stave, 4 m.</summary>
public sealed class CornerStaveTall : StavePieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public CornerStaveTall(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "hjornestav_4m";
    /// <inheritdoc/>
    protected override string FullName => "Tall Corner Stave";
    /// <inheritdoc/>
    protected override string Description => "A round corner post 4 m high, for the tall stave walls.";
    /// <inheritdoc/>
    protected override int Wood => 8;
}

/// <summary>Half a stave gable for the 26° roof.</summary>
public sealed class StaveGable26 : StavePieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public StaveGable26(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "stavgavl_26";
    /// <inheritdoc/>
    protected override string FullName => "Stave Gable 26°";
    /// <inheritdoc/>
    protected override string Description => "Boards up to the 26° roof, 2 m wide and 1 m high, with barge boards. Set one on each half of the end wall, rising towards the ridge.";
    /// <inheritdoc/>
    protected override int Wood => 3;
}

/// <summary>Half a stave gable for the 45° roof.</summary>
public sealed class StaveGable45 : StavePieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public StaveGable45(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "stavgavl_45";
    /// <inheritdoc/>
    protected override string FullName => "Stave Gable 45°";
    /// <inheritdoc/>
    protected override string Description => "Boards up to the 45° roof, 2 m wide and 2 m high, with barge boards. Set one on each half of the end wall, rising towards the ridge.";
    /// <inheritdoc/>
    protected override int Wood => 4;
}

/// <summary>A stave wall with a small round window high up.</summary>
public sealed class StaveWallGlugg : StavePieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public StaveWallGlugg(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "stavvegg_glugg";
    /// <inheritdoc/>
    protected override string FullName => "Stave Wall with Round Glugg";
    /// <inheritdoc/>
    protected override string Description => "A stave wall 2 m long with a small round window high up, as on the stave churches.";
    /// <inheritdoc/>
    protected override int Wood => 6;
}

/// <summary>A stave wall with a shuttered window.</summary>
public sealed class StaveWallWindow : LogHouseDoorBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public StaveWallWindow(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "stavvegg_vindu";
    /// <inheritdoc/>
    protected override string FullName => "Stave Wall with Window";
    /// <inheritdoc/>
    protected override string Description => "A stave wall 2 m long with a window 1 m wide at eye height, closed by two shutters that swing out.";
    /// <inheritdoc/>
    protected override (string Group, Vector3 Axis)[] OneWaySwings => new[] { ("leaf_right", Vector3.up), ("leaf_left", Vector3.down) };
    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Wood", Amount = 8, Recover = true },
        new() { Item = "Resin", Amount = 1, Recover = false }
    };
    /// <inheritdoc/>
    protected override float Health => 450f;
    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Start;
}

/// <summary>The svalgang: a covered gallery along a wall.</summary>
public abstract class GalleryBase : DefensePieceBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    protected GalleryBase(PieceManager instance) : base(instance)
    {
    }

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Wood", Amount = 10, Recover = true },
        new() { Item = "FineWood", Amount = 4, Recover = true },
        new() { Item = "Resin", Amount = 2, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Health => 600f;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.BlackForest;
}

/// <summary>A 2 m section of svalgang.</summary>
public sealed class Gallery : GalleryBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public Gallery(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "svalgang";
    /// <inheritdoc/>
    protected override string FullName => "Svalgang";
    /// <inheritdoc/>
    protected override string Description => "2 m of the covered gallery round a stave church: a board floor, a low wall, an arcade of small posts and arches, and a lean-to of dark shingles that meets the wall at 3.15 m. Snap it to the foot of a tall stave wall.";
}

/// <summary>The outer corner of the svalgang.</summary>
public sealed class GalleryCorner : GalleryBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public GalleryCorner(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "svalgang_hjorne";
    /// <inheritdoc/>
    protected override string FullName => "Svalgang Corner";
    /// <inheritdoc/>
    protected override string Description => "The outer corner of the svalgang, where the galleries along two walls meet under a hipped corner of dark shingles.";
}

/// <summary>A board door 3 m high.</summary>
public sealed class TallPlankDoor : LogHouseDoorBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public TallPlankDoor(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "hoy_plankedor";
    /// <inheritdoc/>
    protected override string FullName => "Tall Plank Door";
    /// <inheritdoc/>
    protected override string Description => "A door of tarred boards 1.5 m wide and 3 m high between hewn posts, a metre over your head, like the vanilla gate. It snaps in place of a 2 m wall and the 1 m over it.";
    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Wood", Amount = 8, Recover = true },
        new() { Item = "Resin", Amount = 2, Recover = false }
    };
    /// <inheritdoc/>
    protected override float Health => 500f;
    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Start;
}

/// <summary>Two narrow leaves 3 m high.</summary>
public sealed class DoubleDoor : LogHouseDoorBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public DoubleDoor(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "dobbeldor";
    /// <inheritdoc/>
    protected override string FullName => "Double Door";
    /// <inheritdoc/>
    protected override string Description => "Two narrow board doors 3 m high that open from the middle, 1.6 m wide together, in place of a 2 m wall.";
    /// <inheritdoc/>
    protected override string LeafGroup => "leaf_right";
    /// <inheritdoc/>
    protected override string MirroredLeafGroup => "leaf_left";
    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Wood", Amount = 10, Recover = true },
        new() { Item = "Resin", Amount = 2, Recover = false }
    };
    /// <inheritdoc/>
    protected override float Health => 500f;
    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Start;
}

/// <summary>The hall's great doors, 4 x 3 m.</summary>
public sealed class HallDoors : LogHouseDoorBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public HallDoors(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "haldor";
    /// <inheritdoc/>
    protected override string FullName => "Hall Doors";
    /// <inheritdoc/>
    protected override string Description => "The great doors of a hall: two leaves 1.7 m wide and 3 m high with long iron straps, in place of a 4 m wall.";
    /// <inheritdoc/>
    protected override string LeafGroup => "leaf_right";
    /// <inheritdoc/>
    protected override string MirroredLeafGroup => "leaf_left";
    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Wood", Amount = 20, Recover = true },
        new() { Item = "Iron", Amount = 2, Recover = true }
    };
    /// <inheritdoc/>
    protected override float Health => 1200f;
    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Swamp;
}

/// <summary>A stave church portal with a round-headed door.</summary>
public sealed class StaveChurchPortal : LogHouseDoorBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public StaveChurchPortal(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "stavkirkeportal";
    /// <inheritdoc/>
    protected override string FullName => "Stave Church Portal";
    /// <inheritdoc/>
    protected override string Description => "A portal of broad boards round a narrow door with a round head, 1.2 m wide and 2.4 m high, dragon heads at the top and a bronze ring, as on the old stave churches. 2 m wide and 3 m high.";
    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "FineWood", Amount = 10, Recover = true },
        new() { Item = "Wood", Amount = 4, Recover = true },
        new() { Item = "Bronze", Amount = 2, Recover = true }
    };
    /// <inheritdoc/>
    protected override float Health => 700f;
    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.BlackForest;
}

/// <summary>A plain log wall 1 m high, over a 3 m door.</summary>
public sealed class LogWallLow : LogPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public LogWallLow(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "laftvegg_lav";
    /// <inheritdoc/>
    protected override string FullName => "Low Log Wall";
    /// <inheritdoc/>
    protected override string Description => "A plain log wall 2 m long and 1 m high, to fill over a door 3 m high in a wall of two rows.";
    /// <inheritdoc/>
    protected override int Logs => 2;
}

/// <summary>A log wall with a narrow window and one shutter.</summary>
public abstract class NarrowWindowLogWallBase : ShutteredLogWallBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    protected NarrowWindowLogWallBase(PieceManager instance) : base(instance)
    {
    }

    /// <inheritdoc/>
    protected override (string Group, Vector3 Axis)[] OneWaySwings => new[] { ("leaf_right", Vector3.up) };
}

/// <summary>A plain log wall with a narrow window.</summary>
public sealed class LogWallNarrowWindow : NarrowWindowLogWallBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public LogWallNarrowWindow(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "laftvegg_smal_vindu";
    /// <inheritdoc/>
    protected override string FullName => "Log Wall with Narrow Window";
    /// <inheritdoc/>
    protected override string Description => "A log wall 2 m long with a tall narrow window 0.6 m wide, from 0.5 m up to the top of the wall, closed by one shutter.";
}

/// <summary>An offset log wall with a narrow window.</summary>
public sealed class OffsetLogWallNarrowWindow : NarrowWindowLogWallBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public OffsetLogWallNarrowWindow(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "laftvegg_forskutt_smal_vindu";
    /// <inheritdoc/>
    protected override string FullName => "Offset Log Wall with Narrow Window";
    /// <inheritdoc/>
    protected override string Description => "An offset log wall 2 m long with a narrow window 0.6 m wide at eye height, closed by one shutter.";
}

/// <summary>A plain log wall with a barred window.</summary>
public sealed class LogWallBarredWindow : LogPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public LogWallBarredWindow(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "laftvegg_sprosser";
    /// <inheritdoc/>
    protected override string FullName => "Log Wall with Barred Window";
    /// <inheritdoc/>
    protected override string Description => "A log wall 2 m long with a window 1 m wide at eye height behind iron bars, for a storehouse or a smithy: light and air in, nothing else.";
    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "RoundLog", Amount = 4, Recover = true },
        new() { Item = "Iron", Amount = 1, Recover = true }
    };
    /// <inheritdoc/>
    protected override int Logs => 4;
    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Swamp;
}

/// <summary>An offset log wall with a barred window.</summary>
public sealed class OffsetLogWallBarredWindow : LogPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public OffsetLogWallBarredWindow(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "laftvegg_forskutt_sprosser";
    /// <inheritdoc/>
    protected override string FullName => "Offset Log Wall with Barred Window";
    /// <inheritdoc/>
    protected override string Description => "An offset log wall 2 m long with a window 1 m wide behind iron bars.";
    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "RoundLog", Amount = 4, Recover = true },
        new() { Item = "Iron", Amount = 1, Recover = true }
    };
    /// <inheritdoc/>
    protected override int Logs => 4;
    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Swamp;
}
