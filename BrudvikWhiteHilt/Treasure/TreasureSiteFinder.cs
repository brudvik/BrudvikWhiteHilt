using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Treasure;

/// <summary>
/// A place a client suggests for a treasure: dry, not too steep, in an allowed biome, on land the buyer has explored
/// well enough to recognise on a map. The server picks the first that also suits it.
/// </summary>
public readonly struct TreasureCandidate
{
    /// <summary>
    /// Creates a candidate.
    /// </summary>
    /// <param name="chest">Where the treasure goes, y = ground height.</param>
    /// <param name="centre">World x and z of the middle of its map.</param>
    /// <param name="nearWater">Whether water lies within the feature distance.</param>
    public TreasureCandidate(Vector3 chest, Vector2 centre, bool nearWater)
    {
        Chest = chest;
        Centre = centre;
        NearWater = nearWater;
    }

    /// <summary>Where the treasure goes.</summary>
    public Vector3 Chest { get; }

    /// <summary>Middle of its map.</summary>
    public Vector2 Centre { get; }

    /// <summary>Whether water lies within the feature distance.</summary>
    public bool NearWater { get; }
}

/// <summary>
/// Picks candidate places for a treasure on the client, which alone knows what its player has explored.
/// </summary>
public static class TreasureSiteFinder
{
    private const int MaxCandidates = 16;
    private const int MaxTries = 4000;
    private const int ShareSamples = 24;
    private const float MinHeightAboveWater = 1.5f;
    private const float SlopeStep = 2f;
    private const float MaxSlopeRise = 1.2f;
    private const float MinRelief = 30f;
    private const float CentreSpread = 0.3f;

    /// <summary>
    /// Finds places for a treasure around a player.
    /// </summary>
    /// <param name="origin">The buyer's position.</param>
    /// <param name="random">Random source.</param>
    /// <returns>Up to 16 candidates, those by water first.</returns>
    public static List<TreasureCandidate> Find(Vector3 origin, System.Random random)
    {
        List<TreasureCandidate> found = new();
        Minimap map = Minimap.instance;
        WorldGenerator world = WorldGenerator.instance;
        if (map == null || world == null || ZoneSystem.instance == null)
        {
            return found;
        }

        float water = ZoneSystem.instance.m_waterLevel;
        float size = TreasureSettings.FragmentSize.Value;
        float min = TreasureSettings.MinDistance.Value;
        float max = Mathf.Max(min + 50f, TreasureSettings.MaxDistance.Value);
        Heightmap.Biome allowed = AllowedBiomes();
        for (int attempt = 0; attempt < MaxTries && found.Count < MaxCandidates; attempt++)
        {
            float angle = (float)(random.NextDouble() * Math.PI * 2.0);
            float distance = Mathf.Sqrt(Mathf.Lerp(min * min, max * max, (float)random.NextDouble()));
            Vector3 point = origin + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;
            if (!map.IsExplored(point) || (world.GetBiome(point.x, point.z) & allowed) == 0)
            {
                continue;
            }

            float height = world.GetHeight(point.x, point.z);
            if (height < water + MinHeightAboveWater || !IsGentle(world, point, height))
            {
                continue;
            }

            Vector2 centre = new(point.x + Spread(random, size), point.z + Spread(random, size));
            if (!IsRecognisable(map, world, centre, size, water))
            {
                continue;
            }

            point.y = height;
            found.Add(new TreasureCandidate(point, centre, IsNearWater(world, point, water)));
        }

        return found.OrderByDescending(candidate => candidate.NearWater).ToList();
    }

    /// <summary>
    /// The biomes from the settings.
    /// </summary>
    /// <returns>The biomes as flags.</returns>
    public static Heightmap.Biome AllowedBiomes()
    {
        Heightmap.Biome biomes = Heightmap.Biome.None;
        foreach (string name in TreasureSettings.AllowedBiomes.Value.Split(','))
        {
            if (Enum.TryParse(name.Trim(), true, out Heightmap.Biome biome))
            {
                biomes |= biome;
            }
        }

        return biomes;
    }

    /// <summary>
    /// Whether water lies within the feature distance of a point.
    /// </summary>
    /// <param name="world">The world generator.</param>
    /// <param name="point">The point.</param>
    /// <param name="water">Water level.</param>
    /// <returns>True near a shore, lake or river.</returns>
    public static bool IsNearWater(WorldGenerator world, Vector3 point, float water)
    {
        float reach = TreasureSettings.FeatureDistance.Value;
        for (int ring = 1; ring <= 4; ring++)
        {
            float radius = reach * ring / 4f;
            for (int step = 0; step < 12; step++)
            {
                float angle = step * Mathf.PI / 6f;
                if (world.GetHeight(point.x + Mathf.Cos(angle) * radius, point.z + Mathf.Sin(angle) * radius) < water)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static float Spread(System.Random random, float size)
    {
        return ((float)random.NextDouble() * 2f - 1f) * CentreSpread * size;
    }

    private static bool IsGentle(WorldGenerator world, Vector3 point, float height)
    {
        return Mathf.Abs(world.GetHeight(point.x + SlopeStep, point.z) - height) < MaxSlopeRise
            && Mathf.Abs(world.GetHeight(point.x - SlopeStep, point.z) - height) < MaxSlopeRise
            && Mathf.Abs(world.GetHeight(point.x, point.z + SlopeStep) - height) < MaxSlopeRise
            && Mathf.Abs(world.GetHeight(point.x, point.z - SlopeStep) - height) < MaxSlopeRise;
    }

    // Enough of the map explored, and something to know it by: a shore or clear hills.
    private static bool IsRecognisable(Minimap map, WorldGenerator world, Vector2 centre, float size, float water)
    {
        int explored = 0;
        int wet = 0;
        float lowest = float.MaxValue;
        float highest = float.MinValue;
        float step = size / ShareSamples;
        float start = -size / 2f + step / 2f;
        int total = ShareSamples * ShareSamples;
        int needed = Mathf.CeilToInt(total * TreasureSettings.MinExploredShare.Value);
        for (int i = 0; i < ShareSamples; i++)
        {
            for (int j = 0; j < ShareSamples; j++)
            {
                float x = centre.x + start + i * step;
                float z = centre.y + start + j * step;
                if (map.IsExplored(new Vector3(x, 0f, z)))
                {
                    explored++;
                }

                float height = world.GetHeight(x, z);
                if (height < water)
                {
                    wet++;
                }

                lowest = Mathf.Min(lowest, height);
                highest = Mathf.Max(highest, height);
            }

            if (explored + (ShareSamples - 1 - i) * ShareSamples < needed)
            {
                return false;
            }
        }

        return explored >= needed && (wet > 0 && wet < total || highest - lowest >= MinRelief);
    }
}
