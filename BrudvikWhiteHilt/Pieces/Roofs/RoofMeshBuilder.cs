using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Roofs;

/// <summary>
/// Shape of a roof piece. Each copies the size, snap points and colliders of a vanilla wooden roof piece.
/// </summary>
public enum RoofShape
{
    /// <summary>A plain slope, like <c>wood_roof</c>.</summary>
    Slope,

    /// <summary>The ridge on top, like <c>wood_roof_top</c>.</summary>
    Ridge,

    /// <summary>The inner (valley) corner, like <c>wood_roof_icorner</c>.</summary>
    InnerCorner,

    /// <summary>The outer (hip) corner, like <c>wood_roof_ocorner</c>.</summary>
    OuterCorner,

    /// <summary>A slope with a smoke hole (ljore) and a hatch.</summary>
    SmokeHole,

    /// <summary>Where a ridge turns a corner: the ridge comes in from -x and goes on towards +z, with a hip on the outside
    /// of the turn and a valley on the inside.</summary>
    RidgeCorner,

    /// <summary>Where a ridge branches: the ridge runs along x and a branch leaves towards +z, with valleys either side of
    /// the branch.</summary>
    RidgeTee,

    /// <summary>Where two ridges cross, with a valley in each of the four corners.</summary>
    RidgeCross,

    /// <summary>The hipped end of a ridge: the ridge comes in from -x and ends in a third slope down towards +x, with a hip
    /// on either side.</summary>
    HipEnd,

    /// <summary>The top of a pyramid roof: four slopes meeting in a point, with four hips.</summary>
    Pyramid
}

/// <summary>
/// Pitch of a roof piece, named after the vanilla pieces (26°, 45° and the steep "67°" one, which rises 2 m per metre).
/// </summary>
public enum RoofPitch
{
    /// <summary>26°: rises 1 m over 2 m.</summary>
    Low,

    /// <summary>45°: rises 2 m over 2 m.</summary>
    Medium,

    /// <summary>"67°": rises 4 m over 2 m.</summary>
    Steep
}

/// <summary>
/// What covers the ridge and the hips of a roof.
/// </summary>
public enum RoofCrest
{
    /// <summary>Nothing; the covering runs over the ridge (turf).</summary>
    None,

    /// <summary>Two ridge boards (shingles).</summary>
    Boards,

    /// <summary>A round roll of thatch (straw and reed).</summary>
    Roll,

    /// <summary>Flat stone caps (slate).</summary>
    Cap,

    /// <summary>A row of round ridge tiles along every ridge and hip (tiled roofs).</summary>
    Tiles
}

/// <summary>
/// How a roof covering is built: thickness, courses, texture size and trim.
/// </summary>
public sealed class RoofStyle
{
    /// <summary>Thickness of the covering, in metres.</summary>
    public float Thickness { get; set; } = 0.1f;

    /// <summary>Wanted length of one course (row of shingles or slates) along the slope; 0 for a smooth covering.</summary>
    public float CourseLength { get; set; }

    /// <summary>How far each course stands out over the one above it.</summary>
    public float Step { get; set; }

    /// <summary>Courses in one texture tile, for a covering with courses.</summary>
    public int CoursesPerTile { get; set; } = 4;

    /// <summary>Width of one texture tile across the slope, in metres.</summary>
    public float TileWidth { get; set; } = 2f;

    /// <summary>Length of one texture tile along the slope, in metres, for a smooth covering.</summary>
    public float TileHeight { get; set; } = 2f;

    /// <summary>How far the covering reaches past the eave, horizontally.</summary>
    public float Overhang { get; set; } = 0.2f;

    /// <summary>A log along the eave that holds the covering (the turf log of a sod roof).</summary>
    public bool EaveLog { get; set; }

    /// <summary>What covers the ridge and hips.</summary>
    public RoofCrest Crest { get; set; }

    /// <summary>True if the edge texture shows the layers of the covering from bottom to top, so it spans the thickness.</summary>
    public bool EdgeStrip { get; set; }

    /// <summary>Width of one edge texture tile, in metres.</summary>
    public float EdgeTile { get; set; } = 2f;

    /// <summary>
    /// True for a roof of separate tiles, each laid as its own piece in rows that lap over each other, half a tile offset
    /// row by row, on a slab of stone. The rows are <see cref="CourseLength"/> apart. The top material is the tiles, the
    /// edge material the stone slab.
    /// </summary>
    public bool Tiled { get; set; }

    /// <summary>How far the stone slab of a tiled roof rises above the roof's plane, out of the slope, in metres.</summary>
    public float TileLift { get; set; }
}

/// <summary>
/// Part of a roof piece: the piece itself, or the overhang past one of its eaves, which hides where the roof goes on
/// downhill.
/// </summary>
public enum RoofPart
{
    /// <summary>The 2 x 2 m piece.</summary>
    Main,

    /// <summary>The overhang past the eave at +z.</summary>
    EaveZ,

    /// <summary>The overhang past the eave at +x (outer corners).</summary>
    EaveX
}

/// <summary>
/// Where the smoke hole's hatch sits on a <see cref="RoofShape.SmokeHole"/> piece.
/// </summary>
public readonly struct HatchPlacement
{
    /// <summary>Creates the placement.</summary>
    /// <param name="hinge">Hinge, at the upper edge of the frame.</param>
    /// <param name="rotation">Rotation of the closed hatch: lying on the slope, local +z running down the roof.</param>
    /// <param name="size">Side of the square hatch.</param>
    public HatchPlacement(Vector3 hinge, Quaternion rotation, float size)
    {
        Hinge = hinge;
        Rotation = rotation;
        Size = size;
    }

    /// <summary>Hinge, at the upper edge of the frame.</summary>
    public Vector3 Hinge { get; }

    /// <summary>Rotation of the closed hatch: lying on the slope, local +z running down the roof.</summary>
    public Quaternion Rotation { get; }

    /// <summary>Side of the square hatch.</summary>
    public float Size { get; }
}

/// <summary>
/// Builds the meshes of the White Hilt roofs in code: a slab of covering over the 2 x 2 m footprint of a vanilla roof
/// piece, with courses, edges, an underside of boards and trim (ridge boards, thatch rolls, turf logs, the smoke hole's
/// frame). Only uses UnityEngine, so the same code renders the offline previews.
/// </summary>
public static class RoofMeshBuilder
{
    /// <summary>Submesh of the covering.</summary>
    public const int TopMaterial = 0;

    /// <summary>Submesh of the boards under the covering.</summary>
    public const int UndersideMaterial = 1;

    /// <summary>Submesh of the cut edges of the covering.</summary>
    public const int EdgeMaterial = 2;

    /// <summary>Submesh of the wooden trim.</summary>
    public const int TrimMaterial = 3;

    /// <summary>Half the side of the smoke hole.</summary>
    public const float HoleHalf = 0.55f;

    /// <summary>Thickness of the smoke hole's frame boards.</summary>
    public const float FrameThickness = 0.07f;

    /// <summary>How far the smoke hole's frame stands up from the roof.</summary>
    public const float FrameHeight = 0.22f;

    /// <summary>Thickness of the smoke hole's hatch.</summary>
    public const float HatchThickness = 0.05f;

    private const float Epsilon = 1e-4f;

    // A tile of the tiled roofs, as the vanilla Grausten Roof lays them, in metres: across the slope, along it, how far it
    // reaches below the start of its row onto the row under it, and its underside and thickness at its lower and upper
    // ends, out of the slab's top.
    private const float TileSpacing = 0.4f;
    private const float TileWidthHalf = 0.205f;
    private const float TileLength = 0.79f;
    private const float TileLapBelow = 0.12f;
    private const float TileLowerBottom = -0.014f;
    private const float TileUpperBottom = -0.062f;
    private const float TileLowerThickness = 0.107f;
    private const float TileUpperThickness = 0.082f;

