using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.ShipUpgrades;
using BrudvikWhiteHilt.Pieces.Ships.WhiteHiltShip;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Ships.Skidbladnir;

/// <summary>Skidbladnir's speed limit, sail presentation and movable building surface.</summary>
public class SkidbladnirShip : MonoBehaviour
{
    /// <summary>Sails pivoting at their individual yards.</summary>
    public Transform[] m_sails = Array.Empty<Transform>();
    /// <summary>Visual fishing net switched with its existing upgrade bit.</summary>
    public GameObject m_fishingNet;

    private static readonly List<SkidbladnirShip> instances = new();

    private Ship ship;
    private Rigidbody body;
    private WhiteHiltShipUpgrades upgrades;
    private float sailSize = 1f;

    /// <summary>Whether the ship is still enough for ordinary hammer placement.</summary>
    public bool CanBuild => SkidbladnirSettings.Building.Value && body != null
        && new Vector2(body.linearVelocity.x, body.linearVelocity.z).magnitude <= SkidbladnirSettings.BuildMaxSpeed.Value
        && (upgrades == null || !upgrades.Has(ShipDriftAnchor.Bit) || upgrades.IsAnchored);

    /// <summary>Ideal full-sail tailwind speed computed from the existing ship's propulsion and drag.</summary>
    /// <param name="sailForce">Full-sail propulsion per physics step.</param>
    /// <param name="forwardDamping">Quadratic water drag coefficient.</param>
    /// <param name="buoyancy">Buoyancy acceleration coefficient.</param>
    /// <param name="linearDrag">Rigidbody drag coefficient.</param>
    /// <param name="step">Physics timestep.</param>
    /// <returns>Reference terminal speed on flat water.</returns>
    public static float ReferenceSpeed(float sailForce, float forwardDamping, float buoyancy, float linearDrag, float step)
    {
        float immersed = Mathf.Clamp01(Mathf.Abs(Physics.gravity.y) / Mathf.Max(0.001f, buoyancy * 50f));
        float low = 0f;
        float high = 128f;
        for (int iteration = 0; iteration < 40; iteration++)
        {
            float speed = (low + high) / 2f;
            float retention = Mathf.Max(0.001f, 1f - linearDrag * step);
            float loss = Mathf.Min(1f, speed * speed * forwardDamping * immersed) + speed * (1f / retention - 1f);
            if (loss < sailForce * 0.7f) low = speed;
            else high = speed;
        }
        return (low + high) / 2f;
    }

    /// <summary>Finds the loaded sailing home whose lower deck contains a point.</summary>
    /// <param name="point">World position, e.g. a player's eye.</param>
    /// <returns>The ship, or null when the point is not below any deck.</returns>
    public static SkidbladnirShip BelowDeck(Vector3 point)
    {
        foreach (SkidbladnirShip candidate in instances)
            if (candidate != null && SkidbladnirModel.LowerDeck.Contains(candidate.transform.InverseTransformPoint(point)))
                return candidate;
        return null;
    }

    /// <summary>Caps horizontal motion without interfering with waves or Kraken lift.</summary>
    public void LimitSpeed()
    {
        ZNetView view = GetComponent<ZNetView>();
        if (view == null || !view.IsValid() || !view.IsOwner() || body == null) return;
        Ship reference = PrefabManager.Instance.GetPrefab("WhiteHiltShip")?.GetComponent<Ship>() ?? ship;
        if (reference == null) return;
        Rigidbody referenceBody = reference.GetComponent<Rigidbody>();
        float maximum = ReferenceSpeed(ShipSettings.SailForce.Value, reference.m_dampingForward, reference.m_force,
            referenceBody != null ? referenceBody.linearDamping : 0f, Time.fixedDeltaTime) * SkidbladnirSettings.SpeedShare.Value;
        Vector3 velocity = body.linearVelocity;
        Vector2 horizontal = Vector2.ClampMagnitude(new Vector2(velocity.x, velocity.z), maximum);
        body.linearVelocity = new Vector3(horizontal.x, velocity.y, horizontal.y);
    }

    // The ship's own solid colliders, without furnishings, as a box in the ship's space; made when first asked for.
    private Bounds? hull;

