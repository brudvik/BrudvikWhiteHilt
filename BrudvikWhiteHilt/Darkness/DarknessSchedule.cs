using UnityEngine;

namespace BrudvikWhiteHilt.Darkness;

/// <summary>
/// Where a darkness is.
/// </summary>
public enum DarkPhase
{
    /// <summary>The place is lit as ever.</summary>
    Calm,

    /// <summary>The lights flicker: the darkness is coming.</summary>
    Flicker,

    /// <summary>The light fades.</summary>
    Fading,

    /// <summary>Almost black.</summary>
    Black,

    /// <summary>The light comes back.</summary>
    Returning
}

/// <summary>
/// When the darkness comes to a place below ground, worked out alike on every machine from when the place was first
/// gone into (the server's word) and the place itself, so everyone inside has it at once. It comes after a calm, then
/// more and more often and for longer, the longer the place has been in use.
/// </summary>
public static class DarknessSchedule
{
    /// <summary>Seconds the lights flicker before a darkness.</summary>
    public const float FlickerSeconds = 8f;

    /// <summary>Seconds the light takes to fade.</summary>
    public const float FadeSeconds = 5f;

    /// <summary>Seconds the light takes to come back.</summary>
    public const float ReturnSeconds = 3f;

    private const int MostEvents = 1000;

    /// <summary>
    /// The timing of the darkness, from the settings.
    /// </summary>
    public readonly struct Timing
    {
        /// <summary>Creates a timing.</summary>
        /// <param name="calm">Seconds before the first darkness.</param>
        /// <param name="longestGap">Seconds between darknesses at first.</param>
        /// <param name="shortestGap">Seconds between darknesses at the most.</param>
        /// <param name="ramp">Seconds after the first darkness until it is at its worst.</param>
        /// <param name="shortestBlack">Seconds of black at first.</param>
        /// <param name="longestBlack">Seconds of black at its worst.</param>
        public Timing(float calm, float longestGap, float shortestGap, float ramp, float shortestBlack, float longestBlack)
        {
            Calm = calm;
            LongestGap = longestGap;
            ShortestGap = shortestGap;
            Ramp = ramp;
            ShortestBlack = shortestBlack;
            LongestBlack = longestBlack;
        }

        /// <summary>Seconds before the first darkness.</summary>
        public float Calm { get; }

        /// <summary>Seconds between darknesses at first.</summary>
        public float LongestGap { get; }

        /// <summary>Seconds between darknesses at the most.</summary>
        public float ShortestGap { get; }

        /// <summary>Seconds after the first darkness until it is at its worst.</summary>
        public float Ramp { get; }

        /// <summary>Seconds of black at first.</summary>
        public float ShortestBlack { get; }

        /// <summary>Seconds of black at its worst.</summary>
        public float LongestBlack { get; }
    }

    /// <summary>
    /// Where the darkness is at a time.
    /// </summary>
    /// <param name="elapsed">Seconds since the place was first gone into.</param>
    /// <param name="seed">The place's own number, so places differ.</param>
    /// <param name="timing">The timing.</param>
    /// <param name="into">Seconds into the phase.</param>
    /// <param name="length">Seconds the phase lasts; for a calm, until the next flicker.</param>
    /// <returns>The phase.</returns>
    public static DarkPhase At(double elapsed, int seed, Timing timing, out float into, out float length)
    {
        double start = FirstDarkness(seed, timing);
        for (int index = 1; index < MostEvents; index++)
        {
            if (elapsed < start)
            {
                into = 0f;
                length = (float)(start - elapsed);
                return DarkPhase.Calm;
            }

            float worse = Worse(start, timing);
            float black = Mathf.Lerp(timing.ShortestBlack, timing.LongestBlack, worse) * Jitter(seed, index * 7 + 3);
            float[] phases = { FlickerSeconds, FadeSeconds, black, ReturnSeconds };
            double at = start;
            for (int phase = 0; phase < phases.Length; phase++)
            {
                if (elapsed < at + phases[phase])
                {
                    into = (float)(elapsed - at);
                    length = phases[phase];
                    return (DarkPhase)(phase + 1);
                }

                at += phases[phase];
            }

            start = at + Gap(start, timing, seed, index);
        }

        into = 0f;
        length = float.MaxValue;
        return DarkPhase.Calm;
    }

