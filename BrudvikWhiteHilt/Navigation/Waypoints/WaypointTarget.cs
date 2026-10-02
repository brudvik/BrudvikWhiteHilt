using System.Globalization;
using UnityEngine;

namespace BrudvikWhiteHilt.Navigation.Waypoints;

/// <summary>
/// The local player's target, kept in the player's custom data per world so it stays after logging out and does not
/// follow the character into another world.
/// </summary>
public static class WaypointTarget
{
    private const string KeyPrefix = "whitehilt_waypoint_";

    private static Player loadedFor;
    private static long loadedWorld;

    /// <summary>Whether a target is set.</summary>
    public static bool Has { get; private set; }

    /// <summary>Where the target is; y is not used.</summary>
    public static Vector3 Position { get; private set; }

    /// <summary>The target's name: the pin's, or a default.</summary>
    public static string Name { get; private set; }

    /// <summary>
    /// Reads the target of the player in the current world, when the player or the world changed.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <returns>True when a target is set.</returns>
    public static bool Refresh(Player player)
    {
        long world = World();
        if (player == loadedFor && world == loadedWorld)
        {
            return Has;
        }

        loadedFor = player;
        loadedWorld = world;
        Has = false;
        if (player == null || world == 0 || !player.m_customData.TryGetValue(Key(world), out string value))
        {
            return false;
        }

        string[] fields = value.Split(new[] { ';' }, 3);
        if (fields.Length == 3
            && float.TryParse(fields[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)
            && float.TryParse(fields[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
        {
            Position = new Vector3(x, 0f, z);
            Name = fields[2];
            Has = true;
        }

        return Has;
    }

    /// <summary>
    /// Sets and saves the target.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="position">Where the target is.</param>
    /// <param name="name">Its name.</param>
    public static void Set(Player player, Vector3 position, string name)
    {
        Refresh(player);
        if (player == null || loadedWorld == 0)
        {
            return;
        }

        Position = new Vector3(position.x, 0f, position.z);
        Name = name;
        Has = true;
        player.m_customData[Key(loadedWorld)] = string.Join(";",
            position.x.ToString("F1", CultureInfo.InvariantCulture), position.z.ToString("F1", CultureInfo.InvariantCulture), name);
    }

    /// <summary>
    /// Removes the target.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Clear(Player player)
    {
        Refresh(player);
        Has = false;
        player?.m_customData.Remove(Key(loadedWorld));
    }

    private static long World()
    {
        return ZNet.instance != null && ZNet.World != null ? ZNet.instance.GetWorldUID() : 0L;
    }

    private static string Key(long world)
    {
        return KeyPrefix + world.ToString(CultureInfo.InvariantCulture);
    }
}
