using System;
using System.Collections.Generic;

namespace BrudvikWhiteHilt.SeaBattles.Weapons;

/// <summary>
/// Which ammunition the Ship Ballista fires, from plain values so it can be tested without the game. The ammunition is a list
/// of prefab names in a fixed order; the gunner has one selected, and the secondary attack moves to the next kind they carry.
/// </summary>
public static class AmmoChoice
{
    /// <summary>
    /// The ammunition a shot uses: the selected kind if the gunner carries it, otherwise the first kind in the list that they do.
    /// </summary>
    /// <param name="order">The prefab names of the ammunition, in the order the secondary attack cycles through them.</param>
    /// <param name="carried">Whether the gunner carries any of a kind.</param>
    /// <param name="selected">The selected kind; need not be in the list.</param>
    /// <returns>The prefab name, or null when none is carried.</returns>
    public static string Resolve(IReadOnlyList<string> order, Func<string, bool> carried, string selected)
    {
        if (order == null || carried == null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(selected) && IndexOf(order, selected) >= 0 && carried(selected))
        {
            return selected;
        }

        for (int i = 0; i < order.Count; i++)
        {
            if (carried(order[i]))
            {
                return order[i];
            }
        }

        return null;
    }

    /// <summary>
    /// The kind the secondary attack switches to: the next one in the list, after the selected one, that the gunner carries.
    /// The list wraps round, so with one kind carried the answer is that kind.
    /// </summary>
    /// <param name="order">The prefab names of the ammunition.</param>
    /// <param name="carried">Whether the gunner carries any of a kind.</param>
    /// <param name="selected">The selected kind; one that is not in the list starts the search at the top.</param>
    /// <returns>The prefab name, or null when none is carried.</returns>
    public static string Next(IReadOnlyList<string> order, Func<string, bool> carried, string selected)
    {
        if (order == null || order.Count == 0 || carried == null)
        {
            return null;
        }

        int start = IndexOf(order, selected);
        for (int step = 1; step <= order.Count; step++)
        {
            string candidate = order[((start + step) % order.Count + order.Count) % order.Count];
            if (carried(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>
    /// How many kinds the gunner carries.
    /// </summary>
    /// <param name="order">The prefab names of the ammunition.</param>
    /// <param name="carried">Whether the gunner carries any of a kind.</param>
    /// <returns>The number of kinds carried.</returns>
    public static int KindsCarried(IReadOnlyList<string> order, Func<string, bool> carried)
    {
        int count = 0;
        for (int i = 0; order != null && carried != null && i < order.Count; i++)
        {
            count += carried(order[i]) ? 1 : 0;
        }

        return count;
    }

    private static int IndexOf(IReadOnlyList<string> order, string name)
    {
        for (int i = 0; name != null && i < order.Count; i++)
        {
            if (string.Equals(order[i], name, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }
}
