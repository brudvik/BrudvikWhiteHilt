using System.Collections.Generic;
using System.Linq;
using BrudvikWhiteHilt.Pieces.Structure.Darkwood;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Structure.Planking;

/// <summary>
/// Builds wooden pieces out of the plank the vanilla wood wall is made of: a unit block with the grain along z, which
/// vanilla stretches into every board of its walls and floors. Boards are laid between two points and sawn to fit
/// (<see cref="Sawing"/>), so their ends are closed. The pieces stand in x (across) and y (up), facing z.
/// </summary>
public static class Joinery
{
    /// <summary>Width and depth of a lattice's frame.</summary>
    public const float Frame = 0.14f;

    /// <summary>Width of a lattice slat.</summary>
    public const float SlatWidth = 0.05f;

    /// <summary>Depth of a lattice slat; the slats one way lie in front of those the other way.</summary>
    public const float SlatDepth = 0.03f;

    /// <summary>Distance between lattice slats, square to them.</summary>
    public const float SlatSpacing = 0.3f;

    /// <summary>
    /// A board between two points: the plank stretched along the line, its width square to it in the xy plane and
    /// its depth along z.
    /// </summary>
    /// <param name="plank">The vanilla plank, a unit block with the grain along z.</param>
    /// <param name="from">One end of the board's middle line.</param>
    /// <param name="to">The other end.</param>
    /// <param name="width">Width.</param>
    /// <param name="depth">Depth, along z.</param>
    /// <returns>The board.</returns>
    public static MeshData Board(MeshData plank, Vector3 from, Vector3 to, float width, float depth)
    {
        Vector3 along = to - from;
        Vector3 across = Vector3.Cross(Vector3.forward, along.normalized);
        return plank.Transformed(Vector3.forward * depth, across * width, along, (from + to) / 2f);
    }

    /// <summary>
    /// A lattice: a frame round the edge and slats both ways at 45° inside it, the slats' ends in the frame.
    /// </summary>
    /// <param name="plank">The vanilla plank.</param>
    /// <param name="width">Width.</param>
    /// <param name="height">Height.</param>
    /// <returns>The lattice, centred on its origin.</returns>
    public static MeshData Lattice(MeshData plank, float width, float height)
    {
        float w = width / 2f, h = height / 2f, f = Frame / 2f;
        List<MeshData> parts = new()
        {
            Board(plank, new Vector3(-w, -h + f, 0f), new Vector3(w, -h + f, 0f), Frame, Frame),
            Board(plank, new Vector3(-w, h - f, 0f), new Vector3(w, h - f, 0f), Frame, Frame),
            Board(plank, new Vector3(-w + f, -h + Frame, 0f), new Vector3(-w + f, h - Frame, 0f), Frame, Frame),
            Board(plank, new Vector3(w - f, -h + Frame, 0f), new Vector3(w - f, h - Frame, 0f), Frame, Frame)
        };

        // The slats reach a little into the frame, so no slit shows at their ends.
        float inner = Frame - 0.01f;
        (Vector3, float)[] inside =
        {
            (Vector3.right, -w + inner), (Vector3.left, -w + inner), (Vector3.up, -h + inner), (Vector3.down, -h + inner)
        };
        float reach = w + h;
        foreach (float sign in new[] { 1f, -1f })
        {
            Vector3 direction = new Vector3(1f, sign, 0f).normalized;
            Vector3 normal = new Vector3(-sign, 1f, 0f).normalized;
            float z = sign * (SlatDepth / 2f + 0.001f);
            for (float offset = 0f; offset <= reach; offset += SlatSpacing)
            {
                foreach (float side in offset == 0f ? new[] { 0f } : new[] { offset, -offset })
                {
                    Vector3 centre = normal * side + Vector3.forward * z;
                    MeshData slat = Board(plank, centre - direction * reach, centre + direction * reach, SlatWidth, SlatDepth);
                    MeshData cut = Sawing.SawAll(slat, inside);
                    if (cut.TriangleCount > 0)
                    {
                        parts.Add(cut);
                    }
                }
            }
        }

        return MeshData.Combine(parts);
    }

    /// <summary>Width of a bargeboard, up from the roof line.</summary>
    public const float BargeWidth = 0.24f;

    /// <summary>Thickness of a bargeboard, as the dragon gable's boards.</summary>
    public const float BargeThickness = 0.06f;

    /// <summary>Height of the top of a vanilla floor's planks over its snap points.</summary>
    public const float FloorTop = 0.081f;

    /// <summary>
    /// A board between two points with its width along a given direction and its depth square to both.
    /// </summary>
    /// <param name="plank">The vanilla plank.</param>
    /// <param name="from">One end of the board's middle line.</param>
    /// <param name="to">The other end.</param>
    /// <param name="widthDirection">Which way its width lies, square to the line.</param>
    /// <param name="width">Width.</param>
    /// <param name="depth">Depth.</param>
    /// <returns>The board.</returns>
    public static MeshData Board(MeshData plank, Vector3 from, Vector3 to, Vector3 widthDirection, float width, float depth)
    {
        Vector3 along = to - from;
        Vector3 deep = Vector3.Cross(along.normalized, widthDirection.normalized);
        return plank.Transformed(deep * depth, widthDirection.normalized * width, along, (from + to) / 2f);
    }

