using BrudvikWhiteHilt.Decor;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace BrudvikWhiteHilt.Tests;

public class DecorTests
{
    private static readonly string CatalogPath = Repository.Path("AssetSource", "Decor", "decor.json");

    private static List<DecorEntry> Catalog() => DecorEntry.Parse(File.ReadAllText(CatalogPath));

    [Fact]
    public void IdsAreUniqueAndFitForPrefabNames()
    {
        List<string> ids = Catalog().Select(entry => entry.Id).ToList();

        Assert.NotEmpty(ids);
        Assert.Equal(ids.Count, ids.Distinct().Count());
        Assert.All(ids, id => Assert.Matches("^[a-z0-9_]+$", id));
    }

    [Fact]
    public void TheTabsComeInTheHammersOrder()
    {
        // Jotunn creates a tab when its first piece is added, so the file's order is the menu's order.
        List<string> firstSeen = Catalog().Select(entry => entry.Category).Distinct().ToList();

        Assert.Equal(DecorHammer.Categories, firstSeen);
    }

    [Fact]
    public void EveryTabAndPieceHasANorwegianText()
    {
        HashSet<string> norwegian = new(JObject.Parse(File.ReadAllText(Repository.Path("BrudvikWhiteHilt", "Translations", "Norwegian.json")))
            .Properties().Select(property => property.Name));
        IEnumerable<string> keys = Catalog().SelectMany(entry => new[] { entry.PrefabName, entry.PrefabName + "_description" })
            .Concat(DecorHammer.Categories.Select(category => "jotunn_cat_" + category.ToLowerInvariant()));

        List<string> missing = keys.Where(key => !norwegian.Contains(key)).ToList();

        Assert.True(missing.Count == 0, "Missing in Norwegian.json:\n" + string.Join("\n", missing));
    }

    [Fact]
    public void EveryModelIsPreparedAndEveryPreparedModelIsUsed()
    {
        string folder = Repository.Path("AssetSource", "Decor", "Models");
        HashSet<string> models = new(Directory.GetFiles(folder, "*.glb").Select(Path.GetFileNameWithoutExtension));
        List<DecorEntry> withModels = Catalog().Where(entry => entry.Look == DecorLook.Model && entry.Size == null).ToList();

        Assert.All(withModels, entry => Assert.True(models.Contains(entry.Id), $"{entry.Id}: run AssetSource/Decor/prepare_decor.py"));
        Assert.All(withModels, entry => Assert.True(entry.Height > 0f, $"{entry.Id} has no height"));
        Assert.Empty(models.Except(withModels.Select(entry => entry.Id)));
    }

    [Fact]
    public void ModelsDownloadedByHandAreCredited()
    {
        // Poly Haven's models are CC0; anything else came from a site whose licence asks for attribution.
        IEnumerable<JToken> pieces = JObject.Parse(File.ReadAllText(CatalogPath))["pieces"];
        List<string> uncredited = pieces
            .Where(piece => ((string)piece["source"])?.StartsWith("file:") == true && string.IsNullOrEmpty((string)piece["credit"]))
            .Select(piece => (string)piece["id"])
            .ToList();

        Assert.Empty(uncredited);
    }

    [Fact]
    public void EveryCreditIsInTheReadme()
    {
        string readme = File.ReadAllText(Repository.Path("README.MD"));
        IEnumerable<string> authors = Catalog().Where(entry => entry.Credit != null)
            .Select(entry => Regex.Match(entry.Credit, " by (.+?) \\(").Groups[1].Value)
            .Distinct();

        Assert.All(authors, author => Assert.Contains(author, readme));
    }

    [Fact]
    public void SizeCopiesShowTheirDecorationsModelScaled()
    {
        List<DecorEntry> catalog = Catalog();
        DecorEntry barrel = catalog.Single(entry => entry.Id == "barrel_old");
        DecorEntry large = catalog.Single(entry => entry.Id == "barrel_old_large");
        DecorEntry small = catalog.Single(entry => entry.Id == "barrel_old_small");

        Assert.Equal("large", large.Size);
        Assert.Equal(barrel.MeshName, large.MeshName);
        Assert.Equal(barrel.Scale * 1.5f, large.Scale, 3);
        Assert.Equal(barrel.Scale * 0.6f, small.Scale, 3);
        Assert.True(large.Requirements.Sum(requirement => requirement.Amount) > barrel.Requirements.Sum(requirement => requirement.Amount));
        Assert.All(catalog.Where(entry => entry.Size != null), entry => Assert.Equal(0f, entry.Seat));
    }

    [Fact]
    public void CostsAreReadAsItemsAndAmounts()
    {
        DecorEntry chopping = Catalog().Single(entry => entry.Id == "chopping_block");

        Assert.Equal(new[] { "Wood", "Flint" }, chopping.Requirements.Select(requirement => requirement.Item));
        Assert.Equal(new[] { 3, 1 }, chopping.Requirements.Select(requirement => requirement.Amount));
        Assert.All(Catalog().SelectMany(entry => entry.Requirements), requirement => Assert.True(requirement.Amount > 0));
    }

    [Fact]
    public void PlantsAndClothLetPlayersThrough()
    {
        List<DecorEntry> catalog = Catalog();

        Assert.All(catalog.Where(entry => entry.Wind), entry => Assert.False(entry.Solid, entry.Id));
        Assert.True(catalog.Single(entry => entry.Id == "barrel_wine").Solid);
        Assert.False(catalog.Single(entry => entry.Id == "spoon").Solid);
    }
}
