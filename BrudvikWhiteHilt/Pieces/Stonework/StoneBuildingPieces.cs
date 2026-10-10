using BrudvikWhiteHilt.Pieces.Defenses;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Stonework;

// Stone to build with: arches, windows, a stair, cornices, columns, a balustrade and gables, in plain stone, black
// marble and grausten. The shapes are in the layout (AssetSource/Preview/build_stone_building.py); each shape has a
// base class here and a class per stone, which only names the stone. Name, cost, health and tier follow from the stone
// as for the stone defences (see StoneDefense).

/// <summary>
/// A stone building piece: built at the Stonecutter, stone in the building rules, and hard to break like the stone
/// defences.
/// </summary>
public abstract class StoneBuildingPieceBase : DefensePieceBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    protected StoneBuildingPieceBase(PieceManager instance) : base(instance)
    {
    }

    /// <summary>The stone the piece is built of; the black marble and grausten classes set it.</summary>
    protected virtual StoneVariant Variant => StoneVariant.Stone;

    /// <summary>The layout name of the plain stone piece.</summary>
    protected abstract string ShapeName { get; }

    /// <summary>The English name of the plain stone piece.</summary>
    protected abstract string StoneName { get; }

    /// <summary>Stone the plain stone piece costs.</summary>
    protected abstract int Stone { get; }

    /// <summary>Health of the plain stone piece.</summary>
    protected abstract float StoneHealth { get; }

    /// <inheritdoc/>
    protected override string LayoutName => ShapeName + StoneDefense.Suffix(Variant);

    /// <inheritdoc/>
    protected override string FullName => StoneDefense.Name(Variant, StoneName);

    /// <inheritdoc/>
    protected override string BuildStation => StoneDefense.Station;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => StoneDefense.Cost(Variant, new RequirementConfig[]
    {
        new() { Item = "Stone", Amount = Stone, Recover = true }
    });

    /// <inheritdoc/>
    protected override float Health => StoneHealth * StoneDefense.Strength(Variant);

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => StoneDefense.Tier(Variant, ProgressionTier.BlackForest);

    /// <inheritdoc/>
    protected override void CustomizePrefab(GameObject prefab, DefensePieceData data, IDictionary<string, Transform> groups)
    {
        StoneDefense.Harden(prefab, Variant);
    }
}

/// <summary>Stone Arched Doorway.</summary>
public class StoneArchedDoorway : StoneBuildingPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public StoneArchedDoorway(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string ShapeName => "buegang";
    /// <inheritdoc/>
    protected override string StoneName => "Stone Arched Doorway";
    /// <inheritdoc/>
    protected override string Description => "A wall 2 m wide and 3 m high with a round-arched doorway 1.4 m wide, 2.7 m to the crown, its arch laid in wedge stones with a keystone.";
    /// <inheritdoc/>
    protected override int Stone => 24;
    /// <inheritdoc/>
    protected override float StoneHealth => 2500f;
}

/// <summary>The StoneArchedDoorway in black marble.</summary>
public sealed class StoneArchedDoorwayMarble : StoneArchedDoorway
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public StoneArchedDoorwayMarble(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override StoneVariant Variant => StoneVariant.BlackMarble;
}

/// <summary>The StoneArchedDoorway in grausten.</summary>
public sealed class StoneArchedDoorwayGrausten : StoneArchedDoorway
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public StoneArchedDoorwayGrausten(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override StoneVariant Variant => StoneVariant.Grausten;
}

/// <summary>Stone Arched Window.</summary>
public class StoneArchedWindow : StoneBuildingPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public StoneArchedWindow(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string ShapeName => "buevindu";
    /// <inheritdoc/>
    protected override string StoneName => "Stone Arched Window";
    /// <inheritdoc/>
    protected override string Description => "A wall 2 m wide and 2 m high with a round-headed window 0.8 m wide at eye height and a sill stone.";
    /// <inheritdoc/>
    protected override int Stone => 16;
    /// <inheritdoc/>
    protected override float StoneHealth => 2000f;
}

/// <summary>The StoneArchedWindow in black marble.</summary>
public sealed class StoneArchedWindowMarble : StoneArchedWindow
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public StoneArchedWindowMarble(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override StoneVariant Variant => StoneVariant.BlackMarble;
}

