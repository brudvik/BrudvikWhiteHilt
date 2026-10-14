using BrudvikWhiteHilt.Pieces.Ships.Ram;
using BrudvikWhiteHilt.SeaBattles;
using BrudvikWhiteHilt.SeaBattles.Damage;
using BrudvikWhiteHilt.SeaBattles.Weapons;
using System;
using System.Collections.Generic;
using UnityEngine;
using Xunit;

namespace BrudvikWhiteHilt.Tests;

public class HoleRulesTests
{
    [Theory]
    [InlineData(59.9f, 60f, true, false, false)]
    [InlineData(60f, 60f, true, false, true)]
    [InlineData(500f, 60f, true, false, true)]
    // Outside a battle only a ram makes a hole.
    [InlineData(500f, 60f, false, false, false)]
    [InlineData(0f, 60f, false, true, true)]
    [InlineData(0f, 60f, true, true, true)]
    public void OnlyHeavyHitsInBattleAndRamsMakeHoles(float damage, float minimum, bool inBattle, bool isRam, bool expected)
    {
        Assert.Equal(expected, HoleRules.MakesHole(damage, minimum, inBattle, isRam));
    }

    [Fact]
    public void AHoleSitsOnTheSideThatWasHit()
    {
        Vector3 centre = new(0f, 1f, 0f);
        Vector3 size = new(5f, 3f, 20f);

        Vector3 starboard = HoleRules.Place(new Vector3(2.4f, 2f, 3f), centre, size, 0.5f);
        Vector3 port = HoleRules.Place(new Vector3(-0.1f, 2f, 3f), centre, size, 0.5f);

        // Half the beam is 2.5; the hole is 0.15 inside it.
        Assert.Equal(2.35f, starboard.x, 3);
        Assert.Equal(-2.35f, port.x, 3);
        Assert.Equal(0.75f, starboard.y, 3);
        Assert.Equal(3f, starboard.z, 3);
        Assert.Equal(3f, port.z, 3);
    }

    [Fact]
    public void AHitAmidshipsGoesToStarboard()
    {
        Vector3 hole = HoleRules.Place(new Vector3(1f, 0f, 0f), new Vector3(1f, 0f, 0f), new Vector3(4f, 2f, 10f), 0f);

        Assert.True(hole.x > 1f);
    }

    [Fact]
    public void ADisplacedHullCentreMovesThePlace()
    {
        Vector3 hole = HoleRules.Place(new Vector3(0f, 0f, 1f), new Vector3(0.5f, 0f, 2f), new Vector3(4f, 2f, 10f), 0f);

        // Left of the centre (x 0.5), so the port side: 0.5 - (2 - 0.15).
        Assert.Equal(-1.35f, hole.x, 3);
    }

    [Theory]
    [InlineData(100f, 4.5f)]
    [InlineData(-100f, -4.5f)]
    [InlineData(4.5f, 4.5f)]
    [InlineData(0f, 0f)]
    public void AHoleStaysHalfAMetreFromTheBowAndStern(float hitZ, float expectedZ)
    {
        Vector3 hole = HoleRules.Place(new Vector3(2f, 0f, hitZ), Vector3.zero, new Vector3(5f, 2f, 10f), 0f);

        Assert.Equal(expectedZ, hole.z, 3);
    }

    [Fact]
    public void AHullTooShortForTheMarginPutsTheHoleInTheMiddle()
    {
        Vector3 hole = HoleRules.Place(new Vector3(2f, 0f, 3f), new Vector3(0f, 0f, 1f), new Vector3(5f, 2f, 0.8f), 0f);

        Assert.Equal(1f, hole.z, 3);
    }

    [Fact]
    public void ANarrowHullDoesNotPutTheHoleOutsideTheCentre()
    {
        Vector3 hole = HoleRules.Place(new Vector3(1f, 0f, 0f), Vector3.zero, new Vector3(0.1f, 2f, 10f), 0f);

        Assert.Equal(0f, hole.x, 3);
    }

    [Fact]
    public void TheNearestHoleWithinReachIsFound()
    {
        Vector3[] holes = { new(2f, 0f, 0f), new(2f, 0f, 5f), new(-2f, 0f, 5f) };

        Assert.Equal(1, HoleRules.Nearest(holes, new Vector3(2f, 0f, 4f), 3f));
        Assert.Equal(2, HoleRules.Nearest(holes, new Vector3(-1f, 0f, 5f), 3f));
        Assert.Equal(0, HoleRules.Nearest(holes, new Vector3(2f, 0f, 1f), 3f));
    }

    [Fact]
    public void NoHoleWithinReachGivesMinusOne()
    {
        Vector3[] holes = { new(2f, 0f, 0f) };

        Assert.Equal(-1, HoleRules.Nearest(holes, new Vector3(2f, 0f, 3.01f), 3f));
        Assert.Equal(0, HoleRules.Nearest(holes, new Vector3(2f, 0f, 3f), 3f));
        Assert.Equal(-1, HoleRules.Nearest(new Vector3[0], Vector3.zero, 3f));
        Assert.Equal(-1, HoleRules.Nearest(null, Vector3.zero, 3f));
        Assert.Equal(-1, HoleRules.Nearest(holes, Vector3.zero, -1f));
    }

    [Fact]
    public void EquallyNearHolesGiveTheFirst()
    {
        Vector3[] holes = { new(1f, 0f, 0f), new(-1f, 0f, 0f) };

        Assert.Equal(0, HoleRules.Nearest(holes, Vector3.zero, 5f));
    }

    [Fact]
    public void NearestAcceptsAnyReadOnlyList()
    {
        IReadOnlyList<Vector3> holes = new List<Vector3> { new(5f, 0f, 0f), new(1f, 0f, 0f) };

        Assert.Equal(1, HoleRules.Nearest(holes, Vector3.zero, 2f));
    }
}

public class FloodModelTests
{
    [Fact]
    public void OpenHolesLetWaterIn()
    {
        // Two holes at 0.01 a second for ten seconds.
        Assert.Equal(0.3f, FloodModel.Step(0.1f, 2, 10f, 0.01f, 0.005f), 4);
    }

    [Fact]
    public void WithNoHolesTheShipDrains()
    {
        Assert.Equal(0.35f, FloodModel.Step(0.4f, 0, 10f, 0.01f, 0.005f), 4);
    }

    [Fact]
    public void DrainingNeedsNoHolesAtAll()
    {
        // One open hole never drains, even if the drain is large.
        Assert.Equal(0.5f, FloodModel.Step(0.5f, 1, 0f, 0.01f, 5f), 4);
        Assert.True(FloodModel.Step(0.5f, 1, 1f, 0.01f, 5f) > 0.5f);
    }

    [Theory]
    [InlineData(0.95f, 3, 60f, 1f)]
    [InlineData(0.02f, 0, 60f, 0f)]
    [InlineData(0f, 0, 1f, 0f)]
    [InlineData(1f, 1, 1f, 1f)]
    public void TheFloodStaysBetweenZeroAndOne(float flood, int holes, float seconds, float expected)
    {
        Assert.Equal(expected, FloodModel.Step(flood, holes, seconds, 0.01f, 0.005f), 4);
    }

    [Fact]
    public void AStepOfNoTimeOrNegativeTimeChangesNothing()
    {
        Assert.Equal(0.4f, FloodModel.Step(0.4f, 3, 0f, 0.01f, 0.005f), 4);
        Assert.Equal(0.4f, FloodModel.Step(0.4f, 3, -5f, 0.01f, 0.005f), 4);
        Assert.Equal(0.4f, FloodModel.Step(0.4f, 0, -5f, 0.01f, 0.005f), 4);
    }

    [Fact]
    public void NegativeRatesAndCountsDoNotReverseTheFlood()
    {
        Assert.Equal(0.4f, FloodModel.Step(0.4f, 2, 10f, -1f, 0.005f), 4);
        Assert.Equal(0.4f, FloodModel.Step(0.4f, 0, 10f, 0.01f, -1f), 4);
        Assert.Equal(0.35f, FloodModel.Step(0.4f, -2, 10f, 0.01f, 0.005f), 4);
    }

    [Fact]
    public void AStartingFloodOutOfRangeIsBroughtBack()
    {
        Assert.Equal(1f, FloodModel.Step(7f, 0, 0f, 0.01f, 0.005f), 4);
        Assert.Equal(0f, FloodModel.Step(-3f, 0, 0f, 0.01f, 0.005f), 4);
    }

    [Theory]
    [InlineData(0f, 0.8f, 0f)]
    [InlineData(0.5f, 0.8f, 0.4f)]
    [InlineData(1f, 0.8f, 0.8f)]
    [InlineData(2f, 0.8f, 0.8f)]
    [InlineData(-1f, 0.8f, 0f)]
    [InlineData(1f, -0.8f, 0f)]
    public void AFullShipLiesAsLowAsTheSinkDepth(float flood, float depth, float expected)
    {
        Assert.Equal(expected, FloodModel.Offset(flood, depth), 4);
    }

