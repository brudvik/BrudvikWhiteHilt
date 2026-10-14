using BrudvikWhiteHilt.Pieces.Ships;
using BrudvikWhiteHilt.Saga;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BrudvikWhiteHilt.SeaBattles.Escorts;

/// <summary>
/// On the trading ship, on every client. Only the ship's owner thinks: four times a second it works out how far along its
/// route the ship is, steers it for its destination with the same help the route autopilot and the Nail Ships use, lets
/// the raiders on its deck empty the hold, and decides whether the escort goes on or has ended. When it ends the ship
/// stops, and a minute later it sails out of sight. Everything the escort needs to survive a change of owner (the
/// destination, where it began, the cargo, the waves, when a player was last near) is in the ship's ZDO, and the
/// others read the state from there for the hover text and the locks on the helm and the hold. The ship is registered in
/// <see cref="SailingShips"/> while it sails, so the game's physics does not stop it for want of a crew.
/// </summary>
public class TradeShip : MonoBehaviour
{
    private const float ThinkInterval = 0.25f;

    // The last time a player was near is written no more often than this.
    private const float SeenInterval = 5f;

    // A ship that has ended lies still this long (world seconds), then sails out of sight, and is removed when nobody is
    // near, or at the latest after the sail-off limit.
    private const float SailOffDelay = 60f;
    private const float SailOffLimit = 120f;

    // The raiders landing on the deck are told to the players near no more often than this, and a wave that has not been
    // raised is asked for again after this many seconds.
    private const float PlunderNoticeGap = 20f;
    private const float WaveRetrySeconds = 10f;

    // The trading ship is raised this far to the side of the ship that calls it, and farther if the water is too shallow
    // there; it needs this much water under it.
    private const float SideDistance = 25f;
    private const float SideDepth = 6f;

    // The port is looked for in the arc this many degrees either side of the heading, at this many bearings, and the line
    // to it is checked for depth every this many metres.
    private const float DestinationArc = 70f;
    private const int DestinationBearings = 9;
    private const float RouteStep = 50f;

    private static readonly Dictionary<Ship, TradeShip> byShip = new();

    private Ship ship;
    private ZNetView nview;
    private ShipAssist assist;
    private WearNTear hull;
    private float nextThink;
    private float lastThink;
    private float steerCourse;
    private bool steering;
    private int avoidSign = 1;
    private bool hullDestroyed;
    private bool finishing;
    private float[] waveShares;
    private int askedWave = -1;
    private float lastWaveAsk = float.NegativeInfinity;
    private float lastPlunderNotice = float.NegativeInfinity;
    private bool hadRaiders;

    /// <summary>The longship this component sails.</summary>
    public Ship Ship => ship;

    /// <summary>The trading ships that are loaded on this machine.</summary>
    public static IEnumerable<TradeShip> All => byShip.Values;

    /// <summary>How many trading ships are loaded on this machine.</summary>
    public static int Loaded => byShip.Count;

    /// <summary>Where the escort stands, from the ship's ZDO.</summary>
    public TradeState State => nview != null && nview.IsValid() ? (TradeState)nview.GetZDO().GetInt(TradeKeys.State) : TradeState.Sailing;

    /// <summary>The cargo left, 0 to 1, from the ship's ZDO.</summary>
    public float Cargo => nview != null && nview.IsValid() ? Mathf.Clamp01(nview.GetZDO().GetFloat(TradeKeys.Cargo, 1f)) : 1f;

    /// <summary>The port the ship sails for, from the ship's ZDO.</summary>
    public Vector3 Destination => nview != null && nview.IsValid() ? nview.GetZDO().GetVec3(TradeKeys.Destination, transform.position) : transform.position;

    /// <summary>The player id of the one who called the ship, from the ship's ZDO; 0 for none.</summary>
    public long Contractor => nview != null && nview.IsValid() ? nview.GetZDO().GetLong(TradeKeys.Contractor) : 0L;

    /// <summary>How many waves have been sent, from the ship's ZDO.</summary>
    public int WavesSent => nview != null && nview.IsValid() ? nview.GetZDO().GetInt(TradeKeys.WavesSent) : 0;

