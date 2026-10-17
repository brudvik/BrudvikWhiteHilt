using System.Collections.Generic;
using System.Linq;
using BrudvikWhiteHilt.Pieces.Structure.Darkwood;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Structure.WoodFloors;

/// <summary>
/// The outline of a wooden floor, seen from above (x, z), and where the vanilla 2 × 2 m floor is laid under it.
/// </summary>
public sealed class FloorShape
{
    /// <summary>Creates a shape.</summary>
    /// <param name="corners">The corners in order round the floor; the shape must be convex.</param>
    /// <param name="tileOrigin">The centre of one vanilla floor laid under it; the others lie 2 m apart.</param>
    public FloorShape(Vector2[] corners, Vector2 tileOrigin)
    {
        Corners = corners;
        TileOrigin = tileOrigin;
    }

    /// <summary>The corners in order round the floor.</summary>
    public Vector2[] Corners { get; }

    /// <summary>The centre of one vanilla floor laid under it.</summary>
    public Vector2 TileOrigin { get; }

    /// <summary>Area in square metres.</summary>
    public float Area
    {
        get
        {
            float twice = 0f;
            for (int i = 0; i < Corners.Length; i++)
            {
                Vector2 a = Corners[i], b = Corners[(i + 1) % Corners.Length];
                twice += a.x * b.y - b.x * a.y;
            }

            return Mathf.Abs(twice) / 2f;
        }
    }

    /// <summary>
    /// A right-angled triangle, half of a square floor cut corner to corner, its square corner where the vanilla floor
    /// of the same size has one.
    /// </summary>
    /// <param name="leg">Length of the two short sides: 2 or 1.</param>
    /// <returns>The shape.</returns>
    public static FloorShape Triangle(float leg)
    {
        float h = leg / 2f;
        return new FloorShape(new[] { new Vector2(-h, -h), new Vector2(h, -h), new Vector2(-h, h) }, new Vector2(1f - h, 1f - h));
    }

    /// <summary>
    /// A floor 1 m wide and 2 m long along its planks, with one end cut at 45°: 2 m along one side, 1 m along the other.
    /// </summary>
    /// <param name="mirrored">Whether the long side is on the left instead of the right.</param>
    /// <returns>The shape.</returns>
    public static FloorShape AngledEnd(bool mirrored)
    {
        float s = mirrored ? -1f : 1f;
        Vector2[] corners = { new(-0.5f * s, -1f), new(0.5f * s, -1f), new(0.5f * s, 1f), new(-0.5f * s, 0f) };
        return new FloorShape(corners, Vector2.zero);
    }

    /// <summary>
    /// A regular octagon with two sides across the planks and two along them, so 2 m walls stand on a 2 m side.
    /// </summary>
    /// <param name="side">Length of a side.</param>
    /// <returns>The shape.</returns>
    public static FloorShape Octagon(float side)
    {
        float a = side / 2f, b = a + side / Mathf.Sqrt(2f);
        Vector2[] corners =
        {
            new(-a, -b), new(a, -b), new(b, -a), new(b, a), new(a, b), new(-a, b), new(-b, a), new(-b, -a)
        };
        return new FloorShape(corners, Vector2.zero);
    }

    /// <summary>
    /// The inward normal (x, z) and distance of each side's line: a point p is inside when dot(normal, p) &gt;= distance
    /// for every side.
    /// </summary>
    /// <returns>One line per side.</returns>
    public IEnumerable<(Vector2 Normal, float Distance)> Sides()
    {
        Vector2 centre = Corners.Aggregate(Vector2.zero, (sum, corner) => sum + corner) / Corners.Length;
        for (int i = 0; i < Corners.Length; i++)
        {
            Vector2 a = Corners[i], b = Corners[(i + 1) % Corners.Length];
            Vector2 normal = new Vector2(-(b - a).y, (b - a).x).normalized;
            if (Vector2.Dot(normal, centre - a) < 0f)
            {
                normal = -normal;
            }

            yield return (normal, Vector2.Dot(normal, a));
        }
    }

    /// <summary>
    /// Snap points: every corner, and the middle of every side longer than a wall, so a 2 m wall can stand on half of a
    /// 4 m side and two triangles meet at the middle of their long sides.
    /// </summary>
    /// <returns>The points, at floor level.</returns>
    public IEnumerable<Vector3> SnapPoints()
    {
        for (int i = 0; i < Corners.Length; i++)
        {
            Vector2 a = Corners[i], b = Corners[(i + 1) % Corners.Length];
            yield return new Vector3(a.x, 0f, a.y);
            if ((b - a).magnitude > 2.5f)
            {
                Vector2 middle = (a + b) / 2f;
                yield return new Vector3(middle.x, 0f, middle.y);
            }
        }
    }
}

