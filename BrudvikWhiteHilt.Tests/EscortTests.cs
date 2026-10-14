using BrudvikWhiteHilt.SeaBattles.Escorts;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Xunit;

namespace BrudvikWhiteHilt.Tests;

public class EscortDestinationTests
{
    private static readonly float[] straight = { 0f };

    [Fact]
    public void AClearSeaGivesTheFarthestDestinationAhead()
    {
        bool found = EscortRules.TryPickDestination(Vector2.zero, 0f, 1200f, 2000f, straight, _ => true, 50f, out Vector2 destination);

        Assert.True(found);
        Assert.Equal(0f, destination.x, 3);
        Assert.Equal(2000f, destination.y, 3);
    }

    [Fact]
    public void TheHeadingTurnsTheDestination()
    {
        EscortRules.TryPickDestination(new Vector2(100f, 100f), 90f, 1200f, 2000f, straight, _ => true, 50f, out Vector2 destination);

        Assert.Equal(2100f, destination.x, 2);
        Assert.Equal(100f, destination.y, 2);
    }

    [Fact]
    public void ABearingOffTheHeadingTurnsItToo()
    {
        EscortRules.TryPickDestination(Vector2.zero, 0f, 1200f, 2000f, new[] { 90f }, _ => true, 50f, out Vector2 destination);

        Assert.Equal(2000f, destination.x, 2);
        Assert.Equal(0f, destination.y, 2);
    }

    [Fact]
    public void ALineWithShallowWaterIsNotUsedAndTheNextBearingIs()
    {
        // A shoal lies on the line straight ahead, 500 m out.
        bool Deep(Vector2 point) => !(Mathf.Abs(point.x) < 20f && Mathf.Abs(point.y - 500f) < 20f);

        bool found = EscortRules.TryPickDestination(Vector2.zero, 0f, 1200f, 2000f, new[] { 0f, 30f }, Deep, 50f, out Vector2 destination);

        Assert.True(found);
        float bearing = Mathf.Atan2(destination.x, destination.y) * Mathf.Rad2Deg;
        Assert.Equal(30f, bearing, 2);
    }

    [Fact]
    public void ShortestDistanceIsTriedWhenTheFarOnesFail()
    {
        // Only the first 1300 m are deep, so 2000, 1800, 1600 and 1400 fail; 1200 is the last step and works.
        bool found = EscortRules.TryPickDestination(Vector2.zero, 0f, 1200f, 2000f, straight, point => point.y <= 1300f, 50f, out Vector2 destination);

        Assert.True(found);
        Assert.Equal(1200f, destination.y, 3);
    }

    [Fact]
    public void TheMinimumIsTriedEvenWhenTheStepsDoNotHitIt()
    {
        // 1200..1700 in 200 steps from 1700 gives 1700, 1500, 1300 and then the minimum 1200.
        bool found = EscortRules.TryPickDestination(Vector2.zero, 0f, 1200f, 1700f, straight, point => point.y <= 1250f, 50f, out Vector2 destination);

        Assert.True(found);
        Assert.Equal(1200f, destination.y, 3);
    }

    [Fact]
    public void NoDeepLineGivesNoDestination()
    {
        bool found = EscortRules.TryPickDestination(new Vector2(5f, 6f), 0f, 1200f, 2000f, new[] { 0f, 20f, -20f }, _ => false, 50f, out Vector2 destination);

        Assert.False(found);
        Assert.Equal(new Vector2(5f, 6f), destination);
    }

    [Fact]
    public void AShoalAtTheVeryEndRejectsTheDistance()
    {
        // The destination itself is shallow even though every point before it is deep.
        bool found = EscortRules.TryPickDestination(Vector2.zero, 0f, 1000f, 1000f, straight, point => point.y < 999f, 50f, out _);

        Assert.False(found);
    }

    [Fact]
    public void AMaximumBelowTheMinimumCountsAsTheMinimum()
    {
        bool found = EscortRules.TryPickDestination(Vector2.zero, 0f, 1200f, 500f, straight, _ => true, 50f, out Vector2 destination);

        Assert.True(found);
        Assert.Equal(1200f, destination.y, 3);
    }

