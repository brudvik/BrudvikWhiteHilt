using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Roofs;

/// <summary>
/// Marks a White Hilt roof piece with its covering and shape; read by the indoor sound and the eave check.
/// </summary>
public class RoofInfo : MonoBehaviour
{
    private const float CheckInterval = 0.5f;
    private const float MaxRoofDistance = 30f;

    private static readonly RaycastHit[] hits = new RaycastHit[8];
    private static int pieceMask;
    private static float nextCheck;
    private static bool quietAbove;

    /// <summary>The covering.</summary>
    public RoofCovering m_covering;

    /// <summary>The shape.</summary>
    public RoofShape m_shape;

    /// <summary>The pitch.</summary>
    public RoofPitch m_pitch;

    /// <summary>
    /// Whether the nearest roof straight above a point is turf, slate or thatch. Checked twice a second.
    /// </summary>
    /// <param name="point">The point, e.g. the player's head.</param>
    /// <returns>True under a quiet roof.</returns>
    public static bool IsQuietRoofAbove(Vector3 point)
    {
        if (Time.time < nextCheck)
        {
            return quietAbove;
        }

        nextCheck = Time.time + CheckInterval;
        if (pieceMask == 0)
        {
            pieceMask = LayerMask.GetMask("piece");
        }

        quietAbove = false;
        int count = Physics.RaycastNonAlloc(point, Vector3.up, hits, MaxRoofDistance, pieceMask, QueryTriggerInteraction.Ignore);
        float nearest = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            if (hits[i].distance >= nearest)
            {
                continue;
            }

            nearest = hits[i].distance;
            RoofInfo roof = hits[i].collider.GetComponentInParent<RoofInfo>();
            quietAbove = roof != null && RoofFamily.Get(roof.m_covering).Quiet;
        }

        return quietAbove;
    }
}
