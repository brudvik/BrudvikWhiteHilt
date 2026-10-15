using BrudvikWhiteHilt.SeaBattles;
using BrudvikWhiteHilt.SeaBattles.Fleets;
using BrudvikWhiteHilt.SeaBattles.Weapons;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Xunit;

namespace BrudvikWhiteHilt.Tests;

public class JarlLevelTests
{
    [Theory]
    [InlineData(new string[0], 3)]
    [InlineData(new[] { "defeated_dragon" }, 3)]
    [InlineData(new[] { "defeated_dragon", "defeated_goblinking" }, 4)]
    [InlineData(new[] { "defeated_dragon", "defeated_goblinking", "defeated_queen" }, 5)]
    [InlineData(new[] { "defeated_dragon", "defeated_goblinking", "defeated_queen", "defeated_fader" }, 6)]
    public void TheJarlGrowsWithEachBoss(string[] keys, int level)
    {
        Assert.Equal(level, FleetRules.PickLevel(FleetSettings.DefaultJarlLevels, keys.Contains, 6, 1));
    }

    [Fact]
    public void LevelsAreHeldToTheCreatureMaximumAndBrokenPartsAreSkipped()
    {
        Assert.Equal(6, FleetRules.PickLevel("a=9; b=x; =4", key => key == "a", 6, 1));
        Assert.Equal(1, FleetRules.PickLevel("a=0", key => true, 6, 3));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("nothing; =3")]
    public void WithoutAValidEntryTheFallbackIsUsed(string text)
    {
        Assert.Equal(3, FleetRules.PickLevel(text, key => true, 6, 3));
    }
}

public class FleetTierTests
{
    [Fact]
    public void TheDefaultTextHasTwoTiers()
    {
        IReadOnlyList<FleetTier> tiers = FleetRules.ParseTiers(FleetSettings.DefaultFleetTiers);

        Assert.Equal(new[] { "defeated_dragon", "defeated_goblinking" }, tiers.Select(tier => tier.Key));
        Assert.Equal(new[] { 2, 3 }, tiers.Select(tier => tier.Size));
        Assert.Equal(new[] { 35f, 50f }, tiers.Select(tier => tier.Chance));
    }

    [Fact]
    public void SpacesAndAPercentSignAreIgnoredAndTheChanceMayBeLeftOut()
    {
        IReadOnlyList<FleetTier> tiers = FleetRules.ParseTiers("  a = 2 : 12.5 % ;\n b=3:40;c=2  ");

        Assert.Equal(3, tiers.Count);
        Assert.Equal(12.5f, tiers[0].Chance);
        Assert.Equal(40f, tiers[1].Chance);
        Assert.Equal(100f, tiers[2].Chance);
        Assert.Equal(2, tiers[2].Size);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData(";;;")]
    [InlineData("no equals sign")]
    [InlineData("=2:50")]
    [InlineData("key=")]
    [InlineData("key=two:50")]
    [InlineData("key=2:lots")]
    [InlineData("key=2:50:7")]
    [InlineData("key=2:NaN")]
    public void BrokenTextGivesNoTiersAndDoesNotThrow(string text)
    {
        Assert.Empty(FleetRules.ParseTiers(text));
    }

    [Fact]
    public void BrokenPartsAreSkippedAndTheRestIsKept()
    {
        IReadOnlyList<FleetTier> tiers = FleetRules.ParseTiers("broken; a=x:50; b=2:50; c=; d=3");

        Assert.Equal(new[] { "b", "d" }, tiers.Select(tier => tier.Key));
    }

    [Fact]
    public void SizeIsHeldToOneToThreeAndChanceToZeroToHundred()
    {
        IReadOnlyList<FleetTier> tiers = FleetRules.ParseTiers("a=0:-5; b=9:250; c=-4:50%");

        Assert.Equal(new[] { 1, 3, 1 }, tiers.Select(tier => tier.Size));
        Assert.Equal(new[] { 0f, 100f, 50f }, tiers.Select(tier => tier.Chance));
    }

