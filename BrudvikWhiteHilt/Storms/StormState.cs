using UnityEngine;

namespace BrudvikWhiteHilt.Storms;

/// <summary>
/// Where the world is in a storm.
/// </summary>
public enum StormPhase
{
    /// <summary>No storm.</summary>
    Calm,

    /// <summary>A storm is coming: the sky darkens and everyone is warned.</summary>
    Warning,

    /// <summary>The storm rages in every biome that has one.</summary>
    Raging
}

/// <summary>
/// The storm as the server last sent it. The server decides when a storm comes; each player is hit by the storm of the
/// biome they are in. Times are the world's time in seconds, so every machine counts down alike.
/// </summary>
public static class StormState
{
    /// <summary>The phase.</summary>
    public static StormPhase Phase { get; private set; }

    /// <summary>World time the phase ends; ignored when calm.</summary>
    public static double PhaseEnds { get; private set; }

    /// <summary>Counts every storm, so a client sees a new one even if it missed the calm in between.</summary>
    public static int Number { get; private set; }

    /// <summary>Whether a storm rages now.</summary>
    public static bool Raging => Phase == StormPhase.Raging;

    /// <summary>Whether a storm is coming.</summary>
    public static bool Coming => Phase == StormPhase.Warning;

    /// <summary>
    /// Stores a new state.
    /// </summary>
    /// <param name="phase">The phase.</param>
    /// <param name="ends">World time it ends.</param>
    /// <param name="number">The storm's number.</param>
    public static void Set(StormPhase phase, double ends, int number)
    {
        Phase = phase;
        PhaseEnds = ends;
        Number = number;
    }

    /// <summary>
    /// Seconds until the phase ends.
    /// </summary>
    /// <returns>Seconds, 0 when calm or overdue.</returns>
    public static float SecondsLeft()
    {
        if (Phase == StormPhase.Calm || ZNet.instance == null)
        {
            return 0f;
        }

        return Mathf.Max(0f, (float)(PhaseEnds - ZNet.instance.GetTimeSeconds()));
    }

    /// <summary>
    /// Writes the state for the clients.
    /// </summary>
    /// <returns>The package.</returns>
    public static ZPackage Write()
    {
        ZPackage package = new();
        package.Write((int)Phase);
        package.Write(PhaseEnds);
        package.Write(Number);
        return package;
    }

    /// <summary>
    /// Reads a state the server sent.
    /// </summary>
    /// <param name="package">The package.</param>
    public static void Read(ZPackage package)
    {
        Set((StormPhase)package.ReadInt(), package.ReadDouble(), package.ReadInt());
    }

    /// <summary>
    /// Forgets the last world's storm.
    /// </summary>
    public static void Reset()
    {
        Set(StormPhase.Calm, 0.0, 0);
    }

    /// <summary>
    /// The chance that a storm starts at one check, so that on average <paramref name="perDay"/> storms come in a day.
    /// </summary>
    /// <param name="perDay">Storms per game day.</param>
    /// <param name="interval">Seconds between checks.</param>
    /// <param name="dayLength">Seconds in a game day.</param>
    /// <returns>The chance, 0 to 1.</returns>
    public static float ChancePerCheck(float perDay, float interval, float dayLength)
    {
        if (perDay <= 0f || interval <= 0f || dayLength <= 0f)
        {
            return 0f;
        }

        // One minus the chance of no storm over the checks of a day, spread evenly: 1 - exp(-rate * interval).
        return Mathf.Clamp01(1f - Mathf.Exp(-perDay * interval / dayLength));
    }

    /// <summary>
    /// The minutes and seconds left, as "2:05".
    /// </summary>
    /// <param name="seconds">Seconds.</param>
    /// <returns>The text.</returns>
    public static string Countdown(float seconds)
    {
        int whole = Mathf.Max(0, Mathf.CeilToInt(seconds));
        return $"{whole / 60}:{whole % 60:00}";
    }
}
