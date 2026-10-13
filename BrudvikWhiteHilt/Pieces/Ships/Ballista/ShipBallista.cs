using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.ShipUpgrades;
using BrudvikWhiteHilt.SeaBattles.Damage;
using BrudvikWhiteHilt.SeaBattles.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Ships.Ballista;

/// <summary>
/// The ballista on the forecastle of a ship with the Ship Ballista upgrade. A player mans it with Use, aims it with the
/// camera and shoots a Ship Bolt with the attack button. The aim lives on the gunner's own player ZDO, so nobody needs to
/// own the ship to shoot and every other client can pose the ballista from it. The bolt is fired as the vanilla turret
/// fires, but with the gunner as its owner, so kills count for the gunner and damage obeys PvP rules. The gunner chooses
/// between the kinds of ammunition they carry with the secondary attack (see <see cref="RequestSwitch"/>).
/// </summary>
public class ShipBallista : MonoBehaviour, Hoverable, Interactable
{
    /// <summary>
    /// Name of the ballista object added to the ship prefab.
    /// </summary>
    public const string ObjectName = "WhiteHiltBallista";

    // The longship's bow holdfast looks out over the bow, so the gunner holds on the same way.
    private const string AttachAnimation = "attach_dragon";

    // The ballista stands on the deck this far behind the bow holdfast, found by a ray cast down from above.
    private const string HoldfastPath = "interactive/front";
    private const float SetBack = 1.5f;
    private const float RayHeight = 4f;
    private const float MaxDeckOffset = 3f;

    // The aim goes to the network this often, and other clients turn their copy this fast towards it.
    private const float AimInterval = 0.1f;
    private const float PoseSmoothing = 12f;
    private const float NeverShot = -100000f;

    // A decorative ballista turns all the way round: nobody stands behind it to be hurt by the swing of the bow.
    private const float DecorYawArc = 180f;

    // The ammunition, in the order the secondary attack cycles through it. All of it has the Ship Bolt's ammo type.
    private static readonly string[] AmmoOrder = { ShipBolts.PrefabName, BallistaSettings.ChainShotName, BallistaSettings.GrapnelBoltName };

    private static readonly KeyValuePair<int, int> ShipKey = ZDO.GetHashZDOID("whitehilt_ballista_ship");
    private static readonly int AimKey = "whitehilt_ballista_aim".GetStableHashCode();
    private static readonly int ShotKey = "whitehilt_ballista_shot".GetStableHashCode();

    // The ballista the local player mans, and whether the player does; the second stays true until the player's ZDO is cleared.
    private static ShipBallista manned;
    private static bool isManning;
    private static bool hidItems;
    private static bool fireRequested;
    private static bool switchRequested;

    // The kind of ammunition the local gunner has chosen; the Ship Bolt until they choose another.
    private static string selectedAmmo = ShipBolts.PrefabName;

    /// <summary>
    /// Where the gunner stands, behind the ballista. Set when the prefab is built.
    /// </summary>
    public Transform m_attachPoint;

    /// <summary>
    /// The part that turns with the yaw only. Set when the prefab is built.
    /// </summary>
    public Transform m_neck;

    /// <summary>
    /// The part that turns with the yaw and the pitch. Set when the prefab is built.
    /// </summary>
    public Transform m_body;

    /// <summary>
    /// Where the bolts leave, at the end of the body. Set when the prefab is built.
    /// </summary>
    public Transform m_muzzle;

    /// <summary>
    /// The loaded bolt, shown while the ballista is ready. Set when the prefab is built.
    /// </summary>
    public GameObject m_bolt;

    /// <summary>
    /// True for a ballista that is only for show, such as the Nail Ship's: nobody can man it, and it turns to the point it is
    /// given with <see cref="AimAt"/> instead of to a gunner's aim. Set when the prefab is built.
    /// </summary>
    public bool m_decor;

    private ZNetView shipView;
    private Ship ship;
    private Rigidbody shipBody;
    private Quaternion neckRest = Quaternion.identity;
    private Quaternion bodyRest = Quaternion.identity;
    private float poseYaw;
    private float posePitch;
    private float nextWrite;
    private Vector3 decorTarget;
    private bool hasDecorTarget;

    /// <summary>
    /// Whether the local player is manning a ballista now.
    /// </summary>
    public static bool LocalManned => isManning && manned != null;

    /// <summary>
    /// True when the ballista found no deck to stand on and hid itself, so whoever switches it on again should not.
    /// </summary>
    public bool PlaceFailed { get; private set; }

    /// <summary>
    /// Where the bolts leave, in world space; the ballista's own place when the model has no muzzle.
    /// </summary>
    public Vector3 MuzzlePosition => m_muzzle != null ? m_muzzle.position : transform.position;

