using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Production;

/// <summary>
/// Every loaded station that can have a production timer, added as vanilla wakes it up. Destroyed ones are dropped
/// as they are found.
/// </summary>
public static class ProductionRegistry
{
    private static readonly List<Component> sources = new();
    private static readonly ProductionStatus hoverStatus = new();

    /// <summary>
    /// Adds a station that has a ZDO; placement ghosts have none and are left out.
    /// </summary>
    /// <param name="source">The station component.</param>
    public static void Add(Component source)
    {
        ZNetView nview = source != null ? source.GetComponentInParent<ZNetView>() : null;
        if (nview != null && nview.GetZDO() != null)
        {
            sources.Add(source);
        }
    }

    /// <summary>
    /// Finds the stations within a range.
    /// </summary>
    /// <param name="position">Centre.</param>
    /// <param name="range">Range in metres.</param>
    /// <param name="result">Filled with the stations.</param>
    public static void Near(Vector3 position, float range, List<Component> result)
    {
        result.Clear();
        float squared = range * range;
        for (int i = sources.Count - 1; i >= 0; i--)
        {
            Component source = sources[i];
            if (source == null)
            {
                sources.RemoveAt(i);
                continue;
            }

            if ((source.transform.position - position).sqrMagnitude <= squared)
            {
                result.Add(source);
            }
        }
    }

    /// <summary>
    /// Adds the station's timer lines to its hover text.
    /// </summary>
    /// <param name="source">The station component.</param>
    /// <param name="text">The vanilla hover text.</param>
    /// <returns>The hover text with the timers.</returns>
    public static string AppendHover(Component source, string text)
    {
        if (string.IsNullOrEmpty(text) || ProductionSettings.ShowHover == null || !ProductionSettings.ShowHover.Value
            || !ProductionReader.Read(source, hoverStatus, true) || hoverStatus.Lines.Count == 0)
        {
            return text;
        }

        return text + "\n" + string.Join("\n", hoverStatus.Lines);
    }
}
