using BrudvikWhiteHilt.Difficulty;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Summoning;
using BrudvikWhiteHilt.Kraken;
using BrudvikWhiteHilt.Monsters;
using BrudvikWhiteHilt.Pieces.Ships;
using BrudvikWhiteHilt.Pieces.Ships.Ram;
using BrudvikWhiteHilt.Saga;
using BrudvikWhiteHilt.SeaBattles.Damage;
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
/// lets the crew mend its own deck, gives up a hunt that land keeps it from, makes the ship a prize when the crew is dead,
/// and lets it sink or sail out of sight. Everyone reads the state from the ZDO, so the hover text, the locks on the helm
/// and the hold, the messages, the hull's health and the looks (a ram, a burning deck) are the same for all. The same
/// component sails the flagship of a fleet and its escorts: the owner of each reads the fleet's facts (the flagship's
/// state, whether its jarl has fallen) from the flagship's data, so a ship never writes to another ship; the flagship's
/// owner raises the ship its jarl's horn calls. A fire ship is a Nail Ship with no crew that sails at its target and sets
/// it alight.
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
    private const string HornMessage = "$msg_whitehilt_fleet_horn";

    // The Nail Ship appears in the arc this many degrees either side of the target's heading, tried at this many bearings,
    // where the sea is at least this deep.
    private const float SpawnArc = 45f;
    private const int SpawnAttempts = 12;
    private const float SpawnDepth = 6f;
    private const float LeastSpawnDistance = 40f;

    // A moored ship keeps its place and heading and only bobs, as a ship at a drift anchor does.
    private const RigidbodyConstraints MooredConstraints =
        RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationY;

    // A fire ship strikes the ship it comes for when their middles are this close: about side to side.
    private const float FireStrikeDistance = 12f;

    // A fire ship warns the ship it comes for once it is this close.
    private const float FireWarnDistance = 120f;

    // A ship that keeps away from its target must have closed in by this many metres to count as getting anywhere.
    private const float StuckProgress = 10f;

    // A reinforcement appears this share of the spawn distance from the target, anywhere round it.
    private const float ReinforceDistanceShare = 0.8f;
    private const float ReinforceArc = 180f;

    // Players this far off a ship's centre line stand at its rail, as a Nail Ship choosing its side counts them.
    private const float RailOffset = 0.5f;

    // The flames a fire ship's deck shows: this many, on posts spread over its deck as a crew's are, or at these points in
    // its own space when no deck is found.
    private const int FireShipFlames = 4;
    private static readonly Vector3[] fireShipFlames =
    {
        new(0f, 1.2f, 4f), new(-0.8f, 1.2f, 0f), new(0.8f, 1.2f, -3f), new(0f, 1.2f, -6f)
    };

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
    private float blockedSince;
    private float blockedDistance;
    private bool justRammed;
    private float nextRepair;
    private float hullApplied;
    private bool ramShown;
    private GameObject ramProw;
    private DeckFireLook fireLook;
    private bool fireWarned;
    private int strikesLeft;
    private Vector3 strikeNear;

    /// <summary>The longship this component sails.</summary>
    public Ship Ship => ship;

    /// <summary>Whether this is the flagship of a fleet.</summary>
    public bool IsFlagship => isFlagship;

    /// <summary>Whether this is a fire ship: no crew, a burning deck, and it sails at its target to set it alight.</summary>
    public bool IsFireShip => nview != null && nview.IsValid() && nview.GetZDO().GetBool(RaiderKeys.FireShip);

    /// <summary>Whether the ship carries a ram on its bow.</summary>
    public bool HasRam => nview != null && nview.IsValid() && nview.GetZDO().GetBool(RaiderKeys.Ram);

    /// <summary>The translation token of the ship's name: the flagship and the fire ship have their own.</summary>
    public string NameToken => IsFireShip ? "$whitehilt_fireship" : isFlagship ? "$whitehilt_nailflagship" : "$whitehilt_nailship";

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

    /// <summary>The index of a second melee crew member who may leap at the same time, or -1.</summary>
    public int LeapTurn2 => nview != null && nview.IsValid() ? nview.GetZDO().GetInt(RaiderKeys.Leap2, -1) : -1;

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
            string kind = raider.IsFireShip ? ", fire ship" : raider.HasRam ? ", ram" : string.Empty;
            float hullMax = raider.hull != null ? raider.hull.m_health : 0f;
            float hullNow = raider.nview != null && raider.nview.IsValid() ? raider.HullShare(raider.nview.GetZDO()) * hullMax : 0f;
            lines.Add($"  {raider.State}{kind}, {fleet}, crew {crew}/{raider.CrewSize()}, hull {hullNow:0}/{hullMax:0}, " +
                $"{distance:0} m away, owner here: {raider.nview != null && raider.nview.IsOwner()}");
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
    /// carries the flagship's id and its own rank in its data. A fire ship, when the order brings one, comes from another
    /// bearing in the same arc and sails on its own.
    /// </summary>
    /// <param name="target">The ship to hunt.</param>
    /// <param name="crewBonus">Extra melee crew for the players aboard the target.</param>
    /// <param name="tier">The crew tier, or null for the one the world has reached.</param>
    /// <param name="size">The number of ships the server chose, 1 to 3; 1 is a plain Nail Ship.</param>
    /// <param name="fireShip">Whether a fire ship comes along.</param>
    /// <param name="forceRam">Whether the lead ship carries a ram whatever the chance says, as an admin may order.</param>
    /// <returns>The flagship (the lone ship when the size is 1), or null when there was no deep water or no prefab.</returns>
    public static RaiderShip SpawnFleet(Ship target, int crewBonus, CrewTier tier, int size, bool fireShip = false, bool forceRam = false)
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
        if (!TryPickNear(target, SeaBattleSettings.SpawnDistance.Value, SpawnArc, out Vector2 spawn))
        {
            Jotunn.Logger.LogInfo("Nail Ship: no deep water within reach");
            return null;
        }

        Vector3 point = new(spawn.x, ZoneSystem.instance.m_waterLevel, spawn.y);
        Quaternion facing = Facing(point, centre);
        ZDOID targetId = target.m_nview.GetZDO().m_uid;
        RaiderShip lead = Raise(prefab, point, facing, targetId, tier, crewBonus, forceRam, false);
        ZDO leadZdo = lead.nview.GetZDO();
        if (fireShip)
        {
            RaiseFireShip(target, targetId, tier);
        }

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
            if (!SeaBattleService.DeepWater(slot.x, slot.y, SpawnDepth) || !SeaBattleService.InNearArea(new Vector3(slot.x, 0f, slot.y), 0))
            {
                Jotunn.Logger.LogInfo($"Nail Ship: no deep water within reach for escort {rank} of the fleet, left out");
                continue;
            }

            RaiderShip escort = Raise(escortPrefab, new Vector3(slot.x, point.y, slot.y), facing, targetId, tier, crewBonus, false, false);
            ZDO escortZdo = escort.nview.GetZDO();
            escortZdo.Set(RaiderKeys.Fleet, leadZdo.m_uid);
            escortZdo.Set(RaiderKeys.Rank, rank);
            raised++;
        }

        Jotunn.Logger.LogInfo($"A fleet of {raised} Nail Ships sails up at {point} to the ship at {centre}");
        return lead;
    }

    // A fire ship at the spawn distance, from a bearing of its own in the arc ahead of the target. It has no crew and sails
    // alone, so a fire ship that finds no deep water is simply left out.
    private static void RaiseFireShip(Ship target, ZDOID targetId, CrewTier tier)
    {
        GameObject prefab = ZNetScene.instance.GetPrefab(NailShipRegistry.ShipName);
        Vector3 centre = target.transform.position;
        if (prefab == null || !TryPickNear(target, SeaBattleSettings.SpawnDistance.Value, SpawnArc, out Vector2 spawn))
        {
            Jotunn.Logger.LogInfo("Nail Ship: no deep water for the fire ship, left out");
            return;
        }

        Vector3 point = new(spawn.x, ZoneSystem.instance.m_waterLevel, spawn.y);
        Raise(prefab, point, Facing(point, centre), targetId, tier, 0, false, true);
        Jotunn.Logger.LogInfo($"A fire ship drifts up at {point} to the ship at {centre}");
    }

    /// <summary>
    /// Raises one more Nail Ship for a fleet, as the jarl's horn calls it: a plain ship with the flagship's crew tier and
    /// extra crew, somewhere round the target at most of the spawn distance, in deep water. It joins the fleet as the
    /// reinforcement, so it takes its own side and keeps the fleet's morale. Only on the flagship's owner.
    /// </summary>
    /// <param name="target">The ship the fleet hunts.</param>
    /// <returns>The new ship, or null when there was no deep water or no prefab.</returns>
    public RaiderShip RaiseReinforcement(Ship target)
    {
        GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(NailShipRegistry.ShipName) : null;
        if (prefab == null || target == null || target.m_nview == null || !target.m_nview.IsValid() || !nview.IsOwner())
        {
            return null;
        }

        ZDO zdo = nview.GetZDO();
        IReadOnlyList<CrewTier> tiers = CrewTable.Parse(SeaBattleSettings.CrewTiers.Value);
        int tierIndex = zdo.GetInt(RaiderKeys.Tier, -1);
        CrewTier tier = tierIndex >= 0 && tierIndex < tiers.Count ? tiers[tierIndex] : CrewTable.Pick(tiers, key => ZoneSystem.instance.GetGlobalKey(key));
        Vector3 centre = target.transform.position;
        if (tier == null || !TryPickNear(target, SeaBattleSettings.SpawnDistance.Value * ReinforceDistanceShare, ReinforceArc, out Vector2 spawn))
        {
            Jotunn.Logger.LogInfo("Nail Flagship: no deep water for the reinforcement");
            return null;
        }

        Vector3 point = new(spawn.x, ZoneSystem.instance.m_waterLevel, spawn.y);
        RaiderShip reinforcement = Raise(prefab, point, Facing(point, centre), target.m_nview.GetZDO().m_uid, tier, zdo.GetInt(RaiderKeys.Bonus), false, false);
        ZDO added = reinforcement.nview.GetZDO();
        added.Set(RaiderKeys.Fleet, zdo.m_uid);
        added.Set(RaiderKeys.Rank, FleetRules.ReinforcementRank);
        Jotunn.Logger.LogInfo($"The jarl's horn calls a Nail Ship at {point} to the ship at {centre}");
        return reinforcement;
    }

    // A place in deep water in the arc ahead of the target, at the set distance or, where that lies beyond the near zones
    // this machine keeps alive, closer: on a server the agreed simulation distance may be short, and a Nail Ship raised
    // outside it is destroyed at once. A place a zone inside the edge is tried first. The depth comes from the world
    // generator, not from a ray at the seabed, as the zone out there may not be loaded.
    private static bool TryPickNear(Ship target, float distance, float arc, out Vector2 spawn)
    {
        Vector3 centre = target.transform.position;
        Vector2 flat = new(centre.x, centre.z);
        float heading = ShipAssist.Heading(target.transform);
        foreach (int margin in new[] { 1, 0 })
        {
            foreach (float tried in EncounterRules.ShorterDistances(distance, LeastSpawnDistance))
            {
                if (EncounterRules.TryPickSpawn(flat, heading, tried, EncounterRules.Offsets(arc, SpawnAttempts, Random.value),
                    spot => SeaBattleService.DeepWater(spot.x, spot.y, SpawnDepth) && SeaBattleService.InNearArea(new Vector3(spot.x, 0f, spot.y), margin),
                    out spawn))
                {
                    return true;
                }
            }
        }

        spawn = flat;
        return false;
    }

    private static Quaternion Facing(Vector3 from, Vector3 to)
    {
        Vector3 toTarget = to - from;
        toTarget.y = 0f;
        return toTarget.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(toTarget) : Quaternion.identity;
    }

    // One ship of the kind the prefab is, with the data every Nail Ship starts with; the caller adds the fleet keys. Its
    // side is random until it comes alongside, and an escort's is replaced by the fleet's rules when it fights. The hull's
    // full health is chosen here, from the settings and the world's keys, and written to the data so every machine uses
    // the same; whether the ship carries a ram is rolled here too. A fire ship gets its own share of the health and the
    // time it was lit.
    private static RaiderShip Raise(GameObject prefab, Vector3 point, Quaternion rotation, ZDOID targetId, CrewTier tier, int crewBonus,
        bool forceRam, bool fireShip)
    {
        GameObject instance = Instantiate(prefab, point, rotation);
        ZDO zdo = instance.GetComponent<ZNetView>().GetZDO();
        long now = ZNet.instance.GetTime().Ticks;
        zdo.Set(RaiderKeys.Target, targetId);
        zdo.Set(RaiderKeys.Tier, tier.Index);
        zdo.Set(RaiderKeys.Side, Random.value < 0.5f ? -1 : 1);
        zdo.Set(RaiderKeys.Bonus, Mathf.Max(0, crewBonus));
        zdo.Set(RaiderKeys.State, (int)RaiderState.Approach);
        zdo.Set(RaiderKeys.StateSince, now);

        bool flagship = NailShipRegistry.IsFlagship(instance);
        float hullMax = (flagship ? FleetSettings.FlagshipHullHealth.Value : SeaBattleSettings.HullHealth.Value)
            * Mathf.Max(0.1f, TierValues.PickNumber(SeaBattleSettings.HullTiers.Value, HasKey, 1f));
        if (fireShip)
        {
            hullMax *= WeaponSettings.FireShipHullShare.Value;
            zdo.Set(RaiderKeys.FireShip, true);
            zdo.Set(RaiderKeys.Lit, now);
        }
        else if (WeaponRules.GetsRam(forceRam, WeaponRules.KeyReached(WeaponSettings.RamKey.Value, HasKey), WeaponSettings.RamChance.Value, Random.value))
        {
            zdo.Set(RaiderKeys.Ram, true);
        }

        zdo.Set(RaiderKeys.HullMax, hullMax);
        zdo.Set(ZDOVars.s_health, hullMax);
        RaiderShip raider = instance.GetComponent<RaiderShip>();
        raider.ApplyHull();
        return raider;
    }

    private static bool HasKey(string key)
    {
        return ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(key);
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

    /// <summary>
    /// Tells the ship that its ram has struck its target, so it breaks off the charge. Called by the ram on the ship's owner.
    /// </summary>
    public void NoteRammed()
    {
        justRammed = true;
    }

    /// <summary>
    /// Gives the hull the full health chosen when the ship was raised, on every machine, so the share of health left, the
    /// hover and the flight from a broken hull agree. A ship with none chosen (a draugr harbour's) keeps its prefab's.
    /// </summary>
    public void ApplyHull()
    {
        float max = nview != null && nview.IsValid() ? nview.GetZDO().GetFloat(RaiderKeys.HullMax) : 0f;
        if (hull != null && max > 0f && max != hullApplied)
        {
            hullApplied = max;
            hull.m_health = max;
        }
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

        fireLook?.Dispose();
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

    // Keeps the set of sailing ships, the hull's health and the looks (a ram, a burning deck) right on every machine, so a
    // new owner already has them. The rest is the owner's: spawning the crew, counting it, the prize, the despawn, the
    // crew's repairs and the AI. A fire ship has no crew and goes its own way.
    private void Think()
    {
        if (!nview.IsValid())
        {
            return;
        }

        RefreshSailing();
        RefreshMooring();
        ApplyHull();
        RefreshLooks();
        ZDO zdo = nview.GetZDO();
        RaiderState state = State;
        if (!nview.IsOwner())
        {
            steering = false;
            return;
        }

        assist ??= GetComponent<ShipAssist>();
        if (IsFireShip)
        {
            SailFireShip(zdo);
            return;
        }

        if (CrewSize() == 0)
        {
            if (!crewSpawnTried && Time.time - awakeTime >= SpawnDelay)
            {
                SpawnCrew(zdo);
            }

            return;
        }

        CountCrew(zdo, out int alive, out int melee, out int onDeck);
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

        if (onDeck > 0)
        {
            Repair();
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

    // On every machine: the ram on the bow shows on a ship that carries one, and a fire ship's deck burns. A dedicated
    // server draws nothing, and a look that fails is left off without touching the ship's rules.
    private void RefreshLooks()
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        try
        {
            bool ram = HasRam;
            if (ram != ramShown)
            {
                ramShown = ram;
                ramProw ??= transform.Find(RamProw.ObjectName)?.gameObject;
                ramProw?.SetActive(ram);
            }

            if (fireLook == null && IsFireShip && !sinking)
            {
                List<Vector3> posts = RaiderDeck.Posts(ship, FireShipFlames);
                fireLook = new DeckFireLook(transform);
                fireLook.Sync(posts.Count > 0 ? posts : fireShipFlames);
            }
        }
        catch (Exception ex)
        {
            ramShown = HasRam;
            Jotunn.Logger.LogWarning($"Nail Ship: the look of {name} could not be set: {ex.Message}");
        }
    }

    // The crew mends its own ship now and then: one fire on the deck is put out, or else one hole is patched. Only the
    // owner does it, and only while a living crew member is still aboard, so a ship whose crew has all leapt across burns.
    private void Repair()
    {
        float seconds = SeaBattleSettings.CrewRepairSeconds.Value;
        if (seconds <= 0f || Time.time < nextRepair)
        {
            return;
        }

        nextRepair = Time.time + seconds;
        DeckFires fires = DeckFires.Of(ship);
        if (fires != null && fires.Points.Count > 0 && fires.PutOutByCrew(0))
        {
            return;
        }

        ShipDamage damage = ShipDamage.Of(ship);
        if (damage != null && damage.Holes.Count > 0)
        {
            damage.PatchByCrew(0);
        }
    }

    // A fire ship, on its owner: it sails at full sail straight for its target, warns it when near, and strikes when it
    // comes alongside: it lights its fires on the target's deck one by one (the deck's owner takes one fire at a time) and
    // then goes down. It burns down and sinks by itself when its time is out, and leaves when nobody is near, as any Nail
    // Ship does.
    private void SailFireShip(ZDO zdo)
    {
        if (sinking)
        {
            return;
        }

        Ship target = TargetShip;
        if (strikesLeft > 0)
        {
            steering = false;
            ship.m_speed = Ship.Speed.Stop;
            DeckFires fires = target != null ? DeckFires.Of(target) : null;
            Vector2 scatter = Random.insideUnitCircle * 2f;
            if (fires != null && RaiderDeck.TryDeckPoint(target, strikeNear + new Vector3(scatter.x, 0f, scatter.y), out Vector3 spot))
            {
                fires.RequestFire(spot);
            }

            if (--strikesLeft == 0)
            {
                Sink();
            }

            return;
        }

        if (WeaponRules.BurntDown(zdo.GetLong(RaiderKeys.Lit), ZNet.instance.GetTime().Ticks, WeaponSettings.FireShipSeconds.Value))
        {
            Sink();
            return;
        }

        if (ShouldDespawn(zdo))
        {
            Despawn();
            return;
        }

        if (target == null || assist == null)
        {
            ship.m_speed = Ship.Speed.Half;
            return;
        }

        Vector3 targetPosition = target.transform.position;
        TrackTarget(targetPosition);
        float distance = Vector2.Distance(Flat(transform.position), Flat(targetPosition));
        if (!fireWarned && distance <= FireWarnDistance)
        {
            fireWarned = true;
            ShipDamage.Of(target)?.Tell("$msg_whitehilt_ship_fireship");
        }

        if (distance <= FireStrikeDistance)
        {
            Strike(target);
            return;
        }

        float course = RaiderTactics.CourseTo(Flat(transform.position), RaiderTactics.LeadPoint(Flat(transform.position), Flat(targetPosition), targetVelocity),
            ShipAssist.Heading(transform));
        ship.m_speed = Ship.Speed.Full;
        steerCourse = ChooseCourse(course, target.GetComponent<Rigidbody>(), out bool clear, out bool turnAbout);
        steering = true;
        if (!clear || turnAbout)
        {
            ship.m_speed = clear ? Ship.Speed.Slow : Ship.Speed.Back;
        }
    }

    // The fire ship has come alongside: the blow burns the target's hull, and its fires are lit over the next ticks.
    private void Strike(Ship target)
    {
        int fires = Mathf.Max(1, WeaponSettings.FireShipFires.Value);
        strikesLeft = fires;
        strikeNear = target.transform.position + (transform.position - target.transform.position) * 0.3f;
        ShipDamage.Of(target)?.Tell("$msg_whitehilt_ship_fireship_strikes");
        WearNTear targetHull = target.GetComponent<WearNTear>();
        if (targetHull != null)
        {
            HitData hit = new() { m_point = strikeNear, m_dir = target.transform.position - transform.position, m_hitType = HitData.HitType.Burning };
            hit.m_damage.m_fire = 25f * fires;
            targetHull.Damage(hit);
        }
    }

    // The ship goes down: its hull is destroyed, as a prize's is when its time is out.
    private void Sink()
    {
        if (sinking || hull == null)
        {
            return;
        }

        sinking = true;
        steering = false;
        fireLook?.Dispose();
        fireLook = null;
        hull.Remove(true);
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

        // The jarl takes his post first, and the warriors share the rest. The crew is at least the level the world's keys
        // give, and a flagship's at least its own.
        int jarlPost = jarlPrefab != null ? FleetRules.NearestPost(posts, meleePosts, MastPosition()) : -1;
        meleePosts.Remove(jarlPost);
        int level = Mathf.Max(isFlagship ? FleetSettings.FlagshipCrewLevel.Value : 1,
            Mathf.RoundToInt(TierValues.PickNumber(SeaBattleSettings.CrewLevels.Value, HasKey, 1f)));
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

    // The hold gets a few draws from the loot list of the stage the world has reached and, by chance, a Raid Chart; a
    // flagship's hold gets more draws and its chart for sure when the settings say so. A chart that is not registered (a
    // failed load) is left out.
    private void FillHold()
    {
        if (hold == null || hold.GetInventory() == null)
        {
            return;
        }

        string loot = TierValues.PickText(SeaBattleSettings.LootTiers.Value, HasKey) ?? SeaBattleSettings.Loot.Value;
        TreasureLoot.Roll(hold.GetInventory(), loot,
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

    // Counts the crew that is loaded here, and how many of it still stand on this ship's deck. A new owner that has not
    // seen the crew yet trusts the last count in the ZDO for a few seconds, so it does not take a ship it has just got for
    // a prize. It also notes whether a living jarl is among them.
    private void CountCrew(ZDO zdo, out int alive, out int melee, out int onDeck)
    {
        alive = 0;
        melee = 0;
        onDeck = 0;
        jarlAlive = false;
        ZDOID id = zdo.m_uid;
        foreach (RaiderCrew member in RaiderCrew.All)
        {
            if (member.ShipId == id && member.Alive)
            {
                alive++;
                melee += member.Archer ? 0 : 1;
                onDeck += member.Boarded ? 0 : 1;
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

    // A hull sunk in battle is a deed too, if the crew did not give the ship away first; a flagship that sinks breaks its
    // fleet. A fire ship that goes down is no deed.
    private void OnHullDestroyed()
    {
        if (nview != null && nview.IsValid() && nview.IsOwner() && !IsFireShip)
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
    // fleet fact comes from the flagship's data, which is read even when the flagship is not loaded here. The time in a
    // state is world time kept in the data, so a new owner goes on counting where the old one stopped.
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
        long now = ZNet.instance.GetTime().Ticks;
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

        // A charge that has struck is over; the tactics then come alongside again.
        RaiderState current = state == RaiderState.Ram && justRammed ? RaiderState.Broadside : state;
        justRammed = false;
        bool canRam = RamSettings.Enabled.Value && WeaponRules.RamReady(HasRam, zdo.GetLong(RaiderKeys.RamTime), now, WeaponSettings.RamSeconds.Value);
        float crewShare = Mathf.Clamp01(alive / (float)Mathf.Max(1, CrewSize()));
        RaiderInput input = new(morale ? RaiderState.Flee : current, Flat(transform.position), ShipAssist.Heading(transform), Flat(targetPosition),
            targetVelocity, ShipAssist.Heading(target.transform), crewShare, HullShare(zdo), StateSeconds(zdo, now), side, melee > 0,
            GrappleLink.IsBound(ship), canRam);
        RaiderOrder order = RaiderTactics.Decide(input, tuning);
        if (order.State == RaiderState.Approach)
        {
            order = FleetOrder(order, escort, rank, flagship, Flat(targetPosition));
        }

        float distance = Vector2.Distance(Flat(transform.position), Flat(targetPosition));
        steerCourse = ChooseCourse(order.Course, target.GetComponent<Rigidbody>(), out bool clear, out bool turnAbout);
        bool gaveUp = order.State != RaiderState.Flee && !GrappleLink.IsBound(ship) && GivesUp(order.Course, clear, distance);
        if (gaveUp)
        {
            order = new RaiderOrder(RaiderState.Flee, order.Course, RaiderSail.Full);
            Message("$msg_whitehilt_raider_gives_up");
        }

        if (order.State != state)
        {
            ChangeState(zdo, state, order.State, now, escort, target, morale, gaveUp);
        }

        ship.m_speed = !clear ? Ship.Speed.Back : turnAbout ? Ship.Speed.Slow : order.Sail switch
        {
            RaiderSail.Full => Ship.Speed.Full,
            RaiderSail.Half => Ship.Speed.Half,
            _ => Ship.Speed.Stop
        };
        steering = true;
        if (isFlagship)
        {
            CallReinforcement(zdo, order.State, target, now);
        }

        HandOutLeap(zdo, order.State);
    }

    // Writes a new state on the owner, with the world time it began, and does what the change brings: a ship that comes
    // alongside on its own picks its side; a charge that ends starts the ram's cooldown; and those near are told when it
    // flees (unless it gave up, which has its own words) or charges.
    private void ChangeState(ZDO zdo, RaiderState from, RaiderState to, long now, bool escort, Ship target, bool morale, bool gaveUp)
    {
        zdo.Set(RaiderKeys.State, (int)to);
        zdo.Set(RaiderKeys.StateSince, now);
        if (to == RaiderState.Broadside && from == RaiderState.Approach && !escort)
        {
            zdo.Set(RaiderKeys.Side, PickSide(target));
        }

        if (from == RaiderState.Ram)
        {
            zdo.Set(RaiderKeys.RamTime, now);
        }

        if (to == RaiderState.Ram)
        {
            Message("$msg_whitehilt_raider_rams");
        }
        else if (to == RaiderState.Flee && !gaveUp)
        {
            Message(morale ? "$msg_whitehilt_fleet_breaks" : "$msg_whitehilt_raider_flees");
        }
    }

    // Seconds since the state began, by the world clock in the data. A ship raised by an older version has no start yet;
    // it gets one now.
    private float StateSeconds(ZDO zdo, long now)
    {
        long since = zdo.GetLong(RaiderKeys.StateSince);
        if (since == 0L)
        {
            zdo.Set(RaiderKeys.StateSince, now);
            return 0f;
        }

        return (float)((now - since) / (double)TimeSpan.TicksPerSecond);
    }

    // The side to come alongside on: the one the ship is on, unless fewer of the target's players stand at the other rail.
    private int PickSide(Ship target)
    {
        int port = 0;
        int starboard = 0;
        foreach (Player player in Player.GetAllPlayers())
        {
            if (player == null || player.IsDead() || !target.IsPlayerInBoat(player))
            {
                continue;
            }

            float x = target.transform.InverseTransformPoint(player.transform.position).x;
            port += x < -RailOffset ? 1 : 0;
            starboard += x > RailOffset ? 1 : 0;
        }

        return RaiderTactics.PickSide(Flat(transform.position), Flat(target.transform.position), ShipAssist.Heading(target.transform), port, starboard);
    }

    // Whether the ship gives up the hunt: land or shallows have kept it off its course for the setting's seconds and it has
    // got no nearer to its target in that time. Coming nearer starts the count again, so a ship that sails round an island
    // keeps hunting.
    private bool GivesUp(float wanted, bool clear, float distance)
    {
        float seconds = SeaBattleSettings.StuckSeconds.Value;
        bool kept = !clear || Mathf.Abs(Mathf.DeltaAngle(wanted, steerCourse)) > 1f;
        if (!kept || seconds <= 0f)
        {
            blockedSince = 0f;
            return false;
        }

        if (blockedSince == 0f || distance < blockedDistance - StuckProgress)
        {
            blockedSince = Time.time;
            blockedDistance = distance;
            return false;
        }

        return Time.time - blockedSince > seconds;
    }

    // The flagship's owner: notes when the fight began and, when the jarl has fought long enough and still lives, blows
    // his horn once and raises one more Nail Ship for the fleet.
    private void CallReinforcement(ZDO zdo, RaiderState state, Ship target, long now)
    {
        bool fighting = state == RaiderState.Broadside || state == RaiderState.Board || state == RaiderState.Ram;
        long start = zdo.GetLong(RaiderKeys.FightStart);
        if (start == 0L)
        {
            if (fighting)
            {
                zdo.Set(RaiderKeys.FightStart, now);
            }

            return;
        }

        double minutes = (now - start) / (double)TimeSpan.TicksPerMinute;
        bool alive = jarlAlive && !zdo.GetBool(RaiderKeys.JarlFallen);
        if (!FleetRules.CallsReinforcement(state, alive, zdo.GetBool(RaiderKeys.Reinforced), minutes, FleetSettings.ReinforceMinutes.Value))
        {
            return;
        }

        zdo.Set(RaiderKeys.Reinforced, true);
        Message(HornMessage);
        RaiseReinforcement(target);
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
    // route autopilot does; the side taken last is tried first so it does not swing back and forth. With none of those
    // clear it turns about, up to 150 degrees off, at slow speed (turnAbout); with no clear way at all it backs off
    // (clear is false). The target ship's own hull does not count, as the Nail Ship is meant to come close to it.
    private float ChooseCourse(float course, Rigidbody target, out bool clear, out bool turnAbout)
    {
        clear = true;
        turnAbout = false;
        float look = assist.Lookahead();
        if (!assist.ShallowAhead() && !assist.Blocked(course, look, target))
        {
            return course;
        }

        for (float angle = 30f; angle <= 150f; angle += 30f)
        {
            foreach (int sign in new[] { avoidSign, -avoidSign })
            {
                if (!assist.Blocked(course + sign * angle, look, target))
                {
                    avoidSign = sign;
                    turnAbout = angle > 90f;
                    return course + sign * angle;
                }
            }
        }

        clear = false;
        return course;
    }

    // While boarding, lets one warrior at a time leap, or two once the world has reached the setting's key: every few
    // seconds the next living warriors get the turn. Leaving the boarding state gives the turns back.
    private void HandOutLeap(ZDO zdo, RaiderState state)
    {
        if (state != RaiderState.Board)
        {
            if (zdo.GetInt(RaiderKeys.Leap, -1) != -1 || zdo.GetInt(RaiderKeys.Leap2, -1) != -1)
            {
                zdo.Set(RaiderKeys.Leap, -1);
                zdo.Set(RaiderKeys.Leap2, -1);
            }

            lastLeap = -1;
            return;
        }

        if (Time.time < nextLeap)
        {
            return;
        }

        nextLeap = Time.time + SeaBattleSettings.BoardIntervalSeconds.Value;
        int first = NextLeap(zdo.m_uid);
        int second = Mathf.RoundToInt(TierValues.PickNumber(SeaBattleSettings.LeapTiers.Value, HasKey, 1f)) >= 2 ? NextLeap(zdo.m_uid) : -1;
        zdo.Set(RaiderKeys.Leap, first);
        zdo.Set(RaiderKeys.Leap2, second != first ? second : -1);
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

    // On every machine: shows the message to the local player if near, and sounds the jarl's horn, which is heard farther
    // than the message is shown.
    private void RPC_Message(long sender, string token)
    {
        if (token == HornMessage)
        {
            HornCaller.PlayAt(transform.position, SeaBattleSettings.SpawnDistance.Value * 2f);
        }

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
