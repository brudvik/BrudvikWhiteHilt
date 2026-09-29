using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Defenses;

/// <summary>
/// A tower ladder. Use it to climb to the next floor up, or with the alternate key to the next floor down.
/// </summary>
public class DefenseLadder : MonoBehaviour, Hoverable, Interactable
{
    // A player standing within this height of a stop counts as being at that stop.
    private const float StopTolerance = 0.6f;

    /// <summary>
    /// Stops from bottom to top, in the piece root's space. Set when the prefab is built.
    /// </summary>
    public Vector3[] m_stops = new Vector3[0];

    private Transform pieceRoot;

    /// <inheritdoc/>
    public string GetHoverText()
    {
        return Localization.instance.Localize(
            "$whitehilt_ladder\n[<color=yellow><b>$KEY_Use</b></color>] $whitehilt_ladder_up\n" +
            "[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] $whitehilt_ladder_down");
    }

    /// <inheritdoc/>
    public string GetHoverName()
    {
        return Localization.instance.Localize("$whitehilt_ladder");
    }

    /// <inheritdoc/>
    public float GetHoverOffset()
    {
        return 0f;
    }

    /// <inheritdoc/>
    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold || user is not Player player || m_stops.Length < 2)
        {
            return false;
        }

        float height = player.transform.position.y;
        Vector3? target = null;
        if (alt)
        {
            for (int i = m_stops.Length - 1; i >= 0 && target == null; i--)
            {
                Vector3 stop = pieceRoot.TransformPoint(m_stops[i]);
                if (stop.y < height - StopTolerance)
                {
                    target = stop;
                }
            }
        }
        else
        {
            for (int i = 0; i < m_stops.Length && target == null; i++)
            {
                Vector3 stop = pieceRoot.TransformPoint(m_stops[i]);
                if (stop.y > height + StopTolerance)
                {
                    target = stop;
                }
            }
        }

        if (target == null)
        {
            return false;
        }

        player.transform.position = target.Value;
        Rigidbody body = player.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.position = target.Value;
            body.linearVelocity = Vector3.zero;
        }

        return true;
    }

    /// <inheritdoc/>
    public bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
        return false;
    }

    private void Awake()
    {
        pieceRoot = GetComponentInParent<Piece>()?.transform ?? transform.parent;
    }
}