    [Fact]
    public void NoBearingsOrNoTestGivesNothing()
    {
        Assert.False(EscortRules.TryPickDestination(Vector2.zero, 0f, 1200f, 2000f, Array.Empty<float>(), _ => true, 50f, out _));
        Assert.False(EscortRules.TryPickDestination(Vector2.zero, 0f, 1200f, 2000f, null, _ => true, 50f, out _));
        Assert.False(EscortRules.TryPickDestination(Vector2.zero, 0f, 1200f, 2000f, straight, null, 50f, out _));
    }

    [Fact]
    public void EveryPointIsCheckedAtTheStepAndAtTheEnd()
    {
        List<float> seen = new();
        EscortRules.TryPickDestination(Vector2.zero, 0f, 120f, 120f, straight, point => { seen.Add(point.y); return true; }, 50f, out _);

        Assert.Equal(new[] { 50f, 100f, 120f }, seen.Select(value => (float)Math.Round(value, 2)));
    }
}

public class EscortWaveTests
{
    private static Func<float> Sequence(params float[] values)
    {
        int index = 0;
        return () => values[index++ % values.Length];
    }

    [Fact]
    public void ZeroWavesGiveNone()
    {
        Assert.Empty(EscortRules.RollWaves(0, 0.25f, 0.8f, () => 0.5f));
        Assert.Empty(EscortRules.RollWaves(-3, 0.25f, 0.8f, () => 0.5f));
    }

    [Fact]
    public void OneWaveLiesAnywhereBetweenTheStartAndTheEnd()
    {
        Assert.Equal(0.25f, EscortRules.RollWaves(1, 0.25f, 0.8f, () => 0f)[0], 4);
        Assert.Equal(0.8f, EscortRules.RollWaves(1, 0.25f, 0.8f, () => 1f)[0], 4);
    }

    [Theory]
    [InlineData(2, 0f)]
    [InlineData(2, 0.5f)]
    [InlineData(2, 1f)]
    [InlineData(4, 0f)]
    [InlineData(4, 1f)]
    public void WavesAreSortedInsideTheStretchAndHalfASlotApart(int count, float roll)
    {
        float[] waves = EscortRules.RollWaves(count, 0.25f, 0.8f, () => roll);

        Assert.Equal(count, waves.Length);
        Assert.Equal(waves.OrderBy(value => value), waves);
        Assert.All(waves, value => Assert.InRange(value, 0.25f - 1e-5f, 0.8f + 1e-5f));
        float least = (0.8f - 0.25f) / (count * 2);
        for (int i = 1; i < waves.Length; i++)
        {
            Assert.True(waves[i] - waves[i - 1] >= least - 1e-5f, $"{waves[i]} - {waves[i - 1]} < {least}");
        }
    }

    [Fact]
    public void WorstCaseRollsStillKeepTheGap()
    {
        // First wave as late as it can be in its slot, the second as early as it can.
        float[] waves = EscortRules.RollWaves(2, 0f, 1f, Sequence(1f, 0f));

        // Two slots of 0.5: half a slot, 0.25, is the least the rule promises.
        Assert.Equal(0.25f, waves[1] - waves[0], 4);
    }

    [Fact]
    public void RandomIsHeldToTheUnitRange()
    {
        float[] waves = EscortRules.RollWaves(2, 0.25f, 0.8f, () => 7f);

        Assert.All(waves, value => Assert.InRange(value, 0.25f, 0.8f + 1e-5f));
    }

    [Fact]
    public void AnEndBelowTheStartPutsEveryWaveAtTheStart()
    {
        float[] waves = EscortRules.RollWaves(3, 0.6f, 0.2f, () => 0.5f);

        Assert.All(waves, value => Assert.Equal(0.6f, value, 4));
    }

    [Fact]
    public void ADueWaveIsOneNotYetSentWhoseShareIsReached()
    {
        float[] at = { 0.3f, 0.6f };

        Assert.False(EscortRules.WaveDue(0.29f, at, 0));
        Assert.True(EscortRules.WaveDue(0.3f, at, 0));
        Assert.False(EscortRules.WaveDue(0.3f, at, 1));
        Assert.True(EscortRules.WaveDue(0.9f, at, 1));
        Assert.False(EscortRules.WaveDue(1f, at, 2));
    }

