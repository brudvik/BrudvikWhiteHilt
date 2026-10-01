using BrudvikWhiteHilt.Navigation;
using BrudvikWhiteHilt.Pieces.Ships;
using BrudvikWhiteHilt.Pieces.Ships.WhiteHiltShip;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Navigation;

/// <summary>
/// The route markers of a ship with a Navigator's Table, and sailing them on its own. Markers, route and state live in
/// the ship's ZDO, so everyone aboard sees them and the next owner carries on. Changes go to the owner by RPC.
/// </summary>
public class ShipRoute : MonoBehaviour
{
    /// <summary>Most markers on a route.</summary>
    public static int MaxMarkers => ShipSettings.RouteMaxMarkers.Value;

    private const string MarkersRpc = "WhiteHiltRouteMarkers";
    private const string SailRpc = "WhiteHiltRouteSail";
    private const string StopRpc = "WhiteHiltRouteStop";
    private const string MessageRpc = "WhiteHiltRouteMessage";

    // A marker is reached this close; the route's turning points a little sooner, so the ship starts its turn in time.
    private const float MarkerReached = 60f;
    private const float PointReached = 20f;
    private const float SlowNearEnd = 150f;
    private const float SlowTurnDegrees = 50f;
    private const float CheckInterval = 0.25f;
    private const float KnotsPerMetrePerSecond = 1.94384f;

    // Over the speed limit the sail is reefed to half, and set full again below this share of the limit.
    private const float UnreefBelow = 0.85f;

    // Headings tried around an obstacle, each way from the course.
    private const float AvoidStep = 15f;
    private const float MaxAvoid = 75f;

    private static readonly int markersKey = "whitehilt_route_markers".GetStableHashCode();
    private static readonly int pathKey = "whitehilt_route_path".GetStableHashCode();
    private static readonly int pointKey = "whitehilt_route_point".GetStableHashCode();
    private static readonly int sailingKey = "whitehilt_route_sailing".GetStableHashCode();
    private static readonly List<ShipRoute> instances = new();

    private readonly List<Vector3> markers = new();
    private readonly List<Vector3> path = new();
    private ZNetView nview;
    private Ship ship;
    private ShipChartTable table;
    private ShipAssist assist;
    private WhiteHiltShipUpgrades upgrades;
    private uint readRevision = uint.MaxValue;
    private float nextCheck;
    private float avoidOffset;
    private bool reefed;

    /// <summary>The ship the route belongs to.</summary>
    public Ship Ship => ship;

    /// <summary>The markers still ahead, in order.</summary>
    public IReadOnlyList<Vector3> Markers
    {
        get
        {
            Read();
            return markers;
        }
    }

    /// <summary>True while the ship sails the route on its own.</summary>
    public bool Sailing => nview != null && nview.IsValid() && nview.GetZDO().GetBool(sailingKey);

    /// <summary>
    /// The route of the ship the player is aboard, if it has a Navigator's Table.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>The route, or null.</returns>
    public static ShipRoute Aboard(Player player)
    {
        if (player == null || !ShipSettings.ShipRoutes.Value)
        {
            return null;
        }

        foreach (ShipRoute route in instances)
        {
            if (route.ship != null && route.table != null && route.table.Installed && route.nview.IsValid() && route.ship.IsPlayerInBoat(player))
            {
                return route;
            }
        }

        return null;
    }

    /// <summary>
    /// How far the ship still has to sail along the route.
    /// </summary>
    /// <returns>The distance in metres, 0 when not sailing.</returns>
    public float Remaining()
    {
        if (!Sailing)
        {
            return 0f;
        }

        Read();
        return Remaining(nview.GetZDO().GetInt(pointKey));
    }

    /// <summary>
    /// Replaces the markers. Stops sailing the old route.
    /// </summary>
    /// <param name="points">The new markers.</param>
    public void SetMarkers(IList<Vector3> points)
    {
        nview.InvokeRPC(MarkersRpc, Pack(points));
    }

    /// <summary>
    /// Sails a planned route on its own.
    /// </summary>
    /// <param name="route">The turning points from the ship to the last marker.</param>
    /// <param name="snappedMarkers">The markers, moved into deep water.</param>
    public void Sail(IList<Vector3> route, IList<Vector3> snappedMarkers)
    {
        nview.InvokeRPC(SailRpc, Pack(route), Pack(snappedMarkers));
    }

