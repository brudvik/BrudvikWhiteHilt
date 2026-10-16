using BrudvikWhiteHilt.Storms;
using Xunit;

namespace BrudvikWhiteHilt.Tests;

/// <summary>
/// The great storms: the exposure meter, shelter, the biomes' storms and how often they come (docs/storms.md).
/// </summary>
public class StormTests
{
    private static readonly StormProfile forest = StormProfile.For(Heightmap.Biome.BlackForest);

    /// <summary>Out in the open at the default pace, the meter is full after four minutes.</summary>
    [Fact]
    public void TheOpenFillsTheMeterInTheSetTime()
    {
        float exposure = 0f;
        for (int i = 0; i < 480; i++)
        {
            exposure = StormExposure.Step(exposure, 0.5f, true, forest, StormCover.Open, 4f, 1.5f);
        }

        Assert.Equal(StormExposure.Max, exposure, 3);
        Assert.Equal(50f, StormExposure.Step(0f, 120f, true, forest, StormCover.Open, 4f, 1.5f), 3);
    }

    /// <summary>The lee halves the pace, the forest takes a quarter off, a roof holds and a fire or a cave brings it down.</summary>
    [Theory]
    [InlineData(StormCover.Open, 75f)]
    [InlineData(StormCover.Lee, 62.5f)]
    [InlineData(StormCover.Forest, 68.75f)]
    [InlineData(StormCover.Roof, 50f)]
    [InlineData(StormCover.RoofAndFire, 16.667f)]
    [InlineData(StormCover.Interior, 16.667f)]
    public void ShelterSlowsHoldsOrBringsItDown(StormCover cover, float after)
    {
        Assert.Equal(after, StormExposure.Step(50f, 60f, true, forest, cover, 4f, 3f), 2);
    }

    /// <summary>When the storm is over, or where there is none, the meter falls even out in the open.</summary>
    [Fact]
    public void NoStormMeansRecovery()
    {
        Assert.Equal(0f, StormExposure.Step(50f, 60f, false, forest, StormCover.Open, 4f, 1f), 3);
        Assert.Equal(0f, StormExposure.Step(50f, 60f, true, StormProfile.For(Heightmap.Biome.Meadows), StormCover.Open, 4f, 1f), 3);
    }

    /// <summary>The Deep North is twice as fast, the Mistlands and the Ashlands half as fast again.</summary>
    // The biomes are given as numbers: the game's Heightmap does not load on .NET Framework, so neither does its enum type.
    [Theory]
    [InlineData((int)Heightmap.Biome.DeepNorth, 2f)]
    [InlineData((int)Heightmap.Biome.Mistlands, 1.5f)]
    [InlineData((int)Heightmap.Biome.AshLands, 1.5f)]
    [InlineData((int)Heightmap.Biome.Mountain, 1.25f)]
    [InlineData((int)Heightmap.Biome.Ocean, 1.25f)]
    [InlineData((int)Heightmap.Biome.Plains, 1f)]
    public void HarsherBiomesAreFaster(int biome, float rate)
    {
        Assert.Equal(rate * 25f, StormExposure.Step(0f, 60f, true, StormProfile.For((Heightmap.Biome)biome), StormCover.Open, 4f, 1f), 3);
    }

    /// <summary>The Meadows never have a storm; every other biome has its own.</summary>
    // As numbers, as above.
    [Theory]
    [InlineData((int)Heightmap.Biome.Meadows, StormKind.None)]
    [InlineData((int)Heightmap.Biome.BlackForest, StormKind.Tempest)]
    [InlineData((int)Heightmap.Biome.Swamp, StormKind.Tempest)]
    [InlineData((int)Heightmap.Biome.Ocean, StormKind.Sea)]
    [InlineData((int)Heightmap.Biome.Plains, StormKind.Sand)]
    [InlineData((int)Heightmap.Biome.Mountain, StormKind.Blizzard)]
    [InlineData((int)Heightmap.Biome.Mistlands, StormKind.Mist)]
    [InlineData((int)Heightmap.Biome.AshLands, StormKind.Fire)]
    [InlineData((int)Heightmap.Biome.DeepNorth, StormKind.Arctic)]
    public void EachBiomeHasItsStorm(int biome, StormKind kind)
    {
        Assert.Equal(kind, StormProfile.For((Heightmap.Biome)biome).Kind);
    }

