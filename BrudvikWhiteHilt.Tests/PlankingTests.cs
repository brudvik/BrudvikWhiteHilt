using System.Collections.Generic;
using System.Linq;
using BrudvikWhiteHilt.Pieces.Structure.Darkwood;
using BrudvikWhiteHilt.Pieces.Structure.Planking;
using UnityEngine;
using Xunit;

namespace BrudvikWhiteHilt.Tests;

public class PlankingTests
{
    // A box between two corners, four corners to a face, facing out, with normals and texture coordinates.
    private static MeshData Box(Vector3 min, Vector3 max)
    {
        MeshData box = new();
        Vector3 centre = (min + max) / 2f;
        foreach (Vector3 normal in new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back })
        {
            Vector3 u = Mathf.Abs(normal.y) > 0.5f ? Vector3.right : Vector3.up;
            Vector3 v = Vector3.Cross(normal, u);
            int start = box.Vertices.Count;
            foreach ((float a, float b) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) })
            {
                Vector3 direction = normal + u * a + v * b;
                box.Vertices.Add(new Vector3(direction.x > 0f ? max.x : min.x, direction.y > 0f ? max.y : min.y, direction.z > 0f ? max.z : min.z));
                box.Normals.Add(normal);
                box.Uvs.Add(new Vector2(Vector3.Dot(box.Vertices[box.Vertices.Count - 1], Vector3.right), Vector3.Dot(box.Vertices[box.Vertices.Count - 1], Vector3.forward)));
            }

            foreach (int[] triangle in new[] { new[] { 0, 1, 2 }, new[] { 0, 2, 3 } })
            {
                Vector3 facing = Vector3.Cross(box.Vertices[start + triangle[1]] - box.Vertices[start + triangle[0]], box.Vertices[start + triangle[2]] - box.Vertices[start + triangle[0]]);
                bool outward = Vector3.Dot(facing, box.Vertices[start + triangle[0]] - centre) > 0f;
                box.Triangles.AddRange(outward ? triangle.Select(i => start + i) : new[] { start + triangle[0], start + triangle[2], start + triangle[1] });
            }
        }

        return box;
    }

    // A stand-in for the vanilla floor: four planks 0.5 m wide along z, 2 m long, and a joist across under them, with a
    // slit at the ends so the planks of floors laid side by side are not joined into one part.
    private static MeshData Tile()
    {
        List<MeshData> parts = new();
        for (int i = 0; i < 4; i++)
        {
            float x = -1f + 0.5f * i;
            parts.Add(Box(new Vector3(x + 0.005f, -0.02f, -0.998f), new Vector3(x + 0.495f, 0.08f, 0.998f)));
        }

        parts.Add(Box(new Vector3(-0.998f, -0.14f, -0.1f), new Vector3(0.998f, -0.02f, 0.1f)));
        return MeshData.Combine(parts);
    }

    // A stand-in for the vanilla wall: four upright boards 0.5 m wide and 2 m high, and two battens across the back at
    // the vanilla wall's heights, with slits at the ends.
    private static MeshData WallTile()
    {
        List<MeshData> parts = new();
        for (int i = 0; i < 4; i++)
        {
            float x = -1f + 0.5f * i;
            parts.Add(Box(new Vector3(x + 0.005f, -0.998f, -0.15f), new Vector3(x + 0.495f, 0.998f, 0.15f)));
        }

        parts.Add(Box(new Vector3(-0.998f, -0.735f, -0.277f), new Vector3(0.998f, -0.555f, -0.15f)));
        parts.Add(Box(new Vector3(-0.998f, 0.664f, -0.277f), new Vector3(0.998f, 0.852f, -0.15f)));
        return MeshData.Combine(parts);
    }

    // Every edge of a closed surface is matched by edges running the other way along the same line: the same edge, or
    // the same stretch split elsewhere (a cut end is one face where the side it closes was cut in two triangles).
    private static void AssertClosed(MeshData mesh)
    {
        List<(Vector3 From, Vector3 To)> edges = new();
        for (int t = 0; t < mesh.TriangleCount; t++)
        {
            for (int k = 0; k < 3; k++)
            {
                Vector3 a = mesh.Vertices[mesh.Triangles[t * 3 + k]], b = mesh.Vertices[mesh.Triangles[t * 3 + (k + 1) % 3]];
                if ((b - a).magnitude > 1e-5f)
                {
                    edges.Add((a, b));
                }
            }
        }

        Assert.All(edges, edge => Assert.True(Covered(edges, edge), $"open edge {edge.From:F4} to {edge.To:F4}"));
    }

    // How much of an edge other edges cover running the other way along its line, measured on the longer of each pair
    // so that a short edge's direction does not decide.
    private static bool Covered(List<(Vector3 From, Vector3 To)> edges, (Vector3 From, Vector3 To) edge)
    {
        const float Tolerance = 1e-4f;
        Vector3 a = edge.From, b = edge.To;
        float length = (b - a).magnitude;
        Vector3 direction = (b - a) / length;
        float covered = 0f;
        foreach ((Vector3 c, Vector3 d) in edges)
        {
            if (Vector3.Dot(d - c, direction) >= 0f)
            {
                continue;
            }

            (Vector3 p, Vector3 q, Vector3 r, Vector3 s) = (d - c).magnitude > length ? (c, d, a, b) : (a, b, c, d);
            Vector3 line = (q - p).normalized;
            if (Vector3.Cross(r - p, line).magnitude < Tolerance && Vector3.Cross(s - p, line).magnitude < Tolerance)
            {
                covered += Mathf.Max(0f, Mathf.Min(Vector3.Dot(c - a, direction), length) - Mathf.Max(Vector3.Dot(d - a, direction), 0f));
            }
        }

        return covered >= length - Tolerance;
    }

    [Fact]
    public void TheShapesHaveTheirAreas()
    {
        Assert.Equal(2f, FloorShape.Triangle(2f).Area, 4);
        Assert.Equal(0.5f, FloorShape.Triangle(1f).Area, 4);
        Assert.Equal(1.5f, FloorShape.AngledEnd(false).Area, 4);
        Assert.Equal(1.5f, FloorShape.AngledEnd(true).Area, 4);
        Assert.Equal(2f * (1f + Mathf.Sqrt(2f)) * 4f, FloorShape.Octagon(2f).Area, 3);
    }

    [Theory]
    [InlineData(2f)]
    [InlineData(4f)]
    public void AnOctagonHasEightEqualSides(float side)
    {
        Vector2[] corners = FloorShape.Octagon(side).Corners;

        Assert.Equal(8, corners.Length);
        for (int i = 0; i < 8; i++)
        {
            Assert.Equal(side, (corners[(i + 1) % 8] - corners[i]).magnitude, 4);
        }
    }

    [Fact]
    public void LongSidesGetASnapPointInTheMiddle()
    {
        Assert.Equal(8, FloorShape.Octagon(2f).SnapPoints().Count());
        Assert.Equal(16, FloorShape.Octagon(4f).SnapPoints().Count());
        Assert.Contains(Vector3.zero, FloorShape.Triangle(2f).SnapPoints());
        Assert.Equal(4, FloorShape.Triangle(2f).SnapPoints().Count());
    }

    [Fact]
    public void ASawnPlankIsClosedAndItsEndFacesOut()
    {
        Vector3 normal = new Vector3(-1f, 0f, -1f).normalized;
        MeshData plank = Sawing.Saw(Box(new Vector3(0f, 0f, -1f), new Vector3(0.5f, 0.1f, 1f)), normal, Vector3.Dot(normal, new Vector3(0f, 0f, 0.5f)));

        Assert.NotNull(plank);
        Assert.All(plank.Vertices, v => Assert.True(Vector3.Dot(normal, v) >= Vector3.Dot(normal, new Vector3(0f, 0f, 0.5f)) - 1e-4f));
        AssertClosed(plank);
        Assert.Contains(plank.Normals, n => Vector3.Dot(n, -normal) > 0.999f);
        for (int t = 0; t < plank.TriangleCount; t++)
        {
            Vector3 a = plank.Vertices[plank.Triangles[t * 3]];
            Vector3 facing = Vector3.Cross(plank.Vertices[plank.Triangles[t * 3 + 1]] - a, plank.Vertices[plank.Triangles[t * 3 + 2]] - a);
            Assert.True(Vector3.Dot(facing, plank.Normals[plank.Triangles[t * 3]]) >= -1e-6f, $"triangle {t} faces in");
        }
    }

    [Fact]
    public void APlankAlmostInsideIsKeptWholeAndOneAlmostOutsideIsLeftOut()
    {
        MeshData plank = Box(new Vector3(-1.01f, 0f, -1f), new Vector3(-0.5f, 0.1f, 1f));

        Assert.Same(plank, Sawing.Saw(plank, Vector3.right, -1f));
        Assert.Null(Sawing.Saw(plank, Vector3.right, -0.5f));
    }

    [Theory]
    [InlineData("triangle2")]
    [InlineData("triangle1")]
    [InlineData("angled")]
    [InlineData("angledMirrored")]
    [InlineData("octagon2")]
    [InlineData("octagon4")]
    public void AFloorStaysInsideItsShapeAndEveryPlankIsClosed(string name)
    {
        FloorShape shape = name switch
        {
            "triangle2" => FloorShape.Triangle(2f),
            "triangle1" => FloorShape.Triangle(1f),
            "angled" => FloorShape.AngledEnd(false),
            "angledMirrored" => FloorShape.AngledEnd(true),
            "octagon2" => FloorShape.Octagon(2f),
            _ => FloorShape.Octagon(4f)
        };

        MeshData floor = FloorCutting.Cut(shape, Tile());

        Assert.True(floor.TriangleCount > 0);
        foreach ((Vector2 normal, float distance) in shape.Sides())
        {
            Assert.All(floor.Vertices, v => Assert.True(Vector2.Dot(normal, new Vector2(v.x, v.z)) >= distance - Sawing.Slack - 1e-4f));
        }

        // The top of the planks covers the shape but for the slits between them (2 % of the width).
        float top = 0f;
        for (int t = 0; t < floor.TriangleCount; t++)
        {
            Vector3 a = floor.Vertices[floor.Triangles[t * 3]], b = floor.Vertices[floor.Triangles[t * 3 + 1]], c = floor.Vertices[floor.Triangles[t * 3 + 2]];
            Vector3 cross = Vector3.Cross(b - a, c - a);
            if (cross.y > 0f && Mathf.Abs(a.y - 0.08f) < 1e-4f && Mathf.Abs(b.y - 0.08f) < 1e-4f && Mathf.Abs(c.y - 0.08f) < 1e-4f)
            {
                top += cross.magnitude / 2f;
            }
        }

        Assert.InRange(top, shape.Area * 0.95f, shape.Area * 1.01f);
        Assert.All(floor.Islands(), AssertClosed);
    }

    [Fact]
    public void TheColliderSlabFacesOut()
    {
        (Vector3[] vertices, int[] triangles) = FloorCutting.Slab(FloorShape.Octagon(2f).Corners, -0.03f, 0.1f);
        Vector3 centre = new(0f, 0.035f, 0f);

        Assert.Equal(16, vertices.Length);
        Assert.Equal((6 * 2 + 8 * 2) * 3, triangles.Length);
        for (int t = 0; t < triangles.Length; t += 3)
        {
            Vector3 a = vertices[triangles[t]];
            Vector3 facing = Vector3.Cross(vertices[triangles[t + 1]] - a, vertices[triangles[t + 2]] - a);
            Assert.True(Vector3.Dot(facing, a - centre) > 0f);
        }
    }

    [Fact]
    public void ASawCrossTheBoardsIsClosedToo()
    {
        MeshData board = Sawing.Saw(Box(new Vector3(0f, -1f, -0.15f), new Vector3(0.5f, 1f, 0.15f)), Vector3.down, 0.5f);

        Assert.NotNull(board);
        board.Range(1, out float min, out float max);
        Assert.Equal(-1f, min, 4);
        Assert.Equal(-0.5f, max, 4);
        AssertClosed(board);
    }

    [Theory]
    [InlineData(4, 2, 6)]
    [InlineData(4, 4, 8)]
    [InlineData(2, 2, 4)]
    public void WallsAreLaidToTheirSizeAndSnapEveryTwoMetres(int width, int height, int snaps)
    {
        MeshData wall = WallCutting.Lay(WallTile(), width, height);

        wall.Range(0, out float left, out float right);
        wall.Range(1, out float bottom, out float top);
        Assert.Equal(-width / 2f, left, 1);
        Assert.Equal(width / 2f, right, 1);
        Assert.Equal(-height / 2f, bottom, 1);
        Assert.Equal(height / 2f, top, 1);
        Assert.Equal(snaps, WallCutting.SnapPoints(width, height).Count());
        Assert.Contains(new Vector3(-width / 2f, height / 2f, 0f), WallCutting.SnapPoints(width, height));
    }

    [Fact]
    public void AWindowIsSawnThroughTheBoardsAndTheyStayClosed()
    {
        Rect opening = WallCutting.Window;
        MeshData wall = WallCutting.CutOpening(WallTile(), opening);

        Assert.All(wall.Vertices, v => Assert.False(
            v.x > opening.xMin + 1e-3f && v.x < opening.xMax - 1e-3f && v.y > opening.yMin + 1e-3f && v.y < opening.yMax - 1e-3f,
            $"{v} lies in the opening"));

        // The two outer boards whole, the two inner ones under and over the opening, and both battens.
        Assert.Equal(8, wall.Islands().Count);
        Assert.All(wall.Islands(), AssertClosed);
    }

    // The vanilla plank: a unit block with the grain along z.
    private static MeshData Plank() => Box(new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.5f, 0.5f));

    [Fact]
    public void ABoardLiesBetweenItsEnds()
    {
        MeshData board = Joinery.Board(Plank(), new Vector3(0f, 0f, 0f), new Vector3(2f, 0f, 0f), 0.2f, 0.1f);

        board.Range(0, out float x0, out float x1);
        board.Range(1, out float y0, out float y1);
        board.Range(2, out float z0, out float z1);
        Assert.Equal(0f, x0, 4);
        Assert.Equal(2f, x1, 4);
        Assert.Equal(0.2f, y1 - y0, 4);
        Assert.Equal(0.1f, z1 - z0, 4);
    }

    [Theory]
    [InlineData(2f, 2f)]
    [InlineData(2f, 1f)]
    public void ALatticeStaysInsideItsFrameAndEverySlatIsClosed(float width, float height)
    {
        MeshData lattice = Joinery.Lattice(Plank(), width, height);

        lattice.Range(0, out float left, out float right);
        lattice.Range(1, out float bottom, out float top);
        lattice.Range(2, out float back, out float front);
        Assert.Equal(-width / 2f, left, 3);
        Assert.Equal(width / 2f, right, 3);
        Assert.Equal(-height / 2f, bottom, 3);
        Assert.Equal(height / 2f, top, 3);
        Assert.True(front <= Joinery.Frame / 2f + 1e-4f && back >= -Joinery.Frame / 2f - 1e-4f);

        // The frame (its four boards touch, so they are one part here) and at least four slats each way.
        Assert.True(lattice.Islands().Count >= 1 + 2 * 4);
        Assert.All(lattice.Islands(), AssertClosed);
    }

    [Theory]
    [InlineData(0.5f, 0.12f)]
    [InlineData(1f, 0.14f)]
    [InlineData(2f, 0.16f)]
    public void ABraceIsSawnFlushWithThePostAndTheBeam(float length, float section)
    {
        MeshData brace = Joinery.Brace(Plank(), length, section);

        brace.Range(0, out float left, out float right);
        brace.Range(1, out float bottom, out float top);
        Assert.Equal(0f, left, 4);
        Assert.Equal(0f, top, 4);
        float reach = length + Joinery.BraceInset;
        Assert.True(right > reach && right < reach + section * 1.5f);
        Assert.True(bottom < -reach && bottom > -reach - section * 1.5f);
        AssertClosed(brace);
        Assert.Equal(3, Joinery.BraceSnapPoints(length).Count());

        // The top of the box's height, turned about z, lands at the brace's end on the beam.
        (Vector3 center, Vector3 size, float degrees) = Joinery.BraceBox(length, section);
        float radians = degrees * Mathf.Deg2Rad;
        Vector3 end = center + new Vector3(-Mathf.Sin(radians), Mathf.Cos(radians), 0f) * (size.y / 2f);
        Assert.Equal(reach, end.x, 3);
        Assert.Equal(0f, end.y, 3);
    }

    [Theory]
    [InlineData(1f, 1f)]
    [InlineData(1f, 2f)]
    [InlineData(2f, 1f)]
    [InlineData(2f, 2f)]
    [InlineData(4f, 1f)]
    [InlineData(4f, 2f)]
    public void ABargeboardLiesOnTheRoofLineAndIsSawnPlumb(float rise, float run)
    {
        MeshData board = Joinery.Bargeboard(Plank(), rise, run);
        Vector3 up = new Vector3(0f, 2f, rise).normalized;

        board.Range(0, out float left, out float right);
        board.Range(2, out float front, out float back);
        Assert.Equal(-Joinery.BargeThickness / 2f, left, 4);
        Assert.Equal(Joinery.BargeThickness / 2f, right, 4);
        Assert.Equal(0f, front, 4);
        Assert.Equal(run, back, 4);

        // Its lower edge on the roof line through its upper end, its upper edge a board's width over it.
        Assert.All(board.Vertices, v => Assert.InRange(Vector3.Dot(v, up), -1e-4f, Joinery.BargeWidth + 1e-4f));
        AssertClosed(board);

        // The far end of the box, turned about x, lands at the board's foot on the roof line.
        (Vector3 center, Vector3 size, float degrees) = Joinery.BargeBox(rise, run);
        float radians = degrees * Mathf.Deg2Rad;
        Vector3 foot = center + new Vector3(0f, -Mathf.Sin(radians), Mathf.Cos(radians)) * (size.z / 2f) - up * (size.y / 2f);
        Assert.Equal(-rise * run / 2f, foot.y, 3);
        Assert.Equal(run, foot.z, 3);
    }

    // A stand-in for the vanilla stair: a slab rising 1 m from its foot at z = 1 to its head at z = -1, 2 m wide.
    private static MeshData StairTile()
    {
        return Box(new Vector3(-1f, 0f, -1f), new Vector3(1f, 1f, 1f));
    }

    [Fact]
    public void AWideStairIsTwoStairsSideBySide()
    {
        MeshData stair = Joinery.WideStair(StairTile());

        stair.Range(0, out float left, out float right);
        Assert.Equal(-2f, left, 4);
        Assert.Equal(2f, right, 4);
        Assert.Equal(2 * StairTile().TriangleCount, stair.TriangleCount);
    }

    [Fact]
    public void AStairWithALandingRisesTwoMetresOverSix()
    {
        MeshData stair = Joinery.StairWithLanding(StairTile(), Plank());

        stair.Range(1, out float bottom, out float top);
        stair.Range(2, out float back, out float front);
        Assert.Equal(0f, bottom, 4);
        Assert.Equal(2f, top, 4);
        Assert.Equal(-5f, back, 4);
        Assert.Equal(1f, front, 4);

        // The landing's planks are level with the top of a vanilla floor laid at the head of the first flight.
        Assert.Contains(stair.Vertices, v => Mathf.Abs(v.y - (1f + Joinery.FloorTop)) < 1e-4f && v.z < -2.9f && v.z > -3.1f);
    }
}
