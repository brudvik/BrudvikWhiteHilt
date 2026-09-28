using BrudvikWhiteHilt.Pieces.Portals.PortalMap;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Portals.PortalAstrolabe;

/// <summary>
/// Hover text of the Portal Astrolabe: whether it stands close enough to a map table to show the portals.
/// The server decides that on its own; this is only what the player sees.
/// </summary>
public class PortalAstrolabeComponent : MonoBehaviour, Hoverable
{
    private const float CheckInterval = 1f;

    private Piece piece;
    private bool nearMapTable;
    private float nextCheck;

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
    public string GetHoverText()
    {
        if (Time.time >= nextCheck)
        {
            nextCheck = Time.time + CheckInterval;
            nearMapTable = FindObjectsByType<MapTable>(FindObjectsSortMode.None)
                .Any(table => Vector3.Distance(table.transform.position, transform.position) <= PortalMapService.ActivationRange);
        }

        return Localization.instance.Localize($"{GetHoverName()}\n{(nearMapTable ? "$whitehilt_astrolabe_active" : "$whitehilt_astrolabe_inactive")}");
    }

    private void Awake()
    {
        piece = GetComponent<Piece>();
    }
}
