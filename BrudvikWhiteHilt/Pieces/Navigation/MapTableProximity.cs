using BrudvikWhiteHilt.Navigation;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Navigation;

/// <summary>
/// Tells whether a piece stands within <see cref="NavigationSettings.MapTableRange"/> of a map table, checked at most once a second.
/// </summary>
public class MapTableProximity : MonoBehaviour
{
    private const float CheckInterval = 1f;

    private static readonly List<Piece> nearbyPieces = new();

    private bool nearMapTable;
    private float nextCheck;

    /// <summary>
    /// True if a map table stands within <see cref="NavigationSettings.MapTableRange"/>.
    /// </summary>
    /// <returns>True near a map table.</returns>
    public bool IsNearMapTable()
    {
        if (Time.time >= nextCheck)
        {
            nextCheck = Time.time + CheckInterval;
            nearbyPieces.Clear();
            Piece.GetAllPiecesInRadius(transform.position, NavigationSettings.MapTableRange.Value, nearbyPieces);
            nearMapTable = nearbyPieces.Exists(piece => piece.GetComponent<MapTable>() != null);
        }

        return nearMapTable;
    }
}