    /// <summary>
    /// A bargeboard along the edge of a gable: along the roof line from its upper end (the origin) down the slope
    /// (towards +z), its lower edge on the roof line, as the dragon gable's boards, and its ends sawn plumb, so two meet
    /// at the ridge and the next one follows on down. It stands in the gable's plane, x = 0. A run of 1 m matches the
    /// half of a vanilla ridge piece, 2 m a vanilla roof piece.
    /// </summary>
    /// <param name="plank">The vanilla plank.</param>
    /// <param name="rise">How far the roof falls over 2 m: 1, 2 or 4 m for 26°, 45° and 67°.</param>
    /// <param name="run">How far it reaches out from its upper end.</param>
    /// <returns>The board.</returns>
    public static MeshData Bargeboard(MeshData plank, float rise, float run)
    {
        Vector3 foot = new Vector3(0f, -rise, 2f) * (run / 2f);
        Vector3 down = foot.normalized;
        Vector3 up = new Vector3(0f, 2f, rise).normalized;
        Vector3 middle = up * (BargeWidth / 2f);
        float reach = BargeWidth * rise;
        MeshData board = Board(plank, middle - down * reach, middle + foot + down * reach, up, BargeWidth, BargeThickness);
        return Sawing.SawAll(board, new[] { (Vector3.forward, 0f), (Vector3.back, -run) });
    }

    /// <summary>
    /// The bargeboard's collider: a box along it, turned about x from level to the roof's slope.
    /// </summary>
    /// <param name="rise">How far the roof falls over 2 m.</param>
    /// <param name="run">How far it reaches out.</param>
    /// <returns>Centre, size and the turn about x in degrees.</returns>
    public static (Vector3 Center, Vector3 Size, float Degrees) BargeBox(float rise, float run)
    {
        Vector3 up = new Vector3(0f, 2f, rise).normalized;
        float length = Mathf.Sqrt(4f + rise * rise) * run / 2f;
        return (new Vector3(0f, -rise / 2f, 1f) * (run / 2f) + up * (BargeWidth / 2f), new Vector3(BargeThickness, BargeWidth, length), Mathf.Atan2(rise, 2f) * Mathf.Rad2Deg);
    }

    /// <summary>
    /// The vanilla stair (2 m wide, rising 1 m over 2 m) twice, side by side.
    /// </summary>
    /// <param name="stair">The vanilla stair's model.</param>
    /// <returns>The wide stair.</returns>
    public static MeshData WideStair(MeshData stair)
    {
        return MeshData.Combine(new[] { stair.Translated(Vector3.left), stair.Translated(Vector3.right) });
    }

    /// <summary>Where the second flight of a stair with a landing stands from the first.</summary>
    public static readonly Vector3 SecondFlight = new(0f, 1f, -4f);

    /// <summary>
    /// Two vanilla stairs with a landing 2 × 2 m between them: up 1 m, 2 m level, up 1 m more. The landing is planked as
    /// the vanilla floor, its top level with the top of the first flight, on two joists.
    /// </summary>
    /// <param name="stair">The vanilla stair's model, from its foot (z = 1) up to its head (z = -1).</param>
    /// <param name="plank">The vanilla plank.</param>
    /// <returns>The stair.</returns>
    public static MeshData StairWithLanding(MeshData stair, MeshData plank)
    {
        List<MeshData> parts = new() { stair, stair.Translated(SecondFlight) };
        float top = 1f + FloorTop;
        for (int i = 0; i < 4; i++)
        {
            float x = -0.75f + 0.5f * i;
            parts.Add(Board(plank, new Vector3(x, top - 0.05f, -1f), new Vector3(x, top - 0.05f, -3f), Vector3.right, 0.495f, 0.1f));
        }

        foreach (float z in new[] { -1.3f, -2.7f })
        {
            parts.Add(Board(plank, new Vector3(-1f, top - 0.2f, z), new Vector3(1f, top - 0.2f, z), Vector3.forward, 0.15f, 0.2f));
        }

        return MeshData.Combine(parts);
    }

    /// <summary>
    /// How far the middle of a vanilla wood pole or beam lies from its face: half of its 0.4 m.
    /// </summary>
    public const float BraceInset = 0.2f;

    /// <summary>
    /// A brace at 45° under the corner where a post meets a beam. Its corner is where their middle lines meet (a pole's
    /// top, a beam's end), and it reaches a length down the post's face and out under the beam's, its ends sawn at the
    /// middle lines, inside the post and the beam.
    /// </summary>
    /// <param name="plank">The vanilla plank.</param>
    /// <param name="length">How far down the post and out under the beam it shows.</param>
    /// <param name="section">Width and depth.</param>
    /// <returns>The brace.</returns>
    public static MeshData Brace(MeshData plank, float length, float section)
    {
        float reach = length + BraceInset;
        Vector3 along = new Vector3(1f, 1f, 0f).normalized;
        Vector3 from = new Vector3(0f, -reach, 0f) - along * section, to = new Vector3(reach, 0f, 0f) + along * section;
        return Sawing.SawAll(Board(plank, from, to, section, section), new[] { (Vector3.right, 0f), (Vector3.down, 0f) });
    }

    /// <summary>
    /// The brace's collider: a box along its middle line, its height turned from up to the brace's slope.
    /// </summary>
    /// <param name="length">How far down and out it shows.</param>
    /// <param name="section">Width and depth.</param>
    /// <returns>Centre, size and the turn about z in degrees.</returns>
    public static (Vector3 Center, Vector3 Size, float Degrees) BraceBox(float length, float section)
    {
        float reach = length + BraceInset;
        return (new Vector3(reach / 2f, -reach / 2f, 0f), new Vector3(section, reach * Mathf.Sqrt(2f), section), -45f);
    }

    /// <summary>
    /// Snap points of a brace: the corner it stands in and the two ends of its middle line.
    /// </summary>
    /// <param name="length">How far down and out it shows.</param>
    /// <returns>The points.</returns>
    public static IEnumerable<Vector3> BraceSnapPoints(float length)
    {
        float reach = length + BraceInset;
        return new[] { Vector3.zero, new Vector3(0f, -reach, 0f), new Vector3(reach, 0f, 0f) };
    }
}
