using System;
using UnityEngine;

namespace BrudvikWhiteHilt.SeaBattles.Damage;

/// <summary>
/// How a flooded ship behaves, from plain values so it can be tested without the game. Flood is the share of a full
/// ship, from 0 (dry) to 1 (full).
/// </summary>
public static class FloodModel
{
    /// <summary>
    /// One step of the flood. Open holes let water in; with no hole open it drains.
    /// </summary>
    /// <param name="flood">The flood before the step.</param>
    /// <param name="holes">Number of open holes.</param>
    /// <param name="seconds">Seconds the step covers.</param>
    /// <param name="perHole">Share of a full ship one hole lets in per second.</param>
    /// <param name="drain">Share of a full ship that drains per second when no hole is open.</param>
    /// <returns>The flood after the step, kept between 0 and 1.</returns>
    public static float Step(float flood, int holes, float seconds, float perHole, float drain)
    {
        float elapsed = Mathf.Max(0f, seconds);
        float change = holes > 0
            ? holes * Mathf.Max(0f, perHole) * elapsed
            : -Mathf.Max(0f, drain) * elapsed;

        return Mathf.Clamp01(flood + change);
    }

    /// <summary>
    /// How far the ship lies lower in the water.
    /// </summary>
    /// <param name="flood">The flood.</param>
    /// <param name="sinkDepth">Metres a full ship lies lower.</param>
    /// <returns>Metres lower, never negative.</returns>
    public static float Offset(float flood, float sinkDepth)
    {
        return Mathf.Clamp01(flood) * Mathf.Max(0f, sinkDepth);
    }

    /// <summary>
    /// Factor the sail force is multiplied by.
    /// </summary>
    /// <param name="flood">The flood.</param>
    /// <param name="loss">Share of the sail force a full ship loses.</param>
    /// <returns>A factor from 0 to 1; 1 for a dry ship.</returns>
    public static float SailFactor(float flood, float loss)
    {
        return 1f - Mathf.Clamp01(flood) * Mathf.Clamp01(loss);
    }

    /// <summary>
    /// Seconds a flood step covers, from the world time of the last step to now. A ship that nobody was near
    /// for a long time must not flood all at once when it loads, so the step is capped.
    /// </summary>
    /// <param name="lastTicks">World time of the last step, in ticks; 0 if there was none.</param>
    /// <param name="nowTicks">World time now, in ticks.</param>
    /// <param name="maxSeconds">The longest step.</param>
    /// <returns>Seconds, from 0 to <paramref name="maxSeconds"/>; 0 if there was no earlier step or time went backwards.</returns>
    public static float ElapsedSeconds(long lastTicks, long nowTicks, float maxSeconds)
    {
        if (lastTicks <= 0L || nowTicks <= lastTicks)
        {
            return 0f;
        }

        double seconds = (nowTicks - lastTicks) / (double)TimeSpan.TicksPerSecond;
        return (float)Math.Min(seconds, Math.Max(0f, maxSeconds));
    }

    /// <summary>
    /// Damage a full ship takes in a step; none until it is full.
    /// </summary>
    /// <param name="flood">The flood.</param>
    /// <param name="perSecond">Damage per second while full.</param>
    /// <param name="seconds">Seconds the step covers.</param>
    /// <returns>The damage, never negative.</returns>
    public static float HullDamage(float flood, float perSecond, float seconds)
    {
        return flood >= 1f ? Mathf.Max(0f, perSecond) * Mathf.Max(0f, seconds) : 0f;
    }

    /// <summary>
    /// The flood as a whole percent for a read-out.
    /// </summary>
    /// <param name="flood">The flood.</param>
    /// <returns>0 to 100.</returns>
    public static int Percent(float flood)
    {
        return Mathf.RoundToInt(Mathf.Clamp01(flood) * 100f);
    }
}
