using BrudvikWhiteHilt.Progression;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Structure;

/// <summary>
/// Wood Beam, 4 m.
/// </summary>
public class WoodBeam4 : BeamPieceBase
{
    /// <summary>
    /// Registers the piece's text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public WoodBeam4(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string PrefabName => "piece_whitehilt_wood_beam_4";

    /// <inheritdoc/>
    protected override string FullName => "Wood beam 4m";

    /// <inheritdoc/>
    protected override string Description => "A wooden beam twice as long as the ordinary one.";

    /// <inheritdoc/>
    protected override string CopyFrom => "wood_beam";

    /// <inheritdoc/>
    protected override BeamShape Shape => BeamShape.Stretch;

    /// <inheritdoc/>
    protected override Vector3 Axis => Vector3.right;
}

/// <summary>
/// Wood Pole, 4 m.
/// </summary>
public class WoodPole4 : BeamPieceBase
{
    /// <summary>
    /// Registers the piece's text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public WoodPole4(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string PrefabName => "piece_whitehilt_wood_pole_4";

    /// <inheritdoc/>
    protected override string FullName => "Wood pole 4m";

    /// <inheritdoc/>
    protected override string Description => "A wooden pole twice as long as the 2 m one.";

    /// <inheritdoc/>
    protected override string CopyFrom => "wood_pole2";

    /// <inheritdoc/>
    protected override BeamShape Shape => BeamShape.Stretch;

    /// <inheritdoc/>
    protected override Vector3 Axis => Vector3.up;
}

/// <summary>
/// Wood Iron Beam, 4 m: two iron-bound beams in one.
/// </summary>
public class WoodIronBeam4 : BeamPieceBase
{
    /// <summary>
    /// Registers the piece's text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public WoodIronBeam4(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Swamp;

    /// <inheritdoc/>
    protected override string PrefabName => "piece_whitehilt_woodiron_beam_4";

    /// <inheritdoc/>
    protected override string FullName => "Wood iron beam 4m";

    /// <inheritdoc/>
    protected override string Description => "An iron-bound beam twice as long as the ordinary one.";

    /// <inheritdoc/>
    protected override string CopyFrom => "woodiron_beam";

    /// <inheritdoc/>
    protected override BeamShape Shape => BeamShape.Double;

    /// <inheritdoc/>
    protected override Vector3 Axis => Vector3.right;
}

/// <summary>
/// Wood Iron Pole, 4 m: two iron-bound poles in one.
/// </summary>
public class WoodIronPole4 : BeamPieceBase
{
    /// <summary>
    /// Registers the piece's text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public WoodIronPole4(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Swamp;

    /// <inheritdoc/>
    protected override string PrefabName => "piece_whitehilt_woodiron_pole_4";

    /// <inheritdoc/>
    protected override string FullName => "Wood iron pole 4m";

    /// <inheritdoc/>
    protected override string Description => "An iron-bound pole twice as long as the ordinary one.";

    /// <inheritdoc/>
    protected override string CopyFrom => "woodiron_pole";

    /// <inheritdoc/>
    protected override BeamShape Shape => BeamShape.Double;

    /// <inheritdoc/>
    protected override Vector3 Axis => Vector3.up;
}

/// <summary>
/// Wood Iron Beam, 1 m.
/// </summary>
public class WoodIronBeam1 : BeamPieceBase
{
    /// <summary>
    /// Registers the piece's text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public WoodIronBeam1(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Swamp;

    /// <inheritdoc/>
    protected override string PrefabName => "piece_whitehilt_woodiron_beam_1";

    /// <inheritdoc/>
    protected override string FullName => "Wood iron beam 1m";

    /// <inheritdoc/>
    protected override string Description => "A short iron-bound beam, half the ordinary length.";

    /// <inheritdoc/>
    protected override string CopyFrom => "woodiron_beam";

    /// <inheritdoc/>
    protected override BeamShape Shape => BeamShape.Stretch;

    /// <inheritdoc/>
    protected override Vector3 Axis => Vector3.right;

    /// <inheritdoc/>
    protected override float LengthFactor => 0.5f;

    /// <inheritdoc/>
    protected override float CostFactor => 0.5f;
}

/// <summary>
/// Wood Iron Pole, 1 m.
/// </summary>
public class WoodIronPole1 : BeamPieceBase
{
    /// <summary>
    /// Registers the piece's text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public WoodIronPole1(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Swamp;

    /// <inheritdoc/>
    protected override string PrefabName => "piece_whitehilt_woodiron_pole_1";

    /// <inheritdoc/>
    protected override string FullName => "Wood iron pole 1m";

    /// <inheritdoc/>
    protected override string Description => "A short iron-bound pole, half the ordinary length.";

    /// <inheritdoc/>
    protected override string CopyFrom => "woodiron_pole";

    /// <inheritdoc/>
    protected override BeamShape Shape => BeamShape.Stretch;

    /// <inheritdoc/>
    protected override Vector3 Axis => Vector3.up;

    /// <inheritdoc/>
    protected override float LengthFactor => 0.5f;

    /// <inheritdoc/>
    protected override float CostFactor => 0.5f;
}

/// <summary>
/// Wood Iron Beam at 26°, for 1:2 roofs like the vanilla wooden 26° beam.
/// </summary>
public class WoodIronBeam26 : BeamPieceBase
{
    /// <summary>
    /// Registers the piece's text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public WoodIronBeam26(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Swamp;

    /// <inheritdoc/>
    protected override string PrefabName => "piece_whitehilt_woodiron_beam_26";

    /// <inheritdoc/>
    protected override string FullName => "Wood iron beam 26°";

    /// <inheritdoc/>
    protected override string Description => "An iron-bound beam at the angle of a 26° roof.";

    /// <inheritdoc/>
    protected override string CopyFrom => "woodiron_beam";

    /// <inheritdoc/>
    protected override BeamShape Shape => BeamShape.Tilt;

    /// <inheritdoc/>
    protected override Vector3 Axis => Vector3.right;

    /// <inheritdoc/>
    protected override float LengthFactor => 1.1180f;

    /// <inheritdoc/>
    protected override float TiltDegrees => 26.565f;

    /// <inheritdoc/>
    protected override float CostFactor => 1f;
}

/// <summary>
/// Wood Iron Beam at 45°, for 45° roofs like the vanilla wooden 45° beam.
/// </summary>
public class WoodIronBeam45 : BeamPieceBase
{
    /// <summary>
    /// Registers the piece's text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public WoodIronBeam45(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Swamp;

    /// <inheritdoc/>
    protected override string PrefabName => "piece_whitehilt_woodiron_beam_45";

    /// <inheritdoc/>
    protected override string FullName => "Wood iron beam 45°";

    /// <inheritdoc/>
    protected override string Description => "An iron-bound beam at the angle of a 45° roof.";

    /// <inheritdoc/>
    protected override string CopyFrom => "woodiron_beam";

    /// <inheritdoc/>
    protected override BeamShape Shape => BeamShape.Tilt;

    /// <inheritdoc/>
    protected override Vector3 Axis => Vector3.right;

    /// <inheritdoc/>
    protected override float LengthFactor => 1.4142f;

    /// <inheritdoc/>
    protected override float TiltDegrees => 45f;

    /// <inheritdoc/>
    protected override float CostFactor => 1f;
}
