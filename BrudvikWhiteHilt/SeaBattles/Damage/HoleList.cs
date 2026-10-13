using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BrudvikWhiteHilt.SeaBattles.Damage;

/// <summary>
/// The holes of a ship as bytes for its ZDO: a 32-bit count, then three 32-bit floats (x, y, z in the ship's local
/// space) for each hole, little-endian, as the game's <c>ZPackage</c> writes them. It is plain bytes so it can be tested
/// without the game, and so a broken or hostile value in the ZDO can never make more than <see cref="Limit"/> holes.
/// </summary>
public static class HoleList
{
    /// <summary>The most holes a list can hold, whatever the setting says; the setting's top value.</summary>
    public const int Limit = 20;

    private const int Header = sizeof(int);
    private const int PointSize = 3 * sizeof(float);

    /// <summary>
    /// Writes the holes.
    /// </summary>
    /// <param name="holes">The holes, in the ship's local space.</param>
    /// <returns>The bytes; only the first <see cref="Limit"/> holes are written.</returns>
    public static byte[] Encode(IReadOnlyList<Vector3> holes)
    {
        int count = holes == null ? 0 : Mathf.Min(holes.Count, Limit);
        using MemoryStream stream = new(Header + count * PointSize);
        using BinaryWriter writer = new(stream);
        writer.Write(count);
        for (int i = 0; i < count; i++)
        {
            writer.Write(holes[i].x);
            writer.Write(holes[i].y);
            writer.Write(holes[i].z);
        }

        writer.Flush();
        return stream.ToArray();
    }

    /// <summary>
    /// Reads holes. Anything that does not parse gives no holes, and a count larger than the bytes hold or the limit is cut.
    /// </summary>
    /// <param name="data">The bytes, or null.</param>
    /// <returns>The holes; empty for null, short or broken data, or one that holds a point that is not a number.</returns>
    public static List<Vector3> Decode(byte[] data)
    {
        List<Vector3> holes = new();
        if (data == null || data.Length < Header)
        {
            return holes;
        }

        try
        {
            using MemoryStream stream = new(data, false);
            using BinaryReader reader = new(stream);
            int count = Mathf.Min(reader.ReadInt32(), Limit, (data.Length - Header) / PointSize);
            for (int i = 0; i < count; i++)
            {
                Vector3 point = new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                if (!IsFinite(point))
                {
                    return new List<Vector3>();
                }

                holes.Add(point);
            }
        }
        catch (IOException)
        {
            return new List<Vector3>();
        }

        return holes;
    }

    /// <summary>
    /// Whether two byte arrays hold the same bytes; null equals only null.
    /// </summary>
    /// <param name="a">One array.</param>
    /// <param name="b">The other.</param>
    /// <returns>True if they are equal.</returns>
    public static bool SameBytes(byte[] a, byte[] b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a == null || b == null || a.Length != b.Length)
        {
            return false;
        }

        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i])
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsFinite(Vector3 point)
    {
        return !float.IsNaN(point.x + point.y + point.z) && !float.IsInfinity(point.x + point.y + point.z);
    }
}
