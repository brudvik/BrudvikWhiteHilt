using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Performance;

/// <summary>
/// Forgets networked objects that were destroyed without <c>ZNetScene</c> being told. The scene drops an entry from
/// <c>m_instances</c> only on its own removal paths; a <c>ZNetView</c> destroyed any other way, most often as the child
/// of another networked object (the coin effect of an infested mine's treasure pile has one), stays registered as a
/// dead object while its ZDO stays marked as made. <c>ZNetScene.IsAreaReady</c> counts a dead entry as not loaded, so a
/// portal trip, dungeon exit or respawn next to it waits on the loading screen for ever, and the object is never made
/// again either. When <c>IsAreaReady</c> says no, the area it was asked about is checked for dead entries, at most once
/// a second, and each is unloaded the way <c>ZNetScene.RemoveObjects</c> unloads an object.
/// </summary>
public static class DeadInstances
{
    private const float ScanSeconds = 1f;

    private static readonly List<ZDO> area = new();
    private static float nextScan;

    /// <summary>
    /// Unloads the dead entries in the zone around a point.
    /// </summary>
    /// <param name="scene">The scene.</param>
    /// <param name="point">The point <c>IsAreaReady</c> was asked about.</param>
    /// <returns>How many entries were unloaded.</returns>
    public static int Repair(ZNetScene scene, Vector3 point)
    {
        if (Time.realtimeSinceStartup < nextScan || ZDOMan.instance == null)
        {
            return 0;
        }

        nextScan = Time.realtimeSinceStartup + ScanSeconds;
        area.Clear();
        ZDOMan.instance.FindSectorObjects(ZoneSystem.GetZone(point), new SimulationDistance(1, 0), area);
        int repaired = 0;
        foreach (ZDO zdo in area)
        {
            if (zdo == null || !scene.m_instances.TryGetValue(zdo, out ZNetView view) || view != null)
            {
                continue;
            }

            scene.m_instances.Remove(zdo);
            // Named before the ZDO may be destroyed and handed back to the pool.
            string name = scene.GetPrefab(zdo.GetPrefab())?.name ?? zdo.GetPrefab().ToString();
            string where = zdo.GetPosition().ToString("F0");
            if (!zdo.Persistent && zdo.IsOwner())
            {
                ZDOMan.instance.DestroyZDO(zdo);
            }
            else
            {
                zdo.Created = false;
            }

            repaired++;
            Jotunn.Logger.LogInfo($"Performance: forgot {name} at {where}, destroyed without the game being told; "
                                  + "it was holding up the loading screen.");
        }

        area.Clear();
        return repaired;
    }
}
