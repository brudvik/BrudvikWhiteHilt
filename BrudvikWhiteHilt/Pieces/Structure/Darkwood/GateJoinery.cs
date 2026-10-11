using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Structure.Darkwood;

/// <summary>The vanilla darkwood gate's models a darkwood piece is cut from, each with its material.</summary>
public enum GateSlot
{
    /// <summary>The leaf, near.</summary>
    Leaf,

    /// <summary>The leaf, far.</summary>
    LeafLow,

    /// <summary>The frame, new, near.</summary>
    New,

    /// <summary>The frame, new, far.</summary>
    NewLow,

    /// <summary>The frame, worn, near.</summary>
    Worn,

    /// <summary>The frame, worn, far.</summary>
    WornLow
}

/// <summary>What a darkwood piece is.</summary>
public enum JoineryKind
{
    /// <summary>A door: a post, one leaf with three straight panels, and a lintel.</summary>
    Door,

    /// <summary>A wall: a post and a fixed leaf without iron under a lintel, so it lines up with the door.</summary>
    Wall,

    /// <summary>A gable wall: a wall cut under a beam that slopes like a roof.</summary>
    Gable,

    /// <summary>Two vanilla gates as one piece, the left one mirrored.</summary>
    DoubleGate
}

/// <summary>
/// The models of the vanilla darkwood gate (darkwood_gate): its leaf in the leaf's own space, its frame in the piece's
/// space, and where the leaf hangs.
/// </summary>
public sealed class GateSource
{
    /// <summary>The meshes by model.</summary>
    public readonly Dictionary<GateSlot, MeshData> Meshes = new();

    /// <summary>The leaf's hinge pivot (the vanilla "door" object) in the piece's space.</summary>
    public Vector3 DoorPivot = new(0.686f, 0f, 0f);

    /// <summary>Where the leaf's model sits from its pivot.</summary>
    public Vector3 LeafOffset = new(-0.701f, 0f, 0f);
}

/// <summary>A mesh of a darkwood piece and the gate model whose material it takes.</summary>
public sealed class JoineryPart
{
    /// <summary>Creates a part.</summary>
    /// <param name="slot">The gate model whose material the part takes.</param>
    /// <param name="mesh">The mesh.</param>
    public JoineryPart(GateSlot slot, MeshData mesh)
    {
        Slot = slot;
        Mesh = mesh;
    }

    /// <summary>The gate model whose material the part takes.</summary>
    public GateSlot Slot { get; }

    /// <summary>The mesh.</summary>
    public MeshData Mesh { get; }
}

/// <summary>A leaf that opens: its pivot, its models in the pivot's space and its collider.</summary>
public sealed class JoineryLeaf
{
    /// <summary>The hinge, in the piece's space.</summary>
    public Vector3 Pivot;

    /// <summary>Whether the leaf turns the other way from the vanilla door.</summary>
    public bool Mirrored;

    /// <summary>Near models.</summary>
    public readonly List<JoineryPart> High = new();

    /// <summary>Far models.</summary>
    public readonly List<JoineryPart> Low = new();

    /// <summary>Collider centre, in the pivot's space.</summary>
    public Vector3 BoxCenter;

    /// <summary>Collider size.</summary>
    public Vector3 BoxSize;
}

/// <summary>A box collider of a darkwood piece.</summary>
public sealed class JoineryBox
{
    /// <summary>Centre, in the piece's space.</summary>
    public Vector3 Center;

    /// <summary>Size.</summary>
    public Vector3 Size;
}

/// <summary>Everything that makes a darkwood piece: its models in each wear state, leaves, colliders and snap points.</summary>
public sealed class JoineryModel
{
    /// <summary>Models that do not move, by wear state and detail: "New", "NewLow", "Worn", "WornLow".</summary>
    public readonly Dictionary<string, List<JoineryPart>> Static = new()
    {
        ["New"] = new List<JoineryPart>(),
        ["NewLow"] = new List<JoineryPart>(),
        ["Worn"] = new List<JoineryPart>(),
        ["WornLow"] = new List<JoineryPart>()
    };