    [Fact]
    public void NoWavesAreNeverDue()
    {
        Assert.False(EscortRules.WaveDue(1f, Array.Empty<float>(), 0));
        Assert.False(EscortRules.WaveDue(1f, null, 0));
        Assert.False(EscortRules.WaveDue(1f, new[] { 0.1f }, -1));
    }
}

public class EscortProgressTests
{
    [Fact]
    public void ProgressIsTheShareOfTheLineDone()
    {
        Vector2 start = new(0f, 0f);
        Vector2 destination = new(0f, 1000f);

        Assert.Equal(0f, EscortRules.Progress(start, destination, start), 4);
        Assert.Equal(0.5f, EscortRules.Progress(start, destination, new Vector2(0f, 500f)), 4);
        Assert.Equal(1f, EscortRules.Progress(start, destination, destination), 4);
    }

    [Fact]
    public void SideWaysDriftDoesNotCount()
    {
        Assert.Equal(0.25f, EscortRules.Progress(Vector2.zero, new Vector2(0f, 1000f), new Vector2(300f, 250f)), 4);
    }

    [Fact]
    public void BehindTheStartIsZeroAndBeyondTheDestinationIsOne()
    {
        Assert.Equal(0f, EscortRules.Progress(Vector2.zero, new Vector2(0f, 1000f), new Vector2(0f, -200f)));
        Assert.Equal(1f, EscortRules.Progress(Vector2.zero, new Vector2(0f, 1000f), new Vector2(0f, 1400f)));
    }

    [Fact]
    public void ARouteOfNoLengthIsDone()
    {
        Assert.Equal(1f, EscortRules.Progress(new Vector2(4f, 4f), new Vector2(4f, 4f), new Vector2(9f, 9f)));
    }
}

public class EscortPlunderTests
{
    [Fact]
    public void NoRaidersLeaveTheCargoAlone()
    {
        Assert.Equal(1f, EscortRules.Plunder(1f, 0, 0.5f, 10f));
        Assert.Equal(0.6f, EscortRules.Plunder(0.6f, -2, 0.5f, 10f));
    }

    [Fact]
    public void EachRaiderTakesItsPercentPerSecond()
    {
        // Three raiders at 0.5 percent a second for 10 seconds: 15 percent.
        Assert.Equal(0.85f, EscortRules.Plunder(1f, 3, 0.5f, 10f), 4);
    }

    [Fact]
    public void TheCargoNeverGoesBelowZero()
    {
        Assert.Equal(0f, EscortRules.Plunder(0.05f, 10, 5f, 10f));
    }

    [Fact]
    public void CargoThatIsAlreadyZeroStaysZero()
    {
        Assert.Equal(0f, EscortRules.Plunder(0f, 4, 0.5f, 1f));
    }

    [Fact]
    public void NoTimeOrNoRateTakesNothing()
    {
        Assert.Equal(0.7f, EscortRules.Plunder(0.7f, 4, 0.5f, 0f));
        Assert.Equal(0.7f, EscortRules.Plunder(0.7f, 4, 0f, 5f));
    }

    [Fact]
    public void TheCargoIsHeldToTheUnitRange()
    {
        Assert.Equal(1f, EscortRules.Plunder(3f, 0, 0.5f, 1f));
    }

    [Fact]
    public void ManyShortStepsEmptyTheHoldLikeOneLongOne()
    {
        float cargo = 1f;
        for (int i = 0; i < 40; i++)
        {
            cargo = EscortRules.Plunder(cargo, 2, 0.5f, 0.25f);
        }

        Assert.Equal(EscortRules.Plunder(1f, 2, 0.5f, 10f), cargo, 3);
    }
}

public class EscortOutcomeTests
{
    private static EscortOutcome Of(float distance = 500f, bool hull = false, float cargo = 1f, float alone = 0f)
    {
        return EscortRules.Outcome(distance, 60f, hull, cargo, alone, 120f);
    }

