namespace BrudvikWhiteHilt.Pieces;

/// <summary>
/// Interface for custom pieces in the White Hilt mod.
/// </summary>
public interface IWhiteHiltCustomPiece
{
    /// <summary>
    /// Is this custom piece enabled?
    /// </summary>
    bool Enabled { get; }

    /// <summary>
    /// Adds the custom piece to the game.
    /// </summary>
    void Add();
}
