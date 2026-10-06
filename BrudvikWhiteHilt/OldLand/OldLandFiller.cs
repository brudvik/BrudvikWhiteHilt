using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.OldLand;

/// <summary>
/// Runs on the server's <see cref="Game"/> object. The zone generator only places vegetation in land it generates, so
/// content added as vegetation (spider nests, slate outcrops, forageables) is missing from land generated before it came.
/// Once per world and kind, this gives that land its share: each zone gets the generator's own roll and placement rules,
/// with the heights of the terrain the game builds there, on free spots away from anything built, unless the zone already
/// has the kind. <see cref="OldLandGrounding"/> puts objects placed before those heights were right back on the ground.
/// </summary>
public class OldLandFiller : MonoBehaviour
{
    private const string KeyPrefix = "whitehilt_oldland_";
    private const double FrameBudgetMs = 2.0;

    private static readonly Dictionary<string, Entry> entries = new();

    private readonly Dictionary<Vector2s, List<Vector3>> buildings = new();
    private readonly List<ZDO> nearby = new();
    private readonly HashSet<int> zonePrefabs = new();
    private readonly Stopwatch watch = new();

    /// <summary>
    /// Registers vegetation added by the mod, to be placed in old land too.
    /// </summary>
    /// <param name="vegetation">The vegetation entry, as injected into the zone system.</param>
    /// <param name="clearRadius">Metres the spot must be clear of trees, rocks and other objects.</param>
    /// <param name="alsoCounts">Other prefabs that show the zone already has it, such as a mined rock's broken form.</param>
    public static void Register(ZoneSystem.ZoneVegetation vegetation, float clearRadius, params string[] alsoCounts)
    {
        if (vegetation?.m_prefab == null)
        {
            return;
        }

        OldLandGrounding grounding = vegetation.m_prefab.GetComponent<OldLandGrounding>();
        if (grounding == null)
        {
            grounding = vegetation.m_prefab.AddComponent<OldLandGrounding>();
        }

        grounding.GroundOffset = vegetation.m_groundOffset;

        string name = vegetation.m_prefab.name;
        HashSet<int> hashes = new(alsoCounts.Select(other => other.GetStableHashCode())) { name.GetStableHashCode() };
        entries[name] = new Entry(vegetation, name.GetStableHashCode(), hashes, clearRadius, KeyPrefix + name.ToLowerInvariant());
    }

    // On the server, once the world's locations are placed: starts filling the already explored land with new
    // vegetation (such as nests) that was added after it was generated.
    private void Update()
    {
        ZNet net = ZNet.instance;
        ZoneSystem zones = ZoneSystem.instance;
        if (net == null || zones == null || ZDOMan.instance == null || WorldGenerator.instance == null || ZNetScene.instance == null)
        {
            return;
        }

        if (!net.IsServer())
        {
            Destroy(this);
            return;
        }

        // A new world places its locations over several frames, and nothing may land inside one.
        if (!zones.m_locationsGenerated)
        {
            return;
        }

        enabled = false;
        if (!OldLandSettings.Enabled.Value)
        {
            return;
        }

        List<Entry> pending = entries.Values
            .Where(entry => entry.Vegetation.m_enable && !zones.GetGlobalKey(entry.DoneKey) && ZNetScene.instance.GetPrefab(entry.Hash) != null)
            .Where(entry =>
            {
                if (Supported(entry.Vegetation))
                {
                    return true;
                }

                Jotunn.Logger.LogWarning($"Old land: {entry.Vegetation.m_prefab.name} uses placement rules that need a loaded zone, skipped");
                return false;
            })
            .ToList();
        if (pending.Count > 0)
        {
            StartCoroutine(Fill(pending));
        }
    }

    // Rules that need the zone's heightmap or its neighbours' vegetation cannot be judged from the world generator.
    private static bool Supported(ZoneSystem.ZoneVegetation vegetation)
    {
        return !vegetation.m_snapToWater && !vegetation.m_snapToStaticSolid && !vegetation.m_surroundCheckVegetation
            && vegetation.m_minOceanDepth.Equals(vegetation.m_maxOceanDepth) && vegetation.m_minVegetation.Equals(vegetation.m_maxVegetation)
            && vegetation.m_terrainDeltaRadius <= 0f && vegetation.m_minDistanceFromCenter <= 0f && vegetation.m_maxDistanceFromCenter <= 0f;
    }

