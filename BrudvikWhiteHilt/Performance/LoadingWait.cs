using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Performance;

/// <summary>
/// Ends the loading screen after a portal, a respawn or joining as soon as the destination is really there, instead of
/// after the game's fixed 8 seconds (<c>Player.UpdateTeleport</c>, <c>Game.m_respawnLoadDuration</c>).
/// <para>
/// A client only knows the objects the server has sent it so far, so the game's own check (<c>IsAreaReady</c>: every
/// known object around the point is built) cannot tell that a floor has not arrived yet. The client therefore asks the
/// server: <see cref="QueryRpc"/> names the destination, and the server answers with <see cref="ReadyRpc"/> once it has
/// generated the 3 x 3 zones around it and has sent this player every building and terrain object in them (its
/// per-player table of sent objects holds them all). The answer travels on the same connection right behind those
/// objects, so when it arrives they have arrived too. A host or single player holds every object itself and only needs
/// the zones generated.
/// </para>
/// <para>
/// The wait ends early only when, in addition, the zones around the point are loaded on the client, the game's own
/// <c>IsAreaReady</c> says every known object there is built, no terrain rebuild is queued, and this has held for
/// <see cref="SettleSeconds"/>. After that the game's own steps run unchanged (finding the floor, the bed, the ground).
/// Without an answer within <see cref="ConfirmTimeout"/> seconds nothing is shortened, so the game's own wait applies.
/// </para>
/// </summary>
public static class LoadingWait
{
    /// <summary>Client to server: a query id and the destination.</summary>
    public const string QueryRpc = "WhiteHilt_AreaQuery";

    /// <summary>Server to client: the query id, once the destination has been sent.</summary>
    public const string ReadyRpc = "WhiteHilt_AreaReady";

    /// <summary>Seconds a client waits for the server's answer before leaving the wait to the game.</summary>
    public const float ConfirmTimeout = 20f;

    /// <summary>Seconds everything must stay ready before the wait ends, so physics has the new floors.</summary>
    public const float SettleSeconds = 0.25f;

    // Server: a query nobody waits for any more, and how often the open ones are checked.
    private const float QueryLifetime = 30f;
    private const float CheckInterval = 0.1f;

    // The zone and the ring around it, as IsAreaReady checks.
    private static readonly SimulationDistance AreaDistance = new SimulationDistance(1, 0);

    private static readonly Dictionary<long, Query> Queries = new Dictionary<long, Query>();
    private static readonly List<long> Done = new List<long>();
    private static readonly List<ZDO> AreaObjects = new List<ZDO>();
    private static float nextCheck;

    // Client: the destination being waited for.
    private static bool haveQuery;
    private static Vector2s queryZone;
    private static int queryId;
    private static int nextQueryId = 1;
    private static float querySentAt;
    private static bool confirmed;
    private static float confirmedAfter;
    private static float readySince = -1f;
    private static bool standDownLogged;

    private struct Query
    {
        public int Id;
        public Vector2s Zone;
        public float ReceivedAt;
    }

    /// <summary>True when the setting is on and Valheim Community Patch does not do the same.</summary>
    public static bool Enabled
    {
        get
        {
            if (!StreamingSettings.LoadingWait.Value)
            {
                return false;
            }

            if (CommunityPatchCoexistence.HasActiveFix("LoadingWaitPatch"))
            {
                if (!standDownLogged)
                {
                    standDownLogged = true;
                    Jotunn.Logger.LogInfo("Loading wait: Valheim Community Patch has its own fix for this, so it is used.");
                }

                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Registers the two RPCs and forgets everything from an earlier world. Call once per world.
    /// </summary>
    public static void RegisterRpc()
    {
        Queries.Clear();
        Forget();
        ZRoutedRpc.instance?.Register<int, Vector3>(QueryRpc, RPC_Query);
        ZRoutedRpc.instance?.Register<int>(ReadyRpc, RPC_Ready);
    }

    /// <summary>
    /// Forgets the destination, so the next wait asks afresh. Call when a portal or a respawn starts.
    /// </summary>
    public static void Forget()
    {
        haveQuery = false;
        confirmed = false;
        readySince = -1f;
    }

    /// <summary>
    /// Asks the server about a destination now, so the answer may be there by the time the client gets there.
    /// </summary>
    /// <param name="point">The destination.</param>
    public static void Prepare(Vector3 point)
    {
        if (Enabled && ZNet.instance != null)
        {
            EnsureQuery(point);
        }
    }

    /// <summary>
    /// True when the destination is ready and the fixed wait may end now. Asks the server the first time.
    /// </summary>
    /// <param name="point">The destination.</param>
    /// <returns>Whether the loading screen may end early.</returns>
    public static bool AreaReady(Vector3 point)
    {
        if (!Enabled || ZNet.instance == null || ZNetScene.instance == null || ZoneSystem.instance == null
            || ZDOMan.instance == null)
        {
            return false;
        }

        EnsureQuery(point);
        if (!Confirmed() || !ReadyHere(point))
        {
            readySince = -1f;
            return false;
        }

        if (readySince < 0f)
        {
            readySince = Time.unscaledTime;
        }

        return Time.unscaledTime - readySince >= SettleSeconds;
    }

    /// <summary>
    /// Describes how the wait went, for the log line when it ends early.
    /// </summary>
    /// <returns>A short text.</returns>
    public static string Describe()
    {
        if (ZNet.instance != null && ZNet.instance.IsServer())
        {
            return "the zones were generated and built";
        }

        return $"the server confirmed after {confirmedAfter:0.0} s";
    }

    /// <summary>
    /// On the server: answers the queries whose destination has been sent. Call every frame.
    /// </summary>
    public static void UpdateServer()
    {
        if (Queries.Count == 0 || Time.unscaledTime < nextCheck)
        {
            return;
        }

        nextCheck = Time.unscaledTime + CheckInterval;
        if (ZNet.instance == null || !ZNet.instance.IsServer() || ZDOMan.instance == null || ZoneSystem.instance == null)
        {
            Queries.Clear();
            return;
        }

        Done.Clear();
        foreach (KeyValuePair<long, Query> entry in Queries)
        {
            ZDOMan.ZDOPeer peer = FindPeer(entry.Key);
            if (peer == null || Time.unscaledTime - entry.Value.ReceivedAt > QueryLifetime)
            {
                Done.Add(entry.Key);
                continue;
            }

            if (ZonesGenerated(entry.Value.Zone) && AllSent(peer, entry.Value.Zone))
            {
                ZRoutedRpc.instance.InvokeRoutedRPC(entry.Key, ReadyRpc, entry.Value.Id);
                Done.Add(entry.Key);
            }
        }

        foreach (long uid in Done)
        {
            Queries.Remove(uid);
        }

        Done.Clear();
    }

    private static void EnsureQuery(Vector3 point)
    {
        Vector2s zone = ZoneSystem.GetZone(point);
        if (haveQuery && zone == queryZone)
        {
            return;
        }

        haveQuery = true;
        queryZone = zone;
        queryId = nextQueryId++;
        querySentAt = Time.unscaledTime;
        confirmed = false;
        readySince = -1f;

        // A host has every object itself; a client asks the server.
        if (!ZNet.instance.IsServer() && ZRoutedRpc.instance != null)
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), QueryRpc, queryId, point);
        }
    }

