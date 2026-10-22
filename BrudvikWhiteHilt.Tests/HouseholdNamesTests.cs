using BrudvikWhiteHilt.Household;
using System.Linq;
using Xunit;

namespace BrudvikWhiteHilt.Tests;

/// <summary>
/// Names for the household (docs/plans/household.md, section 5): Norse, no repeats on one farm, and never a trader's.
/// </summary>
public class HouseholdNamesTests
{
    private static readonly string[] traders = { "Asbjørn", "Gunnar", "Ragnhild", "Hallvard", "Sigrid", "Ylva", "Brand", "Frøydis" };

    /// <summary>The same seed gives the same name, so every machine agrees.</summary>
    [Fact]
    public void SameSeedSameName()
    {
        Assert.Equal(HouseholdNames.Pick(12345, false, null), HouseholdNames.Pick(12345, false, null));
        Assert.Equal(HouseholdNames.Pick(-7, true, null), HouseholdNames.Pick(-7, true, null));
    }

    /// <summary>A man gets a man's name and a woman a woman's, negative seeds too.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void NameFitsTheBody(int seed)
    {
        Assert.Contains(HouseholdNames.Pick(seed, false, null), HouseholdNames.Men);
        Assert.Contains(HouseholdNames.Pick(seed, true, null), HouseholdNames.Women);
    }

    /// <summary>A taken name is passed over for the next free one.</summary>
    [Fact]
    public void TakenNamesArePassedOver()
    {
        string first = HouseholdNames.Pick(3, false, null);
        string second = HouseholdNames.Pick(3, false, new[] { first });
        Assert.NotEqual(first, second);
        Assert.Contains(second, HouseholdNames.Men);
    }

    /// <summary>A whole farm of the same seed still gets different names until every name is used.</summary>
    [Fact]
    public void NoRepeatsUntilEveryNameIsUsed()
    {
        var farm = new System.Collections.Generic.List<string>();
        for (int i = 0; i < HouseholdNames.Women.Length; i++)
        {
            farm.Add(HouseholdNames.Pick(99, true, farm));
        }

        Assert.Equal(farm.Count, farm.Distinct().Count());
        Assert.Contains(HouseholdNames.Pick(99, true, farm), HouseholdNames.Women);
    }

    /// <summary>No member is named like a trader, and no name is listed twice.</summary>
    [Fact]
    public void NoTradersAndNoDuplicates()
    {
        string[] all = HouseholdNames.Men.Concat(HouseholdNames.Women).ToArray();
        Assert.Empty(all.Intersect(traders));
        Assert.Equal(all.Length, all.Distinct().Count());
    }
}
