using System.Collections.Generic;

namespace BrudvikWhiteHilt.SeaBattles;

/// <summary>
/// The player lists of the ships that sail on their own. The game's physics counts the players aboard to know whether
/// anyone steers; a Nail Ship under sail counts one more than it has, so it is not stopped. Lists are told apart by
/// reference, so two empty lists are not mixed up, and a lookup is one hash of a reference.
/// </summary>
internal static class SailingShips
{
    private static readonly HashSet<object> lists = new();

    /// <summary>Notes a ship's player list as one that sails.</summary>
    /// <param name="list">The list.</param>
    public static void Add(object list)
    {
        lists.Add(list);
    }

    /// <summary>Forgets a ship's player list.</summary>
    /// <param name="list">The list.</param>
    public static void Remove(object list)
    {
        lists.Remove(list);
    }

    /// <summary>Whether the list belongs to a ship that sails on its own.</summary>
    /// <param name="list">The list.</param>
    /// <returns>True if it was added and not removed.</returns>
    public static bool Contains(object list)
    {
        return lists.Contains(list);
    }

    /// <summary>
    /// The count the physics should see: one for an empty list of a ship that sails, else the real count.
    /// </summary>
    /// <param name="list">The ship's player list.</param>
    /// <param name="count">How many players it holds.</param>
    /// <returns>The count to use.</returns>
    public static int Count(object list, int count)
    {
        return count == 0 && lists.Contains(list) ? 1 : count;
    }
}
