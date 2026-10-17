using System;
using BrudvikWhiteHilt.Pieces.Portals.WhiteHiltPortal;
using Xunit;

namespace BrudvikWhiteHilt.Tests;

public class PortalSearchTests
{
    private static bool Finds(string name, string search)
    {
        return PortalTravelPanel.Matches(name, PortalTravelPanel.Simplify(search).Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
    }

    [Theory]
    [InlineData("Huset")]
    [InlineData("huset")]
    [InlineData("HUSET")]
    [InlineData("  Huset ")]
    [InlineData("Stilker")]
    [InlineData("på stilker")]
    [InlineData("pa stilker")]
    [InlineData("stilker huset")]
    [InlineData("set p")]
    public void APortalIsFoundByAnyPartOfItsName(string search)
    {
        Assert.True(Finds("Huset på Stilker", search));
    }

    [Fact]
    public void ANameWithTagsOrOtherSpacesIsFoundToo()
    {
        Assert.True(Finds("<color=yellow>Huset</color> på Stilker", "huset på"));
        Assert.True(Finds("Huset på  Stilker", "huset på stilker"));
        Assert.True(Finds("Huset på Stilker", "på"));
    }

    [Fact]
    public void EveryWordMustBeInTheName()
    {
        Assert.False(Finds("Huset på Stilker", "huset fjellet"));
        Assert.False(Finds("Naustet", "huset"));
        Assert.True(Finds("Naustet", string.Empty));
        Assert.True(Finds(string.Empty, "   "));
    }
}
