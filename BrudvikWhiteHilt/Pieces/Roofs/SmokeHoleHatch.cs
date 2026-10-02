using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Roofs;

/// <summary>
/// The hatch over a smoke hole: use it to open or close it. Open, the smoke of a fire below goes out; closed, it
/// keeps the rain out. It can close by itself when rain starts and open when it stops, unless a fire burns below.
/// </summary>
public class SmokeHoleHatch : MonoBehaviour, Hoverable, Interactable
{
    private const string OpenKey = "whitehilt_hatch_open";
    private const string ToggleRpc = "WhiteHiltHatch";
    private const float OpenAngle = 105f;
    private const float Speed = 180f;
    private const float RainCheckSeconds = 2f;

    private static int pieceMask;

    /// <summary>The hinge the hatch turns on.</summary>
    public Transform m_pivot;

    /// <summary>The hatch's collider, on while it is closed.</summary>
    public Collider m_lidCollider;

    /// <summary>Rotation of the closed hatch.</summary>
    public Quaternion m_closedRotation = Quaternion.identity;

    private ZNetView nview;
    private Piece piece;
    private float angle = OpenAngle;
    private bool? lastWet;
    private float nextRainCheck;

    private void Awake()
    {
        nview = GetComponent<ZNetView>();
        piece = GetComponent<Piece>();
        if (nview != null && nview.GetZDO() != null)
        {
            nview.Register<bool>(ToggleRpc, RPC_Toggle);
            angle = IsOpen() ? OpenAngle : 0f;
        }

        Apply();
    }

    private void Update()
    {
        float target = IsOpen() ? OpenAngle : 0f;
        if (!Mathf.Approximately(angle, target))
        {
            angle = Mathf.MoveTowards(angle, target, Speed * Time.deltaTime);
            Apply();
        }

        if (nview != null && nview.IsValid() && nview.IsOwner() && Time.time >= nextRainCheck)
        {
            nextRainCheck = Time.time + RainCheckSeconds;
            FollowRain();
        }
    }

    /// <inheritdoc/>
    public string GetHoverText()
    {
        if (piece == null || !piece.IsPlacedByPlayer())
        {
            return string.Empty;
        }

        string action = IsOpen() ? "$whitehilt_roof_hatch_open" : "$whitehilt_roof_hatch_closed";
        return Localization.instance.Localize($"{piece.m_name}\n[<color=yellow><b>$KEY_Use</b></color>] {action}");
    }

    /// <inheritdoc/>
    public string GetHoverName()
    {
        return piece != null ? piece.m_name : string.Empty;
    }

    /// <inheritdoc/>
    public float GetHoverOffset()
    {
        return 0f;
    }

    /// <inheritdoc/>
    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold || nview == null || !nview.IsValid() || piece == null || !piece.IsPlacedByPlayer())
        {
            return false;
        }

        if (!PrivateArea.CheckAccess(transform.position))
        {
            return true;
        }

        nview.InvokeRPC(ToggleRpc, !IsOpen());
        return true;
    }

    /// <inheritdoc/>
    public bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
        return false;
    }

    private bool IsOpen()
    {
        return nview == null || nview.GetZDO() == null || nview.GetZDO().GetBool(OpenKey, true);
    }

    private void RPC_Toggle(long sender, bool open)
    {
        if (nview.IsOwner())
        {
            nview.GetZDO().Set(OpenKey, open);
        }
    }

    // Closes on the change from dry to wet and opens on the change back; a hatch someone set by hand stays until then.
    private void FollowRain()
    {
        if (!RoofSettings.HatchFollowsRain.Value || EnvMan.instance == null)
        {
            return;
        }

        bool wet = EnvMan.IsWet();
        if (lastWet.HasValue && lastWet.Value != wet && (!wet || !FireBelow()))
        {
            nview.GetZDO().Set(OpenKey, !wet);
        }

        lastWet = wet;
    }

    private bool FireBelow()
    {
        float range = RoofSettings.HatchFireRange.Value;
        if (range <= 0f)
        {
            return false;
        }

        if (pieceMask == 0)
        {
            pieceMask = LayerMask.GetMask("piece", "piece_nonsolid", "Default", "Default_small");
        }

        foreach (Collider collider in Physics.OverlapSphere(transform.position + Vector3.down * range / 2f, range / 2f + 1f, pieceMask))
        {
            Fireplace fire = collider.GetComponentInParent<Fireplace>();
            if (fire != null && fire.IsBurning())
            {
                return true;
            }
        }

        return false;
    }

    private void Apply()
    {
        if (m_pivot != null)
        {
            m_pivot.localRotation = m_closedRotation * Quaternion.Euler(-angle, 0f, 0f);
        }

        if (m_lidCollider != null)
        {
            m_lidCollider.enabled = angle < 5f;
        }
    }
}
