using BrudvikWhiteHilt.TradingPosts;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace BrudvikWhiteHilt.Tests;

/// <summary>
/// The trading posts' goods and prices (plan section 4).
/// </summary>
public class TradingPostGoodsTests
{
    /// <summary>Every biome sells something, and no material belongs to two biomes.</summary>
    [Fact]
    public void EveryBiomeSellsItsOwnMaterials()
    {
        List<string> all = new();
        for (int biome = 0; biome < TradingPostGoods.Biomes.Length; biome++)
        {
            Assert.NotEmpty(TradingPostGoods.Materials(biome));
            all.AddRange(TradingPostGoods.Materials(biome));
        }

        Assert.Equal(all.Count, all.Distinct().Count());
    }

    /// <summary>Each post buys the next biome's goods, the Deep North's the Meadows'.</summary>
    [Fact]
    public void PostsBuyTheNextBiomesGoods()
    {
        int meadows = TradingPostGoods.IndexOf("Meadows");
        int deepNorth = TradingPostGoods.IndexOf("DeepNorth");
        Assert.True(TradingPostGoods.BuyPrice(meadows, "CopperOre", 1, 0.25f) > 0f);
        Assert.Equal(0f, TradingPostGoods.BuyPrice(meadows, "Wood", 1, 0.25f));
        Assert.Equal(0f, TradingPostGoods.BuyPrice(meadows, "IronScrap", 1, 0.25f));
        Assert.True(TradingPostGoods.BuyPrice(deepNorth, "Wood", 1, 0.25f) > 0f);
        Assert.Equal(TradingPostGoods.IndexOf("Ashlands"), TradingPostGoods.IndexOf("AshLands"));
    }

    /// <summary>A post pays a quarter of what it asks.</summary>
    [Fact]
    public void APostPaysAShareOfItsPrice()
    {
        int meadows = TradingPostGoods.IndexOf("Meadows");
        Assert.Equal(TradingPostGoods.UnitPrice("TrollHide") / 4f, TradingPostGoods.BuyPrice(meadows, "TrollHide", 1, 0.25f), 3);
    }

    /// <summary>Every post buys every fish; a later biome's fish and a fish with stars fetch more.</summary>
    [Fact]
    public void FishSellEverywhereAndBetterFishFetchMore()
    {
        for (int biome = 0; biome < TradingPostGoods.Biomes.Length; biome++)
        {
            Assert.True(TradingPostGoods.BuyPrice(biome, "Fish1", 1, 0.25f) > 0f);
        }

        Assert.True(TradingPostGoods.BuyPrice(0, "Fish10", 1, 0.25f) > TradingPostGoods.BuyPrice(0, "Fish1", 1, 0.25f));
        Assert.True(TradingPostGoods.BuyPrice(0, "Fish1", 3, 0.25f) > TradingPostGoods.BuyPrice(0, "Fish1", 1, 0.25f));
    }

    /// <summary>Ore costs more than bulk wood of the same biome, and later biomes cost more.</summary>
    [Fact]
    public void PricesFollowKindAndBiome()
    {
        Assert.True(TradingPostGoods.UnitPrice("CopperOre") > TradingPostGoods.UnitPrice("FineWood"));
        Assert.True(TradingPostGoods.UnitPrice("SilverOre") > TradingPostGoods.UnitPrice("CopperOre"));
        Assert.Equal(0f, TradingPostGoods.UnitPrice("Coins"));
    }

    /// <summary>Configured prices replace the built-in ones; bad entries are skipped.</summary>
    [Fact]
    public void ConfiguredPricesAreRead()
    {
        Dictionary<string, float> prices = TradingPostGoods.ParsePrices("Wood:3, Stone:x, :4, Flint:0.5");
        Assert.Equal(2, prices.Count);
        Assert.Equal(3f, TradingPostGoods.AskingPrice("Wood", prices));
        Assert.Equal(TradingPostGoods.UnitPrice("Resin"), TradingPostGoods.AskingPrice("Resin", prices));
    }

    /// <summary>The Meadows' posts are open at once; every other biome needs its boss key.</summary>
    [Fact]
    public void LaterBiomesNeedTheirKey()
    {
        Assert.Equal(string.Empty, TradingPostGoods.OpenKey(0));
        for (int biome = 1; biome < TradingPostGoods.Biomes.Length; biome++)
        {
            Assert.StartsWith("defeated_", TradingPostGoods.OpenKey(biome));
        }
    }
}
