using UnityEngine;

namespace BrudvikWhiteHilt.Bestiary;

/// <summary>A readable field guide, independent of the guestbook's event log.</summary>
public sealed class BeastBookStand : MonoBehaviour, Hoverable, Interactable
{
    /// <inheritdoc/>
    public string GetHoverName() => Localization.instance.Localize("$whitehilt_bestiary_title");

    /// <inheritdoc/>
    public string GetHoverText() => Localization.instance.Localize("$whitehilt_bestiary_title\n[<color=yellow><b>$KEY_Use</b></color>] $whitehilt_bestiary_read");

    /// <inheritdoc/>
    public float GetHoverOffset() => 0f;

    /// <inheritdoc/>
    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold || user != Player.m_localPlayer)
        {
            return false;
        }
        BeastBookPanel.Open(this);
        return true;
    }

    /// <inheritdoc/>
    public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;
}