using System.Collections.Generic;
using System.Runtime.InteropServices;
using BrudvikWhiteHilt.Performance;
using UnityEngine;
using Xunit;

namespace BrudvikWhiteHilt.Tests;

/// <summary>
/// Checks that the stand-ins for vanilla code give exactly vanilla's answer.
/// </summary>
public class VanillaFixTests
{
    /// <summary>
    /// A nested package must be written byte for byte as <c>ZPackage.Write(ZPackage)</c> writes it, whatever the inner
    /// package's read position and however it was filled.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(255)]
    [InlineData(70000)]
    public void NestedPackageIsWrittenAsVanillaWritesIt(int bytes)
    {
        ZPackage inner = new();
        for (int i = 0; i < bytes; i++)
        {
            inner.Write((byte)(i * 31));
        }

        inner.Write("tail");
        inner.Write(new Vector3(1.5f, -2f, 3.25f));
        inner.SetPos(0);
        inner.ReadByte();

        ZPackage vanilla = new();
        vanilla.Write(42);
        vanilla.Write(inner);
        vanilla.Write("after");

        ZPackage ours = new();
        ours.Write(42);
        Assert.True(PackageWriter.WriteNested(ours, inner));
        ours.Write("after");

        Assert.Equal(vanilla.GetArray(), ours.GetArray());
    }

    /// <summary>
    /// A package built from received bytes (the constructor that copies an array) is written the same way too.
    /// </summary>
    [Fact]
    public void ReceivedPackageIsWrittenAsVanillaWritesIt()
    {
        ZPackage inner = new(new byte[] { 1, 2, 3, 4, 5, 6, 7 }, 5);
        ZPackage vanilla = new();
        vanilla.Write(inner);
        ZPackage ours = new();
        Assert.True(PackageWriter.WriteNested(ours, inner));
        Assert.Equal(vanilla.GetArray(), ours.GetArray());
    }

    /// <summary>
    /// A package is never written into itself by the stand-in; vanilla keeps that case.
    /// </summary>
    [Fact]
    public void PackageIntoItselfIsLeftToVanilla()
    {
        ZPackage package = new();
        package.Write(7);
        Assert.False(PackageWriter.WriteNested(package, package));
    }

    /// <summary>
    /// Setting a ZDO value that exists answers "changed" and stores the value exactly as vanilla's boxed compare does,
    /// a NaN replacing a NaN included.
    /// </summary>
    [Fact]
    public void ZdoFloatWritesMatchVanilla()
    {
        float[] values = { 1f, 1f, 2f, float.NaN, float.NaN, 0f, -0f, float.PositiveInfinity, 2f };
        AssertSameWrites(values);
    }

    /// <summary>
    /// The same for the other value types the stand-in serves.
    /// </summary>
    [Fact]
    public void ZdoVectorIntAndLongWritesMatchVanilla()
    {
        AssertSameWrites(new[] { Vector3.one, Vector3.one, Vector3.zero, new Vector3(float.NaN, 0f, 0f), new Vector3(float.NaN, 0f, 0f) });
        AssertSameWrites(new[] { Quaternion.identity, Quaternion.identity, new Quaternion(0f, 1f, 0f, 0f) });
        AssertSameWrites(new[] { 5, 5, 6, int.MinValue, int.MinValue });
        AssertSameWrites(new[] { 5L, 5L, long.MaxValue, 0L });
    }

    /// <summary>
    /// The active-area distance is the largest axis distance, as <c>Utils.ChebyshevDistance</c> finds it.
    /// </summary>
    [Theory]
    [InlineData(0f, 0f, 0f, 0f, 0f, 0f)]
    [InlineData(1f, -5f, 3f, 4f, 2f, -9f)]
    [InlineData(float.NaN, 0f, 0f, 1f, 2f, 3f)]
    [InlineData(0f, float.NaN, 0f, 10f, 2f, 3f)]
    [InlineData(-1000f, 20f, 4000f, 1000f, -20f, -4000f)]
    public void ZoneDistanceMatchesVanilla(float ax, float ay, float az, float bx, float by, float bz)
    {
        Vector3 a = new(ax, ay, az);
        Vector3 b = new(bx, by, bz);
        Assert.Equal(global::Utils.ChebyshevDistance(a, b), FrameGarbage.ChebyshevDistance(a, b));
    }

    // Writes each value to the same key in two tables, one through vanilla and one through the stand-in where the key
    // already exists, and compares every answer and the final value.
    private static void AssertSameWrites<T>(IEnumerable<T> values) where T : struct, System.IEquatable<T>
    {
        ZDOID zid = new(7L, 3u);
        const int hash = 1234;
        var vanilla = new Dictionary<ZDOID, BinarySearchDictionary<int, T>>();
        var ours = new Dictionary<ZDOID, BinarySearchDictionary<int, T>>();
        foreach (T value in values)
        {
            bool expected = vanilla.InitAndSet(zid, hash, value);
            bool actual = ZdoValueWrites.TrySetExisting(ours, zid, hash, value, out bool changed)
                ? changed
                : ours.InitAndSet(zid, hash, value);
            Assert.Equal(expected, actual);
            Assert.True(vanilla[zid].TryGetValue(hash, out T kept));
            Assert.True(ours[zid].TryGetValue(hash, out T stored));
            Assert.Equal(Bits(kept), Bits(stored));
        }
    }

    // The value's bytes, so a NaN compares equal to the same NaN.
    private static byte[] Bits<T>(T value) where T : struct
    {
        int size = Marshal.SizeOf<T>();
        System.IntPtr memory = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(value, memory, false);
            byte[] bytes = new byte[size];
            Marshal.Copy(memory, bytes, 0, size);
            return bytes;
        }
        finally
        {
            Marshal.FreeHGlobal(memory);
        }
    }
}
