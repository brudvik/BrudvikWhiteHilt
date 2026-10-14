using BrudvikWhiteHilt.TradingPosts;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace BrudvikWhiteHilt.Tests;

/// <summary>
/// The Trading skill and the gear each post sells (docs/plans/trading-skill.md).
/// </summary>
public class TradingGearTests
{
    private static readonly int[] DefaultLevels = { 20, 40, 60, 80 };

    /// <summary>A recipe belongs to the biome of its latest material, and one with an unknown material to none.</summary>
    [Fact]
    public void RecipeBelongsToItsLatestMaterial()
    {
        Assert.Equal(1, TradingGear.RecipeBiome(new[] { ("Wood", 2), ("Bronze", 8), ("LeatherScraps", 2) }));
        Assert.Equal(2, TradingGear.RecipeBiome(new[] { ("Iron", 20), ("ElderBark", 4) }));
        Assert.Equal(0, TradingGear.RecipeBiome(new[] { ("Wood", 4), ("Flint", 6) }));
        Assert.Equal(-1, TradingGear.RecipeBiome(new[] { ("Wood", 4), ("SomethingFromAnotherMod", 1) }));
        Assert.Equal(-1, TradingGear.RecipeBiome(new (string, int)[0]));
    }

    /// <summary>Gear costs its materials times the factor; refined metal is worth more than ore.</summary>
    [Fact]
    public void GearCostsItsMaterials()
    {
        int bronze = TradingGear.RecipePrice(new[] { ("Bronze", 8), ("Wood", 2), ("LeatherScraps", 2) }, 1f);
        Assert.Equal(83, bronze);
        Assert.Equal(124, TradingGear.RecipePrice(new[] { ("Bronze", 8), ("Wood", 2), ("LeatherScraps", 2) }, 1.5f));
        Assert.True(TradingGear.MaterialPrice("Iron") > TradingGear.MaterialPrice("IronScrap"));
        Assert.Equal(7f, TradingGear.MaterialPrice("Wood", new Dictionary<string, float> { { "Wood", 7f } }));
    }

    /// <summary>Every piece of a biome's gear is sold at exactly one of its posts.</summary>
    [Fact]
    public void EachPieceIsSoldAtOnePost()
    {
        List<GearEntry> pool = TradingGear.Ordered(Enumerable.Range(0, 11).Select(i => new GearEntry($"Item{i}", GearKind.Weapons, 1, 100 - i)));
        List<string> sold = new();
        for (int slot = 0; slot < 4; slot++)
        {
            List<GearEntry> post = TradingGear.ForSlot(pool, slot, 4);
            Assert.InRange(post.Count, 2, 3);
            sold.AddRange(post.Select(entry => entry.Prefab));
        }

        Assert.Equal(11, sold.Count);
        Assert.Equal(11, sold.Distinct().Count());
        Assert.Empty(TradingGear.ForSlot(pool, 4, 4));
        Assert.Equal(90, pool[0].Price);
    }

    /// <summary>The posts are ordered by id, so every machine gives a post the same place.</summary>
    [Fact]
    public void PostsAreOrderedById()
    {
        string[] ids = { "c", "a", "b" };
        Assert.Equal(0, TradingGear.SlotOf(ids, "a"));
        Assert.Equal(2, TradingGear.SlotOf(ids, "c"));
        Assert.Equal(-1, TradingGear.SlotOf(ids, "d"));
    }

    /// <summary>The cheapest quarter opens at 20, the dearest at 80.</summary>
    [Fact]
    public void GearOpensInSteps()
    {
        Assert.Equal(20, TradingGear.UnlockLevel(0, 8, DefaultLevels));
        Assert.Equal(20, TradingGear.UnlockLevel(1, 8, DefaultLevels));
        Assert.Equal(40, TradingGear.UnlockLevel(2, 8, DefaultLevels));
        Assert.Equal(80, TradingGear.UnlockLevel(7, 8, DefaultLevels));
        Assert.Equal(20, TradingGear.UnlockLevel(0, 1, DefaultLevels));
        Assert.Equal(0, TradingGear.UnlockLevel(3, 8, new int[0]));
    }

    /// <summary>The levels are read in any order, bad entries left out.</summary>
    [Fact]
    public void LevelsAreRead()
    {
        Assert.Equal(new[] { 10, 40, 90 }, TradingGear.ParseLevels("90, 10,x,140, 40"));
        Assert.Empty(TradingGear.ParseLevels(null));
    }

    /// <summary>The share grows from a quarter to 40 %, and the discount to a tenth.</summary>
    [Fact]
    public void SkillPaysBetter()
    {
        Assert.Equal(0.25f, TradingGear.BuyShare(0f, 0.25f, 0.4f), 3);
        Assert.Equal(0.325f, TradingGear.BuyShare(50f, 0.25f, 0.4f), 3);
        Assert.Equal(0.4f, TradingGear.BuyShare(100f, 0.25f, 0.4f), 3);
        Assert.Equal(0.25f, TradingGear.BuyShare(100f, 0.25f, 0.1f), 3);
        Assert.Equal(100, TradingGear.Discounted(100, 0f, 0.1f));
        Assert.Equal(90, TradingGear.Discounted(100, 100f, 0.1f));
        Assert.Equal(1, TradingGear.Discounted(1, 100f, 0.5f));
    }

    /// <summary>A trade gives a point per so many coins.</summary>
    [Fact]
    public void TradeGivesExperience()
    {
        Assert.Equal(20f, TradingGear.Experience(100, 5f), 3);
        Assert.Equal(0f, TradingGear.Experience(100, 0f));
        Assert.Equal(0f, TradingGear.Experience(0, 5f));
    }

    /// <summary>A post is known for what most of its gear is.</summary>
    [Fact]
    public void PostIsKnownForMostOfItsGear()
    {
        Assert.Equal(GearKind.Armour, TradingGear.Specialty(new[] { GearKind.Weapons, GearKind.Armour, GearKind.Armour }));
        Assert.Equal(GearKind.Weapons, TradingGear.Specialty(new[] { GearKind.Shields, GearKind.Weapons }));
        Assert.Null(TradingGear.Specialty(new GearKind[0]));
    }
}