    [Fact]
    public void TheLastTierWhoseKeyIsSetDecidesTheSize()
    {
        IReadOnlyList<FleetTier> tiers = FleetRules.ParseTiers("a=2:100; b=3:100");

        Assert.Equal(2, FleetRules.PickSize(tiers, key => key == "a", 0f));
        Assert.Equal(3, FleetRules.PickSize(tiers, key => key is "a" or "b", 0f));
        Assert.Equal(3, FleetRules.PickSize(tiers, key => key == "b", 0f));
    }

    [Fact]
    public void NoTierThatAppliesMeansOneShip()
    {
        IReadOnlyList<FleetTier> tiers = FleetRules.ParseTiers(FleetSettings.DefaultFleetTiers);

        Assert.Equal(1, FleetRules.PickSize(tiers, _ => false, 0f));
        Assert.Equal(1, FleetRules.PickSize(FleetRules.ParseTiers(string.Empty), _ => true, 0f));
        Assert.Equal(1, FleetRules.PickSize(null, _ => true, 0f));
        Assert.Equal(1, FleetRules.PickSize(tiers, null, 0f));
    }

    [Fact]
    public void TheRollMustBeUnderTheChance()
    {
        IReadOnlyList<FleetTier> tiers = FleetRules.ParseTiers("a=2:35");

        Assert.Equal(2, FleetRules.PickSize(tiers, _ => true, 0f));
        Assert.Equal(2, FleetRules.PickSize(tiers, _ => true, 0.349f));
        Assert.Equal(1, FleetRules.PickSize(tiers, _ => true, 0.36f));
        Assert.Equal(1, FleetRules.PickSize(tiers, _ => true, 0.99f));
    }

    [Fact]
    public void AFailedRollDoesNotFallBackToAnEarlierTier()
    {
        IReadOnlyList<FleetTier> tiers = FleetRules.ParseTiers("a=2:100; b=3:10");

        Assert.Equal(1, FleetRules.PickSize(tiers, _ => true, 0.5f));
    }

    [Fact]
    public void ZeroChanceNeverAndHundredPercentAlwaysMakesAFleet()
    {
        Assert.Equal(1, FleetRules.PickSize(FleetRules.ParseTiers("a=3:0"), _ => true, 0f));
        Assert.Equal(3, FleetRules.PickSize(FleetRules.ParseTiers("a=3:100"), _ => true, 0.9999f));
    }
}

public class FleetFormationTests
{
    private static void AssertPoint(float x, float z, Vector2 actual)
    {
        Assert.Equal(x, actual.x, 3);
        Assert.Equal(z, actual.y, 3);
    }

    [Fact]
    public void RankZeroIsTheFlagshipItself()
    {
        AssertPoint(5f, 7f, FleetRules.SlotPoint(new Vector2(5f, 7f), 123f, 0, 30f));
    }

    [Fact]
    public void TheEscortsLieBackAndToEachSideOfAFlagshipSailingNorth()
    {
        // 0.7 x 30 m = 21 m back and 21 m to the side; port is west (-x) when sailing north.
        AssertPoint(-21f, -21f, FleetRules.SlotPoint(Vector2.zero, 0f, 1, 30f));
        AssertPoint(21f, -21f, FleetRules.SlotPoint(Vector2.zero, 0f, 2, 30f));
    }

    [Fact]
    public void TheWedgeTurnsWithTheHeading()
    {
        // Sailing east, back is west and port is north (+z).
        AssertPoint(-21f, 21f, FleetRules.SlotPoint(Vector2.zero, 90f, 1, 30f));
        AssertPoint(-21f, -21f, FleetRules.SlotPoint(Vector2.zero, 90f, 2, 30f));
    }

    [Fact]
    public void TheWedgeIsMeasuredFromTheFlagship()
    {
        AssertPoint(100f - 21f, 200f - 21f, FleetRules.SlotPoint(new Vector2(100f, 200f), 0f, 1, 30f));
    }

    [Fact]
    public void RanksAboveTwoAreTakenAsTwo()
    {
        Assert.Equal(FleetRules.SlotPoint(Vector2.zero, 40f, 2, 30f), FleetRules.SlotPoint(Vector2.zero, 40f, 5, 30f));
    }

