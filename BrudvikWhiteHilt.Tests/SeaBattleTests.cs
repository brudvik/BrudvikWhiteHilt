using BrudvikWhiteHilt.Pieces.Ships.Ballista;
using BrudvikWhiteHilt.SeaBattles;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Xunit;

namespace BrudvikWhiteHilt.Tests;

public class CrewTableTests
{
    [Fact]
    public void TheDefaultTextHasFiveTiers()
    {
        IReadOnlyList<CrewTier> tiers = CrewTable.Parse(SeaBattleSettings.DefaultCrewTiers);

        Assert.Equal(new[] { "defeated_bonemass", "defeated_dragon", "defeated_goblinking", "defeated_queen", "defeated_fader" },
            tiers.Select(tier => tier.Key));
        Assert.Equal(new[] { 0, 1, 2, 3, 4 }, tiers.Select(tier => tier.Index));
        Assert.Equal(new[] { "Draugr", "Draugr_Ranged", "Draugr_Elite" }, tiers[1].Entries.Select(entry => entry.Prefab));
        Assert.Equal(new[] { 3, 2, 1 }, tiers[1].Entries.Select(entry => entry.Count));
        Assert.Equal(new[] { false, true, true }, tiers[4].Entries.Select(entry => entry.Archer));
    }

    [Fact]
    public void TheOldDefaultIsTheNewOneCutAtYagluth()
    {
        Assert.StartsWith(SeaBattleSettings.OldDefaultCrewTiers, SeaBattleSettings.DefaultCrewTiers);
        Assert.Equal(3, CrewTable.Parse(SeaBattleSettings.OldDefaultCrewTiers).Count);
    }

    [Fact]
    public void SpacesAreIgnored()
    {
        IReadOnlyList<CrewTier> tiers = CrewTable.Parse("  key_a = Draugr : 2 ,  Draugr_Ranged:1 ;\n key_b=Goblin:4  ");

        Assert.Equal(2, tiers.Count);
        Assert.Equal("key_a", tiers[0].Key);
        Assert.Equal(2, tiers[0].Entries[0].Count);
        Assert.Equal("Draugr_Ranged", tiers[0].Entries[1].Prefab);
        Assert.Equal("Goblin", tiers[1].Entries[0].Prefab);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData(";;;")]
    [InlineData("no equals sign")]
    [InlineData("=Draugr:1")]
    [InlineData("key=")]
    [InlineData("key=:3")]
    public void BrokenTextGivesNoTiersAndDoesNotThrow(string text)
    {
        Assert.Empty(CrewTable.Parse(text));
    }

    [Fact]
    public void BrokenPartsAreSkippedAndTheRestIsKept()
    {
        IReadOnlyList<CrewTier> tiers = CrewTable.Parse("broken; a=Draugr:x,Draugr:0,Draugr:-1,Draugr_Elite:2,:5; b=; c=Goblin");

        Assert.Equal(new[] { "a", "c" }, tiers.Select(tier => tier.Key));
        Assert.Single(tiers[0].Entries);
        Assert.Equal("Draugr_Elite", tiers[0].Entries[0].Prefab);
        Assert.Equal(1, tiers[1].Entries[0].Count);
        // The index counts valid tiers only, so it matches the position in the list.
        Assert.Equal(1, tiers[1].Index);
    }

    [Fact]
    public void TheLastTierWhoseKeyIsSetWins()
    {
        IReadOnlyList<CrewTier> tiers = CrewTable.Parse(SeaBattleSettings.DefaultCrewTiers);

        Assert.Equal("defeated_bonemass", CrewTable.Pick(tiers, key => key == "defeated_bonemass").Key);
        Assert.Equal("defeated_dragon", CrewTable.Pick(tiers, key => key is "defeated_bonemass" or "defeated_dragon").Key);
        Assert.Equal("defeated_fader", CrewTable.Pick(tiers, _ => true).Key);
        // A later key wins even when an earlier one is not set.
        Assert.Equal("defeated_goblinking", CrewTable.Pick(tiers, key => key == "defeated_goblinking").Key);
    }

    [Fact]
    public void NoKeySetMeansNoTier()
    {
        IReadOnlyList<CrewTier> tiers = CrewTable.Parse(SeaBattleSettings.DefaultCrewTiers);

        Assert.Null(CrewTable.Pick(tiers, _ => false));
        Assert.Null(CrewTable.Pick(null, _ => true));
        Assert.Null(CrewTable.Pick(tiers, null));
    }

    [Theory]
    [InlineData("Draugr_Ranged", true)]
    [InlineData("GoblinArcher", true)]
    [InlineData("Draugr", false)]
    [InlineData("Draugr_Elite", false)]
    [InlineData("GoblinBrute", false)]
    [InlineData("Charred_Mage", true)]
    [InlineData("Charred_Melee", false)]
    public void ArchersAreFoundByName(string prefab, bool archer)
    {
        Assert.Equal(archer, CrewTable.IsArcher(prefab));
        Assert.Equal(archer, CrewTable.ParseEntries(prefab + ":1")[0].Archer);
    }

