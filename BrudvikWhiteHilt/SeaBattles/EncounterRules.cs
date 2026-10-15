using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.SeaBattles;

/// <summary>
/// The plain rules of a sea encounter: how likely it is, when the cooldown is over, whether land lies near, and where
/// the Nail Ship may appear. They take numbers and delegates, never game objects, so the client that asks, the server
/// that answers and the tests all use the same code. Positions are world x and z as a <see cref="Vector2"/>.
/// </summary>
public static class EncounterRules
{
    /// <summary>How many directions are sampled when looking for land round a ship.</summary>
    public const int ShoreDirections = 16;

    /// <summary>Weather flag of an encounter request: it is night.</summary>
    public const int WeatherNight = 1;

    /// <summary>Weather flag of an encounter request: a storm blows.</summary>
    public const int WeatherStorm = 2;

    /// <summary>Weather flag of an encounter request: fog or mist lies on the sea.</summary>
    public const int WeatherFog = 4;

    /// <summary>
    /// The names of the eight bearings <see cref="Bearing"/> gives, clockwise from dead ahead; the texts are
    /// <c>whitehilt_bearing_&lt;name&gt;</c>.
    /// </summary>
    public static readonly string[] BearingNames =
    {
        "ahead", "starboardbow", "starboard", "starboardquarter", "astern", "portquarter", "port", "portbow"
    };

    // The ships an encounter order raises are one number: the size in the low bits, then a fire ship and a ram to force.
    private const int SizeMask = 0xF;
    private const int FireShipBit = 0x10;
    private const int ForceRamBit = 0x20;

    // The rings of samples lie this far apart, so a small island between the ship and the limit is still found.
    private const float ShoreRingStep = 25f;

    /// <summary>
    /// The factor on the chance of an encounter from the weather: each of night, storm and fog that holds multiplies it by
    /// its setting.
    /// </summary>
    /// <param name="weather">The flags <see cref="WeatherNight"/>, <see cref="WeatherStorm"/> and <see cref="WeatherFog"/>.</param>
    /// <param name="night">The factor at night.</param>
    /// <param name="storm">The factor in a storm.</param>
    /// <param name="fog">The factor in fog.</param>
    /// <returns>The factor, never negative.</returns>
    public static float WeatherFactor(int weather, float night, float storm, float fog)
    {
        float factor = 1f;
        factor *= (weather & WeatherNight) != 0 ? Mathf.Max(0f, night) : 1f;
        factor *= (weather & WeatherStorm) != 0 ? Mathf.Max(0f, storm) : 1f;
        factor *= (weather & WeatherFog) != 0 ? Mathf.Max(0f, fog) : 1f;
        return factor;
    }

    /// <summary>
    /// Which of eight bearings a point lies on, as seen from a ship: 0 dead ahead, then clockwise in steps of 45 degrees, so
    /// 1 is off the starboard bow and 7 off the port bow. See <see cref="BearingNames"/>.
    /// </summary>
    /// <param name="relativeDegrees">The point's direction less the ship's heading, in degrees, any range.</param>
    /// <returns>0 to 7.</returns>
    public static int Bearing(float relativeDegrees)
    {
        float angle = Mathf.Repeat(float.IsNaN(relativeDegrees) ? 0f : relativeDegrees, 360f);
        return Mathf.FloorToInt((angle + 22.5f) / 45f) % 8;
    }

    /// <summary>
    /// Packs the ships of an encounter order into the one number the order carries.
    /// </summary>
    /// <param name="size">Ships in the fleet, 1 to 3.</param>
    /// <param name="fireShip">Whether a fire ship comes along.</param>
    /// <param name="forceRam">Whether the lead ship carries a ram whatever the chance says (an admin's order).</param>
    /// <returns>The packed number.</returns>
    public static int PackShips(int size, bool fireShip, bool forceRam = false)
    {
        return (Mathf.Clamp(size, 1, SizeMask) & SizeMask) | (fireShip ? FireShipBit : 0) | (forceRam ? ForceRamBit : 0);
    }

    /// <summary>
    /// Reads a number packed by <see cref="PackShips"/>. A plain size, as older orders send, reads as that size alone.
    /// </summary>
    /// <param name="packed">The packed number.</param>
    /// <param name="fireShip">Whether a fire ship comes along.</param>
    /// <param name="forceRam">Whether the lead ship carries a ram whatever the chance says.</param>
    /// <returns>The size of the fleet.</returns>
    public static int UnpackShips(int packed, out bool fireShip, out bool forceRam)
    {
        fireShip = (packed & FireShipBit) != 0;
        forceRam = (packed & ForceRamBit) != 0;
        return packed & SizeMask;
    }

    /// <summary>
    /// The chance, in percent, that a ship is found by a Nail Ship this minute.
    /// </summary>
    /// <param name="baseChance">Percent for one player.</param>
    /// <param name="perExtraPlayer">Extra percentage points per additional player aboard.</param>
    /// <param name="playersAboard">Players aboard the ship.</param>
    /// <returns>0 to 100; 0 for a ship with nobody aboard or a base chance of 0.</returns>
    public static float AttackChance(float baseChance, float perExtraPlayer, int playersAboard)
    {
        if (playersAboard <= 0 || baseChance <= 0f)
        {
            return 0f;
        }

        return Mathf.Clamp(baseChance + (playersAboard - 1) * perExtraPlayer, 0f, 100f);
    }

    /// <summary>
    /// Whether the dice fall for an encounter.
    /// </summary>
    /// <param name="chancePercent">The chance in percent.</param>
    /// <param name="roll">A random number from 0 up to but not including 1.</param>
    /// <returns>True when the roll is under the chance.</returns>
    public static bool Rolled(float chancePercent, float roll)
    {
        return roll * 100f < chancePercent;
    }

