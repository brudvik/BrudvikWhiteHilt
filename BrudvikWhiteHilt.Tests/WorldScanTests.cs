using BrudvikWhiteHilt.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Xunit;

namespace BrudvikWhiteHilt.Tests;

/// <summary>
/// Checks the server's shared walk through the world: every service sees each object it asked for once per scan, a scan
/// begun halfway through the walk still goes once round, and one service that fails does not stop the others.
/// </summary>
public class WorldScanTests
{
    private const int Tree = 1;
    private const int Bed = 2;
    private const int Portal = 3;

    // Sectors as the game keeps them: most empty, some never made.
    private static List<ZDO>[] World()
    {
        List<ZDO>[] sectors = new List<ZDO>[40];
        int id = 0;
        for (int i = 0; i < sectors.Length; i += 3)
        {
            sectors[i] = new List<ZDO>();
            foreach (int prefab in new[] { Tree, Bed, Portal, Tree })
            {
                sectors[i].Add(Object(prefab, ++id));
            }
        }

        sectors[4] = new List<ZDO>();
        return sectors;
    }

    // A ZDO without its constructor, which asks Unity for a rotation; the walk reads only the prefab and the id.
    private static ZDO Object(int prefab, int id)
    {
        ZDO zdo = (ZDO)FormatterServices.GetUninitializedObject(typeof(ZDO));
        zdo.m_prefab = prefab;
        zdo.m_uid = new ZDOID(1L, (uint)id);
        return zdo;
    }

    /// <summary>Each scan sees the prefabs it asked for, every object once, and finishes once.</summary>
    [Fact]
    public void EachScanSeesWhatItAskedForOnce()
    {
        List<ZDO>[] sectors = World();
        WorldScan scan = new();
        Client beds = new();
        Client portals = new();
        scan.Begin(beds, new HashSet<int> { Bed });
        scan.Begin(portals, new HashSet<int> { Portal, Bed });

        Run(scan, sectors);

        Assert.Equal(1, beds.Finished);
        Assert.Equal(1, portals.Finished);
        Assert.Equal(Count(sectors, Bed), beds.Seen.Count);
        Assert.All(beds.Seen, zdo => Assert.Equal(Bed, zdo.m_prefab));
        Assert.Equal(beds.Seen.Count, beds.Seen.Distinct().Count());
        Assert.Equal(Count(sectors, Bed) + Count(sectors, Portal), portals.Seen.Count);
    }

    /// <summary>A scan begun while another is halfway goes once round from there.</summary>
    [Fact]
    public void AScanBegunHalfwayGoesOnceRound()
    {
        List<ZDO>[] sectors = World();
        WorldScan scan = new();
        Client first = new();
        Client late = new();
        scan.Begin(first, new HashSet<int> { Tree });
        for (int i = 0; i < 3; i++)
        {
            scan.Step(sectors, 0L);
        }

        scan.Begin(late, new HashSet<int> { Tree });
        Assert.True(scan.IsRunning(late));
        Run(scan, sectors);

        Assert.Equal(1, late.Finished);
        Assert.Equal(Count(sectors, Tree), late.Seen.Count);
        Assert.Equal(late.Seen.Count, late.Seen.Distinct().Count());
        Assert.Equal(Count(sectors, Tree), first.Seen.Count);
        Assert.False(scan.IsRunning(first));
    }

    /// <summary>A service that throws is stopped and told; the others go on.</summary>
    [Fact]
    public void AFailingServiceDoesNotStopTheOthers()
    {
        List<ZDO>[] sectors = World();
        WorldScan scan = new();
        Client broken = new() { Throws = true };
        Client beds = new();
        scan.Begin(broken, new HashSet<int> { Bed });
        scan.Begin(beds, new HashSet<int> { Bed });

        Run(scan, sectors);

        Assert.Equal(1, broken.Failed);
        Assert.Equal(0, broken.Finished);
        Assert.Equal(1, beds.Finished);
        Assert.Equal(Count(sectors, Bed), beds.Seen.Count);
    }

    // Steps with no time to spare, one sector with objects per step, until no scan runs.
    private static void Run(WorldScan scan, List<ZDO>[] sectors)
    {
        for (int i = 0; i < 1000; i++)
        {
            scan.Step(sectors, 0L);
        }
    }

    private static int Count(List<ZDO>[] sectors, int prefab)
    {
        return sectors.Where(sector => sector != null).SelectMany(sector => sector).Count(zdo => zdo.m_prefab == prefab);
    }

    private sealed class Client : WorldScan.IClient
    {
        public readonly List<ZDO> Seen = new();
        public bool Throws;
        public int Finished;
        public int Failed;

        public void Collect(ZDO zdo, int prefab)
        {
            if (Throws)
            {
                throw new InvalidOperationException("broken");
            }

            Seen.Add(zdo);
        }

        public void Finish() => Finished++;

        public void Fail(Exception error) => Failed++;
    }
}
