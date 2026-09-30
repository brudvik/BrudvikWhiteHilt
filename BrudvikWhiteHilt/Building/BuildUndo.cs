using BrudvikWhiteHilt.Building.Groups;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Building;

/// <summary>
/// Remembers what the local player just built, tore down or moved, so the latest step can be taken back within the undo time.
/// A step is one piece or a whole group (paste, line, area, move or tear-down). Single pieces taken back can be redone.
/// </summary>
public static class BuildUndo
{
    private static readonly List<Step> steps = new();
    private static readonly List<PieceSnapshot> undone = new();

    private static Step group;
    private static bool restoring;

    private static int MaxSteps => Mathf.Max(1, BuildToolSettings.UndoSteps.Value);

    /// <summary>True while <see cref="Player.PlacePiece"/> runs for the local player.</summary>
    public static bool Capturing { get; set; }

    /// <summary>The piece the local player placed most recently, alive or not.</summary>
    public static Piece LastRecorded { get; private set; }

    /// <summary>When the latest step was recorded, or negative infinity.</summary>
    public static float LastStepTime => steps.Count > 0 ? steps[steps.Count - 1].Time : float.NegativeInfinity;

    /// <summary>
    /// The last piece the local player placed that still stands, or null.
    /// </summary>
    public static Piece LastPlaced
    {
        get
        {
            for (int i = steps.Count - 1; i >= 0; i--)
            {
                for (int j = steps[i].Placed.Count - 1; j >= 0; j--)
                {
                    if (steps[i].Placed[j] != null)
                    {
                        return steps[i].Placed[j];
                    }
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Starts collecting placed pieces into one step.
    /// </summary>
    /// <param name="free">True if the step cost nothing (a move), so undoing it gives nothing back.</param>
    /// <param name="removed">Pieces torn down in the same step, or null.</param>
    public static void BeginGroup(bool free, List<PieceSnapshot> removed)
    {
        group = new Step { Time = Time.time, Free = free, Removed = removed ?? new List<PieceSnapshot>() };
    }

    /// <summary>
    /// Ends the step started with <see cref="BeginGroup"/>.
    /// </summary>
    public static void EndGroup()
    {
        Step finished = group;
        group = null;
        if (!restoring && finished != null && (finished.Placed.Count > 0 || finished.Removed.Count > 0))
        {
            Add(finished);
            undone.Clear();
        }
    }

    /// <summary>
    /// Records pieces torn down as one step.
    /// </summary>
    /// <param name="removed">The torn down pieces.</param>
    public static void RecordRemoved(List<PieceSnapshot> removed)
    {
        if (removed.Count > 0)
        {
            Add(new Step { Time = Time.time, Removed = removed });
            undone.Clear();
        }
    }

    /// <summary>
    /// Records a piece the local player just placed. A new piece, other than a redo, clears what can be redone.
    /// </summary>
    /// <param name="piece">The new piece.</param>
    public static void Record(Piece piece)
    {
        LastRecorded = piece;
        if (group != null)
        {
            group.Placed.Add(piece);
            return;
        }

        if (restoring)
        {
            return;
        }

        if (!BuildPlacer.Redoing)
        {
            undone.Clear();
        }

        Step step = new() { Time = Time.time };
        step.Placed.Add(piece);
        Add(step);
    }

    /// <summary>
    /// Takes back the latest step within the undo time: placed pieces are torn down with a full refund, torn down pieces
    /// are put back (paid for again), and moved pieces go back where they were for free.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Undo(Player player)
    {
        float window = BuildToolSettings.UndoSeconds.Value;
        while (steps.Count > 0)
        {
            Step step = steps[steps.Count - 1];
            steps.RemoveAt(steps.Count - 1);
            if (window <= 0f || Time.time - step.Time > window)
            {
                steps.Clear();
                break;
            }

            List<Piece> alive = step.Placed.Where(piece => piece != null && piece.m_nview != null && piece.m_nview.IsValid()).ToList();
            if (alive.Count == 0 && step.Removed.Count == 0)
            {
                continue;
            }

            if (alive.Any(piece => !PrivateArea.CheckAccess(piece.transform.position)))
            {
                steps.Add(step);
                player.Message(MessageHud.MessageType.Center, "$msg_privatezone");
                return;
            }

            List<GroupPlacer.Item> restore = ToItems(step.Removed);
            string problem = restore.Count > 0 ? GroupPlacer.Check(player, restore, pay: !step.Free) : null;
            if (problem != null)
            {
                steps.Add(step);
                player.Message(MessageHud.MessageType.Center, problem);
                return;
            }

            if (step.Placed.Count == 1 && step.Removed.Count == 0)
            {
                undone.Add(PieceSnapshot.Of(alive[0]));
                while (undone.Count > MaxSteps)
                {
                    undone.RemoveAt(0);
                }
            }

            GroupPlacer.Remove(player, alive, step.Free ? GroupPlacer.Refund.None : GroupPlacer.Refund.Full);
            if (restore.Count > 0)
            {
                restoring = true;
                try
                {
                    GroupPlacer.Place(player, restore, pay: !step.Free);
                }
                finally
                {
                    restoring = false;
                }
            }

            player.Message(MessageHud.MessageType.TopLeft, "$msg_whitehilt_build_undone");
            return;
        }

        player.Message(MessageHud.MessageType.TopLeft, "$msg_whitehilt_build_undo_none");
    }

    /// <summary>
    /// Puts back the last single piece taken back, where it stood, if the player can still afford it.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Redo(Player player)
    {
        if (undone.Count == 0)
        {
            player.Message(MessageHud.MessageType.TopLeft, "$msg_whitehilt_build_redo_none");
            return;
        }

        PieceSnapshot snapshot = undone[undone.Count - 1];
        Piece piece = ZNetScene.instance.GetPrefab(snapshot.Prefab)?.GetComponent<Piece>();
        if (piece != null && BuildPlacer.TryPlace(player, piece, snapshot.Position, snapshot.Rotation, redo: true))
        {
            undone.RemoveAt(undone.Count - 1);
        }
    }

    private static List<GroupPlacer.Item> ToItems(List<PieceSnapshot> snapshots)
    {
        List<GroupPlacer.Item> items = new();
        foreach (PieceSnapshot snapshot in snapshots)
        {
            Piece piece = GroupPlacer.Resolve(snapshot.Prefab);
            if (piece != null)
            {
                items.Add(new GroupPlacer.Item { Piece = piece, Position = snapshot.Position, Rotation = snapshot.Rotation, Text = snapshot.Text });
            }
        }

        return items;
    }

    private static void Add(Step step)
    {
        steps.Add(step);
        while (steps.Count > MaxSteps)
        {
            steps.RemoveAt(0);
        }
    }

    private sealed class Step
    {
        public float Time;
        public bool Free;
        public List<Piece> Placed = new();
        public List<PieceSnapshot> Removed = new();
    }
}