    /// <summary>
    /// Stops sailing on its own; the markers stay.
    /// </summary>
    public void Stop()
    {
        nview.InvokeRPC(StopRpc);
    }

    private static ZPackage Pack(IList<Vector3> points)
    {
        ZPackage package = new();
        package.Write(points.Count);
        foreach (Vector3 point in points)
        {
            package.Write(point.x);
            package.Write(point.z);
        }

        return package;
    }

    private static void Unpack(byte[] data, List<Vector3> into)
    {
        into.Clear();
        if (data == null || data.Length == 0)
        {
            return;
        }

        ZPackage package = new(data);
        int count = package.ReadInt();
        for (int i = 0; i < count; i++)
        {
            into.Add(new Vector3(package.ReadSingle(), 0f, package.ReadSingle()));
        }
    }

    private static float Bearing(Vector3 from, Vector3 to)
    {
        return Mathf.Repeat(Mathf.Atan2(to.x - from.x, to.z - from.z) * Mathf.Rad2Deg, 360f);
    }

    private void Awake()
    {
        nview = GetComponent<ZNetView>();
        ship = GetComponent<Ship>();
        table = GetComponent<ShipChartTable>();
        assist = GetComponent<ShipAssist>();
        upgrades = GetComponent<WhiteHiltShipUpgrades>();
        if (nview == null || nview.GetZDO() == null)
        {
            return;
        }

        nview.Register<ZPackage>(MarkersRpc, RPC_Markers);
        nview.Register<ZPackage, ZPackage>(SailRpc, RPC_Sail);
        nview.Register(StopRpc, RPC_Stop);
        nview.Register<string>(MessageRpc, RPC_Message);
        instances.Add(this);
    }

    private void OnDestroy()
    {
        instances.Remove(this);
    }

    private void Read()
    {
        if (nview == null || !nview.IsValid())
        {
            markers.Clear();
            path.Clear();
            return;
        }

        ZDO zdo = nview.GetZDO();
        if (zdo.DataRevision == readRevision)
        {
            return;
        }

        readRevision = zdo.DataRevision;
        Unpack(zdo.GetByteArray(markersKey), markers);
        Unpack(zdo.GetByteArray(pathKey), path);
    }

    private void Write(ZDO zdo, List<Vector3> points, int key)
    {
        zdo.Set(key, Pack(points).GetArray());
    }

    private void FixedUpdate()
    {
        if (ship == null || nview == null || !nview.IsValid() || !nview.IsOwner())
        {
            return;
        }

        Read();
        if (assist == null)
        {
            assist = GetComponent<ShipAssist>();
        }

        ZDO zdo = nview.GetZDO();
        Vector3 here = transform.position;
        if (ShipSettings.ShipRoutes.Value && markers.Count > 0 && Utils.DistanceXZ(here, markers[0]) < MarkerReached)
        {
            markers.RemoveAt(0);
            Write(zdo, markers, markersKey);
        }

        if (!zdo.GetBool(sailingKey))
        {
            return;
        }

        if (ship.m_players.Count == 0 || ship.HaveControllingPlayer() || table == null || !table.Installed
            || !ShipSettings.ShipRoutes.Value || !ShipSettings.RouteAutopilot.Value)
        {
            StopSailing(zdo, ship.HaveControllingPlayer() ? "$whitehilt_route_taken" : null);
            return;
        }

        if (upgrades != null && upgrades.IsAnchored)
        {
            StopSailing(zdo, "$whitehilt_route_anchored");
            return;
        }

        int point = zdo.GetInt(pointKey);
        while (point < path.Count && Utils.DistanceXZ(here, path[point]) < (point == path.Count - 1 ? MarkerReached : PointReached))
        {
            point++;
            zdo.Set(pointKey, point);
        }

        if (point >= path.Count)
        {
            markers.Clear();
            Write(zdo, markers, markersKey);
            StopSailing(zdo, "$whitehilt_route_arrived");
            return;
        }

        Vector3 next = path[point];
        float course = Bearing(here, next);
        if (Time.time >= nextCheck)
        {
            nextCheck = Time.time + CheckInterval;
            if (!FindWayAround(course, Utils.DistanceXZ(here, next) + PointReached))
            {
                StopSailing(zdo, "$whitehilt_route_shallow");
                return;
            }
        }

        float heading = course + avoidOffset;
        assist?.SteerTowards(heading);

        float left = Remaining(point);
        bool turning = Mathf.Abs(Mathf.DeltaAngle(ShipAssist.Heading(transform), heading)) > SlowTurnDegrees;
        UpdateReef();
        ship.m_speed = left < SlowNearEnd || turning || avoidOffset != 0f || reefed ? Ship.Speed.Half : Ship.Speed.Full;
    }

