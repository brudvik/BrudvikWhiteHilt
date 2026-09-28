using BrudvikWhiteHilt.Items.Runes;
using BrudvikWhiteHilt.Pieces.Portals.RuneRack;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Portals.PortalMap;

/// <summary>
/// Runs on the <see cref="Game"/> object. On the server it finds every portal and ship in the world and sends each
/// player what they may see: portals while a Portal Astrolabe stands at a map table, ships while a Harbour Anchor does.
/// On a client it receives that list.
/// </summary>
public class PortalMapService : MonoBehaviour
{
    /// <summary>
    /// How close a map table extension must stand to a map table to switch its markers on.
    /// </summary>
    public const float ActivationRange = 5f;

    private const string RpcName = "WhiteHiltPortalMap";
    private const string PortalStationType = "PortalStations.Stations.PortalStation";
    private const float ScanPause = 10f;
    private const float ShipUpdateInterval = 2f;

    // Non-empty sectors read per frame, as the game's own iterative ZDO search does.
    private const int SectorsPerFrame = 400;

    // The game keeps one entry per peer; the local player of a hosting client is not a peer.
    private const long LocalKey = long.MinValue;

    private static readonly int stationName = "stationName".GetStableHashCode();
    private static readonly int stationFilter = "StationFilter".GetStableHashCode();
    private static readonly int runeKey = RuneRackComponent.ZdoKey.GetStableHashCode();

    private readonly HashSet<ZDOID> seen = new();
    private readonly List<PortalMapEntry> scanPortals = new();
    private readonly List<(ZDOID Id, PortalMapEntry Entry)> scanShips = new();
    private readonly List<(Vector3 Position, int Mask)> runePosts = new();
    private readonly List<Vector3> mapTables = new();
    private readonly List<Vector3> astrolabes = new();
    private readonly List<Vector3> anchors = new();
    private readonly Dictionary<long, string> lastSent = new();

    private List<PortalMapEntry> portals = new();
    private List<(ZDOID Id, PortalMapEntry Entry)> ships = new();
    private HashSet<int> portalPrefabs;
    private HashSet<int> stationPrefabs;
    private HashSet<int> mapTablePrefabs;
    private HashSet<int> shipPrefabs;
    private int astrolabePrefab;
    private int anchorPrefab;
    private int runePostPrefab;
    private int sectorIndex = -1;
    private float nextScan;
    private float nextShipUpdate;

    private void Start()
    {
        // A new game: the pins of the last world must not linger until this server's list arrives.
        PortalMapPins.SetEntries(new List<PortalMapEntry>());
        ZRoutedRpc.instance?.Register<ZPackage>(RpcName, RPC_PortalMap);
    }

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

