using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BrudvikWhiteHilt.SeaBattles.Damage;

/// <summary>
/// One fire on a ship's deck: where it burns, in the ship's local space, and the world time it goes out.
/// </summary>
public readonly struct DeckFire
{
    /// <summary>Creates a fire.</summary>
    /// <param name="point">Where it burns, in the ship's local space.</param>
    /// <param name="expiry">World ticks (see <c>ZNet.GetTime</c>) at which it burns out.</param>
    public DeckFire(Vector3 point, long expiry)
    {
        Point = point;
        Expiry = expiry;
    }

    /// <summary>Where the fire burns, in the ship's local space.</summary>
    public Vector3 Point { get; }

    /// <summary>World ticks at which the fire burns out.</summary>
    public long Expiry { get; }
}

/// <summary>
/// The deck fires of a ship as bytes for its ZDO, and the list operations on them, from plain values so they can be tested
/// without the game. The bytes are a 32-bit count, then for each fire three 32-bit floats (x, y, z) and a 64-bit expiry,
/// little-endian, as the game's <c>ZPackage</c> writes them. A broken or hostile value in the ZDO can never make more than
/// <see cref="Limit"/> fires.
/// </summary>
public static class FireList
{
    /// <summary>The most fires a list can hold, whatever the setting says; the setting's top value.</summary>
    public const int Limit = 20;

    private const int Header = sizeof(int);
    private const int FireSize = 3 * sizeof(float) + sizeof(long);

    /// <summary>
    /// Writes the fires.
    /// </summary>
    /// <param name="fires">The fires.</param>
    /// <returns>The bytes; only the first <see cref="Limit"/> fires are written.</returns>
    public static byte[] Encode(IReadOnlyList<DeckFire> fires)
    {
        int count = fires == null ? 0 : Math.Min(fires.Count, Limit);
        using MemoryStream stream = new(Header + count * FireSize);
        using BinaryWriter writer = new(stream);
        writer.Write(count);
        for (int i = 0; i < count; i++)
        {
            writer.Write(fires[i].Point.x);
            writer.Write(fires[i].Point.y);
            writer.Write(fires[i].Point.z);
            writer.Write(fires[i].Expiry);
        }

        writer.Flush();
        return stream.ToArray();
    }

    /// <summary>
    /// Reads fires. Anything that does not parse gives no fires, and a count larger than the bytes hold or the limit is cut.
    /// </summary>
    /// <param name="data">The bytes, or null.</param>
    /// <returns>The fires; empty for null, short or broken data, or one that holds a point that is not a number.</returns>
    public static List<DeckFire> Decode(byte[] data)
    {
        List<DeckFire> fires = new();
        if (data == null || data.Length < Header)
        {
            return fires;
        }

        try
        {
            using MemoryStream stream = new(data, false);
            using BinaryReader reader = new(stream);
            int count = Math.Min(reader.ReadInt32(), Math.Min(Limit, (data.Length - Header) / FireSize));
            for (int i = 0; i < count; i++)
            {
                Vector3 point = new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                long expiry = reader.ReadInt64();
                if (!IsFinite(point))
                {
                    return new List<DeckFire>();
                }

                fires.Add(new DeckFire(point, expiry));
            }
        }
        catch (IOException)
        {
            return new List<DeckFire>();
        }

        return fires;
    }

    /// <summary>
    /// Takes the fires that have burnt out off a list.
    /// </summary>
    /// <param name="fires">The fires; changed in place.</param>
    /// <param name="now">World ticks now. A fire burns out when its expiry is not after this.</param>
    /// <returns>How many fires were taken off.</returns>
    public static int Prune(List<DeckFire> fires, long now)
    {
        return fires.RemoveAll(fire => fire.Expiry <= now);
    }

    /// <summary>
    /// Makes every fire burn out earlier, as rain does.
    /// </summary>
    /// <param name="fires">The fires; changed in place.</param>
    /// <param name="ticks">World ticks to take off each expiry; 0 or less changes nothing.</param>
    public static void Shorten(List<DeckFire> fires, long ticks)
    {
        if (ticks <= 0L)
        {
            return;
        }

        for (int i = 0; i < fires.Count; i++)
        {
            fires[i] = new DeckFire(fires[i].Point, fires[i].Expiry - ticks);
        }
    }

    /// <summary>
    /// Lights a fire. One lit within the spacing of another feeds it: that one burns for the new time instead, if that is
    /// longer, and no new fire is made. A new fire is made only while the list is below the cap.
    /// </summary>
    /// <param name="fires">The fires; changed in place.</param>
    /// <param name="point">Where the new fire is, in the ship's local space.</param>
    /// <param name="expiry">World ticks at which it burns out.</param>
    /// <param name="max">The most fires the ship can have.</param>
    /// <param name="spacing">Metres within which two fires are one.</param>
    /// <returns>True if the list changed: a new fire, or an old one burning longer.</returns>
    public static bool Add(List<DeckFire> fires, Vector3 point, long expiry, int max, float spacing)
    {
        int near = -1;
        float nearest = spacing * spacing;
        for (int i = 0; i < fires.Count; i++)
        {
            float squared = (fires[i].Point - point).sqrMagnitude;
            if (squared <= nearest)
            {
                near = i;
                nearest = squared;
            }
        }

        if (near >= 0)
        {
            if (fires[near].Expiry >= expiry)
            {
                return false;
            }

            fires[near] = new DeckFire(fires[near].Point, expiry);
            return true;
        }

        if (fires.Count >= Math.Min(Math.Max(0, max), Limit))
        {
            return false;
        }

        fires.Add(new DeckFire(point, expiry));
        return true;
    }

    /// <summary>
    /// The points of the fires.
    /// </summary>
    /// <param name="fires">The fires.</param>
    /// <returns>Their points, in the same order.</returns>
    public static List<Vector3> Points(IReadOnlyList<DeckFire> fires)
    {
        List<Vector3> points = new(fires.Count);
        for (int i = 0; i < fires.Count; i++)
        {
            points.Add(fires[i].Point);
        }

        return points;
    }

    private static bool IsFinite(Vector3 point)
    {
        float sum = point.x + point.y + point.z;
        return !float.IsNaN(sum) && !float.IsInfinity(sum);
    }
}