    [Fact]
    public void AnEscortFarBehindItsSlotSailsFullTowardIt()
    {
        RaiderOrder order = FleetRules.EscortSail(new Vector2(0f, -100f), Vector2.zero, 0f, 30f);

        Assert.Equal(RaiderState.Approach, order.State);
        Assert.Equal(RaiderSail.Full, order.Sail);
        Assert.Equal(0f, order.Course, 2);
    }

    [Fact]
    public void AnEscortNearButBehindItsSlotSailsHalf()
    {
        RaiderOrder order = FleetRules.EscortSail(new Vector2(0f, -25f), Vector2.zero, 0f, 30f);

        Assert.Equal(RaiderSail.Half, order.Sail);
        Assert.Equal(0f, order.Course, 2);
    }

    [Fact]
    public void AnEscortAheadOfItsSlotStops()
    {
        RaiderOrder order = FleetRules.EscortSail(new Vector2(3f, 50f), Vector2.zero, 0f, 30f);

        Assert.Equal(RaiderSail.Stop, order.Sail);
        // It keeps the flagship's heading so it is lined up when the wedge catches up.
        Assert.Equal(0f, order.Course, 2);
    }

    [Fact]
    public void WithinTenMetresOfItsSlotAnEscortMatchesTheFlagshipsHeading()
    {
        RaiderOrder order = FleetRules.EscortSail(new Vector2(6f, -6f), Vector2.zero, 370f, 30f);

        Assert.Equal(RaiderSail.Half, order.Sail);
        Assert.Equal(10f, order.Course, 2);
    }

    [Fact]
    public void AnEscortSteersForTheSlotNotForTheHeading()
    {
        // The slot is 60 m east and 1 m north of the escort, a little ahead of it along the heading: atan2(60, 1), just under 90.
        RaiderOrder order = FleetRules.EscortSail(new Vector2(-60f, 0f), new Vector2(0f, 1f), 0f, 30f);

        Assert.Equal(RaiderSail.Full, order.Sail);
        Assert.Equal(89f, order.Course, 0);
    }

    [Fact]
    public void AnEscortOnItsSlotGivesAFiniteOrder()
    {
        RaiderOrder order = FleetRules.EscortSail(Vector2.zero, Vector2.zero, float.NaN, 30f);

        Assert.False(float.IsNaN(order.Course));
        Assert.Equal(RaiderSail.Half, order.Sail);
    }

    [Fact]
    public void TheFlagshipWaitsWhileAnyEscortIsMoreThanOneSpacingOut()
    {
        Assert.False(FleetRules.FlagshipWaits(new[] { 5f, 30f }, 30f));
        Assert.True(FleetRules.FlagshipWaits(new[] { 5f, 30.1f }, 30f));
        Assert.True(FleetRules.FlagshipWaits(new[] { 90f }, 30f));
    }

    [Fact]
    public void TheFlagshipDoesNotWaitForNoEscorts()
    {
        Assert.False(FleetRules.FlagshipWaits(new float[0], 30f));
        Assert.False(FleetRules.FlagshipWaits(null, 30f));
    }

    [Theory]
    [InlineData(RaiderState.Approach, 200f, 80f, false)]
    [InlineData(RaiderState.Approach, 80.1f, 80f, false)]
    [InlineData(RaiderState.Approach, 80f, 80f, true)]
    [InlineData(RaiderState.Approach, 10f, 80f, true)]
    [InlineData(RaiderState.Broadside, 500f, 80f, true)]
    [InlineData(RaiderState.Board, 500f, 80f, true)]
    [InlineData(RaiderState.Flee, 500f, 80f, true)]
    [InlineData(RaiderState.Prize, 500f, 80f, true)]
    public void TheFormationBreaksWhenTheFlagshipIsCloseOrNoLongerApproaches(RaiderState state, float distance, float breakDistance, bool expected)
    {
        Assert.Equal(expected, FleetRules.BreaksFormation(state, distance, breakDistance));
    }
}

