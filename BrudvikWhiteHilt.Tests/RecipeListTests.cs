using BrudvikWhiteHilt.Crafting;
using Xunit;
using ItemType = ItemDrop.ItemData.ItemType;
using SkillType = Skills.SkillType;

namespace BrudvikWhiteHilt.Tests;

public class RecipeListTests
{
    [Fact]
    public void SearchFindsNameOrMaterial()
    {
        string[] materials = { "Bronze", "Wood" };

        Assert.True(RecipeList.Matches(RecipeList.Words("sword"), "Bronze sword", materials));
        Assert.True(RecipeList.Matches(RecipeList.Words("WOOD"), "Bronze sword", materials));
        Assert.False(RecipeList.Matches(RecipeList.Words("iron"), "Bronze sword", materials));
    }

    [Fact]
    public void EveryWordMustBeFound()
    {
        string[] materials = { "Bronze", "Wood" };

        Assert.True(RecipeList.Matches(RecipeList.Words("  bronze   sword "), "Bronze sword", materials));
        Assert.True(RecipeList.Matches(RecipeList.Words("sword wood"), "Bronze sword", materials));
        Assert.False(RecipeList.Matches(RecipeList.Words("bronze axe"), "Bronze sword", materials));
    }

    [Fact]
    public void EmptySearchFindsEverything()
    {
        Assert.Empty(RecipeList.Words("   "));
        Assert.True(RecipeList.Matches(RecipeList.Words(null), "Bronze sword", null));
    }

    [Fact]
    public void FavoritesReadBackTheSame()
    {
        string saved = RecipeList.FormatFavorites(new[] { "Recipe_SwordBronze", "Recipe_ArrowWood" });

        Assert.Equal("Recipe_ArrowWood;Recipe_SwordBronze", saved);
        Assert.Equal(new[] { "Recipe_ArrowWood", "Recipe_SwordBronze" }, RecipeList.ParseFavorites(saved));
        Assert.Empty(RecipeList.ParseFavorites(null));
        Assert.Single(RecipeList.ParseFavorites(" ;Recipe_Hammer; ;"));
    }

    [Theory]
    [InlineData(ItemType.OneHandedWeapon, SkillType.Swords, RecipeCategory.Weapons)]
    [InlineData(ItemType.OneHandedWeapon, SkillType.Axes, RecipeCategory.Weapons)]
    [InlineData(ItemType.Bow, SkillType.Bows, RecipeCategory.Weapons)]
    [InlineData(ItemType.TwoHandedWeapon, SkillType.Pickaxes, RecipeCategory.Tools)]
    [InlineData(ItemType.TwoHandedWeapon, SkillType.Fishing, RecipeCategory.Tools)]
    [InlineData(ItemType.Tool, SkillType.None, RecipeCategory.Tools)]
    [InlineData(ItemType.Torch, SkillType.None, RecipeCategory.Tools)]
    [InlineData(ItemType.Shield, SkillType.Blocking, RecipeCategory.Armor)]
    [InlineData(ItemType.Utility, SkillType.None, RecipeCategory.Armor)]
    [InlineData(ItemType.Ammo, SkillType.Bows, RecipeCategory.Ammo)]
    [InlineData(ItemType.AmmoNonEquipable, SkillType.None, RecipeCategory.Ammo)]
    [InlineData(ItemType.Consumable, SkillType.None, RecipeCategory.Food)]
    [InlineData(ItemType.Material, SkillType.None, RecipeCategory.Materials)]
    [InlineData(ItemType.Misc, SkillType.None, RecipeCategory.Materials)]
    public void ItemsFallInTheirTab(ItemType type, SkillType skill, RecipeCategory expected)
    {
        Assert.Equal(expected, RecipeCategories.Of(type, skill));
    }
}
