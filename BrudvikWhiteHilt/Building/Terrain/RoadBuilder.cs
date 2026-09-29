using BepInEx;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BrudvikWhiteHilt.Building.Terrain;

/// <summary>
/// Roads planned on the map with the White Hilt hoe. Only terrain near the player is loaded, so a road is built piece by
/// piece whenever the player is within reach of an unbuilt stretch, paying stone as it goes. The road's height is worked
/// out from the world's original ground, smoothed to take out bumps, or as an even slope between the clicked points.
/// Water is left alone. One road is built at a time; it is saved with the world and carries on after a restart.
/// </summary>
public static class RoadBuilder
{
    private const float SampleStep = 1f;
    private const int ChunkSamples = 8;
    private const int SmoothWindow = 6;
    private const float BuildReach = 40f;
    private const float TickInterval = 0.5f;
    private const float PinSpacing = 20f;
    private const float RoadSkirt = 1.5f;
    private const float WaterMargin = 0.3f;
    private const int Version = 1;

    private static readonly List<Vector3> planned = new();
    private static readonly List<Minimap.PinData> pins = new();

    private static Road road;
    private static string loadedWorld;
    private static float nextTick;

    /// <summary>True while road points are being clicked on the map.</summary>
    public static bool Planning { get; private set; }

    /// <summary>Help while planning, for the key hint.</summary>
    public static string HintLine => Planning
        ? string.Format(Localization.instance.Localize("$whitehilt_terrain_hint_road"), planned.Count, Mathf.RoundToInt(Length(planned)))
        : string.Empty;

    /// <summary>True if a road is being built.</summary>
    public static bool HasRoad => road != null;

    /// <summary>True if the road is paused.</summary>
    public static bool Paused => road != null && road.Paused;

    /// <summary>
    /// Status of the road, e.g. "Road 45% being built as you walk along it", or empty.
    /// </summary>
    /// <returns>The text.</returns>
    public static string Status()
    {
        if (road == null)
        {
            return string.Empty;
        }

        int built = 0;
        foreach (bool done in road.Built)
        {
            built += done ? 1 : 0;
        }

        string state = road.Paused
            ? Localization.instance.Localize("$whitehilt_terrain_road_paused") + (string.IsNullOrEmpty(road.Blocker) ? string.Empty : ": " + road.Blocker)
            : Localization.instance.Localize("$whitehilt_terrain_road_building");
        if (road.HitWater)
        {
            state += ", " + Localization.instance.Localize("$whitehilt_terrain_road_water");
        }

        return string.Format(Localization.instance.Localize("$whitehilt_terrain_road_status"), road.Built.Length == 0 ? 100 : built * 100 / road.Built.Length, state);
    }

    /// <summary>
    /// Opens the map for clicking road points.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void StartPlanning(Player player)
    {
        if (Minimap.instance == null || Game.m_noMap)
        {
            return;
        }

        ClearPlanPins();
        planned.Clear();
        Planning = true;
        Minimap.instance.SetMapMode(Minimap.MapMode.Large);
    }

    /// <summary>
    /// Adds a road point where the map was clicked.
    /// </summary>
    /// <param name="world">The clicked world point.</param>
    public static void OnMapClick(Vector3 world)
    {
        if (planned.Count > 0 && Vector2.Distance(Flat(planned[planned.Count - 1]), Flat(world)) < 2f)
        {
            return;
        }

        planned.Add(new Vector3(world.x, 0f, world.z));
        pins.Add(Minimap.instance.AddPin(world, Minimap.PinType.Icon3, planned.Count.ToString(), false, false));
    }

    /// <summary>
    /// Adds a last point and finishes planning.
    /// </summary>
    /// <param name="world">The clicked world point.</param>
    public static void OnMapDoubleClick(Vector3 world)
    {
        OnMapClick(world);
        Minimap.instance.SetMapMode(Minimap.MapMode.Small);
    }

    /// <summary>
    /// Pauses or resumes the road.
    /// </summary>
    public static void TogglePause()
    {
        if (road != null)
        {
            road.Paused = !road.Paused;
            road.Blocker = null;
            Save();
        }
    }

    /// <summary>
    /// Gives up the road; what is built stays.
    /// </summary>
    public static void Cancel()
    {
        road = null;
        ClearPlanPins();
        Save();
    }

