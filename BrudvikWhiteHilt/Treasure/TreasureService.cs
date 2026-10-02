using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Navigation.Discoveries;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Treasure;

/// <summary>
/// Runs on the <see cref="Game"/> object. A client that gets a new map suggests places it has explored; the server
/// checks them against what only it knows (buildings, locations, other treasures, generated zones), buries the treasure
/// at the first that suits and sends back the site with the landmarks around it. Clients also ask whether a map's
/// treasure is still in the ground, and hear when one is dug up.
/// </summary>
public class TreasureService : MonoBehaviour
{
    private const string RequestRpc = "WhiteHiltTreasureRequest";
    private const string ReplyRpc = "WhiteHiltTreasureReply";
    private const string StatusRpc = "WhiteHiltTreasureStatus";
    private const string StatusReplyRpc = "WhiteHiltTreasureStatusReply";
    private const string FoundRpc = "WhiteHiltTreasureFound";

    private const int ResultBuried = 0;
    private const int ResultTooMany = 1;
    private const int ResultNoPlace = 2;

    // Locations count as taken ground this far beyond their own radius.
    private const float LocationMargin = 10f;

    // A request without an answer (server restarted, connection lost) may be sent again after this many seconds.
    private const float RequestTimeout = 15f;

    private static readonly Dictionary<string, float> pending = new();
    private static readonly System.Random random = new();
    private static TreasureService instance;

    private readonly List<Mound> mounds = new();
    private bool moundsScanned;

