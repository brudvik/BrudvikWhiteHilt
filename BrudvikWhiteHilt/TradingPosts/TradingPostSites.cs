using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.TradingPosts;

/// <summary>
/// Whether a place on the coast suits a trading post, judged from the height of the land alone so the server can search
/// the whole world without loading it. Positions are in post coordinates (x across, z out to sea) turned by the post's
/// yaw; the numbers match the agreed layout (docs/plans/trading-posts.md).
/// </summary>
public static class TradingPostSites
{
    /// <summary>Lowest the bank under the landing may lie above the sea.</summary>
    public const float MinBank = 0.3f;

    /// <summary>Highest the bank under the landing may lie above the sea, so the jetty can reach down to the water.</summary>
    public const float MaxBank = 2.5f;

    /// <summary>The deck stands at least this high above the sea.</summary>
    public const float MinFreeboard = 1.0f;

    /// <summary>Water at least this deep must lie under the jetty head, for a longship.</summary>
    public const float HeadDepth = 2.5f;

    /// <summary>Water at least this deep must lie six metres either side of the head, where a ship lies alongside.</summary>
    public const float SideDepth = 2.0f;

    /// <summary>The land behind the landing may rise at most this much above the deck.</summary>
    public const float MaxLandAboveDeck = 2.0f;

    /// <summary>The land behind the landing may differ at most this much in height.</summary>
    public const float MaxLandSpread = 2.5f;

    /// <summary>Directions tried at each place.</summary>
    public const int Directions = 16;

    // The jetty head's middle, from the layout's "head".
    private const float HeadZ = 19f;
    private const float SideX = 6f;
    private const float OpenSeaZ = 25f;

    // Where the land behind the landing is sampled: the house, the fire and the cart stand there.
    private static readonly Vector2[] land =
    {
        new(-7f, -8f), new(-3f, -8f), new(2f, -8f), new(7f, -8f), new(11f, -8f), new(-3f, -12f), new(2f, -12f), new(7f, -12f), new(0f, -4f)
    };

    /// <summary>
    /// Judges a post at a place facing one way.
    /// </summary>
    /// <param name="origin">The middle of the landing (x, z).</param>
    /// <param name="yaw">Which way the jetty runs, in degrees from +z.</param>
    /// <param name="height">The land's height at (x, z).</param>
    /// <param name="water">The sea's level.</param>
    /// <param name="deck">The deck's height, when it suits.</param>
    /// <param name="score">How well it suits: deeper water and flatter land are better.</param>
    /// <returns>True if a post fits there.</returns>
    public static bool Evaluate(Vector2 origin, float yaw, Func<float, float, float> height, float water, out float deck, out float score)
    {
        deck = 0f;
        score = 0f;
        float bank = height(origin.x, origin.y);
        if (bank < water + MinBank || bank > water + MaxBank)
        {
            return false;
        }

        deck = Mathf.Max(bank, water + MinFreeboard) + 0.1f;
        float low = float.MaxValue;
        float high = float.MinValue;
        foreach (Vector2 local in land)
        {
            float h = At(origin, yaw, local, height);
            if (h < water + 0.2f || h > deck + MaxLandAboveDeck)
            {
                return false;
            }

            low = Mathf.Min(low, h);
            high = Mathf.Max(high, h);
        }

        if (high - low > MaxLandSpread)
        {
            return false;
        }

        for (float z = 3f; z <= HeadZ; z += 2f)
        {
            if (At(origin, yaw, new Vector2(0f, z), height) > deck - 0.6f)
            {
                return false;
            }
        }

        float head = water - At(origin, yaw, new Vector2(0f, HeadZ), height);
        float openSea = water - At(origin, yaw, new Vector2(0f, OpenSeaZ), height);
        if (head < HeadDepth || openSea < HeadDepth
            || water - At(origin, yaw, new Vector2(-SideX, HeadZ), height) < SideDepth
            || water - At(origin, yaw, new Vector2(SideX, HeadZ), height) < SideDepth)
        {
            return false;
        }

        // Deep water at the head and flat land behind count most; deeper water further out breaks ties, so the jetty
        // runs straight out from the shore rather than along it.
        score = Mathf.Min(head, 5f) - (high - low) + 0.1f * Mathf.Min(openSea, 15f);
        return true;
    }

    /// <summary>
    /// Finds the best way for a post to face at a place, trying <see cref="Directions"/> directions.
    /// </summary>
    /// <param name="origin">The middle of the landing (x, z).</param>
    /// <param name="height">The land's height at (x, z).</param>
    /// <param name="water">The sea's level.</param>
    /// <param name="yaw">The best direction, in degrees.</param>
    /// <param name="deck">The deck's height.</param>
    /// <param name="score">How well it suits.</param>
    /// <returns>True if any direction suits.</returns>
    public static bool TryFind(Vector2 origin, Func<float, float, float> height, float water, out float yaw, out float deck, out float score)
    {
        yaw = 0f;
        deck = 0f;
        score = float.MinValue;
        bool found = false;
        for (int i = 0; i < Directions; i++)
        {
            float candidate = 360f * i / Directions;
            if (Evaluate(origin, candidate, height, water, out float candidateDeck, out float candidateScore) && candidateScore > score)
            {
                found = true;
                yaw = candidate;
                deck = candidateDeck;
                score = candidateScore;
            }
        }

        return found;
    }

    /// <summary>
    /// Picks posts for one biome out of its suitable places: in a shuffled order, each at least
    /// <paramref name="minDistance"/> from every post already standing or picked.
    /// </summary>
    /// <param name="candidates">The suitable places.</param>
    /// <param name="taken">Posts already standing or picked for other biomes; the picks are added to it.</param>
    /// <param name="count">How many to pick.</param>
    /// <param name="minDistance">Least distance between two posts, in metres.</param>
    /// <param name="random">The world's seeded random numbers, so a world gets the same posts every time.</param>
    /// <returns>The picks.</returns>
    public static List<Vector2> Pick(IEnumerable<Vector2> candidates, List<Vector2> taken, int count, float minDistance, System.Random random)
    {
        List<Vector2> picks = new();
        foreach (Vector2 place in candidates.OrderBy(_ => random.Next()))
        {
            if (picks.Count >= count)
            {
                break;
            }

            if (taken.All(other => Vector2.Distance(other, place) >= minDistance))
            {
                picks.Add(place);
                taken.Add(place);
            }
        }

        return picks;
    }

    /// <summary>
    /// Turns a point from post coordinates into the world.
    /// </summary>
    /// <param name="origin">The middle of the landing (x, z).</param>
    /// <param name="yaw">Which way the jetty runs, in degrees from +z.</param>
    /// <param name="local">The point in post coordinates (x across, y out to sea).</param>
    /// <returns>The point in the world (x, z).</returns>
    public static Vector2 ToWorld(Vector2 origin, float yaw, Vector2 local)
    {
        float radians = yaw * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
        return new Vector2(origin.x + local.x * cos + local.y * sin, origin.y - local.x * sin + local.y * cos);
    }

    private static float At(Vector2 origin, float yaw, Vector2 local, Func<float, float, float> height)
    {
        Vector2 world = ToWorld(origin, yaw, local);
        return height(world.x, world.y);
    }
}
