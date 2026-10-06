using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace BrudvikWhiteHilt.Navigation.Areas;

/// <summary>
/// Runs on the <see cref="Game"/> object. On the server it reads every object in the world, a few hundred sectors per
/// frame, and groups player-built pieces, planted crops and tamed animals into areas of 16 m cells; then it sends each
/// player the areas and wards they may see, only when they have changed. On a client it hands the list to
/// <see cref="MapAreaOverlay"/>.
/// </summary>
public class MapAreaService : MonoBehaviour
{
    private const string RpcName = "WhiteHiltMapAreas";
    private const float ScanPause = 30f;
    private const int SectorsPerFrame = 400;
    private const int MaxNameLength = 32;

    // Cells this far apart (in cells) still belong to one area, so a path or garden between houses does not split it.
    private const int Bridge = 2;

    // The game keeps one entry per peer; the local player of a hosting client is not a peer.
    private const long LocalKey = long.MinValue;

    private static readonly Regex tags = new("<.*?>", RegexOptions.Compiled);

    private readonly Dictionary<long, Cell> cells = new();
    private readonly List<MapWard> scanWards = new();
    private readonly List<(Vector2Int Cell, string Name)> signs = new();
    private readonly Dictionary<long, string> names = new();
    private readonly Dictionary<long, string> lastSent = new();

    private Dictionary<int, Traits> traits;
    private List<MapArea> areas = new();
    private List<MapWard> wards = new();
    private int sectorIndex = -1;
    private float nextScan;

    [Flags]
    private enum Traits
    {
        None = 0,
        Piece = 1,
        Bed = 2,
        Fire = 4,
        Bench = 8,
        Portal = 16,
        Crop = 32,
        PlantedCrop = 64,
        Animal = 128,
        Ward = 256,
        Sign = 512,
    }

    private void Start()
    {
        MapAreaOverlay.SetData(new List<MapArea>(), new List<MapWard>());
        ZRoutedRpc.instance?.Register<ZPackage>(RpcName, RPC_MapAreas);
    }

