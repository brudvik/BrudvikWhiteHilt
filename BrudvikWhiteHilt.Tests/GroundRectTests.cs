using BrudvikWhiteHilt.Building.Terrain;
using UnityEngine;
using Xunit;

namespace BrudvikWhiteHilt.Tests;

public class GroundRectTests
{
    [Fact]
    public void AlongTheAxesItIsTheOldRectangle()
    {
        GroundRect rect = GroundRect.Towards(new Vector3(10f, 5f, 20f), 0f, new Vector3(13.4f, 0f, 16.6f));

        Assert.Equal(new Vector2(3f, -3f), rect.Size);
        Assert.Equal(0f, rect.OutsideDistance(12f, 18f));
        Assert.Equal(0f, rect.OutsideDistance(13f, 17f));
        Assert.Equal(1f, rect.OutsideDistance(14f, 18f), 4);
        rect.Bounds(3f, out Vector2 min, out Vector2 max);
        Assert.Equal(new Vector2(7f, 14f), min);
        Assert.Equal(new Vector2(16f, 23f), max);
    }

    [Fact]
    public void ATurnedRectangleFollowsItsHeading()
    {
        // Turned 90 degrees, its right axis points down the world's z and its forward along x.
        GroundRect rect = GroundRect.Towards(Vector3.zero, 90f, new Vector3(4f, 0f, -2f));

        Assert.Equal(2f, rect.Size.x, 4);
        Assert.Equal(4f, rect.Size.y, 4);
        Assert.Equal(0f, rect.OutsideDistance(2f, -1f), 4);
        Assert.True(rect.OutsideDistance(2f, 1f) > 0.9f);
        Assert.Equal(8f, rect.Area, 4);
    }

    [Fact]
    public void AFixedSizeGoesTowardsTheAim()
    {
        GroundRect rect = GroundRect.Fixed(Vector3.zero, 0f, new Vector3(-5f, 0f, 3f), 1f, 40f);

        Assert.Equal(new Vector2(-1f, 40f), rect.Size);
        Assert.Equal(new Vector2(1f, 40f), rect.Extent);
    }

    [Theory]
    [InlineData(0f, 1f, 0f, 0f, 4f)]
    [InlineData(1f, 0f, 0f, 3f, 0f)]
    [InlineData(-1f, 0.2f, 0f, -3f, 0f)]
    [InlineData(0.1f, -1f, 0f, 0f, -4f)]
    public void TheNextTileLiesAlongsideOnTheSideLookedTo(float lookX, float lookZ, float yaw, float cornerX, float cornerZ)
    {
        GroundRect rect = new(Vector3.zero, yaw, new Vector2(3f, 4f));

        GroundRect next = rect.Next(new Vector3(lookX, -0.5f, lookZ));

        Assert.Equal(cornerX, next.Corner.x, 4);
        Assert.Equal(cornerZ, next.Corner.z, 4);
        Assert.Equal(rect.Size, next.Size);
        Assert.Equal(0f, next.OutsideDistance(cornerX + 1.5f, cornerZ + 0.5f), 4);
    }

    [Fact]
    public void TheNextTileOfANegativeRectangleDoesNotOverlap()
    {
        GroundRect rect = new(Vector3.zero, 0f, new Vector2(-3f, 4f));

        GroundRect next = rect.Next(Vector3.left);

        Assert.Equal(-3f, next.Corner.x, 4);
        Assert.Equal(0f, next.OutsideDistance(-4.5f, 2f), 4);
        Assert.True(next.OutsideDistance(-1.5f, 2f) > 0.5f);
    }

    [Theory]
    [InlineData("40x40", 40f, 40f)]
    [InlineData("1 x 40", 1f, 40f)]
    [InlineData("16*8", 16f, 8f)]
    [InlineData("12", 12f, 12f)]
    [InlineData("2,6X3", 3f, 3f)]
    [InlineData("500x0", 200f, 1f)]
    public void SizesAreReadAsTyped(string text, float width, float length)
    {
        Assert.True(AreaPicker.TryParseSize(text, out Vector2 size));
        Assert.Equal(new Vector2(width, length), size);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("4x4x4")]
    [InlineData(null)]
    public void NonsenseIsNoSize(string text)
    {
        Assert.False(AreaPicker.TryParseSize(text, out _));
    }
}