    /// <summary>The leaves that open.</summary>
    public readonly List<JoineryLeaf> Leaves = new();

    /// <summary>Box colliders of the parts that do not move.</summary>
    public readonly List<JoineryBox> Boxes = new();

    /// <summary>For a gable: the corners of its triangle (x, y), made into a convex collider; otherwise null.</summary>
    public Vector2[] Prism;

    /// <summary>Snap points.</summary>
    public readonly List<Vector3> SnapPoints = new();
}

/// <summary>
/// Cuts the vanilla darkwood gate into the darkwood doors, walls, gable walls and the double gate. The leaf is cut in
/// bands at its rails: rails keep their size, panels are copies of the gate panel nearest in height, stretched to share
/// the leaf equally, and the iron (hinges and ring) is moved whole with the rail it sits on. The gate's post keeps
/// its section and is cut to height; a piece of it laid on its side is the lintel, or the sloping beam of a gable.
/// The numbers below are measured on the gate's models (in the leaf's own space); AssetSource/Unity/DarkwoodPreview.cs
/// renders the result with this same code.
/// </summary>
public static class GateJoinery
{
    /// <summary>Height of the lintel over a door and a wall, as the gate's top beam.</summary>
    public const float Lintel = 0.24f;

    /// <summary>Depth of the post and the lintel, front to back.</summary>
    public const float FrameDepth = 0.37f;

    // The flat fronts of the rails (inside their 2 cm bevels). The bottom rail starts just under the leaf, so it keeps
    // its end face. A panel runs from bevel to bevel, so the boards under a bevel belong to the panel beside it.
    private static readonly Vector2 bottomRail = new(-0.02f, 0.237f);
    private static readonly Vector2 lowerRail = new(0.856f, 1.178f);
    private static readonly Vector2 upperRail = new(2.064f, 2.385f);
    private static readonly Vector2[] panels = { new(bottomRail.y, lowerRail.x), new(lowerRail.y, upperRail.x) };

    // Iron on the leaf and the frame lies in this window of the gate's texture; a loose part with a quarter of its
    // corners there is iron (a hinge, its knuckle, a bolt, the ring), anything else is wood.
    private static readonly Rect ironWindow = Rect.MinMaxRect(0f, 0.69f, 0.15f, 0.92f);

    // How far from a rail a piece of iron may reach and still go with it (the hinge knuckles and the ring hang over).
    private const float IronReach = 0.15f;

    // The leaf's wood across, stile to stile, and the slits between its boards closed by a board behind each panel.
    private const float LeafLeft = -1.006f;
    private const float LeafRight = 0.6665f;
    private const float BackingLeft = -0.75f;
    private const float BackingRight = 0.5f;
    private const float LeafDepth = 0.3f;

    // The post's inner face and the leaf's stile leave 4 mm between them in the gate; the post moves in to close it.
    private const float PostShift = -0.0039f;
    private const float PostMinX = 0.6f;
    private const float PostTop = 3.775f;

    // Where the gate's post may be cut for the lintel: a piece with its bottom end and one with its top end, so both
    // ends of the lintel are whole.
    private const float LintelCutLow = 1.0f;
    private const float LintelCutHigh = 3.0f;

    // The post's edges are bevelled, so the lintel and the gable beam reach this far over what they rest on.
    private const float Overlap = 0.03f;

    /// <summary>
    /// How many panels a leaf of a piece this high has: three from 3 m, two from 2 m, else one.
    /// </summary>
    /// <param name="height">Height of the piece.</param>
    /// <returns>Number of panels.</returns>
    public static int PanelCount(float height)
    {
        return height >= 2.9f ? 3 : height >= 1.9f ? 2 : 1;
    }