    /// <summary>
    /// Finishes planning when the map closes, and builds the road near the player. Called every frame for the local player.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Tick(Player player)
    {
        string world = ZNet.instance != null ? ZNet.instance.GetWorldName() : null;
        if (world != null && world != loadedWorld)
        {
            loadedWorld = world;
            Load();
        }

        if (Planning && (Minimap.instance == null || Minimap.instance.m_mode != Minimap.MapMode.Large))
        {
            FinishPlanning(player);
        }

        if (road == null || road.Paused || Time.time < nextTick || TerrainEdit.Busy)
        {
            return;
        }

        nextTick = Time.time + TickInterval;
        BuildNear(player);
    }

    private static void FinishPlanning(Player player)
    {
        Planning = false;
        if (planned.Count < 2)
        {
            ClearPlanPins();
            planned.Clear();
            return;
        }

        road = new Road
        {
            Points = new List<Vector3>(planned),
            Width = HoeTools.Width,
            Paint = HoeTools.PaintFor(HoeTools.Paving),
            Even = HoeTools.EvenRoad
        };
        road.Prepare();
        planned.Clear();
        ShowRoadPins();
        Save();
        player.Message(MessageHud.MessageType.Center,
            string.Format(Localization.instance.Localize("$msg_whitehilt_terrain_road_started"), Mathf.RoundToInt(Length(road.Points))));
    }

