using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.SeaBattles.Escorts;

/// <summary>
/// How an escort ends, or that it goes on.
/// </summary>
public enum EscortOutcome
{
    /// <summary>The trading ship is still on its way.</summary>
    Sailing,

    /// <summary>It reached its destination.</summary>
    Arrived,

    /// <summary>Its hull is destroyed.</summary>
    Sunk,

    /// <summary>The cargo is gone.</summary>
    Plundered,

    /// <summary>Nobody was near for too long.</summary>
    Abandoned
}

/// <summary>
/// The plain rules of an escort: where the trading ship is going, when the waves come, how the raiders on the deck empty
/// the hold, how the escort ends and what it pays. They take numbers and delegates, never game objects, so the server
/// that approves a contract, the ship's owner and the tests all use the same code. Positions are world x and z as a
/// <see cref="Vector2"/>; headings are degrees from north.
/// </summary>
public static class EscortRules
{
    /// <summary>The destinations are tried at distances this far apart, from the farthest down to the nearest.</summary>
    public const float DistanceStep = 200f;

    // Float rounding must not cost a coin: 600 coins at 70 % of the cargo is 420, not 419.
    private const double PayRounding = 1e-4;

    /// <summary>
    /// Finds a destination ahead of a ship, reached by a straight line of deep water. Each bearing is tried in turn, and
    /// for each the distances from the farthest down to the nearest; a destination is good when every point on the line
    /// to it, every <paramref name="step"/> metres and at its end, is deep.
    /// </summary>
    /// <param name="from">World x and z of the ship.</param>
    /// <param name="heading">The ship's heading.</param>
    /// <param name="min">Least distance to the destination.</param>
    /// <param name="max">Most distance to the destination; below <paramref name="min"/> it counts as <paramref name="min"/>.</param>
    /// <param name="bearings">Degrees off the heading to try, in order.</param>
    /// <param name="deep">Whether a place has deep enough water.</param>
    /// <param name="step">Metres between the points checked on the line; at least 1.</param>
    /// <param name="destination">The place found, or <paramref name="from"/> when none.</param>
    /// <returns>False when no bearing and distance gave a line of deep water.</returns>
    public static bool TryPickDestination(Vector2 from, float heading, float min, float max, IReadOnlyList<float> bearings,
        Func<Vector2, bool> deep, float step, out Vector2 destination)
    {
        destination = from;
        if (bearings == null || deep == null || min <= 0f)
        {
            return false;
        }

        max = Mathf.Max(max, min);
        step = Mathf.Max(1f, step);
        foreach (float offset in bearings)
        {
            float angle = (heading + offset) * Mathf.Deg2Rad;
            Vector2 direction = new(Mathf.Sin(angle), Mathf.Cos(angle));
            for (float wanted = max; ; wanted -= DistanceStep)
            {
                float distance = Mathf.Max(wanted, min);
                if (LineIsDeep(from, direction, distance, step, deep))
                {
                    destination = from + direction * distance;
                    return true;
                }

                if (distance <= min)
                {
                    break;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// The shares of the route at which the waves come, sorted. The stretch from <paramref name="start"/> to
    /// <paramref name="end"/> is divided into one slot per wave, and each wave lies somewhere in its slot, kept from the
    /// slot's edges so that no two waves are closer than half a slot, that is <c>(end - start) / (count * 2)</c>.
    /// </summary>
    /// <param name="count">How many waves.</param>
    /// <param name="start">Share of the route where the first wave can come, 0 to 1.</param>
    /// <param name="end">Share of the route by which the last has come; below <paramref name="start"/> it counts as the start.</param>
    /// <param name="random">A random number from 0 up to but not including 1.</param>
    /// <returns>The shares in order, none for a count of 0 or less.</returns>
    public static float[] RollWaves(int count, float start, float end, Func<float> random)
    {
        if (count <= 0)
        {
            return Array.Empty<float>();
        }

        start = Mathf.Clamp01(start);
        end = Mathf.Clamp(end, start, 1f);
        float slot = (end - start) / count;
        float margin = slot / 4f;
        float[] waves = new float[count];
        for (int i = 0; i < count; i++)
        {
            float low = start + i * slot + (i > 0 ? margin : 0f);
            float high = start + (i + 1) * slot - (i < count - 1 ? margin : 0f);
            waves[i] = low + Mathf.Clamp01(random()) * (high - low);
        }

        return waves;
    }

    /// <summary>
    /// How far along its route a trading ship is: where its place falls on the line from the start to the destination.
    /// </summary>
    /// <param name="start">Where the trading ship began.</param>
    /// <param name="destination">Where it is going.</param>
    /// <param name="now">Where it is.</param>
    /// <returns>0 at the start or behind it, 1 at the destination or beyond it; 1 when start and destination are the same.</returns>
    public static float Progress(Vector2 start, Vector2 destination, Vector2 now)
    {
        Vector2 route = destination - start;
        float length = route.sqrMagnitude;
        return length <= 0f ? 1f : Mathf.Clamp01(Vector2.Dot(now - start, route) / length);
    }

    /// <summary>
    /// Whether the next wave is due.
    /// </summary>
    /// <param name="progress">The route done, 0 to 1.</param>
    /// <param name="waveAt">The share of the route at which each wave comes.</param>
    /// <param name="sent">How many waves have been sent.</param>
    /// <returns>True when a wave is left and the route has reached its share.</returns>
    public static bool WaveDue(float progress, IReadOnlyList<float> waveAt, int sent)
    {
        return waveAt != null && sent >= 0 && sent < waveAt.Count && progress >= waveAt[sent];
    }

    /// <summary>
    /// The cargo left after living raiders stood on the deck for a time.
    /// </summary>
    /// <param name="cargo">The cargo before, 0 to 1.</param>
    /// <param name="raidersAboard">Living raiders on the trading ship's deck.</param>
    /// <param name="percentPerRaiderPerSecond">Percent of the full cargo one raider carries off per second.</param>
    /// <param name="dt">Seconds passed.</param>
    /// <returns>The cargo after, never below 0 and never above what it was.</returns>
    public static float Plunder(float cargo, int raidersAboard, float percentPerRaiderPerSecond, float dt)
    {
        cargo = Mathf.Clamp01(cargo);
        if (raidersAboard <= 0 || percentPerRaiderPerSecond <= 0f || dt <= 0f)
        {
            return cargo;
        }

        return Mathf.Max(0f, cargo - raidersAboard * percentPerRaiderPerSecond / 100f * dt);
    }

    /// <summary>
    /// How the escort stands. A sinking comes first (a ship at the bottom does not arrive), then an empty hold (a ship
    /// that arrives with nothing aboard has not kept its word), then arriving, and last the merchant's patience.
    /// </summary>
    /// <param name="distanceToDestination">Metres from the trading ship to the destination.</param>
    /// <param name="arriveRadius">The distance within which it has arrived.</param>
    /// <param name="hullDestroyed">Whether the hull is destroyed or has no health.</param>
    /// <param name="cargo">The cargo left, 0 to 1.</param>
    /// <param name="secondsAlone">Seconds since a player was last within range.</param>
    /// <param name="abandonSeconds">How long the merchant waits.</param>
    /// <returns>The outcome.</returns>
    public static EscortOutcome Outcome(float distanceToDestination, float arriveRadius, bool hullDestroyed, float cargo,
        float secondsAlone, float abandonSeconds)
    {
        if (hullDestroyed)
        {
            return EscortOutcome.Sunk;
        }

        if (cargo <= 0f)
        {
            return EscortOutcome.Plundered;
        }

        if (distanceToDestination <= arriveRadius)
        {
            return EscortOutcome.Arrived;
        }

        return secondsAlone > abandonSeconds ? EscortOutcome.Abandoned : EscortOutcome.Sailing;
    }

    /// <summary>
    /// The coins a player is paid.
    /// </summary>
    /// <param name="full">The pay for a full cargo.</param>
    /// <param name="cargo">The cargo left, 0 to 1.</param>
    /// <returns>The coins, rounded down; never negative.</returns>
    public static int Pay(int full, float cargo)
    {
        return Math.Max(0, (int)Math.Floor(Math.Max(0, full) * (double)Mathf.Clamp01(cargo) + PayRounding));
    }

    /// <summary>
    /// The pay for a full cargo of a voyage from one trading post to another: <paramref name="perKilometre"/> for every
    /// 1000 m of the way, rounded.
    /// </summary>
    /// <param name="perKilometre">Coins per kilometre.</param>
    /// <param name="distance">The length of the voyage, in metres.</param>
    /// <returns>The coins for a full cargo.</returns>
    public static int PayForVoyage(float perKilometre, float distance)
    {
        return Math.Max(0, (int)Math.Round(Math.Max(0f, perKilometre) * Math.Max(0f, distance) / 1000.0));
    }

    /// <summary>
    /// Whether a point lies within a range of any of the given places.
    /// </summary>
    /// <param name="point">World x and z of the point.</param>
    /// <param name="places">The places, for example the trading ships on their way.</param>
    /// <param name="range">The range in metres; 0 or less never matches.</param>
    /// <returns>True when one of the places is within range.</returns>
    public static bool AnyWithin(Vector2 point, IEnumerable<Vector2> places, float range)
    {
        if (places == null || range <= 0f)
        {
            return false;
        }

        foreach (Vector2 place in places)
        {
            if (Vector2.Distance(point, place) <= range)
            {
                return true;
            }
        }

        return false;
    }

    // Every point of the line, every step metres and at its end, must be deep.
    private static bool LineIsDeep(Vector2 from, Vector2 direction, float distance, float step, Func<Vector2, bool> deep)
    {
        for (float along = step; along < distance; along += step)
        {
            if (!deep(from + direction * along))
            {
                return false;
            }
        }

        return deep(from + direction * distance);
    }
}