    // On the server: scans the world now and then, a few sectors per frame. A failed scan is logged and retried later
    // instead of stopping the feature.
    private void Update()
    {
        if (ZNet.instance == null || !ZNet.instance.IsServer() || ZDOMan.instance == null || ZNetScene.instance == null)
        {
            return;
        }

        try
        {
            if (sectorIndex < 0 && Time.time >= nextScan)
            {
                BeginScan();
            }

            if (sectorIndex >= 0)
            {
                ContinueScan();
            }
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"Map areas: scan failed: {ex.Message}");
            sectorIndex = -1;
            nextScan = Time.time + ScanPause;
        }
    }

    // On a client: takes the areas the server found; anything not from the server is ignored, so a player cannot draw
    // areas on others' maps.
    private void RPC_MapAreas(long sender, ZPackage package)
    {
        if (ZNet.instance == null || sender != ZRoutedRpc.instance.GetServerPeerID())
        {
            return;
        }

        try
        {
            if (MapAreaPackage.Read(package, out List<MapArea> received, out List<MapWard> receivedWards))
            {
                MapAreaOverlay.SetData(received, receivedWards);
            }
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"Map areas: could not read the list from the server: {ex.Message}");
        }
    }

    private void BeginScan()
    {
        traits ??= FindTraits();
        cells.Clear();
        scanWards.Clear();
        signs.Clear();
        names.Clear();
        sectorIndex = MapAreaSettings.AllowAreas.Value ? 0 : ZDOMan.instance.m_objectsBySector.Length;
    }

    // Collects the objects of the next few sectors; when all sectors are done, groups them into areas. Spreading the
    // scan over frames keeps a large world from stalling the server.
    private void ContinueScan()
    {
        List<ZDO>[] sectors = ZDOMan.instance.m_objectsBySector;
        int budget = SectorsPerFrame;
        while (sectorIndex < sectors.Length && budget > 0)
        {
            List<ZDO> sector = sectors[sectorIndex++];
            if (sector == null)
            {
                continue;
            }

            budget--;
            sector.ForEach(Collect);
        }

        if (sectorIndex < sectors.Length)
        {
            return;
        }

        FinishScan();
        sectorIndex = -1;
        nextScan = Time.time + ScanPause;
    }

    // Sorts the game's prefabs once into what makes an area: planted crops, crops grown from them, animals and building
    // pieces with what they are (bed, fire, bench, portal, sign). Found by component, so other mods' pieces count too.
    private static Dictionary<int, Traits> FindTraits()
    {
        Dictionary<int, Traits> found = new();
        HashSet<int> grownCrops = new();
        foreach (GameObject prefab in ZNetScene.instance.m_prefabs.Where(prefab => prefab != null))
        {
            Plant plant = prefab.GetComponent<Plant>();
            if (plant != null && plant.m_grownPrefabs.Any(grown => grown != null && grown.GetComponent<Pickable>() != null))
            {
                found[prefab.name.GetStableHashCode()] = Traits.PlantedCrop;
                foreach (GameObject grown in plant.m_grownPrefabs.Where(grown => grown != null))
                {
                    grownCrops.Add(grown.name.GetStableHashCode());
                }
            }
        }

        foreach (GameObject prefab in ZNetScene.instance.m_prefabs.Where(prefab => prefab != null))
        {
            int hash = prefab.name.GetStableHashCode();
            if (found.ContainsKey(hash))
            {
                continue;
            }

            Traits kind = Classify(prefab, grownCrops.Contains(hash));
            if (kind != Traits.None)
            {
                found[hash] = kind;
            }
        }

        return found;
    }

    // What a prefab tells about the place it stands in, if anything. Ships and carts move about and say nothing about a
    // place.
    private static Traits Classify(GameObject prefab, bool grownCrop)
    {
        if (grownCrop)
        {
            return Traits.Crop;
        }

        if (prefab.GetComponent<Tameable>() != null)
        {
            return Traits.Animal;
        }

        if (prefab.GetComponent<Piece>() == null || prefab.GetComponent<Ship>() != null || prefab.GetComponent<Vagon>() != null
            || prefab.GetComponent<Plant>() != null)
        {
            return Traits.None;
        }

        // Pickables the Planting feature lets players plant (berry bushes, mushrooms) count as crops when planted.
        if (prefab.GetComponent<Pickable>() != null)
        {
            return Traits.PlantedCrop;
        }

        Traits kind = Traits.Piece;
        if (prefab.GetComponent<Bed>() != null)
        {
            kind |= Traits.Bed;
        }

        if (prefab.GetComponent<Fireplace>() != null)
        {
            kind |= Traits.Fire;
        }

        if (prefab.GetComponent<CraftingStation>() != null)
        {
            kind |= Traits.Bench;
        }

        if (prefab.GetComponent<TeleportWorld>() != null || prefab.name.IndexOf("portal", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            kind |= Traits.Portal;
        }

        if (prefab.GetComponent<PrivateArea>() != null)
        {
            kind |= Traits.Ward;
        }

        if (prefab.GetComponent<Sign>() != null)
        {
            kind |= Traits.Sign;
        }

        return kind;
    }

    // Values are read at once: ZDOs are pooled, so a reference kept to the next frame may be another object.
    private void Collect(ZDO zdo)
    {
        if (zdo == null || !zdo.IsValid() || !traits.TryGetValue(zdo.GetPrefab(), out Traits kind))
        {
            return;
        }

        Vector3 position = zdo.GetPosition();
        if ((kind & Traits.Animal) != 0)
        {
            if (zdo.GetBool(ZDOVars.s_tamed))
            {
                CellAt(position).Animals++;
            }

            return;
        }

        long creator = zdo.GetLong(ZDOVars.s_creator);
        if ((kind & Traits.Crop) != 0)
        {
            CellAt(position).Grown++;
            return;
        }

        if ((kind & Traits.PlantedCrop) != 0)
        {
            if (creator != 0L)
            {
                CellAt(position).Planted++;
            }

            return;
        }

        if ((kind & Traits.Piece) == 0 || creator == 0L)
        {
            return;
        }

        Cell cell = CellAt(position);
        cell.Pieces++;
        cell.Traits |= kind;
        cell.Creators.TryGetValue(creator, out int count);
        cell.Creators[creator] = count + 1;

        if ((kind & Traits.Ward) != 0)
        {
            scanWards.Add(new MapWard
            {
                Position = position,
                Radius = WardRadius(zdo.GetPrefab()),
                Enabled = zdo.GetBool(ZDOVars.s_enabled),
                Creator = creator,
            });
            Remember(creator, zdo.GetString(ZDOVars.s_creatorName));
        }

        if ((kind & Traits.Bed) != 0)
        {
            Remember(zdo.GetLong(ZDOVars.s_owner), zdo.GetString(ZDOVars.s_ownerName));
        }

        if ((kind & Traits.Sign) != 0)
        {
            string text = tags.Replace(zdo.GetString(ZDOVars.s_text), string.Empty).Trim();
            if (text.Length > 1 && text[0] == '#')
            {
                string name = text.Substring(1).Trim();
                signs.Add((CellIndex(position), name.Length > MaxNameLength ? name.Substring(0, MaxNameLength) : name));
            }
        }
    }

    private void Remember(long player, string name)
    {
        if (player != 0L && !string.IsNullOrEmpty(name))
        {
            names[player] = name;
        }
    }

    private static float WardRadius(int prefab)
    {
        GameObject ward = ZNetScene.instance.GetPrefab(prefab);
        PrivateArea area = ward != null ? ward.GetComponent<PrivateArea>() : null;
        return area != null ? area.m_radius : 32f;
    }

    private Cell CellAt(Vector3 position)
    {
        Vector2Int index = CellIndex(position);
        long key = Key(index);
        if (!cells.TryGetValue(key, out Cell cell))
        {
            cell = new Cell { Index = index };
            cells[key] = cell;
        }

        return cell;
    }

    private static Vector2Int CellIndex(Vector3 position)
    {
        return new Vector2Int(Mathf.FloorToInt(position.x / MapArea.CellSize), Mathf.FloorToInt(position.z / MapArea.CellSize));
    }

    private static long Key(Vector2Int cell)
    {
        return ((long)cell.x << 32) | (uint)cell.y;
    }

    private void FinishScan()
    {
        // Ripe crops carry no builder and some (seeds, Jotun puffs) also grow wild, so they only count beside a farm.
        foreach (Cell cell in cells.Values)
        {
            cell.Crops = cell.Planted + (cell.Grown > 0 && NearFarm(cell.Index) ? cell.Grown : 0);
        }

        List<MapArea> found = new();
        found.AddRange(Group(cell => cell.Crops, MapAreaSettings.MinPlants.Value, MapAreaKind.Field));
        found.AddRange(Group(cell => cell.Animals, MapAreaSettings.MinAnimals.Value, MapAreaKind.Pasture));
        found.AddRange(Group(cell => cell.Pieces, MapAreaSettings.MinPieces.Value, MapAreaKind.Building));
        areas = found;
        wards = scanWards.ToList();
        Publish();
    }

    private bool NearFarm(Vector2Int index)
    {
        for (int dx = -Bridge; dx <= Bridge; dx++)
        {
            for (int dz = -Bridge; dz <= Bridge; dz++)
            {
                if (cells.TryGetValue(Key(new Vector2Int(index.x + dx, index.y + dz)), out Cell near) && (near.Pieces > 0 || near.Planted > 0))
                {
                    return true;
                }
            }
        }

        return false;
    }

    // Groups the cells holding something into areas by flood fill, letting a small gap bridge two cells; a group with
    // too few things is not an area.
    private IEnumerable<MapArea> Group(Func<Cell, int> count, int minimum, MapAreaKind kind)
    {
        Dictionary<long, Cell> open = cells.Where(pair => count(pair.Value) > 0).ToDictionary(pair => pair.Key, pair => pair.Value);
        Queue<Cell> queue = new();
        while (open.Count > 0)
        {
            KeyValuePair<long, Cell> first = open.First();
            open.Remove(first.Key);
            queue.Enqueue(first.Value);
            List<Cell> group = new();
            while (queue.Count > 0)
            {
                Cell cell = queue.Dequeue();
                group.Add(cell);
                for (int dx = -Bridge; dx <= Bridge; dx++)
                {
                    for (int dz = -Bridge; dz <= Bridge; dz++)
                    {
                        long key = Key(new Vector2Int(cell.Index.x + dx, cell.Index.y + dz));
                        if (open.TryGetValue(key, out Cell next))
                        {
                            open.Remove(key);
                            queue.Enqueue(next);
                        }
                    }
                }
            }

            int total = group.Sum(count);
            if (total >= minimum)
            {
                yield return Describe(group, total, kind);
            }
        }
    }

    // Describes a group of cells as an area: a base if it has a bed and a fire, an outpost if it has a workbench or
    // portal, otherwise just a building. The builder is the player who placed most of its pieces, and a sign inside
    // gives its name.
    private MapArea Describe(List<Cell> group, int total, MapAreaKind kind)
    {
        MapArea area = new() { Kind = kind, Count = total, Cells = group.Select(cell => cell.Index).ToList() };
        if (kind != MapAreaKind.Building)
        {
            return area;
        }

        Traits all = group.Aggregate(Traits.None, (sum, cell) => sum | cell.Traits);
        area.Kind = (all & Traits.Bed) != 0 && (all & Traits.Fire) != 0 ? MapAreaKind.Base
            : (all & (Traits.Bench | Traits.Portal)) != 0 ? MapAreaKind.Outpost
            : MapAreaKind.Building;

        Dictionary<long, int> creators = new();
        foreach (KeyValuePair<long, int> pair in group.SelectMany(cell => cell.Creators))
        {
            creators.TryGetValue(pair.Key, out int count);
            creators[pair.Key] = count + pair.Value;
        }

        area.Creator = creators.OrderByDescending(pair => pair.Value).First().Key;
        HashSet<Vector2Int> inside = new(area.Cells);
        area.Name = signs.FirstOrDefault(sign => inside.Contains(sign.Cell)).Name ?? string.Empty;
        return area;
    }

    // Gives every area its builder's name (of players known online), then sends each player the areas, only when their
    // list has changed.
    private void Publish()
    {
        Dictionary<long, string> online = new(names);
        List<(ZNetPeer Peer, long PlayerID)> peers = new();
        foreach (ZNetPeer peer in ZNet.instance.GetPeers().Where(peer => peer.IsReady()))
        {
            long playerID = ZDOMan.instance.GetZDO(peer.m_characterID)?.GetLong(ZDOVars.s_playerID) ?? 0L;
            peers.Add((peer, playerID));
            if (playerID != 0L)
            {
                online[playerID] = peer.m_playerName;
            }
        }

        bool hostPlays = !ZNet.instance.IsDedicated() && Game.instance != null;
        long hostID = hostPlays ? Game.instance.GetPlayerProfile().GetPlayerID() : 0L;
        if (hostPlays)
        {
            online[hostID] = Game.instance.GetPlayerProfile().GetName();
        }

        foreach (MapArea area in areas.Where(area => area.Creator != 0L))
        {
            area.Builder = online.TryGetValue(area.Creator, out string name) ? name : string.Empty;
        }

        HashSet<long> current = new();
        foreach ((ZNetPeer peer, long playerID) in peers)
        {
            current.Add(peer.m_uid);
            if (TryGetChanged(peer.m_uid, playerID, out ZPackage package))
            {
                ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, RpcName, package);
            }
        }

        if (hostPlays)
        {
            current.Add(LocalKey);
            if (TryGetChanged(LocalKey, hostID, out ZPackage package))
            {
                package.SetPos(0);
                if (MapAreaPackage.Read(package, out List<MapArea> own, out List<MapWard> ownWards))
                {
                    MapAreaOverlay.SetData(own, ownWards);
                }
            }
        }

        foreach (long gone in lastSent.Keys.Where(key => !current.Contains(key)).ToList())
        {
            lastSent.Remove(gone);
        }
    }

    // With ShowOthers off a player gets their own buildings and wards, and the fields and pastures touching them.
    private bool TryGetChanged(long key, long playerID, out ZPackage package)
    {
        List<MapArea> visible = areas;
        List<MapWard> visibleWards = wards;
        if (!MapAreaSettings.ShowOthers.Value)
        {
            List<MapArea> own = areas.Where(area => area.Creator != 0L && area.Creator == playerID).ToList();
            HashSet<Vector2Int> near = new();
            foreach (Vector2Int cell in own.SelectMany(area => area.Cells))
            {
                for (int dx = -Bridge; dx <= Bridge; dx++)
                {
                    for (int dz = -Bridge; dz <= Bridge; dz++)
                    {
                        near.Add(new Vector2Int(cell.x + dx, cell.y + dz));
                    }
                }
            }

            visible = own.Concat(areas.Where(area => area.Creator == 0L && area.Cells.Any(near.Contains))).ToList();
            visibleWards = wards.Where(ward => ward.Creator == playerID).ToList();
        }

        package = MapAreaPackage.Write(visible, visibleWards);
        string content = Convert.ToBase64String(package.GetArray());
        if (lastSent.TryGetValue(key, out string sent) && sent == content)
        {
            return false;
        }

        lastSent[key] = content;
        return true;
    }

    private sealed class Cell
    {
        public Vector2Int Index;
        public int Pieces;
        public int Planted;
        public int Grown;
        public int Crops;
        public int Animals;
        public Traits Traits;
        public readonly Dictionary<long, int> Creators = new();
    }
}
