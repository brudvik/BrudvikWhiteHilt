using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace BrudvikWhiteHilt.Performance;

/// <summary>
/// Makes the objects that come into view within a time budget per frame. The game's <c>ZNetScene.CreateObjects</c>
/// makes a fixed number per pass (at least 10, or a hundredth of the backlog), however heavy they are, so riding into a
/// large base or a dense forest makes a few very long frames. Here objects are made in the game's own order (terrain,
/// then buildings, then the rest, each nearest first) until the budget is spent:
/// <list type="bullet">
/// <item>Objects within <see cref="NearRadius"/> are always made at the game's own rate, budget or not, so nothing
/// close is ever later than in the game.</item>
/// <item>Beyond that they wait for the next pass once the budget is spent; cheap objects go faster than the game's
/// fixed count, up to <see cref="MaxRateFactor"/> times it.</item>
/// <item>The budget is <see cref="StreamingSettings.ObjectBudgetMs"/>, a fifth less while the game runs below 30 frames
/// per second and a fifth more while many objects wait.</item>
/// </list>
/// Behind a loading screen, on a dedicated server and with the setting off, the game's own method runs, so loading
/// stays as fast as it is. Approach after HitchFix's SpawnBudget (MIT, Valheim Performance Fixes contributors).
/// </summary>
public static class ObjectBudget
{
    /// <summary>Distance in metres within which objects are made at least at the game's own rate.</summary>
    public const float NearRadius = 40f;

    /// <summary>How many times the game's count per pass cheap objects may reach.</summary>
    public const int MaxRateFactor = 4;

    // A backlog of near objects above this raises the budget.
    private const int LargeBacklog = 300;
    private const float ReportInterval = 60f;

    private static readonly List<ZDO> Pending = new List<ZDO>();
    private static readonly List<ZDO> PendingDistant = new List<ZDO>();
    private static readonly Stopwatch Watch = new Stopwatch();
    private static readonly Comparison<ZDO> ByDistance = (a, b) => a.m_tempSortValue.CompareTo(b.m_tempSortValue);
    private static readonly Comparison<ZDO> GameOrder = ZNetScene.ZDOCompare;

    private static bool failed;
    private static float smoothedFrame = 1f / 60f;

    // For the log line.
    private static float nextReport;
    private static int passes;
    private static int made;
    private static int budgetHits;
    private static double slowestMs;

    /// <summary>
    /// Makes objects in place of the game's CreateObjects, when the budget applies.
    /// </summary>
    /// <param name="scene">The scene.</param>
    /// <param name="near">The objects in the active area.</param>
    /// <param name="distant">The distant objects (trees and rocks seen from afar).</param>
    /// <returns>True when the objects were made here; false to let the game's method run.</returns>
    public static bool TryCreate(ZNetScene scene, List<ZDO> near, List<ZDO> distant)
    {
        if (failed || !StreamingSettings.ObjectBudget.Value || ZNet.instance == null || ZNet.instance.IsDedicated()
            || ZoneSystem.instance == null)
        {
            return false;
        }

        // Behind a loading screen the game makes a hundred per pass on purpose.
        Player player = Player.m_localPlayer;
        if (player == null || player.IsTeleporting())
        {
            return false;
        }

        smoothedFrame += (Time.unscaledDeltaTime - smoothedFrame) * 0.1f;
        Watch.Restart();
        bool hit = false;
        int created = 0;
        try
        {
            created = CreateNear(scene, near, ref hit);
            CreateDistant(scene, distant, ref created, ref hit);
        }
        catch (Exception ex)
        {
            failed = true;
            Pending.Clear();
            PendingDistant.Clear();
            Jotunn.Logger.LogWarning("Object budget failed, the game's own object creation is used from now on: " + ex);
            return true;
        }

        Watch.Stop();
        Report(created, hit, Watch.Elapsed.TotalMilliseconds);
        return true;
    }

