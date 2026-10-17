using System.Collections.Generic;
using System.Linq;
using BrudvikWhiteHilt.Pieces.Structure.Darkwood;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Structure.Planking;

/// <summary>
/// Saws a plank of a vanilla model along a plane and closes the cut, so a floor or a wall cut to a new shape looks
/// sawn, not hollow. The cut end shows the plank's broad face, as far in from the cut as the end's corner lies under
/// that face. Parts reaching no more than <see cref="Slack"/> over the plane are kept whole, as the vanilla planks reach
/// a centimetre past their piece's edge, and parts no more than that over it are left out. The planks of the vanilla
/// wooden pieces are convex, which the closing of the cut relies on.
/// </summary>
public static class Sawing
{
    /// <summary>How far a plank may reach over a cut, or lie inside it, without being sawn.</summary>
    public const float Slack = 0.02f;

    // A cut corner lies on the saw's plane within this.
    private const float OnCut = 1e-4f;

    /// <summary>
    /// Cuts a convex part along a plane and closes the cut, keeping what lies on the side the normal points to.
    /// </summary>
    /// <param name="part">A convex part, such as one plank.</param>
    /// <param name="normal">The plane's normal, of length 1, towards the side kept.</param>
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
        MeshData end = CutEnd(kept, normal, distance, BroadFace(part));
        return end == null ? kept : MeshData.Combine(new[] { kept, end });
    }

    /// <summary>
    /// Saws every loose part of a model (each plank, batten and joist) along the planes in turn.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <param name="planes">The planes: normal towards the side kept, and distance.</param>
    /// <returns>What is kept.</returns>
    public static MeshData SawAll(MeshData model, IEnumerable<(Vector3 Normal, float Distance)> planes)
    {
        List<(Vector3 Normal, float Distance)> cuts = planes.ToList();
        List<MeshData> kept = new();
        foreach (MeshData plank in model.Islands())
        {
            MeshData cut = plank;
            foreach ((Vector3 normal, float distance) in cuts)
            {
                cut = Saw(cut, normal, distance);
                if (cut == null)
                {
                    break;
                }
            }

            if (cut != null)
            {
                kept.Add(cut);
            }
        }

        return MeshData.Combine(kept);
    }

    // The cut face of a convex part: the corners on the cut, joined round their outline and facing out of the part.
    private static MeshData CutEnd(MeshData kept, Vector3 normal, float distance, Face face)
    {
        Vector3 along = Vector3.Cross(Mathf.Abs(normal.y) < 0.9f ? Vector3.up : Vector3.forward, normal).normalized;
        Vector3 across = Vector3.Cross(normal, along);
        List<Vector3> points = new();
        foreach (int index in kept.Triangles.Distinct())
        {
            Vector3 p = kept.Vertices[index];
            if (Mathf.Abs(Vector3.Dot(normal, p) - distance) < OnCut && !points.Any(q => (q - p).sqrMagnitude < 1e-8f))
            {
                points.Add(p);
            }
        }

        List<Vector3> outline = Hull(points, along, across);
        if (outline.Count < 3)
        {
            return null;
        }

        float outermost = outline.Max(face.Height);
        MeshData end = new();
        foreach (Vector3 p in outline)
        {
            end.Vertices.Add(p);
            end.Normals.Add(-normal);

            // The broad face as far in from the cut as the corner lies under it.
            float under = outermost - face.Height(p);
            end.Uvs.Add(face.At(p + face.Normal * under + normal * under));
        }

        for (int i = 1; i + 1 < outline.Count; i++)
        {
            Vector3 facing = Vector3.Cross(outline[i] - outline[0], outline[i + 1] - outline[0]);
            end.Triangles.AddRange(Vector3.Dot(facing, normal) < 0f ? new[] { 0, i, i + 1 } : new[] { 0, i + 1, i });
        }

        return end;
    }

    // The convex outline of points in a plane spanned by two directions, in order round it. The points are rounded to
    // a hundredth of a millimetre from their middle first: corners on one line that differ by rounding alone would
    // otherwise sort out of order along it and turn the outline the wrong way, dropping a true corner.
    private static List<Vector3> Hull(List<Vector3> points, Vector3 along, Vector3 across)
    {
        Vector3 middle = points.Aggregate(Vector3.zero, (sum, p) => sum + p) / Mathf.Max(1, points.Count);
        static float Round(float value) => Mathf.Round(value * 1e5f) / 1e5f;
        List<(Vector2 Flat, Vector3 Point)> sorted = points
            .Select(p => (new Vector2(Round(Vector3.Dot(p - middle, along)), Round(Vector3.Dot(p - middle, across))), p))
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

    // The part's largest face (a plank's top, or the front of a wall board) and how its texture lies.
    private static Face BroadFace(MeshData part)
    {
        int best = -1;
        float bestArea = 0f;
        for (int t = 0; t < part.TriangleCount; t++)
        {
            Vector3 a = part.Vertices[part.Triangles[t * 3]], b = part.Vertices[part.Triangles[t * 3 + 1]], c = part.Vertices[part.Triangles[t * 3 + 2]];
            float area = Vector3.Cross(b - a, c - a).magnitude;
            if (area > bestArea && part.TriangleUvs(t).Length == 3)
            {
                best = t;
                bestArea = area;
            }
        }

        return best < 0 ? new Face() : new Face(part, best);
    }

    // A flat face: its outward normal, and the texture coordinate over its plane as an affine map.
    private sealed class Face
    {
        private readonly Vector3 origin, u, v;
        private readonly Vector2 uv, perU, perV;

        public Face()
        {
            Normal = Vector3.up;
            u = Vector3.right;
            v = Vector3.forward;
        }

        public Face(MeshData part, int triangle)
        {
            Vector3 a = part.Vertices[part.Triangles[triangle * 3]], b = part.Vertices[part.Triangles[triangle * 3 + 1]], c = part.Vertices[part.Triangles[triangle * 3 + 2]];
            Vector2[] uvs = part.TriangleUvs(triangle);
            Normal = Vector3.Cross(b - a, c - a).normalized;
            u = (b - a).normalized;
            v = Vector3.Cross(Normal, u);
            origin = a;
            uv = uvs[0];

            // Solve uv(p) = uv(a) + perU * (p - a)·u + perV * (p - a)·v through the triangle's three corners.
            Vector2 e1 = new(Vector3.Dot(b - a, u), Vector3.Dot(b - a, v)), e2 = new(Vector3.Dot(c - a, u), Vector3.Dot(c - a, v));
            float det = e1.x * e2.y - e2.x * e1.y;
            if (Mathf.Abs(det) < 1e-10f)
            {
                return;
            }

            Vector2 d1 = uvs[1] - uvs[0], d2 = uvs[2] - uvs[0];
            perU = (d1 * e2.y - d2 * e1.y) / det;
            perV = (d2 * e1.x - d1 * e2.x) / det;
        }

        public Vector3 Normal { get; }

        // How far out along the normal a point lies, from the face's plane.
        public float Height(Vector3 p)
        {
            return Vector3.Dot(p - origin, Normal);
        }

        public Vector2 At(Vector3 p)
        {
            Vector3 d = p - origin;
            return uv + perU * Vector3.Dot(d, u) + perV * Vector3.Dot(d, v);
        }
    }
}
