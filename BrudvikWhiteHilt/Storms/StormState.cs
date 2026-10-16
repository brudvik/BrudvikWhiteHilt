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
    Raging,

    /// <summary>A storm is decided but not yet announced: only the weather forecast shows it.</summary>
    Brewing
}

/// <summary>
/// Where a raging storm is: its first wave, the still eye in the middle, or its last and fiercest wave.
/// </summary>
public enum StormWave
{
    /// <summary>No storm rages.</summary>
    None,

    /// <summary>The storm before the eye, or the whole storm when it has none.</summary>
    First,

    /// <summary>The eye: the wind dies, and the storm comes back worse.</summary>
    Eye,

    /// <summary>The storm after the eye.</summary>
    Last
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

    /// <summary>World time the last storm passed; 0 if none has since the world was loaded.</summary>
    public static double LastEnded { get; private set; }

    /// <summary>Whether the storm brewing, coming or raging is Ragnarok, the worst of all.</summary>
    public static bool Ragnarok { get; private set; }

    /// <summary>World time the eye of the raging storm opens; 0 when it has none.</summary>
    public static double EyeStarts { get; private set; }

    /// <summary>World time the eye closes and the last wave comes.</summary>
    public static double EyeEnds { get; private set; }

    /// <summary>The wave of the raging storm now.</summary>
    public static StormWave Wave => ZNet.instance == null ? StormWave.None : WaveAt(Phase, ZNet.instance.GetTimeSeconds(), EyeStarts, EyeEnds);

    /// <summary>Whether the storm is in its still eye.</summary>
    public static bool InEye => Wave == StormWave.Eye;

    /// <summary>
    /// The wave of a storm at a time.
    /// </summary>
    /// <param name="phase">The phase.</param>
    /// <param name="now">World time.</param>
    /// <param name="eyeStarts">When the eye opens; 0 for none.</param>
    /// <param name="eyeEnds">When it closes.</param>
    /// <returns>The wave; <see cref="StormWave.None"/> unless raging.</returns>
    public static StormWave WaveAt(StormPhase phase, double now, double eyeStarts, double eyeEnds)
    {
        if (phase != StormPhase.Raging)
        {
            return StormWave.None;
        }

        if (eyeStarts <= 0.0 || now < eyeStarts)
        {
            return StormWave.First;
        }

        return now < eyeEnds ? StormWave.Eye : StormWave.Last;
    }

    /// <summary>
    /// How hard a storm drives the exposure on, 1 being an ordinary storm's first wave.
    /// </summary>
    /// <param name="wave">The wave.</param>
    /// <param name="ragnarok">Whether it is Ragnarok.</param>
    /// <param name="lastWave">The last wave's fury.</param>
    /// <param name="ragnarokFury">Ragnarok's fury, on top.</param>
    /// <returns>0 in the eye and when calm.</returns>
    public static float Fury(StormWave wave, bool ragnarok, float lastWave, float ragnarokFury)
    {
        float fury = wave switch
        {
            StormWave.First => 1f,
            StormWave.Last => Mathf.Max(0f, lastWave),
            _ => 0f
        };
        return fury * (ragnarok ? Mathf.Max(0f, ragnarokFury) : 1f);
    }

    /// <summary>
    /// Where the eye of a storm goes: after a share of it, for some seconds that are added to its length.
    /// </summary>
    /// <param name="now">World time the storm breaks.</param>
    /// <param name="stormSeconds">How long it rages, without the eye.</param>
    /// <param name="eyeSeconds">How long the eye lasts; 0 for none.</param>
    /// <param name="split">The share of the storm before the eye.</param>
    /// <returns>When the eye opens and closes (0 and 0 for none), and when the storm ends.</returns>
    public static (double EyeStarts, double EyeEnds, double Ends) PlanEye(double now, float stormSeconds, float eyeSeconds, float split)
    {
        if (eyeSeconds <= 0f)
        {
            return (0.0, 0.0, now + stormSeconds);
        }

        double opens = now + stormSeconds * Mathf.Clamp01(split);
        return (opens, opens + eyeSeconds, now + stormSeconds + eyeSeconds);
    }

    /// <summary>
    /// Stores what the storm is: Ragnarok or not, and its eye. On the server; sent with the state.
    /// </summary>
    /// <param name="ragnarok">Whether it is Ragnarok.</param>
    /// <param name="eyeStarts">When the eye opens; 0 for none.</param>
    /// <param name="eyeEnds">When it closes.</param>
    public static void SetStorm(bool ragnarok, double eyeStarts, double eyeEnds)
    {
        Ragnarok = ragnarok;
        EyeStarts = eyeStarts;
        EyeEnds = eyeEnds;
    }

    /// <summary>
    /// Seconds since the last storm passed.
    /// </summary>
    /// <returns>Seconds, or -1 when none has.</returns>
    public static double SinceLastStorm()
    {
        return LastEnded <= 0.0 || ZNet.instance == null ? -1.0 : ZNet.instance.GetTimeSeconds() - LastEnded;
    }

    /// <summary>
    /// Notes that a storm has passed. On the server; sent with the state.
    /// </summary>
    /// <param name="time">World time it passed.</param>
    public static void Passed(double time)
    {
        LastEnded = time;
    }

    /// <summary>Whether a storm rages now.</summary>
    public static bool Raging => Phase == StormPhase.Raging;

    /// <summary>Whether a storm is coming.</summary>
    public static bool Coming => Phase == StormPhase.Warning;

    /// <summary>Whether a storm is announced or raging, so the sky, the sound and the clock show it.</summary>
    public static bool Visible => Phase is StormPhase.Warning or StormPhase.Raging;

    /// <summary>
    /// Seconds until the storm breaks.
    /// </summary>
    /// <param name="warningSeconds">How long the warning lasts.</param>
    /// <returns>Seconds, 0 when it rages or no storm is coming.</returns>
    public static float SecondsToBreak(float warningSeconds)
    {
        return SecondsToBreak(Phase, SecondsLeft(), warningSeconds);
    }

    /// <summary>
    /// Seconds until a storm breaks.
    /// </summary>
    /// <param name="phase">The phase.</param>
    /// <param name="left">Seconds left of the phase.</param>
    /// <param name="warningSeconds">How long the warning lasts.</param>
    /// <returns>Seconds, 0 when it rages or no storm is coming.</returns>
    public static float SecondsToBreak(StormPhase phase, float left, float warningSeconds)
    {
        return phase switch
        {
            StormPhase.Brewing => left + warningSeconds,
            StormPhase.Warning => left,
            _ => 0f
        };
    }

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
        package.Write(LastEnded);
        package.Write(Ragnarok);
        package.Write(EyeStarts);
        package.Write(EyeEnds);
        return package;
    }

    /// <summary>
    /// Reads a state the server sent.
    /// </summary>
    /// <param name="package">The package.</param>
    public static void Read(ZPackage package)
    {
        Set((StormPhase)package.ReadInt(), package.ReadDouble(), package.ReadInt());
        LastEnded = package.ReadDouble();
        SetStorm(package.ReadBool(), package.ReadDouble(), package.ReadDouble());
    }

    /// <summary>
    /// Forgets the last world's storm.
    /// </summary>
    public static void Reset()
    {
        Set(StormPhase.Calm, 0.0, 0);
        SetStorm(false, 0.0, 0.0);
        LastEnded = 0.0;
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