    /// <summary>
    /// Asks the ballista the local player mans to switch to the next kind of ammunition they carry. Called from the player's
    /// controls, which would otherwise take the secondary attack button for a dismount.
    /// </summary>
    public static void RequestSwitch()
    {
        if (LocalManned)
        {
            switchRequested = true;
        }
    }

    /// <summary>
    /// Writes the line with the chosen ammunition, and how to change it, under the crosshair while the local player mans a
    /// ballista. Called after the game's crosshair update.
    /// </summary>
    /// <param name="hud">The HUD.</param>
    public static void UpdateCrosshair(Hud hud)
    {
        if (!LocalManned || Player.m_localPlayer == null)
        {
            return;
        }

        Inventory inventory = Player.m_localPlayer.GetInventory();
        string line = AmmoLine(inventory);
        if (AmmoChoice.KindsCarried(AmmoOrder, name => Carried(inventory, name) > 0) > 1)
        {
            line += "\n" + Localization.instance.Localize("[<color=yellow><b>$KEY_SecondaryAttack</b></color>] $whitehilt_ballista_switch");
        }

        ShipDamageInput.Append(hud, line);
    }

    /// <summary>
    /// Asks the ballista the local player mans to fire. Called from the player's controls, which would otherwise take
    /// the attack button for a dismount.
    /// </summary>
    public static void RequestFire()
    {
        if (LocalManned)
        {
            fireRequested = true;
        }
    }

    /// <summary>
    /// Lets go of the ballista when the local player no longer stands at it: dismounted, dead, teleported or the
    /// upgrade taken off. Called every frame for the local player.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Tick(Player player)
    {
        if (!isManning)
        {
            return;
        }

        bool attachedHere = manned != null && player.GetAttachPoint() == manned.m_attachPoint;
        if (attachedHere && manned.isActiveAndEnabled && BallistaSettings.Enabled.Value)
        {
            return;
        }

        // The ballista was switched off while the player stood at it: do not leave them held at a hidden point.
        if (attachedHere)
        {
            player.AttachStop();
        }

        Release(player);
    }

    /// <summary>
    /// Turns a decorative ballista (see <see cref="m_decor"/>) towards a point, or back to rest. Called every frame on every
    /// client by whatever owns the ballista.
    /// </summary>
    /// <param name="world">The point to aim at, in world space.</param>
    /// <param name="aiming">False to let it rest.</param>
    public void AimAt(Vector3 world, bool aiming)
    {
        decorTarget = world;
        hasDecorTarget = aiming;
    }

    /// <inheritdoc/>
    public string GetHoverText()
    {
        if (m_decor || !BallistaSettings.Enabled.Value)
        {
            return string.Empty;
        }

        string bolts = Player.m_localPlayer != null ? AmmoLine(Player.m_localPlayer.GetInventory()) : string.Empty;
        return Localization.instance.Localize($"{GetHoverName()}\n[<color=yellow><b>$KEY_Use</b></color>] $whitehilt_ballista_man\n{bolts}");
    }

    /// <inheritdoc/>
    public string GetHoverName() => Translations.Token(Translations.ItemKey(ShipBallistaUpgrade.ItemName));

    /// <inheritdoc/>
    public float GetHoverOffset() => 0f;

