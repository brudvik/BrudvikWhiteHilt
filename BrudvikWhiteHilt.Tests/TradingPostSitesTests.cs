using BrudvikWhiteHilt.TradingPosts;
using System.Collections.Generic;
using UnityEngine;
using Xunit;

namespace BrudvikWhiteHilt.Tests;

/// <summary>
/// The rules for where a trading post may stand, on made-up coasts.
/// </summary>
public class TradingPostSitesTests
{
    private const float Water = 30f;

    // Flat land a metre above the sea up to z = 2, then a seabed that drops 0.4 m per metre: 6.8 m of water 19 m out.
    private static float Shore(float x, float z)
    {
        return z <= 2f ? 31f : 31f - 0.4f * (z - 2f) - 1f;
    }

    /// <summary>A post facing the open sea fits, with its deck just above the water.</summary>
    [Fact]
    public void APostFacingTheSeaFits()
    {
        Assert.True(TradingPostSites.Evaluate(Vector2.zero, 0f, Shore, Water, out float deck, out float score));
        Assert.Equal(Water + TradingPostSites.MinFreeboard + 0.1f, deck, 3);
        Assert.True(score > 0f);
    }

    /// <summary>A post facing inland does not fit.</summary>
    [Fact]
    public void APostFacingInlandDoesNotFit()
    {
        Assert.False(TradingPostSites.Evaluate(Vector2.zero, 180f, Shore, Water, out _, out _));
    }

    /// <summary>The search finds the way to the sea.</summary>
    [Fact]
    public void TheSearchTurnsThePostToTheSea()
    {
        Assert.True(TradingPostSites.TryFind(Vector2.zero, Shore, Water, out float yaw, out _, out _));
        Assert.Equal(0f, yaw, 3);
    }

    /// <summary>Shallow water at the head is not enough for a ship.</summary>
    [Fact]
    public void ShallowWaterDoesNotFit()
    {
        static float Shallows(float x, float z) => z <= 2f ? 31f : 29f;
        Assert.False(TradingPostSites.TryFind(Vector2.zero, Shallows, Water, out _, out _, out _));
    }

    /// <summary>A cliff is too high for a jetty.</summary>
    [Fact]
    public void ACliffDoesNotFit()
    {
        static float Cliff(float x, float z) => z <= 2f ? 40f : 20f;
        Assert.False(TradingPostSites.TryFind(Vector2.zero, Cliff, Water, out _, out _, out _));
    }

    /// <summary>Land rising steeply behind the landing leaves no room for the house.</summary>
    [Fact]
    public void AHillsideBehindDoesNotFit()
    {
        static float Hillside(float x, float z) => z <= 2f ? 31f + Mathf.Max(0f, -z) * 0.8f : 31f - 0.4f * (z - 2f) - 1f;
        Assert.False(TradingPostSites.Evaluate(Vector2.zero, 0f, Hillside, Water, out _, out _));
    }

    /// <summary>Rough land behind the landing suits on the broken coasts, where the post levels its ground.</summary>
    [Fact]
    public void RoughLandSuitsWhereThePostLevelsIt()
    {
        static float Rough(float x, float z) => z <= 2f ? 31f + (x > 0f ? 4f : 0f) * Mathf.Min(1f, -z / 8f) : Shore(x, z);
        Assert.False(TradingPostSites.Evaluate(Vector2.zero, 0f, Rough, Water, out _, out _));
        Assert.True(TradingPostSites.Evaluate(Vector2.zero, 0f, Rough, Water, out _, out _, TradingPostSites.Rugged));
    }

    /// <summary>The biomes behind a post are found looking inland, nearest first.</summary>
    [Fact]
    public void TheBiomesBehindAreFoundInland()
    {
        static string Land(float x, float z) => z > 0f ? "Ocean" : z > -100f ? "Meadows" : "Swamp";
        Assert.Equal(new List<string> { "Meadows", "Swamp" }, TradingPostSites.Inland(Vector2.zero, 0f, Land));
        Assert.DoesNotContain("Swamp", TradingPostSites.Inland(Vector2.zero, 180f, Land));
    }

    /// <summary>A post turned to the east is judged in its own turned coordinates.</summary>
    [Fact]
    public void ATurnedShoreIsFoundToo()
    {
        static float EastShore(float x, float z) => Shore(-z, x);
        Assert.True(TradingPostSites.TryFind(Vector2.zero, EastShore, Water, out float yaw, out _, out _));
        Assert.Equal(90f, yaw, 3);
        Vector2 head = TradingPostSites.ToWorld(Vector2.zero, yaw, new Vector2(0f, 19f));
        Assert.Equal(19f, head.x, 3);
        Assert.Equal(0f, head.y, 3);
    }

    /// <summary>Picks keep their distance from each other and from the posts already standing.</summary>
    [Fact]
    public void PicksKeepTheirDistance()
    {
        List<Vector2> taken = new() { new Vector2(0f, 0f) };
        List<Vector2> candidates = new() { new(100f, 0f), new(1500f, 0f), new(1600f, 0f), new(3000f, 0f), new(0f, 3000f) };
        List<Vector2> picks = TradingPostSites.Pick(candidates, taken, 3, 1000f, new System.Random(7));

        Assert.Equal(3, picks.Count);
        Assert.DoesNotContain(new Vector2(100f, 0f), picks);
        for (int i = 0; i < taken.Count; i++)
        {
            for (int j = i + 1; j < taken.Count; j++)
            {
                Assert.True(Vector2.Distance(taken[i], taken[j]) >= 1000f);
            }
        }
    }
}
