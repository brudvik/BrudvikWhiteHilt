using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace BrudvikWhiteHilt.Tests;

/// <summary>
/// Checks that the source code and the data files it relies on fit together, so a missing translation or layout is
/// found here and not in the game.
/// </summary>
public class DataFileTests
{
    // Only whole literal keys: a key put together in code, like "whitehilt_counter_" + name, cannot be checked here.
    private static readonly Regex englishText = new("Translations\\.AddEnglish\\(\\s*\"([^\"]+)\"\\s*,", RegexOptions.Compiled);
    private static readonly Regex englishNameAndDescription = new("Translations\\.AddEnglishNameAndDescription\\(\\s*\"([^\"]+)\"\\s*,", RegexOptions.Compiled);
    private static readonly Regex jsonKey = new("^\\s*\"((?:[^\"\\\\]|\\\\.)*)\"\\s*:", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex layoutName = new("\\bLayoutName\\s*=>\\s*\"([^\"]+)\"", RegexOptions.Compiled);
    private static readonly Regex visualLayoutName = new("\\bVisualLayoutName\\s*=>\\s*\"([^\"]+)\"", RegexOptions.Compiled);

    [Fact]
    public void EveryEnglishTextHasANorwegianOne()
    {
        HashSet<string> norwegian = new(Keys(Repository.Path("BrudvikWhiteHilt", "Translations", "Norwegian.json")));
        List<string> missing = new();
        foreach (string file in SourceFiles())
        {
            string source = File.ReadAllText(file);
            foreach (Match match in englishText.Matches(source))
            {
                Add(missing, norwegian, match.Groups[1].Value, file);
            }

            foreach (Match match in englishNameAndDescription.Matches(source))
            {
                Add(missing, norwegian, match.Groups[1].Value, file);
                Add(missing, norwegian, match.Groups[1].Value + "_description", file);
            }
        }

        Assert.True(missing.Count == 0, "Missing in Norwegian.json:\n" + string.Join("\n", missing));
    }

    [Theory]
    [InlineData("BrudvikWhiteHilt", "Translations", "Norwegian.json")]
    [InlineData("BrudvikWhiteHilt", "Chests", "Translations", "Norwegian.json")]
    [InlineData("BrudvikWhiteHilt", "Chests", "Translations", "English.json")]
    public void TranslationFilesHaveEachKeyOnce(params string[] path)
    {
        List<string> twice = Keys(Repository.Path(path)).GroupBy(key => key).Where(group => group.Count() > 1).Select(group => group.Key).ToList();

        Assert.True(twice.Count == 0, "Keys given more than once (the last one wins):\n" + string.Join("\n", twice));
    }

    [Fact]
    public void EveryLaidOutPieceIsInTheLayout()
    {
        string layout = File.ReadAllText(Repository.Path("AssetSource", "Preview", "defenses.json"));
        HashSet<string> pieces = new(Regex.Matches(layout, "\"name\":\\s*\"([^\"]+)\"").Cast<Match>().Select(match => match.Groups[1].Value));
        // A piece that borrows another's look (VisualLayoutName) is built from that layout instead of its own.
        List<string> missing = SourceFiles()
            .Select(file => File.ReadAllText(file))
            .Select(source => visualLayoutName.Match(source) is { Success: true } visual ? visual : layoutName.Match(source))
            .Where(match => match.Success)
            .Select(match => match.Groups[1].Value)
            .Where(name => !pieces.Contains(name))
            .ToList();

        Assert.True(missing.Count == 0, "Not in defenses.json (run AssetSource/Preview/build_defenses.py):\n" + string.Join("\n", missing));
    }

    [Fact]
    public void TheEmbeddedLayoutIsWhatTheScriptWrites()
    {
        string script = File.ReadAllText(Repository.Path("AssetSource", "Preview", "build_defenses.py"));
        string layout = File.ReadAllText(Repository.Path("AssetSource", "Preview", "defenses.json"));
        List<string> named = Regex.Matches(script, "defence\\(\"([^\"]+)\"").Cast<Match>().Select(match => match.Groups[1].Value).ToList();

        Assert.NotEmpty(named);
        Assert.All(named, name => Assert.Contains($"\"name\": \"{name}\"", layout));
    }

    private static void Add(List<string> missing, HashSet<string> norwegian, string key, string file)
    {
        if (!norwegian.Contains(key) && !missing.Contains(key))
        {
            missing.Add($"{key}  ({Path.GetFileName(file)})");
        }
    }

    private static IEnumerable<string> SourceFiles()
    {
        return Directory.EnumerateFiles(Repository.Path("BrudvikWhiteHilt"), "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                && !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"));
    }

    // The top-level keys of a flat translation file, in order, duplicates included.
    private static List<string> Keys(string file)
    {
        return jsonKey.Matches(File.ReadAllText(file)).Cast<Match>().Select(match => match.Groups[1].Value).ToList();
    }
}