    [Fact]
    public void AGuardListHasNoKeys()
    {
        List<CrewEntry> guards = CrewTable.ParseEntries(SeaBattleSettings.Raids.DefaultGuards);

        Assert.Equal(new[] { 4, 2, 1 }, guards.Select(guard => guard.Count));
        Assert.Equal(new[] { false, true, false }, guards.Select(guard => guard.Archer));
    }
}

public class RaiderTacticsTests
{
    private static readonly RaiderTuning tuning = new(25f, 40f, 6f, 0.25f, 0.2f);

    private static RaiderInput Input(RaiderState state, Vector2 self, Vector2 target, float secondsInState = 0f,
        float crewShare = 1f, float hullShare = 1f, bool hasMelee = true, Vector2 targetVelocity = default,
        float targetHeading = 0f, int side = 1, float selfHeading = 0f, bool canRam = false)
    {
        return new RaiderInput(state, self, selfHeading, target, targetVelocity, targetHeading, crewShare, hullShare,
            secondsInState, side, hasMelee, false, canRam);
    }

    [Fact]
    public void ApproachSailsFullForTheTargetFarAway()
    {
        RaiderOrder order = RaiderTactics.Decide(Input(RaiderState.Approach, new Vector2(0f, -200f), Vector2.zero), tuning);

        Assert.Equal(RaiderState.Approach, order.State);
        Assert.Equal(RaiderSail.Full, order.Sail);
        Assert.Equal(0f, order.Course, 2);
    }

    [Fact]
    public void ApproachBecomesBroadsideWithinOneAndAHalfBroadsideDistances()
    {
        // 1.5 x 25 = 37.5 m.
        Assert.Equal(RaiderState.Approach, RaiderTactics.Decide(Input(RaiderState.Approach, new Vector2(0f, -38f), Vector2.zero), tuning).State);
        Assert.Equal(RaiderState.Broadside, RaiderTactics.Decide(Input(RaiderState.Approach, new Vector2(0f, -37f), Vector2.zero), tuning).State);
    }

    [Fact]
    public void ApproachSailsForTheLeadPointOfAMovingTarget()
    {
        // The target is 160 m east, sailing north at 10 m/s: the lead time is 160 / 8 = 20 s, capped at 15 s.
        Vector2 self = Vector2.zero;
        Vector2 target = new(160f, 0f);
        Vector2 velocity = new(0f, 10f);

        Vector2 lead = RaiderTactics.LeadPoint(self, target, velocity);
        RaiderOrder order = RaiderTactics.Decide(Input(RaiderState.Approach, self, target, targetVelocity: velocity), tuning);

        Assert.Equal(160f, lead.x, 3);
        Assert.Equal(150f, lead.y, 3);
        Assert.Equal(RaiderTactics.CourseTo(self, lead, 0f), order.Course, 3);
        Assert.InRange(order.Course, 40f, 90f);
    }

    [Fact]
    public void TheLeadTimeGrowsWithTheDistance()
    {
        Vector2 velocity = new(0f, 8f);

        Assert.Equal(8f, RaiderTactics.LeadPoint(Vector2.zero, new Vector2(8f, 0f), velocity).y, 3);
        Assert.Equal(120f, RaiderTactics.LeadPoint(Vector2.zero, new Vector2(1000f, 0f), velocity).y, 3);
    }

    [Fact]
    public void BroadsideAimsForThePointBesideTheTarget()
    {
        // The target sails north. Its starboard side is east: the station is 25 m east of it.
        Vector2 station = RaiderTactics.StationPoint(Vector2.zero, 0f, 1, 25f);
        Vector2 port = RaiderTactics.StationPoint(Vector2.zero, 0f, -1, 25f);

        Assert.Equal(25f, station.x, 3);
        Assert.Equal(0f, station.y, 3);
        Assert.Equal(-25f, port.x, 3);

        // Facing east, starboard is south.
        Vector2 south = RaiderTactics.StationPoint(Vector2.zero, 90f, 1, 25f);
        Assert.Equal(0f, south.x, 3);
        Assert.Equal(-25f, south.y, 3);
    }

    [Fact]
    public void BroadsideSailsHalfForTheStationPointWhileFarFromIt()
    {
        // The target sails on heading 40, so its starboard station is about (19, -16): well away from here.
        Vector2 self = new(40f, 0f);
        Vector2 station = RaiderTactics.StationPoint(Vector2.zero, 40f, 1, 25f);

        RaiderOrder order = RaiderTactics.Decide(
            Input(RaiderState.Broadside, self, Vector2.zero, targetHeading: 40f, side: 1, secondsInState: 5f), tuning);

        Assert.Equal(RaiderState.Broadside, order.State);
        Assert.Equal(RaiderSail.Half, order.Sail);
        Assert.Equal(RaiderTactics.CourseTo(self, station, 0f), order.Course, 2);
        Assert.True(Mathf.Abs(order.Course - 40f) > 1f);
    }

    [Fact]
    public void BroadsideHeadsForTheTargetsHeadingWithinTenMetresOfTheStation()
    {
        // The starboard station of a target on heading 30 is about (21.7, -12.5); this is 2.5 m from it.
        Vector2 self = new(22f, -10f);

        RaiderOrder order = RaiderTactics.Decide(
            Input(RaiderState.Broadside, self, Vector2.zero, targetHeading: 30f, side: 1), tuning);

        Assert.Equal(30f, order.Course, 2);
    }