/// <summary>The StoneArchedWindow in grausten.</summary>
public sealed class StoneArchedWindowGrausten : StoneArchedWindow
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public StoneArchedWindowGrausten(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override StoneVariant Variant => StoneVariant.Grausten;
}

/// <summary>Stone Great Arch.</summary>
public class StoneGreatArch : StoneBuildingPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public StoneGreatArch(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string ShapeName => "storbue";
    /// <inheritdoc/>
    protected override string StoneName => "Stone Great Arch";
    /// <inheritdoc/>
    protected override string Description => "A great arch 4 m wide and 4 m high over an opening 3 m wide, for a hall, a gateway or a bridge.";
    /// <inheritdoc/>
    protected override int Stone => 40;
    /// <inheritdoc/>
    protected override float StoneHealth => 4000f;
}

/// <summary>The StoneGreatArch in black marble.</summary>
public sealed class StoneGreatArchMarble : StoneGreatArch
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public StoneGreatArchMarble(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override StoneVariant Variant => StoneVariant.BlackMarble;
}

/// <summary>The StoneGreatArch in grausten.</summary>
public sealed class StoneGreatArchGrausten : StoneGreatArch
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public StoneGreatArchGrausten(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override StoneVariant Variant => StoneVariant.Grausten;
}

/// <summary>Wide Stone Stair.</summary>
public class StoneStoneStair : StoneBuildingPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public StoneStoneStair(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string ShapeName => "steintrinn";
    /// <inheritdoc/>
    protected override string StoneName => "Wide Stone Stair";
    /// <inheritdoc/>
    protected override string Description => "A stair of solid stone 2 m wide rising 2 m over 4 m, as steep as the vanilla stair.";
    /// <inheritdoc/>
    protected override int Stone => 24;
    /// <inheritdoc/>
    protected override float StoneHealth => 3000f;
}

/// <summary>The StoneStoneStair in black marble.</summary>
public sealed class StoneStoneStairMarble : StoneStoneStair
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public StoneStoneStairMarble(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override StoneVariant Variant => StoneVariant.BlackMarble;
}

/// <summary>The StoneStoneStair in grausten.</summary>
public sealed class StoneStoneStairGrausten : StoneStoneStair
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public StoneStoneStairGrausten(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override StoneVariant Variant => StoneVariant.Grausten;
}

/// <summary>Stone Cornice.</summary>
public class StoneCornice : StoneBuildingPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public StoneCornice(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string ShapeName => "gesims";
    /// <inheritdoc/>
    protected override string StoneName => "Stone Cornice";
    /// <inheritdoc/>
    protected override string Description => "Three courses stepping out over the face of a wall, 2 m long and 0.4 m high, to finish its top.";
    /// <inheritdoc/>
    protected override int Stone => 6;
    /// <inheritdoc/>
    protected override float StoneHealth => 800f;
}

/// <summary>The StoneCornice in black marble.</summary>
public sealed class StoneCorniceMarble : StoneCornice
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public StoneCorniceMarble(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override StoneVariant Variant => StoneVariant.BlackMarble;
}

/// <summary>The StoneCornice in grausten.</summary>
public sealed class StoneCorniceGrausten : StoneCornice
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public StoneCorniceGrausten(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override StoneVariant Variant => StoneVariant.Grausten;
}

/// <summary>Stone Cornice Corner.</summary>
public class StoneCorniceCorner : StoneBuildingPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public StoneCorniceCorner(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string ShapeName => "gesims_hjorne";
    /// <inheritdoc/>
    protected override string StoneName => "Stone Cornice Corner";
    /// <inheritdoc/>
    protected override string Description => "The cornice turned round an outer corner.";
    /// <inheritdoc/>
    protected override int Stone => 6;
    /// <inheritdoc/>
    protected override float StoneHealth => 800f;
}

/// <summary>The StoneCorniceCorner in black marble.</summary>
public sealed class StoneCorniceCornerMarble : StoneCorniceCorner
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public StoneCorniceCornerMarble(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override StoneVariant Variant => StoneVariant.BlackMarble;
}

