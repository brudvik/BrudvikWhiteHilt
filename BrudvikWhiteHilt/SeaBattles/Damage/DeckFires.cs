using BrudvikWhiteHilt.Helpers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.SeaBattles.Damage;

/// <summary>
/// Fires on the deck of one ship, on every ship the game loads. A pitch pot that lands on a deck lights a fire there. The
/// fires live in the ship's ZDO as points in the ship's own space with the world time they burn out, and only the owner
/// writes them: it takes the ones that have burnt out off the list, burns them out faster in rain, and every second burns
/// the hull. Every client draws the flames from the list (<see cref="DeckFireLook"/>), and the local player is burnt where
/// they stand in one. Players put a fire out with Use, through an RPC the owner checks. A dedicated server draws nothing.
/// </summary>
public class DeckFires : MonoBehaviour
{
    private const string FireRpc = "WhiteHiltShipFire";
    private const string StampRpc = "WhiteHiltShipStamp";

    // The message RPC that ShipDamage registers on the same ship, which shows this mod's own texts to those aboard.
    private const string MessageRpc = "WhiteHiltShipMessage";
    private const string FireMessage = "$msg_whitehilt_ship_fire";

    // The owner burns the hull and the local player is burnt this often, in seconds; every client reads the ZDO this often.
    private const float TickInterval = 1f;
    private const float RefreshInterval = 0.25f;

    // A player who stamps out a fire is within this many metres of the ship, as the owner sees it, and the stamped fire is
    // found within this many metres of the point they send.
    private const float AboardRange = 15f;
    private const float StampReach = 0.75f;

    // Who throws a pot at a ship is within this many metres of it, as the owner sees it: a pot thrown at a ship lands
    // near the ship, and a Nail Ship's crew throws from where its owner's machine is.
    private const float ThrowReach = 150f;

    // The owner lights no more than one fire this often, so a client cannot fill a deck at once.
    private const float AcceptInterval = 0.2f;

    private static readonly int FiresKey = "whitehilt_ship_fires".GetStableHashCode();

    private static readonly List<DeckFires> instances = new();
    private static readonly Dictionary<Ship, DeckFires> byShip = new();
    private static int active;

    private ZNetView nview;
    private Ship ship;
    private WearNTear hull;
    private DeckFireLook look;
    private List<DeckFire> fires = new();
    private List<Vector3> points = new();
    private byte[] fireBytes;
    private uint seenRevision = uint.MaxValue;
    private bool isActive;
    private float nextRefresh;
    private float nextTick;
    private float nextBurn;
    private float nextAccept;
    private bool lookFailed;

    /// <summary>True while any loaded ship has a fire. The hooks that run every frame check this first.</summary>
    public static bool Any => active > 0;

    /// <summary>The ships' fires, for walking over without making garbage.</summary>
    public static IReadOnlyList<DeckFires> All => instances;

    /// <summary>The ship.</summary>
    public Ship Ship => ship;

    /// <summary>The fires' points, in the ship's local space, as this machine last read them.</summary>
    public IReadOnlyList<Vector3> Points => points;

    /// <summary>
    /// Adds the component to a ship. Safe to call more than once.
    /// </summary>
    /// <param name="ship">The ship.</param>
    public static void Attach(Ship ship)
    {
        if (ship.GetComponent<DeckFires>() == null)
        {
            ship.gameObject.AddComponent<DeckFires>();
        }
    }

    /// <summary>
    /// The fires of a ship.
    /// </summary>
    /// <param name="ship">The ship, or null.</param>
    /// <returns>The component, or null.</returns>
    public static DeckFires Of(Ship ship)
    {
        return ship != null && byShip.TryGetValue(ship, out DeckFires found) ? found : null;
    }

    /// <summary>
    /// Asks the ship's owner to light a fire where a pot landed.
    /// </summary>
    /// <param name="worldPoint">The landing, in world space.</param>
    public void RequestFire(Vector3 worldPoint)
    {
        if (nview != null && nview.IsValid())
        {
            nview.InvokeRPC(FireRpc, transform.InverseTransformPoint(worldPoint));
        }
    }