    [Fact]
    public void BroadsideSailsFullWhenMoreThanTwiceTheDistanceAway()
    {
        Assert.Equal(RaiderSail.Full, RaiderTactics.Decide(Input(RaiderState.Broadside, new Vector2(60f, 0f), Vector2.zero), tuning).Sail);
        Assert.Equal(RaiderSail.Half, RaiderTactics.Decide(Input(RaiderState.Broadside, new Vector2(40f, 0f), Vector2.zero), tuning).Sail);
    }

    [Fact]
    public void BroadsideBecomesBoardAfterItsTimeWhenThereIsMelee()
    {
        Assert.Equal(RaiderState.Broadside,
            RaiderTactics.Decide(Input(RaiderState.Broadside, new Vector2(25f, 0f), Vector2.zero, secondsInState: 39f), tuning).State);
        Assert.Equal(RaiderState.Board,
            RaiderTactics.Decide(Input(RaiderState.Broadside, new Vector2(25f, 0f), Vector2.zero, secondsInState: 40f), tuning).State);
    }

    [Fact]
    public void BroadsideNeverBoardsWithoutMeleeOrWithZeroSeconds()
    {
        RaiderInput archersOnly = Input(RaiderState.Broadside, new Vector2(25f, 0f), Vector2.zero, secondsInState: 500f, hasMelee: false);
        RaiderInput melee = Input(RaiderState.Broadside, new Vector2(25f, 0f), Vector2.zero, secondsInState: 500f);

        Assert.Equal(RaiderState.Broadside, RaiderTactics.Decide(archersOnly, tuning).State);
        Assert.Equal(RaiderState.Broadside, RaiderTactics.Decide(melee, new RaiderTuning(25f, 0f, 6f, 0.25f, 0.2f)).State);
    }

    [Fact]
    public void BoardClosesInToSixtyPercentOfTheBoardDistanceAtHalfSail()
    {
        // Station: 6 x 0.6 = 3.6 m east of the target. Far out, the ship sails for it.
        Vector2 self = new(30f, 0f);
        RaiderOrder far = RaiderTactics.Decide(Input(RaiderState.Board, self, Vector2.zero), tuning);

        Assert.Equal(RaiderState.Board, far.State);
        Assert.Equal(RaiderSail.Half, far.Sail);
        Assert.Equal(RaiderTactics.CourseTo(self, new Vector2(3.6f, 0f), 0f), far.Course, 2);

        // Close in, it lines up with the target.
        RaiderOrder near = RaiderTactics.Decide(Input(RaiderState.Board, new Vector2(5f, 0f), Vector2.zero, targetHeading: 70f), tuning);
        Assert.Equal(70f, near.Course, 2);
    }

    [Theory]
    [InlineData(RaiderState.Approach)]
    [InlineData(RaiderState.Broadside)]
    [InlineData(RaiderState.Board)]
    public void FleeWinsWhenTheCrewShareIsLowEnough(RaiderState state)
    {
        RaiderOrder order = RaiderTactics.Decide(Input(state, new Vector2(0f, -30f), Vector2.zero, crewShare: 0.25f), tuning);

        Assert.Equal(RaiderState.Flee, order.State);
        Assert.Equal(RaiderSail.Full, order.Sail);
        // Straight away from the target, which lies north of it.
        Assert.Equal(180f, order.Course, 2);
    }

    [Theory]
    [InlineData(RaiderState.Approach)]
    [InlineData(RaiderState.Broadside)]
    [InlineData(RaiderState.Board)]
    public void FleeWinsWhenTheHullShareIsLowEnough(RaiderState state)
    {
        RaiderOrder order = RaiderTactics.Decide(Input(state, new Vector2(30f, 0f), Vector2.zero, hullShare: 0.2f), tuning);

        Assert.Equal(RaiderState.Flee, order.State);
        Assert.Equal(90f, order.Course, 2);
    }

    [Fact]
    public void ARaiderThatFleesKeepsFleeing()
    {
        RaiderOrder order = RaiderTactics.Decide(Input(RaiderState.Flee, new Vector2(0f, 50f), Vector2.zero), tuning);

        Assert.Equal(RaiderState.Flee, order.State);
        Assert.Equal(0f, order.Course, 2);
    }

    [Fact]
    public void ThePrizeAndAMooredShipOnlyStopAndKeepTheirCourse()
    {
        foreach (RaiderState state in new[] { RaiderState.Prize, RaiderState.Moored })
        {
            RaiderOrder order = RaiderTactics.Decide(
                Input(state, new Vector2(10f, 10f), Vector2.zero, crewShare: 0f, hullShare: 0f, selfHeading: 123f), tuning);

            Assert.Equal(state, order.State);
            Assert.Equal(RaiderSail.Stop, order.Sail);
            Assert.Equal(123f, order.Course, 2);
        }
    }

    [Theory]
    [InlineData(RaiderState.Approach)]
    [InlineData(RaiderState.Broadside)]
    [InlineData(RaiderState.Board)]
    [InlineData(RaiderState.Flee)]
    public void NothingIsNotANumberWhenTheShipsAreOnTopOfEachOther(RaiderState state)
    {
        RaiderOrder order = RaiderTactics.Decide(
            Input(state, Vector2.zero, Vector2.zero, secondsInState: 100f, targetHeading: 30f, selfHeading: 15f), tuning);

        Assert.False(float.IsNaN(order.Course));
        Assert.InRange(order.Course, 0f, 360f);
    }

