using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Building.Moats;

/// <summary>
/// The sizes of one moat, taken from the settings when it is planned and kept with it.
/// </summary>
public sealed class MoatSpec
{
    /// <summary>The profile.</summary>
    public MoatProfile Profile;

    /// <summary>Width of the ditch at the top.</summary>
    public float Width;

    /// <summary>Depth of the ditch.</summary>
    public float Depth;

    /// <summary>Width of the flat bottom of a wet moat.</summary>
    public float Bottom;

    /// <summary>How far the water stays below the lowest edge of a wet moat.</summary>
    public float Freeboard;

    /// <summary>Whether a wet moat gets water.</summary>
    public bool Water;

    /// <summary>Where the dug earth goes.</summary>
    public MoatBank Bank;

    /// <summary>Width of the bank.</summary>
    public float BankWidth;

    /// <summary>Height of the bank.</summary>
    public float BankHeight;

    /// <summary>Distance between the sharp stakes of a staked ditch.</summary>
    public float StakeSpacing;

    /// <summary>How far from the centre line the moat and its bank reach.</summary>
    public float Reach => Width / 2f + (Bank != MoatBank.None ? BankWidth : 0f) + 1f;

    /// <summary>
    /// The sizes for a profile from the current settings.
    /// </summary>
    /// <param name="profile">The profile.</param>
    /// <param name="bank">Where the earth goes.</param>
    /// <returns>The sizes.</returns>
    public static MoatSpec FromSettings(MoatProfile profile, MoatBank bank)
    {
        return new MoatSpec
        {
            Profile = profile,
            Width = MoatSettings.Width(profile),
            Depth = MoatSettings.Depth(profile),
            Bottom = Mathf.Min(MoatSettings.WetBottom.Value, MoatSettings.WetWidth.Value - 1f),
            Freeboard = Mathf.Min(MoatSettings.Freeboard.Value, MoatSettings.WetDepth.Value - 0.5f),
            Water = MoatSettings.Water.Value,
            Bank = bank,
            BankWidth = MoatSettings.BankWidth.Value,
            BankHeight = MoatSettings.BankHeight.Value,
            StakeSpacing = MoatSettings.StakeSpacing.Value
        };
    }

    /// <summary>
    /// The wanted ground height at a distance from the centre line.
    /// </summary>
    /// <param name="lateral">Distance from the centre line, positive away from the walls.</param>
    /// <param name="current">The ground's height now.</param>
    /// <param name="bottom">Height of a wet moat's bottom; unused for the dry ditches.</param>
    /// <param name="height">The wanted height.</param>
    /// <returns>True if the moat or its bank reaches the point.</returns>
    public bool Wanted(float lateral, float current, float bottom, out float height)
    {
        float u = lateral + Width / 2f;
        if (u >= 0f && u <= Width)
        {
            if (Profile == MoatProfile.Wet)
            {
                float side = Mathf.Max(0.5f, (Width - Bottom) / 2f);
                float edge = Mathf.Min(u, Width - u);
                height = edge >= side ? bottom : bottom + (side - edge) / side * (current - bottom);
                height = Mathf.Min(current, height);
            }
            else
            {
                height = current - Depth * (1f - Mathf.Abs(2f * u / Width - 1f));
            }

            return true;
        }

        float into = Bank switch
        {
            MoatBank.Outside => u - Width,
            MoatBank.Inside => -u,
            _ => -1f
        };
        if (into > 0f && into < BankWidth)
        {
            height = current + BankHeight * Mathf.Sin(Mathf.PI * into / BankWidth);
            return true;
        }

        height = current;
        return false;
    }

    /// <summary>
    /// Writes the sizes.
    /// </summary>
    /// <param name="package">The package.</param>
    public void Write(ZPackage package)
    {
        package.Write((int)Profile);
        package.Write(Width);
        package.Write(Depth);
        package.Write(Bottom);
        package.Write(Freeboard);
        package.Write(Water);
        package.Write((int)Bank);
        package.Write(BankWidth);
        package.Write(BankHeight);
        package.Write(StakeSpacing);
    }

