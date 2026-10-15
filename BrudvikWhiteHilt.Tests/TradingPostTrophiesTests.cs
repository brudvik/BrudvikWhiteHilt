using BrudvikWhiteHilt.TradingPosts;
using System.Linq;
using Xunit;

namespace BrudvikWhiteHilt.Tests;

/// <summary>
/// What the trading posts' hunter sells, and for how much.
/// </summary>
public class TradingPostTrophiesTests
{
    private static readonly string[] bosses =
    {
        "TrophyEikthyr", "TrophyTheElder", "TrophyBonemass", "TrophyDragonQueen", "TrophyGoblinKing", "TrophySeekerQueen", "TrophyFader"
    };

    /// <summary>Every biome's hunter has trophies to sell.</summary>
    [Fact]
    public void EveryBiomeHasTrophies()
    {
        for (int biome = 0; biome < TradingPostGoods.Biomes.Length; biome++)
        {
            Assert.True(TradingPostTrophies.HasAny(biome), TradingPostGoods.Biomes[biome]);
        }
    }

    /// <summary>No boss's trophy is sold, as they open the Forsaken powers, and no trophy is sold by two biomes.</summary>
    [Fact]
    public void NoBossAndNoTrophyTwice()
    {
        string[] all = Enumerable.Range(0, TradingPostGoods.Biomes.Length).SelectMany(TradingPostTrophies.Of).ToArray();
        Assert.Empty(all.Intersect(bosses));
        Assert.Equal(all.Length, all.Distinct().Count());
    }

    /// <summary>White Hilt's own monsters' trophies are sold where the monsters live.</summary>
    [Fact]
    public void OwnMonstersAreSoldInTheirBiomes()
    {
        Assert.Contains("WhiteHilt_TrophyGiantSpider", TradingPostTrophies.Of(TradingPostGoods.IndexOf("BlackForest")));
        Assert.Contains("WhiteHilt_TrophyDesertDragon", TradingPostTrophies.Of(TradingPostGoods.IndexOf("Plains")));
        Assert.Contains("WhiteHilt_TrophyMistStag", TradingPostTrophies.Of(TradingPostGoods.IndexOf("Mistlands")));
        Assert.Contains("WhiteHilt_TrophyGarmHound", TradingPostTrophies.Of(TradingPostGoods.IndexOf("AshLands")));
    }

    /// <summary>The Deep North's hunter sells the Elaking's trophy, at a great beast's price.</summary>
    [Fact]
    public void TheElakingIsSoldAsAGreatBeast()
    {
        int north = TradingPostGoods.Biomes.Length - 1;
        Assert.Contains("TrophyElaking", TradingPostTrophies.Of(north));
        Assert.Equal(3 * TradingPostTrophies.Price(north, "TrophyMoose", 15f), TradingPostTrophies.Price(north, "TrophyElaking", 15f));
    }

    /// <summary>A later biome's trophies cost more, and a great beast's three times an ordinary one's.</summary>
    [Fact]
    public void PricesFollowTheBiomeAndTheBeast()
    {
        int meadows = TradingPostTrophies.Price(0, "TrophyBoar", 15f);
        int ashlands = TradingPostTrophies.Price(TradingPostGoods.IndexOf("AshLands"), "TrophyCharredMelee", 15f);
        Assert.Equal(15, meadows);
        Assert.True(ashlands > meadows);
        int swamp = TradingPostGoods.IndexOf("Swamp");
        Assert.Equal(3 * TradingPostTrophies.Price(swamp, "TrophyDraugr", 15f), TradingPostTrophies.Price(swamp, "TrophyAbomination", 15f));
    }
}
