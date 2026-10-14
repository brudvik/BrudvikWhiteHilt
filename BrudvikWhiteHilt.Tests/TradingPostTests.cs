using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace BrudvikWhiteHilt.Tests;

/// <summary>
/// Checks the trading post's layout (tradingpost.json, written by build_trading_post.py) against the pieces the mod
/// makes: a misspelt prefab would only show in the game, as a post with a hole in it.
/// </summary>
public class TradingPostTests
{
    // Vanilla pieces a composite in the layout may unfold into.
    private static readonly HashSet<string> vanilla = new() { "wood_roof_45" };

    private static readonly JObject layout = JObject.Parse(File.ReadAllText(Repository.Path("AssetSource", "Preview", "tradingpost.json")));

    /// <summary>Every part names a piece the mod makes or an allowed vanilla piece.</summary>
    [Fact]
    public void EveryPartIsAPieceTheModMakes()
    {
        HashSet<string> known = new(vanilla);
        JObject defenses = JObject.Parse(File.ReadAllText(Repository.Path("AssetSource", "Preview", "defenses.json")));
        foreach (JObject piece in defenses["pieces"]!.Cast<JObject>().Where(piece => piece["base"] != null))
        {
            known.Add($"piece_whitehilt_{piece["name"]}");
        }

        JObject decor = JObject.Parse(File.ReadAllText(Repository.Path("AssetSource", "Decor", "decor.json")));
        foreach (JObject entry in decor["pieces"]!.Cast<JObject>())
        {
            known.Add($"piece_whitehilt_decor_{entry["id"]}");
        }

        List<string> unknown = Parts().Select(part => (string)part["prefab"]!).Where(name => !known.Contains(name)).Distinct().ToList();
        Assert.True(unknown.Count == 0, "Unknown prefabs: " + string.Join(", ", unknown));
    }

    /// <summary>The pieces only a post has are in it: the gate and the flag pole, and the pile is in the building layout.</summary>
    [Fact]
    public void ThePostHasItsOwnPieces()
    {
        List<string> names = Parts().Select(part => (string)part["prefab"]!).ToList();
        Assert.Contains("piece_whitehilt_handelsport", names);
        Assert.Contains("piece_whitehilt_flaggstang", names);
        Assert.Contains("piece_whitehilt_bryggehode", names);
        Assert.True(layout["piles"]!.Any(), "No pile places");
    }

    /// <summary>The dome covers every part and the jetty head, so nothing of the post stands outside it.</summary>
    [Fact]
    public void TheDomeCoversThePost()
    {
        float[] centre = layout["dome"]!["centre"]!.Select(value => (float)value).ToArray();
        float radius = (float)layout["dome"]!["radius"]!;
        List<string> outside = Parts()
            .Where(part =>
            {
                float[] p = part["position"]!.Select(value => (float)value).ToArray();
                float dx = p[0] - centre[0], dz = p[2] - centre[2];
                return dx * dx + dz * dz > radius * radius;
            })
            .Select(part => (string)part["prefab"]!)
            .ToList();
        Assert.True(outside.Count == 0, "Outside the dome: " + string.Join(", ", outside));
    }

    private static IEnumerable<JObject> Parts() => layout["parts"]!.Cast<JObject>();
}