    /// <summary>
    /// Reads sizes written by <see cref="Write"/>.
    /// </summary>
    /// <param name="package">The package.</param>
    /// <returns>The sizes.</returns>
    public static MoatSpec Read(ZPackage package)
    {
        return new MoatSpec
        {
            Profile = (MoatProfile)package.ReadInt(),
            Width = package.ReadSingle(),
            Depth = package.ReadSingle(),
            Bottom = package.ReadSingle(),
            Freeboard = package.ReadSingle(),
            Water = package.ReadBool(),
            Bank = (MoatBank)package.ReadInt(),
            BankWidth = package.ReadSingle(),
            BankHeight = package.ReadSingle(),
            StakeSpacing = package.ReadSingle()
        };
    }
}

/// <summary>
/// One stretch of moat: its centre line sampled one metre apart, which way is away from the walls at each sample, and
/// how much of the ditch is dug there (none at causeways, tapering at the ends).
/// </summary>
public sealed class MoatRun
{
    /// <summary>Centre line samples (x, z), one metre apart.</summary>
    public readonly List<Vector2> Points = new();

    /// <summary>Unit direction away from the walls at each sample.</summary>
    public readonly List<Vector2> Outward = new();

    /// <summary>Share of the ditch dug at each sample: 0 at causeways, 1 in the full ditch.</summary>
    public readonly List<float> Factor = new();

    /// <summary>Whether the run is a closed ring.</summary>
    public bool Closed;

    /// <summary>Number of samples.</summary>
    public int Count => Points.Count;

    /// <summary>Number of samples that are causeways.</summary>
    public int CausewaySamples
    {
        get
        {
            int count = 0;
            foreach (float factor in Factor)
            {
                count += factor <= 0f ? 1 : 0;
            }

            return count;
        }
    }

    /// <summary>
    /// Index of a sample, wrapped for rings and clamped for open runs.
    /// </summary>
    /// <param name="index">Any index.</param>
    /// <returns>A valid index.</returns>
    public int Wrap(int index)
    {
        if (Closed)
        {
            return ((index % Count) + Count) % Count;
        }

        return Mathf.Clamp(index, 0, Count - 1);
    }

    /// <summary>
    /// Direction along the run at a sample.
    /// </summary>
    /// <param name="index">The sample.</param>
    /// <returns>Unit vector.</returns>
    public Vector2 Tangent(int index)
    {
        Vector2 direction = Points[Wrap(index + 1)] - Points[Wrap(index - 1)];
        return direction.sqrMagnitude > 0.0001f ? direction.normalized : new Vector2(-Outward[index].y, Outward[index].x);
    }

    /// <summary>
    /// Writes the run.
    /// </summary>
    /// <param name="package">The package.</param>
    public void Write(ZPackage package)
    {
        package.Write(Closed);
        package.Write(Count);
        for (int i = 0; i < Count; i++)
        {
            package.Write(Points[i].x);
            package.Write(Points[i].y);
            package.Write(Outward[i].x);
            package.Write(Outward[i].y);
            package.Write(Factor[i]);
        }
    }

    /// <summary>
    /// Reads a run written by <see cref="Write"/>.
    /// </summary>
    /// <param name="package">The package.</param>
    /// <returns>The run.</returns>
    public static MoatRun Read(ZPackage package)
    {
        MoatRun run = new() { Closed = package.ReadBool() };
        int count = package.ReadInt();
        for (int i = 0; i < count; i++)
        {
            run.Points.Add(new Vector2(package.ReadSingle(), package.ReadSingle()));
            run.Outward.Add(new Vector2(package.ReadSingle(), package.ReadSingle()));
            run.Factor.Add(package.ReadSingle());
        }

        return run;
    }
}

/// <summary>
/// Works out where a moat runs: along clicked points, or round the walls connected to a clicked piece, with causeways
/// left in front of gates.
/// </summary>
public static class MoatGeometry
{
    private const float Step = 1f;
    private const float Taper = 1.5f;
    private const float ConnectGap = 1.5f;
    private const float WallSlack = 2.5f;
    private const float GateSlack = 1.5f;
    private const float PieceSearch = 40f;

    private static readonly List<Piece> found = new();

