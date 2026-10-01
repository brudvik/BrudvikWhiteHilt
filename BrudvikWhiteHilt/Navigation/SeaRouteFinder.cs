using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Navigation;

/// <summary>
/// Finds a sea route through deep water on a coarse grid over the world's original ground, which is known everywhere,
/// also where nobody has been. Docks, dug channels and other changes by players are not seen.
/// </summary>
public static class SeaRouteFinder
{
    private const float Cell = 32f;
    private const float Draft = 3f;

    // Reaches past the cell's half width, so two neighbouring water cells always leave room between them.
    private const float Margin = 18f;

    // Straight legs are checked on the ground itself every few metres, this far to each side of the line.
    private const float LineStep = 4f;
    private const float LineClearance = 9f;
    private const float WorldRadius = 10000f;
    private const int MaxNodes = 250000;
    private const double MillisecondsPerFrame = 4.0;
    private const int SnapCells = 20;

    private static readonly Vector2[] samples =
    {
        Vector2.zero, new(Margin, Margin), new(Margin, -Margin), new(-Margin, Margin), new(-Margin, -Margin),
        new(Margin, 0f), new(-Margin, 0f), new(0f, Margin), new(0f, -Margin)
    };
    private static readonly (int X, int Z, float Cost)[] steps =
    {
        (1, 0, 1f), (-1, 0, 1f), (0, 1, 1f), (0, -1, 1f),
        (1, 1, 1.4142f), (1, -1, 1.4142f), (-1, 1, 1.4142f), (-1, -1, 1.4142f)
    };

    /// <summary>
    /// Plans a route from a start through each target in turn. Runs over several frames.
    /// </summary>
    /// <param name="from">Where the ship is.</param>
    /// <param name="targets">The markers, in order.</param>
    /// <param name="done">Called with the route and the targets moved into deep water, or with nulls if none was found.</param>
    /// <returns>The coroutine.</returns>
    public static IEnumerator Find(Vector3 from, IList<Vector3> targets, Action<List<Vector3>, List<Vector3>> done)
    {
        Dictionary<long, bool> water = new();
        (int X, int Z)? start = Snap(ToCell(from), water);
        if (start == null)
        {
            done(null, null);
            yield break;
        }

        List<(int X, int Z)> cells = new() { start.Value };
        List<int> stops = new();
        List<Vector3> snapped = new();
        foreach (Vector3 target in targets)
        {
            (int X, int Z)? goal = Snap(ToCell(target), water);
            if (goal == null)
            {
                done(null, null);
                yield break;
            }

            List<(int X, int Z)> leg = null;
            yield return Search(cells[cells.Count - 1], goal.Value, water, result => leg = result);
            if (leg == null)
            {
                done(null, null);
                yield break;
            }

            cells.AddRange(leg.GetRange(1, leg.Count - 1));
            stops.Add(cells.Count - 1);
            snapped.Add(ToWorld(goal.Value));
        }

        List<Vector3> route = null;
        yield return Simplify(cells, stops, water, result => route = result);
        done(route, snapped);
    }

    private static IEnumerator Search((int X, int Z) start, (int X, int Z) goal, Dictionary<long, bool> water, Action<List<(int X, int Z)>> done)
    {
        if (start == goal)
        {
            done(new List<(int X, int Z)> { start });
            yield break;
        }

        Dictionary<long, float> cost = new() { [Key(start)] = 0f };
        Dictionary<long, (int X, int Z)> cameFrom = new();
        HashSet<long> closed = new();
        MinHeap open = new();
        open.Push(Heuristic(start, goal), start);
        int expanded = 0;
        System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();
        while (open.Count > 0)
        {
            (int X, int Z) current = open.Pop();
            long currentKey = Key(current);
            if (!closed.Add(currentKey))
            {
                continue;
            }

            if (current == goal)
            {
                List<(int X, int Z)> path = new() { current };
                while (cameFrom.TryGetValue(Key(path[path.Count - 1]), out (int X, int Z) previous))
                {
                    path.Add(previous);
                }

                path.Reverse();
                done(path);
                yield break;
            }

            if (++expanded > MaxNodes)
            {
                break;
            }

            if (expanded % 32 == 0 && clock.Elapsed.TotalMilliseconds > MillisecondsPerFrame)
            {
                yield return null;
                clock.Restart();
            }

            foreach ((int dx, int dz, float step) in steps)
            {
                (int X, int Z) next = (current.X + dx, current.Z + dz);
                long nextKey = Key(next);
                if (closed.Contains(nextKey) || !IsWater(next, water))
                {
                    continue;
                }

                // No cutting across a corner of land.
                if (dx != 0 && dz != 0 && (!IsWater((current.X + dx, current.Z), water) || !IsWater((current.X, current.Z + dz), water)))
                {
                    continue;
                }

                float nextCost = cost[currentKey] + step;
                if (cost.TryGetValue(nextKey, out float known) && known <= nextCost)
                {
                    continue;
                }

                cost[nextKey] = nextCost;
                cameFrom[nextKey] = current;
                open.Push(nextCost + Heuristic(next, goal), next);
            }
        }

        done(null);
    }

