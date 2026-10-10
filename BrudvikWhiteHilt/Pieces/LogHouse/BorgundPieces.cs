using BrudvikWhiteHilt.Pieces.Defenses;
using BrudvikWhiteHilt.Pieces.Defenses.Siege;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.LogHouse;

/// <summary>
/// A piece after Borgund stave church: carved work, roof ornaments and the parts of its timber frame. The shapes are in
/// the layout (AssetSource/Preview/build_borgund.py), which says how each is made.
/// </summary>
public abstract class BorgundPieceBase : DefensePieceBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    protected BorgundPieceBase(PieceManager instance) : base(instance)
    {
    }

    /// <summary>Wood needed.</summary>
    protected abstract int Wood { get; }

    /// <summary>Fine wood needed for carved work.</summary>
    protected virtual int FineWood => 0;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => FineWood > 0
        ? new RequirementConfig[]
        {
            new() { Item = "Wood", Amount = Wood, Recover = true },
            new() { Item = "FineWood", Amount = FineWood, Recover = true }
        }
        : new RequirementConfig[] { new() { Item = "Wood", Amount = Wood, Recover = true } };

    /// <inheritdoc/>
    protected override float Health => 60f * (Wood + FineWood);

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => FineWood > 0 ? ProgressionTier.BlackForest : ProgressionTier.Start;
}

/// <summary>A stave wall clad in scale shingles.</summary>
public sealed class ShingledStaveWall : BorgundPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public ShingledStaveWall(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "sponvegg";
    /// <inheritdoc/>
    protected override string FullName => "Shingled Stave Wall";
    /// <inheritdoc/>
    protected override string Description => "A stave wall 2 m long and 2 m high clad on the outside in pointed scale shingles, as Borgund's walls and gables are.";
    /// <inheritdoc/>
    protected override int Wood => 10;
}

/// <summary>A tall stave wall clad in scale shingles.</summary>
public sealed class ShingledStaveWallTall : BorgundPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public ShingledStaveWallTall(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "sponvegg_hoy";
    /// <inheritdoc/>
    protected override string FullName => "Tall Shingled Stave Wall";
    /// <inheritdoc/>
    protected override string Description => "A stave wall 2 m long and 4 m high clad in scale shingles.";
    /// <inheritdoc/>
    protected override int Wood => 20;
}

/// <summary>A dragon on the end of a ridge.</summary>
public sealed class RidgeDragon : BorgundPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public RidgeDragon(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "monedrage";
    /// <inheritdoc/>
    protected override string FullName => "Ridge Dragon";
    /// <inheritdoc/>
    protected override string Description => "A dragon head on a tall curved neck with a crest down its back, for the end of a ridge, as on the gables of Borgund stave church. Set it on the ridge at the gable, looking out.";
    /// <inheritdoc/>
    protected override int Wood => 4;
    /// <inheritdoc/>
    protected override int FineWood => 6;
}

/// <summary>A cross for the top of a gable.</summary>
public sealed class GableCross : BorgundPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public GableCross(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "gavlkors";
    /// <inheritdoc/>
    protected override string FullName => "Gable Cross";
    /// <inheritdoc/>
    protected override string Description => "A wooden cross with round ends on a short post, for the top of a gable.";
    /// <inheritdoc/>
    protected override int Wood => 3;
}

/// <summary>A carved ridge crest.</summary>
public sealed class RidgeCrest : BorgundPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public RidgeCrest(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "monekam";
    /// <inheritdoc/>
    protected override string FullName => "Ridge Crest";
    /// <inheritdoc/>
    protected override string Description => "2 m of openwork crest for the top of a ridge, crossed laths between two rails with knobs along the top. Borgund is one of the few stave churches that kept its ridge crests.";
    /// <inheritdoc/>
    protected override int Wood => 2;
    /// <inheritdoc/>
    protected override int FineWood => 2;
}

/// <summary>A tiered ridge turret with a spire.</summary>
public sealed class RidgeTurret : BorgundPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public RidgeTurret(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "takrytter";
    /// <inheritdoc/>
    protected override string FullName => "Ridge Turret";
    /// <inheritdoc/>
    protected override string Description => "The turret on the ridge of a stave church: a boarded shaft, three tiers of shingles, an open belfry with round arches, dragon heads on the lowest tier and a tall spire with an iron cross, 7.5 m high. Set it on the ridge.";
    /// <inheritdoc/>
    protected override int Wood => 24;
    /// <inheritdoc/>
    protected override int FineWood => 8;
}

