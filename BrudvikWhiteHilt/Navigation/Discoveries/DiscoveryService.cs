using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Pieces.Navigation;
using BrudvikWhiteHilt.Pieces.Portals.PortalMap;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Navigation.Discoveries;

/// <summary>
/// Runs on the <see cref="Game"/> object. On the server it searches the world for caves, settlements, wild plants,
/// deposits and landmarks while Munin's Perch stands at a map table, keeps what is uncovered on that table's map, counts
/// plants and deposits per square and sends the list to every player: the whole list once, after that only what changed.
/// On a client it receives that list. The world is read through the server's shared <see cref="WorldScan"/>.
/// </summary>
public class DiscoveryService : MonoBehaviour, WorldScan.IClient
{
    private const string RpcName = "WhiteHiltDiscoveries";
    private const string ResyncRpcName = "WhiteHiltDiscoveriesResync";

    // A client that missed a version asks for the whole list again, but not more often than this.
    private const float ResyncPause = 10f;

    // Dungeon interiors lie far above the world.
    private const float InteriorHeight = 3000f;

    // How often new players get the list; it only goes out when it is new to them.
    private const float PublishInterval = 5f;

    // The vanilla map: 12 m per pixel (read from the game's Minimap), used where the server has no map of its own.
    private const float VanillaPixelSize = 12f;

    // The game keeps one entry per peer; the local player of a hosting client is not a peer.
    private const long LocalKey = long.MinValue;

    private readonly List<RawFind> finds = new();
    private readonly HashSet<Vector2Int> builtCells = new();
    private readonly List<(ZDOID Id, Vector3 Position)> mapTables = new();
    private readonly List<Vector3> perches = new();
    private readonly Dictionary<long, int> lastSent = new();
    private readonly Dictionary<string, int> kindIndex = new();
    private readonly List<DiscoveryKind> kinds = new();
    private readonly Dictionary<string, int> locationKinds = new();

    // The table of kinds as sent; it only grows, so the markers players hold keep their kind.
    private readonly List<DiscoveryKind> sentKinds = new();
    private readonly Dictionary<string, int> sentKindIndex = new();

    private Dictionary<int, PrefabInfo> prefabs;
    private HashSet<int> piecePrefabs;
    private HashSet<int> mapTablePrefabs;
    private HashSet<int> wantedPrefabs;
    private WorldScan scan;
    private int perchPrefab;
    private string classifiedWith;
    private string exploredKey;
    private bool[] explored;
    private int exploredSize;
    private List<DiscoveryEntry> snapshot;
    private int version;
    private byte[] wholeBytes;
    private byte[] changesBytes;
    private float nextScan;
    private float nextPublish;

    // On a client: the list as the server sent it, in the server's order.
    private List<DiscoveryKind> clientKinds;
    private List<DiscoveryEntry> clientEntries;
    private int clientVersion = -1;
    private float resyncAt = float.NegativeInfinity;

    private void Start()
    {
        DiscoveryOverlay.SetData(new List<DiscoveryKind>(), new List<DiscoveryEntry>());
        scan = WorldScanService.On(gameObject);
        ZRoutedRpc.instance?.Register<ZPackage>(RpcName, RPC_Discoveries);
        ZRoutedRpc.instance?.Register(ResyncRpcName, RPC_Resync);
    }

    // On the server: scans the world now and then, a slice of each frame, and sends the result to the players.
    private void Update()
    {
        if (ZNet.instance == null || !ZNet.instance.IsServer() || ZDOMan.instance == null || ZNetScene.instance == null || ZoneSystem.instance == null)
        {
            return;
        }

        try
        {
            if (Time.time >= nextScan && !scan.IsRunning(this))
            {
                BeginScan();
            }

            if (snapshot != null && Time.time >= nextPublish)
            {
                nextPublish = Time.time + PublishInterval;
                Publish();
            }
        }
        catch (Exception ex)
        {
            ((WorldScan.IClient)this).Fail(ex);
        }
    }