    [Fact]
    public void ANormalTickKeepsSailing()
    {
        Assert.Equal(EscortOutcome.Sailing, Of());
    }

    [Fact]
    public void WithinTheRadiusItHasArrived()
    {
        Assert.Equal(EscortOutcome.Arrived, Of(distance: 60f));
        Assert.Equal(EscortOutcome.Arrived, Of(distance: 10f));
        Assert.Equal(EscortOutcome.Sailing, Of(distance: 60.1f));
    }

    [Fact]
    public void ASunkHullEndsIt()
    {
        Assert.Equal(EscortOutcome.Sunk, Of(hull: true));
    }

    [Fact]
    public void ArrivingAndSinkingInTheSameTickIsSinking()
    {
        Assert.Equal(EscortOutcome.Sunk, Of(distance: 5f, hull: true));
    }

    [Fact]
    public void CargoExactlyZeroIsPlundered()
    {
        Assert.Equal(EscortOutcome.Plundered, Of(cargo: 0f));
        Assert.Equal(EscortOutcome.Sailing, Of(cargo: 0.001f));
    }

    [Fact]
    public void AnEmptyHoldAtTheDestinationIsStillPlundered()
    {
        Assert.Equal(EscortOutcome.Plundered, Of(distance: 5f, cargo: 0f));
    }

    [Fact]
    public void SinkingBeatsAnEmptyHold()
    {
        Assert.Equal(EscortOutcome.Sunk, Of(hull: true, cargo: 0f));
    }

    [Fact]
    public void WaitingLongerThanTheLimitAloneIsAbandoning()
    {
        Assert.Equal(EscortOutcome.Sailing, Of(alone: 120f));
        Assert.Equal(EscortOutcome.Abandoned, Of(alone: 120.5f));
    }

    [Fact]
    public void ArrivingBeatsTheMerchantsPatience()
    {
        Assert.Equal(EscortOutcome.Arrived, Of(distance: 5f, alone: 900f));
    }

    [Fact]
    public void PlunderBeatsAbandoning()
    {
        Assert.Equal(EscortOutcome.Plundered, Of(cargo: 0f, alone: 900f));
    }
}

public class EscortPayTests
{
    [Theory]
    [InlineData(600, 1f, 600)]
    [InlineData(600, 0.5f, 300)]
    [InlineData(600, 0.7f, 420)]
    [InlineData(600, 0.333f, 199)]
    [InlineData(600, 0f, 0)]
    [InlineData(0, 1f, 0)]
    public void PayFollowsTheCargoAndRoundsDown(int full, float cargo, int coins)
    {
        Assert.Equal(coins, EscortRules.Pay(full, cargo));
    }

    [Fact]
    public void CargoOutsideTheUnitRangeIsHeldToIt()
    {
        Assert.Equal(600, EscortRules.Pay(600, 4f));
        Assert.Equal(0, EscortRules.Pay(600, -1f));
        Assert.Equal(0, EscortRules.Pay(-50, 1f));
    }

    [Theory]
    [InlineData(400f, 1000f, 400)]
    [InlineData(400f, 2500f, 1000)]
    [InlineData(400f, 333f, 133)]
    [InlineData(0f, 3000f, 0)]
    [InlineData(400f, -10f, 0)]
    public void AVoyageBetweenPostsPaysByItsLength(float perKilometre, float distance, int coins)
    {
        Assert.Equal(coins, EscortRules.PayForVoyage(perKilometre, distance));
    }
}

public class EscortNearTests
{
    [Fact]
    public void APlaceWithinRangeMatches()
    {
        Vector2[] ships = { new(1000f, 0f), new(0f, 300f) };

        Assert.True(EscortRules.AnyWithin(Vector2.zero, ships, 400f));
        Assert.False(EscortRules.AnyWithin(Vector2.zero, ships, 299f));
    }