    private static void BuildNear(Player player)
    {
        Vector2 here = Flat(player.transform.position);
        for (int chunk = 0; chunk < road.Built.Length; chunk++)
        {
            if (road.Built[chunk])
            {
                continue;
            }

            int first = chunk * ChunkSamples;
            int last = Mathf.Min(first + ChunkSamples, road.Samples.Count - 1);
            if (Vector2.Distance(here, Flat(road.Samples[(first + last) / 2])) > BuildReach
                || Heightmap.FindHeightmap(road.Samples[first]) == null || Heightmap.FindHeightmap(road.Samples[last]) == null)
            {
                continue;
            }

            List<Vector3> stretch = new();
            for (int i = first; i <= last; i++)
            {
                if (road.Samples[i].y < ZoneSystem.instance.m_waterLevel + WaterMargin)
                {
                    road.HitWater = true;
                    continue;
                }

                stretch.Add(road.Samples[i]);
            }

            if (stretch.Count > 0)
            {
                TerrainShapes.Plan plan = TerrainShapes.Band(stretch, road.Width, RoadSkirt, road.Paint, road.Done);
                plan.Job.Undoable = false;
                if (plan.Problem != null || !TerrainShapes.Run(player, plan))
                {
                    road.Paused = true;
                    road.Blocker = plan.Problem ?? Localization.instance.Localize(TerrainCost.Stone?.m_itemData.m_shared.m_name ?? string.Empty);
                    player.Message(MessageHud.MessageType.Center,
                        string.Format(Localization.instance.Localize("$msg_whitehilt_terrain_road_paused"), road.Blocker));
                    Save();
                    return;
                }
            }

            road.Built[chunk] = true;
            Save();
            if (Array.TrueForAll(road.Built, done => done))
            {
                player.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$whitehilt_terrain_road") + ": "
                    + Localization.instance.Localize("$whitehilt_terrain_road_done"));
                Cancel();
            }

            return;
        }
    }

    private static void ShowRoadPins()
    {
        ClearPlanPins();
        if (road == null || Minimap.instance == null)
        {
            return;
        }

        float sinceLast = PinSpacing;
        for (int i = 0; i < road.Samples.Count; i++)
        {
            sinceLast += SampleStep;
            if (sinceLast >= PinSpacing || i == road.Samples.Count - 1)
            {
                sinceLast = 0f;
                pins.Add(Minimap.instance.AddPin(road.Samples[i], Minimap.PinType.Icon3, string.Empty, false, false));
            }
        }
    }

    private static void ClearPlanPins()
    {
        if (Minimap.instance != null)
        {
            foreach (Minimap.PinData pin in pins)
            {
                Minimap.instance.RemovePin(pin);
            }
        }

        pins.Clear();
    }

    private static string File()
    {
        string world = loadedWorld;
        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            world = world.Replace(invalid, '_');
        }

        return Path.Combine(Paths.ConfigPath, "BrudvikWhiteHilt", "roads", world + ".whroad");
    }

    private static void Save()
    {
        if (string.IsNullOrEmpty(loadedWorld))
        {
            return;
        }

        try
        {
            string file = File();
            if (road == null)
            {
                if (System.IO.File.Exists(file))
                {
                    System.IO.File.Delete(file);
                }

                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(file));
            ZPackage package = new();
            package.Write(Version);
            package.Write(road.Points.Count);
            foreach (Vector3 point in road.Points)
            {
                package.Write(point);
            }

            package.Write(road.Width);
            package.Write(road.Paint.HasValue ? (int)road.Paint.Value : -1);
            package.Write(road.Even);
            package.Write(road.Paused);
            package.Write(road.Built.Length);
            foreach (bool done in road.Built)
            {
                package.Write(done);
            }

            System.IO.File.WriteAllBytes(file, package.GetArray());
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"Could not save the road: {ex.Message}");
        }
    }

    private static void Load()
    {
        road = null;
        ClearPlanPins();
        string file = File();
        if (!System.IO.File.Exists(file))
        {
            return;
        }

        try
        {
            ZPackage package = new(System.IO.File.ReadAllBytes(file));
            package.ReadInt();
            Road loaded = new() { Points = new List<Vector3>() };
            int count = package.ReadInt();
            for (int i = 0; i < count; i++)
            {
                loaded.Points.Add(package.ReadVector3());
            }

            loaded.Width = package.ReadSingle();
            int paint = package.ReadInt();
            loaded.Paint = paint < 0 ? null : (TerrainModifier.PaintType)paint;
            loaded.Even = package.ReadBool();
            loaded.Paused = package.ReadBool();
            loaded.Prepare();
            int chunks = package.ReadInt();
            for (int i = 0; i < chunks && i < loaded.Built.Length; i++)
            {
                loaded.Built[i] = package.ReadBool();
            }

            road = loaded;
            ShowRoadPins();
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"Could not read the road: {ex.Message}");
        }
    }

    private static float Length(List<Vector3> points)
    {
        float length = 0f;
        for (int i = 1; i < points.Count; i++)
        {
            length += Vector2.Distance(Flat(points[i - 1]), Flat(points[i]));
        }

        return length;
    }

    private static Vector2 Flat(Vector3 point)
    {
        return new Vector2(point.x, point.z);
    }

    private sealed class Road
    {
        public List<Vector3> Points;
        public float Width;
        public TerrainModifier.PaintType? Paint;
        public bool Even;
        public bool Paused;
        public string Blocker;
        public bool HitWater;
        public List<Vector3> Samples = new();
        public bool[] Built = Array.Empty<bool>();
        public HashSet<long> Done = new();

        public void Prepare()
        {
            Samples.Clear();
            List<float> ground = new();
            List<float> along = new();
            List<int> segment = new();
            float travelled = 0f;
            for (int i = 0; i < Points.Count - 1; i++)
            {
                Vector2 a = Flat(Points[i]);
                Vector2 b = Flat(Points[i + 1]);
                float length = Vector2.Distance(a, b);
                int steps = Mathf.Max(1, Mathf.CeilToInt(length / SampleStep));
                for (int s = 0; s < steps; s++)
                {
                    Vector2 at = Vector2.Lerp(a, b, s / (float)steps);
                    Samples.Add(new Vector3(at.x, 0f, at.y));
                    ground.Add(WorldGenerator.instance.GetHeight(at.x, at.y));
                    along.Add(travelled + length * s / steps);
                    segment.Add(i);
                }

                travelled += length;
            }

            Vector2 end = Flat(Points[Points.Count - 1]);
            Samples.Add(new Vector3(end.x, 0f, end.y));
            ground.Add(WorldGenerator.instance.GetHeight(end.x, end.y));
            along.Add(travelled);
            segment.Add(Points.Count - 2);

            float[] pointHeights = new float[Points.Count];
            for (int i = 0; i < Points.Count; i++)
            {
                pointHeights[i] = WorldGenerator.instance.GetHeight(Points[i].x, Points[i].z);
            }

            float[] pointAlong = new float[Points.Count];
            for (int i = 1; i < Points.Count; i++)
            {
                pointAlong[i] = pointAlong[i - 1] + Vector2.Distance(Flat(Points[i - 1]), Flat(Points[i]));
            }

            for (int i = 0; i < Samples.Count; i++)
            {
                float height;
                if (Even)
                {
                    int s = segment[i];
                    float span = Mathf.Max(0.01f, pointAlong[s + 1] - pointAlong[s]);
                    height = Mathf.Lerp(pointHeights[s], pointHeights[s + 1], (along[i] - pointAlong[s]) / span);
                }
                else
                {
                    float sum = 0f;
                    int count = 0;
                    for (int j = Mathf.Max(0, i - SmoothWindow); j <= Mathf.Min(Samples.Count - 1, i + SmoothWindow); j++)
                    {
                        sum += ground[j];
                        count++;
                    }

                    height = sum / count;
                }

                Samples[i] = new Vector3(Samples[i].x, height, Samples[i].z);
            }

            Built = new bool[Mathf.Max(1, Mathf.CeilToInt((Samples.Count - 1) / (float)ChunkSamples))];
        }
    }
}
