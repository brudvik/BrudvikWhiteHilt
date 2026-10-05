using BrudvikWhiteHilt.Quartermaster;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace BrudvikWhiteHilt.Tests;

public class PackListTests
{
    [Fact]
    public void AddingMergesAndTakingAwayRemoves()
    {
        PackList list = new() { Name = "Outpost" };
        list.Add("Wood", 50);
        list.Add("Stone", 20);
        list.Add("Wood", 50);
        list.Add("Stone", -20);
        list.Add("Iron", -5);

        Assert.Equal(new[] { new KeyValuePair<string, int>("Wood", 100) }, list.Items);
    }

    [Fact]
    public void ListsReadBackTheSame()
    {
        List<PackList> lists = new()
        {
            new PackList { Name = "Outpost kit" },
            new PackList { Name = "Empty" }
        };
        lists[0].Add("Wood", 200);
        lists[0].Add("Iron", 20);

        List<PackList> read = PackListStore.Parse(PackListStore.Format(lists).Split('\n'));

        Assert.Equal(new[] { "Outpost kit", "Empty" }, read.Select(list => list.Name));
        Assert.Equal(lists[0].Items, read[0].Items);
        Assert.Empty(read[1].Items);
    }

    [Fact]
    public void BrokenLinesAndItemsBeforeTheFirstListAreSkipped()
    {
        string[] lines =
        {
            "Wood=10",
            "# Kit",
            "Stone=abc",
            "=5",
            "  Flint = 4  ",
            "Resin=0",
            "just text"
        };

        PackList read = PackListStore.Parse(lines).Single();

        Assert.Equal("Kit", read.Name);
        Assert.Equal(new[] { new KeyValuePair<string, int>("Flint", 4) }, read.Items);
    }

    [Fact]
    public void ANameCannotBreakTheFile()
    {
        List<PackList> lists = new() { new PackList { Name = "Two\nlines" } };
        lists[0].Add("Wood", 1);

        List<PackList> read = PackListStore.Parse(PackListStore.Format(lists).Split('\n'));

        Assert.Equal("Two lines", read.Single().Name);
        Assert.Single(read.Single().Items);
    }
}

public class StockWatchTests
{
    [Fact]
    public void WatchesReadBackTheSame()
    {
        List<KeyValuePair<string, int>> watches = new()
        {
            new KeyValuePair<string, int>("Iron", 50),
            new KeyValuePair<string, int>("Coal", 20)
        };

        Assert.Equal(watches, StockWatch.Parse(StockWatch.Format(watches)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Iron")]
    [InlineData("Iron=")]
    [InlineData("Iron=0")]
    [InlineData("Iron=-3")]
    [InlineData("=5")]
    public void BrokenEntriesAreSkipped(string text)
    {
        Assert.Empty(StockWatch.Parse(text));
    }

    [Fact]
    public void AnItemIsWatchedOnce()
    {
        List<KeyValuePair<string, int>> read = StockWatch.Parse("Iron=50;Coal=20;Iron=10");

        Assert.Equal(new[] { "Iron", "Coal" }, read.Select(watch => watch.Key));
        Assert.Equal(50, read[0].Value);
    }
}
