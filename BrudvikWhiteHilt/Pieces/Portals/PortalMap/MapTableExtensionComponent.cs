using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Portals.PortalMap;

/// <summary>
/// Hover text of a map table extension: whether it stands close enough to a map table to show its markers.
/// The server decides that on its own; this is only what the player sees.
/// </summary>
public class MapTableExtensionComponent : MonoBehaviour, Hoverable
{
    private const float CheckInterval = 1f;

    // Serialized, so the value set on the prefab is copied to every placed piece.
    [SerializeField]
    private string activeToken = string.Empty;

    private Piece piece;
    private bool nearMapTable;
    private float nextCheck;

    /// <summary>
    /// Token shown while the extension stands at a map table. Set on the prefab.
    /// </summary>
    public string ActiveToken
    {
        get => activeToken;
        set => activeToken = value;
    }

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

        return Localization.instance.Localize($"{GetHoverName()}\n{(nearMapTable ? activeToken : "$whitehilt_mapextension_inactive")}");
    }

    private void Awake()
    {
        piece = GetComponent<Piece>();
    }
}