    [Theory]
    [InlineData(0f, 10f, 0f)]
    [InlineData(10f, 0f, 90f)]
    [InlineData(0f, -10f, 180f)]
    [InlineData(-10f, 0f, 270f)]
    public void CoursesRunClockwiseFromNorth(float x, float z, float course)
    {
        Assert.Equal(course, RaiderTactics.CourseTo(Vector2.zero, new Vector2(x, z), 0f), 3);
    }

    [Theory]
    [InlineData(RaiderState.Broadside)]
    [InlineData(RaiderState.Board)]
    [InlineData(RaiderState.Ram)]
    public void AShipThatFallsFarBehindGoesBackToTheApproach(RaiderState state)
    {
        // Three broadside distances is 75 m.
        Assert.Equal(state == RaiderState.Ram ? RaiderState.Ram : state,
            RaiderTactics.Decide(Input(state, new Vector2(74f, 0f), Vector2.zero, canRam: true), tuning).State);
        RaiderOrder order = RaiderTactics.Decide(Input(state, new Vector2(76f, 0f), Vector2.zero), tuning);

        Assert.Equal(RaiderState.Approach, order.State);
        Assert.Equal(RaiderSail.Full, order.Sail);
    }

    [Fact]
    public void BoardingSailsFullWhenTheTargetGetsAway()
    {
        Assert.Equal(RaiderSail.Full, RaiderTactics.Decide(Input(RaiderState.Board, new Vector2(60f, 0f), Vector2.zero), tuning).Sail);
    }

    [Fact]
    public void AlongsideAMovingTargetTheStationIsLed()
    {
        // The target sails north at 8 m/s; the ship is 40 m south-east of its starboard station, so it is led 4 s ahead.
        Vector2 self = new(25f, -40f);
        Vector2 velocity = new(0f, 8f);
        RaiderOrder order = RaiderTactics.Decide(Input(RaiderState.Broadside, self, Vector2.zero, targetVelocity: velocity), tuning);

        Vector2 led = RaiderTactics.StationPoint(velocity * 4f, 0f, 1, 25f);
        Assert.Equal(RaiderTactics.CourseTo(self, led, 0f), order.Course, 2);
    }

    [Fact]
    public void NearAMovingTargetTheShipMatchesItsSpeed()
    {
        Vector2 velocity = new(0f, 6f);

        // Level with the station: half sail. Behind it: full. Run past it: no sail.
        Assert.Equal(RaiderSail.Half, RaiderTactics.Decide(Input(RaiderState.Broadside, new Vector2(25f, 0f), Vector2.zero, targetVelocity: velocity), tuning).Sail);
        Assert.Equal(RaiderSail.Full, RaiderTactics.Decide(Input(RaiderState.Broadside, new Vector2(25f, -8f), Vector2.zero, targetVelocity: velocity), tuning).Sail);
        Assert.Equal(RaiderSail.Stop, RaiderTactics.Decide(Input(RaiderState.Broadside, new Vector2(25f, 8f), Vector2.zero, targetVelocity: velocity), tuning).Sail);
    }

    [Fact]
    public void AShipWithAReadyRamChargesASlowTargetInRange()
    {
        RaiderOrder order = RaiderTactics.Decide(Input(RaiderState.Broadside, new Vector2(30f, 0f), Vector2.zero, canRam: true), tuning);

        Assert.Equal(RaiderState.Ram, order.State);
        Assert.Equal(RaiderSail.Full, order.Sail);
        Assert.Equal(270f, order.Course, 2);
    }

    [Fact]
    public void NoChargeAtAFastTargetTooCloseOrWithoutARam()
    {
        Assert.Equal(RaiderState.Broadside,
            RaiderTactics.Decide(Input(RaiderState.Broadside, new Vector2(30f, 0f), Vector2.zero, canRam: true, targetVelocity: new Vector2(0f, 3f)), tuning).State);
        Assert.Equal(RaiderState.Broadside,
            RaiderTactics.Decide(Input(RaiderState.Broadside, new Vector2(10f, 0f), Vector2.zero, canRam: true), tuning).State);
        Assert.Equal(RaiderState.Broadside,
            RaiderTactics.Decide(Input(RaiderState.Broadside, new Vector2(30f, 0f), Vector2.zero), tuning).State);
    }

    [Fact]
    public void AChargeEndsWhenItStrikesTakesTooLongOrTheTargetRuns()
    {
        Assert.Equal(RaiderState.Ram, RaiderTactics.Decide(Input(RaiderState.Ram, new Vector2(20f, 0f), Vector2.zero, secondsInState: 5f), tuning).State);
        Assert.Equal(RaiderState.Broadside, RaiderTactics.Decide(Input(RaiderState.Ram, new Vector2(5f, 0f), Vector2.zero), tuning).State);
        Assert.Equal(RaiderState.Broadside, RaiderTactics.Decide(Input(RaiderState.Ram, new Vector2(20f, 0f), Vector2.zero, secondsInState: 15f), tuning).State);
        Assert.Equal(RaiderState.Broadside,
            RaiderTactics.Decide(Input(RaiderState.Ram, new Vector2(20f, 0f), Vector2.zero, targetVelocity: new Vector2(0f, 6f)), tuning).State);
    }

