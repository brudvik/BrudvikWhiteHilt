using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Building.Terrain;

/// <summary>
/// Changes the terrain with the game's own terrain operations, run on the terrain pieces directly with any radius and
/// height, a few dozen per frame so large jobs do not stall the game. The player takes ownership of the terrain it
/// changes, as the terrain is saved and shared by its owner. Jobs remember the terrain before they ran, so they can be
/// taken back within the undo time.
/// </summary>
public static class TerrainEdit
{
    private static readonly Queue<Job> jobs = new();
    private static readonly HashSet<TerrainComp> dirty = new();
    private static readonly List<Heightmap> heightmaps = new();
    private static readonly List<Job> undoSteps = new();

    private static Job current;

    /// <summary>True while terrain jobs are running.</summary>
    public static bool Busy => current != null || jobs.Count > 0;

    /// <summary>When the latest undoable job finished, or negative infinity.</summary>
    public static float LastUndoTime => undoSteps.Count > 0 ? undoSteps[undoSteps.Count - 1].FinishedAt : float.NegativeInfinity;

    /// <summary>
    /// A settings object for a level operation on a single vertex, optionally painting it.
    /// </summary>
    /// <param name="paint">The paint, or null for none.</param>
    /// <returns>The settings.</returns>
    public static TerrainOp.Settings LevelVertex(TerrainModifier.PaintType? paint)
    {
        return new TerrainOp.Settings
        {
            m_level = true,
            m_levelRadius = 0.5f,
            m_square = false,
            m_paintCleared = paint.HasValue,
            m_paintType = paint ?? TerrainModifier.PaintType.Dirt,
            m_paintRadius = 0.8f,
            m_paintHeightCheck = false
        };
    }

    /// <summary>
    /// A settings object that only paints.
    /// </summary>
    /// <param name="paint">The paint.</param>
    /// <param name="radius">The radius.</param>
    /// <returns>The settings.</returns>
    public static TerrainOp.Settings Paint(TerrainModifier.PaintType paint, float radius)
    {
        return new TerrainOp.Settings
        {
            m_level = false,
            m_raise = false,
            m_smooth = false,
            m_paintCleared = true,
            m_paintType = paint,
            m_paintRadius = radius,
            m_paintHeightCheck = false
        };
    }

    /// <summary>
    /// Queues a job.
    /// </summary>
    /// <param name="job">The job.</param>
    public static void Enqueue(Job job)
    {
        jobs.Enqueue(job);
    }

    /// <summary>
    /// Runs the queued operations. Called every frame for the local player.
    /// </summary>
    public static void Tick()
    {
        int budget = Mathf.Max(1, TerrainSettings.EditsPerFrame.Value);
        while (budget > 0)
        {
            if (current == null)
            {
                if (jobs.Count == 0)
                {
                    break;
                }

                current = jobs.Dequeue();
            }

            while (budget > 0 && current.Next < current.Ops.Count)
            {
                (Vector3 position, TerrainOp.Settings settings) = current.Ops[current.Next++];
                Apply(current, position, settings);
                budget--;
            }

            if (current.Next >= current.Ops.Count)
            {
                Complete(current);
                current = null;
            }
        }

        foreach (TerrainComp comp in dirty)
        {
            if (comp != null && comp.m_nview != null && comp.m_nview.IsValid())
            {
                comp.Save();
                comp.m_hmap.Poke(1);
                if (ClutterSystem.instance != null)
                {
                    ClutterSystem.instance.ResetGrass(comp.m_hmap.transform.position, comp.m_hmap.m_width * comp.m_hmap.m_scale / 2f);
                }
            }
        }

        dirty.Clear();
    }

    /// <summary>
    /// Puts the terrain back where the vertex test says so, as it was when the world was made.
    /// </summary>
    /// <param name="job">The job, for undo.</param>
    /// <param name="inside">Which world points to reset.</param>
    /// <param name="centre">Centre of the area.</param>
    /// <param name="radius">Radius around the centre that holds the area.</param>
    public static void Reset(Job job, Func<Vector3, bool> inside, Vector3 centre, float radius)
    {
        heightmaps.Clear();
        Heightmap.FindHeightmap(centre, radius, heightmaps);
        foreach (Heightmap heightmap in heightmaps)
        {
            TerrainComp comp = heightmap.GetAndCreateTerrainCompiler();
            if (!Prepare(job, comp))
            {
                continue;
            }

            int pitch = comp.m_width + 1;
            for (int y = 0; y < pitch; y++)
            {
                for (int x = 0; x < pitch; x++)
                {
                    Vector3 point = heightmap.transform.position + new Vector3((x - comp.m_width / 2f) * heightmap.m_scale, 0f, (y - comp.m_width / 2f) * heightmap.m_scale);
                    if (!inside(point))
                    {
                        continue;
                    }

                    int index = y * pitch + x;
                    comp.m_modifiedHeight[index] = false;
                    comp.m_levelDelta[index] = 0f;
                    comp.m_smoothDelta[index] = 0f;
                    comp.m_modifiedPaint[index] = false;
                }
            }

            dirty.Add(comp);
        }
    }

