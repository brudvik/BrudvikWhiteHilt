using System.Collections.Generic;
using System.Linq;
using BrudvikWhiteHilt.Pieces.Structure.Darkwood;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Structure.Planking;

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
/// each plank and joist that crosses a side is sawn along it (<see cref="Sawing"/>).
/// </summary>
public static class FloorCutting
{
    /// <summary>
    /// Cuts the floor.
    /// </summary>
    /// <param name="shape">The shape.</param>
    /// <param name="tile">The vanilla 2 × 2 m floor's model, centred on its origin.</param>
    /// <returns>The floor's model.</returns>
    public static MeshData Cut(FloorShape shape, MeshData tile)
    {
        List<(Vector3 Normal, float Distance)> sides = shape.Sides().Select(side => (new Vector3(side.Normal.x, 0f, side.Normal.y), side.Distance)).ToList();

        // Floor by floor, so the planks of floors laid side by side are not taken for one part where they touch.
        return MeshData.Combine(Lay(shape, tile).Select(laid => Sawing.SawAll(laid, sides)));
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
        for (int i = Mathf.CeilToInt((minX - origin.x - 1f) / 2f - 0.01f); origin.x + 2f * i - 1f < maxX - Sawing.Slack; i++)
        {
            for (int j = Mathf.CeilToInt((minZ - origin.y - 1f) / 2f - 0.01f); origin.y + 2f * j - 1f < maxZ - Sawing.Slack; j++)
            {
                float x = origin.x + 2f * i, z = origin.y + 2f * j;
                if (x + 1f > minX + Sawing.Slack && z + 1f > minZ + Sawing.Slack)
                {
                    yield return tile.Translated(new Vector3(x, 0f, z));
                }
            }
        }
    }
}