    // The round ridge tiles: radius, length, how far each laps over the next, and how high over the slab they rest.
    private const float RidgeTileRadius = 0.16f;
    private const float RidgeTileLength = 0.42f;
    private const float RidgeTileLap = 0.04f;
    private const float RidgeTileSeat = 0.119f;

    /// <summary>
    /// The angle the vanilla pieces of a pitch are named after.
    /// </summary>
    /// <param name="pitch">The pitch.</param>
    /// <returns>26, 45 or 67.</returns>
    public static int Degrees(RoofPitch pitch)
    {
        return pitch switch { RoofPitch.Low => 26, RoofPitch.Medium => 45, _ => 67 };
    }

    /// <summary>
    /// How much the roof rises over the 2 m of a piece.
    /// </summary>
    /// <param name="pitch">The pitch.</param>
    /// <returns>1, 2 or 4 metres.</returns>
    public static float Rise(RoofPitch pitch)
    {
        return pitch switch { RoofPitch.Low => 1f, RoofPitch.Medium => 2f, _ => 4f };
    }

    /// <summary>
    /// Height of the covering's surface (without courses) over a point of the piece's footprint, in the piece's own space,
    /// where the slope runs down towards +z.
    /// </summary>
    /// <param name="shape">The shape.</param>
    /// <param name="pitch">The pitch.</param>
    /// <param name="x">Across the slope, -1 to 1.</param>
    /// <param name="z">Down the slope, -1 to 1.</param>
    /// <returns>The height.</returns>
    public static float Height(RoofShape shape, RoofPitch pitch, float x, float z)
    {
        float g = Rise(pitch) / 2f;
        return shape switch
        {
            RoofShape.Ridge => g * (1f - Mathf.Abs(z)),
            RoofShape.InnerCorner => Mathf.Max(g * (1f - z), g * (1f - x)),
            RoofShape.OuterCorner => Mathf.Min(g * (1f - z), g * (1f - x)),
            RoofShape.Slope or RoofShape.SmokeHole => SlopeCentre(pitch) - z * g,
            _ => FindFacet(Facets(shape, pitch, new RoofStyle(), RoofPart.Main, out _), new Vector2(x, z)).Height(new Vector2(x, z))
        };
    }

    /// <summary>
    /// Whether a shape is one of the top pieces that only White Hilt has, with snap points of its own rather than those of
    /// the vanilla piece it is cloned from.
    /// </summary>
    /// <param name="shape">The shape.</param>
    /// <returns>True for the ridge corner, tee and cross, the hip end and the pyramid top.</returns>
    public static bool IsTopJoint(RoofShape shape)
    {
        return shape is RoofShape.RidgeCorner or RoofShape.RidgeTee or RoofShape.RidgeCross or RoofShape.HipEnd or RoofShape.Pyramid;
    }

