using Jotunn.Managers;

namespace BrudvikWhiteHilt.Pieces.Banners;

/// <summary>
/// White banner with the White Hilt logo.
/// </summary>
public class WhiteHiltBannerWhite : WhiteHiltBannerBase
{
    /// <summary>
    /// Registers the banner's text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public WhiteHiltBannerWhite(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string PrefabName => "piece_whitehilt_banner_white";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Banner (white)";

    /// <inheritdoc/>
    protected override string Description => "A white banner with the White Hilt.";

    /// <inheritdoc/>
    protected override bool Black => false;

    /// <inheritdoc/>
    protected override bool Large => false;
}

/// <summary>
/// Black banner with the White Hilt logo.
/// </summary>
public class WhiteHiltBannerBlack : WhiteHiltBannerBase
{
    /// <summary>
    /// Registers the banner's text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public WhiteHiltBannerBlack(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string PrefabName => "piece_whitehilt_banner_black";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Banner (black)";

    /// <inheritdoc/>
    protected override string Description => "A black banner with the White Hilt.";

    /// <inheritdoc/>
    protected override bool Black => true;

    /// <inheritdoc/>
    protected override bool Large => false;
}

/// <summary>
/// Large white banner with the White Hilt logo.
/// </summary>
public class WhiteHiltBannerWhiteLarge : WhiteHiltBannerBase
{
    /// <summary>
    /// Registers the banner's text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public WhiteHiltBannerWhiteLarge(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string PrefabName => "piece_whitehilt_banner_white_large";

    /// <inheritdoc/>
    protected override string FullName => "Large White Hilt Banner (white)";

    /// <inheritdoc/>
    protected override string Description => "A white banner with the White Hilt, half again as big.";

    /// <inheritdoc/>
    protected override bool Black => false;

    /// <inheritdoc/>
    protected override bool Large => true;
}

/// <summary>
/// Large black banner with the White Hilt logo.
/// </summary>
public class WhiteHiltBannerBlackLarge : WhiteHiltBannerBase
{
    /// <summary>
    /// Registers the banner's text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public WhiteHiltBannerBlackLarge(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string PrefabName => "piece_whitehilt_banner_black_large";

    /// <inheritdoc/>
    protected override string FullName => "Large White Hilt Banner (black)";

    /// <inheritdoc/>
    protected override string Description => "A black banner with the White Hilt, half again as big.";

    /// <inheritdoc/>
    protected override bool Black => true;

    /// <inheritdoc/>
    protected override bool Large => true;
}
