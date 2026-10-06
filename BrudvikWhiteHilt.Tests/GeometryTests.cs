using BrudvikWhiteHilt.Navigation.Compass;
using BrudvikWhiteHilt.Treasure;
using UnityEngine;
using Xunit;

namespace BrudvikWhiteHilt.Tests;

public class CompassTests
{
    [Theory]
    [InlineData(0f, 10f, 0f)]
    [InlineData(10f, 0f, 90f)]
    [InlineData(0f, -10f, 180f)]
    [InlineData(-10f, 0f, 270f)]
    [InlineData(10f, 10f, 45f)]
    public void BearingsRunClockwiseFromNorth(float x, float z, float bearing)
    {
        Assert.Equal(bearing, CompassGeometry.Bearing(new Vector3(5f, 3f, 5f), new Vector3(5f + x, 40f, 5f + z)), 3);
    }

    [Theory]
    [InlineData(-90f, 270f)]
    [InlineData(360f, 0f)]
    [InlineData(725f, 5f)]
    public void AnglesAreNormalizedToOneTurn(float angle, float normalized)
    {
        Assert.Equal(normalized, CompassGeometry.Normalize(angle), 3);
    }

    [Fact]
    public void ATargetAcrossNorthIsProjectedTheShortWay()
    {
        // Heading 350 and target 10 are 20 degrees apart to the right, not 340 to the left.
        Assert.True(CompassGeometry.Project(350f, 10f, 400f, 160f, out float position));
        Assert.Equal(20f * 400f / 160f, position, 3);
    }

    [Fact]
    public void ATargetOutsideTheVisibleDegreesIsNotShown()
    {
        Assert.False(CompassGeometry.Project(0f, 100f, 400f, 160f, out _));
        Assert.True(CompassGeometry.Project(0f, -80f, 400f, 160f, out float edge));
        Assert.Equal(-200f, edge, 3);
    }
}

public class TreasureMapTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void MapAndWorldPositionsAreInverses(int rotation)
    {
        TreasureSite site = new() { Centre = new Vector2(-1200f, 830f), Size = 500f, Rotation = rotation };
        Vector2 world = new(-1100f, 700f);

        Vector2 map = TreasureMapRenderer.WorldToMap(site, world);
        Vector2 back = TreasureMapRenderer.MapToWorld(site, map.x, map.y);

        Assert.Equal(world.x, back.x, 2);
        Assert.Equal(world.y, back.y, 2);
    }

    [Theory]
    [InlineData(0, 0f, 1f)]
    [InlineData(1, 1f, 0f)]
    [InlineData(2, 0f, -1f)]
    [InlineData(3, -1f, 0f)]
    public void NorthTurnsWithTheMap(int rotation, float x, float y)
    {
        TreasureSite site = new() { Size = 500f, Rotation = rotation };
        Vector2 north = TreasureMapRenderer.North(site);
        Vector2 centre = TreasureMapRenderer.WorldToMap(site, Vector2.zero);
        Vector2 ahead = TreasureMapRenderer.WorldToMap(site, new Vector2(0f, 100f)) - centre;

        // Turning the map by quarters must keep north where the north arrow points.
        Assert.Equal(x, north.x, 3);
        Assert.Equal(y, north.y, 3);
        Assert.Equal(north.x, ahead.normalized.x, 3);
        Assert.Equal(north.y, ahead.normalized.y, 3);
    }

    [Fact]
    public void TheCentreIsInTheMiddleOfTheMap()
    {
        TreasureSite site = new() { Centre = new Vector2(300f, -40f), Size = 800f, Rotation = 3 };
        Vector2 map = TreasureMapRenderer.WorldToMap(site, site.Centre);

        Assert.Equal(0.5f, map.x, 3);
        Assert.Equal(0.5f, map.y, 3);
    }

    [Theory]
    [InlineData(400f, 100)]
    [InlineData(600f, 100)]
    [InlineData(800f, 200)]
    public void TheScaleBarFitsTheMap(float size, int metres)
    {
        Assert.Equal(metres, TreasureMapRenderer.ScaleMetres(new TreasureSite { Size = size }));
    }
}
