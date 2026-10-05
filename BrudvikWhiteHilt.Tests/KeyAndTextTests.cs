using BrudvikWhiteHilt.Chests.Helpers;
using BrudvikWhiteHilt.Guestbook;
using BrudvikWhiteHilt.Settings;
using Xunit;

namespace BrudvikWhiteHilt.Tests;

public class LevelKeyTests
{
    [Theory]
    [InlineData("Wood", 1, "Wood")]
    [InlineData("Wood", 0, "Wood")]
    [InlineData("FishPerch", 2, "FishPerch@2")]
    [InlineData("FishPerch", 5, "FishPerch@5")]
    public void LevelKeysReadBack(string prefab, int quality, string key)
    {
        Assert.Equal(key, ChestSupply.GetLevelKey(prefab, quality));
        Assert.Equal(prefab, ChestSupply.SplitLevelKey(key, out int level));
        Assert.Equal(quality <= 1 ? 1 : quality, level);
    }

    [Theory]
    [InlineData("@2")]
    [InlineData("Fish@")]
    [InlineData("Fish@x")]
    [InlineData("Fish@1")]
    [InlineData("Fish@-2")]
    public void KeysThatAreNotLevelsStayWhole(string key)
    {
        Assert.Equal(key, ChestSupply.SplitLevelKey(key, out int level));
        Assert.Equal(1, level);
    }
}

public class GuestbookEntryTests
{
    [Fact]
    public void EntriesReadBackTheSame()
    {
        GuestbookStand.Entry entry = new(12, 615, GuestEntryKind.Built, "Kari", "$piece_woodwall", 4);

        Assert.True(GuestbookStand.Entry.TryParse(entry.Serialize(), out GuestbookStand.Entry read));
        Assert.Equal(12, read.Day);
        Assert.Equal(615, read.Minute);
        Assert.Equal(GuestEntryKind.Built, read.Kind);
        Assert.Equal("Kari", read.Who);
        Assert.Equal("$piece_woodwall", read.What);
        Assert.Equal(4, read.Count);
    }

    [Fact]
    public void ANameCannotBreakTheLine()
    {
        GuestbookStand.Entry entry = new(1, 0, GuestEntryKind.Visit, "Ka|ri\nB", null, 1);

        Assert.True(GuestbookStand.Entry.TryParse(entry.Serialize(), out GuestbookStand.Entry read));
        Assert.Equal("Ka/ri B", read.Who);
        Assert.Equal(string.Empty, read.What);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1|2|Built|Kari|x")]
    [InlineData("a|2|Built|Kari|x|1")]
    [InlineData("1|2|Danced|Kari|x|1")]
    public void BrokenLinesAreRejected(string line)
    {
        Assert.False(GuestbookStand.Entry.TryParse(line, out _));
    }
}

public class ConfigTextTests
{
    [Theory]
    [InlineData("Chests.Collection", "Chests")]
    [InlineData("Quartermaster", "Quartermaster")]
    public void FamilyIsThePartBeforeTheFirstDot(string section, string family)
    {
        Assert.Equal(family, ConfigText.Family(section));
    }

    [Theory]
    [InlineData("FishingNetMinutes", "Fishing net minutes")]
    [InlineData("ShowRangeRing", "Show range ring")]
    [InlineData("KeepAtLeast", "Keep at least")]
    [InlineData("Chests.Collection", "Chests collection")]
    [InlineData("Tier2Cost", "Tier 2 cost")]
    public void KeysAreMadeReadable(string key, string text)
    {
        Assert.Equal(text, ConfigText.Humanize(key));
    }
}