    [Theory]
    // The ship lies east of a target sailing north: on its starboard side.
    [InlineData(0, 0, 1)]
    [InlineData(1, 1, 1)]
    // More defenders at the starboard rail: it crosses to port.
    [InlineData(0, 2, -1)]
    // More at port: it stays.
    [InlineData(2, 0, 1)]
    public void TheShipKeepsItsSideUnlessFewerDefendTheOther(int port, int starboard, int expected)
    {
        Assert.Equal(expected, RaiderTactics.PickSide(new Vector2(30f, 0f), Vector2.zero, 0f, port, starboard));
    }

    [Fact]
    public void AShipOnThePortSideKeepsPort()
    {
        Assert.Equal(-1, RaiderTactics.PickSide(new Vector2(-30f, 5f), Vector2.zero, 0f, 0, 0));
        Assert.Equal(1, RaiderTactics.PickSide(new Vector2(-30f, 5f), Vector2.zero, 0f, 3, 1));
    }
}

public class TierValueTests
{
    private static readonly string[] reached = { "defeated_bonemass", "defeated_dragon" };

    [Fact]
    public void TheLastKeyThatIsSetGivesTheNumber()
    {
        string text = "defeated_dragon=1.25; defeated_goblinking=1.5; defeated_bonemass=1.1";

        Assert.Equal(1.1f, TierValues.PickNumber(text, key => reached.Contains(key), 1f), 4);
        Assert.Equal(1.25f, TierValues.PickNumber("defeated_dragon=1.25; defeated_goblinking=1.5", key => reached.Contains(key), 1f), 4);
        Assert.Equal(1f, TierValues.PickNumber(text, _ => false, 1f), 4);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("broken")]
    [InlineData("=2")]
    [InlineData("defeated_dragon=lots")]
    [InlineData("defeated_dragon=NaN")]
    public void ABrokenTextGivesTheFallback(string text)
    {
        Assert.Equal(3f, TierValues.PickNumber(text, key => reached.Contains(key), 3f), 4);
        Assert.Equal(3f, TierValues.PickNumber(text, null, 3f), 4);
    }

    [Fact]
    public void ALootListKeepsItsCommasAndColons()
    {
        string text = "defeated_dragon=Silver:3-8:6, Iron:1-2:4; defeated_goblinking=BlackMetal:3-8:6";

        Assert.Equal("Silver:3-8:6, Iron:1-2:4", TierValues.PickText(text, key => reached.Contains(key)));
        Assert.Equal("BlackMetal:3-8:6", TierValues.PickText(text, _ => true));
        Assert.Null(TierValues.PickText(text, _ => false));
        Assert.Null(TierValues.PickText("defeated_dragon=  ", _ => true));
        Assert.Null(TierValues.PickText(text, null));
    }

    [Fact]
    public void TheDefaultTiersRead()
    {
        Assert.Equal(2.5f, TierValues.PickNumber(SeaBattleSettings.DefaultHullTiers, _ => true, 1f), 4);
        Assert.Equal(2f, TierValues.PickNumber(SeaBattleSettings.DefaultCrewLevels, _ => true, 1f), 4);
        Assert.Equal(2f, TierValues.PickNumber(SeaBattleSettings.DefaultLeapTiers, _ => true, 1f), 4);
        Assert.Equal(1f, TierValues.PickNumber(SeaBattleSettings.DefaultLeapTiers, key => reached.Contains(key), 1f), 4);
        Assert.StartsWith("Coins:120-300:10, FlametalNew", TierValues.PickText(SeaBattleSettings.DefaultLootTiers, _ => true));
        Assert.Null(TierValues.PickText(SeaBattleSettings.DefaultLootTiers, key => key == "defeated_bonemass"));
    }
}

public class EncounterExtrasTests
{
    [Theory]
    [InlineData(0f, 0)]
    [InlineData(22f, 0)]
    [InlineData(-22f, 0)]
    [InlineData(45f, 1)]
    [InlineData(90f, 2)]
    [InlineData(135f, 3)]
    [InlineData(180f, 4)]
    [InlineData(-135f, 5)]
    [InlineData(270f, 6)]
    [InlineData(-45f, 7)]
    [InlineData(float.NaN, 0)]
    public void BearingsRunClockwiseFromDeadAhead(float relative, int expected)
    {
        Assert.Equal(expected, EncounterRules.Bearing(relative));
        Assert.Equal(8, EncounterRules.BearingNames.Length);
    }

    [Fact]
    public void TheWeatherMultipliesTheChance()
    {
        Assert.Equal(1f, EncounterRules.WeatherFactor(0, 1.5f, 2f, 3f), 4);
        Assert.Equal(1.5f, EncounterRules.WeatherFactor(EncounterRules.WeatherNight, 1.5f, 2f, 3f), 4);
        Assert.Equal(9f, EncounterRules.WeatherFactor(EncounterRules.WeatherNight | EncounterRules.WeatherStorm | EncounterRules.WeatherFog, 1.5f, 2f, 3f), 4);
        Assert.Equal(0f, EncounterRules.WeatherFactor(EncounterRules.WeatherFog, 1f, 1f, -2f), 4);
    }

