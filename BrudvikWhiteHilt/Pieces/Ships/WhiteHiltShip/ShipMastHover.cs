using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Ships.WhiteHiltShip;

/// <summary>
/// Makes the mast of the White Hilt Ship the place where upgrades are used and taken off, and where the anchor is lowered.
/// </summary>
public class ShipMastHover : MonoBehaviour, Hoverable, Interactable
{
    private WhiteHiltShipUpgrades upgrades;
    private Piece piece;

    /// <inheritdoc/>
    public string GetHoverText()
    {
        return upgrades != null ? upgrades.GetHoverText(GetHoverName()) : string.Empty;
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
    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold || upgrades == null)
        {
            return false;
        }

        return alt ? upgrades.ToggleAnchor(user) : upgrades.TakeLast(user);
    }

    /// <inheritdoc/>
    public bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
        return upgrades != null && upgrades.UseItem(user, item);
    }

    private void Awake()
    {
        upgrades = GetComponentInParent<WhiteHiltShipUpgrades>();
        piece = GetComponentInParent<Piece>();
    }
}
