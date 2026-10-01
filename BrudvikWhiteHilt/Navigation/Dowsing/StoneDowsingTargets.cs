using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Navigation.Dowsing;

/// <summary>
/// The loaded pickables that give a rock the Stone Dowser finds.
/// </summary>
public static class StoneDowsingTargets
{
    private static readonly HashSet<Pickable> pickables = new();

    /// <summary>
    /// Remembers a pickable if it gives one of the dowser's items.
    /// </summary>
    /// <param name="pickable">A pickable that just woke up.</param>
    public static void Register(Pickable pickable)
    {
        if (pickable.m_itemPrefab == null || !StoneDowsingSettings.IsTrackedItem(pickable.m_itemPrefab.name))
        {
            return;
        }

        pickables.RemoveWhere(known => known == null);
        pickables.Add(pickable);
    }

    /// <summary>
    /// The nearest rock that can still be picked.
    /// </summary>
    /// <param name="point">Where to look from.</param>
    /// <param name="range">How far to look, in metres.</param>
    /// <returns>The pickable, or null when none is within range.</returns>
    public static Pickable FindClosest(Vector3 point, float range)
    {
        Pickable closest = null;
        float best = range;
        foreach (Pickable pickable in pickables)
        {
            if (pickable == null || pickable.m_nview == null || !pickable.m_nview.IsValid() || !pickable.CanBePicked()
                || pickable.m_itemPrefab == null || !StoneDowsingSettings.IsTrackedItem(pickable.m_itemPrefab.name))
            {
                continue;
            }

            float distance = Vector3.Distance(point, pickable.transform.position);
            if (distance < best)
            {
                best = distance;
                closest = pickable;
            }
        }

        return closest;
    }
}