    private void BeginScan()
    {
        string lists = $"{DiscoverySettings.ResourceItemList.Value}|{DiscoverySettings.IgnoredItemList.Value}|{DiscoverySettings.PlantItemList.Value}";
        if (prefabs == null || lists != classifiedWith)
        {
            classifiedWith = lists;
            FindPrefabs();
        }

        finds.Clear();
        builtCells.Clear();
        mapTables.Clear();
        perches.Clear();
        scan.Begin(this, wantedPrefabs);
    }

    void WorldScan.IClient.Finish()
    {
        nextScan = Time.time + DiscoverySettings.ScanInterval.Value;
        FinishScan();
    }

    // A failed scan is logged and tried again later instead of stopping the feature.
    void WorldScan.IClient.Fail(Exception error)
    {
        Jotunn.Logger.LogWarning($"Discoveries: scan failed: {error.Message}");
        nextScan = Time.time + DiscoverySettings.ScanInterval.Value;
    }

    // Values are read at once: ZDOs are pooled, so a reference kept to the next frame may be another object. Only the
    // prefabs in wantedPrefabs come here.
    void WorldScan.IClient.Collect(ZDO zdo, int prefab)
    {
        if (prefab == perchPrefab)
        {
            perches.Add(zdo.GetPosition());
            return;
        }

        if (mapTablePrefabs.Contains(prefab))
        {
            mapTables.Add((zdo.m_uid, zdo.GetPosition()));
        }

        bool piece = piecePrefabs.Contains(prefab);
        long creator = piece ? zdo.GetLong(ZDOVars.s_creator) : 0L;
        Vector3 position = zdo.GetPosition();
        if (creator != 0L)
        {
            builtCells.Add(Cell(position));
        }

        if (!prefabs.TryGetValue(prefab, out PrefabInfo info) || creator != 0L || position.y > InteriorHeight)
        {
            return;
        }

        bool ripe = false;
        if (info.Pickable)
        {
            ripe = !zdo.GetBool(ZDOVars.s_picked, info.DefaultPicked);
            if (!ripe && info.RespawnMinutes > 0f && ZNet.instance != null)
            {
                long pickedTime = zdo.GetLong(ZDOVars.s_pickedTime, 0L);
                ripe = pickedTime > 0L && (ZNet.instance.GetTime() - new DateTime(pickedTime)).TotalMinutes >= info.RespawnMinutes;
            }
        }

        finds.Add(new RawFind { Kind = info.Kind, Position = position, Pickable = info.Pickable, Ripe = ripe, GrownCrop = info.GrownCrop });
    }

    // Turns the finds into map markers, but only once a map table has Munin's perch near it. With the table-only
    // setting, only what is explored on the table's map is shown.
    private void FinishScan()
    {
        List<DiscoveryEntry> entries = new();
        List<Vector3> activeTables = mapTables
            .Where(table => perches.Any(perch => Vector3.Distance(perch, table.Position) <= PortalMapService.ActivationRange))
            .Select(table => table.Position)
            .ToList();
        if (DiscoverySettings.AllowDiscoveries.Value && activeTables.Count > 0)
        {
            bool onlyTable = DiscoverySettings.OnlyMapTableMap.Value;
            if (onlyTable)
            {
                ReadTableMaps();
            }

            AddClusters(entries, onlyTable);
            AddLocations(entries, onlyTable);
        }

        SetPackage(entries);
    }

