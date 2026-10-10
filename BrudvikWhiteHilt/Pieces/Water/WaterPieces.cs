using BrudvikWhiteHilt.Pieces.Defenses;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Water;

// Bridges, jetties and quays. The shapes are in the layout (AssetSource/Preview/build_water.py): every piece's walking
// surface is at y 0 where it meets the land or the next piece, and what is under it reaches 3 m down.

/// <summary>A wooden piece at the water's edge: built near a Workbench, of wood, standing in the water.</summary>
public abstract class TimberWaterPieceBase : DefensePieceBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    protected TimberWaterPieceBase(PieceManager instance) : base(instance)
    {
    }

    /// <summary>Wood needed.</summary>
    protected abstract int Wood { get; }

    /// <summary>Core wood needed, for the piles and stringers.</summary>
    protected abstract int CoreWood { get; }

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Wood", Amount = Wood, Recover = true },
        new() { Item = "RoundLog", Amount = CoreWood, Recover = true }
    };

    /// <inheritdoc/>
    protected override float Health => 60f * (Wood + CoreWood);

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.BlackForest;
}

/// <summary>A stone piece at the water's edge: built at the Stonecutter and hard like the stone defences.</summary>
public abstract class StoneWaterPieceBase : DefensePieceBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    protected StoneWaterPieceBase(PieceManager instance) : base(instance)
    {
    }

    /// <summary>Stone needed.</summary>
    protected abstract int Stone { get; }

    /// <inheritdoc/>
    protected override string BuildStation => StoneDefense.Station;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Stone", Amount = Stone, Recover = true }
    };

    /// <inheritdoc/>
    protected override float Health => 100f * Stone;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.BlackForest;

    /// <inheritdoc/>
    protected override void CustomizePrefab(GameObject prefab, DefensePieceData data, IDictionary<string, Transform> groups)
    {
        StoneDefense.Harden(prefab);
    }
}

/// <summary>A 4 m jetty section.</summary>
public sealed class Jetty : TimberWaterPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public Jetty(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "brygge";
    /// <inheritdoc/>
    protected override string FullName => "Jetty";
    /// <inheritdoc/>
    protected override string Description => "4 m of jetty 2 m wide: a deck of boards on stringers and tarred piles reaching 3 m down into the water. Sections snap end to end out from the shore.";
    /// <inheritdoc/>
    protected override int Wood => 12;
    /// <inheritdoc/>
    protected override int CoreWood => 4;
}

/// <summary>A 2 m jetty section.</summary>
public sealed class JettyShort : TimberWaterPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public JettyShort(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "brygge_2m";
    /// <inheritdoc/>
    protected override string FullName => "Jetty 2 m";
    /// <inheritdoc/>
    protected override string Description => "2 m of jetty, to fill a gap or turn a corner.";
    /// <inheritdoc/>
    protected override int Wood => 6;
    /// <inheritdoc/>
    protected override int CoreWood => 2;
}

/// <summary>The end of a jetty, with bollards and a stair down.</summary>
public sealed class JettyHead : TimberWaterPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public JettyHead(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "bryggehode";
    /// <inheritdoc/>
    protected override string FullName => "Jetty Head";
    /// <inheritdoc/>
    protected override string Description => "The end of a jetty, 2 × 2 m, with a bollard post on each outer corner and a stair down its end to 1.5 m below the deck, for climbing up from a boat.";
    /// <inheritdoc/>
    protected override int Wood => 10;
    /// <inheritdoc/>
    protected override int CoreWood => 3;
}

/// <summary>A log bridge, 4 m.</summary>
public sealed class LogBridge : TimberWaterPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public LogBridge(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "tommerbru";
    /// <inheritdoc/>
    protected override string FullName => "Log Bridge";
    /// <inheritdoc/>
    protected override string Description => "A bridge 4 m long and 2 m wide over a stream: two log stringers resting on the banks, boards across them and a log railing.";
    /// <inheritdoc/>
    protected override int Wood => 10;
    /// <inheritdoc/>
    protected override int CoreWood => 4;
}

/// <summary>A log bridge, 8 m.</summary>
public sealed class LogBridgeLong : TimberWaterPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public LogBridgeLong(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "tommerbru_8m";
    /// <inheritdoc/>
    protected override string FullName => "Log Bridge 8 m";
    /// <inheritdoc/>
    protected override string Description => "A log bridge 8 m long over a river, on whole logs from bank to bank.";
    /// <inheritdoc/>
    protected override int Wood => 18;
    /// <inheritdoc/>
    protected override int CoreWood => 8;
}

/// <summary>A rope bridge, 8 m.</summary>
public sealed class RopeBridge : TimberWaterPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public RopeBridge(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "hengebru";
    /// <inheritdoc/>
    protected override string FullName => "Rope Bridge";
    /// <inheritdoc/>
    protected override string Description => "A rope bridge 8 m long between two pairs of posts: plank treads on ropes that sag half a metre in the middle, and rope handrails.";
    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Wood", Amount = 16, Recover = true },
        new() { Item = "RoundLog", Amount = 4, Recover = true },
        new() { Item = "LeatherScraps", Amount = 8, Recover = false }
    };
    /// <inheritdoc/>
    protected override int Wood => 16;
    /// <inheritdoc/>
    protected override int CoreWood => 4;
}

/// <summary>A stone quay, 4 m.</summary>
public sealed class Quay : StoneWaterPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public Quay(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "kai";
    /// <inheritdoc/>
    protected override string FullName => "Stone Quay";
    /// <inheritdoc/>
    protected override string Description => "4 m of stone quay, 2 m deep and reaching 3 m down into the water: coursed stone under a cope of long stones, a timber fender along the face, an iron ring and a bollard. Its face goes to the water.";
    /// <inheritdoc/>
    protected override int Stone => 40;
}

/// <summary>A stone quay's outer corner.</summary>
public sealed class QuayCorner : StoneWaterPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public QuayCorner(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "kai_hjorne";
    /// <inheritdoc/>
    protected override string FullName => "Stone Quay Corner";
    /// <inheritdoc/>
    protected override string Description => "The outer corner of a stone quay, where it turns to face the water on two sides, with a big stone bollard.";
    /// <inheritdoc/>
    protected override int Stone => 30;
}

/// <summary>Stone steps down a quay's face.</summary>
public sealed class QuaySteps : StoneWaterPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public QuaySteps(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "kaitrapp";
    /// <inheritdoc/>
    protected override string FullName => "Quay Steps";
    /// <inheritdoc/>
    protected override string Description => "Stone steps running down along the face of a quay, 4 m long, from the top to 2 m below it, to a boat or the water.";
    /// <inheritdoc/>
    protected override int Stone => 24;
}

/// <summary>An arched stone bridge.</summary>
public sealed class StoneBridge : StoneWaterPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public StoneBridge(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "steinbru";
    /// <inheritdoc/>
    protected override string FullName => "Stone Bridge";
    /// <inheritdoc/>
    protected override string Description => "A stone bridge 8 m long over an arch 6 m across: its deck humps 0.8 m in the middle between low parapets, and its abutments reach down into the banks.";
    /// <inheritdoc/>
    protected override int Stone => 80;
}
