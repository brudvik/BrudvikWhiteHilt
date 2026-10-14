using BrudvikWhiteHilt.Navigation;
using System.Collections.Generic;
using UnityEngine;
using Xunit;

namespace BrudvikWhiteHilt.Tests;

public class RouteTrackTests
{
    // An L: 100 m east, then 100 m north.
    private static readonly List<Vector3> bend = new() { new(0f, 0f, 0f), new(100f, 0f, 0f), new(100f, 0f, 100f) };

    [Fact]
    public void ARouteIsAsLongAsItsLegs()
    {
        Assert.Equal(200f, RouteTrack.Length(bend), 3);
        Assert.Equal(100f, RouteTrack.Length(bend, 1), 3);
        Assert.Equal(0f, RouteTrack.Length(bend, 2), 3);
        Assert.Equal(0f, RouteTrack.Length(new List<Vector3>()), 3);
    }

    [Fact]
    public void WhatIsLeftIsTheWayToTheNextPointAndTheRest()
    {
        Assert.Equal(160f, RouteTrack.Remaining(bend, 1, new Vector3(40f, 0f, 0f)), 3);
        Assert.Equal(30f, RouteTrack.Remaining(bend, 2, new Vector3(100f, 0f, 70f)), 3);
        Assert.Equal(0f, RouteTrack.Remaining(bend, 3, Vector3.zero), 3);
    }

    [Fact]
    public void OnlyTheFlatDistanceCounts()
    {
        Assert.Equal(100f, RouteTrack.Remaining(new List<Vector3> { new(100f, 0f, 0f) }, 0, new Vector3(0f, 50f, 0f)), 3);
    }

    [Fact]
    public void AReachedPointIsLeftBehindAndAFarOneIsNot()
    {
        Assert.Equal(1, RouteTrack.Advance(bend, 0, new Vector3(5f, 0f, 0f), 20f, 60f));
        Assert.Equal(1, RouteTrack.Advance(bend, 1, new Vector3(50f, 0f, 0f), 20f, 60f));
    }

    [Fact]
    public void SeveralPointsCloseTogetherAreLeftBehindAtOnce()
    {
        Assert.Equal(2, RouteTrack.Advance(bend, 0, new Vector3(95f, 0f, 5f), 150f, 60f));
    }

    [Fact]
    public void TheLastPointNeedsItsOwnReach()
    {
        Vector3 near = new(100f, 0f, 50f);

        Assert.Equal(2, RouteTrack.Advance(bend, 2, near, 20f, 40f));
        Assert.Equal(3, RouteTrack.Advance(bend, 2, near, 20f, 60f));
    }

    [Fact]
    public void AWholeSailedRouteIsPastTheLastPoint()
    {
        Assert.Equal(3, RouteTrack.Advance(bend, 3, Vector3.zero, 20f, 60f));
        Assert.Equal(0, RouteTrack.Advance(new List<Vector3>(), -4, Vector3.zero, 20f, 60f));
    }

    [Fact]
    public void ProgressRunsFromNothingToAll()
    {
        Assert.Equal(0f, RouteTrack.Progress(200f, 200f), 3);
        Assert.Equal(0.25f, RouteTrack.Progress(150f, 200f), 3);
        Assert.Equal(1f, RouteTrack.Progress(0f, 200f), 3);
        Assert.Equal(0f, RouteTrack.Progress(300f, 200f), 3);
    }

    [Fact]
    public void ARouteWithoutLengthIsFinished()
    {
        Assert.Equal(1f, RouteTrack.Progress(0f, 0f), 3);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(0.25f)]
    [InlineData(0.6f)]
    public void ANewRouteKeepsTheProgressWhereItWas(float progress)
    {
        float total = RouteTrack.TotalFor(900f, progress);

        Assert.Equal(progress, RouteTrack.Progress(900f, total), 3);
    }

    [Fact]
    public void ANewRouteAtTheVeryEndDoesNotDivideByNothing()
    {
        float total = RouteTrack.TotalFor(100f, 1f);

        Assert.False(float.IsInfinity(total) || float.IsNaN(total));
        Assert.True(total > 0f);
    }
}