    [Theory]
    [InlineData(0f, 0.6f, 1f)]
    [InlineData(0.5f, 0.6f, 0.7f)]
    [InlineData(1f, 0.6f, 0.4f)]
    [InlineData(1f, 1f, 0f)]
    [InlineData(1f, 0f, 1f)]
    [InlineData(3f, 2f, 0f)]
    [InlineData(-1f, 0.6f, 1f)]
    public void AFloodedShipSailsWithLessForce(float flood, float loss, float expected)
    {
        Assert.Equal(expected, FloodModel.SailFactor(flood, loss), 4);
    }

    [Fact]
    public void ADefaultHoleTakesOneHundredSecondsToFillAnEmptyShip()
    {
        float flood = 0f;
        for (int second = 0; second < 100; second++)
        {
            flood = FloodModel.Step(flood, 1, 1f, 0.01f, 0.005f);
        }

        Assert.Equal(1f, flood, 3);
    }
}

public class BallisticsTests
{
    private const float Gravity = 9.81f;

    // Where a projectile launched from a point is after a time, in a vacuum.
    private static Vector3 Position(Vector3 from, Vector3 velocity, float gravity, float seconds)
    {
        return from + velocity * seconds + Vector3.down * (0.5f * gravity * seconds * seconds);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BothArcsPassThroughTheTarget(bool high)
    {
        Vector3 from = new(2f, 4f, -3f);
        Vector3 to = new(32f, 1f, 18f);

        Assert.True(Ballistics.LaunchVelocity(from, to, 30f, Gravity, high, out Vector3 velocity));

        float seconds = Ballistics.FlightSeconds(from, to, velocity);
        Vector3 arrival = Position(from, velocity, Gravity, seconds);
        Assert.True(Vector3.Distance(arrival, to) < 0.01f, $"arrives at {arrival}");
        Assert.Equal(30f, velocity.magnitude, 3);
    }

    [Fact]
    public void TheHighArcIsSteeperAndSlowerThanTheLowOne()
    {
        Vector3 from = Vector3.zero;
        Vector3 to = new(40f, 0f, 0f);

        Ballistics.LaunchVelocity(from, to, 30f, Gravity, false, out Vector3 low);
        Ballistics.LaunchVelocity(from, to, 30f, Gravity, true, out Vector3 high);

        Assert.True(high.y > low.y);
        Assert.True(high.x < low.x);
        Assert.True(Ballistics.FlightSeconds(from, to, high) > Ballistics.FlightSeconds(from, to, low));
    }

    [Fact]
    public void ALevelShotIsAimedTowardsTheTargetOnTheGround()
    {
        Ballistics.LaunchVelocity(Vector3.zero, new Vector3(0f, 0f, 20f), 25f, Gravity, false, out Vector3 velocity);

        Assert.Equal(0f, velocity.x, 4);
        Assert.True(velocity.z > 0f);
        Assert.True(velocity.y > 0f);
    }

    [Fact]
    public void AtTheLimitOfReachTheTwoArcsMeetAtFortyFiveDegrees()
    {
        // At maximum range on level ground, v^2 / g, the two arcs meet at 45 degrees.
        float speed = 20f;
        float range = speed * speed / Gravity;

        Assert.True(Ballistics.LaunchVelocity(Vector3.zero, new Vector3(range, 0f, 0f), speed, Gravity, false, out Vector3 velocity));

        Assert.Equal(velocity.x, velocity.y, 1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ATargetBeyondReachGivesNoSolution(bool high)
    {
        // 20 m/s reaches about 40.8 m on level ground.
        Assert.False(Ballistics.LaunchVelocity(Vector3.zero, new Vector3(41f, 0f, 0f), 20f, Gravity, high, out Vector3 velocity));
        Assert.Equal(Vector3.zero, velocity);
    }

    [Fact]
    public void ATargetFarAboveTheShooterIsOutOfReachEvenWhenClose()
    {
        Assert.False(Ballistics.LaunchVelocity(Vector3.zero, new Vector3(5f, 60f, 0f), 20f, Gravity, true, out _));
        Assert.True(Ballistics.LaunchVelocity(Vector3.zero, new Vector3(5f, -60f, 0f), 20f, Gravity, true, out _));
    }

    [Fact]
    public void WithoutGravityTheShotIsStraight()
    {
        Vector3 to = new(30f, 40f, 0f);

        Assert.True(Ballistics.LaunchVelocity(Vector3.zero, to, 10f, 0f, false, out Vector3 velocity));

        Assert.Equal(to.normalized * 10f, velocity);
        Assert.True(Ballistics.LaunchVelocity(Vector3.zero, to, 10f, -1f, true, out Vector3 same));
        Assert.Equal(velocity, same);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-5f)]
    public void NoSpeedGivesNoSolution(float speed)
    {
        Assert.False(Ballistics.LaunchVelocity(Vector3.zero, new Vector3(10f, 0f, 0f), speed, Gravity, false, out _));
    }

    [Fact]
    public void ATargetStraightAboveOrAtTheLaunchPointGivesNoSolution()
    {
        Assert.False(Ballistics.LaunchVelocity(Vector3.zero, new Vector3(0f, 5f, 0f), 30f, Gravity, false, out _));
        Assert.False(Ballistics.LaunchVelocity(Vector3.zero, Vector3.zero, 30f, Gravity, true, out _));
        Assert.False(Ballistics.LaunchVelocity(Vector3.zero, Vector3.zero, 30f, 0f, true, out _));
    }

    [Fact]
    public void LeadAimsWhereTheTargetWillBe()
    {
        Assert.Equal(new Vector3(10f, 0f, 6f), Ballistics.Lead(new Vector3(10f, 0f, 0f), new Vector3(0f, 0f, 2f), 3f));
        Assert.Equal(new Vector3(10f, 0f, 0f), Ballistics.Lead(new Vector3(10f, 0f, 0f), new Vector3(0f, 0f, 2f), 0f));
        Assert.Equal(new Vector3(10f, 0f, 0f), Ballistics.Lead(new Vector3(10f, 0f, 0f), new Vector3(0f, 0f, 2f), -4f));
    }

    [Fact]
    public void FlightTimeIsTheHorizontalDistanceOverTheHorizontalSpeed()
    {
        Assert.Equal(2f, Ballistics.FlightSeconds(Vector3.zero, new Vector3(30f, 50f, 40f), new Vector3(15f, 99f, 20f)), 4);
        Assert.Equal(0f, Ballistics.FlightSeconds(Vector3.zero, new Vector3(30f, 0f, 0f), new Vector3(0f, 10f, 0f)));
    }

    [Fact]
    public void LeadingAMovingTargetStillHitsIt()
    {
        Vector3 from = Vector3.zero;
        Vector3 target = new(30f, 0f, 0f);
        Vector3 targetVelocity = new(0f, 0f, 6f);

        // A few passes settle the flight time, as a Nail Ship's crew would aim.
        Vector3 aim = target;
        Vector3 velocity = Vector3.zero;
        for (int pass = 0; pass < 6; pass++)
        {
            Assert.True(Ballistics.LaunchVelocity(from, aim, 20f, Gravity, false, out velocity));
            aim = Ballistics.Lead(target, targetVelocity, Ballistics.FlightSeconds(from, aim, velocity));
        }

        float seconds = Ballistics.FlightSeconds(from, aim, velocity);
        Vector3 arrival = Position(from, velocity, Gravity, seconds);
        Vector3 targetThen = target + targetVelocity * seconds;
        Assert.True(Vector3.Distance(arrival, targetThen) < 0.1f, $"arrives at {arrival}, target at {targetThen}");
    }
}

public class GrappleRulesTests
{
    [Theory]
    [InlineData(4f, 6f, 1.5f, 0f)]
    [InlineData(6f, 6f, 1.5f, 0f)]
    [InlineData(10f, 6f, 1.5f, 6f)]
    [InlineData(10f, 6f, 0f, 0f)]
    [InlineData(10f, 6f, -2f, 0f)]
    public void TheRopePullsOnlyWhenTaut(float distance, float length, float strength, float expected)
    {
        Assert.Equal(expected, GrappleRules.Pull(distance, length, strength), 4);
    }

    [Fact]
    public void TheRopeGivesWayWhenStretchedTooFarOrOld()
    {
        Assert.False(GrappleRules.Snaps(30f, 60f, 10f, 90f));
        Assert.False(GrappleRules.Snaps(60f, 60f, 89.9f, 90f));
        Assert.True(GrappleRules.Snaps(60.1f, 60f, 10f, 90f));
        Assert.True(GrappleRules.Snaps(30f, 60f, 90f, 90f));
        Assert.True(GrappleRules.Snaps(99f, 60f, 99f, 90f));
    }
}

public class RamRulesTests
{
    [Fact]
    public void ClosingSpeedIsTheSpeedAlongTheBow()
    {
        Assert.Equal(5f, RamRules.ClosingSpeed(new Vector3(0f, 0f, 5f), Vector3.forward), 4);
        Assert.Equal(3f, RamRules.ClosingSpeed(new Vector3(4f, 1f, 3f), Vector3.forward), 4);
    }

    [Fact]
    public void ClosingSpeedDoesNotNeedAUnitBow()
    {
        Assert.Equal(5f, RamRules.ClosingSpeed(new Vector3(0f, 0f, 5f), new Vector3(0f, 0f, 12f)), 4);
        Assert.Equal(Mathf.Sqrt(2f) * 5f, RamRules.ClosingSpeed(new Vector3(5f, 0f, 5f), new Vector3(1f, 0f, 0f) + Vector3.forward), 3);
    }

    [Fact]
    public void BackingOntoATargetIsNotARam()
    {
        Assert.Equal(0f, RamRules.ClosingSpeed(new Vector3(0f, 0f, -5f), Vector3.forward));
        Assert.Equal(0f, RamRules.ClosingSpeed(new Vector3(5f, 0f, 0f), Vector3.forward), 4);
        Assert.Equal(0f, RamRules.ClosingSpeed(new Vector3(0f, 0f, 5f), Vector3.zero));
    }

    [Theory]
    [InlineData(5f, 10f, true)]
    [InlineData(2.5f, 10f, true)]
    [InlineData(2.4f, 10f, false)]
    [InlineData(0f, 10f, false)]
    [InlineData(-5f, 10f, false)]
    [InlineData(4f, 12f, true)]
    [InlineData(2.9f, 12f, false)]
    [InlineData(5f, 0f, false)]
    [InlineData(5f, -10f, false)]
    public void OnlyTheFrontQuarterIsTheBow(float contactZ, float length, bool expected)
    {
        Assert.Equal(expected, RamRules.IsBowContact(new Vector3(1.8f, 0.5f, contactZ), length));
    }

    [Fact]
    public void ABowContactIsDecidedByItsPositionAlongTheHullOnly()
    {
        Assert.True(RamRules.IsBowContact(new Vector3(-40f, -9f, 5f), 10f));
    }

    [Theory]
    [InlineData(2.9f, 3f, 40f, 0f)]
    [InlineData(3f, 3f, 40f, 120f)]
    [InlineData(5f, 3f, 40f, 200f)]
    [InlineData(5f, 3f, 0f, 0f)]
    [InlineData(5f, 3f, -40f, 0f)]
    [InlineData(0f, 0f, 40f, 0f)]
    [InlineData(-1f, -3f, 40f, 0f)]
    public void DamageGrowsWithSpeedAndIsNothingBelowTheLeastSpeed(float speed, float minimum, float perSpeed, float expected)
    {
        Assert.Equal(expected, RamRules.Damage(speed, minimum, perSpeed), 3);
    }

    [Fact]
    public void ARamNeverHurtsLessAtHigherSpeed()
    {
        float previous = 0f;
        for (float speed = 0f; speed <= 12f; speed += 0.5f)
        {
            float damage = RamRules.Damage(speed, 3f, 40f);

            Assert.True(damage >= previous || Math.Abs(damage - previous) < 1e-4f, $"damage drops at {speed} m/s");
            previous = damage;
        }
    }

    [Fact]
    public void ApproachSpeedIsTheSameWhicheverWayTheEngineReportsTheVelocity()
    {
        Vector3 velocity = new(1f, 0.5f, 6f);

        Assert.Equal(6f, RamRules.ApproachSpeed(velocity, Vector3.forward), 4);
        Assert.Equal(6f, RamRules.ApproachSpeed(-velocity, Vector3.forward), 4);
        Assert.Equal(RamRules.ApproachSpeed(velocity, new Vector3(0f, 0f, 3f)), RamRules.ApproachSpeed(-velocity, new Vector3(0f, 0f, 3f)), 4);
    }

    [Fact]
    public void ApproachSpeedIgnoresMotionAcrossTheBow()
    {
        Assert.Equal(0f, RamRules.ApproachSpeed(new Vector3(8f, 0f, 0f), Vector3.forward), 4);
        Assert.Equal(0f, RamRules.ApproachSpeed(new Vector3(0f, 0f, 8f), Vector3.zero), 4);
    }

    [Theory]
    [InlineData(5f, 20f, 100f)]
    [InlineData(0f, 20f, 0f)]
    [InlineData(-2f, 20f, 0f)]
    [InlineData(5f, 0f, 0f)]
    [InlineData(5f, -20f, 0f)]
    public void PushGrowsWithSpeedAndNeverPullsBack(float speed, float perSpeed, float expected)
    {
        Assert.Equal(expected, RamRules.Push(speed, perSpeed), 3);
    }

    [Theory]
    [InlineData(-1f, 0.5f, 1.5f, true)]
    [InlineData(10f, 10.5f, 1.5f, false)]
    [InlineData(10f, 11.5f, 1.5f, true)]
    [InlineData(10f, 20f, 1.5f, true)]
    [InlineData(10f, 10f, 0f, true)]
    public void ATargetCanBeRammedAgainWhenTheCooldownHasPassed(float lastHit, float now, float cooldown, bool expected)
    {
        Assert.Equal(expected, RamRules.CooledDown(lastHit, now, cooldown));
    }
}

public class HoleAimingTests
{
    // A player's eye at the origin looking along +z.
    private static readonly Vector3 Eye = Vector3.zero;
    private static readonly Vector3 Forward = Vector3.forward;

    [Fact]
    public void TheHoleOnTheLineOfSightIsAimedAt()
    {
        Vector3[] holes = { new(0.3f, 0.2f, 3f) };

        Assert.Equal(0, HoleRules.Aimed(holes, Eye, Forward, 0.9f, 5f));
    }

    [Fact]
    public void AHoleFarFromTheLineOfSightIsNot()
    {
        Vector3[] holes = { new(2f, 0f, 3f) };

        Assert.Equal(-1, HoleRules.Aimed(holes, Eye, Forward, 0.9f, 5f));
    }

    [Fact]
    public void TheNearerOfTwoHolesOnTheLineWins()
    {
        Vector3[] holes = { new(0f, 0f, 4f), new(0.1f, 0f, 2f), new(0f, 0f, 3f) };

        Assert.Equal(1, HoleRules.Aimed(holes, Eye, Forward, 0.9f, 5f));
    }

    [Fact]
    public void AHoleBehindThePlayerOrBeyondReachIsNot()
    {
        Vector3[] holes = { new(0f, 0f, -2f), new(0f, 0f, 5.01f) };

        Assert.Equal(-1, HoleRules.Aimed(holes, Eye, Forward, 0.9f, 5f));
        Assert.Equal(1, HoleRules.Aimed(holes, Eye, Forward, 0.9f, 5.01f));
    }

    [Fact]
    public void TheDirectionNeedNotBeAUnitVector()
    {
        Vector3[] holes = { new(0f, 0f, 3f) };

        Assert.Equal(0, HoleRules.Aimed(holes, Eye, new Vector3(0f, 0f, 40f), 0.5f, 5f));
    }

    [Fact]
    public void NoDirectionNoPointsOrNoHolesGivesMinusOne()
    {
        Vector3[] holes = { new(0f, 0f, 3f) };

        Assert.Equal(-1, HoleRules.Aimed(holes, Eye, Vector3.zero, 0.9f, 5f));
        Assert.Equal(-1, HoleRules.Aimed(null, Eye, Forward, 0.9f, 5f));
        Assert.Equal(-1, HoleRules.Aimed(new Vector3[0], Eye, Forward, 0.9f, 5f));
    }

    [Fact]
    public void TheDistanceFromTheLineOfSightIsMeasuredSideways()
    {
        // 1 m to the side at 3 m ahead: just outside a radius of 0.9, inside 1.
        Vector3[] holes = { new(1f, 0f, 3f) };

        Assert.Equal(-1, HoleRules.Aimed(holes, Eye, Forward, 0.9f, 5f));
        Assert.Equal(0, HoleRules.Aimed(holes, Eye, Forward, 1f, 5f));
    }
}

public class HoleMendingTests
{
    [Theory]
    [InlineData(500f, 1000f, 0.1f, 600f)]
    [InlineData(950f, 1000f, 0.1f, 1000f)]
    [InlineData(1000f, 1000f, 0.1f, 1000f)]
    [InlineData(0f, 1000f, 0.25f, 250f)]
    [InlineData(500f, 1000f, 0f, 500f)]
    [InlineData(500f, 1000f, 7f, 1000f)]
    [InlineData(500f, 1000f, -1f, 500f)]
    public void APatchMendsAShareOfTheHullButNotBeyondFull(float health, float max, float share, float expected)
    {
        Assert.Equal(expected, HoleRules.Mend(health, max, share), 3);
    }

    [Fact]
    public void APatchNeverTakesHealthAway()
    {
        // More health than the most (the hull was made weaker since): it is left alone.
        Assert.Equal(1200f, HoleRules.Mend(1200f, 1000f, 0.1f), 3);
    }

    [Theory]
    [InlineData(0, 3, true)]
    [InlineData(2, 3, true)]
    [InlineData(3, 3, false)]
    [InlineData(-1, 3, false)]
    [InlineData(0, 0, false)]
    public void OnlyAnIndexInsideTheListIsAHole(int index, int count, bool expected)
    {
        Assert.Equal(expected, HoleRules.ValidIndex(index, count));
    }
}

public class FloodTimeTests
{
    private static long Ticks(double seconds) => (long)(seconds * TimeSpan.TicksPerSecond);

    [Fact]
    public void AStepCoversTheTimeSinceTheLastOne()
    {
        long last = Ticks(1000);

        Assert.Equal(0.5f, FloodModel.ElapsedSeconds(last, last + Ticks(0.5), 60f), 3);
        Assert.Equal(12f, FloodModel.ElapsedSeconds(last, last + Ticks(12), 60f), 3);
    }

    [Fact]
    public void AShipLoadedAfterALongTimeDoesNotFloodAllAtOnce()
    {
        long last = Ticks(1000);

        Assert.Equal(60f, FloodModel.ElapsedSeconds(last, last + Ticks(86400), 60f), 3);
        Assert.Equal(60f, FloodModel.ElapsedSeconds(last, last + Ticks(60), 60f), 3);
    }

    [Theory]
    [InlineData(0L, 5000000L)]
    [InlineData(-5L, 5000000L)]
    [InlineData(5000000L, 5000000L)]
    [InlineData(5000000L, 4000000L)]
    public void NoEarlierStepOrTimeGoingBackwardsGivesNoTime(long last, long now)
    {
        Assert.Equal(0f, FloodModel.ElapsedSeconds(last, now, 60f), 3);
    }

    [Fact]
    public void ANegativeCapGivesNoTime()
    {
        Assert.Equal(0f, FloodModel.ElapsedSeconds(Ticks(1), Ticks(10), -5f), 3);
    }

    [Theory]
    [InlineData(1f, 25f, 0.5f, 12.5f)]
    [InlineData(0.99f, 25f, 0.5f, 0f)]
    [InlineData(1f, 0f, 0.5f, 0f)]
    [InlineData(1f, -25f, 0.5f, 0f)]
    [InlineData(1f, 25f, -1f, 0f)]
    public void OnlyAFullShipTakesDamage(float flood, float perSecond, float seconds, float expected)
    {
        Assert.Equal(expected, FloodModel.HullDamage(flood, perSecond, seconds), 3);
    }

    [Theory]
    [InlineData(0f, 0)]
    [InlineData(0.004f, 0)]
    [InlineData(0.006f, 1)]
    [InlineData(0.376f, 38)]
    [InlineData(1f, 100)]
    [InlineData(3f, 100)]
    [InlineData(-1f, 0)]
    public void TheFloodIsShownInWholePercent(float flood, int expected)
    {
        Assert.Equal(expected, FloodModel.Percent(flood));
    }
}

public class HoleListTests
{
    [Fact]
    public void HolesSurviveTheTripThroughBytes()
    {
        Vector3[] holes = { new(2.35f, 0.25f, -3f), new(-2.1f, 0.25f, 4.5f), Vector3.zero };

        List<Vector3> read = HoleList.Decode(HoleList.Encode(holes));

        Assert.Equal(holes, read.ToArray());
    }

    [Fact]
    public void NoHolesIsFourBytesOfZero()
    {
        Assert.Equal(new byte[] { 0, 0, 0, 0 }, HoleList.Encode(new List<Vector3>()));
        Assert.Equal(new byte[] { 0, 0, 0, 0 }, HoleList.Encode(null));
        Assert.Empty(HoleList.Decode(new byte[] { 0, 0, 0, 0 }));
    }

    [Fact]
    public void ThePackageHasTheLayoutOfAZPackage()
    {
        // A little-endian count, then three little-endian floats per hole: 1.0f is 00 00 80 3F.
        byte[] bytes = HoleList.Encode(new[] { new Vector3(1f, 0f, 0f) });

        Assert.Equal(new byte[] { 1, 0, 0, 0, 0, 0, 0x80, 0x3F, 0, 0, 0, 0, 0, 0, 0, 0 }, bytes);
    }

    [Fact]
    public void NoMoreThanTheLimitIsWritten()
    {
        List<Vector3> many = new();
        for (int i = 0; i < HoleList.Limit + 5; i++)
        {
            many.Add(new Vector3(i, 0f, 0f));
        }

        Assert.Equal(HoleList.Limit, HoleList.Decode(HoleList.Encode(many)).Count);
    }

    [Fact]
    public void ACountThatLiesAboutTheBytesIsCut()
    {
        // The count says 1000 but there are two holes' bytes; the limit and the data both cut it.
        byte[] good = HoleList.Encode(new[] { new Vector3(1f, 2f, 3f), new Vector3(4f, 5f, 6f) });
        byte[] lying = (byte[])good.Clone();
        lying[0] = 0xE8;
        lying[1] = 0x03;

        Assert.Equal(2, HoleList.Decode(lying).Count);
    }

    [Theory]
    [InlineData(new byte[0])]
    [InlineData(new byte[] { 1, 2 })]
    [InlineData(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF })]
    public void ShortOrNegativeDataGivesNoHoles(byte[] bytes)
    {
        Assert.Empty(HoleList.Decode(bytes));
    }

    [Fact]
    public void NullGivesNoHoles()
    {
        Assert.Empty(HoleList.Decode(null));
    }

    [Fact]
    public void APointThatIsNotANumberRefusesTheWholeList()
    {
        byte[] bytes = HoleList.Encode(new[] { new Vector3(1f, 0f, 0f), new Vector3(float.NaN, 0f, 0f) });

        Assert.Empty(HoleList.Decode(bytes));
        Assert.Empty(HoleList.Decode(HoleList.Encode(new[] { new Vector3(float.PositiveInfinity, 0f, 0f) })));
    }

    [Fact]
    public void BytesAreComparedOneByOne()
    {
        byte[] a = HoleList.Encode(new[] { new Vector3(1f, 2f, 3f) });
        byte[] same = (byte[])a.Clone();
        byte[] other = HoleList.Encode(new[] { new Vector3(1f, 2f, 4f) });

        Assert.True(HoleList.SameBytes(a, same));
        Assert.False(HoleList.SameBytes(a, other));
        Assert.False(HoleList.SameBytes(a, new byte[3]));
        Assert.True(HoleList.SameBytes(null, null));
        Assert.False(HoleList.SameBytes(a, null));
        Assert.False(HoleList.SameBytes(null, a));
    }
}

public class HullShapeTests
{
    // A hull seen from above, widest amidships: points on both sides at three places along it, at waterline height and
    // up at the rail, where it is wider.
    private static readonly Vector3[] Hull =
    {
        new(2.0f, 0.2f, 0f), new(-2.0f, 0.2f, 0f), new(2.7f, 1.5f, 0f), new(-2.7f, 1.5f, 0f),
        new(1.0f, 0.2f, 6f), new(-1.0f, 0.2f, 6f), new(0.4f, 0.2f, 9f), new(-0.4f, 0.2f, 9f)
    };