/// <summary>A St Andrew's cross between two columns.</summary>
public sealed class AndrewCross : BorgundPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public AndrewCross(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "andreaskors";
    /// <inheritdoc/>
    protected override string FullName => "St Andrew's Cross";
    /// <inheritdoc/>
    protected override string Description => "Two crossed boards with a round sun where they meet and round leaves down the arms, between two rails, 2 m wide and 1 m high: the bracing over Borgund's arcade. Set it between two columns, over an arcade arch.";
    /// <inheritdoc/>
    protected override int Wood => 3;
    /// <inheritdoc/>
    protected override int FineWood => 2;
}

/// <summary>A round arch between two columns.</summary>
public sealed class ArcadeArch : BorgundPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public ArcadeArch(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "arkadebue";
    /// <inheritdoc/>
    protected override string FullName => "Arcade Arch";
    /// <inheritdoc/>
    protected override string Description => "A round arch 2 m wide and 1 m high under a plate, between two nave columns, as round the nave of Borgund.";
    /// <inheritdoc/>
    protected override int Wood => 4;
}

/// <summary>A round nave column with a capital and masks.</summary>
public sealed class NaveColumn : BorgundPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public NaveColumn(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "skipsstav";
    /// <inheritdoc/>
    protected override string FullName => "Nave Column";
    /// <inheritdoc/>
    protected override string Description => "A round column 4 m high on a round base, with a cushion capital and a grim carved mask looking out on two sides, like the twelve columns round Borgund's nave. Columns 2 m apart take arcade arches between them.";
    /// <inheritdoc/>
    protected override int Wood => 6;
    /// <inheritdoc/>
    protected override int FineWood => 2;
}

/// <summary>A scissor truss under a 45° roof.</summary>
public sealed class ScissorTruss : BorgundPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public ScissorTruss(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "saksesperre";
    /// <inheritdoc/>
    protected override string FullName => "Scissor Truss";
    /// <inheritdoc/>
    protected override string Description => "Two rafters to the ridge with braces crossing between them, under a 45° roof 4 m wide, as in the roof of a stave church. Set it on the wall tops across the hall.";
    /// <inheritdoc/>
    protected override int Wood => 6;
}

/// <summary>A svalgang section with a porch gable over an opening.</summary>
public sealed class GalleryPorch : GalleryBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public GalleryPorch(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "svalgang_inngang";
    /// <inheritdoc/>
    protected override string FullName => "Svalgang Porch";
    /// <inheritdoc/>
    protected override string Description => "2 m of svalgang with an opening 1.2 m wide in its outer side and a steep little gable over it, with lattice in the gable and a cross on the top: the way in to the svalgang and the door behind it.";
}

/// <summary>A consecration cross painted on a wall.</summary>
public sealed class ConsecrationCross : BorgundPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public ConsecrationCross(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "vigselskors";
    /// <inheritdoc/>
    protected override string FullName => "Consecration Cross";
    /// <inheritdoc/>
    protected override string Description => "A red cross in an ochre ring, painted on a round board for the wall, like the consecration crosses kept on Borgund's south wall.";
    /// <inheritdoc/>
    protected override string Category => PieceCategories.Furniture;
    /// <inheritdoc/>
    protected override int Wood => 1;
    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Wood", Amount = 1, Recover = true },
        new() { Item = "Raspberry", Amount = 2, Recover = false }
    };
}

/// <summary>A free-standing stave bell tower whose bell rings like the alarm bell.</summary>
public sealed class BellTower : BorgundPieceBase
{
    /// <summary>Creates the piece.</summary>
    /// <param name="instance">The piece manager.</param>
    public BellTower(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "stopul";
    /// <inheritdoc/>
    protected override string FullName => "Bell Tower";
    /// <inheritdoc/>
    protected override string Description => "A free-standing stave bell tower (støpul), 3 × 3 m and 9.5 m high, as beside Borgund church: a boarded foot, a shingled middle, an open belfry and a pyramid roof with a cross. Its bell rings like the Alarm Bell: use it at the foot of the tower, and it rings by itself when a raid comes.";
    /// <inheritdoc/>
    protected override int Wood => 40;
    /// <inheritdoc/>
    protected override int FineWood => 10;
    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Wood", Amount = 40, Recover = true },
        new() { Item = "FineWood", Amount = 10, Recover = true },
        new() { Item = "Bronze", Amount = 6, Recover = true }
    };

    /// <inheritdoc/>
    protected override void CustomizePrefab(GameObject prefab, DefensePieceData data, IDictionary<string, Transform> groups)
    {
        prefab.AddComponent<AlarmBell>();
    }
}