    [Theory]
    [InlineData(1, false, false)]
    [InlineData(3, true, false)]
    [InlineData(2, true, true)]
    [InlineData(1, false, true)]
    public void TheShipsOfAnOrderPackIntoOneNumber(int size, bool fireShip, bool ram)
    {
        int packed = EncounterRules.PackShips(size, fireShip, ram);

        Assert.Equal(size, EncounterRules.UnpackShips(packed, out bool fire, out bool forced));
        Assert.Equal(fireShip, fire);
        Assert.Equal(ram, forced);
    }

    [Fact]
    public void APlainSizeReadsAsThatSize()
    {
        Assert.Equal(3, EncounterRules.UnpackShips(3, out bool fire, out bool ram));
        Assert.False(fire);
        Assert.False(ram);
    }

    [Theory]
    [InlineData(false, 0f, 1)]
    [InlineData(false, 0.49f, 1)]
    [InlineData(false, 0.5f, 3)]
    [InlineData(true, 0f, 1)]
    [InlineData(true, 0.4f, 2)]
    [InlineData(true, 0.99f, 3)]
    public void TheMerchantsTauntIsOnlyShoutedAtAMerchant(bool atMerchant, float roll, int expected)
    {
        Assert.Equal(expected, RaiderVoice.TauntNumber(atMerchant, roll));
    }
}

public class BallistaAimTests
{
    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(90f, 90f)]
    [InlineData(180f, 180f)]
    [InlineData(-180f, 180f)]
    [InlineData(190f, -170f)]
    [InlineData(-190f, 170f)]
    [InlineData(540f, 180f)]
    [InlineData(-350f, 10f)]
    [InlineData(720f, 0f)]
    public void AnglesWrapIntoHalfATurnEitherSide(float degrees, float wrapped)
    {
        Assert.Equal(wrapped, BallistaAim.Wrap(degrees), 3);
    }

    [Fact]
    public void AnAimInsideTheArcIsKept()
    {
        (float yaw, float pitch) = BallistaAim.Clamp(30f, 10f, 70f, -10f, 25f);

        Assert.Equal(30f, yaw, 3);
        Assert.Equal(10f, pitch, 3);
    }

    [Theory]
    [InlineData(70f, 70f)]
    [InlineData(71f, 70f)]
    [InlineData(120f, 70f)]
    [InlineData(-70f, -70f)]
    [InlineData(-71f, -70f)]
    [InlineData(-120f, -70f)]
    public void TheYawStopsAtTheLimitsOfTheArc(float wanted, float reached)
    {
        Assert.Equal(reached, BallistaAim.Clamp(wanted, 0f, 70f, -10f, 25f).Yaw, 3);
    }

    [Theory]
    [InlineData(170f, 70f)]
    [InlineData(-170f, -70f)]
    [InlineData(350f, -10f)]
    [InlineData(-350f, 10f)]
    public void AYawGivenAsALargeAngleIsWrappedBeforeItIsClamped(float wanted, float reached)
    {
        Assert.Equal(reached, BallistaAim.Clamp(wanted, 0f, 70f, -10f, 25f).Yaw, 3);
    }

    [Theory]
    [InlineData(-10f, -10f)]
    [InlineData(-11f, -10f)]
    [InlineData(-80f, -10f)]
    [InlineData(25f, 25f)]
    [InlineData(26f, 25f)]
    [InlineData(90f, 25f)]
    public void ThePitchStopsAtTheLimits(float wanted, float reached)
    {
        Assert.Equal(reached, BallistaAim.Clamp(0f, wanted, 70f, -10f, 25f).Pitch, 3);
    }

    [Fact]
    public void LimitsGivenTheWrongWayRoundSwapPlaces()
    {
        Assert.Equal(25f, BallistaAim.Clamp(0f, 40f, 70f, 25f, -10f).Pitch, 3);
        Assert.Equal(-10f, BallistaAim.Clamp(0f, -40f, 70f, 25f, -10f).Pitch, 3);
    }

    [Fact]
    public void ANegativeArcMeansNoTurning()
    {
        Assert.Equal(0f, BallistaAim.Clamp(45f, 0f, -5f, -10f, 25f).Yaw, 3);
    }

    [Theory]
    [InlineData(0f, 0f, 1f, 0f, 0f)]
    [InlineData(1f, 0f, 0f, 90f, 0f)]
    [InlineData(-1f, 0f, 0f, -90f, 0f)]
    [InlineData(0f, 0f, -1f, 180f, 0f)]
    [InlineData(0f, 1f, 1f, 0f, 45f)]
    [InlineData(0f, -1f, 1f, 0f, -45f)]
    [InlineData(1f, 1f, 0f, 90f, 45f)]
    [InlineData(0f, 1f, 0f, 0f, 90f)]
    public void DirectionsTurnIntoYawAndPitch(float x, float y, float z, float yaw, float pitch)
    {
        (float gotYaw, float gotPitch) = BallistaAim.FromDirection(x, y, z);

        Assert.Equal(yaw, gotYaw, 3);
        Assert.Equal(pitch, gotPitch, 3);
    }

    [Fact]
    public void NoDirectionLooksStraightAhead()
    {
        Assert.Equal((0f, 0f), BallistaAim.FromDirection(0f, 0f, 0f));
    }
}