/// <summary>The StoneCorniceCorner in grausten.</summary>
public sealed class StoneCorniceCornerGrausten : StoneCorniceCorner
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public StoneCorniceCornerGrausten(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override StoneVariant Variant => StoneVariant.Grausten;
}

/// <summary>Stone Column.</summary>
public class StoneColumn : StoneBuildingPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public StoneColumn(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string ShapeName => "steinsoyle";
    /// <inheritdoc/>
    protected override string StoneName => "Stone Column";
    /// <inheritdoc/>
    protected override string Description => "A column 3 m high on a square base, with a capital stepping out under a square abacus.";
    /// <inheritdoc/>
    protected override int Stone => 10;
    /// <inheritdoc/>
    protected override float StoneHealth => 1500f;
}

/// <summary>The StoneColumn in black marble.</summary>
public sealed class StoneColumnMarble : StoneColumn
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public StoneColumnMarble(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override StoneVariant Variant => StoneVariant.BlackMarble;
}

/// <summary>The StoneColumn in grausten.</summary>
public sealed class StoneColumnGrausten : StoneColumn
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public StoneColumnGrausten(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override StoneVariant Variant => StoneVariant.Grausten;
}

/// <summary>Stone Balustrade.</summary>
public class StoneBalustrade : StoneBuildingPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public StoneBalustrade(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string ShapeName => "brystning";
    /// <inheritdoc/>
    protected override string StoneName => "Stone Balustrade";
    /// <inheritdoc/>
    protected override string Description => "A balustrade 2 m long and 1 m high, balusters between a plinth and a coping, for a terrace, a gallery or a bridge.";
    /// <inheritdoc/>
    protected override int Stone => 8;
    /// <inheritdoc/>
    protected override float StoneHealth => 1000f;
}

/// <summary>The StoneBalustrade in black marble.</summary>
public sealed class StoneBalustradeMarble : StoneBalustrade
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public StoneBalustradeMarble(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override StoneVariant Variant => StoneVariant.BlackMarble;
}

/// <summary>The StoneBalustrade in grausten.</summary>
public sealed class StoneBalustradeGrausten : StoneBalustrade
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public StoneBalustradeGrausten(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override StoneVariant Variant => StoneVariant.Grausten;
}

/// <summary>Stone Gable 26°.</summary>
public class StoneGable26 : StoneBuildingPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public StoneGable26(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string ShapeName => "steingavl_26";
    /// <inheritdoc/>
    protected override string StoneName => "Stone Gable 26°";
    /// <inheritdoc/>
    protected override string Description => "Courses stepping up to the 26° roof under a coping, 2 m wide and 1 m high. Set one on each half of the end wall.";
    /// <inheritdoc/>
    protected override int Stone => 8;
    /// <inheritdoc/>
    protected override float StoneHealth => 1500f;
}

/// <summary>The StoneGable26 in black marble.</summary>
public sealed class StoneGable26Marble : StoneGable26
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public StoneGable26Marble(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override StoneVariant Variant => StoneVariant.BlackMarble;
}

/// <summary>The StoneGable26 in grausten.</summary>
public sealed class StoneGable26Grausten : StoneGable26
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public StoneGable26Grausten(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override StoneVariant Variant => StoneVariant.Grausten;
}

/// <summary>Stone Gable 45°.</summary>
public class StoneGable45 : StoneBuildingPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public StoneGable45(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string ShapeName => "steingavl_45";
    /// <inheritdoc/>
    protected override string StoneName => "Stone Gable 45°";
    /// <inheritdoc/>
    protected override string Description => "Courses stepping up to the 45° roof under a coping, 2 m wide and 2 m high. Set one on each half of the end wall.";
    /// <inheritdoc/>
    protected override int Stone => 14;
    /// <inheritdoc/>
    protected override float StoneHealth => 2000f;
}

/// <summary>The StoneGable45 in black marble.</summary>
public sealed class StoneGable45Marble : StoneGable45
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public StoneGable45Marble(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override StoneVariant Variant => StoneVariant.BlackMarble;
}

/// <summary>The StoneGable45 in grausten.</summary>
public sealed class StoneGable45Grausten : StoneGable45
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public StoneGable45Grausten(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override StoneVariant Variant => StoneVariant.Grausten;
}