    /// <summary>
    /// The snap points of a top joint, in the piece's own space: its four corners, low on the slopes as a ridge's, and the
    /// end of each ridge that leaves it, high on the ridge as a ridge's ends.
    /// </summary>
    /// <param name="shape">A shape for which <see cref="IsTopJoint"/> is true.</param>
    /// <param name="pitch">The pitch.</param>
    /// <returns>The snap points.</returns>
    public static List<Vector3> TopJointSnapPoints(RoofShape shape, RoofPitch pitch)
    {
        float g = Rise(pitch) / 2f;
        List<Vector3> points = new() { new(1f, 0f, 1f), new(1f, 0f, -1f), new(-1f, 0f, 1f), new(-1f, 0f, -1f) };
        Vector2[] ends = shape switch
        {
            RoofShape.RidgeCorner => new[] { new Vector2(-1f, 0f), new Vector2(0f, 1f) },
            RoofShape.RidgeTee => new[] { new Vector2(-1f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f) },
            RoofShape.RidgeCross => new[] { new Vector2(-1f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(0f, -1f) },
            RoofShape.HipEnd => new[] { new Vector2(-1f, 0f) },
            RoofShape.Pyramid => new[] { Vector2.zero },
            _ => new Vector2[0]
        };
        foreach (Vector2 end in ends)
        {
            points.Add(new Vector3(end.x, g, end.y));
        }

        return points;
    }

    /// <summary>
    /// The surface normal of the covering at a point.
    /// </summary>
    /// <param name="shape">The shape.</param>
    /// <param name="pitch">The pitch.</param>
    /// <param name="x">Across the slope, -1 to 1.</param>
    /// <param name="z">Down the slope, -1 to 1.</param>
    /// <returns>The normal.</returns>
    public static Vector3 SurfaceNormal(RoofShape shape, RoofPitch pitch, float x, float z)
    {
        Facet facet = FindFacet(Facets(shape, pitch, new RoofStyle(), RoofPart.Main, out _), new Vector2(x, z));
        return facet.Normal;
    }

    /// <summary>
    /// The eave overhangs a shape has.
    /// </summary>
    /// <param name="shape">The shape.</param>
    /// <returns>The overhang parts.</returns>
    public static RoofPart[] Eaves(RoofShape shape)
    {
        return shape switch
        {
            RoofShape.Slope or RoofShape.SmokeHole => new[] { RoofPart.EaveZ },
            RoofShape.OuterCorner => new[] { RoofPart.EaveZ, RoofPart.EaveX },
            _ => new RoofPart[0]
        };
    }

    /// <summary>
    /// Builds the mesh of a roof piece or of one of its eave overhangs, with four submeshes: covering, underside, edges
    /// and trim.
    /// </summary>
    /// <param name="shape">The shape.</param>
    /// <param name="pitch">The pitch.</param>
    /// <param name="style">The covering.</param>
    /// <param name="part">The piece itself or an overhang.</param>
    /// <returns>The mesh.</returns>
    public static Mesh Build(RoofShape shape, RoofPitch pitch, RoofStyle style, RoofPart part = RoofPart.Main)
    {
        MeshParts parts = new();
        List<Facet> facets = Facets(shape, pitch, style, part, out List<Boundary> boundaries);
        float courseLength = CourseLength(pitch, style);
        foreach (Facet facet in facets)
        {
            AddFacet(parts, facet, boundaries, style, courseLength);
        }

        if (style.EaveLog && part != RoofPart.Main)
        {
            AddEaveLog(parts, style, part, facets);
        }

        AddCrest(parts, shape, pitch, style, part, facets);
        if (shape == RoofShape.SmokeHole && part == RoofPart.Main)
        {
            AddSmokeHoleFrame(parts, pitch, style);
        }

        return parts.ToMesh($"roof_{shape}_{Degrees(pitch)}_{part}".ToLowerInvariant());
    }

    /// <summary>
    /// One convex slab per plane of the covering, for convex mesh colliders. Vanilla placement only measures convex
    /// colliders; with a single concave one it puts the ghost far away.
    /// </summary>
    /// <param name="shape">The shape.</param>
    /// <param name="pitch">The pitch.</param>
    /// <param name="style">The covering.</param>
    /// <returns>The slabs.</returns>
    public static List<Mesh> ColliderSlabs(RoofShape shape, RoofPitch pitch, RoofStyle style)
    {
        List<Mesh> slabs = new();
        foreach (Facet facet in Facets(shape, pitch, style, RoofPart.Main, out _))
        {
            List<Vector3> vertices = new();
            foreach (Vector2 p in facet.Polygon)
            {
                vertices.Add(new Vector3(p.x, facet.Height(p), p.y));
            }

            foreach (Vector2 p in facet.Polygon)
            {
                vertices.Add(new Vector3(p.x, facet.Height(p) - style.Thickness, p.y));
            }

            int n = facet.Polygon.Count;
            List<int> triangles = new();
            for (int i = 1; i < n - 1; i++)
            {
                triangles.AddRange(new[] { 0, i, i + 1, n, n + i + 1, n + i });
            }

            for (int i = 0; i < n; i++)
            {
                int next = (i + 1) % n;
                triangles.AddRange(new[] { i, n + i, next, next, n + i, n + next });
            }

            Mesh slab = new() { name = $"roof_{shape}_{Degrees(pitch)}_collider{slabs.Count}".ToLowerInvariant() };
            slab.SetVertices(vertices);
            slab.SetTriangles(triangles, 0);
            slab.RecalculateBounds();
            slabs.Add(slab);
        }

        return slabs;
    }

    /// <summary>
    /// Where the hatch of a smoke hole piece hangs.
    /// </summary>
    /// <param name="pitch">The pitch.</param>
    /// <returns>The hinge and the closed rotation.</returns>
    public static HatchPlacement Hatch(RoofPitch pitch)
    {
        Vector3 normal = SurfaceNormal(RoofShape.SmokeHole, pitch, 0f, 0f);
        float z = -HoleHalf - FrameThickness;
        Vector3 hinge = new Vector3(0f, Height(RoofShape.SmokeHole, pitch, 0f, z), z) + normal * FrameHeight;
        return new HatchPlacement(hinge, Quaternion.FromToRotation(Vector3.up, normal), 2f * (HoleHalf + FrameThickness));
    }

    /// <summary>
    /// A box mesh with one submesh, e.g. the hatch of a smoke hole.
    /// </summary>
    /// <param name="center">Centre.</param>
    /// <param name="size">Size.</param>
    /// <param name="tile">Metres per texture tile.</param>
    /// <returns>The mesh.</returns>
    public static Mesh Box(Vector3 center, Vector3 size, float tile)
    {
        MeshParts parts = new();
        parts.AddBox(center, Vector3.right * size.x / 2f, Vector3.up * size.y / 2f, Vector3.forward * size.z / 2f, 0, tile);
        return parts.ToMesh("roof_box", 1);
    }

    /// <summary>
    /// The crossed barge boards of a gable, in the gable's own plane (y up, z along the gable), crossing at the origin,
    /// which sits on the ridge's end. Returns the upper tips, where the dragon heads go.
    /// </summary>
    /// <param name="pitch">The pitch of the roof.</param>
    /// <param name="tips">The two upper tips, each with the outward direction and the board's upward side.</param>
    /// <returns>The mesh, one submesh.</returns>
    public static Mesh GableBoards(RoofPitch pitch, out (Vector3 Position, Vector3 Outward, Vector3 Up)[] tips)
    {
        const float Width = 0.24f;
        const float Thickness = 0.06f;
        const float Above = 0.7f;
        const float Below = 1.4f;
        float angle = Mathf.Atan(Rise(pitch) / 2f);
        MeshParts parts = new();
        tips = new (Vector3, Vector3, Vector3)[2];
        for (int side = 0; side < 2; side++)
        {
            float sign = side == 0 ? 1f : -1f;
            Vector3 down = new(0f, -Mathf.Sin(angle), sign * Mathf.Cos(angle));
            Vector3 up = new(0f, Mathf.Cos(angle), sign * Mathf.Sin(angle));
            Vector3 center = down * ((Below - Above) / 2f) + up * (Width / 2f) + Vector3.right * (side == 0 ? -Thickness / 2f : Thickness / 2f);
            parts.AddBox(center, Vector3.right * Thickness / 2f, up * Width / 2f, down * (Below + Above) / 2f, 0, 1f);
            tips[side] = (-down * Above + up * (Width / 2f), -down, up);
        }

        return parts.ToMesh($"roof_gable_{Degrees(pitch)}", 1);
    }

    /// <summary>
    /// Sets a dragon head (the flat carving from the longship, snout towards -x) on an upper tip of the gable boards.
    /// </summary>
    /// <param name="head">The head's transform, a child of the gable.</param>
    /// <param name="meshBounds">Bounds of the head mesh.</param>
    /// <param name="tip">The tip, in the gable's space.</param>
    /// <param name="outward">Where the head looks.</param>
    /// <param name="up">The board's upper side.</param>
    public static void PlaceGableHead(Transform head, Bounds meshBounds, Vector3 tip, Vector3 outward, Vector3 up)
    {
        const float Height = 0.7f;
        Vector3 forward = -outward.normalized;
        Vector3 upward = Vector3.ProjectOnPlane(up, forward).normalized;
        Quaternion rotation = Quaternion.LookRotation(Vector3.Cross(forward, upward), upward);
        float scale = Height / Mathf.Max(0.01f, meshBounds.size.y);
        Vector3 neck = new(meshBounds.max.x, meshBounds.min.y + meshBounds.size.y * 0.2f, meshBounds.center.z);
        head.localRotation = rotation;
        head.localScale = Vector3.one * scale;
        head.localPosition = tip - rotation * (neck * scale);
    }

    private static float SlopeCentre(RoofPitch pitch)
    {
        return pitch switch { RoofPitch.Low => 0.5f, RoofPitch.Medium => 0f, _ => 1f };
    }

    // Courses fit a whole, even number of times into the 2 m run of a piece, so they line up between pieces and on
    // each half of a ridge.
    private static float CourseLength(RoofPitch pitch, RoofStyle style)
    {
        if (style.CourseLength <= 0f)
        {
            return 0f;
        }

        float g = Rise(pitch) / 2f;
        float length = 2f * Mathf.Sqrt(1f + g * g);
        int count = Mathf.Max(2, Mathf.RoundToInt(length / style.CourseLength));
        if (count % 2 == 1)
        {
            count++;
        }

        return length / count;
    }

    // The piece fills its 2 x 2 m footprint exactly, so neighbours meet without overlapping (overlapping faces in one
    // plane flicker); the overhangs are parts of their own.
    private static List<Facet> Facets(RoofShape shape, RoofPitch pitch, RoofStyle style, RoofPart part, out List<Boundary> boundaries)
    {
        float g = Rise(pitch) / 2f;
        float o = 1f + style.Overhang;
        float x0 = -1f;
        float x1 = 1f;
        float z0 = -1f;
        float z1 = 1f;
        if (part == RoofPart.EaveZ)
        {
            z0 = 1f;
            z1 = o;
            x1 = shape == RoofShape.OuterCorner ? o : 1f;
        }
        else if (part == RoofPart.EaveX)
        {
            x0 = 1f;
            x1 = o;
        }

        Facet slopeZ = new(g, new Vector2(0f, -g), -1f);
        Facet slopeX = new(g, new Vector2(-g, 0f), -1f);
        List<Facet> facets = new();
        switch (shape)
        {
            case RoofShape.Slope:
            case RoofShape.SmokeHole:
                Facet slope = new(SlopeCentre(pitch), new Vector2(0f, -g), -1f);
                if (shape == RoofShape.Slope || part != RoofPart.Main)
                {
                    facets.Add(slope.With(Rectangle(x0, x1, z0, z1)));
                }
                else
                {
                    float[] xs = { x0, -HoleHalf, HoleHalf, x1 };
                    float[] zs = { z0, -HoleHalf, HoleHalf, z1 };
                    for (int i = 0; i < 3; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            if (i != 1 || j != 1)
                            {
                                facets.Add(slope.With(Rectangle(xs[i], xs[i + 1], zs[j], zs[j + 1])));
                            }
                        }
                    }
                }

                break;
            case RoofShape.Ridge:
                facets.Add(new Facet(g, new Vector2(0f, g), -1f).With(Rectangle(x0, x1, z0, 0f)));
                facets.Add(new Facet(g, new Vector2(0f, -g), -1f).With(Rectangle(x0, x1, 0f, z1)));
                break;
            case RoofShape.InnerCorner:
                facets.Add(slopeZ.With(Clip(Rectangle(x0, x1, z0, z1), new Vector2(-1f, 1f), 0f)));
                facets.Add(slopeX.With(Clip(Rectangle(x0, x1, z0, z1), new Vector2(1f, -1f), 0f)));
                break;
            case RoofShape.OuterCorner:
                facets.Add(slopeZ.With(Clip(Rectangle(x0, x1, z0, z1), new Vector2(1f, -1f), 0f)));
                facets.Add(slopeX.With(Clip(Rectangle(x0, x1, z0, z1), new Vector2(-1f, 1f), 0f)));
                break;
            default:
                facets.AddRange(TopJointFacets(shape, g));
                break;
        }

        boundaries = new List<Boundary>
        {
            new(true, z0, x0, x1, Vector3.back),
            new(true, z1, x0, x1, Vector3.forward),
            new(false, x0, z0, z1, Vector3.left),
            new(false, x1, z0, z1, Vector3.right)
        };
        if (shape == RoofShape.SmokeHole && part == RoofPart.Main)
        {
            boundaries.Add(new Boundary(true, -HoleHalf, -HoleHalf, HoleHalf, Vector3.forward));
            boundaries.Add(new Boundary(true, HoleHalf, -HoleHalf, HoleHalf, Vector3.back));
            boundaries.Add(new Boundary(false, -HoleHalf, -HoleHalf, HoleHalf, Vector3.right));
            boundaries.Add(new Boundary(false, HoleHalf, -HoleHalf, HoleHalf, Vector3.left));
        }

        facets.RemoveAll(facet => facet.Polygon.Count < 3);
        return facets;
    }

