using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Navigation;

/// <summary>
/// Tells whether a piece stands within 5 m of a map table, checked at most once a second.
/// </summary>
public class MapTableProximity : MonoBehaviour
{
    /// <summary>
    /// Distance in metres to the nearest map table that still counts.
    /// </summary>
    public const float Range = 5f;

    private const float CheckInterval = 1f;

    private static readonly List<Piece> nearbyPieces = new();

    private bool nearMapTable;
    private float nextCheck;

    /// <summary>
    /// True if a map table stands within <see cref="Range"/>.
    /// </summary>
    /// <returns>True near a map table.</returns>
    public bool IsNearMapTable()
    {
        if (Time.time >= nextCheck)
        {
            nextCheck = Time.time + CheckInterval;
            nearbyPieces.Clear();
            Piece.GetAllPiecesInRadius(transform.position, Range, nearbyPieces);
            nearMapTable = nearbyPieces.Exists(piece => piece.GetComponent<MapTable>() != null);
        }

        return nearMapTable;
    }
}
