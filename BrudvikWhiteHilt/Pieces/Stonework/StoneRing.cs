using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Pieces.Stonework;

/// <summary>
/// A ring of low stones, 3.6 m across, for a fire place or a thing site.
/// </summary>
public class StoneRing : StoneworkPieceBase
{
    /// <summary>
    /// Constructor for the StoneRing class. Registers the English text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public StoneRing(PieceManager instance) : base(instance)
    {
    }

    /// <inheritdoc/>
    protected override string LayoutName => "steinring";

    /// <inheritdoc/>
    protected override string FullName => "Stone Ring";

    /// <inheritdoc/>
    protected override string Description => "Ten low stones in a ring 3.6 m across. Light a fire inside it, or hold your thing there.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Stone", Amount = 20, Recover = true }
    };

    /// <inheritdoc/>
    protected override float Health => 1500f;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Start;

    /// <inheritdoc/>
    protected override string Category => PieceCategories.Misc;
}
