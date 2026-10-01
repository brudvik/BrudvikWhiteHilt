using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Armors.WhiteHiltUniform;

/// <summary>
/// Keeps the uniform's chest badge on the surface of the body it is worn on: the female chest sits further out.
/// </summary>
public class UniformBadge : MonoBehaviour
{
    // Clearance along the normal, so the bending chest does not swallow the flat badge.
    private const float Lift = 0.003f;

    /// <summary>
    /// Placement on the male body.
    /// </summary>
    public Placement male;

    /// <summary>
    /// Placement on the female body.
    /// </summary>
    public Placement female;

    private VisEquipment equipment;
    private int model = -1;

    /// <summary>
    /// Badge centre and outward normal, in metres along the chest bone's axes.
    /// </summary>
    [Serializable]
    public struct Placement
    {
        /// <summary>
        /// Centre of the badge.
        /// </summary>
        public Vector3 position;

        /// <summary>
        /// Direction the badge faces.
        /// </summary>
        public Vector3 normal;

        /// <summary>
        /// Creates a placement.
        /// </summary>
        /// <param name="position">Centre of the badge.</param>
        /// <param name="normal">Direction the badge faces.</param>
        public Placement(Vector3 position, Vector3 normal)
        {
            this.position = position;
            this.normal = normal.normalized;
        }
    }

    /// <summary>
    /// Moves the badge to a placement.
    /// </summary>
    /// <param name="placement">Where to put it.</param>
    public void Apply(Placement placement)
    {
        transform.localPosition = placement.position + placement.normal * Lift;
        // The disc faces local -z.
        transform.localRotation = Quaternion.LookRotation(-placement.normal, Vector3.up);
    }

    private void Start()
    {
        equipment = GetComponentInParent<VisEquipment>();
        Refresh();
    }

    // The character creator changes the body while the uniform is worn.
    private void Update()
    {
        Refresh();
    }

    private void Refresh()
    {
        int current = equipment != null ? equipment.m_currentModelIndex : 0;
        if (current != model)
        {
            model = current;
            Apply(model == 1 ? female : male);
        }
    }
}