    /// <summary>
    /// Lays out a leaf: bottom rail, panels with the middle rails between them, and a top rail.
    /// </summary>
    /// <param name="height">Height of the leaf.</param>
    /// <param name="count">Number of panels, 1 to 3.</param>
    /// <param name="topRail">Whether the leaf ends in a rail (not on a gable, whose beam closes it).</param>
    /// <returns>The plan.</returns>
    public static LeafPlan PlanLeaf(float height, int count, bool topRail = true)
    {
        count = Mathf.Clamp(count, 1, 3);
        Vector2[] middle = count == 3 ? new[] { lowerRail, upperRail } : count == 2 ? new[] { lowerRail } : new Vector2[0];
        float rails = Span(bottomRail) + middle.Sum(Span) + (topRail ? Span(bottomRail) : 0f);
        float panel = (height - rails) / count;
        if (panel <= 0.05f)
        {
            throw new ArgumentException($"A leaf of {height} m has no room for {count} panels.");
        }

        Vector2 source = panels.OrderBy(candidate => Mathf.Abs(Mathf.Log(panel / Span(candidate)))).First();
        LeafPlan plan = new() { Height = height, Panel = panel, Source = source };
        float y = 0f;
        plan.Segments.Add(new LeafSegment(bottomRail, y, y += Span(bottomRail), false, false));
        for (int i = 0; i < count; i++)
        {
            plan.Segments.Add(new LeafSegment(source, y, y += panel, true, false));
            if (i < middle.Length)
            {
                plan.Segments.Add(new LeafSegment(middle[i], y, y += Span(middle[i]), false, false));
            }
        }

        if (topRail)
        {
            plan.Segments.Add(new LeafSegment(bottomRail, y, y += Span(bottomRail), false, true));
        }

        return plan;
    }

    /// <summary>
    /// Builds a darkwood piece. Without a source (a dedicated server draws nothing) only the colliders and snap points
    /// are made.
    /// </summary>
    /// <param name="kind">What to build.</param>
    /// <param name="height">Height: of the door or wall, or the rise of a gable over its 2 m.</param>
    /// <param name="source">The gate's models, or null.</param>
    /// <returns>The piece.</returns>
    public static JoineryModel Build(JoineryKind kind, float height, GateSource source)
    {
        JoineryModel model = new();
        switch (kind)
        {
            case JoineryKind.Door:
                BuildDoor(model, height, source);
                break;
            case JoineryKind.Wall:
                BuildWall(model, height, source);
                break;
            case JoineryKind.Gable:
                BuildGable(model, height, source);
                break;
            case JoineryKind.DoubleGate:
                BuildDoubleGate(model, source);
                break;
        }

        return model;
    }

    /// <summary>
    /// Cuts a leaf to a plan, in the leaf's own space.
    /// </summary>
    /// <param name="leaf">The gate's leaf.</param>
    /// <param name="plan">The plan.</param>
    /// <param name="withIron">Whether the hinges and the ring come along (a door), or not (a wall).</param>
    /// <returns>The leaf.</returns>
    public static MeshData CutLeaf(MeshData leaf, LeafPlan plan, bool withIron)
    {
        SplitIron(leaf, out MeshData wood, out List<MeshData> iron);
        Vector2? boardUv = BoardUv(wood, plan.Source);
        List<MeshData> parts = new();
        foreach (LeafSegment segment in plan.Segments)
        {
            MeshData band = wood.Band(1, segment.Source.x, segment.Source.y);
            parts.Add(segment.Flipped
                ? band.Remapped(1, segment.Source.x, segment.Source.y, segment.To, segment.From)
                : band.Remapped(1, segment.Source.x, segment.Source.y, segment.From, segment.To));
            if (segment.IsPanel && boardUv.HasValue)
            {
                parts.Add(Backing(segment.From, segment.To, boardUv.Value));
            }
        }

        if (withIron)
        {
            parts.AddRange(MoveWithRails(iron, plan, Vector3.zero));
        }

        return MeshData.Combine(parts);
    }

