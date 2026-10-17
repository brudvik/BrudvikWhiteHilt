using BrudvikWhiteHilt.Crafting.Refinement;
using Xunit;

namespace BrudvikWhiteHilt.Tests;

/// <summary>
/// The odds at the Forge of Potential.
/// </summary>
public class RefinementTests
{
    private static readonly RefinementBonuses Bonuses = new(0.15f, 0.1f, 0.1f, 0.1f, 0.1f, 0.95f, 0.5f);

    /// <summary>A beginner gets the idol's own chance; the skills raise it, each by its weight.</summary>
    [Fact]
    public void SkillsRaiseTheChance()
    {
        Assert.Equal(0.65f, RefinementOdds.SuccessChance(0.65f, new RefinementSkills(0f, 0f, 0f), 0, 0, Bonuses), 3);
        Assert.Equal(0.8f, RefinementOdds.SuccessChance(0.65f, new RefinementSkills(1f, 0f, 0f), 0, 0, Bonuses), 3);
        Assert.Equal(0.825f, RefinementOdds.SuccessChance(0.65f, new RefinementSkills(0.5f, 0.5f, 0.5f), 0, 0, Bonuses), 3);
    }

    /// <summary>Failed tries and extra idols add to the chance, but never past the most the bonuses may give.</summary>
    [Fact]
    public void PityAndIdolsAreCapped()
    {
        RefinementSkills none = new(0f, 0f, 0f);
        Assert.Equal(0.85f, RefinementOdds.SuccessChance(0.65f, none, 1, 1, Bonuses), 3);
        Assert.Equal(0.95f, RefinementOdds.SuccessChance(0.65f, none, 10, 0, Bonuses), 3);
        Assert.Equal(0.98f, RefinementOdds.SuccessChance(0.98f, none, 10, 0, Bonuses), 3);
    }

    /// <summary>Pity always reaches the cap, so every item can be refined in the end.</summary>
    [Fact]
    public void PityReachesTheCap()
    {
        RefinementSkills none = new(0f, 0f, 0f);
        int failures = 0;
        while (RefinementOdds.SuccessChance(0.2f, none, failures, 0, Bonuses) < 0.95f)
        {
            failures++;
        }

        Assert.Equal(8, failures);
    }

    /// <summary>Experience takes away from the break chance, by the share the server sets.</summary>
    [Fact]
    public void ExperienceLowersTheBreakChance()
    {
        Assert.Equal(0.1f, RefinementOdds.BreakChance(0.1f, new RefinementSkills(0f, 0f, 0f), Bonuses), 3);
        Assert.Equal(0.05f, RefinementOdds.BreakChance(0.1f, new RefinementSkills(1f, 1f, 1f), Bonuses), 3);
        Assert.Equal(0f, RefinementOdds.Experience(new RefinementSkills(1f, 1f, 1f), new RefinementBonuses(0f, 0f, 0f, 0f, 0f, 1f, 1f)), 3);
    }

    /// <summary>Low rolls succeed; a failure follows the server's rule, the game's breaking on the highest rolls.</summary>
    [Theory]
    [InlineData(0.1, RefinementFailure.Vanilla, RefinementOutcome.Success)]
    [InlineData(0.7, RefinementFailure.Vanilla, RefinementOutcome.Downgrade)]
    [InlineData(0.95, RefinementFailure.Vanilla, RefinementOutcome.Break)]
    [InlineData(0.95, RefinementFailure.LoseIdol, RefinementOutcome.KeepLevel)]
    [InlineData(0.7, RefinementFailure.Downgrade, RefinementOutcome.Downgrade)]
    [InlineData(0.7, RefinementFailure.Break, RefinementOutcome.Break)]
    public void RollsComeToOutcomes(double roll, RefinementFailure failure, RefinementOutcome outcome)
    {
        Assert.Equal(outcome, RefinementOdds.Decide(roll, 0.65f, 0.1f, failure));
    }

    /// <summary>An idol costs the base price times the worth of its biome's materials.</summary>
    [Theory]
    [InlineData(200, 1f, 200)]
    [InlineData(200, 6f, 1200)]
    [InlineData(200, 0.5f, 200)]
    public void IdolsCostByTheirBiome(int basePrice, float worth, int price)
    {
        Assert.Equal(price, RefinementOdds.IdolPrice(basePrice, worth));
    }
}
