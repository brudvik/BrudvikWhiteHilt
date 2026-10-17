using System.Collections.Generic;
using System.Linq;
using BrudvikWhiteHilt.Pieces.Structure.Darkwood;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Structure.Planking;

/// <summary>
/// Makes plank walls out of the vanilla 2 × 2 m wood wall: laid side by side and on top of each other as one piece,
/// or with an opening sawn through it (<see cref="Sawing"/>). The wall stands in x (across) and y (up), centred on its
/// origin, as the vanilla wall does.
/// </summary>
public static class WallCutting
{
    /// <summary>The window opening in a 2 × 2 m wall: 1 m wide, from 0.5 m to 1.5 m above its foot.</summary>
    public static readonly Rect Window = Rect.MinMaxRect(-0.5f, -0.5f, 0.5f, 0.5f);

    /// <summary>
    /// Lays the vanilla wall as often as a wall of the given size needs.
    /// </summary>
    /// <param name="tile">The vanilla 2 × 2 m wall's model, centred on its origin.</param>
    /// <param name="width">Width in metres, a multiple of 2.</param>
    /// <param name="height">Height in metres, a multiple of 2.</param>
    /// <returns>The wall's model, centred on its origin.</returns>
    public static MeshData Lay(MeshData tile, int width, int height)
    {
        List<MeshData> laid = new();
        for (int i = 0; i < width / 2; i++)
        {
            for (int j = 0; j < height / 2; j++)
            {
                laid.Add(tile.Translated(new Vector3(-width / 2f + 1f + 2f * i, -height / 2f + 1f + 2f * j, 0f)));
            }
        }

        return MeshData.Combine(laid);
    }

    /// <summary>
    /// Saws an opening through a wall: each plank or batten across it is cut into the parts beside, under and over it.
    /// </summary>
    /// <param name="wall">The wall's model.</param>
    /// <param name="opening">The opening, in x and y.</param>
    /// <returns>The wall with the opening.</returns>
    public static MeshData CutOpening(MeshData wall, Rect opening)
    {
        List<MeshData> parts = new();
        foreach (MeshData plank in wall.Islands())
        {
            if (!Crosses(plank, 0, opening.xMin, opening.xMax) || !Crosses(plank, 1, opening.yMin, opening.yMax))
            {
                parts.Add(plank);
                continue;
            }

            // Beside the opening, full height; then under and over it, between its sides.
            parts.Add(Sawing.Saw(plank, Vector3.left, -opening.xMin));
            parts.Add(Sawing.Saw(plank, Vector3.right, opening.xMax));
            foreach ((Vector3 normal, float distance) in new[] { (Vector3.down, -opening.yMin), (Vector3.up, opening.yMax) })
            {
                MeshData part = Sawing.Saw(plank, normal, distance);
                part = part == null ? null : Sawing.Saw(part, Vector3.right, opening.xMin);
                parts.Add(part == null ? null : Sawing.Saw(part, Vector3.left, -opening.xMax));
            }
        }

        return MeshData.Combine(parts.Where(part => part != null));
    }

    /// <summary>
    /// Snap points round a wall's edge every 2 m, corners included, so a 2 m wall snaps to each stretch of it.
    /// </summary>
    /// <param name="width">Width in metres.</param>
    /// <param name="height">Height in metres.</param>
    /// <returns>The points.</returns>
    public static IEnumerable<Vector3> SnapPoints(int width, int height)
    {
        for (int i = 0; i <= width / 2; i++)
        {
            for (int j = 0; j <= height / 2; j++)
            {
                if (i == 0 || j == 0 || i == width / 2 || j == height / 2)
                {
                    yield return new Vector3(-width / 2f + 2f * i, -height / 2f + 2f * j, 0f);
                }
            }
        }
    }

    // Whether a part reaches into a stretch along an axis by more than the slack.
    private static bool Crosses(MeshData part, int axis, float min, float max)
    {
        part.Range(axis, out float low, out float high);
        return high > min + Sawing.Slack && low < max - Sawing.Slack;
    }
}
