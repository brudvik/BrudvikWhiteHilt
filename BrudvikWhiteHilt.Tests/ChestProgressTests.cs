using BrudvikWhiteHilt.Chests.Helpers;
using System;
using System.IO;
using Xunit;

namespace BrudvikWhiteHilt.Tests;

/// <summary>
/// Checks the move of the chests' world progress out of BrudvikStackedChest's folder: each world's record moves or is
/// merged, the stale stock files and the census go, and the old folder goes once nothing else is left in it.
/// </summary>
public sealed class ChestProgressTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "whitehilt_chests_" + Guid.NewGuid().ToString("N"));

    /// <summary>Makes an old folder with a census in it.</summary>
    public ChestProgressTests()
    {
        Directory.CreateDirectory(Path.Combine(Legacy, "census"));
        File.WriteAllText(Path.Combine(Legacy, "census", "World_1_20261015.tsv"), "chest\tprefab\titem\tquality\tamount");
    }

    private string Legacy => Path.Combine(root, "BrudvikStackedChest");

    private string Target => Path.Combine(root, "BrudvikWhiteHilt", "chests");

    /// <summary>The record moves, the stock file and the census go, and so does the old folder.</summary>
    [Fact]
    public void TheRecordMovesAndTheOldFolderGoes()
    {
        File.WriteAllLines(Path.Combine(Legacy, "World_1.txt"), new[] { "u:Wood", "d:$item_stone" });
        File.WriteAllLines(Path.Combine(Legacy, "World_1_stock.txt"), new[] { "1:2\tWood\t5" });

        Assert.True(WorldProgress.MoveLegacyFiles(Legacy, Target));

        Assert.False(Directory.Exists(Legacy));
        Assert.Equal(new[] { "u:Wood", "d:$item_stone" }, File.ReadAllLines(Path.Combine(Target, "World_1.txt")));
        Assert.False(File.Exists(Path.Combine(Target, "World_1_stock.txt")));
    }

    /// <summary>A record already in the new folder keeps its lines and gets the old one's it lacks.</summary>
    [Fact]
    public void ARecordAlreadyThereIsMerged()
    {
        Directory.CreateDirectory(Target);
        File.WriteAllLines(Path.Combine(Target, "World_1.txt"), new[] { "u:Wood", "u:Stone" });
        File.WriteAllLines(Path.Combine(Legacy, "World_1.txt"), new[] { "u:Wood", "u:Flint", "" });

        WorldProgress.MoveLegacyFiles(Legacy, Target);

        Assert.Equal(new[] { "u:Wood", "u:Stone", "u:Flint" }, File.ReadAllLines(Path.Combine(Target, "World_1.txt")));
    }

    /// <summary>Files the mod does not know stay, and with them the old folder.</summary>
    [Fact]
    public void UnknownFilesStay()
    {
        File.WriteAllText(Path.Combine(Legacy, "notes.md"), "mine");

        Assert.False(WorldProgress.MoveLegacyFiles(Legacy, Target));

        Assert.True(File.Exists(Path.Combine(Legacy, "notes.md")));
        Assert.False(Directory.Exists(Path.Combine(Legacy, "census")));
    }

    /// <summary>Removes the test's folders.</summary>
    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, true);
        }
    }
}