    [Fact]
    public void TheHullIsAsWideAsItIsAtTheWaterlineNotAtTheRail()
    {
        Assert.Equal(2.0f, HullShape.HalfWidth(Hull, 1f, 0.25f, 0.35f, 0f, 0.6f, 4f), 3);
        Assert.Equal(2.0f, HullShape.HalfWidth(Hull, -1f, 0.25f, 0.35f, 0f, 0.6f, 4f), 3);
    }

    [Fact]
    public void TheHullNarrowsTowardsTheBowAndStern()
    {
        Assert.Equal(1.0f, HullShape.HalfWidth(Hull, 1f, 0.25f, 0.35f, 6f, 0.6f, 4f), 3);
        Assert.Equal(0.4f, HullShape.HalfWidth(Hull, 1f, 0.25f, 0.35f, 9f, 0.6f, 4f), 3);
    }

    [Fact]
    public void AtTheRailTheHullIsWider()
    {
        Assert.Equal(2.7f, HullShape.HalfWidth(Hull, 1f, 1.5f, 0.35f, 0f, 0.6f, 4f), 3);
    }

    [Fact]
    public void WithNoPointNearTheFallbackIsUsed()
    {
        Assert.Equal(4f, HullShape.HalfWidth(Hull, 1f, 0.25f, 0.35f, 20f, 0.6f, 4f), 3);
        Assert.Equal(4f, HullShape.HalfWidth(Hull, 1f, 5f, 0.35f, 0f, 0.6f, 4f), 3);
        Assert.Equal(4f, HullShape.HalfWidth(null, 1f, 0.25f, 0.35f, 0f, 0.6f, 4f), 3);
        Assert.Equal(4f, HullShape.HalfWidth(new Vector3[0], 1f, 0.25f, 0.35f, 0f, 0.6f, 4f), 3);
    }

