using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.SeaBattles.Damage;

/// <summary>
/// The rules for holes in a ship's hull, from plain values so they can be tested without the game: whether a hit
/// makes a hole, where the hole goes and which hole a player means. Points are in the ship's local space.
/// </summary>
public static class HoleRules
{
    /// <summary>Metres a hole sits inside the hull's side.</summary>
    public const float SideInset = 0.15f;

    /// <summary>Metres above the waterline a hole is placed.</summary>
    public const float AboveWaterline = 0.25f;

    /// <summary>Metres kept free between a hole and the hull's bow and stern.</summary>
    public const float EndMargin = 0.5f;

    /// <summary>A new hole this close to an old one is the same hole.</summary>
    public const float Spacing = 1f;

    /// <summary>
    /// Whether a hit knocks a hole in the hull. A ram always does; any other hit only in a battle and only when it is
    /// heavy enough.
    /// </summary>
    /// <param name="rawPhysical">Blunt, pierce, slash and chop damage of the hit added together, before resistances.</param>
    /// <param name="minDamage">The least such damage that makes a hole.</param>
    /// <param name="inBattle">Whether the ship is in a sea battle.</param>
    /// <param name="isRam">Whether the hit is a ram.</param>
    /// <returns>True if there is a hole.</returns>
    public static bool MakesHole(float rawPhysical, float minDamage, bool inBattle, bool isRam)
    {
        return isRam || (inBattle && rawPhysical >= minDamage);
    }

    /// <summary>
    /// Where a hole goes: on the side of the hull that was hit, just inside it, a little above the waterline, and at
    /// the same place along the hull as the hit (kept clear of the bow and stern).
    /// </summary>
    /// <param name="localHit">The hit point in the ship's local space.</param>
    /// <param name="hullCentre">Centre of the hull in the ship's local space.</param>
    /// <param name="hullSize">Size of the hull: x across, z along.</param>
    /// <param name="waterlineY">Height of the waterline in the ship's local space.</param>
    /// <returns>The hole's point in the ship's local space.</returns>
    public static Vector3 Place(Vector3 localHit, Vector3 hullCentre, Vector3 hullSize, float waterlineY)
    {
        // A hit exactly amidships goes to starboard (positive x).
        float side = localHit.x >= hullCentre.x ? 1f : -1f;
        float halfBeam = Mathf.Abs(hullSize.x) * 0.5f;
        float x = hullCentre.x + side * Mathf.Max(0f, halfBeam - SideInset);

        float halfLength = Mathf.Abs(hullSize.z) * 0.5f;
        float reach = halfLength - EndMargin;
        float z = reach <= 0f
            ? hullCentre.z
            : Mathf.Clamp(localHit.z, hullCentre.z - reach, hullCentre.z + reach);

        return new Vector3(x, waterlineY + AboveWaterline, z);
    }

    /// <summary>
    /// Finds the hole nearest to a point.
    /// </summary>
    /// <param name="points">The holes, in the ship's local space.</param>
    /// <param name="local">The point, in the ship's local space.</param>
    /// <param name="maxDistance">Farthest distance, in metres, that still counts.</param>
    /// <returns>Index of the nearest hole (the first, if two are equally near), or -1 if none is within reach.</returns>
    public static int Nearest(IReadOnlyList<Vector3> points, Vector3 local, float maxDistance)
    {
        if (points == null || maxDistance < 0f)
        {
            return -1;
        }

        int best = -1;
        float limit = maxDistance * maxDistance;
        float bestSquared = float.MaxValue;
        for (int i = 0; i < points.Count; i++)
        {
            float squared = (points[i] - local).sqrMagnitude;
            if (squared <= limit && squared < bestSquared)
            {
                best = i;
                bestSquared = squared;
            }
        }

        return best;
    }

    /// <summary>
    /// The hole a player is looking at: the nearest one along the line of sight that passes within a radius of it.
    /// </summary>
    /// <param name="points">The holes, in world space.</param>
    /// <param name="origin">Where the player looks from.</param>
    /// <param name="direction">The direction they look; need not be a unit vector.</param>
    /// <param name="radius">How far from the line of sight a hole may lie.</param>
    /// <param name="reach">How far along the line of sight a hole may lie.</param>
    /// <returns>Index of the hole, or -1 if none is aimed at.</returns>
    public static int Aimed(IReadOnlyList<Vector3> points, Vector3 origin, Vector3 direction, float radius, float reach)
    {
        if (points == null || direction.sqrMagnitude < 1e-6f)
        {
            return -1;
        }

        Vector3 forward = direction.normalized;
        float limit = radius * radius;
        int best = -1;
        float bestAlong = float.MaxValue;
        for (int i = 0; i < points.Count; i++)
        {
            Vector3 to = points[i] - origin;
            float along = Vector3.Dot(to, forward);
            if (along < 0f || along > reach || along >= bestAlong)
            {
                continue;
            }

            if ((to - forward * along).sqrMagnitude <= limit)
            {
                best = i;
                bestAlong = along;
            }
        }

        return best;
    }

    /// <summary>
    /// The hull's health after a patch mends a share of it. A patch never takes health away.
    /// </summary>
    /// <param name="health">The health now.</param>
    /// <param name="maxHealth">The most the hull can have.</param>
    /// <param name="share">Share of the most that one patch mends.</param>
    /// <returns>The new health, at most <paramref name="maxHealth"/>; the old health if it is already that high.</returns>
    public static float Mend(float health, float maxHealth, float share)
    {
        if (health >= maxHealth)
        {
            return health;
        }

        return Mathf.Min(maxHealth, health + maxHealth * Mathf.Clamp01(share));
    }

    /// <summary>
    /// Whether an index from the network points at a hole in a list of the given size.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <param name="count">Number of holes.</param>
    /// <returns>True if the index is within the list.</returns>
    public static bool ValidIndex(int index, int count)
    {
        return index >= 0 && index < count;
    }
}