    // A door: the post and the lintel stand still; the leaf hangs from the vanilla door's pivot.
    private static void BuildDoor(JoineryModel model, float height, GateSource source)
    {
        float top = height - Lintel;
        LeafPlan plan = PlanLeaf(top, PanelCount(height));
        AddFrameBoxes(model, top, 2f);
        AddSideSnaps(model, 1f, height);

        JoineryLeaf leaf = new()
        {
            Pivot = source?.DoorPivot ?? new GateSource().DoorPivot,
            BoxSize = new Vector3(LeafRight - LeafLeft, top, LeafDepth)
        };
        Vector3 offset = source?.LeafOffset ?? new GateSource().LeafOffset;
        leaf.BoxCenter = new Vector3((LeafLeft + LeafRight) / 2f + offset.x, top / 2f, 0f);
        model.Leaves.Add(leaf);
        if (source == null)
        {
            return;
        }

        leaf.High.Add(new JoineryPart(GateSlot.Leaf, CutLeaf(source.Meshes[GateSlot.Leaf], plan, true).Translated(offset)));
        leaf.Low.Add(new JoineryPart(GateSlot.LeafLow, CutLeaf(source.Meshes[GateSlot.LeafLow], plan, true).Translated(offset)));
        AddFrame(model, source, plan, top, true);
    }

    // A wall: as a door that does not open and has no iron, so its rails line up with the door beside it.
    private static void BuildWall(JoineryModel model, float height, GateSource source)
    {
        float top = height - Lintel;
        LeafPlan plan = PlanLeaf(top, PanelCount(height));
        model.Boxes.Add(new JoineryBox { Center = new Vector3(0f, height / 2f, 0f), Size = new Vector3(2f, height, FrameDepth) });
        AddSideSnaps(model, 1f, height);
        if (source == null)
        {
            return;
        }

        Vector3 offset = source.DoorPivot + source.LeafOffset;
        AddFrame(model, source, plan, top, false);
        model.Static["New"].Add(new JoineryPart(GateSlot.Leaf, CutLeaf(source.Meshes[GateSlot.Leaf], plan, false).Translated(offset)));
        model.Static["NewLow"].Add(new JoineryPart(GateSlot.LeafLow, CutLeaf(source.Meshes[GateSlot.LeafLow], plan, false).Translated(offset)));
        model.Static["Worn"].Add(model.Static["New"].Last());
        model.Static["WornLow"].Add(model.Static["NewLow"].Last());
    }

    // A gable wall: a wall of the gable's height without a top rail or lintel, cut under a sloping beam that rises
    // from the left foot (-1, 0) to the top of the post (1, rise), as the vanilla wooden gable walls.
    private static void BuildGable(JoineryModel model, float rise, GateSource source)
    {
        model.Prism = new[] { new Vector2(-1f, 0f), new Vector2(1f, 0f), new Vector2(1f, rise) };
        model.SnapPoints.Add(new Vector3(-1f, 0f, 0f));
        for (int k = 0; k <= Mathf.RoundToInt(rise); k++)
        {
            model.SnapPoints.Add(new Vector3(1f, k, 0f));
        }

        if (source == null)
        {
            return;
        }

        LeafPlan plan = PlanLeaf(rise, PanelCount(rise), false);
        Vector2 start = new(-1f, 0f), end = new(1f, rise);
        Vector2 along = (end - start).normalized;
        Vector3 up = new(-along.y, along.x, 0f);
        Vector3 offset = source.DoorPivot + source.LeafOffset;
        float below = -Vector3.Dot(up, (Vector3)start) + Lintel - Overlap;

        foreach ((string holder, GateSlot frame, GateSlot leafSlot) in new[]
        {
            ("New", GateSlot.New, GateSlot.Leaf), ("NewLow", GateSlot.NewLow, GateSlot.LeafLow),
            ("Worn", GateSlot.Worn, GateSlot.Leaf), ("WornLow", GateSlot.WornLow, GateSlot.LeafLow)
        })
        {
            MeshData post = Post(source.Meshes[frame]);
            MeshData body = post.Band(1, -0.02f, PostTop).Remapped(1, -0.02f, PostTop, -0.02f, rise).Translated(new Vector3(PostShift, 0f, 0f));
            MeshData leaf = CutLeaf(source.Meshes[leafSlot], plan, false).Translated(offset);
            MeshData beam = LayDown(post, start, end, Lintel).Clip(Vector3.right, -1f).Clip(Vector3.left, -1f).Clip(Vector3.up, 0f);
            model.Static[holder].Add(new JoineryPart(leafSlot, leaf.Clip(-up, below)));
            model.Static[holder].Add(new JoineryPart(frame, MeshData.Combine(new[] { body.Clip(-up, below), beam })));
        }
    }