    [Fact]
    public void OnlyPointsOnTheAskedSideCount()
    {
        Vector3[] lopsided = { new(2f, 0f, 0f), new(-0.5f, 0f, 0f) };

        Assert.Equal(2f, HullShape.HalfWidth(lopsided, 1f, 0f, 0.1f, 0f, 0.1f, 4f), 3);
        Assert.Equal(0.5f, HullShape.HalfWidth(lopsided, -1f, 0f, 0.1f, 0f, 0.1f, 4f), 3);
        Assert.Equal(4f, HullShape.HalfWidth(new Vector3[] { new(-0.5f, 0f, 0f) }, 1f, 0f, 0.1f, 0f, 0.1f, 4f), 3);
    }

    [Fact]
    public void AHoleGoesInTheHullsSideAndNotInTheFloatBox()
    {
        // The float box is 8 m wide, the hull 4 m at the waterline: the hole is 0.15 m inside the hull's side.
        float half = HullShape.HalfWidth(Hull, 1f, 0.25f, 0.35f, 0f, 0.6f, 4f);
        Vector3 hole = HoleRules.Place(new Vector3(3f, 1f, 0f), new Vector3(0f, 0f, 0f), new Vector3(half * 2f, 1f, 17f), 0f);

        Assert.Equal(1.85f, hole.x, 3);
    }

