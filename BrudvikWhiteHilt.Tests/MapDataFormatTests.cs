using System;
using System.Collections;
using BrudvikWhiteHilt.Performance;
using Xunit;

namespace BrudvikWhiteHilt.Tests;

/// <summary>
/// The map body built from packed bits must be byte for byte what the game's <c>Minimap.GetMapData</c> writes one bool
/// at a time, or the character file would change.
/// </summary>
public class MapDataFormatTests
{
    [Theory]
    [InlineData(64, 1)]
    [InlineData(100, 2)]
    [InlineData(37, 3)]
    [InlineData(256, 4)]
    public void BodyMatchesTheGamesBoolByBoolWrite(int size, int seed)
    {
        Random random = new Random(seed);
        BitArray own = Pattern(size * size, random);
        BitArray shared = Pattern(size * size, random);

        byte[] expected = GameBody(size, own, shared, out byte[] tail);
        byte[] actual = MapDataFormat.Body(size, MapDataFormat.Words(own), MapDataFormat.Words(shared), own.Length, tail);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WordsClearTheBitsPastTheEnd()
    {
        BitArray bits = new BitArray(40, true);
        int[] words = MapDataFormat.Words(bits);

        Assert.Equal(2, words.Length);
        Assert.Equal(-1, words[0]);
        Assert.Equal(0xFF, words[1]);
    }

    [Fact]
    public void ChangedBitIsNotTheSame()
    {
        BitArray bits = new BitArray(1000);
        int[] before = MapDataFormat.Words(bits);
        bits[999] = true;

        Assert.False(MapDataFormat.Same(before, MapDataFormat.Words(bits)));
        Assert.True(MapDataFormat.Same(before, MapDataFormat.Words(new BitArray(1000))));
    }

    // Runs of clear and set words, as an explored map has, with some scattered bits between.
    private static BitArray Pattern(int length, Random random)
    {
        BitArray bits = new BitArray(length);
        int i = 0;
        while (i < length)
        {
            int run = random.Next(1, 300);
            int kind = random.Next(3);
            for (int k = 0; k < run && i < length; k++, i++)
            {
                bits[i] = kind == 1 || (kind == 2 && random.Next(2) == 0);
            }
        }

        return bits;
    }

    // What the game writes: the size, each own bit, each shared bit, then the pins, all into one package.
    private static byte[] GameBody(int size, BitArray own, BitArray shared, out byte[] tail)
    {
        ZPackage game = new ZPackage();
        game.Write(size);
        for (int i = 0; i < own.Length; i++)
        {
            game.Write(own[i]);
        }

        for (int i = 0; i < own.Length; i++)
        {
            game.Write(shared[i]);
        }

        ZPackage pins = new ZPackage();
        foreach (ZPackage package in new[] { game, pins })
        {
            package.Write(1);
            package.Write("Home");
            package.Write(3);
            package.Write(true);
            package.Write(12345678L);
            package.Write("Steam_1");
            package.Write(false);
        }

        tail = pins.GetArray();
        return game.GetArray();
    }
}
