using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;

namespace BrudvikWhiteHilt.Treasure;

/// <summary>
/// Draws a treasure map from the world generator, like a hand-drawn chart on old parchment: water as an ink wash with
/// thick shorelines, height lines and shading for the hills, little trees where the forests are, a cross on the
/// treasure, a north arrow and a scale. Faded patches and torn edges make it incomplete on purpose. The same site
/// always gives the same picture, so a map can be passed on.
/// </summary>
public static class TreasureMapRenderer
{
    /// <summary>Width and height of the picture in pixels.</summary>
    public const int Resolution = 256;

    private const float ContourStep = 10f;
    private const float SnowLine = 90f;
    private const int TreeCell = 8;
    private const int CrossClearance = 22;
    private const long FrameBudgetTicks = TimeSpan.TicksPerMillisecond * 6;

    private static readonly Color paper = new(0.86f, 0.77f, 0.58f);
    private static readonly Color paperDark = new(0.72f, 0.6f, 0.4f);
    private static readonly Color paperFaded = new(0.83f, 0.75f, 0.6f);
    private static readonly Color ink = new(0.25f, 0.18f, 0.11f);
    private static readonly Color waterInk = new(0.42f, 0.5f, 0.5f);
    private static readonly Color redInk = new(0.6f, 0.11f, 0.07f);
    private static readonly Color burnt = new(0.3f, 0.2f, 0.12f);

    /// <summary>
    /// What a map shows besides the land.
    /// </summary>
    public sealed class Options
    {
        /// <summary>Share of the map that is faded away.</summary>
        public float MissingShare { get; set; }

        /// <summary>World x and z a dotted path to the cross starts from, or null for none.</summary>
        public Vector2? PathFrom { get; set; }
    }

    /// <summary>
    /// The map position of a world point, 0..1 from the bottom left, with the map's turn.
    /// </summary>
    /// <param name="site">The site.</param>
    /// <param name="world">World x and z.</param>
    /// <returns>The position on the map.</returns>
    public static Vector2 WorldToMap(TreasureSite site, Vector2 world)
    {
        Vector2 offset = (world - site.Centre) / site.Size;
        return Turn(offset, 4 - site.Rotation) + new Vector2(0.5f, 0.5f);
    }

    /// <summary>
    /// The direction of world north on the map.
    /// </summary>
    /// <param name="site">The site.</param>
    /// <returns>A unit vector in map space.</returns>
    public static Vector2 North(TreasureSite site)
    {
        return Turn(Vector2.up, 4 - site.Rotation);
    }

    /// <summary>
    /// The length of the scale bar in metres.
    /// </summary>
    /// <param name="site">The site.</param>
    /// <returns>100 or 200 metres.</returns>
    public static int ScaleMetres(TreasureSite site)
    {
        return site.Size <= 600f ? 100 : 200;
    }