    // Places the new vegetation in every generated zone the way the game would, a few zones per frame so the server
    // does not stall; marks each kind done with a global key so it is only done once per world.
    private IEnumerator Fill(List<Entry> pending)
    {
        watch.Restart();
        IEnumerator collect = CollectBuildings();
        while (collect.MoveNext())
        {
            yield return null;
        }

        List<Vector2s> generated = new(ZoneSystem.instance.m_generatedZones);
        int seed = WorldGenerator.instance.GetSeed();
        foreach (Vector2s zone in generated)
        {
            if (watch.Elapsed.TotalMilliseconds > FrameBudgetMs)
            {
                yield return null;
                if (ZNet.instance == null || ZoneSystem.instance == null || ZDOMan.instance == null)
                {
                    yield break;
                }

                watch.Restart();
            }

            bool gathered = false;
            foreach (Entry entry in pending)
            {
                Random.State state = Random.state;
                try
                {
                    // The generator's own seed, so land generated after the kind came gets no second roll.
                    Random.InitState(seed + zone.x * 4271 + zone.y * 9187 + entry.Hash);
                    entry.Placed += PlaceIn(zone, entry, ref gathered);
                }
                finally
                {
                    Random.state = state;
                }
            }
        }

        foreach (Entry entry in pending)
        {
            ZoneSystem.instance.SetGlobalKey(entry.DoneKey);
            Jotunn.Logger.LogInfo($"Old land: placed {entry.Placed} {entry.Vegetation.m_prefab.name} ({generated.Count} zones checked)");
        }
    }

