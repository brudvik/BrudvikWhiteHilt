using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Defenses;

/// <summary>
/// Raises and lowers the drawbridge deck with the hidden vanilla door: a closed door is a raised bridge, an open door a
/// lowered one, so opening, closing, sound and sync stay vanilla. The owner also follows the nearest gate: when the
/// gate opens or closes, the bridge does the same.
/// </summary>
public class DrawbridgeDriver : MonoBehaviour
{
    private const float RaisedAngle = 80f;
    private const float LinkSeconds = 0.5f;

    /// <summary>The hidden vanilla door whose swing drives the deck.</summary>
    public Transform m_door;

    /// <summary>The deck, turning round the hinge.</summary>
    public Transform m_deck;

    /// <summary>The hidden door's closed rotation, captured from the prefab before animation.</summary>
    public Quaternion m_doorRest;

    /// <summary>The deck's lowered rotation, captured from the prefab before animation.</summary>
    public Quaternion m_deckRest;

    private ZNetView nview;
    private Door door;
    private ZNetView linked;
    private int linkedState;
    private bool linkKnown;
    private float nextLink;

    private void Awake()
    {
        nview = GetComponent<ZNetView>();
        door = GetComponent<Door>();
        if (m_door == null || m_deck == null)
        {
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        if (Time.time < nextLink || nview == null || !nview.IsValid() || !nview.IsOwner() || door == null)
        {
            return;
        }

        nextLink = Time.time + LinkSeconds;
        FollowGate();
    }

    private void LateUpdate()
    {
        float open = Mathf.Abs(Mathf.DeltaAngle(0f, (Quaternion.Inverse(m_doorRest) * m_door.localRotation).eulerAngles.y));
        float raised = RaisedAngle * (1f - Mathf.Clamp01(open / 90f));
        m_deck.localRotation = m_deckRest * Quaternion.Euler(-raised, 0f, 0f);
    }

    // Follows the gate's changes only, so the bridge can still be worked by hand in between.
    private void FollowGate()
    {
        float range = DefenseSettings.DrawbridgeLinkRange;
        if (range <= 0f)
        {
            linked = null;
            return;
        }

        if (linked == null || !linked.IsValid())
        {
            linked = FindGate(range);
            linkKnown = false;
            if (linked == null)
            {
                return;
            }
        }

        int state = linked.GetZDO().GetInt(ZDOVars.s_state);
        if (linkKnown && state != linkedState)
        {
            bool gateOpen = state != 0;
            bool bridgeOpen = nview.GetZDO().GetInt(ZDOVars.s_state) != 0;
            if (gateOpen != bridgeOpen)
            {
                nview.GetZDO().Set(ZDOVars.s_state, gateOpen ? 1 : 0);
                door.UpdateState();
            }
        }

        linkedState = state;
        linkKnown = true;
    }

    private ZNetView FindGate(float range)
    {
        ZNetView best = null;
        float bestDistance = range * range;
        foreach (Collider collider in Physics.OverlapSphere(transform.position, range, LayerMask.GetMask("piece", "piece_nonsolid", "Default")))
        {
            Door other = collider.GetComponentInParent<Door>();
            if (other == null || other == door || other.GetComponent<DrawbridgeDriver>() != null)
            {
                continue;
            }

            ZNetView view = other.GetComponent<ZNetView>();
            float distance = (other.transform.position - transform.position).sqrMagnitude;
            if (view != null && view.IsValid() && distance < bestDistance)
            {
                bestDistance = distance;
                best = view;
            }
        }

        return best;
    }
}
