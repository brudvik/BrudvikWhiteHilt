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
