using BrudvikWhiteHilt.Building.Terrain;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Building.Moats;

/// <summary>
/// The hoe's moat tool: click a wall to dig round it, or click points on the ground for a moat of your own line.
/// The ditch's edges are drawn on the ground before digging starts; <see cref="MoatBuilder"/> then digs it.
/// </summary>
public static class MoatTool
{
    private const float PreviewInterval = 0.3f;
    private const float JoinDistance = 1.5f;
    private const float WallClearance = 0.5f;

    private static readonly List<Vector3> points = new();
    private static List<MoatRun> wallRuns;
    private static float nextPreview;
    private static int pieceMask;

    /// <summary>Help for the tool, for the key hint.</summary>
    public static string HintLine { get; private set; } = string.Empty;

    /// <summary>
    /// Forgets the clicked points and the followed wall.
    /// </summary>
    public static void Clear()
    {
        points.Clear();
        wallRuns = null;
        HintLine = string.Empty;
        BuildGizmos.HidePaths();
    }

    /// <summary>
    /// Next profile.
    /// </summary>
    public static void CycleProfile()
    {
        MoatSettings.Profile = (MoatProfile)(((int)MoatSettings.Profile + 1) % 3);
        Replan();
    }

    /// <summary>
    /// Next place for the earth.
    /// </summary>
    public static void CycleBank()
    {
        MoatSettings.Bank = (MoatBank)(((int)MoatSettings.Bank + 1) % 3);
    }

    /// <summary>
    /// Causeways in front of gates on and off.
    /// </summary>
    public static void ToggleCauseways()
    {
        MoatSettings.Causeways = !MoatSettings.Causeways;
        Replan();
    }

    /// <summary>
    /// Runs the tool. Called every frame while the moat tool is on.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <returns>False if the tool should stop.</returns>
    public static bool Tick(Player player)
    {
        if (!MoatSettings.Enabled.Value)
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_moat_disabled");
            return false;
        }

        string profile = MoatSettings.ProfileName(MoatSettings.Profile);
        MoatSpec spec = MoatSpec.FromSettings(MoatSettings.Profile, MoatSettings.Bank);
        if (wallRuns != null)
        {
            HintLine = string.Format(Localization.instance.Localize("$whitehilt_moat_hint_wall"), profile, Mathf.RoundToInt(MoatGeometry.Length(wallRuns)),
                wallRuns.Count, CountCauseways(wallRuns));
            if (Time.unscaledTime >= nextPreview)
            {
                nextPreview = Time.unscaledTime + PreviewInterval;
                BuildGizmos.ShowPaths(MoatGeometry.Outline(wallRuns, spec.Width));
            }

            if (Clicked(0))
            {
                MoatBuilder.Start(player, WallSpec(spec), wallRuns);
                Clear();
            }
            else if (Clicked(1))
            {
                Clear();
            }

            return true;
        }

        bool aimed = AreaPicker.Aim(out Vector3 aim);
        if (points.Count == 0)
        {
            HintLine = string.Format(Localization.instance.Localize("$whitehilt_moat_hint_start"), profile);
            BuildGizmos.HidePaths();
            if (Clicked(0))
            {
                Piece wall = AimedPiece();
                if (wall != null)
                {
                    wallRuns = MoatGeometry.FollowWalls(wall, spec, MoatSettings.Causeways);
                    if (wallRuns.Count == 0)
                    {
                        wallRuns = null;
                        player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_moat_no_wall");
                    }

                    nextPreview = 0f;
                }
                else if (aimed)
                {
                    points.Add(aim);
                }
            }
            else if (Clicked(1))
            {
                return false;
            }

            return true;
        }

        List<Vector3> shown = new(points);
        if (aimed)
        {
            shown.Add(aim);
        }

        HintLine = string.Format(Localization.instance.Localize("$whitehilt_moat_hint_points"), profile, points.Count,
            Mathf.RoundToInt(Length(shown)));
        if (Time.unscaledTime >= nextPreview)
        {
            nextPreview = Time.unscaledTime + PreviewInterval;
            MoatRun preview = MoatGeometry.FromPoints(shown, false, spec, MoatSettings.Causeways);
            if (preview != null)
            {
                BuildGizmos.ShowPaths(MoatGeometry.Outline(new[] { preview }, spec.Width));
            }
        }

        if (Clicked(0) && aimed)
        {
            bool closing = points.Count >= 3 && Flat(aim, points[0]) <= JoinDistance;
            bool finishing = points.Count >= 2 && Flat(aim, points[points.Count - 1]) <= JoinDistance;
            if (closing || finishing)
            {
                MoatRun run = MoatGeometry.FromPoints(points, closing, spec, MoatSettings.Causeways);
                if (run != null)
                {
                    MoatBuilder.Start(player, spec, new List<MoatRun> { run });
                }

                Clear();
            }
            else
            {
                points.Add(aim);
            }
        }
        else if (Clicked(1))
        {
            points.RemoveAt(points.Count - 1);
            nextPreview = 0f;
        }

        return true;
    }

    private static void Replan()
    {
        nextPreview = 0f;
        wallRuns = null;
    }

    // A bank inside the ditch must stay on the berm, or it would bury the foot of the wall.
    private static MoatSpec WallSpec(MoatSpec spec)
    {
        if (spec.Bank == MoatBank.Inside)
        {
            spec.BankWidth = Mathf.Min(spec.BankWidth, MoatSettings.Berm.Value - WallClearance);
            if (spec.BankWidth < 1f)
            {
                spec.Bank = MoatBank.None;
            }
        }

        return spec;
    }

    // How many causeways the planned moat has: each run of samples with a factor of 0 counts once.
    private static int CountCauseways(List<MoatRun> runs)
    {
        int count = 0;
        foreach (MoatRun run in runs)
        {
            bool inGap = false;
            for (int i = 0; i < run.Count; i++)
            {
                bool gap = run.Factor[i] <= 0f;
                if (gap && !inGap)
                {
                    count++;
                }

                inGap = gap;
            }
        }

        return count;
    }

    // The piece under the crosshair, if it is nearer than the ground.
    private static Piece AimedPiece()
    {
        if (GameCamera.instance == null)
        {
            return null;
        }

        if (pieceMask == 0)
        {
            pieceMask = LayerMask.GetMask("piece", "piece_nonsolid", "Default", "static_solid", "Default_small", "terrain");
        }

        Transform view = GameCamera.instance.transform;
        float reach = BuildCamera.Active ? BuildToolSettings.MaxPlaceDistance.Value : TerrainSettings.AreaReach.Value;
        if (!Physics.Raycast(view.position, view.forward, out RaycastHit hit, reach, pieceMask))
        {
            return null;
        }

        Piece piece = hit.collider.GetComponentInParent<Piece>();
        return piece != null && piece.IsPlacedByPlayer() ? piece : null;
    }

    private static float Length(List<Vector3> path)
    {
        float length = 0f;
        for (int i = 1; i < path.Count; i++)
        {
            length += Flat(path[i - 1], path[i]);
        }

        return length;
    }

    private static float Flat(Vector3 a, Vector3 b)
    {
        return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
    }

    private static bool Clicked(int button)
    {
        return !BuildToolbar.CursorMode && Input.GetMouseButtonDown(button);
    }
}
