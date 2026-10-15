using BrudvikWhiteHilt.Helpers;
using System.IO;
using System.Linq;
using Xunit;

namespace BrudvikWhiteHilt.Tests;

/// <summary>
/// The names of the recorded voice lines, which generate_audio.py and the mod must agree on.
/// </summary>
public class WhiteHiltSoundTests
{
    /// <summary>A line's name is its token without the mod's prefix; other tokens have none.</summary>
    [Fact]
    public void LineNamesComeFromTheTokens()
    {
        Assert.Equal("greet2", WhiteHiltSound.LineName("$whitehilt_trader_greet2"));
        Assert.Equal("hunter_talk1", WhiteHiltSound.LineName("$whitehilt_trader_hunter_talk1"));
        Assert.Equal("skipper_depart1", WhiteHiltSound.LineName("$whitehilt_skipper_depart1"));
        Assert.Null(WhiteHiltSound.LineName("$msg_sold"));
        Assert.Null(WhiteHiltSound.LineName(null));
        Assert.Equal("voice_mistlands_no_terror_talk2", WhiteHiltSound.ClipName("mistlands", "no", "terror_talk2"));
    }

    /// <summary>Every recorded line in AssetSource/Audio/Voices is named as the mod looks for it.</summary>
    [Fact]
    public void RecordedLinesAreNamedForTheMod()
    {
        string folder = Repository.Path("AssetSource", "Audio", "Voices");
        if (!Directory.Exists(folder))
        {
            return;
        }

        string[] speakers = { "meadows", "blackforest", "swamp", "mountain", "plains", "mistlands", "ashlands", "deepnorth", "hunter", "skipper", "raider1", "raider2", "raiderchief", "whitehilt", "husvette", "munin" };
        foreach (string name in Directory.GetFiles(folder, "*.mp3").Select(Path.GetFileNameWithoutExtension))
        {
            string[] parts = name.Split('_');
            Assert.True(parts.Length >= 4 && parts[0] == "voice" && speakers.Contains(parts[1]) && (parts[2] == "en" || parts[2] == "no"), name);
        }
    }

    /// <summary>Every Black Beast and every plague has its idle, alert, attack, hit and death sounds, three takes of each.</summary>
    [Fact]
    public void BeastsAndPlaguesHaveTheirSounds()
    {
        string folder = Repository.Path("AssetSource", "Audio", "Creatures");
        string[] prefixes = Difficulty.Beasts.BeastDefinition.All.Select(beast => beast.Key.ToLowerInvariant())
            .Concat(TradingPosts.TerrorDefinition.All.Select(terror => terror.Key.ToLowerInvariant()))
            .ToArray();
        foreach (string prefix in prefixes)
        {
            foreach (string action in new[] { "idle", "alert", "attack", "hit", "death" })
            {
                foreach (string variant in new[] { string.Empty, "_2", "_3" })
                {
                    Assert.True(File.Exists(Path.Combine(folder, $"{prefix}{action}{variant}.mp3")), $"{prefix}{action}{variant}");
                }
            }
        }
    }

    /// <summary>Every god's gift has its own drinking sound.</summary>
    [Fact]
    public void EveryGiftHasItsSound()
    {
        string effects = Repository.Path("AssetSource", "Audio", "Effects");
        foreach (string gift in Directory.GetDirectories(Repository.Path("BrudvikWhiteHilt", "Items", "Potions")).Select(Path.GetFileName))
        {
            Assert.True(File.Exists(Path.Combine(effects, $"drink_{gift.ToLowerInvariant()}.mp3")), gift);
        }
    }

    /// <summary>The White Hilt warns at a quarter, a half and three quarters of the pressure.</summary>
    [Fact]
    public void PressureComesInQuarters()
    {
        Assert.Equal(0, Difficulty.DifficultyState.PressureBand(0.1f));
        Assert.Equal(1, Difficulty.DifficultyState.PressureBand(0.25f));
        Assert.Equal(2, Difficulty.DifficultyState.PressureBand(0.6f));
        Assert.Equal(3, Difficulty.DifficultyState.PressureBand(1f));
    }
}
