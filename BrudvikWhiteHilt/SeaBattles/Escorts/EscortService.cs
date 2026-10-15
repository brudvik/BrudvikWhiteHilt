using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Summoning;
using BrudvikWhiteHilt.Navigation;
using BrudvikWhiteHilt.SeaBattles.Fleets;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.SeaBattles.Escorts;

/// <summary>
/// What the trading ship's owner tells the players, by the message that goes to everybody and is shown to those it concerns.
/// The numbers go over the network, so they never change.
/// </summary>
public enum EscortNotice
{
    /// <summary>A wave of Nail Ships has been raised against the merchant.</summary>
    Wave = 0,

    /// <summary>Raiders stand on the merchant's deck and carry the cargo off.</summary>
    Plunder = 1,

    /// <summary>The merchant is in port; the pin goes. The players' pay and the deed come by their own messages.</summary>
    Arrived = 2,

    /// <summary>The merchant sank.</summary>
    Sunk = 3,

    /// <summary>The cargo is gone.</summary>
    Plundered = 4,

    /// <summary>Nobody was near for too long, and the merchant gave up.</summary>
    Abandoned = 5,

    /// <summary>The merchant waited at the trading post, but the contractor never went aboard; the contract goes back.</summary>
    Expired = 6,

    /// <summary>The contractor went aboard, and the merchant has cast off from the trading post.</summary>
    SetOut = 7,

    /// <summary>The merchant ran aground and found no way on; the ship is gone and the contract goes back.</summary>
    Stranded = 8
}

/// <summary>
/// Runs on the <see cref="Game"/> object, on every machine. A client that uses an Escort Contract aboard its ship asks the
/// server; the server checks the request again from what only it knows (the player and the ship it names, the key of the
/// sea battles, the cooldown, a merchant already on its way, the open sea and a line of deep water to a port, from the world
/// generator), keeps the cooldowns and the list of merchants on their way in its memory, and answers the client with the port
/// or the reason it was refused. The client raises the trading ship, uses up the contract, and marks the port on its map.
/// Admins can start an escort with whitehilt_seabattle escort, which skips the contract, the key, the cooldown and the
/// check for a merchant on its way, and send the next wave at once with escort wave. On the way the merchant's owner asks the
/// server for each wave when it is due; the server checks the request and orders the Nail Ships raised on the owner's
/// machine, as for a natural encounter. The owner also pays the players who are near when the merchant reaches port.
/// </summary>
public class EscortService : MonoBehaviour
{
    private const string RequestRpc = "WhiteHiltEscortRequest";
    private const string PostRequestRpc = "WhiteHiltEscortPostRequest";
    private const string PostStartRpc = "WhiteHiltEscortPostStart";
    private const string AdminRpc = "WhiteHiltEscortAdmin";
    private const string StartRpc = "WhiteHiltEscortStart";
    private const string RefusedRpc = "WhiteHiltEscortRefused";
    private const string RaisedRpc = "WhiteHiltEscortRaised";
    private const string WaveRpc = "WhiteHiltEscortWave";
    private const string SpawnRpc = "WhiteHiltEscortSpawn";
    private const string PaidRpc = "WhiteHiltEscortPaid";
    private const string NoticeRpc = "WhiteHiltEscortNotice";

    // What an admin asks for: a trading ship (0), or the next wave at once (1).
    private const int AdminStart = 0;
    private const int AdminWave = 1;

    // Reasons the server gives for a refusal; each has a message, msg_whitehilt_escort_refused_<reason>.
    private const string ReasonOff = "off";
    private const string ReasonSea = "sea";
    private const string ReasonRoute = "route";
    private const string ReasonBusy = "busy";
    private const string ReasonPost = "post";
    private const string ReasonNoPort = "noport";
    private const string ReasonNoRoute = "noroute";

    // A request without an answer (server restarted, connection lost) may be sent again after this many seconds. An answer
    // is only taken while one is awaited, for about this long; the server may need several seconds to plan a long route.
    private const float RequestTimeout = 60f;

    // How long the server's route search may run in one frame, in milliseconds; nobody plays on a dedicated server, and
    // a longer search per frame gives the player the answer sooner.
    private const double ServerSearchMilliseconds = 12.0;

    // The server counts a merchant as on its way from the moment it approves the contract: for this many seconds until the
    // client says it raised the ship, and for this many seconds until the ship's data reaches the server.
    private const float PendingSeconds = 45f;
    private const float UnseenSeconds = 60f;

    // A pay message is believed when the player is no farther from the ship than the reward range plus this margin, as the
    // player may have moved since the owner looked; a notice is shown no farther away than the longest range.
    private const float PayMargin = 50f;
    private const float MaxNoticeRange = 2000f;

    // The pin on the map is dropped when the ship's data is known here and says the escort ended, or has been unknown this long.
    private const float PinCheckInterval = 2f;
    private const float PinUnseenSeconds = 60f;

    // How far outside a post's dome a contract still counts as used at the post, for the player's movement since asking.
    private const float PostReach = 10f;

    // The port lies this far off the destination post's origin along its jetty: 11 m past the jetty head.
    private const float PortOffJetty = 30f;

    // The server's look through the trading ships: how often, how many sectors a frame, who counts as near, and how long
    // an ended ship lies before it is taken away.
    private const float SweepInterval = 60f;
    private const int SweepSectorsPerFrame = 200;
    private const float NearAnyone = 250f;
    private const double EndedRemoveSeconds = 300.0;

    private static EscortService instance;
    private readonly List<ZDO> swept = new();
    private bool sweeping;
    private int sweepIndex;
    private float nextSweep;
    private static float requestedAt = float.NegativeInfinity;

    // On the server: the merchants on their way, by the contractor's player id, and when each player may call another.
    private readonly Dictionary<long, Active> active = new();
    private readonly Dictionary<long, float> cooldownUntil = new();