    // The slopes of a top joint, each a plane falling to the edge of the footprint, over the part of the footprint where
    // it is the roof: under a hip the lower of two slopes, over a valley the higher.
    private static List<Facet> TopJointFacets(RoofShape shape, float g)
    {
        Facet south = new(g, new Vector2(0f, g), -1f);
        Facet north = new(g, new Vector2(0f, -g), -1f);
        Facet east = new(g, new Vector2(-g, 0f), -1f);
        Facet west = new(g, new Vector2(g, 0f), -1f);
        Vector2 o = Vector2.zero;
        Vector2 nw = new(-1f, -1f);
        Vector2 ne = new(1f, -1f);
        Vector2 se = new(1f, 1f);
        Vector2 sw = new(-1f, 1f);
        Vector2 n = new(0f, -1f);
        Vector2 s = new(0f, 1f);
        Vector2 e = new(1f, 0f);
        Vector2 w = new(-1f, 0f);
        List<Vector2> P(params Vector2[] points) => new(points);
        return shape switch
        {
            RoofShape.RidgeCorner => new List<Facet>
            {
                south.With(P(nw, ne, o, w)), east.With(P(o, ne, se, s)), north.With(P(o, w, sw)), west.With(P(o, sw, s))
            },
            RoofShape.RidgeTee => new List<Facet>
            {
                south.With(P(nw, ne, e, w)), north.With(P(o, e, se)), east.With(P(o, se, s)), north.With(P(o, w, sw)), west.With(P(o, sw, s))
            },
            RoofShape.RidgeCross => new List<Facet>
            {
                north.With(P(o, e, se)), east.With(P(o, se, s)), north.With(P(o, w, sw)), west.With(P(o, sw, s)),
                south.With(P(o, w, nw)), west.With(P(o, nw, n)), south.With(P(o, e, ne)), east.With(P(o, ne, n))
            },
            RoofShape.HipEnd => new List<Facet>
            {
                south.With(P(nw, ne, o, w)), north.With(P(w, o, se, sw)), east.With(P(o, ne, se))
            },
            RoofShape.Pyramid => new List<Facet>
            {
                south.With(P(nw, ne, o)), east.With(P(ne, se, o)), north.With(P(se, sw, o)), west.With(P(sw, nw, o))
            },
            _ => new List<Facet>()
        };
    }

    // The ridges and hips of a piece, each from its lower or outer end to its higher or inner one, on the plane of the
    // roof (before the covering's thickness).
    private static List<(Vector3 From, Vector3 To)> CrestLines(RoofShape shape, RoofPitch pitch, RoofStyle style, RoofPart part)
    {
        float g = Rise(pitch) / 2f;
        Vector3 Point(float x, float z) => new(x, Height(shape, pitch, x, z), z);
        Vector3 top = new(0f, g, 0f);
        List<(Vector3, Vector3)> lines = new();
        switch (shape)
        {
            case RoofShape.Ridge when part == RoofPart.Main:
                lines.Add((new Vector3(-1f, g, 0f), new Vector3(1f, g, 0f)));
                break;
            case RoofShape.OuterCorner when part == RoofPart.Main:
                lines.Add((Point(1f, 1f), Point(-1f, -1f)));
                break;
            case RoofShape.OuterCorner when part == RoofPart.EaveZ:
                float o = 1f + style.Overhang;
                lines.Add((new Vector3(o, -g * style.Overhang, o), Point(1f, 1f)));
                break;
            case RoofShape.RidgeCorner:
                lines.Add((new Vector3(-1f, g, 0f), top));
                lines.Add((new Vector3(0f, g, 1f), top));
                lines.Add((Point(1f, -1f), top));
                break;
            case RoofShape.RidgeTee:
                lines.Add((new Vector3(-1f, g, 0f), new Vector3(1f, g, 0f)));
                lines.Add((new Vector3(0f, g, 1f), top));
                break;
            case RoofShape.RidgeCross:
                lines.Add((new Vector3(-1f, g, 0f), new Vector3(1f, g, 0f)));
                lines.Add((new Vector3(0f, g, 1f), new Vector3(0f, g, -1f)));
                break;
            case RoofShape.HipEnd:
                lines.Add((new Vector3(-1f, g, 0f), top));
                lines.Add((Point(1f, -1f), top));
                lines.Add((Point(1f, 1f), top));
                break;
            case RoofShape.Pyramid:
                foreach (Vector2 corner in new[] { new Vector2(1f, 1f), new Vector2(1f, -1f), new Vector2(-1f, 1f), new Vector2(-1f, -1f) })
                {
                    lines.Add((Point(corner.x, corner.y), top));
                }

                break;
        }

        return lines;
    }

    // The facet of the roof a point lies on, or the nearest one if it lies on none (a point on a shared edge).
    private static Facet FindFacet(List<Facet> facets, Vector2 point)
    {
        Facet best = facets[0];
        float bestHeight = float.MaxValue;
        foreach (Facet facet in facets)
        {
            if (Contains(facet.Polygon, point))
            {
                return facet;
            }

            float distance = (Centroid(facet.Polygon) - point).sqrMagnitude;
            if (distance < bestHeight)
            {
                bestHeight = distance;
                best = facet;
            }
        }

        return best;
    }

