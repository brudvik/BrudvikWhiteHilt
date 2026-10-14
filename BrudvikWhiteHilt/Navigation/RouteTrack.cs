using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Navigation;

/// <summary>
/// The plain work of following a planned route, shared by the Navigator's Table autopilot and the trading ship: which
/// turning point is next, how far is left, how far along the ship is, and how a route is packed into a ship's data. A
/// route is a list of turning points as <see cref="SeaRouteFinder"/> returns them; only x and z count. Everything but
/// <see cref="Pack"/> and <see cref="Unpack"/> takes plain values, so the tests can run it without the game.
/// </summary>
public static class RouteTrack
{
    /// <summary>
    /// Moves on past every turning point the ship has reached. A turning point counts as reached a little before the ship
    /// is on it, so the turn starts in time; the last one needs its own, usually wider, reach.
    /// </summary>
    /// <param name="path">The turning points, in order.</param>
    /// <param name="point">The index of the turning point the ship is steering for.</param>
    /// <param name="here">Where the ship is.</param>
    /// <param name="reach">How close a turning point must be to count as reached.</param>
    /// <param name="lastReach">How close the last point must be to count as reached.</param>
    /// <returns>The index of the next turning point; the number of points when the whole route is sailed.</returns>
    public static int Advance(IReadOnlyList<Vector3> path, int point, Vector3 here, float reach, float lastReach)
    {
        point = Mathf.Max(0, point);
        while (point < path.Count && Distance(here, path[point]) < (point == path.Count - 1 ? lastReach : reach))
        {
            point++;
        }

        return point;
    }

    /// <summary>
    /// How far the ship still has to sail: to the next turning point and on along the rest of the route.
    /// </summary>
    /// <param name="path">The turning points, in order.</param>
    /// <param name="point">The index of the turning point the ship is steering for.</param>
    /// <param name="here">Where the ship is.</param>
    /// <returns>The distance in metres; 0 when no point is left.</returns>
    public static float Remaining(IReadOnlyList<Vector3> path, int point, Vector3 here)
    {
        point = Mathf.Max(0, point);
        if (point >= path.Count)
        {
            return 0f;
        }

        return Distance(here, path[point]) + Length(path, point);
    }

    /// <summary>
    /// The length of a route, or of the part from a turning point on.
    /// </summary>
    /// <param name="path">The turning points, in order.</param>
    /// <param name="from">The index to begin at.</param>
    /// <returns>The distance in metres along the legs.</returns>
    public static float Length(IReadOnlyList<Vector3> path, int from = 0)
    {
        float length = 0f;
        for (int i = Mathf.Max(1, from + 1); i < path.Count; i++)
        {
            length += Distance(path[i - 1], path[i]);
        }

        return length;
    }

    /// <summary>
    /// How far along a route the ship is.
    /// </summary>
    /// <param name="remaining">The distance left, in metres.</param>
    /// <param name="total">The distance the route had at the start, in metres.</param>
    /// <returns>0 at the start, 1 at the end; 1 when the route has no length.</returns>
    public static float Progress(float remaining, float total)
    {
        return total <= 0f ? 1f : Mathf.Clamp01(1f - remaining / total);
    }

    /// <summary>
    /// The total that keeps the progress where it is when a new route with another length is planned from the ship's place,
    /// so the waves that are due by progress do not come early or late because of the detour.
    /// </summary>
    /// <param name="remaining">The distance left on the new route, in metres.</param>
    /// <param name="progress">How far along the ship was, 0 to 1.</param>
    /// <returns>The new total, in metres.</returns>
    public static float TotalFor(float remaining, float progress)
    {
        return remaining / Mathf.Max(0.05f, 1f - Mathf.Clamp01(progress));
    }

    /// <summary>
    /// Packs a route for the ship's data: the number of points, then x and z of each.
    /// </summary>
    /// <param name="points">The turning points.</param>
    /// <returns>The package.</returns>
    public static ZPackage Pack(IList<Vector3> points)
    {
        ZPackage package = new();
        package.Write(points.Count);
        foreach (Vector3 point in points)
        {
            package.Write(point.x);
            package.Write(point.z);
        }

        return package;
    }

    /// <summary>
    /// Reads a route packed by <see cref="Pack"/>. Empty data gives an empty route.
    /// </summary>
    /// <param name="data">The bytes of the package; null or empty for none.</param>
    /// <param name="into">The list to fill; it is cleared first.</param>
    public static void Unpack(byte[] data, List<Vector3> into)
    {
        into.Clear();
        if (data == null || data.Length == 0)
        {
            return;
        }

        ZPackage package = new(data);
        int count = package.ReadInt();
        for (int i = 0; i < count; i++)
        {
            into.Add(new Vector3(package.ReadSingle(), 0f, package.ReadSingle()));
        }
    }

    private static float Distance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }
}
