using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Ships.WhiteHiltShip;

/// <summary>
/// Thin boxes that follow the longship's tent cloth, so players can stand on the tent and still walk under it. The
/// cloth is the same cross-section along its ridge, so the top of that cross-section is traced as a few straight
/// pieces, each a box as long as the tent. The cloth is steep beside the ridge, so a walkway lies along the ridge.
/// </summary>
public static class ShipTentColliders
{
    /// <summary>
    /// Name of the object holding the tent's colliders, switched with the tent.
    /// </summary>
    public const string ObjectName = "WhiteHiltTentColliders";

    private const float Thickness = 0.08f;
    private const float Bin = 0.05f;
    private const float Tolerance = 0.04f;
    private const float MinSegment = 0.02f;
    private const float WalkwayWidth = 0.6f;
    private const float NearRidge = 0.3f;

    // Players slide on slopes from 38 degrees.
    private const float MaxStandableSlope = 36f;

    /// <summary>
    /// Builds the colliders from the cloth meshes, under a new inactive child of the ship.
    /// </summary>
    /// <param name="root">The ship prefab's root.</param>
    /// <param name="cloth">The tent cloth meshes.</param>
    /// <param name="layer">Layer of the colliders, the one the deck crates use.</param>
    public static void Build(Transform root, IEnumerable<MeshFilter> cloth, int layer)
    {
        List<Vector3> points = cloth
            .Where(filter => filter.sharedMesh != null && filter.sharedMesh.isReadable)
            .SelectMany(filter => filter.sharedMesh.vertices.Select(vertex => root.InverseTransformPoint(filter.transform.TransformPoint(vertex))))
            .ToList();
        if (points.Count < 8)
        {
            throw new InvalidOperationException("the tent cloth has no readable vertices");
        }

        // The ridge runs along the axis where the height changes least.
        bool alongZ = HeightChange(points, true) <= HeightChange(points, false);
        float Across(Vector3 p) => alongZ ? p.x : p.z;
        float Along(Vector3 p) => alongZ ? p.z : p.x;
        float alongMin = points.Min(Along);
        float alongMax = points.Max(Along);
        float centre = (alongMin + alongMax) / 2f;
        float length = alongMax - alongMin;

        // Highest point of the cloth in each thin strip across the ridge: its top surface.
        List<Vector2> profile = points
            .GroupBy(p => Mathf.RoundToInt(Across(p) / Bin))
            .Select(strip => new Vector2(strip.Key * Bin, strip.Max(p => p.y)))
            .OrderBy(point => point.x)
            .ToList();
        List<Vector2> outline = Simplify(profile, Tolerance);

        GameObject holder = new(ObjectName) { layer = layer };
        holder.transform.SetParent(root, false);
        Vector3 ridge = alongZ ? Vector3.forward : Vector3.right;
        float ridgeHeight = outline.Max(point => point.y);
        float ridgeAcross = outline.First(point => Mathf.Approximately(point.y, ridgeHeight)).x;
        bool steepRidge = false;
        int count = 0;
        for (int i = 0; i + 1 < outline.Count; i++)
        {
            Vector2 a = outline[i];
            Vector2 b = outline[i + 1];
            Vector2 segment = b - a;
            if (segment.magnitude < MinSegment)
            {
                continue;
            }

            Vector3 from = Point(alongZ, a.x, a.y, centre);
            Vector3 to = Point(alongZ, b.x, b.y, centre);
            Vector3 up = Vector3.Cross(to - from, ridge).normalized;
            if (up.y < 0f)
            {
                up = -up;
            }

            AddBox(holder.transform, $"piece{count++}", (from + to) / 2f - up * (Thickness / 2f), Quaternion.LookRotation(ridge, up),
                new Vector3(segment.magnitude, Thickness, length));
            float slope = Mathf.Atan2(Mathf.Abs(segment.y), Mathf.Abs(segment.x)) * Mathf.Rad2Deg;
            bool touchesRidge = Mathf.Min(Mathf.Abs(a.x - ridgeAcross), Mathf.Abs(b.x - ridgeAcross)) < NearRidge;
            steepRidge |= touchesRidge && slope > MaxStandableSlope;
        }

        if (steepRidge)
        {
            AddBox(holder.transform, "walkway", Point(alongZ, ridgeAcross, ridgeHeight, centre) - Vector3.up * (Thickness / 2f),
                Quaternion.LookRotation(ridge, Vector3.up), new Vector3(WalkwayWidth, Thickness, length));
        }

        Jotunn.Logger.LogInfo($"White Hilt Ship: tent colliders, {count} pieces{(steepRidge ? " and a walkway" : string.Empty)}, " +
            $"ridge {ridgeHeight:0.00} m high and {length:0.00} m long {(alongZ ? "along" : "across")} the ship");
        holder.SetActive(false);
    }

    private static Vector3 Point(bool alongZ, float across, float height, float along)
    {
        return alongZ ? new Vector3(across, height, along) : new Vector3(along, height, across);
    }

    // How much the top of the cloth rises and falls when walking along an axis.
    private static float HeightChange(List<Vector3> points, bool alongZ)
    {
        return points
            .GroupBy(p => Mathf.RoundToInt((alongZ ? p.x : p.z) / Bin))
            .Where(strip => strip.Count() > 1)
            .Select(strip => strip.Max(p => p.y) - strip.Min(p => p.y))
            .DefaultIfEmpty(0f)
            .Average();
    }

    private static void AddBox(Transform holder, string name, Vector3 centre, Quaternion rotation, Vector3 size)
    {
        GameObject box = new(name) { layer = holder.gameObject.layer };
        box.transform.SetParent(holder, false);
        box.transform.localPosition = centre;
        box.transform.localRotation = rotation;
        box.AddComponent<BoxCollider>().size = size;
    }

    // Douglas-Peucker: keeps the points that bend the line by more than the tolerance.
    private static List<Vector2> Simplify(List<Vector2> points, float tolerance)
    {
        if (points.Count < 3)
        {
            return points;
        }

        Vector2 first = points[0];
        Vector2 last = points[points.Count - 1];
        int farthest = 0;
        float farthestDistance = 0f;
        for (int i = 1; i < points.Count - 1; i++)
        {
            float distance = DistanceToLine(points[i], first, last);
            if (distance > farthestDistance)
            {
                farthest = i;
                farthestDistance = distance;
            }
        }

        if (farthestDistance <= tolerance)
        {
            return new List<Vector2> { first, last };
        }

        List<Vector2> left = Simplify(points.GetRange(0, farthest + 1), tolerance);
        List<Vector2> right = Simplify(points.GetRange(farthest, points.Count - farthest), tolerance);
        return left.Take(left.Count - 1).Concat(right).ToList();
    }

    private static float DistanceToLine(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 line = b - a;
        float lengthSquared = line.sqrMagnitude;
        if (lengthSquared < 1e-8f)
        {
            return Vector2.Distance(point, a);
        }

        float t = Mathf.Clamp01(Vector2.Dot(point - a, line) / lengthSquared);
        return Vector2.Distance(point, a + t * line);
    }
}
