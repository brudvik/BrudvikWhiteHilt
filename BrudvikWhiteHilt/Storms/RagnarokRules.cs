using UnityEngine;

namespace BrudvikWhiteHilt.Storms;

/// <summary>
/// The rules of Ragnarok's sky and earth, apart from the game so they can be tested: how far the wolves have swallowed
/// the sun, and when the earth shakes. The quakes come at world times every machine works out alike, so players
/// together feel the same quake.
/// </summary>
public static class RagnarokRules
{
    /// <summary>The share of the light gone while Ragnarok gathers: the sky reddens.</summary>
    public const float GatheringEclipse = 0.35f;

    /// <summary>The share of the light gone in the eye: the sun glimpsed through the wolf's jaws.</summary>
    public const float EyeEclipse = 0.6f;

    /// <summary>
    /// How much of the light the wolves have swallowed.
    /// </summary>
    /// <param name="ragnarok">Whether the storm is Ragnarok.</param>
    /// <param name="phase">The storm's phase.</param>
    /// <param name="wave">The raging storm's wave.</param>
    /// <returns>0 for none, 1 for all.</returns>
    public static float Eclipse(bool ragnarok, StormPhase phase, StormWave wave)
    {
        if (!ragnarok)
        {
            return 0f;
        }

        return phase switch
        {
            StormPhase.Warning => GatheringEclipse,
            StormPhase.Raging => wave == StormWave.Eye ? EyeEclipse : 1f,
            _ => 0f
        };
    }

    /// <summary>
    /// The world time of the quake in a stretch of time: one in each stretch, somewhere in its first two thirds, after
    /// the stretch and the storm's number.
    /// </summary>
    /// <param name="slot">The stretch: the world time divided by <paramref name="period"/>, rounded down.</param>
    /// <param name="stormNumber">The storm's number.</param>
    /// <param name="period">Seconds in a stretch.</param>
    /// <returns>The world time.</returns>
    public static double QuakeTime(long slot, int stormNumber, float period)
    {
        // A small integer hash, so every machine picks the same moment.
        uint hash = (uint)(slot * 73856093L) ^ (uint)(stormNumber * 19349663);
        hash ^= hash >> 13;
        hash *= 0x5bd1e995;
        hash ^= hash >> 15;
        return (slot + hash % 1000 / 1000.0 * 0.67) * period;
    }

    /// <summary>
    /// The quake that came between two world times, if any.
    /// </summary>
    /// <param name="from">The earlier time, exclusive.</param>
    /// <param name="to">The later time, inclusive.</param>
    /// <param name="stormNumber">The storm's number.</param>
    /// <param name="period">Seconds between quakes on average.</param>
    /// <returns>True when a quake's moment lies between them.</returns>
    public static bool QuakeBetween(double from, double to, int stormNumber, float period)
    {
        if (period <= 0f || to <= from)
        {
            return false;
        }

        for (long slot = (long)System.Math.Floor(from / period); slot <= (long)System.Math.Floor(to / period); slot++)
        {
            double at = QuakeTime(slot, stormNumber, period);
            if (at > from && at <= to)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Whether the Fimbulwinter holds at a time.
    /// </summary>
    /// <param name="now">World time.</param>
    /// <param name="starts">When it began; 0 for none.</param>
    /// <param name="ends">When it ends.</param>
    /// <returns>True while it holds.</returns>
    public static bool Fimbulwinter(double now, double starts, double ends)
    {
        return starts > 0.0 && now >= starts && now < ends;
    }

    /// <summary>
    /// How long a crop has grown: the time since it was planted without the Fimbulwinter, when nothing grows.
    /// </summary>
    /// <param name="sincePlanted">Seconds since it was planted.</param>
    /// <param name="now">World time.</param>
    /// <param name="starts">When the Fimbulwinter began; 0 for none.</param>
    /// <param name="ends">When it ends.</param>
    /// <returns>Seconds it has grown.</returns>
    public static double Grown(double sincePlanted, double now, double starts, double ends)
    {
        if (starts <= 0.0 || ends <= starts)
        {
            return sincePlanted;
        }

        double planted = now - sincePlanted;
        double frozen = System.Math.Min(now, ends) - System.Math.Max(planted, starts);
        return frozen > 0.0 ? sincePlanted - frozen : sincePlanted;
    }

    /// <summary>
    /// How hard a quake shakes a moment after it began: it swells and dies away.
    /// </summary>
    /// <param name="seconds">Seconds since it began.</param>
    /// <param name="length">How long it lasts.</param>
    /// <returns>0 to 1.</returns>
    public static float QuakeStrength(float seconds, float length)
    {
        if (seconds < 0f || seconds >= length || length <= 0f)
        {
            return 0f;
        }

        return Mathf.Sin(seconds / length * Mathf.PI);
    }
}
