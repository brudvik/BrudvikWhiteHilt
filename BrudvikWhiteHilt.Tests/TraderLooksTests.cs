using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace BrudvikWhiteHilt.Tests;

/// <summary>
/// Checks the traders' looks (traders.json, written by build_traders.py) against what the mod copies and the biomes
/// that get trading posts.
/// </summary>
public class TraderLooksTests
{
    private static readonly string[] biomes = { "Meadows", "BlackForest", "Swamp", "Mountain", "Plains", "Mistlands", "Ashlands", "DeepNorth" };
    private static readonly HashSet<string> tunics = new() { "ArmorTunic1", "ArmorTunic2", "ArmorTunic3" };
    private static readonly HashSet<string> capes = new() { "", "CapeTrollHide", "CapeDeepNorth" };

    private static readonly List<JObject> looks = JObject.Parse(File.ReadAllText(Repository.Path("AssetSource", "Preview", "traders.json")))["traders"]!
        .Cast<JObject>().ToList();

    /// <summary>Every biome with trading posts has exactly one trader.</summary>
    [Fact]
    public void EveryBiomeHasOneTrader()
    {
        Assert.Equal(biomes.OrderBy(name => name), looks.Select(look => (string)look["biome"]!).OrderBy(name => name));
    }

    /// <summary>The clothes are the vanilla items the mod knows how to copy, and the colours have three parts.</summary>
    [Fact]
    public void TheLooksAreComplete()
    {
        foreach (JObject look in looks)
        {
            string biome = (string)look["biome"]!;
            Assert.False(string.IsNullOrWhiteSpace((string)look["name"]), biome);
            Assert.StartsWith("Hair", (string)look["hair"]!);
            Assert.Contains((string)look["tunic"]!, tunics);
            Assert.Contains((string)look["cape"]!, capes);
            Assert.Equal("ArmorTrollLeatherLegs", (string)look["trousers"]);
            foreach (string colour in new[] { "hairColour", "main", "accent", "legs", "capeColour" })
            {
                Assert.Equal(3, look[colour]!.Count());
            }

            // A woman has no beard; the network data then says so, or the game would give her a random one.
            if ((bool)look["female"]!)
            {
                Assert.Equal(string.Empty, (string)look["beard"]);
            }
        }
    }
}
