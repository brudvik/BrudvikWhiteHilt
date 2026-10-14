using BrudvikWhiteHilt.Difficulty;
using BrudvikWhiteHilt.Kraken;
using BrudvikWhiteHilt.Monsters;
using BrudvikWhiteHilt.Pieces.Ships;
using BrudvikWhiteHilt.Saga;
using BrudvikWhiteHilt.SeaBattles.Fleets;
using BrudvikWhiteHilt.SeaBattles.Weapons;
using BrudvikWhiteHilt.Treasure;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BrudvikWhiteHilt.SeaBattles;

/// <summary>
/// On the Nail Ship, on every client. Only the ship's owner thinks: four times a second it counts the crew, lets
/// <see cref="RaiderTactics"/> decide the state, course and sail, and steers with the same help the route autopilot
/// uses. The owner also spawns the crew and fills the hold when the ship is new, hands out the warriors' turns to leap,
/// makes the ship a prize when the crew is dead, and lets it sink or sail out of sight. Everyone reads the state from
/// the ZDO, so the hover text, the locks on the helm and the hold, and the messages are the same for all. The same
/// component sails the flagship of a fleet and its escorts: the owner of each reads the fleet's facts (the flagship's
/// state, whether its jarl has fallen) from the flagship's data, so a ship never writes to another ship.
/// </summary>
public class RaiderShip : MonoBehaviour
{
    private const float ThinkInterval = 0.25f;

    // The crew is spawned this long after the ship appears, once its owner is settled.
    private const float SpawnDelay = 1f;

    // A new owner has not met the crew yet; until it has, or this long has passed, it trusts the last count in the ZDO.
    private const float LoadGraceSeconds = 10f;

    // The last time a player was near is written no more often than this.
    private const float SeenInterval = 5f;

    private const float MessageRange = 100f;
    private const string MessageRpc = "WhiteHiltRaiderMessage";

    // The Nail Ship appears in the arc this many degrees either side of the target's heading, tried at this many bearings,
    // where the sea is at least this deep.
    private const float SpawnArc = 45f;
    private const int SpawnAttempts = 12;
    private const float SpawnDepth = 6f;

    // A moored ship keeps its place and heading and only bobs, as a ship at a drift anchor does.
    private const RigidbodyConstraints MooredConstraints =
        RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationY;

    private static readonly Dictionary<Ship, RaiderShip> byShip = new();
    private static readonly List<float> slotErrors = new();

    private Ship ship;
    private ZNetView nview;
    private ShipAssist assist;
    private WearNTear hull;
    private Container hold;
    private Rigidbody body;
    private float awakeTime;
    private float nextThink;
    private float stateSince;
    private RaiderState knownState;
    private float steerCourse;
    private bool steering;
    private bool crewSpawnTried;
    private bool sawCrew;
    private bool sinking;
    private float missingSince;
    private Vector3 lastTargetPosition;
    private float lastTargetTime;
    private Vector2 targetVelocity;
    private float nextLeap;
    private int lastLeap = -1;
    private int avoidSign = 1;
    private RigidbodyConstraints freeConstraints;
    private bool moorHeld;
    private bool isFlagship;
    private bool formationBroken;
    private float flagshipMissingSince;
    private bool jarlAlive;
    private bool sawJarl;

    /// <summary>The longship this component sails.</summary>
    public Ship Ship => ship;

    /// <summary>Whether this is the flagship of a fleet.</summary>
    public bool IsFlagship => isFlagship;

    /// <summary>The translation token of the ship's name: the flagship has its own.</summary>
    public string NameToken => isFlagship ? "$whitehilt_nailflagship" : "$whitehilt_nailship";

    /// <summary>The flagship of the fleet this ship belongs to, from its data; none for a lone ship.</summary>
    public ZDOID FleetId => nview != null && nview.IsValid() ? nview.GetZDO().GetZDOID(RaiderKeys.Fleet) : ZDOID.None;

    /// <summary>The ship's place in its fleet: 0 the flagship, 1 and 2 the escorts.</summary>
    public int Rank => nview != null && nview.IsValid() ? nview.GetZDO().GetInt(RaiderKeys.Rank) : 0;

    /// <summary>The Nail Ships that are loaded on this machine.</summary>
    public static IEnumerable<RaiderShip> All => byShip.Values;

    /// <summary>How many Nail Ships are loaded on this machine.</summary>
    public static int Loaded => byShip.Count;

    /// <summary>
    /// How many loaded Nail Ships are not moored at a draugr harbour. The encounter service sends one at a time, and a ship
    /// that lies at its harbour does not count.
    /// </summary>
    public static int Hunting => byShip.Values.Count(raider => raider.State != RaiderState.Moored);

    /// <summary>The ship's rigidbody.</summary>
    public Rigidbody Body => body;

    /// <summary>What the Nail Ship is doing now, from its ZDO.</summary>
    public RaiderState State => nview != null && nview.IsValid() ? (RaiderState)nview.GetZDO().GetInt(RaiderKeys.State) : RaiderState.Approach;

    /// <summary>The index of the melee crew member whose turn it is to leap, or -1.</summary>
    public int LeapTurn => nview != null && nview.IsValid() ? nview.GetZDO().GetInt(RaiderKeys.Leap, -1) : -1;

    /// <summary>The ship it hunts, when that ship is loaded here.</summary>
    public Ship TargetShip
    {
        get
        {
            if (nview == null || !nview.IsValid() || ZNetScene.instance == null)
            {
                return null;
            }

            ZDOID id = nview.GetZDO().GetZDOID(RaiderKeys.Target);
            GameObject found = id.IsNone() ? null : ZNetScene.instance.FindInstance(id);
            return found != null ? found.GetComponent<Ship>() : null;
        }
    }

    /// <summary>
    /// The hunted ship's velocity over the water, as its owner has measured it from the ship's movement. Zero on a machine
    /// that does not own the Nail Ship, which does not measure it.
    /// </summary>
    public Vector3 TargetVelocity => new(targetVelocity.x, 0f, targetVelocity.y);

