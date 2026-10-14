using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.TradingPosts;

/// <summary>
/// Places the trading posts in the world, on the server: once per world, after its locations are generated, it
/// searches the coasts for places that suit a post (<see cref="TradingPostSites"/>), picks up to
/// <see cref="TradingPostSettings.PerBiome"/> in each biome that meets the sea, far apart and clear of buildings and
/// locations, and builds them. Worlds made before the posts get them too. The work is spread over frames so the
/// server does not stall.
/// </summary>
public class TradingPostPlacer : MonoBehaviour
{
    /// <summary>Global key set once the posts of a world are placed.</summary>
    public const string DoneKey = "whitehilt_tradingposts_placed";

    // The biomes that get posts, in the order of the game; the Deep North has no content yet and the ocean no land.
    private static readonly Heightmap.Biome[] biomes =
    {
        Heightmap.Biome.Meadows, Heightmap.Biome.BlackForest, Heightmap.Biome.Swamp, Heightmap.Biome.Mountain,
        Heightmap.Biome.Plains, Heightmap.Biome.Mistlands, Heightmap.Biome.AshLands
    };

    private const float WorldRadius = 10000f;
    private const float GridStep = 40f;
    private const float FrameBudgetMs = 4f;

    // No post near the world's centre, where players start.
    private const float CentreClearance = 300f;

    private static TradingPostPlacer instance;
    private readonly Stopwatch watch = new();
    private bool running;

    /// <summary>
    /// Starts a search now, on the server, whatever the world's key says; posts already standing are kept and count
    /// towards each biome. Used by <c>whitehilt_tradepost place</c>.
    /// </summary>
    /// <returns>False if a search is already running or this is not the server.</returns>
    public static bool PlaceNow()
    {
        if (instance == null || instance.running || ZNet.instance == null || !ZNet.instance.IsServer())
        {
            return false;
        }

        instance.StartCoroutine(instance.Place());
        return true;
    }

