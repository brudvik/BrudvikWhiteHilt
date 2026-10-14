using BrudvikWhiteHilt.Navigation;

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
    Lost = 2,

    /// <summary>Lying at a trading post until the contractor goes aboard their own ship.</summary>
    Waiting = 3
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

    /// <summary>The coins the escort pays for a full cargo, written when the ship is raised.</summary>
    public static readonly int Pay = "whitehilt_trade_pay".GetStableHashCode();

    /// <summary>World ticks when the ship was raised at a trading post, for the wait.</summary>
    public static readonly int RaisedAt = "whitehilt_trade_raisedat".GetStableHashCode();

    /// <summary>World ticks when the escort ended; 0 while it goes on.</summary>
    public static readonly int EndedAt = "whitehilt_trade_endedat".GetStableHashCode();

    /// <summary>The planned route: the turning points, packed by <see cref="RouteTrack.Pack"/> (a byte array); none for a straight run.</summary>
    public static readonly int Path = "whitehilt_trade_path".GetStableHashCode();

    /// <summary>The index of the turning point the ship steers for (an int).</summary>
    public static readonly int Point = "whitehilt_trade_point".GetStableHashCode();

    /// <summary>The length of the route when it was planned, in metres (a float), for the progress along it.</summary>
    public static readonly int RouteTotal = "whitehilt_trade_routetotal".GetStableHashCode();

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