    /// <summary>
    /// Draws the map over several frames.
    /// </summary>
    /// <param name="site">The site.</param>
    /// <param name="options">What to show.</param>
    /// <param name="done">Gets the finished texture.</param>
    /// <returns>The coroutine.</returns>
    public static IEnumerator Render(TreasureSite site, Options options, Action<Texture2D> done)
    {
        WorldGenerator world = WorldGenerator.instance;
        if (world == null || ZoneSystem.instance == null)
        {
            yield break;
        }

        const int n = Resolution;
        float water = ZoneSystem.instance.m_waterLevel;
        float pixel = site.Size / n;
        float[] heights = new float[n * n];
        Heightmap.Biome[] biomes = new Heightmap.Biome[(n / 4) * (n / 4)];
        Stopwatch clock = Stopwatch.StartNew();
        for (int j = 0; j < n; j++)
        {
            for (int i = 0; i < n; i++)
            {
                Vector2 point = MapToWorld(site, (i + 0.5f) / n, (j + 0.5f) / n);
                heights[j * n + i] = world.GetHeight(point.x, point.y);
                if ((i & 3) == 0 && (j & 3) == 0)
                {
                    biomes[(j / 4) * (n / 4) + i / 4] = world.GetBiome(point.x, point.y);
                }
            }

            if (clock.ElapsedTicks > FrameBudgetTicks)
            {
                yield return null;
                clock.Restart();
            }
        }

        int seed = site.Seed;
        Color[] colours = new Color[n * n];
        for (int j = 0; j < n; j++)
        {
            for (int i = 0; i < n; i++)
            {
                int index = j * n + i;
                float h = heights[index];
                float stain = Smooth(0.55f, 0.85f, Fbm(i / 40f, j / 40f, seed, 3)) * 0.6f;
                Color colour = Color.Lerp(paper, paperDark, stain) * (0.97f + 0.06f * Value(i / 2.5f, j / 2.5f, seed + 1));
                if (h < water)
                {
                    float depth = Mathf.Clamp01((water - h) / 8f);
                    colour = Color.Lerp(colour, waterInk, 0.35f + 0.35f * depth);
                    if ((j + (int)(Value(i / 9f, j / 9f, seed + 2) * 3f)) % 7 == 0 && Value(i / 11f, j / 5f, seed + 3) > 0.45f)
                    {
                        colour = Color.Lerp(colour, ink, 0.25f);
                    }
                }
                else
                {
                    colour *= Shade(heights, i, j, pixel);
                    colour = Tint(colour, biomes[(j / 4) * (n / 4) + i / 4]);
                    if (h > water + SnowLine)
                    {
                        colour = Color.Lerp(colour, Color.white, 0.25f);
                    }
                }

                colours[index] = colour;
            }
        }

        DrawContoursAndShores(colours, heights, water);
        yield return null;
        DrawTrees(colours, heights, biomes, site, water, pixel);
        Fade(colours, site, options.MissingShare);
        if (options.PathFrom.HasValue)
        {
            DrawPath(colours, ToPixel(site, options.PathFrom.Value), ToPixel(site, site.Chest));
        }

        DrawCross(colours, ToPixel(site, site.Chest));
        DrawNorthArrow(colours, North(site));
        DrawScaleBar(colours, ScaleMetres(site) / site.Size * n);
        TearEdges(colours, seed);

        Texture2D texture = new(n, n, TextureFormat.RGBA32, false) { name = "whitehilt_treasure_map", wrapMode = TextureWrapMode.Clamp };
        texture.SetPixels(colours);
        texture.Apply(false, false);
        done(texture);
    }

    /// <summary>
    /// The pixel of a world point on the map.
    /// </summary>
    /// <param name="site">The site.</param>
    /// <param name="world">World x and z.</param>
    /// <returns>Pixel coordinates from the bottom left.</returns>
    public static Vector2 ToPixel(TreasureSite site, Vector2 world)
    {
        return WorldToMap(site, world) * Resolution;
    }

    /// <summary>
    /// The world point at a map position, the inverse of <see cref="WorldToMap"/>.
    /// </summary>
    /// <param name="site">The site.</param>
    /// <param name="u">Map x, 0..1 from the left.</param>
    /// <param name="v">Map y, 0..1 from the bottom.</param>
    /// <returns>World x and z.</returns>
    internal static Vector2 MapToWorld(TreasureSite site, float u, float v)
    {
        return site.Centre + Turn(new Vector2(u - 0.5f, v - 0.5f), site.Rotation) * site.Size;
    }

    // Quarter turns anticlockwise.
    private static Vector2 Turn(Vector2 value, int quarters)
    {
        switch (quarters & 3)
        {
            case 1: return new Vector2(-value.y, value.x);
            case 2: return new Vector2(-value.x, -value.y);
            case 3: return new Vector2(value.y, -value.x);
            default: return value;
        }
    }

    // Light from the top left of the map, like on old charts; flat land stays as it is.
    private static float Shade(float[] heights, int i, int j, float pixel)
    {
        const int n = Resolution;
        float left = heights[j * n + Mathf.Max(i - 1, 0)];
        float right = heights[j * n + Mathf.Min(i + 1, n - 1)];
        float down = heights[Mathf.Max(j - 1, 0) * n + i];
        float up = heights[Mathf.Min(j + 1, n - 1) * n + i];
        Vector3 normal = new Vector3(-(right - left) / (2f * pixel), 1f, -(up - down) / (2f * pixel)).normalized;
        Vector3 light = new Vector3(-1f, 1.4f, 1f).normalized;
        float shade = Vector3.Dot(normal, light) / Vector3.Dot(Vector3.up, light);
        return Mathf.Clamp(0.75f + 0.25f * shade, 0.6f, 1.1f);
    }