    /// <summary>
    /// A moat along clicked points. The side away from nearby buildings is the outside.
    /// </summary>
    /// <param name="clicked">The points.</param>
    /// <param name="closed">Whether the last point joins the first.</param>
    /// <param name="spec">The moat's sizes.</param>
    /// <param name="causeways">Leave ground in front of gates.</param>
    /// <returns>The run, or null for fewer than two points.</returns>
    public static MoatRun FromPoints(IList<Vector3> clicked, bool closed, MoatSpec spec, bool causeways)
    {
        if (clicked.Count < 2)
        {
            return null;
        }

        List<Vector2> corners = new();
        foreach (Vector3 point in clicked)
        {
            corners.Add(new Vector2(point.x, point.z));
        }

        MoatRun run = new() { Closed = closed && corners.Count >= 3 };
        run.Points.AddRange(Resample(corners, run.Closed));
        if (run.Count < 2)
        {
            return null;
        }

        // Outward is to the right of the direction of travel for a counter-clockwise ring, else towards fewer buildings.
        bool right;
        if (run.Closed)
        {
            right = SignedArea(corners) > 0f;
        }
        else
        {
            right = !BuildingsOnRight(run);
        }

        SetNormals(run, right);
        ComputeFactors(run, causeways ? GatesNear(run, spec.Width / 2f + MoatSettings.Berm.Value + GateSlack) : new List<Vector2>(), spec,
            spec.Width / 2f + MoatSettings.Berm.Value + GateSlack);
        return run;
    }

    /// <summary>
    /// A moat round the walls connected to a piece: the convex outline of the walls, pushed out by the berm and half the
    /// ditch, kept only where walls stand behind it.
    /// </summary>
    /// <param name="start">The clicked piece.</param>
    /// <param name="spec">The moat's sizes.</param>
    /// <param name="causeways">Leave ground in front of gates.</param>
    /// <returns>The runs; empty if there is no wall.</returns>
    public static List<MoatRun> FollowWalls(Piece start, MoatSpec spec, bool causeways)
    {
        List<MoatRun> runs = new();
        List<Footprint> walls = ConnectedWalls(start);
        if (walls.Count == 0)
        {
            return runs;
        }

        List<Vector2> corners = new();
        List<Vector2> gates = new();
        foreach (Footprint wall in walls)
        {
            corners.AddRange(wall.Corners);
            if (wall.Gate)
            {
                gates.Add(wall.Centre);
            }
        }

        List<Vector2> hull = ConvexHull(corners);
        if (hull.Count < 3)
        {
            return runs;
        }

        float offset = MoatSettings.Berm.Value + spec.Width / 2f;
        MoatRun ring = new() { Closed = true };
        ring.Points.AddRange(Resample(OffsetHull(hull, offset), true));
        SetNormals(ring, true);

        // Only where a wall stands behind the ring; where the outline bridges a gap between walls, there is no moat.
        bool[] keep = new bool[ring.Count];
        bool all = true;
        for (int i = 0; i < ring.Count; i++)
        {
            float nearest = float.MaxValue;
            foreach (Footprint wall in walls)
            {
                nearest = Mathf.Min(nearest, wall.Distance(ring.Points[i]));
            }

            keep[i] = nearest <= offset + WallSlack;
            all &= keep[i];
        }

        if (all)
        {
            runs.Add(ring);
        }
        else
        {
            int first = System.Array.IndexOf(keep, false);
            MoatRun current = null;
            for (int k = 1; k <= ring.Count; k++)
            {
                int i = (first + k) % ring.Count;
                if (!keep[i])
                {
                    current = null;
                    continue;
                }

                if (current == null)
                {
                    current = new MoatRun();
                    runs.Add(current);
                }

                current.Points.Add(ring.Points[i]);
                current.Outward.Add(ring.Outward[i]);
            }

            runs.RemoveAll(run => run.Count < 3);
        }

        foreach (MoatRun run in runs)
        {
            ComputeFactors(run, causeways ? gates : new List<Vector2>(), spec, offset + GateSlack);
        }

        return runs;
    }

    /// <summary>
    /// Length of runs in metres.
    /// </summary>
    /// <param name="runs">The runs.</param>
    /// <returns>Metres.</returns>
    public static float Length(IEnumerable<MoatRun> runs)
    {
        float length = 0f;
        foreach (MoatRun run in runs)
        {
            length += run.Closed ? run.Count * Step : (run.Count - 1) * Step;
        }

        return length;
    }

