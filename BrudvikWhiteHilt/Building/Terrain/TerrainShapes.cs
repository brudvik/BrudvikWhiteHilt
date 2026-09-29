using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Building.Terrain;

/// <summary>
/// Turns a wanted terrain shape into terrain operations, one per terrain vertex (they lie one metre apart), and works
/// out how much is dug and filled and what it costs. Edges get a skirt that blends into the ground around.
/// </summary>
public static class TerrainShapes
{
    /// <summary>Width of the blending skirt around levelled areas and ramps, in metres.</summary>
    public const float Skirt = 3f;

    private const float TerrainLimit = 8f;

    /// <summary>
    /// What a shape wants at one vertex.
    /// </summary>
    public readonly struct Want
    {
        /// <summary>Whether the vertex changes at all.</summary>
        public readonly bool Change;

        /// <summary>The wanted height.</summary>
        public readonly float Height;

        /// <summary>True inside the shape itself (painted), false in the skirt.</summary>
        public readonly bool Core;

        /// <summary>
        /// Creates a wish for a vertex.
        /// </summary>
        /// <param name="change">Whether the vertex changes.</param>
        /// <param name="height">The wanted height.</param>
        /// <param name="core">True inside the shape itself.</param>
        public Want(bool change, float height, bool core)
        {
            Change = change;
            Height = height;
            Core = core;
        }
    }

    /// <summary>
    /// A shape turned into terrain operations.
    /// </summary>
    public sealed class Plan
    {
        /// <summary>The job to run.</summary>
        public TerrainEdit.Job Job = new();

        /// <summary>Cubic metres dug away.</summary>
        public float Dig;

        /// <summary>Cubic metres filled.</summary>
        public float Fill;

        /// <summary>Square metres paved with stone.</summary>
        public float Paved;

        /// <summary>Square metres the shape covers.</summary>
        public float Area;

        /// <summary>Stones it costs.</summary>
        public int Cost;

        /// <summary>True if some ground would go past the game's 8 m limit.</summary>
        public bool HitsLimit;

        /// <summary>A message if the shape may not be made here (ward, no-build zone, too big), otherwise null.</summary>
        public string Problem;
    }

    /// <summary>
    /// Plans a shape over the vertices of an area.
    /// </summary>
    /// <param name="min">Lowest corner of the area (x, z).</param>
    /// <param name="max">Highest corner of the area (x, z).</param>
    /// <param name="want">What the shape wants at a vertex, given its current height.</param>
    /// <param name="paint">Paint for the core, or null.</param>
    /// <param name="skip">Vertices already done, or null.</param>
    /// <returns>The plan.</returns>
    public static Plan Build(Vector2 min, Vector2 max, Func<float, float, float, Want> want, TerrainModifier.PaintType? paint, HashSet<long> skip = null)
    {
        Plan plan = new();
        TerrainOp.Settings core = TerrainEdit.LevelVertex(paint);
        TerrainOp.Settings edge = TerrainEdit.LevelVertex(null);
        for (int x = Mathf.CeilToInt(min.x); x <= Mathf.FloorToInt(max.x); x++)
        {
            for (int z = Mathf.CeilToInt(min.y); z <= Mathf.FloorToInt(max.y); z++)
            {
                if (skip != null && skip.Contains(Key(x, z)))
                {
                    continue;
                }

                Vector3 point = new(x, 0f, z);
                if (!Heightmap.GetHeight(point, out float current))
                {
                    continue;
                }

                Want wanted = want(x, z, current);
                if (!wanted.Change)
                {
                    continue;
                }

                float height = wanted.Height;
                if (WorldGenerator.instance != null)
                {
                    float original = WorldGenerator.instance.GetHeight(x, z);
                    if (Mathf.Abs(height - original) > TerrainLimit)
                    {
                        plan.HitsLimit = true;
                        height = Mathf.Clamp(height, original - TerrainLimit, original + TerrainLimit);
                    }
                }

                if (plan.Problem == null && ((x & 1) == 0 && (z & 1) == 0))
                {
                    point.y = current;
                    if (Location.IsInsideNoBuildLocation(point))
                    {
                        plan.Problem = Localization.instance.Localize("$msg_nobuildzone");
                    }
                    else if (!PrivateArea.CheckAccess(point, 0f, false))
                    {
                        plan.Problem = Localization.instance.Localize("$msg_privatezone");
                    }
                }

                plan.Dig += Mathf.Max(0f, current - height);
                plan.Fill += Mathf.Max(0f, height - current);
                if (wanted.Core)
                {
                    plan.Area += 1f;
                    if (paint == TerrainModifier.PaintType.Paved)
                    {
                        plan.Paved += 1f;
                    }
                }

                plan.Job.Ops.Add((new Vector3(x, height, z), wanted.Core ? core : edge));
                skip?.Add(Key(x, z));
            }
        }

        float maxArea = TerrainSettings.MaxArea.Value;
        if (plan.Problem == null && plan.Area > maxArea)
        {
            plan.Problem = string.Format(Localization.instance.Localize("$msg_whitehilt_terrain_too_big"), Mathf.RoundToInt(plan.Area), Mathf.RoundToInt(maxArea));
        }

        plan.Cost = TerrainCost.Amount(plan.Paved, plan.Fill);
        return plan;
    }