    private static Color Tint(Color colour, Heightmap.Biome biome)
    {
        Color tint = biome switch
        {
            Heightmap.Biome.Meadows => new Color(0.95f, 1f, 0.85f),
            Heightmap.Biome.BlackForest => new Color(0.85f, 0.92f, 0.82f),
            Heightmap.Biome.Swamp => new Color(0.9f, 0.88f, 0.75f),
            Heightmap.Biome.Mountain => new Color(1.05f, 1.05f, 1.08f),
            Heightmap.Biome.Plains => new Color(1.05f, 1f, 0.8f),
            Heightmap.Biome.Mistlands => new Color(0.92f, 0.88f, 0.95f),
            Heightmap.Biome.AshLands => new Color(1f, 0.8f, 0.75f),
            Heightmap.Biome.DeepNorth => new Color(1.05f, 1.05f, 1.1f),
            _ => Color.white
        };
        return Color.Lerp(colour, colour * tint, 0.6f);
    }

    // Draws the shore lines and height contours in ink over the coloured map, every fifth contour darker.
    private static void DrawContoursAndShores(Color[] colours, float[] heights, float water)
    {
        const int n = Resolution;
        Color[] source = (Color[])colours.Clone();
        for (int j = 1; j < n - 1; j++)
        {
            for (int i = 1; i < n - 1; i++)
            {
                int index = j * n + i;
                float h = heights[index];
                bool land = h >= water;
                int wetNear = 0;
                int wetFar = 0;
                for (int dy = -2; dy <= 2; dy++)
                {
                    for (int dx = -2; dx <= 2; dx++)
                    {
                        int x = Mathf.Clamp(i + dx, 0, n - 1);
                        int y = Mathf.Clamp(j + dy, 0, n - 1);
                        if (heights[y * n + x] >= water == land)
                        {
                            continue;
                        }

                        if (Mathf.Abs(dx) + Mathf.Abs(dy) <= 1)
                        {
                            wetNear++;
                        }
                        else
                        {
                            wetFar++;
                        }
                    }
                }

                Color colour = source[index];
                if (land && wetNear > 0)
                {
                    colour = Color.Lerp(colour, ink, 0.9f);
                }
                else if (wetNear > 0)
                {
                    colour = Color.Lerp(colour, ink, 0.3f);
                }
                else if (land && wetFar > 0)
                {
                    colour = Color.Lerp(colour, ink, 0.3f);
                }
                else if (land)
                {
                    int level = Mathf.FloorToInt((h - water) / ContourStep);
                    int right = Mathf.FloorToInt((heights[index + 1] - water) / ContourStep);
                    int up = Mathf.FloorToInt((heights[index + n] - water) / ContourStep);
                    if (level != right || level != up)
                    {
                        colour = Color.Lerp(colour, ink, Mathf.Max(level, Mathf.Max(right, up)) % 5 == 0 ? 0.45f : 0.22f);
                    }
                }

                colours[index] = colour;
            }
        }
    }

