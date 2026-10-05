using Jotunn.Managers;

namespace BrudvikWhiteHilt.Pieces.Defenses;

// The stone defences in black marble (Mistlands) and grausten (Ashlands): the same pieces, built of a later stone.
// Each only names its stone; the layout, cost, health, tier and hardening follow from it (see StoneDefense).

/// <summary>The StoneRampart in black marble.</summary>
public class StoneRampartMarble : StoneRampart
{
    /// <summary>
    /// Constructor for the StoneRampartMarble class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public StoneRampartMarble(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override StoneVariant Variant => StoneVariant.BlackMarble;
}

/// <summary>The StoneRampart in grausten.</summary>
public class StoneRampartGrausten : StoneRampart
{
    /// <summary>
    /// Constructor for the StoneRampartGrausten class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public StoneRampartGrausten(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override StoneVariant Variant => StoneVariant.Grausten;
}

/// <summary>The StoneRampartPlain in black marble.</summary>
public class StoneRampartPlainMarble : StoneRampartPlain
{
    /// <summary>
    /// Constructor for the StoneRampartPlainMarble class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public StoneRampartPlainMarble(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override StoneVariant Variant => StoneVariant.BlackMarble;
}

/// <summary>The StoneRampartPlain in grausten.</summary>
public class StoneRampartPlainGrausten : StoneRampartPlain
{
    /// <summary>
    /// Constructor for the StoneRampartPlainGrausten class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public StoneRampartPlainGrausten(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override StoneVariant Variant => StoneVariant.Grausten;
}

/// <summary>The StoneCorner in black marble.</summary>
public class StoneCornerMarble : StoneCorner
{
    /// <summary>
    /// Constructor for the StoneCornerMarble class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public StoneCornerMarble(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override StoneVariant Variant => StoneVariant.BlackMarble;
}

/// <summary>The StoneCorner in grausten.</summary>
public class StoneCornerGrausten : StoneCorner
{
    /// <summary>
    /// Constructor for the StoneCornerGrausten class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public StoneCornerGrausten(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override StoneVariant Variant => StoneVariant.Grausten;
}

/// <summary>The StoneCorner45 in black marble.</summary>
public class StoneCorner45Marble : StoneCorner45
{
    /// <summary>
    /// Constructor for the StoneCorner45Marble class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public StoneCorner45Marble(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override StoneVariant Variant => StoneVariant.BlackMarble;
}

/// <summary>The StoneCorner45 in grausten.</summary>
public class StoneCorner45Grausten : StoneCorner45
{
    /// <summary>
    /// Constructor for the StoneCorner45Grausten class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public StoneCorner45Grausten(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override StoneVariant Variant => StoneVariant.Grausten;
}

/// <summary>The StoneStairs in black marble.</summary>
public class StoneStairsMarble : StoneStairs
{
    /// <summary>
    /// Constructor for the StoneStairsMarble class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public StoneStairsMarble(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override StoneVariant Variant => StoneVariant.BlackMarble;
}

/// <summary>The StoneStairs in grausten.</summary>
public class StoneStairsGrausten : StoneStairs
{
    /// <summary>
    /// Constructor for the StoneStairsGrausten class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public StoneStairsGrausten(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override StoneVariant Variant => StoneVariant.Grausten;
}

/// <summary>The StoneGatehouse in black marble.</summary>
public class StoneGatehouseMarble : StoneGatehouse
{
    /// <summary>
    /// Constructor for the StoneGatehouseMarble class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public StoneGatehouseMarble(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override StoneVariant Variant => StoneVariant.BlackMarble;
}

/// <summary>The StoneGatehouse in grausten.</summary>
public class StoneGatehouseGrausten : StoneGatehouse
{
    /// <summary>
    /// Constructor for the StoneGatehouseGrausten class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public StoneGatehouseGrausten(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override StoneVariant Variant => StoneVariant.Grausten;
}

/// <summary>The StoneDrawbridge in black marble.</summary>
public class StoneDrawbridgeMarble : StoneDrawbridge
{
    /// <summary>
    /// Constructor for the StoneDrawbridgeMarble class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public StoneDrawbridgeMarble(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override StoneVariant Variant => StoneVariant.BlackMarble;
}

/// <summary>The StoneDrawbridge in grausten.</summary>
public class StoneDrawbridgeGrausten : StoneDrawbridge
{
    /// <summary>
    /// Constructor for the StoneDrawbridgeGrausten class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public StoneDrawbridgeGrausten(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override StoneVariant Variant => StoneVariant.Grausten;
}

/// <summary>The DragonsTeeth in black marble.</summary>
public class DragonsTeethMarble : DragonsTeeth
{
    /// <summary>
    /// Constructor for the DragonsTeethMarble class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public DragonsTeethMarble(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override StoneVariant Variant => StoneVariant.BlackMarble;
}

/// <summary>The DragonsTeeth in grausten.</summary>
public class DragonsTeethGrausten : DragonsTeeth
{
    /// <summary>
    /// Constructor for the DragonsTeethGrausten class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public DragonsTeethGrausten(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override StoneVariant Variant => StoneVariant.Grausten;
}

/// <summary>The StoneTowerSmall in black marble.</summary>
public class StoneTowerSmallMarble : StoneTowerSmall
{
    /// <summary>
    /// Constructor for the StoneTowerSmallMarble class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public StoneTowerSmallMarble(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override StoneVariant Variant => StoneVariant.BlackMarble;
}

/// <summary>The StoneTowerSmall in grausten.</summary>
public class StoneTowerSmallGrausten : StoneTowerSmall
{
    /// <summary>
    /// Constructor for the StoneTowerSmallGrausten class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public StoneTowerSmallGrausten(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override StoneVariant Variant => StoneVariant.Grausten;
}

/// <summary>The StoneTowerMedium in black marble.</summary>
public class StoneTowerMediumMarble : StoneTowerMedium
{
    /// <summary>
    /// Constructor for the StoneTowerMediumMarble class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public StoneTowerMediumMarble(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override StoneVariant Variant => StoneVariant.BlackMarble;
}

/// <summary>The StoneTowerMedium in grausten.</summary>
public class StoneTowerMediumGrausten : StoneTowerMedium
{
    /// <summary>
    /// Constructor for the StoneTowerMediumGrausten class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public StoneTowerMediumGrausten(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override StoneVariant Variant => StoneVariant.Grausten;
}

/// <summary>The StoneTowerLarge in black marble.</summary>
public class StoneTowerLargeMarble : StoneTowerLarge
{
    /// <summary>
    /// Constructor for the StoneTowerLargeMarble class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public StoneTowerLargeMarble(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override StoneVariant Variant => StoneVariant.BlackMarble;
}

/// <summary>The StoneTowerLarge in grausten.</summary>
public class StoneTowerLargeGrausten : StoneTowerLarge
{
    /// <summary>
    /// Constructor for the StoneTowerLargeGrausten class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public StoneTowerLargeGrausten(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override StoneVariant Variant => StoneVariant.Grausten;
}
