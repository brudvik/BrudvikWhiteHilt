using BrudvikWhiteHilt.SeaBattles;
using Xunit;

namespace BrudvikWhiteHilt.Tests;

public class PursuitTests
{
    [Fact]
    public void TheShockHoldsTheShipWhateverIsAboard()
    {
        Assert.Equal(PursuitPhase.Shock, PursuitRules.Phase(0f, 60f, false));
        Assert.Equal(PursuitPhase.Shock, PursuitRules.Phase(59.9f, 60f, true));
    }

    [Fact]
    public void AfterTheShockTheShipIsFreeUnlessEnemiesAreAboard()
    {
        Assert.Equal(PursuitPhase.Free, PursuitRules.Phase(60f, 60f, false));
        Assert.Equal(PursuitPhase.Held, PursuitRules.Phase(120f, 60f, true));
        Assert.Equal(PursuitPhase.Free, PursuitRules.Phase(121f, 60f, false));
    }

    [Fact]
    public void ZeroShockSecondsMeansNoShock()
    {
        Assert.Equal(PursuitPhase.Free, PursuitRules.Phase(0f, 0f, false));
    }

    [Fact]
    public void TheCapIsBelowTheHuntersSpeed()
    {
        Assert.Equal(8.5f, PursuitRules.Cap(PursuitPhase.Shock, 10f, 0.85f, 4f).Value, 3);
        Assert.Equal(8.5f, PursuitRules.Cap(PursuitPhase.Held, 10f, 0.85f, 4f).Value, 3);
    }

    [Fact]
    public void TheFloorKeepsTheShipMoving()
    {
        Assert.Equal(4f, PursuitRules.Cap(PursuitPhase.Shock, 0f, 0.85f, 4f).Value);
        Assert.Equal(4f, PursuitRules.Cap(PursuitPhase.Shock, 3f, 0.85f, 4f).Value);
    }

    [Fact]
    public void AFreeShipHasNoCap()
    {
        Assert.Null(PursuitRules.Cap(PursuitPhase.Free, 10f, 0.85f, 4f));
    }

    [Fact]
    public void TheReferenceRisesFasterThanItFalls()
    {
        float up = PursuitRules.Reference(0f, 10f, 0.5f) - 0f;
        float down = 10f - PursuitRules.Reference(10f, 0f, 0.5f);

        Assert.True(up > down * 3f);
        Assert.Equal(10f, PursuitRules.Reference(0f, 10f, 100f));
        Assert.Equal(5f, PursuitRules.Reference(5f, 5f, 0.02f));
    }

    [Fact]
    public void TheLimitBrakesGraduallyAndNeverBelowTheCap()
    {
        Assert.Equal(11.94f, PursuitRules.Limit(12f, 8f, 0.02f, 3f), 3);
        Assert.Equal(8f, PursuitRules.Limit(8.01f, 8f, 0.02f, 3f));
        Assert.Equal(6f, PursuitRules.Limit(6f, 8f, 0.02f, 3f));
        Assert.Equal(12f, PursuitRules.Limit(12f, null, 0.02f, 3f));
    }
}