    // Keeps only the turning points, skipping every point the ship can sail past in a straight line,
    // but never a marker: each leg ends on its marker's cell.
    private static IEnumerator Simplify(List<(int X, int Z)> cells, List<int> stops, Dictionary<long, bool> water, Action<List<Vector3>> done)
    {
        List<Vector3> route = new();
        int from = 0;
        int stop = 0;
        route.Add(ToWorld(cells[0]));
        System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();
        while (from < cells.Count - 1)
        {
            while (stops[stop] <= from)
            {
                stop++;
            }

            int to = stops[stop];
            while (to > from + 1 && !(ClearLine(cells[from], cells[to], water) && DeepLine(ToWorld(cells[from]), ToWorld(cells[to]))))
            {
                to--;
                if (clock.Elapsed.TotalMilliseconds > MillisecondsPerFrame)
                {
                    yield return null;
                    clock.Restart();
                }
            }

            route.Add(ToWorld(cells[to]));
            from = to;
        }

        done(route);
    }

    // The ground itself along the line and to both sides of it, finer than the cells.
    private static bool DeepLine(Vector3 start, Vector3 end)
    {
        Vector3 along = end - start;
        along.y = 0f;
        float length = along.magnitude;
        if (length < 0.01f)
        {
            return true;
        }

        Vector3 direction = along / length;
        Vector3 side = new(direction.z, 0f, -direction.x);
        float limit = ZoneSystem.instance.m_waterLevel - Draft;
        int steps = Mathf.CeilToInt(length / LineStep);
        for (int i = 0; i <= steps; i++)
        {
            Vector3 point = start + direction * (length * i / steps);
            for (int lane = -1; lane <= 1; lane++)
            {
                Vector3 sample = point + side * (lane * LineClearance);
                if (WorldGenerator.instance.GetHeight(sample.x, sample.z) > limit)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool ClearLine((int X, int Z) a, (int X, int Z) b, Dictionary<long, bool> water)
    {
        Vector3 start = ToWorld(a);
        Vector3 end = ToWorld(b);
        int samples = Mathf.CeilToInt(Vector3.Distance(start, end) / (Cell / 2f));
        for (int i = 1; i < samples; i++)
        {
            if (!IsWater(ToCell(Vector3.Lerp(start, end, i / (float)samples)), water))
            {
                return false;
            }
        }

        return true;
    }

    // The nearest deep-water cell, searched in growing squares.
    private static (int X, int Z)? Snap((int X, int Z) cell, Dictionary<long, bool> water)
    {
        for (int ring = 0; ring <= SnapCells; ring++)
        {
            (int X, int Z)? best = null;
            float bestDistance = float.MaxValue;
            for (int dx = -ring; dx <= ring; dx++)
            {
                for (int dz = -ring; dz <= ring; dz++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)) != ring)
                    {
                        continue;
                    }

                    (int X, int Z) candidate = (cell.X + dx, cell.Z + dz);
                    float distance = dx * dx + dz * dz;
                    if (distance < bestDistance && IsWater(candidate, water))
                    {
                        best = candidate;
                        bestDistance = distance;
                    }
                }
            }

            if (best != null)
            {
                return best;
            }
        }

        return null;
    }

    private static bool IsWater((int X, int Z) cell, Dictionary<long, bool> water)
    {
        long key = Key(cell);
        if (water.TryGetValue(key, out bool known))
        {
            return known;
        }

        Vector3 center = ToWorld(cell);
        bool deep = new Vector2(center.x, center.z).magnitude < WorldRadius;
        float limit = ZoneSystem.instance.m_waterLevel - Draft;
        foreach (Vector2 sample in samples)
        {
            if (deep && WorldGenerator.instance.GetHeight(center.x + sample.x, center.z + sample.y) > limit)
            {
                deep = false;
            }
        }

        water[key] = deep;
        return deep;
    }

    private static float Heuristic((int X, int Z) a, (int X, int Z) b)
    {
        int dx = Math.Abs(a.X - b.X);
        int dz = Math.Abs(a.Z - b.Z);
        return Math.Max(dx, dz) + 0.4142f * Math.Min(dx, dz);
    }

    private static (int X, int Z) ToCell(Vector3 world)
    {
        return (Mathf.RoundToInt(world.x / Cell), Mathf.RoundToInt(world.z / Cell));
    }

    private static Vector3 ToWorld((int X, int Z) cell)
    {
        return new Vector3(cell.X * Cell, ZoneSystem.instance.m_waterLevel, cell.Z * Cell);
    }

    private static long Key((int X, int Z) cell)
    {
        return ((long)cell.X << 32) ^ (uint)cell.Z;
    }

    private sealed class MinHeap
    {
        private readonly List<(float Priority, (int X, int Z) Cell)> items = new();

        public int Count => items.Count;

        public void Push(float priority, (int X, int Z) cell)
        {
            items.Add((priority, cell));
            int child = items.Count - 1;
            while (child > 0)
            {
                int parent = (child - 1) / 2;
                if (items[parent].Priority <= items[child].Priority)
                {
                    break;
                }

                (items[parent], items[child]) = (items[child], items[parent]);
                child = parent;
            }
        }

        public (int X, int Z) Pop()
        {
            (int X, int Z) top = items[0].Cell;
            int last = items.Count - 1;
            items[0] = items[last];
            items.RemoveAt(last);
            int parent = 0;
            while (true)
            {
                int left = parent * 2 + 1;
                if (left >= items.Count)
                {
                    break;
                }

                int smallest = left + 1 < items.Count && items[left + 1].Priority < items[left].Priority ? left + 1 : left;
                if (items[parent].Priority <= items[smallest].Priority)
                {
                    break;
                }

                (items[parent], items[smallest]) = (items[smallest], items[parent]);
                parent = smallest;
            }

            return top;
        }
    }
}