    /// <summary>
    /// The outline of runs for drawing: both edges of the ditch, a little above the ground.
    /// </summary>
    /// <param name="runs">The runs.</param>
    /// <param name="width">Width of the ditch.</param>
    /// <returns>One line per edge.</returns>
    public static List<Vector3[]> Outline(IEnumerable<MoatRun> runs, float width)
    {
        List<Vector3[]> lines = new();
        foreach (MoatRun run in runs)
        {
            foreach (float side in new[] { -0.5f, 0.5f })
            {
                int count = run.Closed ? run.Count + 1 : run.Count;
                Vector3[] line = new Vector3[count];
                for (int i = 0; i < count; i++)
                {
                    int index = run.Wrap(i);
                    Vector2 point = run.Points[index] + run.Outward[index] * (side * width);
                    Vector3 world = new(point.x, 0f, point.y);
                    world.y = (Heightmap.GetHeight(world, out float ground) ? ground : 0f) + 0.2f;
                    line[i] = world;
                }

                lines.Add(line);
            }
        }

        return lines;
    }

    private static List<Vector2> Resample(IList<Vector2> corners, bool closed)
    {
        List<Vector2> samples = new();
        int segments = closed ? corners.Count : corners.Count - 1;
        float carry = 0f;
        for (int i = 0; i < segments; i++)
        {
            Vector2 a = corners[i];
            Vector2 b = corners[(i + 1) % corners.Count];
            float length = Vector2.Distance(a, b);
            float at = carry;
            while (at < length)
            {
                samples.Add(Vector2.Lerp(a, b, at / length));
                at += Step;
            }

            carry = at - length;
        }

        if (!closed)
        {
            Vector2 end = corners[corners.Count - 1];
            if (samples.Count == 0 || Vector2.Distance(samples[samples.Count - 1], end) > Step * 0.3f)
            {
                samples.Add(end);
            }
        }

        return samples;
    }

    private static void SetNormals(MoatRun run, bool right)
    {
        run.Outward.Clear();
        for (int i = 0; i < run.Count; i++)
        {
            Vector2 direction = run.Points[run.Wrap(i + 1)] - run.Points[run.Wrap(i - 1)];
            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            run.Outward.Add(right ? new Vector2(direction.y, -direction.x) : new Vector2(-direction.y, direction.x));
        }
    }

    // Each sample gets the share of ditch: none within half a causeway of a gate, tapering in over a metre and a half,
    // and tapering out at the ends of an open run.
    private static void ComputeFactors(MoatRun run, List<Vector2> gates, MoatSpec spec, float gateReach)
    {
        float[] gap = new float[run.Count];
        for (int i = 0; i < run.Count; i++)
        {
            gap[i] = float.MaxValue;
        }

        float half = MoatSettings.CausewayWidth.Value / 2f;
        foreach (Vector2 gate in gates)
        {
            int nearest = -1;
            float best = float.MaxValue;
            for (int i = 0; i < run.Count; i++)
            {
                float distance = Vector2.Distance(run.Points[i], gate);
                if (distance < best)
                {
                    best = distance;
                    nearest = i;
                }
            }

            if (nearest < 0 || best > gateReach)
            {
                continue;
            }

            for (int i = 0; i < run.Count; i++)
            {
                int steps = Mathf.Abs(i - nearest);
                if (run.Closed)
                {
                    steps = Mathf.Min(steps, run.Count - steps);
                }

                gap[i] = Mathf.Min(gap[i], Mathf.Max(0f, steps * Step - half));
            }
        }

        run.Factor.Clear();
        for (int i = 0; i < run.Count; i++)
        {
            float factor = gap[i] == float.MaxValue ? 1f : Mathf.Clamp01(gap[i] / Taper);
            if (!run.Closed)
            {
                factor *= Mathf.Clamp01((Mathf.Min(i, run.Count - 1 - i) + 0.5f) / 2.5f);
            }

            run.Factor.Add(factor);
        }
    }

