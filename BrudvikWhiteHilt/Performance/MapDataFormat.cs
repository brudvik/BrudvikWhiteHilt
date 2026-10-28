using System;
using System.Collections;

namespace BrudvikWhiteHilt.Performance;

/// <summary>
/// The uncompressed body of the map data the game saves in the character file (<c>Minimap.GetMapData</c>), built from
/// the explored maps' packed bits. The game writes the same bytes one bool at a time: the map's size, one byte (0 or
/// 1) per pixel the player uncovered, one byte per pixel other players shared, and then the pins. Eight million calls
/// on a 2048 map make a save hitch; copying 32 bits at a time does not.
/// </summary>
public static class MapDataFormat
{
    /// <summary>
    /// Copies a bit array into 32-bit words, with the bits past its end cleared so two copies compare equal.
    /// </summary>
    /// <param name="bits">The bits.</param>
    /// <returns>One int per 32 bits, lowest bit first.</returns>
    public static int[] Words(BitArray bits)
    {
        int[] words = new int[(bits.Length + 31) / 32];
        bits.CopyTo(words, 0);
        int rest = bits.Length & 31;
        if (rest != 0)
        {
            words[words.Length - 1] &= (int)((1u << rest) - 1u);
        }

        return words;
    }

    /// <summary>
    /// Builds the body the game compresses: the size as a little-endian int, <paramref name="count"/> bytes from the
    /// player's own bits, <paramref name="count"/> bytes from the shared bits, and the pins' bytes as they are.
    /// </summary>
    /// <param name="textureSize">The map's width in pixels.</param>
    /// <param name="own">The player's uncovered pixels, as <see cref="Words"/>.</param>
    /// <param name="shared">The shared pixels, as <see cref="Words"/>.</param>
    /// <param name="count">Pixels to write from each; the game writes as many as the player's own array holds.</param>
    /// <param name="tail">The pins and the last flag, written by the game's own ZPackage.</param>
    /// <returns>The body.</returns>
    public static byte[] Body(int textureSize, int[] own, int[] shared, int count, byte[] tail)
    {
        if (own.Length * 32 < count || shared.Length * 32 < count)
        {
            throw new ArgumentException("The explored maps are smaller than the pixels to write.");
        }

        byte[] body = new byte[4 + count * 2 + tail.Length];
        body[0] = (byte)textureSize;
        body[1] = (byte)(textureSize >> 8);
        body[2] = (byte)(textureSize >> 16);
        body[3] = (byte)(textureSize >> 24);
        Expand(own, count, body, 4);
        Expand(shared, count, body, 4 + count);
        Buffer.BlockCopy(tail, 0, body, 4 + count * 2, tail.Length);
        return body;
    }

    /// <summary>
    /// True when two arrays hold the same values; false when either is missing.
    /// </summary>
    /// <param name="a">One array.</param>
    /// <param name="b">The other.</param>
    /// <returns>Whether they are equal.</returns>
    public static bool Same(int[] a, int[] b)
    {
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

    /// <summary>
    /// True when two byte arrays hold the same values; false when either is missing.
    /// </summary>
    /// <param name="a">One array.</param>
    /// <param name="b">The other.</param>
    /// <returns>Whether they are equal.</returns>
    public static bool Same(byte[] a, byte[] b)
    {
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

    // One byte per bit. Most words of a map are all clear or all set, so those are filled without looking at each bit.
    private static void Expand(int[] words, int count, byte[] target, int offset)
    {
        int full = count / 32;
        for (int w = 0; w < full; w++)
        {
            int word = words[w];
            int at = offset + w * 32;
            if (word == 0)
            {
                continue;
            }

            if (word == -1)
            {
                for (int b = 0; b < 32; b++)
                {
                    target[at + b] = 1;
                }

                continue;
            }

            for (int b = 0; b < 32; b++)
            {
                target[at + b] = (byte)((word >> b) & 1);
            }
        }

        for (int i = full * 32; i < count; i++)
        {
            target[offset + i] = (byte)((words[i >> 5] >> (i & 31)) & 1);
        }
    }
}
