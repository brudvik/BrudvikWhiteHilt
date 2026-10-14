using BrudvikWhiteHilt.Crafting;
using Xunit;

namespace BrudvikWhiteHilt.Tests;

/// <summary>
/// Sorting the recipe list by what the items give, and the line shown beside each recipe.
/// </summary>
public class RecipeStatsTests
{
    private static readonly (float, float, float) Even = (1f, 1f, 1f);
    private static readonly (string, string, string) Labels = ("dmg", "armour", "block");

    private static readonly ItemStats Stew = new(70f, 20f, 0f, 30f);
    private static readonly ItemStats Soup = new(25f, 60f, 40f, 25f);
    private static readonly ItemStats Sword = new(0f, 0f, 0f, 0f, 46f, StrengthKind.Damage);

    /// <summary>Each food sort picks its own number, and the total adds them up.</summary>
    [Fact]
    public void FoodIsSortedByTheChosenNumber()
    {
        Assert.Equal(70f, RecipeStats.Key(Stew, RecipeSort.Health, Even));
        Assert.Equal(60f, RecipeStats.Key(Soup, RecipeSort.Stamina, Even));
        Assert.Equal(40f, RecipeStats.Key(Soup, RecipeSort.Eitr, Even));
        Assert.Equal(125f, RecipeStats.Key(Soup, RecipeSort.Total, Even));
    }

    /// <summary>The profile decides which food is best for the player.</summary>
    [Fact]
    public void TheProfileDecidesTheBestFood()
    {
        (float, float, float) warrior = RecipeStats.Weights(FoodProfile.Warrior, Even);
        (float, float, float) mage = RecipeStats.Weights(FoodProfile.Mage, Even);
        Assert.True(RecipeStats.Key(Stew, RecipeSort.BestForMe, warrior) > RecipeStats.Key(Soup, RecipeSort.BestForMe, warrior));
        Assert.True(RecipeStats.Key(Soup, RecipeSort.BestForMe, mage) > RecipeStats.Key(Stew, RecipeSort.BestForMe, mage));
        Assert.Equal((2f, 0f, 1f), RecipeStats.Weights(FoodProfile.Custom, (2f, 0f, 1f)));
    }

    /// <summary>What a sort says nothing about goes after what it does.</summary>
    [Fact]
    public void WhatTheSortDoesNotCoverGoesLast()
    {
        Assert.Null(RecipeStats.Key(Sword, RecipeSort.Health, Even));
        Assert.Null(RecipeStats.Key(Stew, RecipeSort.Strength, Even));
        Assert.Equal(46f, RecipeStats.Key(Sword, RecipeSort.Strength, Even));
        Assert.True(RecipeStats.Compare(5f, null) < 0);
        Assert.True(RecipeStats.Compare(null, 5f) > 0);
        Assert.True(RecipeStats.Compare(10f, 5f) < 0);
        Assert.Equal(0, RecipeStats.Compare(null, null));
    }

    /// <summary>A food's line has its numbers in the game's colours, the sorted one in bold, and its time.</summary>
    [Fact]
    public void AFoodLineShowsItsNumbers()
    {
        string line = RecipeStats.Format(Stew, RecipeSort.Health, Even, Labels);
        Assert.Contains("<color=#FF8080><b>70</b></color>", line);
        Assert.Contains("<color=#FFFF80>20</color>", line);
        Assert.DoesNotContain("#9090FF", line);
        Assert.Contains("30m", line);
        Assert.Contains("(125)", RecipeStats.Format(Soup, RecipeSort.Total, Even, Labels));
    }

    /// <summary>Gear shows its strength with its word, a potion its effect.</summary>
    [Fact]
    public void GearAndPotionsShowWhatTheyGive()
    {
        Assert.Contains("46", RecipeStats.Format(Sword, RecipeSort.Name, Even, Labels));
        Assert.Contains("dmg", RecipeStats.Format(Sword, RecipeSort.Name, Even, Labels));
        Assert.Contains("block", RecipeStats.Format(new ItemStats(0f, 0f, 0f, 0f, 30f, StrengthKind.Block), RecipeSort.Name, Even, Labels));
        Assert.Contains("Healing", RecipeStats.Format(new ItemStats(0f, 0f, 0f, 2f, effect: "Healing"), RecipeSort.Name, Even, Labels));
        Assert.Equal(string.Empty, RecipeStats.Format(default, RecipeSort.Name, Even, Labels));
    }
}