    // Adds one sloped facet of the roof: the underside as boards running down, and the covering on top as courses of
    // tiles or shingles that step down the slope, each course lifted a little over the one below so the edges show like
    // real tiles. Texture coordinates follow the slope, so tiles keep their size on every pitch.
    private static void AddFacet(MeshParts parts, Facet facet, List<Boundary> boundaries, RoofStyle style, float courseLength)
    {
        if (style.Tiled)
        {
            AddTiledFacet(parts, facet, boundaries, style, courseLength);
            return;
        }

        float slopeFactor = Mathf.Sqrt(1f + facet.Gradient.sqrMagnitude);
        Vector2 up = facet.Up;
        Vector2 lateral = new(up.y, -up.x);
        float tileV = courseLength > 0f ? courseLength * style.CoursesPerTile : style.TileHeight;
        Vector2 TopUv(Vector2 p) => new(Vector2.Dot(p, lateral) / style.TileWidth, (Vector2.Dot(p, up) - facet.EaveW) * slopeFactor / tileV);

        // Underside: boards running down the roof.
        List<Vector3> under = new();
        List<Vector2> underUv = new();
        foreach (Vector2 p in facet.Polygon)
        {
            under.Add(new Vector3(p.x, facet.Height(p) - style.Thickness, p.y));
            underUv.Add(new Vector2(Vector2.Dot(p, lateral) / 2f, (Vector2.Dot(p, up) - facet.EaveW) * slopeFactor / 2f));
        }

        parts.AddPolygon(UndersideMaterial, under, underUv, -facet.Normal);

        float wMin = float.MaxValue;
        float wMax = float.MinValue;
        foreach (Vector2 p in facet.Polygon)
        {
            float w = Vector2.Dot(p, up);
            wMin = Mathf.Min(wMin, w);
            wMax = Mathf.Max(wMax, w);
        }

        float dw = courseLength > 0f ? courseLength / slopeFactor : 0f;
        int first = dw > 0f ? Mathf.FloorToInt((wMin - facet.EaveW) / dw + Epsilon) : 0;
        int last = dw > 0f ? Mathf.CeilToInt((wMax - facet.EaveW) / dw - Epsilon) - 1 : 0;
        for (int k = first; k <= last; k++)
        {
            List<Vector2> band = facet.Polygon;
            float wa = float.MinValue;
            float wb = float.MaxValue;
            if (dw > 0f)
            {
                wa = facet.EaveW + k * dw;
                wb = wa + dw;
                band = Clip(Clip(band, -up, -wa), up, wb);
                if (band.Count < 3)
                {
                    continue;
                }
            }

            float Offset(Vector2 p) => dw > 0f ? style.Step * Mathf.Clamp01((wb - Vector2.Dot(p, up)) / dw) : 0f;

            List<Vector3> top = new();
            List<Vector2> topUv = new();
            foreach (Vector2 p in band)
            {
                top.Add(new Vector3(p.x, facet.Height(p) + Offset(p), p.y));
                topUv.Add(TopUv(p));
            }

            parts.AddPolygon(TopMaterial, top, topUv, facet.Normal);

            for (int i = 0; i < band.Count; i++)
            {
                Vector2 a = band[i];
                Vector2 b = band[(i + 1) % band.Count];
                Boundary? boundary = FindBoundary(boundaries, a, b);
                if (boundary.HasValue)
                {
                    AddEdge(parts, facet, style, boundary.Value, a, b, Offset(a), Offset(b));
                }

                // The butt of a course: the step up from the course below.
                if (dw > 0f && k > first && Mathf.Abs(Vector2.Dot(a, up) - wa) < Epsilon && Mathf.Abs(Vector2.Dot(b, up) - wa) < Epsilon)
                {
                    Vector3 lowA = new(a.x, facet.Height(a), a.y);
                    Vector3 lowB = new(b.x, facet.Height(b), b.y);
                    Vector3 rise = Vector3.up * style.Step;
                    parts.AddPolygon(
                        TopMaterial,
                        new List<Vector3> { lowA, lowB, lowB + rise, lowA + rise },
                        new List<Vector2> { TopUv(a), TopUv(b), TopUv(b) + new Vector2(0f, 0.01f), TopUv(a) + new Vector2(0f, 0.01f) },
                        new Vector3(-up.x, 0f, -up.y));
                }
            }
        }
    }

    // A facet of a tiled roof: the stone slab, its top lifted out of the plane by TileLift, and on it the tiles, each its
    // own block in rows that lap down the slope, every other row half a tile across. A tile that crosses the edge of the
    // facet (a hip, a valley, the ridge, the edge of the piece) is cut there, so neighbouring pieces meet without
    // overlapping and the rows run on from one piece to the next.
    private static void AddTiledFacet(MeshParts parts, Facet facet, List<Boundary> boundaries, RoofStyle style, float courseLength)
    {
        float slopeFactor = Mathf.Sqrt(1f + facet.Gradient.sqrMagnitude);
        float lift = style.TileLift * slopeFactor;
        Vector2 up = facet.Up;
        Vector2 lateral = new(up.y, -up.x);

        List<Vector3> under = new();
        List<Vector2> underUv = new();
        List<Vector3> slab = new();
        foreach (Vector2 p in facet.Polygon)
        {
            Vector2 uv = new(Vector2.Dot(p, lateral) / 2f, (Vector2.Dot(p, up) - facet.EaveW) * slopeFactor / 2f);
            under.Add(new Vector3(p.x, facet.Height(p) - style.Thickness, p.y));
            slab.Add(new Vector3(p.x, facet.Height(p) + lift, p.y));
            underUv.Add(uv);
        }

        parts.AddPolygon(UndersideMaterial, under, underUv, -facet.Normal);
        parts.AddPolygon(EdgeMaterial, slab, underUv, facet.Normal);
        for (int i = 0; i < facet.Polygon.Count; i++)
        {
            Vector2 a = facet.Polygon[i];
            Vector2 b = facet.Polygon[(i + 1) % facet.Polygon.Count];
            Boundary? boundary = FindBoundary(boundaries, a, b);
            if (boundary.HasValue)
            {
                AddEdge(parts, facet, style, boundary.Value, a, b, lift, lift);
            }
        }

        if (courseLength <= 0f)
        {
            return;
        }

        float wMin = float.MaxValue;
        float wMax = float.MinValue;
        float uMin = float.MaxValue;
        float uMax = float.MinValue;
        foreach (Vector2 p in facet.Polygon)
        {
            wMin = Mathf.Min(wMin, Vector2.Dot(p, up));
            wMax = Mathf.Max(wMax, Vector2.Dot(p, up));
            uMin = Mathf.Min(uMin, Vector2.Dot(p, lateral));
            uMax = Mathf.Max(uMax, Vector2.Dot(p, lateral));
        }

        float dw = courseLength / slopeFactor;
        float below = TileLapBelow / slopeFactor;
        float length = TileLength / slopeFactor;
        int first = Mathf.FloorToInt((wMin - facet.EaveW) / dw + Epsilon);
        int last = Mathf.CeilToInt((wMax + below - facet.EaveW) / dw - Epsilon);
        for (int k = first; k <= last; k++)
        {
            float wLow = facet.EaveW + k * dw - below;
            float wHigh = wLow + length;
            float shift = Mathf.Abs(k) % 2 == 1 ? TileSpacing / 2f : 0f;
            int jMin = Mathf.FloorToInt((uMin - shift) / TileSpacing) - 1;
            int jMax = Mathf.CeilToInt((uMax - shift) / TileSpacing) + 1;
            for (int j = jMin; j <= jMax; j++)
            {
                float centre = shift + j * TileSpacing;
                List<Vector2> footprint = new()
                {
                    lateral * (centre - TileWidthHalf) + up * wLow,
                    lateral * (centre + TileWidthHalf) + up * wLow,
                    lateral * (centre + TileWidthHalf) + up * wHigh,
                    lateral * (centre - TileWidthHalf) + up * wHigh
                };
                footprint = Intersect(footprint, facet.Polygon);
                if (footprint.Count >= 3 && Area(footprint) > 0.002f)
                {
                    AddTile(parts, facet, footprint, up, wLow, wHigh, slopeFactor, style.TileLift);
                }
            }
        }
    }