    // Anything a player has built has its creator set.
    private IEnumerator CollectBuildings()
    {
        buildings.Clear();
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

    // Mirrors ZoneSystem.PlaceVegetation for one zone, in the same order of random draws.
    private int PlaceIn(Vector2s zone, Entry entry, ref bool gathered)
    {
        ZoneSystem.ZoneVegetation vegetation = entry.Vegetation;
        int count = 1;
        if (vegetation.m_max < 1f)
        {
            if (Random.value > vegetation.m_max)
            {
                return 0;
            }
        }
        else
        {
            count = Random.Range((int)vegetation.m_min, (int)vegetation.m_max + 1);
        }

        Vector3 centre = ZoneSystem.GetZonePos(zone);
        float spread = ZoneSystem.instance.m_zoneSize / 2f - vegetation.m_groupRadius;
        float minNormal = Mathf.Cos(Mathf.Deg2Rad * vegetation.m_maxTilt);
        float maxNormal = Mathf.Cos(Mathf.Deg2Rad * vegetation.m_minTilt);
        int tries = vegetation.m_forcePlacement ? count * 50 : count;
        int groups = 0;
        int placed = 0;
        bool checkedZone = false;
        for (int i = 0; i < tries && groups < count; i++)
        {
            Vector3 start = new(Random.Range(centre.x - spread, centre.x + spread), 0f, Random.Range(centre.z - spread, centre.z + spread));
            int size = Random.Range(vegetation.m_groupSizeMin, vegetation.m_groupSizeMax + 1);
            bool any = false;
            for (int j = 0; j < size; j++)
            {
                Vector3 point = j == 0 ? start : RandomPointInRadius(start, vegetation.m_groupRadius);
                float yaw = Random.Range(0, 360);
                float scale = Random.Range(vegetation.m_scaleMin, vegetation.m_scaleMax);
                float tiltX = Random.Range(0f - vegetation.m_randTilt, vegetation.m_randTilt);
                float tiltZ = Random.Range(0f - vegetation.m_randTilt, vegetation.m_randTilt);
                if (!Fits(vegetation, ref point, minNormal, maxNormal, out Vector3 normal))
                {
                    continue;
                }

                if (!gathered)
                {
                    gathered = true;
                    Gather(zone);
                }

                if (!checkedZone)
                {
                    checkedZone = true;
                    if (zonePrefabs.Overlaps(entry.Hashes))
                    {
                        return 0;
                    }
                }

                if (!IsFree(point, vegetation.m_blockCheck ? entry.ClearRadius : 0f))
                {
                    continue;
                }

                Quaternion rotation = vegetation.m_chanceToUseGroundTilt > 0f && Random.value <= vegetation.m_chanceToUseGroundTilt
                    ? Quaternion.LookRotation(Vector3.Cross(normal, Quaternion.Euler(0f, yaw, 0f) * Vector3.forward), normal)
                    : Quaternion.Euler(tiltX, yaw, tiltZ);
                point.y += vegetation.m_groundOffset;
                nearby.Add(Create(vegetation.m_prefab, entry.Hash, point, rotation, scale));
                any = true;
                placed++;
            }

            if (any)
            {
                groups++;
            }
        }

        return placed;
    }

    private static Vector3 RandomPointInRadius(Vector3 centre, float radius)
    {
        float angle = Random.value * Mathf.PI * 2f;
        float distance = Random.Range(0f, radius);
        return centre + new Vector3(Mathf.Sin(angle) * distance, 0f, Mathf.Cos(angle) * distance);
    }

    // Whether a point suits the vegetation: biome, altitude, slope and forest, as the game checks them.
    private static bool Fits(ZoneSystem.ZoneVegetation vegetation, ref Vector3 point, float minNormal, float maxNormal, out Vector3 normal)
    {
        normal = Vector3.up;
        WorldGenerator world = WorldGenerator.instance;
        if ((world.GetBiome(point.x, point.z) & vegetation.m_biome) == 0 || (world.GetBiomeArea(point) & vegetation.m_biomeArea) == 0)
        {
            return false;
        }

        point.y = TerrainHeight(point.x, point.z);
        float altitude = point.y - ZoneSystem.instance.m_waterLevel;
        if (altitude < vegetation.m_minAltitude || altitude > vegetation.m_maxAltitude)
        {
            return false;
        }

        normal = new Vector3(TerrainHeight(point.x - 1f, point.z) - TerrainHeight(point.x + 1f, point.z), 2f,
            TerrainHeight(point.x, point.z - 1f) - TerrainHeight(point.x, point.z + 1f)).normalized;
        if (normal.y < minNormal || normal.y > maxNormal)
        {
            return false;
        }

        if (vegetation.m_inForest)
        {
            float forest = WorldGenerator.GetForestFactor(point);
            if (forest < vegetation.m_forestTresholdMin || forest > vegetation.m_forestTresholdMax)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// The height of the terrain the game builds at a point. Its heightmap blends the heights of the zone's four corner
    /// biomes over the whole zone (HeightmapBuilder), so near a biome border the ground lies well away from the height of
    /// the biome at the point: metres higher in a Swamp beside a Meadow or Black Forest, burying what was placed there.
    /// </summary>
    /// <param name="x">World x.</param>
    /// <param name="z">World z.</param>
    /// <returns>The height, before any terrain changes.</returns>
    internal static float TerrainHeight(float x, float z)
    {
        WorldGenerator world = WorldGenerator.instance;
        float size = ZoneSystem.instance.m_zoneSize;
        int vertices = Mathf.RoundToInt(size);
        Vector3 corner = ZoneSystem.GetZonePos(ZoneSystem.GetZone(new Vector3(x, 0f, z))) - new Vector3(size / 2f, 0f, size / 2f);
        Heightmap.Biome b0 = world.GetBiome(corner.x, corner.z);
        Heightmap.Biome b1 = world.GetBiome(corner.x + size, corner.z);
        Heightmap.Biome b2 = world.GetBiome(corner.x, corner.z + size);
        Heightmap.Biome b3 = world.GetBiome(corner.x + size, corner.z + size);
        bool single = b0 == b1 && b0 == b2 && b0 == b3;

        float Vertex(int vx, int vz)
        {
            float wx = corner.x + vx;
            float wz = corner.z + vz;
            if (single)
            {
                return world.GetBiomeHeight(b0, wx, wz, out _);
            }

            float tx = Mathf.SmoothStep(0f, 1f, (float)vx / vertices);
            float tz = Mathf.SmoothStep(0f, 1f, (float)vz / vertices);
            float bottom = Mathf.Lerp(world.GetBiomeHeight(b0, wx, wz, out _), world.GetBiomeHeight(b1, wx, wz, out _), tx);
            float top = Mathf.Lerp(world.GetBiomeHeight(b2, wx, wz, out _), world.GetBiomeHeight(b3, wx, wz, out _), tx);
            return Mathf.Lerp(bottom, top, tz);
        }

        // The terrain mesh has a vertex every metre; between them the ground is close to the bilinear blend.
        float fx = x - corner.x;
        float fz = z - corner.z;
        int ix = Mathf.Clamp(Mathf.FloorToInt(fx), 0, vertices - 1);
        int iz = Mathf.Clamp(Mathf.FloorToInt(fz), 0, vertices - 1);
        float sx = Mathf.Clamp01(fx - ix);
        float sz = Mathf.Clamp01(fz - iz);
        return Mathf.Lerp(Mathf.Lerp(Vertex(ix, iz), Vertex(ix + 1, iz), sx), Mathf.Lerp(Vertex(ix, iz + 1), Vertex(ix + 1, iz + 1), sx), sz);
    }

    // The unbuilt objects around the zone, which block spots, and the prefabs the zone itself has.
    private void Gather(Vector2s zone)
    {
        nearby.Clear();
        zonePrefabs.Clear();
        List<ZDO>[] sectors = ZDOMan.instance.m_objectsBySector;
        for (int x = zone.x - 1; x <= zone.x + 1; x++)
        {
            for (int y = zone.y - 1; y <= zone.y + 1; y++)
            {
                uint index = ZoneSystem.SectorToIndex(x, y).Sector;
                List<ZDO> objects = index < sectors.Length ? sectors[index] : null;
                if (objects == null)
                {
                    continue;
                }

                bool own = x == zone.x && y == zone.y;
                foreach (ZDO zdo in objects)
                {
                    nearby.Add(zdo);
                    if (own)
                    {
                        zonePrefabs.Add(zdo.GetPrefab());
                    }
                }
            }
        }
    }

    // Whether a point is clear of buildings, other objects and locations.
    private bool IsFree(Vector3 point, float clearRadius)
    {
        float buildingDistance = OldLandSettings.BuildingDistance.Value;
        if (buildingDistance > 0f)
        {
            Vector2s min = ZoneSystem.GetZone(point - new Vector3(buildingDistance, 0f, buildingDistance));
            Vector2s max = ZoneSystem.GetZone(point + new Vector3(buildingDistance, 0f, buildingDistance));
            for (int x = min.x; x <= max.x; x++)
            {
                for (int y = min.y; y <= max.y; y++)
                {
                    if (buildings.TryGetValue(new Vector2s(x, y), out List<Vector3> built) && built.Any(position => Utils.DistanceXZ(position, point) < buildingDistance))
                    {
                        return false;
                    }
                }
            }
        }

        if (clearRadius > 0f && nearby.Any(zdo => Utils.DistanceXZ(zdo.GetPosition(), point) < clearRadius))
        {
            return false;
        }

        Vector2s zone = ZoneSystem.GetZone(point);
        for (int x = zone.x - 1; x <= zone.x + 1; x++)
        {
            for (int y = zone.y - 1; y <= zone.y + 1; y++)
            {
                if (ZoneSystem.instance.m_locationInstances.TryGetValue(new Vector2s(x, y), out ZoneSystem.LocationInstance location)
                    && Utils.DistanceXZ(location.m_position, point) < (location.m_location?.m_exteriorRadius ?? 0f) + clearRadius)
                {
                    return false;
                }
            }
        }

        return true;
    }

    // Creates an object in the world's network data only, as the game does for vegetation in unloaded zones; it comes
    // to life when a player comes near.
    private static ZDO Create(GameObject prefab, int hash, Vector3 position, Quaternion rotation, float scale)
    {
        ZNetView view = prefab.GetComponent<ZNetView>();
        ZDO zdo = ZDOMan.instance.CreateNewZDO(position, hash);
        zdo.Persistent = view.m_persistent;
        zdo.Type = view.m_type;
        zdo.Distant = view.m_distant;
        zdo.SetPrefab(hash);
        zdo.SetRotation(rotation);
        if (view.m_syncInitialScale && !scale.Equals(prefab.transform.localScale.x))
        {
            zdo.Set(ZDOVars.s_scaleHash, Vector3.one * scale);
        }

        // Nobody owns it until a player comes near; then that player's client takes it over.
        zdo.SetOwnerInternal(0L);
        return zdo;
    }

    private sealed class Entry
    {
        public Entry(ZoneSystem.ZoneVegetation vegetation, int hash, HashSet<int> hashes, float clearRadius, string doneKey)
        {
            Vegetation = vegetation;
            Hash = hash;
            Hashes = hashes;
            ClearRadius = clearRadius;
            DoneKey = doneKey;
        }

        public ZoneSystem.ZoneVegetation Vegetation { get; }

        public int Hash { get; }

        public HashSet<int> Hashes { get; }

        public float ClearRadius { get; }

        public string DoneKey { get; }

        public int Placed { get; set; }
    }
}
