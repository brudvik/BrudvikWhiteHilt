using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Navigation;
using BrudvikWhiteHilt.Navigation.Waypoints;
using BrudvikWhiteHilt.Treasure;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.SeaBattles.Raids;

/// <summary>
/// Runs on the <see cref="Game"/> object. A client that uses a Raid Chart suggests coasts it has explored; the server
/// checks them against what only it knows, then makes a draugr harbour in the network data: a hoard chest on the shore, a
/// moored Nail Ship in deep water off it and the guards, all persistent and with no owner until a player comes near, so
/// it works with nobody there and on a dedicated server. The client then marks the harbour on its map. Admins can
/// call a harbour up near themselves with whitehilt_seabattle raid.
/// </summary>
public class RaidService : MonoBehaviour
{
    private const string RequestRpc = "WhiteHiltRaidRequest";
    private const string AdminRpc = "WhiteHiltRaidAdmin";
    private const string ReplyRpc = "WhiteHiltRaidReply";

    private const int ResultMade = 0;
    private const int ResultTooMany = 1;
    private const int ResultNoPlace = 2;
    private const int ResultOff = 3;

    private const int MaxCandidates = 16;

    // A request without an answer (server restarted, connection lost) may be sent again after this many seconds.
    private const float RequestTimeout = 15f;

    // The server believes a candidate within the distances of the settings, give or take this, as the player has moved.
    private const float DistanceMargin = 25f;

    // Locations count as taken ground this far beyond their own radius.
    private const float LocationMargin = 10f;

    // Nothing else (a tree, a rock) within this many metres of the hoard.
    private const float HoardClearance = 2.5f;

    // An admin's raid looks for a coast from this far from the player, in rings this far apart, and tries this many
    // coast points at most.
    private const float AdminMinDistance = 50f;
    private const float AdminStep = 25f;
    private const int AdminMaxPoints = 200;

    // Guards are placed a little above the ground and settle on it.
    private const float GuardLift = 0.5f;

    private static readonly System.Random random = new();
    private static RaidService instance;
    private static float requestedAt = float.NegativeInfinity;

    private readonly List<Site> sites = new();
    private bool sitesScanned;

