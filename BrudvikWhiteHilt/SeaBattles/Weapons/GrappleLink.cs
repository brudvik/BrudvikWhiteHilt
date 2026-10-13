using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Pieces.Ships.Ballista;
using BrudvikWhiteHilt.SeaBattles.Damage;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.SeaBattles.Weapons;

/// <summary>
/// The grapnel rope between two ships, on every ship the game loads. The rope lives in the ZDO of the ship it was shot from,
/// the holder, as <see cref="GrappleRecord"/> bytes, and only the holder's owner writes it. Each ship's owner pulls only its
/// own body towards the other end, half the pull each, so no machine ever pushes a body it does not own: the holder's owner
/// reads its own record, and the owner of the other ship finds the record in the static list of links loaded on its machine.
/// The holder's owner also lets the rope snap when it is stretched too far, has held too long or its ship is gone. Every
/// client draws the rope, and a player cuts it with Use at its end on the holder's rail (see <see cref="GrappleInput"/>).
/// </summary>
public class GrappleLink : MonoBehaviour
{
    private const string GrappleRpc = "WhiteHiltShipGrapple";
    private const string CutRpc = "WhiteHiltShipCut";
    private const string GrappledMessage = "$msg_whitehilt_ship_grappled";
    private const string SnapMessage = "$msg_whitehilt_ship_rope_snaps";

    // Every client reads the ZDO this often, and the holder's owner checks the rope this often, in seconds.
    private const float RefreshInterval = 0.25f;

    // The rope is made fast by a player aboard the holder, at the bow ballista: within this many metres of the ship's middle,
    // as the owner sees it. A player who cuts it stands within this many metres of the ship.
    private const float GunnerReach = 25f;
    private const float CutReach = 15f;

    // The owner makes fast no more than one rope in this many seconds, so a client cannot throw ropes at once.
    private const float AcceptInterval = 1f;

    // Each of the two ships takes half the pull.
    private const float PullShare = 0.5f;

    // How the rope is drawn: its points, its width, its colour, and how far it sags when slack and when taut, in metres.
    private const int RopePoints = 16;
    private const float RopeWidth = 0.06f;
    private const float SlackSag = 0.15f;
    private const float SagPerMetre = 0.03f;
    private const float TautSag = 0.08f;
    private static readonly Color ropeColour = new(0.6f, 0.48f, 0.3f, 1f);
    private static Material ropeMaterial;

    private static readonly int GrappleKey = "whitehilt_ship_grapple".GetStableHashCode();

    private static readonly List<GrappleLink> instances = new();
    private static readonly Dictionary<Ship, GrappleLink> byShip = new();
    private static int active;

    private ZNetView nview;
    private Ship ship;
    private Rigidbody body;
    private GrappleRecord record;
    private bool holding;
    private bool isActive;
    private Ship partner;
    private byte[] recordBytes;
    private uint seenRevision = uint.MaxValue;
    private float nextRefresh;
    private float nextAccept;
    private LineRenderer rope;
    private bool lookFailed;

    /// <summary>True while any loaded ship holds a rope. The hooks that run every physics step check this first.</summary>
    public static bool Any => active > 0;

    /// <summary>The ships' links, for walking over without making garbage.</summary>
    public static IReadOnlyList<GrappleLink> All => instances;

    /// <summary>The ship this link is on.</summary>
    public Ship Ship => ship;

    /// <summary>True while this ship holds a rope, as this machine last read it.</summary>
    public bool IsHolding => holding;

    /// <summary>Where the rope is made fast on this ship, in world space; the ship's own place when it holds none.</summary>
    public Vector3 HolderPoint => holding ? transform.TransformPoint(record.Here) : transform.position;

    /// <summary>
    /// Adds the link to a ship. Safe to call more than once.
    /// </summary>
    /// <param name="ship">The ship.</param>
    public static void Attach(Ship ship)
    {
        if (ship.GetComponent<GrappleLink>() == null)
        {
            ship.gameObject.AddComponent<GrappleLink>();
        }
    }

