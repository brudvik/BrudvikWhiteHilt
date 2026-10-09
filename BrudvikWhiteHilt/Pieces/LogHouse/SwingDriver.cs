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

    /// <summary>The axis each part turns about, in its parent's space; its direction sets which way it opens.</summary>
    public Vector3[] m_axes = new Vector3[0];

    private Quaternion doorRest;
    private Quaternion[] partRests;

    private void Awake()
    {
        if (m_door == null || m_parts.Length != m_axes.Length)
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
    }

    private void LateUpdate()
    {
        float open = Mathf.Abs(Mathf.DeltaAngle(0f, (Quaternion.Inverse(doorRest) * m_door.localRotation).eulerAngles.y));
        for (int i = 0; i < m_parts.Length; i++)
        {
            if (m_parts[i] != null)
            {
                m_parts[i].localRotation = Quaternion.AngleAxis(open, m_axes[i]) * partRests[i];
            }
        }
    }
}
