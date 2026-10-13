using System;
using System.IO;
using UnityEngine;

namespace BrudvikWhiteHilt.SeaBattles.Weapons;

/// <summary>
/// A grapnel rope as the bytes in the holding ship's ZDO: the other ship's id (user and number), where the rope is made fast
/// on each ship in that ship's local space, and the world ticks at which it was made fast. It is plain bytes so it can be
/// tested without the game; anything that does not parse is no rope.
/// </summary>
public readonly struct GrappleRecord
{
    // A long, an unsigned int, six floats and a long.
    private const int Size = sizeof(long) + sizeof(uint) + 6 * sizeof(float) + sizeof(long);

    /// <summary>Creates a record.</summary>
    /// <param name="otherUser">The user part of the other ship's id.</param>
    /// <param name="otherId">The number part of the other ship's id.</param>
    /// <param name="here">Where the rope is made fast on the holding ship, in its local space.</param>
    /// <param name="there">Where it is made fast on the other ship, in that ship's local space.</param>
    /// <param name="startTicks">World ticks when it was made fast.</param>
    public GrappleRecord(long otherUser, uint otherId, Vector3 here, Vector3 there, long startTicks)
    {
        OtherUser = otherUser;
        OtherId = otherId;
        Here = here;
        There = there;
        StartTicks = startTicks;
    }

    /// <summary>The bytes that mean no rope.</summary>
    public static byte[] None => Array.Empty<byte>();

    /// <summary>The user part of the other ship's id.</summary>
    public long OtherUser { get; }

    /// <summary>The number part of the other ship's id.</summary>
    public uint OtherId { get; }

    /// <summary>Where the rope is made fast on the holding ship, in its local space.</summary>
    public Vector3 Here { get; }

    /// <summary>Where it is made fast on the other ship, in that ship's local space.</summary>
    public Vector3 There { get; }

    /// <summary>World ticks when it was made fast.</summary>
    public long StartTicks { get; }

    /// <summary>
    /// Writes the record.
    /// </summary>
    /// <returns>The bytes.</returns>
    public byte[] Encode()
    {
        using MemoryStream stream = new(Size);
        using BinaryWriter writer = new(stream);
        writer.Write(OtherUser);
        writer.Write(OtherId);
        Write(writer, Here);
        Write(writer, There);
        writer.Write(StartTicks);
        writer.Flush();
        return stream.ToArray();
    }

    /// <summary>
    /// Reads a record.
    /// </summary>
    /// <param name="data">The bytes, or null.</param>
    /// <param name="record">The record; unset when there is none.</param>
    /// <returns>True if the bytes are a rope: whole, and with points that are numbers.</returns>
    public static bool TryDecode(byte[] data, out GrappleRecord record)
    {
        record = default;
        if (data == null || data.Length < Size)
        {
            return false;
        }

        try
        {
            using MemoryStream stream = new(data, false);
            using BinaryReader reader = new(stream);
            long user = reader.ReadInt64();
            uint id = reader.ReadUInt32();
            Vector3 here = ReadPoint(reader);
            Vector3 there = ReadPoint(reader);
            long start = reader.ReadInt64();
            if (user == 0L && id == 0u || !IsFinite(here) || !IsFinite(there))
            {
                return false;
            }

            record = new GrappleRecord(user, id, here, there, start);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }

    private static void Write(BinaryWriter writer, Vector3 point)
    {
        writer.Write(point.x);
        writer.Write(point.y);
        writer.Write(point.z);
    }

    private static Vector3 ReadPoint(BinaryReader reader)
    {
        return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }

    private static bool IsFinite(Vector3 point)
    {
        float sum = point.x + point.y + point.z;
        return !float.IsNaN(sum) && !float.IsInfinity(sum);
    }
}