    private static bool BuildingsOnRight(MoatRun run)
    {
        Vector2 middle = run.Points[run.Count / 2];
        found.Clear();
        Piece.GetAllPiecesInRadius(new Vector3(middle.x, 0f, middle.y), PieceSearch + run.Count / 2f, found);
        int right = 0;
        int left = 0;
        foreach (Piece piece in found)
        {
            if (piece == null || !piece.IsPlacedByPlayer())
            {
                continue;
            }

            Vector2 at = new(piece.transform.position.x, piece.transform.position.z);
            int nearest = 0;
            float best = float.MaxValue;
            for (int i = 0; i < run.Count; i++)
            {
                float distance = (run.Points[i] - at).sqrMagnitude;
                if (distance < best)
                {
                    best = distance;
                    nearest = i;
                }
            }

            Vector2 direction = run.Tangent(nearest);
            Vector2 rightNormal = new(direction.y, -direction.x);
            if (Vector2.Dot(at - run.Points[nearest], rightNormal) > 0f)
            {
                right++;
            }
            else
            {
                left++;
            }
        }

        return right > left;
    }

    private static List<Vector2> GatesNear(MoatRun run, float reach)
    {
        List<Vector2> gates = new();
        Vector2 min = run.Points[0];
        Vector2 max = run.Points[0];
        foreach (Vector2 point in run.Points)
        {
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }

        Vector2 centre = (min + max) / 2f;
        found.Clear();
        Piece.GetAllPiecesInRadius(new Vector3(centre.x, 0f, centre.y), Vector2.Distance(min, max) / 2f + reach, found);
        foreach (Piece piece in found)
        {
            if (piece != null && piece.IsPlacedByPlayer() && piece.GetComponent<Door>() != null)
            {
                Footprint footprint = Footprint.Of(piece);
                gates.Add(footprint != null ? footprint.Centre : new Vector2(piece.transform.position.x, piece.transform.position.z));
            }
        }

        return gates;
    }

    private static List<Footprint> ConnectedWalls(Piece start)
    {
        found.Clear();
        Piece.GetAllPiecesInRadius(start.transform.position, MoatSettings.FollowReach.Value, found);
        List<Footprint> candidates = new();
        Footprint first = null;
        foreach (Piece piece in found)
        {
            if (piece == null || !piece.IsPlacedByPlayer() || piece.GetComponent<WearNTear>() == null
                || piece.GetComponentInParent<Ship>() != null || piece.GetComponentInParent<Vagon>() != null || piece.GetComponent<Plant>() != null)
            {
                continue;
            }

            Footprint footprint = Footprint.Of(piece);
            if (footprint == null)
            {
                continue;
            }

            candidates.Add(footprint);
            if (piece == start)
            {
                first = footprint;
            }
        }

        List<Footprint> walls = new();
        if (first == null)
        {
            return walls;
        }

        Queue<Footprint> queue = new();
        HashSet<Footprint> seen = new() { first };
        queue.Enqueue(first);
        while (queue.Count > 0)
        {
            Footprint current = queue.Dequeue();
            walls.Add(current);
            foreach (Footprint other in candidates)
            {
                if (!seen.Contains(other) && current.Gap(other) <= ConnectGap)
                {
                    seen.Add(other);
                    queue.Enqueue(other);
                }
            }
        }

        return walls;
    }

    // Andrew's monotone chain; counter-clockwise with x right and z up.
    private static List<Vector2> ConvexHull(List<Vector2> points)
    {
        points.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
        List<Vector2> hull = new();
        for (int pass = 0; pass < 2; pass++)
        {
            int start = hull.Count;
            for (int k = 0; k < points.Count; k++)
            {
                Vector2 point = points[pass == 0 ? k : points.Count - 1 - k];
                while (hull.Count >= start + 2 && Cross(hull[hull.Count - 2], hull[hull.Count - 1], point) <= 0f)
                {
                    hull.RemoveAt(hull.Count - 1);
                }

                hull.Add(point);
            }

            hull.RemoveAt(hull.Count - 1);
        }

        return hull;
    }

