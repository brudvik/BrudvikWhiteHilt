using System.Diagnostics;
using UnityEngine;

namespace BrudvikWhiteHilt.Helpers;

/// <summary>
/// Runs on the <see cref="Game"/> object and drives the server's shared <see cref="WorldScan"/>: a slice of each frame
/// goes to walking the world's objects while any service has a scan running. Added by the first service that needs it.
/// </summary>
public class WorldScanService : MonoBehaviour
{
    // How long the walk may take per frame, for all the services together.
    private const double FrameBudgetMs = 1.5;

    private static readonly long budgetTicks = (long)(FrameBudgetMs * Stopwatch.Frequency / 1000.0);

    /// <summary>
    /// The walk this game object drives.
    /// </summary>
    public WorldScan Scan { get; } = new();

    /// <summary>
    /// The shared walk of a game object, adding its driver the first time.
    /// </summary>
    /// <param name="owner">The <see cref="Game"/> object.</param>
    /// <returns>The walk.</returns>
    public static WorldScan On(GameObject owner)
    {
        WorldScanService service = owner.GetComponent<WorldScanService>() ?? owner.AddComponent<WorldScanService>();
        return service.Scan;
    }

    private void Update()
    {
        if (ZNet.instance != null && ZNet.instance.IsServer() && ZDOMan.instance != null)
        {
            Scan.Step(ZDOMan.instance.m_objectsBySector, budgetTicks);
        }
    }
}