    // Little trees where the world generator grows forest, a few per cell, conifers in the dark forests.
    private static void DrawTrees(Color[] colours, float[] heights, Heightmap.Biome[] biomes, TreasureSite site, float water, float pixel)
    {
        const int n = Resolution;
        for (int cj = 0; cj < n / TreeCell; cj++)
        {
            for (int ci = 0; ci < n / TreeCell; ci++)
            {
                int x = ci * TreeCell + 2 + (int)(Hash(ci, cj, site.Seed + 5) * (TreeCell - 4));
                int y = cj * TreeCell + 2 + (int)(Hash(ci, cj, site.Seed + 6) * (TreeCell - 4));
                float h = heights[y * n + x];
                if (h < water + 1f || Shade(heights, x, y, pixel) < 0.7f)
                {
                    continue;
                }

                Heightmap.Biome biome = biomes[(y / 4) * (n / 4) + x / 4];
                Vector2 point = MapToWorld(site, (x + 0.5f) / n, (y + 0.5f) / n);
                float forest = WorldGenerator.GetForestFactor(new Vector3(point.x, 0f, point.y));
                float density = biome switch
                {
                    Heightmap.Biome.BlackForest => forest < 1.15f ? 0.9f : 0.35f,
                    Heightmap.Biome.Meadows => forest < 1.15f ? 0.7f : 0.04f,
                    Heightmap.Biome.Swamp => 0.3f,
                    Heightmap.Biome.Plains => forest < 0.8f ? 0.25f : 0.03f,
                    Heightmap.Biome.Mistlands => 0.5f,
                    Heightmap.Biome.Mountain => h < water + SnowLine - 30f ? 0.3f : 0f,
                    _ => 0f
                };
                if (Hash(ci, cj, site.Seed + 7) >= density)
                {
                    continue;
                }

                bool conifer = biome == Heightmap.Biome.BlackForest || biome == Heightmap.Biome.Mountain || biome == Heightmap.Biome.Swamp;
                if (conifer)
                {
                    for (int dy = 0; dy <= 5; dy++)
                    {
                        int half = (5 - dy) / 2;
                        for (int dx = -half; dx <= half; dx++)
                        {
                            Blend(colours, x + dx, y + dy, ink, 0.6f);
                        }
                    }
                }
                else
                {
                    for (int dy = -2; dy <= 2; dy++)
                    {
                        for (int dx = -2; dx <= 2; dx++)
                        {
                            if (dx * dx + dy * dy <= 5)
                            {
                                Blend(colours, x + dx, y + 3 + dy, ink, 0.45f);
                            }
                        }
                    }
                }

                Blend(colours, x, y - 1, ink, 0.6f);
            }
        }
    }

    // Washed-out patches where the ink has gone, never over the cross.
    private static void Fade(Color[] colours, TreasureSite site, float share)
    {
        if (share <= 0f)
        {
            return;
        }

        const int n = Resolution;
        float[] noise = new float[n * n];
        for (int j = 0; j < n; j++)
        {
            for (int i = 0; i < n; i++)
            {
                noise[j * n + i] = Fbm(i / 48f, j / 48f, site.Seed + 9, 4);
            }
        }

        float[] sorted = (float[])noise.Clone();
        Array.Sort(sorted);
        float threshold = sorted[Mathf.Clamp((int)(share * sorted.Length), 0, sorted.Length - 1)];
        Vector2 cross = ToPixel(site, site.Chest);
        for (int j = 0; j < n; j++)
        {
            for (int i = 0; i < n; i++)
            {
                int index = j * n + i;
                float fade = Smooth(threshold, threshold - 0.06f, noise[index]);
                fade *= Smooth(CrossClearance, CrossClearance + 10f, Vector2.Distance(cross, new Vector2(i, j)));
                if (fade > 0f)
                {
                    colours[index] = Color.Lerp(colours[index], paperFaded * (0.95f + 0.1f * Value(i / 3f, j / 3f, site.Seed + 10)), fade * 0.92f);
                }
            }
        }
    }

    // Draws a dashed red path between two points.
    private static void DrawPath(Color[] colours, Vector2 from, Vector2 to)
    {
        Vector2 line = to - from;
        float length = line.magnitude - 10f;
        if (length <= 0f)
        {
            return;
        }

        Vector2 direction = line.normalized;
        for (float t = 4f; t < length; t += 7f)
        {
            for (float s = 0f; s < 3f; s += 0.5f)
            {
                Vector2 point = from + direction * (t + s);
                Disc(colours, point, 1.1f, redInk, 0.8f);
            }
        }
    }

    private static void DrawCross(Color[] colours, Vector2 centre)
    {
        for (float t = -6f; t <= 6f; t += 0.5f)
        {
            Disc(colours, centre + new Vector2(t, t), 1.4f, redInk, 0.95f);
            Disc(colours, centre + new Vector2(t, -t), 1.4f, redInk, 0.95f);
        }
    }

