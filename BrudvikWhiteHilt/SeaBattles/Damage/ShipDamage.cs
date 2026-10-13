using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Pieces.Ships.Ballista;
using BrudvikWhiteHilt.SeaBattles.Weapons;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.SeaBattles.Damage;

/// <summary>
/// Holes and flooding on one ship, on every ship the game loads. The holes and the flood live in the ship's ZDO and only
/// its owner writes them: a heavy hit in a sea battle (see <see cref="TryHit"/>) makes a hole at the waterline, every open
/// hole lets water in, and the water slows the ship, lowers it and, if it is not indestructible, finally sinks it. Players
/// patch holes and bail water through RPCs to the owner, which checks them. Every client draws the holes and the water
/// from the ZDO (<see cref="ShipDamageLook"/>), and a dedicated server draws nothing. The ship's torn sail (a chain shot hit
/// it, see <see cref="RequestTear"/>) is kept here too: it takes force out of the sail like water does.
/// </summary>
public class ShipDamage : MonoBehaviour
{
    private const string PatchRpc = "WhiteHiltShipPatch";
    private const string BailRpc = "WhiteHiltShipBail";
    private const string TearRpc = "WhiteHiltShipTear";
    private const string MessageRpc = "WhiteHiltShipMessage";
    private const string MessagePrefix = "$msg_whitehilt_ship_";

    // The owner steps the flood this often, in seconds; every client reads the ZDO this often.
    private const float StepInterval = 0.5f;
    private const float RefreshInterval = 0.25f;

    // A step never covers more than this many seconds, so a ship loaded after a long time does not flood at once.
    private const float MaxStepSeconds = 60f;

    // A player who patches or bails is within this many metres of the ship, as the owner sees it.
    private const float AboardRange = 15f;

    // The owner accepts bails no faster than this share of the bail delay, so a client cannot empty the ship at once.
    private const float BailRateShare = 0.5f;

    // A chain shot's tear is accepted from a player within this many metres of the ship, as the owner sees it: the bolt flies
    // from a ship alongside, and no more often than this many seconds apart.
    private const float TearReach = 150f;
    private const float TearInterval = 0.2f;

    // The hull's width is read from mesh points this far above and below the height asked, and this far along the hull.
    // A narrow band follows the flare of the hull; the wider ones are tried when the mesh has no points in a narrow one.
    private static readonly float[] WidthBandsY = { 0.1f, 0.35f, 1f };
    private const float WidthBandZ = 0.6f;

    // The flare is the difference of two widths this far above and below the height asked.
    private const float FlareStep = 0.2f;

    private const string HullMeshPath = "ship/visual/hull_new/hull";

    private static readonly int HolesKey = "whitehilt_ship_holes".GetStableHashCode();
    private static readonly int FloodKey = "whitehilt_ship_flood".GetStableHashCode();
    private static readonly int TickKey = "whitehilt_ship_floodtick".GetStableHashCode();
    private static readonly int TornKey = "whitehilt_ship_torn".GetStableHashCode();

    private static readonly List<ShipDamage> instances = new();
    private static readonly Dictionary<Ship, ShipDamage> byShip = new();
    private static readonly Dictionary<WearNTear, ShipDamage> byHull = new();
    private static readonly HashSet<string> measured = new();
    private static int active;

    private ZNetView nview;
    private Ship ship;
    private WearNTear hull;
    private Rigidbody body;
    private ShipDamageLook look;
    private float baseOffset;
    private List<Vector3> holes = new();
    private byte[] holeBytes;
    private float flood;
    private uint seenRevision = uint.MaxValue;
    private bool isActive;
    private bool sinkingAnnounced;
    private float nextRefresh;
    private float nextStep;
    private float nextBail;
    private float nextTear;
    private long tornUntil;
    private bool torn;
    private bool hullMeasured;
    private Vector3 hullCentre;
    private Vector3 hullSize;
    private Vector3[] hullPoints;
    private bool hullPointsRead;
    private bool lookFailed;

    /// <summary>True while any loaded ship has a hole, water or a torn sail. The hooks that run every physics step check this first.</summary>
    public static bool Any => active > 0;

    /// <summary>True while any ship has this component, so a hit on a hull is worth looking at.</summary>
    public static bool Loaded => byHull.Count > 0;

    /// <summary>The ships' damage, for walking over without making garbage.</summary>
    public static IReadOnlyList<ShipDamage> All => instances;

