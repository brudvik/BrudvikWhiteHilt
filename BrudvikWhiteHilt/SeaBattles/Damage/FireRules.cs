using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.SeaBattles.Damage;

/// <summary>
/// The rules of deck fires, from plain values so they can be tested without the game. Points are in the ship's local space.
/// </summary>
public static class FireRules
{
    /// <summary>Metres within which two fires are one: a pot that lands this near a fire feeds it.</summary>
    public const float Spacing = 1f;

    /// <summary>Metres above or below a fire at which a player still stands in it.</summary>
    public const float Height = 2f;

    // A thrown pot that lands this far outside the hull box, sideways or along, is not on the ship.
    private const float HullMargin = 1f;
    private const float BelowHull = 1f;
    private const float AboveHull = 5f;

    /// <summary>
    /// World ticks at which a fire lit now burns out.
    /// </summary>
    /// <param name="now">World ticks now.</param>
    /// <param name="seconds">Seconds it burns.</param>
    /// <returns>The expiry; <paramref name="now"/> itself when the time is 0 or less.</returns>
    public static long Expiry(long now, float seconds)
    {
        return now + Ticks(seconds);
    }

    /// <summary>
    /// World ticks in a number of seconds, never negative.
    /// </summary>
    /// <param name="seconds">Seconds.</param>
    /// <returns>Ticks.</returns>
    public static long Ticks(float seconds)
    {
        return seconds <= 0f ? 0L : (long)(seconds * TimeSpan.TicksPerSecond);
    }

    /// <summary>
    /// Whether a player at a point stands in a fire: within the radius sideways and within <see cref="Height"/> up or down.
    /// </summary>
    /// <param name="fires">The fire points.</param>
    /// <param name="point">The player's point.</param>
    /// <param name="radius">Metres round a fire that burn.</param>
    /// <returns>True if the point is in any fire.</returns>
    public static bool Burns(IReadOnlyList<Vector3> fires, Vector3 point, float radius)
    {
        float limit = radius * radius;
        for (int i = 0; i < fires.Count; i++)
        {
            Vector3 to = fires[i] - point;
            if (Mathf.Abs(to.y) <= Height && to.x * to.x + to.z * to.z <= limit)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The fire damage the hull takes over a time from a number of fires.
    /// </summary>
    /// <param name="fires">How many fires burn.</param>
    /// <param name="perFire">Fire damage per second from one fire.</param>
    /// <param name="seconds">Seconds.</param>
    /// <returns>The damage, never negative.</returns>
    public static float HullDamage(int fires, float perFire, float seconds)
    {
        return Mathf.Max(0f, fires) * Mathf.Max(0f, perFire) * Mathf.Max(0f, seconds);
    }

    /// <summary>
    /// Whether a point sent over the network can be a place on a ship: a number, and not far outside the hull's box. The
    /// owner uses this on a pot's landing before it lights a fire there.
    /// </summary>
    /// <param name="local">The point, in the ship's local space.</param>
    /// <param name="hullCentre">Centre of the hull's box in that space.</param>
    /// <param name="hullSize">Size of the box: x across, y up, z along.</param>
    /// <returns>True if the point is on or near the hull.</returns>
    public static bool ValidSpot(Vector3 local, Vector3 hullCentre, Vector3 hullSize)
    {
        float sum = local.x + local.y + local.z;
        if (float.IsNaN(sum) || float.IsInfinity(sum))
        {
            return false;
        }

        Vector3 offset = local - hullCentre;
        return Mathf.Abs(offset.x) <= Mathf.Abs(hullSize.x) / 2f + HullMargin
            && Mathf.Abs(offset.z) <= Mathf.Abs(hullSize.z) / 2f + HullMargin
            && offset.y >= -BelowHull && offset.y <= AboveHull;
    }
}
