using BrudvikWhiteHilt.TradingPosts;
using System.Linq;
using Xunit;

namespace BrudvikWhiteHilt.Tests;

/// <summary>
/// Where the traders' garb is sold, what it costs and what it gets its wearer at the posts.
/// </summary>
public class TraderGarbTests
{
    /// <summary>With four posts each sells one piece; with fewer every piece is still sold somewhere, with more again.</summary>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(6)]
    public void EveryPieceIsSoldAtSomePost(int count)
    {
        int[] sold = Enumerable.Range(0, count).SelectMany(slot => TraderGarb.PiecesAt(slot, count)).ToArray();
        Assert.Equal(Enumerable.Range(0, TraderGarb.Pieces.Length), sold.Distinct().OrderBy(piece => piece));
        if (count <= TraderGarb.Pieces.Length)
        {
            Assert.Equal(TraderGarb.Pieces.Length, sold.Length);
        }
    }

    /// <summary>Four posts: one piece each, in order; a fifth post sells the first piece again.</summary>
    [Fact]
    public void FourPostsOnePieceEach()
    {
        for (int slot = 0; slot < 4; slot++)
        {
            Assert.Equal(new[] { slot }, TraderGarb.PiecesAt(slot, 4));
        }

        Assert.Equal(new[] { 0 }, TraderGarb.PiecesAt(4, 5));
        Assert.Empty(TraderGarb.PiecesAt(-1, 4));
    }

    /// <summary>Every piece worn betters the prices, the whole set more.</summary>
    [Fact]
    public void TradeBonusGrowsWithThePieces()
    {
        Assert.Equal(0f, TraderGarb.TradeBonus(0, 3f, 3f));
        Assert.Equal(0.03f, TraderGarb.TradeBonus(1, 3f, 3f), 4);
        Assert.Equal(0.09f, TraderGarb.TradeBonus(3, 3f, 3f), 4);
        Assert.Equal(0.15f, TraderGarb.TradeBonus(4, 3f, 3f), 4);
        Assert.Equal(97, TraderGarb.Lowered(100, 0.03f));
        Assert.Equal(1, TraderGarb.Lowered(1, 0.15f));
        Assert.Equal(100, TraderGarb.Lowered(100, 0f));
    }

    /// <summary>A later biome's garb costs more.</summary>
    [Fact]
    public void PriceFollowsTheBiome()
    {
        Assert.Equal(120, TraderGarb.Price(0, 120f));
        Assert.Equal(1200, TraderGarb.Price(TradingPostGoods.IndexOf("DeepNorth"), 120f));
    }
}