    /// <summary>
    /// Asks the owner to put out a fire. The stamina is taken by the caller.
    /// </summary>
    /// <param name="local">The fire's point, in the ship's local space.</param>
    public void RequestStamp(Vector3 local)
    {
        if (nview != null && nview.IsValid())
        {
            nview.InvokeRPC(StampRpc, local);
        }
    }

    /// <summary>
    /// Puts out a fire on the owner, as a Nail Ship's crew does on its own deck.
    /// </summary>
    /// <param name="index">Index of the fire in <see cref="Points"/>.</param>
    /// <returns>True if a fire was put out.</returns>
    public bool PutOutByCrew(int index)
    {
        if (!IsOwner() || index < 0 || index >= fires.Count)
        {
            return false;
        }

        List<DeckFire> next = new(fires);
        next.RemoveAt(index);
        Write(next);
        return true;
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
        instances.Add(this);
        byShip[ship] = this;
        nview.Register<Vector3>(FireRpc, RPC_Fire);
        nview.Register<Vector3>(StampRpc, RPC_Stamp);
    }

    private void OnDestroy()
    {
        instances.Remove(this);
        if (ship != null)
        {
            byShip.Remove(ship);
        }

        if (isActive)
        {
            isActive = false;
            active--;
        }

        look?.Dispose();
    }

    // Reads the ZDO a few times a second on every machine; a ship with no fire does nothing more.
    private void Update()
    {
        if (Time.time >= nextRefresh)
        {
            nextRefresh = Time.time + RefreshInterval;
            Refresh();
        }

        if (fires.Count == 0)
        {
            return;
        }

        if (Time.time >= nextTick && IsOwner())
        {
            nextTick = Time.time + TickInterval;
            OwnerTick();
        }

        if (Time.time >= nextBurn)
        {
            nextBurn = Time.time + TickInterval;
            BurnLocalPlayer();
        }
    }

    // Reads the fires when the ZDO has changed and the bytes differ from the last look, and brings the picture up to date.
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
        byte[] data = zdo.GetByteArray(FiresKey);
        if (HoleList.SameBytes(fireBytes, data))
        {
            return;
        }

