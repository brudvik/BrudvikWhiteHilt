using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.LogHouse;

/// <summary>
/// Turns shutters and hatch lids with the hidden vanilla door, always the same way. The vanilla door opens away from
/// whoever opens it, so its angle is taken without its sign: a shutter swings out against the wall and a lid lifts,
/// whichever side it is opened from. The Door component and its animation stay in charge of opening, closing and sync.
/// </summary>
public class SwingDriver : MonoBehaviour
{
    /// <summary>The hidden vanilla door whose swing drives the parts.</summary>
    public Transform m_door;

    /// <summary>The parts that turn, each about its pivot.</summary>
    public Transform[] m_parts = new Transform[0];

    /// <summary>
    /// The axis each part turns about, in its parent's space; its direction sets which way it opens, and its length how
    /// far: 1 turns as far as the door, 0.4 turns 0.4 of that (a well sweep's 36 degrees).
    /// </summary>
    public Vector3[] m_axes = new Vector3[0];

    /// <summary>
    /// Parts that hang from a turning part and follow it without turning themselves, such as a bucket on its rod: each
    /// keeps its rest rotation and moves with the point of the part it hangs from that its pivot was on at rest.
    /// </summary>
    public Transform[] m_hangers = new Transform[0];

    /// <summary>For each hanger, the index in <see cref="m_parts"/> of the part it hangs from.</summary>
    public int[] m_hangFrom = new int[0];

    private Quaternion doorRest;
    private Quaternion[] partRests;
    private Vector3[] hangOffsets;

    private void Awake()
    {
        if (m_door == null || m_parts.Length != m_axes.Length || m_hangers.Length != m_hangFrom.Length)
        {
            enabled = false;
            return;
        }

        doorRest = m_door.localRotation;
        partRests = new Quaternion[m_parts.Length];
        for (int i = 0; i < m_parts.Length; i++)
        {
            partRests[i] = m_parts[i] != null ? m_parts[i].localRotation : Quaternion.identity;
        }

        // Where each hanger hangs, in the space of the part it hangs from.
        hangOffsets = new Vector3[m_hangers.Length];
        for (int i = 0; i < m_hangers.Length; i++)
        {
            Transform from = m_hangFrom[i] >= 0 && m_hangFrom[i] < m_parts.Length ? m_parts[m_hangFrom[i]] : null;
            if (m_hangers[i] != null && from != null)
            {
                hangOffsets[i] = Quaternion.Inverse(from.localRotation) * (m_hangers[i].localPosition - from.localPosition);
            }
        }
    }

    private void LateUpdate()
    {
        float open = Mathf.Abs(Mathf.DeltaAngle(0f, (Quaternion.Inverse(doorRest) * m_door.localRotation).eulerAngles.y));
        for (int i = 0; i < m_parts.Length; i++)
        {
            if (m_parts[i] != null)
            {
                m_parts[i].localRotation = Quaternion.AngleAxis(open * m_axes[i].magnitude, m_axes[i]) * partRests[i];
            }
        }

        for (int i = 0; i < m_hangers.Length; i++)
        {
            Transform from = m_hangFrom[i] >= 0 && m_hangFrom[i] < m_parts.Length ? m_parts[m_hangFrom[i]] : null;
            if (m_hangers[i] != null && from != null)
            {
                m_hangers[i].localPosition = from.localPosition + from.localRotation * hangOffsets[i];
            }
        }
    }
}