    /// <summary>Stamina comes back at half the pace from 20, slowly from 40, hardly at all from 60.</summary>
    [Theory]
    [InlineData(0f, 0, 1f)]
    [InlineData(19.9f, 0, 1f)]
    [InlineData(20f, 1, 0.5f)]
    [InlineData(40f, 2, 0.25f)]
    [InlineData(60f, 3, 0.1f)]
    [InlineData(80f, 4, 0.1f)]
    [InlineData(100f, 4, 0.1f)]
    public void StagesCostStamina(float exposure, int stage, float regen)
    {
        Assert.Equal(stage, StormExposure.Stage(exposure));
        Assert.Equal(regen, StormExposure.StaminaRegen(exposure), 3);
    }

    /// <summary>The potions give half from 20, a quarter from 40 and nothing from 60, when you freeze.</summary>
    [Theory]
    [InlineData(0f, 1f)]
    [InlineData(20f, 0.5f)]
    [InlineData(40f, 0.25f)]
    [InlineData(60f, 0f)]
    [InlineData(100f, 0f)]
    public void PotionsFadeInTheStorm(float exposure, float share)
    {
        Assert.Equal(share, StormExposure.PotionShare(exposure), 3);
    }

    /// <summary>Health goes from 80, sooner in the fire storm, and never in the sandstorm.</summary>
    [Fact]
    public void HealthGoesLast()
    {
        Assert.Equal(0f, StormExposure.DamageShare(79f, forest));
        Assert.Equal(0.005f, StormExposure.DamageShare(80f, forest), 4);
        Assert.Equal(0.04f, StormExposure.DamageShare(100f, forest), 4);
        Assert.True(StormExposure.DamageShare(55f, StormProfile.For(Heightmap.Biome.AshLands)) > 0f);
        Assert.Equal(0f, StormExposure.DamageShare(100f, StormProfile.For(Heightmap.Biome.Plains)));
    }

    /// <summary>The sandstorm blinds without freezing; the fire storm burns from where it starts to hurt.</summary>
    [Fact]
    public void SandBlindsAndFireBurns()
    {
        StormProfile sand = StormProfile.For(Heightmap.Biome.Plains);
        StormProfile fire = StormProfile.For(Heightmap.Biome.AshLands);
        Assert.False(StormExposure.Suffering(100f, sand));
        Assert.True(StormExposure.Vision(0f, sand, true) > 0.3f);
        Assert.Equal(0f, StormExposure.Vision(0f, sand, false));
        Assert.True(StormExposure.Suffering(50f, fire));
        Assert.False(StormExposure.Suffering(50f, forest));
        Assert.True(StormExposure.Suffering(60f, forest));
        Assert.Equal(1f, StormExposure.Vision(100f, forest, false), 3);
    }

    /// <summary>A cave beats everything; a roof needs a fire to help, and in the Ashlands it must be stone.</summary>
    [Fact]
    public void CoverIsWorkedOut()
    {
        StormProfile fire = StormProfile.For(Heightmap.Biome.AshLands);
        Assert.Equal(StormCover.Interior, StormExposure.Resolve(forest, true, false, false, false, false, false));
        Assert.Equal(StormCover.RoofAndFire, StormExposure.Resolve(forest, false, true, true, false, false, false));
        Assert.Equal(StormCover.Roof, StormExposure.Resolve(forest, false, true, false, false, false, false));
        Assert.Equal(StormCover.Open, StormExposure.Resolve(forest, false, false, true, false, false, false));
        Assert.Equal(StormCover.Lee, StormExposure.Resolve(forest, false, false, false, false, true, true));
        Assert.Equal(StormCover.Forest, StormExposure.Resolve(forest, false, false, false, false, false, true));
        Assert.Equal(StormCover.Open, StormExposure.Resolve(fire, false, true, true, false, false, false));
        Assert.Equal(StormCover.RoofAndFire, StormExposure.Resolve(fire, false, true, true, true, false, false));
        Assert.Equal(StormCover.Open, StormExposure.Resolve(fire, false, false, false, false, false, true));
    }