public class EncounterRulesTests
{
    private const float Sea = -50f;
    private const float Water = 30f;
    private const float Land = 40f;

    [Theory]
    [InlineData(4f, 2f, 1, 4f)]
    [InlineData(4f, 2f, 3, 8f)]
    [InlineData(4f, 2f, 0, 0f)]
    [InlineData(0f, 2f, 3, 0f)]
    [InlineData(99f, 5f, 4, 100f)]
    [InlineData(4f, -10f, 2, 0f)]
    public void TheChanceGrowsWithThePlayersAboardAndStaysInRange(float baseChance, float extra, int aboard, float expected)
    {
        Assert.Equal(expected, EncounterRules.AttackChance(baseChance, extra, aboard), 3);
    }

    [Theory]
    [InlineData(4f, 0f, true)]
    [InlineData(4f, 0.0399f, true)]
    [InlineData(4f, 0.05f, false)]
    [InlineData(4f, 0.99f, false)]
    [InlineData(0f, 0f, false)]
    [InlineData(100f, 0.999f, true)]
    public void TheDiceFallUnderTheChance(float chance, float roll, bool expected)
    {
        Assert.Equal(expected, EncounterRules.Rolled(chance, roll));
    }

    [Fact]
    public void TheCooldownRunsInRealMinutes()
    {
        Assert.False(EncounterRules.CooldownOver(100f + 44f * 60f, 100f, 45f));
        Assert.True(EncounterRules.CooldownOver(100f + 45f * 60f, 100f, 45f));
        Assert.True(EncounterRules.CooldownOver(10f, float.NegativeInfinity, 45f));
        Assert.True(EncounterRules.CooldownOver(10f, 9f, 0f));
    }

    [Theory]
    [InlineData(1, 1, 0)]
    [InlineData(3, 1, 2)]
    [InlineData(3, 2, 4)]
    [InlineData(0, 1, 0)]
    [InlineData(3, -1, 0)]
    public void ExtraPlayersBringExtraWarriors(int aboard, int perExtra, int expected)
    {
        Assert.Equal(expected, EncounterRules.CrewBonus(aboard, perExtra));
    }

    [Fact]
    public void OpenSeaHasNoLand()
    {
        Assert.True(float.IsPositiveInfinity(EncounterRules.NearestLand(0f, 0f, 500f, (_, _) => Sea, Water)));
        Assert.True(EncounterRules.ShoreIsClear(0f, 0f, 500f, (_, _) => Sea, Water));
    }

    [Fact]
    public void LandIsFoundAtTheFirstRingThatReachesIt()
    {
        // A coast to the east from x = 55: the 50 m ring misses it, the 75 m ring finds it.
        float Coast(float x, float z) => x >= 55f ? Land : Sea;

        Assert.Equal(75f, EncounterRules.NearestLand(0f, 0f, 100f, Coast, Water));
        Assert.False(EncounterRules.ShoreIsClear(0f, 0f, 100f, Coast, Water));
    }

    [Fact]
    public void LandBeyondTheLimitDoesNotCount()
    {
        float Coast(float x, float z) => x >= 55f ? Land : Sea;

        Assert.True(float.IsPositiveInfinity(EncounterRules.NearestLand(0f, 0f, 50f, Coast, Water)));
        Assert.True(EncounterRules.ShoreIsClear(0f, 0f, 50f, Coast, Water));
    }

    [Fact]
    public void ALimitUnderTheRingStepStillLooksAtTheLimit()
    {
        float Coast(float x, float z) => x >= 8f ? Land : Sea;

        Assert.Equal(10f, EncounterRules.NearestLand(0f, 0f, 10f, Coast, Water));
    }

    [Fact]
    public void LandInEveryDirectionIsFound()
    {
        // A coast on each side in turn; the sixteen directions cover north, east, south and west exactly.
        Assert.Equal(50f, EncounterRules.NearestLand(0f, 0f, 100f, (x, z) => z >= 40f ? Land : Sea, Water));
        Assert.Equal(50f, EncounterRules.NearestLand(0f, 0f, 100f, (x, z) => z <= -40f ? Land : Sea, Water));
        Assert.Equal(50f, EncounterRules.NearestLand(0f, 0f, 100f, (x, z) => x >= 40f ? Land : Sea, Water));
        Assert.Equal(50f, EncounterRules.NearestLand(0f, 0f, 100f, (x, z) => x <= -40f ? Land : Sea, Water));
    }