    // Draws a small compass with an arrow pointing north.
    private static void DrawNorthArrow(Color[] colours, Vector2 north)
    {
        Vector2 centre = new(Resolution - 24f, Resolution - 26f);
        for (float angle = 0f; angle < 360f; angle += 4f)
        {
            float radians = angle * Mathf.Deg2Rad;
            Disc(colours, centre + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * 10f, 0.6f, ink, 0.7f);
        }

        for (float t = -8f; t <= 8f; t += 0.5f)
        {
            Disc(colours, centre + north * t, 0.8f, ink, 0.9f);
        }

        Vector2 side = new(-north.y, north.x);
        for (float t = 0f; t <= 4f; t += 0.5f)
        {
            Vector2 back = centre + north * (8f - t);
            Disc(colours, back + side * t * 0.8f, 0.8f, ink, 0.9f);
            Disc(colours, back - side * t * 0.8f, 0.8f, ink, 0.9f);
        }
    }

    private static void DrawScaleBar(Color[] colours, float length)
    {
        Vector2 start = new(16f, 16f);
        for (float t = 0f; t <= length; t += 0.5f)
        {
            Disc(colours, start + new Vector2(t, 0f), 0.8f, ink, 0.9f);
        }

        foreach (float x in new[] { 0f, length / 2f, length })
        {
            for (float t = -2.5f; t <= 2.5f; t += 0.5f)
            {
                Disc(colours, start + new Vector2(x, t), 0.6f, ink, 0.9f);
            }
        }
    }

    // Ragged, singed edges; outside them the picture is see-through.
    private static void TearEdges(Color[] colours, int seed)
    {
        const int n = Resolution;
        for (int j = 0; j < n; j++)
        {
            for (int i = 0; i < n; i++)
            {
                int index = j * n + i;
                float edge = Mathf.Min(Mathf.Min(i, j), Mathf.Min(n - 1 - i, n - 1 - j));
                float tear = edge - 2f - Fbm(i / 6f, j / 6f, seed + 11, 3) * 9f;
                Color colour = Color.Lerp(burnt, colours[index], Mathf.Clamp01(tear / 4f));
                colour.a = Mathf.Clamp01(tear / 1.5f);
                colours[index] = colour;
            }
        }
    }

    private static void Disc(Color[] colours, Vector2 centre, float radius, Color colour, float alpha)
    {
        int r = Mathf.CeilToInt(radius);
        int cx = Mathf.RoundToInt(centre.x);
        int cy = Mathf.RoundToInt(centre.y);
        for (int y = cy - r; y <= cy + r; y++)
        {
            for (int x = cx - r; x <= cx + r; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), centre);
                if (distance <= radius)
                {
                    Blend(colours, x, y, colour, alpha);
                }
            }
        }
    }

    private static void Blend(Color[] colours, int x, int y, Color colour, float alpha)
    {
        if (x < 0 || y < 0 || x >= Resolution || y >= Resolution)
        {
            return;
        }

        int index = y * Resolution + x;
        colours[index] = Color.Lerp(colours[index], colour, alpha);
    }

    private static float Smooth(float from, float to, float value)
    {
        float t = Mathf.Clamp01((value - from) / (to - from));
        return t * t * (3f - 2f * t);
    }

    private static float Hash(int x, int y, int seed)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0xFFFFFF) / (float)0x1000000;
        }
    }

    private static float Value(float x, float y, int seed)
    {
        int x0 = Mathf.FloorToInt(x);
        int y0 = Mathf.FloorToInt(y);
        float fx = x - x0;
        float fy = y - y0;
        fx = fx * fx * (3f - 2f * fx);
        fy = fy * fy * (3f - 2f * fy);
        float bottom = Mathf.Lerp(Hash(x0, y0, seed), Hash(x0 + 1, y0, seed), fx);
        float top = Mathf.Lerp(Hash(x0, y0 + 1, seed), Hash(x0 + 1, y0 + 1, seed), fx);
        return Mathf.Lerp(bottom, top, fy);
    }

    private static float Fbm(float x, float y, int seed, int octaves)
    {
        float value = 0f;
        float weight = 1f;
        float total = 0f;
        for (int octave = 0; octave < octaves; octave++)
        {
            value += Value(x, y, seed + octave * 31) * weight;
            total += weight;
            weight *= 0.5f;
            x *= 2f;
            y *= 2f;
        }

        return value / total;
    }
}