    // One tile: a block over its footprint, its lower end raised where it lies on the row below, lifted out of the slope
    // onto the slab.
    private static void AddTile(MeshParts parts, Facet facet, List<Vector2> footprint, Vector2 up, float wLow, float wHigh, float slopeFactor, float lift)
    {
        List<Vector3> bottom = new();
        List<Vector3> top = new();
        List<Vector2> uv = new();
        Vector3 centre = Vector3.zero;
        foreach (Vector2 p in footprint)
        {
            float t = Mathf.Clamp01((Vector2.Dot(p, up) - wLow) / (wHigh - wLow));
            float under = lift + Mathf.Lerp(TileLowerBottom, TileUpperBottom, t);
            float over = under + Mathf.Lerp(TileLowerThickness, TileUpperThickness, t);
            float height = facet.Height(p);
            bottom.Add(new Vector3(p.x, height + under * slopeFactor, p.y));
            top.Add(new Vector3(p.x, height + over * slopeFactor, p.y));
            uv.Add(new Vector2(p.x + p.y, Vector2.Dot(p, up) * slopeFactor) * 0.8f);
            centre += top[top.Count - 1] + bottom[bottom.Count - 1];
        }

        centre /= 2f * footprint.Count;
        parts.AddPolygon(TopMaterial, top, uv, facet.Normal);
        parts.AddPolygon(TopMaterial, bottom, uv, -facet.Normal);
        for (int i = 0; i < footprint.Count; i++)
        {
            int next = (i + 1) % footprint.Count;
            Vector3 middle = (top[i] + top[next] + bottom[i] + bottom[next]) / 4f;
            Vector3 outward = middle - centre;
            outward -= facet.Normal * Vector3.Dot(outward, facet.Normal);
            float run = (top[next] - top[i]).magnitude * 0.8f;
            float rise = (top[i] - bottom[i]).magnitude * 0.8f;
            parts.AddPolygon(
                TopMaterial,
                new List<Vector3> { bottom[i], bottom[next], top[next], top[i] },
                new List<Vector2> { new(0f, 0f), new(run, 0f), new(run, rise), new(0f, rise) },
                outward);
        }
    }

    // The part of a convex polygon inside another convex polygon.
    private static List<Vector2> Intersect(List<Vector2> subject, List<Vector2> clip)
    {
        Vector2 inside = Centroid(clip);
        List<Vector2> result = subject;
        for (int i = 0; i < clip.Count && result.Count >= 3; i++)
        {
            Vector2 a = clip[i];
            Vector2 b = clip[(i + 1) % clip.Count];
            Vector2 outward = new(b.y - a.y, a.x - b.x);
            if (outward.sqrMagnitude < 1e-10f)
            {
                continue;
            }

            if (Vector2.Dot(outward, inside - a) > 0f)
            {
                outward = -outward;
            }

            result = Clip(result, outward, Vector2.Dot(outward, a));
        }

        return result;
    }

    private static float Area(List<Vector2> polygon)
    {
        float sum = 0f;
        for (int i = 0; i < polygon.Count; i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[(i + 1) % polygon.Count];
            sum += a.x * b.y - b.x * a.y;
        }

        return Mathf.Abs(sum) / 2f;
    }

    // A row of round ridge tiles along a ridge or a hip, from its lower end up: each tile wider at its lower end, where
    // it laps over the one below, and resting on the tiles of the slopes.
    private static void AddRidgeTiles(MeshParts parts, RoofPitch pitch, RoofStyle style, Vector3 from, Vector3 to)
    {
        float slopeFactor = Mathf.Sqrt(1f + Mathf.Pow(Rise(pitch) / 2f, 2f));
        Vector3 raise = Vector3.up * ((style.TileLift + RidgeTileSeat) * slopeFactor + RidgeTileRadius * 0.15f);
        Vector3 a = from + raise;
        Vector3 b = to + raise;
        float length = (b - a).magnitude;
        if (length < 0.05f)
        {
            return;
        }

        int count = Mathf.Max(1, Mathf.CeilToInt(length / (RidgeTileLength - RidgeTileLap)));
        Vector3 along = (b - a) / length;
        float step = length / count;
        for (int i = 0; i < count; i++)
        {
            Vector3 start = a + along * Mathf.Max(0f, i * step - RidgeTileLap / 2f);
            Vector3 end = a + along * Mathf.Min(length, (i + 1) * step + RidgeTileLap / 2f);
            parts.AddTaperedTube(TopMaterial, start, end, RidgeTileRadius, RidgeTileRadius * 0.88f, 14, 0.5f);
        }
    }

    private static void AddEdge(MeshParts parts, Facet facet, RoofStyle style, Boundary boundary, Vector2 a, Vector2 b, float offsetA, float offsetB)
    {
        float topA = facet.Height(a) + offsetA;
        float topB = facet.Height(b) + offsetB;
        float bottomA = facet.Height(a) - style.Thickness;
        float bottomB = facet.Height(b) - style.Thickness;
        Vector2 along = boundary.AlongX ? Vector2.right : Vector2.up;
        float uA = Vector2.Dot(a, along) / style.EdgeTile;
        float uB = Vector2.Dot(b, along) / style.EdgeTile;
        float span = style.Thickness + style.Step;
        Vector2 Uv(float u, float y, float bottom) => style.EdgeStrip ? new Vector2(u, Mathf.Clamp01((y - bottom) / span)) : new Vector2(u, y / style.TileHeight);
        parts.AddPolygon(
            EdgeMaterial,
            new List<Vector3> { new(a.x, bottomA, a.y), new(b.x, bottomB, b.y), new(b.x, topB, b.y), new(a.x, topA, a.y) },
            new List<Vector2> { Uv(uA, bottomA, bottomA), Uv(uB, bottomB, bottomB), Uv(uB, topB, bottomB), Uv(uA, topA, bottomA) },
            boundary.Outward);
    }

    // The edge of the piece a facet's edge lies on, if any: where the roof meets the next piece and the courses must
    // line up with it.
    private static Boundary? FindBoundary(List<Boundary> boundaries, Vector2 a, Vector2 b)
    {
        foreach (Boundary boundary in boundaries)
        {
            float va = boundary.AlongX ? a.y : a.x;
            float vb = boundary.AlongX ? b.y : b.x;
            if (Mathf.Abs(va - boundary.Value) > Epsilon || Mathf.Abs(vb - boundary.Value) > Epsilon)
            {
                continue;
            }

            float middle = boundary.AlongX ? (a.x + b.x) / 2f : (a.y + b.y) / 2f;
            if (middle >= boundary.Min - Epsilon && middle <= boundary.Max + Epsilon)
            {
                return boundary;
            }
        }

        return null;
    }

    private static void AddEaveLog(MeshParts parts, RoofStyle style, RoofPart part, List<Facet> facets)
    {
        const float Radius = 0.08f;
        float edge = 1f + style.Overhang - Radius;
        if (part == RoofPart.EaveZ)
        {
            float end = facets.Count > 1 ? edge : 1f;
            AddLog(parts, facets, new Vector2(-1f, edge), new Vector2(end, edge), Radius);
        }
        else
        {
            AddLog(parts, facets, new Vector2(edge, -1f), new Vector2(edge, edge), Radius);
        }
    }

    private static void AddLog(MeshParts parts, List<Facet> facets, Vector2 from, Vector2 to, float radius)
    {
        // Each end on its own facet's surface, so a log along a hip corner stays on the roof.
        Facet facetFrom = FindFacet(facets, Vector2.Lerp(from, to, 0.25f));
        Facet facetTo = FindFacet(facets, Vector2.Lerp(from, to, 0.75f));
        Vector3 a = new Vector3(from.x, facetFrom.Height(from), from.y) + facetFrom.Normal * radius * 0.85f;
        Vector3 b = new Vector3(to.x, facetTo.Height(to), to.y) + facetTo.Normal * radius * 0.85f;
        parts.AddCylinder(TrimMaterial, a, b, radius, 8, 2f);
    }