    /// <summary>Whether a point lies within the ship's hull: inside the box around its own colliders.</summary>
    /// <param name="point">World position.</param>
    /// <returns>True inside the hull.</returns>
    public bool Holds(Vector3 point)
    {
        hull ??= MeasureHull();
        return hull.Value.size != Vector3.zero && hull.Value.Contains(transform.InverseTransformPoint(point));
    }

    private Bounds MeasureHull()
    {
        Bounds? bounds = null;
        foreach (Collider collider in GetComponentsInChildren<Collider>(true))
        {
            if (collider.isTrigger || collider.GetComponentInParent<ShipFurniture>() != null) continue;
            Bounds local;
            if (collider is BoxCollider box) local = new Bounds(box.center, box.size);
            else if (collider is MeshCollider mesh && mesh.sharedMesh != null) local = mesh.sharedMesh.bounds;
            else continue;
            Matrix4x4 toShip = transform.worldToLocalMatrix * collider.transform.localToWorldMatrix;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 point = toShip.MultiplyPoint3x4(local.center + Vector3.Scale(local.extents,
                    new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1)));
                if (bounds == null) bounds = new Bounds(point, Vector3.zero);
                else { Bounds grown = bounds.Value; grown.Encapsulate(point); bounds = grown; }
            }
        }
        return bounds ?? new Bounds(Vector3.zero, Vector3.zero);
    }

    private void Awake()
    {
        ship = GetComponent<Ship>();
        body = GetComponent<Rigidbody>();
        upgrades = GetComponent<WhiteHiltShipUpgrades>();
    }

    private void OnEnable() => instances.Add(this);

    private void OnDisable() => instances.Remove(this);

    private void Update()
    {
        if (ship == null) return;
        float target = ship.m_speed == Ship.Speed.Full ? 1f : ship.m_speed == Ship.Speed.Half ? 0.5f : 0.04f;
        sailSize = Mathf.MoveTowards(sailSize, target, Time.deltaTime / SkidbladnirSettings.SailSeconds.Value);
        foreach (Transform sail in m_sails)
            if (sail != null) sail.localScale = new Vector3(1f, sailSize, 1f);
        if (m_fishingNet != null)
            m_fishingNet.SetActive(upgrades != null && upgrades.Has(ShipFishingNet.Bit));
    }
}

/// <summary>Persists a building piece in ship-local coordinates while keeping its ZDO in the current world sector.</summary>
[DefaultExecutionOrder(1000)]
public class ShipFurniture : MonoBehaviour
{
    /// <summary>Saved identity of the parent ship.</summary>
    public const string ParentKey = "whitehilt_ship_parent";
    private static readonly int PositionKey = "whitehilt_ship_local_position".GetStableHashCode();
    private static readonly int RotationKey = "whitehilt_ship_local_rotation".GetStableHashCode();

    private ZNetView view;
    private Transform shipRoot;
    private float nextSync;

    /// <summary>Whether this piece has a persisted ship attachment, including while the ship loads.</summary>
    public bool IsAttached => view != null && view.IsValid() && !view.GetZDO().GetZDOID(ParentKey).IsNone();

    /// <summary>Attaches a newly built piece without changing its world-space appearance.</summary>
    /// <param name="ship">Sailing home beneath the building piece.</param>
    public void Attach(SkidbladnirShip ship)
    {
        view = GetComponent<ZNetView>();
        ZNetView parent = ship.GetComponent<ZNetView>();
        if (view == null || !view.IsValid() || !view.IsOwner() || parent == null || !parent.IsValid()) return;
        ZDO data = view.GetZDO();
        data.Set(ParentKey, parent.GetZDO().m_uid);
        data.Set(PositionKey, ship.transform.InverseTransformPoint(transform.position));
        data.Set(RotationKey, (Quaternion.Inverse(ship.transform.rotation) * transform.rotation).eulerAngles);
        Apply(ship.transform, data);
        SaveWorldPosition();
        enabled = true;
    }

    /// <summary>
    /// Finds the ship a proposed piece belongs on: the ship aimed at, if the piece stands within its hull or on it.
    /// A piece fixed to the ship becomes part of its body, so one beside the hull, on a jetty, would hold it fast.
    /// </summary>
    /// <param name="aimed">The ship whose surface the build ray hit, or null.</param>
    /// <param name="position">World position of the proposed piece.</param>
    /// <returns>The supporting sailing home, or null.</returns>
    public static SkidbladnirShip Supporting(SkidbladnirShip aimed, Vector3 position)
    {
        if (aimed == null) return null;
        // On a wall or high up the ray down below misses the ship, but such a piece stands within the hull.
        return aimed.Holds(position) || Below(position) == aimed ? aimed : null;
    }