    /// <summary>
    /// Whether enough time has passed since the last Nail Ship anywhere in the world.
    /// </summary>
    /// <param name="now">Real time in seconds.</param>
    /// <param name="last">Real time of the last one; negative infinity for none yet.</param>
    /// <param name="cooldownMinutes">The cooldown in minutes.</param>
    /// <returns>True when a new one may come.</returns>
    public static bool CooldownOver(float now, float last, float cooldownMinutes)
    {
        return cooldownMinutes <= 0f || now - last >= cooldownMinutes * 60f;
    }

    /// <summary>
    /// Extra melee crew for the players aboard the hunted ship beyond the first.
    /// </summary>
    /// <param name="playersAboard">Players aboard.</param>
    /// <param name="perExtraPlayer">Crew per additional player.</param>
    /// <returns>The extra crew, never negative.</returns>
    public static int CrewBonus(int playersAboard, int perExtraPlayer)
    {
        return Mathf.Max(0, playersAboard - 1) * Mathf.Max(0, perExtraPlayer);
    }

    /// <summary>
    /// The distance to the nearest land found round a point, sampling a ring every 25 m in 16 directions out to a limit.
    /// </summary>
    /// <param name="x">World x of the centre.</param>
    /// <param name="z">World z of the centre.</param>
    /// <param name="limit">How far to look, in metres.</param>
    /// <param name="height">The ground height at a world x and z.</param>
    /// <param name="waterLevel">The height of the sea; ground at or above it is land.</param>
    /// <returns>The ring distance where land was first found, or infinity when there is none within the limit.</returns>
    public static float NearestLand(float x, float z, float limit, Func<float, float, float> height, float waterLevel)
    {
        if (limit <= 0f)
        {
            return float.PositiveInfinity;
        }

        for (float ring = ShoreRingStep; ; ring += ShoreRingStep)
        {
            float distance = Mathf.Min(ring, limit);
            for (int i = 0; i < ShoreDirections; i++)
            {
                float angle = i * 360f / ShoreDirections * Mathf.Deg2Rad;
                if (height(x + Mathf.Sin(angle) * distance, z + Mathf.Cos(angle) * distance) >= waterLevel)
                {
                    return distance;
                }
            }

            if (distance >= limit)
            {
                return float.PositiveInfinity;
            }
        }
    }

    /// <summary>
    /// Whether no land lies within a distance of a point.
    /// </summary>
    /// <param name="x">World x of the centre.</param>
    /// <param name="z">World z of the centre.</param>
    /// <param name="minDistance">The least distance to land, in metres; 0 accepts any shore.</param>
    /// <param name="height">The ground height at a world x and z.</param>
    /// <param name="waterLevel">The height of the sea.</param>
    /// <returns>True when open sea lies all round.</returns>
    public static bool ShoreIsClear(float x, float z, float minDistance, Func<float, float, float> height, float waterLevel)
    {
        return float.IsPositiveInfinity(NearestLand(x, z, minDistance, height, waterLevel));
    }

    /// <summary>
    /// Candidate bearings, as degrees off the target's heading, spread evenly over the arc ahead of it. The first is
    /// chosen by <paramref name="start"/>, the others follow round the arc, so a few misses still leave the whole arc tried.
    /// </summary>
    /// <param name="arc">Degrees either side of straight ahead.</param>
    /// <param name="count">How many candidates.</param>
    /// <param name="start">Where in the arc the first one lies, from 0 up to but not including 1.</param>
    /// <returns>The offsets, each from -arc up to but not including arc.</returns>
    public static IEnumerable<float> Offsets(float arc, int count, float start)
    {
        for (int i = 0; i < count; i++)
        {
            float t = Mathf.Repeat(start + i / (float)count, 1f);
            yield return arc * (2f * t - 1f);
        }
    }

    /// <summary>
    /// Picks the first candidate place ahead of a ship that the caller accepts.
    /// </summary>
    /// <param name="centre">World x and z of the ship.</param>
    /// <param name="heading">The ship's heading in degrees from north.</param>
    /// <param name="distance">How far from the ship the place lies.</param>
    /// <param name="offsets">Degrees off the heading to try, in order.</param>
    /// <param name="acceptable">Whether a place will do, for example deep water.</param>
    /// <param name="point">The place found.</param>
    /// <returns>False when no candidate was accepted.</returns>
    public static bool TryPickSpawn(Vector2 centre, float heading, float distance, IEnumerable<float> offsets,
        Func<Vector2, bool> acceptable, out Vector2 point)
    {
        foreach (float offset in offsets)
        {
            float angle = (heading + offset) * Mathf.Deg2Rad;
            Vector2 candidate = centre + new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * distance;
            if (acceptable(candidate))
            {
                point = candidate;
                return true;
            }
        }

        point = centre;
        return false;
    }

    /// <summary>
    /// A moving ship's velocity from two positions, blended into the last estimate. A ship another machine owns has no
    /// real velocity here, so a player who leaps onto it carries what its movement shows.
    /// </summary>
    /// <param name="previous">The ship's position at the last sample.</param>
    /// <param name="current">The ship's position now.</param>
    /// <param name="seconds">Seconds between the samples.</param>
    /// <param name="smoothed">The estimate so far.</param>
    /// <param name="blend">How much of the new sample to take, 0 to 1.</param>
    /// <returns>The new estimate; the old one when no time has passed.</returns>
    public static Vector3 SmoothedVelocity(Vector3 previous, Vector3 current, float seconds, Vector3 smoothed, float blend)
    {
        if (seconds <= 0.001f)
        {
            return smoothed;
        }

        return Vector3.Lerp(smoothed, (current - previous) / seconds, Mathf.Clamp01(blend));
    }
}