public class FleetFightTests
{
    [Theory]
    [InlineData(1, 1, -1)]
    [InlineData(1, -1, 1)]
    [InlineData(2, 1, 1)]
    [InlineData(2, -1, -1)]
    [InlineData(0, 1, 1)]
    [InlineData(0, -1, -1)]
    public void RankOneTakesTheOtherSideAndTheOthersTheFlagshipsSide(int rank, int flagshipSide, int expected)
    {
        Assert.Equal(expected, FleetRules.EscortSide(rank, flagshipSide));
    }

    [Fact]
    public void ASideOfZeroCountsAsStarboard()
    {
        Assert.Equal(-1, FleetRules.EscortSide(1, 0));
        Assert.Equal(1, FleetRules.EscortSide(2, 0));
    }

    [Theory]
    // Rank 1 lies on the other side and keeps the plain broadside distance.
    [InlineData(1, 1, 25f, 1.6f, 25f)]
    [InlineData(1, -1, 25f, 1.6f, 25f)]
    // Rank 2 shares the flagship's side and keeps the spread.
    [InlineData(2, 1, 25f, 1.6f, 40f)]
    [InlineData(2, -1, 25f, 1.6f, 40f)]
    // The flagship itself is not an escort.
    [InlineData(0, 1, 25f, 1.6f, 25f)]
    // A spread under one never brings an escort closer than the broadside distance.
    [InlineData(2, 1, 25f, 0.5f, 25f)]
    public void AnEscortOnTheFlagshipsSideKeepsTheSpreadDistance(int rank, int flagshipSide, float broadside, float spread, float expected)
    {
        Assert.Equal(expected, FleetRules.EscortDistance(rank, flagshipSide, broadside, spread), 3);
    }

    [Theory]
    [InlineData(false, RaiderState.Approach, false, true, true, false)]
    [InlineData(false, RaiderState.Broadside, false, true, true, false)]
    [InlineData(false, RaiderState.Flee, false, true, true, false)]
    // The flagship is gone, or a prize.
    [InlineData(true, RaiderState.Approach, false, true, true, true)]
    [InlineData(false, RaiderState.Prize, false, true, true, true)]
    [InlineData(true, RaiderState.Approach, false, false, true, false)]
    [InlineData(false, RaiderState.Prize, false, false, true, false)]
    // The jarl has fallen.
    [InlineData(false, RaiderState.Broadside, true, true, true, true)]
    [InlineData(false, RaiderState.Broadside, true, true, false, false)]
    [InlineData(false, RaiderState.Broadside, true, false, true, true)]
    // Both settings off: they never run.
    [InlineData(true, RaiderState.Prize, true, false, false, false)]
    public void MoraleBreaksByTheSettings(bool flagshipGone, RaiderState state, bool jarlFallen, bool fleeOnFlagship, bool fleeOnJarl, bool expected)
    {
        Assert.Equal(expected, FleetRules.MoraleBreaks(flagshipGone, state, jarlFallen, fleeOnFlagship, fleeOnJarl));
    }
}

public class FleetShipRuleTests
{
    [Fact]
    public void ASlotErrorIsTheDistanceFromTheSlot()
    {
        Vector2 flagship = new(100f, 200f);
        Vector2 slot = FleetRules.SlotPoint(flagship, 90f, 1, 30f);

        Assert.Equal(0f, FleetRules.SlotError(slot, flagship, 90f, 1, 30f), 3);
        Assert.Equal(15f, FleetRules.SlotError(slot + new Vector2(15f, 0f), flagship, 90f, 1, 30f), 3);
    }

