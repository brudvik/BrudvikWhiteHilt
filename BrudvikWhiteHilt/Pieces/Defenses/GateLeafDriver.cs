using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Defenses;

/// <summary>
/// Turns the gatehouse leaves with the hidden vanilla door. The vanilla Door component and its animation stay in charge
/// of opening, closing and networking; the leaves copy the door's swing, one of them mirrored.
/// </summary>
public class GateLeafDriver : MonoBehaviour
{
    /// <summary>The hidden vanilla door whose animation drives the leaves.</summary>
    public Transform m_door;

    /// <summary>Leaf that turns the same way as the vanilla door.</summary>
    public Transform m_leaf;

    /// <summary>Leaf that turns the opposite way.</summary>
    public Transform m_mirroredLeaf;

    private Quaternion doorRest;
    private Quaternion leafRest;
    private Quaternion mirroredRest;

    private void Awake()
    {
        if (m_door == null)
        {
            enabled = false;
            return;
        }

        doorRest = m_door.localRotation;
        leafRest = m_leaf != null ? m_leaf.localRotation : Quaternion.identity;
        mirroredRest = m_mirroredLeaf != null ? m_mirroredLeaf.localRotation : Quaternion.identity;
    }

    private void LateUpdate()
    {
        float angle = Mathf.DeltaAngle(0f, (Quaternion.Inverse(doorRest) * m_door.localRotation).eulerAngles.y);
        if (m_leaf != null)
        {
            m_leaf.localRotation = leafRest * Quaternion.Euler(0f, angle, 0f);
        }

        if (m_mirroredLeaf != null)
        {
            m_mirroredLeaf.localRotation = mirroredRest * Quaternion.Euler(0f, -angle, 0f);
        }
    }
}
