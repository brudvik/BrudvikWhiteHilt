using System.Collections;
using System.Diagnostics;
using UnityEngine;

namespace BrudvikWhiteHilt.Navigation.Overview;

/// <summary>
/// How much of each biome the local player has uncovered. The map is sampled every few pixels; the biome of every
/// sample is worked out once per world, a few milliseconds each frame, and the uncovered samples are counted from the
/// map's explored data whenever asked. Map shared by others counts once it is drawn as the player's own.
/// </summary>
public static class ExplorationOverview
{
    /// <summary>The biomes in the order they are listed.</summary>
    public static readonly Heightmap.Biome[] Biomes =
    {
        Heightmap.Biome.Meadows, Heightmap.Biome.BlackForest, Heightmap.Biome.Swamp, Heightmap.Biome.Mountain,
        Heightmap.Biome.Plains, Heightmap.Biome.Mistlands, Heightmap.Biome.AshLands, Heightmap.Biome.DeepNorth, Heightmap.Biome.Ocean
    };

    // Map pixels between samples, each way, and how long each frame may spend on the biomes.
    private const int Step = 4;
    private const double BudgetMilliseconds = 4.0;
    private const byte Outside = byte.MaxValue;

    // Beyond this radius is the edge of the world.
    private const float WorldRadius = 10000f;

    private static readonly Stopwatch watch = new();

    private static byte[] biomeOfSample;
    private static int[] totals;
    private static int samplesPerSide;
    private static int cursor;
    private static long world;

    /// <summary>True once the biome of every sample is known.</summary>
    public static bool Ready => biomeOfSample != null && cursor >= biomeOfSample.Length;

    /// <summary>Share of the biomes worked out so far, 0 to 1.</summary>
    public static float Progress => biomeOfSample == null ? 0f : (float)cursor / biomeOfSample.Length;

    /// <summary>
    /// Works out the biomes of some more samples. Call every frame while the overview is wanted.
    /// </summary>
    /// <param name="map">The map.</param>
    public static void Build(Minimap map)
    {
        if (WorldGenerator.instance == null || ZNet.World == null)
        {
            return;
        }

        long current = ZNet.World.m_uid;
        if (biomeOfSample == null || current != world)
        {
            world = current;
            samplesPerSide = map.m_textureSize / Step;
            biomeOfSample = new byte[samplesPerSide * samplesPerSide];
            totals = new int[Biomes.Length];
            cursor = 0;
        }

        if (Ready)
        {
            return;
        }

        watch.Restart();
        while (cursor < biomeOfSample.Length && watch.Elapsed.TotalMilliseconds < BudgetMilliseconds)
        {
            Vector3 point = SampleWorld(map, cursor);
            byte index = Outside;
            if (Utils.LengthXZ(point) <= WorldRadius)
            {
                index = (byte)System.Array.IndexOf(Biomes, WorldGenerator.instance.GetBiome(point.x, point.z));
                if (index != Outside)
                {
                    totals[index]++;
                }
            }

            biomeOfSample[cursor++] = index;
        }
    }

    /// <summary>
    /// Counts the uncovered samples of each biome.
    /// </summary>
    /// <param name="map">The map.</param>
    /// <param name="explored">Uncovered samples per biome, in the order of <see cref="Biomes"/>.</param>
    /// <param name="all">All samples per biome.</param>
    /// <returns>False until <see cref="Ready"/>.</returns>
    public static bool Count(Minimap map, out int[] explored, out int[] all)
    {
        explored = new int[Biomes.Length];
        all = totals;
        if (!Ready || map.m_explored == null)
        {
            return false;
        }

        BitArray own = map.m_explored;
        BitArray others = SharedMapReveal.Revealed ? map.m_exploredOthers : null;
        int size = map.m_textureSize;
        for (int i = 0; i < biomeOfSample.Length; i++)
        {
            byte biome = biomeOfSample[i];
            if (biome == Outside)
            {
                continue;
            }

            int pixel = Pixel(i, size);
            if (own[pixel] || (others != null && others[pixel]))
            {
                explored[biome]++;
            }
        }

        return true;
    }

    /// <summary>
    /// Square kilometres one sample stands for.
    /// </summary>
    /// <param name="map">The map.</param>
    /// <returns>The area.</returns>
    public static float SampleArea(Minimap map)
    {
        float side = Step * map.m_pixelSize;
        return side * side / 1_000_000f;
    }

    private static int Pixel(int sample, int size)
    {
        int x = sample % samplesPerSide * Step + Step / 2;
        int y = sample / samplesPerSide * Step + Step / 2;
        return y * size + x;
    }

    private static Vector3 SampleWorld(Minimap map, int sample)
    {
        int half = map.m_textureSize / 2;
        int x = sample % samplesPerSide * Step + Step / 2;
        int y = sample / samplesPerSide * Step + Step / 2;
        return new Vector3((x - half) * map.m_pixelSize, 0f, (y - half) * map.m_pixelSize);
    }
}
