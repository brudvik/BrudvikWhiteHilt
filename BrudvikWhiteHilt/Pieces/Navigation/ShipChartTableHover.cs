using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Navigation;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Navigation;

/// <summary>
/// The Navigator's Table's own hover text on its collider: its name, how far the map is uncovered, and Shift + Use to
/// take it back, as on the helm.
/// </summary>
public class ShipChartTableHover : MonoBehaviour, Hoverable, Interactable
{
    private ShipChartTable table;

    /// <inheritdoc/>
    public string GetHoverText()
    {
        string open = Ships.ShipSettings.ShipRoutes.Value ? "\n[<color=yellow><b>$KEY_Use</b></color>] $whitehilt_route_open" : string.Empty;
        return table != null
            ? Localization.instance.Localize(GetHoverName() + open + table.GetHoverText())
            : string.Empty;
    }

    /// <inheritdoc/>
    public string GetHoverName()
    {
        return Translations.Token(Translations.ItemKey(NavigatorTable.PrefabName));
    }

    /// <inheritdoc/>
    public float GetHoverOffset()
    {
        return 0f;
    }

    /// <inheritdoc/>
    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold || table == null)
        {
            return false;
        }

        if (alt)
        {
            return table.Take();
        }

        return user is Player player && player == Player.m_localPlayer && table.Installed
            && ShipRoutePlanner.Open(player, table.GetComponent<ShipRoute>());
    }

    /// <inheritdoc/>
    public bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
        return false;
    }

    private void Awake()
    {
        table = GetComponentInParent<ShipChartTable>();
    }
}