    /// <summary>
    /// Whether a ship is a Nail Ship.
    /// </summary>
    /// <param name="ship">The ship, or null.</param>
    /// <returns>True for a Nail Ship.</returns>
    public static bool Is(Ship ship)
    {
        return ship != null && byShip.ContainsKey(ship);
    }

    /// <summary>
    /// The component of a Nail Ship.
    /// </summary>
    /// <param name="ship">The ship, or null.</param>
    /// <returns>The component, or null for any other ship.</returns>
    public static RaiderShip Of(Ship ship)
    {
        return ship != null && byShip.TryGetValue(ship, out RaiderShip raider) ? raider : null;
    }

    /// <summary>
    /// Whether a Nail Ship that is still a danger, one that is not a prize, is loaded within reach of a point. A ship
    /// near one is in a sea battle.
    /// </summary>
    /// <param name="position">The point, in world space.</param>
    /// <param name="range">How far, in metres.</param>
    /// <returns>True if such a ship is within range.</returns>
    public static bool AnyNear(Vector3 position, float range)
    {
        foreach (RaiderShip raider in byShip.Values)
        {
            if (raider != null && raider.State != RaiderState.Prize && Vector3.Distance(raider.transform.position, position) <= range)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Whether the ship's AI is sailing it now, with nobody aboard.
    /// </summary>
    /// <param name="ship">The ship.</param>
    /// <returns>True while it sails on its own.</returns>
    public static bool Sails(Ship ship)
    {
        return ship != null && SailingShips.Contains(ship.m_players);
    }

    /// <summary>
    /// How many players the ship's physics counts aboard. The game stops a ship and brakes it when it counts none;
    /// a Nail Ship under sail counts one, so it keeps its speed. A transpiler in <c>RaiderShipPatches</c> puts this in
    /// place of the game's count; it runs a few times per ship per physics step, so it only looks up a reference.
    /// </summary>
    /// <param name="players">The ship's list of players aboard.</param>
    /// <returns>The number of players, or 1 for an empty list of a Nail Ship that sails.</returns>
    public static int CrewCount(List<Player> players)
    {
        return SailingShips.Count(players, players.Count);
    }

    /// <summary>
    /// The sail force of a Nail Ship with the wind straight behind it: the game's own wind strength, but always along
    /// the ship's heading, smoothed as the game smooths it.
    /// </summary>
    /// <param name="ship">The ship.</param>
    /// <param name="sailSize">How much sail is up, 0 to 1.</param>
    /// <returns>The force, to be multiplied by the ship's mass.</returns>
    public static Vector3 TailwindForce(Ship ship, float sailSize)
    {
        float wind = Mathf.Lerp(0.25f, 1f, EnvMan.instance.GetWindIntensity());
        Vector3 target = ship.transform.forward * (wind * ship.m_sailForceFactor * sailSize);
        ship.m_sailForce = Vector3.SmoothDamp(ship.m_sailForce, target, ref ship.m_windChangeVelocity, 1f, 99f);
        return ship.m_sailForce;
    }

    /// <summary>
    /// Whether the dead still hold the ship: its helm and hold stay shut until the crew is dead.
    /// </summary>
    /// <param name="ship">The ship.</param>
    /// <returns>True for a Nail Ship that is not yet a prize.</returns>
    public static bool CrewHolds(Ship ship)
    {
        RaiderShip raider = Of(ship);
        return raider != null && raider.State != RaiderState.Prize;
    }

    /// <summary>
    /// A line per Nail Ship that is loaded here, for the console.
    /// </summary>
    /// <returns>The description.</returns>
    public static string Describe()
    {
        if (byShip.Count == 0)
        {
            return "Nail Ships loaded: 0";
        }

        List<string> lines = new() { $"Nail Ships loaded: {byShip.Count}" };
        foreach (RaiderShip raider in byShip.Values)
        {
            int crew = 0;
            ZDOID id = raider.nview != null && raider.nview.IsValid() ? raider.nview.GetZDO().m_uid : ZDOID.None;
            foreach (RaiderCrew member in RaiderCrew.All)
            {
                if (member.ShipId == id && member.Alive)
                {
                    crew++;
                }
            }

            float distance = Player.m_localPlayer != null ? Vector3.Distance(Player.m_localPlayer.transform.position, raider.transform.position) : 0f;
            string fleet = raider.FleetId.IsNone() ? "alone" : $"fleet {raider.FleetId.ID}, {(raider.isFlagship ? "flagship" : "escort " + raider.Rank)}";
            lines.Add($"  {raider.State}, {fleet}, crew {crew}/{raider.CrewSize()}, {distance:0} m away, owner here: {raider.nview != null && raider.nview.IsOwner()}");
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Raises a Nail Ship at <see cref="SeaBattleSettings.SpawnDistance"/> in the quarter ahead of a ship, in deep water,
    /// facing it. The ship belongs to this machine, which spawns its crew a moment later. The encounter service calls
    /// this on the server's order.
    /// </summary>
    /// <param name="target">The ship to hunt.</param>
    /// <param name="crewBonus">Extra melee crew for the players aboard the target.</param>
    /// <param name="tier">The crew tier, or null for the one the world has reached.</param>
    /// <returns>The new ship, or null when there was no deep water or no prefab.</returns>
    public static RaiderShip Spawn(Ship target, int crewBonus, CrewTier tier = null)
    {
        return SpawnFleet(target, crewBonus, tier, 1);
    }

    /// <summary>
    /// Raises a Nail Ship, or a fleet of two or three: the flagship at the spawn point as <see cref="Spawn"/> places a
    /// lone ship, and the escorts at their places in the wedge behind it, in deep water (an escort with no deep water at
    /// its place is left out). Every ship belongs to this machine, so the fleet is whole and placed in one go, and each
    /// carries the flagship's id and its own rank in its data.
    /// </summary>
    /// <param name="target">The ship to hunt.</param>
    /// <param name="crewBonus">Extra melee crew for the players aboard the target.</param>
    /// <param name="tier">The crew tier, or null for the one the world has reached.</param>
    /// <param name="size">The number of ships the server chose, 1 to 3; 1 is a plain Nail Ship.</param>
    /// <returns>The flagship (the lone ship when the size is 1), or null when there was no deep water or no prefab.</returns>
    public static RaiderShip SpawnFleet(Ship target, int crewBonus, CrewTier tier, int size)
    {
        size = Mathf.Clamp(size, 1, FleetRules.MaxSize);
        ZNetScene scene = ZNetScene.instance;
        GameObject flagshipPrefab = size > 1 && scene != null ? scene.GetPrefab(NailShipRegistry.FlagshipName) : null;
        if (size > 1 && flagshipPrefab == null)
        {
            Jotunn.Logger.LogWarning($"Nail Ship: prefab {NailShipRegistry.FlagshipName} was not found; a lone Nail Ship comes instead of the fleet");
            size = 1;
        }

        GameObject prefab = size > 1 ? flagshipPrefab : scene != null ? scene.GetPrefab(NailShipRegistry.ShipName) : null;
        if (prefab == null || target == null || target.m_nview == null || !target.m_nview.IsValid())
        {
            Jotunn.Logger.LogWarning($"Nail Ship: prefab {NailShipRegistry.ShipName} or the target ship was not found");
            return null;
        }

        IReadOnlyList<CrewTier> tiers = CrewTable.Parse(SeaBattleSettings.CrewTiers.Value);
        tier ??= CrewTable.Pick(tiers, key => ZoneSystem.instance.GetGlobalKey(key));
        if (tier == null)
        {
            Jotunn.Logger.LogWarning("Nail Ship: no crew tier has its key set, and the crew list has no valid tier to fall back on");
            return null;
        }

        Vector3 centre = target.transform.position;
        // The depth comes from the world generator, not from a ray at the seabed, as the zone out there may not be loaded.
        if (!EncounterRules.TryPickSpawn(new Vector2(centre.x, centre.z), ShipAssist.Heading(target.transform), SeaBattleSettings.SpawnDistance.Value,
            EncounterRules.Offsets(SpawnArc, SpawnAttempts, Random.value), spot => SeaBattleService.DeepWater(spot.x, spot.y, SpawnDepth), out Vector2 spawn))
        {
            Jotunn.Logger.LogInfo("Nail Ship: no deep water at the spawn distance");
            return null;
        }

        Vector3 point = new(spawn.x, ZoneSystem.instance.m_waterLevel, spawn.y);
        Vector3 toTarget = centre - point;
        toTarget.y = 0f;
        Quaternion facing = Quaternion.LookRotation(toTarget);
        ZDOID targetId = target.m_nview.GetZDO().m_uid;
        RaiderShip lead = Raise(prefab, point, facing, targetId, tier, crewBonus);
        ZDO leadZdo = lead.nview.GetZDO();
        if (size == 1)
        {
            Jotunn.Logger.LogInfo($"A Nail Ship sails up at {point} to the ship at {centre}");
            return lead;
        }

        leadZdo.Set(RaiderKeys.Fleet, leadZdo.m_uid);
        leadZdo.Set(RaiderKeys.Rank, 0);
        float heading = RaiderTactics.CourseTo(new Vector2(point.x, point.z), new Vector2(centre.x, centre.z), 0f);
        GameObject escortPrefab = scene.GetPrefab(NailShipRegistry.ShipName);
        int raised = 1;
        for (int rank = 1; rank < size && escortPrefab != null; rank++)
        {
            Vector2 slot = FleetRules.SlotPoint(spawn, heading, rank, FleetSettings.FormationSpacing.Value);
            if (!SeaBattleService.DeepWater(slot.x, slot.y, SpawnDepth))
            {
                Jotunn.Logger.LogInfo($"Nail Ship: no deep water for escort {rank} of the fleet, left out");
                continue;
            }

            RaiderShip escort = Raise(escortPrefab, new Vector3(slot.x, point.y, slot.y), facing, targetId, tier, crewBonus);
            ZDO escortZdo = escort.nview.GetZDO();
            escortZdo.Set(RaiderKeys.Fleet, leadZdo.m_uid);
            escortZdo.Set(RaiderKeys.Rank, rank);
            raised++;
        }

        Jotunn.Logger.LogInfo($"A fleet of {raised} Nail Ships sails up at {point} to the ship at {centre}");
        return lead;
    }

    // One ship of the kind the prefab is, with the data every Nail Ship starts with; the caller adds the fleet keys. Its
    // side is random, and an escort's is replaced by the fleet's rules when it fights.
    private static RaiderShip Raise(GameObject prefab, Vector3 point, Quaternion rotation, ZDOID targetId, CrewTier tier, int crewBonus)
    {
        GameObject instance = Instantiate(prefab, point, rotation);
        ZDO zdo = instance.GetComponent<ZNetView>().GetZDO();
        zdo.Set(RaiderKeys.Target, targetId);
        zdo.Set(RaiderKeys.Tier, tier.Index);
        zdo.Set(RaiderKeys.Side, Random.value < 0.5f ? -1 : 1);
        zdo.Set(RaiderKeys.Bonus, Mathf.Max(0, crewBonus));
        zdo.Set(RaiderKeys.State, (int)RaiderState.Approach);
        return instance.GetComponent<RaiderShip>();
    }

    /// <summary>
    /// Stops the drift and the turn of a moored Nail Ship after the game's physics step, leaving the bobbing. Only on the
    /// ship's owner, on top of the rigidbody constraints; called from a patch on the ship's physics.
    /// </summary>
    public void HoldMoored()
    {
        if (!moorHeld || body == null || nview == null || !nview.IsValid() || !nview.IsOwner())
        {
            return;
        }

        body.linearVelocity = new Vector3(0f, body.linearVelocity.y, 0f);
        Vector3 spin = body.angularVelocity;
        body.angularVelocity = new Vector3(spin.x, 0f, spin.z);
    }

    /// <summary>
    /// The text shown over the hull: its name, and while it is a prize the minutes left before it sinks.
    /// </summary>
    /// <returns>The localized text.</returns>
    public string HoverText()
    {
        string text = Localization.instance.Localize(NameToken);
        if (State == RaiderState.Prize && nview.IsValid())
        {
            double left = SeaBattleSettings.PrizeMinutes.Value - PrizeAgeMinutes(nview.GetZDO());
            text += "\n" + string.Format(Localization.instance.Localize("$whitehilt_raider_prize_hover"), Mathf.Max(0, Mathf.CeilToInt((float)left)));
        }

        return text;
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
        isFlagship = NailShipRegistry.IsFlagship(gameObject);
        body = GetComponent<Rigidbody>();
        freeConstraints = body != null ? body.constraints : RigidbodyConstraints.None;
        hull = GetComponent<WearNTear>();
        hold = GetComponentInChildren<Container>(true);
        awakeTime = Time.time;
        nextThink = Time.time + Random.Range(0f, ThinkInterval);
        stateSince = Time.time;
        nview.Register<string>(MessageRpc, RPC_Message);
        if (hull != null)
        {
            hull.m_onDestroyed = (Action)Delegate.Combine(hull.m_onDestroyed, new Action(OnHullDestroyed));
        }
    }

    private void OnEnable()
    {
        RefreshSailing();
        RefreshMooring();
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

    // Keeps the set of sailing ships right on every machine, so a new owner already has its ship in it. The rest is
    // the owner's: spawning the crew, counting it, the prize, the despawn, and the AI.
    private void Think()
    {
        if (!nview.IsValid())
        {
            return;
        }

        RefreshSailing();
        RefreshMooring();
        ZDO zdo = nview.GetZDO();
        RaiderState state = State;
        if (state != knownState)
        {
            knownState = state;
            stateSince = Time.time;
        }

        if (!nview.IsOwner())
        {
            steering = false;
            return;
        }

        assist ??= GetComponent<ShipAssist>();
        if (CrewSize() == 0)
        {
            if (!crewSpawnTried && Time.time - awakeTime >= SpawnDelay)
            {
                SpawnCrew(zdo);
            }

            return;
        }

        CountCrew(zdo, out int alive, out int melee);
        if (state != RaiderState.Prize)
        {
            WatchJarl(zdo);
        }

        if (state == RaiderState.Prize)
        {
            steering = false;
            SinkWhenDue(zdo);
            return;
        }

        if (alive == 0)
        {
            BecomePrize(zdo);
            return;
        }

        if (state != RaiderState.Moored && ShouldDespawn(zdo))
        {
            Despawn();
            return;
        }

        if (state == RaiderState.Moored || assist == null)
        {
            steering = false;
            return;
        }

        Sail(zdo, state, alive, melee);
    }

    // The set the ship's physics reads: a ship sails on its own in every state but prize and moored.
    private void RefreshSailing()
    {
        if (ship == null)
        {
            return;
        }

        RaiderState state = State;
        if (state != RaiderState.Prize && state != RaiderState.Moored)
        {
            SailingShips.Add(ship.m_players);
        }
        else
        {
            SailingShips.Remove(ship.m_players);
        }
    }

    private int CrewSize()
    {
        return nview != null && nview.IsValid() ? nview.GetZDO().GetInt(RaiderKeys.CrewSize) : 0;
    }

    // A moored ship lies still on every machine, so the one that owns it and the ones that only look agree: the drift
    // and the turn are frozen and the bobbing is left. A prize is let go.
    private void RefreshMooring()
    {
        bool held = State == RaiderState.Moored;
        if (body == null || held == moorHeld)
        {
            return;
        }

        moorHeld = held;
        body.constraints = held ? freeConstraints | MooredConstraints : freeConstraints;
    }

    // Spawns the crew of the tier the server chose, on posts spread over the deck, and fills the hold. A flagship has extra
    // crew at a higher level, and the jarl on the post nearest the mast, who is the last warrior to leap. A ship that
    // cannot get a crew is removed, as it would sail without one for ever.
    private void SpawnCrew(ZDO zdo)
    {
        crewSpawnTried = true;
        IReadOnlyList<CrewTier> tiers = CrewTable.Parse(SeaBattleSettings.CrewTiers.Value);
        int tierIndex = zdo.GetInt(RaiderKeys.Tier, -1);
        CrewTier tier = tierIndex >= 0 && tierIndex < tiers.Count
            ? tiers[tierIndex]
            : CrewTable.Pick(tiers, key => ZoneSystem.instance.GetGlobalKey(key));
        int bonus = FleetRules.CrewBonus(zdo.GetInt(RaiderKeys.Bonus), isFlagship, FleetSettings.FlagshipExtraCrew.Value);
        List<(GameObject Prefab, bool Archer)> crew = tier != null ? ChooseCrew(tier, bonus) : new();
        GameObject jarlPrefab = isFlagship && crew.Count > 0 ? ZNetScene.instance.GetPrefab(WildMonsters.DraugrJarl.PrefabName) : null;
        if (isFlagship && crew.Count > 0 && jarlPrefab == null)
        {
            Jotunn.Logger.LogWarning($"Nail Flagship: no creature called {WildMonsters.DraugrJarl.PrefabName}; the flagship sails without its jarl");
        }

        List<Vector3> posts = RaiderDeck.Posts(ship, crew.Count + (jarlPrefab != null ? 1 : 0));
        if (crew.Count == 0 || posts.Count == 0)
        {
            Jotunn.Logger.LogWarning($"Nail Ship: no crew could be made ({crew.Count} creatures, {posts.Count} places on the deck); the ship is removed");
            Despawn();
            return;
        }

        int archers = crew.FindAll(member => member.Archer).Count;
        List<int> archerPosts = RaiderDeck.OuterIndexes(posts, Mathf.Min(archers, posts.Count));
        List<int> meleePosts = new();
        for (int i = 0; i < posts.Count; i++)
        {
            if (!archerPosts.Contains(i))
            {
                meleePosts.Add(i);
            }
        }

        // The jarl takes his post first, and the warriors share the rest.
        int jarlPost = jarlPrefab != null ? FleetRules.NearestPost(posts, meleePosts, MastPosition()) : -1;
        meleePosts.Remove(jarlPost);
        int level = isFlagship ? FleetSettings.FlagshipCrewLevel.Value : 1;
        int spawned = 0;
        int melee = 0;
        int nextArcher = 0;
        int nextMelee = 0;
        foreach ((GameObject prefab, bool archer) in crew)
        {
            List<int> slots = archer ? archerPosts : meleePosts;
            int slot = archer ? nextArcher++ : nextMelee++;
            if (slot >= slots.Count)
            {
                continue;
            }

            SpawnMember(zdo, prefab, archer, posts[slots[slot]], archer ? -1 : melee, level, false);
            spawned++;
            melee += archer ? 0 : 1;
        }

        if (jarlPost >= 0)
        {
            int jarlLevel = FleetRules.PickLevel(FleetSettings.JarlLevels.Value, key => ZoneSystem.instance.GetGlobalKey(key),
                CreatureStars.MaxLevel, FleetRules.JarlFallbackLevel);
            SpawnMember(zdo, jarlPrefab, false, posts[jarlPost], melee, jarlLevel, true);
            zdo.Set(RaiderKeys.Jarl, true);
            sawJarl = true;
            spawned++;
            melee++;
        }

        FillHold();
        if (spawned == 0)
        {
            Jotunn.Logger.LogWarning("Nail Ship: none of the crew could be spawned; the ship is removed");
            Despawn();
            return;
        }

        zdo.Set(RaiderKeys.Alive, spawned);
        zdo.Set(RaiderKeys.CrewSize, spawned);
        Jotunn.Logger.LogInfo($"The Nail Ship is manned by {spawned} ({melee} warriors), tier {tier.Key}");
    }

    // The prefabs of a tier, with the extra warriors for the players aboard the target; unknown prefabs are skipped.
    private static List<(GameObject Prefab, bool Archer)> ChooseCrew(CrewTier tier, int bonus)
    {
        List<(GameObject, bool)> crew = new();
        GameObject firstMelee = null;
        foreach (CrewEntry entry in tier.Entries)
        {
            GameObject prefab = ZNetScene.instance.GetPrefab(entry.Prefab);
            if (prefab == null)
            {
                Jotunn.Logger.LogWarning($"Nail Ship: no creature called {entry.Prefab} in the crew list, skipped");
                continue;
            }

            firstMelee ??= entry.Archer ? null : prefab;
            for (int i = 0; i < entry.Count; i++)
            {
                crew.Add((prefab, entry.Archer));
            }
        }

        for (int i = 0; i < bonus && firstMelee != null; i++)
        {
            crew.Add((firstMelee, false));
        }

        return crew;
    }

    // One creature on its post, facing outboard. The stars are rolled as for any spawn, and the creature gets its
    // ship, post and role in its ZDO before the crew component is added, as it woke with an empty one. A level above
    // the one it has is set after the spawn, outside the spawn context, as the Lindorm's is, so the star rules of the
    // spawn do not touch it.
    private void SpawnMember(ZDO shipZdo, GameObject prefab, bool archer, Vector3 post, int index, int level, bool jarl)
    {
        Vector3 outboard = post.x >= 0f ? ship.transform.right : -ship.transform.right;
        GameObject member;
        CreatureStars.BeginSpawn(false);
        try
        {
            member = Instantiate(prefab, ship.transform.TransformPoint(post) + Vector3.up * 0.2f, Quaternion.LookRotation(outboard));
        }
        finally
        {
            CreatureStars.EndSpawn();
        }

        ZDO zdo = member.GetComponent<ZNetView>().GetZDO();
        zdo.Persistent = shipZdo.Persistent;
        zdo.Set(RaiderKeys.Ship, shipZdo.m_uid);
        zdo.Set(RaiderKeys.Post, post);
        zdo.Set(RaiderKeys.Role, archer ? 1 : 0);
        zdo.Set(RaiderKeys.Index, index);
        zdo.Set(RaiderKeys.Site, shipZdo.GetString(RaiderKeys.Site));
        if (jarl)
        {
            zdo.Set(RaiderKeys.Jarl, true);
        }

        Character character = member.GetComponent<Character>();
        RaiderCrew.Attach(character);
        character.GetComponent<BaseAI>()?.SetHuntPlayer(true);
        if (level > 1)
        {
            character.SetLevel(FleetRules.CrewLevel(character.GetLevel(), level));
        }
    }

    // The mast in ship-local space, or the middle of the deck when the ship has none.
    private Vector3 MastPosition()
    {
        return ship.m_mastObject != null ? ship.transform.InverseTransformPoint(ship.m_mastObject.transform.position) : Vector3.zero;
    }

    // The hold gets a few draws from the loot list and, by chance, a Raid Chart; a flagship's hold gets more draws and
    // its chart for sure when the settings say so. A chart that is not registered (a failed load) is left out.
    private void FillHold()
    {
        if (hold == null || hold.GetInventory() == null)
        {
            return;
        }

        TreasureLoot.Roll(hold.GetInventory(), SeaBattleSettings.Loot.Value,
            FleetRules.HoldRolls(SeaBattleSettings.LootRolls.Value, isFlagship, FleetSettings.FlagshipLootRolls.Value));
        if (FleetRules.ChartInHold(isFlagship, FleetSettings.FlagshipRaidChart.Value, SeaBattleSettings.RaidChartChance.Value, Random.value))
        {
            GameObject chart = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(NailShipRegistry.RaidChartName) : null;
            if (chart != null)
            {
                hold.GetInventory().AddItem(chart, 1);
            }
        }

        hold.Save();
    }

    // Counts the crew that is loaded here. A new owner that has not seen the crew yet trusts the last count in the ZDO
    // for a few seconds, so it does not take a ship it has just got for a prize. It also notes whether a living jarl is
    // among them.
    private void CountCrew(ZDO zdo, out int alive, out int melee)
    {
        alive = 0;
        melee = 0;
        jarlAlive = false;
        ZDOID id = zdo.m_uid;
        foreach (RaiderCrew member in RaiderCrew.All)
        {
            if (member.ShipId == id && member.Alive)
            {
                alive++;
                melee += member.Archer ? 0 : 1;
                jarlAlive |= member.Jarl;
            }
        }

        if (alive > 0)
        {
            sawCrew = true;
            zdo.Set(RaiderKeys.Alive, alive);
        }
        else if (!sawCrew && Time.time - awakeTime < LoadGraceSeconds)
        {
            alive = zdo.GetInt(RaiderKeys.Alive);
            melee = Mathf.Min(alive, 1);
        }
        else
        {
            zdo.Set(RaiderKeys.Alive, 0);
        }
    }

    // The flagship's owner notes when the jarl is slain: the ship carries one, this machine has seen him alive, and no
    // living jarl is loaded now. The escorts read the flag from the flagship's data.
    private void WatchJarl(ZDO zdo)
    {
        if (!isFlagship || zdo.GetBool(RaiderKeys.JarlFallen))
        {
            return;
        }

        sawJarl |= jarlAlive;
        if (FleetRules.JarlHasFallen(zdo.GetBool(RaiderKeys.Jarl), sawJarl, jarlAlive))
        {
            zdo.Set(RaiderKeys.JarlFallen, true);
            Message("$msg_whitehilt_jarl_falls");
        }
    }

    // The crew is dead: the ship is the players'. It stops, the deed goes into the sagas once, and everyone near is
    // told. It sinks when the prize minutes are out (see SinkWhenDue).
    private void BecomePrize(ZDO zdo)
    {
        zdo.Set(RaiderKeys.State, (int)RaiderState.Prize);
        zdo.Set(RaiderKeys.Prize, ZNet.instance.GetTime().Ticks);
        ship.m_speed = Ship.Speed.Stop;
        steering = false;
        SailingShips.Remove(ship.m_players);
        AnnounceDeed(zdo);
        BreakFleet(zdo);
        Message("$msg_whitehilt_raider_prize");
    }

    // The nails do not hold for long: after the prize minutes (world time, so it counts while nobody is near) the
    // hull is destroyed, which drops the hold as a cargo crate.
    private void SinkWhenDue(ZDO zdo)
    {
        if (sinking || hull == null || PrizeAgeMinutes(zdo) < SeaBattleSettings.PrizeMinutes.Value)
        {
            return;
        }

        sinking = true;
        hull.Remove(true);
    }

    // A hull sunk in battle is a deed too, if the crew did not give the ship away first; a flagship that sinks breaks its fleet.
    private void OnHullDestroyed()
    {
        if (nview != null && nview.IsValid() && nview.IsOwner())
        {
            AnnounceDeed(nview.GetZDO());
            BreakFleet(nview.GetZDO());
        }
    }

    private void AnnounceDeed(ZDO zdo)
    {
        if (zdo.GetBool(RaiderKeys.Saga))
        {
            return;
        }

        zdo.Set(RaiderKeys.Saga, true);
        SagaLog.Announce(SagaKind.SeaBattle, NameToken, transform.position);
    }

    // A flagship that is taken or sunk breaks its fleet: the deed goes into the sagas once, besides the ship's own, and
    // the escorts read the flag from the data.
    private void BreakFleet(ZDO zdo)
    {
        if (!isFlagship || zdo.GetBool(RaiderKeys.Broken))
        {
            return;
        }

        zdo.Set(RaiderKeys.Broken, true);
        SagaLog.Announce(SagaKind.Fleet, "$whitehilt_nailflagship", transform.position);
    }

    private static double PrizeAgeMinutes(ZDO zdo)
    {
        long ticks = zdo.GetLong(RaiderKeys.Prize);
        return ticks == 0L ? 0d : (ZNet.instance.GetTime() - new DateTime(ticks)).TotalMinutes;
    }

    // With nobody within the despawn range for long enough, or the hunted ship gone for as long, the ship sails out of
    // sight. A player standing on it, or near it, keeps it. The last time a player was near is kept in the ZDO, so it
    // counts for a new owner too, and is written now and then, not every tick.
    private bool ShouldDespawn(ZDO zdo)
    {
        long now = ZNet.instance.GetTime().Ticks;
        long seen = zdo.GetLong(RaiderKeys.Seen);
        bool aboard = ship.m_players.Count > 0;
        if (aboard || PlayerWithin(SeaBattleSettings.DespawnRange.Value))
        {
            if (seen == 0L || now - seen >= SeenInterval * TimeSpan.TicksPerSecond)
            {
                zdo.Set(RaiderKeys.Seen, now);
            }

            return !aboard && TargetMissingTooLong();
        }

        if (seen == 0L)
        {
            zdo.Set(RaiderKeys.Seen, now);
            return false;
        }

        return (now - seen) / (double)TimeSpan.TicksPerSecond > SeaBattleSettings.DespawnSeconds.Value || TargetMissingTooLong();
    }

    private bool TargetMissingTooLong()
    {
        if (TargetShip != null)
        {
            missingSince = 0f;
            return false;
        }

        if (missingSince == 0f)
        {
            missingSince = Time.time;
        }

        return Time.time - missingSince > SeaBattleSettings.DespawnSeconds.Value;
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

    // Removes the crew that this machine owns and then the ship. A crew member another machine owns removes itself
    // when it sees that the ship is gone (see RaiderCrew).
    private void Despawn()
    {
        ZDOID id = nview.GetZDO().m_uid;
        List<GameObject> crew = new();
        foreach (RaiderCrew member in RaiderCrew.All)
        {
            if (member.ShipId == id && !member.Boarded && member.Character != null && member.Character.m_nview.IsOwner())
            {
                crew.Add(member.gameObject);
            }
        }

        foreach (GameObject member in crew)
        {
            ZNetScene.instance.Destroy(member);
        }

        Jotunn.Logger.LogInfo($"A Nail Ship at {transform.position} sails out of sight");
        ZNetScene.instance.Destroy(gameObject);
    }

    // One tick of the AI: decide, then set the sail and the course. The hunted ship's speed is measured from its
    // movement, as a ship owned by another machine has no velocity here. The fleet's rules come on top of the tactics:
    // an escort runs when the fleet's morale breaks, keeps its place in the wedge until the flagship closes in, and then
    // fights from the side and distance the fleet gives it; the flagship waits for escorts that have fallen behind. Every
    // fleet fact comes from the flagship's data, which is read even when the flagship is not loaded here.
    private void Sail(ZDO zdo, RaiderState state, int alive, int melee)
    {
        Ship target = TargetShip;
        if (target == null)
        {
            // Out of sight of its target: it keeps its course until the despawn timer has run out.
            ship.m_speed = Ship.Speed.Half;
            return;
        }

        Vector3 targetPosition = target.transform.position;
        TrackTarget(targetPosition);
        int rank = zdo.GetInt(RaiderKeys.Rank);
        ZDOID fleet = zdo.GetZDOID(RaiderKeys.Fleet);
        bool escort = rank > 0 && !fleet.IsNone();
        FlagshipView flagship = escort ? LookAtFlagship(fleet) : default;
        bool morale = escort && FleetRules.MoraleBreaks(flagship.Gone || flagship.Broken, flagship.State, flagship.JarlFallen,
            FleetSettings.FleeWhenFlagshipFalls.Value, FleetSettings.FleeWhenJarlFalls.Value);
        int side = zdo.GetInt(RaiderKeys.Side, 1);
        RaiderTuning tuning = SeaBattleSettings.Tuning();
        if (escort && flagship.Exists)
        {
            side = FleetRules.EscortSide(rank, flagship.Side);
            tuning = tuning.WithBroadside(FleetRules.EscortDistance(rank, flagship.Side, tuning.BroadsideDistance, FleetSettings.EscortSpread.Value));
        }

        float crewShare = Mathf.Clamp01(alive / (float)Mathf.Max(1, CrewSize()));
        RaiderInput input = new(morale ? RaiderState.Flee : state, Flat(transform.position), ShipAssist.Heading(transform), Flat(targetPosition),
            targetVelocity, ShipAssist.Heading(target.transform), crewShare, HullShare(zdo), Time.time - stateSince, side, melee > 0,
            GrappleLink.IsBound(ship));
        RaiderOrder order = RaiderTactics.Decide(input, tuning);
        if (order.State == RaiderState.Approach)
        {
            order = FleetOrder(order, escort, rank, flagship, Flat(targetPosition));
        }

        if (order.State != state)
        {
            zdo.Set(RaiderKeys.State, (int)order.State);
            knownState = order.State;
            stateSince = Time.time;
            if (order.State == RaiderState.Flee)
            {
                Message(morale && state != RaiderState.Flee ? "$msg_whitehilt_fleet_breaks" : "$msg_whitehilt_raider_flees");
            }
        }

        ship.m_speed = order.Sail switch
        {
            RaiderSail.Full => Ship.Speed.Full,
            RaiderSail.Half => Ship.Speed.Half,
            _ => Ship.Speed.Stop
        };
        steerCourse = ChooseCourse(order.Course, target.GetComponent<Rigidbody>(), out bool clear);
        steering = true;
        if (!clear)
        {
            ship.m_speed = Ship.Speed.Stop;
        }

        HandOutLeap(zdo, order.State);
    }

    // What the fleet makes of an order to approach: an escort that has not broken formation sails for its place in the
    // wedge behind the flagship, which is loaded here; the flagship sails half while its escorts are behind. A ship
    // that is not in a fleet, or whose flagship is not loaded here, acts alone.
    private RaiderOrder FleetOrder(RaiderOrder order, bool escort, int rank, FlagshipView flagship, Vector2 target)
    {
        float spacing = FleetSettings.FormationSpacing.Value;
        if (escort)
        {
            if (formationBroken || flagship.Loaded == null)
            {
                return order;
            }

            Vector2 flagshipPosition = Flat(flagship.Loaded.transform.position);
            if (FleetRules.BreaksFormation(flagship.State, Vector2.Distance(flagshipPosition, target), FleetSettings.FormationBreak.Value))
            {
                formationBroken = true;
                return order;
            }

            float heading = ShipAssist.Heading(flagship.Loaded.transform);
            return FleetRules.EscortSail(Flat(transform.position), FleetRules.SlotPoint(flagshipPosition, heading, rank, spacing), heading, spacing);
        }

        if (!isFlagship || FleetRules.BreaksFormation(RaiderState.Approach, Vector2.Distance(Flat(transform.position), target), FleetSettings.FormationBreak.Value))
        {
            return order;
        }

        // The escorts this machine has loaded that still hold formation; the ones it does not have are not waited for.
        slotErrors.Clear();
        ZDOID id = nview.GetZDO().m_uid;
        Vector2 self = Flat(transform.position);
        float flagshipHeading = ShipAssist.Heading(transform);
        foreach (RaiderShip other in byShip.Values)
        {
            if (other != null && other != this && other.FleetId == id && other.Rank > 0 && other.State == RaiderState.Approach)
            {
                slotErrors.Add(FleetRules.SlotError(Flat(other.transform.position), self, flagshipHeading, other.Rank, spacing));
            }
        }

        return FleetRules.FlagshipOrder(order, FleetRules.FlagshipWaits(slotErrors, spacing));
    }

    // What an escort knows of its flagship: its data, which is there even when the ship is not loaded here, and the ship
    // itself when it is. A flagship whose data has been missing for a few seconds counts as gone; a shorter gap is a
    // machine that has not been sent the data yet.
    private FlagshipView LookAtFlagship(ZDOID fleet)
    {
        ZDO zdo = ZDOMan.instance != null ? ZDOMan.instance.GetZDO(fleet) : null;
        if (zdo == null)
        {
            if (flagshipMissingSince == 0f)
            {
                flagshipMissingSince = Time.time;
            }

            return new FlagshipView(false, FleetRules.FlagshipGone(Time.time - flagshipMissingSince), RaiderState.Approach, false, false, 1, null);
        }

        flagshipMissingSince = 0f;
        GameObject found = ZNetScene.instance != null ? ZNetScene.instance.FindInstance(fleet) : null;
        return new FlagshipView(true, false, (RaiderState)zdo.GetInt(RaiderKeys.State), zdo.GetBool(RaiderKeys.JarlFallen), zdo.GetBool(RaiderKeys.Broken),
            zdo.GetInt(RaiderKeys.Side, 1), found != null ? found.GetComponent<RaiderShip>() : null);
    }

    // The flagship as an escort sees it.
    private readonly struct FlagshipView
    {
        public FlagshipView(bool exists, bool gone, RaiderState state, bool jarlFallen, bool broken, int side, RaiderShip loaded)
        {
            Exists = exists;
            Gone = gone;
            State = state;
            JarlFallen = jarlFallen;
            Broken = broken;
            Side = side;
            Loaded = loaded;
        }

        // Its data was found.
        public bool Exists { get; }

        // Its data has been missing long enough for it to count as sunk or gone.
        public bool Gone { get; }

        public RaiderState State { get; }

        public bool JarlFallen { get; }

        public bool Broken { get; }

        // The side of its target it lies on, -1 port and 1 starboard.
        public int Side { get; }

        // The ship, when it is loaded on this machine.
        public RaiderShip Loaded { get; }
    }

    // The target's velocity from its last two positions, smoothed a little.
    private void TrackTarget(Vector3 position)
    {
        float dt = Time.time - lastTargetTime;
        if (lastTargetTime > 0f && dt > 0.01f)
        {
            Vector2 velocity = (Flat(position) - Flat(lastTargetPosition)) / dt;
            targetVelocity = Vector2.Lerp(targetVelocity, velocity, 0.5f);
        }

        lastTargetPosition = position;
        lastTargetTime = Time.time;
    }

    private float HullShare(ZDO zdo)
    {
        if (hull == null || hull.m_health <= 0f)
        {
            return 1f;
        }

        return Mathf.Clamp01(zdo.GetFloat(ZDOVars.s_health, hull.m_health) / hull.m_health);
    }

    // The course asked for if the sea ahead is clear, else the nearest clear one 30, 60 or 90 degrees off it, as the
    // route autopilot does; the side taken last is tried first so it does not swing back and forth. The target ship's
    // own hull does not count, as the Nail Ship is meant to come close to it.
    private float ChooseCourse(float course, Rigidbody target, out bool clear)
    {
        clear = true;
        float look = assist.Lookahead();
        if (!assist.ShallowAhead() && !assist.Blocked(course, look, target))
        {
            return course;
        }

        for (float angle = 30f; angle <= 90f; angle += 30f)
        {
            foreach (int sign in new[] { avoidSign, -avoidSign })
            {
                if (!assist.Blocked(course + sign * angle, look, target))
                {
                    avoidSign = sign;
                    return course + sign * angle;
                }
            }
        }

        clear = false;
        return course;
    }

    // While boarding, lets one warrior at a time leap: every few seconds the next living warrior gets the turn. Leaving
    // the boarding state gives the turn back.
    private void HandOutLeap(ZDO zdo, RaiderState state)
    {
        if (state != RaiderState.Board)
        {
            if (zdo.GetInt(RaiderKeys.Leap, -1) != -1)
            {
                zdo.Set(RaiderKeys.Leap, -1);
            }

            lastLeap = -1;
            return;
        }

        if (Time.time < nextLeap)
        {
            return;
        }

        nextLeap = Time.time + SeaBattleSettings.BoardIntervalSeconds.Value;
        zdo.Set(RaiderKeys.Leap, NextLeap(zdo.m_uid));
    }

    // The next living warrior who has not leapt, after the one who had the last turn; the first one when the numbers
    // run out; -1 when no one is left.
    private int NextLeap(ZDOID id)
    {
        int next = int.MaxValue;
        int first = int.MaxValue;
        foreach (RaiderCrew member in RaiderCrew.All)
        {
            if (member.ShipId != id || member.Archer || !member.Alive || member.Boarded || member.Index < 0)
            {
                continue;
            }

            first = Mathf.Min(first, member.Index);
            if (member.Index > lastLeap)
            {
                next = Mathf.Min(next, member.Index);
            }
        }

        lastLeap = next != int.MaxValue ? next : first != int.MaxValue ? first : -1;
        return lastLeap;
    }

    // Tells everyone within range, by the ship, a message. Sent as an RPC on the ship, so every machine that has it
    // loaded shows it to its own player if that player is near.
    private void Message(string token)
    {
        nview.InvokeRPC(ZNetView.Everybody, MessageRpc, token);
    }

    private void RPC_Message(long sender, string token)
    {
        Player player = Player.m_localPlayer;
        if (player != null && Vector3.Distance(player.transform.position, transform.position) <= MessageRange)
        {
            player.Message(MessageHud.MessageType.Center, token);
        }
    }

    private static Vector2 Flat(Vector3 position)
    {
        return new Vector2(position.x, position.z);
    }
}