    /// <inheritdoc/>
    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold || m_decor || !BallistaSettings.Enabled.Value || user is not Player player || manned == this || shipView == null || !shipView.IsValid())
        {
            return false;
        }

        if (FindGunner(shipView.GetZDO().m_uid, player) != null)
        {
            player.Message(MessageHud.MessageType.Center, "$msg_blocked");
            return false;
        }

        if (player.IsEncumbered() || player.m_nview == null || !player.m_nview.IsValid())
        {
            return false;
        }

        bool hadItems = player.GetRightItem() != null || player.GetLeftItem() != null;
        player.AttachStart(m_attachPoint, null, hideWeapons: true, isBed: false, onShip: true, AttachAnimation, Vector3.zero);
        if (player.GetAttachPoint() != m_attachPoint)
        {
            return false;
        }

        hidItems = hadItems;
        manned = this;
        isManning = true;
        fireRequested = false;
        switchRequested = false;
        nextWrite = 0f;
        player.m_nview.GetZDO().Set(ShipKey, shipView.GetZDO().m_uid);
        return false;
    }

    /// <inheritdoc/>
    public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

    // Lets go of the ballista: clears the gunner's mark on their player and draws the weapons that were put away to man it.
    private static void Release(Player player)
    {
        manned = null;
        isManning = false;
        fireRequested = false;
        switchRequested = false;
        if (player.m_nview != null && player.m_nview.IsValid())
        {
            player.m_nview.GetZDO().Set(ShipKey, ZDOID.None);
        }

        if (hidItems && !player.IsDead())
        {
            player.ShowHandItems();
        }

        hidItems = false;
    }

    // The player, other than the given one, who mans the ballista of the ship with this id.
    private static Player FindGunner(ZDOID shipId, Player except)
    {
        foreach (Player player in Player.GetAllPlayers())
        {
            if (player != except && player.m_nview != null && player.m_nview.IsValid() && player.m_nview.GetZDO().GetZDOID(ShipKey) == shipId)
            {
                return player;
            }
        }

        return null;
    }

    private void Awake()
    {
        // A dedicated server draws nothing and has nobody to man it.
        if (VisualHelper.IsHeadless)
        {
            enabled = false;
            return;
        }

        shipView = GetComponentInParent<ZNetView>();
        ship = GetComponentInParent<Ship>();
        shipBody = GetComponentInParent<Rigidbody>();
        neckRest = m_neck != null ? m_neck.localRotation : Quaternion.identity;
        bodyRest = m_body != null ? m_body.localRotation : Quaternion.identity;
    }

    // Shown for the first time: finds its place on the deck, on every client. A ship where it can not be placed goes
    // without the ballista rather than with one floating in the air.
    private void Start()
    {
        if (!Place())
        {
            PlaceFailed = true;
            Jotunn.Logger.LogWarning($"{transform.root.name}: no deck found for the ship ballista, it stays hidden.");
            gameObject.SetActive(false);
        }
    }

    // Everyone turns the ballista from its gunner's aim. The local gunner aims with the camera and shoots; the others
    // read the aim and the time of the last shot from the gunner's ZDO.
    private void Update()
    {
        if (shipView == null || !shipView.IsValid())
        {
            return;
        }

        bool ready = true;
        if (m_decor)
        {
            PoseDecor();
            return;
        }

        if (isManning && manned == this && Player.m_localPlayer != null)
        {
            ready = AimAndFire(Player.m_localPlayer);
        }
        else
        {
            Player gunner = FindGunner(shipView.GetZDO().m_uid, null);
            if (gunner != null)
            {
                ZDO zdo = gunner.m_nview.GetZDO();
                Vector3 aim = zdo.GetVec3(AimKey, Vector3.zero);
                float k = 1f - Mathf.Exp(-PoseSmoothing * Time.deltaTime);
                SetPose(Mathf.LerpAngle(poseYaw, aim.x, k), Mathf.Lerp(posePitch, aim.y, k));
                ready = IsReady(zdo.GetFloat(ShotKey, NeverShot));
            }
            else
            {
                float k = 1f - Mathf.Exp(-PoseSmoothing * Time.deltaTime);
                SetPose(Mathf.LerpAngle(poseYaw, 0f, k), Mathf.Lerp(posePitch, 0f, k));
            }
        }

        if (m_bolt != null && m_bolt.activeSelf != ready)
        {
            m_bolt.SetActive(ready);
        }
    }

    // On the gunner's own client: turns the ballista to where the camera looks, within its arc, sends the aim to the
    // others a few times a second and fires on request. Returns whether the ballista is loaded.
    private bool AimAndFire(Player player)
    {
        ZNetView view = player.m_nview;
        if (view == null || !view.IsValid())
        {
            return true;
        }

        Vector3 look = GameCamera.instance != null ? GameCamera.instance.transform.forward : player.GetLookDir();
        Vector3 local = transform.InverseTransformDirection(look);
        (float yaw, float pitch) = BallistaAim.FromDirection(local.x, local.y, local.z);
        (yaw, pitch) = BallistaAim.Clamp(yaw, pitch, BallistaSettings.YawArc.Value, BallistaSettings.PitchMin.Value, BallistaSettings.PitchMax.Value);
        SetPose(yaw, pitch);

        ZDO zdo = view.GetZDO();
        if (Time.time >= nextWrite)
        {
            nextWrite = Time.time + AimInterval;
            zdo.Set(AimKey, new Vector3(yaw, pitch, 0f));
        }

        if (fireRequested)
        {
            fireRequested = false;
            TryFire(player, zdo);
        }

        if (switchRequested)
        {
            switchRequested = false;
            SwitchAmmo(player.GetInventory());
        }

        return IsReady(zdo.GetFloat(ShotKey, NeverShot));
    }

    private static bool IsReady(float shotAt)
    {
        return ZNet.instance == null || ZNet.instance.GetTimeSeconds() >= shotAt + BallistaSettings.ReloadSeconds.Value;
    }

    // Takes the chosen ammunition from the gunner's inventory and fires it, once the ballista has been reloaded. The chosen
    // kind is used if the gunner carries it, otherwise the first kind they do carry.
    private void TryFire(Player player, ZDO zdo)
    {
        if (!IsReady(zdo.GetFloat(ShotKey, NeverShot)))
        {
            return;
        }

        Inventory inventory = player.GetInventory();
        string prefab = AmmoChoice.Resolve(AmmoOrder, name => Carried(inventory, name) > 0, selectedAmmo);
        ItemDrop.ItemData ammo = prefab != null ? inventory.GetAmmoItem(ShipBolts.AmmoType, prefab) : null;
        if (ammo == null)
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_ballista_noammo");
            return;
        }

        Shoot(player, ammo);
        inventory.RemoveItem(ammo, 1);
        zdo.Set(ShotKey, (float)ZNet.instance.GetTimeSeconds());
    }

    // Moves to the next kind of ammunition the gunner carries; with one kind or none there is nothing to switch to.
    private static void SwitchAmmo(Inventory inventory)
    {
        string next = AmmoChoice.Next(AmmoOrder, name => Carried(inventory, name) > 0, selectedAmmo);
        if (next != null)
        {
            selectedAmmo = next;
        }
    }

    // How many of a kind of ammunition an inventory holds.
    private static int Carried(Inventory inventory, string prefab)
    {
        return inventory.CountItems(Translations.Token(Translations.ItemKey(prefab)));
    }

    // The line that names the ammunition a shot would use now and how many of it the gunner carries.
    private static string AmmoLine(Inventory inventory)
    {
        string prefab = AmmoChoice.Resolve(AmmoOrder, name => Carried(inventory, name) > 0, selectedAmmo) ?? selectedAmmo;
        Localization text = Localization.instance;
        return string.Format(text.Localize("$whitehilt_ballista_ammo"), text.Localize(Translations.Token(Translations.ItemKey(prefab))),
            Carried(inventory, prefab));
    }

    // Fires the bolt as the vanilla turret does (see ShipShot); the gunner owns the projectile. The shot is told which ship
    // it came from and where the ballista stands, for a grapnel bolt that must find the rope's end on this ship's rail.
    private void Shoot(Player player, ItemDrop.ItemData ammo)
    {
        Transform muzzle = m_muzzle != null ? m_muzzle : transform;
        GameObject shot = ShipShot.Fire(muzzle.position, muzzle.forward, ammo, player, -1f);
        if (shot != null && ship != null)
        {
            shot.GetComponent<ShotHits>()?.SetShooter(ship, ship.transform.InverseTransformPoint(transform.position));
        }
    }

    // A ballista for show turns all the way round towards its target, loaded, and eases back to rest without one.
    private void PoseDecor()
    {
        float yaw = 0f;
        float pitch = 0f;
        if (hasDecorTarget && m_body != null)
        {
            Vector3 local = transform.InverseTransformDirection(decorTarget - m_body.position);
            (yaw, pitch) = BallistaAim.FromDirection(local.x, local.y, local.z);
            (yaw, pitch) = BallistaAim.Clamp(yaw, pitch, DecorYawArc, BallistaSettings.PitchMin.Value, BallistaSettings.PitchMax.Value);
        }

        float k = 1f - Mathf.Exp(-PoseSmoothing * Time.deltaTime);
        SetPose(Mathf.LerpAngle(poseYaw, yaw, k), Mathf.Lerp(posePitch, pitch, k));
        if (m_bolt != null && !m_bolt.activeSelf)
        {
            m_bolt.SetActive(true);
        }
    }

    private void SetPose(float yaw, float pitch)
    {
        poseYaw = yaw;
        posePitch = pitch;
        if (m_neck != null)
        {
            m_neck.localRotation = neckRest * Quaternion.Euler(0f, yaw, 0f);
        }

        if (m_body != null)
        {
            m_body.localRotation = bodyRest * Quaternion.Euler(-pitch, yaw, 0f);
        }
    }

    // Finds the deck behind the ship's front holdfast with a ray cast down from above, onto the ship's own colliders, and
    // stands the ballista there, upright in the ship's frame. Works the same for every ship that has the holdfast.
    private bool Place()
    {
        Transform root = shipView != null ? shipView.transform : null;
        Transform holdfast = root != null ? root.Find(HoldfastPath) : null;
        if (holdfast == null || shipBody == null)
        {
            return false;
        }

        Vector3 hold = root.InverseTransformPoint(holdfast.position);
        Vector3 origin = root.TransformPoint(new Vector3(hold.x, hold.y + RayHeight, hold.z - SetBack));
        RaycastHit[] hits = Physics.RaycastAll(origin, -root.up, RayHeight * 2f + MaxDeckOffset, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits.Where(hit => hit.collider.attachedRigidbody == shipBody && !hit.collider.transform.IsChildOf(transform)))
        {
            Vector3 deck = root.InverseTransformPoint(hit.point);
            if (Vector3.Dot(hit.normal, root.up) < 0.5f || Mathf.Abs(deck.y - hold.y) > MaxDeckOffset)
            {
                continue;
            }

            transform.localPosition = deck;
            transform.localRotation = Quaternion.identity;
            return true;
        }

        return false;
    }
}
