using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace BrudvikWhiteHilt.Tests;

/// <summary>
/// Checks the embedded layout (defenses.json) against what the mod looks up in it: the groups its drivers move, the
/// ladders, hit areas and colliders. A typo there would otherwise show only in the game, as a piece that does not move.
/// </summary>
public class LayoutTests
{
    // The groups the mod's code finds by name, for every piece whose layout name matches.
    private static readonly (string Pattern, string[] Groups)[] requiredGroups =
    {
        ("^skanseport$", new[] { "leaf_left", "leaf_right" }),
        ("^steinport(_\\w+)?$", new[] { "leaf_left", "leaf_right", "portcullis" }),
        ("^(vindebro|steinvindebro(_\\w+)?)$", new[] { "deck" }),
        ("^porttau$", new[] { "pull" }),
        ("^vindehus$", new[] { "wheel", "crank", "lever" }),
        ("^oljegryte$", new[] { "pot" }),
        ("^(alarmklokke|stopul)$", new[] { "bell" }),
        ("^havnekran$", new[] { "jib", "fall", "hook" }),
        ("^(plankedor|dorportal)$", new[] { "leaf" }),
        ("^lavedor$", new[] { "leaf_left", "leaf_right" }),
        ("^(laftvegg(_forskutt)?|stavvegg)_vindu$", new[] { "leaf_left", "leaf_right" }),
        ("^laftvegg(_forskutt)?_smal_vindu$", new[] { "leaf_right" }),
        ("^(hoy_plankedor|stavkirkeportal)$", new[] { "leaf" }),
        ("^(dobbeldor|haldor)$", new[] { "leaf_left", "leaf_right" }),
        ("^grind$", new[] { "leaf" }),
        ("^dobbelgrind$", new[] { "leaf_left", "leaf_right" }),
        ("^bronnvipp$", new[] { "sweep", "bucket" }),
        ("^bekkekvern$", new[] { "wheel" }),
        ("^utskaret_portal$", new[] { "leaf" }),
        ("^lem$", new[] { "lid" }),
    };

    private static readonly Dictionary<string, JObject> pieces = Load();

    [Fact]
    public void EveryMovingPieceHasTheGroupsItsCodeTurns()
    {
        List<string> missing = new();
        foreach ((string pattern, string[] groups) in requiredGroups)
        {
            List<string> names = pieces.Keys.Where(name => Regex.IsMatch(name, pattern)).ToList();
            Assert.True(names.Count > 0, $"No piece matches {pattern}");
            foreach (string name in names)
            {
                HashSet<string> have = Groups(name);
                missing.AddRange(groups.Where(group => !have.Contains(group)).Select(group => $"{name}: {group}"));
            }
        }

        Assert.True(missing.Count == 0, "Missing groups:\n" + string.Join("\n", missing));
    }

    [Fact]
    public void PartsAndCollidersOnlyUseGroupsThatExist()
    {
        List<string> wrong = new();
        foreach (string name in pieces.Keys)
        {
            HashSet<string> have = Groups(name);
            foreach (JToken item in Items(name, "parts").Concat(Items(name, "colliders")))
            {
                string group = (string)item["group"];
                if (group != null && !have.Contains(group))
                {
                    wrong.Add($"{name}: {group}");
                }
            }
        }

        Assert.True(wrong.Count == 0, "Unknown groups:\n" + string.Join("\n", wrong.Distinct()));
    }

    [Fact]
    public void EveryBuildablePieceHasABaseAndColliders()
    {
        List<string> wrong = pieces.Keys
            .Where(name => Resolve(name)["base"] != null && !Items(name, "colliders").Any())
            .ToList();

        Assert.True(wrong.Count == 0, "No colliders:\n" + string.Join("\n", wrong));
    }

    [Fact]
    public void LaddersHaveAStopForEveryFloor()
    {
        foreach (string name in pieces.Keys)
        {
            foreach (JToken ladder in Items(name, "ladders"))
            {
                float[] stops = ladder["stops"].ToObject<float[]>();
                Assert.True(stops.Length >= 6 && stops.Length % 3 == 0, $"{name}: a ladder needs two or more stops of three numbers");
                Assert.Equal(3, ladder["center"].Count());
                Assert.Equal(3, ladder["size"].Count());
            }
        }
    }

