namespace BrudvikWhiteHilt.Storms;

/// <summary>
/// Why a Storm Bar cannot bar the house now.
/// </summary>
public enum StormBarRefusal
{
    /// <summary>It can.</summary>
    None,

    /// <summary>No storm is coming or raging where it stands.</summary>
    NoStorm,

    /// <summary>The comfort where it stands is too low.</summary>
    Comfort
}

/// <summary>
/// The rules of the Storm Bar, apart from the game so they can be tested: when it may bar the house, and whether it
/// still holds.
/// </summary>
public static class StormBarRules
{
    /// <summary>
    /// Whether the bar may be put across now.
    /// </summary>
    /// <param name="stormHere">Whether a storm is coming or raging where the bar stands.</param>
    /// <param name="comfort">The comfort where it stands.</param>
    /// <param name="needed">The comfort it needs.</param>
    /// <returns>Why not, or <see cref="StormBarRefusal.None"/>.</returns>
    public static StormBarRefusal Refusal(bool stormHere, int comfort, int needed)
    {
        if (!stormHere)
        {
            return StormBarRefusal.NoStorm;
        }

        return comfort < needed ? StormBarRefusal.Comfort : StormBarRefusal.None;
    }

    /// <summary>
    /// Whether a bar put across in one storm still holds: only while that storm is coming or raging, so it lifts by
    /// itself once the storm has passed, whoever is near.
    /// </summary>
    /// <param name="barredIn">The number of the storm it was put across in; 0 when it is lifted.</param>
    /// <param name="stormNumber">The number of the storm now.</param>
    /// <param name="stormVisible">Whether a storm is coming or raging now.</param>
    /// <returns>True while it holds.</returns>
    public static bool Holds(int barredIn, int stormNumber, bool stormVisible)
    {
        return barredIn != 0 && barredIn == stormNumber && stormVisible;
    }
}