    // The vanilla gate twice: as it is, moved right, and mirrored, moved left. The vanilla door stays as the driver.
    private static void BuildDoubleGate(JoineryModel model, GateSource source)
    {
        foreach (float x in new[] { -2f, -1f, 0f, 1f, 2f })
        {
            model.SnapPoints.Add(new Vector3(x, 0f, 0f));
            model.SnapPoints.Add(new Vector3(x, 4f, 0f));
        }

        for (int k = 1; k < 4; k++)
        {
            model.SnapPoints.Add(new Vector3(-2f, k, 0f));
            model.SnapPoints.Add(new Vector3(2f, k, 0f));
        }

        GateSource pivots = source ?? new GateSource();
        Vector3 shift = new(1f, 0f, 0f);
        float leafCenter = (LeafLeft + LeafRight) / 2f + pivots.LeafOffset.x;
        Vector3 boxSize = new(LeafRight - LeafLeft, 3.78f, LeafDepth);
        JoineryLeaf right = new() { Pivot = pivots.DoorPivot + shift, BoxCenter = new Vector3(leafCenter, 1.89f, 0f), BoxSize = boxSize };
        JoineryLeaf left = new()
        {
            Pivot = new Vector3(-pivots.DoorPivot.x, pivots.DoorPivot.y, pivots.DoorPivot.z) - shift,
            Mirrored = true,
            BoxCenter = new Vector3(-leafCenter, 1.89f, 0f),
            BoxSize = boxSize
        };
        model.Leaves.Add(right);
        model.Leaves.Add(left);
        if (source == null)
        {
            return;
        }

        foreach ((string holder, GateSlot slot) in new[] { ("New", GateSlot.New), ("NewLow", GateSlot.NewLow), ("Worn", GateSlot.Worn), ("WornLow", GateSlot.WornLow) })
        {
            MeshData frame = source.Meshes[slot];
            model.Static[holder].Add(new JoineryPart(slot, MeshData.Combine(new[] { frame.Translated(shift), frame.MirroredX().Translated(-shift) })));
        }

        foreach ((GateSlot slot, bool high) in new[] { (GateSlot.Leaf, true), (GateSlot.LeafLow, false) })
        {
            MeshData leaf = source.Meshes[slot].Translated(source.LeafOffset);
            (high ? right.High : right.Low).Add(new JoineryPart(slot, leaf));
            (high ? left.High : left.Low).Add(new JoineryPart(slot, leaf.MirroredX()));
        }
    }

    // The post and the lintel in every wear state, and for a door the hinge leaves on the post.
    private static void AddFrame(JoineryModel model, GateSource source, LeafPlan plan, float top, bool withIron)
    {
        foreach ((string holder, GateSlot slot) in new[] { ("New", GateSlot.New), ("NewLow", GateSlot.NewLow), ("Worn", GateSlot.Worn), ("WornLow", GateSlot.WornLow) })
        {
            MeshData frame = source.Meshes[slot];
            MeshData post = Post(frame);
            List<MeshData> parts = new()
            {
                post.Band(1, -0.02f, PostTop).Remapped(1, -0.02f, PostTop, -0.02f, top).Translated(new Vector3(PostShift, 0f, 0f)),
                LayDown(post, new Vector2(-1f, top + Lintel), new Vector2(1f, top + Lintel), Lintel + Overlap)
            };
            if (withIron)
            {
                SplitIron(frame, out _, out List<MeshData> iron);
                parts.AddRange(MoveWithRails(iron, plan, new Vector3(PostShift, 0f, 0f)));
            }

            model.Static[holder].Add(new JoineryPart(slot, MeshData.Combine(parts)));
        }
    }