    [Fact]
    public void TheHullLeansOutwardWithHeight()
    {
        // 2.0 m wide at 0.2 m and 2.7 m wide at 1.5 m, so it gains 0.7 m over 1.3 m.
        Assert.Equal(0.7f / 1.3f, HullShape.Flare(Hull, 1f, 0.85f, 0.65f, 0.1f, 0f, 0.6f), 3);
        Assert.Equal(0.7f / 1.3f, HullShape.Flare(Hull, -1f, 0.85f, 0.65f, 0.1f, 0f, 0.6f), 3);
    }

    [Theory]
    [InlineData(5f)]
    [InlineData(0.2f)]
    public void WithNoPointsAtOneOfTheHeightsThereIsNoFlare(float y)
    {
        Assert.Equal(0f, HullShape.Flare(Hull, 1f, y, 0.65f, 0.1f, 0f, 0.6f), 3);
        Assert.Equal(0f, HullShape.Flare(null, 1f, 0.85f, 0.65f, 0.1f, 0f, 0.6f), 3);
        Assert.Equal(0f, HullShape.Flare(Hull, 1f, 0.85f, 0f, 0.1f, 0f, 0.6f), 3);
    }

    [Fact]
    public void AHullThatNarrowsUpwardHasNoNegativeFlare()
    {
        Vector3[] tumblehome = { new(2.5f, 0f, 0f), new(2f, 1f, 0f) };

        Assert.Equal(0f, HullShape.Flare(tumblehome, 1f, 0.5f, 0.5f, 0.1f, 0f, 0.6f), 3);
    }
}

public class FireListTests
{
    private static readonly long Second = TimeSpan.TicksPerSecond;

    [Fact]
    public void FiresSurviveAWriteAndARead()
    {
        List<DeckFire> fires = new()
        {
            new DeckFire(new Vector3(1f, 0.64f, -2f), 12345678901L),
            new DeckFire(new Vector3(-0.5f, 0.7f, 4.25f), 99L)
        };

        List<DeckFire> read = FireList.Decode(FireList.Encode(fires));

        Assert.Equal(2, read.Count);
        Assert.Equal(new Vector3(1f, 0.64f, -2f), read[0].Point);
        Assert.Equal(12345678901L, read[0].Expiry);
        Assert.Equal(new Vector3(-0.5f, 0.7f, 4.25f), read[1].Point);
        Assert.Equal(99L, read[1].Expiry);
    }

    [Fact]
    public void NothingAndNullAreNoFires()
    {
        Assert.Empty(FireList.Decode(null));
        Assert.Empty(FireList.Decode(new byte[0]));
        Assert.Empty(FireList.Decode(FireList.Encode(null)));
        Assert.Empty(FireList.Decode(FireList.Encode(new List<DeckFire>())));
    }

    [Fact]
    public void ACountLargerThanTheBytesHoldIsCutToWhatIsThere()
    {
        byte[] data = FireList.Encode(new List<DeckFire> { new(Vector3.up, 7L) });
        data[0] = 50;

        List<DeckFire> read = FireList.Decode(data);

        Assert.Single(read);
        Assert.Equal(7L, read[0].Expiry);
    }

    [Fact]
    public void AListIsNeverLongerThanTheLimit()
    {
        List<DeckFire> many = new();
        for (int i = 0; i < FireList.Limit + 10; i++)
        {
            many.Add(new DeckFire(new Vector3(i, 0f, 0f), i + 1L));
        }

        Assert.Equal(FireList.Limit, FireList.Decode(FireList.Encode(many)).Count);
    }

    [Fact]
    public void ABrokenPointGivesNoFires()
    {
        byte[] data = FireList.Encode(new List<DeckFire> { new(new Vector3(float.NaN, 0f, 0f), 7L) });

        Assert.Empty(FireList.Decode(data));
        Assert.Empty(FireList.Decode(new byte[] { 1, 0, 0 }));
    }

    [Fact]
    public void BurntOutFiresAreTakenOff()
    {
        List<DeckFire> fires = new() { new(Vector3.zero, 100L), new(Vector3.one, 200L), new(Vector3.up, 300L) };

        int removed = FireList.Prune(fires, 200L);

        // A fire whose expiry is not after now has burnt out.
        Assert.Equal(2, removed);
        Assert.Single(fires);
        Assert.Equal(Vector3.up, fires[0].Point);
    }

    [Fact]
    public void RainTakesTimeOffEveryFire()
    {
        List<DeckFire> fires = new() { new(Vector3.zero, 10 * Second), new(Vector3.one, 4 * Second) };

        FireList.Shorten(fires, 3 * Second);

        Assert.Equal(7 * Second, fires[0].Expiry);
        Assert.Equal(1 * Second, fires[1].Expiry);
        FireList.Shorten(fires, 0L);
        FireList.Shorten(fires, -5L);
        Assert.Equal(7 * Second, fires[0].Expiry);
    }

    [Fact]
    public void ANewFireIsAddedWhereThePotLanded()
    {
        List<DeckFire> fires = new();

        Assert.True(FireList.Add(fires, new Vector3(1f, 0.6f, 2f), 500L, 6, FireRules.Spacing));

        Assert.Single(fires);
        Assert.Equal(new Vector3(1f, 0.6f, 2f), fires[0].Point);
        Assert.Equal(500L, fires[0].Expiry);
    }

    [Fact]
    public void APotLandingNearAFireFeedsItInsteadOfMakingAnother()
    {
        List<DeckFire> fires = new() { new(new Vector3(0f, 0.6f, 0f), 100L) };

        Assert.True(FireList.Add(fires, new Vector3(0.5f, 0.6f, 0.5f), 400L, 6, FireRules.Spacing));

        Assert.Single(fires);
        Assert.Equal(new Vector3(0f, 0.6f, 0f), fires[0].Point);
        Assert.Equal(400L, fires[0].Expiry);
    }

    [Fact]
    public void AFireIsNeverMadeToBurnShorter()
    {
        List<DeckFire> fires = new() { new(Vector3.zero, 900L) };

        Assert.False(FireList.Add(fires, new Vector3(0.2f, 0f, 0f), 400L, 6, FireRules.Spacing));

        Assert.Equal(900L, fires[0].Expiry);
    }

    [Fact]
    public void TheNearestFireIsTheOneThatIsFed()
    {
        List<DeckFire> fires = new() { new(new Vector3(0f, 0f, 0f), 100L), new(new Vector3(1.6f, 0f, 0f), 100L) };

        // Within a metre of both, nearer the second (0.7 m against 0.9 m).
        FireList.Add(fires, new Vector3(0.9f, 0f, 0f), 700L, 6, 1f);

        Assert.Equal(100L, fires[0].Expiry);
        Assert.Equal(700L, fires[1].Expiry);
    }

    [Fact]
    public void AShipHasNoMoreFiresThanTheCap()
    {
        List<DeckFire> fires = new();
        for (int i = 0; i < 5; i++)
        {
            FireList.Add(fires, new Vector3(i * 3f, 0f, 0f), 100L, 3, FireRules.Spacing);
        }

        Assert.Equal(3, fires.Count);
        Assert.False(FireList.Add(fires, new Vector3(50f, 0f, 0f), 100L, 3, FireRules.Spacing));
    }

    [Fact]
    public void TheCapNeverGoesAboveTheLimitOfAList()
    {
        List<DeckFire> fires = new();
        for (int i = 0; i < FireList.Limit + 5; i++)
        {
            FireList.Add(fires, new Vector3(i * 3f, 0f, 0f), 100L, 1000, FireRules.Spacing);
        }

        Assert.Equal(FireList.Limit, fires.Count);
    }