    [Theory]
    [InlineData(RaiderState.Approach, RaiderSail.Full, true, RaiderSail.Half)]
    [InlineData(RaiderState.Approach, RaiderSail.Full, false, RaiderSail.Full)]
    // Half or no sail is never raised, and only an approach is held back.
    [InlineData(RaiderState.Approach, RaiderSail.Stop, true, RaiderSail.Stop)]
    [InlineData(RaiderState.Broadside, RaiderSail.Full, true, RaiderSail.Full)]
    [InlineData(RaiderState.Flee, RaiderSail.Full, true, RaiderSail.Full)]
    public void TheFlagshipSailsHalfOnlyWhileItApproachesAndEscortsAreBehind(RaiderState state, RaiderSail sail, bool waits, RaiderSail expected)
    {
        RaiderOrder order = FleetRules.FlagshipOrder(new RaiderOrder(state, 123f, sail), waits);

        Assert.Equal(state, order.State);
        Assert.Equal(123f, order.Course);
        Assert.Equal(expected, order.Sail);
    }

    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(1, 2, 2)]
    [InlineData(3, 2, 3)]
    [InlineData(1, 6, 6)]
    // Never past the game's highest level, and never below one.
    [InlineData(1, 9, 6)]
    [InlineData(1, 0, 1)]
    [InlineData(0, 0, 1)]
    public void ACrewMemberGetsAtLeastTheLeastLevel(int current, int least, int expected)
    {
        Assert.Equal(expected, FleetRules.CrewLevel(current, least));
    }

    [Fact]
    public void TheJarlTakesTheFreePostNearestTheMast()
    {
        Vector3[] posts = { new(0f, 0f, 8f), new(3f, 0f, 1f), new(-1f, 0f, -0.5f), new(0f, 0f, -7f) };

        // The mast is at the middle of the deck, and post 2 is the nearest of the free ones.
        Assert.Equal(2, FleetRules.NearestPost(posts, new[] { 0, 1, 2, 3 }, Vector3.zero));
        // Post 2 is taken by an archer: the next nearest.
        Assert.Equal(1, FleetRules.NearestPost(posts, new[] { 0, 1, 3 }, Vector3.zero));
        // Height does not count.
        Assert.Equal(0, FleetRules.NearestPost(new[] { new Vector3(1f, 50f, 0f), new Vector3(2f, 0f, 0f) }, new[] { 0, 1 }, Vector3.zero));
    }

    [Fact]
    public void NoPostIsFoundWhenThereAreNoCandidates()
    {
        Vector3[] posts = { new(0f, 0f, 1f) };

        Assert.Equal(-1, FleetRules.NearestPost(posts, new int[0], Vector3.zero));
        Assert.Equal(-1, FleetRules.NearestPost(posts, new[] { 5, -1 }, Vector3.zero));
        Assert.Equal(-1, FleetRules.NearestPost(null, new[] { 0 }, Vector3.zero));
        Assert.Equal(-1, FleetRules.NearestPost(posts, null, Vector3.zero));
    }

    [Theory]
    // The flagship has a jarl, this machine has seen him, and he is not there now.
    [InlineData(true, true, false, true)]
    [InlineData(true, true, true, false)]
    // A machine that never saw him cannot say he fell.
    [InlineData(true, false, false, false)]
    // A flagship without a jarl has none to lose.
    [InlineData(false, true, false, false)]
    [InlineData(false, false, false, false)]
    public void TheJarlHasFallenWhenASeenJarlIsGone(bool carries, bool seen, bool alive, bool expected)
    {
        Assert.Equal(expected, FleetRules.JarlHasFallen(carries, seen, alive));
    }

    [Theory]
    [InlineData(0f, false)]
    [InlineData(4.9f, false)]
    [InlineData(5f, true)]
    [InlineData(60f, true)]
    public void AFlagshipIsGoneOnlyAfterItsDataHasBeenMissingForAWhile(float missingSeconds, bool expected)
    {
        Assert.Equal(expected, FleetRules.FlagshipGone(missingSeconds));
    }

    [Theory]
    [InlineData(1, false, 2, 1)]
    [InlineData(1, true, 2, 3)]
    [InlineData(0, true, 0, 0)]
    // A negative setting or bonus never takes crew away.
    [InlineData(-3, true, -2, 0)]
    public void OnlyTheFlagshipGetsTheExtraCrew(int bonus, bool flagship, int extra, int expected)
    {
        Assert.Equal(expected, FleetRules.CrewBonus(bonus, flagship, extra));
    }

    [Theory]
    [InlineData(4, false, 4, 4)]
    [InlineData(4, true, 4, 8)]
    [InlineData(4, true, 0, 4)]
    [InlineData(0, true, 3, 3)]
    public void OnlyTheFlagshipsHoldGetsTheExtraDraws(int normal, bool flagship, int extra, int expected)
    {
        Assert.Equal(expected, FleetRules.HoldRolls(normal, flagship, extra));
    }

    [Theory]
    // The flagship's chart is sure when the setting says so.
    [InlineData(true, true, 0f, 0.99f, true)]
    // Otherwise it is the chance of any Nail Ship: a roll under the chance wins.
    [InlineData(true, false, 0.35f, 0.34f, true)]
    [InlineData(true, false, 0.35f, 0.35f, false)]
    [InlineData(false, true, 0.35f, 0.5f, false)]
    [InlineData(false, true, 0.35f, 0.1f, true)]
    public void TheFlagshipsHoldCarriesAChartForSureWhenTheSettingSaysSo(bool flagship, bool always, float chance, float roll, bool expected)
    {
        Assert.Equal(expected, FleetRules.ChartInHold(flagship, always, chance, roll));
    }

    [Fact]
    public void AnotherDistanceAlongsideChangesOnlyTheDistance()
    {
        RaiderTuning tuning = new(25f, 40f, 6f, 0.25f, 0.2f);

        RaiderTuning wider = tuning.WithBroadside(40f);

        Assert.Equal(40f, wider.BroadsideDistance);
        Assert.Equal(tuning.BroadsideSeconds, wider.BroadsideSeconds);
        Assert.Equal(tuning.BoardDistance, wider.BoardDistance);
        Assert.Equal(tuning.FleeCrewShare, wider.FleeCrewShare);
        Assert.Equal(tuning.FleeHullShare, wider.FleeHullShare);
        Assert.Equal(25f, tuning.BroadsideDistance);
    }

    [Theory]
    [InlineData(true, true, false, true)]
    [InlineData(true, false, true, true)]
    [InlineData(true, false, false, false)]
    [InlineData(false, true, true, false)]
    public void ABallistaIsCarriedWhenTheSettingIsOnAndTheShipIsAFlagshipOrTheKeyIsReached(bool enabled, bool flagship, bool key, bool expected)
    {
        Assert.Equal(expected, WeaponRules.BallistaOn(enabled, flagship, key));
    }
}