    /// <summary>The ship.</summary>
    public Ship Ship => ship;

    /// <summary>The ship's rigidbody.</summary>
    public Rigidbody Body => body;

    /// <summary>The share of a full ship that is water, 0 to 1, as this machine last read it.</summary>
    public float Flood => flood;

    /// <summary>The holes, in the ship's local space.</summary>
    public IReadOnlyList<Vector3> Holes => holes;

    /// <summary>True while a chain shot's tear is on the ship's sail, as this machine last read it.</summary>
    public bool IsTorn => torn;

    /// <summary>True for a ship that cannot be destroyed: it floods and slows but never sinks or loses health.</summary>
    public bool IsImmune => hull != null && hull.m_damages.m_blunt == HitData.DamageModifier.Immune;

    /// <summary>Centre of the hull in the ship's local space, from the box that makes the ship float.</summary>
    public Vector3 HullCentre
    {
        get
        {
            MeasureHull();
            return hullCentre;
        }
    }

    /// <summary>Size of the float box: x across, y up, z along.</summary>
    public Vector3 HullSize
    {
        get
        {
            MeasureHull();
            return hullSize;
        }
    }

    /// <summary>
    /// Adds the component to a ship. Safe to call more than once.
    /// </summary>
    /// <param name="ship">The ship.</param>
    public static void Attach(Ship ship)
    {
        if (ship.GetComponent<ShipDamage>() == null)
        {
            ship.gameObject.AddComponent<ShipDamage>();
        }
    }

    /// <summary>
    /// The damage of a ship.
    /// </summary>
    /// <param name="ship">The ship, or null.</param>
    /// <returns>The component, or null.</returns>
    public static ShipDamage Of(Ship ship)
    {
        return ship != null && byShip.TryGetValue(ship, out ShipDamage damage) ? damage : null;
    }

    /// <summary>
    /// The damage of the ship whose hull this is.
    /// </summary>
    /// <param name="hull">The hull, or null.</param>
    /// <returns>The component, or null for anything that is not a ship's hull.</returns>
    public static ShipDamage Of(WearNTear hull)
    {
        return hull != null && byHull.TryGetValue(hull, out ShipDamage damage) ? damage : null;
    }

    /// <summary>
    /// The water in a ship as a line for the HUD.
    /// </summary>
    /// <param name="ship">The ship.</param>
    /// <returns>The text, or null for a dry ship.</returns>
    public static string FloodLine(Ship ship)
    {
        ShipDamage damage = Any ? Of(ship) : null;
        int percent = damage != null ? FloodModel.Percent(damage.flood) : 0;
        return percent > 0 ? string.Format(Localization.instance.Localize("$whitehilt_ship_flood_hud"), percent) : null;
    }

    /// <summary>
    /// Makes a hole if a hit on the hull is heavy enough in a sea battle, or is a ram. Called on the hull's owner from the
    /// hit, before the game works out what the hull's resistances leave of it; it changes nothing about the hit.
    /// </summary>
    /// <param name="hit">The hit.</param>
    public void TryHit(HitData hit)
    {
        if (!DamageSettings.Enabled.Value || !IsOwner() || IgnoresHit(hit.m_hitType))
        {
            return;
        }

        float physical = hit.m_damage.m_blunt + hit.m_damage.m_pierce + hit.m_damage.m_slash + hit.m_damage.m_chop;
        bool ram = hit.m_hitType == HitData.HitType.Boat;

        // Whether a fight is near is only worked out for a hit that is heavy enough to matter.
        if (!ram && physical < DamageSettings.HoleMinDamage.Value)
        {
            return;
        }

        if (HoleRules.MakesHole(physical, DamageSettings.HoleMinDamage.Value, ram || InBattle(), ram))
        {
            AddHole(hit.m_point == Vector3.zero ? transform.position : hit.m_point);
        }
    }

    /// <summary>
    /// Makes a hole in the hull's side at the waterline, near a world point. Only the owner does it. A hole too near an
    /// old one is the same hole, and the ship has no more than the setting allows.
    /// </summary>
    /// <param name="worldPoint">Where the hull was hit.</param>
    /// <returns>True if there is a new hole.</returns>
    public bool AddHole(Vector3 worldPoint)
    {
        if (!IsOwner() || !DamageSettings.Enabled.Value || holes.Count >= DamageSettings.MaxHoles.Value)
        {
            return false;
        }

        Vector3 placed = PlaceHole(transform.InverseTransformPoint(worldPoint));
        if (HoleRules.Nearest(holes, placed, HoleRules.Spacing) >= 0)
        {
            return false;
        }

        WriteHoles(new List<Vector3>(holes) { placed });
        ZDO zdo = nview.GetZDO();
        if (zdo.GetLong(TickKey) == 0L)
        {
            zdo.Set(TickKey, ZNet.instance.GetTime().Ticks);
        }

        Message(MessagePrefix + "holed");
        return true;
    }

