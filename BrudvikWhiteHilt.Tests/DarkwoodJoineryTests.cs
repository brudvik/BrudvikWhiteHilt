using System.Linq;
using BrudvikWhiteHilt.Pieces.Structure.Darkwood;
using UnityEngine;
using Xunit;

namespace BrudvikWhiteHilt.Tests;

public class DarkwoodJoineryTests
{
    // A 1 x 1 square in the xy plane facing -z, as two triangles, with normals and texture coordinates.
    private static MeshData Square(float x = 0f)
    {
        MeshData square = new();
        square.Vertices.AddRange(new[] { new Vector3(x, 0f, 0f), new Vector3(x + 1f, 0f, 0f), new Vector3(x + 1f, 1f, 0f), new Vector3(x, 1f, 0f) });
        square.Normals.AddRange(Enumerable.Repeat(Vector3.back, 4));
        square.Uvs.AddRange(new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) });
        square.Triangles.AddRange(new[] { 0, 2, 1, 0, 3, 2 });
        return square;
    }

    private static float Area(MeshData mesh)
    {
        float area = 0f;
        for (int t = 0; t < mesh.TriangleCount; t++)
        {
            Vector3 a = mesh.Vertices[mesh.Triangles[t * 3]], b = mesh.Vertices[mesh.Triangles[t * 3 + 1]], c = mesh.Vertices[mesh.Triangles[t * 3 + 2]];
            area += Vector3.Cross(b - a, c - a).magnitude / 2f;
        }

        return area;
    }

    // The side a triangle faces by its winding (clockwise seen from the front, as Unity draws it).
    private static Vector3 Facing(MeshData mesh, int triangle)
    {
        Vector3 a = mesh.Vertices[mesh.Triangles[triangle * 3]], b = mesh.Vertices[mesh.Triangles[triangle * 3 + 1]], c = mesh.Vertices[mesh.Triangles[triangle * 3 + 2]];
        return Vector3.Cross(b - a, c - a).normalized;
    }

    [Fact]
    public void ClippingKeepsOneSideAndSplitsTheTrianglesAcross()
    {
        MeshData half = Square().Clip(Vector3.right, 0.25f);

        Assert.All(half.Vertices, vertex => Assert.True(vertex.x >= 0.25f - 1e-5f));
        Assert.Equal(0.75f, Area(half), 4);
        Assert.Contains(half.Uvs, uv => Mathf.Abs(uv.x - 0.25f) < 1e-5f);
    }

    [Fact]
    public void ABandIsWhatLiesBetweenTwoPlanes()
    {
        MeshData band = Square().Band(1, 0.2f, 0.7f);

        band.Range(1, out float min, out float max);
        Assert.Equal(0.2f, min, 4);
        Assert.Equal(0.7f, max, 4);
        Assert.Equal(0.5f, Area(band), 4);
    }

    [Fact]
    public void MirroringKeepsTheFrontOutside()
    {
        MeshData square = Square();
        MeshData mirrored = square.MirroredX();

        Assert.Equal(Facing(square, 0), Facing(mirrored, 0));
        Assert.Equal(Vector3.back, mirrored.Normals[0]);
        Assert.Equal(0f, mirrored.Vertices.Max(vertex => vertex.x), 4);
        Assert.Equal(-1f, mirrored.Vertices.Min(vertex => vertex.x), 4);
    }

    [Fact]
    public void StretchingMovesAStretchOntoAnother()
    {
        MeshData stretched = Square().Remapped(1, 0f, 1f, 2f, 4f);

        stretched.Range(1, out float min, out float max);
        Assert.Equal(2f, min, 4);
        Assert.Equal(4f, max, 4);
        Assert.Equal(Vector3.back, stretched.Normals[0]);
    }

    [Fact]
    public void TurningAStretchUpsideDownKeepsTheFrontOutside()
    {
        MeshData square = Square();
        MeshData flipped = square.Remapped(1, 0f, 1f, 1f, 0f);

        Assert.Equal(Facing(square, 0), Facing(flipped, 0));
    }

    [Fact]
    public void LoosePartsAreSeparateIslands()
    {
        MeshData two = MeshData.Combine(new[] { Square(), Square(3f) });

        Assert.Equal(2, two.Islands().Count);
        Assert.All(two.Islands(), island => Assert.Equal(2, island.TriangleCount));
    }

    [Theory]
    [InlineData(2.76f, 3)]
    [InlineData(3.76f, 3)]
    [InlineData(1.76f, 2)]
    [InlineData(0.76f, 1)]
    public void ALeafIsLaidOutWithoutGapsAndWithEqualPanels(float height, int count)
    {
        LeafPlan plan = GateJoinery.PlanLeaf(height, count);

        Assert.Equal(0f, plan.Segments[0].From, 4);
        Assert.Equal(height, plan.Segments.Last().To, 4);
        for (int i = 1; i < plan.Segments.Count; i++)
        {
            Assert.Equal(plan.Segments[i - 1].To, plan.Segments[i].From, 4);
        }

        Assert.Equal(count, plan.Segments.Count(segment => segment.IsPanel));
        Assert.All(plan.Segments.Where(segment => segment.IsPanel), panel => Assert.Equal(plan.Panel, panel.To - panel.From, 4));
        Assert.All(plan.Segments.Where(segment => !segment.IsPanel), rail => Assert.Equal(rail.Source.y - rail.Source.x, rail.To - rail.From, 4));
        Assert.True(plan.Segments.Last().Flipped);
    }

    [Fact]
    public void AGableLeafEndsInAPanelForTheBeamToClose()
    {
        LeafPlan plan = GateJoinery.PlanLeaf(2f, 2, topRail: false);

        Assert.True(plan.Segments.Last().IsPanel);
        Assert.Equal(2f, plan.Segments.Last().To, 4);
    }

    [Theory]
    [InlineData(3f)]
    [InlineData(4f)]
    public void ADoorHasALeafUnderItsLintelAndSnapsEveryMetre(float height)
    {
        JoineryModel door = GateJoinery.Build(JoineryKind.Door, height, null);

        JoineryLeaf leaf = Assert.Single(door.Leaves);
        Assert.False(leaf.Mirrored);
        Assert.Equal(height - GateJoinery.Lintel, leaf.BoxSize.y, 4);
        Assert.Equal(2 * ((int)height + 1), door.SnapPoints.Count);
        Assert.Contains(door.Boxes, box => Mathf.Abs(box.Center.y + box.Size.y / 2f - height) < 1e-4f);
    }

    [Fact]
    public void TheDoubleGateHangsItsLeavesMirroredFromBothSides()
    {
        JoineryModel gate = GateJoinery.Build(JoineryKind.DoubleGate, 4f, null);

        Assert.Equal(2, gate.Leaves.Count);
        JoineryLeaf right = gate.Leaves.Single(leaf => !leaf.Mirrored);
        JoineryLeaf left = gate.Leaves.Single(leaf => leaf.Mirrored);
        Assert.Equal(right.Pivot.x, -left.Pivot.x, 4);
        Assert.Equal(right.BoxCenter.x, -left.BoxCenter.x, 4);
        Assert.True(right.Pivot.x > 1f && right.Pivot.x < 2f);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(2f)]
    [InlineData(4f)]
    public void AGableRisesFromItsLeftFootToTheTopOfItsPost(float rise)
    {
        JoineryModel gable = GateJoinery.Build(JoineryKind.Gable, rise, null);

        Assert.Equal(new[] { new Vector2(-1f, 0f), new Vector2(1f, 0f), new Vector2(1f, rise) }, gable.Prism);
        Assert.Contains(new Vector3(1f, rise, 0f), gable.SnapPoints);
        Assert.Empty(gable.Leaves);
    }
}