/// <summary>
/// Cuts a wooden floor to a shape out of the vanilla 2 × 2 m floor: the floor is laid as often as the shape needs, and
/// each plank and joist that crosses a side is sawn along it and gets a closed end, textured as the plank's top.
/// Planks that reach no more than <see cref="Slack"/> over a side are kept whole, as the vanilla floor's own planks
/// reach a centimetre past its edge; planks no more than that inside are left out. The vanilla planks are convex, which
/// the closing of the cut ends relies on.
/// </summary>
public static class FloorCutting
{
    /// <summary>How far a plank may reach over a side, or lie inside it, without being sawn.</summary>
    public const float Slack = 0.02f;

    // A cut corner lies on the saw's line within this.
    private const float OnCut = 1e-4f;

    /// <summary>
    /// Cuts the floor.
    /// </summary>
    /// <param name="shape">The shape.</param>
    /// <param name="tile">The vanilla 2 × 2 m floor's model, centred on its origin.</param>
    /// <returns>The floor's model.</returns>
    public static MeshData Cut(FloorShape shape, MeshData tile)
    {
        List<(Vector2 Normal, float Distance)> sides = shape.Sides().ToList();
        List<MeshData> planks = new();
        foreach (MeshData plank in Lay(shape, tile).SelectMany(laid => laid.Islands()))
        {
            MeshData cut = plank;
            foreach ((Vector2 normal, float distance) in sides)
            {
                cut = Saw(cut, new Vector3(normal.x, 0f, normal.y), distance);
                if (cut == null)
                {
                    break;
                }
            }

            if (cut != null)
            {
                planks.Add(cut);
            }
        }

        return MeshData.Combine(planks);
    }

    /// <summary>
    /// Cuts a convex part along a plane and closes the cut, keeping what lies on the side the normal points to.
    /// </summary>
    /// <param name="part">A convex part, such as one plank.</param>
    /// <param name="normal">The plane's normal, level, towards the side kept.</param>
    /// <param name="distance">The plane's distance along the normal.</param>
    /// <returns>The part kept, or null when nothing is.</returns>
    public static MeshData Saw(MeshData part, Vector3 normal, float distance)
    {
        float min = float.MaxValue, max = float.MinValue;
        foreach (int index in part.Triangles)
        {
            float along = Vector3.Dot(normal, part.Vertices[index]);
            min = Mathf.Min(min, along);
            max = Mathf.Max(max, along);
        }

        if (part.TriangleCount == 0 || max <= distance + Slack)
        {
            return null;
        }

        if (min >= distance - Slack)
        {
            return part;
        }

        MeshData kept = part.Clip(normal, distance);
        MeshData end = CutEnd(kept, normal, distance, TopTexture(part));
        return end == null ? kept : MeshData.Combine(new[] { kept, end });
    }

    /// <summary>
    /// A level, convex outline as a slab: top, bottom and sides, facing out. Used for the collider.
    /// </summary>
    /// <param name="corners">The outline (x, z).</param>
    /// <param name="bottom">Height of the bottom.</param>
    /// <param name="top">Height of the top.</param>
    /// <returns>The slab's corners, top ring first, and its triangles.</returns>
    public static (Vector3[] Vertices, int[] Triangles) Slab(Vector2[] corners, float bottom, float top)
    {
        int n = corners.Length;
        Vector3[] vertices = corners.Select(c => new Vector3(c.x, top, c.y)).Concat(corners.Select(c => new Vector3(c.x, bottom, c.y))).ToArray();
        Vector3 centre = vertices.Aggregate(Vector3.zero, (sum, v) => sum + v) / vertices.Length;
        List<int> triangles = new();
        void Face(int a, int b, int c)
        {
            Vector3 facing = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
            triangles.AddRange(Vector3.Dot(facing, vertices[a] - centre) >= 0f ? new[] { a, b, c } : new[] { a, c, b });
        }

        for (int i = 1; i + 1 < n; i++)
        {
            Face(0, i, i + 1);
            Face(n, n + i, n + i + 1);
        }

        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            Face(i, j, n + j);
            Face(i, n + j, n + i);
        }

