using BrudvikWhiteHilt.Building.Groups;
using System.Linq;
using UnityEngine;
using Xunit;

namespace BrudvikWhiteHilt.Tests;

public class BlueprintStoreTests
{
    [Fact]
    public void WrittenBlueprintReadsBackTheSame()
    {
        Blueprint blueprint = new() { Name = "Longhouse", Creator = "Kari" };
        blueprint.Pieces.Add(new PieceSnapshot { Prefab = "wood_wall", Position = new Vector3(0f, 0f, 0f), Rotation = Quaternion.identity });
        blueprint.Pieces.Add(new PieceSnapshot
        {
            Prefab = "sign",
            Position = new Vector3(1.25f, 2f, -0.5f),
            Rotation = new Quaternion(0f, 0.7071068f, 0f, 0.7071068f),
            Text = "Welcome"
        });

        Blueprint read = BlueprintStore.ParseBlueprint(BlueprintStore.Format(blueprint).Split('\n'), "file name");

        Assert.Equal("Longhouse", read.Name);
        Assert.Equal("Kari", read.Creator);
        Assert.Equal(new[] { "wood_wall", "sign" }, read.Pieces.Select(piece => piece.Prefab));
        Assert.Equal("Welcome", read.Pieces[1].Text);
        Assert.Null(read.Pieces[0].Text);
        Assert.Equal(0.7071068f, read.Pieces[1].Rotation.y, 5);
    }

    [Theory]
    [InlineData("C:\\new folder")]
    [InlineData("Say \"hi\"")]
    [InlineData("Two\nlines")]
    [InlineData("Hei, du")]
    [InlineData("back\\slash at the end\\")]
    public void SignTextSurvivesSavingAndReading(string text)
    {
        Blueprint blueprint = new() { Name = "Sign" };
        blueprint.Pieces.Add(new PieceSnapshot { Prefab = "sign", Rotation = Quaternion.identity, Text = text });

        Blueprint read = BlueprintStore.ParseBlueprint(BlueprintStore.Format(blueprint).Split('\n'), "Sign");

        Assert.Equal(text, read.Pieces.Single().Text);
    }

    [Fact]
    public void SemicolonsAreDroppedFromSignText()
    {
        Blueprint blueprint = new() { Name = "Sign" };
        blueprint.Pieces.Add(new PieceSnapshot { Prefab = "sign", Rotation = Quaternion.identity, Text = "a;b" });

        Blueprint read = BlueprintStore.ParseBlueprint(BlueprintStore.Format(blueprint).Split('\n'), "Sign");

        Assert.Equal("ab", read.Pieces.Single().Text);
    }

    [Fact]
    public void OldFilesWithoutSectionsAndWithDecimalCommasAreRead()
    {
        string[] lines =
        {
            "wood_floor;Building;1,5;0;2;0;0;0;1",
            "wood_floor(Clone);Building;-1,5;0;-2;0;0;0;1"
        };

        Blueprint read = BlueprintStore.ParseBlueprint(lines, "old");

        Assert.Equal("old", read.Name);
        Assert.Equal(new[] { "wood_floor", "wood_floor" }, read.Pieces.Select(piece => piece.Prefab));
        Assert.Equal(1.5f, read.Pieces[0].Position.x, 5);
        Assert.Equal(-1.5f, read.Pieces[1].Position.x, 5);
    }

    [Fact]
    public void OnlyThePiecesSectionIsRead()
    {
        string[] lines =
        {
            "#Name:Hut",
            "#SnapPoints",
            "1;2;3",
            "#Pieces",
            "wood_wall;Building;0;0;0;0;0;0;1"
        };

        Blueprint read = BlueprintStore.ParseBlueprint(lines, "file");

        Assert.Equal("Hut", read.Name);
        Assert.Single(read.Pieces);
    }

    [Fact]
    public void BlueprintsAreAnchoredAtTheirBottomCentre()
    {
        string[] lines =
        {
            "#Pieces",
            "a;Building;10;5;20;0;0;0;1",
            "b;Building;14;7;24;0;0;0;1"
        };

        Blueprint read = BlueprintStore.ParseBlueprint(lines, "anchored");

        Assert.Equal(new Vector3(-2f, 0f, -2f), read.Pieces[0].Position);
        Assert.Equal(new Vector3(2f, 2f, 2f), read.Pieces[1].Position);
    }

    [Fact]
    public void VBuildLinesAreRotationThenPosition()
    {
        string[] lines =
        {
            "stone_wall_1x1 0 0 0 1 3 1 4",
            "too short line",
            "stone_wall_1x1(Clone) 0 0 0 1 5,0 1 6"
        };

        Blueprint read = BlueprintStore.ParseVBuild(lines, "vb");

        Assert.Equal(2, read.Pieces.Count);
        Assert.Equal("stone_wall_1x1", read.Pieces[1].Prefab);
        Assert.Equal(new Vector3(-1f, 0f, -1f), read.Pieces[0].Position);
        Assert.Equal(new Vector3(1f, 0f, 1f), read.Pieces[1].Position);
    }
}
