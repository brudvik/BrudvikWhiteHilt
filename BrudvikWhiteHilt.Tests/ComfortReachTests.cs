using BrudvikWhiteHilt.Comfort;
using Xunit;

namespace BrudvikWhiteHilt.Tests;

/// <summary>
/// The player's own comfort reach (docs/plans/husvette.md, the table in section 1).
/// </summary>
public class ComfortReachTests
{
    /// <summary>The defaults give 10, 20, 30 and 40 m, as the plan's table says.</summary>
    [Theory]
    [InlineData(false, 0f, 10f)]
    [InlineData(true, 0f, 20f)]
    [InlineData(true, 50f, 30f)]
    [InlineData(true, 100f, 40f)]
    [InlineData(false, 100f, 30f)]
    public void TheDefaultsGiveThePlansReach(bool book, float level, float reach)
    {
        Assert.Equal(reach, ComfortReach.Personal(book, level, 10f, 20f, 40f), 3);
    }

    /// <summary>The cap holds however large the bonuses are set.</summary>
    [Fact]
    public void TheCapHolds()
    {
        Assert.Equal(40f, ComfortReach.Personal(true, 100f, 50f, 50f, 40f), 3);
    }

    /// <summary>The reach never drops below the game's 10 m, even with a cap set lower or negative bonuses.</summary>
    [Fact]
    public void NeverBelowTheGame()
    {
        Assert.Equal(10f, ComfortReach.Personal(true, 100f, -5f, -5f, 5f), 3);
    }
}
