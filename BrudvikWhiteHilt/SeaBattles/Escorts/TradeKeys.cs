namespace BrudvikWhiteHilt.SeaBattles.Escorts;

/// <summary>
/// Where a trading ship is in its escort. Stored in the ship's data as a whole number, so the numbers never change.
/// </summary>
public enum TradeState
{
    /// <summary>On its way to the destination.</summary>
    Sailing = 0,

    /// <summary>In port: the escort was a success.</summary>
    Arrived = 1,

    /// <summary>The escort failed: the ship sank, the cargo was carried off, or the merchant gave up.</summary>
    Lost = 2
}

/// <summary>
/// The ZDO keys of the trading ships, hashed once. All of them are written by the ship's owner only.
/// </summary>
internal static class TradeKeys
{
    /// <summary>The most waves a trading ship stores; the setting is held to the same limit.</summary>
    public const int MaxWaves = 4;

    private static readonly int[] waveAt =
    {
        "whitehilt_trade_waveat0".GetStableHashCode(),
        "whitehilt_trade_waveat1".GetStableHashCode(),
        "whitehilt_trade_waveat2".GetStableHashCode(),
        "whitehilt_trade_waveat3".GetStableHashCode()
    };

    /// <summary>Where it is going (a <see cref="UnityEngine.Vector3"/>).</summary>
    public static readonly int Destination = "whitehilt_trade_destination".GetStableHashCode();

    /// <summary>Where it began (a <see cref="UnityEngine.Vector3"/>), for the progress along the route.</summary>
    public static readonly int Start = "whitehilt_trade_start".GetStableHashCode();

    /// <summary>The player id of the one who called it (a long).</summary>
    public static readonly int Contractor = "whitehilt_trade_contractor".GetStableHashCode();

    /// <summary>The <see cref="TradeState"/>.</summary>
    public static readonly int State = "whitehilt_trade_state".GetStableHashCode();

    /// <summary>The cargo left, 0 to 1 (a float).</summary>
    public static readonly int Cargo = "whitehilt_trade_cargo".GetStableHashCode();

    /// <summary>How many waves have been sent.</summary>
    public static readonly int WavesSent = "whitehilt_trade_wavessent".GetStableHashCode();

    /// <summary>How many waves the escort has in all.</summary>
    public static readonly int Waves = "whitehilt_trade_waves".GetStableHashCode();

    /// <summary>World ticks when a player was last within range.</summary>
    public static readonly int Seen = "whitehilt_trade_seen".GetStableHashCode();

    /// <summary>World ticks when the escort ended; 0 while it goes on.</summary>
    public static readonly int EndedAt = "whitehilt_trade_endedat".GetStableHashCode();

    /// <summary>
    /// The key of the share of the route at which a wave comes.
    /// </summary>
    /// <param name="index">The wave, from 0 up to <see cref="MaxWaves"/> minus 1.</param>
    /// <returns>The hashed key.</returns>
    public static int WaveAt(int index)
    {
        return waveAt[index];
    }
}
