using BrudvikWhiteHilt.TradingPosts;
using System.Linq;
using Xunit;

namespace BrudvikWhiteHilt.Tests;

/// <summary>
/// The monsters that plague the trading posts, and what their heads are paid.
/// </summary>
public class TradingPostTerrorTests
{
    /// <summary>Every biome has its own plague, each with its own monster and trophy.</summary>
    [Fact]
    public void EveryBiomeHasItsOwnPlague()
    {
        Assert.Equal(TradingPostGoods.Biomes.Length, TerrorDefinition.All.Length);
        Assert.Equal(TerrorDefinition.All.Length, TerrorDefinition.All.Select(terror => terror.PrefabName).Distinct().Count());
        Assert.Equal(TerrorDefinition.All.Length, TerrorDefinition.All.Select(terror => terror.TrophyName).Distinct().Count());
        Assert.All(TerrorDefinition.All, terror => Assert.True(terror.Scale > 1f, terror.Key));
    }

    /// <summary>A trophy names the biome of its plague; any other item none.</summary>
    [Fact]
    public void TrophiesKnowTheirBiome()
    {
        for (int biome = 0; biome < TerrorDefinition.All.Length; biome++)
        {
            Assert.Equal(biome, TerrorDefinition.BiomeOfTrophy(TerrorDefinition.Of(biome).TrophyName));
        }

        Assert.Equal(-1, TerrorDefinition.BiomeOfTrophy("TrophyBoar"));
        Assert.Null(TerrorDefinition.Of(-1));
    }

    /// <summary>The bounty grows with the biome: 800 coins in the Meadows, 8000 in the Deep North.</summary>
    [Fact]
    public void BountyFollowsTheBiome()
    {
        Assert.Equal(800, TradingPostTerrors.Bounty(0, 800f));
        Assert.Equal(8000, TradingPostTerrors.Bounty(TradingPostGoods.IndexOf("DeepNorth"), 800f));
    }

    /// <summary>A plague lasts until its end time, and none is on without one.</summary>
    [Fact]
    public void PlagueLastsUntilItsTime()
    {
        Assert.True(TradingPostTerror.Lasts(5000, 4999));
        Assert.False(TradingPostTerror.Lasts(5000, 5000));
        Assert.False(TradingPostTerror.Lasts(0, 10));
        Assert.Equal(5400.0, TradingPostTerror.Duration);
    }
}