    // The game's CreateObjectsSorted with a time budget beyond the near radius.
    private static int CreateNear(ZNetScene scene, List<ZDO> near, ref bool hit)
    {
        if (!ZoneSystem.instance.IsActiveAreaLoaded())
        {
            return 0;
        }

        Vector3 reference = ZNet.instance.GetReferencePosition();
        Pending.Clear();
        foreach (ZDO zdo in near)
        {
            if (!zdo.Created)
            {
                zdo.m_tempSortValue = Utils.DistanceSqr(reference, zdo.GetPosition());
                Pending.Add(zdo);
            }
        }

        if (Pending.Count == 0)
        {
            return 0;
        }

        // The game's count per pass, which near objects always get, and the most cheap ones may reach.
        int gameRate = Mathf.Max(Pending.Count / 100, 10);
        int maxRate = gameRate * MaxRateFactor;
        double budget = BudgetMs(Pending.Count);
        float nearSqr = NearRadius * NearRadius;
        Pending.Sort(GameOrder);

        int created = 0;
        foreach (ZDO zdo in Pending)
        {
            bool close = zdo.m_tempSortValue <= nearSqr && created < gameRate;
            if (!close && created > 0 && Watch.Elapsed.TotalMilliseconds >= budget)
            {
                // Later objects of a lower kind may still be close, so keep looking rather than stop.
                hit = true;
                continue;
            }

            if (!ZoneSystem.instance.IsZoneReadyForType(zdo.GetSector(), zdo.Type))
            {
                continue;
            }

            if (scene.CreateObject(zdo) != null)
            {
                created++;
                if (created >= maxRate)
                {
                    break;
                }
            }
            else if (ZNet.instance.IsServer())
            {
                // As the game does: a host removes an object whose prefab no longer exists.
                zdo.SetOwner(ZDOMan.GetSessionID());
                ZLog.Log("Destroyed invalid prefab ZDO:" + zdo.m_uid);
                ZDOMan.instance.DestroyZDO(zdo);
            }
        }

        Pending.Clear();
        return created;
    }

    // The game's CreateDistantObjects, nearest first, while budget is left.
    private static void CreateDistant(ZNetScene scene, List<ZDO> distant, ref int created, ref bool hit)
    {
        const int gameRate = 10;
        if (created > gameRate || distant.Count == 0)
        {
            return;
        }

        double budget = BudgetMs(0);
        if (created > 0 && Watch.Elapsed.TotalMilliseconds >= budget)
        {
            hit = true;
            return;
        }

        Vector3 reference = ZNet.instance.GetReferencePosition();
        PendingDistant.Clear();
        foreach (ZDO zdo in distant)
        {
            if (!zdo.Created)
            {
                Vector3 position = zdo.GetPosition();
                float dx = position.x - reference.x;
                float dz = position.z - reference.z;
                zdo.m_tempSortValue = dx * dx + dz * dz;
                PendingDistant.Add(zdo);
            }
        }

        PendingDistant.Sort(ByDistance);
        foreach (ZDO zdo in PendingDistant)
        {
            if (created > 0 && Watch.Elapsed.TotalMilliseconds >= budget)
            {
                hit = true;
                break;
            }

            if (scene.CreateObject(zdo) != null)
            {
                created++;
                if (created > gameRate)
                {
                    break;
                }
            }
            else if (ZNet.instance.IsServer())
            {
                zdo.SetOwner(ZDOMan.GetSessionID());
                ZLog.Log("Destroyed invalid predab ZDO:" + zdo.m_uid + "  prefab hash:" + zdo.GetPrefab());
                ZDOMan.instance.DestroyZDO(zdo);
            }
        }

        PendingDistant.Clear();
    }

    // The setting, a fifth less when frames are slow and a fifth more when much is waiting.
    private static double BudgetMs(int backlog)
    {
        double budget = StreamingSettings.ObjectBudgetMs.Value;
        if (smoothedFrame > 1f / 30f)
        {
            return budget * 0.8;
        }

        return backlog > LargeBacklog ? budget * 1.2 : budget;
    }

    private static void Report(int created, bool hit, double ms)
    {
        if (!StreamingSettings.ObjectBudgetLog.Value)
        {
            return;
        }

        if (created > 0)
        {
            passes++;
            made += created;
            budgetHits += hit ? 1 : 0;
            slowestMs = Math.Max(slowestMs, ms);
        }

        if (Time.unscaledTime < nextReport)
        {
            return;
        }

        if (passes > 0)
        {
            Jotunn.Logger.LogInfo($"Object budget: {made} objects in {passes} passes over the last minute, {budgetHits} "
                + $"passes hit the {StreamingSettings.ObjectBudgetMs.Value:0.#} ms budget, slowest {slowestMs:0.0} ms.");
        }

        nextReport = Time.unscaledTime + ReportInterval;
        passes = 0;
        made = 0;
        budgetHits = 0;
        slowestMs = 0;
    }
}
