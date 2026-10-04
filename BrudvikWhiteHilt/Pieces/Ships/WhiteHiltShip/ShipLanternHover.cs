using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Ships.WhiteHiltShip;

/// <summary>Lets sailors switch the ship lantern on or off at the lamp.</summary>
public class ShipLanternHover : MonoBehaviour, Hoverable, Interactable
{
    private WhiteHiltShipUpgrades upgrades;

    /// <inheritdoc/>
    public string GetHoverText()
    {
        if (upgrades == null)
            return string.Empty;
        string action = upgrades.IsLanternBlocked ? "$whitehilt_ship_lantern_blocked"
            : "[<color=yellow><b>$KEY_Use</b></color>] "
                + (upgrades.IsLanternOn ? "$whitehilt_ship_lantern_off" : "$whitehilt_ship_lantern_on");
        return Localization.instance.Localize(GetHoverName() + "\n" + action);
    }

    /// <inheritdoc/>
    public string GetHoverName() => "$item_whitehiltshiplantern";

    /// <inheritdoc/>
    public float GetHoverOffset() => 0f;

    /// <inheritdoc/>
    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        return !hold && upgrades != null && upgrades.ToggleLantern(user);
    }

    /// <inheritdoc/>
    public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

    private void Awake()
    {
        upgrades = GetComponentInParent<WhiteHiltShipUpgrades>();
    }
}