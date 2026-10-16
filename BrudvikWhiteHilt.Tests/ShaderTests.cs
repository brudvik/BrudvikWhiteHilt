using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace BrudvikWhiteHilt.Tests;

/// <summary>
/// Shaders the mod looks up by name must be in the game's build, or the material is made from nothing and throws.
/// </summary>
public class ShaderTests
{
    // Unity's built-in shaders the game is known to ship; "Unlit/Color" and "Standard" are not among them.
    private static readonly string[] shipped = { "Sprites/Default", "UI/Default" };

    /// <summary>Every Shader.Find in the mod names a shader the game has.</summary>
    [Fact]
    public void OnlyShippedShadersAreLookedUp()
    {
        string source = Repository.Path("BrudvikWhiteHilt");
        foreach (string file in Directory.GetFiles(source, "*.cs", SearchOption.AllDirectories)
                     .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")))
        {
            foreach (Match match in Regex.Matches(File.ReadAllText(file), @"Shader\.Find\(""([^""]+)""\)"))
            {
                Assert.True(shipped.Contains(match.Groups[1].Value), $"{Path.GetFileName(file)}: {match.Groups[1].Value}");
            }
        }
    }
}
