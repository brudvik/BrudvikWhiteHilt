using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.School;
using System.Linq;
using Xunit;

namespace BrudvikWhiteHilt.Tests;

public class SchoolTests
{
    [Theory]
    [InlineData(60f, 0.5f, 30)]
    [InlineData(61f, 0.5f, 30)]
    [InlineData(100f, 0.5f, 50)]
    [InlineData(1f, 0.5f, 0)]
    public void TargetIsTheShareOfTheBestLevelRoundedDown(float best, float share, int expected)
    {
        Assert.Equal(expected, SchoolRules.StudyTarget(best, share));
    }

    [Fact]
    public void StudyingNeverPassesTheTarget()
    {
        Assert.True(SchoolRules.CanStudy(29.9f, 30));
        Assert.False(SchoolRules.CanStudy(30f, 30));
        Assert.False(SchoolRules.CanStudy(45f, 30));
    }

    [Fact]
    public void TheSkillFurthestBehindIsStudiedFirst()
    {
        int? picked = SchoolRules.PickSkill(new[] { (1, 20f, 25), (2, 5f, 30), (3, 40f, 30) });

        Assert.Equal(2, picked);
        Assert.Null(SchoolRules.PickSkill(new[] { (1, 30f, 30) }));
    }

    [Fact]
    public void LibraryReadsBackTheSame()
    {
        SchoolLibrary library = new();
        library.Writers.Add(new SchoolLibrary.Writer("Kari", 12));
        library.Skills[1] = new SchoolLibrary.SkillRecord(42.5f, "Kari");
        library.Materials.Add("$item_wood");
        library.Stations["$piece_workbench"] = 3;
        library.Texts["$tutorial_x"] = "$tutorial_x_text";

        SchoolLibrary read = SchoolLibrary.FromBytes(library.ToBytes());

        Assert.Equal("Kari", read.Writers.Single().Name);
        Assert.Equal(12, read.Writers.Single().Day);
        Assert.Equal(42.5f, read.Skills[1].Level);
        Assert.Contains("$item_wood", read.Materials);
        Assert.Equal(3, read.Stations["$piece_workbench"]);
        Assert.Equal("$tutorial_x_text", read.Texts["$tutorial_x"]);
    }

    [Fact]
    public void BrokenBytesGiveAnEmptyLibrary()
    {
        SchoolLibrary read = SchoolLibrary.FromBytes(new byte[] { 1, 2, 3, 4 });

        Assert.Empty(read.Writers);
        Assert.Empty(read.Materials);
    }

    [Fact]
    public void MergeKeepsTheBestLevelAndJoinsTheRest()
    {
        SchoolLibrary bench = new();
        bench.Writers.Add(new SchoolLibrary.Writer("Kari", 1));
        bench.Skills[1] = new SchoolLibrary.SkillRecord(50f, "Kari");
        bench.Materials.Add("$item_wood");
        SchoolLibrary written = new();
        written.Writers.Add(new SchoolLibrary.Writer("Ola", 2));
        written.Skills[1] = new SchoolLibrary.SkillRecord(30f, "Ola");
        written.Skills[2] = new SchoolLibrary.SkillRecord(10f, "Ola");
        written.Materials.Add("$item_stone");

        bench.Merge(written, 50);

        Assert.Equal("Kari", bench.Skills[1].Teacher);
        Assert.Equal("Ola", bench.Skills[2].Teacher);
        Assert.Equal(2, bench.Materials.Count);
        Assert.Equal(new[] { "Kari", "Ola" }, bench.Writers.Select(writer => writer.Name));
    }

    [Fact]
    public void WritingAgainMovesTheWriterLastAndOldWritersGo()
    {
        SchoolLibrary bench = new();
        bench.Writers.Add(new SchoolLibrary.Writer("Kari", 1));
        bench.Writers.Add(new SchoolLibrary.Writer("Ola", 2));
        SchoolLibrary again = new();
        again.Writers.Add(new SchoolLibrary.Writer("Kari", 3));

        bench.Merge(again, 1);

        Assert.Equal("Kari", bench.Writers.Single().Name);
        Assert.Equal(3, bench.Writers.Single().Day);
    }

    [Fact]
    public void UnpackingStopsAtTheLimit()
    {
        byte[] packed = Utils.Compress(new byte[100000]);

        Assert.Null(BoundedGzip.Decompress(packed, 1000));
        Assert.Equal(100000, BoundedGzip.Decompress(packed, 200000).Length);
        Assert.Null(BoundedGzip.Decompress(new byte[] { 9, 9, 9 }, 1000));
    }
}