    private static bool Confirmed()
    {
        if (ZNet.instance.IsServer())
        {
            return ZonesGenerated(queryZone);
        }

        // No answer in time: nothing is shortened, and the game's own wait decides (RPC_Ready ignores a late one).
        return confirmed;
    }

    // What this machine can see for itself: the zones are loaded, every known object is built, the terrain is done.
    private static bool ReadyHere(Vector3 point)
    {
        Vector2s zone = ZoneSystem.GetZone(point);
        for (int y = zone.y - 1; y <= zone.y + 1; y++)
        {
            for (int x = zone.x - 1; x <= zone.x + 1; x++)
            {
                if (!ZoneSystem.instance.IsZoneLoaded(new Vector2s(x, y)))
                {
                    return false;
                }
            }
        }

        return ZNetScene.instance.IsAreaReady(point) && !Heightmap.HaveQueuedRebuild(point, ZoneSystem.c_ZoneSize);
    }

    private static void RPC_Query(long sender, int id, Vector3 point)
    {
        if (ZNet.instance == null || !ZNet.instance.IsServer() || float.IsNaN(point.x) || float.IsNaN(point.z)
            || float.IsInfinity(point.x) || float.IsInfinity(point.z))
        {
            return;
        }

        // One destination per player; a newer one replaces the older. A forged sender id gains nothing: the answer
        // only says what is true for that player's own connection.
        Queries[sender] = new Query { Id = id, Zone = ZoneSystem.GetZone(point), ReceivedAt = Time.unscaledTime };
    }

    private static void RPC_Ready(long sender, int id)
    {
        if (ZNet.instance == null || ZNet.instance.IsServer() || sender != ZRoutedRpc.instance.GetServerPeerID())
        {
            return;
        }

        if (haveQuery && id == queryId && !confirmed && Time.unscaledTime - querySentAt <= ConfirmTimeout)
        {
            confirmed = true;
            confirmedAfter = Time.unscaledTime - querySentAt;
        }
    }

    // The server has made (generated) the zone and the ring around it, so their objects exist.
    private static bool ZonesGenerated(Vector2s zone)
    {
        for (int y = zone.y - 1; y <= zone.y + 1; y++)
        {
            for (int x = zone.x - 1; x <= zone.x + 1; x++)
            {
                if (!ZoneSystem.instance.IsZoneGenerated(new Vector2s(x, y)))
                {
                    return false;
                }
            }
        }

        return true;
    }

    // Every saved object around the zone is in the player's table of sent objects, or is the player's own. Not only
    // the Solid and Terrain ones: a building piece's prefab may well be of the Default type, and a floor missing on
    // arrival drops the player through it. A large base takes longer to send, but never longer than the game's own wait.
    private static bool AllSent(ZDOMan.ZDOPeer peer, Vector2s zone)
    {
        AreaObjects.Clear();
        ZDOMan.instance.FindSectorObjects(zone, AreaDistance, AreaObjects);
        long uid = peer.m_peer.m_uid;
        bool all = true;
        foreach (ZDO zdo in AreaObjects)
        {
            if (!zdo.Persistent || zdo.GetOwner() == uid)
            {
                continue;
            }

            if (!peer.m_zdos.ContainsKey(zdo.m_uid))
            {
                all = false;
                break;
            }
        }

        AreaObjects.Clear();
        return all;
    }

    private static ZDOMan.ZDOPeer FindPeer(long uid)
    {
        foreach (ZDOMan.ZDOPeer peer in ZDOMan.instance.m_peers)
        {
            if (peer.m_peer != null && peer.m_peer.m_uid == uid)
            {
                return peer;
            }
        }

        return null;
    }
}