    [Fact]
    public void NothingOrARangeOfZeroNeverMatches()
    {
        Assert.False(EscortRules.AnyWithin(Vector2.zero, Array.Empty<Vector2>(), 400f));
        Assert.False(EscortRules.AnyWithin(Vector2.zero, null, 400f));
        Assert.False(EscortRules.AnyWithin(Vector2.zero, new[] { Vector2.zero }, 0f));
    }
}

public class TradeShipDataTests
{
    [Fact]
    public void EveryTradeKeyHasItsOwnHash()
    {
        List<int> hashes = typeof(TradeKeys).GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
            .Where(field => field.FieldType == typeof(int) && !field.IsLiteral)
            .Select(field => (int)field.GetValue(null))
            .ToList();
        hashes.AddRange(Enumerable.Range(0, TradeKeys.MaxWaves).Select(TradeKeys.WaveAt));

        Assert.Equal(11 + TradeKeys.MaxWaves, hashes.Count);
        Assert.Equal(hashes.Count, hashes.Distinct().Count());
    }

    [Fact]
    public void TheKeysHaveTheNamesOfThePlan()
    {
        Assert.Equal("whitehilt_trade_destination".GetStableHashCode(), TradeKeys.Destination);
        Assert.Equal("whitehilt_trade_start".GetStableHashCode(), TradeKeys.Start);
        Assert.Equal("whitehilt_trade_contractor".GetStableHashCode(), TradeKeys.Contractor);
        Assert.Equal("whitehilt_trade_state".GetStableHashCode(), TradeKeys.State);
        Assert.Equal("whitehilt_trade_cargo".GetStableHashCode(), TradeKeys.Cargo);
        Assert.Equal("whitehilt_trade_wavessent".GetStableHashCode(), TradeKeys.WavesSent);
        Assert.Equal("whitehilt_trade_seen".GetStableHashCode(), TradeKeys.Seen);
        Assert.Equal("whitehilt_trade_endedat".GetStableHashCode(), TradeKeys.EndedAt);
        Assert.Equal("whitehilt_trade_pay".GetStableHashCode(), TradeKeys.Pay);
        Assert.Equal("whitehilt_trade_raisedat".GetStableHashCode(), TradeKeys.RaisedAt);
        Assert.Equal("whitehilt_trade_waveat2".GetStableHashCode(), TradeKeys.WaveAt(2));
    }

    [Fact]
    public void TheStateNumbersAreSavedInTheShipsDataSoTheyNeverChange()
    {
        Assert.Equal(0, (int)TradeState.Sailing);
        Assert.Equal(1, (int)TradeState.Arrived);
        Assert.Equal(2, (int)TradeState.Lost);
    }

    [Fact]
    public void TheNoticeNumbersGoOverTheNetworkSoTheyNeverChange()
    {
        Assert.Equal(new[] { 0, 1, 2, 3, 4, 5 }, new[]
        {
            (int)EscortNotice.Wave, (int)EscortNotice.Plunder, (int)EscortNotice.Arrived,
            (int)EscortNotice.Sunk, (int)EscortNotice.Plundered, (int)EscortNotice.Abandoned
        });
    }

    [Fact]
    public void OnlyTheTradingShipIsATradingShip()
    {
        Assert.True(TradeShipRegistry.IsTradeShip(TradeShipRegistry.ShipName.GetStableHashCode()));
        Assert.False(TradeShipRegistry.IsTradeShip(SeaBattles.NailShipRegistry.ShipName.GetStableHashCode()));
        Assert.False(TradeShipRegistry.IsTradeShip("VikingShip".GetStableHashCode()));
        Assert.False(SeaBattles.NailShipRegistry.IsNailShip(TradeShipRegistry.ShipName));
    }

    [Fact]
    public void TheContractsTextsAreRegisteredUnderTheKeyItsPrefabNameGives()
    {
        // EscortSettings registers "item_whitehilt_escortcontract" and the item asks for the key made from its prefab name.
        Assert.Equal("item_whitehilt_escortcontract", Helpers.Translations.ItemKey(EscortContract.PrefabName));
    }

    [Fact]
    public void TheSavedWaveLimitCoversTheSetting()
    {
        Assert.Equal(4, TradeKeys.MaxWaves);
    }
}