    [Fact]
    public void ACapOfZeroMakesNoFire()
    {
        List<DeckFire> fires = new();

        Assert.False(FireList.Add(fires, Vector3.zero, 100L, 0, FireRules.Spacing));

        Assert.Empty(fires);
    }

    [Fact]
    public void PointsComeInTheOrderOfTheFires()
    {
        List<Vector3> points = FireList.Points(new List<DeckFire> { new(Vector3.up, 1L), new(Vector3.right, 2L) });

        Assert.Equal(new[] { Vector3.up, Vector3.right }, points);
    }
}

public class FireRulesTests
{
    [Fact]
    public void AFireBurnsForTheSetTimeFromNow()
    {
        Assert.Equal(1000L + 30 * TimeSpan.TicksPerSecond, FireRules.Expiry(1000L, 30f));
        Assert.Equal(1000L, FireRules.Expiry(1000L, 0f));
        Assert.Equal(1000L, FireRules.Expiry(1000L, -4f));
        Assert.Equal(0L, FireRules.Ticks(-1f));
        Assert.Equal(TimeSpan.TicksPerSecond / 2, FireRules.Ticks(0.5f));
    }

    [Fact]
    public void APlayerStandingInAFireBurns()
    {
        List<Vector3> fires = new() { new Vector3(0f, 0.6f, 0f), new Vector3(8f, 0.6f, 2f) };

        Assert.True(FireRules.Burns(fires, new Vector3(1f, 0.6f, 0.5f), 1.5f));
        Assert.True(FireRules.Burns(fires, new Vector3(8.5f, 0.6f, 3f), 1.5f));
        Assert.True(FireRules.Burns(fires, new Vector3(1.5f, 0.6f, 0f), 1.5f));
    }

    [Fact]
    public void APlayerBesideAFireOrFarAboveItDoesNotBurn()
    {
        List<Vector3> fires = new() { new Vector3(0f, 0.6f, 0f) };

        Assert.False(FireRules.Burns(fires, new Vector3(1.6f, 0.6f, 0f), 1.5f));
        Assert.False(FireRules.Burns(fires, new Vector3(0f, 3f, 0f), 1.5f));
        Assert.True(FireRules.Burns(fires, new Vector3(0f, 2.5f, 0f), 1.5f));
        Assert.False(FireRules.Burns(new List<Vector3>(), Vector3.zero, 1.5f));
    }

    [Fact]
    public void HeightIsNotPartOfTheRadius()
    {
        List<Vector3> fires = new() { new Vector3(0f, 0f, 0f) };

        // 1 m sideways and 1.9 m up is farther than the radius in a straight line, but the player stands over the fire.
        Assert.True(FireRules.Burns(fires, new Vector3(1f, 1.9f, 0f), 1.2f));
    }

    [Fact]
    public void EachFireBurnsTheHullForTheTimeThatPasses()
    {
        Assert.Equal(12f, FireRules.HullDamage(3, 4f, 1f), 4);
        Assert.Equal(6f, FireRules.HullDamage(3, 4f, 0.5f), 4);
        Assert.Equal(0f, FireRules.HullDamage(0, 4f, 1f));
        Assert.Equal(0f, FireRules.HullDamage(3, -4f, 1f));
        Assert.Equal(0f, FireRules.HullDamage(-3, 4f, 1f));
        Assert.Equal(0f, FireRules.HullDamage(3, 4f, -1f));
    }

    [Theory]
    [InlineData(0f, 0.64f, 3f, true)]
    [InlineData(2f, 0.64f, -8f, true)]
    // A rail, a mast foot or a pot that lands just outside the hull's box is still the ship's.
    [InlineData(4.9f, 0.64f, 9.4f, true)]
    [InlineData(5.1f, 0.64f, 0f, false)]
    [InlineData(0f, 0.64f, 9.6f, false)]
    [InlineData(0f, -1f, 0f, true)]
    [InlineData(0f, -1.1f, 0f, false)]
    [InlineData(0f, 5f, 0f, true)]
    [InlineData(0f, 5.1f, 0f, false)]
    public void AFireIsOnlyLitOnTheShip(float x, float y, float z, bool expected)
    {
        Assert.Equal(expected, FireRules.ValidSpot(new Vector3(x, y, z), Vector3.zero, new Vector3(8f, 1f, 17f)));
    }

    [Fact]
    public void ABrokenPointIsNotOnTheShip()
    {
        Vector3 size = new(8f, 1f, 17f);

        Assert.False(FireRules.ValidSpot(new Vector3(float.NaN, 0f, 0f), Vector3.zero, size));
        Assert.False(FireRules.ValidSpot(new Vector3(0f, float.PositiveInfinity, 0f), Vector3.zero, size));
    }

    [Fact]
    public void AHullBoxThatIsNotAtTheOriginMovesTheSpot()
    {
        Vector3 centre = new(0f, 0f, 2f);
        Vector3 size = new(6.2f, 0.5f, 18f);

        Assert.True(FireRules.ValidSpot(new Vector3(0f, 0.3f, 11f), centre, size));
        Assert.False(FireRules.ValidSpot(new Vector3(0f, 0.3f, -8.1f), centre, size));
    }
}

public class WeaponRulesTests
{
    [Theory]
    [InlineData(RaiderState.Approach, false)]
    [InlineData(RaiderState.Broadside, true)]
    [InlineData(RaiderState.Board, true)]
    [InlineData(RaiderState.Flee, false)]
    [InlineData(RaiderState.Prize, false)]
    [InlineData(RaiderState.Moored, false)]
    public void PotsAreThrownAlongsideAndWhileBoarding(RaiderState state, bool expected)
    {
        Assert.Equal(expected, WeaponRules.CanThrow(state));
    }

    [Theory]
    [InlineData(RaiderState.Approach, true)]
    [InlineData(RaiderState.Broadside, true)]
    [InlineData(RaiderState.Board, true)]
    [InlineData(RaiderState.Flee, false)]
    [InlineData(RaiderState.Prize, false)]
    [InlineData(RaiderState.Moored, false)]
    public void TheBallistaShootsInEveryFightingState(RaiderState state, bool expected)
    {
        Assert.Equal(expected, WeaponRules.CanShoot(state));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void AnEmptyKeyMeansTheBallistaIsAlwaysThere(string key)
    {
        Assert.True(WeaponRules.KeyReached(key, _ => false));
        Assert.True(WeaponRules.KeyReached(key, null));
    }

    [Fact]
    public void AKeyMustBeSetInTheWorld()
    {
        HashSet<string> keys = new() { "defeated_dragon" };

        Assert.True(WeaponRules.KeyReached("defeated_dragon", keys.Contains));
        Assert.True(WeaponRules.KeyReached(" defeated_dragon ", keys.Contains));
        Assert.False(WeaponRules.KeyReached("defeated_goblinking", keys.Contains));
    }

    [Fact]
    public void WithoutAWorldAKeyIsNotReached()
    {
        Assert.False(WeaponRules.KeyReached("defeated_dragon", null));
    }

    [Theory]
    [InlineData(34.9f, 35f, true)]
    [InlineData(35f, 35f, true)]
    [InlineData(35.1f, 35f, false)]
    public void ATargetIsShotAtWithinRange(float distance, float range, bool expected)
    {
        Assert.Equal(expected, WeaponRules.InRange(distance, range));
    }
}

public class BallisticsInterceptTests
{
    private const float Gravity = 10f;

    private static Vector3 Position(Vector3 from, Vector3 velocity, float seconds)
    {
        return from + velocity * seconds + Vector3.down * (0.5f * Gravity * seconds * seconds);
    }

    [Fact]
    public void APotThrownAheadOfAMovingDeckLandsOnIt()
    {
        // A pot at the Ooze Bomb's speed and gravity, thrown at a deck that sails across its path.
        Vector3 from = new(0f, 3f, 0f);
        Vector3 deck = new(25f, 1f, 6f);
        Vector3 course = new(0f, 0f, 5f);

        Assert.True(Ballistics.Intercept(from, deck, course, 20f, Gravity, false, out Vector3 velocity, 8));

        // The time at which the pot is over the place the deck has reached by then.
        float flight = 0f;
        for (int i = 0; i < 40; i++)
        {
            flight = Ballistics.FlightSeconds(from, deck + course * flight, velocity);
        }

        Vector3 landing = Position(from, velocity, flight);
        Vector3 deckThen = deck + course * flight;
        Assert.True(Vector3.Distance(landing, deckThen) < 0.3f, $"lands at {landing}, the deck is at {deckThen}");
    }

    [Fact]
    public void AStandingTargetIsAimedAtAsItIs()
    {
        Vector3 from = Vector3.zero;
        Vector3 target = new(20f, 0f, 0f);

        Assert.True(Ballistics.Intercept(from, target, Vector3.zero, 25f, Gravity, false, out Vector3 led));
        Assert.True(Ballistics.LaunchVelocity(from, target, 25f, Gravity, false, out Vector3 plain));

        Assert.Equal(plain.x, led.x, 3);
        Assert.Equal(plain.y, led.y, 3);
        Assert.Equal(plain.z, led.z, 3);
    }