    /// <summary>
    /// A flat rectangle at a height, with a skirt blending into the ground around it.
    /// </summary>
    /// <param name="a">One corner.</param>
    /// <param name="b">The opposite corner.</param>
    /// <param name="height">The height.</param>
    /// <param name="paint">Paint inside the rectangle, or null.</param>
    /// <returns>The plan.</returns>
    public static Plan Level(Vector3 a, Vector3 b, float height, TerrainModifier.PaintType? paint)
    {
        Vector2 min = new(Mathf.Min(a.x, b.x), Mathf.Min(a.z, b.z));
        Vector2 max = new(Mathf.Max(a.x, b.x), Mathf.Max(a.z, b.z));
        return Build(min - Vector2.one * Skirt, max + Vector2.one * Skirt, (x, z, current) =>
        {
            float dx = Mathf.Max(min.x - x, 0f, x - max.x);
            float dz = Mathf.Max(min.y - z, 0f, z - max.y);
            float distance = Mathf.Sqrt(dx * dx + dz * dz);
            if (distance <= 0.01f)
            {
                return new Want(true, height, true);
            }

            if (distance >= Skirt)
            {
                return default;
            }

            return new Want(true, Mathf.Lerp(height, current, Mathf.SmoothStep(0f, 1f, distance / Skirt)), false);
        }, paint);
    }