    [Fact]
    public void GroundAtTheWaterLevelIsLand()
    {
        Assert.Equal(25f, EncounterRules.NearestLand(0f, 0f, 100f, (_, _) => Water, Water));
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-5f)]
    public void ANoShoreLimitAcceptsAnyCoast(float limit)
    {
        Assert.True(EncounterRules.ShoreIsClear(0f, 0f, limit, (_, _) => 100f, Water));
    }

    [Fact]
    public void TheOffsetsSpreadEvenlyOverTheArcAndStartWhereAsked()
    {
        List<float> offsets = EncounterRules.Offsets(45f, 12, 0.5f).ToList();

        Assert.Equal(12, offsets.Count);
        Assert.Equal(0f, offsets[0], 3);
        Assert.All(offsets, offset => Assert.InRange(offset, -45f, 45f));
        List<float> sorted = offsets.OrderBy(offset => offset).ToList();
        for (int i = 1; i < sorted.Count; i++)
        {
            Assert.Equal(90f / 12f, sorted[i] - sorted[i - 1], 3);
        }
    }

    [Fact]
    public void NoCandidatesGiveNoOffsets()
    {
        Assert.Empty(EncounterRules.Offsets(45f, 0, 0.3f));
    }

    [Theory]
    [InlineData(0f, 0f, 180f)]
    [InlineData(90f, 180f, 0f)]
    [InlineData(180f, 0f, -180f)]
    [InlineData(270f, -180f, 0f)]
    public void TheSpawnPointLiesAheadOfTheHeading(float heading, float x, float z)
    {
        bool found = EncounterRules.TryPickSpawn(Vector2.zero, heading, 180f, new[] { 0f }, _ => true, out Vector2 point);

        Assert.True(found);
        Assert.Equal(x, point.x, 2);
        Assert.Equal(z, point.y, 2);
    }

    [Fact]
    public void TheFirstAcceptedCandidateWinsAndTheOffsetsTurnTheBearing()
    {
        List<Vector2> tried = new();

        bool found = EncounterRules.TryPickSpawn(new Vector2(100f, 200f), 0f, 100f, new[] { 0f, 90f, 180f }, point =>
        {
            tried.Add(point);
            return point.x > 150f;
        }, out Vector2 chosen);

        Assert.True(found);
        Assert.Equal(2, tried.Count);
        Assert.Equal(200f, chosen.x, 2);
        Assert.Equal(200f, chosen.y, 2);
    }

    [Fact]
    public void NoAcceptedCandidateFindsNothing()
    {
        bool found = EncounterRules.TryPickSpawn(new Vector2(5f, 6f), 30f, 100f, EncounterRules.Offsets(45f, 12, 0.2f), _ => false, out Vector2 point);

        Assert.False(found);
        Assert.Equal(new Vector2(5f, 6f), point);
    }

    [Fact]
    public void AllTheCandidatesLieAtTheSpawnDistance()
    {
        List<Vector2> tried = new();

        EncounterRules.TryPickSpawn(new Vector2(-40f, 70f), 123f, 180f, EncounterRules.Offsets(45f, 12, 0.7f), point =>
        {
            tried.Add(point);
            return false;
        }, out _);

        Assert.Equal(12, tried.Count);
        Assert.All(tried, point => Assert.Equal(180f, Vector2.Distance(point, new Vector2(-40f, 70f)), 2));
    }

    [Fact]
    public void AVelocityIsMeasuredFromTwoPositionsAndSmoothed()
    {
        Vector3 raw = EncounterRules.SmoothedVelocity(Vector3.zero, new Vector3(6f, 0f, 2f), 2f, Vector3.zero, 1f);
        Vector3 half = EncounterRules.SmoothedVelocity(Vector3.zero, new Vector3(6f, 0f, 2f), 2f, new Vector3(1f, 0f, 1f), 0.5f);

        Assert.Equal(3f, raw.x, 3);
        Assert.Equal(1f, raw.z, 3);
        Assert.Equal(2f, half.x, 3);
        Assert.Equal(1f, half.z, 3);
    }

    [Fact]
    public void NoTimeKeepsTheOldVelocity()
    {
        Vector3 old = new(4f, 0f, -1f);

        Assert.Equal(old, EncounterRules.SmoothedVelocity(Vector3.zero, new Vector3(100f, 0f, 0f), 0f, old, 1f));
    }
}

/// <summary>
/// Where a Nail Ship can be raised and live: the near zones the game keeps a machine's own objects in.
/// </summary>
public class NearZoneTests
{
    /// <summary>The classic square takes every zone within the distance, corners too.</summary>
    [Theory]
    [InlineData(0, 0, 1, true)]
    [InlineData(1, 1, 1, true)]
    [InlineData(2, 0, 1, false)]
    [InlineData(2, 2, 2, true)]
    public void TheClassicAreaIsSquare(int dx, int dy, int near, bool inside)
    {
        Assert.Equal(inside, EncounterRules.InNearZones(dx, dy, near, true));
    }

    /// <summary>The newer areas are round: a corner two zones out is beyond a distance of 2, as the game counts it.</summary>
    [Theory]
    [InlineData(2, 0, 2, true)]
    [InlineData(2, 1, 2, true)]
    [InlineData(2, 2, 2, false)]
    [InlineData(3, 0, 2, false)]
    [InlineData(0, 0, 0, true)]
    [InlineData(0, 0, -1, false)]
    public void TheNewerAreasAreRound(int dx, int dy, int near, bool inside)
    {
        Assert.Equal(inside, EncounterRules.InNearZones(dx, dy, near, false));
    }

    /// <summary>A spawn is tried at the set distance first, then a fifth shorter each time, never under the least.</summary>
    [Fact]
    public void ShorterDistancesStepDown()
    {
        List<float> distances = EncounterRules.ShorterDistances(180f, 40f).ToList();

        Assert.Equal(180f, distances[0], 3);
        Assert.Equal(144f, distances[1], 3);
        Assert.True(distances.Last() >= 40f);
        Assert.True(distances.Last() * 0.8f < 40f);
    }
}