    [Fact]
    public void NoPassesAimsAtTheTargetWhereItIsNow()
    {
        Vector3 target = new(20f, 0f, 0f);

        Assert.True(Ballistics.Intercept(Vector3.zero, target, new Vector3(0f, 0f, 9f), 25f, Gravity, false, out Vector3 velocity, 0));
        Assert.True(Ballistics.LaunchVelocity(Vector3.zero, target, 25f, Gravity, false, out Vector3 plain));

        Assert.Equal(plain, velocity);
    }

    [Fact]
    public void LeadingTowardsTheShooterShortensTheFlight()
    {
        Vector3 from = Vector3.zero;
        Vector3 target = new(30f, 0f, 0f);

        Ballistics.Intercept(from, target, new Vector3(-4f, 0f, 0f), 25f, Gravity, false, out Vector3 towards);
        Ballistics.Intercept(from, target, new Vector3(4f, 0f, 0f), 25f, Gravity, false, out Vector3 away);

        // The flat arc is lower for a nearer aim point.
        Assert.True(towards.y < away.y);
    }

    [Fact]
    public void ATargetThatSailsOutOfReachGivesNoSolution()
    {
        // In reach now, but led to a place beyond the range of the throw.
        Vector3 target = new(30f, 0f, 0f);

        Assert.True(Ballistics.LaunchVelocity(Vector3.zero, target, 20f, Gravity, false, out _));
        Assert.False(Ballistics.Intercept(Vector3.zero, target, new Vector3(30f, 0f, 0f), 20f, Gravity, false, out Vector3 velocity));
        Assert.Equal(Vector3.zero, velocity);
    }
}

public class AmmoChoiceTests
{
    private static readonly string[] Order = { "Bolt", "Chain", "Grapnel" };

    private static Func<string, bool> Carrying(params string[] kinds)
    {
        HashSet<string> set = new(kinds);
        return set.Contains;
    }

    [Fact]
    public void TheSelectedKindIsFiredWhenItIsCarried()
    {
        Assert.Equal("Chain", AmmoChoice.Resolve(Order, Carrying("Bolt", "Chain"), "Chain"));
    }