    // The ridge cover along the top of a ridge or an outer corner: crossed boards or a roll, as the covering's style
    // says.
    private static void AddCrest(MeshParts parts, RoofShape shape, RoofPitch pitch, RoofStyle style, RoofPart part, List<Facet> facets)
    {
        if (style.Crest == RoofCrest.Tiles)
        {
            foreach ((Vector3 from, Vector3 to) in CrestLines(shape, pitch, style, part))
            {
                AddRidgeTiles(parts, pitch, style, from, to);
            }

            return;
        }

        if (style.Crest == RoofCrest.None || facets.Count < 2 || (shape != RoofShape.Ridge && shape != RoofShape.OuterCorner) || part == RoofPart.EaveX)
        {
            return;
        }

        Vector3 a;
        Vector3 b;
        Facet first = facets[0];
        Facet second = facets[1];
        Vector2 hintFirst;
        Vector2 hintSecond;
        if (shape == RoofShape.Ridge)
        {
            float g = Rise(pitch) / 2f;
            a = new Vector3(-1f, g, 0f);
            b = new Vector3(1f, g, 0f);
            hintFirst = new Vector2(0f, -1f);
            hintSecond = new Vector2(0f, 1f);
        }
        else
        {
            float low = part == RoofPart.EaveZ ? 1f + style.Overhang : 1f;
            float high = part == RoofPart.EaveZ ? 1f : -1f;
            a = new Vector3(low, Height(shape, pitch, low, low), low);
            b = new Vector3(high, Height(shape, pitch, high, high), high);
            hintFirst = new Vector2(-1f, 1f);
            hintSecond = new Vector2(1f, -1f);
        }

        switch (style.Crest)
        {
            case RoofCrest.Boards:
                AddCrestBoard(parts, TrimMaterial, a, b, first.Normal, hintFirst, 0.2f, 0.035f);
                AddCrestBoard(parts, TrimMaterial, a, b, second.Normal, hintSecond, 0.2f, 0.035f);
                break;
            case RoofCrest.Cap:
                AddCrestBoard(parts, TopMaterial, a, b, first.Normal, hintFirst, 0.28f, 0.04f);
                AddCrestBoard(parts, TopMaterial, a, b, second.Normal, hintSecond, 0.28f, 0.04f);
                break;
            case RoofCrest.Roll:
                parts.AddCylinder(TopMaterial, a + Vector3.up * 0.05f, b + Vector3.up * 0.05f, 0.16f, 10, style.TileWidth);
                break;
        }
    }

    private static void AddCrestBoard(MeshParts parts, int material, Vector3 a, Vector3 b, Vector3 normal, Vector2 sideHint, float width, float thickness)
    {
        Vector3 along = (b - a).normalized;
        Vector3 down = Vector3.Cross(along, normal).normalized;
        if (down.x * sideHint.x + down.z * sideHint.y < 0f)
        {
            down = -down;
        }

        Vector3 center = (a + b) / 2f + down * (width / 2f - thickness) + normal * (thickness / 2f + 0.005f);
        parts.AddBox(center, along * ((b - a).magnitude / 2f), normal * (thickness / 2f), down * (width / 2f), material, 1f);
    }

    // The frame round the smoke hole: four boards standing on the roof's plane.
    private static void AddSmokeHoleFrame(MeshParts parts, RoofPitch pitch, RoofStyle style)
    {
        Vector3 normal = SurfaceNormal(RoofShape.SmokeHole, pitch, 0f, 0f);
        Vector3 upSlope = Vector3.Cross(Vector3.right, normal).normalized;
        if (upSlope.y < 0f)
        {
            upSlope = -upSlope;
        }

        float half = HoleHalf + FrameThickness / 2f;
        float frameTop = FrameHeight + (style.Tiled ? style.TileLift + TileLowerThickness : 0f);
        float height = (frameTop + style.Thickness) / 2f;
        Vector3 Base(float x, float z) => new Vector3(x, Height(RoofShape.SmokeHole, pitch, x, z), z) + normal * ((frameTop - style.Thickness) / 2f);
        float slopeFactor = Mathf.Sqrt(1f + Mathf.Pow(Rise(pitch) / 2f, 2f));
        float alongHalf = HoleHalf + FrameThickness;
        parts.AddBox(Base(0f, -half), Vector3.right * alongHalf, normal * height, upSlope * (FrameThickness / 2f), TrimMaterial, 1f);
        parts.AddBox(Base(0f, half), Vector3.right * alongHalf, normal * height, upSlope * (FrameThickness / 2f), TrimMaterial, 1f);
        parts.AddBox(Base(-half, 0f), Vector3.right * (FrameThickness / 2f), normal * height, upSlope * (HoleHalf * slopeFactor), TrimMaterial, 1f);
        parts.AddBox(Base(half, 0f), Vector3.right * (FrameThickness / 2f), normal * height, upSlope * (HoleHalf * slopeFactor), TrimMaterial, 1f);
    }

    private static List<Vector2> Rectangle(float x0, float x1, float z0, float z1)
    {
        return new List<Vector2> { new(x0, z0), new(x1, z0), new(x1, z1), new(x0, z1) };
    }

    // Keeps the part of a convex polygon where dot(normal, p) <= limit.
    private static List<Vector2> Clip(List<Vector2> polygon, Vector2 normal, float limit)
    {
        List<Vector2> result = new();
        for (int i = 0; i < polygon.Count; i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[(i + 1) % polygon.Count];
            float da = Vector2.Dot(normal, a) - limit;
            float db = Vector2.Dot(normal, b) - limit;
            if (da <= Epsilon)
            {
                result.Add(a);
            }

            if ((da < -Epsilon && db > Epsilon) || (da > Epsilon && db < -Epsilon))
            {
                result.Add(a + (b - a) * (da / (da - db)));
            }
        }

        // Snap points created on the clip line exactly onto it, so later boundary tests find them.
        for (int i = 0; i < result.Count; i++)
        {
            float d = Vector2.Dot(normal, result[i]) - limit;
            if (Mathf.Abs(d) < Epsilon * 10f && Mathf.Abs(d) > 0f)
            {
                result[i] -= normal * (d / normal.sqrMagnitude);
            }
        }

        return result;
    }

    // Whether a point lies inside a convex polygon: on the same side of every edge.
    private static bool Contains(List<Vector2> polygon, Vector2 point)
    {
        bool? sign = null;
        for (int i = 0; i < polygon.Count; i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[(i + 1) % polygon.Count];
            float cross = (b.x - a.x) * (point.y - a.y) - (b.y - a.y) * (point.x - a.x);
            if (Mathf.Abs(cross) < Epsilon)
            {
                continue;
            }

            bool positive = cross > 0f;
            if (sign.HasValue && sign.Value != positive)
            {
                return false;
            }

            sign = positive;
        }

        return true;
    }

    private static Vector2 Centroid(List<Vector2> polygon)
    {
        Vector2 sum = Vector2.zero;
        foreach (Vector2 p in polygon)
        {
            sum += p;
        }

        return sum / Mathf.Max(1, polygon.Count);
    }

    // A plane over part of the footprint: height = H0 + dot(Gradient, p), with p = (x, z).
    private sealed class Facet
    {
        public Facet(float h0, Vector2 gradient, float eaveW)
        {
            H0 = h0;
            Gradient = gradient;
            EaveW = eaveW;
            Polygon = new List<Vector2>();
        }

        public float H0 { get; }

        public Vector2 Gradient { get; }

        // The up-slope coordinate dot(p, Up) of the footprint's eave, where the courses start.
        public float EaveW { get; }

        public List<Vector2> Polygon { get; private set; }

