using BrudvikWhiteHilt.TradingPosts;
using System.Collections.Generic;
using Xunit;

namespace BrudvikWhiteHilt.Tests;

/// <summary>
/// Supply and demand at the trading posts.
/// </summary>
public class TradingMarketTests
{
    /// <summary>Each stack sold takes a tenth off, down to the least share.</summary>
    [Theory]
    [InlineData(0f, 1f)]
    [InlineData(1f, 0.9f)]
    [InlineData(3f, 0.7f)]
    [InlineData(20f, 0.4f)]
    public void StacksSoldLowerThePrice(float glut, float share)
    {
        Assert.Equal(share, TradingMarket.Share(glut, 0.1f, 0.4f), 3);
    }

    /// <summary>A sale adds its share of a stack; the glut runs off over the days and never below nothing.</summary>
    [Fact]
    public void GlutGrowsAndRunsOff()
    {
        float glut = TradingMarket.AfterSale(0f, 25, 10);
        Assert.Equal(2.5f, glut, 3);
        Assert.Equal(1f, TradingMarket.Glut(glut, 1.0, 1.5f), 3);
        Assert.Equal(0f, TradingMarket.Glut(glut, 5.0, 1.5f), 3);
    }

    /// <summary>The market is written and read back; goods without a glut are left out.</summary>
    [Fact]
    public void WrittenAndReadBack()
    {
        Dictionary<string, TradingMarket.Entry> market = new()
        {
            ["Iron"] = new TradingMarket.Entry(1.25f, 638000000000000000L),
            ["Wood"] = new TradingMarket.Entry(0f, 1L)
        };

        Dictionary<string, TradingMarket.Entry> read = TradingMarket.Read(TradingMarket.Write(market));

        Assert.Single(read);
        Assert.Equal(1.25f, read["Iron"].Glut, 3);
        Assert.Equal(638000000000000000L, read["Iron"].Ticks);
        Assert.Empty(TradingMarket.Read("broken|data;;x"));
    }

    /// <summary>The posts pay more while a storm rages and for a day after it.</summary>
    [Theory]
    [InlineData(true, -1.0, true)]
    [InlineData(false, -1.0, false)]
    [InlineData(false, 100.0, true)]
    [InlineData(false, 1900.0, false)]
    public void StormsRaiseDemand(bool raging, double since, bool demand)
    {
        Assert.Equal(demand, TradingMarket.StormDemand(raging, since, 1800f));
    }
}
