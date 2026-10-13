using BrudvikWhiteHilt.SeaBattles;
using BrudvikWhiteHilt.SeaBattles.Raids;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Xunit;

namespace BrudvikWhiteHilt.Tests;

public class RaidSitesTests
{
    private const float Water = 30f;

    // Land up to x = 0 (five metres above the sea), then a seabed that drops a quarter of a metre per metre: six metres
    // of water at x = 24.
    private static float Shore(float x, float z)
    {
        return x <= 0f ? 35f : 30f - 0.25f * x;
    }

    [Fact]
    public void GuardsStandEvenlyOnTheRingStartingAtThePhase()
    {
        List<Vector2> posts = RaidSites.GuardPosts(new Vector2(10f, 20f), 12f, 4, 0f, (_, _) => 40f, Water);

        Assert.Equal(4, posts.Count);
        Assert.All(posts, post => Assert.Equal(12f, Vector2.Distance(post, new Vector2(10f, 20f)), 3));
        // The first stands north of the hoard, the second east of it.
        Assert.Equal(10f, posts[0].x, 3);
        Assert.Equal(32f, posts[0].y, 3);
        Assert.Equal(22f, posts[1].x, 3);
        Assert.Equal(20f, posts[1].y, 3);
    }

    [Fact]
    public void ThePhaseTurnsTheWholeRing()
    {
        List<Vector2> posts = RaidSites.GuardPosts(Vector2.zero, 10f, 2, 90f, (_, _) => 40f, Water);

        Assert.Equal(10f, posts[0].x, 3);
        Assert.Equal(0f, posts[0].y, 3);
        Assert.Equal(-10f, posts[1].x, 3);
    }

    [Fact]
    public void AGuardInTheWaterIsPulledInUntilItIsDry()
    {
        // East of x = 8 is sea.
        List<Vector2> posts = RaidSites.GuardPosts(Vector2.zero, 12f, 4, 0f, (x, _) => x > 8f ? 20f : 40f, Water);

        Assert.Equal(4, posts.Count);
        // 12 m is wet, 9 m is wet, 6.75 m is dry.
        Assert.Equal(6.75f, posts[1].x, 3);
        Assert.Equal(12f, Vector2.Distance(posts[0], Vector2.zero), 3);
    }