    private void UpdateReef()
    {
        float limit = ShipSettings.RouteMaxSpeed.Value;
        float knots = Mathf.Abs(ship.GetSpeed()) * KnotsPerMetrePerSecond;
        if (limit > 0f && knots > limit)
        {
            reefed = true;
        }
        else if (limit <= 0f || knots < limit * UnreefBelow)
        {
            reefed = false;
        }
    }

    // Keeps the course when it is clear, else the nearest clear heading, trying the side already taken first.
    // Looks no further than the next turning point, so land beyond the turn does not count.
    private bool FindWayAround(float course, float maxDistance)
    {
        if (assist == null)
        {
            avoidOffset = 0f;
            return true;
        }

        float look = Mathf.Min(assist.Lookahead(), maxDistance);
        if (!assist.Blocked(course, look))
        {
            avoidOffset = 0f;
            return true;
        }

        float side = avoidOffset < 0f ? -1f : 1f;
        for (float angle = AvoidStep; angle <= MaxAvoid; angle += AvoidStep)
        {
            foreach (float sign in new[] { side, -side })
            {
                if (!assist.Blocked(course + sign * angle, look))
                {
                    avoidOffset = sign * angle;
                    return true;
                }
            }
        }

        return false;
    }

    private float Remaining(int point)
    {
        if (point >= path.Count)
        {
            return 0f;
        }

        float left = Utils.DistanceXZ(transform.position, path[point]);
        for (int i = point + 1; i < path.Count; i++)
        {
            left += Utils.DistanceXZ(path[i - 1], path[i]);
        }

        return left;
    }

    private void StopSailing(ZDO zdo, string message)
    {
        zdo.Set(sailingKey, false);
        ship.m_speed = Ship.Speed.Stop;
        avoidOffset = 0f;
        reefed = false;
        if (!string.IsNullOrEmpty(message))
        {
            nview.InvokeRPC(ZNetView.Everybody, MessageRpc, message);
        }
    }

    private void RPC_Markers(long sender, ZPackage package)
    {
        if (!nview.IsOwner())
        {
            return;
        }

        List<Vector3> points = new();
        Unpack(package.GetArray(), points);
        ZDO zdo = nview.GetZDO();
        Write(zdo, points.GetRange(0, Mathf.Min(points.Count, MaxMarkers)), markersKey);
        if (zdo.GetBool(sailingKey))
        {
            StopSailing(zdo, "$whitehilt_route_stopped");
        }
    }

    private void RPC_Sail(long sender, ZPackage route, ZPackage snappedMarkers)
    {
        if (!nview.IsOwner() || !ShipSettings.ShipRoutes.Value || !ShipSettings.RouteAutopilot.Value)
        {
            return;
        }

        List<Vector3> points = new();
        Unpack(route.GetArray(), points);
        List<Vector3> snapped = new();
        Unpack(snappedMarkers.GetArray(), snapped);
        if (points.Count == 0)
        {
            return;
        }

        ZDO zdo = nview.GetZDO();
        Write(zdo, points, pathKey);
        Write(zdo, snapped, markersKey);
        zdo.Set(pointKey, 0);
        zdo.Set(sailingKey, true);
        assist?.StopHolding();
        upgrades?.WeighAnchor();
        nview.InvokeRPC(ZNetView.Everybody, MessageRpc, "$whitehilt_route_started");
    }

    private void RPC_Stop(long sender)
    {
        if (nview.IsOwner() && nview.GetZDO().GetBool(sailingKey))
        {
            StopSailing(nview.GetZDO(), "$whitehilt_route_stopped");
        }
    }

    private void RPC_Message(long sender, string message)
    {
        Player player = Player.m_localPlayer;
        if (player != null && ship != null && ship.IsPlayerInBoat(player))
        {
            player.Message(MessageHud.MessageType.Center, message);
        }
    }
}