    private static void AddFrameBoxes(JoineryModel model, float top, float width)
    {
        model.Boxes.Add(new JoineryBox { Center = new Vector3(0.835f + PostShift, top / 2f, 0f), Size = new Vector3(0.33f, top, FrameDepth) });
        model.Boxes.Add(new JoineryBox { Center = new Vector3(0f, top + Lintel / 2f, 0f), Size = new Vector3(width, Lintel, FrameDepth) });
    }

    // A snap point every metre up both sides.
    private static void AddSideSnaps(JoineryModel model, float halfWidth, float height)
    {
        for (int k = 0; k <= Mathf.RoundToInt(height); k++)
        {
            model.SnapPoints.Add(new Vector3(-halfWidth, k, 0f));
            model.SnapPoints.Add(new Vector3(halfWidth, k, 0f));
        }
    }

    // The gate's post: the largest loose part of wood standing right of the leaf.
    private static MeshData Post(MeshData frame)
    {
        SplitIron(frame, out MeshData wood, out _);
        return wood.Islands()
            .Where(island => island.Range(0, out float min, out _) && min >= PostMinX)
            .OrderByDescending(island => island.TriangleCount)
            .FirstOrDefault() ?? throw new InvalidOperationException("The darkwood gate's post was not found.");
    }

    // Lays the post on its side along a line, its outer face on the line and its inner face the thickness below it:
    // one piece with the post's bottom end at the start and one with its top end at the end, so the beam has whole ends.
    private static MeshData LayDown(MeshData post, Vector2 start, Vector2 end, float thickness)
    {
        post.Range(0, out float inner, out float outer);
        post.Range(1, out float bottom, out float topEnd);
        Vector2 middle = (start + end) / 2f;
        MeshData first = Lay(post.Band(1, bottom - 0.05f, LintelCutLow), bottom, LintelCutLow, start, middle, inner, outer, thickness);
        MeshData second = Lay(post.Band(1, LintelCutHigh, topEnd + 0.05f), LintelCutHigh, topEnd, middle, end, inner, outer, thickness);
        return MeshData.Combine(new[] { first, second });
    }

    // Maps the post's length (from..to) onto the line a..b and its section (inner..outer) onto the thickness below it.
    private static MeshData Lay(MeshData piece, float from, float to, Vector2 a, Vector2 b, float inner, float outer, float thickness)
    {
        Vector3 along = (Vector3)(b - a) / (to - from);
        Vector2 direction = (b - a).normalized;
        Vector3 up = new(-direction.y, direction.x, 0f);
        Vector3 across = up * (thickness / (outer - inner));
        Vector3 offset = (Vector3)a - along * from - across * outer;
        return piece.Transformed(across, along, Vector3.forward, offset);
    }

    // Splits a model into wood and the loose parts of iron.
    private static void SplitIron(MeshData mesh, out MeshData wood, out List<MeshData> iron)
    {
        List<MeshData> woodParts = new();
        iron = new List<MeshData>();
        foreach (MeshData island in mesh.Islands())
        {
            (IsIron(island) ? iron : woodParts).Add(island);
        }

        wood = MeshData.Combine(woodParts);
    }

    private static bool IsIron(MeshData island)
    {
        if (island.Uvs.Count != island.Vertices.Count || island.Vertices.Count == 0)
        {
            return false;
        }

        return island.Uvs.Count(uv => ironWindow.Contains(uv)) >= island.Uvs.Count / 4f;
    }

    // Moves each piece of iron whole with the middle rail it sits on; iron of a rail the plan does not use is left out.
    private static IEnumerable<MeshData> MoveWithRails(IEnumerable<MeshData> iron, LeafPlan plan, Vector3 extra)
    {
        foreach (MeshData part in iron)
        {
            part.Range(1, out float min, out float max);
            float middle = (min + max) / 2f;
            LeafSegment rail = plan.Segments.FirstOrDefault(segment => !segment.IsPanel && !segment.Flipped
                && segment.Source != bottomRail
                && middle >= segment.Source.x - IronReach && middle <= segment.Source.y + IronReach);
            if (rail != null)
            {
                yield return part.Translated(new Vector3(0f, rail.From - rail.Source.x, 0f) + extra);
            }
        }
    }