    /// <summary>
    /// Asks the owner to patch a hole. The patch material is taken by the caller.
    /// </summary>
    /// <param name="index">Index of the hole.</param>
    public void RequestPatch(int index)
    {
        if (nview != null && nview.IsValid())
        {
            nview.InvokeRPC(PatchRpc, index);
        }
    }

    /// <summary>
    /// Asks the owner to bail water. The stamina is taken by the caller.
    /// </summary>
    public void RequestBail()
    {
        if (nview != null && nview.IsValid())
        {
            nview.InvokeRPC(BailRpc);
        }
    }

    /// <summary>
    /// Sets the ship's depth in the water from its flood. Called after the game's physics step on every machine; only the
    /// owner's physics reads the depth.
    /// </summary>
    public void ApplyBuoyancy()
    {
        float depth = baseOffset - FloodModel.Offset(flood, DamageSettings.SinkDepth.Value);
        if (ship.m_waterLevelOffset != depth)
        {
            ship.m_waterLevelOffset = depth;
        }
    }

    /// <summary>
    /// Asks the owner to tear the ship's sail, as a chain shot that hit the ship does.
    /// </summary>
    public void RequestTear()
    {
        if (nview != null && nview.IsValid())
        {
            nview.InvokeRPC(TearRpc);
        }
    }

    /// <summary>
    /// Shows a text of this mod's ship messages to the players aboard this ship, on every machine that has it.
    /// </summary>
    /// <param name="token">The text's token, which must start with <c>$msg_whitehilt_ship_</c>.</param>
    public void Tell(string token)
    {
        if (nview != null && nview.IsValid())
        {
            nview.InvokeRPC(ZNetView.Everybody, MessageRpc, token);
        }
    }