            // Ships move, so their positions are read again between the slow full scans.
            if (ships.Count > 0 && Time.time >= nextShipUpdate)
            {
                nextShipUpdate = Time.time + ShipUpdateInterval;
                ships.RemoveAll(ship => !UpdateShip(ship.Id, ship.Entry));
                Publish();
            }
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"Portal map: scan failed: {ex.Message}");
            sectorIndex = -1;
            nextScan = Time.time + ScanPause;
        }
    }

    private static bool UpdateShip(ZDOID id, PortalMapEntry entry)
    {
        ZDO zdo = ZDOMan.instance.GetZDO(id);
        if (zdo == null)
        {
            return false;
        }

        entry.Position = zdo.GetPosition();
        return true;
    }

    private void BeginScan()
    {
        if (portalPrefabs == null)
        {
            FindPrefabs();
        }

        seen.Clear();
        scanPortals.Clear();
        scanShips.Clear();
        runePosts.Clear();
        mapTables.Clear();
        astrolabes.Clear();
        anchors.Clear();
        sectorIndex = 0;
    }

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

        // Portals outside loaded areas live in their own list.
        foreach (List<ZDO> list in ZDOMan.instance.m_portalObjects.Values)
        {
            list.ForEach(Collect);
        }

        FinishScan();
        sectorIndex = -1;
        nextScan = Time.time + ScanPause;
    }

    // Values are read at once: ZDOs are pooled, so a reference kept to the next frame may be another object.
    private void Collect(ZDO zdo)
    {
        if (zdo == null || !zdo.IsValid())
        {
            return;
        }

        int prefab = zdo.GetPrefab();
        bool wanted = portalPrefabs.Contains(prefab) || stationPrefabs.Contains(prefab) || shipPrefabs.Contains(prefab)
            || mapTablePrefabs.Contains(prefab) || prefab == runePostPrefab || prefab == astrolabePrefab || prefab == anchorPrefab;
        if (!wanted || !seen.Add(zdo.m_uid))
        {
            return;
        }

        if (portalPrefabs.Contains(prefab))
        {
            scanPortals.Add(new PortalMapEntry { Name = zdo.GetString(ZDOVars.s_tag), Position = zdo.GetPosition() });
        }
        else if (stationPrefabs.Contains(prefab))
        {
            scanPortals.Add(new PortalMapEntry
            {
                Name = zdo.GetString(stationName),
                Position = zdo.GetPosition(),
                Privacy = zdo.GetInt(stationFilter),
                Creator = zdo.GetLong(ZDOVars.s_creator)
            });
        }
        else if (shipPrefabs.Contains(prefab))
        {
            scanShips.Add((zdo.m_uid, new PortalMapEntry
            {
                Kind = PortalMapEntry.ShipKind,
                Prefab = prefab,
                Position = zdo.GetPosition(),
                Creator = zdo.GetLong(ZDOVars.s_creator)
            }));
        }
        else if (prefab == runePostPrefab)
        {
            runePosts.Add((zdo.GetPosition(), zdo.GetInt(runeKey)));
        }
        else if (prefab == astrolabePrefab)
        {
            astrolabes.Add(zdo.GetPosition());
        }
        else if (prefab == anchorPrefab)
        {
            anchors.Add(zdo.GetPosition());
        }
        else
        {
            mapTables.Add(zdo.GetPosition());
        }
    }

    private void FinishScan()
    {
        foreach (PortalMapEntry portal in scanPortals)
        {
            foreach ((Vector3 position, int mask) in runePosts)
            {
                if (Vector3.Distance(position, portal.Position) <= RunePortalRules.Range)
                {
                    portal.RuneMask |= mask;
                    portal.Everything |= mask == WhiteHiltRuneBase.FullMask;
                }
            }
        }

        portals = AtMapTable(astrolabes) ? scanPortals.ToList() : new List<PortalMapEntry>();
        ships = AtMapTable(anchors) ? scanShips.ToList() : new List<(ZDOID, PortalMapEntry)>();
        Publish();
    }

    private bool AtMapTable(List<Vector3> extensions)
    {
        return extensions.Any(extension => mapTables.Any(table => Vector3.Distance(extension, table) <= ActivationRange));
    }

    private void Publish()
    {
        Dictionary<long, string> online = new();
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

        foreach ((ZDOID _, PortalMapEntry ship) in ships)
        {
            ship.Builder = online.TryGetValue(ship.Creator, out string name) ? name : string.Empty;
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
                PortalMapPins.SetEntries(PortalMapEntry.Read(package));
            }
        }

        foreach (long gone in lastSent.Keys.Where(key => !current.Contains(key)).ToList())
        {
            lastSent.Remove(gone);
        }
    }

    // Public stations, portals and ships for everyone; other stations only for the player who built them.
    private bool TryGetChanged(long key, long playerID, out ZPackage package)
    {
        List<PortalMapEntry> visible = portals
            .Where(portal => portal.Privacy == PortalMapEntry.Public || (portal.Creator != 0L && portal.Creator == playerID))
            .Concat(ships.Select(ship => ship.Entry))
            .ToList();
        package = PortalMapEntry.Write(visible);
        string content = Convert.ToBase64String(package.GetArray());
        if (lastSent.TryGetValue(key, out string sent) && sent == content)
        {
            return false;
        }

        lastSent[key] = content;
        return true;
    }

    private void FindPrefabs()
    {
        portalPrefabs = new HashSet<int>();
        stationPrefabs = new HashSet<int>();
        mapTablePrefabs = new HashSet<int>();
        shipPrefabs = new HashSet<int>();
        foreach (GameObject prefab in ZNetScene.instance.m_prefabs.Where(prefab => prefab != null))
        {
            int hash = prefab.name.GetStableHashCode();
            if (prefab.GetComponent<TeleportWorld>() != null)
            {
                portalPrefabs.Add(hash);
            }
            else if (prefab.GetComponent<Ship>() != null)
            {
                shipPrefabs.Add(hash);
            }
            else if (prefab.GetComponent<MapTable>() != null)
            {
                mapTablePrefabs.Add(hash);
            }
            else if (prefab.GetComponents<MonoBehaviour>().Any(component => component != null && component.GetType().FullName == PortalStationType))
            {
                stationPrefabs.Add(hash);
            }
        }

        astrolabePrefab = PortalAstrolabe.PortalAstrolabe.PrefabName.GetStableHashCode();
        anchorPrefab = Ships.HarbourAnchor.HarbourAnchor.PrefabName.GetStableHashCode();
        runePostPrefab = RuneRack.RuneRack.PrefabName.GetStableHashCode();
        Jotunn.Logger.LogInfo($"Portal map: {portalPrefabs.Count} portal, {stationPrefabs.Count} station, {shipPrefabs.Count} ship and {mapTablePrefabs.Count} map table prefabs");
    }

    private void RPC_PortalMap(long sender, ZPackage package)
    {
        if (sender != ZRoutedRpc.instance.GetServerPeerID())
        {
            return;
        }

        PortalMapPins.SetEntries(PortalMapEntry.Read(package));
    }
}
