using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.SeaBattles.Damage;

/// <summary>
/// How wide a hull is at one place, from the points of its mesh, so a hole goes in the hull's side and not in the
/// box that makes the ship float, which is wider than the hull. Plain vectors in the ship's local space.
/// </summary>
public static class HullShape
{
    /// <summary>
    /// The distance from the ship's middle to the hull's side, on one side, around a place on the hull.
    /// </summary>
    /// <param name="points">The hull's mesh points, in the ship's local space; may be null.</param>
    /// <param name="side">1 for starboard (positive x), -1 for port.</param>
    /// <param name="y">Height of the place.</param>
    /// <param name="yBand">How far above and below the height points count.</param>
    /// <param name="z">Place along the hull.</param>
    /// <param name="zBand">How far ahead and astern points count.</param>
    /// <param name="fallback">What to give when no point is near.</param>
    /// <returns>The greatest distance on that side among the points near the place; the fallback if there are none.</returns>
    public static float HalfWidth(IReadOnlyList<Vector3> points, float side, float y, float yBand, float z, float zBand, float fallback)
    {
        if (points == null)
        {
            return fallback;
        }

        float sign = side >= 0f ? 1f : -1f;
        float widest = 0f;
        for (int i = 0; i < points.Count; i++)
        {
            Vector3 point = points[i];
            float across = point.x * sign;
            if (across > widest && Mathf.Abs(point.y - y) <= yBand && Mathf.Abs(point.z - z) <= zBand)
            {
                widest = across;
            }
        }

        return widest > 0f ? widest : fallback;
    }

    /// <summary>
    /// How fast the hull's side leans outward with height around a place: metres of half width gained per metre up. A hull
    /// flares, so a mark on it is tilted to lie flat on it.
    /// </summary>
    /// <param name="points">The hull's mesh points, in the ship's local space; may be null.</param>
    /// <param name="side">1 for starboard (positive x), -1 for port.</param>
    /// <param name="y">Height of the place.</param>
    /// <param name="halfStep">Metres above and below the height at which the width is read.</param>
    /// <param name="yBand">How far above and below each of those heights points count.</param>
    /// <param name="z">Place along the hull.</param>
    /// <param name="zBand">How far ahead and astern points count.</param>
    /// <returns>The lean, never negative; 0 when the hull has no points at one of the two heights.</returns>
    public static float Flare(IReadOnlyList<Vector3> points, float side, float y, float halfStep, float yBand, float z, float zBand)
    {
        if (halfStep <= 0f)
        {
            return 0f;
        }

        float above = HalfWidth(points, side, y + halfStep, yBand, z, zBand, 0f);
        float below = HalfWidth(points, side, y - halfStep, yBand, z, zBand, 0f);
        return above > 0f && below > 0f ? Mathf.Max(0f, (above - below) / (2f * halfStep)) : 0f;
    }
}