    /// <summary>
    /// Seconds after the place was first gone into that the first darkness flickers in.
    /// </summary>
    /// <param name="seed">The place's number.</param>
    /// <param name="timing">The timing.</param>
    /// <returns>The calm, give or take a fifth.</returns>
    public static double FirstDarkness(int seed, Timing timing) => timing.Calm * Jitter(seed, 0);

    /// <summary>
    /// How dark it is, from 0 (as ever) to 1 (black).
    /// </summary>
    /// <param name="phase">The phase.</param>
    /// <param name="into">Seconds into it.</param>
    /// <param name="length">Its length.</param>
    /// <param name="time">The time, for the flicker.</param>
    /// <returns>The darkness.</returns>
    public static float Level(DarkPhase phase, float into, float length, float time)
    {
        float share = length > 0f ? Mathf.Clamp01(into / length) : 1f;
        return phase switch
        {
            // Quick dips that grow deeper as the flicker goes on.
            DarkPhase.Flicker => Mathf.Clamp01(Flutter(time) * 1.4f - 0.55f) * Mathf.Lerp(0.3f, 0.9f, share),
            DarkPhase.Fading => Mathf.Lerp(0.5f, 1f, share),
            DarkPhase.Black => 1f,
            DarkPhase.Returning => 1f - share,
            _ => 0f
        };
    }

    /// <summary>
    /// How quiet the place is, from 0 (as ever) to 1 (silent): the sounds fall away while the lights flicker, before the
    /// dark, stay away in it and come back with the light.
    /// </summary>
    /// <param name="phase">The phase.</param>
    /// <param name="into">Seconds into it.</param>
    /// <param name="length">Its length.</param>
    /// <returns>The silence.</returns>
    public static float Silence(DarkPhase phase, float into, float length)
    {
        float share = length > 0f ? Mathf.Clamp01(into / length) : 1f;
        return phase switch
        {
            DarkPhase.Flicker => share,
            DarkPhase.Fading or DarkPhase.Black => 1f,
            DarkPhase.Returning => 1f - share,
            _ => 0f
        };
    }

    /// <summary>
    /// How far the darkness has crept into a place, from 0 at its first darkness to 1 at its worst.
    /// </summary>
    /// <param name="elapsed">Seconds since the place was first gone into.</param>
    /// <param name="timing">The timing.</param>
    /// <returns>0 to 1.</returns>
    public static float Creep(double elapsed, Timing timing) => Worse(elapsed, timing);

    /// <summary>
    /// A place's number from where it is, the same on every machine.
    /// </summary>
    /// <param name="key">The place's key.</param>
    /// <returns>The number.</returns>
    public static int Seed(string key)
    {
        unchecked
        {
            int hash = 23;
            foreach (char c in key ?? string.Empty)
            {
                hash = hash * 31 + c;
            }

            return hash;
        }
    }

    // A restless wavering from 0 to 1, as of flames in a draught: three waves out of step.
    private static float Flutter(float time)
    {
        return 0.5f + 0.25f * Mathf.Sin(time * 13.1f) + 0.15f * Mathf.Sin(time * 29.7f + 1.3f) + 0.1f * Mathf.Sin(time * 47.3f + 2.1f);
    }

    // How far the darkness has crept in, 0 at the first darkness to 1 after the ramp.
    private static float Worse(double start, Timing timing)
    {
        return timing.Ramp <= 0f ? 1f : Mathf.Clamp01((float)((start - timing.Calm) / timing.Ramp));
    }

    private static double Gap(double start, Timing timing, int seed, int index)
    {
        float longest = Mathf.Max(timing.LongestGap, timing.ShortestGap);
        return Mathf.Lerp(longest, timing.ShortestGap, Worse(start, timing)) * Jitter(seed, index * 7 + 1);
    }

    // From 0.8 to 1.2, the same for the same seed and index on every machine.
    private static float Jitter(int seed, int index)
    {
        unchecked
        {
            uint hash = (uint)(seed * 73856093) ^ (uint)(index * 19349663);
            hash ^= hash >> 13;
            hash *= 0x5bd1e995;
            hash ^= hash >> 15;
            return 0.8f + (hash % 1000) / 999f * 0.4f;
        }
    }
}