    /// <summary>The chance per check gives the set number of storms a day, and none when set to 0.</summary>
    [Fact]
    public void ChanceMatchesStormsPerDay()
    {
        float chance = StormState.ChancePerCheck(0.4f, 60f, 1800f);
        Assert.InRange(chance, 0.0130f, 0.0133f);
        Assert.Equal(0f, StormState.ChancePerCheck(0f, 60f, 1800f));
        Assert.InRange(StormState.ChancePerCheck(100f, 60f, 1800f), 0.96f, 1f);
    }

    /// <summary>The lean-to's roof runs from 2 m at the open front to the ground behind.</summary>
    [Fact]
    public void TheLeanToSlopesToTheGround()
    {
        Assert.Equal(2f, StormLeanTo.RoofHeight(0.9f), 3);
        Assert.Equal(0.12f, StormLeanTo.RoofHeight(-1.35f), 3);
        Assert.True(StormLeanTo.RoofHeight(0f) > StormLeanTo.RoofHeight(-1f));
    }

    /// <summary>Under the front half of the lean-to there is shelter; outside it, behind it or on its roof there is none.</summary>
    [Theory]
    [InlineData(0f, 0f, 0.5f, true)]
    [InlineData(1.1f, 0f, 0f, true)]
    [InlineData(0f, 0f, -1.3f, false)]
    [InlineData(1.5f, 0f, 0f, false)]
    [InlineData(0f, 0f, 1.5f, false)]
    [InlineData(0f, 2.2f, 0.5f, false)]
    public void TheLeanToCoversItsFloor(float x, float y, float z, bool covered)
    {
        Assert.Equal(covered, StormLeanTo.Covers(new UnityEngine.Vector3(x, y, z)));
    }

    /// <summary>Weathering takes 30% off the pace at level 100 and nothing at 0, never more than set.</summary>
    [Theory]
    [InlineData(0f, 0.3f, 1f)]
    [InlineData(0.5f, 0.3f, 0.85f)]
    [InlineData(1f, 0.3f, 0.7f)]
    [InlineData(2f, 0.3f, 0.7f)]
    [InlineData(1f, 0f, 1f)]
    public void WeatheringSlowsTheMeter(float skill, float reduction, float pace)
    {
        Assert.Equal(pace, StormSkill.Pace(skill, reduction), 3);
    }

    /// <summary>A brewing storm breaks after the forecast and the warning; a warned one after the warning alone.</summary>
    [Fact]
    public void TheForecastCountsToTheBreak()
    {
        Assert.Equal(480f, StormState.SecondsToBreak(StormPhase.Brewing, 300f, 180f), 3);
        Assert.Equal(120f, StormState.SecondsToBreak(StormPhase.Warning, 120f, 180f), 3);
        Assert.Equal(0f, StormState.SecondsToBreak(StormPhase.Raging, 200f, 180f), 3);
        Assert.Equal(0f, StormState.SecondsToBreak(StormPhase.Calm, 0f, 180f), 3);
    }

    /// <summary>The eye comes after the first wave and lasts its time, and the storm is that much longer.</summary>
    [Fact]
    public void TheEyeComesInTheMiddle()
    {
        (double opens, double closes, double ends) = StormState.PlanEye(1000.0, 300f, 60f, 0.5f);
        Assert.Equal(1150.0, opens, 3);
        Assert.Equal(1210.0, closes, 3);
        Assert.Equal(1360.0, ends, 3);

        (opens, closes, ends) = StormState.PlanEye(1000.0, 300f, 0f, 0.5f);
        Assert.Equal(0.0, opens, 3);
        Assert.Equal(0.0, closes, 3);
        Assert.Equal(1300.0, ends, 3);
    }

