using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Items;

/// <summary>
/// Interface for custom items.
/// </summary>
public interface IWhiteHiltCustomItem : IWhiteHiltProgressionEntry
{
    /// <summary>
    /// Is this custom item enabled?
    /// </summary>
    bool Enabled { get; }

    /// <summary>
    /// Adds the custom item to the game.
    /// </summary>
    void Add();
}