    /// <summary>
    /// Asks the server to make a draugr harbour on a coast the local player knows. The chart is used up when the harbour
    /// is made, not before. Does nothing while an answer is awaited.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Request(Player player)
    {
        if (instance == null || ZRoutedRpc.instance == null || player == null)
        {
            return;
        }

        if (!SeaBattleSettings.Enabled.Value)
        {
            player.Message(MessageHud.MessageType.Center, Translations.Word("msg_whitehilt_raid_nosite"));
            return;
        }

        if (Time.time - requestedAt < RequestTimeout)
        {
            return;
        }

        List<TreasureCandidate> found = TreasureSiteFinder.Find(player.transform.position, random,
            SeaBattleSettings.Raids.MaxDistance.Value, SeaBattleSettings.Raids.MinDistance.Value)
            .Where(candidate => candidate.NearWater).Take(MaxCandidates).ToList();
        if (found.Count == 0)
        {
            player.Message(MessageHud.MessageType.Center, Translations.Word("msg_whitehilt_raid_nosite"));
            return;
        }

        requestedAt = Time.time;
        ZPackage package = new();
        package.Write(found.Count);
        foreach (TreasureCandidate candidate in found)
        {
            package.Write(candidate.Chest);
        }

        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), RequestRpc, package);
    }

    /// <summary>
    /// Asks the server, as an admin, to make a draugr harbour on the coast nearest the local player.
    /// </summary>
    public static void RequestAdmin()
    {
        if (ZRoutedRpc.instance == null)
        {
            return;
        }

        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), AdminRpc);
    }

    private static void Print(string message)
    {
        if (global::Console.instance != null)
        {
            global::Console.instance.Print(message);
        }
    }

    private void Start()
    {
        instance = this;
        requestedAt = float.NegativeInfinity;
        ZRoutedRpc.instance?.Register<ZPackage>(RequestRpc, RPC_Request);
        ZRoutedRpc.instance?.Register(AdminRpc, RPC_Admin);
        ZRoutedRpc.instance?.Register<ZPackage>(ReplyRpc, RPC_Reply);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    // On the server: makes a harbour at the first coast the client proposed that suits it. The sender must be a real
    // connection with a living player, and every candidate must lie where a chart could have sent it.
    private void RPC_Request(long sender, ZPackage package)
    {
        if (ZNet.instance == null || !ZNet.instance.IsServer() || ZDOMan.instance == null || ZoneSystem.instance == null)
        {
            return;
        }

        if (RoutedRpcSender.Verify(sender, out _) == null)
        {
            Jotunn.Logger.LogWarning($"Raid: a request from {sender} could not be tied to a connection and was ignored");
            return;
        }

        ZDO player = SeaBattleService.PlayerZdo(sender);
        if (player == null)
        {
            return;
        }

        if (!SeaBattleSettings.Enabled.Value)
        {
            Reply(sender, ResultOff, true, Vector3.zero, string.Empty);
            return;
        }

        List<Vector3> candidates = ReadCandidates(package, player.GetPosition());
        RefreshSites();
        if (ActiveSites() >= SeaBattleSettings.Raids.MaxActive.Value)
        {
            Reply(sender, ResultTooMany, true, Vector3.zero, string.Empty);
            return;
        }

        if (TryMake(candidates, out Site site))
        {
            Reply(sender, ResultMade, true, site.Position, string.Empty);
        }
        else
        {
            Reply(sender, ResultNoPlace, true, Vector3.zero, string.Empty);
        }
    }

    // On the server: an admin asks for a harbour near the player. The coast is found here, not by the client, since
    // nothing needs to be explored for a test; every other check is the same as for a chart.
    private void RPC_Admin(long sender)
    {
        if (ZNet.instance == null || !ZNet.instance.IsServer() || ZDOMan.instance == null || ZoneSystem.instance == null)
        {
            return;
        }

        if (!SeaBattleService.IsAdmin(sender))
        {
            Reply(sender, ResultOff, false, Vector3.zero, "Only admins can do that.");
            return;
        }

        if (!SeaBattleSettings.Enabled.Value)
        {
            Reply(sender, ResultOff, false, Vector3.zero, "Nail Ships and raids are switched off in the settings ([SeaBattles] Enabled).");
            return;
        }

        ZDO player = SeaBattleService.PlayerZdo(sender);
        if (player == null)
        {
            Reply(sender, ResultNoPlace, false, Vector3.zero, "Your character was not found.");
            return;
        }

        RefreshSites();
        int active = ActiveSites();
        if (active >= SeaBattleSettings.Raids.MaxActive.Value)
        {
            Reply(sender, ResultTooMany, false, Vector3.zero,
                $"{active} draugr harbours wait in the world already; [SeaBattles.Raids] MaxActive is {SeaBattleSettings.Raids.MaxActive.Value}.");
            return;
        }

        Vector3 origin = player.GetPosition();
        WorldGenerator world = WorldGenerator.instance;
        List<Vector3> candidates = RaidSites.CoastPoints(new Vector2(origin.x, origin.z), AdminMinDistance, SeaBattleSettings.Raids.MaxDistance.Value,
                AdminStep, (float)random.NextDouble() * 360f, world.GetHeight, ZoneSystem.instance.m_waterLevel)
            .Take(AdminMaxPoints).Select(point => new Vector3(point.x, world.GetHeight(point.x, point.y), point.y)).ToList();
        if (TryMake(candidates, out Site site))
        {
            Reply(sender, ResultMade, false, site.Position,
                $"A draugr harbour was made {Utils.DistanceXZ(origin, site.Position):0} m from you, at {site.Position.x:0} {site.Position.z:0}.");
        }
        else
        {
            Reply(sender, ResultNoPlace, false, Vector3.zero,
                $"No coast with open water for a ship was found within {SeaBattleSettings.Raids.MaxDistance.Value:0} m of you.");
        }
    }

    // On the client: marks the harbour on the map and uses up the chart, or says why nothing was made.
    private void RPC_Reply(long sender, ZPackage reply)
    {
        if (sender != ZRoutedRpc.instance.GetServerPeerID())
        {
            return;
        }

        requestedAt = float.NegativeInfinity;
        int result = reply.ReadInt();
        bool chart = reply.ReadBool();
        string note = reply.ReadString();
        Vector3 position = reply.ReadVector3();
        if (note.Length > 0)
        {
            Print(note);
        }

        Player player = Player.m_localPlayer;
        if (player == null)
        {
            return;
        }

        if (result != ResultMade)
        {
            if (chart)
            {
                player.Message(MessageHud.MessageType.Center, Translations.Word(result == ResultTooMany ? "msg_whitehilt_raid_toomany" : "msg_whitehilt_raid_nosite"));
            }

            return;
        }

        string name = Translations.Word("whitehilt_raid_pin");
        if (Minimap.instance != null)
        {
            Minimap.instance.AddPin(position, Minimap.PinType.Icon3, name, save: true, isChecked: false);
        }

        // The target arrow is the Ruby Pathfinder's Amulet's; without it the pin on the map is the only guide.
        if (RubyPathfinderAmulet.WearsRuby(player))
        {
            WaypointTarget.Set(player, position, name);
        }

        if (chart)
        {
            UseUpChart(player);
        }

        player.Message(MessageHud.MessageType.Center, Translations.Word("msg_whitehilt_raid_marked"));
    }

    private static void UseUpChart(Player player)
    {
        Inventory inventory = player.GetInventory();
        ItemDrop.ItemData chart = inventory.GetAllItems()
            .FirstOrDefault(item => item.m_dropPrefab != null && item.m_dropPrefab.name == NailShipRegistry.RaidChartName);
        if (chart != null)
        {
            inventory.RemoveOneItem(chart);
        }
    }

    private static void Reply(long sender, int result, bool chart, Vector3 position, string note)
    {
        ZPackage reply = new();
        reply.Write(result);
        reply.Write(chart);
        reply.Write(note);
        reply.Write(position);
        ZRoutedRpc.instance.InvokeRoutedRPC(sender, ReplyRpc, reply);
    }

    // Reads the candidates of a request. A broken package, a point that is not a number, or one farther from the player
    // than a chart could send it, is dropped; so is everything past the sixteenth.
    private static List<Vector3> ReadCandidates(ZPackage package, Vector3 playerPosition)
    {
        List<Vector3> found = new();
        float min = SeaBattleSettings.Raids.MinDistance.Value - DistanceMargin;
        float max = Mathf.Max(SeaBattleSettings.Raids.MinDistance.Value + 50f, SeaBattleSettings.Raids.MaxDistance.Value) + DistanceMargin;
        try
        {
            int count = Mathf.Clamp(package.ReadInt(), 0, MaxCandidates);
            for (int i = 0; i < count; i++)
            {
                Vector3 point = package.ReadVector3();
                float distance = Utils.DistanceXZ(point, playerPosition);
                if (!float.IsNaN(distance) && !float.IsInfinity(distance) && distance >= min && distance <= max)
                {
                    found.Add(point);
                }
            }
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"Raid: a request could not be read: {ex.Message}");
        }

        return found;
    }

    // Makes the harbour at the first candidate that suits: land the world has made, away from other harbours, places
    // and buildings, with deep water off it for the ship. Then the guards, the hoard and the ship are made as objects in
    // the network data only, which come to life when a player comes near.
    private bool TryMake(List<Vector3> candidates, out Site made)
    {
        made = null;
        ZNetScene scene = ZNetScene.instance;
        GameObject hoardPrefab = scene != null ? scene.GetPrefab(NailShipRegistry.HoardName) : null;
        GameObject shipPrefab = scene != null ? scene.GetPrefab(NailShipRegistry.ShipName) : null;
        if (hoardPrefab == null || shipPrefab == null)
        {
            Jotunn.Logger.LogWarning("Raid: the hoard or the Nail Ship prefab is missing, no harbour can be made");
            return false;
        }

        IReadOnlyList<CrewTier> tiers = CrewTable.Parse(SeaBattleSettings.CrewTiers.Value);
        CrewTier tier = CrewTable.Pick(tiers, key => ZoneSystem.instance.GetGlobalKey(key)) ?? (tiers.Count > 0 ? tiers[0] : null);
        if (tier == null)
        {
            Jotunn.Logger.LogWarning("Raid: [SeaBattles] CrewTiers has no valid tier, no harbour can be made");
            return false;
        }

        List<(GameObject Prefab, bool Archer)> guards = ChooseGuards(scene);
        float water = ZoneSystem.instance.m_waterLevel;
        WorldGenerator world = WorldGenerator.instance;
        Func<float, float, float> height = world.GetHeight;
        foreach (Vector3 candidate in candidates)
        {
            Vector2 spot = new(candidate.x, candidate.z);
            if (!SuitsGround(spot, height, water)
                || !RaidSites.TryFindShipPoint(spot, SeaBattleSettings.Raids.ShipSearchRadius.Value, height, water, out Vector2 shipPoint)
                || !IsClear(new Vector3(spot.x, height(spot.x, spot.y), spot.y)))
            {
                continue;
            }

            made = Build(spot, shipPoint, hoardPrefab, shipPrefab, tier, guards, height, water);
            return true;
        }

        return false;
    }

    // The guards' prefabs, one entry per guard; unknown prefabs are skipped with a warning.
    private static List<(GameObject Prefab, bool Archer)> ChooseGuards(ZNetScene scene)
    {
        List<(GameObject, bool)> guards = new();
        foreach (CrewEntry entry in CrewTable.ParseEntries(SeaBattleSettings.Raids.Guards.Value))
        {
            GameObject prefab = scene.GetPrefab(entry.Prefab);
            if (prefab == null)
            {
                Jotunn.Logger.LogWarning($"Raid: no creature called {entry.Prefab} in the guard list, skipped");
                continue;
            }

            for (int i = 0; i < entry.Count; i++)
            {
                guards.Add((prefab, entry.Archer));
            }
        }

        return guards;
    }

    // Dry land above the sea, in a zone the world has made, away from the other harbours.
    private bool SuitsGround(Vector2 spot, Func<float, float, float> height, float water)
    {
        float ground = height(spot.x, spot.y);
        Vector3 point = new(spot.x, ground, spot.y);
        if (ground < water + RaidSites.MinLandHeight || !ZoneSystem.instance.IsZoneGenerated(ZoneSystem.GetZone(point)))
        {
            return false;
        }

        float apart = TreasureSettings.MinTreasureDistance.Value;
        return !sites.Any(site => Utils.DistanceXZ(site.Position, point) < apart);
    }

    // Away from locations, from anything a player has built and from trees and rocks at the hoard's own place. These
    // walk through many objects, so they run last.
    private static bool IsClear(Vector3 point)
    {
        foreach (ZoneSystem.LocationInstance location in ZoneSystem.instance.m_locationInstances.Values)
        {
            float radius = (location.m_location?.m_exteriorRadius ?? 0f) + LocationMargin;
            if (Utils.DistanceXZ(location.m_position, point) < radius)
            {
                return false;
            }
        }

        return !TreasureService.IsNearBuilding(point, TreasureSettings.MinBaseDistance.Value) && !IsCrowded(point, HoardClearance);
    }

    // Whether any object lies within a radius of a point, as a tree or a rock would.
    private static bool IsCrowded(Vector3 point, float radius)
    {
        Vector2s min = ZoneSystem.GetZone(point - new Vector3(radius, 0f, radius));
        Vector2s max = ZoneSystem.GetZone(point + new Vector3(radius, 0f, radius));
        List<ZDO>[] sectors = ZDOMan.instance.m_objectsBySector;
        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                uint index = ZoneSystem.SectorToIndex(x, y).Sector;
                List<ZDO> objects = index < sectors.Length ? sectors[index] : null;
                if (objects != null && objects.Any(zdo => Utils.DistanceXZ(zdo.GetPosition(), point) < radius))
                {
                    return true;
                }
            }
        }

        return false;
    }

    // Makes the objects of a harbour and remembers it. The ship lies along the shore, moored, with the crew tier of the
    // world; the guards stand in a ring round the hoard, each with its post.
    private Site Build(Vector2 spot, Vector2 shipPoint, GameObject hoardPrefab, GameObject shipPrefab, CrewTier tier,
        List<(GameObject Prefab, bool Archer)> guards, Func<float, float, float> height, float water)
    {
        string id = Guid.NewGuid().ToString("N");
        Vector3 hoardPosition = new(spot.x, height(spot.x, spot.y), spot.y);
        ZDO hoard = CreateObject(hoardPrefab, hoardPosition, Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f));
        hoard.Set(RaiderKeys.Site, id);

        ZDO ship = CreateObject(shipPrefab, new Vector3(shipPoint.x, water, shipPoint.y), Quaternion.Euler(0f, RaidSites.ShipHeading(spot, shipPoint), 0f));
        ship.Set(RaiderKeys.State, (int)RaiderState.Moored);
        ship.Set(RaiderKeys.Tier, tier.Index);
        ship.Set(RaiderKeys.Site, id);

        List<Vector2> posts = RaidSites.GuardPosts(spot, SeaBattleSettings.Raids.GuardRadius.Value, guards.Count,
            (float)random.NextDouble() * 360f, height, water);
        for (int i = 0; i < guards.Count; i++)
        {
            Vector3 post = new(posts[i].x, height(posts[i].x, posts[i].y) + GuardLift, posts[i].y);
            Vector2 outward = posts[i] - spot;
            float facing = outward.sqrMagnitude < 0.01f ? 0f : Mathf.Atan2(outward.x, outward.y) * Mathf.Rad2Deg;
            ZDO guard = CreateObject(guards[i].Prefab, post, Quaternion.Euler(0f, facing, 0f));
            guard.Set(RaiderKeys.Site, id);
            guard.Set(RaiderKeys.Post, post);
            guard.Set(RaiderKeys.Role, guards[i].Archer ? 1 : 0);
        }

        Site site = new(hoard.m_uid, id, hoardPosition);
        sites.Add(site);
        Jotunn.Logger.LogInfo($"Raid: a draugr harbour with {guards.Count} guards was made at {hoardPosition.x:0} {hoardPosition.z:0}, its ship at {shipPoint.x:0} {shipPoint.y:0}");
        return site;
    }

    // Creates an object in the world's network data only, as the game does for vegetation in unloaded zones: it is kept
    // in the saved world and comes to life when a player comes near. Nobody owns it until then; that player's client
    // takes it over, so it works on a dedicated server with nobody near.
    private static ZDO CreateObject(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        ZNetView view = prefab.GetComponent<ZNetView>();
        int hash = prefab.name.GetStableHashCode();
        ZDO zdo = ZDOMan.instance.CreateNewZDO(position, hash);
        zdo.Persistent = true;
        zdo.Type = view.m_type;
        zdo.Distant = view.m_distant;
        zdo.SetPrefab(hash);
        zdo.SetRotation(rotation);
        zdo.SetOwnerInternal(0L);
        return zdo;
    }

    // Harbours that have not been sacked yet; the limit counts these only.
    private int ActiveSites()
    {
        int count = 0;
        foreach (Site site in sites)
        {
            ZDO zdo = ZDOMan.instance.GetZDO(site.Hoard);
            if (zdo != null && !zdo.GetBool(RaidHoard.DoneKey))
            {
                count++;
            }
        }

        return count;
    }

    // Built once from every object in the world, then kept up to date; sacked and removed hoards drop out.
    private void RefreshSites()
    {
        if (!sitesScanned)
        {
            sitesScanned = true;
            int hash = NailShipRegistry.HoardName.GetStableHashCode();
            foreach (ZDO zdo in ZDOMan.instance.m_objectsByID.Values)
            {
                if (zdo.GetPrefab() == hash)
                {
                    sites.Add(new Site(zdo.m_uid, zdo.GetString(RaiderKeys.Site), zdo.GetPosition()));
                }
            }
        }

        sites.RemoveAll(site => ZDOMan.instance.GetZDO(site.Hoard) == null);
    }

    private sealed class Site
    {
        public Site(ZDOID hoard, string id, Vector3 position)
        {
            Hoard = hoard;
            Id = id;
            Position = position;
        }

        public ZDOID Hoard { get; }

        public string Id { get; }

        public Vector3 Position { get; }
    }
}