    /// <summary>
    /// Asks the server to bury a treasure for a map that has none yet. Does nothing while an answer is awaited.
    /// </summary>
    /// <param name="player">The local player holding the map.</param>
    /// <param name="map">The map.</param>
    public static void RequestSite(Player player, ItemDrop.ItemData map)
    {
        if (instance == null || ZRoutedRpc.instance == null || player == null)
        {
            return;
        }

        string id = TreasureMapItem.Assign(map, 0);
        if (IsPending(id))
        {
            player.Message(MessageHud.MessageType.Center, Translations.Word("msg_whitehilt_treasure_pending"));
            return;
        }

        pending[id] = Time.time;

        List<TreasureCandidate> candidates = TreasureSiteFinder.Find(player.transform.position, random);
        if (candidates.Count == 0)
        {
            pending.Remove(id);
            GiveBack(player, map, "msg_whitehilt_treasure_noplace");
            return;
        }

        ZPackage package = new();
        package.Write(id);
        package.Write(player.GetPlayerID());
        package.Write(candidates.Count);
        foreach (TreasureCandidate candidate in candidates)
        {
            package.Write(candidate.Chest);
            package.Write(candidate.Centre.x);
            package.Write(candidate.Centre.y);
            package.Write(candidate.NearWater);
        }

        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), RequestRpc, package);
    }

    /// <summary>
    /// Asks the server whether the treasure of a map is still in the ground; the map is marked plundered if not.
    /// </summary>
    /// <param name="map">The map.</param>
    public static void RequestStatus(ItemDrop.ItemData map)
    {
        ZDOID mound = TreasureMapItem.GetMound(map);
        if (instance == null || ZRoutedRpc.instance == null || mound.IsNone() || TreasureMapItem.IsPlundered(map))
        {
            return;
        }

        ZPackage package = new();
        package.Write(TreasureMapItem.GetId(map));
        package.Write(mound);
        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), StatusRpc, package);
    }

    /// <summary>
    /// Tells everybody a treasure has been dug up, so their maps of it are marked plundered.
    /// </summary>
    /// <param name="id">Treasure id.</param>
    public static void AnnounceFound(string id)
    {
        ZRoutedRpc.instance?.InvokeRoutedRPC(ZRoutedRpc.Everybody, FoundRpc, id);
    }

    /// <summary>
    /// Whether a map is waiting for the server to bury its treasure.
    /// </summary>
    /// <param name="id">Treasure id.</param>
    /// <returns>True while an answer is awaited.</returns>
    public static bool IsPending(string id)
    {
        return id != null && pending.TryGetValue(id, out float since) && Time.time - since < RequestTimeout;
    }

    private void Start()
    {
        instance = this;
        pending.Clear();
        ZRoutedRpc.instance?.Register<ZPackage>(RequestRpc, RPC_Request);
        ZRoutedRpc.instance?.Register<ZPackage>(ReplyRpc, RPC_Reply);
        ZRoutedRpc.instance?.Register<ZPackage>(StatusRpc, RPC_Status);
        ZRoutedRpc.instance?.Register<ZPackage>(StatusReplyRpc, RPC_StatusReply);
        ZRoutedRpc.instance?.Register<string>(FoundRpc, RPC_Found);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void RPC_Request(long sender, ZPackage package)
    {
        if (ZNet.instance == null || !ZNet.instance.IsServer() || ZDOMan.instance == null || ZoneSystem.instance == null)
        {
            return;
        }

        string id = package.ReadString();
        long playerId = package.ReadLong();
        int count = Mathf.Min(package.ReadInt(), 32);
        List<TreasureCandidate> candidates = new();
        for (int i = 0; i < count; i++)
        {
            Vector3 chest = package.ReadVector3();
            Vector2 centre = new(package.ReadSingle(), package.ReadSingle());
            candidates.Add(new TreasureCandidate(chest, centre, package.ReadBool()));
        }

        RefreshMounds();
        ZPackage reply = new();
        reply.Write(id);
        Mound existing = mounds.FirstOrDefault(mound => mound.Id == id);
        if (existing != null)
        {
            WriteSite(reply, existing.Zdo, existing.Centre, existing.Size);
        }
        else if (mounds.Count(mound => mound.Buyer == playerId) >= TreasureSettings.MaxActiveMaps.Value)
        {
            reply.Write(ResultTooMany);
        }
        else if (TryBury(id, playerId, candidates, out Mound buried))
        {
            WriteSite(reply, buried.Zdo, buried.Centre, buried.Size);
        }
        else
        {
            reply.Write(ResultNoPlace);
        }

        ZRoutedRpc.instance.InvokeRoutedRPC(sender, ReplyRpc, reply);
    }

    private bool TryBury(string id, long playerId, List<TreasureCandidate> candidates, out Mound buried)
    {
        buried = null;
        GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(TreasureRegistry.MoundPrefabName) : null;
        if (prefab == null)
        {
            Jotunn.Logger.LogWarning("Treasure: the mound prefab is missing, no treasure can be buried");
            return false;
        }

        foreach (TreasureCandidate candidate in candidates)
        {
            if (!Suits(candidate))
            {
                continue;
            }

            Vector3 position = candidate.Chest;
            position.y = WorldGenerator.instance.GetHeight(position.x, position.z);
            ZNetView view = prefab.GetComponent<ZNetView>();
            int hash = prefab.name.GetStableHashCode();
            ZDO zdo = ZDOMan.instance.CreateNewZDO(position, hash);
            zdo.Persistent = view.m_persistent;
            zdo.Type = view.m_type;
            zdo.Distant = view.m_distant;
            zdo.SetPrefab(hash);
            zdo.SetRotation(Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            zdo.Set(BuriedTreasure.IdKey, id);
            zdo.Set(BuriedTreasure.BuyerKey, playerId);
            zdo.Set(BuriedTreasure.CentreKey, new Vector3(candidate.Centre.x, 0f, candidate.Centre.y));
            zdo.Set(BuriedTreasure.SizeKey, TreasureSettings.FragmentSize.Value);

            // Nobody owns it until a player comes near; then that player's client takes it over.
            zdo.SetOwnerInternal(0L);
            buried = new Mound(zdo.m_uid, id, playerId, position, candidate.Centre, TreasureSettings.FragmentSize.Value);
            mounds.Add(buried);
            Jotunn.Logger.LogInfo($"Treasure buried at {position.x:0} {position.z:0}");
            return true;
        }

        return false;
    }

    private bool Suits(TreasureCandidate candidate)
    {
        Vector3 point = candidate.Chest;
        WorldGenerator world = WorldGenerator.instance;
        float water = ZoneSystem.instance.m_waterLevel;
        if ((world.GetBiome(point.x, point.z) & TreasureSiteFinder.AllowedBiomes()) == 0 || world.GetHeight(point.x, point.z) < water + 1f
            || !ZoneSystem.instance.IsZoneGenerated(ZoneSystem.GetZone(point)))
        {
            return false;
        }

        float apart = TreasureSettings.MinTreasureDistance.Value;
        if (mounds.Any(mound => Utils.DistanceXZ(mound.Position, point) < apart))
        {
            return false;
        }

        foreach (ZoneSystem.LocationInstance location in ZoneSystem.instance.m_locationInstances.Values)
        {
            float radius = (location.m_location?.m_exteriorRadius ?? 0f) + LocationMargin;
            if (Utils.DistanceXZ(location.m_position, point) < radius)
            {
                return false;
            }
        }

        if (IsNearBuilding(point, TreasureSettings.MinBaseDistance.Value))
        {
            return false;
        }

        return candidate.NearWater || FindLandmarks(new Vector2(point.x, point.z), TreasureSettings.FeatureDistance.Value * 2f)
            .Any(mark => Vector2.Distance(mark.Position, new Vector2(point.x, point.z)) <= TreasureSettings.FeatureDistance.Value);
    }

    // Anything a player has built has its creator set.
    private static bool IsNearBuilding(Vector3 point, float radius)
    {
        if (radius <= 0f)
        {
            return false;
        }

        Vector2s min = ZoneSystem.GetZone(point - new Vector3(radius, 0f, radius));
        Vector2s max = ZoneSystem.GetZone(point + new Vector3(radius, 0f, radius));
        List<ZDO>[] sectors = ZDOMan.instance.m_objectsBySector;
        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                uint index = ZoneSystem.SectorToIndex(x, y).Sector;
                List<ZDO> objects = index < sectors.Length ? sectors[index] : null;
                if (objects == null)
                {
                    continue;
                }

                foreach (ZDO zdo in objects)
                {
                    if (zdo.GetLong(ZDOVars.s_creator) != 0L && Utils.DistanceXZ(zdo.GetPosition(), point) < radius)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    // Locations of a kind Munin's Perch knows, within a square around a point.
    private static List<TreasureLandmark> FindLandmarks(Vector2 centre, float size)
    {
        List<TreasureLandmark> marks = new();
        float half = size / 2f;
        foreach (ZoneSystem.LocationInstance location in ZoneSystem.instance.m_locationInstances.Values)
        {
            Vector3 position = location.m_position;
            if (Mathf.Abs(position.x - centre.x) > half || Mathf.Abs(position.z - centre.y) > half
                || !DiscoveryCatalog.TryClassifyLocation(location.m_location?.m_prefabName, out string kind, out _))
            {
                continue;
            }

            marks.Add(new TreasureLandmark(kind, new Vector2(position.x, position.z)));
        }

        return marks;
    }

    private static void WriteSite(ZPackage reply, ZDOID mound, Vector2 centre, float size)
    {
        ZDO zdo = ZDOMan.instance.GetZDO(mound);
        Vector3 chest = zdo != null ? zdo.GetPosition() : Vector3.zero;
        reply.Write(ResultBuried);
        reply.Write(mound);
        reply.Write(chest);
        reply.Write(centre.x);
        reply.Write(centre.y);
        reply.Write(size);
        List<TreasureLandmark> marks = FindLandmarks(centre, size);
        reply.Write(marks.Count);
        foreach (TreasureLandmark mark in marks)
        {
            reply.Write(mark.Kind);
            reply.Write(mark.Position.x);
            reply.Write(mark.Position.y);
        }
    }

    private void RPC_Reply(long sender, ZPackage reply)
    {
        Player player = Player.m_localPlayer;
        if (player == null || sender != ZRoutedRpc.instance.GetServerPeerID())
        {
            return;
        }

        string id = reply.ReadString();
        pending.Remove(id);
        ItemDrop.ItemData map = TreasureMapItem.Find(player.GetInventory(), id);
        if (map == null)
        {
            return;
        }

        int result = reply.ReadInt();
        if (result != ResultBuried)
        {
            GiveBack(player, map, result == ResultTooMany ? "msg_whitehilt_treasure_toomany" : "msg_whitehilt_treasure_noplace");
            return;
        }

        ZDOID mound = reply.ReadZDOID();
        Vector3 chest = reply.ReadVector3();
        TreasureSite site = new()
        {
            Chest = new Vector2(chest.x, chest.z),
            Centre = new Vector2(reply.ReadSingle(), reply.ReadSingle()),
            Size = reply.ReadSingle(),
            Seed = id.GetStableHashCode()
        };
        site.Rotation = TreasureSettings.RotateFragment.Value ? (site.Seed >> 4) & 3 : 0;
        int count = reply.ReadInt();
        for (int i = 0; i < count; i++)
        {
            string kind = reply.ReadString();
            Vector2 position = new(reply.ReadSingle(), reply.ReadSingle());

            // Only what the buyer has seen goes on the map, so the landmarks can be found on their own map.
            if (Minimap.instance == null || Minimap.instance.IsExplored(new Vector3(position.x, 0f, position.y)))
            {
                site.Landmarks.Add(new TreasureLandmark(kind, position));
            }
        }

        TreasureMapItem.SetSite(map, site, mound);
        player.Message(MessageHud.MessageType.Center, Translations.Word("msg_whitehilt_treasure_marked"));
    }

    private void RPC_Status(long sender, ZPackage package)
    {
        if (ZNet.instance == null || !ZNet.instance.IsServer())
        {
            return;
        }

        string id = package.ReadString();
        ZDOID mound = package.ReadZDOID();
        ZPackage reply = new();
        reply.Write(id);
        reply.Write(ZDOMan.instance.GetZDO(mound) != null);
        ZRoutedRpc.instance.InvokeRoutedRPC(sender, StatusReplyRpc, reply);
    }

    private void RPC_StatusReply(long sender, ZPackage reply)
    {
        string id = reply.ReadString();
        if (!reply.ReadBool())
        {
            MarkPlundered(id);
        }
    }

    private void RPC_Found(long sender, string id)
    {
        MarkPlundered(id);
        mounds.RemoveAll(mound => mound.Id == id);
    }

    private static void MarkPlundered(string id)
    {
        ItemDrop.ItemData map = TreasureMapItem.Find(Player.m_localPlayer?.GetInventory(), id);
        if (map != null && !TreasureMapItem.IsPlundered(map))
        {
            TreasureMapItem.SetPlundered(map);
            TreasureMapPanel.Refresh(id);
        }
    }

    // A bought map no treasure could be buried for goes back to Hildir, and its coins to the buyer.
    private static void GiveBack(Player player, ItemDrop.ItemData map, string messageKey)
    {
        int paid = TreasureMapItem.GetPaid(map);
        if (paid > 0)
        {
            player.GetInventory().RemoveItem(map);
            GameObject coins = ObjectDB.instance.GetItemPrefab("Coins");
            if (coins != null && player.GetInventory().AddItem("Coins", paid, 1, 0, 0L, string.Empty, false) == null)
            {
                ItemDrop.DropItem(coins.GetComponent<ItemDrop>().m_itemData.Clone(), paid, player.transform.position + Vector3.up, Quaternion.identity);
            }
        }

        player.Message(MessageHud.MessageType.Center, Translations.Word(messageKey));
    }

    // Built once from every object in the world, then kept up to date; dug-up mounds drop out.
    private void RefreshMounds()
    {
        if (!moundsScanned)
        {
            moundsScanned = true;
            int hash = TreasureRegistry.MoundPrefabName.GetStableHashCode();
            foreach (ZDO zdo in ZDOMan.instance.m_objectsByID.Values)
            {
                if (zdo.GetPrefab() == hash)
                {
                    Vector3 centre = zdo.GetVec3(BuriedTreasure.CentreKey, zdo.GetPosition());
                    mounds.Add(new Mound(zdo.m_uid, zdo.GetString(BuriedTreasure.IdKey), zdo.GetLong(BuriedTreasure.BuyerKey), zdo.GetPosition(),
                        new Vector2(centre.x, centre.z), zdo.GetFloat(BuriedTreasure.SizeKey, TreasureSettings.FragmentSize.Value)));
                }
            }
        }

        mounds.RemoveAll(mound => ZDOMan.instance.GetZDO(mound.Zdo) == null);
    }

    private sealed class Mound
    {
        public Mound(ZDOID zdo, string id, long buyer, Vector3 position, Vector2 centre, float size)
        {
            Zdo = zdo;
            Id = id;
            Buyer = buyer;
            Position = position;
            Centre = centre;
            Size = size;
        }

        public ZDOID Zdo { get; }

        public string Id { get; }

        public long Buyer { get; }

        public Vector3 Position { get; }

        public Vector2 Centre { get; }

        public float Size { get; }
    }
}
