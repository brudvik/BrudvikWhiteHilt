using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Pieces.Stonework;

/// <summary>
/// A ship setting: raised stones in the outline of a ship, 12 m long, the tallest at the stems, as the Norse set them round their graves.
/// </summary>
public class ShipSetting : StoneworkPieceBase
{
    /// <summary>
    /// Constructor for the ShipSetting class. Registers the English text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public ShipSetting(PieceManager instance) : base(instance)
    {
    }

    /// <inheritdoc/>
    protected override string LayoutName => "skipssetning";

    /// <inheritdoc/>
    protected override string FullName => "Ship Setting";

    /// <inheritdoc/>
    protected override string Description => "Twenty-two raised stones in the outline of a ship, 12 m long, with the tallest at the stems. Set round a grave, or where a ship of stone should stand.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Stone", Amount = 60, Recover = true }
    };

    /// <inheritdoc/>
    protected override float Health => 3000f;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Start;

    /// <inheritdoc/>
    protected override string Category => PieceCategories.Misc;
}