    /// <summary>
    /// Whether a living player owned by the sender of an RPC is within range of a place, as this machine knows the players.
    /// The id in a routed RPC is not checked by the game, so this is a sanity check and not a guard against a hacked client.
    /// </summary>
    /// <param name="sender">The peer that sent the RPC.</param>
    /// <param name="position">The place.</param>
    /// <param name="range">Metres.</param>
    /// <returns>True if such a player is near.</returns>
    public static bool SenderNear(long sender, Vector3 position, float range)
    {
        foreach (Player player in Player.GetAllPlayers())
        {
            if (player != null && player.GetOwner() == sender && !player.IsDead()
                && Vector3.Distance(player.transform.position, position) <= range)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The factor the sail force is multiplied by while the ship holds water or its sail is torn.
    /// </summary>
    /// <returns>1 for a dry ship with a whole sail, less otherwise.</returns>
    public float SailFactor()
    {
        return FloodModel.SailFactor(flood, DamageSettings.FloodSpeedLoss.Value)
            * WeaponRules.TornFactor(torn, BallistaSettings.SailTearShare.Value);
    }

    /// <summary>
    /// The distance from the ship's middle to the hull's side at a place, read from the hull's mesh where the game's own
    /// ship has one, otherwise half the float box.
    /// </summary>
    /// <param name="side">1 for starboard, -1 for port.</param>
    /// <param name="y">Height, in the ship's local space.</param>
    /// <param name="z">Place along the ship, in its local space.</param>
    /// <returns>Metres.</returns>
    public float HalfWidth(float side, float y, float z)
    {
        Vector3[] points = HullPoints();
        foreach (float band in WidthBandsY)
        {
            float width = HullShape.HalfWidth(points, side, y, band, z, WidthBandZ, 0f);
            if (width > 0f)
            {
                return width;
            }
        }

        return HullSize.x / 2f;
    }

    /// <summary>
    /// How much the hull's side leans outward with height at a place, in metres of width per metre up; 0 for a ship without a
    /// hull mesh to read. A mark on the hull is tilted by it.
    /// </summary>
    /// <param name="side">1 for starboard, -1 for port.</param>
    /// <param name="y">Height, in the ship's local space.</param>
    /// <param name="z">Place along the ship, in its local space.</param>
    /// <returns>The lean, never negative.</returns>
    public float Flare(float side, float y, float z)
    {
        return HullShape.Flare(HullPoints(), side, y, FlareStep, WidthBandsY[0], z, WidthBandZ);
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

        hull = GetComponent<WearNTear>();
        body = GetComponent<Rigidbody>();
        baseOffset = ship.m_waterLevelOffset;
        instances.Add(this);
        byShip[ship] = this;
        if (hull != null)
        {
            byHull[hull] = this;
        }

        nview.Register<int>(PatchRpc, RPC_Patch);
        nview.Register(BailRpc, RPC_Bail);
        nview.Register(TearRpc, RPC_Tear);
        nview.Register<string>(MessageRpc, RPC_Message);
    }

    private void OnDestroy()
    {
        instances.Remove(this);
        if (ship != null)
        {
            byShip.Remove(ship);
        }

        if (hull != null)
        {
            byHull.Remove(hull);
        }

        if (isActive)
        {
            isActive = false;
            active--;
        }

        look?.Dispose();
    }

    // Reads the ZDO a few times a second on every machine, and on the owner steps the flood twice a second.
    private void Update()
    {
        if (Time.time >= nextRefresh)
        {
            nextRefresh = Time.time + RefreshInterval;
            Refresh();
            CheckTorn();
        }

        if (Time.time >= nextStep && IsOwner())
        {
            nextStep = Time.time + StepInterval;
            Step();
        }
    }

    // Reads the holes and the flood when the ZDO has changed since the last look, and brings the picture up to date.
    // Other things in the ZDO change it all the time, so the holes are compared byte for byte before they are drawn again.
    private void Refresh()
    {
        if (nview == null || !nview.IsValid())
        {
            return;
        }

        ZDO zdo = nview.GetZDO();
        if (zdo.DataRevision == seenRevision)
        {
            return;
        }

        seenRevision = zdo.DataRevision;
        byte[] data = zdo.GetByteArray(HolesKey);
        bool holesChanged = !HoleList.SameBytes(holeBytes, data);
        if (holesChanged)
        {
            holeBytes = data;
            holes = HoleList.Decode(data);
        }

        flood = zdo.GetFloat(FloodKey);
        tornUntil = zdo.GetLong(TornKey);
        CheckTorn();
        UpdateActive();
        UpdateLook(holesChanged);
    }

    // Whether the sail is torn now. The tear ends by the clock, which changes nothing in the ZDO, so this runs on its own.
    private void CheckTorn()
    {
        bool now = tornUntil != 0L && ZNet.instance != null && WeaponRules.IsTorn(tornUntil, ZNet.instance.GetTime().Ticks);
        if (now != torn)
        {
            torn = now;
            UpdateActive();
        }
    }

    // The ship needs the hooks that run every physics step while it has a hole, water or a torn sail.
    private void UpdateActive()
    {
        SetActive(holes.Count > 0 || flood > 0f || torn);
    }

    // One flood step on the owner: holes let water in, no holes let it drain, and a full ship that can be destroyed takes
    // damage. The time since the last step is world time kept in the ZDO, so it carries over a change of owner.
    private void Step()
    {
        ZDO zdo = nview.GetZDO();
        if (!DamageSettings.Enabled.Value && holes.Count > 0)
        {
            WriteHoles(new List<Vector3>());
        }

        float current = zdo.GetFloat(FloodKey);
        long last = zdo.GetLong(TickKey);
        if (holes.Count == 0 && current <= 0f)
        {
            if (last != 0L)
            {
                zdo.Set(TickKey, 0L);
            }

            return;
        }

        long now = ZNet.instance.GetTime().Ticks;
        float seconds = FloodModel.ElapsedSeconds(last, now, MaxStepSeconds);
        flood = FloodModel.Step(current, holes.Count, seconds, DamageSettings.FloodPerHole.Value, DamageSettings.DrainPerSecond.Value);
        zdo.Set(FloodKey, flood);
        zdo.Set(TickKey, now);
        UpdateActive();
        Sink(seconds);
    }

    // A full ship that is not indestructible takes damage until it breaks up; the crew is told once.
    private void Sink(float seconds)
    {
        if (flood < 1f)
        {
            sinkingAnnounced = false;
            return;
        }

        if (hull == null || IsImmune)
        {
            return;
        }

        if (!sinkingAnnounced)
        {
            sinkingAnnounced = true;
            Message(MessagePrefix + "sinking");
        }

        float damage = FloodModel.HullDamage(flood, DamageSettings.FloodDamagePerSecond.Value, seconds);
        if (damage > 0f)
        {
            hull.ApplyDamage(damage);
        }
    }

    // Draws the holes again when they changed and moves the water. A failure in the drawing is logged once and the ship
    // goes on without a picture; it never reaches the physics.
    private void UpdateLook(bool holesChanged)
    {
        if (VisualHelper.IsHeadless || lookFailed)
        {
            return;
        }

        try
        {
            look ??= new ShipDamageLook(this);
            if (holesChanged)
            {
                look.Rebuild(holes);
            }

            look.SetFlood(flood);
        }
        catch (Exception ex)
        {
            lookFailed = true;
            Jotunn.Logger.LogWarning($"Ship damage: cannot draw the holes of {name}: {ex.Message}");
        }
    }

    private void SetActive(bool value)
    {
        if (value == isActive)
        {
            return;
        }

        isActive = value;
        active += value ? 1 : -1;
        if (!value && ship != null)
        {
            ship.m_waterLevelOffset = baseOffset;
        }
    }

    private bool IsOwner()
    {
        return nview != null && nview.IsValid() && nview.IsOwner();
    }

    private void WriteHoles(List<Vector3> next)
    {
        nview.GetZDO().Set(HolesKey, HoleList.Encode(next));
        holes = next;
        UpdateActive();
    }

    // Only a Nail Ship, or a ship within reach of one that is still fighting, is in battle.
    private bool InBattle()
    {
        return RaiderShip.Is(ship) || RaiderShip.AnyNear(transform.position, DamageSettings.BattleRange.Value);
    }

    // The hit types that are not a blow to the hull: fire, water, poison, the ash ocean and the like.
    private static bool IgnoresHit(HitData.HitType type)
    {
        switch (type)
        {
            case HitData.HitType.Fall:
            case HitData.HitType.Drowning:
            case HitData.HitType.Burning:
            case HitData.HitType.Freezing:
            case HitData.HitType.Poisoned:
            case HitData.HitType.Water:
            case HitData.HitType.Smoke:
            case HitData.HitType.EdgeOfWorld:
            case HitData.HitType.Self:
            case HitData.HitType.Structural:
            case HitData.HitType.CinderFire:
            case HitData.HitType.AshlandsOcean:
            case HitData.HitType.AshlandsLava:
            case HitData.HitType.Incinerator:
                return true;
            default:
                return false;
        }
    }

    // Where a hole goes, in the ship's space: on the side that was hit, at the waterline, in the hull's side where the hull
    // really is. The first hole of each kind of ship logs the numbers it used.
    private Vector3 PlaceHole(Vector3 local)
    {
        Vector3 centre = HullCentre;
        Vector3 size = HullSize;
        float waterline = centre.y;
        float side = local.x >= 0f ? 1f : -1f;
        float half = HalfWidth(side, waterline + HoleRules.AboveWaterline, local.z);
        LogGeometry(centre, size, waterline, half);
        return HoleRules.Place(local, new Vector3(0f, centre.y, centre.z), new Vector3(half * 2f, size.y, size.z), waterline);
    }

    private void LogGeometry(Vector3 centre, Vector3 size, float waterline, float half)
    {
        string prefab = Utils.GetPrefabName(gameObject);
        if (measured.Add(prefab))
        {
            Jotunn.Logger.LogInfo($"Ship damage: {prefab}: float box centre {centre}, size {size}, waterline y {waterline:0.00}, "
                + $"base depth offset {baseOffset:0.00}, hull half width {half:0.00} at the waterline "
                + (HullPoints() != null ? "(from the hull mesh)" : "(from the float box)"));
        }
    }

    // The float box in the ship's space.
    private void MeasureHull()
    {
        if (hullMeasured)
        {
            return;
        }

        hullMeasured = true;
        BoxCollider box = ship != null ? ship.m_floatCollider : null;
        if (box == null)
        {
            hullCentre = Vector3.zero;
            hullSize = new Vector3(5f, 1f, 15f);
            return;
        }

        Vector3 scale = box.transform.lossyScale;
        hullCentre = transform.InverseTransformPoint(box.transform.TransformPoint(box.center));
        hullSize = new Vector3(Mathf.Abs(box.size.x * scale.x), Mathf.Abs(box.size.y * scale.y), Mathf.Abs(box.size.z * scale.z));
    }

    // The points of the longship hull's mesh in the ship's space, read once when first needed. Null for a ship without that
    // mesh or with one the game has not left readable (the float box is used then).
    private Vector3[] HullPoints()
    {
        if (hullPointsRead)
        {
            return hullPoints;
        }

        hullPointsRead = true;
        try
        {
            MeshFilter filter = transform.Find(HullMeshPath)?.GetComponent<MeshFilter>();
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            if (mesh != null && mesh.isReadable)
            {
                Vector3[] vertices = mesh.vertices;
                hullPoints = new Vector3[vertices.Length];
                for (int i = 0; i < vertices.Length; i++)
                {
                    hullPoints[i] = transform.InverseTransformPoint(filter.transform.TransformPoint(vertices[i]));
                }
            }
        }
        catch (Exception ex)
        {
            hullPoints = null;
            Jotunn.Logger.LogWarning($"Ship damage: cannot read the hull mesh of {name}: {ex.Message}");
        }

        return hullPoints;
    }

    // Whether a player who sent an RPC is a player near the ship, as this machine knows the players. The id in a routed
    // RPC is not checked by the game, so this is a sanity check and not a guard against a hacked client.
    private bool PlayerIsNear(long sender)
    {
        return SenderNear(sender, transform.position, AboardRange);
    }

    // A chain shot hit the ship: the sail is torn for the set time, and a sail that was whole tells the crew. The owner checks
    // that the sender is a player and that the hits do not come faster than a ballista could shoot them.
    private void RPC_Tear(long sender)
    {
        if (!IsOwner() || !BallistaSettings.Enabled.Value || Time.time < nextTear || !SenderNear(sender, transform.position, TearReach))
        {
            return;
        }

        nextTear = Time.time + TearInterval;
        bool wasTorn = torn;
        tornUntil = FireRules.Expiry(ZNet.instance.GetTime().Ticks, BallistaSettings.SailTearSeconds.Value);
        nview.GetZDO().Set(TornKey, tornUntil);
        CheckTorn();
        if (!wasTorn && torn)
        {
            Message(MessagePrefix + "torn");
        }
    }

    // A player patched a hole: it is gone, and a destructible hull mends a share of its health.
    private void RPC_Patch(long sender, int index)
    {
        if (!IsOwner() || !HoleRules.ValidIndex(index, holes.Count) || !PlayerIsNear(sender))
        {
            return;
        }

        List<Vector3> next = new(holes);
        next.RemoveAt(index);
        WriteHoles(next);
        Mend();
    }

    // A player bailed: water leaves, and the next bail must wait for the delay.
    private void RPC_Bail(long sender)
    {
        if (!IsOwner() || Time.time < nextBail || !PlayerIsNear(sender))
        {
            return;
        }

        nextBail = Time.time + DamageSettings.BailSeconds.Value * BailRateShare;
        ZDO zdo = nview.GetZDO();
        flood = Mathf.Max(0f, zdo.GetFloat(FloodKey) - DamageSettings.BailAmount.Value);
        zdo.Set(FloodKey, flood);
        UpdateActive();
        if (flood < 1f)
        {
            sinkingAnnounced = false;
        }
    }

    // Shown to the players aboard on every machine that has the ship. Only this mod's own texts are shown.
    private void RPC_Message(long sender, string token)
    {
        Player player = Player.m_localPlayer;
        if (player != null && ship != null && token != null && token.StartsWith(MessagePrefix, StringComparison.Ordinal) && ship.IsPlayerInBoat(player))
        {
            player.Message(MessageHud.MessageType.Center, token);
        }
    }

    private void Message(string token)
    {
        nview.InvokeRPC(ZNetView.Everybody, MessageRpc, token);
    }

    // The hull's health as RPC_Repair writes it, but only the share a patch mends; an indestructible hull has none to mend.
    private void Mend()
    {
        if (hull == null || IsImmune)
        {
            return;
        }

        ZDO zdo = nview.GetZDO();
        float health = zdo.GetFloat(ZDOVars.s_health, hull.m_health);
        float mended = HoleRules.Mend(health, hull.m_health, DamageSettings.PatchRepair.Value);
        if (mended > health)
        {
            zdo.Set(ZDOVars.s_health, mended);
            nview.InvokeRPC(ZNetView.Everybody, "RPC_HealthChanged", mended);
        }
    }
}
