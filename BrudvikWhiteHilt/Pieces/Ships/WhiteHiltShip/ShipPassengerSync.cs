using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Ships.WhiteHiltShip;

/// <summary>
/// On clients that do not own a White Hilt ship, steers its body by velocity toward the owner's extrapolated pose
/// instead of vanilla's per-step position jumps, so passengers are carried smoothly and do not slide while walking.
/// </summary>
public class ShipPassengerSync : MonoBehaviour
{
    // Vanilla ZSyncTransform's own limits: turn at once beyond 45 degrees, extrapolate at most 2 seconds.
    private const float SnapAngle = 45f;
    private const float MaxExtrapolation = 2f;

    private static readonly Dictionary<ZSyncTransform, ShipPassengerSync> instances = new();

    private ZNetView nview;
    private ZSyncTransform sync;
    private Rigidbody body;
    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private float received = -1f;
    private float lastStep = -1f;

    /// <summary>Runs the smoothing in place of vanilla's client sync for a White Hilt ship.</summary>
    /// <param name="sync">Sync component vanilla is about to update.</param>
    /// <returns>True if the ship was handled here; false lets vanilla run.</returns>
    public static bool TryStep(ZSyncTransform sync)
    {
        return instances.Count > 0 && instances.TryGetValue(sync, out ShipPassengerSync ship) && ship.Step();
    }

    private void Awake()
    {
        nview = GetComponent<ZNetView>();
        sync = GetComponent<ZSyncTransform>();
        body = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        if (sync != null)
        {
            instances[sync] = this;
        }
    }

    private void OnDisable()
    {
        if (sync != null)
        {
            instances.Remove(sync);
        }
    }

    // On a passenger's machine: moves the ship's body towards the pose the owner last sent, through its velocity rather
    // than by setting the position, so physics and the players riding it move smoothly instead of in jumps.
    private bool Step()
    {
        if (!ShipSettings.PassengerSmoothing.Value || body == null || nview == null || !nview.IsValid())
        {
            return false;
        }

        ZDO zdo = nview.GetZDO();
        if (zdo.IsOwner() || !zdo.HasOwner())
        {
            received = -1f;
            return false;
        }

        // Riders on the ship call this again in the same step.
        float now = Time.fixedTime;
        if (now == lastStep)
        {
            return true;
        }

        lastStep = now;
        Vector3 position = zdo.GetPosition();
        Quaternion rotation = zdo.GetRotation();
        // Measured from the pose's arrival, not from any ZDO change: inventories and controls change the revision too.
        if (received < 0f || position != lastPosition || rotation != lastRotation)
        {
            lastPosition = position;
            lastRotation = rotation;
            received = now;
        }

        float age = Mathf.Min(now - received, MaxExtrapolation);
        Vector3 velocity = zdo.GetVec3(ZDOVars.s_velHash, Vector3.zero);
        Vector3 bodyVelocity = zdo.GetVec3(ZDOVars.s_bodyVelHash, velocity);
        Vector3 spin = zdo.GetVec3(ZDOVars.s_bodyAVelHash, Vector3.zero);
        Vector3 target = position + velocity * age;
        Quaternion targetRotation = spin.sqrMagnitude > 0f
            ? Quaternion.AngleAxis(spin.magnitude * age * Mathf.Rad2Deg, spin.normalized) * rotation
            : rotation;

        // Anchor and mooring constraints belong to the owner's simulation and would block the correction here.
        if (body.constraints != RigidbodyConstraints.None)
        {
            body.constraints = RigidbodyConstraints.None;
        }

        body.useGravity = false;
        bool snapped = false;
        if (Vector3.Distance(body.position, target) > ShipSettings.PassengerSnapDistance.Value)
        {
            body.position = target;
            transform.position = target;
            snapped = true;
        }

        if (Quaternion.Angle(body.rotation, targetRotation) > SnapAngle)
        {
            body.rotation = targetRotation;
            transform.rotation = targetRotation;
            snapped = true;
        }

        if (snapped)
        {
            Physics.SyncTransforms();
        }

        float smooth = ShipSettings.PassengerSmoothSeconds.Value;
        Vector3 turn = Vector3.zero;
        (targetRotation * Quaternion.Inverse(body.rotation)).ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180f)
        {
            angle -= 360f;
        }

        if (angle != 0f && !float.IsNaN(axis.x) && !float.IsInfinity(axis.x))
        {
            turn = axis * (angle * Mathf.Deg2Rad / smooth);
        }

        body.linearVelocity = bodyVelocity + (target - body.position) / smooth;
        body.angularVelocity = spin + turn;
        return true;
    }
}