        public Vector2 Up => Gradient.normalized;

        public Vector3 Normal => new Vector3(-Gradient.x, 1f, -Gradient.y).normalized;

        public float Height(Vector2 p)
        {
            return H0 + Vector2.Dot(Gradient, p);
        }

        public Facet With(List<Vector2> polygon)
        {
            return new Facet(H0, Gradient, EaveW) { Polygon = polygon };
        }
    }

    // An outer edge of the piece (or of its smoke hole): the line z = Value (AlongX) or x = Value, from Min to Max.
    private readonly struct Boundary
    {
        public Boundary(bool alongX, float value, float min, float max, Vector3 outward)
        {
            AlongX = alongX;
            Value = value;
            Min = min;
            Max = max;
            Outward = outward;
        }

        public bool AlongX { get; }

        public float Value { get; }

        public float Min { get; }

        public float Max { get; }

        public Vector3 Outward { get; }
    }

    private sealed class MeshParts
    {
        private readonly List<Vector3> vertices = new();
        private readonly List<Vector3> normals = new();
        private readonly List<Vector2> uvs = new();
        private readonly List<int>[] triangles = { new(), new(), new(), new() };

        // Adds a flat convex polygon, wound so it faces along the wanted normal.
        public void AddPolygon(int material, List<Vector3> points, List<Vector2> uv, Vector3 facing)
        {
            if (points.Count < 3)
            {
                return;
            }

            Vector3 normal = Vector3.zero;
            for (int i = 1; i + 1 < points.Count; i++)
            {
                normal += Vector3.Cross(points[i] - points[0], points[i + 1] - points[0]);
            }

            if (normal.sqrMagnitude < 1e-10f)
            {
                return;
            }

            bool flip = Vector3.Dot(normal, facing) < 0f;
            normal = (flip ? -normal : normal).normalized;
            int start = vertices.Count;
            for (int i = 0; i < points.Count; i++)
            {
                vertices.Add(points[i]);
                normals.Add(normal);
                uvs.Add(uv[i]);
            }

            for (int i = 1; i + 1 < points.Count; i++)
            {
                if (flip)
                {
                    triangles[material].Add(start);
                    triangles[material].Add(start + i + 1);
                    triangles[material].Add(start + i);
                }
                else
                {
                    triangles[material].Add(start);
                    triangles[material].Add(start + i);
                    triangles[material].Add(start + i + 1);
                }
            }
        }

        // An oriented box from its centre and three half-axes.
        public void AddBox(Vector3 center, Vector3 halfX, Vector3 halfY, Vector3 halfZ, int material, float tile)
        {
            Vector3[] axes = { halfX, halfY, halfZ };
            for (int axis = 0; axis < 3; axis++)
            {
                Vector3 n = axes[axis];
                Vector3 u = axes[(axis + 1) % 3];
                Vector3 v = axes[(axis + 2) % 3];
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    Vector3 c = center + n * sign;
                    List<Vector3> quad = new() { c - u - v, c + u - v, c + u + v, c - u + v };
                    float lu = u.magnitude * 2f / tile;
                    float lv = v.magnitude * 2f / tile;
                    List<Vector2> uv = new() { new(0f, 0f), new(lu, 0f), new(lu, lv), new(0f, lv) };
                    AddPolygon(material, quad, uv, n * sign);
                }
            }
        }

        // A cylinder from a to b, open at the ends' caps closed with flat polygons.
        public void AddCylinder(int material, Vector3 a, Vector3 b, float radius, int sides, float tile)
        {
            Vector3 axis = (b - a).normalized;
            Vector3 side = Vector3.Cross(axis, Mathf.Abs(axis.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
            Vector3 other = Vector3.Cross(axis, side).normalized;
            float length = (b - a).magnitude;
            List<Vector3> capA = new();
            List<Vector3> capB = new();
            List<Vector2> capUv = new();
            for (int i = 0; i < sides; i++)
            {
                float angle0 = i * Mathf.PI * 2f / sides;
                float angle1 = (i + 1) * Mathf.PI * 2f / sides;
                Vector3 r0 = (side * Mathf.Cos(angle0) + other * Mathf.Sin(angle0)) * radius;
                Vector3 r1 = (side * Mathf.Cos(angle1) + other * Mathf.Sin(angle1)) * radius;
                float v0 = i * radius * Mathf.PI * 2f / sides / tile;
                float v1 = (i + 1) * radius * Mathf.PI * 2f / sides / tile;
                AddPolygon(
                    material,
                    new List<Vector3> { a + r0, b + r0, b + r1, a + r1 },
                    new List<Vector2> { new(0f, v0), new(length / tile, v0), new(length / tile, v1), new(0f, v1) },
                    (r0 + r1).normalized);
                capA.Add(a + r0);
                capB.Add(b + r0);
                capUv.Add(new Vector2(Mathf.Cos(angle0), Mathf.Sin(angle0)) * radius / tile);
            }

            AddPolygon(material, capA, capUv, -axis);
            AddPolygon(material, capB, capUv, axis);
        }

        // A tube from a to b, narrowing from one radius to the other, its ends closed.
        public void AddTaperedTube(int material, Vector3 a, Vector3 b, float radiusA, float radiusB, int sides, float tile)
        {
            Vector3 axis = (b - a).normalized;
            Vector3 side = Vector3.Cross(axis, Mathf.Abs(axis.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
            Vector3 other = Vector3.Cross(axis, side).normalized;
            float length = (b - a).magnitude;
            List<Vector3> capA = new();
            List<Vector3> capB = new();
            List<Vector2> capUvA = new();
            List<Vector2> capUvB = new();
            for (int i = 0; i < sides; i++)
            {
                float angle0 = i * Mathf.PI * 2f / sides;
                float angle1 = (i + 1) * Mathf.PI * 2f / sides;
                Vector3 d0 = side * Mathf.Cos(angle0) + other * Mathf.Sin(angle0);
                Vector3 d1 = side * Mathf.Cos(angle1) + other * Mathf.Sin(angle1);
                float v0 = i * radiusA * Mathf.PI * 2f / sides / tile;
                float v1 = (i + 1) * radiusA * Mathf.PI * 2f / sides / tile;
                AddPolygon(
                    material,
                    new List<Vector3> { a + d0 * radiusA, b + d0 * radiusB, b + d1 * radiusB, a + d1 * radiusA },
                    new List<Vector2> { new(0f, v0), new(length / tile, v0), new(length / tile, v1), new(0f, v1) },
                    (d0 + d1).normalized);
                capA.Add(a + d0 * radiusA);
                capB.Add(b + d0 * radiusB);
                capUvA.Add(new Vector2(Mathf.Cos(angle0), Mathf.Sin(angle0)) * radiusA / tile);
                capUvB.Add(new Vector2(Mathf.Cos(angle0), Mathf.Sin(angle0)) * radiusB / tile);
            }

            AddPolygon(material, capA, capUvA, -axis);
            AddPolygon(material, capB, capUvB, axis);
        }

        /// <summary>
        /// The collected vertices as a mesh: one sub-mesh per material (underside, top, trim and so on), or one for
        /// all.
        /// </summary>
        public Mesh ToMesh(string name, int subMeshes = 4)
        {
            Mesh mesh = new() { name = name };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            if (subMeshes == 1)
            {
                List<int> all = new();
                foreach (List<int> list in triangles)
                {
                    all.AddRange(list);
                }

                mesh.subMeshCount = 1;
                mesh.SetTriangles(all, 0);
            }
            else
            {
                mesh.subMeshCount = triangles.Length;
                for (int i = 0; i < triangles.Length; i++)
                {
                    mesh.SetTriangles(triangles[i], i);
                }
            }

            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }
    }
}
