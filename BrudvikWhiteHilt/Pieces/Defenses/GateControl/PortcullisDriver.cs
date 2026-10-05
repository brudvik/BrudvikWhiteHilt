using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Defenses.GateControl;

/// <summary>
/// Raises and lowers the portcullis of the stone gatehouse: the layout's group "portcullis", grates and collider, slides
/// down by <see cref="m_drop"/> to the ground. Whether it is lowered is kept on the gatehouse, set by its owner, so every
/// player sees the same; each machine moves the grate towards it.
/// </summary>
public class PortcullisDriver : MonoBehaviour
{
    /// <summary>Metres the portcullis comes down from raised to the ground; the layout's PORTCULLIS_DROP.</summary>
    public const float Drop = 2.75f;

    private const string SetRpc = "WhiteHiltPortcullis";
    private const float Speed = 1.4f;

    private static readonly int loweredKey = "whitehilt_portcullis".GetStableHashCode();

    /// <summary>The portcullis group, at its raised place in the prefab.</summary>
    public Transform m_grate;

    /// <summary>How far it comes down.</summary>
    public float m_drop = Drop;

    private ZNetView nview;
    private Door door;
    private Vector3 rest;
    private float current;
    private bool shown;

    /// <summary>True if the portcullis is down, or on its way down.</summary>
    public bool IsLowered => nview != null && nview.IsValid() && nview.GetZDO().GetBool(loweredKey);

    /// <summary>
    /// Asks the owner of the gatehouse to lower or raise the portcullis.
    /// </summary>
    /// <param name="lowered">True to lower it.</param>
    public void RequestLowered(bool lowered)
    {
        if (nview != null && nview.IsValid())
        {
            nview.InvokeRPC(SetRpc, lowered);
        }
    }

    private void Awake()
    {
        nview = GetComponent<ZNetView>();
        door = GetComponent<Door>();
        if (m_grate == null || nview == null || !nview.IsValid())
        {
            enabled = false;
            return;
        }

        rest = m_grate.localPosition;
        nview.Register<bool>(SetRpc, RPC_SetLowered);
        // A loaded gatehouse shows the portcullis where it is, without moving it there.
        shown = IsLowered;
        current = shown ? 1f : 0f;
        Place();
    }

    private void RPC_SetLowered(long sender, bool lowered)
    {
        if (nview.IsOwner() && IsLowered != lowered)
        {
            nview.GetZDO().Set(loweredKey, lowered);
        }
    }

    private void Update()
    {
        bool lowered = IsLowered;
        if (lowered != shown)
        {
            shown = lowered;
            // The rattle of the gate's own door effects, at the portcullis.
            EffectList effects = door != null ? (lowered ? door.m_closeEffects : door.m_openEffects) : null;
            effects?.Create(m_grate.position, m_grate.rotation);
        }

        float target = lowered ? 1f : 0f;
        if (!Mathf.Approximately(current, target))
        {
            current = Mathf.MoveTowards(current, target, Speed / m_drop * Time.deltaTime);
            Place();
        }
    }

    private void Place()
    {
        m_grate.localPosition = rest + Vector3.down * (m_drop * current);
    }
}