    private void Awake()
    {
        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    // Waits on the server until the world's locations are placed, then searches once if the world has no posts yet.
    private void Update()
    {
        ZNet net = ZNet.instance;
        ZoneSystem zones = ZoneSystem.instance;
        if (net == null || zones == null || ZDOMan.instance == null || WorldGenerator.instance == null)
        {
            return;
        }

        if (!net.IsServer())
        {
            enabled = false;
            return;
        }

        if (!zones.m_locationsGenerated)
        {
            return;
        }

        enabled = false;
        if (TradingPostSettings.Enabled.Value && !zones.GetGlobalKey(DoneKey))
        {
            StartCoroutine(Place());
        }
    }

    private IEnumerator Place()
    {
        running = true;
        watch.Restart();
        float water = ZoneSystem.instance.m_waterLevel;
        Func<float, float, float> height = (x, z) => WorldGenerator.instance.GetHeight(x, z);

        Dictionary<Vector2s, List<Vector3>> buildings = new();
        IEnumerator collect = CollectBuildings(buildings);
        while (collect.MoveNext())
        {
            yield return null;
        }

        Dictionary<Heightmap.Biome, List<Site>> found = biomes.ToDictionary(biome => biome, _ => new List<Site>());
        for (float x = -WorldRadius; x <= WorldRadius; x += GridStep)
        {
            for (float z = -WorldRadius; z <= WorldRadius; z += GridStep)
            {
                if (watch.Elapsed.TotalMilliseconds > FrameBudgetMs)
                {
                    yield return null;
                    if (ZNet.instance == null || WorldGenerator.instance == null)
                    {
                        running = false;
                        yield break;
                    }

                    watch.Restart();
                }

                Vector2 origin = new(x, z);
                float magnitude = origin.magnitude;
                if (magnitude > WorldRadius || magnitude < CentreClearance)
                {
                    continue;
                }

                float bank = height(x, z) - water;
                if (bank < TradingPostSites.MinBank || bank > TradingPostSites.MaxBank)
                {
                    continue;
                }

                Heightmap.Biome biome = WorldGenerator.instance.GetBiome(x, z);
                if (!found.TryGetValue(biome, out List<Site> sites)
                    || !TradingPostSites.TryFind(origin, height, water, out float yaw, out float deck, out _))
                {
                    continue;
                }

                if (IsFree(origin, buildings))
                {
                    sites.Add(new Site(origin, yaw, deck));
                }
            }
        }

        if (ZNet.instance == null || ZoneSystem.instance == null)
        {
            running = false;
            yield break;
        }

        Build(found);
        ZoneSystem.instance.SetGlobalKey(DoneKey);
        running = false;
    }

    // Picks and builds the posts of each biome, counting the posts already standing in it.
    private static void Build(Dictionary<Heightmap.Biome, List<Site>> found)
    {
        List<ZDO> standing = TradingPostBuilder.Markers().ToList();
        List<Vector2> taken = standing.Select(zdo => new Vector2(zdo.GetPosition().x, zdo.GetPosition().z)).ToList();
        System.Random random = new(WorldGenerator.instance.GetSeed());
        List<string> summary = new();
        foreach (Heightmap.Biome biome in biomes)
        {
            List<Site> sites = found[biome];
            int have = standing.Count(zdo => zdo.GetInt(TradingPostKeys.Biome) == (int)biome);
            int wanted = Mathf.Max(0, TradingPostSettings.PerBiome.Value - have);
            List<Vector2> picks = TradingPostSites.Pick(sites.Select(site => site.Origin), taken, wanted, TradingPostSettings.MinDistance.Value, random);
            foreach (Vector2 pick in picks)
            {
                Site site = sites.First(candidate => candidate.Origin == pick);
                TradingPostBuilder.Build(new Vector3(pick.x, site.Deck, pick.y), site.Yaw, biome);
            }

            summary.Add($"{biome} {have + picks.Count} ({sites.Count} places suit)");
        }

        Jotunn.Logger.LogInfo($"Trading posts placed: {string.Join(", ", summary)}.");
    }

    // Anything a player has built has its creator set; collected per zone, a few sectors per frame.
    private IEnumerator CollectBuildings(Dictionary<Vector2s, List<Vector3>> buildings)
    {
        foreach (List<ZDO> objects in ZDOMan.instance.m_objectsBySector)
        {
            if (watch.Elapsed.TotalMilliseconds > FrameBudgetMs)
            {
                yield return null;
                if (ZDOMan.instance == null)
                {
                    yield break;
                }

                watch.Restart();
            }

            if (objects == null)
            {
                continue;
            }

            foreach (ZDO zdo in objects)
            {
                if (zdo.GetLong(ZDOVars.s_creator) == 0L)
                {
                    continue;
                }

                Vector3 position = zdo.GetPosition();
                Vector2s zone = ZoneSystem.GetZone(position);
                if (!buildings.TryGetValue(zone, out List<Vector3> list))
                {
                    list = new List<Vector3>();
                    buildings.Add(zone, list);
                }

                list.Add(position);
            }
        }
    }

    // Whether a post's area is clear of what players built and of the world's locations (Haldor, altars, villages).
    private static bool IsFree(Vector2 origin, Dictionary<Vector2s, List<Vector3>> buildings)
    {
        Vector3 point = new(origin.x, 0f, origin.y);
        float reach = TradingPostSettings.BuildingDistance.Value;
        Vector2s min = ZoneSystem.GetZone(point - new Vector3(reach, 0f, reach));
        Vector2s max = ZoneSystem.GetZone(point + new Vector3(reach, 0f, reach));
        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                if (buildings.TryGetValue(new Vector2s(x, y), out List<Vector3> built) && built.Any(position => Utils.DistanceXZ(position, point) < reach))
                {
                    return false;
                }
            }
        }

        float radius = TradingPostLayout.Load().dome.radius;
        Vector2s zone = ZoneSystem.GetZone(point);
        for (int x = zone.x - 1; x <= zone.x + 1; x++)
        {
            for (int y = zone.y - 1; y <= zone.y + 1; y++)
            {
                if (ZoneSystem.instance.m_locationInstances.TryGetValue(new Vector2s(x, y), out ZoneSystem.LocationInstance location)
                    && Utils.DistanceXZ(location.m_position, point) < (location.m_location?.m_exteriorRadius ?? 0f) + radius)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private readonly struct Site
    {
        public Site(Vector2 origin, float yaw, float deck)
        {
            Origin = origin;
            Yaw = yaw;
            Deck = deck;
        }

        public Vector2 Origin { get; }

        public float Yaw { get; }

        public float Deck { get; }
    }
}
