using BrudvikWhiteHilt.Helpers;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Ships;

/// <summary>
/// The working part of a Mooring Post: Use moors the nearest ship that is not moored yet, or casts off the one moored
/// here. One ship per post. The mooring itself lives on the ship (<see cref="ShipMooring"/>).
/// </summary>
public class MooringPostComponent : MonoBehaviour, Hoverable, Interactable
{
    private ZNetView nview;
    private Piece piece;

    /// <summary>Where the rope to the ship leaves the post, in world space.</summary>
    public Vector3 RopePoint => transform.TransformPoint(new Vector3(0f, MooringPost.RopeHeight, 0f));

    /// <inheritdoc/>
    public string GetHoverText()
    {
        if (nview == null || !nview.IsValid())
        {
            return string.Empty;
        }

        string text = GetHoverName();
        ShipMooring moored = ShipMooring.MooredTo(nview.GetZDO().m_uid);
        if (moored != null)
        {
            return Localization.instance.Localize($"{text}\n[<color=yellow><b>$KEY_Use</b></color>] $whitehilt_moor_castoff ({ShipName(moored)})");
        }

        ShipMooring nearest = ShipMooring.Nearest(transform.position, ShipSettings.MooringRange.Value);
        return nearest != null
            ? Localization.instance.Localize($"{text}\n[<color=yellow><b>$KEY_Use</b></color>] $whitehilt_moor_moor ({ShipName(nearest)})")
            : $"{Localization.instance.Localize(text)}\n{string.Format(Localization.instance.Localize("$whitehilt_moor_noship"), Translations.Number(ShipSettings.MooringRange.Value))}";
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

    /// <summary>
    /// Moors the nearest ship, or casts off the moored one.
    /// </summary>
    /// <param name="user">The player.</param>
    /// <param name="hold">True while the key is held.</param>
    /// <param name="alt">True with the alternative key.</param>
    /// <returns>True if something happened.</returns>
    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold || nview == null || !nview.IsValid() || !PrivateArea.CheckAccess(transform.position))
        {
            return false;
        }

        ZDOID self = nview.GetZDO().m_uid;
        ShipMooring moored = ShipMooring.MooredTo(self);
        if (moored != null)
        {
            moored.Moor(ZDOID.None);
            user.Message(MessageHud.MessageType.Center, "$msg_whitehilt_moor_castoff");
            return true;
        }

        ShipMooring nearest = ShipMooring.Nearest(transform.position, ShipSettings.MooringRange.Value);
        if (nearest == null)
        {
            return false;
        }

        nearest.Moor(self);
        user.Message(MessageHud.MessageType.Center, "$msg_whitehilt_moor_moored");
        return true;
    }

    /// <inheritdoc/>
    public bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
        return false;
    }

    private static string ShipName(ShipMooring mooring)
    {
        Piece shipPiece = mooring.Ship != null ? mooring.Ship.GetComponent<Piece>() : null;
        return shipPiece != null ? shipPiece.m_name : string.Empty;
    }

    private void Awake()
    {
        nview = GetComponent<ZNetView>();
        piece = GetComponent<Piece>();
    }
}