    /// <summary>
    /// Takes back the latest terrain job within the undo time and gives back the stone it cost.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <returns>True if a job was taken back.</returns>
    public static bool Undo(Player player)
    {
        float window = TerrainSettings.UndoSeconds.Value;
        while (undoSteps.Count > 0)
        {
            Job job = undoSteps[undoSteps.Count - 1];
            undoSteps.RemoveAt(undoSteps.Count - 1);
            if (window <= 0f || Time.time - job.FinishedAt > window)
            {
                undoSteps.Clear();
                return false;
            }

            foreach (KeyValuePair<TerrainComp, Snapshot> before in job.Before)
            {
                TerrainComp comp = before.Key;
                if (comp == null || comp.m_nview == null || !comp.m_nview.IsValid())
                {
                    continue;
                }

                if (!comp.m_nview.IsOwner())
                {
                    comp.m_nview.ClaimOwnership();
                }

                before.Value.Restore(comp);
                dirty.Add(comp);
            }

            if (job.Paid != null && job.PaidAmount > 0)
            {
                TerrainCost.Refund(player, job.Paid, job.PaidAmount);
            }

            player.Message(MessageHud.MessageType.TopLeft, "$msg_whitehilt_terrain_undone");
            return true;
        }

        return false;
    }

    private static void Apply(Job job, Vector3 position, TerrainOp.Settings settings)
    {
        heightmaps.Clear();
        Heightmap.FindHeightmap(position, settings.GetRadius() + 0.5f, heightmaps);
        foreach (Heightmap heightmap in heightmaps)
        {
            TerrainComp comp = heightmap.GetAndCreateTerrainCompiler();
            if (Prepare(job, comp))
            {
                comp.InternalDoOperation(position, Vector3.zero, settings);
                dirty.Add(comp);
            }
        }
    }

    // Readies a terrain piece for changing: snapshots it for undo, and takes ownership of it, as only the owner of a
    // network object may write its data.
    private static bool Prepare(Job job, TerrainComp comp)
    {
        if (comp == null || !comp.m_initialized || comp.m_nview == null || !comp.m_nview.IsValid())
        {
            return false;
        }

        if (job.Undoable && !job.Before.ContainsKey(comp))
        {
            job.Before[comp] = new Snapshot(comp);
        }

        if (!comp.m_nview.IsOwner())
        {
            comp.m_nview.ClaimOwnership();
        }

        return true;
    }

    /// <summary>
    /// Ends a job: it becomes undoable and its callback runs. Queued jobs end by themselves; call this after <see cref="Reset"/>.
    /// </summary>
    /// <param name="job">The job.</param>
    public static void Complete(Job job)
    {
        job.FinishedAt = Time.time;
        if (job.Undoable && job.Before.Count > 0)
        {
            undoSteps.Add(job);
            while (undoSteps.Count > Mathf.Max(1, TerrainSettings.UndoSteps.Value))
            {
                undoSteps.RemoveAt(0);
            }
        }

        job.Done?.Invoke();
    }

    /// <summary>
    /// A list of terrain operations run as one step.
    /// </summary>
    public sealed class Job
    {
        /// <summary>The operations: where, and what.</summary>
        public readonly List<(Vector3 Position, TerrainOp.Settings Settings)> Ops = new();

        /// <summary>Terrain before the job, per terrain piece.</summary>
        public readonly Dictionary<TerrainComp, Snapshot> Before = new();

        /// <summary>Whether the job can be taken back.</summary>
        public bool Undoable = true;

        /// <summary>The resource paid, or null.</summary>
        public ItemDrop Paid;

        /// <summary>How much was paid.</summary>
        public int PaidAmount;

        /// <summary>Called when all operations have run.</summary>
        public Action Done;

        /// <summary>Index of the next operation.</summary>
        public int Next;

        /// <summary>When the job finished.</summary>
        public float FinishedAt;
    }

    /// <summary>
    /// A copy of a terrain piece's changes.
    /// </summary>
    public sealed class Snapshot
    {
        private readonly bool[] modifiedHeight;
        private readonly float[] levelDelta;
        private readonly float[] smoothDelta;
        private readonly bool[] modifiedPaint;
        private readonly Color[] paintMask;

        /// <summary>
        /// Copies the terrain piece's changes.
        /// </summary>
        /// <param name="comp">The terrain piece.</param>
        public Snapshot(TerrainComp comp)
        {
            modifiedHeight = (bool[])comp.m_modifiedHeight.Clone();
            levelDelta = (float[])comp.m_levelDelta.Clone();
            smoothDelta = (float[])comp.m_smoothDelta.Clone();
            modifiedPaint = (bool[])comp.m_modifiedPaint.Clone();
            paintMask = (Color[])comp.m_paintMask.Clone();
        }

        /// <summary>
        /// Puts the copy back.
        /// </summary>
        /// <param name="comp">The terrain piece.</param>
        public void Restore(TerrainComp comp)
        {
            Array.Copy(modifiedHeight, comp.m_modifiedHeight, modifiedHeight.Length);
            Array.Copy(levelDelta, comp.m_levelDelta, levelDelta.Length);
            Array.Copy(smoothDelta, comp.m_smoothDelta, smoothDelta.Length);
            Array.Copy(modifiedPaint, comp.m_modifiedPaint, modifiedPaint.Length);
            Array.Copy(paintMask, comp.m_paintMask, paintMask.Length);
        }
    }
}