    /// <summary>How many waves the escort has in all, from the ship's ZDO.</summary>
    public int Waves => nview != null && nview.IsValid() ? nview.GetZDO().GetInt(TradeKeys.Waves) : 0;

    /// <summary>The ship's network id; none when its data is gone.</summary>
    public ZDOID Id => nview != null && nview.IsValid() ? nview.GetZDO().m_uid : ZDOID.None;

    /// <summary>Whether this machine owns the ship and so decides everything about it.</summary>
    public bool IsOwner => nview != null && nview.IsValid() && nview.IsOwner();

    /// <summary>
    /// Whether a ship is a trading ship.
    /// </summary>
    /// <param name="ship">The ship, or null.</param>
    /// <returns>True for a trading ship.</returns>
    public static bool Is(Ship ship)
    {
        return ship != null && byShip.ContainsKey(ship);
    }

    /// <summary>
    /// The component of a trading ship.
    /// </summary>
    /// <param name="ship">The ship, or null.</param>
    /// <returns>The component, or null for any other ship.</returns>
    public static TradeShip Of(Ship ship)
    {
        return ship != null && byShip.TryGetValue(ship, out TradeShip trade) ? trade : null;
    }

    /// <summary>
    /// Whether a trading ship that is on its way is loaded within reach of a point.
    /// </summary>
    /// <param name="position">The point, in world space.</param>
    /// <param name="range">How far, in metres.</param>
    /// <returns>True if such a ship is within range.</returns>
    public static bool AnyNear(Vector3 position, float range)
    {
        foreach (TradeShip trade in byShip.Values)
        {
            if (trade != null && trade.State == TradeState.Sailing && Vector3.Distance(trade.transform.position, position) <= range)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// A line per trading ship that is loaded here, for the console.
    /// </summary>
    /// <returns>The description.</returns>
    public static string Describe()
    {
        if (byShip.Count == 0)
        {
            return "Trading ships loaded: 0";
        }

        List<string> lines = new() { $"Trading ships loaded: {byShip.Count}" };
        foreach (TradeShip trade in byShip.Values)
        {
            Vector3 here = trade.transform.position;
            float away = Player.m_localPlayer != null ? Vector3.Distance(Player.m_localPlayer.transform.position, here) : 0f;
            float toPort = Utils.DistanceXZ(here, trade.Destination);
            lines.Add($"  {trade.State}, cargo {trade.Cargo * 100f:0} %, waves {trade.WavesSent}/{trade.Waves}, {toPort:0} m to port, " +
                $"{away:0} m away, owner here: {trade.nview != null && trade.nview.IsOwner()}");
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Finds a port for a trading ship that sails from a place: <see cref="EscortSettings.MinDistance"/> to
    /// <see cref="EscortSettings.MaxDistance"/> away, ahead of the heading in an arc either side of it, over a straight
    /// line that is deep enough all the way according to the world generator. That works for a place whose zone is not
    /// loaded and on the server, which approves the contracts.
    /// </summary>
    /// <param name="from">World x and z of the place.</param>
    /// <param name="heading">The ship's heading, in degrees from north.</param>
    /// <param name="destination">World x and z of the port.</param>
    /// <returns>False when no line of deep water was found.</returns>
    public static bool TryPickDestination(Vector2 from, float heading, out Vector2 destination)
    {
        List<float> bearings = EncounterRules.Offsets(DestinationArc, DestinationBearings, Random.value).ToList();
        return EscortRules.TryPickDestination(from, heading, EscortSettings.MinDistance.Value, EscortSettings.MaxDistance.Value, bearings,
            point => SeaBattleService.DeepWater(point.x, point.y, EscortSettings.MinDepth.Value), RouteStep, out destination);
    }

    /// <summary>
    /// Raises a trading ship beside a ship, to one side of it in deep water, facing its destination, and fills its data:
    /// where it began, where it goes, who called it, a full cargo and the shares of the route at which the waves come. The
    /// new ship belongs to this machine. The contract's server flow and the admin command both end here.
    /// </summary>
    /// <param name="near">The ship that calls it; the trading ship appears beside this one.</param>
    /// <param name="destination">World x and z of the port.</param>
    /// <param name="contractor">The player id of the one who called it.</param>
    /// <returns>The new ship, or null when there was no deep water beside the ship or no prefab.</returns>
    public static TradeShip Raise(Ship near, Vector2 destination, long contractor)
    {
        ZNetScene scene = ZNetScene.instance;
        GameObject prefab = scene != null ? scene.GetPrefab(TradeShipRegistry.ShipName) : null;
        if (prefab == null || near == null || ZoneSystem.instance == null)
        {
            Jotunn.Logger.LogWarning($"Trading Ship: prefab {TradeShipRegistry.ShipName} or the calling ship was not found");
            return null;
        }

        if (!TrySidePoint(near, out Vector2 spot))
        {
            Jotunn.Logger.LogInfo("Trading Ship: no deep water beside the ship");
            return null;
        }

        Vector3 point = new(spot.x, ZoneSystem.instance.m_waterLevel, spot.y);
        Vector2 toPort = destination - spot;
        Quaternion facing = toPort.sqrMagnitude > 1f ? Quaternion.Euler(0f, RaiderTactics.CourseTo(spot, destination, 0f), 0f) : near.transform.rotation;
        GameObject instance = Instantiate(prefab, point, facing);
        ZDO zdo = instance.GetComponent<ZNetView>().GetZDO();
        zdo.Set(TradeKeys.Start, point);
        zdo.Set(TradeKeys.Destination, new Vector3(destination.x, point.y, destination.y));
        zdo.Set(TradeKeys.Contractor, contractor);
        zdo.Set(TradeKeys.State, (int)TradeState.Sailing);
        zdo.Set(TradeKeys.Cargo, 1f);
        zdo.Set(TradeKeys.WavesSent, 0);
        zdo.Set(TradeKeys.Seen, ZNet.instance.GetTime().Ticks);

        int waves = Mathf.Clamp(EscortSettings.Waves.Value, 0, TradeKeys.MaxWaves);
        float[] shares = EscortRules.RollWaves(waves, EscortSettings.WaveStart.Value, EscortSettings.WaveEnd.Value, () => Random.value);
        zdo.Set(TradeKeys.Waves, shares.Length);
        for (int i = 0; i < shares.Length; i++)
        {
            zdo.Set(TradeKeys.WaveAt(i), shares[i]);
        }

        Jotunn.Logger.LogInfo($"A trading ship sails from {point} to {destination.x:0} {destination.y:0}, {shares.Length} wave(s)");
        return instance.GetComponent<TradeShip>();
    }

    /// <summary>
    /// Notes that a wave has been sent against the ship: the next one is the one after. Only on the ship's owner, called when
    /// the Nail Ships of the wave have been raised, so a wave that could not be raised is asked for again.
    /// </summary>
    public void WaveSpawned()
    {
        if (IsOwner)
        {
            nview.GetZDO().Set(TradeKeys.WavesSent, WavesSent + 1);
        }
    }

    /// <summary>
    /// The text shown over the hull: its name, the cargo left and the way to port.
    /// </summary>
    /// <returns>The localized text.</returns>
    public string HoverText()
    {
        string text = Localization.instance.Localize("$whitehilt_tradeship");
        if (State != TradeState.Sailing || nview == null || !nview.IsValid())
        {
            return text;
        }

        string line = Localization.instance.Localize("$whitehilt_trade_hover");
        float toPort = Utils.DistanceXZ(transform.position, Destination);
        return text + "\n" + string.Format(line, Mathf.RoundToInt(Cargo * 100f), Mathf.RoundToInt(toPort));
    }

    private void Awake()
    {
        ship = GetComponent<Ship>();
        nview = GetComponent<ZNetView>();
        if (ship == null || nview == null || nview.GetZDO() == null)
        {
            enabled = false;
            return;
        }

        byShip[ship] = this;
        hull = GetComponent<WearNTear>();
        nextThink = Time.time + Random.Range(0f, ThinkInterval);
        if (hull != null)
        {
            hull.m_onDestroyed = (Action)Delegate.Combine(hull.m_onDestroyed, new Action(OnHullDestroyed));
        }
    }

    private void OnEnable()
    {
        RefreshSailing();
    }

    private void OnDisable()
    {
        if (ship != null)
        {
            SailingShips.Remove(ship.m_players);
        }
    }

    private void OnDestroy()
    {
        if (ship != null)
        {
            byShip.Remove(ship);
            SailingShips.Remove(ship.m_players);
        }
    }

    private void Update()
    {
        if (Time.time < nextThink)
        {
            return;
        }

        nextThink = Time.time + ThinkInterval;
        Think();
    }

    // The owner holds the course between its decisions; the rudder is turned every physics step, as the autopilot does.
    private void FixedUpdate()
    {
        if (steering && assist != null && nview.IsValid() && nview.IsOwner())
        {
            assist.SteerTowards(steerCourse);
        }
    }

    // Keeps the set of sailing ships right on every machine, so a new owner already has its ship in it. The rest is the
    // owner's.
    private void Think()
    {
        if (!nview.IsValid())
        {
            return;
        }

        RefreshSailing();
        if (!nview.IsOwner())
        {
            steering = false;
            lastThink = 0f;
            return;
        }

        assist ??= GetComponent<ShipAssist>();
        float now = Time.time;
        float dt = lastThink > 0f ? Mathf.Min(now - lastThink, 1f) : ThinkInterval;
        lastThink = now;
        if (assist == null)
        {
            return;
        }

        ZDO zdo = nview.GetZDO();
        if (State == TradeState.Sailing)
        {
            Sail(zdo, dt);
        }
        else
        {
            SailOff(zdo);
        }
    }

    // The ship is in the set the physics reads while it sails, and again when it sails out of sight; lying in port it is
    // not, so the game's drag holds it still.
    private void RefreshSailing()
    {
        if (ship == null)
        {
            return;
        }

        if (WantsToSail())
        {
            SailingShips.Add(ship.m_players);
        }
        else
        {
            SailingShips.Remove(ship.m_players);
        }
    }

    private bool WantsToSail()
    {
        if (State == TradeState.Sailing)
        {
            return true;
        }

        return EndedSeconds(nview.GetZDO()) >= SailOffDelay;
    }

    // One tick of an escort that goes on: steer for the port, let the raiders on the deck empty the hold, note that a
    // player is near, and see whether it has ended.
    private void Sail(ZDO zdo, float dt)
    {
        Vector3 destination = zdo.GetVec3(TradeKeys.Destination, transform.position);
        Vector2 here = Flat(transform.position);
        Vector2 port = Flat(destination);

        float course = RaiderTactics.CourseTo(here, port, ShipAssist.Heading(transform));
        steerCourse = ChooseCourse(course, out bool clear);
        steering = true;
        ship.m_speed = clear ? Ship.Speed.Full : Ship.Speed.Stop;

        float cargo = zdo.GetFloat(TradeKeys.Cargo, 1f);
        int raiders = RaiderCrew.AliveAboard(ship);
        float left = EscortRules.Plunder(cargo, raiders, EscortSettings.PlunderPerRaider.Value, dt);
        if (left < cargo - 1e-4f)
        {
            zdo.Set(TradeKeys.Cargo, left);
        }

        WarnOfPlunder(raiders);
        AskForWave(zdo, EscortRules.Progress(Flat(zdo.GetVec3(TradeKeys.Start, transform.position)), port, here));

        float alone = SecondsAlone(zdo);
        EscortOutcome outcome = EscortRules.Outcome(Vector2.Distance(here, port), EscortSettings.ArriveRadius.Value, HullGone(zdo), left,
            alone, EscortSettings.AbandonSeconds.Value);
        if (outcome != EscortOutcome.Sailing)
        {
            Finish(zdo, outcome, left);
        }
    }

    // How long nobody has been within the abandon range, in seconds. A player aboard or near renews the time, written
    // now and then and not every tick; a ship that no one has seen yet counts from now.
    private float SecondsAlone(ZDO zdo)
    {
        long now = ZNet.instance.GetTime().Ticks;
        long seen = zdo.GetLong(TradeKeys.Seen);
        if (ship.m_players.Count > 0 || PlayerWithin(EscortSettings.AbandonRange.Value))
        {
            if (seen == 0L || now - seen >= SeenInterval * TimeSpan.TicksPerSecond)
            {
                zdo.Set(TradeKeys.Seen, now);
            }

            return 0f;
        }

        if (seen == 0L)
        {
            zdo.Set(TradeKeys.Seen, now);
            return 0f;
        }

        return (float)((now - seen) / (double)TimeSpan.TicksPerSecond);
    }

    private bool HullGone(ZDO zdo)
    {
        return hullDestroyed || (hull != null && zdo.GetFloat(ZDOVars.s_health, hull.m_health) <= 0f);
    }

    // Writes the end of the escort: the state and the time, and stops the ship. Done once, by the owner; the ship then
    // lies still until it sails out of sight.
    private void Finish(ZDO zdo, EscortOutcome outcome, float cargo)
    {
        if (finishing || State != TradeState.Sailing)
        {
            return;
        }

        finishing = true;
        zdo.Set(TradeKeys.State, (int)(outcome == EscortOutcome.Arrived ? TradeState.Arrived : TradeState.Lost));
        zdo.Set(TradeKeys.EndedAt, ZNet.instance.GetTime().Ticks);
        ship.m_speed = Ship.Speed.Stop;
        steering = false;
        RefreshSailing();
        Jotunn.Logger.LogInfo($"Trading Ship: the escort ended at {transform.position}: {outcome}, cargo {cargo * 100f:0} %");
        Announce(outcome, cargo);
    }

    // Tells the world how it ended. A ship in port pays every player within reach, by the cargo that is left, and the deed
    // goes into the sagas of those who saw it. A failure is told to the players near the ship, and to the contractor
    // wherever they are, who also loses the pin on the map.
    private void Announce(EscortOutcome outcome, float cargo)
    {
        try
        {
            if (outcome == EscortOutcome.Arrived)
            {
                EscortService.Pay(this, cargo);
                SagaLog.Announce(SagaKind.Escort, "$whitehilt_tradeship", transform.position);
                EscortService.Notice(EscortNotice.Arrived, this, 0f);
                return;
            }

            EscortNotice notice = outcome switch
            {
                EscortOutcome.Sunk => EscortNotice.Sunk,
                EscortOutcome.Plundered => EscortNotice.Plundered,
                _ => EscortNotice.Abandoned
            };
            EscortService.Notice(notice, this, EscortSettings.AbandonRange.Value * 2f);
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"Trading Ship: the end of the escort was not announced: {ex}");
        }
    }

    // The first raider to land on the deck is told to the players near, and not again for 20 seconds.
    private void WarnOfPlunder(int raiders)
    {
        if (raiders > 0 && !hadRaiders && Time.time - lastPlunderNotice >= PlunderNoticeGap)
        {
            lastPlunderNotice = Time.time;
            EscortService.Notice(EscortNotice.Plunder, this, EscortSettings.RewardRange.Value);
        }

        hadRaiders = raiders > 0;
    }

    // Asks the server for the next wave when the route has come far enough: once for each wave, and again after ten seconds
    // if the wave has not been raised by then. The server checks the request and orders the Nail Ships raised here.
    private void AskForWave(ZDO zdo, float progress)
    {
        waveShares ??= ReadWaveShares(zdo);
        int sent = zdo.GetInt(TradeKeys.WavesSent);
        if (!EscortRules.WaveDue(progress, waveShares, sent) || (sent == askedWave && Time.time - lastWaveAsk < WaveRetrySeconds))
        {
            return;
        }

        askedWave = sent;
        lastWaveAsk = Time.time;
        EscortService.RequestWave(this, sent);
    }

    private static float[] ReadWaveShares(ZDO zdo)
    {
        int count = Mathf.Clamp(zdo.GetInt(TradeKeys.Waves), 0, TradeKeys.MaxWaves);
        float[] shares = new float[count];
        for (int i = 0; i < count; i++)
        {
            shares[i] = zdo.GetFloat(TradeKeys.WaveAt(i), 2f);
        }

        return shares;
    }

    // A ship whose hull is destroyed sinks and is gone with its data at once; the owner notes the end before that.
    private void OnHullDestroyed()
    {
        if (nview == null || !nview.IsValid() || !nview.IsOwner())
        {
            return;
        }

        hullDestroyed = true;
        Finish(nview.GetZDO(), EscortOutcome.Sunk, Cargo);
    }

    // After the end: lies still for a minute, so the players can see it in port, then sails away from the nearest player at
    // full speed and is removed when nobody is near any more, or after the limit. With no player near at all it goes at
    // once. A player standing on the deck keeps it where it is.
    private void SailOff(ZDO zdo)
    {
        double ended = EndedSeconds(zdo);
        bool anyone = PlayerWithin(EscortSettings.AbandonRange.Value);
        if (ship.m_players.Count > 0)
        {
            ship.m_speed = Ship.Speed.Stop;
            steering = false;
            return;
        }

        if (!anyone || ended >= SailOffDelay + SailOffLimit)
        {
            Despawn();
            return;
        }

        if (ended < SailOffDelay)
        {
            ship.m_speed = Ship.Speed.Stop;
            steering = false;
            return;
        }

        Player nearest = NearestPlayer();
        float away = nearest != null
            ? RaiderTactics.CourseTo(Flat(nearest.transform.position), Flat(transform.position), ShipAssist.Heading(transform))
            : ShipAssist.Heading(transform);
        steerCourse = ChooseCourse(away, out bool clear);
        steering = true;
        ship.m_speed = clear ? Ship.Speed.Full : Ship.Speed.Stop;
        if (!PlayerWithin(SeaBattleSettings.DespawnRange.Value))
        {
            Despawn();
        }
    }

    // World seconds since the escort ended; 0 while it goes on.
    private static double EndedSeconds(ZDO zdo)
    {
        long ended = zdo.GetLong(TradeKeys.EndedAt);
        return ended == 0L ? 0d : (ZNet.instance.GetTime() - new DateTime(ended)).TotalSeconds;
    }

    private void Despawn()
    {
        Jotunn.Logger.LogInfo($"A trading ship at {transform.position} sails out of sight");
        ZNetScene.instance.Destroy(gameObject);
    }

    private bool PlayerWithin(float range)
    {
        foreach (Player player in Player.GetAllPlayers())
        {
            if (player != null && Vector3.Distance(player.transform.position, transform.position) <= range)
            {
                return true;
            }
        }

        return false;
    }

    private Player NearestPlayer()
    {
        Player nearest = null;
        float best = float.MaxValue;
        foreach (Player player in Player.GetAllPlayers())
        {
            float distance = player != null ? Vector3.Distance(player.transform.position, transform.position) : float.MaxValue;
            if (distance < best)
            {
                best = distance;
                nearest = player;
            }
        }

        return nearest;
    }

    // The course asked for if the sea ahead is clear, else the nearest clear one 30, 60 or 90 degrees off it, as the Nail
    // Ships and the route autopilot do; the side taken last is tried first so it does not swing back and forth.
    private float ChooseCourse(float course, out bool clear)
    {
        clear = true;
        float look = assist.Lookahead();
        if (!assist.ShallowAhead() && !assist.Blocked(course, look))
        {
            return course;
        }

        for (float angle = 30f; angle <= 90f; angle += 30f)
        {
            foreach (int sign in new[] { avoidSign, -avoidSign })
            {
                if (!assist.Blocked(course + sign * angle, look))
                {
                    avoidSign = sign;
                    return course + sign * angle;
                }
            }
        }

        clear = false;
        return course;
    }

    // A place to one side of a ship, 25 m out and then farther, where the sea is deep enough; the right side is tried first.
    private static bool TrySidePoint(Ship near, out Vector2 spot)
    {
        Vector2 centre = Flat(near.transform.position);
        float heading = ShipAssist.Heading(near.transform);
        foreach (float distance in new[] { SideDistance, SideDistance * 1.6f, SideDistance * 2.4f })
        {
            foreach (float side in new[] { 90f, -90f })
            {
                float angle = (heading + side) * Mathf.Deg2Rad;
                spot = centre + new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * distance;
                if (SeaBattleService.DeepWater(spot.x, spot.y, SideDepth))
                {
                    return true;
                }
            }
        }

        spot = centre;
        return false;
    }

    private static Vector2 Flat(Vector3 position)
    {
        return new Vector2(position.x, position.z);
    }
}