    /// <summary>
    /// The link of a ship.
    /// </summary>
    /// <param name="ship">The ship, or null.</param>
    /// <returns>The component, or null.</returns>
    public static GrappleLink Of(Ship ship)
    {
        return ship != null && byShip.TryGetValue(ship, out GrappleLink link) ? link : null;
    }

    /// <summary>
    /// Whether a ship is held by a rope, at either end, as far as this machine knows. A Nail Ship that is bound does not flee.
    /// </summary>
    /// <param name="ship">The ship, or null.</param>
    /// <returns>True if a loaded ship holds a rope to it, or it holds one.</returns>
    public static bool IsBound(Ship ship)
    {
        if (!Any || ship == null)
        {
            return false;
        }

        for (int i = 0; i < instances.Count; i++)
        {
            GrappleLink link = instances[i];
            if (link != null && link.holding && (link.ship == ship || link.partner == ship))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Pulls a ship towards the other end of every rope it is on, after the game's physics step. Does nothing unless this
    /// machine owns the ship's body: each owner moves only its own ship.
    /// </summary>
    /// <param name="ship">The ship.</param>
    public static void Apply(Ship ship)
    {
        if (!Any)
        {
            return;
        }

        Of(ship)?.PullBody();
    }

    /// <summary>
    /// Asks the owner of this ship to make a rope fast between two places, as a grapnel bolt that hit the other ship does.
    /// </summary>
    /// <param name="other">The ship that was hit.</param>
    /// <param name="here">Where the rope is made fast on this ship, in its local space.</param>
    /// <param name="there">Where it is made fast on the other ship, in that ship's local space.</param>
    public void RequestGrapple(Ship other, Vector3 here, Vector3 there)
    {
        ZNetView otherView = other != null ? other.GetComponent<ZNetView>() : null;
        if (nview != null && nview.IsValid() && otherView != null && otherView.IsValid())
        {
            nview.InvokeRPC(GrappleRpc, otherView.GetZDO().m_uid, here, there);
        }
    }

    /// <summary>
    /// Asks the owner to cut the rope.
    /// </summary>
    public void RequestCut()
    {
        if (nview != null && nview.IsValid())
        {
            nview.InvokeRPC(CutRpc);
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

        body = GetComponent<Rigidbody>();
        instances.Add(this);
        byShip[ship] = this;
        nview.Register<ZDOID, Vector3, Vector3>(GrappleRpc, RPC_Grapple);
        nview.Register(CutRpc, RPC_Cut);
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

        if (rope != null)
        {
            Destroy(rope.gameObject);
        }
    }

    // Reads the ZDO a few times a second on every machine; the holder's owner also checks the rope then.
    private void Update()
    {
        if (Time.time < nextRefresh)
        {
            return;
        }

        nextRefresh = Time.time + RefreshInterval;
        Refresh();
        if (holding && IsOwner())
        {
            CheckRope();
        }
    }

    // Draws the rope on every machine that has both ships loaded. A failure in the drawing is logged once and the rope goes
    // on without a picture; it never reaches the physics.
    private void LateUpdate()
    {
        if (!holding || lookFailed || VisualHelper.IsHeadless)
        {
            if (rope != null && rope.gameObject.activeSelf)
            {
                rope.gameObject.SetActive(false);
            }

            return;
        }

        try
        {
            DrawRope();
        }
        catch (Exception ex)
        {
            lookFailed = true;
            Jotunn.Logger.LogWarning($"Grapnel rope: cannot draw the rope of {name}: {ex.Message}");
        }
    }

    // Reads the rope when the ZDO changed and the bytes differ from the last look, and finds the other ship among the ones
    // loaded here. The other ship can load or unload without the ZDO changing, so it is looked for every time.
    private void Refresh()
    {
        if (nview == null || !nview.IsValid())
        {
            return;
        }

        ZDO zdo = nview.GetZDO();
        if (zdo.DataRevision != seenRevision)
        {
            seenRevision = zdo.DataRevision;
            byte[] data = zdo.GetByteArray(GrappleKey);
            if (!HoleList.SameBytes(recordBytes, data))
            {
                recordBytes = data;
                holding = GrappleRecord.TryDecode(data, out record);
                partner = null;
            }
        }

        if (holding && partner == null && ZNetScene.instance != null)
        {
            GameObject found = ZNetScene.instance.FindInstance(OtherId());
            partner = found != null ? found.GetComponent<Ship>() : null;
        }

        SetActive(holding);
    }

    // The holder's owner, a few times a second: the rope snaps when it is stretched too far or has held too long, and is
    // let go when the other ship is gone from the world. A ship that is only unloaded here is judged from its ZDO.
    private void CheckRope()
    {
        Vector3 here = transform.TransformPoint(record.Here);
        float distance;
        if (partner != null)
        {
            distance = Vector3.Distance(here, partner.transform.TransformPoint(record.There));
        }
        else
        {
            ZDO other = ZDOMan.instance != null ? ZDOMan.instance.GetZDO(OtherId()) : null;
            if (other == null)
            {
                Clear(true);
                return;
            }

            distance = Vector3.Distance(here, other.GetPosition());
        }

        float age = GrappleRules.AgeSeconds(record.StartTicks, ZNet.instance.GetTime().Ticks);
        if (GrappleRules.Snaps(distance, BallistaSettings.GrappleBreak.Value, age, BallistaSettings.GrappleSeconds.Value))
        {
            Clear(true);
        }
    }

    // On the owner of the ship that holds the rope: pulls this ship's body towards the other end of the rope it holds and of
    // every rope another loaded ship holds to it.
    private void PullBody()
    {
        if (body == null || nview == null || !nview.IsValid() || !nview.IsOwner())
        {
            return;
        }

        if (holding && partner != null)
        {
            PullToward(transform.TransformPoint(record.Here), partner.transform.TransformPoint(record.There), partner.m_body);
        }

        for (int i = 0; i < instances.Count; i++)
        {
            GrappleLink other = instances[i];
            if (other != null && other != this && other.holding && other.partner == ship)
            {
                PullToward(transform.TransformPoint(other.record.There), other.transform.TransformPoint(other.record.Here), other.body);
            }
        }
    }

    // One ship's acceleration from one rope. The other ship's velocity is only what this machine knows of it: its real
    // velocity if it owns it, the velocity the network gave it if not.
    private void PullToward(Vector3 own, Vector3 target, Rigidbody otherBody)
    {
        Vector3 relative = body.linearVelocity - (otherBody != null ? otherBody.linearVelocity : Vector3.zero);
        Vector3 acceleration = GrappleRules.Acceleration(target - own, relative, BallistaSettings.GrappleLength.Value,
            BallistaSettings.GrapplePull.Value, BallistaSettings.GrappleDamping.Value, PullShare);
        if (acceleration != Vector3.zero)
        {
            body.AddForce(acceleration, ForceMode.Acceleration);
        }
    }

    // Draws the rope between its two ends, hanging a little when slack and nearly straight when taut.
    private void DrawRope()
    {
        if (partner == null)
        {
            if (rope != null && rope.gameObject.activeSelf)
            {
                rope.gameObject.SetActive(false);
            }

            return;
        }

        rope ??= CreateRope();
        if (rope == null)
        {
            lookFailed = true;
            return;
        }

        if (!rope.gameObject.activeSelf)
        {
            rope.gameObject.SetActive(true);
        }

        Vector3 from = transform.TransformPoint(record.Here);
        Vector3 to = partner.transform.TransformPoint(record.There);
        float distance = Vector3.Distance(from, to);
        float sag = distance > BallistaSettings.GrappleLength.Value ? TautSag : SlackSag + SagPerMetre * distance;
        for (int i = 0; i < RopePoints; i++)
        {
            float t = (float)i / (RopePoints - 1);
            rope.SetPosition(i, Vector3.Lerp(from, to, t) + Vector3.down * (sag * 4f * t * (1f - t)));
        }
    }

    // The rope's line renderer, with one shared plain-coloured material; null when the shader is missing.
    private static LineRenderer CreateRope()
    {
        if (ropeMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                return null;
            }

            ropeMaterial = new Material(shader) { name = "whitehilt_grapnelrope", color = ropeColour };
        }

        GameObject line = new("WhiteHiltGrapnelRope");
        LineRenderer renderer = line.AddComponent<LineRenderer>();
        renderer.sharedMaterial = ropeMaterial;
        renderer.positionCount = RopePoints;
        renderer.startWidth = RopeWidth;
        renderer.endWidth = RopeWidth;
        renderer.useWorldSpace = true;
        renderer.numCapVertices = 2;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return renderer;
    }

    private ZDOID OtherId()
    {
        return new ZDOID(record.OtherUser, record.OtherId);
    }

    private bool IsOwner()
    {
        return nview != null && nview.IsValid() && nview.IsOwner();
    }

    private void SetActive(bool value)
    {
        if (value == isActive)
        {
            return;
        }

        isActive = value;
        active += value ? 1 : -1;
    }

    // Lets the rope go on the owner, and tells both crews when it snapped.
    private void Clear(bool snapped)
    {
        nview.GetZDO().Set(GrappleKey, GrappleRecord.None);
        recordBytes = GrappleRecord.None;
        holding = false;
        SetActive(false);
        if (snapped)
        {
            Tell(SnapMessage, partner);
        }

        partner = null;
    }

    // Shows a text to the players aboard both ships.
    private void Tell(string token, Ship other)
    {
        ShipDamage.Of(ship)?.Tell(token);
        ShipDamage.Of(other)?.Tell(token);
    }

    // A grapnel bolt hit: the owner checks the sender, that the other ship is loaded here and that both places are on their
    // ships and within the rope's reach, and then makes the rope fast. A rope that is there is replaced.
    private void RPC_Grapple(long sender, ZDOID other, Vector3 here, Vector3 there)
    {
        if (!IsOwner() || !BallistaSettings.Enabled.Value || Time.time < nextAccept || other.IsNone() || ZNetScene.instance == null
            || !ShipDamage.SenderNear(sender, transform.position, GunnerReach))
        {
            return;
        }

        GameObject found = ZNetScene.instance.FindInstance(other);
        Ship target = found != null ? found.GetComponent<Ship>() : null;
        if (target == null || target == ship || !OnShip(ship, here) || !OnShip(target, there))
        {
            return;
        }

        float distance = Vector3.Distance(transform.TransformPoint(here), target.transform.TransformPoint(there));
        if (distance > BallistaSettings.GrappleBreak.Value)
        {
            return;
        }

        nextAccept = Time.time + AcceptInterval;
        record = new GrappleRecord(other.UserID, other.ID, here, there, ZNet.instance.GetTime().Ticks);
        recordBytes = record.Encode();
        nview.GetZDO().Set(GrappleKey, recordBytes);
        holding = true;
        partner = target;
        SetActive(true);
        Tell(GrappledMessage, target);
    }

    // A player cut the rope: it goes if the player is near the ship it is made fast to.
    private void RPC_Cut(long sender)
    {
        if (!IsOwner() || !holding || !ShipDamage.SenderNear(sender, transform.position, CutReach))
        {
            return;
        }

        Clear(false);
    }

    // Whether a point sent over the network is a place on a ship.
    private static bool OnShip(Ship target, Vector3 local)
    {
        ShipDamage damage = ShipDamage.Of(target);
        return damage != null && FireRules.ValidSpot(local, damage.HullCentre, damage.HullSize);
    }
}