    [Fact]
    public void AGuardWithNoDryGroundOnItsLineStandsAtTheHoard()
    {
        List<Vector2> posts = RaidSites.GuardPosts(new Vector2(5f, 5f), 12f, 4, 0f, (x, z) => Vector2.Distance(new Vector2(x, z), new Vector2(5f, 5f)) > 0.5f ? 20f : 40f, Water);

        Assert.All(posts, post => Assert.Equal(new Vector2(5f, 5f), post));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void NoGuardsGivesNoPosts(int count)
    {
        Assert.Empty(RaidSites.GuardPosts(Vector2.zero, 12f, count, 0f, (_, _) => 40f, Water));
    }

    [Fact]
    public void TheShipLiesInTheNearestWaterThatIsDeepEnough()
    {
        // From (-5, 0) the first deep water is 30 m east.
        bool found = RaidSites.TryFindShipPoint(new Vector2(-5f, 0f), 80f, Shore, Water, out Vector2 point);

        Assert.True(found);
        Assert.Equal(25f, point.x, 3);
        Assert.Equal(0f, point.y, 3);
        Assert.True(Shore(point.x, point.y) <= Water - RaidSites.ShipDepth);
    }

    [Fact]
    public void NoDeepWaterWithinTheSearchRadiusMeansNoSite()
    {
        bool found = RaidSites.TryFindShipPoint(new Vector2(-5f, 0f), 20f, Shore, Water, out Vector2 point);

        Assert.False(found);
        Assert.Equal(new Vector2(-5f, 0f), point);
    }

    [Fact]
    public void LandAllRoundMeansNoSite()
    {
        Assert.False(RaidSites.TryFindShipPoint(Vector2.zero, 200f, (_, _) => 40f, Water, out _));
    }

    [Fact]
    public void ShallowWaterIsNotDeepEnough()
    {
        Assert.False(RaidSites.TryFindShipPoint(Vector2.zero, 200f, (_, _) => Water - RaidSites.ShipDepth + 1f, Water, out _));
    }

    [Fact]
    public void TheShipKeepsClearOfAnIsletBesideIt()
    {
        // A small island where the shore search would otherwise put the ship; its edge is 2 m from that place.
        float Islet(float x, float z)
        {
            return Vector2.Distance(new Vector2(x, z), new Vector2(35f, 0f)) < 8f ? 35f : Shore(x, z);
        }

        bool found = RaidSites.TryFindShipPoint(new Vector2(-5f, 0f), 80f, Islet, Water, out Vector2 point);

        Assert.True(found);
        Assert.NotEqual(new Vector2(25f, 0f), point);
        Assert.True(EncounterRules.ShoreIsClear(point.x, point.y, RaidSites.ShipClearance, Islet, Water));
        Assert.True(Islet(point.x, point.y) <= Water - RaidSites.ShipDepth);
        Assert.True(Vector2.Distance(point, new Vector2(-5f, 0f)) <= 80f);
    }

    [Fact]
    public void TheDeepestPlaceOnTheNearestRingWins()
    {
        // Two deep places at the same distance: the sea to the east is deeper than to the north.
        float Uneven(float x, float z)
        {
            return z > 0f ? 22f : 10f;
        }

        Assert.True(RaidSites.TryFindShipPoint(Vector2.zero, 40f, Uneven, Water, out Vector2 point));
        Assert.True(point.y <= 0f);
        Assert.Equal(10f, Uneven(point.x, point.y));
    }

    [Theory]
    [InlineData(30f, 0f, 180f)]
    [InlineData(0f, 30f, 90f)]
    [InlineData(-30f, 0f, 0f)]
    [InlineData(0f, -30f, 270f)]
    [InlineData(0f, 0f, 90f)]
    public void TheShipLiesAlongTheShore(float shipX, float shipZ, float heading)
    {
        Assert.Equal(heading, RaidSites.ShipHeading(Vector2.zero, new Vector2(shipX, shipZ)), 2);
    }

    [Fact]
    public void CoastPointsAreLowLandWithWaterNearAndTheNearestComeFirst()
    {
        // Land at x <= 0 (three metres above the sea), sea beyond it.
        float Coast(float x, float z)
        {
            return x <= 0f ? 33f : 20f;
        }

        List<Vector2> points = RaidSites.CoastPoints(new Vector2(-100f, 0f), 50f, 150f, 25f, 0f, Coast, Water);

        Assert.NotEmpty(points);
        Assert.Equal(75f, Vector2.Distance(points[0], new Vector2(-100f, 0f)), 2);
        Assert.All(points, point => Assert.True(point.x <= 0f && point.x > -40.01f, $"{point} is not on the coast"));
        float last = 0f;
        foreach (Vector2 point in points)
        {
            float distance = Vector2.Distance(point, new Vector2(-100f, 0f));
            Assert.True(distance >= last - 0.01f);
            last = distance;
        }
    }

    [Fact]
    public void CoastPointsSkipHighGroundAndOpenSea()
    {
        Assert.Empty(RaidSites.CoastPoints(Vector2.zero, 10f, 200f, 25f, 0f, (_, _) => Water + 30f, Water));
        Assert.Empty(RaidSites.CoastPoints(Vector2.zero, 10f, 200f, 25f, 0f, (_, _) => Water - 20f, Water));
        // Low land with no water anywhere near is not a coast.
        Assert.Empty(RaidSites.CoastPoints(Vector2.zero, 10f, 200f, 25f, 0f, (_, _) => Water + 3f, Water));
    }

    [Fact]
    public void CoastPointsOfTheSameInputAreTheSame()
    {
        Func<float, float, float> coast = (x, z) => x <= 0f ? 33f : 20f;

        Assert.Equal(RaidSites.CoastPoints(new Vector2(-60f, 5f), 20f, 120f, 20f, 17f, coast, Water),
            RaidSites.CoastPoints(new Vector2(-60f, 5f), 20f, 120f, 20f, 17f, coast, Water));
    }

    [Fact]
    public void ARingStepOfZeroDoesNotLoopForEver()
    {
        Assert.NotNull(RaidSites.CoastPoints(Vector2.zero, 0f, 3f, 0f, 0f, (_, _) => 40f, Water));
    }
}