    /// <summary>
    /// A band along a line of points, with heights going evenly from point to point, and a skirt on both sides.
    /// </summary>
    /// <param name="points">The points, with their heights.</param>
    /// <param name="width">Width of the band.</param>
    /// <param name="skirt">Width of the skirt on each side.</param>
    /// <param name="paint">Paint on the band, or null.</param>
    /// <param name="skip">Vertices already done, or null.</param>
    /// <returns>The plan.</returns>
    public static Plan Band(IList<Vector3> points, float width, float skirt, TerrainModifier.PaintType? paint, HashSet<long> skip = null)
    {
        Vector2 min = new(float.MaxValue, float.MaxValue);
        Vector2 max = new(float.MinValue, float.MinValue);
        foreach (Vector3 point in points)
        {
            min = Vector2.Min(min, new Vector2(point.x, point.z));
            max = Vector2.Max(max, new Vector2(point.x, point.z));
        }

        float half = width / 2f;
        float reach = half + skirt;
        return Build(min - Vector2.one * reach, max + Vector2.one * reach, (x, z, current) =>
        {
            Vector2 at = new(x, z);
            float best = float.MaxValue;
            float bestHeight = 0f;
            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector2 a = new(points[i].x, points[i].z);
                Vector2 b = new(points[i + 1].x, points[i + 1].z);
                Vector2 ab = b - a;
                float t = ab.sqrMagnitude < 0.0001f ? 0f : Mathf.Clamp01(Vector2.Dot(at - a, ab) / ab.sqrMagnitude);
                float distance = Vector2.Distance(at, a + ab * t);
                if (distance < best)
                {
                    best = distance;
                    bestHeight = Mathf.Lerp(points[i].y, points[i + 1].y, t);
                }
            }

            if (points.Count == 1)
            {
                best = Vector2.Distance(at, new Vector2(points[0].x, points[0].z));
                bestHeight = points[0].y;
            }

            if (best <= half)
            {
                return new Want(true, bestHeight, true);
            }

            if (best >= reach || skirt <= 0f)
            {
                return default;
            }

            return new Want(true, Mathf.Lerp(bestHeight, current, Mathf.SmoothStep(0f, 1f, (best - half) / skirt)), false);
        }, paint, skip);
    }

    /// <summary>
    /// Paint on a rectangle, without changing heights.
    /// </summary>
    /// <param name="a">One corner.</param>
    /// <param name="b">The opposite corner.</param>
    /// <param name="paint">The paint.</param>
    /// <returns>The plan.</returns>
    public static Plan PaintArea(Vector3 a, Vector3 b, TerrainModifier.PaintType paint)
    {
        Plan plan = new();
        TerrainOp.Settings settings = TerrainEdit.Paint(paint, 0.8f);
        for (int x = Mathf.CeilToInt(Mathf.Min(a.x, b.x)); x <= Mathf.FloorToInt(Mathf.Max(a.x, b.x)); x++)
        {
            for (int z = Mathf.CeilToInt(Mathf.Min(a.z, b.z)); z <= Mathf.FloorToInt(Mathf.Max(a.z, b.z)); z++)
            {
                Vector3 point = new(x, 0f, z);
                if (!Heightmap.GetHeight(point, out float height))
                {
                    continue;
                }

                point.y = height;
                if (plan.Problem == null && (x & 1) == 0 && (z & 1) == 0 && !PrivateArea.CheckAccess(point, 0f, false))
                {
                    plan.Problem = Localization.instance.Localize("$msg_privatezone");
                }

                plan.Area += 1f;
                plan.Job.Ops.Add((point, settings));
            }
        }

        if (paint == TerrainModifier.PaintType.Paved)
        {
            plan.Paved = plan.Area;
        }

        if (plan.Problem == null && plan.Area > TerrainSettings.MaxArea.Value)
        {
            plan.Problem = string.Format(Localization.instance.Localize("$msg_whitehilt_terrain_too_big"), Mathf.RoundToInt(plan.Area),
                Mathf.RoundToInt(TerrainSettings.MaxArea.Value));
        }

        plan.Cost = TerrainCost.Amount(plan.Paved, 0f);
        return plan;
    }

    /// <summary>
    /// Runs a plan: pays for it and queues its operations.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="plan">The plan.</param>
    /// <returns>True if it was started.</returns>
    public static bool Run(Player player, Plan plan)
    {
        if (plan.Problem != null)
        {
            player.Message(MessageHud.MessageType.Center, plan.Problem);
            return false;
        }

        if (plan.Job.Ops.Count == 0 || !TerrainCost.TryPay(player, plan.Cost))
        {
            return false;
        }

        plan.Job.Paid = TerrainCost.Stone;
        plan.Job.PaidAmount = player.m_noPlacementCost ? 0 : plan.Cost;
        TerrainEdit.Enqueue(plan.Job);
        if (plan.HitsLimit)
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_terrain_limit");
        }

        return true;
    }

    /// <summary>
    /// A key for a vertex, for sets of done vertices.
    /// </summary>
    /// <param name="x">World x.</param>
    /// <param name="z">World z.</param>
    /// <returns>The key.</returns>
    public static long Key(int x, int z)
    {
        return ((long)x << 32) ^ (uint)z;
    }
}