    // Clusters the finds of a kind by cell, so a field of berries is one marker. Crops grown near buildings are
    // planted, not found, and left out.
    private void AddClusters(List<DiscoveryEntry> entries, bool onlyTable)
    {
        Dictionary<(int Kind, Vector2Int Cell), Cluster> clusters = new();
        foreach (RawFind find in finds)
        {
            if (!Allowed(find.Kind) || (find.GrownCrop && NearBuilt(Cell(find.Position))) || (onlyTable && !IsExplored(find.Position)))
            {
                continue;
            }

            (int, Vector2Int) key = (find.Kind, Cell(find.Position));
            if (!clusters.TryGetValue(key, out Cluster cluster))
            {
                cluster = new Cluster();
                clusters[key] = cluster;
            }

            cluster.Sum += find.Position;
            cluster.Count++;
            cluster.Members.Add(find);
            cluster.Pickable |= find.Pickable;
            cluster.Ripe += find.Ripe ? 1 : 0;
        }

        foreach (KeyValuePair<(int Kind, Vector2Int Cell), Cluster> pair in clusters)
        {
            entries.Add(new DiscoveryEntry
            {
                Kind = pair.Key.Kind,
                Position = MarkerPosition(pair.Value),
                Count = pair.Value.Count,
                Ripe = pair.Value.Pickable ? pair.Value.Ripe : -1
            });
        }
    }

    // The middle of a square's finds can lie between two groups with nothing there, so the marker goes on the find
    // nearest to it, an unpicked one where the square has any, and pinging it leads to something to pick.
    private static Vector3 MarkerPosition(Cluster cluster)
    {
        Vector3 middle = cluster.Sum / cluster.Count;
        bool anyRipe = cluster.Pickable && cluster.Ripe > 0;
        Vector3 best = middle;
        float bestDistance = float.MaxValue;
        foreach (RawFind member in cluster.Members)
        {
            if (anyRipe && !member.Ripe)
            {
                continue;
            }

            float distance = Utils.DistanceXZ(member.Position, middle);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = member.Position;
            }
        }

