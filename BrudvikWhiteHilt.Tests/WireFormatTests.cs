using BrudvikWhiteHilt.Difficulty;
using BrudvikWhiteHilt.Navigation.Areas;
using BrudvikWhiteHilt.Navigation.Portraits;
using BrudvikWhiteHilt.Painting;
using System.Collections.Generic;
using UnityEngine;
using Xunit;

namespace BrudvikWhiteHilt.Tests;

public class PaintColorTests
{
    [Theory]
    [InlineData(PaintMode.Stain)]
    [InlineData(PaintMode.Paint)]
    public void PackedColoursReadBackTheSame(PaintMode mode)
    {
        Color32 colour = new(142, 43, 34, 255);

        Assert.True(PaintColor.Unpack(PaintColor.Pack(colour, mode), out Color32 read, out PaintMode readMode));
        Assert.Equal(colour, read);
        Assert.Equal(mode, readMode);
    }

    [Fact]
    public void BlackIsStillPainted()
    {
        // 0 is what an unpainted piece has in its ZDO, so no colour may pack to it.
        Assert.NotEqual(0, PaintColor.Pack(new Color32(0, 0, 0, 255), PaintMode.Stain));
        Assert.False(PaintColor.Unpack(0, out _, out _));
    }

    [Theory]
    [InlineData("8E2B22")]
    [InlineData("#8e2b22")]
    [InlineData("  #8E2B22 ")]
    public void HexIsReadWithOrWithoutHash(string text)
    {
        Assert.True(PaintColor.TryParseHex(text, out Color32 colour));
        Assert.Equal("8E2B22", PaintColor.ToHex(colour));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("8E2B2")]
    [InlineData("8E2B22FF")]
    [InlineData("GGGGGG")]
    [InlineData("-12345")]
    public void BrokenHexIsRejected(string text)
    {
        Assert.False(PaintColor.TryParseHex(text, out _));
    }
}

public class PortraitStoreTests
{
    [Fact]
    public void PortraitsSurviveCompression()
    {
        byte[] raw = new byte[PortraitStore.RawLength];
        for (int i = 0; i < raw.Length; i++)
        {
            raw[i] = (byte)(i * 7 % 251);
        }

        byte[] data = PortraitStore.Compress(raw);

        Assert.Equal(raw, PortraitStore.Decompress(data));
        Assert.Equal(PortraitStore.Hash(raw), PortraitStore.Hash(PortraitStore.Decompress(data)));
    }

    [Fact]
    public void PortraitsOfTheWrongSizeAreRefused()
    {
        // Another player's portrait is only accepted when it has exactly the size the game draws.
        Assert.Null(PortraitStore.Decompress(PortraitStore.Compress(new byte[PortraitStore.RawLength - 4])));
        Assert.Null(PortraitStore.Decompress(PortraitStore.Compress(new byte[PortraitStore.RawLength + 4])));
        Assert.Null(PortraitStore.Decompress(new byte[] { 1, 2, 3, 4, 5 }));
        Assert.Null(PortraitStore.Decompress(new byte[PortraitStore.MaxCompressed + 1]));
        Assert.Null(PortraitStore.Decompress(null));
    }

    [Fact]
    public void HashesAreLowerCaseSha1()
    {
        string hash = PortraitStore.Hash(new byte[] { 97, 98, 99 });

        Assert.Equal("a9993e364706816aba3e25717850c26c9cd0d89d", hash);
        Assert.True(PortraitStore.IsHash(hash));
        Assert.False(PortraitStore.IsHash(hash.ToUpperInvariant()));
        Assert.False(PortraitStore.IsHash("../" + hash.Substring(3)));
        Assert.False(PortraitStore.IsHash(null));
    }
}

public class MapAreaPackageTests
{
    [Fact]
    public void AreasAndWardsReadBackTheSame()
    {
        MapArea area = new()
        {
            Kind = MapAreaKind.Base,
            Creator = 1234567890123L,
            Builder = "Astrid",
            Name = "Ravnholt",
            Count = 412,
            Cells = new List<Vector2Int> { new(0, 0), new(-1, 5), new(-700, -650), new(640, 655) }
        };
        MapWard ward = new() { Position = new Vector3(-10240.5f, 33f, 512.25f), Radius = 32f, Enabled = true, Creator = 42L };

        ZPackage written = MapAreaPackage.Write(new List<MapArea> { area }, new List<MapWard> { ward });
        Assert.True(MapAreaPackage.Read(new ZPackage(written.GetArray()), out List<MapArea> areas, out List<MapWard> wards));

        MapArea read = Assert.Single(areas);
        Assert.Equal(area.Kind, read.Kind);
        Assert.Equal(area.Creator, read.Creator);
        Assert.Equal(area.Builder, read.Builder);
        Assert.Equal(area.Name, read.Name);
        Assert.Equal(area.Count, read.Count);
        Assert.Equal(area.Cells, read.Cells);

        MapWard readWard = Assert.Single(wards);
        Assert.Equal(ward.Position.x, readWard.Position.x);
        Assert.Equal(ward.Position.z, readWard.Position.z);
        Assert.Equal(ward.Radius, readWard.Radius);
        Assert.True(readWard.Enabled);
        Assert.Equal(ward.Creator, readWard.Creator);
    }

    [Fact]
    public void APackageOfAnotherVersionIsIgnored()
    {
        ZPackage package = new();
        package.Write(99);

        Assert.False(MapAreaPackage.Read(new ZPackage(package.GetArray()), out List<MapArea> areas, out _));
        Assert.Empty(areas);
    }

    [Fact]
    public void TheCentreIsTheMiddleOfTheCells()
    {
        MapArea area = new() { Cells = new List<Vector2Int> { new(0, 0), new(2, 0) } };

        Vector3 centre = area.Centre();

        Assert.Equal(1.5f * MapArea.CellSize, centre.x, 3);
        Assert.Equal(0.5f * MapArea.CellSize, centre.z, 3);
    }
}

public class DifficultyStateTests
{
    [Fact]
    public void TheStateReadsBackTheSameAndIsClamped()
    {
        try
        {
            DifficultyState.Set(1.4f, 0.37f, true, 0, 3, 120, 2);
            ZPackage written = DifficultyState.Write();
            DifficultyState.Reset();
            DifficultyState.Read(new ZPackage(written.GetArray()));

            Assert.Equal(1f, DifficultyState.Pressure);
            Assert.Equal(0.37f, DifficultyState.RawPressure);
            Assert.True(DifficultyState.BloodMoon);
            Assert.Equal(1, DifficultyState.Biomes);
            Assert.Equal(3, DifficultyState.Players);
            Assert.Equal(120, DifficultyState.Days);
            Assert.Equal(2, DifficultyState.GearPlayers);
        }
        finally
        {
            DifficultyState.Reset();
        }
    }

    [Theory]
    [InlineData(5.99f, true)]
    [InlineData(6f, false)]
    [InlineData(12f, false)]
    [InlineData(17.99f, false)]
    [InlineData(18f, true)]
    [InlineData(0f, true)]
    public void NightIsFromSixInTheEveningToSixInTheMorning(float hours, bool night)
    {
        Assert.Equal(night, DifficultyState.IsNightHour(hours));
    }
}