    /// <summary>A raging storm is in its first wave until the eye, then still, then in its last wave.</summary>
    [Theory]
    [InlineData((int)StormPhase.Raging, 1100.0, 1150.0, 1210.0, (int)StormWave.First)]
    [InlineData((int)StormPhase.Raging, 1150.0, 1150.0, 1210.0, (int)StormWave.Eye)]
    [InlineData((int)StormPhase.Raging, 1210.0, 1150.0, 1210.0, (int)StormWave.Last)]
    [InlineData((int)StormPhase.Raging, 5000.0, 0.0, 0.0, (int)StormWave.First)]
    [InlineData((int)StormPhase.Warning, 1180.0, 1150.0, 1210.0, (int)StormWave.None)]
    public void TheWavesFollowTheEye(int phase, double now, double opens, double closes, int wave)
    {
        Assert.Equal((StormWave)wave, StormState.WaveAt((StormPhase)phase, now, opens, closes));
    }

    /// <summary>The eye drives nothing on; the last wave and Ragnarok drive harder, and together harder still.</summary>
    [Theory]
    [InlineData((int)StormWave.First, false, 1f)]
    [InlineData((int)StormWave.Eye, false, 0f)]
    [InlineData((int)StormWave.Last, false, 1.5f)]
    [InlineData((int)StormWave.First, true, 1.5f)]
    [InlineData((int)StormWave.Last, true, 2.25f)]
    [InlineData((int)StormWave.Eye, true, 0f)]
    [InlineData((int)StormWave.None, true, 0f)]
    public void FuryGrowsWithTheWaves(int wave, bool ragnarok, float fury)
    {
        Assert.Equal(fury, StormState.Fury((StormWave)wave, ragnarok, 1.5f, 1.5f), 3);
    }

    /// <summary>In the eye the meter holds out in the open and falls only in real shelter; fury quickens it.</summary>
    [Fact]
    public void TheEyeHoldsTheMeterAndFuryQuickensIt()
    {
        Assert.Equal(50f, StormExposure.Step(50f, 60f, true, forest, StormCover.Open, 4f, 3f, 0f), 3);
        Assert.Equal(50f, StormExposure.Step(50f, 60f, true, forest, StormCover.Lee, 4f, 3f, 0f), 3);
        Assert.Equal(16.667f, StormExposure.Step(50f, 60f, true, forest, StormCover.RoofAndFire, 4f, 3f, 0f), 2);
        Assert.Equal(37.5f, StormExposure.Step(0f, 60f, true, forest, StormCover.Open, 4f, 3f, 1.5f), 3);
    }

    /// <summary>The storm remembers who brave it, up to ten storms, and forgets a little who sit it out.</summary>
    [Theory]
    [InlineData(0, 45f, 1)]
    [InlineData(10, 90f, 10)]
    [InlineData(3, 30f, 3)]
    [InlineData(3, 10f, 2)]
    [InlineData(0, 0f, 0)]
    public void TheStormRemembers(int before, float peak, int after)
    {
        Assert.Equal(after, StormGrudge.After(before, peak));
    }

    /// <summary>Each storm remembered quickens the meter by the step, and every two leave one more find.</summary>
    [Theory]
    [InlineData(0, 1f, 0)]
    [InlineData(4, 1.2f, 2)]
    [InlineData(10, 1.5f, 5)]
    [InlineData(25, 1.5f, 5)]
    public void TheGrudgeQuickensAndRewards(int grudge, float pace, int finds)
    {
        Assert.Equal(pace, StormGrudge.Pace(grudge, 0.05f), 3);
        Assert.Equal(finds, StormGrudge.ExtraFinds(grudge));
    }

    /// <summary>The visions come every 14 to 35 seconds when weary, twice as often at the top, more often in Ragnarok.</summary>
    [Theory]
    [InlineData(40f, false, 0f, 14f)]
    [InlineData(40f, false, 1f, 35f)]
    [InlineData(100f, false, 1f, 17.5f)]
    [InlineData(40f, true, 0f, 9.8f)]
    public void VisionsComeOftenerTheWorseItIs(float exposure, bool ragnarok, float roll, float seconds)
    {
        Assert.Equal(seconds, StormVisions.Interval(exposure, ragnarok, roll), 3);
    }

    /// <summary>The countdown reads minutes and seconds.</summary>
    [Theory]
    [InlineData(0f, "0:00")]
    [InlineData(5.2f, "0:06")]
    [InlineData(180f, "3:00")]
    [InlineData(125f, "2:05")]
    public void CountdownReads(float seconds, string text)
    {
        Assert.Equal(text, StormState.Countdown(seconds));
    }
}