    /// <summary>Finds a ship straight below a proposed building position, before any other piece.</summary>
    /// <param name="position">World position of the proposed piece.</param>
    /// <returns>The supporting sailing home, or null.</returns>
    public static SkidbladnirShip Below(Vector3 position)
    {
        RaycastHit[] hits = Physics.RaycastAll(position + Vector3.up * 0.3f, Vector3.down, 5f,
            LayerMask.GetMask("vehicle", "piece", "piece_nonsolid"), QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.GetComponentInParent<SkidbladnirShip>() is SkidbladnirShip ship) return ship;
            if (hit.collider.GetComponentInParent<Piece>() != null) return null;
        }
        return null;
    }

    /// <summary>Whether a ship still has saved furnishings, including pieces outside the active area.</summary>
    /// <param name="ship">Ship whose removal is requested.</param>
    /// <returns>True if any saved ZDO points at the ship.</returns>
    public static bool HasFurniture(GameObject ship)
    {
        ZNetView parent = ship.GetComponent<ZNetView>();
        if (parent == null || !parent.IsValid() || ZDOMan.instance == null) return false;
        ZDOID identity = parent.GetZDO().m_uid;
        foreach (var sector in ZDOMan.instance.m_objectsBySector)
        {
            if (sector == null) continue;
            foreach (ZDO data in sector)
                if (data != null && data.GetZDOID(ParentKey) == identity) return true;
        }
        return false;
    }

    /// <summary>Detaches local network objects before their parent ship is unloaded, preserving saved links.</summary>
    /// <param name="ship">The ship whose view is being reset.</param>
    public static void BeforeShipUnload(GameObject ship)
    {
        foreach (ShipFurniture furniture in ship.GetComponentsInChildren<ShipFurniture>(true))
        {
            if (furniture.view != null && furniture.view.IsValid() && furniture.view.IsOwner()) furniture.SaveWorldPosition();
            furniture.transform.SetParent(null, true);
            furniture.shipRoot = null;
        }
    }

    private void Awake() => view = GetComponent<ZNetView>();

    // Added to every building piece; only furnishings need the per-frame update. Decided at Start, when the network
    // view has surely read its saved data, whatever the order of the piece's components.
    private void Start() => enabled = enabled && IsAttached;

    private void LateUpdate()
    {
        if (!IsAttached) return;
        ZDO data = view.GetZDO();
        if (shipRoot == null)
        {
            GameObject parent = ZNetScene.instance != null ? ZNetScene.instance.FindInstance(data.GetZDOID(ParentKey)) : null;
            if (parent == null || parent.GetComponent<SkidbladnirShip>() == null || !parent.activeInHierarchy
                || !(parent.GetComponent<ZNetView>()?.IsValid() ?? false)) return;
            Apply(parent.transform, data);
        }
        if (view.IsOwner() && Time.unscaledTime >= nextSync)
        {
            SaveWorldPosition();
            nextSync = Time.unscaledTime + SkidbladnirSettings.FurnitureSyncSeconds.Value;
        }
    }

    private void Apply(Transform parent, ZDO data)
    {
        shipRoot = parent;
        transform.SetParent(parent, false);
        transform.localPosition = data.GetVec3(PositionKey, Vector3.zero);
        transform.localRotation = Quaternion.Euler(data.GetVec3(RotationKey, Vector3.zero));
        ZSyncTransform sync = GetComponent<ZSyncTransform>();
        if (sync != null) sync.enabled = false;
        Rigidbody body = GetComponent<Rigidbody>();
        if (body != null) body.isKinematic = true;
        WearNTear wear = GetComponent<WearNTear>();
        if (wear != null) wear.m_noSupportWear = true;
    }

    private void SaveWorldPosition()
    {
        ZDO data = view.GetZDO();
        data.SetPosition(transform.position);
        data.SetRotation(transform.rotation);
    }

    private void OnDestroy()
    {
        if (shipRoot != null && view != null && view.IsValid() && view.IsOwner()) SaveWorldPosition();
    }
}