    // The texture coordinate of a point on a board of the source panel, or null when the model has no such board.
    private static Vector2? BoardUv(MeshData wood, Vector2 panel)
    {
        for (int t = 0; t < wood.TriangleCount; t++)
        {
            Vector3 centre = wood.Centroid(t);
            Vector2[] uvs = wood.TriangleUvs(t);
            if (uvs.Length == 3 && centre.y > panel.x + 0.1f && centre.y < panel.y - 0.1f && centre.x > -0.6f && centre.x < 0.3f
                && Mathf.Abs(wood.Vertices[wood.Triangles[t * 3]].z) < 0.117f
                && Mathf.Abs(wood.Vertices[wood.Triangles[t * 3 + 1]].z) < 0.117f
                && Mathf.Abs(wood.Vertices[wood.Triangles[t * 3 + 2]].z) < 0.117f)
            {
                return (uvs[0] + uvs[1] + uvs[2]) / 3f;
            }
        }

        return null;
    }

    // A thin board in the middle of a panel, seen from both sides, in one colour of the boards.
    private static MeshData Backing(float bottom, float top, Vector2 uv)
    {
        MeshData board = new();
        foreach (float side in new[] { -1f, 1f })
        {
            int start = board.Vertices.Count;
            board.Vertices.AddRange(new[]
            {
                new Vector3(BackingLeft, bottom, 0f), new Vector3(BackingRight, bottom, 0f),
                new Vector3(BackingRight, top, 0f), new Vector3(BackingLeft, top, 0f)
            });
            for (int i = 0; i < 4; i++)
            {
                board.Normals.Add(new Vector3(0f, 0f, side));
                board.Tangents.Add(new Vector4(-side, 0f, 0f, 1f));
                board.Uvs.Add(uv);
            }

            // Facing -z (the front) the corners run clockwise as seen from the front; facing +z the other way.
            int[] order = side < 0f ? new[] { 0, 2, 1, 0, 3, 2 } : new[] { 0, 1, 2, 0, 2, 3 };
            board.Triangles.AddRange(order.Select(index => start + index));
        }

        return board;
    }

    private static float Span(Vector2 range)
    {
        return range.y - range.x;
    }
}

/// <summary>A leaf laid out in bands, bottom to top.</summary>
public sealed class LeafPlan
{
    /// <summary>Height of the leaf.</summary>
    public float Height;

    /// <summary>Height of each panel.</summary>
    public float Panel;

    /// <summary>The gate panel the panels are copied from (bottom and top on the gate's leaf).</summary>
    public Vector2 Source;

    /// <summary>The bands, bottom to top.</summary>
    public readonly List<LeafSegment> Segments = new();
}

/// <summary>One band of a leaf: a stretch of the gate's leaf and where it goes.</summary>
public sealed class LeafSegment
{
    /// <summary>Creates a band.</summary>
    /// <param name="source">Bottom and top on the gate's leaf.</param>
    /// <param name="from">Bottom in the new leaf.</param>
    /// <param name="to">Top in the new leaf.</param>
    /// <param name="isPanel">Whether it is a panel (stretched) rather than a rail.</param>
    /// <param name="flipped">Whether it is turned upside down (the top rail is the bottom rail turned).</param>
    public LeafSegment(Vector2 source, float from, float to, bool isPanel, bool flipped)
    {
        Source = source;
        From = from;
        To = to;
        IsPanel = isPanel;
        Flipped = flipped;
    }

    /// <summary>Bottom and top on the gate's leaf.</summary>
    public Vector2 Source { get; }

    /// <summary>Bottom in the new leaf.</summary>
    public float From { get; }

    /// <summary>Top in the new leaf.</summary>
    public float To { get; }

    /// <summary>Whether it is a panel rather than a rail.</summary>
    public bool IsPanel { get; }

    /// <summary>Whether it is turned upside down.</summary>
    public bool Flipped { get; }
}