    private static float Cross(Vector2 o, Vector2 a, Vector2 b)
    {
        return (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
    }

    // The hull pushed out by a distance, with round corners.
    private static List<Vector2> OffsetHull(List<Vector2> hull, float offset)
    {
        List<Vector2> ring = new();
        int count = hull.Count;
        for (int i = 0; i < count; i++)
        {
            Vector2 previous = hull[(i - 1 + count) % count];
            Vector2 corner = hull[i];
            Vector2 next = hull[(i + 1) % count];
            Vector2 inNormal = RightNormal(corner - previous);
            Vector2 outNormal = RightNormal(next - corner);
            float from = Mathf.Atan2(inNormal.y, inNormal.x);
            float to = Mathf.Atan2(outNormal.y, outNormal.x);
            // Outward normals of a counter-clockwise hull turn counter-clockwise.
            while (to < from)
            {
                to += Mathf.PI * 2f;
            }

            int steps = Mathf.Max(1, Mathf.CeilToInt((to - from) * offset / Step));
            for (int s = 0; s <= steps; s++)
            {
                float angle = Mathf.Lerp(from, to, s / (float)steps);
                ring.Add(corner + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * offset);
            }
        }

        return ring;
    }

    private static Vector2 RightNormal(Vector2 direction)
    {
        direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        return new Vector2(direction.y, -direction.x);
    }

    private static float SignedArea(List<Vector2> corners)
    {
        float area = 0f;
        for (int i = 0; i < corners.Count; i++)
        {
            Vector2 a = corners[i];
            Vector2 b = corners[(i + 1) % corners.Count];
            area += a.x * b.y - b.x * a.y;
        }

        return area / 2f;
    }

    /// <summary>
    /// Where a piece stands on the ground: the corners of its colliders and their bounding rectangle.
    /// </summary>
    private sealed class Footprint
    {
        public readonly List<Vector2> Corners = new();
        public Rect Box;
        public bool Gate;

        public Vector2 Centre => Box.center;

        public static Footprint Of(Piece piece)
        {
            Footprint footprint = new() { Gate = piece.GetComponent<Door>() != null };
            bool any = false;
            Vector2 min = default;
            Vector2 max = default;
            foreach (Collider collider in piece.GetComponentsInChildren<Collider>())
            {
                if (collider.isTrigger || !collider.enabled)
                {
                    continue;
                }

                int before = footprint.Corners.Count;
                if (collider is BoxCollider box)
                {
                    Vector3 half = box.size / 2f;
                    for (int corner = 0; corner < 4; corner++)
                    {
                        Vector3 local = box.center + new Vector3((corner & 1) == 0 ? -half.x : half.x, 0f, (corner & 2) == 0 ? -half.z : half.z);
                        Vector3 world = box.transform.TransformPoint(local);
                        footprint.Corners.Add(new Vector2(world.x, world.z));
                    }
                }
                else
                {
                    Bounds bounds = collider.bounds;
                    footprint.Corners.Add(new Vector2(bounds.min.x, bounds.min.z));
                    footprint.Corners.Add(new Vector2(bounds.max.x, bounds.min.z));
                    footprint.Corners.Add(new Vector2(bounds.min.x, bounds.max.z));
                    footprint.Corners.Add(new Vector2(bounds.max.x, bounds.max.z));
                }

                for (int i = before; i < footprint.Corners.Count; i++)
                {
                    min = any ? Vector2.Min(min, footprint.Corners[i]) : footprint.Corners[i];
                    max = any ? Vector2.Max(max, footprint.Corners[i]) : footprint.Corners[i];
                    any = true;
                }
            }

            if (!any)
            {
                return null;
            }

            footprint.Box = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return footprint;
        }

        public float Gap(Footprint other)
        {
            float dx = Mathf.Max(0f, Mathf.Max(Box.xMin - other.Box.xMax, other.Box.xMin - Box.xMax));
            float dz = Mathf.Max(0f, Mathf.Max(Box.yMin - other.Box.yMax, other.Box.yMin - Box.yMax));
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        public float Distance(Vector2 point)
        {
            float dx = Mathf.Max(0f, Mathf.Max(Box.xMin - point.x, point.x - Box.xMax));
            float dz = Mathf.Max(0f, Mathf.Max(Box.yMin - point.y, point.y - Box.yMax));
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}