    [Fact]
    public void AKindThatIsNotCarriedFallsBackToTheFirstOneThatIs()
    {
        Assert.Equal("Chain", AmmoChoice.Resolve(Order, Carrying("Chain", "Grapnel"), "Bolt"));
        Assert.Equal("Grapnel", AmmoChoice.Resolve(Order, Carrying("Grapnel"), "Chain"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Something else")]
    public void ASelectionThatIsNotInTheListFallsBackToo(string selected)
    {
        Assert.Equal("Bolt", AmmoChoice.Resolve(Order, Carrying("Bolt", "Chain"), selected));
    }

    [Fact]
    public void WithNoAmmunitionNothingIsFired()
    {
        Assert.Null(AmmoChoice.Resolve(Order, Carrying(), "Bolt"));
        Assert.Null(AmmoChoice.Resolve(Array.Empty<string>(), Carrying("Bolt"), "Bolt"));
        Assert.Null(AmmoChoice.Resolve(null, Carrying("Bolt"), "Bolt"));
        Assert.Null(AmmoChoice.Resolve(Order, null, "Bolt"));
    }

    [Fact]
    public void TheSwitchGoesToTheNextKindCarried()
    {
        Assert.Equal("Chain", AmmoChoice.Next(Order, Carrying("Bolt", "Chain", "Grapnel"), "Bolt"));
        Assert.Equal("Grapnel", AmmoChoice.Next(Order, Carrying("Bolt", "Chain", "Grapnel"), "Chain"));
    }

    [Fact]
    public void TheSwitchSkipsKindsThatAreNotCarriedAndWrapsRound()
    {
        Assert.Equal("Grapnel", AmmoChoice.Next(Order, Carrying("Bolt", "Grapnel"), "Bolt"));
        Assert.Equal("Bolt", AmmoChoice.Next(Order, Carrying("Bolt", "Grapnel"), "Grapnel"));
    }

    [Fact]
    public void WithOneKindCarriedTheSwitchStaysOnIt()
    {
        Assert.Equal("Chain", AmmoChoice.Next(Order, Carrying("Chain"), "Chain"));
        Assert.Equal("Chain", AmmoChoice.Next(Order, Carrying("Chain"), "Bolt"));
    }

    [Fact]
    public void TheSwitchFromAKindNotInTheListStartsAtTheTop()
    {
        Assert.Equal("Bolt", AmmoChoice.Next(Order, Carrying("Bolt", "Chain"), "Something else"));
        Assert.Equal("Bolt", AmmoChoice.Next(Order, Carrying("Bolt", "Chain"), null));
    }

    [Fact]
    public void WithNothingCarriedTheSwitchHasNowhereToGo()
    {
        Assert.Null(AmmoChoice.Next(Order, Carrying(), "Bolt"));
        Assert.Null(AmmoChoice.Next(Array.Empty<string>(), Carrying("Bolt"), "Bolt"));
        Assert.Null(AmmoChoice.Next(null, Carrying("Bolt"), "Bolt"));
    }

    [Fact]
    public void KindsCarriedCountsTheKindsNotTheItems()
    {
        Assert.Equal(0, AmmoChoice.KindsCarried(Order, Carrying()));
        Assert.Equal(2, AmmoChoice.KindsCarried(Order, Carrying("Bolt", "Grapnel")));
        Assert.Equal(0, AmmoChoice.KindsCarried(null, Carrying("Bolt")));
    }
}

public class GrappleRecordTests
{
    [Fact]
    public void ARecordSurvivesTheRoundTrip()
    {
        GrappleRecord record = new(123456789012L, 4242u, new Vector3(1.5f, 2f, -3f), new Vector3(-4f, 0.5f, 6f), 637000000000000000L);

        Assert.True(GrappleRecord.TryDecode(record.Encode(), out GrappleRecord read));

        Assert.Equal(123456789012L, read.OtherUser);
        Assert.Equal(4242u, read.OtherId);
        Assert.Equal(new Vector3(1.5f, 2f, -3f), read.Here);
        Assert.Equal(new Vector3(-4f, 0.5f, 6f), read.There);
        Assert.Equal(637000000000000000L, read.StartTicks);
    }

    [Fact]
    public void TheBytesAreAlwaysTheSameLength()
    {
        Assert.Equal(44, new GrappleRecord(1L, 1u, Vector3.zero, Vector3.one, 0L).Encode().Length);
    }

    [Fact]
    public void NoBytesAreNoRope()
    {
        Assert.False(GrappleRecord.TryDecode(null, out _));
        Assert.False(GrappleRecord.TryDecode(GrappleRecord.None, out _));
        Assert.False(GrappleRecord.TryDecode(new byte[43], out _));
    }

    [Fact]
    public void ARopeToNobodyIsNoRope()
    {
        byte[] bytes = new GrappleRecord(0L, 0u, Vector3.zero, Vector3.zero, 5L).Encode();

        Assert.False(GrappleRecord.TryDecode(bytes, out _));
    }

    [Fact]
    public void APointThatIsNotANumberIsNoRope()
    {
        Assert.False(GrappleRecord.TryDecode(new GrappleRecord(7L, 7u, new Vector3(float.NaN, 0f, 0f), Vector3.zero, 0L).Encode(), out _));
        Assert.False(GrappleRecord.TryDecode(new GrappleRecord(7L, 7u, Vector3.zero, new Vector3(0f, float.PositiveInfinity, 0f), 0L).Encode(), out _));
    }

    [Fact]
    public void ExtraBytesAreIgnored()
    {
        byte[] bytes = new GrappleRecord(7L, 9u, Vector3.up, Vector3.down, 11L).Encode();
        byte[] longer = new byte[bytes.Length + 10];
        bytes.CopyTo(longer, 0);

        Assert.True(GrappleRecord.TryDecode(longer, out GrappleRecord read));
        Assert.Equal(9u, read.OtherId);
    }
}

public class GrappleAccelerationTests
{
    [Fact]
    public void ASlackRopeDoesNotPull()
    {
        Assert.Equal(Vector3.zero, GrappleRules.Acceleration(new Vector3(5f, 0f, 0f), Vector3.zero, 6f, 1.5f, 0.5f, 0.5f));
        Assert.Equal(Vector3.zero, GrappleRules.Acceleration(new Vector3(6f, 0f, 0f), Vector3.zero, 6f, 1.5f, 0.5f, 0.5f));
    }

    [Fact]
    public void ATautRopePullsAlongItTowardsTheOtherEnd()
    {
        // 16 m apart, 6 m rope: 10 m of slack at 1.5 each is 15, and a ship takes half.
        Vector3 acceleration = GrappleRules.Acceleration(new Vector3(0f, 0f, -16f), Vector3.zero, 6f, 1.5f, 0.5f, 0.5f);

        Assert.Equal(0f, acceleration.x, 4);
        Assert.Equal(0f, acceleration.y, 4);
        Assert.Equal(-7.5f, acceleration.z, 4);
    }

    [Fact]
    public void TheRopeIsLevelWithTheWater()
    {
        Vector3 acceleration = GrappleRules.Acceleration(new Vector3(8f, 30f, 6f), Vector3.zero, 6f, 1f, 0f, 1f);

        // 10 m apart on the water and 4 m of slack: 4 along the rope, split by its direction (0.8, 0.6).
        Assert.Equal(0f, acceleration.y);
        Assert.Equal(3.2f, acceleration.x, 4);
        Assert.Equal(2.4f, acceleration.z, 4);
    }

    [Fact]
    public void ClosingSpeedTakesPullOff()
    {
        Vector3 still = GrappleRules.Acceleration(new Vector3(16f, 0f, 0f), Vector3.zero, 6f, 1.5f, 0.5f, 1f);
        Vector3 closing = GrappleRules.Acceleration(new Vector3(16f, 0f, 0f), new Vector3(10f, 0f, 0f), 6f, 1.5f, 0.5f, 1f);
        Vector3 opening = GrappleRules.Acceleration(new Vector3(16f, 0f, 0f), new Vector3(-10f, 0f, 0f), 6f, 1.5f, 0.5f, 1f);

        // 15 with no speed, 10 when closing at 10 m/s, 20 when moving apart.
        Assert.Equal(15f, still.x, 4);
        Assert.Equal(10f, closing.x, 4);
        Assert.Equal(20f, opening.x, 4);
    }

    [Fact]
    public void ARopeNeverPushes()
    {
        Vector3 acceleration = GrappleRules.Acceleration(new Vector3(7f, 0f, 0f), new Vector3(50f, 0f, 0f), 6f, 1.5f, 1f, 0.5f);

        Assert.Equal(Vector3.zero, acceleration);
    }

    [Fact]
    public void SpeedAcrossTheRopeIsNotDamped()
    {
        Vector3 acceleration = GrappleRules.Acceleration(new Vector3(16f, 0f, 0f), new Vector3(0f, 0f, 30f), 6f, 1.5f, 0.5f, 1f);

        Assert.Equal(15f, acceleration.x, 4);
    }

    [Fact]
    public void NegativeDampingAndShareDoNotReverseThePull()
    {
        Assert.Equal(15f, GrappleRules.Acceleration(new Vector3(16f, 0f, 0f), new Vector3(10f, 0f, 0f), 6f, 1.5f, -3f, 1f).x, 4);
        Assert.Equal(Vector3.zero, GrappleRules.Acceleration(new Vector3(16f, 0f, 0f), Vector3.zero, 6f, 1.5f, 0f, -1f));
    }

    [Fact]
    public void TwoShipsPullTheSameTotalAsOneShipWouldAlone()
    {
        Vector3 each = GrappleRules.Acceleration(new Vector3(16f, 0f, 0f), Vector3.zero, 6f, 1.5f, 0f, 0.5f);

        Assert.Equal(GrappleRules.Pull(16f, 6f, 1.5f), each.x * 2f, 4);
    }

    [Fact]
    public void TwoEndsOnTheSamePointGiveNoDirection()
    {
        Assert.Equal(Vector3.zero, GrappleRules.Acceleration(Vector3.zero, Vector3.zero, 0f, 1f, 0f, 1f));
        Assert.Equal(Vector3.zero, GrappleRules.Acceleration(new Vector3(0f, 9f, 0f), Vector3.zero, 0f, 1f, 0f, 1f));
    }

    [Fact]
    public void ARopeAgesByTheWorldClock()
    {
        Assert.Equal(90f, GrappleRules.AgeSeconds(1000L, 1000L + 90L * TimeSpan.TicksPerSecond), 3);
        Assert.Equal(0f, GrappleRules.AgeSeconds(2000L, 1000L));
        Assert.Equal(0f, GrappleRules.AgeSeconds(1000L, 1000L));
    }
}

public class SailTearTests
{
    [Theory]
    [InlineData(0L, 100L, false)]
    [InlineData(100L, 100L, false)]
    [InlineData(101L, 100L, true)]
    [InlineData(100L, 200L, false)]
    public void ASailIsTornUntilItsTimeIsOut(long until, long now, bool expected)
    {
        Assert.Equal(expected, WeaponRules.IsTorn(until, now));
    }

    [Theory]
    [InlineData(false, 0.5f, 1f)]
    [InlineData(true, 0.5f, 0.5f)]
    [InlineData(true, 0f, 1f)]
    [InlineData(true, 1f, 0f)]
    [InlineData(true, 2f, 0f)]
    [InlineData(true, -1f, 1f)]
    public void ATornSailLosesItsShareOfTheForce(bool torn, float share, float expected)
    {
        Assert.Equal(expected, WeaponRules.TornFactor(torn, share), 4);
    }

    [Fact]
    public void TheTearEndsAtTheSetTimeFromNow()
    {
        long now = 5_000_000_000L;
        long until = FireRules.Expiry(now, 45f);

        Assert.True(WeaponRules.IsTorn(until, now + 44L * TimeSpan.TicksPerSecond));
        Assert.False(WeaponRules.IsTorn(until, now + 45L * TimeSpan.TicksPerSecond));
    }
}

public class GrappledRaiderTests
{
    private static readonly RaiderTuning Tuning = new(25f, 40f, 6f, 0.25f, 0.2f);

    private static RaiderInput Input(RaiderState state, float crewShare, float hullShare, bool hasMelee, bool grappled)
    {
        return new RaiderInput(state, new Vector2(20f, 0f), 0f, Vector2.zero, Vector2.zero, 0f, crewShare, hullShare, 0f, 1, hasMelee, grappled);
    }

    [Theory]
    [InlineData(0.1f, 1f)]
    [InlineData(1f, 0.1f)]
    [InlineData(0.1f, 0.1f)]
    public void ABoundRaiderDoesNotFleeHoweverBadlyItIsHurt(float crewShare, float hullShare)
    {
        RaiderOrder order = RaiderTactics.Decide(Input(RaiderState.Approach, crewShare, hullShare, true, true), Tuning);

        Assert.NotEqual(RaiderState.Flee, order.State);
    }

    [Fact]
    public void ARaiderThatWasFleeingStopsWhenTheRopeBindsIt()
    {
        RaiderOrder order = RaiderTactics.Decide(Input(RaiderState.Flee, 0.1f, 0.1f, true, true), Tuning);

        Assert.Equal(RaiderState.Board, order.State);
    }

    [Fact]
    public void ABoundRaiderWithWarriorsGoesStraightToBoarding()
    {
        Assert.Equal(RaiderState.Board, RaiderTactics.Decide(Input(RaiderState.Approach, 1f, 1f, true, true), Tuning).State);
        Assert.Equal(RaiderState.Board, RaiderTactics.Decide(Input(RaiderState.Broadside, 1f, 1f, true, true), Tuning).State);
    }

    [Fact]
    public void ABoundRaiderWithOnlyArchersStaysAlongside()
    {
        Assert.Equal(RaiderState.Broadside, RaiderTactics.Decide(Input(RaiderState.Approach, 1f, 1f, false, true), Tuning).State);
    }

    [Fact]
    public void ARaiderThatIsNotBoundStillFlees()
    {
        Assert.Equal(RaiderState.Flee, RaiderTactics.Decide(Input(RaiderState.Approach, 0.1f, 1f, true, false), Tuning).State);
    }

    [Theory]
    [InlineData(RaiderState.Prize)]
    [InlineData(RaiderState.Moored)]
    public void APrizeOrAMooredShipIsNotMovedByARope(RaiderState state)
    {
        Assert.Equal(state, RaiderTactics.Decide(Input(state, 1f, 1f, true, true), Tuning).State);
    }
}

public class FriendlyFireTests
{
    [Theory]
    // target on raider ship, has attacker, attacker is raider, turret shot, expected
    [InlineData(true, true, true, false, true)]
    [InlineData(true, true, true, true, true)]
    // The player, or any other attacker, still harms a Nail Ship.
    [InlineData(true, true, false, false, false)]
    [InlineData(true, true, false, true, false)]
    // A turret shot with no owner is a Nail Ship's ballista.
    [InlineData(true, false, false, true, true)]
    [InlineData(true, false, false, false, false)]
    // Other ships are never protected.
    [InlineData(false, true, true, false, false)]
    [InlineData(false, false, false, true, false)]
    public void OnlyTheOwnSidesHitsOnANailShipAreIgnored(bool onRaider, bool hasAttacker, bool attackerRaider, bool turret, bool expected)
    {
        Assert.Equal(expected, FriendlyFire.IgnoresHit(onRaider, hasAttacker, attackerRaider, turret));
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, false)]
    public void OnlyTwoNailShipsIgnoreEachOthersCollision(bool self, bool other, bool expected)
    {
        Assert.Equal(expected, FriendlyFire.IgnoresCollision(self, other));
    }
}