    // On the client: the ships it has been paid for, so a message sent twice pays once.
    private readonly HashSet<ZDOID> paid = new();

    // On the client that called a merchant: the pins on the map, by the ship's id.
    private readonly Dictionary<ZDOID, Mark> marks = new();
    private float nextPinCheck;

    private sealed class Active
    {
        public ZDOID Ship = ZDOID.None;
        public float Since;
    }

    private sealed class Mark
    {
        public Minimap.PinData Pin;
        public float MissingSince;
    }

    /// <summary>
    /// Asks the server to send a trading ship for the ship the local player is aboard. The contract is used up when the
    /// ship is raised, not before. Does nothing while an answer is awaited.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Request(Player player)
    {
        if (instance == null || ZRoutedRpc.instance == null || player == null || Time.time - requestedAt < RequestTimeout)
        {
            return;
        }

        if (!EscortSettings.Enabled.Value || !SeaBattleSettings.Enabled.Value)
        {
            Say(player, ReasonOff);
            return;
        }

        // With trading posts the contract is used at a post, whose trader sells it, and the merchant comes in there.
        if (TradingPosts.TradingPostSettings.Enabled.Value)
        {
            TradingPosts.TradingPostSite site = TradingPosts.TradingPostSite.Within(player.transform.position);
            if (site == null)
            {
                Say(player, ReasonPost);
                return;
            }

            requestedAt = Time.time;
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), PostRequestRpc, site.Id);
            return;
        }

        Ship ship = AboardShip(player);
        if (ship == null || !SeaBattleService.IsOcean(ship.transform.position))
        {
            Say(player, ReasonSea);
            return;
        }

        requestedAt = Time.time;
        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), RequestRpc, ship.m_nview.GetZDO().m_uid);
    }

    /// <summary>
    /// Asks the server, as an admin, to send a trading ship for the ship the local player is aboard, with no contract; or,
    /// with <paramref name="wave"/>, to send the next wave of Nail Ships against the nearest merchant on its way at once.
    /// </summary>
    /// <param name="wave">True for the next wave, false for a new merchant.</param>
    public static void RequestAdmin(bool wave = false)
    {
        Player player = Player.m_localPlayer;
        Ship ship = player != null ? AboardShip(player) : null;
        if (instance == null || ZRoutedRpc.instance == null || (ship == null && !wave))
        {
            Print("Board a ship at sea first.");
            return;
        }

        if (!wave)
        {
            requestedAt = Time.time;
        }

        ZDOID shipId = wave || ship == null ? ZDOID.None : ship.m_nview.GetZDO().m_uid;
        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), AdminRpc, shipId, wave ? AdminWave : AdminStart);
    }

    /// <summary>
    /// Asks the server for a wave of Nail Ships against a trading ship that is due one. Called by the ship's owner; the
    /// server checks the request and orders the raising back to the owner.
    /// </summary>
    /// <param name="trade">The trading ship.</param>
    /// <param name="index">The number of the wave, from 0: how many have been sent.</param>
    internal static void RequestWave(TradeShip trade, int index)
    {
        if (instance == null || ZRoutedRpc.instance == null || trade == null || trade.Id.IsNone())
        {
            return;
        }

        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), WaveRpc, trade.Id, index);
    }

    /// <summary>
    /// Tells everybody something about a trading ship; the machines of the players it concerns show it. Called by the
    /// ship's owner.
    /// </summary>
    /// <param name="kind">What happened.</param>
    /// <param name="trade">The trading ship.</param>
    /// <param name="range">The players within this many metres of the ship are told; the contractor is also told of a failure.</param>
    internal static void Notice(EscortNotice kind, TradeShip trade, float range)
    {
        if (ZRoutedRpc.instance == null || trade == null)
        {
            return;
        }

        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, NoticeRpc, (int)kind, trade.Id, trade.Contractor, trade.transform.position, range);
    }

    /// <summary>
    /// Pays every player within reach of a trading ship in port, by the cargo left: each one's own machine adds the coins and
    /// rolls for the chart. Called by the ship's owner.
    /// </summary>
    /// <param name="trade">The trading ship.</param>
    /// <param name="cargo">The cargo left, 0 to 1.</param>
    internal static void Pay(TradeShip trade, float cargo)
    {
        if (ZRoutedRpc.instance == null || trade == null)
        {
            return;
        }

        foreach (Player player in Player.GetAllPlayers())
        {
            if (player != null && !player.IsDead() && Vector3.Distance(player.transform.position, trade.transform.position) <= EscortSettings.RewardRange.Value)
            {
                ZRoutedRpc.instance.InvokeRoutedRPC(player.GetOwner(), PaidRpc, trade.Id, cargo);
            }
        }
    }

    /// <summary>
    /// Whether no natural Nail Ship encounter is to be rolled at a place because a trading ship is on its way near it. Asked
    /// by the server once per encounter request, which is once a minute per ship at most.
    /// </summary>
    /// <param name="position">The place, in world space.</param>
    /// <returns>True when a merchant on its way is within <see cref="EscortSettings.NoEncounterRange"/>.</returns>
    public static bool Quiet(Vector3 position)
    {
        if (instance == null || EscortSettings.NoEncounterRange.Value <= 0f)
        {
            return false;
        }

        List<Vector2> merchants = new();
        foreach (ZDO zdo in instance.MerchantsOnTheirWay())
        {
            Vector3 at = zdo.GetPosition();
            merchants.Add(new Vector2(at.x, at.z));
        }

        return EscortRules.AnyWithin(new Vector2(position.x, position.z), merchants, EscortSettings.NoEncounterRange.Value);
    }

    // The ship the local player is aboard, if it is an ordinary ship; null for none, a Nail Ship or a trading ship.
    private static Ship AboardShip(Player player)
    {
        Ship ship = Ship.GetLocalShip() ?? player.GetStandingOnShip();
        return ship == null || ship.m_nview == null || !ship.m_nview.IsValid() || RaiderShip.Is(ship) || TradeShip.Is(ship) ? null : ship;
    }

    private static void Say(Player player, string reason)
    {
        player.Message(MessageHud.MessageType.Center, Translations.Word("msg_whitehilt_escort_refused_" + reason));
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
        ZRoutedRpc.instance?.Register<ZDOID>(RequestRpc, RPC_Request);
        ZRoutedRpc.instance?.Register<ZDOID>(PostRequestRpc, RPC_PostRequest);
        ZRoutedRpc.instance?.Register<ZDOID, Vector3, int, ZPackage>(PostStartRpc, RPC_PostStart);
        ZRoutedRpc.instance?.Register<ZDOID, int>(AdminRpc, RPC_Admin);
        ZRoutedRpc.instance?.Register<ZDOID, Vector3, bool>(StartRpc, RPC_Start);
        ZRoutedRpc.instance?.Register<string>(RefusedRpc, RPC_Refused);
        ZRoutedRpc.instance?.Register<ZDOID>(RaisedRpc, RPC_Raised);
        ZRoutedRpc.instance?.Register<ZDOID, int>(WaveRpc, RPC_Wave);
        ZRoutedRpc.instance?.Register<ZDOID, int, int, int, int>(SpawnRpc, RPC_Spawn);
        ZRoutedRpc.instance?.Register<ZDOID, float>(PaidRpc, RPC_Paid);
        ZRoutedRpc.instance?.Register<int, ZDOID, long, Vector3, float>(NoticeRpc, RPC_Notice);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void Update()
    {
        if (ZNet.instance != null && ZNet.instance.IsServer())
        {
            SweepStep();
        }

        if (marks.Count == 0 || Time.time < nextPinCheck)
        {
            return;
        }

        nextPinCheck = Time.time + PinCheckInterval;
        CheckPins();
    }

    // On the server: now and then looks through the world's trading ships, a few sectors a frame as there may be
    // millions of objects, and deals with those nobody is near (see Tidy).
    private void SweepStep()
    {
        if (ZDOMan.instance == null)
        {
            return;
        }

        if (!sweeping)
        {
            if (Time.time < nextSweep)
            {
                return;
            }

            sweeping = true;
            sweepIndex = 0;
            swept.Clear();
        }

        for (int step = 0; step < SweepSectorsPerFrame; step++)
        {
            if (ZDOMan.instance.GetAllZDOsWithPrefabIterative(TradeShipRegistry.ShipName, swept, ref sweepIndex))
            {
                sweeping = false;
                nextSweep = Time.time + SweepInterval;
                foreach (ZDO zdo in swept)
                {
                    Tidy(zdo);
                }

                swept.Clear();
                return;
            }
        }
    }

    // On the server: a trading ship that nobody is near no longer has anyone to look after it. One that waited at a post
    // too long expires and its contract goes back; one left alone at sea is abandoned; one whose business is done is
    // taken away, so none stays in the saved world. The server takes the ship over to write it.
    private void Tidy(ZDO zdo)
    {
        if (zdo == null || !zdo.IsValid() || AnyoneNear(zdo.GetPosition()))
        {
            return;
        }

        long now = ZNet.instance.GetTime().Ticks;
        StaleAction action = EscortRules.Stale((TradeState)zdo.GetInt(TradeKeys.State), Since(now, zdo.GetLong(TradeKeys.Seen)),
            Since(now, zdo.GetLong(TradeKeys.RaisedAt)), Since(now, zdo.GetLong(TradeKeys.EndedAt)), -Since(now, zdo.GetLong(TradeKeys.VisitUntil)),
            EscortSettings.WaitSeconds.Value, EscortSettings.AbandonSeconds.Value, EndedRemoveSeconds);
        if (action == StaleAction.None)
        {
            return;
        }

        zdo.SetOwner(ZDOMan.GetSessionID());
        long contractor = zdo.GetLong(TradeKeys.Contractor);
        switch (action)
        {
            case StaleAction.Expire:
            case StaleAction.Abandon:
                zdo.Set(TradeKeys.State, (int)TradeState.Lost);
                zdo.Set(TradeKeys.EndedAt, now);
                EscortNotice notice = action == StaleAction.Expire ? EscortNotice.Expired : EscortNotice.Abandoned;
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, NoticeRpc, (int)notice, zdo.m_uid, contractor, zdo.GetPosition(), 0f);
                if (active.TryGetValue(contractor, out Active record) && record.Ship == zdo.m_uid)
                {
                    active.Remove(contractor);
                    if (action == StaleAction.Expire)
                    {
                        cooldownUntil.Remove(contractor);
                    }
                }

                Jotunn.Logger.LogInfo($"Escort: the trading ship at {zdo.GetPosition()} was left alone; {action}");
                break;
            case StaleAction.Remove:
                ZDOMan.instance.DestroyZDO(zdo);
                break;
        }
    }

    // Seconds since a time in world ticks; a time never written counts as long ago.
    private static double Since(long now, long ticks)
    {
        return ticks == 0L ? double.MaxValue / 2.0 : (now - ticks) / (double)System.TimeSpan.TicksPerSecond;
    }

    // Whether any player online is within reach of a point, by where their machine keeps its world.
    private static bool AnyoneNear(Vector3 position)
    {
        foreach (ZNetPeer peer in ZNet.instance.GetPeers())
        {
            if (Utils.DistanceXZ(peer.m_refPos, position) <= NearAnyone)
            {
                return true;
            }
        }

        return Player.m_localPlayer != null && Utils.DistanceXZ(Player.m_localPlayer.transform.position, position) <= NearAnyone;
    }

    // On the server: a player used a contract. The server asks the same questions as for an admin's call, and more.
    private void RPC_Request(long sender, ZDOID shipId)
    {
        Approve(sender, shipId, admin: false);
    }

    // On the server: a player used a contract at a trading post. The sender must be a real connection with a living player
    // inside the post's dome, past the cooldown and with no merchant on its way; the world must have reached the key of
    // the sea battles; and there must be another post to sail to, with a sea route to it. The port is the nearest other
    // post, however far, 30 m off its jetty head, and the pay grows with the length of the route.
    private void RPC_PostRequest(long sender, ZDOID postId)
    {
        if (ZNet.instance == null || !ZNet.instance.IsServer() || ZDOMan.instance == null || RoutedRpcSender.Verify(sender, out _) == null)
        {
            return;
        }

        ZDO player = SeaBattleService.PlayerZdo(sender);
        ZDO post = postId.IsNone() ? null : ZDOMan.instance.GetZDO(postId);
        if (player == null)
        {
            return;
        }

        if (!EscortSettings.Enabled.Value || !SeaBattleSettings.Enabled.Value || !SeaBattleService.HasKey(SeaBattleSettings.RequiredKey.Value))
        {
            Refuse(sender, ReasonOff);
            return;
        }

        if (post == null || post.GetPrefab() != TradingPosts.TradingPostKeys.MarkerHash
            || Utils.DistanceXZ(player.GetPosition(), post.GetPosition()) > TradingPosts.TradingPostLayout.Load().dome.radius + PostReach)
        {
            Refuse(sender, ReasonPost);
            return;
        }

        long contractor = player.GetLong(ZDOVars.s_playerID);
        if (IsBusy(contractor) || cooldownUntil.TryGetValue(contractor, out float until) && Time.time < until)
        {
            Refuse(sender, ReasonBusy);
            return;
        }

        ZDO other = TradingPosts.TradingPostBuilder.Markers()
            .Where(zdo => zdo.m_uid != post.m_uid)
            .OrderBy(zdo => Utils.DistanceXZ(zdo.GetPosition(), post.GetPosition()))
            .FirstOrDefault();
        if (other == null)
        {
            Refuse(sender, ReasonNoPort);
            return;
        }

        Vector3 start = post.GetPosition() + post.GetRotation() * Vector3.forward * PortOffJetty;
        start.y = ZoneSystem.instance.m_waterLevel;
        Vector3 port = other.GetPosition() + other.GetRotation() * Vector3.forward * PortOffJetty;
        port.y = ZoneSystem.instance.m_waterLevel;
        cooldownUntil[contractor] = Time.time + EscortSettings.CooldownMinutes.Value * 60f;
        active[contractor] = new Active { Since = Time.time };
        StartCoroutine(PlanPostVoyage(sender, contractor, postId, start, port));
    }

    // On the server: finds the way from a post to the port over the world generator's ground, as the Navigator's Table
    // does, a few frames at a time. The contract is refused when there is no way, so nobody pays for a merchant that could
    // never arrive. The port is moved into deep water where the jetty head lies in shallows, the pay grows with the length
    // of the route round the islands and not the line over them, and the route goes to the client with the answer.
    private IEnumerator PlanPostVoyage(long sender, long contractor, ZDOID postId, Vector3 start, Vector3 port)
    {
        List<Vector3> route = null;
        List<Vector3> harbours = null;
        yield return SeaRouteFinder.Find(start, new List<Vector3> { port }, null, (found, snapped) =>
        {
            route = found;
            harbours = snapped;
        }, ServerSearchMilliseconds);

        if (route == null || route.Count == 0 || harbours == null || harbours.Count == 0)
        {
            active.Remove(contractor);
            cooldownUntil.Remove(contractor);
            Jotunn.Logger.LogInfo($"Escort: no sea route from {start.x:0} {start.z:0} to {port.x:0} {port.z:0}; the contract is refused");
            Refuse(sender, ReasonNoRoute);
            yield break;
        }

        Vector3 harbour = harbours[harbours.Count - 1];
        harbour.y = ZoneSystem.instance.m_waterLevel;
        int pay = EscortRules.PayForVoyage(EscortSettings.PayPerKilometre.Value, RouteTrack.Length(route));
        if (active.TryGetValue(contractor, out Active record))
        {
            record.Since = Time.time;
        }

        Jotunn.Logger.LogInfo($"Escort: a merchant comes to the post for {sender}, bound for {harbour.x:0} {harbour.z:0} " +
            $"over {route.Count} turning points, {RouteTrack.Length(route):0} m, {pay} coins");
        ZRoutedRpc.instance.InvokeRoutedRPC(sender, PostStartRpc, postId, harbour, pay, RouteTrack.Pack(route));
    }

    // On the client that was sent a port from a trading post: brings the trading ship in beside the post's jetty, on the
    // side away from the player's own ship, where it waits; uses up the contract; marks the port; tells the server which
    // ship it is. Only the server may order this, and only after this client asked.
    private void RPC_PostStart(long sender, ZDOID postId, Vector3 port, int pay, ZPackage routePackage)
    {
        if (sender != ZRoutedRpc.instance.GetServerPeerID() || Time.time - requestedAt > RequestTimeout)
        {
            return;
        }

        requestedAt = float.NegativeInfinity;
        Player player = Player.m_localPlayer;
        GameObject found = ZNetScene.instance != null ? ZNetScene.instance.FindInstance(postId) : null;
        ItemDrop.ItemData contract = player != null ? FindContract(player) : null;
        List<Vector3> route = new();
        RouteTrack.Unpack(routePackage.GetArray(), route);
        TradeShip trade = found != null && player != null && contract != null
            ? TradeShip.RaiseAtPost(found.transform, new Vector2(port.x, port.z), player.GetPlayerID(), Mathf.Max(0, pay), route)
            : null;
        if (trade == null)
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), RaisedRpc, ZDOID.None);
            if (player != null && found != null && contract != null)
            {
                Say(player, ReasonRoute);
            }

            return;
        }

        player.GetInventory().RemoveOneItem(contract);
        ZDOID tradeId = trade.GetComponent<ZNetView>().GetZDO().m_uid;
        MarkPort(tradeId, port);
        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), RaisedRpc, tradeId);
        player.Message(MessageHud.MessageType.Center, Translations.Word("msg_whitehilt_escort_waiting"));
    }

    // On the server: an admin asks for a trading ship at once.
    private void RPC_Admin(long sender, ZDOID shipId, int mode)
    {
        if (ZNet.instance == null || !ZNet.instance.IsServer())
        {
            return;
        }

        if (!SeaBattleService.IsAdmin(sender))
        {
            SeaBattleService.Reply(sender, "Only admins can do that.");
            return;
        }

        if (mode == AdminWave)
        {
            SendNextWave(sender);
            return;
        }

        Approve(sender, shipId, admin: true);
    }

    // On the server: an admin asks for the next wave against the merchant nearest to them, at once, whatever the progress.
    private void SendNextWave(long sender)
    {
        ZDO player = SeaBattleService.PlayerZdo(sender);
        ZDO nearest = null;
        float best = float.MaxValue;
        foreach (ZDO zdo in MerchantsOnTheirWay())
        {
            float distance = player != null ? Vector3.Distance(player.GetPosition(), zdo.GetPosition()) : 0f;
            if (distance < best)
            {
                best = distance;
                nearest = zdo;
            }
        }

        if (nearest == null)
        {
            SeaBattleService.Reply(sender, "No trading ship is on its way. Start one with whitehilt_seabattle escort.");
            return;
        }

        SeaBattleService.Reply(sender, SendWave(nearest, nearest.GetInt(TradeKeys.WavesSent))
            ? "A wave of Nail Ships is sent against the merchant."
            : "No wave could be sent; see the log.");
    }

    // On the server: the trading ship's owner says a wave is due. The ship must exist, be the trading ship, still sail, belong
    // to the sender, and the number must be the next one; the server writes nothing, it orders the Nail Ships raised.
    private void RPC_Wave(long sender, ZDOID shipId, int index)
    {
        if (ZNet.instance == null || !ZNet.instance.IsServer() || ZDOMan.instance == null || RoutedRpcSender.Verify(sender, out _) == null
            || !EscortSettings.Enabled.Value || !SeaBattleSettings.Enabled.Value)
        {
            return;
        }

        ZDO zdo = shipId.IsNone() ? null : ZDOMan.instance.GetZDO(shipId);
        if (zdo == null || !IsSailing(zdo) || zdo.GetOwner() != sender || zdo.GetInt(TradeKeys.WavesSent) != index
            || index < 0 || index >= zdo.GetInt(TradeKeys.Waves))
        {
            return;
        }

        SendWave(zdo, index);
    }

    // On the server: picks the crew from the world's keys (the first tier when none applies, so a test world works), the
    // extra crew from the players near the merchant and the size of the fleet from the fleet tiers, and orders the
    // Nail Ships raised on the merchant's owner's machine.
    private static bool SendWave(ZDO zdo, int index)
    {
        IReadOnlyList<CrewTier> tiers = CrewTable.Parse(SeaBattleSettings.CrewTiers.Value);
        CrewTier tier = CrewTable.Pick(tiers, key => ZoneSystem.instance.GetGlobalKey(key)) ?? (tiers.Count > 0 ? tiers[0] : null);
        if (tier == null || zdo.GetOwner() == 0L)
        {
            Jotunn.Logger.LogWarning("Escort: a wave could not be sent: no crew tier, or the merchant has no owner");
            return false;
        }

        int bonus = EncounterRules.CrewBonus(SeaBattleService.PlayersNear(zdo.GetPosition()), SeaBattleSettings.CrewPerExtraPlayer.Value);
        int size = SeaBattleService.PickFleetSize();
        bool fireShip = SeaBattleService.PickFireShip();
        Jotunn.Logger.LogInfo($"Escort: wave {index + 1} against the merchant: {(size > 1 ? $"a fleet of {size}" : "one ship")}{(fireShip ? " and a fire ship" : string.Empty)}, tier {tier.Key}, {bonus} extra crew");
        ZRoutedRpc.instance.InvokeRoutedRPC(zdo.GetOwner(), SpawnRpc, zdo.m_uid, index, tier.Index, bonus, EncounterRules.PackShips(size, fireShip));
        return true;
    }

    // On the trading ship's owner: raises the Nail Ships of a wave against it, as a natural encounter would, but with the
    // merchant as the target. Only the server may order this, and only for the next wave of a ship this machine owns. The
    // wave is counted when the ships are up; if there was no deep water for them the owner asks again.
    private void RPC_Spawn(long sender, ZDOID shipId, int index, int tierIndex, int crewBonus, int packed)
    {
        if (sender != ZRoutedRpc.instance.GetServerPeerID())
        {
            return;
        }

        GameObject found = ZNetScene.instance != null ? ZNetScene.instance.FindInstance(shipId) : null;
        TradeShip trade = found != null ? found.GetComponent<TradeShip>() : null;
        IReadOnlyList<CrewTier> tiers = CrewTable.Parse(SeaBattleSettings.CrewTiers.Value);
        if (trade == null || !trade.IsOwner || trade.State != TradeState.Sailing || trade.WavesSent != index || tierIndex < 0 || tierIndex >= tiers.Count)
        {
            return;
        }

        int size = EncounterRules.UnpackShips(packed, out bool fireShip, out bool ram);
        RaiderShip lead = RaiderShip.SpawnFleet(trade.Ship, Mathf.Max(0, crewBonus), tiers[tierIndex], Mathf.Clamp(size, 1, FleetRules.MaxSize), fireShip, ram);
        if (lead == null)
        {
            return;
        }

        trade.WaveSpawned();
        Notice(EscortNotice.Wave, trade, EscortSettings.RewardRange.Value);
    }

    // On every client: shows what the trading ship's owner says to the players it concerns, and takes the pin off the map
    // when the escort has ended. A wave sounds the horn where it is heard. The text is chosen here from the number, never
    // taken from the message, and the range is held to a sensible size.
    private void RPC_Notice(long sender, int kind, ZDOID shipId, long contractor, Vector3 position, float range)
    {
        if (kind < (int)EscortNotice.Wave || kind > (int)EscortNotice.Stranded)
        {
            return;
        }

        EscortNotice notice = (EscortNotice)kind;
        string shout = notice switch { EscortNotice.SetOut => "depart", EscortNotice.Arrived => "arrive", EscortNotice.Wave => "wave", _ => null };
        if (shout != null && ZNetScene.instance != null)
        {
            ZNetScene.instance.FindInstance(shipId)?.GetComponent<TradeShipSkipper>()?.Shout(shout);
        }

        bool failure = notice == EscortNotice.Sunk || notice == EscortNotice.Plundered || notice == EscortNotice.Abandoned;
        bool returned = notice == EscortNotice.Expired || notice == EscortNotice.Stranded;
        if (failure || returned || notice == EscortNotice.Arrived)
        {
            DropPin(shipId);
        }

        if (notice == EscortNotice.Stranded)
        {
            Release(sender, shipId, contractor);
        }

        Player player = Player.m_localPlayer;
        if (returned)
        {
            // The merchant waited in vain or ran aground: the contractor gets the contract back, once for the ship.
            if (player != null && contractor != 0L && player.GetPlayerID() == contractor && paid.Add(shipId))
            {
                Give(player, EscortContract.PrefabName, 1);
                string back = notice == EscortNotice.Stranded ? "msg_whitehilt_escort_stranded" : "msg_whitehilt_escort_expired";
                player.Message(MessageHud.MessageType.Center, Translations.Word(back));
            }

            return;
        }

        string token = notice switch
        {
            EscortNotice.Wave => "msg_whitehilt_escort_wave",
            EscortNotice.Plunder => "msg_whitehilt_escort_plunder",
            EscortNotice.Sunk => "msg_whitehilt_escort_sunk",
            EscortNotice.Plundered => "msg_whitehilt_escort_plundered",
            EscortNotice.Abandoned => "msg_whitehilt_escort_abandoned",
            EscortNotice.SetOut => "msg_whitehilt_escort_setout",
            _ => null
        };
        if (player == null || token == null)
        {
            return;
        }

        bool near = Vector3.Distance(player.transform.position, position) <= Mathf.Clamp(range, 0f, MaxNoticeRange);
        bool contractorHere = failure && contractor != 0L && player.GetPlayerID() == contractor;
        if (!near && !contractorHere)
        {
            return;
        }

        if (notice == EscortNotice.Wave && near)
        {
            HornCaller.PlayAt(position, SeaBattleSettings.SpawnDistance.Value * 2f);
        }

        player.Message(MessageHud.MessageType.Center, Translations.Word(token));
    }

    // On the server: a merchant that ran aground frees its contractor at once, without the cooldown, so they can try again
    // with the contract they got back. Believed only from the ship's owner, for the ship the server has on record.
    private void Release(long sender, ZDOID shipId, long contractor)
    {
        if (ZNet.instance == null || !ZNet.instance.IsServer() || ZDOMan.instance == null || contractor == 0L
            || !active.TryGetValue(contractor, out Active record) || record.Ship != shipId)
        {
            return;
        }

        ZDO zdo = ZDOMan.instance.GetZDO(shipId);
        if (zdo != null && zdo.GetOwner() != sender)
        {
            return;
        }

        active.Remove(contractor);
        cooldownUntil.Remove(contractor);
        Jotunn.Logger.LogInfo($"Escort: the merchant of {contractor} ran aground; they may take a new contract");
    }

    // On the client of a player who was near when the merchant came into port: adds the coins, rolls for a Raid Chart and
    // tells of both. The ship's owner sends it, so it is believed only from the owner of that ship's data, for a ship that is
    // near, once, and for no more cargo than the ship's data shows (give or take the last tick). Coins that do not fit in
    // the pack fall at the player's feet.
    private void RPC_Paid(long sender, ZDOID shipId, float cargo)
    {
        Player player = Player.m_localPlayer;
        ZDO zdo = ZDOMan.instance != null && !shipId.IsNone() ? ZDOMan.instance.GetZDO(shipId) : null;
        if (player == null || player.IsDead() || zdo == null || paid.Contains(shipId) || !TradeShipRegistry.IsTradeShip(zdo.GetPrefab())
            || zdo.GetOwner() != sender || float.IsNaN(cargo)
            || Vector3.Distance(player.transform.position, zdo.GetPosition()) > EscortSettings.RewardRange.Value + PayMargin)
        {
            return;
        }

        paid.Add(shipId);
        cargo = Mathf.Clamp(cargo, 0f, Mathf.Min(1f, zdo.GetFloat(TradeKeys.Cargo, 1f) + 0.05f));
        int coins = EscortRules.Pay(zdo.GetInt(TradeKeys.Pay, EscortSettings.Pay.Value), cargo);
        if (coins > 0)
        {
            Give(player, "Coins", coins);
            TradingPosts.TradingSkill.Raise(player, coins);
        }

        if (Random.value * 100f < EscortSettings.RaidChartChance.Value)
        {
            Give(player, NailShipRegistry.RaidChartName, 1);
        }

        player.Message(MessageHud.MessageType.Center, string.Format(Translations.Word("msg_whitehilt_escort_arrived"), coins));
    }

    // Puts an item in the player's pack, a stack at a time; what does not fit falls at the player's feet.
    private static void Give(Player player, string prefabName, int amount)
    {
        GameObject prefab = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(prefabName) : null;
        ItemDrop drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
        if (drop == null)
        {
            Jotunn.Logger.LogWarning($"Escort: the item {prefabName} was not found, so it was not given");
            return;
        }

        int maxStack = Mathf.Max(1, drop.m_itemData.m_shared.m_maxStackSize);
        for (int left = amount; left > 0; left -= maxStack)
        {
            int stack = Mathf.Min(left, maxStack);
            if (player.PickupPrefab(prefab, stack) != null)
            {
                continue;
            }

            ItemDrop.ItemData item = drop.m_itemData.Clone();
            item.m_dropPrefab = prefab;
            item.m_stack = stack;
            ItemDrop.DropItem(item, stack, player.transform.position + player.transform.forward + Vector3.up, Quaternion.identity);
        }
    }

    // On the server: checks a call for a trading ship and, if it is in order, tells the client where the port is. What the
    // client says is only a claim: the sender must be a real connection with a living player, standing near a ship that
    // exists and is no Nail Ship or trading ship, at open sea. A player who is not an admin must also be past the cooldown
    // and have no merchant on its way, and the world must have reached the key of the sea battles.
    private void Approve(long sender, ZDOID shipId, bool admin)
    {
        if (ZNet.instance == null || !ZNet.instance.IsServer() || ZDOMan.instance == null)
        {
            return;
        }

        if (RoutedRpcSender.Verify(sender, out _) == null)
        {
            Jotunn.Logger.LogWarning($"Escort: a request from {sender} could not be tied to a connection and was ignored");
            return;
        }

        ZDO player = SeaBattleService.PlayerZdo(sender);
        if (player == null)
        {
            return;
        }

        if (!EscortSettings.Enabled.Value || !SeaBattleSettings.Enabled.Value
            || (!admin && !SeaBattleService.HasKey(SeaBattleSettings.RequiredKey.Value)))
        {
            Refuse(sender, ReasonOff);
            return;
        }

        ZDO ship = SeaBattleService.TargetShip(shipId);
        if (ship == null || Vector3.Distance(player.GetPosition(), ship.GetPosition()) > SeaBattleService.AboardRange
            || !SeaBattleService.IsOcean(ship.GetPosition()))
        {
            Refuse(sender, ReasonSea);
            return;
        }

        long contractor = player.GetLong(ZDOVars.s_playerID);
        if (!admin && (IsBusy(contractor) || cooldownUntil.TryGetValue(contractor, out float until) && Time.time < until))
        {
            Refuse(sender, ReasonBusy);
            return;
        }

        Vector3 at = ship.GetPosition();
        if (!TradeShip.TryPickDestination(new Vector2(at.x, at.z), ship.GetRotation().eulerAngles.y, out Vector2 port))
        {
            Refuse(sender, ReasonRoute);
            return;
        }

        if (!admin)
        {
            cooldownUntil[contractor] = Time.time + EscortSettings.CooldownMinutes.Value * 60f;
        }

        active[contractor] = new Active { Since = Time.time };
        Jotunn.Logger.LogInfo($"Escort: a merchant is sent for {sender}'s ship, to {port.x:0} {port.y:0}{(admin ? " (admin)" : string.Empty)}");
        ZRoutedRpc.instance.InvokeRoutedRPC(sender, StartRpc, shipId, new Vector3(port.x, ZoneSystem.instance.m_waterLevel, port.y), !admin);
    }

    private static void Refuse(long sender, string reason)
    {
        ZRoutedRpc.instance.InvokeRoutedRPC(sender, RefusedRpc, reason);
    }

    // On the server: whether a player has a merchant on its way, from the server's memory. An answer that never got a ship,
    // or a ship whose data has not reached the server for long, stops counting.
    private bool IsBusy(long contractor)
    {
        if (!active.TryGetValue(contractor, out Active record))
        {
            return false;
        }

        if (record.Ship.IsNone())
        {
            return Forget(contractor, Time.time - record.Since >= PendingSeconds);
        }

        ZDO zdo = ZDOMan.instance.GetZDO(record.Ship);
        if (zdo == null)
        {
            return Forget(contractor, Time.time - record.Since >= UnseenSeconds);
        }

        return Forget(contractor, !IsActive(zdo));
    }

    // Drops a record when told to; the answer is whether the record is still there, that is, still counts.
    private bool Forget(long contractor, bool drop)
    {
        if (drop)
        {
            active.Remove(contractor);
        }

        return !drop;
    }

    private static bool IsSailing(ZDO zdo)
    {
        return TradeShipRegistry.IsTradeShip(zdo.GetPrefab()) && zdo.GetInt(TradeKeys.State) == (int)TradeState.Sailing;
    }

    // Sailing, or waiting at a trading post for the contractor: the escort has not ended.
    private static bool IsActive(ZDO zdo)
    {
        int state = zdo.GetInt(TradeKeys.State);
        return TradeShipRegistry.IsTradeShip(zdo.GetPrefab()) && (state == (int)TradeState.Sailing || state == (int)TradeState.Waiting);
    }

    // On the server: the data of every merchant known to be on its way. Records that have ended or were never realised
    // are dropped on the way.
    private IEnumerable<ZDO> MerchantsOnTheirWay()
    {
        List<ZDO> found = new();
        if (ZDOMan.instance == null)
        {
            return found;
        }

        foreach (long contractor in new List<long>(active.Keys))
        {
            Active record = active[contractor];
            if (!record.Ship.IsNone() && IsBusy(contractor))
            {
                ZDO zdo = ZDOMan.instance.GetZDO(record.Ship);
                if (zdo != null && IsSailing(zdo))
                {
                    found.Add(zdo);
                }
            }
        }

        return found;
    }

    // On the client that was sent a port: raises the trading ship beside its ship, uses up the contract, marks the port on
    // the map and tells the server which ship it is. Only the server may order this, and only after this client asked.
    private void RPC_Start(long sender, ZDOID shipId, Vector3 port, bool useContract)
    {
        if (sender != ZRoutedRpc.instance.GetServerPeerID() || Time.time - requestedAt > RequestTimeout)
        {
            return;
        }

        requestedAt = float.NegativeInfinity;
        Player player = Player.m_localPlayer;
        GameObject found = ZNetScene.instance != null ? ZNetScene.instance.FindInstance(shipId) : null;
        Ship ship = found != null ? found.GetComponent<Ship>() : null;
        ItemDrop.ItemData contract = useContract && player != null ? FindContract(player) : null;
        if (player == null || ship == null || RaiderShip.Is(ship) || TradeShip.Is(ship) || (useContract && contract == null))
        {
            // The ship or the contract is gone since the server was asked; the server gets its memory back.
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), RaisedRpc, ZDOID.None);
            return;
        }

        TradeShip trade = TradeShip.Raise(ship, new Vector2(port.x, port.z), player.GetPlayerID());
        if (trade == null)
        {
            // No deep water beside the ship: the contract is kept.
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), RaisedRpc, ZDOID.None);
            Say(player, ReasonRoute);
            return;
        }

        if (contract != null)
        {
            player.GetInventory().RemoveOneItem(contract);
        }

        ZDOID tradeId = trade.GetComponent<ZNetView>().GetZDO().m_uid;
        MarkPort(tradeId, port);
        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), RaisedRpc, tradeId);
        player.Message(MessageHud.MessageType.Center, Translations.Word("msg_whitehilt_escort_started"));
    }

    private static ItemDrop.ItemData FindContract(Player player)
    {
        foreach (ItemDrop.ItemData item in player.GetInventory().GetAllItems())
        {
            if (EscortContract.Is(item))
            {
                return item;
            }
        }

        return null;
    }

    // On the client: the server refused. Only the server may say so, and only the reasons it knows.
    private void RPC_Refused(long sender, string reason)
    {
        if (sender != ZRoutedRpc.instance.GetServerPeerID())
        {
            return;
        }

        requestedAt = float.NegativeInfinity;
        Player player = Player.m_localPlayer;
        if (player != null && (reason == ReasonOff || reason == ReasonSea || reason == ReasonRoute || reason == ReasonBusy
            || reason == ReasonPost || reason == ReasonNoPort))
        {
            Say(player, reason);
        }
    }

    // On the server: the client says which ship it raised, so the server can tell where merchants are and who has one on its
    // way; or that it raised none, which gives the contractor the cooldown back.
    private void RPC_Raised(long sender, ZDOID tradeId)
    {
        if (ZNet.instance == null || !ZNet.instance.IsServer() || RoutedRpcSender.Verify(sender, out _) == null)
        {
            return;
        }

        ZDO player = SeaBattleService.PlayerZdo(sender);
        if (player == null)
        {
            return;
        }

        long contractor = player.GetLong(ZDOVars.s_playerID);
        if (tradeId.IsNone())
        {
            active.Remove(contractor);
            cooldownUntil.Remove(contractor);
            return;
        }

        active[contractor] = new Active { Ship = tradeId, Since = Time.time };
    }

    // On the client: marks the port on the map, for this player only and not saved.
    private void MarkPort(ZDOID tradeId, Vector3 port)
    {
        if (Minimap.instance == null)
        {
            return;
        }

        Minimap.PinData pin = Minimap.instance.AddPin(port, Minimap.PinType.Icon3, Translations.Word("whitehilt_escort_pin"), save: false, isChecked: false);
        marks[tradeId] = new Mark { Pin = pin };
    }

    /// <summary>
    /// Takes a merchant's pin off the map of the local player.
    /// </summary>
    /// <param name="tradeId">The trading ship's id.</param>
    internal void DropPin(ZDOID tradeId)
    {
        if (marks.TryGetValue(tradeId, out Mark mark))
        {
            RemovePin(mark);
            marks.Remove(tradeId);
        }
    }

    // On the client: drops the pin of an escort whose data says it has ended, or is no longer known.
    private void CheckPins()
    {
        List<ZDOID> done = null;
        foreach (KeyValuePair<ZDOID, Mark> pair in marks)
        {
            ZDO zdo = ZDOMan.instance != null ? ZDOMan.instance.GetZDO(pair.Key) : null;
            if (zdo != null)
            {
                pair.Value.MissingSince = 0f;
            }
            else if (pair.Value.MissingSince == 0f)
            {
                pair.Value.MissingSince = Time.time;
            }

            int state = zdo != null ? zdo.GetInt(TradeKeys.State) : -1;
            bool ended = zdo != null ? state != (int)TradeState.Sailing && state != (int)TradeState.Waiting : Time.time - pair.Value.MissingSince > PinUnseenSeconds;
            if (ended)
            {
                (done ??= new List<ZDOID>()).Add(pair.Key);
            }
        }

        if (done != null)
        {
            foreach (ZDOID id in done)
            {
                DropPin(id);
            }
        }
    }

    private static void RemovePin(Mark mark)
    {
        if (mark.Pin != null && Minimap.instance != null)
        {
            Minimap.instance.RemovePin(mark.Pin);
        }
    }
}