        fireBytes = data;
        Accept(FireList.Decode(data));
    }

    // One second of the fires on the owner: the burnt-out ones go, rain puts the rest out faster, and each burns the hull. A
    // hull that cannot be damaged (a White Hilt ship) takes the hit as nothing, as it does any other.
    private void OwnerTick()
    {
        if (!DamageSettings.Enabled.Value)
        {
            Write(new List<DeckFire>());
            return;
        }

        List<DeckFire> next = new(fires);
        long now = ZNet.instance.GetTime().Ticks;
        if (EnvMan.instance != null && EnvMan.IsWet())
        {
            FireList.Shorten(next, FireRules.Ticks(TickInterval));
        }

        FireList.Prune(next, now);
        if (next.Count != fires.Count || !SameExpiry(next))
        {
            Write(next);
        }

        BurnHull(next.Count);
    }

    private bool SameExpiry(List<DeckFire> next)
    {
        for (int i = 0; i < next.Count; i++)
        {
            if (next[i].Expiry != fires[i].Expiry)
            {
                return false;
            }
        }

        return true;
    }

    private void BurnHull(int count)
    {
        float damage = FireRules.HullDamage(count, DamageSettings.DeckFireHullDamage.Value, TickInterval);
        if (hull == null || damage <= 0f)
        {
            return;
        }

        HitData hit = new() { m_point = transform.position, m_dir = Vector3.up, m_hitType = HitData.HitType.Burning };
        hit.m_damage.m_fire = damage;
        hull.Damage(hit);
    }

    // The local player burns while they stand in a fire on the ship they are aboard.
    private void BurnLocalPlayer()
    {
        Player player = Player.m_localPlayer;
        float damage = DamageSettings.DeckFireDamage.Value * TickInterval;
        if (player == null || player.IsDead() || damage <= 0f || !DamageSettings.Enabled.Value || !ship.IsPlayerInBoat(player))
        {
            return;
        }

        Vector3 local = transform.InverseTransformPoint(player.transform.position);
        if (!FireRules.Burns(points, local, DamageSettings.DeckFireRadius.Value))
        {
            return;
        }

        HitData hit = new() { m_point = player.transform.position, m_dir = Vector3.up, m_hitType = HitData.HitType.Burning };
        hit.m_damage.m_fire = damage;
        player.Damage(hit);
    }

    // A pot landed: the owner checks the point and the sender and lights a fire there.
    private void RPC_Fire(long sender, Vector3 local)
    {
        if (!IsOwner() || !DamageSettings.Enabled.Value || Time.time < nextAccept || !(SenderIsThisOrServer(sender) || PlayerIsNear(sender, ThrowReach)))
        {
            return;
        }

        ShipDamage damage = ShipDamage.Of(ship);
        if (damage == null || !FireRules.ValidSpot(local, damage.HullCentre, damage.HullSize))
        {
            return;
        }

        nextAccept = Time.time + AcceptInterval;
        Light(local);
    }

    // A player put a fire out: the fire nearest the point they send goes, if they are on the ship.
    private void RPC_Stamp(long sender, Vector3 local)
    {
        if (!IsOwner() || !PlayerIsNear(sender, AboardRange))
        {
            return;
        }

        int index = HoleRules.Nearest(points, local, StampReach);
        if (index < 0)
        {
            return;
        }

        List<DeckFire> next = new(fires);
        next.RemoveAt(index);
        Write(next);
    }

    // Lights a fire on the owner: it burns the set time, or feeds a fire that is already there. The crew is told when the
    // deck had none.
    private void Light(Vector3 local)
    {
        List<DeckFire> next = new(fires);
        long now = ZNet.instance.GetTime().Ticks;
        FireList.Prune(next, now);
        bool hadFire = next.Count > 0;
        long expiry = FireRules.Expiry(now, DamageSettings.DeckFireSeconds.Value);
        if (!FireList.Add(next, local, expiry, DamageSettings.MaxDeckFires.Value, FireRules.Spacing))
        {
            return;
        }

        Write(next);
        if (!hadFire)
        {
            nview.InvokeRPC(ZNetView.Everybody, MessageRpc, FireMessage);
        }
    }

    // Stores the fires in the ZDO on the owner.
    private void Write(List<DeckFire> next)
    {
        fireBytes = FireList.Encode(next);
        nview.GetZDO().Set(FiresKey, fireBytes);
        Accept(next);
    }

    // Takes a list as the fires of this machine: the points for aiming and burning, the picture, and the static count.
    private void Accept(List<DeckFire> next)
    {
        fires = next;
        points = FireList.Points(next);
        bool burning = next.Count > 0;
        if (burning != isActive)
        {
            isActive = burning;
            active += burning ? 1 : -1;
        }

        UpdateLook();
    }

    // Lights and puts out flames to match the points. A failure in the drawing is logged once and the fires go on without a
    // picture; it never reaches the rules.
    private void UpdateLook()
    {
        if (VisualHelper.IsHeadless || lookFailed)
        {
            return;
        }

        try
        {
            look ??= new DeckFireLook(transform);
            look.Sync(points);
        }
        catch (Exception ex)
        {
            lookFailed = true;
            Jotunn.Logger.LogWarning($"Deck fires: cannot draw the fires of {name}: {ex.Message}");
        }
    }

    private bool IsOwner()
    {
        return nview != null && nview.IsValid() && nview.IsOwner();
    }

    // Whether the RPC comes from this machine or from the server, whose Nail Ships throw pots when no player is near.
    private static bool SenderIsThisOrServer(long sender)
    {
        ZRoutedRpc rpc = ZRoutedRpc.instance;
        return rpc != null && (sender == rpc.m_id || sender == rpc.GetServerPeerID());
    }

    // Whether a living player owned by the sender is within range of the ship, as this machine knows the players. The id
    // in a routed RPC is not checked by the game, so this is a sanity check and not a guard against a hacked client.
    private bool PlayerIsNear(long sender, float range)
    {
        foreach (Player player in Player.GetAllPlayers())
        {
            if (player != null && player.GetOwner() == sender && !player.IsDead()
                && Vector3.Distance(player.transform.position, transform.position) <= range)
            {
                return true;
            }
        }

        return false;
    }
}
