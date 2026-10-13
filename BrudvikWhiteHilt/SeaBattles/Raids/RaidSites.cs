using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.SeaBattles.Raids;

/// <summary>
/// The plain geometry of a draugr harbour: where the guards stand round the hoard, where the moored Nail Ship lies in
/// deep water off the shore, and which coast points an admin's raid may use. Every method takes the ground height as a
/// delegate and returns world x and z as a <see cref="Vector2"/>, so the server that builds the site and the tests use
/// the same code.
/// </summary>
public static class RaidSites
{
    /// <summary>Water at least this deep must lie under the moored ship.</summary>
    public const float ShipDepth = 6f;

    /// <summary>No land within this many metres of the moored ship, so the hull is not wedged against the shore.</summary>
    public const float ShipClearance = 15f;

    /// <summary>The hoard stands at least this high above the sea.</summary>
    public const float MinLandHeight = 1.5f;

    private const int Bearings = 16;
    private const float SearchStep = 10f;
    private const float WetMargin = 0.5f;
    private const float GuardPull = 0.75f;
    private const float MinGuardRadius = 1f;
    private const float CoastReach = 40f;
    private const int CoastBearings = 8;
    private const float CoastMaxHeight = 8f;

    /// <summary>
    /// Posts for the shore guards, spread evenly round the hoard. A post that falls in the water is pulled in towards the
    /// hoard until it is dry; the hoard's own place is the last resort.
    /// </summary>
    /// <param name="centre">World x and z of the hoard.</param>
    /// <param name="radius">Metres from the hoard the guards stand.</param>
    /// <param name="count">How many guards.</param>
    /// <param name="phase">Where on the ring the first guard stands, in degrees.</param>
    /// <param name="height">The ground height at a world x and z.</param>
    /// <param name="waterLevel">The height of the sea.</param>
    /// <returns>One post per guard, in order; empty for a count of nothing.</returns>
    public static List<Vector2> GuardPosts(Vector2 centre, float radius, int count, float phase, Func<float, float, float> height, float waterLevel)
    {
        List<Vector2> posts = new();
        for (int i = 0; i < count; i++)
        {
            float angle = (phase + i * 360f / count) * Mathf.Deg2Rad;
            Vector2 direction = new(Mathf.Sin(angle), Mathf.Cos(angle));
            float reach = radius;
            Vector2 post = centre + direction * reach;
            while (height(post.x, post.y) < waterLevel + WetMargin && reach > MinGuardRadius)
            {
                reach *= GuardPull;
                post = centre + direction * reach;
            }

            posts.Add(reach > MinGuardRadius ? post : centre);
        }

        return posts;
    }

    /// <summary>
    /// Looks for a place off the shore for the moored Nail Ship: the nearest ring round the hoard, up to the search
    /// radius, with water deeper than <see cref="ShipDepth"/> and no land within <see cref="ShipClearance"/>. Where more
    /// than one place on that ring will do, the deepest wins.
    /// </summary>
    /// <param name="hoard">World x and z of the hoard.</param>
    /// <param name="searchRadius">How far from the hoard to look, in metres.</param>
    /// <param name="height">The ground height at a world x and z.</param>
    /// <param name="waterLevel">The height of the sea.</param>
    /// <param name="point">The place found.</param>
    /// <returns>False when there is no such water within the radius.</returns>
    public static bool TryFindShipPoint(Vector2 hoard, float searchRadius, Func<float, float, float> height, float waterLevel, out Vector2 point)
    {
        for (float ring = SearchStep; ring <= searchRadius + 0.01f; ring += SearchStep)
        {
            bool found = false;
            float deepest = float.MaxValue;
            point = hoard;
            for (int i = 0; i < Bearings; i++)
            {
                float angle = i * 360f / Bearings * Mathf.Deg2Rad;
                Vector2 candidate = hoard + new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * ring;
                float ground = height(candidate.x, candidate.y);
                if (ground > waterLevel - ShipDepth || ground >= deepest
                    || !EncounterRules.ShoreIsClear(candidate.x, candidate.y, ShipClearance, height, waterLevel))
                {
                    continue;
                }

                deepest = ground;
                point = candidate;
                found = true;
            }

            if (found)
            {
                return true;
            }
        }

        point = hoard;
        return false;
    }

    /// <summary>
    /// The heading, in degrees from north, of a moored ship that lies along the shore: at right angles to the line from
    /// the hoard to the ship.
    /// </summary>
    /// <param name="hoard">World x and z of the hoard.</param>
    /// <param name="ship">World x and z of the ship.</param>
    /// <returns>A heading from 0 up to but not including 360.</returns>
    public static float ShipHeading(Vector2 hoard, Vector2 ship)
    {
        Vector2 away = ship - hoard;
        float bearing = away.sqrMagnitude < 0.0001f ? 0f : Mathf.Atan2(away.x, away.y) * Mathf.Rad2Deg;
        return Mathf.Repeat(bearing + 90f, 360f);
    }

    /// <summary>
    /// Low coast points round an origin for a raid an admin calls up: dry land a little above the sea with water within
    /// reach. The server's other checks decide which of them is used. The nearest come first.
    /// </summary>
    /// <param name="origin">World x and z to search round.</param>
    /// <param name="minDistance">Nearest distance, in metres.</param>
    /// <param name="maxDistance">Farthest distance, in metres.</param>
    /// <param name="step">Metres between two rings.</param>
    /// <param name="startAngle">Where the first bearing of each ring lies, in degrees.</param>
    /// <param name="height">The ground height at a world x and z.</param>
    /// <param name="waterLevel">The height of the sea.</param>
    /// <returns>The points, nearest ring first.</returns>
    public static List<Vector2> CoastPoints(Vector2 origin, float minDistance, float maxDistance, float step, float startAngle,
        Func<float, float, float> height, float waterLevel)
    {
        List<Vector2> points = new();
        step = Mathf.Max(1f, step);
        for (float ring = Mathf.Max(0f, minDistance); ring <= maxDistance + 0.01f; ring += step)
        {
            for (int i = 0; i < Bearings; i++)
            {
                float angle = (startAngle + i * 360f / Bearings) * Mathf.Deg2Rad;
                Vector2 point = origin + new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * ring;
                float ground = height(point.x, point.y);
                if (ground >= waterLevel + MinLandHeight && ground <= waterLevel + CoastMaxHeight && WaterWithinReach(point, height, waterLevel))
                {
                    points.Add(point);
                }
            }
        }

        return points;
    }

    private static bool WaterWithinReach(Vector2 point, Func<float, float, float> height, float waterLevel)
    {
        for (int i = 0; i < CoastBearings; i++)
        {
            float angle = i * 360f / CoastBearings * Mathf.Deg2Rad;
            if (height(point.x + Mathf.Sin(angle) * CoastReach, point.y + Mathf.Cos(angle) * CoastReach) < waterLevel)
            {
                return true;
            }
        }

        return false;
    }
}