public class FleetPrefabTests
{
    [Fact]
    public void BothKindsOfShipAreNailShipsByNameAndByHash()
    {
        Assert.True(NailShipRegistry.IsNailShip(NailShipRegistry.ShipName));
        Assert.True(NailShipRegistry.IsNailShip(NailShipRegistry.FlagshipName));
        Assert.True(NailShipRegistry.IsNailShip(NailShipRegistry.ShipName.GetStableHashCode()));
        Assert.True(NailShipRegistry.IsNailShip(NailShipRegistry.FlagshipName.GetStableHashCode()));
        Assert.NotEqual(NailShipRegistry.ShipName.GetStableHashCode(), NailShipRegistry.FlagshipName.GetStableHashCode());
    }

    [Theory]
    [InlineData("VikingShip")]
    [InlineData("WhiteHilt_Ship")]
    [InlineData("WhiteHilt_NailShip(Clone)")]
    [InlineData("")]
    [InlineData(null)]
    public void OtherPrefabsAreNoNailShips(string name)
    {
        Assert.False(NailShipRegistry.IsNailShip(name));
    }

    [Fact]
    public void OtherHashesAreNoNailShips()
    {
        Assert.False(NailShipRegistry.IsNailShip("VikingShip".GetStableHashCode()));
        Assert.False(NailShipRegistry.IsNailShip(0));
    }

    [Fact]
    public void EveryRaiderKeyHasItsOwnHash()
    {
        List<int> hashes = typeof(RaiderKeys).GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
            .Where(field => field.FieldType == typeof(int))
            .Select(field => (int)field.GetValue(null))
            .ToList();
        List<string> names = typeof(RaiderKeys).GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
            .Where(field => field.IsLiteral)
            .Select(field => (string)field.GetRawConstantValue())
            .ToList();

        Assert.True(hashes.Count >= 15);
        Assert.Equal(hashes.Count, hashes.Distinct().Count());
        Assert.Equal(new[] { "whitehilt_raider_fleet", "whitehilt_raider_ship", "whitehilt_raider_target" }, names.OrderBy(name => name));
    }