        return (vertices, triangles.ToArray());
    }

    // The vanilla floor laid under the shape, every 2 m from the tile origin, where it covers more than a sliver.
    private static IEnumerable<MeshData> Lay(FloorShape shape, MeshData tile)
    {
        float minX = shape.Corners.Min(c => c.x), maxX = shape.Corners.Max(c => c.x);
        float minZ = shape.Corners.Min(c => c.y), maxZ = shape.Corners.Max(c => c.y);
        Vector2 origin = shape.TileOrigin;
        for (int i = Mathf.CeilToInt((minX - origin.x - 1f) / 2f - 0.01f); origin.x + 2f * i - 1f < maxX - Slack; i++)
        {
            for (int j = Mathf.CeilToInt((minZ - origin.y - 1f) / 2f - 0.01f); origin.y + 2f * j - 1f < maxZ - Slack; j++)
            {
                float x = origin.x + 2f * i, z = origin.y + 2f * j;
                if (x + 1f > minX + Slack && z + 1f > minZ + Slack)
                {
                    yield return tile.Translated(new Vector3(x, 0f, z));
                }
            }
        }
    }

    // The cut face of a convex part: the corners on the cut, joined round their outline and facing out of the part.
    private static MeshData CutEnd(MeshData kept, Vector3 normal, float distance, PlankTexture texture)
    {
        Vector3 along = Vector3.Cross(Vector3.up, normal).normalized;
        List<Vector3> points = new();
        foreach (int index in kept.Triangles.Distinct())
        {
            Vector3 p = kept.Vertices[index];
            if (Mathf.Abs(Vector3.Dot(normal, p) - distance) < OnCut && !points.Any(q => (q - p).sqrMagnitude < 1e-8f))
            {
                points.Add(p);
            }
        }

        List<Vector3> outline = Hull(points, along);
        if (outline.Count < 3)
        {
            return null;
        }

        float top = outline.Max(p => p.y);
        MeshData end = new();
        foreach (Vector3 p in outline)
        {
            end.Vertices.Add(p);
            end.Normals.Add(-normal);

            // The face shows the plank's top as far in from the cut as the corner is under the top.
            Vector3 onTop = p + normal * (top - p.y);
            end.Uvs.Add(texture.At(onTop.x, onTop.z));
        }

        for (int i = 1; i + 1 < outline.Count; i++)
        {
            Vector3 facing = Vector3.Cross(outline[i] - outline[0], outline[i + 1] - outline[0]);
            end.Triangles.AddRange(Vector3.Dot(facing, normal) < 0f ? new[] { 0, i, i + 1 } : new[] { 0, i + 1, i });
        }

        return end;
    }

    // The convex outline of points in the plane spanned by a level direction and up, in order round it.
    private static List<Vector3> Hull(List<Vector3> points, Vector3 along)
    {
        List<(Vector2 Flat, Vector3 Point)> sorted = points
            .Select(p => (new Vector2(Vector3.Dot(p, along), p.y), p))
            .OrderBy(entry => entry.Item1.x).ThenBy(entry => entry.Item1.y)
            .ToList();
        if (sorted.Count < 3)
        {
            return sorted.Select(entry => entry.Point).ToList();
        }

        static float Turn(Vector2 o, Vector2 a, Vector2 b) => (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
        List<(Vector2 Flat, Vector3 Point)> hull = new();
        foreach (IEnumerable<(Vector2 Flat, Vector3 Point)> pass in new[] { sorted, Enumerable.Reverse(sorted) })
        {
            int start = hull.Count;
            foreach ((Vector2 Flat, Vector3 Point) entry in pass)
            {
                while (hull.Count >= start + 2 && Turn(hull[hull.Count - 2].Flat, hull[hull.Count - 1].Flat, entry.Flat) <= 1e-9f)
                {
                    hull.RemoveAt(hull.Count - 1);
                }

                hull.Add(entry);
            }

            hull.RemoveAt(hull.Count - 1);
        }

        return hull.Select(entry => entry.Point).ToList();
    }

    // How the texture lies on the top of a part: from its largest upward face.
    private static PlankTexture TopTexture(MeshData part)
    {
        int best = -1;
        float bestArea = 0f;
        for (int t = 0; t < part.TriangleCount; t++)
        {
            Vector3 a = part.Vertices[part.Triangles[t * 3]], b = part.Vertices[part.Triangles[t * 3 + 1]], c = part.Vertices[part.Triangles[t * 3 + 2]];
            Vector3 cross = Vector3.Cross(b - a, c - a);
            float area = cross.magnitude;
            if (area > bestArea && Mathf.Abs(cross.normalized.y) > 0.9f && part.TriangleUvs(t).Length == 3)
            {
                best = t;
                bestArea = area;
            }
        }

        return best < 0 ? new PlankTexture() : new PlankTexture(part, best);
    }

    // The texture coordinate over a level face as an affine map of x and z.
    private sealed class PlankTexture
    {
        private readonly Vector2 origin, uv, perX, perZ;

        public PlankTexture()
        {
        }

        public PlankTexture(MeshData part, int triangle)
        {
            Vector3 a = part.Vertices[part.Triangles[triangle * 3]], b = part.Vertices[part.Triangles[triangle * 3 + 1]], c = part.Vertices[part.Triangles[triangle * 3 + 2]];
            Vector2[] uvs = part.TriangleUvs(triangle);
            Vector2 e1 = new(b.x - a.x, b.z - a.z), e2 = new(c.x - a.x, c.z - a.z);
            float det = e1.x * e2.y - e2.x * e1.y;
            if (Mathf.Abs(det) < 1e-8f)
            {
                uv = uvs[0];
                return;
            }

            // Solve uv(p) = uv(a) + perX * (p.x - a.x) + perZ * (p.z - a.z) through the triangle's three corners.
            Vector2 d1 = uvs[1] - uvs[0], d2 = uvs[2] - uvs[0];
            perX = (d1 * e2.y - d2 * e1.y) / det;
            perZ = (d2 * e1.x - d1 * e2.x) / det;
            origin = new Vector2(a.x, a.z);
            uv = uvs[0];
        }

        public Vector2 At(float x, float z)
        {
            return uv + perX * (x - origin.x) + perZ * (z - origin.y);
        }
    }
}