        return best;
    }

    // Adds the world's locations (dungeons, camps and so on) of the kinds shown: with the table-only setting those
    // explored on the table's map, otherwise those already placed in the world.
    private void AddLocations(List<DiscoveryEntry> entries, bool onlyTable)
    {
        foreach (ZoneSystem.LocationInstance instance in ZoneSystem.instance.m_locationInstances.Values)
        {
            string name = instance.m_location?.m_prefabName;
            if (string.IsNullOrEmpty(name) || (!onlyTable && !instance.m_placed) || (onlyTable && !IsExplored(instance.m_position)))
            {
                continue;
            }

            if (!locationKinds.TryGetValue(name, out int kind))
            {
                kind = DiscoveryCatalog.TryClassifyLocation(name, out string key, out DiscoveryGroup group) ? KindIndex(key, group) : -1;
                locationKinds[name] = kind;
            }

            if (kind >= 0 && Allowed(kind))
            {
                entries.Add(new DiscoveryEntry { Kind = kind, Position = instance.m_position, Count = 1 });
            }
        }
    }

    private bool Allowed(int kind)
    {
        DiscoveryKind info = kinds[kind];
        return DiscoverySettings.Allows(info.Group)
            && (DiscoverySettings.AllowSilver.Value || info.Key != DiscoveryCatalog.ItemKey("SilverOre"));
    }

    // Planted crops grow into the same pickables as wild ones, without a builder; near built pieces they count as planted.
    private bool NearBuilt(Vector2Int cell)
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (builtCells.Contains(new Vector2Int(cell.x + x, cell.y + y)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static Vector2Int Cell(Vector3 position)
    {
        float size = DiscoverySettings.ClusterSize.Value;
        return new Vector2Int(Mathf.FloorToInt(position.x / size), Mathf.FloorToInt(position.z / size));
    }

    // The tables' maps are read again only when one of them was written to.
    private void ReadTableMaps()
    {
        List<ZDO> tables = mapTables
            .Where(table => perches.Any(perch => Vector3.Distance(perch, table.Position) <= PortalMapService.ActivationRange))
            .Select(table => ZDOMan.instance.GetZDO(table.Id))
            .Where(zdo => zdo != null)
            .ToList();
        string key = string.Join(",", tables.Select(zdo => $"{zdo.m_uid}:{zdo.DataRevision}").OrderBy(text => text));
        if (key == exploredKey)
        {
            return;
        }

        exploredKey = key;
        explored = null;
        exploredSize = 0;
        foreach (ZDO zdo in tables)
        {
            byte[] stored = zdo.GetByteArray(ZDOVars.s_data);
            if (stored == null)
            {
                continue;
            }

            // The table's map: version, pixel count and one byte per pixel, then the pins (Minimap.GetSharedMapData).
            byte[] data = Utils.Decompress(stored);
            int count = data.Length >= 8 ? BitConverter.ToInt32(data, 4) : 0;
            int size = Mathf.RoundToInt(Mathf.Sqrt(count));
            if (count <= 0 || size * size != count || data.Length < 8 + count || (explored != null && size != exploredSize))
            {
                continue;
            }

            if (explored == null)
            {
                explored = new bool[count];
                exploredSize = size;
            }

            for (int i = 0; i < count; i++)
            {
                explored[i] |= data[8 + i] != 0;
            }
        }
    }

    private bool IsExplored(Vector3 position)
    {
        if (explored == null)
        {
            return false;
        }

        float pixelSize = Minimap.instance != null ? Minimap.instance.m_pixelSize : VanillaPixelSize;
        int half = exploredSize / 2;
        int x = Mathf.RoundToInt(position.x / pixelSize + half);
        int y = Mathf.RoundToInt(position.z / pixelSize + half);
        return x >= 0 && y >= 0 && x < exploredSize && y < exploredSize && explored[y * exploredSize + x];
    }

    // Makes the new version of the list from a scan: the old list with what changed, in the order the players build it
    // too, and the changes packed for those who have the old version.
    private void SetPackage(List<DiscoveryEntry> entries)
    {
        // A kind moved to another group by the settings: the table starts over, and everyone gets the whole list.
        if (sentKinds.Exists(sent => kindIndex.TryGetValue(sent.Key, out int kind) && kinds[kind].Group != sent.Group))
        {
            sentKinds.Clear();
            sentKindIndex.Clear();
            snapshot = null;
        }

        entries.ForEach(entry => entry.Kind = SentKind(entry.Kind));
        nextPublish = 0f;
        DiscoveryChanges changes = snapshot != null ? DiscoveryPackage.Diff(snapshot, entries) : null;
        if (changes != null && changes.IsEmpty)
        {
            return;
        }

        version++;
        List<DiscoveryEntry> next = null;
        if (changes != null)
        {
            changes.BaseVersion = version - 1;
            changes.Version = version;
            changes.Kinds = sentKinds;
            next = DiscoveryPackage.Apply(snapshot, changes);
        }

        changesBytes = next != null ? ServerLists.Pack(DiscoveryPackage.WriteChanges(changes)).GetArray() : null;
        snapshot = next ?? entries;
        wholeBytes = ServerLists.Pack(DiscoveryPackage.WriteWhole(version, sentKinds, snapshot)).GetArray();
        if (changesBytes != null && changesBytes.Length >= wholeBytes.Length)
        {
            changesBytes = null;
        }
    }

    private int SentKind(int kind)
    {
        string key = kinds[kind].Key;
        if (!sentKindIndex.TryGetValue(key, out int index))
        {
            index = sentKinds.Count;
            sentKindIndex[key] = index;
            sentKinds.Add(new DiscoveryKind { Key = key, Group = kinds[kind].Group });
        }

        return index;
    }

    // Sends each player who has not had this version yet what changed since the version before, or the whole list to
    // one who joined, missed a version or has an older list still waiting to be sent. The host playing on its own
    // machine gets it directly.
    private void Publish()
    {
        HashSet<long> current = new();
        foreach (ZNetPeer peer in ZNet.instance.GetPeers().Where(peer => peer.IsReady()))
        {
            current.Add(peer.m_uid);
            bool known = lastSent.TryGetValue(peer.m_uid, out int sent);
            if (known && sent == version)
            {
                continue;
            }

            bool changesFit = known && sent == version - 1 && changesBytes != null && !ServerLists.IsWaiting(peer.m_uid, RpcName);
            ServerLists.SendPacked(peer.m_uid, RpcName, changesFit ? changesBytes : wholeBytes);
            lastSent[peer.m_uid] = version;
        }

        if (!ZNet.instance.IsDedicated() && Game.instance != null)
        {
            current.Add(LocalKey);
            if (!lastSent.TryGetValue(LocalKey, out int sent) || sent != version)
            {
                lastSent[LocalKey] = version;
                ShowOnMap(sentKinds, snapshot);
            }
        }

        foreach (long gone in lastSent.Keys.Where(key => !current.Contains(key)).ToList())
        {
            lastSent.Remove(gone);
        }
    }

    // Sorts the game's prefabs once into the kinds of discoveries, and notes the map tables and pieces, so the scan
    // only needs to look up a prefab hash.
    private void FindPrefabs()
    {
        kinds.Clear();
        kindIndex.Clear();
        locationKinds.Clear();
        prefabs = new Dictionary<int, PrefabInfo>();
        piecePrefabs = new HashSet<int>();
        mapTablePrefabs = new HashSet<int>();
        HashSet<string> grownCrops = new();
        foreach (GameObject prefab in ZNetScene.instance.m_prefabs.Where(prefab => prefab != null))
        {
            foreach (GameObject grown in prefab.GetComponent<Plant>()?.m_grownPrefabs ?? Array.Empty<GameObject>())
            {
                if (grown != null)
                {
                    grownCrops.Add(grown.name);
                }
            }
        }

        foreach (GameObject prefab in ZNetScene.instance.m_prefabs.Where(prefab => prefab != null))
        {
            int hash = prefab.name.GetStableHashCode();
            if (prefab.GetComponent<Piece>() != null)
            {
                piecePrefabs.Add(hash);
            }

            if (prefab.GetComponent<MapTable>() != null)
            {
                mapTablePrefabs.Add(hash);
            }

            if (TryClassify(prefab, out string key, out DiscoveryGroup group, out Pickable pickable))
            {
                prefabs[hash] = new PrefabInfo
                {
                    Kind = KindIndex(key, group),
                    Pickable = pickable != null,
                    DefaultPicked = pickable != null && pickable.m_defaultPicked,
                    RespawnMinutes = pickable != null ? pickable.m_respawnTimeMinutes : 0f,
                    GrownCrop = grownCrops.Contains(prefab.name)
                };
            }
        }

        perchPrefab = MuninsPerch.PrefabName.GetStableHashCode();
        wantedPrefabs = new HashSet<int>(piecePrefabs.Concat(mapTablePrefabs).Concat(prefabs.Keys)) { perchPrefab };
        Jotunn.Logger.LogInfo($"Discoveries: {prefabs.Count} prefabs in {kinds.Count} kinds, {mapTablePrefabs.Count} map table prefabs");
    }

    // What a prefab can be discovered as: a plant or a resource from what it can be picked for, or a resource from what
    // it drops. Items set to be ignored are left out.
    private static bool TryClassify(GameObject prefab, out string key, out DiscoveryGroup group, out Pickable pickable)
    {
        key = null;
        group = default;
        pickable = prefab.GetComponent<Pickable>();
        if (pickable != null)
        {
            ItemDrop item = pickable.m_itemPrefab != null ? pickable.m_itemPrefab.GetComponent<ItemDrop>() : null;
            string name = pickable.m_itemPrefab != null ? pickable.m_itemPrefab.name : null;
            if (item == null || DiscoverySettings.IsIgnored(name))
            {
                return false;
            }

            key = DiscoveryCatalog.ItemKey(name);
            if (DiscoverySettings.IsResource(name))
            {
                group = DiscoveryGroup.Resources;
                return true;
            }

            group = DiscoveryGroup.Plants;
            return item.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable
                || pickable.m_respawnTimeMinutes > 0f
                || name.EndsWith("Seeds", StringComparison.Ordinal)
                || DiscoverySettings.IsPlantItem(name);
        }

        GameObject resource = ResourceDrop(prefab, 0);
        if (resource == null)
        {
            return false;
        }

        key = DiscoveryCatalog.ItemKey(resource.name);
        group = DiscoveryGroup.Resources;
        return true;
    }

    // An intact deposit (rock4_copper) is a Destructible that turns into the minable rock, which holds the drops.
    private static GameObject ResourceDrop(GameObject prefab, int depth)
    {
        if (prefab == null || depth > 2)
        {
            return null;
        }

        DropTable drops = prefab.GetComponentInChildren<MineRock5>(true)?.m_dropItems ?? prefab.GetComponentInChildren<MineRock>(true)?.m_dropItems;
        Destructible destructible = prefab.GetComponent<Destructible>();
        if (drops == null && destructible != null)
        {
            drops = prefab.GetComponent<DropOnDestroyed>()?.m_dropWhenDestroyed;
        }

        GameObject resource = drops?.m_drops?.Select(drop => drop.m_item).FirstOrDefault(drop => drop != null && DiscoverySettings.IsResource(drop.name));
        return resource != null || destructible == null ? resource : ResourceDrop(destructible.m_spawnWhenDestroyed, depth + 1);
    }

    private int KindIndex(string key, DiscoveryGroup group)
    {
        if (!kindIndex.TryGetValue(key, out int index))
        {
            index = kinds.Count;
            kindIndex[key] = index;
            kinds.Add(new DiscoveryKind { Key = key, Group = group });
        }

        return index;
    }

    private void RPC_Discoveries(long sender, ZPackage received)
    {
        if (sender == ZRoutedRpc.instance.GetServerPeerID() && ServerLists.Receive(sender, RpcName, received) is ZPackage list)
        {
            Apply(list);
        }
    }

    // On the server: a player missed a version, so the next round sends them the whole list.
    private void RPC_Resync(long sender)
    {
        if (ZNet.instance != null && ZNet.instance.IsServer())
        {
            lastSent.Remove(sender);
        }
    }

    // Takes the whole list, or changes to the list this client has. Changes to another version are of no use, and the
    // whole list is asked for again.
    private void Apply(ZPackage received)
    {
        try
        {
            if (!DiscoveryPackage.Read(received, out int receivedVersion, out List<DiscoveryKind> receivedKinds,
                    out List<DiscoveryEntry> receivedEntries, out DiscoveryChanges changes))
            {
                return;
            }

            if (changes != null)
            {
                receivedKinds = changes.Kinds;
                receivedEntries = changes.BaseVersion == clientVersion && clientEntries != null ? DiscoveryPackage.Apply(clientEntries, changes) : null;
                if (receivedEntries == null)
                {
                    AskForWholeList();
                    return;
                }
            }

            clientKinds = receivedKinds;
            clientEntries = receivedEntries;
            clientVersion = receivedVersion;
            ShowOnMap(clientKinds, clientEntries);
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"Discoveries: could not read the server's list: {ex.Message}");
        }
    }

    private void AskForWholeList()
    {
        clientVersion = -1;
        if (Time.time - resyncAt >= ResyncPause && ZRoutedRpc.instance != null)
        {
            resyncAt = Time.time;
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), ResyncRpcName);
        }
    }

    private static void ShowOnMap(List<DiscoveryKind> kinds, List<DiscoveryEntry> entries)
    {
        DiscoveryPackage.ForMap(kinds, entries, out List<DiscoveryKind> shownKinds, out List<DiscoveryEntry> shownEntries);
        DiscoveryOverlay.SetData(shownKinds, shownEntries);
    }

    private struct RawFind
    {
        public int Kind;
        public Vector3 Position;
        public bool Pickable;
        public bool Ripe;
        public bool GrownCrop;
    }

    private sealed class PrefabInfo
    {
        public int Kind;
        public bool Pickable;
        public bool DefaultPicked;
        public float RespawnMinutes;
        public bool GrownCrop;
    }

    private sealed class Cluster
    {
        public readonly List<RawFind> Members = new();
        public Vector3 Sum;
        public int Count;
        public int Ripe;
        public bool Pickable;
    }
}