    [Fact]
    public void PiecesThatHurtHaveAHitArea()
    {
        foreach (string name in pieces.Keys.Where(name => (Resolve(name)["keep"] as JArray)?.Any(keep => (string)keep == "HIT AREA") == true))
        {
            Assert.NotNull(Resolve(name)["hitArea"]);
        }
    }

    [Fact]
    public void PieceReferencesLeadToPieces()
    {
        List<string> missing = new();
        foreach (string name in pieces.Keys)
        {
            missing.AddRange(Items(name, "parts").Select(part => (string)part["piece"]).Where(other => other != null && !pieces.ContainsKey(other)));
        }

        Assert.True(missing.Count == 0, "Unknown pieces:\n" + string.Join("\n", missing.Distinct()));
    }

    [Theory]
    [InlineData("_marmor")]
    [InlineData("_grausten")]
    public void EveryStonePieceComesInEveryStone(string suffix)
    {
        string source = File.ReadAllText(Repository.Path("BrudvikWhiteHilt", "Pieces", "Defenses", "StoneDefensePieces.cs"));
        List<string> stone = Regex.Matches(source, "LayoutName => \"(\\w+)\" \\+ StoneDefense\\.Suffix").Cast<Match>().Select(match => match.Groups[1].Value).ToList();

        Assert.NotEmpty(stone);
        Assert.All(stone, name => Assert.True(pieces.ContainsKey(name + suffix), $"{name + suffix} is not in the layout"));
    }

    [Fact]
    public void VariantsAreBuiltOnPiecesThatExist()
    {
        foreach (KeyValuePair<string, JObject> piece in pieces.Where(piece => piece.Value["of"] != null))
        {
            Assert.True(pieces.ContainsKey((string)piece.Value["of"]), $"{piece.Key} is built on {(string)piece.Value["of"]}, which is not in the layout");
        }
    }

    [Theory]
    [InlineData("steinport_marmor", "marble_d", false)]
    [InlineData("steinport_grausten", "Grausten_d", true)]
    public void TheModPutsVariantsTogether(string name, string texture, bool spikes)
    {
        Pieces.Defenses.DefenseLayout layout = Pieces.Defenses.DefenseLayout.Load();
        Pieces.Defenses.DefensePieceData plain = layout.Get("steinport");
        Pieces.Defenses.DefensePieceData variant = layout.Get(name);

        Assert.Null(variant.of);
        Assert.Equal(plain.colliders.Length, variant.colliders.Length);
        Assert.Equal(plain.groups.Select(group => group.name), variant.groups.Select(group => group.name));
        Assert.Equal(spikes, variant.parts.Length > plain.parts.Length);
        Assert.Contains(variant.parts, part => part.mesh == "stone_wall_1x1" && part.texture == texture);
        Assert.DoesNotContain(plain.parts, part => part.texture == texture);
    }

    // A variant ("of") takes everything it does not give itself from the piece it is built on.
    private static JObject Resolve(string name)
    {
        JObject piece = pieces[name];
        string of = (string)piece["of"];
        if (of == null)
        {
            return piece;
        }

        JObject merged = (JObject)Resolve(of).DeepClone();
        foreach (JProperty property in piece.Properties())
        {
            merged[property.Name] = property.Value;
        }

        return merged;
    }

    private static IEnumerable<JToken> Items(string name, string field)
    {
        return Resolve(name)[field] as JArray ?? new JArray();
    }

    private static HashSet<string> Groups(string name)
    {
        return new HashSet<string>(Items(name, "groups").Select(group => (string)group["name"]));
    }

    private static Dictionary<string, JObject> Load()
    {
        JObject layout = JObject.Parse(File.ReadAllText(Repository.Path("AssetSource", "Preview", "defenses.json")));
        return layout["pieces"].Cast<JObject>().ToDictionary(piece => (string)piece["name"]);
    }
}