    [Fact]
    public void TheFleetKeysHaveTheNamesOfThePlan()
    {
        Assert.Equal("whitehilt_raider_rank".GetStableHashCode(), RaiderKeys.Rank);
        Assert.Equal("whitehilt_raider_jarl".GetStableHashCode(), RaiderKeys.Jarl);
        Assert.Equal("whitehilt_raider_jarlfallen".GetStableHashCode(), RaiderKeys.JarlFallen);
        Assert.Equal("whitehilt_raider_broken".GetStableHashCode(), RaiderKeys.Broken);
        Assert.Equal("whitehilt_raider_fleet", RaiderKeys.Fleet);
    }
}

public class DraugrJarlTests
{
    private static readonly Monsters.WildMonster Jarl = Monsters.WildMonsters.DraugrJarl;

    [Fact]
    public void TheJarlIsOneOfTheWildMonstersWithTheNamesTheFleetUses()
    {
        Assert.Contains(Jarl, Monsters.WildMonsters.All);
        Assert.Equal("draugrjarl", Jarl.Id);
        Assert.Equal("WhiteHilt_DraugrJarl", Jarl.PrefabName);
        Assert.Equal("Draugr Jarl", Jarl.EnglishName);
        Assert.Equal("enemy_whitehilt_draugrjarl", Jarl.NameKey);
        Assert.Equal("Monsters.DraugrJarl", Jarl.Section);
    }

    [Fact]
    public void TheJarlNeverSpawnsInTheWild()
    {
        Assert.False(Jarl.Spawns);
        Assert.All(Monsters.WildMonsters.All.Where(monster => monster != Jarl), monster => Assert.True(monster.Spawns));
    }

    [Fact]
    public void TheJarlIsADraugrWithSoundsOfHisOwn()
    {
        // His faction (undead, like the crew) is checked in the game: the enum lives in a type the tests cannot load.
        Assert.Equal("Draugr_Elite", Jarl.TemplateCreature);
        Assert.Equal("jarl", Jarl.SoundPrefix);
    }

    [Fact]
    public void TheJarlHasTheTwoAttacksOfThePlanOnTheTriggersOfTheModel()
    {
        Assert.Equal(new[] { "jarl_swing", "jarl_sweep" }, Jarl.Attacks.Select(attack => attack.Trigger));
        Assert.Equal(new[] { "Chop", "Sweep" }, Jarl.Attacks.Select(attack => attack.Name));
        Assert.All(Jarl.Attacks, attack => Assert.Equal(HitData.DamageType.Slash, attack.Type));
        // The chop hits harder and pushes harder; the sweep reaches wider.
        Assert.True(Jarl.Attacks[0].Damage > Jarl.Attacks[1].Damage);
        Assert.True(Jarl.Attacks[0].Force > Jarl.Attacks[1].Force);
        Assert.True(Jarl.Attacks[1].Angle > Jarl.Attacks[0].Angle);
    }

    [Fact]
    public void TheJarlIsTheSizeOfTheModelAndAlwaysDropsHisTrophy()
    {
        Assert.Equal(0.0126f, Jarl.ModelScale);
        Assert.Equal(Monsters.WildMonsters.JarlTrophy, Jarl.Trophy);
        Assert.Equal("WhiteHilt_TrophyDraugrJarl", Monsters.WildMonsters.JarlTrophy);
        Assert.Equal(100f, Jarl.TrophyChance);
        Assert.Empty(Jarl.Drops);
        Assert.NotNull(Jarl.Configure);
    }

    [Fact]
    public void EveryWildMonsterHasItsOwnNamesAndSections()
    {
        Assert.Equal(Monsters.WildMonsters.All.Count, Monsters.WildMonsters.All.Select(monster => monster.Id).Distinct().Count());
        Assert.Equal(Monsters.WildMonsters.All.Count, Monsters.WildMonsters.All.Select(monster => monster.PrefabName).Distinct().Count());
        Assert.Equal(Monsters.WildMonsters.All.Count, Monsters.WildMonsters.All.Select(monster => monster.Section).Distinct().Count());
    }
}
